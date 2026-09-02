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

    [Header("Status Effect Icons")]
    [Tooltip("Daftarkan Sprite berdasarkan Effect ID. Slot tanpa Sprite memakai singkatan ID.")]
    public List<PlayerStatusEffectIconEntry> statusEffectIcons = new();

    [Header("Debug Clues")]
    [Tooltip("Master switch untuk seluruh clue pengembangan. Dapat diubah saat Play Mode memakai tombol toggle.")]
    public bool showDebugClues = true;
    [Tooltip("Tombol untuk menampilkan atau menyembunyikan seluruh Debug Clues.")]
    public KeyCode debugCluesToggleKey = KeyCode.F9;
    [Tooltip("Tampilkan cuaca runtime sebagai informasi debug.")]
    public bool showWeatherDebugText = true;
    [Tooltip("Tampilkan panduan kontrol dan detail target farming sebagai informasi debug.")]
    public bool showFarmingDebugControls = true;

    public static bool DebugCluesEnabled { get; private set; } = true;
    public static bool FarmingDebugCluesEnabled { get; private set; } = true;

    RectTransform runtimeHudRoot;
    RectTransform runtimeCanvasRoot;
    RectTransform timePanelRoot;
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
        if (playerInv != null)
            playerInv.OnInventoryChanged += UpdateInventoryUI;
        if (playerStatus != null)
            playerStatus.Changed += UpdatePlayerStatusUI;
    }

    void OnDisable()
    {
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

    void Update()
    {
        if (Input.GetKeyDown(debugCluesToggleKey))
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
        if (clockText == null)
            return;

        if (TimeManager.Instance == null)
            return;

        var t = TimeManager.Instance;

        clockText.text =
            $"{t.hour:00}:{t.minute:00}\n" +
            $"Day {t.day}";

        if (weatherDebugText != null)
        {
            string weather = WeatherSystem.Instance != null
                ? WeatherSystem.GetShortName(WeatherSystem.Instance.CurrentWeather)
                : "Loading...";
            weatherDebugText.text = $"Weather: {weather}";
        }
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
        rect.anchoredPosition = new Vector2(0f, -66f);
        rect.sizeDelta = new Vector2(300f, 34f);
        rect.localScale = Vector3.one;
    }

    void SetDebugCluesEnabled(bool enabled, bool showFeedback)
    {
        showDebugClues = enabled;
        ApplyDebugClueVisibility();
        if (showFeedback)
            SaveLoadFeedback.Instance?.ShowMessage($"Debug Clues {(enabled ? "ON" : "OFF")}");
    }

    void ApplyDebugClueVisibility()
    {
        DebugCluesEnabled = showDebugClues;
        FarmingDebugCluesEnabled = showDebugClues && showFarmingDebugControls;

        if (weatherDebugText != null)
            weatherDebugText.gameObject.SetActive(showDebugClues && showWeatherDebugText);
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
            $"Gold : {ScoreManager.Instance.points}";
    }

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
        hudPanelSize.y = 188f;
        runtimeHudRoot.sizeDelta = hudPanelSize;
        AddSolidImage(runtimeHudRoot, hudPanelColor, hudPanelSprite);

        ConfigureExistingText(moneyText, runtimeHudRoot, new Vector2(18f, -10f), new Vector2(250f, 32f), 24f);
        ConfigureExistingText(seedText, runtimeHudRoot, new Vector2(18f, -46f), new Vector2(108f, 28f), 19f);
        ConfigureExistingText(cabbageText, runtimeHudRoot, new Vector2(137f, -46f), new Vector2(130f, 28f), 19f);
        ConfigureExistingText(milkText, runtimeHudRoot, new Vector2(278f, -46f), new Vector2(100f, 28f), 19f);

        CreateProgressBar(
            runtimeHudRoot,
            template,
            "HealthBar",
            new Vector2(18f, -84f),
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
            new Vector2(18f, -132f),
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
            new Vector2(18f, -180f),
            barBackgroundColor,
            hungerBarColor,
            null,
            out hungerFill,
            out hungerValueText
        );
        hungerRoot = hungerFill.parent as RectTransform;
        hungerRoot.gameObject.SetActive(false);

        BuildStatusEffectGrid(runtimeCanvasRoot, template);

        ConfigureClockPanel(clockText);

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
            string booster = snapshot.GrowthBoosterPercent > 0
                ? $"booster -{snapshot.GrowthBoosterPercent}%"
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


    static RectTransform CreateOverlayCanvas()
    {
        GameObject canvasObject = new("UnifiedHUDCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject.GetComponent<RectTransform>();
    }

    void ConfigureClockPanel(TMP_Text clock)
    {
        if (clock == null || runtimeCanvasRoot == null)
            return;

        timePanelRoot = CreateRect("TimeWeatherPanel", runtimeCanvasRoot);
        timePanelRoot.anchorMin = timePanelRoot.anchorMax = new Vector2(1f, 1f);
        timePanelRoot.pivot = new Vector2(1f, 1f);
        timePanelRoot.anchoredPosition = new Vector2(-20f, -20f);
        timePanelRoot.sizeDelta = new Vector2(320f, 112f);

        RectTransform rect = clock.rectTransform;
        rect.SetParent(timePanelRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(300f, 64f);
        rect.localScale = Vector3.one;
        clock.fontSize = 24f;
        clock.alignment = TextAlignmentOptions.TopRight;
        clock.textWrappingMode = TextWrappingModes.NoWrap;
        clock.raycastTarget = false;
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
        background.sizeDelta = new Vector2(364f, 34f);
        AddSolidImage(background, backgroundColor, progressBackgroundSprite);

        RectTransform fillRect = CreateRect("Fill", background);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        AddSolidImage(fillRect, fillColor, fillSprite);
        fill = fillRect;

        valueText = Instantiate(textTemplate, background);
        valueText.name = "Value";
        valueText.gameObject.SetActive(true);
        valueText.color = Color.white;
        valueText.fontSize = 18f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.raycastTarget = false;

        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.pivot = new Vector2(0.5f, 0.5f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        valueRect.localScale = Vector3.one;
    }

    void BuildStatusEffectGrid(RectTransform canvasRoot, TMP_Text textTemplate)
    {
        RectTransform grid = CreateRect("StatusEffectGrid_Runtime", canvasRoot);
        grid.anchorMin = grid.anchorMax = new Vector2(1f, 0.5f);
        grid.pivot = new Vector2(1f, 0.5f);
        grid.anchoredPosition = new Vector2(-28f, 0f);
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
        Image image = rect.gameObject.AddComponent<Image>();
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
            panelSize.y = status.HungerEnabled ? 226f : 188f;
            runtimeHudRoot.sizeDelta = panelSize;
        }
        if (status.HungerEnabled && hungerFill != null)
            SetProgress(hungerFill, status.Hunger / status.MaxHunger);
        healthValueText.text = $"HP  {Mathf.CeilToInt(status.Health)} / {Mathf.CeilToInt(status.MaxHealth)}";
        staminaValueText.text = status.IsExhausted
            ? $"EXHAUSTED  {Mathf.CeilToInt(status.Stamina)} / {Mathf.CeilToInt(status.MaxStamina)}"
            : $"STAMINA  {Mathf.CeilToInt(status.Stamina)} / {Mathf.CeilToInt(status.MaxStamina)}";
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
