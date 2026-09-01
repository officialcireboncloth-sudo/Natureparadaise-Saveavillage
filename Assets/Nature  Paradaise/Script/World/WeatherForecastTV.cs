using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>
/// Interaksi TV yang menampilkan cuaca hari ini dan ramalan besok.
/// Waktu permainan dihentikan sementara selama panel forecast terbuka.
/// </summary>
public sealed class WeatherForecastTV : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode forecastShortcutKey = KeyCode.F;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.2f;
    [SerializeField, Min(0f)] float promptHeight = 1.35f;

    PlayerController player;
    TimeManager timeManager;
    GameObject panel;
    GameObject menuContent;
    GameObject forecastContent;
    TMP_Text currentWeatherText;
    TMP_Text tomorrowWeatherText;
    TMP_Text presenterText;
    bool isOpen;
    bool showingForecast;

    void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        bool inRange = (player.transform.position - transform.position).sqrMagnitude <= interactionRadius * interactionRadius;
        if (!inRange)
        {
            if (isOpen) CloseTV();
            return;
        }

        if (!isOpen)
            WorldInteractionPrompt.Request(this, transform, "Tekan E untuk menonton TV", Vector3.Distance(player.transform.position, transform.position), promptHeight);

        if (!isOpen && Input.GetKeyDown(interactKey))
            OpenTV();
        else if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)))
            CloseTV();
        else if (isOpen && !showingForecast && Input.GetKeyDown(forecastShortcutKey))
            ShowForecast();
    }

    /// <summary>Membuka menu TV dan mengunci movement serta waktu.</summary>
    public void OpenTV()
    {
        if (isOpen)
            return;
        BuildUI();
        isOpen = true;
        showingForecast = false;
        panel.SetActive(true);
        WorldInteractionPrompt.AcquireSuppression(this);
        menuContent.SetActive(true);
        forecastContent.SetActive(false);
        if (timeManager == null) timeManager = TimeManager.Instance;
        timeManager?.AcquirePause(this);
        player?.AcquireMovementLock(this);
    }

    /// <summary>Menampilkan forecast yang sudah ditentukan oleh WeatherSystem.</summary>
    public void ShowForecast()
    {
        if (!isOpen || WeatherSystem.Instance == null)
            return;

        showingForecast = true;
        menuContent.SetActive(false);
        forecastContent.SetActive(true);
        WeatherSystem weather = WeatherSystem.Instance;
        currentWeatherText.text = $"TODAY\n{WeatherSystem.GetDisplayName(weather.CurrentWeather)}";
        tomorrowWeatherText.text = $"TOMORROW\n{WeatherSystem.GetDisplayName(weather.TomorrowWeather)}";
        presenterText.text = $"Presenter:\n\"{WeatherSystem.GetForecastMessage(weather.TomorrowWeather)}\"";
    }

    /// <summary>Menutup TV dan melepaskan lock yang dimiliki interaksi ini.</summary>
    public void CloseTV()
    {
        if (!isOpen)
            return;
        isOpen = false;
        showingForecast = false;
        if (panel != null) panel.SetActive(false);
        WorldInteractionPrompt.ReleaseSuppression(this);
        timeManager?.ReleasePause(this);
        player?.ReleaseMovementLock(this);
    }

    void OnDisable() => CloseTV();

    void BuildUI()
    {
        if (panel != null)
            return;

        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject canvasObject = new("WeatherForecastCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = uiLayer >= 0 ? uiLayer : 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateRect("TVPanel", canvasObject.transform).gameObject;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 480f);
        panel.AddComponent<Image>().color = new Color(0.025f, 0.04f, 0.065f, 0.98f);

        TMP_Text header = CreateText(panel.transform, "Header", "NATURE PARADISE TV", 32f, FontStyles.Bold);
        SetRect(header.rectTransform, new Vector2(30f, -24f), new Vector2(660f, 52f));
        header.alignment = TextAlignmentOptions.Center;

        menuContent = CreateRect("Menu", panel.transform).gameObject;
        Stretch(menuContent.GetComponent<RectTransform>());
        Button forecastButton = CreateButton(menuContent.transform, "ForecastButton", "WEATHER FORECAST  [F]", new Vector2(180f, -170f), new Vector2(360f, 76f));
        forecastButton.onClick.AddListener(ShowForecast);
        TMP_Text menuHint = CreateText(menuContent.transform, "Hint", "Pilih program untuk melihat informasi cuaca.", 20f, FontStyles.Normal);
        SetRect(menuHint.rectTransform, new Vector2(80f, -105f), new Vector2(560f, 44f));
        menuHint.alignment = TextAlignmentOptions.Center;

        forecastContent = CreateRect("Forecast", panel.transform).gameObject;
        Stretch(forecastContent.GetComponent<RectTransform>());
        currentWeatherText = CreateText(forecastContent.transform, "Today", string.Empty, 23f, FontStyles.Bold);
        SetRect(currentWeatherText.rectTransform, new Vector2(55f, -105f), new Vector2(280f, 92f));
        currentWeatherText.alignment = TextAlignmentOptions.Center;
        tomorrowWeatherText = CreateText(forecastContent.transform, "Tomorrow", string.Empty, 23f, FontStyles.Bold);
        SetRect(tomorrowWeatherText.rectTransform, new Vector2(385f, -105f), new Vector2(280f, 92f));
        tomorrowWeatherText.alignment = TextAlignmentOptions.Center;
        presenterText = CreateText(forecastContent.transform, "Presenter", string.Empty, 21f, FontStyles.Normal);
        SetRect(presenterText.rectTransform, new Vector2(55f, -235f), new Vector2(610f, 120f));
        presenterText.alignment = TextAlignmentOptions.Center;
        presenterText.textWrappingMode = TextWrappingModes.Normal;

        Button close = CreateButton(panel.transform, "CloseButton", "CLOSE  [E / ESC]", new Vector2(250f, -405f), new Vector2(220f, 48f));
        close.onClick.AddListener(CloseTV);
        panel.SetActive(false);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject target = new(name, typeof(RectTransform));
        target.layer = parent.gameObject.layer;
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.28f, 0.42f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(rect, "Label", label, 20f, FontStyles.Bold);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureTestingTVExists()
    {
        if (FindFirstObjectByType<WeatherForecastTV>() != null)
            return;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        if (PlayerSpawnPoint.TryGet("player-home", out PlayerSpawnPoint home))
        {
            position = home.transform.position - home.transform.right * 2.5f + Vector3.up * 0.9f;
            rotation = home.transform.rotation;
        }
        else
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            position = player.transform.position - player.transform.right * 2.5f + Vector3.up * 0.9f;
        }

        GameObject tv = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tv.name = "WeatherForecastTV_DebugRuntime";
        tv.transform.SetPositionAndRotation(position, rotation);
        tv.transform.localScale = new Vector3(1.8f, 1.2f, 0.35f);
        Renderer renderer = tv.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.035f, 0.055f, 0.08f, 1f);
        tv.AddComponent<WeatherForecastTV>();
        Debug.Log("[WEATHER] TV forecast testing dibuat dekat PlayerHomeSpawn.");
    }
}
