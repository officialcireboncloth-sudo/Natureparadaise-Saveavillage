using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Display data supplied by the future gifting service; no eligibility rules are inferred here.</summary>
[Serializable]
public sealed class NPCGiftOption
{
    public ItemSO item;
    public int inventorySlot = -1;
    [Min(0)] public int availableCount;
    [Range(0, 5)] public int qualityStars;
    public bool selectable = true;
    public string unavailableReason;
}

/// <summary>A request to give one item. The receiving service must validate and apply it.</summary>
public sealed class NPCGiftSelection
{
    public DialogueSpeakerSO Recipient { get; }
    public ItemSO Item { get; }
    public int InventorySlot { get; }
    public int QualityStars { get; }
    public int Quantity => 1;
    internal NPCGiftSelection(DialogueSpeakerSO recipient, NPCGiftOption option)
    { Recipient = recipient; Item = option.item; InventorySlot = option.inventorySlot; QualityStars = option.qualityStars; }
}

[Serializable] public sealed class NPCGiftRequestedEvent : UnityEvent<NPCGiftSelection> { }

/// <summary>Authored gift-selection UI. Does not consume items, award relationship points or write saves.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-9500)]
public sealed class NPCGiftUI : MonoBehaviour
{
    public const string ResourcePath = "UI/NPCGiftUI";
    public static NPCGiftUI Active { get; private set; }
    public bool IsOpen => Active == this && ownsModal;
    public int SelectedIndex => selected;
    public NPCGiftOption SelectedOption => selected >= 0 && selected < options.Count ? options[selected] : null;

    [Serializable]
    public sealed class CardView
    {
        public Button button;
        public MainMenuRoundedImage surface;
        public Image artwork, icon;
        public TMP_Text name, count, quality;
        public Image[] stars;
    }

    [Header("Shared style and artwork")]
    [SerializeField] NPCGiftTheme theme;
    [SerializeField] Canvas canvas;
    [SerializeField] Image npcPortrait, panelArtwork, headerIcon, giveIcon, leftDecoration, rightDecoration;
    [Header("Gift cards")]
    [SerializeField] CardView[] cards;
    [SerializeField] Button previousButton, nextButton;
    [Header("Selected gift")]
    [SerializeField] Image detailIllustration;
    [SerializeField] ScrollRect descriptionScroll;
    [SerializeField] TMP_Text heading, weeklyCount, detailName, description, stockCount, confirmation, status, pageCount, emptyMessage;
    [SerializeField] Button giveButton, cancelButton;
    [SerializeField] Image giveButtonArtwork, cancelButtonArtwork;
    [Header("Future gifting function")]
    [Tooltip("Receives a request only. The connected service owns validation, item removal, limits and NPC response.")]
    [SerializeField] NPCGiftRequestedEvent onGiveRequested = new();
    [SerializeField] UnityEvent onClosed = new();
    public UnityEvent Closed => onClosed;

    readonly List<NPCGiftOption> options = new();
    DialogueSpeakerSO recipient;
    Action<NPCGiftSelection> submit;
    PlayerController player;
    TimeManager clock;
    Canvas mapCanvas;
    bool ownsModal, spawned, previousCursor, previousMapEnabled, awaitingResult;
    float previousTimeScale;
    CursorLockMode previousCursorLock;
    int selected = -1, given = -1, limit = -1, openedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActive() => Active = null;

    void Awake()
    {
        if (canvas != null) canvas.enabled = false;
        for (int i = 0; cards != null && i < cards.Length; i++)
        { int card = i; cards[i].button.onClick.AddListener(() => Select((selected < 0 ? 0 : selected / cards.Length) * cards.Length + card)); }
        previousButton.onClick.AddListener(() => MoveSelection(-1));
        nextButton.onClick.AddListener(() => MoveSelection(1));
        giveButton.onClick.AddListener(RequestGive);
        cancelButton.onClick.AddListener(Close);
    }

    /// <summary>Opens the reusable prefab with supplied view data and an optional submission callback.</summary>
    public static NPCGiftUI Show(DialogueSpeakerSO npc, IReadOnlyList<NPCGiftOption> gifts,
        Action<NPCGiftSelection> onGive = null, int giftsGiven = -1, int weeklyLimit = -1,
        PlayerController actor = null, Sprite portraitOverride = null)
    {
        if (Active != null || WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput) return null;
        var prefab = Resources.Load<NPCGiftUI>(ResourcePath);
        if (prefab == null) { Debug.LogError("Missing Resources/UI/NPCGiftUI prefab."); return null; }
        var ui = Instantiate(prefab); ui.spawned = true;
        if (!ui.Present(npc, gifts, onGive, giftsGiven, weeklyLimit, actor, portraitOverride))
        { Destroy(ui.gameObject); return null; }
        return ui;
    }

