using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AnimalSaveData
{
    public string animalId;
    public AnimalType animalType;
    public AnimalBirthSource birthSource;
    public int birthDay;
    public int ageDays;
    public int growthDays;
    public int prenatalDays;
    public bool hasBeenBorn;
    public AnimalHealthState health;
    public float fullness;
    public float happiness;
    public float friendship;
    public bool fedToday;
    public bool pettedToday;
    public bool sheltered;
    public bool productReady;
    public int lastProductionDay;
    public string inheritedTrait;
    public bool runtimePurchased;
    public float x;
    public float y;
    public float z;
}

/// <summary>
/// Menangani umur, pertumbuhan harian, care, health, visual stage, production gate,
/// trait, dan persistence satu hewan. Interaksi item tetap ditangani AnimalController.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AnimalController))]
public sealed class AnimalGrowthSystem : MonoBehaviour
{
    static readonly List<AnimalGrowthSystem> Registry = new();

    [Header("Identity & Species")]
    [SerializeField] string animalId = "animal-cow-001";
    [SerializeField] AnimalType animalType = AnimalType.Cow;
    [SerializeField] AnimalBirthSource birthSource = AnimalBirthSource.PurchasedYoung;
    [SerializeField] AnimalGrowthProfileSO growthProfile;
    [SerializeField] string inheritedTrait;
    [SerializeField] bool runtimePurchased;

    [Header("Age & Growth")]
    [SerializeField] int birthDay = 1;
    [SerializeField, Min(0)] int ageDays;
    [SerializeField, Min(0)] int growthDays = 28;
    [SerializeField, Min(0)] int prenatalDays;
    [SerializeField] bool hasBeenBorn = true;

    [Header("Daily Care")]
    [SerializeField, Range(0f, 100f)] float fullness = 50f;
    [SerializeField, Min(0f)] float dailyFullnessLoss = 35f;
    [SerializeField] bool fedToday;
    [SerializeField] bool pettedToday;
    [SerializeField] bool sheltered = true;

    [Header("Condition")]
    [SerializeField] AnimalHealthState health = AnimalHealthState.Healthy;
    [SerializeField, Range(0f, 100f)] float happiness = 50f;
    [SerializeField, Range(0f, 100f)] float friendship;
    [SerializeField, Range(0f, 1f)] float extremeWeatherSicknessChance = 0.35f;

    [Header("Production")]
    [SerializeField] bool productReady;
    [SerializeField] int lastProductionDay = -1000;

    [Header("Stage Visual Slots (Optional)")]
    [SerializeField] Transform visualAnchor;
    [SerializeField] List<AnimalGrowthStageSlot> stageOverrides = new();
    [SerializeField] Vector3 hatchlingOrNewbornScale = new(0.45f, 0.45f, 0.45f);
    [SerializeField] Vector3 babyScale = new(0.6f, 0.6f, 0.6f);
    [SerializeField] Vector3 youngScale = new(0.78f, 0.78f, 0.78f);
    [SerializeField] Vector3 adolescentScale = new(0.9f, 0.9f, 0.9f);
    [SerializeField] Vector3 adultScale = Vector3.one;

    [Header("Dummy Visual Fallback")]
    [Tooltip("Mamalia tanpa model menggunakan Capsule.")]
    [SerializeField] Vector3 mammalDummyShapeScale = new(0.65f, 0.85f, 0.65f);
    [Tooltip("Unggas tanpa model menggunakan Sphere yang dipipihkan seperti lingkaran.")]
    [SerializeField] Vector3 birdDummyShapeScale = new(0.8f, 0.55f, 0.8f);
    [SerializeField] Vector3 eggDummyShapeScale = new(0.55f, 0.72f, 0.55f);

    GameObject spawnedStageModel;
    Renderer[] fallbackRenderers;
    Vector3 originalVisualScale = Vector3.one;
    AnimalGrowthStage appliedStage = (AnimalGrowthStage)(-1);

