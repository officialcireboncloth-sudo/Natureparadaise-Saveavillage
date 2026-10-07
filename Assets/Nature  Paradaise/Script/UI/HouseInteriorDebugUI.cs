using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>House-only runtime layout selector. Features follow the selected level; no persistent house upgrade.</summary>
[DisallowMultipleComponent]
public sealed class HouseInteriorDebugUI : MonoBehaviour
{
    HouseInteriorController house;
    GameObject canvasObject,panel;
    TMP_Text label;
    readonly Button[] levelButtons=new Button[5];
    bool wasInside;
    void Awake()=>house=GetComponent<HouseInteriorController>();
    void Start()=>Build();
    void Update()
    {
        if(house==null)return;
        if(canvasObject==null)Build();
        bool inside=house.IsPlayerInside;
        if(wasInside&&!inside)house.ClearDebugLayout();wasInside=inside;
        canvasObject.SetActive(inside);panel.SetActive(inside);if(!inside)return;
        label.text=$"DEBUG INTERIOR • Lv.{house.ActiveLayoutLevel}";
        bool available=!WorldInteractionPrompt.IsSuppressed&&!GameplayPauseMenu.BlocksGameplayInput&&!(SceneTransitionManager.Instance?.IsTransitioning??false);
        for(int i=0;i<levelButtons.Length;i++)levelButtons[i].interactable=available&&i<house.AvailableLayoutCount;
    }
    public bool SelectLevel(int level)=>house!=null&&house.SetDebugLayout(level);
    void OnDisable(){if(canvasObject!=null)canvasObject.SetActive(false);}
    void OnEnable(){if(canvasObject!=null)canvasObject.SetActive(house!=null&&house.IsPlayerInside);}
    void OnDestroy(){if(canvasObject!=null)Destroy(canvasObject);}
    void Build()
    {
        if(house==null||canvasObject!=null)return;
        canvasObject=new GameObject("House Interior Debug Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        if(canvasObject.scene!=gameObject.scene)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject,gameObject.scene);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=440;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        if(EventSystem.current==null)new GameObject("EventSystem_HouseDebug",typeof(EventSystem),typeof(StandaloneInputModule));
        var safe=GameplayHUDStyle.Rect("Safe Area",canvasObject.transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        var rect=GameplayHUDStyle.Rect("House Interior Debug Menu",safe,new(.015f,.70f),new(.235f,.865f));panel=rect.gameObject;
        GameplayHUDStyle.Surface(rect,new(.12f,.22f,.28f,.94f),12).raycastTarget=true;
        label=GameplayHUDStyle.Text("Level",rect,"DEBUG INTERIOR",22,new(.04f,.72f),new(.96f,.95f));
        for(int i=0;i<5;i++)
        {
            int level=i+1;float x=.04f+i*.187f;levelButtons[i]=Button(rect,"Lv."+level,new(x,.35f),new(x+.17f,.68f),()=>SelectLevel(level));
        }
        Button(rect,"Kembali ke level asli",new(.04f,.07f),new(.96f,.29f),()=>SelectLevel(0));
        panel.SetActive(house.IsPlayerInside);canvasObject.SetActive(house.IsPlayerInside);
    }
    static Button Button(Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var rect=GameplayHUDStyle.Rect(title,parent,min,max);var graphic=GameplayHUDStyle.Surface(rect,Color.white,8);graphic.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;var colors=button.colors;
        colors.normalColor=new(.25f,.32f,.36f,.95f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.32f,.53f,.37f);colors.pressedColor=new(.23f,.40f,.28f);colors.fadeDuration=0;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};
        rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        var text=GameplayHUDStyle.Text("Label",rect,title,18,new(.03f,.05f),new(.97f,.95f));text.alignment=TextAlignmentOptions.Midline;return button;
    }
}
