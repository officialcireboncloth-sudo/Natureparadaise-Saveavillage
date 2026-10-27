using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared PC/touch companion commands, using the existing companion state and riding system.</summary>
[DisallowMultipleComponent]
public sealed class CompanionCommandPanel : MonoBehaviour
{
    static CompanionCommandPanel instance;
    public static bool IsOpen => instance != null && instance.open;
    PersonalAnimal companion;
    PlayerController player;
    CompanionCommandTheme theme;
    bool ownsTheme, open, oldCursor;
    CursorLockMode oldLock;
    GameObject canvasObject, oldSelection;
    RectTransform safe, panel, heartFill;
    TMP_Text title, status, affection, footer, closeGuide;
    Button mountButton;
    readonly List<Button> commands = new();
    readonly List<MainMenuRoundedImage> commandSurfaces = new();
    readonly List<GameObject> indicators = new();
    HUDManager hud;
    GameplayPauseMenu pauseMenu;
    Canvas mapCanvas;
    bool mapWasVisible;
    int openedFrame;

    /// <summary>Opens the selected tame companion; the same panel is used by world mouse, keyboard and touch prompts.</summary>
    public static void Show(PersonalAnimal selected, PlayerController selectedPlayer)
    {
        if (selected == null || !selected.isActiveAndEnabled || !selected.IsTamed || selected.IsMounted || selectedPlayer == null ||
            instance != null || WorldInteractionPrompt.IsSuppressed || GameplayPauseMenu.BlocksGameplayInput || selectedPlayer.IsMovementLocked) return;
        instance = new GameObject("CompanionCommandPanel_Runtime").AddComponent<CompanionCommandPanel>();
        instance.Open(selected, selectedPlayer);
    }
    void Open(PersonalAnimal selected, PlayerController selectedPlayer)
    {
        companion = selected; player = selectedPlayer;
        theme = Resources.Load<CompanionCommandTheme>("UI/CompanionCommandTheme");
        if (theme == null) { theme = ScriptableObject.CreateInstance<CompanionCommandTheme>(); ownsTheme = true; }
        open = true; openedFrame = Time.frameCount;
        oldCursor = Cursor.visible; oldLock = Cursor.lockState;
        oldSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        player.AcquireMovementLock(this); TimeManager.Instance?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this);
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        Build(); Refresh(); Fit();
        hud = FindFirstObjectByType<HUDManager>(); hud?.SetModalContextVisible(true);
        pauseMenu = FindFirstObjectByType<GameplayPauseMenu>(); pauseMenu?.SetModalContextVisible(true);
        mapCanvas = FindFirstObjectByType<WorldMapUI>()?.GetComponent<Canvas>();
        if (mapCanvas != null) { mapWasVisible = mapCanvas.enabled; mapCanvas.enabled = false; }
        GameplayInput.ConsumeCurrentFrame();
    }
    void Build()
    {
        canvasObject = new GameObject("CompanionCommandsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if(EventSystem.current==null)new GameObject("EventSystem_Companion",typeof(EventSystem),typeof(StandaloneInputModule));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 470;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        var backdrop = GameplayHUDStyle.Rect("Input Shield", canvasObject.transform, Vector2.zero, Vector2.one);
        var shield = backdrop.gameObject.AddComponent<Image>(); shield.color = new Color(0,0,0,.08f); shield.raycastTarget = true;
        safe = GameplayHUDStyle.Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one); safe.gameObject.AddComponent<SafeAreaFitter>();
        bool horse = companion.Species == CompanionSpecies.Horse;
        panel = Fixed("Companion Panel", safe, new Vector2(0,.5f), Vector2.zero, new Vector2(560,horse?760:664));
        panel.pivot = new Vector2(0,.5f);
        var surface = GameplayHUDStyle.Surface(panel, theme.panel, 24); surface.borderColor = theme.border; surface.borderWidth = 2; surface.raycastTarget = true;
        Sprite speciesArt = horse ? theme.horseIcon : companion.Species == CompanionSpecies.Cat ? theme.catIcon : theme.dogIcon;
        Icon("Species Icon", panel, speciesArt, horse?MainMenuIcon.Kind.Horse:companion.Species==CompanionSpecies.Cat?MainMenuIcon.Kind.Cat:MainMenuIcon.Kind.Dog, new Vector2(30,-30),new Vector2(66,66));
        title = Label("Name", panel, "", 36, new Vector2(116,-30),new Vector2(350,50),true);
        status = Label("Status", panel, "", 22, new Vector2(116,-85),new Vector2(410,32)); status.color = theme.muted;
        var close = MakeButton("Close", panel, new Vector2(484,-20), new Vector2(56,56), new Color(0,0,0,0), Close);
        Icon("Close Icon", close.transform, null, MainMenuIcon.Kind.Close, new Vector2(10,-10),new Vector2(36,36));
        Divider(-140);
        var heart = Icon("Heart", panel, null, MainMenuIcon.Kind.Heart,new Vector2(32,-164),new Vector2(30,30));heart.color = theme.affection;
        Label("Affection Label", panel, "Kedekatan", 26,new Vector2(78,-162),new Vector2(300,38),true);
        var track = Fixed("Affection Track",panel,new Vector2(0,1),new Vector2(32,-218),new Vector2(328,20));GameplayHUDStyle.Surface(track,new Color(.48f,.53f,.42f,.55f),10);
        heartFill = GameplayHUDStyle.Rect("Affection Fill",track,Vector2.zero,Vector2.one);GameplayHUDStyle.Surface(heartFill,theme.affection,10);
        affection = Label("Affection Value",panel,"",22,new Vector2(380,-207),new Vector2(148,38));affection.alignment=TextAlignmentOptions.MidlineRight;
        AddCommand("Follow", "Ikuti Saya", CompanionCommand.Follow, theme.followIcon, MainMenuIcon.Kind.Footsteps,0);
        AddCommand("Stay", "Tunggu", CompanionCommand.Stay, theme.stayIcon, MainMenuIcon.Kind.Hand,1);
        AddCommand("Home", horse?"Pulang ke Kandang":"Pulang ke Rumah", CompanionCommand.GoHome, theme.homeIcon, MainMenuIcon.Kind.Home,2);
        if(horse)
        {
            mountButton = MakeButton("Mount",panel,new Vector2(30,-558),new Vector2(500,84),theme.mountRow,Mount);
            Icon("Mount Icon",mountButton.transform,theme.mountIcon,MainMenuIcon.Kind.Saddle,new Vector2(26,-18),new Vector2(48,48));
            Label("Mount Label",mountButton.transform,"Naik Kuda",27,new Vector2(104,-18),new Vector2(350,48),true);
        }
        float bottom = horse?668:572; Divider(-bottom);
        footer = Label("Feedback",panel,"",18,new Vector2(32,-bottom-10),new Vector2(496,26));footer.color=theme.muted;footer.alignment=TextAlignmentOptions.Center;
        var done=MakeButton("Close Footer",panel,new Vector2(170,-bottom-39),new Vector2(220,48),new Color(0,0,0,0),Close);
        closeGuide=Label("Close Guide",done.transform,"",22,new Vector2(0,0),new Vector2(220,48));closeGuide.alignment=TextAlignmentOptions.Center;
        SetNavigation();
    }
    void AddCommand(string name,string caption,CompanionCommand command,Sprite artwork,MainMenuIcon.Kind kind,int index)
    {
        var button=MakeButton(name,panel,new Vector2(30,-270-index*96),new Vector2(500,84),theme.row,()=>Issue(command));
        Icon(name+" Icon",button.transform,artwork,kind,new Vector2(26,-18),new Vector2(48,48));
        Label(name+" Label",button.transform,caption,26,new Vector2(104,-18),new Vector2(340,48),true);
        var dot=Fixed("Active Command",button.transform,new Vector2(0,1),new Vector2(452,-32),new Vector2(20,20));GameplayHUDStyle.Surface(dot,theme.active,10);
        commands.Add(button);commandSurfaces.Add(button.GetComponent<MainMenuRoundedImage>());indicators.Add(dot.gameObject);
    }
    void SetNavigation()
    {
        var rows=new List<Button>(commands);if(mountButton!=null)rows.Add(mountButton);
        for(int i=0;i<rows.Count;i++)rows[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[(i+rows.Count-1)%rows.Count],selectOnDown=rows[(i+1)%rows.Count]};
    }
    void Issue(CompanionCommand command)
    {
        if(!open || companion==null || !companion.IsTamed)return;
        companion.SetCommand(command,player.transform);Close();
    }
    void Mount()
    {
        if(!open || companion==null || player==null)return;
        if(companion.TryMount(player)){Close();return;}
        footer.text="Belum bisa naik. Pastikan posisi dekat dan tangan bebas.";
    }
    void Update()
    {
        if(!open)return;
        if(companion==null || !companion.isActiveAndEnabled || !companion.IsTamed || player==null){Close();return;}
        if(Time.frameCount==openedFrame || GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)){Close();return;}
        if(EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==null &&
            (Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.Tab)))
            EventSystem.current.SetSelectedGameObject(commands[0].gameObject);
    }
    void LateUpdate(){if(open){Refresh();Fit();}}
    void Refresh()
    {
        title.text=LocalizedName(companion);
        status.text="Status: "+(companion.Command==CompanionCommand.Follow?"Mengikuti":companion.Command==CompanionCommand.GoHome?"Pulang":"Menunggu");
        int points=Mathf.Clamp(companion.HeartPoints,0,1000);affection.text=$"{points} / 1000";heartFill.anchorMax=new Vector2(points/1000f,1);
        for(int i=0;i<commands.Count;i++)
        {
            bool selected=companion.Command==(i==0?CompanionCommand.Follow:i==1?CompanionCommand.Stay:CompanionCommand.GoHome);
            commandSurfaces[i].color=selected?theme.selectedRow:theme.row;commandSurfaces[i].borderColor=selected?theme.border:new Color(.52f,.61f,.46f,.22f);commandSurfaces[i].borderWidth=1;
            indicators[i].SetActive(selected);
        }
        closeGuide.text=Application.isMobilePlatform?"Tutup":"Esc   Tutup";
    }
    static string LocalizedName(PersonalAnimal animal)
    {
        string name=animal.DisplayName;
        if(name=="Horse" || name=="Dog" || name=="Cat" || name=="Companion")
            return animal.Species==CompanionSpecies.Horse?"Kuda":animal.Species==CompanionSpecies.Dog?"Anjing":animal.Species==CompanionSpecies.Cat?"Kucing":"Teman Hewan";
        return name;
    }
    void Fit()
    {
        float fit=Mathf.Min(1,Mathf.Min((safe.rect.width-48)/560,(safe.rect.height-48)/panel.sizeDelta.y));
        panel.localScale=Vector3.one*Mathf.Max(.1f,fit);
        panel.anchoredPosition=new Vector2(safe.rect.width<900?24:72,0);
    }
    /// <summary>Releases only this panel's locks, allowing a successful horse mount to keep its own transition lock.</summary>
    public void Close()
    {
        if(!open)return;open=false;if(instance==this)instance=null;
        if(canvasObject!=null)canvasObject.SetActive(false);
        player?.ReleaseMovementLock(this);TimeManager.Instance?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
        Cursor.visible=oldCursor;Cursor.lockState=oldLock;
        hud?.SetModalContextVisible(false);pauseMenu?.SetModalContextVisible(false);
        if(mapCanvas!=null)mapCanvas.enabled=mapWasVisible;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(oldSelection);
        GameplayInput.ConsumeCurrentFrame();Destroy(gameObject);
    }
    void OnDisable()=>Close();
    void OnDestroy(){Close();if(canvasObject!=null)Destroy(canvasObject);if(ownsTheme&&theme!=null)Destroy(theme);if(instance==this)instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>instance=null;
    RectTransform Fixed(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
    {
        var rect=GameplayHUDStyle.Rect(name,parent,anchor,anchor);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    TMP_Text Label(string name,Transform parent,string value,float size,Vector2 position,Vector2 dimensions,bool bold=false)
    {
        var rect=Fixed(name,parent,new Vector2(0,1),position,dimensions);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=TMP_Settings.defaultFontAsset;label.text=value;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=size*.8f;label.fontSizeMax=size;
        label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;label.color=theme.text;label.alignment=TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;label.raycastTarget=false;return label;
    }
    Graphic Icon(string name,Transform parent,Sprite sprite,MainMenuIcon.Kind kind,Vector2 position,Vector2 size)
    {
        var rect=Fixed(name,parent,new Vector2(0,1),position,size);Graphic art;
        if(sprite!=null){var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;art=image;}
        else{var vector=rect.gameObject.AddComponent<MainMenuIcon>();vector.kind=kind;art=vector;}
        art.color=sprite!=null?Color.white:theme.text;art.raycastTarget=false;return art;
    }
    Button MakeButton(string name,Transform parent,Vector2 position,Vector2 size,Color color,UnityEngine.Events.UnityAction action)
    {
        var rect=Fixed(name,parent,new Vector2(0,1),position,size);var image=GameplayHUDStyle.Surface(rect,color,16);image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.18f,1.18f,1.12f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.82f,.91f,.76f);colors.fadeDuration=.1f;button.colors=colors;
        button.onClick.AddListener(action);return button;
    }
    void Divider(float y){var line=Fixed("Divider",panel,new Vector2(0,1),new Vector2(30,y),new Vector2(500,1));line.gameObject.AddComponent<Image>().color=new Color(.72f,.78f,.61f,.16f);line.GetComponent<Image>().raycastTarget=false;}
}
