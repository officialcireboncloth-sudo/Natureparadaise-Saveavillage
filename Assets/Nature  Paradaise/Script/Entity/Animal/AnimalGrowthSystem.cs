using System;
using System.Collections.Generic;
using UnityEngine;

public enum AnimalFoodSource : byte
{
    None,
    HandFeed,
    FeedingTrough,
    Grazing,
    AutoFeeder
}

[Serializable]
public sealed class AnimalSceneStageVisual
{
    public AnimalGrowthStage stage;
    public GameObject visual;
}

[Serializable]
public sealed class AnimalSaveData
{
    public string animalId;
    public string animalName;
    public AnimalHeartState heart;
    public int productQuality;
    public string homeId;
    public bool housed;
    public bool returningHome;
    public AnimalType animalType;
    public AnimalBirthSource birthSource;
    public int birthDay;
    public int ageDays;
    public int growthDays;
    public int prenatalDays;
    public bool hasBeenBorn;
    public AnimalHealthState health;
    public AnimalHealthProgress healthProgress;
    public float fullness;
    public float happiness;
    public float friendship;
    public bool fedToday;
    public AnimalFoodSource foodSource;
    public int lastFedDay = -1;
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
    [SerializeField] string animalName;
    [Header("Heart (100 Points = 1 Heart)")]
    [SerializeField] AnimalHeartRules heartRules = new();
    [SerializeField] AnimalHeartState heart;
    [SerializeField] AnimalType animalType = AnimalType.Cow;
    [SerializeField] AnimalBirthSource birthSource = AnimalBirthSource.PurchasedYoung;
    [SerializeField] AnimalGrowthProfileSO growthProfile;
    [Header("Animal Sale Price")]
    [Tooltip("Aktif untuk memakai harga lokal, bukan harga dari Growth Profile.")]
    [SerializeField] bool overrideSalePrice;
    [Tooltip("Dipakai jika override aktif atau Growth Profile belum dipasang.")]
    [SerializeField] AnimalSalePriceSettings localSalePrice = new();
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
    [SerializeField] AnimalFoodSource foodSource;
    [SerializeField] int lastFedDay = -1;
    [SerializeField] bool pettedToday;
    [SerializeField] bool sheltered = true;

    [Header("Condition")]
    [Tooltip("Status kompatibilitas lama: Sick juga mencakup masa pemulihan. Tahap detail ada di Health Progress.")]
    [SerializeField] AnimalHealthState health = AnimalHealthState.Healthy;
    [Tooltip("Durasi dalam hari game; peluang sakit 0..1. Konfigurasi per hewan/prefab.")]
    [SerializeField] AnimalHealthRules healthRules = new();
    [Tooltip("State runtime yang disimpan: tahap penyakit, streak, pemulihan, dan kekebalan.")]
    [SerializeField] AnimalHealthProgress healthProgress = new();
    [SerializeField, Range(0f, 100f)] float happiness = 50f;
    [SerializeField, Range(0f, 100f)] float friendship;
    [Header("Condition Visual Overrides (Optional)")]
    [SerializeField] List<AnimalConditionVisualSlot> conditionVisualOverrides = new();

    [Header("Production")]
    [SerializeField] bool productReady;
    [SerializeField, Range(1, 5)] int productQuality = 1;
    [SerializeField] int lastProductionDay = -1000;

    [Header("Stage Visual Slots (Optional)")]
    [SerializeField] Transform visualAnchor;
    [SerializeField] List<AnimalGrowthStageSlot> stageOverrides = new();
    [SerializeField] Vector3 hatchlingOrNewbornScale = new(0.45f, 0.45f, 0.45f);
    [SerializeField] Vector3 babyScale = new(0.6f, 0.6f, 0.6f);
    [SerializeField] Vector3 youngScale = new(0.78f, 0.78f, 0.78f);
    [SerializeField] Vector3 adolescentScale = new(0.9f, 0.9f, 0.9f);
    [SerializeField] Vector3 adultScale = Vector3.one;

    [Header("Editable Stage Objects")]
    [Tooltip("Use saved child objects for growth stages. Missing/deleted objects stay missing; no model or dummy is spawned.")]
    [SerializeField] bool useSceneStageVisuals;
    [SerializeField] List<AnimalSceneStageVisual> sceneStageVisuals = new();

