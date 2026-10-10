using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Cave-only HUD preset; the existing inventory hotbar remains visible.</summary>
[DisallowMultipleComponent]
public sealed class MiningHUD : MonoBehaviour
{
    CaveInteriorController cave;
    GameObject root;
    TMP_Text location, clock, gold, stamina;
    RectTransform fill;
    PlayerStatusSystem status;
    void Awake()
    {
        cave=GetComponent<CaveInteriorController>();
        root=new GameObject("MiningHUDCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200;
        var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var left=GameplayHUDStyle.Rect("MineAndClock",root.transform,new Vector2(.014f,.915f),new Vector2(.27f,.985f));
        location=GameplayHUDStyle.Text("Location",left,"",22,new Vector2(.1f,.45f),Vector2.one);
        clock=GameplayHUDStyle.Text("Clock",left,"",22,new Vector2(.1f,0),new Vector2(1,.45f));
        var right=GameplayHUDStyle.Rect("GoldAndStamina",root.transform,new Vector2(.85f,.91f),new Vector2(.985f,.985f));
        gold=GameplayHUDStyle.Text("Gold",right,"",23,new Vector2(.15f,.5f),Vector2.one);
        stamina=GameplayHUDStyle.Text("Stamina",right,"",20,new Vector2(.64f,0),new Vector2(1,.4f));
        var bar=GameplayHUDStyle.Rect("StaminaBar",right,new Vector2(0,.12f),new Vector2(.6f,.28f));
        var bg=GameplayHUDStyle.Surface(bar,new Color(.03f,.05f,.06f,.9f),7);
        fill=GameplayHUDStyle.Rect("Fill",bar,Vector2.zero,Vector2.one);
        var fg=GameplayHUDStyle.Surface(fill,new Color(.44f,.89f,.85f,1),7);
        var theme=Resources.Load<MiningHUDTheme>("UI/MiningHUDTheme");
        Slot(left,"MineIcon",theme!=null?theme.mineIcon:null,new Vector2(0,.48f),new Vector2(.075f,.96f));
        Slot(right,"CoinIcon",theme!=null?theme.coinIcon:null,new Vector2(0,.52f),new Vector2(.12f,.97f));
        if(theme!=null) { ApplyArtwork(bar,theme.staminaBackground,bg); ApplyArtwork(fill,theme.staminaFill,fg); }
        GameplayHUDStyle.Text("Controls",root.transform,"Tab  Tas     M  Peta",18,new Vector2(.86f,.015f),new Vector2(.987f,.05f));
        root.SetActive(false);
    }
    internal static void ApplyArtwork(RectTransform parent, Sprite sprite, Image fallback)
    {
        if(sprite==null) return;
        fallback.enabled=false;
        var image=GameplayHUDStyle.Rect("ArtworkSlot",parent,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
        image.sprite=sprite; image.type=sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;
        image.raycastTarget=false;
        image.transform.SetAsFirstSibling();
    }
    static void Slot(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
    {
        var image=GameplayHUDStyle.Rect(name,parent,min,max).gameObject.AddComponent<Image>();
        image.sprite=sprite; image.enabled=sprite!=null; image.preserveAspect=true; image.raycastTarget=false;
    }
    void Update()
    {
        bool visible=cave!=null && cave.IsCurrentInterior && !WorldInteractionPrompt.IsSuppressed &&
            !GameplayPauseMenu.BlocksGameplayInput;
        root.SetActive(visible);
        if(!visible) return;
        if(status==null) status=FindFirstObjectByType<PlayerStatusSystem>();
        location.text=$"{cave.MineName} · Lantai {cave.Floor}";
        var time=TimeManager.Instance; clock.text=time!=null?$"{time.hour:00}:{time.minute:00}":"";
        gold.text=ScoreManager.Instance!=null?$"{ScoreManager.Instance.points:N0} G":"0 G";
        stamina.text=status!=null?$"{status.Stamina:0.#} / {status.MaxStamina:0.#}":"";
        fill.anchorMax=new Vector2(status!=null?Mathf.Clamp01(status.Stamina/status.MaxStamina):0,1);
    }
    void OnDisable() { if(root!=null) root.SetActive(false); }
    void OnDestroy() { if(root!=null) Destroy(root); }
}
