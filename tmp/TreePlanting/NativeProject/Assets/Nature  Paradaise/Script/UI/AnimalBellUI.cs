using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Bell command panel. Player input is locked while animal simulation keeps running.</summary>
[DefaultExecutionOrder(-9500)]
public sealed class AnimalBellUI : MonoBehaviour
{
    public static AnimalBellUI Instance { get; private set; }
    public AnimalBellStation Station { get; private set; }
    public int OutsideCount { get; private set; }
    public int InsideCount { get; private set; }
    public int MovingCount { get; private set; }
    public bool CanRecall { get; private set; }
    public bool CanRelease { get; private set; }
    PlayerController movement;
    AnimalBellTheme theme;
    TMP_Text title,subtitle,warning,outsideText,insideText,movingText,feedback;
    Image weatherImage;
    Button recall,release;
    bool disposed;
    float nextRefresh;
    public static void Show(AnimalBellStation station)
    {
        if(station==null||station.Home==null||station.Bell==null||station.PlayerInventory==null)return;
        if(Instance!=null){if(Instance.Station==station)return;Instance.Dispose();}
        var ui=new GameObject("Animal Bell UI",typeof(RectTransform)).AddComponent<AnimalBellUI>();Instance=ui;ui.Station=station;
        ui.movement=station.PlayerInventory.GetComponent<PlayerController>();ui.movement?.AcquireMovementLock(ui);WorldInteractionPrompt.AcquireSuppression(ui);
        ui.Build();ui.Refresh();GameplayInput.ConsumeCurrentFrame();
    }
    void Update()
    {
        if(disposed)return;
        if(Station==null||!Station.isActiveAndEnabled||Station.Home==null||!Station.Home.Available||Station.Bell==null||Station.PlayerInventory==null||!Station.IsPlayerInRange(Station.PlayerInventory.transform)){Dispose();return;}
        if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.15f;Refresh();}
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame||GameplayPauseMenu.IsOpen)return;
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)){Dispose();return;}
        if(Input.GetKeyDown(KeyCode.F))Recall();if(Input.GetKeyDown(KeyCode.G))Release();
    }
    public void Refresh()
    {
        OutsideCount=InsideCount=MovingCount=0;string reason="";bool allowed=true;
        foreach(var resident in Station.Home.Residents)
        {
            if(resident==null||resident.Animal==null||!resident.Animal.HasBeenBorn)continue;
            if(resident.Returning)MovingCount++;else if(resident.IsHoused)InsideCount++;else OutsideCount++;
            if(!resident.CanReleaseNow()){allowed=false;if(reason.Length==0)reason=resident.ReleaseBlockReason();}
        }
        CanRecall=OutsideCount>0&&Station.Bell.IsReady;
        CanRelease=(InsideCount+MovingCount)>0&&allowed&&Station.Bell.IsReady;
        recall.interactable=CanRecall&&!GameplayPauseMenu.IsOpen;release.interactable=CanRelease&&!GameplayPauseMenu.IsOpen;
        title.text=Station.Home.Kind==AnimalHousingKind.Coop?"BELL KANDANG UNGGAS":"BELL KANDANG BESAR";
        subtitle.text=$"{Station.Home.Label}\n{OutsideCount} hewan di luar";
        outsideText.text=$"Di luar\n<b>{OutsideCount}</b>";insideText.text=$"Di dalam\n<b>{InsideCount}</b>";movingText.text=$"Bergerak\n<b>{MovingCount}</b>";
        var weather=WeatherSystem.Instance;
        bool rain=weather!=null&&WeatherSystem.IsRainWeather(weather.CurrentWeather);
        bool snow=weather!=null&&(weather.CurrentWeather==WeatherType.Snow||weather.CurrentWeather==WeatherType.Blizzard);
        warning.color=rain||snow?new(1,.72f,.3f):new(.78f,.87f,.93f);
        warning.text=weather==null?"Data cuaca belum tersedia.":rain||snow?$"{WeatherSystem.GetDisplayName(weather.CurrentWeather)}\nMasukkan hewan agar terlindung.":$"{WeatherSystem.GetDisplayName(weather.CurrentWeather)}\nHewan sehat dapat keluar pukul 06:00–18:00.";
        weatherImage.sprite=rain?theme?.rainIcon:snow?theme?.snowIcon:theme?.fairWeatherIcon;weatherImage.enabled=weatherImage.sprite!=null;
        if(!allowed&&InsideCount+MovingCount>0)feedback.text=$"Belum dapat keluar: {reason}.";
        else if(OutsideCount+InsideCount+MovingCount==0)feedback.text="Belum ada hewan yang sudah lahir di kandang ini.";
        else if(feedback.text.StartsWith("Belum dapat keluar:")||feedback.text.StartsWith("Belum ada hewan"))feedback.text="Gunakan bell untuk kandang ini.";
    }
    public bool Recall()
    {
        Refresh();if(!CanRecall||GameplayPauseMenu.IsOpen)return false;Station.Bell.RecallAnimals(Station.Home);feedback.text="Perintah masuk dikirim. Hewan menuju kandang.";Refresh();GameplayInput.ConsumeCurrentFrame();return true;
    }
    public bool Release()
    {
        Refresh();if(!CanRelease||GameplayPauseMenu.IsOpen)return false;Station.Bell.ReleaseAnimals(Station.Home);feedback.text="Perintah keluar dikirim.";Refresh();GameplayInput.ConsumeCurrentFrame();return true;
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;if(Instance==this)Instance=null;
        movement?.ReleaseMovementLock(this);WorldInteractionPrompt.ReleaseSuppression(this);GameplayInput.ConsumeCurrentFrame();
        gameObject.SetActive(false);Destroy(gameObject);
    }
    void OnDisable()=>Dispose();void OnDestroy()=>Dispose();
    void Build()
    {
        theme=Resources.Load<AnimalBellTheme>("UI/AnimalBellTheme");var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=455;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_Bell",typeof(EventSystem),typeof(StandaloneInputModule));
        var safe=Rect(transform,"Safe Area",0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();var panel=Rect(safe,"Bell Panel",.54f,.30f,.78f,.75f);
        if(theme?.panel!=null){var image=panel.gameObject.AddComponent<Image>();image.sprite=theme.panel;image.type=Image.Type.Sliced;}else GameplayHUDStyle.Surface(panel,GameplayHUDStyle.Modal,15).raycastTarget=true;
        Art("Bell Image Slot",panel,theme?.bellIcon,.035f,.83f,.16f,.96f);title=Text(panel,"",22,.18f,.895f,.96f,.98f);title.fontStyle=FontStyles.Bold;subtitle=Text(panel,"",17,.18f,.78f,.96f,.90f);
        recall=Button(panel,"[F]  Masukkan Semua Hewan",.05f,.62f,.95f,.75f,()=>Recall());release=Button(panel,"[G]  Keluarkan Semua Hewan",.05f,.465f,.95f,.595f,()=>Release());
        var weather=Rect(panel,"Weather Advice",.05f,.27f,.95f,.43f);GameplayHUDStyle.Surface(weather,GameplayHUDStyle.Card,10);
        weatherImage=Art("Weather Image Slot",weather,null,.04f,.15f,.18f,.85f);warning=Text(weather,"",18,.22f,.05f,.97f,.95f);
        var stats=Rect(panel,"Resident Counts",.05f,.115f,.95f,.24f);GameplayHUDStyle.Surface(stats,GameplayHUDStyle.Card,10);
        outsideText=Text(stats,"",18,.02f,.05f,.32f,.95f);insideText=Text(stats,"",18,.35f,.05f,.65f,.95f);movingText=Text(stats,"",18,.68f,.05f,.98f,.95f);
        outsideText.alignment=insideText.alignment=movingText.alignment=TextAlignmentOptions.Center;
        Art("Info Image Slot",panel,theme?.infoIcon,.05f,.02f,.095f,.08f);feedback=Text(panel,"Gunakan bell untuk kandang ini.",15,.115f,.015f,.95f,.10f);
        var keys=Rect(safe,"Bell Controls",.335f,.03f,.67f,.10f);GameplayHUDStyle.Surface(keys,GameplayHUDStyle.Modal,12);Text(keys,"F Masukkan Semua   G Keluarkan Semua",18,.035f,.1f,.75f,.9f);Button(keys,"Esc Tutup",.765f,.13f,.975f,.87f,Dispose);
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)=>GameplayHUDStyle.Rect(name,parent,new(x,y),new(xx,yy));
    static TMP_Text Text(Transform parent,string value,float size,float x,float y,float xx,float yy)=>GameplayHUDStyle.Text(value.Length>0?value:"Value",parent,value,size,new(x,y),new(xx,yy));
    static Image Art(string name,Transform parent,Sprite sprite,float x,float y,float xx,float yy){var image=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;}
    static Button Button(Transform parent,string value,float x,float y,float xx,float yy,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(parent,value,x,y,xx,yy);var graphic=GameplayHUDStyle.Surface(rect,Color.white,8);graphic.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;var colors=button.colors;
        colors.normalColor=new(.22f,.29f,.34f,.94f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.32f,.53f,.37f);colors.pressedColor=new(.23f,.40f,.28f);colors.disabledColor=new(.16f,.20f,.24f,.6f);colors.fadeDuration=0;button.colors=colors;GameplayHUDStyle.ButtonStates(button);button.navigation=new Navigation{mode=Navigation.Mode.None};rect.gameObject.AddComponent<MainMenuButtonAudio>();
        Text(rect,value,20,.04f,.05f,.96f,.95f).alignment=TextAlignmentOptions.Center;button.onClick.AddListener(()=>{if(Instance!=null&&!Instance.disposed&&!GameplayPauseMenu.IsOpen)action();});return button;
    }
}
