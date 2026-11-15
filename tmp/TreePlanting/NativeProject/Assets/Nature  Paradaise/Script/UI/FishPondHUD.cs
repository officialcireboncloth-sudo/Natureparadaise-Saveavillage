using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One nearby-pond overview. Reads real fish/feed data without pausing or changing saves.</summary>
public sealed class FishPondHUD : MonoBehaviour
{
    static FishPondHUD instance;
    Inventory player;
    FishPond shown;
    RectTransform safe,panel,body;
    GameObject content;
    TMP_Text title,level,count,maker,feed,health,status,compactHint;
    readonly List<TMP_Text> captions=new();
    FishPondTheme theme;
    bool expanded;
    float refreshAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()=>instance=null;
    public static void Ensure()
    {
        if(instance==null)instance=new GameObject("Fish Pond Overview").AddComponent<FishPondHUD>();
    }
    void Awake(){instance=this;Build();}
    void OnDestroy(){if(instance==this)instance=null;}
    void LateUpdate()
    {
        if(player==null)player=FindFirstObjectByType<Inventory>();
        FishPond pond=player!=null?FishPond.NearestOverview(player.transform.position,6f):null;
        bool visible=pond!=null&&!pond.IsPanelOpen&&!GameplayPauseMenu.BlocksGameplayInput&&
            !WorldInteractionPrompt.IsSuppressed&&!(SceneTransitionManager.Instance!=null&&SceneTransitionManager.Instance.IsInsideInterior);
        content.SetActive(visible);
        if(!visible){shown=null;return;}
        if(shown!=pond){shown=pond;expanded=false;refreshAt=0;}
        Align();
        if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.2f;
        RefreshData(pond);
    }
    public void RefreshData(FishPond pond)
    {
        level.text=$"Lv. {pond.Level}";count.text=$"{pond.FishCount} / {pond.Capacity}";
        var machine=pond.FeedMachine;
        maker.text=machine==null||machine.FishFeedOutput==0&&machine.Jobs.Count==0?"Kosong":
            machine.FishFeedOutput>0?$"Siap x{machine.FishFeedOutput}":"Proses";
        feed.text=pond.FeedStock>0?$"{pond.FeedStock} / {pond.FeedCapacity}":"Kosong";
        int sick=0,hungry=0;
        if(pond.Fish!=null)foreach(var fish in pond.Fish)if(fish!=null){if(fish.sick)sick++;if(fish.daysWithoutFood>0)hungry++;}
        health.text=pond.FishCount==0?"—":sick>0?$"{sick} sakit":hungry>0?"Lapar":"Sehat";
        status.text=pond.FishCount==0?"Belum ada ikan":sick>0?"Ada ikan perlu diobati":
            pond.FeedStock==0?"Siapkan pakan ikan":"Pakan tersedia";
        compactHint.text=$"{pond.FishCount} / {pond.Capacity} ikan · Ketuk untuk detail";
    }
    void Align()
    {
        bool compact=Application.isMobilePlatform||safe.rect.width/Mathf.Max(1,safe.rect.height)<1.4f;
        float width=Mathf.Min(compact?350:470,safe.rect.width-32);
        panel.sizeDelta=new Vector2(width,compact&&!expanded?132:394);
        panel.anchoredPosition=new Vector2(24,-236);
        body.gameObject.SetActive(!compact||expanded);
        compactHint.gameObject.SetActive(compact&&!expanded);
        title.fontSizeMin=compact?20:25.6f;title.fontSizeMax=compact?27:32;
        foreach(var caption in captions){caption.fontSizeMin=compact?17:19.2f;caption.fontSizeMax=compact?21:24;}
    }
    void Build()
    {
        theme=Resources.Load<FishPondTheme>("UI/FishPondTheme");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=206;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        safe=GameplayHUDStyle.Rect("Safe Area",transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        content=new GameObject("Nearby Pond",typeof(RectTransform));var all=(RectTransform)content.transform;
        all.SetParent(safe,false);all.anchorMin=Vector2.zero;all.anchorMax=Vector2.one;all.offsetMin=all.offsetMax=Vector2.zero;
        panel=At(all,"Pond Overview",0,0,470,394);
        var surface=GameplayHUDStyle.Surface(panel,GameplayHUDStyle.Modal,26);surface.borderColor=new(GameplayHUDStyle.Muted.r,GameplayHUDStyle.Muted.g,GameplayHUDStyle.Muted.b,.18f);surface.borderWidth=2;
        var header=At(panel,"Header",0,0,470,86);header.anchorMax=new Vector2(1,1);header.sizeDelta=new Vector2(0,86);
        GameplayHUDStyle.Surface(header,GameplayHUDStyle.Card,24);
        var hit=header.gameObject.AddComponent<Button>();hit.targetGraphic=header.GetComponent<MainMenuRoundedImage>();hit.targetGraphic.raycastTarget=true;
        hit.transition=Selectable.Transition.None;hit.navigation=new Navigation{mode=Navigation.Mode.None};hit.onClick.AddListener(()=>{expanded=!expanded;Align();});
        Icon(header,FishPondHUDIcon.Symbol.Fish,GameplayHUDStyle.TextColor,22,15,57);
        title=Label(header,"Kolam Ikan",32,93,17,250,50);title.fontStyle=FontStyles.Bold;
        title.rectTransform.anchorMax=new Vector2(1,1);title.rectTransform.sizeDelta=new Vector2(-204,50);
        var badge=At(header,"Level Badge",-102,23,82,40);badge.anchorMin=badge.anchorMax=Vector2.one;
        GameplayHUDStyle.Surface(badge,GameplayHUDStyle.Modal,20);
        level=Label(badge,"Lv. 1",22,0,0,82,40);level.alignment=TextAlignmentOptions.Center;level.fontStyle=FontStyles.Bold;
        body=At(panel,"Details",0,86,470,308);body.anchorMax=new Vector2(1,1);body.sizeDelta=new Vector2(0,308);
        count=Row(body,"Jumlah Ikan",FishPondHUDIcon.Symbol.Fish,10);
        maker=Row(body,"Pembuat Pakan",FishPondHUDIcon.Symbol.Machine,70);
        feed=Row(body,"Tempat Pakan",FishPondHUDIcon.Symbol.Feed,130);
        health=Row(body,"Kesehatan",FishPondHUDIcon.Symbol.Heart,190);
        var footer=At(body,"Pond Status",34,256,402,38);footer.anchorMax=new Vector2(1,1);footer.sizeDelta=new Vector2(-68,38);
        GameplayHUDStyle.Surface(footer,GameplayHUDStyle.Card,19);
        status=Label(footer,"Belum ada ikan",21,5,0,392,38);status.rectTransform.anchorMax=new Vector2(1,1);status.rectTransform.sizeDelta=new Vector2(-10,38);status.alignment=TextAlignmentOptions.Center;
        compactHint=Label(panel,"",19,20,90,430,34);compactHint.color=GameplayHUDStyle.Muted;compactHint.rectTransform.anchorMax=new Vector2(1,1);compactHint.rectTransform.sizeDelta=new Vector2(-40,34);compactHint.alignment=TextAlignmentOptions.Center;compactHint.gameObject.SetActive(false);
        content.SetActive(false);
    }
    TMP_Text Row(RectTransform parent,string caption,FishPondHUDIcon.Symbol symbol,float y)
    {
        Icon(parent,symbol,GameplayHUDStyle.TextColor,24,y+6,40);
        var label=Label(parent,caption,24,80,y,250,50);label.color=GameplayHUDStyle.Muted;label.fontStyle=FontStyles.Bold;
        captions.Add(label);label.rectTransform.anchorMax=new Vector2(.65f,1);label.rectTransform.sizeDelta=new Vector2(-80,50);
        var value=Label(parent,"",24,0,y,135,50);value.rectTransform.anchorMin=new Vector2(.70f,1);value.rectTransform.anchorMax=Vector2.one;value.rectTransform.sizeDelta=new Vector2(-26,50);value.alignment=TextAlignmentOptions.MidlineRight;
        var line=At(parent,"Divider",24,y+58,422,1);line.anchorMax=new Vector2(1,1);line.sizeDelta=new Vector2(-48,1);line.gameObject.AddComponent<Image>().color=new(GameplayHUDStyle.Muted.r,GameplayHUDStyle.Muted.g,GameplayHUDStyle.Muted.b,.18f);
        return value;
    }
    void Icon(Transform parent,FishPondHUDIcon.Symbol symbol,Color color,float x,float y,float size)
    {
        Sprite sprite=theme==null?null:symbol switch
        {FishPondHUDIcon.Symbol.Fish=>theme.overviewFish,FishPondHUDIcon.Symbol.Machine=>theme.overviewMachine,
            FishPondHUDIcon.Symbol.Feed=>theme.overviewFeed,_=>theme.overviewHealth};
        var rect=At(parent,symbol.ToString(),x,y,size,size);
        if(sprite!=null)
        {
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.preserveAspect=true;
            image.raycastTarget=false;if(theme.overviewIconMaterial!=null)image.material=theme.overviewIconMaterial;
        }
        else
        {var icon=rect.gameObject.AddComponent<FishPondHUDIcon>();icon.symbol=symbol;icon.color=color;icon.raycastTarget=false;}
    }
    static RectTransform At(Transform parent,string name,float x,float y,float w,float h)
    {var r=GameplayHUDStyle.Rect(name,parent,Vector2.up,Vector2.up);r.pivot=Vector2.up;r.anchoredPosition=new(x,-y);r.sizeDelta=new(w,h);return r;}
    static TMP_Text Label(Transform parent,string text,float size,float x,float y,float w,float h)
    {
        var r=At(parent,text.Length>0?text:"Value",x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=size;t.fontSizeMin=size*.8f;t.fontSizeMax=size;t.enableAutoSizing=true;
        t.color=GameplayHUDStyle.TextColor;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
}
