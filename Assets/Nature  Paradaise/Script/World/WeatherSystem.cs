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
    [SerializeField] int communityCleanupDay = -1;

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

    [Header("Debug")]
    [SerializeField] bool enableDebugKeys = true;
    [SerializeField] KeyCode nextCurrentWeatherKey = KeyCode.F12;
    [Tooltip("None secara default agar tidak berbenturan dengan toggle/load. Dapat diisi manual jika diperlukan.")]
    [SerializeField] KeyCode nextForecastWeatherKey = KeyCode.None;

    public WeatherType CurrentWeather => currentWeather;
    public WeatherType TomorrowWeather => tomorrowWeather;
    public WeatherType PreviousWeather => previousWeather;
    public int CurrentWeatherDay => currentWeatherDay;
    public int WorldWeatherSeed => worldWeatherSeed;
    public bool IsRainToday => IsRainWeather(currentWeather);
    public bool IsStormToday => IsStormWeather(currentWeather);
    public bool IsCommunityCleanupDay => currentWeatherDay == communityCleanupDay;
    float nextLightningCheckTime;
    int lightningResolvedDay = -1;
    GameObject activeWeatherEffect;
    WeatherType activeEffectWeather = (WeatherType)(-1);

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
    }

    void OnEnable()
    {
        TimeManager.OnBeforeDayChange += HandleBeforeDayChanged;
        TimeManager.OnDay += HandleDayChanged;
    }

    void Start()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        EnsureForecastForDay(day);
    }

    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= HandleBeforeDayChanged;
        TimeManager.OnDay -= HandleDayChanged;
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

        if (!enableDebugKeys || !HUDManager.DebugCluesEnabled)
            return;

        if (nextCurrentWeatherKey != KeyCode.None && Input.GetKeyDown(nextCurrentWeatherKey))
        {
            currentWeather = NextWeather(currentWeather);
            NotifyWeatherChanged();
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
        worldWeatherSeed = savedSeed;
        currentWeatherDay = Mathf.Max(1, savedDay);
        currentWeather = ClampWeather(savedCurrent);
        tomorrowWeather = ClampWeather(savedTomorrow);
        previousWeather = ClampWeather(savedPrevious);
        communityCleanupDay = savedCleanupDay;
        NotifyWeatherChanged();
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
                sunMultiplier = 0.52f; ambientMultiplier = 0.82f; exposureMultiplier = 0.7f; tint = new Color(0.72f, 0.8f, 0.9f); break;
            case WeatherType.Rain:
                sunMultiplier = 0.4f; ambientMultiplier = 0.72f; exposureMultiplier = 0.58f; tint = new Color(0.62f, 0.7f, 0.82f); break;
            case WeatherType.HeavyRain:
                sunMultiplier = 0.28f; ambientMultiplier = 0.62f; exposureMultiplier = 0.46f; tint = new Color(0.52f, 0.6f, 0.74f); break;
            case WeatherType.WindRainStorm:
                sunMultiplier = 0.24f; ambientMultiplier = 0.56f; exposureMultiplier = 0.42f; tint = new Color(0.48f, 0.57f, 0.7f); break;
            case WeatherType.Cyclone:
                sunMultiplier = 0.16f; ambientMultiplier = 0.48f; exposureMultiplier = 0.34f; tint = new Color(0.42f, 0.5f, 0.62f); break;
            case WeatherType.Thunderstorm:
                sunMultiplier = 0.2f; ambientMultiplier = 0.5f; exposureMultiplier = 0.36f; tint = new Color(0.46f, 0.5f, 0.66f); break;
            case WeatherType.Snow:
                sunMultiplier = 0.68f; ambientMultiplier = 1.08f; exposureMultiplier = 0.9f; tint = new Color(0.82f, 0.9f, 1f); break;
            case WeatherType.Blizzard:
                sunMultiplier = 0.26f; ambientMultiplier = 0.8f; exposureMultiplier = 0.58f; tint = new Color(0.7f, 0.78f, 0.9f); break;
        }
    }

    WeatherType GenerateWeather(int day)
    {
        int hash = unchecked(worldWeatherSeed * 73856093 ^ day * 19349663 ^ (day + 17) * 83492791);
        System.Random random = new(hash);
        int roll = random.Next(0, 100);
        if (roll < 25) return WeatherType.Sunny;
        if (roll < 45) return WeatherType.PartlyCloudy;
        if (roll < 51) return WeatherType.Heatwave;
        if (roll < 63) return WeatherType.Drizzle;
        if (roll < 78) return WeatherType.Rain;
        if (roll < 86) return WeatherType.HeavyRain;
        if (roll < 91) return WeatherType.WindRainStorm;
        if (roll < 92) return WeatherType.Cyclone;
        if (roll < 97) return WeatherType.Thunderstorm;
        if (roll < 99) return WeatherType.Snow;
        return WeatherType.Blizzard;
    }

    void NotifyWeatherChanged()
    {
        WeatherImpactFlow.Publish(WeatherImpactSnapshot.Create(this));
        RefreshWeatherEffect();
        WeatherChanged?.Invoke(currentWeather, tomorrowWeather);
        CurrentWeatherChanged?.Invoke(currentWeather);
        Debug.Log($"[WEATHER] Day {currentWeatherDay}: {GetDisplayName(currentWeather)} | Tomorrow: {GetDisplayName(tomorrowWeather)}");
    }

    void RefreshWeatherEffect()
    {
        if (activeEffectWeather == currentWeather) return;
        if (activeWeatherEffect != null) Destroy(activeWeatherEffect);
        activeWeatherEffect = null;
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
        if (prefab == null) return;
        Vector3 position = effectFollowTarget != null ? effectFollowTarget.position : transform.position;
        activeWeatherEffect = Instantiate(prefab, position, Quaternion.identity, transform);
        activeWeatherEffect.name = $"WeatherEffect_{currentWeather}";
    }

    void FollowWeatherEffect()
    {
        if (activeWeatherEffect != null && effectFollowTarget != null)
            activeWeatherEffect.transform.position = effectFollowTarget.position;
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
