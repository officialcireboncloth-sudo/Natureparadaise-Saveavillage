using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SaveSlotMenuMode { Save, Load, NewGame }

/// <summary>Shared mouse, keyboard and touch picker for manual saves, loads and new games.</summary>
[DefaultExecutionOrder(-9600)]
public sealed class SaveSlotMenu : MonoBehaviour
{
    public static SaveSlotMenu Instance { get; private set; }
    public static bool IsOpen => Instance != null;
    public int SelectedSlot { get; private set; }
    public SaveSlotMenuMode Mode { get; private set; }

    readonly SaveSlotInfo[] slots = new SaveSlotInfo[SaveSlots.AutoSaveSlot];
    readonly Button[] rows = new Button[SaveSlots.AutoSaveSlot];
    readonly MainMenuRoundedImage[] surfaces = new MainMenuRoundedImage[SaveSlots.AutoSaveSlot];
    int RowCount => Mode == SaveSlotMenuMode.Load ? SaveSlots.AutoSaveSlot : SaveSlots.Count;
    Action<int> onSelected;
    PlayerController movement;
    TimeManager clock;
    float previousScale;
    GameObject previousSelection;
    RectTransform safeArea, panel;
    TMP_Text notice, confirmLabel;
    Button confirmButton;
    bool released, awaitingOverwrite;
    int lastConfirmFrame = -1;
    const float DoubleClickSeconds = .35f;
    int lastClickedSlot;
    float lastClickTime;