    [Header("Dummy Visual Fallback")]
    [Tooltip("Mamalia tanpa model menggunakan Capsule.")]
    [SerializeField] Vector3 mammalDummyShapeScale = new(0.65f, 0.85f, 0.65f);
    [Tooltip("Unggas tanpa model menggunakan Sphere yang dipipihkan seperti lingkaran.")]
    [SerializeField] Vector3 birdDummyShapeScale = new(0.8f, 0.55f, 0.8f);
    [SerializeField] Vector3 eggDummyShapeScale = new(0.55f, 0.72f, 0.55f);

    GameObject spawnedStageModel;
    GameObject spawnedConditionModel;
    AnimalIllnessStage appliedCondition = (AnimalIllnessStage)(-1);
    Animator conditionAnimator;
    RuntimeAnimatorController originalConditionAnimator;
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
    public AnimalIllnessStage IllnessStage => healthProgress?.stage ?? AnimalIllnessStage.Healthy;
    public bool CanReceiveMedicine => hasBeenBorn && healthProgress.CanTreat && healthProgress.treatmentDay != CurrentDay;
    public string HealthSummary => healthProgress.stage == AnimalIllnessStage.Recovering
        ? $"Recovering ({healthProgress.recoveryRemaining} care days)"
        : healthProgress.Healthy && healthProgress.immunityRemaining > 0
            ? $"Healthy (Immune {healthProgress.immunityRemaining} days)"
            : healthProgress.stage == AnimalIllnessStage.Mild ? "Unwell"
            : healthProgress.stage == AnimalIllnessStage.Severe ? "Sick"
            : healthProgress.stage == AnimalIllnessStage.Critical ? "Severely Sick"
            : "Healthy";
    public float Fullness => fullness;
    public float Happiness => happiness;
    public float Friendship => HeartPoints / 10f;
    public int HeartPoints => heart != null ? heart.points : Mathf.RoundToInt(friendship * 10f);
    public int HeartLevel => Mathf.Clamp(HeartPoints / 100, 0, 10);
    public string AnimalName => string.IsNullOrWhiteSpace(animalName) ? Type.ToString() : animalName;
    public bool PetToday => pettedToday;
    public bool GrazedToday => heart != null && heart.grazingDay == CurrentDay;
    public bool HasBeenBorn => hasBeenBorn;
    AnimalSalePriceSettings SalePriceSettings => !overrideSalePrice && growthProfile != null && growthProfile.salePrice != null
        ? growthProfile.salePrice : (localSalePrice ??= new AnimalSalePriceSettings());
    public float AnimalValueMultiplier => SalePriceSettings.Multiplier(HeartLevel);
    public int AnimalSellPrice => SalePriceSettings.Calculate(HeartLevel);
    public string InfoSummary => $"{AnimalName} — {Type}\nHeart: {HeartLevel}/10 | Happiness: {(happiness >= 70 ? "Happy" : happiness >= 35 ? "Calm" : "Stressed")}\n" +
        $"Health: {HealthSummary} | Fed: {(fedToday ? $"Yes ({FoodSourceLabel})" : "No")} | Pet: {(pettedToday ? "Yes" : "No")}\n" +
        $"Growth: {GrowthStage} | Production: {(productReady ? "Ready" : "Not Ready")}\n" +
        $"Sell Value: {AnimalSellPrice} G (Heart x{AnimalValueMultiplier:0.##})";
    public bool FedToday => fedToday;
    public AnimalFoodSource FoodSource => fedToday ? foodSource : AnimalFoodSource.None;
    public int LastFedDay => lastFedDay;
    public bool IsSheltered => sheltered;
    public bool IsAdult => hasBeenBorn && GrowthStage == AnimalGrowthStage.Adult;
    public static int ActiveAnimalCount => Registry.Count;
    public static IReadOnlyList<AnimalGrowthSystem> ActiveAnimals => Registry;
    public void SetAnimalName(string value)
    {
        value = value?.Replace("<", "").Replace(">", "").Replace("\n", " ").Replace("\r", " ").Trim();
        animalName = string.IsNullOrEmpty(value) ? Type.ToString() : value.Substring(0, Mathf.Min(24, value.Length));
    }
    public bool HasProductReady => productReady;
    public int ProductQualityLevel => Mathf.Clamp(productQuality, 1, 5);

    public bool IsProductionEligible
    {
        get
        {
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
            int interval = growthProfile != null ? growthProfile.productionIntervalDays : 1;
            return IsAdult && fedToday && happiness >= 20f && (sheltered || CanGrazeToday) && health == AnimalHealthState.Healthy &&
                   currentDay - lastProductionDay >= Mathf.Max(1, interval);
        }
    }

