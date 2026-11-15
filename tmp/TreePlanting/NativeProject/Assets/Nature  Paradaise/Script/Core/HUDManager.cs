using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Pemetaan icon UI berdasarkan ID effect agar effect gameplay tetap modular.</summary>
[Serializable]
public sealed class PlayerStatusEffectIconEntry
{
    public string effectId;
    public Sprite icon;
}

/// <summary>
/// Menyatukan tampilan gold, jumlah item utama, waktu, HP, dan stamina.
/// Dapat membangun visual runtime untuk menjaga kompatibilitas SampleScene lama.
/// </summary>
public class HUDManager : MonoBehaviour
{
    // =====================================================
    // TEXT UI
    // =====================================================

    [Header("Text UI")]
    public TMP_Text clockText;
    public TMP_Text seedText;
    public TMP_Text cabbageText;
    public TMP_Text milkText;
    public TMP_Text moneyText;

    // =====================================================
    // DATA REFERENCES
    // =====================================================

    [Header("Data References")]
    public Inventory playerInv;

    public ItemSO seedItem;
    public ItemSO cabbageItem;
    public ItemSO milkItem;

    [Header("Player Status")]
    public PlayerStatusSystem playerStatus;

    [Header("Unified HUD Style")]
    public Vector2 hudPanelSize = new Vector2(400f, 188f);
    [Tooltip("Margin HUD dari sudut kiri atas layar.")]
    public Vector2 hudScreenMargin = new Vector2(20f, 20f);
    public Color hudPanelColor = new Color(0.035f, 0.045f, 0.055f, 0.88f);
    public Color barBackgroundColor = new Color(0.12f, 0.13f, 0.15f, 0.95f);
    public Color healthBarColor = new Color(0.88f, 0.16f, 0.19f, 1f);
    public Color staminaBarColor = new Color(0.18f, 0.76f, 0.34f, 1f);
    public Color hungerBarColor = new Color(0.94f, 0.62f, 0.16f, 1f);
    public Color buffSlotColor = new Color(0.15f, 0.62f, 0.92f, 0.92f);
    public Color debuffSlotColor = new Color(0.74f, 0.24f, 0.65f, 0.92f);

    [Header("Optional PNG Sprites")]
    [Tooltip("Kosongkan untuk memakai dummy minimalis modern.")]
    public Sprite hudPanelSprite;
    public Sprite progressBackgroundSprite;
    public Sprite healthFillSprite;
    public Sprite staminaFillSprite;

    [Header("Weather Artwork (Optional)")]
    [Tooltip("Pemetaan sprite cuaca yang sama dengan TV. Jika kosong, ikon cuaca digambar otomatis tanpa tekstur tambahan.")]
    public WeatherForecastTVTheme weatherArtwork;
    [Tooltip("Currency artwork. Empty uses the shared InventoryTheme coin icon, then a simple readable fallback.")]
    public Sprite goldIcon;

    [Header("Status Effect Icons")]
    [Tooltip("Daftarkan Sprite berdasarkan Effect ID. Slot tanpa Sprite memakai singkatan ID.")]
    public List<PlayerStatusEffectIconEntry> statusEffectIcons = new();

    [Header("Debug Clues")]
    [Tooltip("Master switch untuk seluruh clue pengembangan. Dapat diubah saat Play Mode memakai tombol toggle.")]
    public bool showDebugClues;
    [Tooltip("Tombol untuk menampilkan atau menyembunyikan seluruh Debug Clues.")]
    public KeyCode debugCluesToggleKey = KeyCode.F9;
    [Tooltip("Tampilkan cuaca runtime. Dibuat terpisah dari Debug Clues agar efek auto-water hujan selalu dapat diperiksa.")]
    public bool showWeatherDebugText = true;
    [Tooltip("Tampilkan panduan kontrol dan detail target farming sebagai informasi debug.")]
    public bool showFarmingDebugControls = true;

    public static bool DebugCluesEnabled { get; private set; }
    public static bool FarmingDebugCluesEnabled { get; private set; }

