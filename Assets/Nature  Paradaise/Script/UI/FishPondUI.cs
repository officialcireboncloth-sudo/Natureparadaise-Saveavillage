using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-9500)]
public sealed class FishPondUI : MonoBehaviour
{
    public static FishPondUI Instance { get; private set; }
    public FishPond Pond { get; private set; }
    FishPondTheme theme;
    readonly List<int> bagSlots=new();
    readonly List<List<int>> groups=new();
    readonly List<GameObject> cards=new();
    readonly List<(MainMenuRoundedImage image,int index,bool bag)> highlights=new();
    RectTransform bagArea,pondArea;
    TMP_Text capacity,health,detail,feedStatus,feedCount,feedback,bagPageLabel,pondPageLabel;
    Image detailImage;
    Button add,take,feed,ready,treat;
    int bagSelection=-1,pondSelection=-1,bagPage,pondPage;
    bool bagFocused=true,dirty=true,released;
    float previousScale;
    public static void Show(FishPond pond)
    {
        if(pond==null||Instance!=null)return;
        var ui=new GameObject("Fish Pond UI",typeof(RectTransform)).AddComponent<FishPondUI>();
        Instance=ui;ui.Pond=pond;ui.previousScale=Time.timeScale;Time.timeScale=0;
        FishPondService.Changed+=ui.MarkDirty;pond.PlayerInventory.OnInventoryChanged+=ui.MarkDirty;
        ui.Build();ui.Refresh();
    }
    public static void Hide(FishPond pond){if(Instance!=null&&Instance.Pond==pond)Instance.Dispose();}
    void MarkDirty()=>dirty=true;
    void Update()
    {
        if(released)return;
        if(Pond==null||!Pond.isActiveAndEnabled||Pond.PlayerInventory==null){Dispose();return;}
        if(dirty)Refresh();
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame||GameplayPauseMenu.IsOpen)return;
        if(Input.GetKeyDown(KeyCode.Escape)){Pond.ClosePanel();return;}
        if(Input.GetKeyDown(KeyCode.A))Cycle(-1);if(Input.GetKeyDown(KeyCode.D))Cycle(1);
        if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.S)){bagFocused=!bagFocused;RenderDetail();}
        if(Input.GetKeyDown(KeyCode.E))InsertSelected();if(Input.GetKeyDown(KeyCode.R))TakeSelected();if(Input.GetKeyDown(KeyCode.F))GiveFeed();
    }
    public void Dispose()
    {
        if(released)return;released=true;if(Instance==this)Instance=null;
        FishPondService.Changed-=MarkDirty;if(Pond!=null&&Pond.PlayerInventory!=null)Pond.PlayerInventory.OnInventoryChanged-=MarkDirty;
        Time.timeScale=previousScale;Pond?.ClosePanel();GameplayInput.ConsumeCurrentFrame();gameObject.SetActive(false);Destroy(gameObject);
    }
    void OnDisable()=>Dispose();void OnDestroy()=>Dispose();
    public void SelectBag(int index){if(!bagSlots.Contains(index))return;bagSelection=index;bagFocused=true;bagPage=bagSlots.IndexOf(index)/4;RebuildCards();RenderDetail();}
    public void SelectPond(int index){if(index<0||index>=groups.Count)return;pondSelection=index;bagFocused=false;pondPage=index/4;RebuildCards();RenderDetail();}
    void Cycle(int direction)
    {
        if(bagFocused&&bagSlots.Count>0){int index=bagSlots.IndexOf(bagSelection);SelectBag(bagSlots[(index+direction+bagSlots.Count)%bagSlots.Count]);}
        else if(!bagFocused&&groups.Count>0)SelectPond((pondSelection+direction+groups.Count)%groups.Count);
    }
    public void InsertSelected(){if(bagSelection>=0)Pond.AddFish(bagSelection);Refresh();GameplayInput.ConsumeCurrentFrame();}
    public void TakeSelected(){if(pondSelection>=0&&pondSelection<groups.Count)Pond.TakeFish(groups[pondSelection][0]);Refresh();GameplayInput.ConsumeCurrentFrame();}
    public void TreatSelected(){if(pondSelection>=0&&pondSelection<groups.Count)Pond.TreatFish(groups[pondSelection][0]);Refresh();GameplayInput.ConsumeCurrentFrame();}
    public void GiveFeed()
    {
        for(int i=0;i<Pond.PlayerInventory.slots.Count;i++){var stack=Pond.PlayerInventory.GetSlot(i);if(Pond.IsFishFeed(stack)){Pond.DepositFeed(i,1);Refresh();GameplayInput.ConsumeCurrentFrame();return;}}
        feedback.text="Pakan ikan tidak tersedia di tas.";GameplayInput.ConsumeCurrentFrame();
    }
    public int TakeReady()
    {
        int moved=0;
        for(int i=Pond.Fish.Count-1;i>=0;i--)
        {
            var fish=Pond.Fish[i];if(!IsReady(fish))continue;int before=Pond.FishCount;Pond.TakeFish(i);if(Pond.FishCount<before)moved++;
        }
        Refresh();feedback.text=moved>0?$"{moved} ikan siap diambil.":"Belum ada ikan siap atau tas penuh.";GameplayInput.ConsumeCurrentFrame();return moved;
    }
    static ItemSO Item(FishPondFishData fish)=>fish==null?null:ItemCatalog.Resolve(fish.itemId,fish.assetName,fish.itemName);
    static FishSizeTier Tier(FishPondFishData fish)=>FishMeasurement.GetSizeTier(Item(fish),fish.sizeCm);
    static bool IsReady(FishPondFishData fish)=>fish!=null&&!fish.sick&&(Tier(fish)==FishSizeTier.Large||Tier(fish)==FishSizeTier.Jumbo);
    static string Stars(int value)=>value<=0?"Normal":$"Kualitas {Mathf.Clamp(value,0,5)} / 5";
    void Refresh()
    {
        dirty=false;bagSlots.Clear();groups.Clear();var inv=Pond.PlayerInventory;
        for(int i=0;i<inv.slots.Count;i++){var s=inv.GetSlot(i);if(s?.item!=null&&s.item.category==ItemCategory.Fish&&s.count>0)bagSlots.Add(i);}
        // Group only identical stored metadata so selection never hides differing growth or health.
        var keys=new Dictionary<string,int>();
        for(int i=0;i<Pond.Fish.Count;i++)
        {
            var f=Pond.Fish[i];string key=$"{f.itemId}|{f.assetName}|{f.sizeCm:R}|{f.weightKg:R}|{f.qualityStars}|{f.growthDays}|{f.daysWithoutFood}|{f.sick}";
            if(!keys.TryGetValue(key,out int row)){row=groups.Count;keys[key]=row;groups.Add(new List<int>());}groups[row].Add(i);
        }
        if(!bagSlots.Contains(bagSelection))bagSelection=bagSlots.Count>0?bagSlots[0]:-1;
        pondSelection=groups.Count>0?Mathf.Clamp(pondSelection,0,groups.Count-1):-1;
        bagPage=Mathf.Clamp(bagPage,0,Mathf.Max(0,(bagSlots.Count-1)/4));pondPage=Mathf.Clamp(pondPage,0,Mathf.Max(0,(groups.Count-1)/4));
        capacity.text=$"Kapasitas\n{Pond.FishCount} / {Pond.Capacity} Ikan";
        int sick=Pond.Fish.Count(f=>f.sick);health.text=Pond.FishCount==0?"Kolam kosong":sick>0?$"{sick} ikan sakit":"Ikan sehat";
        feedStatus.text=Pond.FeedStock>0?$"Pakan siap\nStok {Pond.FeedStock} / {Pond.FeedCapacity}":"Belum disiapkan";feedStatus.color=Pond.FeedStock>0?new(.65f,.9f,.6f):new(1,.55f,.5f);
        int count=0;foreach(var s in inv.slots)if(Pond.IsFishFeed(s))count+=s.count;
        feedCount.text=$"Pakan Ikan\n{count}";feed.interactable=count>0&&Pond.FeedSpace>0;
        add.interactable=bagSelection>=0&&Pond.FishCount<Pond.Capacity;take.interactable=pondSelection>=0;ready.interactable=Pond.Fish.Any(IsReady);
        feedback.text=Pond.Feedback;RebuildCards();RenderDetail();
    }
    void RebuildCards()
    {
        foreach(var card in cards){card.SetActive(false);Destroy(card);}cards.Clear();highlights.Clear();
        bagPageLabel.text=$"{(bagSlots.Count==0?0:bagPage+1)} / {Mathf.Max(1,(bagSlots.Count+3)/4)}";
        pondPageLabel.text=$"{(groups.Count==0?0:pondPage+1)} / {Mathf.Max(1,(groups.Count+3)/4)}";
        for(int n=0;n<4;n++)
        {
            int p=bagPage*4+n;if(p>=bagSlots.Count)break;int slot=bagSlots[p];var stack=Pond.PlayerInventory.GetSlot(slot);
            float x=.07f+n*.217f;var button=Button(bagArea,"",x,.26f,x+.202f,.83f,()=>SelectBag(slot));cards.Add(button.gameObject);
            highlights.Add(((MainMenuRoundedImage)button.targetGraphic,slot,true));Art("Fish Image Slot",button.transform,stack.item.icon??theme?.fishPlaceholder,.08f,.39f,.92f,.97f);
            Text(button.transform,$"{stack.item.itemName}\n{Stars(stack.qualityStars)}\nx{stack.count}",17,.04f,.03f,.96f,.43f).alignment=TextAlignmentOptions.Center;
        }
        for(int n=0;n<4;n++)
        {
            int row=pondPage*4+n;if(row>=groups.Count)break;int selection=row;var data=Pond.Fish[groups[row][0]];var item=Item(data);float y=.70f-n*.155f;
            var button=Button(pondArea,"",.035f,y,.965f,y+.145f,()=>SelectPond(selection));cards.Add(button.gameObject);highlights.Add(((MainMenuRoundedImage)button.targetGraphic,row,false));
            Art("Fish Image Slot",button.transform,item?.icon??theme?.fishPlaceholder,.025f,.13f,.19f,.88f);
            Text(button.transform,$"{item?.itemName??data.itemName}  x{groups[row].Count}",17,.21f,.1f,.56f,.90f);
            Text(button.transform,Tier(data).ToString(),16,.57f,.1f,.71f,.90f);
            int needed=FishPondService.GrowthDaysNeeded(Tier(data));bool max=needed==0;
            Text(button.transform,data.sick?"Sakit":max?"Siap":$"{Mathf.RoundToInt(data.growthDays*100f/needed)}%",15,.89f,.1f,.98f,.9f);
            if(!max){var bar=Rect(button.transform,"Growth",.72f,.32f,.88f,.55f);GameplayHUDStyle.Surface(bar,new(.1f,.2f,.24f,.8f),3);var fill=Rect(bar,"Fill",0,0,Mathf.Clamp01((float)data.growthDays/needed),1);GameplayHUDStyle.Surface(fill,new(.43f,.76f,.95f),3).borderWidth=0;}
        }
        if(bagSlots.Count==0){var empty=Text(bagArea,"Tidak ada ikan di tas.",20,.1f,.40f,.9f,.7f);cards.Add(empty.gameObject);}
        if(groups.Count==0){var empty=Text(pondArea,"Kolam masih kosong.",20,.1f,.4f,.9f,.7f);cards.Add(empty.gameObject);}
    }
    void RenderDetail()
    {
        ItemSO item=null;float size=0,weight=0;int quality=0,count=0,growth=0;bool sick=false;int hunger=0;
        if(bagFocused&&bagSelection>=0){var s=Pond.PlayerInventory.GetSlot(bagSelection);if(s?.item!=null){item=s.item;size=s.fishSizeCm;weight=s.fishWeightKg;quality=s.qualityStars;count=s.count;}}
        else if(pondSelection>=0&&pondSelection<groups.Count){var f=Pond.Fish[groups[pondSelection][0]];item=Item(f);size=f.sizeCm;weight=f.weightKg;quality=f.qualityStars;growth=f.growthDays;count=groups[pondSelection].Count;sick=f.sick;hunger=f.daysWithoutFood;}
        detailImage.sprite=item?.icon??theme?.fishPlaceholder;detailImage.enabled=detailImage.sprite!=null;
        if(count==0)detail.text="Pilih ikan untuk melihat detail.";
        else
        {
            if(size<=0)size=FishMeasurement.FindDefinition(item)?.minimumSizeCm??15f;if(weight<=0)weight=FishMeasurement.EstimateWeightKg(item,size);
            var tier=FishMeasurement.GetSizeTier(item,size);int needed=FishPondService.GrowthDaysNeeded(tier);int remaining=tier==FishSizeTier.Small?FishPondService.SmallGrowthDays+FishPondService.MediumGrowthDays-growth:tier==FishSizeTier.Medium?FishPondService.MediumGrowthDays-growth:0;
            detail.text=$"<b>{item?.itemName??"Ikan"}</b>\n{Stars(quality)}  •  Jumlah: {count}\nUkuran: {tier}  ({size:0.#} cm)\nBerat: {weight:0.00} kg\n"+(bagFocused?"Masukkan ke kolam untuk dibesarkan.":sick?$"Sakit — pertumbuhan berhenti\nTanpa pakan: {hunger} hari":needed==0?"Ukuran maksimal — siap diambil":$"Progres: {growth} / {needed} hari\nMenuju Large: {Mathf.Max(0,remaining)} hari berpakan")+"\nSmall → Medium → Large";
        }
        treat.interactable=!bagFocused&&sick;
        foreach(var h in highlights){h.image.borderColor=(h.bag?bagSelection:pondSelection)==h.index?new(.58f,.87f,1):Color.clear;h.image.borderWidth=(h.bag?bagSelection:pondSelection)==h.index?1.5f:0;h.image.SetVerticesDirty();}
    }
    void Build()
    {
        theme=Resources.Load<FishPondTheme>("UI/FishPondTheme");var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=465;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_FishPond",typeof(EventSystem),typeof(StandaloneInputModule));
        var dim=Rect(transform,"Dim World",0,0,1,1).gameObject.AddComponent<Image>();dim.color=new(0,0,0,.2f);
        var safe=Rect(transform,"Safe Area",0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();var panel=Rect(safe,"Fish Pond Panel",.375f,.025f,.99f,.975f);
        if(theme?.panel!=null){var image=panel.gameObject.AddComponent<Image>();image.sprite=theme.panel;image.type=Image.Type.Sliced;}else GameplayHUDStyle.Surface(panel,GameplayHUDStyle.Modal,18).raycastTarget=true;
        Art("Header Fish Slot",panel,theme?.headerFish,.025f,.885f,.12f,.975f);Text(panel,"KOLAM IKAN",42,.14f,.916f,.61f,.984f).fontStyle=FontStyles.Bold;Text(panel,"Kolam Kebun",25,.14f,.87f,.6f,.92f);
        Art("Leaf Decoration Slot",panel,theme?.leafDecoration,.60f,.93f,.635f,.975f);Art("Water Icon Slot",panel,theme?.waterIcon,.96f,.89f,.985f,.94f);
        capacity=Text(panel,"",21,.64f,.88f,.79f,.97f);health=Text(panel,"",19,.81f,.88f,.955f,.97f);
        bagArea=Section(panel,"Bag Fish","IKAN DI TAS",theme?.bagIcon,.02f,.52f,.535f,.853f);
        Button(bagArea,"‹",.005f,.44f,.06f,.69f,()=>{bagPage=Mathf.Max(0,bagPage-1);RebuildCards();});Button(bagArea,"›",.94f,.44f,.995f,.69f,()=>{bagPage=Mathf.Min(Mathf.Max(0,(bagSlots.Count-1)/4),bagPage+1);RebuildCards();});
        bagPageLabel=Text(bagArea,"",14,.035f,.02f,.18f,.12f);add=Button(bagArea,"[E]  Masukkan",.24f,.045f,.77f,.21f,InsertSelected);
        pondArea=Section(panel,"Pond Contents","ISI KOLAM",theme?.pondIcon,.55f,.45f,.985f,.853f);take=Button(pondArea,"[R]  Ambil",.24f,.035f,.76f,.16f,TakeSelected);
        Button(pondArea,"‹",.035f,.035f,.115f,.16f,()=>{pondPage=Mathf.Max(0,pondPage-1);RebuildCards();});Button(pondArea,"›",.88f,.035f,.965f,.16f,()=>{pondPage=Mathf.Min(Mathf.Max(0,(groups.Count-1)/4),pondPage+1);RebuildCards();});pondPageLabel=Text(pondArea,"",12,.12f,.035f,.23f,.16f);
        var selected=Section(panel,"Fish Detail","DETAIL IKAN TERPILIH",theme?.detailIcon,.02f,.13f,.535f,.503f);
        detailImage=Art("Selected Fish Image Slot",selected,null,.035f,.34f,.45f,.82f);detail=Text(selected,"",20,.47f,.18f,.975f,.85f);
        Text(selected,"Ikan tumbuh saat mendapat pakan dan sehat.",16,.035f,.015f,.96f,.11f);treat=Button(selected,"Obati Ikan",.06f,.15f,.40f,.29f,TreatSelected);
        var feeding=Section(panel,"Feeding","PAKAN HARI INI",theme?.feedBowl,.55f,.185f,.985f,.433f);
        Art("Feed Bowl Image Slot",feeding,theme?.feedBowl,.04f,.30f,.30f,.77f);feedStatus=Text(feeding,"",20,.32f,.38f,.67f,.73f);feedCount=Text(feeding,"",19,.73f,.35f,.98f,.75f);Art("Feed Bag Image Slot",feeding,theme?.feedBag,.72f,.19f,.84f,.38f);
        feed=Button(feeding,"[F]  Beri Pakan",.22f,.045f,.83f,.27f,GiveFeed);
        ready=Button(panel,"Ambil Ikan Siap",.55f,.10f,.84f,.16f,()=>TakeReady());Button(panel,"Tutup",.86f,.10f,.985f,.16f,()=>Pond.ClosePanel());
        feedback=Text(panel,"",16,.025f,.095f,.535f,.128f);Text(panel,"A / D Pilih ikan   W / S Pilih bagian   E Masukkan   R Ambil   F Pakan   Esc Tutup",16,.13f,.015f,.97f,.055f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Button(panel,"Debug +1 hari",.025f,.06f,.24f,.09f,()=>{Pond.DebugAdvanceGrowth(false);Refresh();});Button(panel,"Debug naik ukuran",.25f,.06f,.49f,.09f,()=>{Pond.DebugAdvanceGrowth(true);Refresh();});
#endif
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)=>GameplayHUDStyle.Rect(name,parent,new(x,y),new(xx,yy));
    static TMP_Text Text(Transform parent,string value,float size,float x,float y,float xx,float yy)=>GameplayHUDStyle.Text(value.Length>0?value:"Value",parent,value,size,new(x,y),new(xx,yy));
    static Image Art(string name,Transform parent,Sprite sprite,float x,float y,float xx,float yy){var image=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;}
    static RectTransform Section(Transform parent,string name,string title,Sprite icon,float x,float y,float xx,float yy)
    {
        var rect=Rect(parent,name,x,y,xx,yy);GameplayHUDStyle.Surface(rect,GameplayHUDStyle.Card);Art(title+" Icon Slot",rect,icon,.035f,.865f,.095f,.98f);Text(rect,title,23,.125f,.865f,.97f,.98f).fontStyle=FontStyles.Bold;return rect;
    }
    static Button Button(Transform parent,string value,float x,float y,float xx,float yy,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(parent,value.Length>0?value:"Fish Card",x,y,xx,yy);var surface=GameplayHUDStyle.Surface(rect,Color.white,10);surface.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=surface;var colors=button.colors;colors.normalColor=new(.22f,.31f,.36f,.9f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.32f,.53f,.37f);colors.pressedColor=new(.23f,.40f,.28f);colors.disabledColor=new(.17f,.24f,.28f,.55f);colors.fadeDuration=0;button.colors=colors;GameplayHUDStyle.ButtonStates(button);button.navigation=new Navigation{mode=Navigation.Mode.None};
        rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(()=>{if(!releasedGlobally())action();});if(value.Length>0)Text(rect,value,21,.04f,.05f,.96f,.95f).alignment=TextAlignmentOptions.Center;return button;
    }
    static bool releasedGlobally()=>Instance==null||Instance.released||GameplayPauseMenu.IsOpen;
}