    /// <summary>Can also open a manually placed prefab. End an active conversation before opening this modal.</summary>
    public bool Present(DialogueSpeakerSO npc, IReadOnlyList<NPCGiftOption> gifts,
        Action<NPCGiftSelection> onGive = null, int giftsGiven = -1, int weeklyLimit = -1,
        PlayerController actor = null, Sprite portraitOverride = null)
    {
        if (!Application.isPlaying || ownsModal || Active != null || WorldInteractionPrompt.IsSuppressed ||
            GameplayPauseMenu.BlocksGameplayInput || canvas == null || cards == null || cards.Length == 0) return false;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        // A screen-space modal must not inherit an NPC's world scale or position.
        transform.SetParent(null, false); transform.localScale = Vector3.one;
        recipient = npc; submit = onGive; given = giftsGiven; limit = weeklyLimit;
        player = actor != null ? actor : FindFirstObjectByType<PlayerController>(); clock = TimeManager.Instance;
        previousTimeScale = Time.timeScale; previousCursor = Cursor.visible; previousCursorLock = Cursor.lockState;
        ownsModal = true; Active = this; openedFrame = Time.frameCount;
        player?.AcquireMovementLock(this); clock?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this);
        Time.timeScale = 0; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        if (EventSystem.current == null) new GameObject("EventSystem_NPCGift", typeof(EventSystem), typeof(StandaloneInputModule));
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(true);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(true);
        mapCanvas = FindFirstObjectByType<WorldMapUI>()?.GetComponent<Canvas>();
        if (mapCanvas != null) { previousMapEnabled = mapCanvas.enabled; mapCanvas.enabled = false; }
        Paint(npcPortrait, portraitOverride != null ? portraitOverride : npc != null ? npc.portrait : null);
        ApplyArtwork(); canvas.enabled = true; SetOptions(gifts);
        GameplayHUDStyle.ApplyHierarchy(gameObject); GameplayInput.ConsumeCurrentFrame();
        return true;
    }

    /// <summary>Refreshes view data after an inventory or eligibility change; preserves the selected source slot.</summary>
    public void SetOptions(IReadOnlyList<NPCGiftOption> gifts)
    {
        var old = SelectedOption; options.Clear();
        if (gifts != null) for (int i = 0; i < gifts.Count; i++)
        {
            var v = gifts[i]; if (v == null || v.item == null || v.availableCount <= 0) continue;
            options.Add(new NPCGiftOption { item = v.item, inventorySlot = v.inventorySlot,
                availableCount = v.availableCount, qualityStars = Mathf.Clamp(v.qualityStars, 0, 5),
                selectable = v.selectable, unavailableReason = v.unavailableReason });
        }
        selected = old == null ? -1 : options.FindIndex(v => v.item == old.item && v.inventorySlot == old.inventorySlot && v.qualityStars == old.qualityStars);
        if (selected < 0 && options.Count > 0) selected = 0;
        Refresh();
    }

    /// <summary>Updates a supplied weekly counter. Negative values leave the unknown limit undisplayed.</summary>
    public void SetWeeklyCount(int giftsGiven, int weeklyLimit) { given = giftsGiven; limit = weeklyLimit; Refresh(); }

    /// <summary>Selects a supplied gift entry; mouse and keyboard share this path.</summary>
    public void Select(int index)
    {
        if (awaitingResult || index < 0 || index >= options.Count) return;
        selected = index; status.text = ""; Refresh(); GameplayInput.ConsumeCurrentFrame();
    }
    public void MoveSelection(int delta)
    { if (options.Count > 0) Select(((Mathf.Max(0, selected) + delta) % options.Count + options.Count) % options.Count); }

    /// <summary>Emits one request and waits for the caller. It never applies a gift itself.</summary>
    public void RequestGive()
    {
        if (!IsOpen || !CanSubmit()) return;
        awaitingResult = true; Refresh(); GameplayInput.ConsumeCurrentFrame();
        var request = new NPCGiftSelection(recipient, SelectedOption);
        try { if (submit != null) submit(request); else onGiveRequested.Invoke(request); }
        catch (Exception e) { CompleteRequest(false); Debug.LogException(e); }
    }

    /// <summary>The future service acknowledges its result; rejection keeps selection and displays caller feedback.</summary>
    public void CompleteRequest(bool accepted, string feedback = "", bool closeOnSuccess = true)
    {
        if (!IsOpen) return;
        awaitingResult = false;
        if (accepted && closeOnSuccess) { Close(); return; }
        status.text = feedback ?? ""; Refresh();
    }

    bool AtLimit => given >= 0 && limit >= 0 && given >= limit;
    bool CanSubmit() => !awaitingResult && recipient != null && SelectedOption != null && SelectedOption.selectable &&
        !AtLimit && (submit != null || onGiveRequested.GetPersistentEventCount() > 0);

    void Refresh()
    {
        if (heading == null) return;
        string npcName = recipient != null ? recipient.displayName : "NPC";
        heading.text = "Berikan Hadiah untuk " + npcName;
        weeklyCount.text = given >= 0 && limit >= 0 ? $"Hadiah minggu ini  {given} / {limit}" : "";
        int page = selected < 0 ? 0 : selected / cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i]; int index = page * cards.Length + i;
            var v = index < options.Count ? options[index] : null;
            card.button.interactable = v != null && !awaitingResult;
            card.name.text = v != null ? v.item.itemName : ""; card.count.text = v != null ? "x" + v.availableCount : "";
            Paint(card.icon, v?.item.icon);
            bool focused = v != null && index == selected;
            card.surface.borderWidth = focused ? 2 : 1;
            card.surface.borderColor = focused ? GameplayHUDStyle.Accent : new Color(.7f, .77f, .7f, .15f);
            card.surface.SetVerticesDirty();
            card.name.color = v == null || v.selectable ? GameplayHUDStyle.TextColor : GameplayHUDStyle.Muted;
            int quality = v?.qualityStars ?? 0;
            card.quality.text = theme?.qualityStar == null && quality > 0 ? $"Kualitas {quality}/5" : "";
            for (int s = 0; s < card.stars.Length; s++) Paint(card.stars[s], s < quality ? theme?.qualityStar : null);
        }
        var choice = SelectedOption;
        detailName.text = choice != null ? choice.item.itemName : "Belum ada hadiah";
        description.text = choice != null ? choice.item.inventoryDescription : "Barang yang tersedia akan tampil di sini.";
        if (descriptionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            var viewport = descriptionScroll.viewport;
            description.rectTransform.sizeDelta = new Vector2(0, Mathf.Max(viewport.rect.height,
                description.GetPreferredValues(description.text, viewport.rect.width, 0).y));
            descriptionScroll.verticalNormalizedPosition = 1;
        }
        Paint(detailIllustration, choice != null ? choice.item.inventoryIllustration != null ? choice.item.inventoryIllustration : choice.item.icon : null);
        stockCount.text = choice != null ? $"Jumlah di Tas: {choice.availableCount}" : "";
        confirmation.text = AtLimit ? "Batas hadiah minggu ini sudah tercapai." : choice == null ? "Pilih hadiah untuk " + npcName + "." :
            !choice.selectable ? string.IsNullOrEmpty(choice.unavailableReason) ? "Hadiah ini belum dapat diberikan." : choice.unavailableReason : $"Berikan {choice.item.itemName} kepada {npcName}?";
        emptyMessage.gameObject.SetActive(options.Count == 0);
        giveButton.interactable = CanSubmit();
        previousButton.gameObject.SetActive(options.Count > cards.Length); nextButton.gameObject.SetActive(options.Count > cards.Length);
        previousButton.interactable = nextButton.interactable = !awaitingResult;
        pageCount.text = options.Count > cards.Length ? $"{page + 1} / {Mathf.CeilToInt(options.Count / (float)cards.Length)}" : "";
    }

    void ApplyArtwork()
    {
        if (theme == null) theme = Resources.Load<NPCGiftTheme>("UI/NPCGiftTheme");
        var dialogue = Resources.Load<DialogueTheme>("UI/DialogueTheme");
        Paint(panelArtwork, theme?.panel != null ? theme.panel : dialogue?.panel);
        Paint(headerIcon, theme?.headerIcon); Paint(giveIcon, theme?.giveIcon);
        Paint(leftDecoration, theme?.decoration); Paint(rightDecoration, theme?.decoration);
        Paint(giveButtonArtwork, theme?.button); Paint(cancelButtonArtwork, theme?.button);
        foreach (var card in cards) Paint(card.artwork, theme?.card);
    }
    static void Paint(Image image, Sprite sprite) { if (image == null) return; image.sprite = sprite; image.enabled = sprite != null; }

    void Update()
    {
        if (!IsOpen || Time.frameCount <= openedFrame || GameplayInput.ConsumedThisFrame) return;
#if UNITY_EDITOR
        var focus = UnityEditor.EditorWindow.focusedWindow; if (focus == null || focus.GetType().Name != "GameView") return;
#else
        if (!Application.isFocused) return;
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) RequestGive();
    }

    /// <summary>Releases only this modal's input, pause and cursor ownership.</summary>
    public void Close()
    {
        if (canvas != null) canvas.enabled = false;
        if (!ownsModal) return;
        ownsModal = false; if (Active == this) Active = null;
        player?.ReleaseMovementLock(this); clock?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
        Time.timeScale = previousTimeScale; Cursor.visible = previousCursor; Cursor.lockState = previousCursorLock;
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(false);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(false);
        if (mapCanvas != null) mapCanvas.enabled = previousMapEnabled;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        submit = null; awaitingResult = false; GameplayInput.ConsumeCurrentFrame(); onClosed.Invoke();
        if (spawned) { spawned = false; Destroy(gameObject); }
    }
    void OnDisable() => Close();
    void OnDestroy() => Close();
}