    RectTransform runtimeHudRoot;
    RectTransform runtimeCanvasRoot;
    RectTransform timePanelRoot;
    WeatherHUDIcon weatherIcon;
    TMP_Text weatherStatusText;
    TMP_Text seasonText;
    TMP_Text timeOfDayText;
    bool animalContextVisible;
    bool modalContextVisible;
    public void SetModalContextVisible(bool visible){modalContextVisible=visible;ApplyContextVisibility();}
    public void SetAnimalContextVisible(bool visible)
    {
        if(animalContextVisible==visible) return;
        animalContextVisible=visible;
        ApplyContextVisibility();
    }
    void ApplyContextVisibility()
    {
        bool visible=animalContextVisible||modalContextVisible||MiningContextActive();
        foreach(var root in new[] {runtimeHudRoot,timePanelRoot})
        {
            if(root==null) continue;
            var group=root.GetComponent<CanvasGroup>();
            if(group==null) group=root.gameObject.AddComponent<CanvasGroup>();
            group.alpha=visible?0:1;
            group.blocksRaycasts=!visible;
        }
    }
    RectTransform healthFill;
    RectTransform staminaFill;
    RectTransform hungerFill;
    RectTransform hungerRoot;
    TMP_Text healthValueText;
    TMP_Text staminaValueText;
    TMP_Text hungerValueText;
    readonly List<StatusEffectView> buffEffectViews = new();
    readonly List<StatusEffectView> debuffEffectViews = new();
    TMP_Text weatherDebugText;
    FarmingTool farmingTool;
    PlayerToolHotbar toolHotbar;
    InventoryHotbarUI inventoryHotbar;
    RectTransform farmingDebugRoot;
    TMP_Text farmingDebugText;
    bool housingDebugSuppressed;

    // =====================================================
    // INITIALIZE
    // =====================================================

    void Awake()
    {
        DebugCluesEnabled = showDebugClues;
        FarmingDebugCluesEnabled = showDebugClues && showFarmingDebugControls;

        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();

        if (playerInv != null)
        {
            playerStatus = playerInv.GetComponent<PlayerStatusSystem>();
            if (playerStatus == null)
                playerStatus = playerInv.gameObject.AddComponent<PlayerStatusSystem>();
        }

        BuildUnifiedHUD();
        BuildWeatherDebug();
        BuildFarmingDebug();
        ApplyDebugClueVisibility();
    }

    void OnEnable()
    {
        WeatherSystem.CurrentWeatherChanged += HandleWeatherChanged;
        RefreshWeatherIndicator();
        if (playerInv != null)
            playerInv.OnInventoryChanged += UpdateInventoryUI;
        if (playerStatus != null)
            playerStatus.Changed += UpdatePlayerStatusUI;
    }

    void OnDisable()
    {
        WeatherSystem.CurrentWeatherChanged -= HandleWeatherChanged;
        if (playerInv != null)
            playerInv.OnInventoryChanged -= UpdateInventoryUI;
        if (playerStatus != null)
            playerStatus.Changed -= UpdatePlayerStatusUI;
    }

