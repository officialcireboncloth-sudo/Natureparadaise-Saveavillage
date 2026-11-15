using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Read-only display snapshot supplied by the future hot-spring controller.</summary>
[Serializable]
public sealed class HotSpringViewData
{
    public string dateText, timeText;
    public int gold = -1;
    public Sprite weatherIcon;
    [Min(0)] public float stamina, maximumStamina, recoveryPerTick, recoveryIntervalSeconds, elapsedSeconds;
}

/// <summary>Bathing HUD and exit hook. Does not regenerate stamina, advance timers or move the actor.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-9500)]
public sealed class HotSpringUI : MonoBehaviour
{
    public const string ResourcePath = "UI/HotSpringUI";
    public static HotSpringUI Active { get; private set; }
    public bool IsOpen => Active == this && ownsContext;

    [Header("Shared style / optional images")]
    [SerializeField] HotSpringTheme theme;
    [SerializeField] Canvas canvas;
    [SerializeField] Image weatherIcon, coinIcon, springIcon, exitIcon;
    [SerializeField] Image dateArtwork, goldArtwork, staminaArtwork, staminaFillArtwork;
    [Header("HUD sections")]
    [SerializeField] GameObject datePanel, goldPanel;
    [SerializeField] RectTransform currentFill, nextFill;
    [SerializeField] TMP_Text dateText, timeText, goldText, currentText, nextText, gainText, arrowText;
    [SerializeField] TMP_Text recoveryTitle, recoveryRate, elapsedText, toolsHint;
    [SerializeField] Button exitButton;
    [Header("Future controller callbacks")]
    [SerializeField] UnityEvent onExitRequested = new();
    [SerializeField] UnityEvent onClosed = new();
    public UnityEvent Closed => onClosed;

    HotSpringViewData state = new();
    PlayerController actor;
    Canvas mapCanvas;
    Action exitCallback;
    bool ownsContext, spawned, previousMapVisible, previousCursor;
    CursorLockMode previousCursorLock;
    int openedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActive() => Active = null;

    void Awake()
    {
        if (canvas != null) canvas.enabled = false;
        if (exitButton != null) exitButton.onClick.AddListener(RequestExit);
    }

    /// <summary>Opens the native HUD prefab; supplied recovery/timer values are displayed, never applied.</summary>
    public static HotSpringUI Show(HotSpringViewData data, Action onExit = null, PlayerController player = null)
    {
        if (Active != null || WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput) return null;
        var prefab = Resources.Load<HotSpringUI>(ResourcePath);
        if (prefab == null) { Debug.LogError("Missing Resources/UI/HotSpringUI prefab."); return null; }
        var ui = Instantiate(prefab); ui.spawned = true;
        if (!ui.Present(data, onExit, player)) { Destroy(ui.gameObject); return null; }
        return ui;
    }

    /// <summary>Opens an authored instance without pausing world time. An existing modal must close first.</summary>
    public bool Present(HotSpringViewData data, Action onExit = null, PlayerController player = null)
    {
        if (!Application.isPlaying || ownsContext || Active != null || canvas == null ||
            WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput) return false;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        transform.SetParent(null, false); transform.localScale = Vector3.one;
        actor = player != null ? player : FindFirstObjectByType<PlayerController>();
        exitCallback = onExit; ownsContext = true; Active = this; openedFrame = Time.frameCount;
        previousCursor = Cursor.visible; previousCursorLock = Cursor.lockState;
        actor?.AcquireMovementLock(this); WorldInteractionPrompt.AcquireSuppression(this);
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        if (EventSystem.current == null) new GameObject("EventSystem_HotSpring", typeof(EventSystem), typeof(StandaloneInputModule));
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(true);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(true);
        mapCanvas = FindFirstObjectByType<WorldMapUI>()?.GetComponent<Canvas>();
        if (mapCanvas != null) { previousMapVisible = mapCanvas.enabled; mapCanvas.enabled = false; }
        ApplyArtwork(); canvas.enabled = true; SetData(data);
        GameplayHUDStyle.ApplyHierarchy(gameObject); GameplayInput.ConsumeCurrentFrame(); return true;
    }

