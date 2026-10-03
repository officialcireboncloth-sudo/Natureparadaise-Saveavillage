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
        scroll.verticalNormalizedPosition=count<=3?1:1-(float)focusedChoice/(count-1);
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
        panelRoot=Rect("Dialogue Panel",safe,.012f,.03f,.988f,.315f).gameObject;
        if(theme?.panel!=null){var art=panelRoot.AddComponent<Image>();art.sprite=theme.panel;art.type=Image.Type.Sliced;}
        else GameplayHUDStyle.Surface((RectTransform)panelRoot.transform,new(.16f,.23f,.28f,.94f),16).raycastTarget=true;
        portraitImage=Rect("NPC Portrait Image Slot",panelRoot.transform,.01f,.0f,.17f,1.20f).gameObject.AddComponent<Image>();portraitImage.preserveAspect=true;portraitImage.raycastTarget=false;
        speakerNameText=Text(panelRoot.transform,"",30,.18f,.80f,.66f,.94f);speakerNameText.fontStyle=FontStyles.Bold;
        roleText=Text(panelRoot.transform,"",19,.18f,.69f,.66f,.81f);roleText.color=new(.74f,.81f,.85f);
        var relation=Rect("Relationship Image Slot",panelRoot.transform,.18f,.61f,.28f,.69f).gameObject.AddComponent<Image>();relation.sprite=theme?.relationshipIcon;relation.enabled=relation.sprite!=null;relation.preserveAspect=true;relation.raycastTarget=false;
        bodyText=Text(panelRoot.transform,"",27,.18f,.13f,.665f,.59f);bodyText.alignment=TextAlignmentOptions.TopLeft;bodyText.textWrappingMode=TextWrappingModes.Normal;
        emotionText=Text(panelRoot.transform,"",16,.18f,.025f,.64f,.12f);emotionText.fontStyle=FontStyles.Italic;
        var viewport=Rect("Choices Viewport",panelRoot.transform,.69f,.27f,.975f,.90f);viewport.gameObject.AddComponent<RectMask2D>();viewport.gameObject.AddComponent<Image>().color=new(0,0,0,.01f);
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
        choicesRoot=Rect("Choices",viewport,0,1,1,1);choicesRoot.pivot=new(.5f,1);scroll.content=choicesRoot;
        continueButton=Button(panelRoot.transform,"Lanjutkan",.69f,.44f,.975f,.64f,Continue);
        hintText=Text(panelRoot.transform,"",18,.655f,.045f,.98f,.18f);hintText.alignment=TextAlignmentOptions.MidlineRight;
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
        hintText.text=count>0?"W / S  Pilih jawaban    Enter  Konfirmasi    Esc  Akhiri":"Enter / E  Lanjutkan    Esc  Akhiri percakapan";
        choicesRoot.sizeDelta=new(0,count*62);
        for(int i=0;i<count;i++)
        {
            if(i>=choicePool.Count)
            {
                int index=i;var button=Button(choicesRoot,"",0,1,1,1,()=>Choose(index));var rect=(RectTransform)button.transform;rect.offsetMin=new(0,-(i+1)*62+5);rect.offsetMax=new(0,-i*62-5);
                Text(rect,"",21,.08f,.08f,.975f,.92f);var marker=Rect("Focused Answer",rect,.025f,.20f,.033f,.80f).gameObject;var image=marker.AddComponent<Image>();image.color=new(.63f,.84f,.90f);image.raycastTarget=false;
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
        var rect=Rect("Answer Button",parent,x1,y1,x2,y2);var graphic=GameplayHUDStyle.Surface(rect,Color.white,10);graphic.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
        var colors=button.colors;colors.normalColor=new(.24f,.31f,.36f,.90f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.32f,.53f,.37f,.94f);colors.pressedColor=new(.23f,.40f,.28f);colors.fadeDuration=0;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        if(!string.IsNullOrEmpty(label))Text(rect,label,24,.06f,.05f,.95f,.95f);return button;
    }
}