    /// <summary>Opens a paused picker without changing the active slot until the caller completes its action.</summary>
    public static void Show(SaveSlotMenuMode mode, Action<int> selected)
    {
        if (IsOpen || selected == null) return;
        var menu = new GameObject("Save Slot Menu", typeof(RectTransform)).AddComponent<SaveSlotMenu>();
        Instance = menu;
        menu.Mode = mode; menu.onSelected = selected;
        menu.previousScale = Time.timeScale;
        menu.clock = TimeManager.Instance;
        menu.movement = FindFirstObjectByType<PlayerController>();
        menu.clock?.AcquirePause(menu);
        menu.movement?.AcquireMovementLock(menu);
        WorldInteractionPrompt.AcquireSuppression(menu);
        Time.timeScale = 0;
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
        FitPanel();
        // Keyboard navigation is owned here, avoiding duplicate Submit on a pointer-selected button.
        EventSystem.current?.SetSelectedGameObject(null);
        if (GameplayInput.ConsumedThisFrame) return;
#if UNITY_EDITOR
        var focus = UnityEditor.EditorWindow.focusedWindow;
        if (focus == null || focus.GetType().Name != "GameView") return;
#else
        if (!Application.isFocused) return;
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
        for (int slot = 1; slot <= SaveSlots.Count; slot++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + slot - 1)) ||
                Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + slot - 1))) SelectSlot(slot);
        if (Mode == SaveSlotMenuMode.Load && Input.GetKeyDown(KeyCode.A)) SelectSlot(SaveSlots.AutoSaveSlot);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmSelection();
    }

    void MoveSelection(int direction)
    {
        int slot = SelectedSlot > 0 ? SelectedSlot : 1;
        for (int i = 0; i < RowCount; i++)
        {
            slot = (slot - 1 + direction + RowCount) % RowCount + 1;
            if (rows[slot - 1].interactable) { SelectSlot(slot); return; }
        }
    }

    /// <summary>Selects an available card; load mode keeps empty or unreadable slots disabled.</summary>
    public void SelectSlot(int slot)
    {
        if (released || slot < 1 || slot > RowCount || !rows[slot - 1].interactable) return;
        lastClickedSlot = 0;
        SelectedSlot = slot; awaitingOverwrite = false;
        RefreshSelection();
    }

    void ClickSlot(int slot)
    {
        if (released || !rows[slot - 1].interactable) return;
        // Use unscaled time: the picker pauses gameplay, including on touch devices.
        bool doubleClick = lastClickedSlot == slot && Time.unscaledTime - lastClickTime <= DoubleClickSeconds;
        if (SelectedSlot != slot || !awaitingOverwrite) SelectSlot(slot);
        if (doubleClick)
        {
            lastClickedSlot = 0;
            ConfirmSelection();
        }
        else
        {
            lastClickedSlot = slot;
            lastClickTime = Time.unscaledTime;
        }
    }

    /// <summary>Confirms a slot, with an extra explicit confirmation before replacing an occupied save.</summary>
    public void ConfirmSelection()
    {
        if (released || SelectedSlot == 0 || !confirmButton.interactable || lastConfirmFrame == Time.frameCount) return;
        lastClickedSlot = 0;
        lastConfirmFrame = Time.frameCount;
        if (Mode != SaveSlotMenuMode.Load && slots[SelectedSlot - 1].exists && !awaitingOverwrite)
        {
            awaitingOverwrite = true;
            notice.text = $"Timpa save di Slot {SelectedSlot}? Slot lain tetap tersimpan.";
            notice.color = new Color(1f, .77f, .47f);
            confirmLabel.text = $"Ya, Timpa Slot {SelectedSlot}";
            GameplayInput.ConsumeCurrentFrame();
            return;
        }
        int slot = SelectedSlot;
        var callback = onSelected;
        Close();
        callback(slot);
    }

    /// <summary>Cancels overwrite confirmation first, then closes without changing saves or the active slot.</summary>
    public void Cancel()
    {
        lastClickedSlot = 0;
        if (awaitingOverwrite) { awaitingOverwrite = false; RefreshSelection(); GameplayInput.ConsumeCurrentFrame(); }
        else Close();
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
        clock?.ReleasePause(this); movement?.ReleaseMovementLock(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        Time.timeScale = previousScale;
        GameplayInput.ConsumeCurrentFrame();
        if (Instance == this) Instance = null;
        EventSystem.current?.SetSelectedGameObject(previousSelection);
    }
    void OnDisable() => Release();
    void OnDestroy() => Release();

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 700;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null) new GameObject("EventSystem_SaveSlots", typeof(EventSystem), typeof(StandaloneInputModule));
        var shade = GameplayHUDStyle.Rect("Backdrop", transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        shade.color = new Color(0, 0, 0, .60f);
        safeArea = GameplayHUDStyle.Rect("Safe Area", transform, Vector2.zero, Vector2.one);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();
        panel = GameplayHUDStyle.Rect("Slot Panel", safeArea, Vector2.one * .5f, Vector2.one * .5f);
        panel.sizeDelta = new Vector2(620, 624);
        GameplayHUDStyle.Surface(panel, GameplayHUDStyle.Modal, 16).raycastTarget = true;
        string title = Mode == SaveSlotMenuMode.Save ? "Simpan Permainan" : Mode == SaveSlotMenuMode.Load ? "Pilih Save Data" : "Game Baru";
        Label("Title", panel, title, 28, 24, 18, 520, 40, true);
        string choices = Mode == SaveSlotMenuMode.Load ? "Slot 1–4 / Autosave" : "Slot 1–4";
        Label("Subtitle", panel, Application.isMobilePlatform ? choices + " · Ketuk dua kali untuk konfirmasi" :
            choices + " · Double klik untuk konfirmasi", 16, 24, 60, 572, 26).color = GameplayHUDStyle.Muted;
        MakeButton("Close", panel, "×", 552, 16, 44, 44, Close);
        bool compact = Mode == SaveSlotMenuMode.Load;
        for (int i = 0; i < RowCount; i++)
        {
            int slot = i + 1;
            var info = slots[i] = SaveSlots.ReadInfo(slot);
            bool automatic = slot == SaveSlots.AutoSaveSlot;
            var row = MakeButton(automatic ? "Autosave" : "Slot " + slot, panel, "", 24, 102 + i * (compact ? 78 : 98), 572, compact ? 72 : 88, () => ClickSlot(slot));
            rows[i] = row; surfaces[i] = row.GetComponent<MainMenuRoundedImage>();
            row.interactable = Mode != SaveSlotMenuMode.Load || info.canLoad;
            var number = Fixed("Number", row.transform, 12, compact ? 10 : 15, 52, compact ? 52 : 58);
            GameplayHUDStyle.Surface(number, new Color(.12f, .19f, .20f, .7f), 9);
            Label("Slot Number", number, automatic ? "A" : slot.ToString(), 28, 0, 0, 52, compact ? 52 : 58, true);
            Label("Slot Name", row.transform, automatic ? "Autosave" : "Slot " + slot, 19, 78, compact ? 2 : 6, 190, 26, false, true);
            if (movement != null && slot == SaveSlots.ActiveSlot)
                Label("Active Tag", row.transform, "Sedang dimainkan", 13, 302, 7, 252, 22).alignment = TextAlignmentOptions.MidlineRight;
            string detail = !info.exists ? "Belum ada save" : !info.canLoad ? info.problem :
                $"{info.CalendarLabel}   ·   {info.gold.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))} G";
            string location = !info.exists ? "Slot kosong" : !info.canLoad ? "Pilih slot lain untuk Load" : $"{info.location}   ·   {info.savedAt:dd/MM/yyyy HH:mm}";
            Label("Summary", row.transform, detail, 16, 78, compact ? 25 : 32, 474, 24);
            Label("Location", row.transform, location, 13, 78, compact ? 49 : 58, 474, compact ? 20 : 22).color = GameplayHUDStyle.Muted;
        }
        notice = Label("Notice", panel, "", 15, 24, 498, 572, 40);
        MakeButton("Cancel", panel, "Kembali", 24, 550, 160, 46, Cancel);
        confirmButton = MakeButton("Confirm", panel, "", 200, 550, 396, 46, ConfirmSelection);
        confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>();
        Label("Keyboard Hints", panel, Application.isMobilePlatform ? "Ketuk slot, lalu konfirmasi pilihanmu." :
            (Mode == SaveSlotMenuMode.Load ? "1–4 / A / ↑ ↓  Pilih" : "1–4 / ↑ ↓  Pilih") + "    Enter  Konfirmasi    Esc  Kembali", 12, 24, 600, 572, 20).color = GameplayHUDStyle.Muted;
        int initial = SaveSlots.ActiveSlot;
        if (initial < 1 || initial > RowCount) initial = 1;
        if (!rows[initial - 1].interactable)
        {
            initial = 0;
            for (int i = 0; i < RowCount; i++) if (rows[i].interactable) { initial = i + 1; break; }
        }
        SelectedSlot = initial;
        RefreshSelection();
        Canvas.ForceUpdateCanvases(); FitPanel();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < RowCount; i++)
        {
            var colors = rows[i].colors;
            colors.normalColor = i + 1 == SelectedSlot ? new Color(.28f, .41f, .32f, 1) : GameplayHUDStyle.Card;
            colors.selectedColor = colors.normalColor;
            rows[i].colors = colors;
            surfaces[i].borderWidth = i + 1 == SelectedSlot ? 2 : 0;
            surfaces[i].borderColor = GameplayHUDStyle.Accent;
            surfaces[i].SetVerticesDirty();
        }
        confirmButton.interactable = SelectedSlot > 0;
        string action = Mode == SaveSlotMenuMode.Save ? "Simpan ke" : Mode == SaveSlotMenuMode.Load ? "Load" : "Mulai di";
        confirmLabel.text = SelectedSlot == SaveSlots.AutoSaveSlot ? "Load Autosave" : SelectedSlot > 0 ? $"{action} Slot {SelectedSlot}" : "Belum ada save";
        notice.color = GameplayHUDStyle.Muted;
        notice.text = SelectedSlot == 0 ? "Belum ada save yang bisa dimuat." : Mode == SaveSlotMenuMode.Load ?
            "Progres saat ini yang belum disimpan akan diganti." : "Autosave terpisah. Slot 1–4 hanya diganti saat kamu menyimpan.";
    }

    void FitPanel()
    {
        if (safeArea == null || panel == null) return;
        float scale = Mathf.Min(1, Mathf.Min(Mathf.Max(.1f, (safeArea.rect.width - 24) / 620), Mathf.Max(.1f, (safeArea.rect.height - 24) / 624)));
        panel.localScale = Vector3.one * scale;
    }

    static RectTransform Fixed(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = GameplayHUDStyle.Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1));
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
    static TMP_Text Label(string name, Transform parent, string value, float size, float x, float y, float width, float height, bool center = false, bool bold = false)
    {
        var rect = Fixed(name, parent, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.color = GameplayHUDStyle.TextColor;
        text.alignment = center ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
    static Button MakeButton(string name, Transform parent, string caption, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
    {
        var rect = Fixed(name, parent, x, y, width, height);
        var image = GameplayHUDStyle.Surface(rect, Color.white, 10); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = GameplayHUDStyle.Card;
        colors.highlightedColor = new Color(.28f, .43f, .34f, 1);
        colors.pressedColor = new Color(.23f, .35f, .29f, 1);
        colors.disabledColor = new Color(.15f, .20f, .21f, .55f);
        button.colors = colors; GameplayHUDStyle.ButtonStates(button);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        if (!string.IsNullOrEmpty(caption) || name == "Confirm") Label("Label", rect, caption, 18, 8, 0, width - 16, height, true);
        return button;
    }
}
