using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Live housing summary and trough context; one HUD per loaded Barn/Coop interior.</summary>
[DisallowMultipleComponent,RequireComponent(typeof(BarnInteriorSceneController))]
[DefaultExecutionOrder(10500)]
public sealed class BarnInteriorHUD : MonoBehaviour
{
    BarnInteriorSceneController controller;
    AnimalHome shownHome;
    GameObject canvasObject;
    Canvas canvas;
    RectTransform safe,summary,details;
    TMP_Text title,level,feed,used,estimate,inputs,ready,machineState,compactFeed,toggleLabel;
    BarnHUDIcon housingIcon;
    MainMenuIcon stateDot;
    float nextRefresh;
    bool compactLayout,compactExpanded,desktopCollapsed;
    int hungry;
    string troughTitle,troughInstruction,troughDetail;
    const float ExpandedHeight=416,CompactHeight=142;

    void Awake(){controller=GetComponent<BarnInteriorSceneController>();Build();}
    void OnEnable(){Canvas.willRenderCanvases-=Align;Canvas.willRenderCanvases+=Align;}
    void OnDisable(){if(canvasObject!=null)canvasObject.SetActive(false);Canvas.willRenderCanvases-=Align;}
    void OnDestroy(){Canvas.willRenderCanvases-=Align;if(canvasObject!=null)Destroy(canvasObject);}

