using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Visual musim global. Tidak memindai atau mengubah material pada Update; pekerjaan berat hanya
/// dilakukan saat scene dimuat atau kalender memasuki musim baru.
/// </summary>
[DefaultExecutionOrder(-850)]
[DisallowMultipleComponent]
public sealed class SeasonVisualController : MonoBehaviour
{
    sealed class TerrainSeasonCache
    {
        public TerrainLayer[] original;
        public readonly Dictionary<CropSeason, TerrainLayer[]> variants = new();
    }

    static readonly Dictionary<TerrainData, TerrainSeasonCache> TerrainCache = new();
    static readonly int TerrainTintId = Shader.PropertyToID("_NP_SeasonTerrainTint");
    static readonly int VegetationTintId = Shader.PropertyToID("_NP_SeasonVegetationTint");
    static readonly int DrynessId = Shader.PropertyToID("_NP_SeasonDryness");
    static readonly int SnowAmountId = Shader.PropertyToID("_NP_SeasonSnowAmount");

    public static SeasonVisualController Instance { get; private set; }
    public static CropSeason CurrentSeason { get; private set; } = CropSeason.Spring;
    public static SeasonVisualProfile CurrentProfile { get; private set; }
    public static Color LightingTint => CurrentProfile != null ? CurrentProfile.lightingTint : Color.white;
    public static float SunMultiplier => CurrentProfile != null ? CurrentProfile.sunMultiplier : 1f;
    public static float AmbientMultiplier => CurrentProfile != null ? CurrentProfile.ambientMultiplier : 1f;
    public static float SkyExposureMultiplier => CurrentProfile != null ? CurrentProfile.skyExposureMultiplier : 1f;
    public static bool IsDebugOverrideActive => Instance != null && Instance.debugOverride;
    public static event Action<CropSeason> SeasonChanged;

    /// <summary>
    /// Musim yang dipakai sistem gameplay. Debug override ikut dihormati agar weather dan visual
    /// dapat dites bersama tanpa memajukan kalender.
    /// </summary>
    public static CropSeason ResolveSeasonForDay(int day)
    {
        if (Instance != null && Instance.debugOverride)
            return Instance.debugSeason;
        int duration = Instance != null ? Instance.daysPerSeason : 28;
        return CropDataSO.GetSeasonForDay(Mathf.Max(1, day), duration);
    }

    /// <summary>Dipanggil setelah Save/Load mengganti hari tanpa menjalankan event pergantian hari.</summary>
    public static void RefreshFromCalendar() => Instance?.ApplyCurrent(true);

    [SerializeField, Min(1)] int daysPerSeason = 28;
    [SerializeField] SeasonVisualProfile[] profiles = Array.Empty<SeasonVisualProfile>();
    [Header("Debug (Editor / Development Build)")]
    [SerializeField] KeyCode nextSeasonKey = KeyCode.PageUp;
    [SerializeField] KeyCode clearOverrideKey = KeyCode.PageDown;

