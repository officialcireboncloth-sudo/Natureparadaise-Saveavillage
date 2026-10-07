using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DialogueUIController : MonoBehaviour
{
    // Keep serialized fields compatible with existing authored scenes/setup tools.
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text speakerNameText,bodyText,emotionText;
    [SerializeField] Image portraitImage;
    [SerializeField] Button continueButton;
    [SerializeField] RectTransform choicesRoot;
    [SerializeField] Button choiceButtonPrefab;
    [SerializeField] KeyCode continueKey=KeyCode.E;
    readonly List<Button> choicePool=new();
    readonly List<GameObject> choiceMarkers=new();
    DialogueService boundService;
    DialogueTheme theme;
    GameObject runtimeCanvas;
    TMP_Text roleText,hintText;
    ScrollRect scroll;
    RectTransform portraitFrame,bodyViewport;
    ScrollRect bodyScroll;
    TMP_Text confirmKeyText;
    int openedFrame,focusedChoice;
    public static void EnsurePresenter()
    {
        var presenter=FindFirstObjectByType<DialogueUIController>();
        if(presenter==null)presenter=new GameObject("Dialogue Presenter").AddComponent<DialogueUIController>();
        presenter.Bind();presenter.Build();
    }
    void OnEnable()=>Bind();
    void Start(){Bind();Build();Refresh();}
    void OnDisable(){var service=boundService;Unbind();if(service!=null&&service.IsOpen)service.EndConversation();if(runtimeCanvas!=null)runtimeCanvas.SetActive(false);}
    void OnDestroy(){if(runtimeCanvas!=null)Destroy(runtimeCanvas);}
    void Update()
    {
        if(boundService==null)Bind();
        if(boundService==null||!boundService.IsOpen||Time.frameCount<=openedFrame)return;
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){boundService.EndConversation();GameplayInput.ConsumeCurrentFrame();return;}
        if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))FocusChoice(focusedChoice-1);
        if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))FocusChoice(focusedChoice+1);
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Confirm();
        else if(boundService.VisibleChoices.Count==0&&(Input.GetKeyDown(continueKey)||Input.GetKeyDown(KeyCode.Space)))Continue();
    }
    void Bind()
    {
        var service=DialogueService.Instance;if(service==boundService)return;Unbind();boundService=service;if(service==null)return;
        service.ConversationStarted+=HandleStarted;service.NodeChanged+=HandleNode;service.ConversationEnded+=Refresh;
    }
    void Unbind(){if(boundService==null)return;boundService.ConversationStarted-=HandleStarted;boundService.NodeChanged-=HandleNode;boundService.ConversationEnded-=Refresh;boundService=null;}
    void HandleStarted(){openedFrame=Time.frameCount;focusedChoice=0;Refresh();}
    void HandleNode(){focusedChoice=0;Refresh();}
    public void Continue(){if(boundService!=null&&boundService.IsOpen&&boundService.VisibleChoices.Count==0){boundService.Continue();GameplayInput.ConsumeCurrentFrame();}}
    public void Confirm(){if(boundService==null||!boundService.IsOpen)return;if(boundService.VisibleChoices.Count>0)Choose(focusedChoice);else Continue();}
    public void Choose(int index){boundService?.SelectChoice(index);GameplayInput.ConsumeCurrentFrame();}
    public void FocusChoice(int index)
    {
        int count=boundService?.VisibleChoices.Count??0;if(count==0)return;focusedChoice=(index%count+count)%count;
        for(int i=0;i<choiceMarkers.Count;i++)choiceMarkers[i].SetActive(i==focusedChoice);
        for(int i=0;i<choicePool.Count;i++)
        {
            var surface=choicePool[i].targetGraphic as MainMenuRoundedImage;
            if(surface!=null)surface.color=i==focusedChoice?new Color(.34f,.48f,.35f,1):GameplayHUDStyle.Card;
        }
        scroll.verticalNormalizedPosition=count<=1||choicesRoot.rect.height<=scroll.viewport.rect.height?1:1-(float)focusedChoice/(count-1);
    }
    void Build()
    {
        if(runtimeCanvas!=null){runtimeCanvas.SetActive(true);return;}
        choicePool.Clear();choiceMarkers.Clear();
        if(panelRoot!=null)panelRoot.SetActive(false);
        theme=Resources.Load<DialogueTheme>("UI/DialogueTheme");
        runtimeCanvas=new GameObject("Dialogue Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        // A nested Canvas inherits its parent layout instead of sizing itself to the screen.
        // Keep the presenter lifecycle on this component, but render from a scene-owned root.
        var ownerScene=gameObject.scene;
        if(ownerScene.IsValid()&&ownerScene.isLoaded&&runtimeCanvas.scene!=ownerScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(runtimeCanvas,ownerScene);
        var canvas=runtimeCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=470;canvas.overrideSorting=true;
        var scaler=runtimeCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        if(EventSystem.current==null)new GameObject("EventSystem_Dialogue",typeof(EventSystem),typeof(StandaloneInputModule));
        var safe=Rect("Safe Area",runtimeCanvas.transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        panelRoot=Rect("Dialogue Panel",safe,.09f,.035f,.91f,.34f).gameObject;
        if(theme?.panel!=null){var art=panelRoot.AddComponent<Image>();art.sprite=theme.panel;art.type=Image.Type.Sliced;}
        else GameplayHUDStyle.Surface((RectTransform)panelRoot.transform,GameplayHUDStyle.Modal,16).raycastTarget=true;
        var shadow=panelRoot.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.22f);shadow.effectDistance=new Vector2(0,-5);
        GameplayHUDStyle.Surface(Rect("Header Accent",panelRoot.transform,.025f,.84f,.029f,.94f),GameplayHUDStyle.Accent,2);
        portraitFrame=Rect("Portrait Frame",panelRoot.transform,.025f,.29f,.155f,.78f);
        GameplayHUDStyle.Surface(portraitFrame,GameplayHUDStyle.Card,12);
        portraitImage=Rect("NPC Portrait Image Slot",portraitFrame,.04f,.04f,.96f,.96f).gameObject.AddComponent<Image>();portraitImage.preserveAspect=true;portraitImage.raycastTarget=false;
        speakerNameText=Text(panelRoot.transform,"",30,.042f,.80f,.43f,.965f);speakerNameText.fontStyle=FontStyles.Bold;
        roleText=Text(panelRoot.transform,"",18,.042f,.735f,.63f,.815f);roleText.color=GameplayHUDStyle.Muted;
        var relation=Rect("Relationship Image Slot",panelRoot.transform,.80f,.855f,.83f,.93f).gameObject.AddComponent<Image>();relation.sprite=theme?.relationshipIcon;relation.enabled=relation.sprite!=null;relation.preserveAspect=true;relation.raycastTarget=false;
        emotionText=Text(panelRoot.transform,"",18,.84f,.84f,.975f,.95f);emotionText.alignment=TextAlignmentOptions.MidlineRight;emotionText.color=GameplayHUDStyle.Accent;
        GameplayHUDStyle.Surface(Rect("Header Divider",panelRoot.transform,.025f,.715f,.975f,.718f),new Color(.7f,.77f,.7f,.12f),0);
        bodyViewport=Rect("Dialogue Text Viewport",panelRoot.transform,.04f,.29f,.96f,.66f);bodyViewport.gameObject.AddComponent<RectMask2D>();
        bodyViewport.gameObject.AddComponent<Image>().color=Color.clear;
        bodyScroll=bodyViewport.gameObject.AddComponent<ScrollRect>();bodyScroll.horizontal=false;bodyScroll.viewport=bodyViewport;bodyScroll.movementType=ScrollRect.MovementType.Clamped;
        bodyText=Text(bodyViewport,"",26,0,1,1,1);bodyText.rectTransform.pivot=new(.5f,1);bodyScroll.content=bodyText.rectTransform;
        bodyText.enableAutoSizing=false;bodyText.alignment=TextAlignmentOptions.TopLeft;bodyText.textWrappingMode=TextWrappingModes.Normal;bodyText.overflowMode=TextOverflowModes.Overflow;bodyText.lineSpacing=8;
        var viewport=Rect("Choices Viewport",panelRoot.transform,.67f,.28f,.975f,.67f);viewport.gameObject.AddComponent<RectMask2D>();viewport.gameObject.AddComponent<Image>().color=new(0,0,0,.01f);
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
        choicesRoot=Rect("Choices",viewport,0,1,1,1);choicesRoot.pivot=new(.5f,1);scroll.content=choicesRoot;
        var track=Rect("Answers Scrollbar",viewport,.985f,.04f,.997f,.96f);
        GameplayHUDStyle.Surface(track,new Color(.7f,.77f,.7f,.10f),3);
        var handle=Rect("Handle",track,0,0,1,1);var handleGraphic=GameplayHUDStyle.Surface(handle,GameplayHUDStyle.Accent,3);handleGraphic.raycastTarget=true;
        var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handleGraphic;scrollbar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        GameplayHUDStyle.Surface(Rect("Footer Divider",panelRoot.transform,.025f,.225f,.975f,.228f),new Color(.7f,.77f,.7f,.12f),0);
        continueButton=Button(panelRoot.transform,"Lanjutkan  ›",.79f,.045f,.975f,.185f,Continue);
        continueButton.targetGraphic.color=new Color(.27f,.39f,.28f,1);
        hintText=Text(panelRoot.transform,"",18,.10f,.065f,.77f,.16f);hintText.color=GameplayHUDStyle.Muted;
        var key=Rect("Confirm Key",panelRoot.transform,.025f,.065f,.09f,.165f);GameplayHUDStyle.Surface(key,GameplayHUDStyle.Card,5);
        confirmKeyText=Text(key,"E",17,0,0,1,1);confirmKeyText.alignment=TextAlignmentOptions.Center;confirmKeyText.fontStyle=FontStyles.Bold;
        panelRoot.SetActive(false);
    }
    void Refresh()
    {
        Build();bool open=boundService!=null&&boundService.IsOpen;panelRoot.SetActive(open);
        foreach(var button in choicePool)if(button!=null)button.gameObject.SetActive(false);if(!open)return;
        var node=boundService.CurrentNode;var speaker=boundService.CurrentSpeaker;
        speakerNameText.text=speaker!=null?speaker.displayName:"";roleText.text=speaker!=null?speaker.roleDescription:"";
        bodyText.text=node.text;emotionText.text=node.emotion;portraitImage.sprite=speaker!=null?speaker.portrait:null;portraitImage.enabled=portraitImage.sprite!=null;
        int count=boundService.VisibleChoices.Count;continueButton.gameObject.SetActive(count==0);choicesRoot.parent.gameObject.SetActive(count>0);
        portraitFrame.gameObject.SetActive(portraitImage.enabled);
        bodyViewport.anchorMin=new(portraitImage.enabled?.18f:.04f,.29f);bodyViewport.anchorMax=new(count>0?.635f:.96f,.66f);
        Canvas.ForceUpdateCanvases();
        bodyText.rectTransform.sizeDelta=new(0,Mathf.Max(bodyViewport.rect.height,bodyText.GetPreferredValues(node.text,bodyViewport.rect.width,0).y));
        bodyScroll.verticalNormalizedPosition=1;
        confirmKeyText.text=count>0?"W / S":"E / Enter";
        hintText.text=count>0?$"{count} jawaban     Enter  Konfirmasi     Esc  Tutup":"Lanjutkan percakapan     ·     Esc  Tutup";
        choicesRoot.sizeDelta=new(0,count*62);
        for(int i=0;i<count;i++)
        {
            if(i>=choicePool.Count)
            {
                int index=i;var button=Button(choicesRoot,"",0,1,1,1,()=>Choose(index));var rect=(RectTransform)button.transform;rect.offsetMin=new(0,-(i+1)*62+5);rect.offsetMax=new(0,-i*62-5);
                Text(rect,"",21,.08f,.08f,.95f,.92f);var marker=Rect("Focused Answer",rect,.025f,.20f,.033f,.80f).gameObject;var image=marker.AddComponent<Image>();image.color=GameplayHUDStyle.Accent;image.raycastTarget=false;
                choicePool.Add(button);choiceMarkers.Add(marker);
            }
            choicePool[i].GetComponentInChildren<TMP_Text>().text=boundService.VisibleChoices[i].text;choicePool[i].gameObject.SetActive(true);
        }
        FocusChoice(focusedChoice);
    }
    static RectTransform Rect(string name,Transform parent,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Rect(name,parent,new(x1,y1),new(x2,y2));
    static TMP_Text Text(Transform parent,string value,float size,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Text("Text",parent,value,size,new(x1,y1),new(x2,y2));
    static Button Button(Transform parent,string label,float x1,float y1,float x2,float y2,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect("Answer Button",parent,x1,y1,x2,y2);var graphic=GameplayHUDStyle.Surface(rect,GameplayHUDStyle.Card,10);graphic.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
        var colors=button.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;colors.highlightedColor=new(1.12f,1.12f,1.12f,1);colors.pressedColor=new(.82f,.82f,.82f,1);colors.fadeDuration=.12f;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        if(!string.IsNullOrEmpty(label)){var text=Text(rect,label,23,.06f,.05f,.95f,.95f);text.alignment=TextAlignmentOptions.Center;}return button;
    }
}


