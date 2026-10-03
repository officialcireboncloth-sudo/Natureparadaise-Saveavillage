using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-9500)]
public sealed class StorageChestUI : MonoBehaviour
{
    public static StorageChestUI Instance { get; private set; }
    public static bool IsOpen => Instance != null;
    public ToolStorageChest Chest { get; private set; }
    public Refrigerator Fridge { get; private set; }
    public HouseStorageChest HouseChest { get; private set; }
    public ShippingBin Bin { get; private set; }
    GameObject confirmationPanel;
    TMP_Text confirmationText,shippingTotal;
    StorageChestTheme theme;
    GameObject dragGhost;
    bool dragStorage;
    int dragIndex = -1;
    ItemStack dragStack;
    int StoredCount => Bin != null ? Bin.Listings.Count : Chest != null ? ToolStorageService.Entries.Count : Fridge != null ? RefrigeratorService.Entries.Count : HouseStorageService.Entries.Count;
    int StorageCapacity => Bin != null ? Mathf.Max(12,StoredCount) : Chest != null ? Mathf.Max(12,StoredCount) : Fridge != null ? RefrigeratorService.MaxSlots : HouseStorageService.Capacity;
    bool Accepts(ItemSO item) => item != null && (Bin != null ? item.CanSellAtMarket : Chest != null ? item.CanStoreInToolStorage : Fridge != null ? item.CanStoreInRefrigerator : HouseStorageService.Accepts(item));
    ItemStack Stored(int index)
    {
        if(index < 0 || index >= StoredCount) return null;
        if(Bin != null){var e=Bin.Listings[index];return new ItemStack{item=e.item,count=e.count,qualityStars=e.qualityStars,fishSizeCm=e.fishSizeCm};}
        if(Chest != null){var e=ToolStorageService.Entries[index];return new ItemStack{item=e.item,count=e.count};}
        if(Fridge != null){var e=RefrigeratorService.Entries[index];return new ItemStack{item=e.item,count=e.count,qualityStars=e.qualityStars,fishSizeCm=e.fishSizeCm,fishWeightKg=e.fishWeightKg};}
        return HouseStorageService.Entries[index];
    }
    bool MoveStack(bool take,int index,int count)
    {
        if(Bin!=null){if(take)return Bin.TryWithdraw(index,count);int before=Bin.PendingItemCount;Bin.RequestDeposit(index,count);return Bin.AwaitingConfirmation||Bin.PendingItemCount>before;}
        if(Chest!=null)return take?Chest.TryTake(index,count):Chest.TryStoreFromSlot(index,count);
        if(Fridge!=null)return take?Fridge.TryTake(index,count):Fridge.TryStore(index,count);
        return HouseChest!=null && (take?HouseChest.TryTake(index,count):HouseChest.TryStore(index,count));
    }
    string Feedback => Bin != null ? Bin.Feedback : Chest != null ? Chest.Feedback : Fridge != null ? Fridge.Feedback : HouseChest.Feedback;
    Inventory inventory;
    PlayerController movement;
    TimeManager clock;
    float previousScale;
    bool released, dirty, storageSelected;
    int bagIndex, chestIndex, quantity=1;
    float lastGridWidth;
    RectTransform bagContent,chestContent,bagViewport,chestViewport;
    ScrollRect bagScroll,chestScroll;
    TMP_Text bagCount,chestCount,detail,amount,feedback;
    Image illustration;