    readonly Dictionary<CropSeason, SeasonVisualProfile> bySeason = new();
    Coroutine terrainApplyRoutine;
    bool debugOverride;
    CropSeason debugSeason;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool debugMenuOpen;
    Rect debugWindow = new(0f, 0f, 560f, 650f);
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfiles();
    }

    void OnEnable()
    {
        TimeManager.OnDay += HandleDayChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void Start() => ApplyCurrent(true);

    void OnDisable()
    {
        TimeManager.OnDay -= HandleDayChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        CloseDebugMenu();
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        if (nextSeasonKey != KeyCode.None && GameplayInput.GetKeyDown(nextSeasonKey))
        {
            if (debugMenuOpen) CloseDebugMenu();
            else OpenDebugMenu();
        }
        if (clearOverrideKey != KeyCode.None && GameplayInput.GetKeyDown(clearOverrideKey))
        {
            FollowCalendar();
            CloseDebugMenu();
        }
        if (debugMenuOpen && GameplayInput.GetKeyDown(KeyCode.Escape)) CloseDebugMenu();
    }

    void OnGUI()
    {
        if (GameplayPauseMenu.BlocksGameplayInput) return;
        if (!debugMenuOpen) return;
        debugWindow.width = Mathf.Min(560f, Screen.width - 24f);
        debugWindow.height = Mathf.Min(650f, Screen.height - 24f);
        debugWindow = GUI.Window(GetInstanceID(), debugWindow, DrawDebugMenu, "DEBUG SEASON MENU");
    }

    void DrawDebugMenu(int id)
    {
        int calendarDay = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        CropSeason calendarSeason = CropDataSO.GetSeasonForDay(calendarDay, daysPerSeason);
        GUILayout.Space(4f);
        GUILayout.Label($"Aktif: {CurrentSeason} {(debugOverride ? "(DEBUG OVERRIDE)" : "(CALENDAR)")}");
        GUILayout.Label($"Kalender: Day {calendarDay} — {calendarSeason}");
        GUILayout.Space(8f);
        if (GUILayout.Button("SPRING", GUILayout.Height(38f))) SelectDebugSeason(CropSeason.Spring);
        if (GUILayout.Button("SUMMER", GUILayout.Height(38f))) SelectDebugSeason(CropSeason.Summer);
        if (GUILayout.Button("AUTUMN / FALL", GUILayout.Height(38f))) SelectDebugSeason(CropSeason.Autumn);
        if (GUILayout.Button("WINTER", GUILayout.Height(38f))) SelectDebugSeason(CropSeason.Winter);
        GUILayout.Space(6f);
        if (GUILayout.Button("FOLLOW CALENDAR", GUILayout.Height(34f))) FollowCalendar();

        WeatherSystem weather = WeatherSystem.Instance;
        GUILayout.Space(10f);
        GUILayout.Label("WEATHER");
        GUILayout.Label(weather != null
            ? $"Aktif: {WeatherSystem.GetShortName(weather.CurrentWeather)} " +
              $"{(weather.IsDebugWeatherOverrideActive ? "(DEBUG OVERRIDE)" : "(AUTO SEASON)")}"
            : "WeatherSystem belum tersedia");
        DrawWeatherRow(weather,
            ("CERAH", WeatherType.Sunny),
            ("MENDUNG", WeatherType.PartlyCloudy),
            ("PANAS", WeatherType.Heatwave));
        DrawWeatherRow(weather,
            ("GERIMIS", WeatherType.Drizzle),
            ("HUJAN", WeatherType.Rain),
            ("HUJAN LEBAT", WeatherType.HeavyRain));
        DrawWeatherRow(weather,
            ("BADAI ANGIN", WeatherType.WindRainStorm),
            ("TOPAN", WeatherType.Cyclone),
            ("BADAI PETIR", WeatherType.Thunderstorm));
        DrawWeatherRow(weather,
            ("SALJU", WeatherType.Snow),
            ("BADAI SALJU", WeatherType.Blizzard));
        GUI.enabled = weather != null;
        if (GUILayout.Button("AUTO WEATHER — IKUTI RULE MUSIM", GUILayout.Height(34f)))
            weather.FollowSeasonWeather();
        GUI.enabled = true;
        GUILayout.Space(6f);
        if (GUILayout.Button($"CLOSE [{nextSeasonKey} / Esc]", GUILayout.Height(30f))) CloseDebugMenu();
        GUI.DragWindow(new Rect(0f, 0f, debugWindow.width, 26f));
    }

    static void DrawWeatherRow(WeatherSystem weather,
        params (string label, WeatherType weather)[] options)
    {
        GUILayout.BeginHorizontal();
        GUI.enabled = weather != null;
        foreach ((string label, WeatherType value) in options)
            if (GUILayout.Button(label, GUILayout.Height(34f))) weather.SetDebugWeather(value);
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    void OpenDebugMenu()
    {
        debugMenuOpen = true;
        debugWindow.x = Mathf.Max(12f, (Screen.width - debugWindow.width) * 0.5f);
        debugWindow.y = Mathf.Max(12f, (Screen.height - debugWindow.height) * 0.5f);
        TimeManager.Instance?.AcquirePause(this);
    }

    void CloseDebugMenu()
    {
        if (!debugMenuOpen) return;
        debugMenuOpen = false;
        TimeManager.Instance?.ReleasePause(this);
    }

    void SelectDebugSeason(CropSeason season)
    {
        debugOverride = true;
        debugSeason = season;
        Apply(season, true);
        SaveLoadFeedback.Instance?.ShowMessage($"DEBUG SEASON: {season}");
    }

    void FollowCalendar()
    {
        debugOverride = false;
        ApplyCurrent(true);
        SaveLoadFeedback.Instance?.ShowMessage($"SEASON: mengikuti kalender ({CurrentSeason})");
    }
#endif

    void HandleDayChanged()
    {
        if (!debugOverride) ApplyCurrent(false);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        CloseDebugMenu();
#endif
        ApplyCurrent(true);
    }

    void LoadProfiles()
    {
        bySeason.Clear();
        SeasonVisualProfile[] loaded = Resources.LoadAll<SeasonVisualProfile>("Profiles/Seasons");
        if (loaded != null && loaded.Length > 0) profiles = loaded;
        foreach (SeasonVisualProfile profile in profiles ?? Array.Empty<SeasonVisualProfile>())
            if (profile != null) bySeason[profile.season] = profile;
        EnsureFallback(CropSeason.Spring, new Color(0.78f, 1f, 0.80f), new Color(0.80f, 1f, 0.82f), new Color(0.92f, 1f, 0.96f), 0f, 0f, 0.94f, 1.02f);
        EnsureFallback(CropSeason.Summer, new Color(1f, 0.88f, 0.68f), new Color(1f, 0.92f, 0.70f), new Color(1f, 0.90f, 0.72f), 0.22f, 0f, 1.22f, 1.06f);
        EnsureFallback(CropSeason.Autumn, new Color(0.86f, 0.79f, 0.58f), new Color(0.94f, 0.62f, 0.30f), new Color(1f, 0.86f, 0.72f), 0.18f, 0f, 0.90f, 0.96f);
        EnsureFallback(CropSeason.Winter, new Color(0.92f, 0.96f, 1f), new Color(0.72f, 0.80f, 0.84f), new Color(0.82f, 0.90f, 1f), 0f, 1f, 0.78f, 1.04f);
    }

    void EnsureFallback(CropSeason season, Color terrain, Color vegetation, Color lighting,
        float dryness, float snow, float sun, float ambient)
    {
        if (bySeason.ContainsKey(season)) return;
        SeasonVisualProfile profile = ScriptableObject.CreateInstance<SeasonVisualProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        profile.name = $"{season} Runtime Profile";
        profile.season = season;
        profile.terrainTint = terrain;
        profile.vegetationTint = vegetation;
        profile.lightingTint = lighting;
        profile.dryness = dryness;
        profile.snowAmount = snow;
        profile.sunMultiplier = sun;
        profile.ambientMultiplier = ambient;
        bySeason[season] = profile;
    }

    void ApplyCurrent(bool force)
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        CropSeason season = ResolveSeasonForDay(day);
        Apply(season, force);
    }

    void Apply(CropSeason season, bool force)
    {
        if (!bySeason.TryGetValue(season, out SeasonVisualProfile profile)) return;
        if (!force && season == CurrentSeason && CurrentProfile == profile) return;
        CurrentSeason = season;
        CurrentProfile = profile;

        Shader.SetGlobalColor(TerrainTintId, profile.terrainTint);
        Shader.SetGlobalColor(VegetationTintId, profile.vegetationTint);
        Shader.SetGlobalFloat(DrynessId, profile.dryness);
        Shader.SetGlobalFloat(SnowAmountId, profile.snowAmount);
        if (terrainApplyRoutine != null) StopCoroutine(terrainApplyRoutine);
        terrainApplyRoutine = StartCoroutine(ApplyTerrainsOverFrames(profile));
        SeasonalRendererTarget.ApplyAll(season);
        SeasonalObjectTarget.ApplyAll(season);
        SeasonChanged?.Invoke(season);
        Debug.Log($"[SEASON] Visual {season} diterapkan sekali. Terrain aktif: {Terrain.activeTerrains.Length}.");
    }

    IEnumerator ApplyTerrainsOverFrames(SeasonVisualProfile profile)
    {
        HashSet<TerrainData> processed = new();
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null || !processed.Add(data)) continue;
            if (!TerrainCache.TryGetValue(data, out TerrainSeasonCache cache))
            {
                cache = new TerrainSeasonCache { original = (TerrainLayer[])data.terrainLayers.Clone() };
                TerrainCache.Add(data, cache);
            }
            if (!cache.variants.TryGetValue(profile.season, out TerrainLayer[] layers))
            {
                layers = BuildTerrainVariant(cache.original, profile);
                cache.variants.Add(profile.season, layers);
            }
            data.terrainLayers = layers;
            terrain.Flush();
            // Hindari seluruh terrain chunk rebuild pada frame yang sama di mobile.
            yield return null;
        }
        terrainApplyRoutine = null;
    }

    static TerrainLayer[] BuildTerrainVariant(TerrainLayer[] original, SeasonVisualProfile profile)
    {
        TerrainLayer[] result = (TerrainLayer[])original.Clone();
        for (int index = 0; index < result.Length; index++)
        {
            TerrainLayer source = original[index];
            bool grassLayer = source != null &&
                (source.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (source.diffuseTexture != null &&
                  source.diffuseTexture.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0));
            if (!grassLayer) continue;
            TerrainLayer visualSource = profile.grassLayerOverride != null ? profile.grassLayerOverride : source;
            TerrainLayer clone = Instantiate(visualSource);
            clone.name = $"{visualSource.name}_{profile.season}_Runtime";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.diffuseRemapMax = new Vector4(profile.terrainTint.r, profile.terrainTint.g, profile.terrainTint.b, profile.terrainTint.a);
            result[index] = clone;
        }
        return result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        CurrentSeason = CropSeason.Spring;
        CurrentProfile = null;
        SeasonChanged = null;
        TerrainCache.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindFirstObjectByType<SeasonVisualController>() != null) return;
        new GameObject("SeasonVisualSystem_Runtime").AddComponent<SeasonVisualController>();
    }
}
