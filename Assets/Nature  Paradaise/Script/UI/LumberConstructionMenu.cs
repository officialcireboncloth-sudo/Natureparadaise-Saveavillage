using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data-driven Lumber orders. Selection starts the existing placement/upgrade preview; payment happens at site confirmation.</summary>
public sealed class LumberConstructionMenu : MonoBehaviour
{
    public static LumberConstructionMenu Active { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActive()=>Active=null;
    sealed class Offer
    {
        public BuildingDefinitionSO definition;
        public PropertySite property;
        public BuildingSite fixedSite;
        public int catalogIndex, level;
        public bool Busy => property != null ? property.State == BuildingConstructionState.UnderConstruction :
            fixedSite != null && fixedSite.State == BuildingConstructionState.UnderConstruction;
        public ConstructionProject Project => property != null ? property.Construction : fixedSite != null ? fixedSite.Construction : null;
    }
    readonly List<Offer> offers = new();
    readonly List<Button> cards = new();
    readonly List<TMP_Text> labels = new();
    readonly List<Image> icons = new();
    GameObject canvas;
    TMP_Text title, detail, requirements, notice, page;
    Button order, previous, next;
    UpgradeShopFront store;
    BuildingCatalogSO catalog;
    PlayerController player;
    Inventory inventory;
    bool upgrading, open, oldCursor;
    CursorLockMode oldLock;
    int selection, openedFrame;
    public void Show(UpgradeShopFront front, bool upgrade)
    {
        if(open || Active != null || PropertySite.HasActivePlacementPreview())return;
        store=front;upgrading=upgrade;player=GetComponent<PlayerController>();inventory=GetComponent<Inventory>();
        catalog=front.constructionCatalog != null ? front.constructionCatalog : Resources.Load<BuildingCatalogSO>("Buildings/Lumber Building Catalog");
        if(catalog==null)catalog=Resources.Load<BuildingCatalogSO>("Buildings/Farm Building Catalog");
        offers.Clear();
        if(upgrading)
        {
            foreach(var site in PropertySite.ActiveSites)
                if(site!=null && site.IsUnlocked && site.ActiveDefinition != null &&
                   (site.State == BuildingConstructionState.UnderConstruction || site.State == BuildingConstructionState.Completed && site.ActiveDefinition.HasUpgradeAfter(site.CurrentLevel)))
                    offers.Add(new Offer {definition=site.ActiveDefinition,property=site,level=site.CurrentLevel+1});
            foreach(var site in BuildingSite.ActiveSites)
                if(site!=null && site.IsUnlocked && site.Definition != null &&
                   (site.State == BuildingConstructionState.UnderConstruction || site.State == BuildingConstructionState.Completed && site.Definition.HasUpgradeAfter(site.CurrentLevel)))
                    offers.Add(new Offer {definition=site.Definition,fixedSite=site,level=site.CurrentLevel+1});
        }
        else if(catalog!=null)
            for(int i=0;i<catalog.Count;i++)if(catalog.GetAt(i)!=null && catalog.GetAt(i).category != BuildingCategory.House)
                offers.Add(new Offer {definition=catalog.GetAt(i),catalogIndex=i,level=1});
        if(canvas==null)BuildUI();
        selection=0;open=true;Active=this;openedFrame=Time.frameCount;canvas.SetActive(true);
        player?.AcquireMovementLock(this);TimeManager.Instance?.AcquirePause(this);WorldInteractionPrompt.AcquireSuppression(this);
        oldCursor=Cursor.visible;oldLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(true);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(true);
        Refresh();GameplayInput.ConsumeCurrentFrame();
    }
    void BuildUI()
    {
        canvas=new GameObject("Lumber_Structure_Orders_Runtime",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=660;
        var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var backdrop=CommerceUIStyle.Panel("FullViewport_Backdrop",canvas.transform,0,0,1,1,new Color(.025f,.045f,.05f,.77f));
        GameplayHUDStyle.FullScreenBackground(backdrop);backdrop.raycastTarget=true;
        var safe=CommerceUIStyle.Rect("SafeArea",canvas.transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel=CommerceUIStyle.Panel("Orders",safe,.18f,.13f,.64f,.75f,CommerceUIStyle.Ink);
        title=CommerceUIStyle.Label("Heading",panel.transform,"",32,.045f,.875f,.72f,.075f,true);
        CommerceUIStyle.Button("Close",panel.transform,"Tutup ×",.79f,.885f,.17f,.065f).onClick.AddListener(()=>Close(true));
        for(int i=0;i<6;i++)
        {
            int index=i;var card=CommerceUIStyle.Button("Structure_"+i,panel.transform,"",.045f+i*.153f,.61f,.14f,.235f);
            card.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            icons.Add(CommerceUIStyle.ImageSlot("Illustration",card.transform,.12f,.38f,.76f,.55f));
            labels.Add(CommerceUIStyle.Label("Name",card.transform,"",21,.07f,.06f,.86f,.29f,true));labels[i].alignment=TextAlignmentOptions.Center;
            cards.Add(card);card.onClick.AddListener(()=>Select((selection/6)*6+index));
        }
        previous=CommerceUIStyle.Button("Previous",panel.transform,"‹",.045f,.53f,.06f,.06f);previous.onClick.AddListener(()=>Select(selection-6));
        next=CommerceUIStyle.Button("Next",panel.transform,"›",.895f,.53f,.06f,.06f);next.onClick.AddListener(()=>Select(selection+6));
        page=CommerceUIStyle.Label("Page",panel.transform,"",18,.40f,.535f,.20f,.04f);page.alignment=TextAlignmentOptions.Center;page.color=CommerceUIStyle.Muted;
        CommerceUIStyle.Panel("Details",panel.transform,.045f,.16f,.91f,.35f,CommerceUIStyle.Surface);
        detail=CommerceUIStyle.Label("Description",panel.transform,"",24,.07f,.20f,.39f,.27f,true);
        requirements=CommerceUIStyle.Label("Materials",panel.transform,"",21,.51f,.23f,.42f,.24f);
        notice=CommerceUIStyle.Label("Availability",panel.transform,"",19,.07f,.085f,.58f,.055f);notice.color=CommerceUIStyle.Muted;
        order=CommerceUIStyle.Button("Order",panel.transform,"Pilih lokasi",.70f,.08f,.255f,.065f,true);order.onClick.AddListener(Order);
        CommerceUIStyle.Label("KeyboardGuide",safe,"A / D  Pilih bangunan     Enter  Konfirmasi     Esc  Kembali ke Lumber",20,.20f,.06f,.60f,.045f).alignment=TextAlignmentOptions.Center;
    }
    void Update()
    {
        if(!open || Time.frameCount==openedFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){Close(true);return;}
        if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))Select(selection-1);
        if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))Select(selection+1);
        if(Input.GetKeyDown(KeyCode.Return))Order();
    }
    void Select(int value){selection=Mathf.Clamp(value,0,Mathf.Max(0,offers.Count-1));Refresh();}
    void Refresh()
    {
        title.text=upgrading?"LUMBER  /  UPGRADE STRUKTUR":"LUMBER  /  BANGUN STRUKTUR";
        for(int i=0;i<6;i++)
        {
            int index=selection/6*6+i;bool valid=index<offers.Count;cards[i].gameObject.SetActive(valid);if(!valid)continue;
            var offer=offers[index];labels[i].text=$"{offer.definition.displayName}\nLv.{offer.level}";
            icons[i].sprite=offer.definition.icon;icons[i].color=offer.definition.icon!=null?Color.white:Color.clear;
            CommerceUIStyle.Selection(cards[i],index==selection);
        }
        previous.interactable=selection>=6;next.interactable=selection/6*6+6<offers.Count;
        page.text=$"{(offers.Count>0?selection/6+1:0)} / {Mathf.CeilToInt(offers.Count/6f)}";
        order.interactable=false;
        if(offers.Count==0){detail.text=upgrading?"Belum ada struktur yang dapat di-upgrade.":"Belum ada bangunan yang tersedia.";requirements.text="";notice.text="Pilih layanan lain di Lumber.";return;}
        var selected=offers[selection];var level=selected.definition.GetLevel(selected.level);
        detail.text=$"{selected.definition.displayName}\n<color=#A8D4AC>Level {selected.level}</color>\n\n{level?.constructionDays??0} hari kerja\n{(upgrading?"Renovasi di lokasi struktur yang dipilih.":"Pilih lokasi di dunia setelah memesan.")}";
        requirements.text=BuildingCostUtility.BuildRequirementLabel(level,inventory);
        bool affordable=BuildingCostUtility.CanAfford(level,inventory,out var reason);
        order.interactable=!selected.Busy&&affordable;
        order.GetComponentInChildren<TMP_Text>().text=upgrading?"Preview upgrade":"Pilih lokasi";
        notice.text=selected.Busy?(selected.Project!=null?$"{selected.Project.Status} · {selected.Project.Progress:P0}":"Konstruksi sedang berjalan"):affordable?"Biaya dibayar saat konfirmasi proyek.":reason;
    }
    void Order()
    {
        if(!open || offers.Count==0 || !order.interactable)return;
        var selected=offers[selection];Close(false);bool started;
        if(upgrading)started=selected.property!=null?selected.property.BeginUpgradePreview():selected.fixedSite!=null&&selected.fixedSite.BeginPreview(selected.level);
        else
        {
            var site=PropertySite.GetOrCreateGlobalBuildSite(catalog,transform.position);
            started=site!=null&&site.BeginBuildSelection(selected.catalogIndex,1,false);
        }
        if(!started)SaveLoadFeedback.Instance?.ShowMessage("Preview proyek belum bisa dimulai");
        GameplayInput.ConsumeCurrentFrame();
    }
    void Close(bool returnToStore)
    {
        if(!open)return;open=false;if(Active==this)Active=null;canvas.SetActive(false);
        player?.ReleaseMovementLock(this);TimeManager.Instance?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
        Cursor.visible=oldCursor;Cursor.lockState=oldLock;GameplayInput.ConsumeCurrentFrame();
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(false);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(false);
        if(returnToStore && store!=null)store.Open();
    }
    void OnDisable()=>Close(false);
    void OnDestroy(){Close(false);if(canvas!=null)Destroy(canvas);}
}