    public string StatusSummary =>
        $"{Type} | {GrowthStage} | Age {ageDays}d\n" +
        $"Growth {growthDays}/{AdultGrowthDays} | Fed: {(fedToday ? $"Yes ({FoodSourceLabel})" : "No")}\n" +
        $"Health: {HealthSummary} | Happy {Mathf.RoundToInt(happiness)} | Heart {HeartPoints}/1000";

    void Awake()
    {
        heart ??= new AnimalHeartState { points = Mathf.Clamp(Mathf.RoundToInt(friendship * 10f), 0, 1000) };
        heartRules ??= new AnimalHeartRules();
        healthRules ??= new AnimalHealthRules();
        healthProgress ??= new AnimalHealthProgress();
        if (health == AnimalHealthState.Sick && healthProgress.Healthy) healthProgress.stage = AnimalIllnessStage.Mild;
        SyncHealth();
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
        if (Registry.Exists(animal => animal != null && animal != this && animal.animalId == animalId))
        {
            string baseId = FormattableString.Invariant($"{gameObject.scene.name}/{gameObject.name}/{transform.position.x:R}/{transform.position.z:R}");
            animalId = baseId;
            int suffix = 1;
            while (Registry.Exists(animal => animal != null && animal != this && animal.animalId == animalId))
                animalId = baseId + "/" + suffix++;
        }
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
        GetComponent<AnimalRoutine>()?.PrepareDailyCare();
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
        if (heart.lastDailyDay >= currentDay) return;

        if (!hasBeenBorn)
        {
            heart.lastDailyDay = currentDay;
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
        bool healthyDuringDay = healthProgress.Healthy;
        RecordHealthExposure();
        healthProgress.EndDay(healthRules, currentDay, fedToday, sheltered, healthRules.stormSicknessChance,
            healthRules.nightSicknessChance,
            new System.Random(unchecked(StableHash(animalId) * 397 ^ currentDay)).NextDouble());
        SyncHealth();
        ApplyConditionVisual(false);
        bool rain = WeatherSystem.Instance != null && WeatherSystem.Instance.IsRainToday;
        bool storm = WeatherSystem.Instance != null && WeatherSystem.Instance.IsStormToday;
        // Hewan yang sedang diobati tidak lagi dianggap sakit tanpa penanganan.
        heart.EndDay(heartRules, currentDay, fedToday, pettedToday, healthProgress.Healthy || healthProgress.stage == AnimalIllnessStage.Recovering, !sheltered, rain, storm);

        // Growth hanya maju jika kebutuhan hari yang baru selesai semuanya terpenuhi.
        if (!IsAdult && fedToday && (sheltered || CanGrazeToday) && healthyDuringDay && health == AnimalHealthState.Healthy)
            growthDays = Mathf.Min(AdultGrowthDays, growthDays + 1);

        happiness = Mathf.Clamp(
            happiness + (fedToday ? 1f : -5f) + (pettedToday ? 2f : 0f) + (!sheltered && (rain || storm) ? -8f : 0f) +
            (healthProgress.stage == AnimalIllnessStage.Critical ? -9f : healthProgress.stage == AnimalIllnessStage.Severe ? -6f : healthProgress.stage == AnimalIllnessStage.Mild ? -3f : healthProgress.stage == AnimalIllnessStage.Recovering ? -1f : 0f),
            0f,
            100f
        );
        fullness = Mathf.Max(0f, fullness - dailyFullnessLoss);
        GetComponent<AnimalController>().RestoreProduction(fullness);
        fedToday = false;
        foodSource = AnimalFoodSource.None;
        pettedToday = false;
        ApplyGrowthVisual(false);
    }

    void Update()
    {
        if (Time.timeScale <= 0 || (TimeManager.Instance != null && TimeManager.Instance.IsPaused)) return;
        RecordHealthExposure();
    }

    void RecordHealthExposure()
    {
        if (!hasBeenBorn || sheltered || WeatherSystem.Instance == null) return;
        WeatherType weather = WeatherSystem.Instance.CurrentWeather;
        float risk = weather switch
        {
            WeatherType.Drizzle => healthRules.drizzleSicknessChance,
            WeatherType.Rain => 0f, // Rain memakai streak Rainy Days agar satu paparan tidak langsung sakit.
            WeatherType.HeavyRain => healthRules.heavyRainSicknessChance,
            WeatherType.WindRainStorm or WeatherType.Thunderstorm => healthRules.stormSicknessChance,
            WeatherType.Heatwave or WeatherType.Cyclone or WeatherType.Blizzard => healthRules.extremeWeatherSicknessChance,
            _ => 0f
        };
        bool extreme = weather is WeatherType.Heatwave or WeatherType.Cyclone or WeatherType.Blizzard;
        healthProgress.ExposeWeather(WeatherSystem.Instance.IsRainToday, WeatherSystem.Instance.IsStormToday, risk, extreme);
        if (TimeManager.Instance != null && TimeManager.Instance.hour >= Mathf.Clamp(healthRules.nightRiskStartsAtHour, 0, 23))
            healthProgress.ExposeNight();
    }

    void SyncHealth() => health = healthProgress.Healthy ? AnimalHealthState.Healthy : AnimalHealthState.Sick;

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

    public bool RegisterFeeding(float fullnessAmount, AnimalFoodSource source = AnimalFoodSource.HandFeed)
    {
        if (!hasBeenBorn || fedToday || fullnessAmount <= 0f) return false;
        fullness = Mathf.Clamp(fullness + Mathf.Max(0f, fullnessAmount), 0f, 100f);
        fedToday = true;
        foodSource = source;
        lastFedDay = CurrentDay;
        if (heart.RewardOnce(ref heart.lastFeedDay, CurrentDay, heartRules.feedingPoints))
            happiness = Mathf.Min(100f, happiness + 2f);
        return true;
    }

    public void Pet()
    {
        if (!hasBeenBorn || pettedToday || !heart.RewardOnce(ref heart.lastPetDay, CurrentDay, heartRules.petPoints)) return;
        pettedToday = true;
        happiness = Mathf.Min(100f, happiness + 3f);
        SaveLoadFeedback.Instance?.ShowMessage($"{AnimalName} senang dielus. Heart {HeartLevel}/10");
    }

    public void SetSheltered(bool value) => sheltered = value;

    public void TreatWithMedicine()
    {
        TreatWithMedicine(AnimalMedicineLevel.Basic);
    }

    public AnimalMedicineResult TreatWithMedicine(AnimalMedicineLevel medicineLevel)
    {
        if (!CanReceiveMedicine) return AnimalMedicineResult.Rejected;
        AnimalMedicineResult result = healthProgress.Treat(healthRules, CurrentDay, medicineLevel);
        if (result == AnimalMedicineResult.Rejected) return result;
        SyncHealth();
        ApplyConditionVisual(false);
        SaveLoadFeedback.Instance?.ShowMessage(result == AnimalMedicineResult.Recovering
            ? $"{AnimalName} mulai recovery. Beri pakan dan istirahatkan di kandang."
            : $"Kondisi {AnimalName} membaik menjadi {HealthSummary}. Berikan obat yang sesuai besok.");
        return result;
    }

    public void MarkProductCollected()
    {
        productReady = false;
        lastProductionDay = TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
    }

    public void SetProductReady(bool value)
    {
        if (value && !productReady)
        {
            if (!IsProductionEligible) return;
            productQuality = heart.RollQuality(happiness, new System.Random(unchecked(StableHash(animalId) * 397 ^ CurrentDay)).NextDouble());
        }
        productReady = value;
    }

    int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : birthDay + ageDays;
    public bool CanGrazeToday => WeatherSystem.Instance == null ||
        WeatherSystem.Instance.CurrentWeather is WeatherType.Sunny or WeatherType.PartlyCloudy;
    public bool CanReceiveTreat => hasBeenBorn && heart.lastTreatDay != CurrentDay;
    public bool GiveFavoriteTreat()
    {
        if (!CanReceiveTreat) return false;
        heart.RewardOnce(ref heart.lastTreatDay, CurrentDay, heartRules.favoriteTreatPoints);
        happiness = Mathf.Min(100f, happiness + 5f);
        return true;
    }

    /// <summary>Hook grazing setelah makan rumput; tidak memberi reward hanya karena keluar kandang.</summary>
    public bool RegisterGrazing()
    {
        if (!hasBeenBorn || fedToday || sheltered || !CanGrazeToday || health != AnimalHealthState.Healthy || heart.grazingDay == CurrentDay) return false;
        heart.grazingDay = CurrentDay;
        return RegisterFeeding(35f, AnimalFoodSource.Grazing);
    }

    public void InitializePurchasedYoung(int currentDay)
    {
        birthSource = AnimalBirthSource.PurchasedYoung;
        hasBeenBorn = true;
        birthDay = currentDay - Mathf.Max(0, AdultGrowthDays - PurchasedYoungGrowthDays());
        growthDays = Mathf.Max(0, AdultGrowthDays - PurchasedYoungGrowthDays());
        ageDays = growthDays;
        ApplyGrowthVisual(true);
        ApplyConditionVisual(true);
    }

    public void ConfigureShopPurchase(
        AnimalType purchasedType,
        AnimalGrowthProfileSO profile,
        AnimalShopOfferKind offerKind,
        int currentDay)
    {
        animalId = Guid.NewGuid().ToString("N");
        heart = new AnimalHeartState();
        animalName = string.Empty;
        fedToday = pettedToday = false;
        healthProgress = new AnimalHealthProgress();
        health = AnimalHealthState.Healthy;
        happiness = fullness = 50f;
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
        ApplyConditionVisual(true);
    }

    public void PrepareRestoreProfile(AnimalType savedType, AnimalGrowthProfileSO profile)
    {
        animalType = savedType;
        growthProfile = profile;
    }

    public void InitializeBreeding(int currentDay, string trait = "")
    {
        animalId = Guid.NewGuid().ToString("N");
        heart = new AnimalHeartState();
        fedToday = pettedToday = false;
        productReady = false;
        healthProgress = new AnimalHealthProgress();
        health = AnimalHealthState.Healthy;
        happiness = fullness = 50f;
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

    AnimalConditionVisualSlot FindConditionSlot(AnimalIllnessStage condition)
    {
        AnimalConditionVisualSlot slot = growthProfile != null ? growthProfile.GetConditionSlot(condition) : null;
        if (slot != null) return slot;
        if (conditionVisualOverrides == null) return null;
        for (int i = 0; i < conditionVisualOverrides.Count; i++)
            if (conditionVisualOverrides[i] != null && conditionVisualOverrides[i].condition == condition)
                return conditionVisualOverrides[i];
        return null;
    }

    void ApplyConditionVisual(bool force)
    {
        // Authored visuals belong to the scene/prefab; condition changes must not replace them.
        if (useSceneStageVisuals) return;
        AnimalIllnessStage condition = healthProgress?.stage ?? AnimalIllnessStage.Healthy;
        if (!force && condition == appliedCondition) return;
        appliedCondition = condition;

        if (conditionAnimator != null) conditionAnimator.runtimeAnimatorController = originalConditionAnimator;
        conditionAnimator = null;
        originalConditionAnimator = null;
        if (spawnedConditionModel != null) Destroy(spawnedConditionModel);
        spawnedConditionModel = null;
        if (spawnedStageModel != null) spawnedStageModel.SetActive(true);
        if (condition == AnimalIllnessStage.Healthy) return;

        AnimalConditionVisualSlot slot = FindConditionSlot(condition);
        if (slot == null) return;
        if (slot.modelPrefab != null)
        {
            try { spawnedConditionModel = Instantiate(slot.modelPrefab, visualAnchor); }
            catch (InvalidCastException exception)
            {
                Debug.LogWarning($"[ANIMAL] Model kondisi {Type}/{condition} tidak valid; visual growth dipertahankan. {exception.Message}");
            }
            if (spawnedConditionModel != null)
            {
                spawnedConditionModel.name = $"Condition_{Type}_{condition}";
                spawnedConditionModel.transform.SetLocalPositionAndRotation(slot.localOffset, Quaternion.Euler(slot.localEulerAngles));
                spawnedConditionModel.transform.localScale = slot.scale;
                foreach (Collider generated in spawnedConditionModel.GetComponentsInChildren<Collider>(true)) Destroy(generated);
                if (spawnedStageModel != null) spawnedStageModel.SetActive(false);
                conditionAnimator = spawnedConditionModel.GetComponentInChildren<Animator>();
            }
        }
        else if (spawnedStageModel != null)
        {
            conditionAnimator = spawnedStageModel.GetComponentInChildren<Animator>();
        }
        if (conditionAnimator != null && slot.animatorController != null)
        {
            originalConditionAnimator = conditionAnimator.runtimeAnimatorController;
            conditionAnimator.runtimeAnimatorController = slot.animatorController;
        }
    }

    void ApplyGrowthVisual(bool force)
    {
        AnimalGrowthStage stage = GrowthStage;
        if (!force && stage == appliedStage) return;
        appliedStage = stage;

        if (useSceneStageVisuals)
        {
            // Only switch the saved stage objects. Keep authored transforms and deletions intact.
            foreach (AnimalSceneStageVisual entry in sceneStageVisuals)
                if (entry != null && entry.visual != null)
                    entry.visual.SetActive(entry.stage == stage);
            return;
        }

        // Model kondisi bergantung pada model growth aktif, jadi bangun ulang sesudah stage berubah.
        if (spawnedConditionModel != null) Destroy(spawnedConditionModel);
        spawnedConditionModel = null;
        conditionAnimator = null;
        originalConditionAnimator = null;
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
            ApplyConditionVisual(true);
            return;
        }

        SpawnPrimitiveDummy(stage, slot != null ? slot.scale : GetDefaultScale(stage));
        ApplyConditionVisual(true);
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
        animalName = animalName,
        heart = heart?.Copy(),
        productQuality = productQuality,
        homeId = GetComponent<AnimalRoutine>()?.HomeId,
        housed = GetComponent<AnimalRoutine>()?.IsHoused ?? false,
        returningHome = GetComponent<AnimalRoutine>()?.Returning ?? false,
        animalType = Type,
        birthSource = birthSource,
        birthDay = birthDay,
        ageDays = ageDays,
        growthDays = growthDays,
        prenatalDays = prenatalDays,
        hasBeenBorn = hasBeenBorn,
        health = health,
        healthProgress = healthProgress.Copy(),
        fullness = fullness,
        happiness = happiness,
        friendship = Friendship,
        fedToday = fedToday,
        foodSource = foodSource,
        lastFedDay = lastFedDay,
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
        animalName = data.animalName;
        heart = data.heart?.Copy() ?? new AnimalHeartState { points = Mathf.Clamp(Mathf.RoundToInt(data.friendship * 10f), 0, 1000) };
        heart.Add(0);
        productQuality = Mathf.Clamp(data.productQuality, 1, 5);
        animalType = data.animalType;
        birthSource = data.birthSource;
        birthDay = data.birthDay;
        ageDays = Mathf.Max(0, data.ageDays);
        growthDays = Mathf.Clamp(data.growthDays, 0, AdultGrowthDays);
        prenatalDays = Mathf.Max(0, data.prenatalDays);
        hasBeenBorn = data.hasBeenBorn;
        healthProgress = data.healthProgress?.Copy() ?? new AnimalHealthProgress
        { stage = data.health == AnimalHealthState.Sick ? AnimalIllnessStage.Mild : AnimalIllnessStage.Healthy };
        SyncHealth();
        fullness = Mathf.Clamp(data.fullness, 0f, 100f);
        happiness = Mathf.Clamp(data.happiness, 0f, 100f);
        friendship = Mathf.Clamp(data.friendship, 0f, 100f);
        fedToday = data.fedToday;
        foodSource = data.fedToday
            ? data.foodSource == AnimalFoodSource.None ? AnimalFoodSource.FeedingTrough : data.foodSource
            : AnimalFoodSource.None;
        lastFedDay = data.lastFedDay > 0 ? data.lastFedDay : heart.lastFeedDay;
        pettedToday = data.pettedToday;
        sheltered = data.sheltered;
        productReady = data.productReady;
        lastProductionDay = data.lastProductionDay;
        inheritedTrait = data.inheritedTrait;
        runtimePurchased = data.runtimePurchased;
        transform.position = new Vector3(data.x, data.y, data.z);
        GetComponent<AnimalController>().RestoreProduction(fullness);
        ApplyGrowthVisual(true);
        AnimalRoutine routine = GetComponent<AnimalRoutine>();
        if (routine == null) routine = gameObject.AddComponent<AnimalRoutine>();
        routine.RestoreHome(data.homeId, data.housed, data.returningHome);
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
            if (data[i] != null && data[i].runtimePurchased && !restoredIds.Contains(data[i].animalId))
                shop?.RestoreAnimal(data[i]);
    }

    void OnValidate()
    {
        dailyFullnessLoss = Mathf.Max(0f, dailyFullnessLoss);
        fullness = Mathf.Clamp(fullness, 0f, 100f);
        happiness = Mathf.Clamp(happiness, 0f, 100f);
        friendship = Mathf.Clamp(friendship, 0f, 100f);
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

    string FoodSourceLabel => foodSource switch
    {
        AnimalFoodSource.HandFeed => "Hand Feed",
        AnimalFoodSource.FeedingTrough => "Trough",
        AnimalFoodSource.Grazing => "Grazing",
        AnimalFoodSource.AutoFeeder => "Auto Feeder",
        _ => "Unknown"
    };
}