    Button storeButton,takeButton;
    readonly List<Button> bagSlots=new(), chestSlots=new();
    readonly Color[] accents={new(.55f,.73f,.66f),new(.52f,.70f,.88f),new(.78f,.65f,.50f),new(.76f,.58f,.76f),new(.83f,.77f,.49f)};
    static readonly Color Gray=new(.22f,.30f,.34f,.8f);
    public static void Show(ToolStorageChest chest) => Open(chest?.PlayerInventory,chest,null,null);
    public static void Show(Refrigerator fridge) => Open(fridge?.PlayerInventory,null,fridge,null);
    public static void Show(HouseStorageChest chest) => Open(chest?.PlayerInventory,null,null,chest);
    public static void Show(ShippingBin bin) => Open(bin?.PlayerInventory,null,null,null,bin);
    static void Open(Inventory bag,ToolStorageChest tools,Refrigerator fridge,HouseStorageChest house,ShippingBin bin=null)
    {
        if(bag==null || IsOpen || BedRestMenu.IsOpen || AnimalCarePanel.IsOpen || WorldInteractionPrompt.IsSuppressed) return;
        var ui=new GameObject("House Storage UI",typeof(RectTransform)).AddComponent<StorageChestUI>();
        Instance=ui;ui.Chest=tools;ui.Fridge=fridge;ui.HouseChest=house;ui.Bin=bin;ui.inventory=bag;
        ui.movement=bag.GetComponent<PlayerController>();ui.clock=TimeManager.Instance;
        ui.previousScale=Time.timeScale;Time.timeScale=0;ui.movement?.AcquireMovementLock(ui);ui.clock?.AcquirePause(ui);
        WorldInteractionPrompt.AcquireSuppression(ui);GameplayInput.ConsumeCurrentFrame();
        bag.OnInventoryChanged+=ui.MarkDirty;ToolStorageService.Changed+=ui.MarkDirty;RefrigeratorService.Changed+=ui.MarkDirty;HouseStorageService.Changed+=ui.MarkDirty;
        ui.Build();ui.dirty=true;
    }
    void MarkDirty()=>dirty=true;
    void Update()
    {
        if(released)return;
        if((Chest==null && Fridge==null && HouseChest==null && Bin==null) || (Bin!=null&&!Bin.isActiveAndEnabled) || inventory==null){Close();return;}
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null || focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){if(Bin!=null&&Bin.AwaitingConfirmation){Bin.CancelDeposit();dirty=true;GameplayInput.ConsumeCurrentFrame();}else Close();return;}
        if(Bin!=null&&Bin.AwaitingConfirmation){if(Input.GetKeyDown(KeyCode.Return)){Bin.ConfirmDeposit();dirty=true;GameplayInput.ConsumeCurrentFrame();}return;}
        if(dragGhost!=null)return;

