using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Optional PNG Sprites")]
    [Tooltip("Kosongkan untuk memakai dummy minimalis modern.")]
    public Sprite hudPanelSprite;
    public Sprite progressBackgroundSprite;
    public Sprite healthFillSprite;
    public Sprite staminaFillSprite;

    RectTransform runtimeHudRoot;
    RectTransform runtimeCanvasRoot;
    RectTransform timePanelRoot;
    RectTransform healthFill;
    RectTransform staminaFill;
    TMP_Text healthValueText;
    TMP_Text staminaValueText;
    TMP_Text weatherDebugText;

    // =====================================================
    // INITIALIZE
    // =====================================================

    void Awake()
    {
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
        UpdateInventoryUI();
        UpdatePlayerStatusUI(playerStatus);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        UpdateClockUI();
        UpdateMoneyUI();
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

        ConfigureClockPanel(clockText);

        Debug.Log("[HUD] Unified HUD aktif pada Screen Space Overlay Canvas.");
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
        healthValueText.text = $"HP  {Mathf.CeilToInt(status.Health)} / {Mathf.CeilToInt(status.MaxHealth)}";
        staminaValueText.text = $"STAMINA  {Mathf.CeilToInt(status.Stamina)} / {Mathf.CeilToInt(status.MaxStamina)}";
    }

    static void SetProgress(RectTransform fill, float value)
    {
        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = Mathf.Clamp01(value);
        fill.anchorMax = anchorMax;
    }
}
