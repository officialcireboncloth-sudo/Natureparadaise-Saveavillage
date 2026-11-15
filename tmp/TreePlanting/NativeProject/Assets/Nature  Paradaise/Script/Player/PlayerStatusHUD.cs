using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
/// <summary>
/// HUD kompatibilitas untuk HP dan stamina. Dapat membuat UI runtime/fallback ketika scene lama
/// belum memiliki referensi visual, sementara HUD utama tetap dikelola sistem unified.
/// </summary>
public sealed class PlayerStatusHUD : MonoBehaviour
{
    [SerializeField] bool useLegacyRuntimeHUD;
    [SerializeField] PlayerStatusSystem status;

    [Header("Layout")]
    [SerializeField] Vector2 panelSize = new Vector2(300f, 96f);
    [SerializeField] Vector2 screenMargin = new Vector2(20f, 20f);
    [SerializeField] bool drawImmediateFallback = true;

    [Header("Colors")]
    [SerializeField] Color panelColor = new Color(0.04f, 0.05f, 0.06f, 0.88f);
    [SerializeField] Color barBackgroundColor = new Color(0.12f, 0.13f, 0.15f, 0.95f);
    [SerializeField] Color healthColor = new Color(0.86f, 0.18f, 0.2f, 1f);
    [SerializeField] Color staminaColor = new Color(0.2f, 0.78f, 0.35f, 1f);