        if(Input.GetKeyDown(KeyCode.Tab)){storageSelected=!storageSelected;quantity=1;dirty=true;}
        int delta=Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow)?-4:
            Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow)?4:
            Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow)?-1:
            Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow)?1:0;
        if(delta!=0)Select(storageSelected,(storageSelected?chestIndex:bagIndex)+delta);
        bool all=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
        if(Input.GetKeyDown(KeyCode.E))Transfer(false,all);
        if(Input.GetKeyDown(KeyCode.R))Transfer(true,all);
        if(Bin==null&&Input.GetKeyDown(KeyCode.Q))StoreAll();
        if(Bin==null&&Input.GetKeyDown(KeyCode.F))TakeAll();

    }
    void LateUpdate()
    {
        if(released||bagViewport==null)return;
        if(confirmationPanel!=null){confirmationPanel.SetActive(Bin!=null&&Bin.AwaitingConfirmation);if(Bin!=null&&Bin.AwaitingConfirmation)confirmationText.text="Jual barang bernilai tinggi?\n"+Bin.ConfirmationDescription;}
        float width=bagViewport.rect.width+chestViewport.rect.width;
        if(!Mathf.Approximately(lastGridWidth,width))dirty=true;
        if(dirty && dragGhost==null){dirty=false;Refresh();lastGridWidth=width;}
    }
    public void Close(){Release();gameObject.SetActive(false);Destroy(gameObject);}
    void Release()
    {
        if(released)return;released=true;EndDrag();
        if(inventory!=null)inventory.OnInventoryChanged-=MarkDirty;ToolStorageService.Changed-=MarkDirty;RefrigeratorService.Changed-=MarkDirty;
        HouseStorageService.Changed-=MarkDirty;
        movement?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
        Time.timeScale=previousScale;GameplayInput.ConsumeCurrentFrame();if(Instance==this)Instance=null;
        Bin?.ClosePanel();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }
    void OnDisable()=>Release();void OnDestroy()=>Release();
    public void Select(bool storage,int index)
    {
        if(Bin!=null&&Bin.AwaitingConfirmation)return;
        storageSelected=storage;
        if(storage)chestIndex=Mathf.Clamp(index,0,Mathf.Max(0,StoredCount-1));
        else bagIndex=Mathf.Clamp(index,0,inventory.Capacity-1);
        quantity=1;dirty=true;
        var scroll=storage?chestScroll:bagScroll;int selected=storage?chestIndex:bagIndex;
        int rows=Mathf.CeilToInt((storage?StorageCapacity:inventory.Capacity)/4f);
        scroll.verticalNormalizedPosition=1-Mathf.Clamp01((selected/4f)/Mathf.Max(1,rows-1));
    }
    public void ChangeQuantity(int delta){quantity=Mathf.Clamp(quantity+delta,1,Mathf.Max(1,SelectedCount()));RefreshDetails();}
    int SelectedCount()=>storageSelected?(chestIndex<StoredCount?Stored(chestIndex).count:0):(inventory.GetSlot(bagIndex)?.count??0);
    public void Transfer(bool take,bool entireStack=false)
    {
        if(storageSelected!=take||(Bin!=null&&Bin.AwaitingConfirmation))return;
        int n=entireStack?SelectedCount():quantity;
        MoveStack(take,take?chestIndex:bagIndex,n);
        dirty=true;GameplayInput.ConsumeCurrentFrame();
    }
    public void StoreAll(){if(Bin!=null)return;for(int i=inventory.Capacity-1;i>=0;i--){var s=inventory.GetSlot(i);if(s!=null && Accepts(s.item))MoveStack(false,i,s.count);}dirty=true;GameplayInput.ConsumeCurrentFrame();}
    public void TakeAll(){if(Bin!=null)return;for(int i=StoredCount-1;i>=0;i--)MoveStack(true,i,Stored(i).count);dirty=true;GameplayInput.ConsumeCurrentFrame();}

    void Build()
    {
        theme=Resources.Load<StorageChestTheme>(Bin!=null?"UI/ShippingBinTheme":"UI/StorageChestTheme");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=460;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_Storage",typeof(EventSystem),typeof(StandaloneInputModule));
        var dim=Rect("Dim",transform,0,0,1,1).gameObject.AddComponent<Image>();dim.color=new(0,0,0,.28f);
        var safe=Rect("Safe Area",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel=Rect("Storage Panel",safe,.315f,.015f,.99f,.985f);Surface(panel,new(.11f,.19f,.24f,.93f),theme?.panel);
        Art("Leaf Image Slot",panel,theme?.leafIcon,.025f,.914f,.08f,.975f);
        Text(panel,Bin!=null?"KOTAK PENJUALAN":Chest!=null?"TOOL STORAGE":Fridge!=null?"REFRIGERATOR":"STORAGE RUMAH",38,.09f,.935f,.70f,.985f);
        Text(panel,Bin!=null?"Pindahkan barang dari tas untuk dijual besok pagi.":"Drag item antara tas dan storage untuk memindahkan satu stack.",23,.09f,.898f,.76f,.94f).color=new(.74f,.83f,.88f);
        Art("Logo Image Slot",panel,theme?.logo,.78f,.90f,.97f,.985f);
        var nameRow=Rect("Chest Appearance",panel,.02f,.82f,.98f,.891f);Surface(nameRow,new(.25f,.32f,.36f,.65f));
        Art("Chest Image Slot",nameRow,theme?.chestIcon,.02f,.1f,.09f,.9f);
        Text(nameRow,Bin!=null?"Shipping Bin • Barang dapat diambil kembali sebelum pergantian hari":Chest!=null?"Rak Alat Rumah • Hanya Tool":Fridge!=null?"Kulkas • Makanan matang dan minuman":HouseStorageService.Level==1?"Storage Rumah • Semua item":"Storage Rumah • Tools, seed dan bahan mentah",25,.12f,.12f,.94f,.88f);
        var bag=Rect("Player Bag",panel,.02f,.29f,.44f,.807f);Surface(bag,new(.15f,.24f,.29f,.6f));
        var chest=Rect("Chest Contents",panel,.545f,.29f,.98f,.807f);Surface(chest,new(.15f,.24f,.29f,.6f));
        Art("Bag Image Slot",bag,theme?.bagIcon,.03f,.9f,.12f,.98f);Text(bag,"TAS PEMAIN",27,.15f,.9f,.76f,.98f);
        bagCount=Text(bag,"",22,.76f,.9f,.98f,.98f);
        Art("Contents Image Slot",chest,theme?.chestIcon,.03f,.9f,.12f,.98f);Text(chest,Bin!=null?"ISI SHIPPING BIN":"ISI PETI",27,.15f,.9f,.75f,.98f);
        chestCount=Text(chest,"",22,.76f,.9f,.98f,.98f);
        bagScroll=Grid(bag,out bagViewport,out bagContent);chestScroll=Grid(chest,out chestViewport,out chestContent);
        storeButton=ButtonAt(panel,Bin!=null?"E  Jual":"E  Simpan",.451f,.56f,.532f,.69f,()=>Transfer(false),theme?.storeIcon);
        takeButton=ButtonAt(panel,Bin!=null?"R  Kembali":"R  Ambil",.451f,.40f,.532f,.53f,()=>Transfer(true),theme?.takeIcon);
        var info=Rect("Item Details",panel,.02f,.14f,.98f,.275f);Surface(info,new(.22f,.29f,.33f,.7f));
        illustration=Art("Item Illustration Slot",info,null,.025f,.1f,.16f,.9f);
        detail=Text(info,"Pilih item",21,.18f,.08f,.75f,.95f);
        Text(info,"Jumlah",20,.78f,.7f,.98f,.98f);
        ButtonAt(info,"−",.78f,.23f,.835f,.68f,()=>ChangeQuantity(-1));
        amount=Text(info,"1",27,.845f,.23f,.93f,.68f);amount.alignment=TextAlignmentOptions.Midline;
        ButtonAt(info,"+",.935f,.23f,.985f,.68f,()=>ChangeQuantity(1));
        if(Bin==null)ButtonAt(panel,"Q  Simpan Semua",.18f,.072f,.47f,.129f,StoreAll,theme?.storeAllIcon);
        if(Bin==null)ButtonAt(panel,"F  Ambil Semua",.53f,.072f,.82f,.129f,TakeAll,theme?.takeAllIcon);
        if(Bin!=null)shippingTotal=Text(panel,"",25,.04f,.072f,.97f,.129f);
        feedback=Text(panel,"",17,.025f,.043f,.98f,.07f);
        Text(panel,"Drag  Satu Stack    E / R  Transfer    Shift + E/R  Stack    Esc  Tutup",18,.04f,.007f,.87f,.043f);
        ButtonAt(panel,"Tutup",.89f,.007f,.98f,.043f,Close);
        bag.gameObject.AddComponent<StorageSlotDragHandler>().Initialize(this,false,-1);
        chest.gameObject.AddComponent<StorageSlotDragHandler>().Initialize(this,true,-1);
        if(Bin!=null)BuildConfirmation(panel);
        Canvas.ForceUpdateCanvases();
    }
    void BuildConfirmation(RectTransform panel)
    {
        confirmationPanel=Rect("Valuable Item Confirmation",panel,0,0,1,1).gameObject;
        confirmationPanel.AddComponent<Image>().color=new(0,0,0,.78f);
        var card=Rect("Confirmation",confirmationPanel.transform,.20f,.35f,.80f,.65f);Surface(card,new(.18f,.26f,.32f));
        confirmationText=Text(card,"",26,.05f,.43f,.95f,.94f);
        ButtonAt(card,"Batal",.05f,.10f,.47f,.35f,()=>{Bin.CancelDeposit();dirty=true;});
        ButtonAt(card,"Jual",.53f,.10f,.95f,.35f,()=>{Bin.ConfirmDeposit();dirty=true;GameplayInput.ConsumeCurrentFrame();});
        confirmationPanel.SetActive(false);
    }
    ScrollRect Grid(RectTransform parent,out RectTransform viewport,out RectTransform content)
    {
        var area=Rect("Scrollable Grid",parent,.025f,.025f,.975f,.895f);var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=38;
        viewport=Rect("Viewport",area,0,0,1,1);viewport.gameObject.AddComponent<RectMask2D>();
        content=Rect("Slots",viewport,0,1,1,1);content.pivot=new(0,1);scroll.viewport=viewport;scroll.content=content;
        return scroll;
    }
    void Refresh()
    {
        bagIndex=Mathf.Clamp(bagIndex,0,inventory.Capacity-1);chestIndex=Mathf.Clamp(chestIndex,0,Mathf.Max(0,StoredCount-1));
        bagCount.text=$"{inventory.UsedSlots} / {inventory.Capacity}";chestCount.text=Bin!=null?$"{StoredCount} jenis":Chest!=null?$"{StoredCount} jenis":$"{StoredCount}/{StorageCapacity}";
        BuildSlots(false,bagContent,bagViewport,bagSlots,inventory.Capacity);
        BuildSlots(true,chestContent,chestViewport,chestSlots,StorageCapacity);
        RefreshDetails();
    }
    void BuildSlots(bool storage,RectTransform content,RectTransform viewport,List<Button> slots,int count)
    {
        foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}slots.Clear();
        float width=Mathf.Max(64,(viewport.rect.width-24)/4),height=width*1.18f;content.sizeDelta=new(0,Mathf.CeilToInt(count/4f)*(height+8));
        for(int i=0;i<count;i++)
        {
            int index=i;ItemSO item=null;int number=0,quality=0;
            if(storage&&i<StoredCount){var e=Stored(i);item=e.item;number=e.count;quality=e.qualityStars;}
            else if(!storage){var s=inventory.GetSlot(i);if(s!=null){item=s.item;number=s.count;quality=s.qualityStars;}}
            var row=Rect("Slot "+i,content,0,1,0,1);row.pivot=new(0,1);row.anchoredPosition=new((i%4)*(width+8),-(i/4)*(height+8));row.sizeDelta=new(width,height);
            var button=ButtonAt(row,"",0,0,1,1,()=>Select(storage,index));slots.Add(button);row.gameObject.AddComponent<StorageSlotDragHandler>().Initialize(this,storage,index);
            if(Bin==null&&!storage&&item!=null&&!Accepts(item))button.interactable=false;
            var surface=button.targetGraphic as MainMenuRoundedImage;
            bool selected=storageSelected==storage&&(storage?chestIndex:bagIndex)==i;
            surface.borderColor=selected?new Color(.5f,1,.96f):accents[0]*new Color(1,1,1,.45f);surface.borderWidth=selected?2:1;
            if(item!=null)
            {
                Art("Item Image Slot",row,item.icon,.08f,.39f,.92f,.96f);
                Text(row,Bin!=null&&!storage&&!Accepts(item)?"Tidak dijual":quality>0?$"Kualitas {quality}":"",14,.04f,.3f,.96f,.41f).color=new(.95f,.80f,.45f);
                var label=Text(row,item.itemName,18,.03f,.15f,.97f,.31f);label.alignment=TextAlignmentOptions.Midline;
                Text(row,$"x{number}",21,.04f,.015f,.96f,.155f).alignment=TextAlignmentOptions.Midline;
            }
            else Text(row,"+",32,.05f,.05f,.95f,.95f).alignment=TextAlignmentOptions.Midline;
        }
    }
    void RefreshDetails()
    {
        if(shippingTotal!=null)shippingTotal.text=$"Total Penjualan: {Bin.PendingEstimatedValue.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("id-ID"))} G • Uang diterima besok pagi";
        ItemSO item=null;int quality=0;float size=0,weight=0;
        if(storageSelected&&chestIndex<StoredCount){var e=Stored(chestIndex);item=e.item;quality=e.qualityStars;size=e.fishSizeCm;weight=e.fishWeightKg;}
        else if(!storageSelected){var s=inventory.GetSlot(bagIndex);if(s!=null){item=s.item;quality=s.qualityStars;size=s.fishSizeCm;weight=s.fishWeightKg;}}
        quantity=Mathf.Clamp(quantity,1,Mathf.Max(1,SelectedCount()));amount.text=quantity.ToString();
        illustration.sprite=item!=null?(item.inventoryIllustration!=null?item.inventoryIllustration:item.icon):null;illustration.enabled=illustration.sprite!=null;
        detail.text=item==null?"Pilih item dari tas atau peti.":$"<b>{item.itemName}</b>\n{item.inventoryDescription}\nJumlah di Tas: {inventory.GetCount(item)}    Jumlah di Peti: {StorageCount(item)}";
        if(item!=null&&!Accepts(item))detail.text+=Bin!=null?"\nBarang ini tidak dapat dijual.":"\nItem ini tidak dapat disimpan di storage ini.";
        if(Bin!=null&&item!=null){int price=storageSelected&&chestIndex<Bin.Listings.Count?Bin.Listings[chestIndex].unitPriceSnapshot:item.GetMarketSellPrice(quality,size);detail.text+=$"\nHarga jual: {price} G • Kualitas {quality}/5";}
        if(Chest!=null&&item!=null&&Accepts(item)) { var status=inventory.GetComponent<PlayerStatusSystem>(); detail.text+=$"    Level: {(status!=null?status.GetToolLevel(item.equippedTool):1)}"; }
        if(size>0)detail.text+=$"\n{size:0.#} cm • {weight:0.##} kg";
        storeButton.interactable=!storageSelected&&item!=null&&Accepts(item);takeButton.interactable=storageSelected&&item!=null;feedback.text=Feedback;
    }
    int StorageCount(ItemSO item){int count=0;for(int i=0;i<StoredCount;i++){var e=Stored(i);if(e.item==item)count+=e.count;}return count;}
    public void BeginDrag(bool storage,int index,PointerEventData pointer)
    {
        if(Bin!=null&&Bin.AwaitingConfirmation)return;EndDrag();if(pointer.button!=PointerEventData.InputButton.Left)return;
        var source=storage?Stored(index):inventory.GetSlot(index);
        if(source==null || source.count<=0 || (!storage && !Accepts(source.item)))return;
        dragStorage=storage;dragIndex=index;
        dragStack=new ItemStack{item=source.item,count=source.count,qualityStars=source.qualityStars,fishSizeCm=source.fishSizeCm,fishWeightKg=source.fishWeightKg};
        var ghost=Rect("Dragged Item",transform,.5f,.5f,.5f,.5f);ghost.sizeDelta=new(100,100);dragGhost=ghost.gameObject;
        var group=dragGhost.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var icon=Art("Icon",ghost,source.item.icon,0,.2f,1,1);
        if(source.item.icon==null)Text(ghost,source.item.itemName,20,0,.2f,1,1);
        Text(ghost,$"x{source.count}",22,0,0,1,.25f);UpdateDrag(pointer);
    }
    public void UpdateDrag(PointerEventData pointer)
    {
        if(dragGhost==null)return;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,pointer.position,pointer.pressEventCamera,out var point))
            ((RectTransform)dragGhost.transform).anchoredPosition=point;
    }
    public bool Drop(bool destinationStorage,int destinationIndex=-1)
    {
        if(dragGhost==null || dragIndex<0||(Bin!=null&&Bin.AwaitingConfirmation))return false;
        var source=dragStorage?Stored(dragIndex):inventory.GetSlot(dragIndex);
        bool valid=source!=null && source.item==dragStack.item && source.count==dragStack.count && source.qualityStars==dragStack.qualityStars && source.fishSizeCm==dragStack.fishSizeCm && source.fishWeightKg==dragStack.fishWeightKg;
        bool moved=false;
        if(valid && destinationStorage!=dragStorage)moved=MoveStack(dragStorage,dragIndex,dragStack.count);
        else if(valid && !dragStorage && !destinationStorage && destinationIndex>=0)moved=inventory.SwapSlots(dragIndex,destinationIndex);
        EndDrag();dirty=true;GameplayInput.ConsumeCurrentFrame();return moved;
    }
    public void EndDrag(){if(dragGhost!=null){dragGhost.SetActive(false);Destroy(dragGhost);}dragGhost=null;dragIndex=-1;dragStack=null;}
    static RectTransform Rect(string name,Transform parent,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Rect(name,parent,new(x1,y1),new(x2,y2));
    static void Surface(RectTransform rect,Color color,Sprite art=null)
    {
        if(art!=null){var image=rect.gameObject.AddComponent<Image>();image.sprite=art;image.type=Image.Type.Sliced;image.color=color;}
        else GameplayHUDStyle.Surface(rect,color,14).raycastTarget=true;
    }
    static TMP_Text Text(Transform parent,string value,float size,float x1,float y1,float x2,float y2)
    {
        var text=GameplayHUDStyle.Text("Text",parent,value,size,new(x1,y1),new(x2,y2));text.fontSizeMin=11;return text;
    }
    static Image Art(string name,Transform parent,Sprite sprite,float x1,float y1,float x2,float y2)
    {
        var image=Rect(name,parent,x1,y1,x2,y2).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;
    }
    static Button ButtonAt(RectTransform parent,string label,float x1,float y1,float x2,float y2,UnityEngine.Events.UnityAction action,Sprite icon=null)
    {
        var rect=Rect(label+" Button",parent,x1,y1,x2,y2);var image=GameplayHUDStyle.Surface(rect,Color.white,10);image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;
        colors.normalColor=Gray;colors.selectedColor=Gray;colors.highlightedColor=new(.32f,.53f,.37f,.94f);colors.pressedColor=new(.23f,.40f,.28f);colors.disabledColor=new(.16f,.22f,.25f,.45f);colors.fadeDuration=0;button.colors=colors;
        button.navigation=new Navigation{mode=Navigation.Mode.None};rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        var text=Text(rect,label,21,icon!=null?.29f:.04f,.06f,.96f,.94f);text.alignment=TextAlignmentOptions.Midline;
        if(icon!=null)Art("Image Slot",rect,icon,.04f,.15f,.25f,.85f);return button;
    }
}