    /// <summary>Refreshes caller-owned values, for example after a recovery tick or a clock update.</summary>
    public void SetData(HotSpringViewData data)
    {
        data ??= new HotSpringViewData();
        float maximum = Positive(data.maximumStamina);
        state = new HotSpringViewData
        {
            dateText = data.dateText, timeText = data.timeText, gold = data.gold, weatherIcon = data.weatherIcon,
            maximumStamina = maximum, stamina = Mathf.Clamp(Positive(data.stamina), 0, maximum),
            recoveryPerTick = Positive(data.recoveryPerTick), recoveryIntervalSeconds = Positive(data.recoveryIntervalSeconds),
            elapsedSeconds = Positive(data.elapsedSeconds)
        };
        if (currentText == null) return;
        dateText.text = state.dateText ?? ""; timeText.text = state.timeText ?? "";
        Paint(weatherIcon, state.weatherIcon != null ? state.weatherIcon : theme?.weatherIcon);
        datePanel.SetActive(!string.IsNullOrEmpty(state.dateText) || !string.IsNullOrEmpty(state.timeText) || weatherIcon.enabled);
        goldPanel.SetActive(state.gold >= 0); goldText.text = state.gold >= 0 ? state.gold.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + " G" : "";
        bool known = maximum > 0, full = known && state.stamina >= maximum;
        bool recovering = known && !full && state.recoveryPerTick > 0 && state.recoveryIntervalSeconds > 0;
        float next = recovering ? Mathf.Min(maximum, state.stamina + state.recoveryPerTick) : state.stamina;
        currentText.text = known ? $"{state.stamina:0.#} / {maximum:0.#}" : "— / —";
        nextText.text = recovering ? $"{next:0.#} / {maximum:0.#}" : "";
        arrowText.gameObject.SetActive(recovering);
        gainText.text = recovering ? $"+{next - state.stamina:0.#}" : "";
        recoveryTitle.text = full ? "Stamina penuh" : recovering ? "Stamina dipulihkan" : "Berendam";
        recoveryRate.text = recovering ? $"+{state.recoveryPerTick:0.#} setiap {state.recoveryIntervalSeconds:0.#} detik" : "";
        Fill(currentFill, known ? state.stamina / maximum : 0);
        Fill(nextFill, recovering ? next / maximum : 0);
        elapsedText.text = "Berendam  " + Duration(state.elapsedSeconds);
        toolsHint.text = "Peralatan tidak dapat digunakan";
    }

    static float Positive(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 0 : Mathf.Max(0, v);
    static string Duration(float value)
    {
        int seconds = Mathf.FloorToInt(Mathf.Min(359999, value));
        return seconds >= 3600 ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60:00}:{seconds % 60:00}";
    }
    static void Fill(RectTransform rect, float value)
    { rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1); rect.gameObject.SetActive(value > 0); }
    static void Paint(Image image, Sprite sprite) { image.sprite = sprite; image.enabled = sprite != null; }
    void ApplyArtwork()
    {
        if (theme == null) theme = Resources.Load<HotSpringTheme>("UI/HotSpringTheme");
        Paint(coinIcon, theme?.coinIcon); Paint(springIcon, theme?.springIcon); Paint(exitIcon, theme?.exitIcon);
        Paint(dateArtwork, theme?.panel); Paint(goldArtwork, theme?.panel);
        Paint(staminaArtwork, theme?.staminaBackground); Paint(staminaFillArtwork, theme?.staminaFill);
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount <= openedFrame || GameplayInput.ConsumedThisFrame) return;
#if UNITY_EDITOR
        var focus = UnityEditor.EditorWindow.focusedWindow; if (focus == null || focus.GetType().Name != "GameView") return;
#else
        if (!Application.isFocused) return;
#endif
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) RequestExit();
    }

    /// <summary>Notifies the future bathing controller once, then releases the HUD context.</summary>
    public void RequestExit()
    {
        if (!IsOpen) return;
        var callback = exitCallback; exitCallback = null;
        // Close first so callback-driven scene changes cannot re-enter this request.
        Close();
        try { if (callback != null) callback(); else onExitRequested.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>Closes presentation only. The controller owns ending recovery, animation and water placement.</summary>
    public void Close()
    {
        if (canvas != null) canvas.enabled = false;
        if (!ownsContext) return;
        ownsContext = false; if (Active == this) Active = null;
        actor?.ReleaseMovementLock(this); WorldInteractionPrompt.ReleaseSuppression(this);
        Cursor.visible = previousCursor; Cursor.lockState = previousCursorLock;
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(false);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(false);
        if (mapCanvas != null) mapCanvas.enabled = previousMapVisible;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        exitCallback = null; GameplayInput.ConsumeCurrentFrame(); onClosed.Invoke();
        if (spawned) { spawned = false; Destroy(gameObject); }
    }
    void OnDisable() => Close();
    void OnDestroy() => Close();
}
