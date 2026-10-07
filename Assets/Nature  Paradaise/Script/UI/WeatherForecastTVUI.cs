using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-9500)]
public sealed class WeatherForecastTVUI : MonoBehaviour
{
    public static WeatherForecastTVUI Instance { get; private set; }
    public WeatherForecastTV Owner { get; private set; }
    WeatherForecastTVTheme theme;
    GameObject forecastPage,newsPage;
    TMP_Text broadcast,date,currentName,todayName,tomorrowName,advice,presenter,newsHeadline,newsBody,expandedHeadline,expandedBody;
    Image currentIcon,todayIcon,tomorrowIcon;
    readonly GameObject[] tabMarkers=new GameObject[2];
    int focusedTab;
    float previousScale,nextRefresh;
    bool released;
    public static void Show(WeatherForecastTV owner)
    {
        if(Instance!=null||owner==null)return;
        var ui=new GameObject("Television Forecast UI",typeof(RectTransform)).AddComponent<WeatherForecastTVUI>();
        Instance=ui;ui.Owner=owner;ui.previousScale=Time.timeScale;Time.timeScale=0;
        ui.Build();ui.SelectTab(0);
    }
    void Update()
    {
        if(released)return;
        if(Owner==null){Dispose();return;}
        if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)){Owner.CloseTV();return;}
        if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow)){focusedTab=0;RefreshMarkers();}
        if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow)){focusedTab=1;RefreshMarkers();}
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))SelectTab(focusedTab);
        if(Owner.ForecastShortcutKey!=KeyCode.None&&Input.GetKeyDown(Owner.ForecastShortcutKey))SelectTab(0);
    }
    public void SelectTab(int tab)
    {
        focusedTab=Mathf.Clamp(tab,0,1);forecastPage.SetActive(focusedTab==0);newsPage.SetActive(focusedTab==1);
        RefreshMarkers();Refresh();GameplayInput.ConsumeCurrentFrame();
    }
    void RefreshMarkers(){for(int i=0;i<2;i++)tabMarkers[i].SetActive(i==focusedTab);}
    public void Dispose()
    {
        if(released)return;released=true;Time.timeScale=previousScale;
        if(Instance==this)Instance=null;GameplayInput.ConsumeCurrentFrame();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        Owner?.CloseTV();gameObject.SetActive(false);Destroy(gameObject);
    }
    void OnDisable()=>Dispose();void OnDestroy()=>Dispose();
    void Build()
    {
        theme=Resources.Load<WeatherForecastTVTheme>("UI/WeatherForecastTVTheme");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=460;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_TV",typeof(EventSystem),typeof(StandaloneInputModule));
        var shade=Rect("Dim World",transform,0,0,1,1).gameObject.AddComponent<Image>();shade.color=new(0,0,0,.22f);
        var safe=Rect("Safe Area",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel=Rect("Television Panel",safe,.51f,.035f,.985f,.965f);
        if(theme?.panel!=null){var art=panel.gameObject.AddComponent<Image>();art.sprite=theme.panel;art.type=Image.Type.Sliced;art.color=new(1,1,1,.94f);}
        else Surface(panel,GameplayHUDStyle.Modal);
        Text(panel,Owner != null && Owner.IsRadio ? "RADIO" : "TELEVISI",43,.035f,.913f,.58f,.97f).fontStyle=FontStyles.Bold;
        broadcast=Text(panel,"",21,.66f,.92f,.9f,.965f);
        Art("Leaf Image Slot",panel,theme?.leafIcon,.92f,.92f,.965f,.975f);
        var line=Rect("Header Divider",panel,.035f,.901f,.965f,.903f).gameObject.AddComponent<Image>();line.color=new(.65f,.79f,.85f,.5f);line.raycastTarget=false;
        var weatherTab=Button(panel,"Ramalan Cuaca",.027f,.806f,.495f,.884f,()=>SelectTab(0),theme?.forecastIcon);
        var villageTab=Button(panel,"Berita Desa",.515f,.806f,.975f,.884f,()=>SelectTab(1),theme?.newsIcon);
        tabMarkers[0]=Rect("Selection Indicator",weatherTab.transform,.08f,.025f,.92f,.055f).gameObject;
        tabMarkers[1]=Rect("Selection Indicator",villageTab.transform,.08f,.025f,.92f,.055f).gameObject;
        foreach(var marker in tabMarkers){var image=marker.AddComponent<Image>();image.color=new(.66f,.84f,.93f);image.raycastTarget=false;}
        forecastPage=Rect("Forecast Program",panel,.027f,.133f,.975f,.791f).gameObject;
        var weather=Rect("Weather Card",forecastPage.transform,0,.385f,1,1);Surface(weather,GameplayHUDStyle.Card);
        Text(weather,"RAMALAN CUACA",29,.025f,.87f,.975f,.98f).fontStyle=FontStyles.Bold;
        date=Text(weather,"",21,.025f,.79f,.96f,.885f);
        currentIcon=Art("Current Weather Image Slot",weather,null,.07f,.56f,.22f,.78f);
        currentName=Text(weather,"",43,.25f,.63f,.64f,.80f);
        Text(weather,"Suhu: belum tersedia",19,.25f,.55f,.64f,.64f).color=new(.72f,.81f,.86f);
        Art("Rain Image Slot",weather,theme?.rainIcon,.68f,.68f,.735f,.76f);
        Text(weather,"Peluang hujan: —",19,.745f,.68f,.97f,.76f);
        Art("Wind Image Slot",weather,theme?.windIcon,.68f,.56f,.735f,.64f);
        Text(weather,"Kekuatan angin: —",19,.745f,.56f,.97f,.64f);
        todayName=ForecastCard(weather,"Hari Ini",.035f,.25f,.337f,.515f,out todayIcon);
        tomorrowName=ForecastCard(weather,"Besok",.351f,.25f,.653f,.515f,out tomorrowIcon);
        var future=ForecastCard(weather,"Lusa",.668f,.25f,.97f,.515f,out _);future.text="Belum disiarkan";future.fontSizeMax=19;
        Art("Advice Leaf Image Slot",weather,theme?.leafIcon,.025f,.13f,.075f,.21f);
        advice=Text(weather,"",19,.09f,.12f,.975f,.235f);
        presenter=Text(weather,"",18,.025f,.015f,.975f,.12f);
        var news=Rect("Village News Summary",forecastPage.transform,0,0,1,.362f);Surface(news,GameplayHUDStyle.Card);
        Text(news,"BERITA DESA",28,.025f,.80f,.97f,.98f).fontStyle=FontStyles.Bold;
        Art("News Image Slot",news,theme?.villageNewsIcon,.045f,.39f,.145f,.72f);
        newsHeadline=Text(news,"",25,.18f,.53f,.96f,.80f);
        newsBody=Text(news,"",20,.18f,.08f,.96f,.53f);
        newsPage=Rect("Village News Program",panel,.027f,.133f,.975f,.791f).gameObject;Surface((RectTransform)newsPage.transform,GameplayHUDStyle.Card);
        Text(newsPage.transform,"BERITA DESA",32,.025f,.86f,.97f,.97f).fontStyle=FontStyles.Bold;
        Art("News Program Image Slot",newsPage.transform,theme?.villageNewsIcon,.045f,.62f,.20f,.83f);
        expandedHeadline=Text(newsPage.transform,"",30,.24f,.67f,.96f,.85f);
        expandedBody=Text(newsPage.transform,"",25,.055f,.3f,.945f,.60f);
        Text(newsPage.transform,"Informasi desa mengikuti kondisi hari ini.",20,.055f,.12f,.945f,.25f);
        Text(panel,"Ramalan cuaca diperbarui setiap hari.",17,.035f,.085f,.79f,.122f).color=new(.73f,.83f,.89f);
        Text(panel,"A / D  Pilih Menu      Enter  Buka      Esc / E  Tutup",20,.035f,.025f,.81f,.078f);
        Button(panel,"Tutup",.83f,.025f,.975f,.075f,()=>Owner.CloseTV());
        Art("Logo Image Slot",panel,theme?.logo,.79f,.084f,.97f,.129f);
    }
    TMP_Text ForecastCard(Transform parent,string title,float x1,float y1,float x2,float y2,out Image icon)
    {
        var card=Rect(title,parent,x1,y1,x2,y2);Surface(card,GameplayHUDStyle.Card);
        Text(card,title,22,.05f,.70f,.95f,.94f).alignment=TextAlignmentOptions.Midline;
        icon=Art(title+" Weather Image Slot",card,null,.25f,.28f,.75f,.70f);
        var label=Text(card,"",23,.035f,.025f,.965f,.27f);label.alignment=TextAlignmentOptions.Midline;return label;
    }
    void Refresh()
    {
        var time=TimeManager.Instance;int day=time!=null?time.day:1,hour=time!=null?time.hour:0,minute=time!=null?time.minute:0;
        string period=hour<11?"Pagi":hour<15?"Siang":hour<18?"Sore":"Malam";
        broadcast.text=$"Siaran {period} · {hour:00}:{minute:00}";
        var season=SeasonVisualController.ResolveSeasonForDay(day);
        string seasonName=season switch{CropSeason.Spring=>"Musim Semi",CropSeason.Summer=>"Musim Panas",CropSeason.Autumn=>"Musim Gugur",_=>"Musim Dingin"};
        date.text=$"Hari {day} · {seasonName}";
        var weather=WeatherSystem.Instance;
        currentName.text=todayName.text=weather!=null?WeatherSystem.GetShortName(weather.CurrentWeather):"Belum tersedia";
        tomorrowName.text=weather!=null?WeatherSystem.GetShortName(weather.TomorrowWeather):"Belum tersedia";
        Sprite today=weather!=null?theme?.Icon(weather.CurrentWeather):null,tomorrow=weather!=null?theme?.Icon(weather.TomorrowWeather):null;
        SetArt(currentIcon,today);SetArt(todayIcon,today);SetArt(tomorrowIcon,tomorrow);
        advice.text=weather==null?"Ramalan cuaca belum tersedia.":weather.IsRainToday?"Lahan terbuka mendapat air hujan hari ini.":"Periksa kebutuhan air tanaman hari ini.";
        presenter.text=weather!=null?WeatherSystem.GetForecastMessage(weather.TomorrowWeather):"";
        bool cleanup=weather!=null&&weather.IsCommunityCleanupDay;
        newsHeadline.text=expandedHeadline.text=cleanup?"Gotong Royong Desa":"Belum ada berita desa";
        newsBody.text=expandedBody.text=cleanup?"Hari ini warga bergotong royong membersihkan sisa topan.":"Belum ada pengumuman desa untuk hari ini.";
    }
    static void SetArt(Image image,Sprite sprite){image.sprite=sprite;image.enabled=sprite!=null;}
    static RectTransform Rect(string name,Transform parent,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Rect(name,parent,new(x1,y1),new(x2,y2));
    static void Surface(RectTransform rect,Color color)=>GameplayHUDStyle.Surface(rect,color,18).raycastTarget=true;
    static TMP_Text Text(Transform parent,string value,float size,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Text("Text",parent,value,size,new(x1,y1),new(x2,y2));
    static Image Art(string name,Transform parent,Sprite sprite,float x1,float y1,float x2,float y2)
    {var image=Rect(name,parent,x1,y1,x2,y2).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;}
    static Button Button(Transform parent,string label,float x1,float y1,float x2,float y2,UnityEngine.Events.UnityAction action,Sprite sprite=null)
    {
        var rect=Rect(label,parent,x1,y1,x2,y2);var image=GameplayHUDStyle.Surface(rect,Color.white,12);image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;
        colors.normalColor=new(.23f,.32f,.38f,.85f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.29f,.53f,.36f,.95f);
        colors.pressedColor=new(.21f,.39f,.27f);colors.fadeDuration=0;button.colors=colors;GameplayHUDStyle.ButtonStates(button);button.navigation=new Navigation{mode=Navigation.Mode.None};
        rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        var text=Text(rect,label,25,sprite!=null?.26f:.03f,.08f,.97f,.92f);text.alignment=TextAlignmentOptions.Midline;
        Art("Tab Image Slot",rect,sprite,.07f,.15f,.23f,.85f);return button;
    }
}
