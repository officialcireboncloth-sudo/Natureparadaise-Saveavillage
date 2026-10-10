using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared modal lifecycle and authored UI controls for calendar/cafe.</summary>
public abstract class DataDrivenModal : MonoBehaviour
{
    public GameObject uiRoot;
    public float interactionRadius=2.5f;
    public static DataDrivenModal Active { get; private set; }
    protected PlayerController player;
    protected TimeManager clock;
    bool oldCursor;CursorLockMode oldLock;
    Canvas mapCanvas;bool mapWasVisible;
    public bool IsOpen=>Active==this;
    protected abstract string Prompt {get;}
    protected virtual void Start(){PrepareRuntimeCanvas();if(uiRoot!=null&&!IsOpen)uiRoot.SetActive(false);}
    void PrepareRuntimeCanvas(){if(Application.isPlaying&&uiRoot!=null&&uiRoot.transform.parent!=null){uiRoot.transform.SetParent(null,false);uiRoot.transform.localRotation=Quaternion.identity;uiRoot.transform.localScale=Vector3.one;}}
    protected virtual void Update()
    {
        if(IsOpen){if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E))Close();return;}
        if(Active!=null||ShopFront.IsAnyOpen||UpgradeShopFront.Active!=null||MarketStand.IsAnyOpen||WorldInteractionPrompt.IsSuppressed||GameplayPauseMenu.BlocksGameplayInput)return;
        player??=FindFirstObjectByType<PlayerController>();
        if(player==null||!PlayerInteractionTarget.ContainsPickup(player.transform,transform,interactionRadius))return;
        WorldInteractionPrompt.Request(this,transform,Prompt,Vector3.Distance(player.transform.position,transform.position),1.5f);
        if(PlayerInteractionTarget.PressPickup(player.transform,transform,KeyCode.E,interactionRadius))Open();
    }
    [ContextMenu("Open For Testing")]public void Open()
    {
        if(!Application.isPlaying){Preview();return;}
        if(Active!=null||ShopFront.IsAnyOpen||UpgradeShopFront.Active!=null||MarketStand.IsAnyOpen||GameplayPauseMenu.BlocksGameplayInput||WorldInteractionPrompt.IsSuppressed)return;
        if(uiRoot==null)Build();PrepareRuntimeCanvas();BindControls();Active=this;
        player??=FindFirstObjectByType<PlayerController>();clock=TimeManager.Instance;
        player?.AcquireMovementLock(this);clock?.AcquirePause(this);WorldInteractionPrompt.AcquireSuppression(this);
        oldCursor=Cursor.visible;oldLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        uiRoot.SetActive(true);OnOpen();Refresh();GameplayInput.ConsumeCurrentFrame();
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(true);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(true);
        mapCanvas=FindFirstObjectByType<WorldMapUI>()?.GetComponent<Canvas>();if(mapCanvas!=null){mapWasVisible=mapCanvas.enabled;mapCanvas.enabled=false;}
    }
    public void Close()
    {
        if(uiRoot!=null)uiRoot.SetActive(false);if(Active!=this)return;Active=null;
        player?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
        Cursor.visible=oldCursor;Cursor.lockState=oldLock;GameplayInput.ConsumeCurrentFrame();
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(false);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(false);
        if(mapCanvas!=null)mapCanvas.enabled=mapWasVisible;mapCanvas=null;
    }
    protected virtual void OnOpen(){}
    protected void OnDisable()=>Close();protected void OnDestroy(){Close();if(Application.isPlaying&&uiRoot!=null)Destroy(uiRoot);}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetActive()=>Active=null;
    [ContextMenu("Preview UI In Hierarchy")]public void Preview(){if(uiRoot==null)Build();BindControls();uiRoot.SetActive(true);OnOpen();Refresh();}
    protected abstract void BindControls();
    protected void Bind(string name,System.Action action){var b=Find<Button>(name);if(b==null)return;b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>action());}
    protected abstract void Refresh();public abstract void Build();
    protected Transform MakeCanvas(string name)
    {
        var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);uiRoot=root;
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=450;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var scrim=Panel("Backdrop",root.transform,new(0,0),new(1,1),new(.035f,.06f,.07f,.46f),0);scrim.GetComponent<Image>().raycastTarget=true;
        var safe=GameplayHUDStyle.Rect("Safe Area",root.transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();return safe;
    }
    protected static RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Color? color=null,float radius=12)
    {var r=GameplayHUDStyle.Rect(name,parent,min,max);GameplayHUDStyle.Surface(r,color??GameplayHUDStyle.Modal,radius);return r;}
    protected static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 min,Vector2 max)
    {var t=GameplayHUDStyle.Text(name,parent,value,size,min,max);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
    protected static Button Button(string name,Transform parent,string value,Vector2 min,Vector2 max,System.Action action)
    {
        var r=Panel(name,parent,min,max,GameplayHUDStyle.Card);var image=r.GetComponent<Image>();image.raycastTarget=true;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new(1.18f,1.18f,1.1f);colors.pressedColor=new(.85f,.95f,.82f);colors.selectedColor=Color.white;colors.disabledColor=new(1,1,1,.82f);colors.fadeDuration=.12f;b.colors=colors;
        var label=Text("Label",r,value,22,new(.06f,.10f),new(.94f,.90f));label.alignment=TextAlignmentOptions.Center;
        b.onClick.AddListener(()=>action());return b;
    }
    protected static Image ImageSlot(string name,Transform parent,Vector2 min,Vector2 max)
    {var r=GameplayHUDStyle.Rect(name,parent,min,max);var image=r.gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;return image;}
    protected T Find<T>(string name)where T:Component=>System.Array.Find(uiRoot.GetComponentsInChildren<T>(true),c=>c.name==name);
    protected static void Paint(Image image,Sprite sprite){if(image==null)return;image.sprite=sprite;image.enabled=sprite!=null;image.color=Color.white;}
    protected static void Selected(Button b,bool selected)=>b.GetComponent<Image>().color=selected?new(.36f,.49f,.39f,.98f):GameplayHUDStyle.Card;
}
