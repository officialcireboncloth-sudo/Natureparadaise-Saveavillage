using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Left-side detail UI opened explicitly; care progress is displayed independently.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Inventory))]
public sealed class AnimalInteractionHUD : MonoBehaviour
{
    [SerializeField] AnimalInteractionTheme theme;
    Inventory inventory;
    HUDManager mainHUD;
    GameObject canvasObject, context, progressRoot;
    RectTransform safe, progressFill, bubble;
    TMP_Text speciesText, nameText, ageText, heartFallback, genderText, fourthLabel, fourthValue;
    TMP_Text clockText, moneyText, actionLabel, remainingText;
    Image portrait, genderIcon, fourthIcon, actionIcon, reactionImage;
    readonly List<Image> hearts=new();
    AnimalController target;
    RectTransform informationCard, detailContents, detailArea;
    AnimalCarePanel renderedDetail;
    int renderedRevision=-1;
    readonly List<System.Action> refreshDetailRows=new();
    bool contextVisible;
    public bool IsContextVisible => contextVisible;
    public AnimalGrowthSystem CurrentAnimal => contextVisible ? AnimalCarePanel.Instance?.Animal : null;
    void Awake()
    {
        inventory=GetComponent<Inventory>();
        if(theme==null) theme=Resources.Load<AnimalInteractionTheme>("UI/AnimalInteractionTheme");
    }
    void Start() => BuildUI();
    void Update()
    {
        var active=AnimalController.CurrentCareAction;
        if(active!=null && active.playerInv==inventory && !GameplayPauseMenu.BlocksGameplayInput &&
            (GameplayInput.GetKeyDown(KeyCode.E) || GameplayInput.GetKeyDown(KeyCode.Escape))) active.CancelCareAction();
    }
    void LateUpdate()
    {
        if(context==null) BuildUI();
        target=AnimalController.CurrentCareAction;
        if(target!=null && target.playerInv!=inventory) target=null;
        bool ownAction=target!=null && target.IsCareBusy;
        var detail=AnimalCarePanel.Instance;
        bool visible=detail!=null && detail.Inventory==inventory && !GameplayPauseMenu.IsOpen;
        SetVisibility(visible);
        if(visible)
        {
            RefreshClock();
            informationCard.gameObject.SetActive(detail.Animal!=null);
            detailArea.offsetMax=new Vector2(458,detail.Animal!=null?-468:-142);
            if(detail.Animal!=null) RefreshCard(detail.Animal);
            if(renderedDetail!=detail || renderedRevision!=detail.Revision) BuildDetailContents(detail);
            foreach(var refresh in refreshDetailRows) refresh();
        }
        else { renderedDetail=null; renderedRevision=-1; }
        progressRoot.SetActive(ownAction && !GameplayPauseMenu.IsOpen);
        if(ownAction && !GameplayPauseMenu.IsOpen)
        {
            actionLabel.text=target.CareActionLabel;
            remainingText.text=$"{target.CareSecondsRemaining:0.0} s";
            progressFill.anchorMax=new Vector2(target.CareProgress,1);
            progressFill.GetComponent<Image>().color=target.IsBrushing?new Color(.60f,.91f,.56f):new Color(.29f,.73f,.95f);
            actionIcon.sprite=target.IsBrushing?theme?.brushIcon:target.IsShearing?theme?.shearsIcon:
                target.Growth!=null && target.Growth.Type==AnimalType.Cow?theme?.milkIcon:theme?.productIcon;
            actionIcon.enabled=actionIcon.sprite!=null;
        }
        UpdateBubble(ownAction && target.IsBrushing && !GameplayPauseMenu.IsOpen);
    }
    void SetVisibility(bool value)
    {
        contextVisible=value; context.SetActive(value);
        if(mainHUD==null) mainHUD=FindFirstObjectByType<HUDManager>();
        mainHUD?.SetAnimalContextVisible(value);
    }
    void OnDisable()
    {
        mainHUD?.SetAnimalContextVisible(false);
        if(context!=null)context.SetActive(false);
        contextVisible=false;
        var active=AnimalController.CurrentCareAction;
        if(active!=null && active.playerInv==inventory) active.CancelCareAction(false);
        if(AnimalCarePanel.Instance?.Inventory==inventory) AnimalCarePanel.Instance.Close();
    }
    void OnDestroy(){if(canvasObject!=null)Destroy(canvasObject);}
    void BuildUI()
    {
        if(context!=null) return;
        canvasObject=new GameObject("AnimalInteractionCanvas_Runtime",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform,false);canvasObject.layer=5;
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=260;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        int size=GameplayUISettings.Current.uiSize;
        scaler.referenceResolution=new Vector2(1920,1080)/(size==0?.9f:size==2?1.1f:1f);scaler.matchWidthOrHeight=.5f;
        safe=GameplayHUDStyle.Rect("SafeArea",canvasObject.transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        context=GameplayHUDStyle.Rect("AnimalContext",safe,Vector2.zero,Vector2.one).gameObject;
        var time=Fixed("AnimalClock",context.transform,new Vector2(0,1),new Vector2(28,-28),new Vector2(320,92));
        Surface(time);Art(time,"SunImageSlot",theme?.sunIcon,new Vector2(.05f,.13f),new Vector2(.25f,.87f));
        clockText=Text(time,"Clock","",19,new Vector2(.30f,.06f),new Vector2(.96f,.94f));
        var money=Fixed("AnimalMoney",context.transform,Vector2.one,new Vector2(-28,-28),new Vector2(230,62));
        Surface(money);Art(money,"CoinImageSlot",theme?.coinIcon,new Vector2(.06f,.18f),new Vector2(.24f,.82f));
        moneyText=Text(money,"Money","",25,new Vector2(.30f,.04f),new Vector2(.95f,.96f));moneyText.alignment=TextAlignmentOptions.Center;
        var card=Fixed("AnimalInformationCard",context.transform,new Vector2(0,1),new Vector2(28,-142),new Vector2(430,312));
        informationCard=card;
        Surface(card);
        Art(card,"PortraitFrameImageSlot",theme?.portraitFrame,new Vector2(.045f,.68f),new Vector2(.23f,.945f));
        portrait=Art(card,"AnimalPortraitImageSlot",null,new Vector2(.05f,.69f),new Vector2(.225f,.94f));
        speciesText=Text(card,"Species","",19,new Vector2(.28f,.825f),new Vector2(.96f,.94f));speciesText.color=new Color(.66f,.86f,.61f);
        nameText=Text(card,"AnimalName","",31,new Vector2(.28f,.69f),new Vector2(.96f,.84f));nameText.fontStyle=FontStyles.Bold;
        Row(card,0,"Umur",theme?.ageIcon,out ageText);
        Row(card,1,"Heart",theme?.affectionIcon,out heartFallback);
        Row(card,2,"Jenis Kelamin",null,out genderText);
        genderIcon=Art(card,"GenderImageSlot",null,new Vector2(.05f,.205f),new Vector2(.105f,.28f));
        Row(card,3,"Status",null,out fourthValue);
        fourthLabel=card.Find("RowLabel_3").GetComponent<TMP_Text>();
        fourthIcon=Art(card,"ProductOrMoodImageSlot",null,new Vector2(.05f,.055f),new Vector2(.105f,.13f));
        for(int i=0;i<5;i++) hearts.Add(Art(card,"HeartImageSlot_"+i,null,new Vector2(.53f+i*.085f,.352f),new Vector2(.597f+i*.085f,.43f)));
        BuildDetailArea();
        var progress=Fixed("AnimalCareProgress",safe,new Vector2(.5f,0),new Vector2(0,205),new Vector2(440,106));
        progressRoot=progress.gameObject;Surface(progress);
        actionIcon=Art(progress,"CareToolImageSlot",null,new Vector2(.025f,.20f),new Vector2(.16f,.86f));
        actionLabel=Text(progress,"Action","",22,new Vector2(.20f,.59f),new Vector2(.97f,.96f));
        var track=GameplayHUDStyle.Rect("ProgressTrack",progress,new Vector2(.20f,.30f),new Vector2(.96f,.46f));
        track.gameObject.AddComponent<Image>().color=new Color(.08f,.12f,.14f,.9f);
        progressFill=GameplayHUDStyle.Rect("ProgressFill",track,Vector2.zero,new Vector2(0,1));progressFill.gameObject.AddComponent<Image>();
        remainingText=Text(progress,"Seconds","",18,new Vector2(.20f,.035f),new Vector2(.48f,.29f));
        Text(progress,"CancelHint","E / Esc: Batalkan",17,new Vector2(.49f,.035f),new Vector2(.96f,.29f)).alignment=TextAlignmentOptions.MidlineRight;
        bubble=GameplayHUDStyle.Rect("AnimalReactionImageSlot",safe,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
        bubble.sizeDelta=new Vector2(92,92);reactionImage=bubble.gameObject.AddComponent<Image>();reactionImage.preserveAspect=true;reactionImage.raycastTarget=false;
        reactionImage.sprite=theme?.brushReactionBubble;reactionImage.enabled=reactionImage.sprite!=null;
        context.SetActive(false);
        progressRoot.SetActive(false);
    }
    void RefreshCard(AnimalGrowthSystem animal)
    {
        string species=Species(animal.Type);speciesText.text="Nama "+species;
        nameText.text=animal.AnimalName==animal.Type.ToString()?species:animal.AnimalName;
        portrait.sprite=theme?.Portrait(animal.Type);portrait.enabled=portrait.sprite!=null;
        ageText.text=$"{Mathf.Max(0,animal.AgeSeasons)/4} Tahun {Mathf.Max(0,animal.AgeSeasons)%4} Musim";
        var label=context.transform.Find("AnimalInformationCard/RowLabel_1").GetComponent<TMP_Text>();label.text="Heart "+species;
        bool heartArt=theme?.fullHeart!=null && theme?.emptyHeart!=null;
        heartFallback.gameObject.SetActive(!heartArt);
        heartFallback.text=$"{animal.HeartPoints/200f:0.#} / 5";
        for(int i=0;i<5;i++)
        {
            int points=animal.HeartPoints-i*200;
            hearts[i].sprite=heartArt?(points>=200?theme.fullHeart:points>=100 && theme.halfHeart!=null?theme.halfHeart:theme.emptyHeart):null;
            hearts[i].enabled=heartArt;
        }
        genderText.text=animal.GenderLabel;genderIcon.sprite=animal.IsFemale?theme?.femaleIcon:theme?.maleIcon;genderIcon.enabled=genderIcon.sprite!=null;
        bool cow=animal.Type==AnimalType.Cow;
        fourthLabel.text=cow?"Ukuran Susu":animal.Type==AnimalType.Sheep?"Bulu Wol":"Status";
        fourthValue.text=cow?animal.MilkSizeLabel.Replace("Premium / Large","Premium / Besar").Replace("Medium","Sedang").Replace("Small","Kecil").Replace("Large","Besar"):
            animal.Type==AnimalType.Sheep?animal.SheepWoolStageLabel:Mood(animal);
        fourthValue.color=!cow && animal.Type!=AnimalType.Sheep && animal.IllnessStage==AnimalIllnessStage.Healthy?new Color(.63f,.91f,.64f):Color.white;
        fourthIcon.sprite=cow?theme?.milkIcon:theme?.moodIcon;fourthIcon.enabled=fourthIcon.sprite!=null;
    }
    void RefreshClock()
    {
        var t=TimeManager.Instance;
        clockText.text=Season()+ (t!=null?$"\nHari ke-{t.day}\n{t.hour:00}:{t.minute:00}":"\nJam belum tersedia");
        moneyText.text=(ScoreManager.Instance!=null?ScoreManager.Instance.points:0).ToString("N0",CultureInfo.GetCultureInfo("id-ID"))+" G";
    }
    void UpdateBubble(bool show)
    {
        var camera=Camera.main;
        bool visible=show && camera!=null && reactionImage.sprite!=null;
        if(visible)
        {
            var collider=target.GetComponent<Collider>();
            Vector3 world=target.transform.position+Vector3.up*1.5f;
            if(collider!=null)world.y=collider.bounds.max.y+.35f;
            Vector3 screen=camera.WorldToScreenPoint(world);visible=screen.z>0;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,null,out var point))bubble.anchoredPosition=point;
        }
        bubble.gameObject.SetActive(visible);
    }
    void BuildDetailArea()
    {
        detailArea=GameplayHUDStyle.Rect("AnimalDetailControls",context.transform,Vector2.zero,new Vector2(0,1));
        detailArea.offsetMin=new Vector2(28,24);detailArea.offsetMax=new Vector2(458,-468);
        Surface(detailArea);
        var viewport=GameplayHUDStyle.Rect("Viewport",detailArea,new Vector2(.035f,.09f),new Vector2(.965f,.98f));
        viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
        detailContents=GameplayHUDStyle.Rect("Content",viewport,new Vector2(0,1),Vector2.one);detailContents.pivot=new Vector2(.5f,1);
        var layout=detailContents.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.padding=new RectOffset(4,4,4,4);
        layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var fitter=detailContents.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=detailContents;
        var closeRect=GameplayHUDStyle.Rect("Close",detailArea,new Vector2(.035f,.015f),new Vector2(.965f,.075f));
        DetailButton(closeRect,"Tutup Detail [Esc]",()=>AnimalCarePanel.Instance?.Close());
    }
    void BuildDetailContents(AnimalCarePanel detail)
    {
        for(int i=detailContents.childCount-1;i>=0;i--){var child=detailContents.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
        refreshDetailRows.Clear();renderedDetail=detail;renderedRevision=detail.Revision;
        var animal=detail.Animal;
        var home=detail.Home;
        if(home!=null)
        {
            DetailLine("Kandang",()=> $"{home.Label} — Level {(home.site!=null?home.site.CurrentLevel:1)}\nHewan {home.AnimalCount} / {home.Capacity}\nSlot reservasi {home.ReservedSlots}  •  Slot kosong {home.AvailableSlots}");
            DetailLine("FeedStorage",()=> $"Tempat pakan: {home.TotalFeed} / {home.FeedingSlotCapacity}\nAnimal Feed {home.Fodder}  •  Grass {home.Grass}\nBelum makan: {home.RequiredFeedToday}  •  Auto Feeder: {(home.HasAutoFeeder?"Aktif":"Nonaktif")}\nDipakai hari ini: {home.FeedPortionsReserved} box\nKebutuhan: {home.DailyFeedRequirement} / hari  •  Stok: {home.EstimatedFeedDays} hari");
            DetailLine("FeedingGuide",()=>"Isi pakan: pegang Grass atau Animal Feed pada hotbar, dekati box kosong, lalu F untuk memasukkan satu item. Pakan yang dipakai berkurang pukul 00:00.");
            DetailLine("BellState",()=>home.AnimalsOutside?"Hewan di luar. Gunakan bell untuk memasukkan semuanya.":"Hewan di dalam. Gunakan bell untuk mengeluarkan semuanya.");
            foreach(var resident in home.Residents)
            {
                if(resident?.Animal==null)continue;
                var selected=resident.Animal;
                DetailButtonRow("Lihat: "+selected.AnimalName,()=>detail.Select(selected));
            }
        }
        if(animal!=null)
        {
            DetailLine("Health",()=> $"Kesehatan: {(animal.IllnessStage==AnimalIllnessStage.Healthy?"Sehat":Mood(animal))}\nKenyang: {animal.Fullness:0} / 100  •  Mood: {animal.Happiness:0} / 100\nTanpa makan: {animal.HungryDays} / 3 hari\nPertumbuhan: {GrowthLabel(animal.GrowthStage)}");
            DetailLine("DailyCare",()=> $"Perawatan hari ini\nMakan: {(animal.FedToday?"Selesai":"Belum")}  •  Gosok: {(animal.PetToday?"Selesai":"Belum")}"+(animal.Type==AnimalType.Cow?$"\nInteraksi: {(animal.InteractedToday?"Selesai":"Belum")}":""));
            if(animal.Type==AnimalType.Cow)DetailLine("CareRule",()=>"Lengkapi makan, gosok, dan interaksi harian untuk +1% heart. Perawatan tidak lengkap selama 7 hari berturut-turut mengurangi 5%.");
            DetailLine("Product",()=> $"Produk: {(animal.HasProductReady?"Siap diambil":"Belum siap")}\nNilai hewan: {animal.AnimalSellPrice:N0} G");
            var routine=animal.GetComponent<AnimalRoutine>();
            DetailLine("HomeAssignment",()=>"Kandang: "+(routine?.Home?.Label??"Belum ditugaskan"));
            BuildRename(detail);
            DetailButtonRow("Simpan Nama",detail.Rename);
            DetailButtonRow("Beri Makan — 1 Animal Feed",detail.Feed,()=>!animal.FedToday);
            DetailButtonRow("Gosok Hewan",detail.Brush,()=>!animal.PetToday);
            if(animal.Type==AnimalType.Cow)DetailButtonRow("Interaksi Harian",detail.Interact,()=>!animal.InteractedToday);
            DetailButtonRow("Beri Treat",detail.Treat,()=>animal.CanReceiveTreat);
            DetailButtonRow("Beri Obat",detail.Medicine,()=>animal.CanReceiveMedicine);
            DetailButtonRow(animal.Type==AnimalType.Cow?"Memerah Susu":animal.Type==AnimalType.Sheep?"Cukur — 5 Wool":"Ambil Produk",detail.Collect,()=>animal.HasProductReady);
            if(animal.Type==AnimalType.Sheep)DetailLine("Shearing",()=> $"{animal.SheepShearingSummary}\nPilih Shears pada hotbar sebelum mencukur. Wol tumbuh kembali dalam {AnimalGrowthSystem.SheepWoolRegrowthDays} hari.");
            DetailButtonRow(AnimalGrowthProfileSO.IsBird(animal.Type)?"Mulai Inkubasi":"Mulai Breeding",detail.Breed,()=>animal.IsAdult);
            if(routine!=null)foreach(var candidate in AnimalHome.Active)
            {
                if(candidate==null || candidate.Id==routine.HomeId || !candidate.HasRoom(animal.Type))continue;
                var destination=candidate;
                DetailButtonRow("Pindah: "+candidate.Label,()=>detail.Assign(destination),()=>destination!=null && destination.HasRoom(animal.Type));
            }
        }
        DetailLine("Feedback",()=>detail.Feedback??"");
        detailContents.anchoredPosition=Vector2.zero;
    }
    void DetailLine(string name,System.Func<string> value)
    {
        var text=Text(detailContents,name,value(),18,Vector2.zero,Vector2.one);
        text.textWrappingMode=TextWrappingModes.Normal;text.enableAutoSizing=false;
        var layout=text.gameObject.AddComponent<LayoutElement>();
        System.Action refresh=()=>
        {
            string next=value();if(text.text!=next)text.text=next;
            float height=Mathf.Max(28,text.GetPreferredValues(next,Mathf.Max(300,detailContents.rect.width-16),0).y+10);
            if(!Mathf.Approximately(layout.preferredHeight,height))layout.preferredHeight=height;
        };
        refresh();refreshDetailRows.Add(refresh);
    }
    void BuildRename(AnimalCarePanel detail)
    {
        var root=GameplayHUDStyle.Rect("RenameInput",detailContents,Vector2.zero,Vector2.one);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight=46;
        var image=root.gameObject.AddComponent<Image>();image.color=new Color(.23f,.30f,.32f,.85f);
        var input=root.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=image;input.characterLimit=24;
        var area=GameplayHUDStyle.Rect("TextArea",root,new Vector2(.04f,.04f),new Vector2(.96f,.96f));area.gameObject.AddComponent<RectMask2D>();input.textViewport=area;
        var text=Text(area,"Text","",20,Vector2.zero,Vector2.one);text.enableAutoSizing=false;input.textComponent=text;
        input.text=detail.EditedName??"";input.onValueChanged.AddListener(value=>detail.EditedName=value);
    }
    void DetailButtonRow(string label,UnityEngine.Events.UnityAction action,System.Func<bool> enabled=null)
    {
        var root=GameplayHUDStyle.Rect(label,detailContents,Vector2.zero,Vector2.one);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight=44;
        var button=DetailButton(root,label,action);
        if(enabled!=null)refreshDetailRows.Add(()=>button.interactable=enabled());
    }
    Button DetailButton(RectTransform root,string label,UnityEngine.Events.UnityAction action)
    {
        var image=GameplayHUDStyle.Surface(root,Color.white,8);image.raycastTarget=true;
        var button=root.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=new Color(.32f,.40f,.42f,.8f);colors.highlightedColor=new Color(.36f,.62f,.38f,.85f);
        colors.pressedColor=new Color(.27f,.48f,.29f,.95f);colors.selectedColor=colors.normalColor;colors.disabledColor=new Color(.23f,.28f,.29f,.6f);button.colors=colors;
        root.gameObject.AddComponent<MainMenuButtonAudio>();
        Text(root,"Label",label,18,new Vector2(.03f,0),new Vector2(.97f,1)).alignment=TextAlignmentOptions.Center;
        button.onClick.AddListener(action);return button;
    }

    static string Mood(AnimalGrowthSystem a)=>a.IllnessStage switch
    {AnimalIllnessStage.Mild=>"Kurang Sehat",AnimalIllnessStage.Severe=>"Sakit",AnimalIllnessStage.Critical=>"Sakit Parah",AnimalIllnessStage.Recovering=>"Pemulihan",_=>a.Happiness>=70?"Senang":a.Happiness>=35?"Tenang":"Stres"};
    static string Species(AnimalType type)=>type switch{AnimalType.Cow=>"Sapi",AnimalType.Chicken=>"Ayam",AnimalType.Duck=>"Bebek",AnimalType.Goat=>"Kambing",AnimalType.Sheep=>"Domba",_=>"Hewan"};
    static string GrowthLabel(AnimalGrowthStage stage)=>stage switch
    {AnimalGrowthStage.Egg=>"Telur",AnimalGrowthStage.Pregnancy=>"Dalam kandungan",AnimalGrowthStage.Hatchling=>"Baru menetas",AnimalGrowthStage.Newborn=>"Baru lahir",AnimalGrowthStage.Baby=>"Anak",AnimalGrowthStage.Young=>"Muda",AnimalGrowthStage.Adolescent=>"Remaja",AnimalGrowthStage.Adult=>"Dewasa",_=>"Belum diketahui"};
    static string Season()=>SeasonVisualController.CurrentSeason.ToString() switch{"Summer"=>"Musim Panas","Winter"=>"Musim Dingin","Autumn" or "Fall"=>"Musim Gugur",_=>"Musim Semi"};
    static RectTransform Fixed(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
    {var r=GameplayHUDStyle.Rect(name,parent,anchor,anchor);r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;}
    void Surface(RectTransform r)
    {
        if(theme?.panel!=null){var image=r.gameObject.AddComponent<Image>();image.sprite=theme.panel;image.type=Image.Type.Sliced;image.raycastTarget=false;}
        else GameplayHUDStyle.Surface(r,new Color(.14f,.19f,.21f,.88f),16);
    }
    static TMP_Text Text(Transform parent,string name,string value,float size,Vector2 min,Vector2 max)=>GameplayHUDStyle.Text(name,parent,value,size,min,max);
    static Image Art(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
    {var r=GameplayHUDStyle.Rect(name,parent,min,max);var img=r.gameObject.AddComponent<Image>();img.sprite=sprite;img.enabled=sprite!=null;img.preserveAspect=true;img.raycastTarget=false;return img;}
    void Row(RectTransform card,int row,string label,Sprite icon,out TMP_Text value)
    {
        float top=.64f-row*.15f, bottom=top-.15f;
        var line=GameplayHUDStyle.Rect("Divider",card,new Vector2(.04f,top),new Vector2(.96f,top+.003f));var image=line.gameObject.AddComponent<Image>();image.color=new Color(.8f,.85f,.85f,.22f);image.raycastTarget=false;
        Art(card,"RowImageSlot_"+row,icon,new Vector2(.05f,bottom+.04f),new Vector2(.105f,top-.035f));
        Text(card,"RowLabel_"+row,label,19,new Vector2(.14f,bottom),new Vector2(.52f,top));
        value=Text(card,"RowValue_"+row,"",19,new Vector2(.53f,bottom),new Vector2(.96f,top));
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureHUD()
    {var inv=FindFirstObjectByType<Inventory>();if(inv!=null && inv.GetComponent<AnimalInteractionHUD>()==null)inv.gameObject.AddComponent<AnimalInteractionHUD>();}
}

