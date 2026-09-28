using System;
using UnityEngine;

/// <summary>Daftar kondisi cuaca yang dapat dijadwalkan oleh sistem cuaca.</summary>
public enum WeatherType
{
    Sunny,
    PartlyCloudy,
    Heatwave,
    Drizzle,
    Rain,
    HeavyRain,
    WindRainStorm,
    Cyclone,
    Thunderstorm,
    Snow,
    Blizzard
}

[DisallowMultipleComponent]
/// <summary>
/// Menentukan cuaca hari ini dan besok secara deterministik, menyimpan forecast,
/// serta meneruskan modifier visual dan farming ke sistem terkait.
/// </summary>
public sealed class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance { get; private set; }

    [Header("Forecast Seed")]
    [SerializeField] int worldWeatherSeed = 18427;
    [SerializeField] WeatherType currentWeather = WeatherType.Sunny;
    [SerializeField] WeatherType tomorrowWeather = WeatherType.PartlyCloudy;
    [SerializeField] WeatherType previousWeather = WeatherType.Sunny;
    [SerializeField] int currentWeatherDay = -1;

    [Header("Gameplay Balancing")]
    [Tooltip("Risiko dasar crop hilang saat Storm. Wind Vulnerability dan stage crop ikut mengalikan nilai ini.")]
    [SerializeField, Range(0f, 1f)] float stormCropLossChance = 0.01f;
    [Tooltip("Risiko dasar crop hilang saat Extreme Weather/Topan.")]
    [SerializeField, Range(0f, 1f)] float extremeCropLossChance = 0.03f;
    [SerializeField, Min(5f)] float lightningCheckInterval = 25f;
    [SerializeField, Range(0f, 1f)] float lightningStrikeChance = 0.08f;
    [Tooltip("Stamina yang hilang setiap pergantian satu jam game ketika player berada di luar saat hujan lebat atau badai.")]
    [SerializeField, Min(0f)] float heavyRainOutdoorStaminaPerHour = 5f;
    [SerializeField] int communityCleanupDay = -1;

    [Header("Thunderstorm Lightning")]
    [Tooltip("AudioSource 2D untuk suara petir. Jika kosong, WeatherSystem membuat AudioSource otomatis saat runtime.")]
    [SerializeField] AudioSource thunderAudioSource;
    [Tooltip("Satu klip akan dipilih secara acak setiap petir muncul.")]
    [SerializeField] AudioClip[] thunderClips;
    [SerializeField, Range(0f, 1f)] float thunderVolume = 0.9f;
    [SerializeField, Min(0.5f)] float minimumThunderInterval = 5f;
    [SerializeField, Min(0.5f)] float maximumThunderInterval = 14f;
    [Tooltip("Tambahan intensitas Directional Light pada puncak kilat.")]
    [SerializeField, Range(0.5f, 5f)] float lightningFlashIntensity = 2.4f;

    [Header("Season Weather Rules")]
    [Tooltip("Kabut tipis Spring hanya muncul pada sebagian pagi agar suasana bervariasi.")]
    [SerializeField, Range(0f, 1f)] float springMorningFogChance = 0.4f;
    [SerializeField, Range(0f, 0.01f)] float springMorningFogDensity = 0.0028f;

    [Header("Fake Cloud Shadow - Atur di sini")]
    [Tooltip("Aktifkan noise shadow yang menempel pada permukaan Terrain.")]
    [SerializeField] bool enableTerrainCloudShadow = true;
    [SerializeField, Range(0.05f, 0.6f)] float terrainCloudShadowOpacity = 0.34f;
    [Tooltip("Ukuran gumpalan noise dalam world unit. Lebih besar menghasilkan bayangan awan yang lebih lebar dan natural.")]
    [SerializeField, Range(30f, 180f)] float terrainCloudNoiseWorldScale = 80f;
    [SerializeField] Vector2 terrainCloudDriftSpeed = new(0.012f, 0.007f);

    [Header("Future Weather Effect Slots")]
    [Tooltip("Belum diaktifkan pada tahap lighting-only.")]
    [SerializeField] GameObject drizzleParticlePrefab;
    [SerializeField] GameObject rainParticlePrefab;
    [SerializeField] GameObject heavyRainParticlePrefab;
    [SerializeField] GameObject windVisualPrefab;
    [SerializeField] GameObject thunderEffectPrefab;
    [SerializeField] GameObject snowParticlePrefab;
    [SerializeField] GameObject blizzardParticlePrefab;
    [SerializeField] Transform effectFollowTarget;

    [Header("Rain Visual Comfort")]
    [Tooltip("Pengali jumlah garis hujan. Nilai rendah lebih nyaman dan tidak menutup gameplay.")]
    [SerializeField, Range(0.1f, 1f)] float rainVisualDensity = 0.65f;
    [Tooltip("Opacity hujan sedang. Gerimis lebih transparan; badai sedikit lebih pekat.")]
    [SerializeField, Range(0.05f, 0.7f)] float rainOpacity = 0.5f;
    [SerializeField] Color rainWaterTint = new(0.42f, 0.68f, 1f, 1f);
    [Tooltip("Material transparan water-drop. Jika kosong, material kompatibel URP dibuat saat runtime.")]
    [SerializeField] Material rainParticleMaterial;
    [SerializeField, Range(0.5f, 2f)] float rainDropWidthScale = 1.4f;
    [SerializeField, Range(0.2f, 1.2f)] float rainStreakLengthScale = 0.8f;
    [SerializeField, Range(0.4f, 1.2f)] float rainFallSpeedScale = 0.82f;

    [Header("Debug")]
    [SerializeField] bool enableDebugKeys = true;
    [SerializeField] KeyCode nextCurrentWeatherKey = KeyCode.F12;
    [Tooltip("None secara default agar tidak berbenturan dengan toggle/load. Dapat diisi manual jika diperlukan.")]
    [SerializeField] KeyCode nextForecastWeatherKey = KeyCode.None;
    bool debugWeatherOverride;
    WeatherType debugWeather;

    public WeatherType CurrentWeather => currentWeather;
    public WeatherType TomorrowWeather => tomorrowWeather;
    public WeatherType PreviousWeather => previousWeather;
    public int CurrentWeatherDay => currentWeatherDay;
    public int WorldWeatherSeed => worldWeatherSeed;
    public bool IsRainToday => IsRainWeather(currentWeather);
    public bool IsStormToday => IsStormWeather(currentWeather);
    public bool IsCommunityCleanupDay => currentWeatherDay == communityCleanupDay;
    public CropSeason CurrentSeason => SeasonVisualController.ResolveSeasonForDay(Mathf.Max(1, currentWeatherDay));
    public bool IsDebugWeatherOverrideActive => debugWeatherOverride;
    public bool TerrainCloudShadowEnabled => enableTerrainCloudShadow;
    public float TerrainCloudShadowOpacity => terrainCloudShadowOpacity;
    public float TerrainCloudNoiseWorldScale => terrainCloudNoiseWorldScale;
    public Vector2 TerrainCloudDriftSpeed => terrainCloudDriftSpeed;
    public float HeavyRainOutdoorStaminaPerHour => heavyRainOutdoorStaminaPerHour;
    public float CurrentLightningFlash => CalculateLightningFlash();
    float nextLightningCheckTime;
    int lightningResolvedDay = -1;
    float nextThunderTime = -1f;
    float lightningFlashStartedAt = -100f;
    GameObject activeWeatherEffect;
    WeatherType activeEffectWeather = (WeatherType)(-1);
    bool activeEffectUsesFallback;
    float nextCoverageRefreshTime;
    Camera weatherCamera;
    static readonly Vector2[] RainViewportCorners =
    {
        new(0f, 0f), new(1f, 0f), new(0f, 1f), new(1f, 1f)
    };

    public event Action<WeatherType, WeatherType> WeatherChanged;
    public static event Action<WeatherType> CurrentWeatherChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (effectFollowTarget == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                effectFollowTarget = player.transform;
        }
        EnsureThunderAudioSource();
    }

    void OnEnable()
    {
        TimeManager.OnBeforeDayChange += HandleBeforeDayChanged;
        TimeManager.OnDay += HandleDayChanged;
        SeasonVisualController.SeasonChanged += HandleSeasonChanged;
    }

    void Start()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        EnsureForecastForDay(day);
        // EnsureForecastForDay sengaja return saat forecast save sudah valid. Efek visual
        // tetap wajib dibuat pada startup walaupun tipe cuacanya tidak berubah.
        if (activeEffectWeather != currentWeather ||
            (IsPrecipitationWeather(currentWeather) && activeWeatherEffect == null))
            NotifyWeatherChanged();
    }

    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= HandleBeforeDayChanged;
        TimeManager.OnDay -= HandleDayChanged;
        SeasonVisualController.SeasonChanged -= HandleSeasonChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        FollowWeatherEffect();
        UpdateLightningHazard();
        UpdateThunderstormLightning();

        if (!enableDebugKeys || !HUDManager.DebugCluesEnabled)
            return;

        if (nextCurrentWeatherKey != KeyCode.None && Input.GetKeyDown(nextCurrentWeatherKey))
        {
            SetDebugWeather(NextWeather(currentWeather));
        }
        if (nextForecastWeatherKey != KeyCode.None && Input.GetKeyDown(nextForecastWeatherKey))
        {
            tomorrowWeather = NextWeather(tomorrowWeather);
            NotifyWeatherChanged();
        }
    }

    void HandleDayChanged()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : currentWeatherDay + 1;
        if (debugWeatherOverride)
        {
            previousWeather = currentWeather;
            currentWeatherDay = day;
            currentWeather = debugWeather;
            tomorrowWeather = debugWeather;
            NotifyWeatherChanged();
            return;
        }
        if (day == currentWeatherDay + 1)
        {
            previousWeather = currentWeather;
            currentWeather = tomorrowWeather;
            currentWeatherDay = day;
            communityCleanupDay = previousWeather == WeatherType.Cyclone ? day : -1;
            tomorrowWeather = GenerateWeather(day + 1);
            NotifyWeatherChanged();
            return;
        }

        EnsureForecastForDay(day);
    }

    void HandleBeforeDayChanged()
    {
        float cropLossChance = GetCropLossChance(currentWeather, stormCropLossChance, extremeCropLossChance);
        if (cropLossChance <= 0f)
            return;

        int lost = 0;
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : currentWeatherDay;
        foreach (FieldArea field in FieldArea.ActiveAreas)
            if (field != null)
                lost += field.ApplyWeatherCropLoss(cropLossChance, unchecked(worldWeatherSeed * 397 ^ day));

        if (lost > 0)
            SaveLoadFeedback.Instance?.ShowMessage($"{GetShortName(currentWeather)} merusak {lost} tanaman.");
    }

    void HandleSeasonChanged(CropSeason _)
    {
        // Pergantian kalender normal sudah menyiapkan forecast untuk musim hari tujuan.
        // Regenerasi di sini hanya dibutuhkan ketika debug menu memaksa suatu musim.
        if (debugWeatherOverride || !SeasonVisualController.IsDebugOverrideActive || currentWeatherDay < 1)
            return;
        currentWeather = GenerateWeather(currentWeatherDay);
        tomorrowWeather = GenerateWeather(currentWeatherDay + 1);
        NotifyWeatherChanged();
    }

    void UpdateLightningHazard()
    {
        if (currentWeather != WeatherType.Thunderstorm || !IsPlayerOutdoors() ||
            (TimeManager.Instance != null && TimeManager.Instance.IsPaused) ||
            currentWeatherDay == lightningResolvedDay || Time.unscaledTime < nextLightningCheckTime)
            return;

        nextLightningCheckTime = Time.unscaledTime + Mathf.Max(5f, lightningCheckInterval);
        int tick = TimeManager.Instance != null ? TimeManager.Instance.hour * 6 + TimeManager.Instance.minute / 10 : Mathf.FloorToInt(Time.unscaledTime);
        System.Random random = new(unchecked(worldWeatherSeed * 31 ^ currentWeatherDay * 397 ^ tick));
        if (random.NextDouble() >= Mathf.Clamp01(lightningStrikeChance))
            return;

        lightningResolvedDay = currentWeatherDay;
        PlayerLifeCycle lifeCycle = FindFirstObjectByType<PlayerLifeCycle>();
        if (lifeCycle != null)
            lifeCycle.RequestWeatherFaint(12, true, "Kamu tersambar petir dan dibawa ke klinik.");
    }

    void UpdateThunderstormLightning()
    {
        if (currentWeather != WeatherType.Thunderstorm)
        {
            nextThunderTime = -1f;
            return;
        }

        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused)
            return;

        if (nextThunderTime < 0f)
        {
            ScheduleNextThunder(true);
            return;
        }

        if (Time.unscaledTime < nextThunderTime)
            return;

        lightningFlashStartedAt = Time.unscaledTime;
        PlayThunderSound();
        ScheduleNextThunder(false);
    }

    void ScheduleNextThunder(bool enteringStorm)
    {
        float minimum = Mathf.Max(0.5f, minimumThunderInterval);
        float maximum = Mathf.Max(minimum, maximumThunderInterval);
        float delay = UnityEngine.Random.Range(minimum, maximum);
        if (enteringStorm)
            delay = Mathf.Min(delay, UnityEngine.Random.Range(2f, Mathf.Min(6f, maximum) + 0.01f));
        nextThunderTime = Time.unscaledTime + delay;
    }

    void PlayThunderSound()
    {
        EnsureThunderAudioSource();
        if (thunderAudioSource == null || thunderClips == null || thunderClips.Length == 0)
            return;

        AudioClip clip = thunderClips[UnityEngine.Random.Range(0, thunderClips.Length)];
        if (clip == null)
            return;

        thunderAudioSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        GameAudio.PlayOneShot(thunderAudioSource, clip, GameAudioBus.Main, thunderVolume);
    }

    void EnsureThunderAudioSource()
    {
        if (thunderAudioSource != null)
            return;
        thunderAudioSource = gameObject.AddComponent<AudioSource>();
        thunderAudioSource.playOnAwake = false;
        thunderAudioSource.loop = false;
        thunderAudioSource.spatialBlend = 0f;
    }

    float CalculateLightningFlash()
    {
        if (currentWeather != WeatherType.Thunderstorm)
            return 0f;

        float elapsed = Time.unscaledTime - lightningFlashStartedAt;
        float pulse = 0f;
        if (elapsed >= 0f && elapsed < 0.09f)
            pulse = 1f - elapsed / 0.09f;
        else if (elapsed >= 0.15f && elapsed < 0.23f)
            pulse = 0.72f * (1f - (elapsed - 0.15f) / 0.08f);
        return pulse * lightningFlashIntensity;
    }

    /// <summary>Memastikan current/tomorrow weather sudah tersedia untuk hari game tertentu.</summary>
    public void EnsureForecastForDay(int day)
    {
        day = Mathf.Max(1, day);
        if (currentWeatherDay == day)
            return;

        currentWeatherDay = day;
        currentWeather = GenerateWeather(day);
        tomorrowWeather = GenerateWeather(day + 1);
        NotifyWeatherChanged();
    }

    /// <summary>Mengembalikan forecast persis dari save agar ramalan tidak berubah setelah load.</summary>
    public void RestoreForecast(int savedDay, int savedSeed, WeatherType savedCurrent, WeatherType savedTomorrow,
        WeatherType savedPrevious = WeatherType.Sunny, int savedCleanupDay = -1)
    {
        debugWeatherOverride = false;
        worldWeatherSeed = savedSeed;
        currentWeatherDay = Mathf.Max(1, savedDay);
        currentWeather = ClampWeather(savedCurrent);
        tomorrowWeather = ClampWeather(savedTomorrow);
        previousWeather = ClampWeather(savedPrevious);
        communityCleanupDay = savedCleanupDay;
        NotifyWeatherChanged();
    }

    /// <summary>Memaksa cuaca untuk pengujian sampai AUTO WEATHER dipilih.</summary>
    public void SetDebugWeather(WeatherType weather)
    {
        debugWeatherOverride = true;
        debugWeather = ClampWeather(weather);
        currentWeather = debugWeather;
        tomorrowWeather = debugWeather;
        if (currentWeatherDay < 1)
            currentWeatherDay = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : 1;
        NotifyWeatherChanged();
        SaveLoadFeedback.Instance?.ShowMessage($"DEBUG WEATHER: {GetShortName(currentWeather)}");
    }

    /// <summary>Melepas override debug dan menghitung ulang cuaca berdasarkan season aktif.</summary>
    public void FollowSeasonWeather()
    {
        debugWeatherOverride = false;
        int day = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : Mathf.Max(1, currentWeatherDay);
        currentWeatherDay = day;
        currentWeather = GenerateWeather(day);
        tomorrowWeather = GenerateWeather(day + 1);
        NotifyWeatherChanged();
        SaveLoadFeedback.Instance?.ShowMessage($"WEATHER AUTO: {GetShortName(currentWeather)}");
    }

    /// <summary>Menyediakan modifier lighting untuk cuaca aktif tanpa mengubah lampu secara langsung.</summary>
    public void GetLightingModifiers(out float sunMultiplier, out float ambientMultiplier, out float exposureMultiplier, out Color tint)
    {
        sunMultiplier = 1f;
        ambientMultiplier = 1f;
        exposureMultiplier = 1f;
        tint = Color.white;

        switch (currentWeather)
        {
            case WeatherType.PartlyCloudy:
                sunMultiplier = 0.72f; ambientMultiplier = 0.9f; exposureMultiplier = 0.85f; tint = new Color(0.88f, 0.92f, 1f); break;
            case WeatherType.Heatwave:
                sunMultiplier = 1.18f; ambientMultiplier = 1.02f; exposureMultiplier = 1.08f; tint = new Color(1f, 0.88f, 0.7f); break;
            case WeatherType.Drizzle:
                sunMultiplier = 0.66f; ambientMultiplier = 0.88f; exposureMultiplier = 0.8f; tint = new Color(0.82f, 0.87f, 0.94f); break;
            case WeatherType.Rain:
                sunMultiplier = 0.46f; ambientMultiplier = 0.76f; exposureMultiplier = 0.62f; tint = new Color(0.68f, 0.74f, 0.85f); break;
            case WeatherType.HeavyRain:
                sunMultiplier = 0.32f; ambientMultiplier = 0.64f; exposureMultiplier = 0.5f; tint = new Color(0.57f, 0.64f, 0.76f); break;
            case WeatherType.WindRainStorm:
                sunMultiplier = 0.27f; ambientMultiplier = 0.58f; exposureMultiplier = 0.44f; tint = new Color(0.51f, 0.59f, 0.72f); break;
            case WeatherType.Cyclone:
                sunMultiplier = 0.22f; ambientMultiplier = 0.53f; exposureMultiplier = 0.4f; tint = new Color(0.47f, 0.54f, 0.68f); break;
            case WeatherType.Thunderstorm:
                sunMultiplier = 0.29f; ambientMultiplier = 0.6f; exposureMultiplier = 0.46f; tint = new Color(0.53f, 0.58f, 0.72f); break;
            case WeatherType.Snow:
                sunMultiplier = 0.68f; ambientMultiplier = 1.08f; exposureMultiplier = 0.9f; tint = new Color(0.82f, 0.9f, 1f); break;
            case WeatherType.Blizzard:
                sunMultiplier = 0.34f; ambientMultiplier = 0.84f; exposureMultiplier = 0.64f; tint = new Color(0.74f, 0.81f, 0.92f); break;
        }
    }

    WeatherType GenerateWeather(int day)
    {
        int hash = unchecked(worldWeatherSeed * 73856093 ^ day * 19349663 ^ (day + 17) * 83492791);
        System.Random random = new(hash);
        int roll = random.Next(0, 100);
        CropSeason season = SeasonVisualController.ResolveSeasonForDay(day);

        if (season == CropSeason.Spring)
            return GenerateSpringWeather(roll);
        if (season == CropSeason.Summer)
            return GenerateSummerWeather(roll);
        if (season == CropSeason.Autumn)
            return GenerateAutumnWeather(roll);
        return GenerateWinterWeather(roll);
    }

    /// <summary>
    /// Spring / musim hujan: 40% gerimis, 27% cerah berawan, 17% cerah,
    /// 10% hujan sedang, 4% hujan lebat, dan 2% panas. Tidak menghasilkan badai atau salju.
    /// </summary>
    static WeatherType GenerateSpringWeather(int roll)
    {
        if (roll < 40) return WeatherType.Drizzle;
        if (roll < 67) return WeatherType.PartlyCloudy;
        if (roll < 84) return WeatherType.Sunny;
        if (roll < 94) return WeatherType.Rain;
        if (roll < 98) return WeatherType.HeavyRain;
        return WeatherType.Heatwave;
    }

    /// <summary>
    /// Summer: total 60% hari cerah/panas, lalu 18% mendung, 8% gerimis,
    /// 10% hujan sedang, dan 4% hujan lebat. Tidak menghasilkan badai atau salju.
    /// </summary>
    static WeatherType GenerateSummerWeather(int roll)
    {
        if (roll < 40) return WeatherType.Sunny;
        if (roll < 60) return WeatherType.Heatwave;
        if (roll < 78) return WeatherType.PartlyCloudy;
        if (roll < 86) return WeatherType.Drizzle;
        if (roll < 96) return WeatherType.Rain;
        return WeatherType.HeavyRain;
    }

    /// <summary>
    /// Fall: 45% cerah sejuk, 30% berawan, 20% hujan ringan-sedang, dan 5% hujan
    /// lebat/badai. Tidak menghasilkan panas terik, topan, salju, atau badai salju.
    /// </summary>
    static WeatherType GenerateAutumnWeather(int roll)
    {
        if (roll < 45) return WeatherType.Sunny;
        if (roll < 75) return WeatherType.PartlyCloudy;
        if (roll < 85) return WeatherType.Drizzle;
        if (roll < 95) return WeatherType.Rain;
        if (roll < 99) return WeatherType.HeavyRain;
        return WeatherType.WindRainStorm;
    }

    /// <summary>
    /// Winter: salju ringan dan langit berawan menjadi cuaca utama. Hari cerah tetap
    /// muncul dengan cahaya pucat, sedangkan badai salju hanya sesekali.
    /// </summary>
    static WeatherType GenerateWinterWeather(int roll)
    {
        if (roll < 45) return WeatherType.Snow;
        if (roll < 75) return WeatherType.PartlyCloudy;
        if (roll < 90) return WeatherType.Sunny;
        return WeatherType.Blizzard;
    }

    /// <summary>Kabut Spring murah berbasis RenderSettings; tidak membuat particle atau camera tambahan.</summary>
    public float GetSeasonalMorningFogDensity(float hour)
    {
        if (CurrentSeason != CropSeason.Spring || hour < 5f || hour > 9.5f)
            return 0f;

        int hash = unchecked(worldWeatherSeed * 486187739 ^ Mathf.Max(1, currentWeatherDay) * 16777619);
        System.Random random = new(hash);
        if (random.NextDouble() >= springMorningFogChance)
            return 0f;

        float centerWeight = 1f - Mathf.Clamp01(Mathf.Abs(hour - 7f) / 2.5f);
        return springMorningFogDensity * Mathf.SmoothStep(0f, 1f, centerWeight);
    }

    void NotifyWeatherChanged()
    {
        nextThunderTime = -1f;
        lightningFlashStartedAt = -100f;
        float wetness = currentWeather switch
        {
            WeatherType.Drizzle => 0.35f,
            WeatherType.Rain => 0.62f,
            WeatherType.HeavyRain or WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => 1f,
            _ => 0f
        };
        Shader.SetGlobalFloat("_NP_SurfaceWetness", wetness);
        Shader.SetGlobalFloat("_NP_LeafWetness", Mathf.Clamp01(wetness * 1.15f));
        WeatherImpactFlow.Publish(WeatherImpactSnapshot.Create(this));
        RefreshWeatherEffect();
        WeatherChanged?.Invoke(currentWeather, tomorrowWeather);
        CurrentWeatherChanged?.Invoke(currentWeather);
        Debug.Log($"[WEATHER] Day {currentWeatherDay}: {GetDisplayName(currentWeather)} | Tomorrow: {GetDisplayName(tomorrowWeather)}");
    }

    void RefreshWeatherEffect()
    {
        if (activeEffectWeather == currentWeather &&
            (!IsPrecipitationWeather(currentWeather) || activeWeatherEffect != null)) return;
        if (activeWeatherEffect != null) Destroy(activeWeatherEffect);
        activeWeatherEffect = null;
        activeEffectUsesFallback = false;
        activeEffectWeather = currentWeather;
        GameObject prefab = currentWeather switch
        {
            WeatherType.Drizzle => drizzleParticlePrefab,
            WeatherType.Rain => rainParticlePrefab,
            WeatherType.HeavyRain => heavyRainParticlePrefab,
            WeatherType.WindRainStorm or WeatherType.Cyclone => windVisualPrefab,
            WeatherType.Thunderstorm => thunderEffectPrefab,
            WeatherType.Snow => snowParticlePrefab,
            WeatherType.Blizzard => blizzardParticlePrefab,
            _ => null
        };
        Vector3 position = effectFollowTarget != null ? effectFollowTarget.position : transform.position;
        activeWeatherEffect = prefab != null
            ? Instantiate(prefab, position, Quaternion.identity, transform)
            : CreateFallbackPrecipitation(currentWeather, position);
        if (activeWeatherEffect == null) return;
        activeEffectUsesFallback = prefab == null;
        activeWeatherEffect.name = $"WeatherEffect_{currentWeather}";
        RefreshFallbackCoverage(true);
    }

    /// <summary>
    /// Efek cadangan ringan untuk project yang belum memasang prefab hujan.
    /// Particle hanya hidup saat presipitasi aktif dan mengikuti player, sehingga tidak memenuhi seluruh map.
    /// </summary>
    GameObject CreateFallbackPrecipitation(WeatherType weather, Vector3 position)
    {
        bool drizzle = weather == WeatherType.Drizzle;
        bool rain = weather is WeatherType.Rain or WeatherType.HeavyRain or WeatherType.WindRainStorm or
            WeatherType.Cyclone or WeatherType.Thunderstorm;
        bool snow = weather is WeatherType.Snow or WeatherType.Blizzard;
        if (!drizzle && !rain && !snow) return null;

        GameObject root = new(snow ? "Runtime Snow" : "Runtime Rain");
        root.transform.SetParent(transform, false);
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = GetFallbackParticleLifetime(weather);
        main.startSpeed = 0f;
        main.startSize = weather switch
        {
            WeatherType.Drizzle => 0.052f * rainDropWidthScale,
            WeatherType.Rain => 0.062f * rainDropWidthScale,
            WeatherType.HeavyRain => 0.076f * rainDropWidthScale,
            WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => 0.086f * rainDropWidthScale,
            WeatherType.Snow => new ParticleSystem.MinMaxCurve(0.11f, 0.22f),
            WeatherType.Blizzard => new ParticleSystem.MinMaxCurve(0.08f, 0.18f),
            _ => 0.05f
        };
        main.startColor = snow
            ? new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.95f, 1f, 0.72f), Color.white)
            : BuildRainColor(weather);
        main.maxParticles = GetVisualMaxParticles(weather);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = GetVisualMaximumEmission(weather) * 0.65f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 9f, 0f);
        shape.scale = new Vector3(38f, 1f, 34f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        if (snow)
        {
            velocity.y = weather == WeatherType.Blizzard ? -7f : -2.2f;
            velocity.x = weather == WeatherType.Blizzard ? 10f : 0f;
            velocity.z = weather == WeatherType.Blizzard ? 3f : 0f;
        }
        else
        {
            // Semua preset memakai magnitude yang sama agar panjang streak konsisten.
            // Preset badai hanya mengubah arah sehingga tampak ditiup angin.
            Vector3 rainDirection = weather switch
            {
                WeatherType.WindRainStorm => new Vector3(0.36f, -0.93f, 0f),
                WeatherType.Cyclone => new Vector3(0.55f, -0.82f, 0.14f),
                WeatherType.Thunderstorm => new Vector3(0.22f, -0.98f, 0f),
                _ => Vector3.down
            };
            Vector3 rainVelocity = rainDirection.normalized * (15f * rainFallSpeedScale);
            velocity.x = rainVelocity.x;
            velocity.y = rainVelocity.y;
            velocity.z = rainVelocity.z;
        }

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = snow || weather is WeatherType.WindRainStorm or WeatherType.Cyclone;
        if (noise.enabled)
        {
            noise.strength = weather == WeatherType.Snow ? 0.65f : weather == WeatherType.Blizzard ? 2.2f : 1.2f;
            noise.frequency = weather == WeatherType.Snow ? 0.35f : 0.55f;
            noise.scrollSpeed = weather == WeatherType.Snow ? 0.15f : 0.4f;
            noise.damping = true;
        }

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = snow ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = snow ? 0f : 0.12f * rainStreakLengthScale;
        renderer.lengthScale = snow ? 0f : 1.1f * rainStreakLengthScale;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        Material material = !snow && rainParticleMaterial != null
            ? new Material(rainParticleMaterial) { hideFlags = HideFlags.HideAndDontSave }
            : shader != null
                ? new Material(shader) { hideFlags = HideFlags.HideAndDontSave }
                : null;
        if (material != null)
        {
            // Material water-drop bawaan sudah mempunyai texture alpha yang teruji di URP.
            // Texture runtime hanya diperlukan untuk snow atau bila slot material kosong.
            Texture2D particleTexture = snow
                ? BuildSoftParticleTexture(32)
                : rainParticleMaterial == null ? BuildSoftRainTexture(16, 64) : null;
            if (particleTexture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", particleTexture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", particleTexture);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_HeadStrength")) material.SetFloat("_HeadStrength",
                weather == WeatherType.Drizzle ? 0.045f : 0.14f);
            if (material.HasProperty("_CoreWidth")) material.SetFloat("_CoreWidth",
                weather == WeatherType.Drizzle ? 0.16f : 0.12f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 2f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ColorMode")) material.SetFloat("_ColorMode", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_COLORADDSUBDIFF_ON");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = 3000;
            renderer.sharedMaterial = material;
            RuntimeWeatherMaterialOwner owner = root.AddComponent<RuntimeWeatherMaterialOwner>();
            owner.Material = material;
            owner.Texture = particleTexture;
        }

        particles.Play();
        return root;
    }

    void FollowWeatherEffect()
    {
        if (activeWeatherEffect != null && effectFollowTarget != null)
            activeWeatherEffect.transform.position = effectFollowTarget.position;
        RefreshFallbackCoverage(false);
    }

    /// <summary>
    /// Menyesuaikan volume presipitasi dengan area tanah yang terlihat kamera. Dihitung empat kali
    /// per detik saja agar zoom-out selalu tertutup tanpa raycast Physics atau render texture.
    /// </summary>
    void RefreshFallbackCoverage(bool force)
    {
        if (!activeEffectUsesFallback || activeWeatherEffect == null ||
            (!force && Time.unscaledTime < nextCoverageRefreshTime))
            return;
        nextCoverageRefreshTime = Time.unscaledTime + 0.25f;

        if (weatherCamera == null) weatherCamera = Camera.main;
        ParticleSystem particles = activeWeatherEffect.GetComponent<ParticleSystem>();
        if (weatherCamera == null || particles == null) return;

        float groundY = effectFollowTarget != null ? effectFollowTarget.position.y : activeWeatherEffect.transform.position.y;
        Plane groundPlane = new(Vector3.up, new Vector3(0f, groundY, 0f));
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
        int hitCount = 0;
        foreach (Vector2 corner in RainViewportCorners)
        {
            Ray ray = weatherCamera.ViewportPointToRay(new Vector3(corner.x, corner.y, 0f));
            if (!groundPlane.Raycast(ray, out float distance)) continue;
            Vector3 point = ray.GetPoint(distance);
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
            hitCount++;
        }
        if (hitCount < 4) return;

        const float screenPadding = 12f;
        float width = Mathf.Clamp(maxX - minX + screenPadding * 2f, 48f, 110f);
        float depth = Mathf.Clamp(maxZ - minZ + screenPadding * 2f, 44f, 100f);
        Vector3 center = new((minX + maxX) * 0.5f, groundY + 10f, (minZ + maxZ) * 0.5f);
        Vector3 localCenter = activeWeatherEffect.transform.InverseTransformPoint(center);

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.position = localCenter;
        shape.scale = new Vector3(width, 1f, depth);

        float area = width * depth;
        float rate = Mathf.Min(GetVisualMaximumEmission(currentWeather),
            area * GetVisualEmissionDensity(currentWeather));
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        ParticleSystem.MainModule main = particles.main;
        main.maxParticles = GetVisualMaxParticles(currentWeather);
    }

    ParticleSystem.MinMaxGradient BuildRainColor(WeatherType weather)
    {
        float weatherOpacity = weather switch
        {
            WeatherType.Drizzle => rainOpacity * 0.86f,
            WeatherType.Rain => rainOpacity * 0.7f,
            WeatherType.HeavyRain => rainOpacity * 0.76f,
            _ => rainOpacity * 0.82f
        };
        Color bright = rainWaterTint;
        bright.a = Mathf.Clamp01(weatherOpacity);
        Color faint = Color.Lerp(rainWaterTint, Color.white, 0.12f);
        faint.a = bright.a * 0.72f;
        return new ParticleSystem.MinMaxGradient(faint, bright);
    }

    float GetVisualEmissionDensity(WeatherType weather) => GetFallbackEmissionDensity(weather) *
        (IsRainWeather(weather) ? rainVisualDensity * GetRainPresetIntensity(weather) : 1f);

    float GetVisualMaximumEmission(WeatherType weather) => GetFallbackMaximumEmission(weather) *
        (IsRainWeather(weather) ? rainVisualDensity * GetRainPresetIntensity(weather) : 1f);

    int GetVisualMaxParticles(WeatherType weather) => Mathf.Max(1, Mathf.RoundToInt(
        GetFallbackMaxParticles(weather) *
        (IsRainWeather(weather) ? rainVisualDensity * GetRainPresetIntensity(weather) : 1f)));

    static float GetRainPresetIntensity(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => 1f,
        WeatherType.Rain => 0.8f,
        WeatherType.HeavyRain => 0.65f,
        WeatherType.WindRainStorm => 0.55f,
        WeatherType.Cyclone => 0.5f,
        WeatherType.Thunderstorm => 0.5f,
        _ => 1f
    };

    static float GetFallbackParticleLifetime(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => 1.35f,
        WeatherType.Rain => 1f,
        WeatherType.HeavyRain => 0.85f,
        WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => 0.8f,
        WeatherType.Snow => 4.2f,
        WeatherType.Blizzard => 2.4f,
        _ => 1f
    };

    static float GetFallbackEmissionDensity(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => 0.22f,
        WeatherType.Rain => 0.48f,
        WeatherType.HeavyRain => 0.82f,
        WeatherType.WindRainStorm => 0.92f,
        WeatherType.Cyclone => 1.05f,
        WeatherType.Thunderstorm => 0.98f,
        WeatherType.Snow => 0.12f,
        WeatherType.Blizzard => 0.42f,
        _ => 0f
    };

    static float GetFallbackMaximumEmission(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => 750f,
        WeatherType.Rain => 1450f,
        WeatherType.HeavyRain => 2200f,
        WeatherType.WindRainStorm => 2400f,
        WeatherType.Cyclone => 2600f,
        WeatherType.Thunderstorm => 2450f,
        WeatherType.Snow => 320f,
        WeatherType.Blizzard => 1050f,
        _ => 0f
    };

    static int GetFallbackMaxParticles(WeatherType weather) => weather switch
    {
        WeatherType.Drizzle => 1100,
        WeatherType.Rain => 1800,
        WeatherType.HeavyRain => 2600,
        WeatherType.WindRainStorm => 2900,
        WeatherType.Cyclone => 3200,
        WeatherType.Thunderstorm => 3000,
        WeatherType.Snow => 1400,
        WeatherType.Blizzard => 2700,
        _ => 1000
    };

    static Texture2D BuildSoftParticleTexture(int resolution)
    {
        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false, true)
        {
            name = "Runtime Soft Snow Particle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
        float radius = resolution * 0.5f;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
            float alpha = 1f - Mathf.SmoothStep(0.35f, 1f, distance);
            pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    static Texture2D BuildSoftRainTexture(int width, int height)
    {
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "Runtime Soft Water Rain Particle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float horizontal = Mathf.Abs(((x + 0.5f) / width) * 2f - 1f);
            float vertical = (y + 0.5f) / height;
            float sideFade = 1f - Mathf.SmoothStep(0.24f, 0.96f, horizontal);
            float endFade = Mathf.SmoothStep(0f, 0.08f, vertical) *
                            (1f - Mathf.SmoothStep(0.86f, 1f, vertical));
            pixels[y * width + x] = new Color(1f, 1f, 1f, sideFade * endFade);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    public static bool AreNpcOutdoorActivitiesAllowed => !WeatherImpactFlow.HasCurrent || WeatherImpactFlow.Current.NpcOutdoorActivitiesAllowed;
    public static float CurrentHuntingMultiplier => WeatherImpactFlow.HasCurrent ? WeatherImpactFlow.Current.HuntingMultiplier : 1f;
    public static float CurrentFishingMultiplier => WeatherImpactFlow.HasCurrent ? WeatherImpactFlow.Current.FishingMultiplier : 1f;

    static WeatherType NextWeather(WeatherType weather)
    {
        int count = Enum.GetValues(typeof(WeatherType)).Length;
        return (WeatherType)(((int)weather + 1) % count);
    }

    static WeatherType ClampWeather(WeatherType weather)
    {
        return Enum.IsDefined(typeof(WeatherType), weather) ? weather : WeatherType.Sunny;
    }

    /// <summary>True untuk cuaca yang memberi penyiraman outdoor.</summary>
    public static bool IsRainWeather(WeatherType weather)
    {
        return weather is WeatherType.Drizzle or WeatherType.Rain or WeatherType.HeavyRain or
               WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm;
    }

    static bool IsPrecipitationWeather(WeatherType weather) => IsRainWeather(weather) ||
        weather is WeatherType.Snow or WeatherType.Blizzard;

    /// <summary>True untuk cuaca berbahaya yang perlu peringatan forecast.</summary>
    public static bool IsStormWeather(WeatherType weather)
    {
        return weather is WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm or WeatherType.Blizzard;
    }

    /// <summary>Jumlah moisture farming dari hujan aktif.</summary>
    public static int GetRainMoistureAmount(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Drizzle => 30,
            WeatherType.Rain => 45,
            WeatherType.HeavyRain => 60,
            WeatherType.WindRainStorm => 55,
            WeatherType.Cyclone => 65,
            WeatherType.Thunderstorm => 60,
            _ => 0
        };
    }

    /// <summary>Modifier pertumbuhan harian akibat cuaca, terpisah dari status penyiraman.</summary>
    public static float GetCropGrowthMultiplier(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Heatwave => 0.8f,
            WeatherType.Drizzle => 1f,
            WeatherType.Rain => 1f,
            WeatherType.HeavyRain => 1f,
            WeatherType.WindRainStorm => 1f,
            WeatherType.Cyclone => 1f,
            WeatherType.Thunderstorm => 1f,
            // Catatan desain: hujan salju hanya visual dan tidak memberi efek gameplay.
            WeatherType.Snow => 1f,
            WeatherType.Blizzard => 1f,
            _ => 1f
        };
    }

    public static float GetCropLossChance(WeatherType weather, float stormChance = 0.01f, float extremeChance = 0.03f)
    {
        return weather switch
        {
            WeatherType.WindRainStorm or WeatherType.Thunderstorm => Mathf.Clamp01(stormChance),
            WeatherType.Cyclone or WeatherType.Blizzard => Mathf.Clamp01(extremeChance),
            _ => 0f
        };
    }

    public static float GetOutdoorAnimalSicknessChance(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Drizzle or WeatherType.Rain or WeatherType.HeavyRain => 0.25f,
            WeatherType.WindRainStorm or WeatherType.Thunderstorm => 0.60f,
            WeatherType.Cyclone or WeatherType.Blizzard => 0.90f,
            WeatherType.Heatwave => 0.80f,
            _ => 0f
        };
    }

    /// <summary>Penalti relationship/XP sekali pada Daily Reset untuk hewan yang masih di luar.</summary>
    public static int GetOutdoorAnimalRelationshipPenalty(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Drizzle => 10,
            WeatherType.Rain => 15,
            WeatherType.HeavyRain => 20,
            WeatherType.WindRainStorm => 50,
            WeatherType.Cyclone => 100,
            WeatherType.Blizzard => 30,
            _ => 0
        };
    }

    /// <summary>Multiplier seluruh biaya stamina ketika player melakukan aktivitas di luar.</summary>
    public static float GetOutdoorStaminaMultiplier(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.WindRainStorm => 3f,
            WeatherType.Thunderstorm => 2.5f,
            WeatherType.Blizzard => 4f,
            _ => 1f
        };
    }

    public static bool DrainsOutdoorStaminaHourly(WeatherType weather)
    {
        return weather is WeatherType.HeavyRain or WeatherType.WindRainStorm or
            WeatherType.Cyclone or WeatherType.Thunderstorm;
    }

    public static bool BlocksLeavingHome(WeatherType weather) => weather == WeatherType.Cyclone;
    public static bool BlocksTelevision(WeatherType weather) => weather == WeatherType.Thunderstorm;

    public static bool IsPlayerOutdoors()
    {
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInsideInterior)
            return false;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return string.IsNullOrEmpty(sceneName) || sceneName.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) < 0;
    }

    public static string GetDisplayName(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Sunny => "Sunny / Cerah",
            WeatherType.PartlyCloudy => "Partly Cloudy / Cerah Mendung",
            WeatherType.Heatwave => "Heatwave / Panas Terik",
            WeatherType.Drizzle => "Drizzle / Gerimis",
            WeatherType.Rain => "Rainy / Hujan Sedang",
            WeatherType.HeavyRain => "Heavy Rain / Hujan Lebat",
            WeatherType.WindRainStorm => "Wind Rainstorm / Hujan Angin Badai",
            WeatherType.Cyclone => "Cyclone / Angin Topan",
            WeatherType.Thunderstorm => "Thunderstorm / Badai Petir",
            WeatherType.Snow => "Snow / Hujan Salju",
            WeatherType.Blizzard => "Blizzard / Badai Salju",
            _ => weather.ToString()
        };
    }

    public static string GetShortName(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Sunny => "Cerah",
            WeatherType.PartlyCloudy => "Cerah Mendung",
            WeatherType.Heatwave => "Panas Terik",
            WeatherType.Drizzle => "Gerimis",
            WeatherType.Rain => "Hujan Sedang",
            WeatherType.HeavyRain => "Hujan Lebat",
            WeatherType.WindRainStorm => "Hujan Angin Badai",
            WeatherType.Cyclone => "Angin Topan",
            WeatherType.Thunderstorm => "Badai Petir",
            WeatherType.Snow => "Hujan Salju",
            WeatherType.Blizzard => "Badai Salju",
            _ => weather.ToString()
        };
    }

    public static string GetForecastMessage(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Sunny => "Langit cerah. Hari yang baik untuk farming dan eksplorasi.",
            WeatherType.PartlyCloudy => "Awan ringan akan menutupi sebagian langit.",
            WeatherType.Heatwave => "Suhu diperkirakan sangat panas. Persiapkan stamina dengan baik.",
            WeatherType.Drizzle => "Gerimis ringan diperkirakan turun besok.",
            WeatherType.Rain => "Hujan diperkirakan turun sepanjang hari besok.",
            WeatherType.HeavyRain => "Hujan lebat diperkirakan terjadi. Aktivitas luar akan lebih gelap.",
            WeatherType.WindRainStorm => "Peringatan hujan dan angin kencang untuk besok.",
            WeatherType.Cyclone => "Peringatan angin topan. Hindari perjalanan jauh besok.",
            WeatherType.Thunderstorm => "Peringatan badai petir. Rencanakan aktivitas indoor.",
            WeatherType.Snow => "Salju ringan diperkirakan turun besok.",
            WeatherType.Blizzard => "Peringatan badai salju dengan jarak pandang rendah.",
            _ => "Belum ada informasi cuaca."
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSystemExists()
    {
        if (FindFirstObjectByType<WeatherSystem>() != null)
            return;
        new GameObject("WeatherSystem_Runtime").AddComponent<WeatherSystem>();
    }
}

/// <summary>Melepas material procedural ketika efek cuaca diganti agar tidak menumpuk di memori.</summary>
sealed class RuntimeWeatherMaterialOwner : MonoBehaviour
{
    public Material Material { get; set; }
    public Texture2D Texture { get; set; }

    void OnDestroy()
    {
        if (Material != null)
            Destroy(Material);
        if (Texture != null)
            Destroy(Texture);
    }
}