    public string AnimalId => animalId;
    public AnimalType Type => growthProfile != null ? growthProfile.animalType : animalType;
    public AnimalBirthSource BirthSource => birthSource;
    public int BirthDay => birthDay;
    public int AgeDays => ageDays;
    public int GrowthDays => growthDays;
    public int AdultGrowthDays => growthProfile != null
        ? growthProfile.bornToAdultDays
        : AnimalGrowthProfileSO.DefaultBornToAdultDays(Type);
    public AnimalGrowthStage GrowthStage => ResolveGrowthStage();
    public AnimalHealthState Health => health;
    public float Fullness => fullness;
    public float Happiness => happiness;
    public float Friendship => friendship;
    public bool FedToday => fedToday;
    public bool IsSheltered => sheltered;
    public bool IsAdult => hasBeenBorn && GrowthStage == AnimalGrowthStage.Adult;
    public static int ActiveAnimalCount => Registry.Count;
    public bool HasProductReady => productReady;
    public int ProductQualityLevel => Mathf.Clamp(1 + Mathf.FloorToInt((happiness + friendship) / 50f), 1, 5);

    public bool IsProductionEligible
    {
        get
        {
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
            int interval = growthProfile != null ? growthProfile.productionIntervalDays : 1;
            return IsAdult && fedToday && sheltered && health == AnimalHealthState.Healthy &&
                   currentDay - lastProductionDay >= Mathf.Max(1, interval);
        }
    }