    void Build()
    {
        canvasObject=new GameObject("Barn Interior HUD",typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(canvasObject,gameObject.scene);
        canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=205;
        var scaler=canvasObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        safe=GameplayHUDStyle.Rect("Safe Area",canvasObject.transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        summary=GameplayHUDStyle.Rect("Housing Summary",safe,Vector2.up,Vector2.up);summary.pivot=Vector2.up;
        summary.sizeDelta=new Vector2(352,ExpandedHeight);
        var surface=GameplayHUDStyle.Surface(summary,new Color(.055f,.105f,.15f,.91f),18);
        surface.borderColor=new Color(.71f,.81f,.88f,.12f);surface.borderWidth=1;
        var header=At("Toggle Details",summary,0,0,352,100);
        var hit=header.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,.001f);hit.canvasRenderer.cullTransparentMesh=false;
        var button=header.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
        button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(ToggleDetails);
        var icon=At("Housing Icon",summary,22,22,66,66);housingIcon=icon.gameObject.AddComponent<BarnHUDIcon>();housingIcon.raycastTarget=false;
        title=Label("Housing Name",summary,104,24,208,34,26);title.fontStyle=FontStyles.Bold;
        level=Label("Housing Level",summary,104,60,170,28,22);level.color=new Color(.77f,.82f,.86f);
        toggleLabel=Label("Expand or Collapse",summary,316,17,22,24,23);toggleLabel.alignment=TextAlignmentOptions.Center;
        details=GameplayHUDStyle.Rect("Details",summary,Vector2.zero,Vector2.one);
        Divider(details,108);
        feed=Row(details,"Pakan",120);
        used=Row(details,"Terpakai Hari Ini",164);
        estimate=Row(details,"Estimasi Stok",208);
        Divider(details,258);
        inputs=Row(details,"Bahan Masuk",272);
        ready=Row(details,"Hasil Siap",316);
        machineState=Row(details,"Status Mesin",360);machineState.fontSize=22;machineState.fontSizeMax=22;machineState.fontSizeMin=18;
        var dot=At("Machine Status Indicator",details,197,372,14,14);stateDot=dot.gameObject.AddComponent<MainMenuIcon>();stateDot.kind=MainMenuIcon.Kind.Coin;stateDot.raycastTarget=false;
        compactFeed=Label("Compact Feed",summary,24,100,304,28,21);
        canvasObject.SetActive(false);
    }
    static RectTransform At(string name,Transform parent,float x,float y,float width,float height)
    {
        var rect=GameplayHUDStyle.Rect(name,parent,Vector2.up,Vector2.up);rect.pivot=Vector2.up;
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    static TMP_Text Label(string name,Transform parent,float x,float y,float width,float height,float size)
    {
        var rect=At(name,parent,x,y,width,height);
        var text=GameplayHUDStyle.Text("Text",rect,"",size,Vector2.zero,Vector2.one);
        text.color=Color.white;text.textWrappingMode=TextWrappingModes.NoWrap;return text;
    }
    static void Divider(Transform parent,float y)
    {
        var line=At("Divider",parent,24,y,304,1);var image=line.gameObject.AddComponent<Image>();
        image.color=new Color(.65f,.75f,.82f,.24f);image.raycastTarget=false;
    }
    static TMP_Text Row(Transform parent,string caption,float y)
    {
        var text=Label(caption,parent,24,y,170,36,21);text.text=caption;text.color=new Color(.77f,.82f,.86f);
        var value=Label(caption+" Value",parent,204,y,124,36,24);value.alignment=TextAlignmentOptions.MidlineRight;value.fontStyle=FontStyles.Bold;
        return value;
    }

    /// <summary>Touch/mouse can expand the compact phone summary without pausing gameplay.</summary>
    public void ToggleDetails()
    {
        if(compactLayout)compactExpanded=!compactExpanded;else desktopCollapsed=!desktopCollapsed;
        Align();
    }
    void LateUpdate()
    {
        AnimalHome home=BarnInterior.Current!=null?BarnInterior.Current.home:null;
        bool visible=controller!=null&&controller.isActiveAndEnabled&&home!=null&&home.Available&&
            !GameplayPauseMenu.BlocksGameplayInput&&!WorldInteractionPrompt.IsSuppressed;
        canvasObject.SetActive(visible);
        if(!visible){shownHome=null;return;}
        if(home!=shownHome||Time.unscaledTime>=nextRefresh)
        {shownHome=home;nextRefresh=Time.unscaledTime+.2f;RefreshData(home);}
        Align();
        if(controller.IsFeedingPromptDisplayed)
            GameplayInfoPanel.RequestBesideToolsInfo(this,troughTitle,troughInstruction,troughDetail);
    }
    void RefreshData(AnimalHome home)
    {
        bool coop=home.Kind==AnimalHousingKind.Coop;
        hungry=0;
        foreach(var resident in home.Residents)
        {
            var animal=resident!=null?resident.Animal:null;
            if(animal==null||!animal.HasBeenBorn||animal.IsInIncubator)continue;
            if(!animal.FedToday)hungry++;
        }
        title.text=coop?"Kandang Unggas":"Kandang Ternak";
        var symbol=coop?BarnHUDIcon.Symbol.Poultry:BarnHUDIcon.Symbol.Cow;
        if(housingIcon.symbol!=symbol){housingIcon.symbol=symbol;housingIcon.SetVerticesDirty();}
        level.text=$"Lv. {controller.LayoutLevel}";
        feed.text=$"{home.TotalFeed} / {home.FeedingSlotCapacity}";
        compactFeed.text=$"Pakan  <b>{home.TotalFeed} / {home.FeedingSlotCapacity}</b>";
        used.text=home.FeedPortionsReserved.ToString();estimate.text=$"{home.EstimatedFeedDays} Hari";
        FeedMaker machine=controller.InteriorFeedMaker;
        if(machine==null)
        {inputs.text="—";ready.text="—";machineState.text="Tidak ada";stateDot.color=new Color(.59f,.66f,.71f);}
        else
        {
            inputs.text=$"{machine.Inputs} / {machine.InputCapacity}";
            int output=machine.Ready(home.RequiredFeedKind);ready.text=$"{output} / {machine.OutputCapacity}";
            bool running=false;foreach(var job in machine.Jobs)if(job.finish>=0){running=true;break;}
            machineState.text=output>0?"Siap diambil":running?"Memproses":machine.Jobs.Count>0?"Menunggu":"Kosong";
            stateDot.color=output>0?new Color(.50f,.82f,.54f):running?new Color(.45f,.75f,.97f):new Color(1f,.76f,.19f);
        }
        troughTitle=home.FeedSpace<=0?"Tempat pakan penuh":home.TotalFeed>0?"Tempat pakan terisi":"Tempat pakan kosong";
        troughInstruction=home.FeedSpace<=0?"Semua slot sudah terisi":$"Gunakan {FeedTypes.Label(home.RequiredFeedKind)}";
        troughDetail=$"Belum makan: {hungry}";
    }
    void Align()
    {
        if(summary==null||canvasObject==null||!canvasObject.activeInHierarchy)return;
        compactLayout=canvas.pixelRect.width<1000||safe.rect.width<safe.rect.height*1.25f;
        bool expanded=compactLayout?compactExpanded:!desktopCollapsed;
        float width=Mathf.Min(352,Mathf.Max(300,safe.rect.width-48));
        summary.sizeDelta=new Vector2(width,expanded?ExpandedHeight:CompactHeight);
        float top=Mathf.Clamp(safe.rect.height*.20f,150,220);
        summary.anchoredPosition=new Vector2(32,-Mathf.Min(top,Mathf.Max(110,safe.rect.height-summary.sizeDelta.y-120)));
        details.gameObject.SetActive(expanded);compactFeed.gameObject.SetActive(!expanded);toggleLabel.text=expanded?"−":"+";
    }
}
