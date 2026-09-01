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
    [SerializeField] int currentWeatherDay = -1;

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
    [SerializeField] KeyCode nextCurrentWeatherKey = KeyCode.F8;
    [SerializeField] KeyCode nextForecastWeatherKey = KeyCode.F9;

    public WeatherType CurrentWeather => currentWeather;
    public WeatherType TomorrowWeather => tomorrowWeather;
    public int CurrentWeatherDay => currentWeatherDay;
    public int WorldWeatherSeed => worldWeatherSeed;
    public bool IsRainToday => IsRainWeather(currentWeather);
    public bool IsStormToday => IsStormWeather(currentWeather);

    public event Action<WeatherType, WeatherType> WeatherChanged;

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
        TimeManager.OnDay += HandleDayChanged;
    }

    void Start()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        EnsureForecastForDay(day);
    }

    void OnDisable()
    {
        TimeManager.OnDay -= HandleDayChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(nextCurrentWeatherKey))
        {
            currentWeather = NextWeather(currentWeather);
            NotifyWeatherChanged();
        }
        if (Input.GetKeyDown(nextForecastWeatherKey))
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
            currentWeather = tomorrowWeather;
            currentWeatherDay = day;
            tomorrowWeather = GenerateWeather(day + 1);
            NotifyWeatherChanged();
            return;
        }

        EnsureForecastForDay(day);
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
    public void RestoreForecast(int savedDay, int savedSeed, WeatherType savedCurrent, WeatherType savedTomorrow)
    {
        worldWeatherSeed = savedSeed;
        currentWeatherDay = Mathf.Max(1, savedDay);
        currentWeather = ClampWeather(savedCurrent);
        tomorrowWeather = ClampWeather(savedTomorrow);
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
        WeatherChanged?.Invoke(currentWeather, tomorrowWeather);
        Debug.Log($"[WEATHER] Day {currentWeatherDay}: {GetDisplayName(currentWeather)} | Tomorrow: {GetDisplayName(tomorrowWeather)}");
    }

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
            WeatherType.WindRainStorm => "Wind Rainstorm / Hujan Angin",
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
            WeatherType.WindRainStorm => "Hujan Angin",
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