    public string StatusSummary =>
        $"{Type} | {GrowthStage} | Age {ageDays}d\n" +
        $"Growth {growthDays}/{AdultGrowthDays} | Fed: {(fedToday ? "Yes" : "No")}\n" +
        $"Health: {health} | Happy {Mathf.RoundToInt(happiness)} | Friend {Mathf.RoundToInt(friendship)}";

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(animalId))
        {
            animalId = $"{gameObject.scene.name}-{gameObject.name}-" +
                       $"{Mathf.RoundToInt(transform.position.x * 10f)}-{Mathf.RoundToInt(transform.position.z * 10f)}";
        }

        if (visualAnchor == null) visualAnchor = transform;
        originalVisualScale = visualAnchor.localScale;
        fallbackRenderers = visualAnchor.GetComponentsInChildren<Renderer>(true);

        if (birthSource == AnimalBirthSource.PurchasedYoung && growthDays <= 0)
            growthDays = Mathf.Max(0, AdultGrowthDays - PurchasedYoungGrowthDays());

        ApplyGrowthVisual(true);
    }

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
        TimeManager.OnBeforeDayChange += HandleDailyReset;
    }

    void OnDisable()
    {
        Registry.Remove(this);
        TimeManager.OnBeforeDayChange -= HandleDailyReset;
    }

    void HandleDailyReset()
    {
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;

        if (!hasBeenBorn)
        {
            prenatalDays++;
            int requiredDays = growthProfile != null ? growthProfile.incubationOrPregnancyDays : 7;
            if (prenatalDays >= requiredDays)
            {
                hasBeenBorn = true;
                birthDay = currentDay + 1;
                ageDays = 0;
                growthDays = 0;
                ApplyGrowthVisual(true);
            }
            return;
        }

        ageDays++;
        ApplyExtremeWeatherRisk(currentDay);

        // Growth hanya maju jika kebutuhan hari yang baru selesai semuanya terpenuhi.
        if (!IsAdult && fedToday && sheltered && health == AnimalHealthState.Healthy)
            growthDays = Mathf.Min(AdultGrowthDays, growthDays + 1);

        happiness = Mathf.Clamp(
            happiness + (fedToday ? 1f : -5f) + (pettedToday ? 2f : 0f) + (sheltered ? 0f : -3f),
            0f,
            100f
        );
        fullness = Mathf.Max(0f, fullness - dailyFullnessLoss);
        fedToday = false;
        pettedToday = false;
        ApplyGrowthVisual(false);
    }

    void ApplyExtremeWeatherRisk(int currentDay)
    {
        if (sheltered || WeatherSystem.Instance == null || !WeatherSystem.Instance.IsStormToday)
            return;

        System.Random random = new(unchecked(StableHash(animalId) * 397 ^ currentDay));
        if (random.NextDouble() < extremeWeatherSicknessChance)
            health = AnimalHealthState.Sick;
    }

    static int StableHash(string value)
    {
        unchecked
        {
            int hash = 17;
            if (value == null) return hash;
            for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
            return hash;
        }
    }

    public void RegisterFeeding(float fullnessAmount)
    {
        fullness = Mathf.Clamp(fullness + Mathf.Max(0f, fullnessAmount), 0f, 100f);
        fedToday = true;
        happiness = Mathf.Min(100f, happiness + 2f);
    }

    public void Pet()
    {
        if (pettedToday) return;
        pettedToday = true;
        happiness = Mathf.Min(100f, happiness + 3f);
        friendship = Mathf.Min(100f, friendship + 1f);
        SaveLoadFeedback.Instance?.ShowMessage($"{Type} senang dielus. Friendship {Mathf.RoundToInt(friendship)}/100");
    }

    public void SetSheltered(bool value) => sheltered = value;

    public void TreatWithMedicine()
    {
        health = AnimalHealthState.Healthy;
        SaveLoadFeedback.Instance?.ShowMessage($"{Type} sudah sehat kembali");
    }

    public void MarkProductCollected()
    {
        productReady = false;
        lastProductionDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
    }

    public void SetProductReady(bool value) => productReady = value;

    public void InitializePurchasedYoung(int currentDay)
    {
        birthSource = AnimalBirthSource.PurchasedYoung;
        hasBeenBorn = true;
        birthDay = currentDay - Mathf.Max(0, AdultGrowthDays - PurchasedYoungGrowthDays());
        growthDays = Mathf.Max(0, AdultGrowthDays - PurchasedYoungGrowthDays());
        ageDays = growthDays;
        ApplyGrowthVisual(true);
    }

    public void ConfigureShopPurchase(
        AnimalType purchasedType,
        AnimalGrowthProfileSO profile,
        AnimalShopOfferKind offerKind,
        int currentDay)
    {
        animalId = Guid.NewGuid().ToString("N");
        animalType = purchasedType;
        growthProfile = profile;
        runtimePurchased = true;
        prenatalDays = 0;
        ageDays = 0;
        growthDays = 0;
        productReady = false;
        lastProductionDay = -1000;

        if (offerKind == AnimalShopOfferKind.Egg)
        {
            birthSource = AnimalBirthSource.PurchasedEgg;
            hasBeenBorn = false;
            birthDay = currentDay;
        }
        else
        {
            InitializePurchasedYoung(currentDay);
            return;
        }

        ApplyGrowthVisual(true);
    }

    public void PrepareRestoreProfile(AnimalType savedType, AnimalGrowthProfileSO profile)
    {
        animalType = savedType;
        growthProfile = profile;
    }

    public void InitializeBreeding(int currentDay, string trait = "")
    {
        birthSource = AnimalGrowthProfileSO.IsBird(Type)
            ? AnimalBirthSource.HatchedOnFarm
            : AnimalBirthSource.BornOnFarm;
        inheritedTrait = trait;
        birthDay = currentDay;
        prenatalDays = 0;
        ageDays = 0;
        growthDays = 0;
        hasBeenBorn = false;
        ApplyGrowthVisual(true);
    }

    int PurchasedYoungGrowthDays() => growthProfile != null
        ? growthProfile.purchasedYoungToAdultDays
        : AnimalGrowthProfileSO.DefaultPurchasedToAdultDays(Type);

    AnimalGrowthStage ResolveGrowthStage()
    {
        if (!hasBeenBorn)
            return AnimalGrowthProfileSO.IsBird(Type) ? AnimalGrowthStage.Egg : AnimalGrowthStage.Pregnancy;

        AnimalGrowthStageSlot bestSlot = FindStageSlotForDay(growthDays);
        if (bestSlot != null && growthDays < AdultGrowthDays)
            return bestSlot.stage;
        return AnimalGrowthProfileSO.DefaultStage(Type, growthDays, AdultGrowthDays);
    }

    AnimalGrowthStageSlot FindStageSlotForDay(int day)
    {
        IReadOnlyList<AnimalGrowthStageSlot> slots = growthProfile != null
            ? growthProfile.stageSlots
            : stageOverrides;
        AnimalGrowthStageSlot best = null;
        if (slots == null) return null;

        for (int i = 0; i < slots.Count; i++)
        {
            AnimalGrowthStageSlot candidate = slots[i];
            if (candidate != null &&
                candidate.stage is not AnimalGrowthStage.Egg and not AnimalGrowthStage.Pregnancy &&
                candidate.startsOnGrowthDay <= day &&
                (best == null || candidate.startsOnGrowthDay > best.startsOnGrowthDay))
                best = candidate;
        }
        return best;
    }

    AnimalGrowthStageSlot FindExactStageSlot(AnimalGrowthStage stage)
    {
        AnimalGrowthStageSlot profileSlot = growthProfile != null ? growthProfile.GetStageSlot(stage) : null;
        if (profileSlot != null) return profileSlot;
        for (int i = 0; i < stageOverrides.Count; i++)
            if (stageOverrides[i] != null && stageOverrides[i].stage == stage)
                return stageOverrides[i];
        return null;
    }

    void ApplyGrowthVisual(bool force)
    {
        AnimalGrowthStage stage = GrowthStage;
        if (!force && stage == appliedStage) return;
        appliedStage = stage;

        if (spawnedStageModel != null) Destroy(spawnedStageModel);
        spawnedStageModel = null;
        visualAnchor.localScale = originalVisualScale;

        AnimalGrowthStageSlot slot = FindExactStageSlot(stage);
        // Renderer dummy lama disembunyikan; collider root tetap aktif untuk interaksi.
        SetFallbackRenderersVisible(false);

        if (slot != null && slot.modelPrefab != null)
        {
            try
            {
                spawnedStageModel = Instantiate(slot.modelPrefab, visualAnchor);
            }
            catch (InvalidCastException exception)
            {
                Debug.LogWarning($"[ANIMAL] Model stage {Type}/{stage} tidak valid; memakai primitive. {exception.Message}");
            }
        }

        if (spawnedStageModel != null)
        {
            spawnedStageModel.transform.SetLocalPositionAndRotation(
                slot.localOffset,
                Quaternion.Euler(slot.localEulerAngles)
            );
            spawnedStageModel.transform.localScale = slot.scale;
            Animator animator = spawnedStageModel.GetComponentInChildren<Animator>();
            if (animator != null && slot.animatorController != null)
                animator.runtimeAnimatorController = slot.animatorController;
            return;
        }

        SpawnPrimitiveDummy(stage, slot != null ? slot.scale : GetDefaultScale(stage));
    }

    void SpawnPrimitiveDummy(AnimalGrowthStage stage, Vector3 growthScale)
    {
        bool isEgg = stage == AnimalGrowthStage.Egg;
        bool isBird = AnimalGrowthProfileSO.IsBird(Type);
        PrimitiveType primitiveType = isBird || isEgg ? PrimitiveType.Sphere : PrimitiveType.Capsule;
        Vector3 shapeScale = isEgg
            ? eggDummyShapeScale
            : isBird ? birdDummyShapeScale : mammalDummyShapeScale;

        spawnedStageModel = GameObject.CreatePrimitive(primitiveType);
        spawnedStageModel.name = isBird || isEgg
            ? $"Dummy_{Type}_Circle_{stage}"
            : $"Dummy_{Type}_Capsule_{stage}";
        spawnedStageModel.transform.SetParent(visualAnchor, false);
        spawnedStageModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        spawnedStageModel.transform.localScale = Vector3.Scale(shapeScale, growthScale);

        // Collider utama animal tetap menjadi satu-satunya area interaksi.
        Collider generatedCollider = spawnedStageModel.GetComponent<Collider>();
        if (generatedCollider != null) Destroy(generatedCollider);

        Renderer dummyRenderer = spawnedStageModel.GetComponent<Renderer>();
        Material fallbackMaterial = FindFallbackMaterial();
        if (dummyRenderer != null && fallbackMaterial != null)
            dummyRenderer.sharedMaterial = fallbackMaterial;
    }

    /// <summary>
    /// Melompat ke stage visual berikutnya untuk QA. Hanya aktif saat master Debug Clues menyala;
    /// waktu game dan jalur pertumbuhan normal tidak diubah.
    /// </summary>
    public void DebugAdvanceToNextStage()
    {
        if (!HUDManager.DebugCluesEnabled)
            return;

        if (!hasBeenBorn)
        {
            int requiredDays = growthProfile != null ? growthProfile.incubationOrPregnancyDays : 7;
            prenatalDays = Mathf.Max(prenatalDays, requiredDays);
            hasBeenBorn = true;
            ageDays = 0;
            growthDays = 0;
            ApplyGrowthVisual(true);
            SaveLoadFeedback.Instance?.ShowMessage($"DEBUG {Type}: menetas/lahir → {GrowthStage}");
            return;
        }

        int nextGrowthDay = AdultGrowthDays;
        IReadOnlyList<AnimalGrowthStageSlot> slots = growthProfile != null
            ? growthProfile.stageSlots
            : stageOverrides;
        if (slots != null)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                AnimalGrowthStageSlot candidate = slots[index];
                if (candidate != null && candidate.startsOnGrowthDay > growthDays)
                    nextGrowthDay = Mathf.Min(nextGrowthDay, candidate.startsOnGrowthDay);
            }
        }

        growthDays = Mathf.Clamp(nextGrowthDay, 0, AdultGrowthDays);
        ageDays = Mathf.Max(ageDays, growthDays);
        ApplyGrowthVisual(true);
        SaveLoadFeedback.Instance?.ShowMessage($"DEBUG {Type}: {GrowthStage} ({growthDays}/{AdultGrowthDays})");
    }

    Material FindFallbackMaterial()
    {
        if (fallbackRenderers == null) return null;
        for (int i = 0; i < fallbackRenderers.Length; i++)
            if (fallbackRenderers[i] != null && fallbackRenderers[i].sharedMaterial != null)
                return fallbackRenderers[i].sharedMaterial;
        return null;
    }

    Vector3 GetDefaultScale(AnimalGrowthStage stage) => stage switch
    {
        AnimalGrowthStage.Egg => Vector3.one,
        AnimalGrowthStage.Pregnancy => adultScale,
        AnimalGrowthStage.Hatchling or AnimalGrowthStage.Newborn => hatchlingOrNewbornScale,
        AnimalGrowthStage.Baby => babyScale,
        AnimalGrowthStage.Young => youngScale,
        AnimalGrowthStage.Adolescent => adolescentScale,
        _ => adultScale
    };

    void SetFallbackRenderersVisible(bool visible)
    {
        if (fallbackRenderers == null) return;
        for (int i = 0; i < fallbackRenderers.Length; i++)
            if (fallbackRenderers[i] != null)
                fallbackRenderers[i].enabled = visible;
    }

    public AnimalSaveData Capture() => new()
    {
        animalId = animalId,
        animalType = Type,
        birthSource = birthSource,
        birthDay = birthDay,
        ageDays = ageDays,
        growthDays = growthDays,
        prenatalDays = prenatalDays,
        hasBeenBorn = hasBeenBorn,
        health = health,
        fullness = fullness,
        happiness = happiness,
        friendship = friendship,
        fedToday = fedToday,
        pettedToday = pettedToday,
        sheltered = sheltered,
        productReady = productReady,
        lastProductionDay = lastProductionDay,
        inheritedTrait = inheritedTrait,
        runtimePurchased = runtimePurchased,
        x = transform.position.x,
        y = transform.position.y,
        z = transform.position.z
    };

    public void Restore(AnimalSaveData data)
    {
        if (data == null) return;
        animalId = data.animalId;
        animalType = data.animalType;
        birthSource = data.birthSource;
        birthDay = data.birthDay;
        ageDays = Mathf.Max(0, data.ageDays);
        growthDays = Mathf.Clamp(data.growthDays, 0, AdultGrowthDays);
        prenatalDays = Mathf.Max(0, data.prenatalDays);
        hasBeenBorn = data.hasBeenBorn;
        health = data.health;
        fullness = Mathf.Clamp(data.fullness, 0f, 100f);
        happiness = Mathf.Clamp(data.happiness, 0f, 100f);
        friendship = Mathf.Clamp(data.friendship, 0f, 100f);
        fedToday = data.fedToday;
        pettedToday = data.pettedToday;
        sheltered = data.sheltered;
        productReady = data.productReady;
        lastProductionDay = data.lastProductionDay;
        inheritedTrait = data.inheritedTrait;
        runtimePurchased = data.runtimePurchased;
        transform.position = new Vector3(data.x, data.y, data.z);
        ApplyGrowthVisual(true);
    }

    public static List<AnimalSaveData> CaptureAll()
    {
        List<AnimalSaveData> result = new(Registry.Count);
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null)
                result.Add(Registry[i].Capture());
        return result;
    }

    public static void RestoreAll(List<AnimalSaveData> data)
    {
        if (data == null) return;
        Dictionary<string, AnimalSaveData> byId = new();
        for (int i = 0; i < data.Count; i++)
            if (data[i] != null && !string.IsNullOrWhiteSpace(data[i].animalId))
                byId[data[i].animalId] = data[i];

        HashSet<string> restoredIds = new();
        int existingCount = Registry.Count;
        for (int i = 0; i < existingCount; i++)
        {
            if (Registry[i] == null)
                continue;
            if (!byId.TryGetValue(Registry[i].animalId, out AnimalSaveData saved))
            {
                if (Registry[i].runtimePurchased)
                    Destroy(Registry[i].gameObject);
                continue;
            }
            Registry[i].Restore(saved);
            restoredIds.Add(saved.animalId);
        }

        // Hewan hasil pembelian tidak ada di scene awal, sehingga dibuat kembali saat load.
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        for (int i = 0; i < data.Count; i++)
            if (data[i] != null && !restoredIds.Contains(data[i].animalId))
                shop?.RestoreAnimal(data[i]);
    }

    void OnValidate()
    {
        dailyFullnessLoss = Mathf.Max(0f, dailyFullnessLoss);
        fullness = Mathf.Clamp(fullness, 0f, 100f);
        happiness = Mathf.Clamp(happiness, 0f, 100f);
        friendship = Mathf.Clamp(friendship, 0f, 100f);
        extremeWeatherSicknessChance = Mathf.Clamp01(extremeWeatherSicknessChance);
        ClampMinimumScale(ref mammalDummyShapeScale);
        ClampMinimumScale(ref birdDummyShapeScale);
        ClampMinimumScale(ref eggDummyShapeScale);
    }

    static void ClampMinimumScale(ref Vector3 scale)
    {
        scale.x = Mathf.Max(0.01f, scale.x);
        scale.y = Mathf.Max(0.01f, scale.y);
        scale.z = Mathf.Max(0.01f, scale.z);
    }
}
