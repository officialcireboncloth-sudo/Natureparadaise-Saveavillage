using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
/// <summary>
/// Orkestrator interaksi resource di depan player: mencabut weed, menyabit rumput,
/// memukul batu, menebang pohon, membawa hasil, dan memakai stamina/tool level.
/// </summary>
public sealed class PlayerGatheringTool : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Min(0.5f)] float interactionRange = 2.5f;
    [SerializeField, Range(-1f, 1f)] float minimumFacingDot = 0.2f;
    [SerializeField, Min(0.02f)] float scanInterval = 0.08f;

    [Header("Input")]
    [SerializeField] KeyCode pullOrStoreKey = KeyCode.E;
    [SerializeField] KeyCode dropCarriedKey = KeyCode.Q;

    [Header("Stamina")]
    [SerializeField, Min(0f)] float pullCost = 0.05f;
    [SerializeField, Min(0f)] float sickleCost = 0.1f;
    [SerializeField, Min(0f)] float hammerCost = 0.1f;
    [SerializeField, Min(0f)] float axeCost = 0.1f;

    [Header("Tool Levels")]
    [SerializeField, Min(1)] int sickleLevel = 1;
    [SerializeField, Min(1)] int hammerLevel = 1;
    [SerializeField, Min(1)] int axeLevel = 1;

    [Header("Tool Feedback Slots")]
    [SerializeField] Animator animator;
    [SerializeField] string pullTrigger = "Pull";
    [SerializeField] string sickleTrigger = "Sickle";
    [SerializeField] string hammerTrigger = "Hammer";
    [SerializeField] string axeTrigger = "Axe";
    [SerializeField] AudioClip sickleSwish;
    [SerializeField] AudioClip hammerImpact;
    [SerializeField] AudioClip axeImpact;
    [SerializeField] ParticleSystem grassParticles;
    [SerializeField] ParticleSystem stoneParticles;
    [SerializeField] GameObject sickleVisual;
    [SerializeField] GameObject hammerVisual;
    [SerializeField] GameObject axeVisual;

    [Header("Animation Impact Timing")]
    [SerializeField, Min(0f)] float pullImpactDelay = 1.05f;
    [SerializeField, Min(0f), Tooltip("Saat bilah animasi Sickle menyapu di depan player (detik).")]
    float sickleImpactDelay = 0.85f;
    [SerializeField, Min(0f), Tooltip("Saat kepala palu mencapai bagian bawah ayunan Hammering Rock (detik).")]
    float hammerImpactDelay = 0.68f;
    [SerializeField, Min(0f)] float axeImpactDelay = 0.55f;

    PlayerController movement;
    PlayerToolHotbar hotbar;
    PlayerStatusSystem status;
    Inventory inventory;
    AudioSource audioSource;
    TopDownCameraFollow cameraFollow;
    WorldGatherable target;
    WorldTree treeTarget;
    float nextScan;

    ItemSO carriedItem;
    int carriedAmount;
    int carriedQualityStars;
    float carriedFishSizeCm;
    GameObject carriedVisual;
    bool actionBusy;

    int SickleLevel => status != null ? status.GetToolLevel(PlayerToolType.Sickle) : sickleLevel;
    int AxeLevel => status != null ? status.GetToolLevel(PlayerToolType.Axe) : axeLevel;
    public int HammerLevel => status != null ? status.GetToolLevel(PlayerToolType.Hammer) : hammerLevel;
    public bool IsCarrying => carriedItem != null;

    void Awake()
    {
        movement = GetComponent<PlayerController>();
        hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null) hotbar = gameObject.AddComponent<PlayerToolHotbar>();
        status = GetComponent<PlayerStatusSystem>();
        if (status != null)
        {
            if (sickleLevel > 1) status.SetToolLevel(PlayerToolType.Sickle, sickleLevel);
            if (hammerLevel > 1) status.SetToolLevel(PlayerToolType.Hammer, hammerLevel);
            if (axeLevel > 1) status.SetToolLevel(PlayerToolType.Axe, axeLevel);
        }
        inventory = GetComponent<Inventory>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
        // PlayerHeldTools presents the imported models; keep legacy scene visuals hidden.
        if (sickleVisual != null) sickleVisual.SetActive(false);
        if (hammerVisual != null) hammerVisual.SetActive(false);
        if (axeVisual != null) axeVisual.SetActive(false);
        hotbar.SelectionChanged += UpdateToolVisuals;
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void OnDestroy()
    {
        if (hotbar != null) hotbar.SelectionChanged -= UpdateToolVisuals;
    }

    void Update()
    {
        // Target scan diberi interval untuk menghindari pencarian collider setiap frame,
        // tetapi input tool tetap diperiksa setiap frame agar kontrol terasa responsif.
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            RefreshTarget();
        }

        if (actionBusy) return;

        if (carriedItem != null)
        {
            WorldInteractionPrompt.Request(this, transform, $"{pullOrStoreKey}: simpan   {dropCarriedKey}: jatuhkan", 0f, 1.65f);
            if (GameplayInput.GetKeyDown(pullOrStoreKey)) StoreCarriedItem();
            else if (GameplayInput.GetKeyDown(dropCarriedKey)) DropCarriedItem();
            return;
        }

        ShowTargetPrompt();
        if (movement != null && movement.IsMovementLocked) return;

        if (target != null && GameplayInput.GetKeyDown(pullOrStoreKey) && target.CanPull)
            PullTarget();
        else if (hotbar.IsUsePressed(PlayerToolType.Sickle))
            UseSickle();
        else if (hotbar.SelectedTool == PlayerToolType.Hammer && target != null && target.CanHammer &&
            GameplayInput.GetKeyDown(pullOrStoreKey) && PlayerInteractionTarget.Press(pullOrStoreKey))
            UseHammer();
        else if (hotbar.IsUsePressed(PlayerToolType.Hammer))
            UseHammer();
        else if (hotbar.IsUsePressed(PlayerToolType.Axe))
            UseAxe();
    }

    void RefreshTarget()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = transform.forward;
        facing.Normalize();

        WorldGatherable best = null;
        float bestScore = float.PositiveInfinity;
        IReadOnlyList<WorldGatherable> all = WorldGatherable.Active;
        for (int i = 0; i < all.Count; i++)
        {
            WorldGatherable candidate = all[i];
            if (candidate == null || !candidate.IsAvailable) continue;
            Vector3 observer = transform.position + Vector3.up * 0.8f;
            Vector3 interactionPoint = candidate.GetInteractionPoint(observer);
            Vector3 delta = interactionPoint - observer;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > interactionRange) continue;

            // Jika player menyentuh collider, arah dinilai dari pivot sebagai fallback.
            Vector3 direction = delta;
            if (direction.sqrMagnitude < 0.0025f)
            {
                direction = candidate.transform.position - transform.position;
                direction.y = 0f;
            }
            float dot = direction.sqrMagnitude < 0.0025f ? 1f : Vector3.Dot(facing, direction.normalized);
            if (dot < minimumFacingDot) continue;
            float score = distance - dot * 0.5f;
            if (score >= bestScore) continue;
            bestScore = score;
            best = candidate;
        }

        if (target != best)
        {
            if (target != null) target.SetHighlighted(false);
            target = best;
            if (target != null) target.SetHighlighted(hotbar.SelectedTool != PlayerToolType.Axe);
        }

        WorldTree bestTree = null;
        float bestTreeScore = float.PositiveInfinity;
        IReadOnlyList<WorldTree> trees = WorldTree.Active;
        for (int i = 0; i < trees.Count; i++)
        {
            WorldTree candidate = trees[i];
            if (candidate == null || !candidate.IsAvailable) continue;
            Vector3 delta = candidate.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (!PlayerInteractionTarget.Contains(transform, candidate.transform) || distance < 0.01f) continue;
            float dot = Vector3.Dot(facing, delta / distance);
            float score = distance - dot * 0.5f;
            if (score < bestTreeScore) { bestTreeScore = score; bestTree = candidate; }
        }
        if (treeTarget != bestTree)
        {
            if (treeTarget != null) treeTarget.SetHighlighted(false);
            treeTarget = bestTree;
            if (treeTarget != null) treeTarget.SetHighlighted(hotbar.SelectedTool == PlayerToolType.Axe);
        }
        if (target != null) target.SetHighlighted(hotbar.SelectedTool != PlayerToolType.Axe);
        if (treeTarget != null) treeTarget.SetHighlighted(hotbar.SelectedTool == PlayerToolType.Axe);
    }

    void ShowTargetPrompt()
    {
        if (hotbar.SelectedTool == PlayerToolType.Axe && treeTarget != null)
        {
            string treePrompt = AxeLevel < treeTarget.MinimumAxeLevel
                ? "Level Axe belum cukup"
                : $"F: Tebang {(treeTarget.IsStump ? "Tunggul" : "Pohon")}  HP {treeTarget.Durability}";
            WorldInteractionPrompt.Request(this, treeTarget.transform, treePrompt,
                Vector3.Distance(transform.position, treeTarget.transform.position), treeTarget.PromptHeight);
            return;
        }
        if (target == null) return;
        string prompt = null;
        if (hotbar.SelectedTool == PlayerToolType.Hammer && target.CanHammer)
            prompt = HammerLevel < target.MinimumHammerLevel ? "Level Hammer belum cukup" : $"{pullOrStoreKey}: Pecahkan Batu\n<size=17>Stamina digunakan</size>";
        else if (hotbar.SelectedTool == PlayerToolType.Sickle && target.CanSickle)
            prompt = "F: Sabit";
        else if (target.CanPull)
            prompt = $"{pullOrStoreKey}: Cabut";
        else if (target.CanSickle)
            prompt = "Butuh Sickle";
        else if (target.CanHammer)
            prompt = "Butuh Hammer";

        if (prompt != null)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (hotbar.SelectedTool == PlayerToolType.Hammer && target.CanHammer)
                WorldInteractionPrompt.RequestClean(this, target.transform, prompt, distance, target.PromptHeight);
            else WorldInteractionPrompt.Request(this, target.transform, prompt, distance, target.PromptHeight);
        }
    }

    void PullTarget()
    {
        if (!SpendStamina(pullCost)) return;
        FaceGatherable(target);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(pullTrigger);
        WorldGatherable pending = target;
        target = null;
        StartCoroutine(PullImpactRoutine(pending));
    }

    void UseSickle()
    {
        if (!SpendStamina(sickleCost)) return;
        if (target != null && target.CanSickle)
            FaceGatherable(target);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(sickleTrigger);
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        facing.Normalize();
        float radius = 0.85f + SickleLevel * 0.35f;
        Vector3 center = transform.position + facing * 1.35f;
        GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Sickle,center,1.35f);
        StartCoroutine(SickleImpactRoutine(center,radius));
    }

    IEnumerator SickleImpactRoutine(Vector3 center,float radius)
    {
        BeginTimedAction();
        if(sickleImpactDelay>0f) yield return new WaitForSeconds(sickleImpactDelay);
        if (sickleSwish != null) GameAudio.PlayOneShot(audioSource, sickleSwish, GameAudioBus.Main);
        if (grassParticles != null) grassParticles.Play();
        int cut = 0;
        IReadOnlyList<WorldGatherable> all = WorldGatherable.Active;
        for (int i = 0; i < all.Count; i++)
        {
            WorldGatherable candidate = all[i];
            if (candidate != null && candidate.CanSickle && Vector3.Distance(candidate.transform.position, center) <= radius && candidate.Cut(this))
                cut++;
        }
        cut += FieldArea.CutCropsInRadius(center, radius);
        cut += TerrainDetailGrassManager.CutAllInRadius(center, radius);
        SaveLoadFeedback.Instance?.ShowMessage(cut > 0 ? $"Sabit memotong {cut} target" : "Tidak ada rumput di depan");
        if (target != null && !target.IsAvailable) target = null;
        yield return new WaitForSeconds(Mathf.Max(0f,1.35f-sickleImpactDelay));
        EndTimedAction();
    }

    void UseHammer()
    {
        // Input tool tidak boleh bergantung pada interval scan; player bisa berbalik dan
        // langsung memukul pada frame yang sama.
        RefreshTarget();
        if (target == null || !target.CanHammer)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada batu di depan");
            return;
        }
        if (HammerLevel < target.MinimumHammerLevel)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Level Hammer belum cukup");
            return;
        }
        if (!SpendStamina(hammerCost)) return;
        FaceGatherable(target);
        GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Hammer,target.GetInteractionPoint(transform.position+Vector3.up),.95f);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(hammerTrigger);
        WorldGatherable pending=target;
        StartCoroutine(HammerImpactRoutine(pending));
    }

    void UseAxe()
    {
        if (treeTarget == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada pohon di depan");
            return;
        }
        if (AxeLevel < treeTarget.MinimumAxeLevel)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Level Axe belum cukup");
            return;
        }
        if (!SpendStamina(axeCost)) return;
        movement?.FaceTowardsInteraction(treeTarget.transform.position);
        var trunk=treeTarget.GetComponent<Collider>();
        Vector3 hitPoint=trunk!=null?trunk.ClosestPoint(transform.position+Vector3.up*1.8f):treeTarget.transform.position+Vector3.up*1.8f;
        GetComponent<PlayerHeldTools>()?.BeginWorkAction(PlayerToolType.Axe,hitPoint,.95f);
        status?.PulseActivity(PlayerMovementState.ToolAction);
        TriggerAnimation(axeTrigger);
        WorldTree pending=treeTarget;
        StartCoroutine(AxeImpactRoutine(pending));
    }

    void FaceGatherable(WorldGatherable candidate)
    {
        if (movement == null || candidate == null)
            return;

        Vector3 observer = transform.position + Vector3.up * 0.8f;
        Vector3 point = candidate.GetInteractionPoint(observer);
        Vector3 horizontal = point - transform.position;
        horizontal.y = 0f;
        // ClosestPoint dapat sama dengan posisi player saat collider bersentuhan.
        // Pivot objek menjadi fallback supaya arah lock tetap stabil.
        if (horizontal.sqrMagnitude < 0.0025f)
            point = candidate.transform.position;
        movement.FaceTowardsInteraction(point);
    }

    IEnumerator PullImpactRoutine(WorldGatherable pending)
    {
        BeginTimedAction();
        if(pullImpactDelay>0f) yield return new WaitForSeconds(pullImpactDelay);
        if(pending!=null && pending.IsAvailable) pending.Pull(this);
        yield return new WaitForSeconds(Mathf.Max(0f,1.4f-pullImpactDelay));
        EndTimedAction();
    }

    IEnumerator HammerImpactRoutine(WorldGatherable pending)
    {
        BeginTimedAction();
        if(hammerImpactDelay>0f) yield return new WaitForSeconds(hammerImpactDelay);
        if(pending!=null && pending.IsAvailable)
        {
            if (hammerImpact != null) GameAudio.PlayOneShot(audioSource, hammerImpact, GameAudioBus.Main);
            if (stoneParticles != null) stoneParticles.Play();
            pending.Hammer(this,HammerLevel);
            cameraFollow?.AddImpulse(0.09f,0.12f);
            if(target==pending && !pending.IsAvailable) target=null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f,0.95f-hammerImpactDelay));
        EndTimedAction();
    }

    IEnumerator AxeImpactRoutine(WorldTree pending)
    {
        BeginTimedAction();
        if(axeImpactDelay>0f) yield return new WaitForSeconds(axeImpactDelay);
        if(pending!=null && pending.IsAvailable)
        {
            if (axeImpact != null) GameAudio.PlayOneShot(audioSource, axeImpact, GameAudioBus.Main);
            pending.Chop(AxeLevel);
            cameraFollow?.AddImpulse(0.07f,0.1f);
            if(treeTarget==pending && !pending.IsAvailable) treeTarget=null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f,0.95f-axeImpactDelay));
        EndTimedAction();
    }

    void BeginTimedAction()
    {
        actionBusy=true;
        movement?.AcquireMovementLock(this);
    }

    void EndTimedAction()
    {
        movement?.ReleaseMovementLock(this);
        actionBusy=false;
    }

    void OnDisable()
    {
        movement?.ReleaseMovementLock(this);
        actionBusy = false;
    }

    bool SpendStamina(float amount)
    {
        if (status == null || status.TrySpendStamina(amount)) return true;
        SaveLoadFeedback.Instance?.ShowMessage("Stamina tidak cukup");
        return false;
    }

    void TriggerAnimation(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    /// <summary>Mencoba memegang hasil gather sebelum disimpan atau dijatuhkan.</summary>
    public bool TryCarry(ItemSO item, int amount, int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (item == null || amount <= 0 || carriedItem != null) return false;
        PlayerAnimalCarry animalCarry = GetComponent<PlayerAnimalCarry>();
        if (animalCarry != null && animalCarry.HasAnimal) return false;
        carriedItem = item;
        carriedAmount = amount;
        carriedQualityStars = Mathf.Clamp(qualityStars, 0, 4);
        carriedFishSizeCm = Mathf.Max(0f, fishSizeCm);
        carriedVisual = item.worldPrefab != null
            ? Instantiate(item.worldPrefab)
            : GameObject.CreatePrimitive(item.category == ItemCategory.Fish ? PrimitiveType.Sphere : PrimitiveType.Cube);
        carriedVisual.name = $"Carried_{item.itemName}";
        foreach (Collider collider in carriedVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        carriedVisual.transform.SetParent(transform, false);
        carriedVisual.transform.localPosition = new Vector3(0f, 1.45f, 0.38f);
        carriedVisual.transform.localRotation = item.category == ItemCategory.Fish
            ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        carriedVisual.transform.localScale = item.worldPrefab != null
            ? item.worldScale
            : item.category == ItemCategory.Fish
                ? new Vector3(0.58f, 0.24f, 0.22f)
                : new Vector3(0.35f, 0.22f, 0.35f);
        movement?.SetCarrying(true);
        UpdateToolVisuals(hotbar.SelectedTool);
        return true;
    }

    void StoreCarriedItem()
    {
        if (inventory == null || !inventory.Add(carriedItem, carriedAmount, carriedQualityStars, carriedFishSizeCm))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh");
            return;
        }
        QuestEventHub.Publish(QuestObjectiveType.Collect, carriedItem.name, carriedAmount, carriedItem);
        ClearCarry();
    }

    void DropCarriedItem()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        WorldGatherable.SpawnLoosePickup(carriedItem, carriedAmount,
            transform.position + facing.normalized * 1.1f + Vector3.up * 0.25f,
            carriedQualityStars, carriedFishSizeCm);
        ClearCarry();
    }

    void ClearCarry()
    {
        carriedItem = null;
        carriedAmount = 0;
        carriedQualityStars = 0;
        carriedFishSizeCm = 0f;
        if (carriedVisual != null) Destroy(carriedVisual);
        movement?.SetCarrying(false);
        UpdateToolVisuals(hotbar.SelectedTool);
    }

    void UpdateToolVisuals(PlayerToolType selected)
    {
        if (sickleVisual != null) sickleVisual.SetActive(false);
        if (hammerVisual != null) hammerVisual.SetActive(false);
        if (axeVisual != null) axeVisual.SetActive(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerGatheringTool>() == null)
            player.gameObject.AddComponent<PlayerGatheringTool>();
    }
}
