using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One persistent opaque cover shared by scene launch, save restoration and interior transitions.</summary>
public sealed class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }
    public static bool IsVisible => Instance != null && Instance.owners.Count > 0;
    static readonly string[] Labels = { "Loading", "Loading.", "Loading..", "Loading...", "Loading...." };
    readonly HashSet<object> owners = new();
    TMP_Text label;
    PlayerController player;
    TimeManager clock;
    float openedAt;
    int displayedDots = -1;

    /// <summary>Covers the viewport immediately. Multiple loading stages can hold independent ownership.</summary>
    public static void Show(object owner)
    {
        if (owner == null) return;
        if (Instance == null) new GameObject("LoadingScreen_Runtime", typeof(RectTransform)).AddComponent<LoadingScreen>();
        var screen = Instance;
        bool opening = screen.owners.Count == 0;
        screen.owners.Add(owner);
        screen.gameObject.SetActive(true);
        if (opening) { screen.openedAt = Time.unscaledTime; screen.displayedDots = -1; }
        screen.BindContext();
        screen.RefreshLabel();
        GameplayInput.ConsumeCurrentFrame();
    }

    /// <summary>Reveals gameplay only when every loading stage has finished.</summary>
    public static void Hide(object owner)
    {
        if (Instance == null || owner == null || !Instance.owners.Remove(owner) || Instance.owners.Count > 0) return;
        Instance.ReleaseContext();
        Instance.gameObject.SetActive(false);
        GameplayInput.ConsumeCurrentFrame();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var black = GameplayHUDStyle.Rect("Opaque Black", transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        black.color = Color.black; black.raycastTarget = true;
        var safe = GameplayHUDStyle.Rect("Safe Area", transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        label = GameplayHUDStyle.Text("Loading Label", safe, Labels[0], 25, new Vector2(.1f, .42f), new Vector2(.9f, .54f));
        label.color = Color.white; label.alignment = TextAlignmentOptions.Center;
        SceneManager.sceneLoaded += SceneLoaded;
        gameObject.SetActive(false);
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode) { if (owners.Count > 0) BindContext(); }
    void BindContext()
    {
        var nextClock = TimeManager.Instance;
        var nextPlayer = SaveManager.Instance != null && SaveManager.Instance.playerInv != null
            ? SaveManager.Instance.playerInv.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
        if (clock != nextClock) { clock?.ReleasePause(this); clock = nextClock; }
        if (player != nextPlayer) { player?.ReleaseMovementLock(this); player = nextPlayer; }
        clock?.AcquirePause(this); player?.AcquireMovementLock(this);
        WorldInteractionPrompt.AcquireSuppression(this);
    }
    void ReleaseContext()
    {
        clock?.ReleasePause(this); player?.ReleaseMovementLock(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        clock = null; player = null;
    }
    void Update() => RefreshLabel();
    void RefreshLabel()
    {
        int dots = Mathf.FloorToInt((Time.unscaledTime - openedAt) / .3f) % Labels.Length;
        if (dots == displayedDots) return;
        displayedDots = dots; label.text = Labels[dots];
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        ReleaseContext();
        if (Instance == this) Instance = null;
    }
}
