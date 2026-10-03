using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(Inventory))]
public sealed class InventoryUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] KeyCode closeKey = KeyCode.Escape;
    [SerializeField] KeyCode sortKey = KeyCode.R;
    [Header("Optional image slots")]
    [SerializeField] InventoryTheme theme;
    [SerializeField] Sprite panelSprite;
    [SerializeField] Sprite slotSprite;
    [SerializeField] Sprite buttonSprite;
    readonly Vector2 slotSize = new(106, 86);
    Inventory inventory;
    PlayerController movement;
    FarmingTool farmingTool;
    SeedTool seedTool;
    InventoryHotbarUI hotbarUI;
    GameObject canvasObject, panel, dialog, dragGhost;
    RectTransform gridContent;
    TMP_Text capacityText, moneyText, detailName, detailBody, description, priceRange, emptyText, qualityText;
    Image detailArtwork;
    readonly List<Image> stars = new();
    readonly List<SlotView> slotViews = new();
    readonly List<SlotView> toolbarViews = new();
    readonly List<TMP_Text> filterLabels = new();
    readonly List<RectTransform> filterLines = new();
    int filter, selectedIndex = -1, draggedSlotIndex = -1;
    bool isOpen, farmingWasEnabled, seedWasEnabled;
    static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    public bool IsOpen => isOpen;
    static int closedFrame = -1;
    public static bool ConsumedCloseInputThisFrame => closedFrame == Time.frameCount;

    void Awake()
    {
        inventory = GetComponent<Inventory>(); movement = GetComponent<PlayerController>();
        farmingTool = GetComponent<FarmingTool>(); seedTool = GetComponent<SeedTool>();
        hotbarUI = GetComponent<InventoryHotbarUI>();
        if (hotbarUI == null) hotbarUI = gameObject.AddComponent<InventoryHotbarUI>();
        if (theme == null) theme = Resources.Load<InventoryTheme>("UI/InventoryTheme");
    }
    void OnEnable() { if (inventory != null) inventory.OnInventoryChanged += Refresh; }
    void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (isOpen) SetOpen(false);
    }
    void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
    void Start() { BuildUI(); Refresh(); }
    void Update()
    {
        // A pause overlay owns input while open, including inventory shortcuts.
        if (GameplayPauseMenu.BlocksGameplayInput || AnimalCarePanel.IsOpen || AnimalController.CurrentCareAction != null) return;
        if (gridContent != null)
        {
            var grid = gridContent.GetComponent<GridLayoutGroup>();
            float width = Mathf.Max(1, (gridContent.rect.width - grid.spacing.x * 4) / 5);
            grid.cellSize = new Vector2(width, width * slotSize.y / slotSize.x);
        }
        if (GameplayInput.GetKeyDown(toggleKey)) SetOpen(!isOpen);
        else if (isOpen && GameplayInput.GetKeyDown(closeKey))
        {
            if (dialog != null) CancelDiscard(); else SetOpen(false);
        }
        if (!isOpen) return;
        if (ScoreManager.Instance != null) moneyText.text = Format(ScoreManager.Instance.points) + " G";
        if (dialog != null) return;
        if (GameplayInput.GetKeyDown(sortKey)) SortByType();
        if (GameplayInput.GetKeyDown(KeyCode.Delete)) RequestDiscard();
        if (GameplayInput.GetKeyDown(KeyCode.LeftShift) || GameplayInput.GetKeyDown(KeyCode.RightShift)) SplitSelected();
        if (GameplayInput.GetKeyDown(KeyCode.E) && selectedIndex >= 0) OnSlotClicked(selectedIndex);
    }
    public void ToggleInventory() { if (!GameplayPauseMenu.BlocksGameplayInput && !AnimalCarePanel.IsOpen && AnimalController.CurrentCareAction == null) SetOpen(!isOpen); }
    public void SortByType()
    {
        if (dialog != null || GameplayPauseMenu.BlocksGameplayInput) return;
        inventory.SortByCategory(); selectedIndex = -1; Refresh();
    }
    void SetOpen(bool open)
    {
        if (isOpen == open && panel != null) return;
        if (panel == null) BuildUI();
        isOpen = open; panel.SetActive(open); EndSlotDrag(); CancelDiscard();
        if (open)
        {
            WorldInteractionPrompt.AcquireSuppression(this);
            farmingWasEnabled = farmingTool != null && farmingTool.enabled;
            seedWasEnabled = seedTool != null && seedTool.enabled;
            movement?.AcquireMovementLock(this);
            if (farmingTool != null) farmingTool.enabled = false;
            if (seedTool != null) seedTool.enabled = false;
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None; Refresh();
        }
        else
        {
            closedFrame = Time.frameCount;
            WorldInteractionPrompt.ReleaseSuppression(this); movement?.ReleaseMovementLock(this);
            if (farmingTool != null) farmingTool.enabled = farmingWasEnabled;
            if (seedTool != null) seedTool.enabled = seedWasEnabled;
        }
    }
    void BuildUI()
    {
        if (panel != null) return;
        canvasObject = new GameObject("InventoryCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = 5; canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 250;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        int uiSize = GameplayUISettings.Current.uiSize;
        scaler.referenceResolution /= uiSize == 0 ? .9f : uiSize == 2 ? 1.1f : 1f;
        var safe = GameplayHUDStyle.Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        panel = GameplayHUDStyle.Rect("InventoryOverlay", safe, Vector2.zero, Vector2.one).gameObject;
        var shade = panel.AddComponent<Image>(); shade.color = new Color(0,0,0,.48f);
        Artwork(panel.transform, "BackgroundImageSlot", theme != null ? theme.background : null, Vector2.zero, Vector2.one);
        var card = GameplayHUDStyle.Rect("InventoryPanel", panel.transform, new Vector2(.22f,.10f), new Vector2(.78f,.91f));
        Surface(card, panelSprite != null ? panelSprite : theme != null ? theme.panel : null, new Color(.20f,.29f,.31f,.92f));
        Label(card,"Title","INVENTORY",34,new Vector2(.065f,.91f),new Vector2(.55f,.975f)).fontStyle = FontStyles.Bold;
        capacityText = Label(card,"Capacity","",18,new Vector2(.065f,.865f),new Vector2(.55f,.915f));
        Artwork(card,"CoinImageSlot",theme != null ? theme.coinIcon : null,new Vector2(.81f,.927f),new Vector2(.85f,.97f));
        moneyText=Label(card,"Money","0 G",24,new Vector2(.85f,.91f),new Vector2(.95f,.975f));
        string[] labels={"Semua","Alat","Tanaman","Ikan","Material"};
        Sprite[] icons={theme?.allIcon,theme?.toolsIcon,theme?.plantsIcon,theme?.fishIcon,theme?.materialsIcon};
        for(int i=0;i<5;i++)
        {
            int category=i; float x=.065f+i*.118f;
            var b=ButtonAt(card,"Filter_"+labels[i],"",new Vector2(x,.815f),new Vector2(x+.115f,.865f));
            Artwork(b.transform,"CategoryImageSlot",icons[i],new Vector2(.03f,.22f),new Vector2(.22f,.78f));
            filterLabels.Add(Label(b.transform,"Label",labels[i],17,new Vector2(.27f,0),Vector2.one));
            b.onClick.AddListener(()=>{filter=category; EndSlotDrag(); Refresh(); gridContent.anchoredPosition=Vector2.zero;});
            var line=GameplayHUDStyle.Rect("ActiveUnderline",b.transform,Vector2.zero,new Vector2(1,.04f));
            var img=line.gameObject.AddComponent<Image>(); img.color=new Color(.56f,.82f,.55f); img.raycastTarget=false;
            filterLines.Add(line);
        }
        var viewport=GameplayHUDStyle.Rect("GridViewport",card,new Vector2(.065f,.145f),new Vector2(.615f,.795f));
        viewport.gameObject.AddComponent<Image>().color=Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
        gridContent=GameplayHUDStyle.Rect("GridContent",viewport,new Vector2(0,1),Vector2.one);
        gridContent.pivot=new Vector2(.5f,1); scroll.content=gridContent; scroll.viewport=viewport;
        var grid=gridContent.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize=slotSize; grid.spacing=new Vector2(9,9);
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=5;
        // Match the screenshot's five-column layout without changing the save's slot indexes.
        var fitter=gridContent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        for(int i=0;i<inventory.Capacity;i++)
        {
            int index=i; var b=ButtonAt(gridContent,"Slot_"+i,"",Vector2.zero,Vector2.one,slotSprite != null ? slotSprite : theme?.slot);
            b.onClick.AddListener(()=>OnSlotClicked(index)); b.gameObject.AddComponent<InventorySlotDragHandler>().Initialize(this,index);
            var icon=Artwork(b.transform,"ItemImageSlot",null,new Vector2(.13f,.13f),new Vector2(.87f,.94f));
            var name=Label(b.transform,"MissingImageName","",13,new Vector2(.07f,.18f),new Vector2(.93f,.88f));
            name.alignment=TextAlignmentOptions.Center; name.textWrappingMode=TextWrappingModes.Normal;
            var count=Label(b.transform,"Count","",17,new Vector2(.42f,.025f),new Vector2(.94f,.27f)); count.alignment=TextAlignmentOptions.BottomRight;
            var key=Label(b.transform,"HotbarKey","",11,new Vector2(.06f,.79f),new Vector2(.34f,.97f));
            var outline=GameplayHUDStyle.Rect("SelectionOutline",b.transform,Vector2.zero,Vector2.one);
            var mint=new Color(.60f,.89f,.59f);
            Line(outline,Vector2.zero,new Vector2(1,.022f),mint);
            Line(outline,new Vector2(0,.978f),Vector2.one,mint);
            Line(outline,Vector2.zero,new Vector2(.018f,1),mint);
            Line(outline,new Vector2(.982f,0),Vector2.one,mint);
            slotViews.Add(new SlotView{Root=b.gameObject,Icon=icon,Name=name,Count=count,Key=key,Selection=outline.gameObject});
        }
        emptyText=Label(viewport,"NoResults","Tidak ada item dalam kategori ini.",18,Vector2.zero,Vector2.one); emptyText.alignment=TextAlignmentOptions.Center;
        Line(card,new Vector2(.645f,.145f),new Vector2(.646f,.795f));
        detailArtwork=Artwork(card,"SelectedItemIllustrationSlot",null,new Vector2(.68f,.55f),new Vector2(.95f,.85f));
        detailName=Label(card,"ItemName","Pilih item",32,new Vector2(.68f,.47f),new Vector2(.95f,.54f));
        qualityText=Label(card,"QualityLabel","Kualitas",18,new Vector2(.68f,.425f),new Vector2(.95f,.47f));
        for(int i=0;i<5;i++) stars.Add(Artwork(card,"QualityStarImageSlot_"+i,null,new Vector2(.77f+i*.033f,.437f),new Vector2(.797f+i*.033f,.465f)));
        Line(card,new Vector2(.68f,.415f),new Vector2(.95f,.416f));
        detailBody=Label(card,"ItemDetails","",19,new Vector2(.68f,.235f),new Vector2(.95f,.41f));
        detailBody.textWrappingMode=TextWrappingModes.Normal;
        description=Label(card,"Description","",17,new Vector2(.68f,.15f),new Vector2(.95f,.23f)); description.textWrappingMode=TextWrappingModes.Normal;
        priceRange=Label(card,"QualityPriceRange","",15,new Vector2(.68f,.11f),new Vector2(.95f,.15f));
        var trash=ButtonAt(card,"Discard","",new Vector2(.06f,.03f),new Vector2(.14f,.12f)); trash.onClick.AddListener(RequestDiscard);
        Artwork(trash.transform,"TrashImageSlot",theme?.trashIcon,new Vector2(.25f,.2f),new Vector2(.75f,.8f));
        if(theme?.trashIcon==null) Label(trash.transform,"Label","Buang",15,Vector2.zero,Vector2.one).alignment=TextAlignmentOptions.Center;
        var sort=ButtonAt(card,"Sort","Urutkan [R]",new Vector2(.16f,.035f),new Vector2(.30f,.085f)); sort.onClick.AddListener(SortByType);
        var split=ButtonAt(card,"Split","Bagi [Shift]",new Vector2(.32f,.035f),new Vector2(.47f,.085f)); split.onClick.AddListener(SplitSelected);
        var move=ButtonAt(card,"Move","Pindah [Klik Kanan]",new Vector2(.49f,.035f),new Vector2(.73f,.085f)); move.onClick.AddListener(()=>MoveSlot(selectedIndex));
        var close=ButtonAt(card,"Close","Tutup [Tab / Esc]",new Vector2(.75f,.035f),new Vector2(.96f,.085f)); close.onClick.AddListener(()=>SetOpen(false));
        BuildInventoryToolbar(panel.transform);
        panel.SetActive(isOpen);
    }
    void BuildInventoryToolbar(Transform parent)
    {
        var toolbar=GameplayHUDStyle.Rect("InventoryToolbar",parent,new Vector2(.30f,.012f),new Vector2(.70f,.094f));
        Surface(toolbar,theme?.panel,new Color(.16f,.25f,.29f,.98f));
        Label(toolbar,"ToolbarHint","TOOLBAR · Seret item ke nomor slot",15,new Vector2(.015f,.79f),new Vector2(.985f,.99f));
        int count=inventory.HotbarSlotCount;
        for(int i=0;i<count;i++)
        {
            int index=i;float left=.015f+i*.97f/count;
            var button=ButtonAt(toolbar,"ToolbarSlot_"+(i+1),"",new Vector2(left,.055f),new Vector2(left+.97f/count-.006f,.77f),slotSprite!=null?slotSprite:theme?.slot);
            button.onClick.AddListener(()=>OnSlotClicked(index));
            button.gameObject.AddComponent<InventorySlotDragHandler>().Initialize(this,index);
            var icon=Artwork(button.transform,"ItemImageSlot",null,new Vector2(.16f,.10f),new Vector2(.84f,.86f));
            var name=Label(button.transform,"MissingImageName","",12,new Vector2(.06f,.14f),new Vector2(.94f,.76f));name.alignment=TextAlignmentOptions.Center;
            var amount=Label(button.transform,"Count","",16,new Vector2(.45f,.02f),new Vector2(.94f,.28f));amount.alignment=TextAlignmentOptions.BottomRight;
            var key=Label(button.transform,"HotbarKey",(i+1).ToString(),13,new Vector2(.06f,.76f),new Vector2(.35f,.99f));
            var outline=GameplayHUDStyle.Rect("SelectionOutline",button.transform,Vector2.zero,Vector2.one);
            var tint=new Color(.43f,.86f,.89f);
            Line(outline,Vector2.zero,new Vector2(1,.025f),tint);Line(outline,new Vector2(0,.975f),Vector2.one,tint);
            Line(outline,Vector2.zero,new Vector2(.02f,1),tint);Line(outline,new Vector2(.98f,0),Vector2.one,tint);
            toolbarViews.Add(new SlotView{Root=button.gameObject,Icon=icon,Name=name,Count=amount,Key=key,Selection=outline.gameObject});
        }
    }
    void RefreshInventoryToolbar()
    {
        for(int i=0;i<toolbarViews.Count;i++)
        {
            var view=toolbarViews[i];var stack=inventory.GetSlot(i);bool valid=stack?.item!=null&&stack.count>0;
            view.Icon.sprite=valid?stack.item.icon:null;view.Icon.enabled=view.Icon.sprite!=null;
            view.Name.text=valid&&stack.item.icon==null?stack.item.itemName:"";
            view.Count.text=valid&&stack.count>1?stack.count.ToString():"";
            view.Selection.SetActive(i==(hotbarUI!=null?hotbarUI.SelectedIndex:selectedIndex));
        }
    }
    void Refresh()
    {
        if(inventory==null) return;
        if(panel==null || slotViews.Count!=inventory.Capacity) RebuildUI();
        capacityText.text=$"Tas {inventory.UsedSlots} / {inventory.Capacity}";
        moneyText.text=Format(ScoreManager.Instance != null ? ScoreManager.Instance.points : 0)+" G";
        int visible=0;
        for(int i=0;i<slotViews.Count;i++)
        {
            var v=slotViews[i]; var s=inventory.GetSlot(i); bool valid=s?.item!=null && s.count>0;
            bool show=filter==0 || (valid && Matches(s.item)); v.Root.SetActive(show); if(show) visible++;
            v.Icon.sprite=valid?s.item.icon:null; v.Icon.enabled=v.Icon.sprite!=null;
            v.Name.text=valid && s.item.icon==null?s.item.itemName:"";
            v.Count.text=valid && s.count>1?s.count.ToString():"";
            v.Key.text=i<inventory.HotbarSlotCount?$"{i+1}":"";
            v.Selection.SetActive(i==selectedIndex);
        }
        RefreshInventoryToolbar();
        emptyText.gameObject.SetActive(visible==0);
        for(int i=0;i<filterLabels.Count;i++)
        {
            filterLabels[i].fontStyle=i==filter?FontStyles.Bold:FontStyles.Normal;
            filterLines[i].gameObject.SetActive(i==filter);
        }
        RefreshDetail();
    }
    bool Matches(ItemSO item)=>filter switch
    {
        1=>item.category==ItemCategory.Tool || item.category==ItemCategory.Weapon,
        2=>item.IsSeed || item.refrigeratorCategory==RefrigeratorCategory.Crop || item.refrigeratorCategory==RefrigeratorCategory.Fruit,
        3=>item.category==ItemCategory.Fish || item.category==ItemCategory.Bait,
        4=>item.category==ItemCategory.Material,
        _=>true
    };
    void RefreshDetail()
    {
        var s=inventory.GetSlot(selectedIndex); bool valid=s?.item!=null && s.count>0;
        detailArtwork.sprite=valid?(s.item.inventoryIllustration!=null?s.item.inventoryIllustration:s.item.icon):null;
        detailArtwork.enabled=detailArtwork.sprite!=null; detailName.text=valid?s.item.itemName:"Pilih item";
        for(int i=0;i<stars.Count;i++)
        {
            stars[i].sprite=valid?(i<s.qualityStars?theme?.filledStar:theme?.emptyStar):null; stars[i].enabled=stars[i].sprite!=null;
        }
        qualityText.text=valid && (theme?.filledStar==null || theme?.emptyStar==null)?$"Kualitas: {s.qualityStars} / 5":"Kualitas";
        if(!valid){qualityText.text="";detailBody.text="Klik slot untuk melihat detail."; description.text="";priceRange.text="";return;}
        int price=s.item.GetMarketSellPrice(s.qualityStars,s.fishSizeCm);
        string sell=s.item.CanSellAtMarket?$"{Format(price)} G / buah":"Tidak dapat dijual";
        detailBody.text=$"Harga jual\n{sell}\n\nJumlah: {s.count}\nTotal: {Format((long)price*s.count)} G";
        description.text=s.item.inventoryDescription;
        if(s.item.category==ItemCategory.Fish) description.text+=(string.IsNullOrEmpty(description.text)?"":"\n")+$"Ukuran: {s.fishSizeCm:0.#} cm";
        priceRange.text=s.item.CanSellAtMarket?$"0/5: {Format(s.item.GetMarketSellPrice(0,s.fishSizeCm))} G   •   5/5: {Format(s.item.GetMarketSellPrice(5,s.fishSizeCm))} G":"";
    }
    void RebuildUI()
    {
        EndSlotDrag(); CancelDiscard(); if(canvasObject!=null) {canvasObject.SetActive(false);Destroy(canvasObject);}
        panel=null; canvasObject=null; slotViews.Clear(); toolbarViews.Clear(); stars.Clear();filterLabels.Clear(); filterLines.Clear();BuildUI();
    }
    void OnSlotClicked(int index)
    {
        if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput) return;
        selectedIndex=index; if(index<inventory.HotbarSlotCount) hotbarUI?.SelectSlot(index); Refresh();
    }
    public void MoveSlot(int index)
    {
        if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput || inventory.GetSlot(index)?.item==null) return;
        int start=index<inventory.HotbarSlotCount?inventory.HotbarSlotCount:0;
        int end=index<inventory.HotbarSlotCount?inventory.Capacity:inventory.HotbarSlotCount;
        for(int i=start;i<end;i++) if(inventory.GetSlot(i)?.item==null)
        {
            inventory.SwapSlots(index,i);selectedIndex=i;Refresh();return;
        }
        SaveLoadFeedback.Instance?.ShowMessage("Tidak ada slot kosong untuk dipindahkan.");
    }
    void SplitSelected()
    {
        if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput) return;
        var s=inventory.GetSlot(selectedIndex); if(s?.item==null || s.count<2) return;
        for(int i=0;i<inventory.Capacity;i++) if(inventory.GetSlot(i)?.item==null)
        {
            int amount=s.count/2;
            if(inventory.TrySetSlot(i,s.item,amount,s.qualityStars,s.fishSizeCm,s.fishWeightKg)) inventory.RemoveFromSlot(selectedIndex,amount);
            Refresh();return;
        }
        SaveLoadFeedback.Instance?.ShowMessage("Perlu slot kosong untuk membagi stack.");
    }
    void RequestDiscard()
    {
        if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput) return;
        var s=inventory.GetSlot(selectedIndex);if(s?.item==null || s.count<=0) return;
        if(s.item.isKeyItem || s.item.category==ItemCategory.Tool || s.item.category==ItemCategory.Quest)
        {SaveLoadFeedback.Instance?.ShowMessage("Item penting ini tidak bisa dibuang.");return;}
        int index=selectedIndex; var snapshot=s; int amount=s.count; EndSlotDrag();
        var blocker=GameplayHUDStyle.Rect("DiscardConfirmation",panel.transform,Vector2.zero,Vector2.one); dialog=blocker.gameObject;
        var dim=blocker.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.65f);
        var box=GameplayHUDStyle.Rect("Dialog",blocker,new Vector2(.34f,.36f),new Vector2(.66f,.64f));Surface(box,null,new Color(.2f,.29f,.31f,.99f));
        Label(box,"Message",$"Buang {s.item.itemName} x{amount}?\nItem akan dihapus dari tas.",23,new Vector2(.07f,.37f),new Vector2(.93f,.90f)).textWrappingMode=TextWrappingModes.Normal;
        ButtonAt(box,"Cancel","Batal",new Vector2(.07f,.10f),new Vector2(.46f,.30f)).onClick.AddListener(CancelDiscard);
        ButtonAt(box,"Confirm","Buang",new Vector2(.54f,.10f),new Vector2(.93f,.30f)).onClick.AddListener(()=>
        {
            if(GameplayPauseMenu.BlocksGameplayInput)return;
            if(inventory.GetSlot(index)==snapshot && snapshot.count==amount) inventory.RemoveFromSlot(index,amount);
            CancelDiscard();Refresh();
        });
    }
    void CancelDiscard(){if(dialog!=null){dialog.SetActive(false);Destroy(dialog);}dialog=null;}
    public void BeginSlotDrag(int index,PointerEventData eventData)
    {
        var s=inventory.GetSlot(index);if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput || s?.item==null)return;
        EndSlotDrag(); selectedIndex=index; draggedSlotIndex=index;Refresh();
        var rect=GameplayHUDStyle.Rect("DraggedItemGhost",canvasObject.transform,Vector2.zero,Vector2.zero);dragGhost=rect.gameObject;
        rect.sizeDelta=slotSize;rect.position=eventData.position;
        var img=rect.gameObject.AddComponent<Image>();img.sprite=s.item.icon;img.enabled=img.sprite!=null;img.preserveAspect=true;img.raycastTarget=false;
        var group=rect.gameObject.AddComponent<CanvasGroup>();group.alpha=.8f;group.blocksRaycasts=false;
        if(img.sprite==null) Label(rect,"Name",s.item.itemName,16,Vector2.zero,Vector2.one);
    }
    public void UpdateSlotDrag(PointerEventData eventData){if(dragGhost!=null)dragGhost.transform.position=eventData.position;}
    public void DropOnSlot(int targetIndex)
    {
        if(!isOpen || dialog!=null || GameplayPauseMenu.BlocksGameplayInput || draggedSlotIndex<0 || targetIndex==draggedSlotIndex)return;
        inventory.SwapSlots(draggedSlotIndex,targetIndex);selectedIndex=targetIndex;
        if(targetIndex<inventory.HotbarSlotCount)hotbarUI?.SelectSlot(targetIndex);Refresh();
    }
    public void EndSlotDrag(){draggedSlotIndex=-1;if(dragGhost!=null)Destroy(dragGhost);dragGhost=null;}
    static TMP_Text Label(Transform parent,string name,string value,float size,Vector2 min,Vector2 max)=>GameplayHUDStyle.Text(name,parent,value,size,min,max);
    static Image Artwork(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
    {
        var r=GameplayHUDStyle.Rect(name,parent,min,max);var img=r.gameObject.AddComponent<Image>();
        img.sprite=sprite;img.enabled=sprite!=null;img.preserveAspect=true;img.raycastTarget=false;return img;
    }
    static Graphic Surface(RectTransform rect,Sprite sprite,Color color)
    {
        if(sprite==null){var shape=GameplayHUDStyle.Surface(rect,color,8);shape.raycastTarget=true;return shape;}
        var img=rect.gameObject.AddComponent<Image>();img.sprite=sprite;img.type=Image.Type.Sliced;img.color=color;return img;
    }
    Button ButtonAt(Transform parent,string name,string text,Vector2 min,Vector2 max,Sprite sprite=null)
    {
        var r=GameplayHUDStyle.Rect(name,parent,min,max);
        var graphic=Surface(r,sprite!=null?sprite:buttonSprite!=null?buttonSprite:theme?.button,Color.white);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=graphic;var c=b.colors;
        c.normalColor=new Color(.30f,.38f,.40f,.65f);c.highlightedColor=new Color(.36f,.62f,.38f,.85f);
        c.pressedColor=new Color(.27f,.48f,.29f,.95f);c.selectedColor=c.normalColor;b.colors=c;
        r.gameObject.AddComponent<MainMenuButtonAudio>();
        if(!string.IsNullOrEmpty(text))Label(r,"Label",text,16,new Vector2(.04f,.03f),new Vector2(.96f,.97f)).alignment=TextAlignmentOptions.Center;
        return b;
    }
    static void Line(Transform parent,Vector2 min,Vector2 max,Color? tint=null)
    {var r=GameplayHUDStyle.Rect("Separator",parent,min,max);var img=r.gameObject.AddComponent<Image>();img.color=tint??new Color(.7f,.8f,.8f,.25f);img.raycastTarget=false;}
    static string Format(long value)=>value.ToString("N0",Indonesian);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInputFrame() => closedFrame = -1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInventoryUIExists()
    {if(FindFirstObjectByType<InventoryUI>()==null){var inv=FindFirstObjectByType<Inventory>();if(inv!=null)inv.gameObject.AddComponent<InventoryUI>();}}
    sealed class SlotView
    {public GameObject Root,Selection;public Image Icon;public TMP_Text Name,Count,Key;}
}

