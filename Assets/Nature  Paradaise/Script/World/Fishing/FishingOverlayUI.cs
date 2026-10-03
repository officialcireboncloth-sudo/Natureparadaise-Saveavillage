using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Screen-space fishing presentation; all values come from the existing fishing session.</summary>
[DisallowMultipleComponent]
public sealed class FishingOverlayUI : MonoBehaviour
{
    [SerializeField] FishingUITheme imageSlots;
    FishingSystem fishing;
    GameObject canvasObject, panel, track, catchArt;
    TMP_Text title, progressText, percentText, stressText, instructions, details, hint;
    RectTransform fishZone, reelCursor, progressFill, stressFill;
    Image ring, catchImage;
    FishingHoldButton hold;
    public bool ReelHeld => isActiveAndEnabled && fishing != null && fishing.State == FishingState.Minigame && hold != null && hold.Held;
    public bool CastHeld => isActiveAndEnabled && fishing != null && (fishing.State == FishingState.Idle || fishing.State == FishingState.Charging) && hold != null && hold.Held;
    public void Bind(FishingSystem owner) { fishing=owner; }
    void LateUpdate()
    {
        if(fishing==null)return;
        if(canvasObject==null)Build();
        bool visible=fishing.State!=FishingState.Idle && !GameplayPauseMenu.IsOpen;
        canvasObject.SetActive(visible);
        if(!visible){hold.Held=false;return;}
        bool minigame=fishing.State==FishingState.Minigame;
        track.SetActive(minigame);catchArt.SetActive(fishing.State==FishingState.Result);
        title.text=fishing.State switch
        {
            FishingState.Charging=>"KEKUATAN LEMPARAN", FishingState.Casting=>"MELEMPAR KAIL",
            FishingState.WaitingForBite=>"MENUNGGU IKAN", FishingState.Bite=>"IKAN MENGGIGIT!",
            FishingState.Minigame=>"IKAN TERKAIT", FishingState.Catching=>"MENGANGKAT TANGKAPAN",
            _=>"HASIL TANGKAPAN"
        };
        float progress=fishing.State==FishingState.Charging?fishing.CastPower:fishing.State==FishingState.Bite?fishing.HookTimeRemaining:fishing.CatchProgress;
        progressFill.anchorMax=new(Mathf.Clamp01(progress),1);
        progressText.text=fishing.State==FishingState.Charging?"Kekuatan Lemparan":fishing.State==FishingState.Bite?"Waktu Mengait":"Progres Tangkapan";
        percentText.text=Mathf.RoundToInt(progress*100)+"%";
        ring.fillAmount=Mathf.Clamp01(progress);
        bool result=fishing.State==FishingState.Result;
        progressText.gameObject.SetActive(!result);percentText.gameObject.SetActive(!result);ring.gameObject.SetActive(!result);progressFill.parent.gameObject.SetActive(!result);
        details.rectTransform.anchorMin=result?new(.33f,.33f):new(.30f,.02f);
        details.rectTransform.anchorMax=result?new(.96f,.83f):new(.96f,.15f);
        details.fontSizeMax=result?22:15;
        stressFill.anchorMax=new(Mathf.Clamp01(fishing.LineStress),1);
        stressFill.parent.gameObject.SetActive(minigame);stressText.gameObject.SetActive(minigame);
        stressText.text=$"Ketegangan Senar   {Mathf.RoundToInt(fishing.LineStress*100)}%";
        float zone=Mathf.Clamp(fishing.ActiveFish!=null?fishing.ActiveFish.catchZoneSize:.3f,.05f,1);
        float bottom=Mathf.Clamp(fishing.FishPosition-zone*.5f,0,1-zone);
        fishZone.anchorMin=new(0,bottom);fishZone.anchorMax=new(1,bottom+zone);
        float cursor=Mathf.Clamp01(fishing.ReelPosition);reelCursor.anchorMin=new(0,cursor);reelCursor.anchorMax=new(1,cursor);
        string key=fishing.ReelKey.ToString();
        instructions.text=minigame?$"Tahan [{key}] / klik untuk menarik\nLepaskan saat senar terlalu tegang":fishing.State switch
        {
            FishingState.Charging=>$"Tahan [{key}] untuk mengisi kekuatan\nLepaskan untuk melempar",
            FishingState.Bite=>$"Tekan [{key}] untuk mengait ikan",
            FishingState.WaitingForBite=>"Tunggu sampai ikan menggigit kail",
            FishingState.Result=>"Tangkapan akan masuk ke inventory",
            _=>""
        };
        details.text=fishing.State==FishingState.Result
            ?$"{fishing.ResultItem?.itemName}\n{(fishing.ResultItem?.category==ItemCategory.Fish?$"Ukuran: {fishing.ResultSize:0.0} cm\n":"")}Lokasi: {fishing.CatchLocation}\nMasuk inventory: {fishing.ResultTimeRemaining:0.0}s"
            :$"Umpan: {(fishing.ActiveCastBait!=null?fishing.ActiveCastBait.itemName:"Tanpa umpan")}\nJarak: {(fishing.State==FishingState.Charging?fishing.PreviewCastDistance:fishing.CastDistance):0.0} m";
        catchImage.sprite=fishing.ResultItem?.icon!=null?fishing.ResultItem.icon:imageSlots?.catchPlaceholder;
        catchImage.enabled=catchImage.sprite!=null;
        hint.text=minigame?$"[{key}]  Tahan / Lepas untuk menarik":instructions.text;
        hold.gameObject.SetActive(Application.isMobilePlatform && minigame);
    }
    void OnDisable(){if(canvasObject!=null)canvasObject.SetActive(false);if(hold!=null)hold.Held=false;}
    void OnDestroy(){if(canvasObject!=null)Destroy(canvasObject);}
    void Build()
    {
        if(imageSlots==null)imageSlots=Resources.Load<FishingUITheme>("UI/FishingUITheme");
        canvasObject=new GameObject("Fishing Overlay Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject,gameObject.scene);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=320;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        var safe=GameplayHUDStyle.Rect("Safe Area",canvasObject.transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        var rect=GameplayHUDStyle.Rect("Fishing Panel",safe,new(.765f,.36f),new(.985f,.755f));panel=rect.gameObject;
        if(imageSlots?.panel!=null){var surface=rect.gameObject.AddComponent<Image>();surface.sprite=imageSlots.panel;surface.type=Image.Type.Sliced;surface.raycastTarget=false;}
        else GameplayHUDStyle.Surface(rect,new(.15f,.23f,.28f,.88f),14);
        title=Text(rect,"Title","",23,new(.05f,.88f),new(.95f,.98f));
        title.fontStyle=FontStyles.Bold;
        GameplayHUDStyle.Surface(GameplayHUDStyle.Rect("Divider",rect,new(.04f,.875f),new(.96f,.879f)),new(.7f,.85f,.9f,.5f),0);
        var trackRect=GameplayHUDStyle.Rect("Vertical Fish Track",rect,new(.07f,.07f),new(.255f,.84f));track=trackRect.gameObject;
        GameplayHUDStyle.Surface(trackRect,new(.09f,.16f,.20f,.55f),9);
        fishZone=GameplayHUDStyle.Rect("Fish Target Zone",trackRect,new(0,.35f),new(1,.65f));GameplayHUDStyle.Surface(fishZone,new(.48f,.72f,.55f,.45f),3);
        Artwork("Fish Silhouette Slot",fishZone,imageSlots?.fishSilhouette,new(.1f,.08f),new(.9f,.92f));
        Artwork("Upward Arrow Slot",trackRect,imageSlots?.upwardArrow,new(.22f,.85f),new(.78f,.96f));
        reelCursor=GameplayHUDStyle.Rect("Reel Position",trackRect,new(0,.5f),new(1,.5f));reelCursor.sizeDelta=new(0,5);
        GameplayHUDStyle.Surface(reelCursor,Color.white,2);
        Artwork("Reel Indicator Slot",reelCursor,imageSlots?.reelIndicator,new(0,-2),new(1,3));
        progressText=Text(rect,"Catch Progress","",19,new(.39f,.78f),new(.96f,.84f));
        ring=Artwork("Progress Ring Slot",rect,imageSlots?.progressRing,new(.33f,.60f),new(.49f,.78f));ring.type=Image.Type.Filled;ring.fillMethod=Image.FillMethod.Radial360;
        percentText=Text(rect,"Catch Percent","",19,new(.33f,.60f),new(.49f,.78f));percentText.alignment=TextAlignmentOptions.Center;
        progressFill=Bar(rect,"Catch Progress Bar",new(.52f,.67f),new(.95f,.71f),new(.6f,.88f,.57f));
        stressText=Text(rect,"Line Tension","",18,new(.39f,.43f),new(.96f,.54f));
        stressFill=Bar(rect,"Line Tension Bar",new(.40f,.38f),new(.95f,.42f),new(1,.74f,.33f));
        instructions=Text(rect,"Fishing Instructions","",17,new(.39f,.16f),new(.96f,.33f));
        details=Text(rect,"Session Details","",15,new(.30f,.02f),new(.96f,.15f));
        var art=GameplayHUDStyle.Rect("Catch Artwork",rect,new(.05f,.3f),new(.29f,.8f));catchArt=art.gameObject;catchImage=Artwork("Catch Image Slot",art,null,Vector2.zero,Vector2.one);
        hint=Text(safe,"Fishing Key Hints","",19,new(.77f,.12f),new(.985f,.22f));
        var mobile=GameplayHUDStyle.Rect("Touch Hold",safe,new(.79f,.025f),new(.98f,.09f));GameplayHUDStyle.Surface(mobile,new(.25f,.33f,.37f,.95f),8).raycastTarget=true;
        Text(mobile,"Label","TAHAN / LEPAS",20,Vector2.zero,Vector2.one).alignment=TextAlignmentOptions.Center;
        hold=mobile.gameObject.AddComponent<FishingHoldButton>();
        if(Application.isMobilePlatform&&EventSystem.current==null)new GameObject("EventSystem_Fishing",typeof(EventSystem),typeof(StandaloneInputModule));
    }
    static TMP_Text Text(Transform parent,string name,string value,float size,Vector2 min,Vector2 max)=>GameplayHUDStyle.Text(name,parent,value,size,min,max);
    static Image Artwork(string name,Transform parent,Sprite sprite,Vector2 min,Vector2 max)
    {
        var image=GameplayHUDStyle.Rect(name,parent,min,max).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;
    }
    static RectTransform Bar(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var track=GameplayHUDStyle.Rect(name,parent,min,max);GameplayHUDStyle.Surface(track,new(.1f,.16f,.2f,.9f),7);
        var fill=GameplayHUDStyle.Rect("Fill",track,Vector2.zero,Vector2.one);GameplayHUDStyle.Surface(fill,color,7).borderWidth=0;return fill;
    }
}

public sealed class FishingHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool Held;
    public void OnPointerDown(PointerEventData e)=>Held=true;
    public void OnPointerUp(PointerEventData e)=>Held=false;
    public void OnPointerExit(PointerEventData e)=>Held=false;
    void OnDisable()=>Held=false;
}