    Canvas runtimeCanvas;
    RectTransform healthFill;
    RectTransform staminaFill;
    TMP_Text healthText;
    TMP_Text staminaText;
    TMP_Text compactStatusText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureStatusHUDExists()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[PLAYER HUD] Inventory player tidak ditemukan.");
            return;
        }

        PlayerStatusSystem playerStatus = inventory.GetComponent<PlayerStatusSystem>();
        if (playerStatus == null)
            playerStatus = inventory.gameObject.AddComponent<PlayerStatusSystem>();

        Debug.Log("[PLAYER HUD] Status tersedia; visual dikelola Unified HUD.");
    }

    void Awake()
    {
        if (!useLegacyRuntimeHUD)
        {
            enabled = false;
            return;
        }

        if (status == null)
            status = GetComponent<PlayerStatusSystem>();

        if (!BuildOnExistingHUDCanvas())
            BuildRuntimeHUD();
    }

    void OnEnable()
    {
        if (status != null)
            status.Changed += Refresh;
    }

    void Start()
    {
        Refresh(status);
    }

    void OnDisable()
    {
        if (status != null)
            status.Changed -= Refresh;
    }

    void OnDestroy()
    {
        if (compactStatusText != null)
            Destroy(compactStatusText.gameObject);

        if (runtimeCanvas != null)
            Destroy(runtimeCanvas.gameObject);
    }

    bool BuildOnExistingHUDCanvas()
    {
        HUDManager hudManager = FindFirstObjectByType<HUDManager>();
        if (hudManager == null)
            return false;

        TMP_Text template = hudManager.moneyText != null
            ? hudManager.moneyText
            : hudManager.clockText;
        if (template == null || template.transform.parent == null)
            return false;

        compactStatusText = Instantiate(template, template.transform.parent);
        compactStatusText.name = "PlayerStatusText_Runtime";
        compactStatusText.gameObject.SetActive(true);
        compactStatusText.raycastTarget = false;
        compactStatusText.textWrappingMode = TextWrappingModes.NoWrap;
        compactStatusText.fontSize = 23f;
        compactStatusText.alignment = TextAlignmentOptions.BottomLeft;

        RectTransform rect = compactStatusText.rectTransform;
        RectTransform templateRect = template.rectTransform;
        rect.anchorMin = templateRect.anchorMin;
        rect.anchorMax = templateRect.anchorMax;
        rect.pivot = templateRect.pivot;
        rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -90f);
        rect.sizeDelta = new Vector2(520f, 100f);
        rect.localScale = Vector3.one;

        drawImmediateFallback = false;
        Debug.Log("[PLAYER HUD] Menggunakan Canvas HUD lama yang sama dengan Gold/Seed.");
        return true;
    }

    void BuildRuntimeHUD()
    {
        if (runtimeCanvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "PlayerStatusHUD_Runtime",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        runtimeCanvas = canvasObject.GetComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("StatusPanel", canvasObject.transform);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = screenMargin;
        panel.sizeDelta = panelSize;
        AddSolidImage(panel, panelColor);

        CreateBar(panel, "HP", 54f, healthColor, out healthFill, out healthText);
        CreateBar(panel, "ST", 14f, staminaColor, out staminaFill, out staminaText);
    }

    void CreateBar(
        RectTransform parent,
        string label,
        float bottom,
        Color fillColor,
        out RectTransform fill,
        out TMP_Text valueText)
    {
        RectTransform background = CreateRect(label + "Bar", parent);
        background.anchorMin = new Vector2(0f, 0f);
        background.anchorMax = new Vector2(1f, 0f);
        background.pivot = new Vector2(0.5f, 0f);
        background.offsetMin = new Vector2(12f, bottom);
        background.offsetMax = new Vector2(-12f, bottom + 28f);
        AddSolidImage(background, barBackgroundColor);

        RectTransform fillRect = CreateRect("Fill", background);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        AddSolidImage(fillRect, fillColor);
        fill = fillRect;

        RectTransform textRect = CreateRect("Value", background);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        valueText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.fontSize = 17f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        valueText.text = label;
    }

    static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static RawImage AddSolidImage(RectTransform rect, Color color)
    {
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void Refresh(PlayerStatusSystem playerStatus)
    {
        if (playerStatus == null)
            return;

        if (compactStatusText != null)
        {
            string hpColor = ColorUtility.ToHtmlStringRGB(healthColor);
            string staminaHex = ColorUtility.ToHtmlStringRGB(staminaColor);
            compactStatusText.text =
                $"<color=#{hpColor}>HP [{BuildTextBar(playerStatus.Health / playerStatus.MaxHealth)}] " +
                $"{playerStatus.Health:0.##}/{playerStatus.MaxHealth:0.##}</color>\n" +
                $"<color=#{staminaHex}>ST [{BuildTextBar(playerStatus.Stamina / playerStatus.MaxStamina)}] " +
                $"{playerStatus.Stamina:0.##}/{playerStatus.MaxStamina:0.##}</color>";
            return;
        }

        if (healthFill == null || staminaFill == null)
            return;

        SetFill(healthFill, playerStatus.Health / playerStatus.MaxHealth);
        SetFill(staminaFill, playerStatus.Stamina / playerStatus.MaxStamina);
        healthText.text = $"HP  {playerStatus.Health:0.##} / {playerStatus.MaxHealth:0.##}";
        staminaText.text = $"ST  {playerStatus.Stamina:0.##} / {playerStatus.MaxStamina:0.##}";
    }

    static string BuildTextBar(float normalizedValue)
    {
        const int segmentCount = 12;
        int filled = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * segmentCount);
        return new string('#', filled) + new string('-', segmentCount - filled);
    }

    void OnGUI()
    {
        if (!drawImmediateFallback || status == null)
            return;

        float scale = Mathf.Clamp(Screen.width / 1100f, 0.75f, 1.35f);
        float width = 300f * scale;
        float height = 94f * scale;
        float margin = 18f * scale;
        Rect panel = new Rect((Screen.width - width) * 0.5f, 64f * scale, width, height);

        DrawRect(panel, panelColor);
        DrawImmediateBar(
            new Rect(panel.x + 12f * scale, panel.y + 12f * scale, panel.width - 24f * scale, 28f * scale),
            status.Health / status.MaxHealth,
            healthColor,
            $"HP  {Mathf.CeilToInt(status.Health)} / {Mathf.CeilToInt(status.MaxHealth)}",
            scale
        );
        DrawImmediateBar(
            new Rect(panel.x + 12f * scale, panel.y + 52f * scale, panel.width - 24f * scale, 28f * scale),
            status.Stamina / status.MaxStamina,
            staminaColor,
            $"ST  {Mathf.CeilToInt(status.Stamina)} / {Mathf.CeilToInt(status.MaxStamina)}",
            scale
        );
    }

    void DrawImmediateBar(Rect rect, float normalizedValue, Color color, string label, float scale)
    {
        DrawRect(rect, barBackgroundColor);
        Rect fillRect = rect;
        fillRect.width *= Mathf.Clamp01(normalizedValue);
        DrawRect(fillRect, color);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(17f * scale),
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, label, style);
    }

    static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static void SetFill(RectTransform fill, float normalizedValue)
    {
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(normalizedValue);
        fill.anchorMax = max;
    }
}
