using System.Collections.Generic;
using UnityEngine;

public enum FishingState : byte { Idle, WaitingForBite, Bite, Minigame }

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
/// <summary>Orkestrator cast, bite, hook, minigame, reward, stamina, visual, dan audio fishing.</summary>
public sealed class FishingSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] KeyCode reelKey = KeyCode.F;
    [SerializeField] bool allowLeftMouse = true;

    [Header("Player")]
    [SerializeField, Min(0f)] float castStaminaCost = 0.5f;
    [SerializeField, Min(0.05f)] float reelRiseSpeed = 0.62f;
    [SerializeField, Min(0.05f)] float reelFallSpeed = 0.48f;
    [SerializeField, Min(0.05f)] float lineStressGain = 0.55f;
    [SerializeField, Min(0.05f)] float lineStressRecovery = 0.8f;

    [Header("Bait & Fishing Progress")]
    [SerializeField] ItemSO equippedBait;
    [SerializeField, Min(1)] int fishingLevel = 1;
    [SerializeField, Min(0)] int fishingExperience;

    [Header("Animation (Optional)")]
    [SerializeField] Animator animator;
    [SerializeField] string castTrigger = "Cast";
    [SerializeField] string hookTrigger = "Hook";
    [SerializeField] string catchTrigger = "Catch";

    [Header("Audio / Visual Slots")]
    [SerializeField] AudioClip castSound;
    [SerializeField] AudioClip biteSound;
    [SerializeField] AudioClip reelSound;
    [SerializeField] AudioClip catchSound;
    [SerializeField] GameObject fishingRodVisual;
    [SerializeField] Transform lineOrigin;

    PlayerController movement;
    PlayerToolHotbar hotbar;
    PlayerStatusSystem status;
    Inventory inventory;
    PlayerGatheringTool gathering;
    PlayerAnimalCarry animalCarry;
    InventoryHotbarUI hotbarUI;
    FishingMinigameUI ui;
    AudioSource audioSource;
    FishingSpot activeSpot;
    FishDefinitionSO activeFish;
    ItemSO activeJunk;
    ItemSO activeCastBait;
    int activeCastBaitRemaining;
    GameObject bobber;
    LineRenderer fishingLine;
    float stateTimer;
    float fishPosition = 0.5f;
    float fishDirection = 1f;
    float nextFishTurn;
    float reelPosition = 0.5f;
    float catchProgress;
    float lineStress;
    float minigameElapsed;
    float successfulContactTime;
    Vector3 bobberBasePosition;

    public FishingState State { get; private set; }
    public FishDefinitionSO ActiveFish => activeFish;
    public string ActiveCatchLabel => activeJunk != null ? activeJunk.itemName : activeFish != null ? activeFish.item.itemName : "Fishing";
    public float FishPosition => fishPosition;
    public float ReelPosition => reelPosition;
    public float CatchProgress => catchProgress;
    public float LineStress => lineStress;
    public float HookTimeRemaining => State == FishingState.Bite && activeFish != null ? Mathf.Clamp(stateTimer / activeFish.hookWindow, 0f, 1f) : 0f;
    public bool IsReeling => State == FishingState.Minigame && (Input.GetKey(reelKey) || (allowLeftMouse && Input.GetMouseButton(0)) || (ui != null && ui.MobileReelHeld));
    public ItemSO EquippedBait => ResolveEquippedBait();
    public ItemSO ActiveCastBait => activeCastBait;
    public int EquippedBaitCount => EquippedBait != null && inventory != null ? inventory.GetCount(EquippedBait) : 0;
    public int ActiveCastBaitRemaining => activeCastBaitRemaining;
    public int FishingLevel => fishingLevel;
    public int FishingExperience => fishingExperience;
    public int ExperienceToNextLevel => Mathf.Max(1, fishingLevel * 100);
    public string EquippedBaitLabel => EquippedBait != null ? $"{EquippedBait.itemName} x{EquippedBaitCount}" : "No Bait";

    void Awake()
    {
        movement = GetComponent<PlayerController>();
        hotbar = GetComponent<PlayerToolHotbar>();
        status = GetComponent<PlayerStatusSystem>();
        inventory = GetComponent<Inventory>();
        gathering = GetComponent<PlayerGatheringTool>();
        animalCarry = GetComponent<PlayerAnimalCarry>();
        hotbarUI = GetComponent<InventoryHotbarUI>();
        if (animator == null && movement != null) animator = movement.CharacterAnimator;
        // Source khusus fishing agar loop reel tidak menghentikan footstep/tool audio player.
        audioSource = gameObject.AddComponent<AudioSource>();
        ui = GetComponent<FishingMinigameUI>();
        if (ui == null) ui = gameObject.AddComponent<FishingMinigameUI>();
        ui.Bind(this);
        EnsureRodVisual();
    }

    void OnEnable() => TimeManager.OnBeforeDayChange += CancelForDayChange;
    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= CancelForDayChange;
        CleanupSession(false);
    }

    void Update()
    {
        if (hotbar == null) return;
        UpdateRodVisual();
        if (State != FishingState.Idle && hotbar.SelectedTool != PlayerToolType.FishingRod)
        {
            Fail("Memancing dibatalkan karena pancing dilepas.");
            return;
        }

        ItemSO selectedItem = hotbarUI != null ? hotbarUI.SelectedItem : null;
        if (State == FishingState.Idle && selectedItem != null && selectedItem.IsFishingBait &&
            !WorldInteractionPrompt.IsSuppressed)
        {
            string action = EquippedBait == selectedItem ? "F: Lepas bait" : $"F: Pasang {selectedItem.itemName}";
            WorldInteractionPrompt.Request(this, transform, action, 0f, 1.9f);
            if (hotbar.IsUsePressed(PlayerToolType.None)) ToggleBait(selectedItem);
            return;
        }
        if (hotbar.SelectedTool != PlayerToolType.FishingRod || WorldInteractionPrompt.IsSuppressed) return;

        switch (State)
        {
            case FishingState.Idle:
                WorldInteractionPrompt.Request(this, transform, $"F: Lempar kail ke air  |  Bait: {EquippedBaitLabel}", 0f, 1.9f);
                if (hotbar.IsUsePressed(PlayerToolType.FishingRod)) TryCast();
                break;
            case FishingState.WaitingForBite:
                stateTimer -= Time.deltaTime;
                UpdateLine();
                if (hotbar.IsUsePressed(PlayerToolType.FishingRod)) Fail("Kail ditarik terlalu cepat.");
                else if (stateTimer <= 0f) BeginBite();
                break;
            case FishingState.Bite:
                stateTimer -= Time.deltaTime;
                AnimateBobber();
                WorldInteractionPrompt.Request(this, transform, "BITE! F: HOOK", 0f, 2.15f);
                if (hotbar.IsUsePressed(PlayerToolType.FishingRod)) BeginMinigame();
                else if (stateTimer <= 0f) Fail("Ikan lepas — hook terlambat.");
                break;
            case FishingState.Minigame:
                UpdateMinigame(Time.deltaTime);
                UpdateLine();
                break;
        }
    }

    void TryCast()
    {
        if (movement == null || status == null || inventory == null) return;
        if ((gathering != null && gathering.IsCarrying) || (animalCarry != null && animalCarry.HasAnimal))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Turunkan barang atau hewan sebelum memancing.");
            return;
        }
        Vector3 facing = movement.FacingDirection;
        if (!FishingSpot.TryFindCastPoint(transform, facing, out activeSpot, out Vector3 castPoint))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada Fishing Spot di arah lempar.");
            return;
        }
        int rodLevel = Mathf.Max(1, status.GetToolLevel(PlayerToolType.FishingRod));
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        int hour = TimeManager.Instance != null ? TimeManager.Instance.hour : 12;
        WeatherType weather = WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : WeatherType.Sunny;
        List<FishDefinitionSO> candidates = activeSpot.GetEligibleFish(day, hour, weather, rodLevel);
        ItemSO castBait = ResolveEquippedBait();
        activeFish = PickWeighted(candidates, castBait, fishingLevel);
        if (activeFish == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada ikan aktif di lokasi dan waktu ini.");
            return;
        }
        activeSpot.TryRollJunk(castBait, out activeJunk);
        if (!status.BeginFishing(this, castStaminaCost))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Stamina tidak cukup untuk memancing.");
            activeFish = null;
            activeJunk = null;
            activeSpot = null;
            return;
        }

        activeCastBait = castBait;
        if (activeCastBait != null)
        {
            if (!inventory.Remove(activeCastBait, 1)) activeCastBait = null;
            activeCastBaitRemaining = activeCastBait != null ? inventory.GetCount(activeCastBait) : 0;
            if (activeCastBaitRemaining <= 0) equippedBait = null;
        }
        else activeCastBaitRemaining = 0;

        movement.AcquireMovementLock(this);
        TriggerIfPresent(castTrigger);
        GameAudio.PlayOneShot(audioSource, castSound, GameAudioBus.Main);
        CreateBobber(castPoint);
        float weatherSpeed = Mathf.Max(0.25f, WeatherSystem.CurrentFishingMultiplier);
        float baitSpeed = 1f + (activeCastBait != null ? Mathf.Clamp01(activeCastBait.baitBiteSpeedBonus) : 0f);
        stateTimer = Random.Range(activeFish.minimumBiteWait, activeFish.maximumBiteWait) / (weatherSpeed * baitSpeed);
        State = FishingState.WaitingForBite;
    }

    void BeginBite()
    {
        State = FishingState.Bite;
        stateTimer = activeFish.hookWindow;
        GameAudio.PlayOneShot(audioSource, biteSound, GameAudioBus.Main);
    }

    void BeginMinigame()
    {
        State = FishingState.Minigame;
        TriggerIfPresent(hookTrigger);
        fishPosition = Random.Range(0.25f, 0.75f);
        reelPosition = 0.5f;
        catchProgress = 0.12f;
        lineStress = 0f;
        minigameElapsed = 0f;
        successfulContactTime = 0f;
        nextFishTurn = 0f;
        if (reelSound != null)
        {
            audioSource.clip = reelSound;
            audioSource.loop = true;
            audioSource.volume = GameAudio.Volume(GameAudioBus.Main);
            audioSource.Play();
        }
    }

    void UpdateMinigame(float deltaTime)
    {
        if (activeFish == null) { Fail("Data ikan tidak tersedia."); return; }
        minigameElapsed += deltaTime;
        nextFishTurn -= deltaTime;
        if (nextFishTurn <= 0f)
        {
            float rarityTurnRate = 1f + (int)activeFish.rarity * 0.35f;
            nextFishTurn = Random.Range(0.35f, 1.15f) / rarityTurnRate;
            if (Random.value < 0.72f) fishDirection *= -1f;
        }
        fishPosition += fishDirection * activeFish.fishMoveSpeed * deltaTime;
        if (fishPosition <= 0f || fishPosition >= 1f)
        {
            fishPosition = Mathf.Clamp01(fishPosition);
            fishDirection *= -1f;
        }

        bool reeling = IsReeling;
        reelPosition = Mathf.Clamp01(reelPosition + (reeling ? reelRiseSpeed : -reelFallSpeed) * deltaTime);
        float halfZone = activeFish.catchZoneSize * 0.5f;
        bool touchingFish = Mathf.Abs(reelPosition - fishPosition) <= halfZone;
        float weatherFactor = Mathf.Max(0.5f, WeatherSystem.CurrentFishingMultiplier);
        if (touchingFish)
        {
            catchProgress += activeFish.progressGainPerSecond * weatherFactor * deltaTime;
            successfulContactTime += deltaTime;
        }
        else catchProgress -= activeFish.progressLossPerSecond * deltaTime;
        catchProgress = Mathf.Clamp01(catchProgress);

        bool stressing = reeling && reelPosition >= 0.985f;
        lineStress = Mathf.Clamp01(lineStress + (stressing ? lineStressGain : -lineStressRecovery) * deltaTime);
        if (lineStress >= 1f) { Fail("Tali pancing putus."); return; }
        if (catchProgress >= 1f) { CompleteCatch(); return; }
        if (minigameElapsed >= activeFish.timeLimit || catchProgress <= 0f)
            Fail("Ikan berhasil melepaskan diri.");
    }

    void CompleteCatch()
    {
        float contactRatio = successfulContactTime / Mathf.Max(0.01f, minigameElapsed);
        int qualityStars = Mathf.Clamp(Mathf.FloorToInt(contactRatio * 5f), 0, 4);
        float sizeBias = Mathf.Lerp(0.15f, 0.95f, Mathf.Clamp01(contactRatio));
        float sizeCm = Mathf.Lerp(activeFish.minimumSizeCm, activeFish.maximumSizeCm, Mathf.Clamp01(sizeBias + Random.Range(-0.12f, 0.12f)));
        bool caughtJunk = activeJunk != null;
        ItemSO item = caughtJunk ? activeJunk : activeFish.item;
        if (!caughtJunk) FishCollectionService.Record(activeFish, sizeCm);
        if (caughtJunk) { qualityStars = 0; sizeCm = 0f; }
        int earnedExperience = caughtJunk ? 2 : ExperienceFor(activeFish.rarity);
        AddFishingExperience(earnedExperience);
        TriggerIfPresent(catchTrigger);
        StopReelLoop();
        GameAudio.PlayOneShot(audioSource, catchSound, GameAudioBus.Main);

        bool held = gathering != null && gathering.TryCarry(item, 1, qualityStars, sizeCm);
        if (!held)
        {
            if (inventory.Add(item, 1, qualityStars, sizeCm))
            {
                PlayerPickupNotification.ShowItem(inventory,item,1);
                QuestEventHub.Publish(QuestObjectiveType.Collect, item.name, 1, item);
            }
            else
                WorldGatherable.SpawnLoosePickup(item, 1, transform.position + transform.forward + Vector3.up * 0.3f, qualityStars, sizeCm);
        }
        string destination = held ? "E simpan / Q jatuhkan" : "masuk Inventory";
        string catchDetail = caughtJunk ? "sampah memancing" : $"{sizeCm:0.0} cm, Quality {qualityStars + 1}★";
        SaveLoadFeedback.Instance?.ShowMessage($"{item.itemName} didapat — {catchDetail}, Fishing XP +{earnedExperience} ({destination})");
        CleanupSession(true);
    }

    void Fail(string message)
    {
        SaveLoadFeedback.Instance?.ShowMessage(message);
        CleanupSession(true);
    }

    void CleanupSession(bool keepRodVisible)
    {
        StopReelLoop();
        if (bobber != null) Destroy(bobber);
        bobber = null;
        if (fishingLine != null) Destroy(fishingLine.gameObject);
        fishingLine = null;
        movement?.ReleaseMovementLock(this);
        status?.EndFishing(this);
        activeSpot = null;
        activeFish = null;
        activeJunk = null;
        activeCastBait = null;
        activeCastBaitRemaining = 0;
        State = FishingState.Idle;
        if (!keepRodVisible && fishingRodVisual != null) fishingRodVisual.SetActive(false);
    }

    void CreateBobber(Vector3 point)
    {
        bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bobber.name = "FishingBobber_Runtime";
        bobber.transform.position = point;
        bobberBasePosition = point;
        bobber.transform.localScale = Vector3.one * 0.18f;
        Collider collider = bobber.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        GameObject lineObject = new("FishingLine_Runtime");
        fishingLine = lineObject.AddComponent<LineRenderer>();
        fishingLine.positionCount = 2;
        fishingLine.startWidth = fishingLine.endWidth = 0.025f;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) fishingLine.material = new Material(shader);
        fishingLine.startColor = fishingLine.endColor = Color.white;
        UpdateLine();
    }

    void StopReelLoop()
    {
        if (audioSource == null || !audioSource.loop) return;
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
    }

    void AnimateBobber()
    {
        if (bobber == null) return;
        Vector3 position = bobberBasePosition;
        position.y += Mathf.Sin(Time.time * 22f) * 0.045f;
        bobber.transform.position = position;
        UpdateLine();
    }

    void UpdateLine()
    {
        if (fishingLine == null || bobber == null) return;
        fishingLine.SetPosition(0, lineOrigin != null ? lineOrigin.position : transform.position + Vector3.up * 2f + transform.forward * 0.4f);
        fishingLine.SetPosition(1, bobber.transform.position);
    }

    void EnsureRodVisual()
    {
        if (fishingRodVisual != null) return;
        fishingRodVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fishingRodVisual.name = "FishingRodVisual_Debug";
        fishingRodVisual.transform.SetParent(transform, false);
        fishingRodVisual.transform.localPosition = new Vector3(0.42f, 1.35f, 0.5f);
        fishingRodVisual.transform.localRotation = Quaternion.Euler(72f, 0f, -18f);
        fishingRodVisual.transform.localScale = new Vector3(0.025f, 0.72f, 0.025f);
        Collider collider = fishingRodVisual.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    void UpdateRodVisual()
    {
        if (fishingRodVisual != null)
            fishingRodVisual.SetActive(hotbar != null && hotbar.SelectedTool == PlayerToolType.FishingRod &&
                                       (gathering == null || !gathering.IsCarrying));
    }

    void TriggerIfPresent(string parameter)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameter)) return;
        foreach (AnimatorControllerParameter candidate in animator.parameters)
            if (candidate.type == AnimatorControllerParameterType.Trigger && candidate.name == parameter)
            { animator.SetTrigger(parameter); return; }
    }

    static FishDefinitionSO PickWeighted(List<FishDefinitionSO> candidates, ItemSO bait, int level)
    {
        if (candidates == null || candidates.Count == 0) return null;
        float total = 0f;
        foreach (FishDefinitionSO fish in candidates) total += WeightedChance(fish, bait, level);
        float roll = Random.value * total;
        foreach (FishDefinitionSO fish in candidates)
        {
            roll -= WeightedChance(fish, bait, level);
            if (roll <= 0f) return fish;
        }
        return candidates[^1];
    }

    static float WeightedChance(FishDefinitionSO fish, ItemSO bait, int level)
    {
        float baitMultiplier = bait != null ? bait.GetBaitRarityMultiplier(fish.rarity) : 1f;
        float levelMultiplier = 1f + Mathf.Max(0, level - 1) * 0.015f * (int)fish.rarity;
        return Mathf.Max(0.01f, fish.encounterWeight) * baitMultiplier * levelMultiplier;
    }

    void ToggleBait(ItemSO bait)
    {
        if (bait == null || !bait.IsFishingBait || inventory == null || inventory.GetCount(bait) <= 0) return;
        if (equippedBait == bait)
        {
            equippedBait = null;
            InventoryHotbarUI.TryShowTemporaryMessage("Fishing bait dilepas.", 1.5f);
            return;
        }
        int villageLevel = VillageProgressionService.Instance != null ? VillageProgressionService.Instance.VillageLevel : 1;
        if (!ProgressionRequirementSettings.MeetsFishingLevel(fishingLevel, bait.requiredFishingLevel) ||
            !ProgressionRequirementSettings.MeetsVillageLevel(villageLevel, bait.requiredVillageLevel))
        {
            InventoryHotbarUI.TryShowTemporaryMessage(
                $"Butuh Fishing Lv.{bait.requiredFishingLevel} dan Village Lv.{bait.requiredVillageLevel}.", 2f);
            return;
        }
        equippedBait = bait;
        InventoryHotbarUI.TryShowTemporaryMessage($"{bait.itemName} dipasang — tersisa {inventory.GetCount(bait)}.", 1.8f);
    }

    ItemSO ResolveEquippedBait()
    {
        if (equippedBait != null && equippedBait.IsFishingBait && inventory != null && inventory.GetCount(equippedBait) > 0)
            return equippedBait;
        equippedBait = null;
        return null;
    }

    static int ExperienceFor(FishRarity rarity) => rarity switch
    {
        FishRarity.Uncommon => 25,
        FishRarity.Rare => 45,
        FishRarity.Legendary => 90,
        _ => 15
    };

    void AddFishingExperience(int amount)
    {
        fishingExperience += Mathf.Max(0, amount);
        while (fishingExperience >= ExperienceToNextLevel)
        {
            fishingExperience -= ExperienceToNextLevel;
            fishingLevel++;
            SaveLoadFeedback.Instance?.ShowMessage($"Fishing Level naik menjadi Lv.{fishingLevel}!");
        }
    }

    public void RestoreProgress(string baitItemId, int level, int experience)
    {
        fishingLevel = Mathf.Max(1, level);
        fishingExperience = Mathf.Clamp(experience, 0, ExperienceToNextLevel - 1);
        equippedBait = string.IsNullOrWhiteSpace(baitItemId)
            ? null
            : ItemCatalog.Resolve(baitItemId, null, null);
        ResolveEquippedBait();
    }

    void CancelForDayChange()
    {
        if (State != FishingState.Idle) CleanupSession(true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstalled()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<FishingSystem>() == null)
            player.gameObject.AddComponent<FishingSystem>();
    }
}
