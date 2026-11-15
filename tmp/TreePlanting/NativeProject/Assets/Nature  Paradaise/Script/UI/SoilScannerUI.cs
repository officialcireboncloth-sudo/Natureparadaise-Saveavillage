using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Mouse/touch scanner using live field IDs and the shared Terra transaction.</summary>
[DefaultExecutionOrder(-9400)]
public sealed class SoilScannerUI : MonoBehaviour
{
    public static SoilScannerUI Instance { get; private set; }
    public SoilScannerMachine Machine { get; private set; }
    public int SelectedFieldNumber { get; private set; }
    public ItemSO SelectedTerra { get; private set; }
    public bool IsBagOpen => bagOverlay != null && bagOverlay.activeSelf;
    readonly List<FieldArea> fields = new();
    readonly List<ItemSO> bagItems = new();
    readonly List<Button> fieldButtons = new();
    readonly List<TMP_Text> fieldLabels = new();
    readonly List<Image> fieldDots = new();
    readonly List<TMP_Text> bagLabels = new();
    readonly List<Button> bagButtons = new();
    readonly List<Image> bagIcons = new();
    readonly Vector2[] projectedCorners = new Vector2[4];
    Inventory inventory;
    PlayerController player;
    TimeManager clock;
    RectTransform map, viewArea, detailFrame, bagPanel, legendFrame, controlsFrame, header, itemSlot;
    TMP_Text farmName, itemMark;
    Image chosenIcon;
    MainMenuIcon chosenFallback;
    Material glassMaterial;
    GameObject keyboardHints;
    SoilScannerDroneView droneView;
    GameObject bagOverlay;
    TMP_Text title, condition, amount, process, itemName, hint, feedback, bagHint, controls;
    Image conditionDot, fill;
    Button useButton, itemButton;
    RectTransform fillRect;
    float priorScale, nextDiscovery;
    bool priorCursorVisible, closing, dirty;
    CursorLockMode priorCursorLock;
    int width, height;
    Vector2 ViewSize => GetComponent<Canvas>().pixelRect.size;
    bool Portrait => ViewSize.y > ViewSize.x;
    static readonly Color Poor = new(.88f, .35f, .33f), Medium = new(.91f, .70f, .27f), Good = new(.48f, .72f, .37f);

    public static bool Show(SoilScannerMachine machine, Inventory source)
    {
        if (Instance != null || machine == null || source == null) return false;
        var actor = source.GetComponent<PlayerController>();
        if (actor == null) return false;
        var ui = new GameObject("Soil Scanner UI", typeof(RectTransform)).AddComponent<SoilScannerUI>();
        Instance = ui; ui.Machine = machine; ui.inventory = source; ui.player = actor; ui.clock = TimeManager.Instance;
        ui.priorScale = Time.timeScale; ui.priorCursorVisible = Cursor.visible; ui.priorCursorLock = Cursor.lockState;
        ui.player.AcquireMovementLock(ui); ui.clock?.AcquirePause(ui); WorldInteractionPrompt.AcquireSuppression(ui);
        Time.timeScale = 0; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        try
        {
            ui.Build(); ui.RebindFields();
            ui.inventory.OnInventoryChanged += ui.MarkDirty;
            ui.Refresh(); GameplayInput.ConsumeCurrentFrame(); return true;
        }
        catch { ui.Close(); throw; }
    }

    void MarkDirty() => dirty = true;
    void FieldChanged(FieldArea _) => MarkDirty();
    void Update()
    {
        if (closing) return;
        if (Machine == null || !Machine.isActiveAndEnabled || inventory == null || player == null || LoadingScreen.IsVisible)
        { Close(); return; }
        if (width != (int)ViewSize.x || height != (int)ViewSize.y) { Layout(); dirty = true; }
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + .5f;
            if (fields.Count != CountFields()) dirty = true;
            foreach(var field in fields) if(field==null||!field.isActiveAndEnabled)dirty=true;
        }
        if (dirty) Refresh();
        if (!GameplayInput.ConsumedThisFrame && InputFocused())
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if (IsBagOpen) SetBagOpen(false); else Close(); return; }
            int step = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ? -1
                : Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ? 1
                : Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ? -1
                : Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0;
            if (step != 0)
            {
                if (IsBagOpen) CycleTerra(step); else CycleField(step);
                GameplayInput.ConsumeCurrentFrame();
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            { if (IsBagOpen) SetBagOpen(false); else TryUseSelected(); }
            if (Input.GetKeyDown(KeyCode.I) && !IsBagOpen) SetBagOpen(true);
        }
    }

    static bool InputFocused()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorWindow.focusedWindow != null && UnityEditor.EditorWindow.focusedWindow.GetType().Name == "GameView";
