using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Bed choices own their pause locks; choosing sleep releases them before the lifecycle starts.</summary>
[DefaultExecutionOrder(-9500)]
public sealed class BedRestMenu : MonoBehaviour
{
    public static BedRestMenu Instance { get; private set; }
    public static bool IsOpen => Instance != null;
    PlayerLifeCycle player;
    PlayerController movement;
    TimeManager clock;
    Transform sleepPose, wakeStandPoint;
    float previousTimeScale;
    bool released;
    int selection;
    readonly Button[] buttons = new Button[4];
    readonly TMP_Text[] markers = new TMP_Text[4];
    GameObject previousSelection;

    public static void Show(PlayerLifeCycle player, Transform sleepPose = null, Transform wakeStandPoint = null)
    {
        if (player == null || player.IsBusy || IsOpen || GameplayPauseMenu.BlocksGameplayInput ||
            WorldInteractionPrompt.IsSuppressed || AnimalController.CurrentCareAction != null) return;
        var menu = new GameObject("Bed Rest Menu", typeof(RectTransform)).AddComponent<BedRestMenu>();
        Instance = menu;
        menu.player = player;
        menu.sleepPose = sleepPose;
        menu.wakeStandPoint = wakeStandPoint;
        menu.movement = player.GetComponent<PlayerController>();
        menu.clock = TimeManager.Instance;
        menu.previousTimeScale = Time.timeScale;
        menu.movement?.AcquireMovementLock(menu);
        menu.clock?.AcquirePause(menu);
        WorldInteractionPrompt.AcquireSuppression(menu);
        Time.timeScale = 0f;
        GameplayInput.ConsumeCurrentFrame();
        if (EventSystem.current != null)
        {
            menu.previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }
        menu.Build();
    }

    void Update()
    {
        if (released) return;
        if (player == null || player.IsBusy) { Close(); return; }
        if (GameplayInput.ConsumedThisFrame) return;
#if UNITY_EDITOR
        var focused = UnityEditor.EditorWindow.focusedWindow;
        if (focused == null || focused.GetType().Name != "GameView") return;
#else
        if (!Application.isFocused) return;
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Choose(selection);
    }

    void MoveSelection(int direction)
    {
        do { selection = (selection + direction + buttons.Length) % buttons.Length; }
        while (!buttons[selection].interactable);
        RefreshSelection();
        GameplayInput.ConsumeCurrentFrame();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < markers.Length; i++) markers[i].text = i == selection ? "›" : "";
    }

    public void Choose(int choice)
    {
        if (released || choice < 0 || choice >= buttons.Length || !buttons[choice].interactable) return;
        var target = player;
        var pose = sleepPose;
        var stand = wakeStandPoint;
        Close();
        switch (choice)
        {
            case 0: target.SleepWithoutSave(pose, stand); break;
            case 1: target.SleepAndSave(pose, stand); break;
            case 2: SaveManager.Instance?.LoadGame(); break;
        }
    }

    public void Close()
    {
        Release();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void Release()
    {
        if (released) return;
        released = true;
        movement?.ReleaseMovementLock(this);
        clock?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        Time.timeScale = previousTimeScale;
        GameplayInput.ConsumeCurrentFrame();
        if (Instance == this) Instance = null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }
    void OnDisable() => Release();
    void OnDestroy() => Release();

    void Build()
    {
        var theme = Resources.Load<BedRestTheme>("UI/BedRestTheme");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
            new GameObject("EventSystem_RestMenu", typeof(EventSystem), typeof(StandaloneInputModule));
        var backdrop = GameplayHUDStyle.Rect("Dim Background", transform, Vector2.zero, Vector2.one);
        backdrop.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .48f);
        var safe = GameplayHUDStyle.Rect("Safe Area", transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel = GameplayHUDStyle.Rect("Rest Panel", safe, Vector2.one * .5f, Vector2.one * .5f);
        panel.sizeDelta = new Vector2(414, 526);
        if (theme != null && theme.panel != null)
        {
            var art = panel.gameObject.AddComponent<Image>(); art.sprite = theme.panel; art.type = Image.Type.Sliced;
        }
        else GameplayHUDStyle.Surface(panel, new Color(.10f, .16f, .22f, .94f), 14).raycastTarget = true;
        Label("Title", panel, "Menu Istirahat", 31, 28, 28, 358, 46, true);
        Label("Subtitle", panel, "Apa yang ingin kamu lakukan?", 19, 28, 80, 358, 30, true).color = new Color(.7f, .81f, .93f);
        string[] labels = { "Tidur Tanpa Save", "Tidur & Save Game", "Load Game", "Batal" };
        Sprite[] icons = { theme != null ? theme.sleepIcon : null, theme != null ? theme.sleepAndSaveIcon : null,
            theme != null ? theme.loadIcon : null, theme != null ? theme.backIcon : null };
        bool validSave = TrySaveInfo(out string saveInfo);
        for (int i = 0; i < buttons.Length; i++)
        {
            int choice = i;
            var row = Fixed(labels[i], panel, 28, 130 + i * 68, 358, 62);
            var image = GameplayHUDStyle.Surface(row, Color.white, 10); image.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(.24f, .29f, .33f, .64f);
            colors.highlightedColor = new Color(.31f, .55f, .36f, .92f);
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(.24f, .44f, .29f, .95f);
            colors.disabledColor = new Color(.17f, .20f, .23f, .4f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            row.gameObject.AddComponent<MainMenuButtonAudio>();
            button.onClick.AddListener(() => Choose(choice));
            button.interactable = i != 2 || (validSave && SaveManager.Instance != null);
            buttons[i] = button;
            var icon = Fixed("Image Slot", row, 22, 12, 38, 38).gameObject.AddComponent<Image>();
            icon.sprite = icons[i]; icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = icons[i] != null;
            Label("Label", row, labels[i], 21, 84, 0, 244, 62).alpha = button.interactable ? 1f : .42f;
            markers[i] = Label("Keyboard Selection", row, "", 26, 6, 0, 18, 62);
        }
        Label("Last Save", panel, saveInfo, 18, 28, 415, 358, 30, true).color = new Color(.7f, .81f, .93f);
        Label("Key Hints", panel, "↑ ↓  Pilih     Enter  Konfirmasi     Esc  Kembali", 16, 20, 469, 374, 30, true);
        RefreshSelection();
    }

    static bool TrySaveInfo(out string info)
    {
        info = "Save terakhir: Belum ada save";
        if (!SaveManager.SaveExists()) return false;
        try
        {
            var data = JsonUtility.FromJson<SaveManager.SaveData>(File.ReadAllText(SaveManager.DefaultSavePath));
            if (data == null || data.day < 1 || data.hour < 0 || data.hour > 23 || data.minute < 0 || data.minute > 59)
                throw new InvalidDataException("Invalid save calendar");
            info = $"Save terakhir: Hari {data.day} • {data.hour:00}:{data.minute:00}";
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
        {
            info = "Save terakhir: Tidak dapat dibaca";
            return false;
        }
    }

    static RectTransform Fixed(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = GameplayHUDStyle.Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1));
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
    static TMP_Text Label(string name, Transform parent, string value, float size, float x, float y, float width, float height, bool center = false)
    {
        var rect = Fixed(name, parent, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.raycastTarget = false;
        text.alignment = center ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = size;
        return text;
    }
}