    void Start()
    {
        UpdateClockUI();
        UpdateMoneyUI();
        UpdateFarmingDebugUI();
        UpdateInventoryUI();
        UpdatePlayerStatusUI(playerStatus);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    static bool MiningContextActive() => CaveInteriorController.IsMiningContext;
    static bool HousingContextActive()=>BarnInterior.Current!=null&&BarnInteriorSceneController.Instance!=null&&BarnInteriorSceneController.Instance.isActiveAndEnabled;

    void Update()
    {
        ApplyContextVisibility();
        if(housingDebugSuppressed!=HousingContextActive())ApplyDebugClueVisibility();
        if (!housingDebugSuppressed&&GameplayInput.GetKeyDown(debugCluesToggleKey))
            SetDebugCluesEnabled(!DebugCluesEnabled, true);

        UpdateClockUI();
        UpdateMoneyUI();
        if (FarmingDebugCluesEnabled)
            UpdateFarmingDebugUI();
    }

    // =====================================================
    // CLOCK
    // =====================================================

    void UpdateClockUI()
    {
        RefreshWeatherIndicator();
        if (clockText == null)
            return;

        if (TimeManager.Instance == null)
            return;

        var t = TimeManager.Instance;

        GameScheduleData.Date(t.day,out _,out _,out int date);
        clockText.text = $"Hari {date}";
        if (seasonText != null) seasonText.text = GetSeasonLabel();
        if (timeOfDayText != null) timeOfDayText.text = $"{t.hour:00}:{t.minute:00}";

        if (weatherDebugText != null)
        {
            if (WeatherSystem.Instance == null)
            {
                weatherDebugText.text = "Cuaca: Loading...";
            }
            else
            {
                WeatherSystem weather = WeatherSystem.Instance;
                string cropEffect = weather.IsRainToday
                    ? "AUTO WATER TANAMAN"
                    : "TIDAK MENYIRAM";
                weatherDebugText.text =
                    $"Musim: {SeasonVisualController.CurrentSeason}\n" +
                    $"Cuaca: {WeatherSystem.GetShortName(weather.CurrentWeather)} ({cropEffect})\n" +
                    $"Besok: {WeatherSystem.GetShortName(weather.TomorrowWeather)}";
            }
        }
    }

    void HandleWeatherChanged(WeatherType _) => RefreshWeatherIndicator();

    void RefreshWeatherIndicator()
    {
        if (weatherIcon == null || weatherStatusText == null)
            return;

        WeatherType? current = WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : null;
        var time = TimeManager.Instance;
        bool night = time != null && (time.hour < 6 || time.hour >= 18);
        weatherIcon.SetWeather(current, night, current.HasValue ? weatherArtwork?.Icon(current.Value, night) : null);
        string label = current.HasValue ? WeatherSystem.GetShortName(current.Value) : "Memuat...";
        if (weatherStatusText.text != label)
            weatherStatusText.text = label;
    }

    void BuildWeatherDebug()
    {
        if (clockText == null || clockText.transform.parent == null || weatherDebugText != null)
            return;

        weatherDebugText = Instantiate(clockText, timePanelRoot != null ? timePanelRoot : clockText.transform.parent);
        weatherDebugText.name = "WeatherDebug_Runtime";
        weatherDebugText.fontSize = 18f;
        weatherDebugText.alignment = TextAlignmentOptions.TopRight;
        weatherDebugText.textWrappingMode = TextWrappingModes.NoWrap;
        weatherDebugText.raycastTarget = false;
        RectTransform rect = weatherDebugText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(0f, -94f);
        rect.sizeDelta = new Vector2(420f, 58f);
        rect.localScale = Vector3.one;
    }

    /// <summary>Shared visibility control for testing clues and the unified debug menu.</summary>
    public void SetDebugCluesEnabled(bool enabled, bool showFeedback)
    {
        showDebugClues = enabled;
        ApplyDebugClueVisibility();
        if (showFeedback)
            SaveLoadFeedback.Instance?.ShowMessage($"Debug Clues {(enabled ? "ON" : "OFF")}");
    }

    void ApplyDebugClueVisibility()
    {
        housingDebugSuppressed=HousingContextActive();
        DebugCluesEnabled = showDebugClues&&!housingDebugSuppressed;
        FarmingDebugCluesEnabled = DebugCluesEnabled && showFarmingDebugControls;

        if (weatherDebugText != null)
            weatherDebugText.gameObject.SetActive(showWeatherDebugText && DebugCluesEnabled);
        if (farmingDebugRoot != null)
            farmingDebugRoot.gameObject.SetActive(FarmingDebugCluesEnabled);
    }

    // =====================================================
    // MONEY
    // =====================================================

    void UpdateMoneyUI()
    {
        if (moneyText == null)
            return;

        if (ScoreManager.Instance == null)
            return;

        moneyText.text =
            $"{ScoreManager.Instance.points.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))} G";
    }

    static string GetSeasonLabel() => GameScheduleData.SeasonName(SeasonVisualController.CurrentSeason);

    // =====================================================
    // INVENTORY
    // =====================================================

    void UpdateInventoryUI()
    {
        if (playerInv == null)
            return;

        // Seed
        if (seedText != null && seedItem != null)
        {
            seedText.text =
                $"Seed : {playerInv.GetCount(seedItem)}";
        }

        // Cabbage
        if (cabbageText != null && cabbageItem != null)
        {
            cabbageText.text =
                $"Cabbage : {playerInv.GetCount(cabbageItem)}";
        }

        // Milk
        if (milkText != null && milkItem != null)
        {
            milkText.text =
                $"Milk : {playerInv.GetCount(milkItem)}";
        }
    }

    void BuildUnifiedHUD()
    {
        TMP_Text template = moneyText != null ? moneyText : seedText;
        if (template == null || template.transform.parent == null)
        {
            Debug.LogWarning("[HUD] Template text atau Canvas UI tidak ditemukan.");
            return;
        }

        runtimeCanvasRoot = CreateOverlayCanvas();
        runtimeHudRoot = CreateRect("UnifiedPlayerHUD_Runtime", runtimeCanvasRoot);
        runtimeHudRoot.anchorMin = new Vector2(0f, 1f);
        runtimeHudRoot.anchorMax = new Vector2(0f, 1f);
        runtimeHudRoot.pivot = new Vector2(0f, 1f);
        runtimeHudRoot.anchoredPosition = new Vector2(hudScreenMargin.x, -hudScreenMargin.y);
        // Hunger dan status effect tidak mengambil ruang panel utama saat tersembunyi.
        hudPanelSize = new Vector2(300f, 68f);
        runtimeHudRoot.sizeDelta = hudPanelSize;
        AddSolidImage(runtimeHudRoot, GameplayHUDStyle.Panel, hudPanelSprite);

        // Counts are shown on their inventory stacks rather than duplicating them in the HUD.
        if (seedText != null) seedText.gameObject.SetActive(false);
        if (cabbageText != null) cabbageText.gameObject.SetActive(false);
        if (milkText != null) milkText.gameObject.SetActive(false);

        CreateProgressBar(
            runtimeHudRoot,
            template,
            "HealthBar",
            new Vector2(10f, -8f),
            barBackgroundColor,
            healthBarColor,
            healthFillSprite,
            out healthFill,
            out healthValueText
        );
        CreateProgressBar(
            runtimeHudRoot,
            template,
            "StaminaBar",
            new Vector2(10f, -36f),
            barBackgroundColor,
            staminaBarColor,
            staminaFillSprite,
            out staminaFill,
            out staminaValueText
        );
        CreateProgressBar(
            runtimeHudRoot,
            template,
            "HungerBar",
            new Vector2(10f, -64f),
            barBackgroundColor,
            hungerBarColor,
            null,
            out hungerFill,
            out hungerValueText
        );
        hungerRoot = hungerFill.parent.parent as RectTransform;
        hungerRoot.gameObject.SetActive(false);

        BuildStatusEffectGrid(runtimeCanvasRoot, template);

        ConfigureClockPanel(clockText);
        if (timePanelRoot != null && moneyText != null)
        {
            ConfigureExistingText(moneyText, timePanelRoot, new Vector2(494f, -24f), new Vector2(174f, 40f), 23f);
            moneyText.enableAutoSizing = true;
            moneyText.fontSizeMin = 18f;
            moneyText.fontSizeMax = 23f;
            moneyText.alignment = TextAlignmentOptions.MidlineLeft;
            moneyText.margin = Vector4.zero;
            moneyText.gameObject.SetActive(true);
            var currency = CreateRect("Gold Icon", timePanelRoot);
            currency.anchorMin = currency.anchorMax = new Vector2(0, .5f);
            currency.anchoredPosition = new Vector2(466, 0);
            currency.sizeDelta = new Vector2(48, 48);
            if (goldIcon == null) goldIcon = Resources.Load<InventoryTheme>("UI/InventoryTheme")?.coinIcon;
            if (goldIcon != null)
            {
                var art = currency.gameObject.AddComponent<Image>(); art.sprite = goldIcon;
                art.preserveAspect = true; art.raycastTarget = false;
            }
            else
            {
                var fallback = currency.gameObject.AddComponent<MainMenuIcon>();
                fallback.kind = MainMenuIcon.Kind.Coin; fallback.color = new Color(1, .76f, .15f);
                fallback.raycastTarget = false;
            }
        }

        Debug.Log("[HUD] Unified HUD aktif pada Screen Space Overlay Canvas.");
    }

    void BuildFarmingDebug()
    {
        if (runtimeCanvasRoot == null || farmingDebugRoot != null)
            return;

        TMP_Text template = moneyText != null ? moneyText : seedText;
        if (template == null)
            return;

        farmingTool = playerInv != null ? playerInv.GetComponent<FarmingTool>() : FindFirstObjectByType<FarmingTool>();
        toolHotbar = playerInv != null ? playerInv.GetComponent<PlayerToolHotbar>() : FindFirstObjectByType<PlayerToolHotbar>();
        inventoryHotbar = playerInv != null ? playerInv.GetComponent<InventoryHotbarUI>() : FindFirstObjectByType<InventoryHotbarUI>();
        farmingDebugRoot = CreateRect("FarmingDebug_Runtime", runtimeCanvasRoot);
        farmingDebugRoot.anchorMin = farmingDebugRoot.anchorMax = new Vector2(0f, 1f);
        farmingDebugRoot.pivot = new Vector2(0f, 1f);
        farmingDebugRoot.anchoredPosition = new Vector2(hudScreenMargin.x, -hudScreenMargin.y - 198f);
        farmingDebugRoot.sizeDelta = new Vector2(400f, 232f);
        AddSolidImage(farmingDebugRoot, new Color(0.025f, 0.032f, 0.04f, 0.9f));

        farmingDebugText = Instantiate(template, farmingDebugRoot);
        farmingDebugText.name = "TargetStatus";
        farmingDebugText.gameObject.SetActive(true);
        farmingDebugText.fontSize = 14f;
        farmingDebugText.fontStyle = FontStyles.Normal;
        farmingDebugText.alignment = TextAlignmentOptions.TopLeft;
        farmingDebugText.textWrappingMode = TextWrappingModes.Normal;
        farmingDebugText.raycastTarget = false;
        RectTransform statusRect = farmingDebugText.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(12f, -8f);
        statusRect.sizeDelta = new Vector2(376f, 216f);
        statusRect.localScale = Vector3.one;
    }

    void UpdateFarmingDebugUI()
    {
        if (farmingDebugRoot == null || farmingDebugText == null)
            return;

        if (farmingTool == null)
            farmingTool = FindFirstObjectByType<FarmingTool>();

        if (toolHotbar == null)
            toolHotbar = FindFirstObjectByType<PlayerToolHotbar>();
        if (inventoryHotbar == null)
            inventoryHotbar = FindFirstObjectByType<InventoryHotbarUI>();

        string activeTool = toolHotbar != null
            ? PlayerToolHotbar.GetDisplayName(toolHotbar.SelectedTool)
            : "None";
        string selectedItem = inventoryHotbar != null && inventoryHotbar.SelectedItem != null
            ? inventoryHotbar.SelectedItem.itemName
            : "Kosong";
        string target = "Target: arahkan player ke tile di depan";
        if (TryGetDebugSoil(out _, out int x, out int z, out FieldTileSnapshot snapshot))
        {
            string water = snapshot.WateredToday ? "sudah disiram" : "perlu air";
            string fertilizer = snapshot.FertilizedForCurrentCycle
                ? "sudah dipupuk"
                : snapshot.SoilLevel < 5 ? "perlu pupuk" : "pupuk belum perlu";
            string booster = snapshot.BoosterLevelToday > 0
                ? $"booster kualitas Lv.{snapshot.BoosterLevelToday} hari ini"
                : "booster belum aktif";
            string planted = snapshot.State == TileState.Planted && snapshot.Crop != null
                ? $"Bibit {(snapshot.Crop.produceItem != null ? snapshot.Crop.produceItem.itemName : snapshot.Crop.cropId)} tertanam"
                : "Tanah siap ditanami";
            target = $"Target [{x},{z}]: {planted} | {water} | {fertilizer} | {booster}\n" +
                     $"Soil Lv.{snapshot.SoilLevel} {FieldArea.GetSoilStatusName(snapshot.SoilStatus)} " +
                     $"{snapshot.SoilDurability}/80";
        }

        farmingDebugText.text =
            "<b>DEBUG FARMING — PANDUAN KONTROL</b>\n" +
            $"Tool: {activeTool} | Item hotbar: {selectedItem}\n" +
            $"{target}\n\n" +
            "[1–4] Pilih item hotbar  |  [F / Klik Kiri] Gunakan item\n" +
            "[H] Cangkul tanah / panen tanaman matang\n" +
            "[F / Klik Kiri] Tanam sesuai bibit yang sedang dipegang\n" +
            "[V] Siram tanah atau tanaman\n" +
            "[N] Pupuk tanah kosong — pilih Fertilizer di hotbar dahulu\n" +
            "[M] Pasang Crop Booster pada tanaman — pilih Booster di hotbar\n" +
            "[E] Tidur di kasur → Next Day  |  [L] Debug tidur / Next Day\n" +
            "Shortcut debug: F1 Hoe, F2 Seed, F3 Water, F4 Fertilizer, F8 Booster\n" +
            "PageUp Buka Debug Season Menu | PageDown Ikuti musim kalender\n" +
            "F10 Save | F11 Load | F12 Ganti cuaca hari ini\n" +
            $"[{debugCluesToggleKey}] Toggle semua Debug Clues";
    }

    bool TryGetDebugSoil(
        out FieldArea field,
        out int x,
        out int z,
        out FieldTileSnapshot snapshot)
    {
        field = null;
        x = -1;
        z = -1;
        snapshot = default;
        return farmingTool != null &&
               farmingTool.TryGetCurrentTile(out field, out x, out z) &&
               field.TryGetSnapshot(x, z, out snapshot) &&
               (snapshot.State == TileState.Hoed || snapshot.State == TileState.Planted);
    }


    RectTransform CreateOverlayCanvas()
    {
        GameObject canvasObject = new("UnifiedHUDCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f) / .85f;
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform safe = GameplayHUDStyle.Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        return safe;
    }

    void OnDestroy()
    {
        if (runtimeCanvasRoot != null)
            Destroy(runtimeCanvasRoot.GetComponentInParent<Canvas>().gameObject);
    }

    void ConfigureClockPanel(TMP_Text clock)
    {
        if (clock == null || runtimeCanvasRoot == null)
            return;

        timePanelRoot = CreateRect("TimeWeatherPanel", runtimeCanvasRoot);
        timePanelRoot.anchorMin = timePanelRoot.anchorMax = new Vector2(1f, 1f);
        timePanelRoot.pivot = new Vector2(1f, 1f);
        timePanelRoot.anchoredPosition = new Vector2(-134f, -20f);
        timePanelRoot.sizeDelta = new Vector2(690f, 88f);
        var panel = GameplayHUDStyle.Surface(timePanelRoot, new Color(.13f, .19f, .14f, .84f), 14);
        panel.borderWidth = 1;
        panel.borderColor = new Color(.72f, .79f, .62f, .28f);
        var iconRect = CreateRect("Current Weather Icon", timePanelRoot);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(.5f, 1);
        iconRect.anchoredPosition = new Vector2(56, -8);
        iconRect.sizeDelta = new Vector2(42, 42);
        weatherIcon = iconRect.gameObject.AddComponent<WeatherHUDIcon>();
        weatherIcon.raycastTarget = false;
        weatherStatusText = GameplayHUDStyle.Text("Current Weather Name", timePanelRoot, "Memuat...", 15f, Vector2.zero, Vector2.zero);
        ConfigureExistingText(weatherStatusText, timePanelRoot, new Vector2(8, -54), new Vector2(96, 24), 15);
        weatherStatusText.alignment = TextAlignmentOptions.Center;
        weatherStatusText.enableAutoSizing = true;
        weatherStatusText.fontSizeMin = 12f;
        weatherStatusText.fontSizeMax = 15f;
        weatherStatusText.textWrappingMode = TextWrappingModes.NoWrap;
        if (weatherArtwork == null)
            weatherArtwork = Resources.Load<WeatherForecastTVTheme>("UI/WeatherForecastTVTheme");
        foreach (float x in new[] { 112f, 290f, 430f })
        {
            var divider = CreateRect("Divider", timePanelRoot);
            divider.anchorMin = divider.anchorMax = new Vector2(0, .5f);
            divider.anchoredPosition = new Vector2(x, 0);
            divider.sizeDelta = new Vector2(1, 44);
            AddSolidImage(divider, new Color(.74f,.80f,.68f,.25f));
        }

        RectTransform rect = clock.rectTransform;
        rect.SetParent(timePanelRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(130f, -17f);
        rect.sizeDelta = new Vector2(146f, 30f);
        rect.localScale = Vector3.one;
        clock.fontSize = 23f;
        clock.enableAutoSizing = true;
        clock.fontSizeMin = 19;
        clock.fontSizeMax = 23;
        clock.fontStyle = FontStyles.Bold;
        clock.richText = true;
        clock.alignment = TextAlignmentOptions.MidlineLeft;
        clock.textWrappingMode = TextWrappingModes.NoWrap;
        clock.raycastTarget = false;
        clock.margin = Vector4.zero;
        seasonText = GameplayHUDStyle.Text("Season Name", timePanelRoot, GetSeasonLabel(), 16, Vector2.zero, Vector2.zero);
        ConfigureExistingText(seasonText, timePanelRoot, new Vector2(130, -49), new Vector2(146, 24), 16);
        seasonText.alignment = TextAlignmentOptions.MidlineLeft;
        seasonText.margin = Vector4.zero;
        seasonText.color = new Color(.79f, .84f, .75f);
        timeOfDayText = GameplayHUDStyle.Text("Time Of Day", timePanelRoot, "06:00", 25, Vector2.zero, Vector2.zero);
        ConfigureExistingText(timeOfDayText, timePanelRoot, new Vector2(304, -23), new Vector2(112, 42), 25);
        timeOfDayText.alignment = TextAlignmentOptions.Center;
        timeOfDayText.margin = Vector4.zero;
        timeOfDayText.fontStyle = FontStyles.Bold;
        RefreshWeatherIndicator();
        timePanelRoot.gameObject.AddComponent<GameplayPauseMenu>();
    }

    static void ConfigureExistingText(
        TMP_Text text,
        RectTransform parent,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    void CreateProgressBar(
        RectTransform parent,
        TMP_Text textTemplate,
        string objectName,
        Vector2 position,
        Color backgroundColor,
        Color fillColor,
        Sprite fillSprite,
        out RectTransform fill,
        out TMP_Text valueText)
    {
        RectTransform background = CreateRect(objectName, parent);
        background.anchorMin = new Vector2(0f, 1f);
        background.anchorMax = new Vector2(0f, 1f);
        background.pivot = new Vector2(0f, 1f);
        background.anchoredPosition = position;
        background.sizeDelta = new Vector2(280f, 22f);
        GameplayHUDStyle.Icon(background, objectName == "HealthBar" ? MainMenuIcon.Kind.Heart : objectName == "StaminaBar" ? MainMenuIcon.Kind.Energy : MainMenuIcon.Kind.Drop,
            fillColor, new(0f,0f), new(.08f,1f));
        RectTransform track = GameplayHUDStyle.Rect("Track", background, new(.12f,.27f), new(.63f,.73f));
        AddSolidImage(track, backgroundColor, progressBackgroundSprite);

        RectTransform fillRect = CreateRect("Fill", track);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        AddSolidImage(fillRect, fillColor, fillSprite);
        fill = fillRect;

        valueText = Instantiate(textTemplate, background);
        valueText.name = "Value";
        valueText.gameObject.SetActive(true);
        valueText.color = Color.white;
        valueText.fontSize = 16f;
        valueText.fontStyle = FontStyles.Normal;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.raycastTarget = false;

        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(.65f, 0f);
        valueRect.anchorMax = Vector2.one;
        valueRect.pivot = new Vector2(0.5f, 0.5f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        valueRect.localScale = Vector3.one;
    }

    void BuildStatusEffectGrid(RectTransform canvasRoot, TMP_Text textTemplate)
    {
        RectTransform grid = CreateRect("StatusEffectGrid_Runtime", canvasRoot);
        grid.anchorMin = grid.anchorMax = new Vector2(0f, 1f);
        grid.pivot = new Vector2(0f, 1f);
        grid.anchoredPosition = new Vector2(22f, -130f);
        grid.sizeDelta = new Vector2(100f, 306f);

        for (int index = 0; index < 6; index++)
        {
            buffEffectViews.Add(CreateStatusEffectView(
                grid, textTemplate, $"Buff_{index + 1}", 0, index, buffSlotColor));
            debuffEffectViews.Add(CreateStatusEffectView(
                grid, textTemplate, $"Debuff_{index + 1}", 1, index, debuffSlotColor));
        }
    }

    static StatusEffectView CreateStatusEffectView(
        RectTransform parent,
        TMP_Text textTemplate,
        string objectName,
        int column,
        int row,
        Color color)
    {
        RectTransform root = CreateRect(objectName, parent);
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(column * 52f, -row * 52f);
        root.sizeDelta = new Vector2(46f, 46f);
        Image background = AddSolidImage(root, color);

        RectTransform iconRect = CreateRect("Icon", root);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text fallback = Instantiate(textTemplate, root);
        fallback.name = "FallbackID";
        fallback.gameObject.SetActive(true);
        fallback.fontSize = 13f;
        fallback.fontStyle = FontStyles.Bold;
        fallback.alignment = TextAlignmentOptions.Center;
        fallback.raycastTarget = false;
        RectTransform fallbackRect = fallback.rectTransform;
        fallbackRect.anchorMin = Vector2.zero;
        fallbackRect.anchorMax = Vector2.one;
        fallbackRect.offsetMin = fallbackRect.offsetMax = Vector2.zero;
        fallbackRect.localScale = Vector3.one;

        TMP_Text timer = Instantiate(textTemplate, root);
        timer.name = "Timer";
        timer.gameObject.SetActive(true);
        timer.fontSize = 10f;
        timer.fontStyle = FontStyles.Bold;
        timer.alignment = TextAlignmentOptions.BottomRight;
        timer.raycastTarget = false;
        RectTransform timerRect = timer.rectTransform;
        timerRect.anchorMin = Vector2.zero;
        timerRect.anchorMax = Vector2.one;
        timerRect.offsetMin = new Vector2(2f, 1f);
        timerRect.offsetMax = new Vector2(-2f, -1f);
        timerRect.localScale = Vector3.one;

        root.gameObject.SetActive(false);
        return new StatusEffectView(root, background, icon, fallback, timer);
    }

    static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Image AddSolidImage(RectTransform rect, Color color, Sprite sprite = null)
    {
        Image image = sprite != null ? rect.gameObject.AddComponent<Image>() : GameplayHUDStyle.Surface(rect, color);
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void UpdatePlayerStatusUI(PlayerStatusSystem status)
    {
        if (status == null || healthFill == null || staminaFill == null)
            return;

        SetProgress(healthFill, status.Health / status.MaxHealth);
        SetProgress(staminaFill, status.Stamina / status.MaxStamina);
        if (hungerRoot != null)
            hungerRoot.gameObject.SetActive(status.HungerEnabled);
        if (runtimeHudRoot != null)
        {
            Vector2 panelSize = runtimeHudRoot.sizeDelta;
            panelSize.y = status.HungerEnabled ? 96f : 68f;
            runtimeHudRoot.sizeDelta = panelSize;
        }
        if (status.HungerEnabled && hungerFill != null)
            SetProgress(hungerFill, status.Hunger / status.MaxHunger);
        healthValueText.text = $"{Mathf.CeilToInt(status.Health)} / {Mathf.CeilToInt(status.MaxHealth)}";
        staminaValueText.text = $"{Mathf.CeilToInt(status.Stamina)} / {Mathf.CeilToInt(status.MaxStamina)}";
        if (status.HungerEnabled && hungerValueText != null)
            hungerValueText.text = $"HUNGER  {Mathf.CeilToInt(status.Hunger)} / {Mathf.CeilToInt(status.MaxHunger)}";
        RefreshStatusEffectColumn(buffEffectViews, status.BuffSlots);
        RefreshStatusEffectColumn(debuffEffectViews, status.DebuffSlots);
    }

    void RefreshStatusEffectColumn(
        List<StatusEffectView> views,
        IReadOnlyList<PlayerStatusEffectSlot> slots)
    {
        for (int index = 0; index < views.Count; index++)
        {
            PlayerStatusEffectSlot slot = index < slots.Count ? slots[index] : null;
            bool occupied = slot != null && slot.IsOccupied;
            StatusEffectView view = views[index];
            view.Root.gameObject.SetActive(occupied);
            if (!occupied)
                continue;

            Sprite icon = ResolveStatusEffectIcon(slot.EffectId);
            view.Icon.sprite = icon;
            view.Icon.gameObject.SetActive(icon != null);
            view.Fallback.gameObject.SetActive(icon == null);
            view.Fallback.text = slot.EffectId.Substring(0, Mathf.Min(3, slot.EffectId.Length)).ToUpperInvariant();
            view.Timer.text = slot.RemainingDuration > 0f
                ? Mathf.CeilToInt(slot.RemainingDuration).ToString()
                : string.Empty;
        }
    }

    Sprite ResolveStatusEffectIcon(string effectId)
    {
        PlayerStatusEffectIconEntry entry = statusEffectIcons.Find(candidate =>
            candidate != null && string.Equals(candidate.effectId, effectId, StringComparison.OrdinalIgnoreCase));
        return entry?.icon;
    }

    sealed class StatusEffectView
    {
        public readonly RectTransform Root;
        public readonly Image Background;
        public readonly Image Icon;
        public readonly TMP_Text Fallback;
        public readonly TMP_Text Timer;

        public StatusEffectView(
            RectTransform root,
            Image background,
            Image icon,
            TMP_Text fallback,
            TMP_Text timer)
        {
            Root = root;
            Background = background;
            Icon = icon;
            Fallback = fallback;
            Timer = timer;
        }
    }

    static void SetProgress(RectTransform fill, float value)
    {
        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = Mathf.Clamp01(value);
        fill.anchorMax = anchorMax;
    }
}