#else
        return Application.isFocused;
#endif
    }

    void RebindFields()
    {
        foreach (var field in fields) if (field != null) field.SoilChanged -= FieldChanged;
        SoilScannerService.GetFields(fields);
        foreach (var field in fields) field.SoilChanged += FieldChanged;
        if (SelectedFieldNumber == 0 && fields.Count > 0) SelectedFieldNumber = fields[0].FieldNumber;
        BuildMap();
    }

    public bool SelectField(int number)
    {
        if (!SoilScannerService.TryScan(number, out _, out var problem)) { feedback.text = problem; return false; }
        SelectedFieldNumber = number; SelectedTerra = null; feedback.text = ""; Refresh();
        GameplayInput.ConsumeCurrentFrame(); return true;
    }
    void CycleField(int step)
    {
        int current = fields.FindIndex(f => f.FieldNumber == SelectedFieldNumber);
        if (fields.Count > 0) SelectField(fields[(current + step + fields.Count) % fields.Count].FieldNumber);
    }
    public bool SelectTerra(ItemSO item)
    {
        if (item == null || !item.IsSoilRestorer || inventory.GetCount(item) < 1) return false;
        SelectedTerra = item; feedback.text = ""; Refresh(); GameplayInput.ConsumeCurrentFrame(); return true;
    }
    void CycleTerra(int step)
    {
        if (bagItems.Count == 0) return;
        int current = bagItems.IndexOf(SelectedTerra);
        SelectTerra(bagItems[(current + step + bagItems.Count) % bagItems.Count]);
    }
    public bool TryUseSelected()
    {
        if (closing || IsBagOpen) return false;
        var item = SelectedTerra;
        bool success = SoilScannerService.TryUse(SelectedFieldNumber, inventory, item, out var reason);
        feedback.text = success ? $"{item.itemName} digunakan. Petak {SelectedFieldNumber} mulai disuburkan." : reason;
        if (success) SelectedTerra = null;
        Refresh(); GameplayInput.ConsumeCurrentFrame(); return success;
    }
    public void SetBagOpen(bool value)
    {
        if (closing) return;
        bagOverlay.SetActive(value); Refresh(); GameplayInput.ConsumeCurrentFrame();
    }

    public void Close()
    {
        if (closing) return; closing = true;
        if (inventory != null) inventory.OnInventoryChanged -= MarkDirty;
        foreach (var field in fields) if (field != null) field.SoilChanged -= FieldChanged;
        player?.ReleaseMovementLock(this); clock?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
        if (droneView != null) droneView.Dispose();
        Time.timeScale = priorScale; Cursor.visible = priorCursorVisible; Cursor.lockState = priorCursorLock;
        if (Instance == this) Instance = null;
        EventSystem.current?.SetSelectedGameObject(null); GameplayInput.ConsumeCurrentFrame();
        gameObject.SetActive(false); Destroy(gameObject);
    }
    void OnDisable() { if (Instance == this) Close(); }
    void OnDestroy() { if (Instance == this) Close(); if(glassMaterial!=null)Destroy(glassMaterial); }

    static readonly Color Glass = new(.115f,.14f,.13f,.96f);
    static readonly Color Rim = new(.63f,.67f,.59f,.38f);
    static readonly Color SoftText = new(.77f,.81f,.76f);

    MainMenuRoundedImage GlassPanel(string name,Transform parent,float x,float y,float x2,float y2)
    {
        var panel=Panel(name,parent,Glass,x,y,x2,y2);
        panel.radius=12;panel.borderColor=Rim;panel.borderWidth=1;
        if(glassMaterial!=null)panel.material=glassMaterial;
        return panel;
    }
    static Image Dot(string name,Transform parent,Color color,Vector2 anchor,float diameter=10)
    {
        var image=Panel(name,parent,color,anchor.x,anchor.y,anchor.x,anchor.y);
        image.rectTransform.sizeDelta=new Vector2(diameter,diameter);image.radius=diameter;
        return image;
    }
    static void Divider(Transform parent,float y)
    {
        var line=Panel("Divider",parent,new(.76f,.80f,.73f,.46f),.07f,y,.93f,y);
        line.rectTransform.sizeDelta=new Vector2(0,1);
    }
    void Build()
    {
        var shader=Resources.Load<Shader>("UI/SoilScannerGlass");
        if(shader!=null&&shader.isSupported)glassMaterial=new Material(shader){name="Scanner Glass Runtime",hideFlags=HideFlags.DontSave};
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=720;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new(1280,720);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        if(EventSystem.current==null)new GameObject("Scanner Event System",typeof(EventSystem),typeof(StandaloneInputModule));
        Panel("Backdrop",transform,new(.10f,.15f,.11f,1),0,0,1,1).raycastTarget=true;
        map=R("Fields",transform,0,0,1,1);
        var safe=R("Safe Area",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        viewArea=R("Unobstructed Farm Area",safe,.03f,.14f,.735f,.89f);
        header=GlassPanel("Header",safe,.025f,.92f,.985f,.98f).rectTransform;
        GameplayHUDStyle.Icon(header,MainMenuIcon.Kind.Leaf,new(.61f,.73f,.44f),new(.015f,.20f),new(.037f,.80f));
        T("Title",header,"PEMINDAI TANAH",21,.048f,.10f,.70f,.90f).characterSpacing=1.4f;
        farmName=T("Farm Name",header,"Ladang Utama",16,.75f,.12f,.94f,.88f);farmName.alignment=TextAlignmentOptions.Right;
        var close=B("Close",header,"",.955f,.08f,.991f,.92f,Close);
        var closeSurface=close.GetComponent<MainMenuRoundedImage>();closeSurface.color=new(1,1,1,.035f);
        GameplayHUDStyle.Icon(close.transform,MainMenuIcon.Kind.Close,GameplayHUDStyle.TextColor,new(.25f,.22f),new(.75f,.78f));
        detailFrame=GlassPanel("Selected Field",safe,.755f,.24f,.985f,.875f).rectTransform;
        detailFrame.GetComponent<Image>().raycastTarget=true;
        title=T("Field Name",detailFrame,"",25,.075f,.895f,.925f,.97f);
        conditionDot=Dot("Condition Dot",detailFrame,Good,new(.09f,.845f),11);
        condition=T("Condition",detailFrame,"",16,.145f,.805f,.93f,.88f);condition.color=SoftText;
        Divider(detailFrame,.785f);
        T("Fertility Heading",detailFrame,"Kesuburan",17,.075f,.705f,.59f,.765f);
        amount=T("Fertility Value",detailFrame,"",18,.60f,.705f,.925f,.765f);amount.alignment=TextAlignmentOptions.Right;
        var track=GlassPanel("Fertility Track",detailFrame,.07f,.635f,.93f,.675f).rectTransform;
        track.GetComponent<Image>().color=new(.66f,.71f,.69f,.19f);
        fill=Panel("Fertility",track,Good,0,0,1,1);fillRect=fill.rectTransform;
        process=T("Process",detailFrame,"",16,.075f,.49f,.925f,.605f);process.textWrappingMode=TextWrappingModes.Normal;process.lineSpacing=5;process.color=SoftText;
        Divider(detailFrame,.46f);
        T("Restorer Heading",detailFrame,"Penyubur Tanah",18,.075f,.365f,.925f,.43f);
        itemButton=B("Choose Terra",detailFrame,"",.07f,.19f,.93f,.35f,()=>SetBagOpen(true));
        itemButton.GetComponent<MainMenuRoundedImage>().color=Color.clear;
        var slot=GlassPanel("Terra Slot",itemButton.transform,.02f,.03f,.32f,.97f);slot.borderColor=GameplayHUDStyle.Accent;
        itemSlot=slot.rectTransform;
        itemMark=T("Slot Mark",itemSlot,"+",30,.02f,.02f,.98f,.98f);itemMark.alignment=TextAlignmentOptions.Center;
        chosenIcon=R("Chosen Item Icon",itemSlot,.12f,.12f,.88f,.88f).gameObject.AddComponent<Image>();
        chosenIcon.preserveAspect=true;chosenIcon.raycastTarget=false;chosenIcon.enabled=false;
        chosenFallback=R("Terra Leaf",itemSlot,.21f,.21f,.79f,.79f).gameObject.AddComponent<MainMenuIcon>();
        chosenFallback.kind=MainMenuIcon.Kind.Sprout;chosenFallback.color=GameplayHUDStyle.Accent;
        chosenFallback.raycastTarget=false;chosenFallback.enabled=false;
        itemName=T("Chosen Terra",itemButton.transform,"Pilih dari tas",17,.39f,.04f,.99f,.96f);
        useButton=B("Use Terra",detailFrame,"Gunakan",.07f,.055f,.93f,.155f,()=>TryUseSelected(),true);
        var useSurface=useButton.GetComponent<MainMenuRoundedImage>();useSurface.borderColor=new(.58f,.71f,.44f,.8f);useSurface.borderWidth=1;
        var actionLabel=useButton.GetComponentInChildren<TMP_Text>();
        actionLabel.fontSize=actionLabel.fontSizeMin=actionLabel.fontSizeMax=18;
        hint=T("Help",detailFrame,"",12,.07f,.005f,.93f,.05f);hint.color=SoftText;
        feedback=T("Feedback",safe,"",16,.03f,.09f,.73f,.135f);
        legendFrame=GlassPanel("Legend",safe,.025f,.035f,.28f,.08f).rectTransform;
        string[] names={"Bagus","Sedang","Buruk"};Color[] colors={Good,Medium,Poor};
        for(int i=0;i<3;i++)
        {
            float left=.04f+i*.33f;
            Dot("Legend Dot",legendFrame,colors[i],new(left+.018f,.5f),9);
            T("Legend Text",legendFrame,names[i],13,left+.065f,.12f,left+.30f,.88f);
        }
        controlsFrame=GlassPanel("Control Strip",safe,.335f,.035f,.735f,.08f).rectTransform;
        controls=T("Controls",controlsFrame,"",13,.025f,.1f,.975f,.9f);controls.alignment=TextAlignmentOptions.Center;
        keyboardHints=R("Keyboard Hints",controlsFrame,.025f,.14f,.975f,.86f).gameObject;
        string[] keys={"W","A","S","D"};
        for(int i=0;i<keys.Length;i++)Keycap(keyboardHints.transform,keys[i],i*.049f,i*.049f+.044f);
        T("Choose Hint",keyboardHints.transform,"Pilih petak",12,.21f,0,.40f,1).color=SoftText;
        Keycap(keyboardHints.transform,"Enter",.45f,.56f);
        T("Use Hint",keyboardHints.transform,"Gunakan",12,.58f,0,.77f,1).color=SoftText;
        Keycap(keyboardHints.transform,"Esc",.80f,.88f);
        T("Close Hint",keyboardHints.transform,"Tutup",12,.90f,0,1,1).color=SoftText;
        bagOverlay=R("Choose From Bag",safe,0,0,1,1).gameObject;
        Panel("Bag Scrim",bagOverlay.transform,new(0,0,0,.60f),0,0,1,1).raycastTarget=true;
        bagPanel=GlassPanel("Terra Picker",bagOverlay.transform,.18f,.13f,.82f,.87f).rectTransform;
        bagPanel.GetComponent<Image>().color=new(.13f,.18f,.16f,.98f);
        T("Bag Title",bagPanel,"PENYUBUR DI TAS",25,.04f,.86f,.78f,.97f).fontStyle=FontStyles.Bold;
        B("Back",bagPanel,"Kembali",.78f,.88f,.96f,.96f,()=>SetBagOpen(false));
        bagHint=T("Bag Hint",bagPanel,"",18,.04f,.75f,.96f,.85f);
        for(int i=0;i<4;i++)
        {
            int slotIndex=i;float top=.73f-i*.145f;
            var button=B("Terra "+i,bagPanel,"",.04f,top-.13f,.96f,top,()=>
            {if(slotIndex<bagItems.Count&&SelectTerra(bagItems[slotIndex]))SetBagOpen(false);});
            bagButtons.Add(button);
            var icon=R("Item Icon",button.transform,.025f,.12f,.13f,.88f).gameObject.AddComponent<Image>();
            icon.preserveAspect=true;icon.raycastTarget=false;bagIcons.Add(icon);
            bagLabels.Add(T("Name And Count",button.transform,"",21,.15f,.08f,.96f,.92f));
        }
        T("Terra Note",bagPanel,"Terra memulihkan seluruh petak ke 80. Petak terkunci selama proses.",17,.04f,.025f,.96f,.13f).color=SoftText;
        bagOverlay.SetActive(false);Layout();
    }

    void Layout()
    {
        width=(int)ViewSize.x;height=(int)ViewSize.y;
        SetRect(header,.025f,.92f,.985f,.98f);
        SetRect(viewArea,.03f,Portrait?.52f:.14f,Portrait?.97f:.735f,.89f);
        SetRect(detailFrame,Portrait?.025f:.755f,Portrait?.115f:.24f,.985f,Portrait?.49f:.875f);
        SetRect(bagPanel,Portrait?.04f:.18f,.13f,Portrait?.96f:.82f,.87f);
        SetRect(legendFrame,.025f,.035f,Portrait?.49f:.28f,.08f);
        SetRect(controlsFrame,Portrait?.515f:.335f,.035f,Portrait?.985f:.735f,.08f);
        SetRect(feedback.rectTransform,.03f,Portrait?.495f:.09f,Portrait?.97f:.73f,Portrait?.52f:.135f);
        SetRect(itemSlot,.02f,.03f,Portrait?.16f:.32f,.97f);
        SetRect(itemName.rectTransform,Portrait?.21f:.39f,.04f,.99f,.96f);
        farmName.gameObject.SetActive(!Portrait);
        SetRect(header.Find("Close").GetComponent<RectTransform>(),Portrait?.92f:.955f,.08f,.991f,.92f);
        bool touch=Application.isMobilePlatform||Portrait;
        controls.text=touch?"Ketuk petak untuk memeriksa":"";keyboardHints.SetActive(!touch);
        Canvas.ForceUpdateCanvases();if(fields.Count>0)BuildMap();
    }

    void BuildMap()
    {
        if (map == null) return;
        foreach (Transform child in map) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        fieldButtons.Clear(); fieldLabels.Clear(); fieldDots.Clear();
        var worldMap=R("Live Farm View",map,0,0,1,1);
        worldMap.gameObject.AddComponent<RectMask2D>();
        var live=R("Drone Image",worldMap,0,0,1,1).gameObject.AddComponent<RawImage>();
        live.raycastTarget=false;
        if (droneView == null)
        {
            droneView=gameObject.AddComponent<SoilScannerDroneView>();
            droneView.FrameChanged+=PositionFieldOverlays;
        }
        droneView.Initialize(live);
        var clearBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(map,viewArea);
        var viewport=map.rect;
        droneView.SetFrameArea(new Rect((clearBounds.min.x-viewport.xMin)/viewport.width,
            (clearBounds.min.y-viewport.yMin)/viewport.height,clearBounds.size.x/viewport.width,clearBounds.size.y/viewport.height));
        droneView.SetFields(fields);
        if (fields.Count==0) { live.color=Color.clear; return; }
        droneView.RenderNow();
        foreach (var f in fields)
        {
            int number=f.FieldNumber;
            var rect=R("Field "+number,worldMap,0,0,1,1);
            var surface=rect.gameObject.AddComponent<SoilScannerFieldGraphic>();
            surface.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=surface;
            button.navigation=new Navigation{mode=Navigation.Mode.None}; GameplayHUDStyle.ButtonStates(button);
            button.onClick.AddListener(()=>SelectField(number));
            var badgeImage=GlassPanel("Field Label",rect,.5f,.5f,.5f,.5f);
            badgeImage.color=new(.095f,.12f,.09f,.92f);badgeImage.radius=7;badgeImage.borderWidth=0;
            badgeImage.raycastTarget=true;
            var badge=badgeImage.rectTransform; badge.sizeDelta=new Vector2(112,47);
            fieldDots.Add(Dot("Status Dot",badge,Good,new(.13f,.70f),9));
            var label=T("Field Status",badge,"",16,.25f,.06f,.96f,.94f); label.alignment=TextAlignmentOptions.Left;
            fieldButtons.Add(button); fieldLabels.Add(label);
        }
        PositionFieldOverlays();
    }

    void PositionFieldOverlays()
    {
        if (droneView == null || droneView.ViewCamera == null) return;
        for(int i=0;i<fieldButtons.Count;i++)
        {
            var field=fields[i]; if(field==null)continue;
            var corners=projectedCorners;Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;
            for(int c=0;c<4;c++)
            {
                var world=field.transform.TransformPoint(new Vector3((c<2?-.5f:.5f)*field.Columns*field.CellSize,0,
                    (c==0||c==3?-.5f:.5f)*field.Rows*field.CellSize));
                corners[c]=droneView.Project(FieldGroundSurface.GroundPosition(world));min=Vector2.Min(min,corners[c]);max=Vector2.Max(max,corners[c]);
            }
            var rect=fieldButtons[i].GetComponent<RectTransform>();
            rect.anchorMin=min;rect.anchorMax=max;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.localRotation=Quaternion.identity;
            for(int c=0;c<4;c++)corners[c]=new Vector2((corners[c].x-min.x)/Mathf.Max(.001f,max.x-min.x),(corners[c].y-min.y)/Mathf.Max(.001f,max.y-min.y));
            rect.GetComponent<SoilScannerFieldGraphic>().SetFootprint(corners);
            var badge=(RectTransform)fieldLabels[i].transform.parent;
            Vector2 center=droneView.Project(FieldGroundSurface.GroundPosition(field.transform.position));
            var anchor=new Vector2((center.x-min.x)/Mathf.Max(.001f,max.x-min.x),(center.y-min.y)/Mathf.Max(.001f,max.y-min.y));
            badge.anchorMin=badge.anchorMax=anchor;badge.anchoredPosition=Vector2.zero;
        }
    }

    public void Refresh()
    {
        if (closing || inventory==null || title==null) return; dirty=false;
        bool same=fields.Count==CountFields();
        foreach(var field in fields) if(field==null||!field.isActiveAndEnabled) same=false;
        if(!same) RebindFields();
        bool valid=SoilScannerService.TryScan(SelectedFieldNumber,out var soil,out var error);
        if(fields.Count==0)feedback.text="Belum ada petak yang dapat dipindai.";
        for(int i=0;i<fieldButtons.Count;i++)
        {
            var state=fields[i].SoilSnapshot;
            var color=state.IsRestoring?new Color(.38f,.45f,.50f):ConditionColor(state.Condition);
            bool active=state.FieldNumber==SelectedFieldNumber;
            var surface=fieldButtons[i].GetComponent<SoilScannerFieldGraphic>(); surface.color=new Color(color.r,color.g,color.b,active?.065f:.012f);
            surface.borderColor=new Color(.84f,.94f,.66f,1);
            surface.borderWidth=active?2:0; surface.SetVerticesDirty();
            fieldDots[i].color=color;
            fieldLabels[i].text=$"Petak {state.FieldNumber}\n<size=85%>{(state.IsRestoring?state.RemainingDays+" hari":state.ConditionName)}</size>";
            fieldButtons[i].interactable=FieldArea.ValidateSoilIdentities(out _);
        }
        if(SelectedTerra!=null&&inventory.GetCount(SelectedTerra)<1)SelectedTerra=null;
        title.text=valid?$"Petak {soil.FieldNumber}":"Pilih petak";
        condition.text=valid?(soil.IsRestoring?"Dalam Penyuburan":"Kondisi "+soil.ConditionName.ToLowerInvariant()):error;
        amount.text=valid?$"{soil.Fertility} / 80":"- / 80";
        fillRect.anchorMax=new(valid?soil.Fertility/80f:0,1);
        fill.color=valid?ConditionColor(soil.Condition):GameplayHUDStyle.Muted;
        conditionDot.color=valid&&soil.IsRestoring?new Color(.51f,.60f,.62f):fill.color;
        process.text=!valid?"Pilih petak pada ladang.":soil.IsRestoring?$"Petak terkunci · {soil.RemainingDays} hari lagi\nPulih ke 80 / 80":soil.Fertility>=80?
            "Tanah siap digunakan.\nKesuburan sudah maksimal.":"Tambahkan penyubur untuk memulihkan tanah.";
        itemName.text=SelectedTerra!=null?$"{SelectedTerra.itemName}  x{inventory.GetCount(SelectedTerra)}\n{SelectedTerra.soilRestorationDays} hari game":"Pilih dari tas";
        chosenIcon.sprite=SelectedTerra!=null?(SelectedTerra.inventoryIllustration!=null?SelectedTerra.inventoryIllustration:SelectedTerra.icon):null;
        chosenIcon.enabled=chosenIcon.sprite!=null;
        chosenFallback.enabled=SelectedTerra!=null&&!chosenIcon.enabled;
        itemMark.gameObject.SetActive(SelectedTerra==null);
        bool canUse=false; string reason=null;
        if(valid&&FieldArea.TryGetByNumber(SelectedFieldNumber,out var selected))canUse=selected.CanBeginSoilRestoration(out reason);
        itemButton.interactable=valid&&!soil.IsRestoring&&soil.Fertility<80;
        useButton.interactable=canUse&&SelectedTerra!=null;
        useButton.GetComponent<MainMenuRoundedImage>().color=useButton.interactable?new Color(.36f,.46f,.28f,1):new Color(.29f,.35f,.26f,.85f);
        useButton.GetComponentInChildren<TMP_Text>().color=useButton.interactable?new Color(.95f,.96f,.90f):new Color(.63f,.68f,.60f);
        hint.text=valid&&!canUse&&soil.Fertility<80&&!soil.IsRestoring?reason:"";
        if(IsBagOpen)RefreshBag();
    }
    int CountFields() { int n=0; foreach(var field in FieldArea.ActiveAreas)if(field!=null&&field.UsesFieldSoil)n++; return n; }
    void RefreshBag()
    {
        bagItems.Clear();
        foreach(var slot in inventory.slots)
            if(slot?.item!=null&&slot.count>0&&slot.item.IsSoilRestorer&&!bagItems.Contains(slot.item))bagItems.Add(slot.item);
        bagItems.Sort((a,b)=>b.soilRestorationDays.CompareTo(a.soilRestorationDays));
        bagHint.text=bagItems.Count==0?"Tidak ada Terra di tas. Bawa penyubur tanah terlebih dahulu.":"Pilih satu penyubur untuk field yang sedang diperiksa.";
        for(int i=0;i<bagButtons.Count;i++)
        {
            bool exists=i<bagItems.Count; bagButtons[i].gameObject.SetActive(exists);
            if(!exists)continue;
            var item=bagItems[i];bagLabels[i].text=$"{item.itemName}     x{inventory.GetCount(item)}\n{item.soilRestorationDays} hari game  /  Pulih ke 80";
            bagIcons[i].sprite=item.inventoryIllustration!=null?item.inventoryIllustration:item.icon;bagIcons[i].enabled=bagIcons[i].sprite!=null;
        }
    }

    static Color ConditionColor(FieldSoilCondition value)=>value==FieldSoilCondition.Good?Good:value==FieldSoilCondition.Medium?Medium:Poor;
    static void Keycap(Transform parent,string key,float x,float x2)
    {
        var surface=Panel("Key "+key,parent,new(.13f,.16f,.14f,.8f),x,0,x2,1);
        surface.radius=3;surface.borderWidth=.7f;surface.borderColor=new(.62f,.66f,.60f,.7f);
        var label=T("Key",surface.transform,key,11,.03f,.05f,.97f,.95f);label.alignment=TextAlignmentOptions.Center;
    }
    static RectTransform R(string name,Transform parent,float x,float y,float x2,float y2)=>GameplayHUDStyle.Rect(name,parent,new(x,y),new(x2,y2));
    static void SetRect(RectTransform rect,float x,float y,float x2,float y2){rect.anchorMin=new(x,y);rect.anchorMax=new(x2,y2);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static MainMenuRoundedImage Panel(string name,Transform parent,Color color,float x,float y,float x2,float y2)
    {var image=GameplayHUDStyle.Surface(R(name,parent,x,y,x2,y2),color,10);image.raycastTarget=false;return image;}
    static TMP_Text T(string name,Transform parent,string value,float size,float x,float y,float x2,float y2)=>GameplayHUDStyle.Text(name,parent,value,size,new(x,y),new(x2,y2));
    static Button B(string name,Transform parent,string value,float x,float y,float x2,float y2,UnityEngine.Events.UnityAction action,bool primary=false)
    {
        var surface=Panel(name,parent,primary?new Color(.36f,.52f,.38f):GameplayHUDStyle.Card,x,y,x2,y2);surface.raycastTarget=true;
        var button=surface.gameObject.AddComponent<Button>();button.targetGraphic=surface;button.navigation=new Navigation{mode=Navigation.Mode.None};GameplayHUDStyle.ButtonStates(button);
        button.onClick.AddListener(action);if(value.Length>0)T("Label",surface.transform,value,22,.03f,.05f,.97f,.95f).alignment=TextAlignmentOptions.Center;return button;
    }
}
