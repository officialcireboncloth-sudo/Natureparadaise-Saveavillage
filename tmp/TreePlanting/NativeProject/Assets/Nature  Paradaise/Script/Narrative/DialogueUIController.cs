using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueUIController : MonoBehaviour
{
    // Preserve old serialized slots so authored scene presenters upgrade in place.
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text speakerNameText, bodyText, emotionText;
    [SerializeField] Image portraitImage;
    [SerializeField] Button continueButton;
    [SerializeField] RectTransform choicesRoot;
    [SerializeField] Button choiceButtonPrefab;
    [SerializeField] KeyCode continueKey = KeyCode.E;
    readonly List<Button> choicePool = new();
    readonly List<GameObject> choiceMarkers = new();
    DialogueService boundService;
    DialogueTheme theme;
    GameObject runtimeCanvas, playerFallback, npcFallback;
    CanvasScaler scaler;
    TMP_Text roleText, hintText;
    ScrollRect scroll, bodyScroll;
    RectTransform safeArea, playerFrame, npcFrame, bodyViewport;
    Image playerPortrait;
    DialogueSpeakerSO lastCounterpart;
    string playerEmotion, npcEmotion;
    int openedFrame, focusedChoice, layoutWidth, layoutHeight;
    Color Ink => theme != null ? theme.textColor : new(.19f, .16f, .12f, 1);
    Color Sage => theme != null ? theme.accentColor : new(.40f, .48f, .34f, 1);

    public static void EnsurePresenter()
    {
        var presenter = FindFirstObjectByType<DialogueUIController>();
        if (presenter == null) presenter = new GameObject("Dialogue Presenter").AddComponent<DialogueUIController>();
        presenter.Bind(); presenter.Build();
    }
    void OnEnable() => Bind();
    void Start() { Bind(); Build(); Refresh(); }
    void OnDisable()
    {
        var service = boundService; Unbind();
        if (service != null && service.IsOpen) service.EndConversation();
        if (runtimeCanvas != null) runtimeCanvas.SetActive(false);
    }
    void OnDestroy() { if (runtimeCanvas != null) Destroy(runtimeCanvas); }
    void Update()
    {
        if (boundService == null) Bind();
        if (boundService == null || !boundService.IsOpen) return;
        if (layoutWidth != Screen.width || layoutHeight != Screen.height) Refresh();
        if (Time.frameCount <= openedFrame) return;
#if UNITY_EDITOR
        var focus = UnityEditor.EditorWindow.focusedWindow;
        if (focus == null || focus.GetType().Name != "GameView") return;
#else
        if (!Application.isFocused) return;
#endif
        if (GameplayInput.ConsumedThisFrame) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) FocusChoice(focusedChoice - 1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) FocusChoice(focusedChoice + 1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
        else if (boundService.VisibleChoices.Count == 0 && (Input.GetKeyDown(continueKey) || Input.GetKeyDown(KeyCode.Space))) Continue();
    }
    void Bind()
    {
        var service = DialogueService.Instance;
        if (service == boundService) return;
        Unbind(); boundService = service;
        if (service == null) return;
        service.ConversationStarted += HandleStarted;
        service.NodeChanged += HandleNode;
        service.ConversationEnded += Refresh;
    }
    void Unbind()
    {
        if (boundService == null) return;
        boundService.ConversationStarted -= HandleStarted;
        boundService.NodeChanged -= HandleNode;
        boundService.ConversationEnded -= Refresh;
        boundService = null;
    }
    void HandleStarted()
    {
        openedFrame = Time.frameCount; focusedChoice = 0;
        playerEmotion = npcEmotion = "Neutral"; lastCounterpart = null;
        Refresh();
    }
    void HandleNode() { focusedChoice = 0; Refresh(); }
    public void Continue()
    {
        if (boundService == null || !boundService.IsOpen || boundService.VisibleChoices.Count != 0) return;
        boundService.Continue(); GameplayInput.ConsumeCurrentFrame();
    }
    public void Confirm()
    {
        if (boundService == null || !boundService.IsOpen) return;
        if (boundService.VisibleChoices.Count > 0) Choose(focusedChoice); else Continue();
    }
    public void Choose(int index) { boundService?.SelectChoice(index); GameplayInput.ConsumeCurrentFrame(); }
    public void Close() { boundService?.EndConversation(); GameplayInput.ConsumeCurrentFrame(); }
    public void FocusChoice(int index)
    {
        int count = boundService?.VisibleChoices.Count ?? 0;
        if (count == 0) return;
        focusedChoice = (index % count + count) % count;
        for (int i = 0; i < choiceMarkers.Count; i++) choiceMarkers[i].SetActive(i == focusedChoice);
        for (int i = 0; i < choicePool.Count; i++)
        {
            choicePool[i].targetGraphic.color = i == focusedChoice ? Sage : new Color(.88f, .88f, .79f, 1);
            choicePool[i].GetComponentInChildren<TMP_Text>().color = i == focusedChoice ? Color.white : Ink;
        }
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = count <= 1 || choicesRoot.rect.height <= scroll.viewport.rect.height ? 1 : 1 - (float)focusedChoice / (count - 1);
    }
    void Build()
    {
        if (runtimeCanvas != null) return;
        choicePool.Clear(); choiceMarkers.Clear();
        if (panelRoot != null) panelRoot.SetActive(false);
        if (emotionText != null) emotionText.gameObject.SetActive(false);
        theme = Resources.Load<DialogueTheme>("UI/DialogueTheme");
        runtimeCanvas = new GameObject("Dialogue Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var ownerScene = gameObject.scene;
        if (ownerScene.IsValid() && ownerScene.isLoaded && runtimeCanvas.scene != ownerScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(runtimeCanvas, ownerScene);
        var canvas = runtimeCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 470; canvas.overrideSorting = true;
        scaler = runtimeCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null) new GameObject("EventSystem_Dialogue", typeof(EventSystem), typeof(StandaloneInputModule));
        var backdrop = Rect("Dialogue Backdrop", runtimeCanvas.transform, 0, 0, 1, 1).gameObject.AddComponent<Image>();
        backdrop.color = new(.025f, .04f, .025f, .20f); backdrop.raycastTarget = true;
        safeArea = Rect("Safe Area", runtimeCanvas.transform, 0, 0, 1, 1);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();
        playerPortrait = Portrait("Player Portrait", out playerFrame, out playerFallback);
        portraitImage = Portrait("NPC Portrait", out npcFrame, out npcFallback);
        panelRoot = Rect("Dialogue Panel", safeArea, .035f, .035f, .965f, .34f).gameObject;
        if (theme != null && theme.panel != null)
        {
            var art = panelRoot.AddComponent<Image>(); art.sprite = theme.panel; art.type = Image.Type.Sliced;
        }
        else
        {
            var art = GameplayHUDStyle.Surface((RectTransform)panelRoot.transform, theme != null ? theme.panelColor : new(.97f, .94f, .86f, 1), 24);
            art.borderWidth = 3; art.borderColor = new(.55f, .60f, .45f, 1); art.raycastTarget = true;
        }
        var shadow = panelRoot.AddComponent<Shadow>(); shadow.effectColor = new(0, 0, 0, .18f); shadow.effectDistance = new(0, -4);
        var tab = Rect("Speaker Name Tab", panelRoot.transform, .035f, .89f, .31f, 1.09f);
        GameplayHUDStyle.Surface(tab, Sage, 24);
        GameplayHUDStyle.Icon(tab, MainMenuIcon.Kind.Sprout, new(.94f, .94f, .84f, 1), new(.04f, .2f), new(.17f, .8f));
        speakerNameText = Text(tab, "", 30, .21f, .08f, .95f, .92f); speakerNameText.color = Color.white; speakerNameText.fontStyle = FontStyles.Bold;
        roleText = Text(panelRoot.transform, "", 18, .35f, .88f, .96f, 1); roleText.alignment = TextAlignmentOptions.MidlineRight;
        bodyViewport = Rect("Dialogue Text Viewport", panelRoot.transform, .04f, .28f, .96f, .81f);
        bodyViewport.gameObject.AddComponent<RectMask2D>(); bodyViewport.gameObject.AddComponent<Image>().color = Color.clear;
        bodyScroll = bodyViewport.gameObject.AddComponent<ScrollRect>();
        bodyScroll.horizontal = false; bodyScroll.viewport = bodyViewport; bodyScroll.movementType = ScrollRect.MovementType.Clamped;
        bodyText = Text(bodyViewport, "", 28, 0, 1, 1, 1); bodyText.rectTransform.pivot = new(.5f, 1); bodyScroll.content = bodyText.rectTransform;
        bodyText.enableAutoSizing = false; bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.textWrappingMode = TextWrappingModes.Normal; bodyText.overflowMode = TextOverflowModes.Overflow; bodyText.lineSpacing = 7;
        var viewport = Rect("Choices Viewport", panelRoot.transform, .66f, .27f, .965f, .82f);
        viewport.gameObject.AddComponent<RectMask2D>(); viewport.gameObject.AddComponent<Image>().color = Color.clear;
        scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = viewport; scroll.movementType = ScrollRect.MovementType.Clamped;
        choicesRoot = Rect("Choices", viewport, 0, 1, 1, 1); choicesRoot.pivot = new(.5f, 1); scroll.content = choicesRoot;
        continueButton = Button(panelRoot.transform, "Lanjut  >", .79f, .035f, .965f, .22f, Continue);
        hintText = Text(panelRoot.transform, "", 18, .04f, .055f, .77f, .19f);
        hintText.color = new(.37f, .41f, .31f, 1);
        Button(safeArea, "Tutup", .88f, .92f, .97f, .985f, Close);
        Layout(); runtimeCanvas.SetActive(false);
    }
    Image Portrait(string name, out RectTransform frame, out GameObject fallback)
    {
        frame = Rect(name + " Frame", safeArea, 0, 0, 1, 1);
        var imageRect = Rect(name, frame, 0, 0, 1, 1);
        imageRect.pivot = new(.5f, 0); imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
        var image = imageRect.gameObject.AddComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
        fallback = Rect(name + " Fallback", frame, .2f, .05f, .8f, .85f).gameObject;
        GameplayHUDStyle.Icon(fallback.transform, MainMenuIcon.Kind.People, new(.56f, .61f, .48f, 1), Vector2.zero, Vector2.one);
        return image;
    }
    void Layout()
    {
        layoutWidth = Screen.width; layoutHeight = Screen.height;
        bool narrow = Screen.height > Screen.width;
        scaler.referenceResolution = Mathf.Min(Screen.width, Screen.height) <= 800 ? new(1280, 720) : new(1920, 1080);
        float top = narrow ? .40f : .34f;
        SetAnchors((RectTransform)panelRoot.transform, .035f, .035f, .965f, top);
        SetAnchors(playerFrame, .035f, top - .012f, .46f, .92f);
        SetAnchors(npcFrame, .54f, top - .012f, .965f, .92f);
    }
    void Refresh()
    {
        Build();
        bool open = boundService != null && boundService.IsOpen;
        runtimeCanvas.SetActive(open);
        foreach (var button in choicePool) if (button != null) button.gameObject.SetActive(false);
        if (!open) return;
        Layout();
        var node = boundService.CurrentNode;
        var speaker = boundService.CurrentSpeaker;
        var npc = boundService.CounterpartSpeaker;
        bool playerSpeaking = boundService.IsPlayerSpeaking;
        if (npc != lastCounterpart) { npcEmotion = "Neutral"; lastCounterpart = npc; }
        if (playerSpeaking) playerEmotion = node.emotion; else npcEmotion = node.emotion;
        PaintPortrait(playerPortrait, playerFallback, boundService.PlayerSpeaker, playerEmotion, playerSpeaking);
        PaintPortrait(portraitImage, npcFallback, npc, npcEmotion, !playerSpeaking);
        npcFrame.gameObject.SetActive(npc != null);
        speakerNameText.text = speaker != null ? speaker.displayName : "";
        roleText.text = speaker != null ? speaker.roleDescription : "";
        bodyText.text = node.text;
        int count = boundService.VisibleChoices.Count;
        continueButton.gameObject.SetActive(count == 0); choicesRoot.parent.gameObject.SetActive(count > 0);
        bodyViewport.anchorMin = new(.04f, .28f); bodyViewport.anchorMax = new(count > 0 ? .63f : .96f, .81f);
        hintText.text = Application.isMobilePlatform ? (count > 0 ? "Pilih jawaban" : "Ketuk Lanjut untuk meneruskan")
            : (count > 0 ? "W / S  Pilih    Enter  Konfirmasi    Esc  Tutup" : "E / Enter  Lanjut    Esc  Tutup");
        Canvas.ForceUpdateCanvases();
        float rowHeight = Mathf.Min(76, scroll.viewport.rect.height / Mathf.Max(1, Mathf.Min(count, 2)));
        choicesRoot.sizeDelta = new(0, count * rowHeight);
        for (int i = 0; i < count; i++)
        {
            if (i >= choicePool.Count)
            {
                int index = i;
                var button = Button(choicesRoot, "", 0, 1, 1, 1, () => Choose(index));
                var rect = (RectTransform)button.transform;
                Text(rect, "", 22, .08f, .06f, .95f, .94f);
                var marker = Rect("Focused Answer", rect, .025f, .20f, .033f, .80f).gameObject;
                var image = marker.AddComponent<Image>(); image.color = Color.white; image.raycastTarget = false;
                choicePool.Add(button); choiceMarkers.Add(marker);
            }
            choicePool[i].GetComponentInChildren<TMP_Text>().text = boundService.VisibleChoices[i].text;
            var choiceRect = (RectTransform)choicePool[i].transform;
            choiceRect.offsetMin = new(0, -(i + 1) * rowHeight + 5); choiceRect.offsetMax = new(0, -i * rowHeight - 5);
            choicePool[i].gameObject.SetActive(true);
        }
        RefreshBody(); FocusChoice(focusedChoice);
    }
    void PaintPortrait(Image image, GameObject fallback, DialogueSpeakerSO speaker, string emotion, bool active)
    {
        image.sprite = speaker != null ? speaker.GetPortrait(emotion) : null;
        if (image.sprite == null && theme != null) image.sprite = theme.missingPortrait;
        image.enabled = image.sprite != null; fallback.SetActive(!image.enabled);
        float brightness = active ? 1 : (theme != null ? theme.listenerBrightness : .68f);
        image.color = new(brightness, brightness, brightness, 1);
    }
    void RefreshBody()
    {
        if (bodyText == null || boundService == null || !boundService.IsOpen) return;
        Canvas.ForceUpdateCanvases();
        bodyText.rectTransform.sizeDelta = new(0, Mathf.Max(bodyViewport.rect.height, bodyText.GetPreferredValues(bodyText.text, bodyViewport.rect.width, 0).y));
        bodyScroll.verticalNormalizedPosition = 1;
    }
    static void SetAnchors(RectTransform rect, float x1, float y1, float x2, float y2)
    {
        rect.anchorMin = new(x1, y1); rect.anchorMax = new(x2, y2); rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static RectTransform Rect(string name, Transform parent, float x1, float y1, float x2, float y2) => GameplayHUDStyle.Rect(name, parent, new(x1, y1), new(x2, y2));
    TMP_Text Text(Transform parent, string value, float size, float x1, float y1, float x2, float y2)
    {
        var text = GameplayHUDStyle.Text("Text", parent, value, size, new(x1, y1), new(x2, y2)); text.color = Ink; return text;
    }
    Button Button(Transform parent, string label, float x1, float y1, float x2, float y2, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect("Dialogue Button", parent, x1, y1, x2, y2);
        var graphic = GameplayHUDStyle.Surface(rect, Sage, 14); graphic.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
        var colors = button.colors; colors.normalColor = Color.white; colors.selectedColor = Color.white;
        colors.highlightedColor = new(1.1f, 1.1f, 1.1f, 1); colors.pressedColor = new(.82f, .82f, .82f, 1); colors.fadeDuration = .12f; button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        rect.gameObject.AddComponent<MainMenuButtonAudio>(); button.onClick.AddListener(action);
        if (!string.IsNullOrEmpty(label)) { var text = Text(rect, label, 25, .06f, .05f, .95f, .95f); text.color = Color.white; text.alignment = TextAlignmentOptions.Center; }
        return button;
    }
}


