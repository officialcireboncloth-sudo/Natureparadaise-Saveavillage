using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One mouse/touch testing menu, available in the Editor and development builds. All actions use live gameplay services.</summary>
[DefaultExecutionOrder(-11000)]
public sealed class TestingDebugMenu : MonoBehaviour
{
    public static TestingDebugMenu Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;
    public static bool Available => PropertySite.DebugShortcutsEnabled;
    public static readonly string[] Categories = { "Player", "Item & Tools", "Hewan", "Waktu & Cuaca", "Kebun & Kolam", "Bangunan", "Progres & Aktivitas", "Save / Load" };
    GameObject canvasObject, overlay, launcher;
    RectTransform rows;
    TMP_Text heading, feedback;
    ScrollRect scroll;
    PlayerController player;
    TimeManager clock;
    Inventory inventory;
    bool open, cursorVisible;
    CursorLockMode cursorLock;
    float previousScale;
    int category, itemPage, amount = 1;
    string search = "", itemGroup = "Semua";
    ItemSO[] items;
    AnimalGrowthSystem selectedAnimal;
    readonly List<Button> tabs = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= SceneReady;
        SceneManager.sceneLoaded += SceneReady;
        Ensure();
    }
    static void SceneReady(Scene scene, LoadSceneMode mode) => Ensure();
    static void Ensure()
    {
        if (!Available || Instance != null) return;
        var actor = FindFirstObjectByType<PlayerController>();
        if (actor != null && actor.GetComponent<TestingDebugMenu>() == null) actor.gameObject.AddComponent<TestingDebugMenu>();
    }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        player = GetComponent<PlayerController>(); inventory = GetComponent<Inventory>();
    }
    void Start() { if (Available) Build(); }
    void Update()
    {
        if (!Available) return;
        if (open)
        {
            if (LoadingScreen.IsVisible || SaveManager.Instance != null && SaveManager.Instance.IsLoading) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        else if (GameplayInput.GetKeyDown(KeyCode.F12)) Open();
        if (launcher != null) launcher.SetActive(!open && !GameplayPauseMenu.IsOpen && !WorldInteractionPrompt.IsSuppressed && !LoadingScreen.IsVisible);
    }

    /// <summary>Opens one paused testing window; no action runs just by opening or selecting a category.</summary>
    public void Open(int page = -1)
    {
        if (!Available || open || player == null || GameplayPauseMenu.BlocksGameplayInput || WorldInteractionPrompt.IsSuppressed || LoadingScreen.IsVisible || PropertySite.HasActivePlacementPreview()) return;
        if (canvasObject == null) Build();
        clock = TimeManager.Instance; clock?.AcquirePause(this);
        player.AcquireMovementLock(this); WorldInteractionPrompt.AcquireSuppression(this);
        previousScale = Time.timeScale; Time.timeScale = 0;
        cursorVisible = Cursor.visible; cursorLock = Cursor.lockState; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        open = true; overlay.SetActive(true); launcher.SetActive(false);
        items ??= Resources.LoadAll<ItemSO>("Items").Where(i => i != null).GroupBy(i => i.Id).Select(g => g.First()).OrderBy(i => i.itemName).ToArray();
        SelectCategory(page >= 0 ? page : category);
        GameplayInput.ConsumeCurrentFrame();
    }
    /// <summary>Releases only this menu's input, time and movement locks.</summary>
    public void Close()
    {
        if (!open) return;
        open = false; if (overlay != null) overlay.SetActive(false);
        Time.timeScale = previousScale; clock?.ReleasePause(this); player?.ReleaseMovementLock(this);
        WorldInteractionPrompt.ReleaseSuppression(this); Cursor.visible = cursorVisible; Cursor.lockState = cursorLock;
        GameplayInput.ConsumeCurrentFrame(); EventSystem.current?.SetSelectedGameObject(null);
    }
    void OnDisable() => Close();
    void OnDestroy() { Close(); if (canvasObject != null) Destroy(canvasObject); if (Instance == this) Instance = null; }

    void Build()
    {
        canvasObject = new GameObject("Testing Debug Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1800;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new(1280,720); scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null) new GameObject("Debug EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var safe = GameplayHUDStyle.Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one); safe.gameObject.AddComponent<SafeAreaFitter>();
        launcher = ButtonAt(safe, "F12 · DEBUG", new(.015f,.79f), new(.15f,.86f), () => Open()).gameObject;
        var shade = GameplayHUDStyle.Rect("Debug Overlay", safe, Vector2.zero, Vector2.one); overlay = shade.gameObject;
        var dim = shade.gameObject.AddComponent<Image>(); dim.color = new(0,0,0,.65f); dim.raycastTarget = true;
        var panel = GameplayHUDStyle.Rect("Testing Menu", shade, new(.025f,.035f), new(.975f,.965f)); GameplayHUDStyle.Surface(panel, new(.065f,.105f,.13f,.99f), 14).raycastTarget = true;
        heading = Label(panel, "DEBUG · TESTING", new(.025f,.90f), new(.82f,.98f), 26);
        ButtonAt(panel, "Tutup ×", new(.84f,.905f), new(.975f,.98f), Close);
        Label(panel, "Pilih kategori. Perubahan gameplay ikut tersimpan jika kamu menyimpan game.", new(.025f,.85f), new(.97f,.90f), 16);
        for (int i=0;i<Categories.Length;i++)
        {
            int page=i; float top=.825f-i*.092f;
            tabs.Add(ButtonAt(panel, Categories[i], new(.02f,top-.08f), new(.255f,top), () => SelectCategory(page)));
        }
        var viewport=GameplayHUDStyle.Rect("Action Viewport",panel,new(.275f,.115f),new(.98f,.83f));
        viewport.gameObject.AddComponent<Image>().color=new(.1f,.15f,.18f,1); viewport.gameObject.AddComponent<RectMask2D>();
        scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.scrollSensitivity=40; scroll.movementType=ScrollRect.MovementType.Clamped;
        rows=GameplayHUDStyle.Rect("Actions",viewport,new(0,1),Vector2.one); rows.pivot=new(.5f,1);
        var layout=rows.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding=new RectOffset(12,12,12,12); layout.spacing=8; layout.childControlHeight=true; layout.childForceExpandHeight=false; layout.childControlWidth=true; layout.childForceExpandWidth=true;
        rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport; scroll.content=rows;
        feedback=Label(panel,"F12 / Esc untuk menutup · Mendukung klik dan sentuh",new(.275f,.02f),new(.975f,.10f),18);
        overlay.SetActive(false);
    }
    static TMP_Text Label(Transform parent,string text,Vector2 min,Vector2 max,float size=20)
    {
        var label=GameplayHUDStyle.Text("Label",parent,text,size,min,max); label.enableAutoSizing=true; label.fontSizeMin=14; label.fontSizeMax=size; label.raycastTarget=false; return label;
    }
    static Button ButtonAt(Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var rect=GameplayHUDStyle.Rect(title,parent,min,max); var image=GameplayHUDStyle.Surface(rect,new(.22f,.32f,.34f,1),8); image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image; GameplayHUDStyle.ButtonStates(button);
        var caption=Label(rect,title,new(.035f,.08f),new(.965f,.92f)); caption.alignment=TextAlignmentOptions.Center;
        button.onClick.AddListener(action); button.navigation=new Navigation{mode=Navigation.Mode.None}; return button;
    }
    RectTransform Row(string name,float height)
    {
        var rect=GameplayHUDStyle.Rect(name,rows,Vector2.zero,Vector2.one); var layout=rect.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight=height; layout.minHeight=height; return rect;
    }
    void Note(string text,float height=56) => Label(Row("Info",height),text,new(.02f,0),new(.98f,1),18);
    void Action(string title,System.Action action,bool enabled=true)
    {
        var button=ButtonAt(Row(title,58),title,Vector2.zero,Vector2.one,()=>
        {
            try { string before=feedback.text; action(); if(open && feedback.text==before) feedback.text="Selesai · "+title; }
            catch(Exception error) { feedback.text="Gagal · "+error.Message; Debug.LogException(error); }
        }); button.interactable=enabled;
    }
    void Launch(string title,System.Action action,bool enabled=true) => Action(title,()=>{ Close(); action(); },enabled);

    /// <summary>Rebuilds only the selected category; inactive scenes and missing systems stay unavailable.</summary>
    public void SelectCategory(int page)
    {
        category=Mathf.Clamp(page,0,Categories.Length-1);
        for(int i=rows.childCount-1;i>=0;i--){var child=rows.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
        for(int i=0;i<tabs.Count;i++)
        {
            tabs[i].GetComponentInChildren<TMP_Text>().fontStyle=i==category?FontStyles.Bold:FontStyles.Normal;
            var colors=tabs[i].colors;colors.normalColor=i==category?new Color(.38f,.66f,.48f):Color.white;colors.selectedColor=colors.normalColor;tabs[i].colors=colors;
        }
        heading.text="DEBUG · "+Categories[category]; feedback.text="Pilih aksi · Scroll / geser untuk melihat lainnya";
        switch(category)
        {
            case 0: PlayerPage();break; case 1: ItemsPage();break; case 2: AnimalsPage();break;
            case 3: WorldPage();break; case 4: FarmPage();break; case 5: BuildingsPage();break;
            case 6: ProgressPage();break; case 7: SavesPage();break;
        }
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition=1;
    }
    void RefreshPage() => SelectCategory(category);
    void PlayerPage()
    {
        var status=player.GetComponent<PlayerStatusSystem>();
        Note(status!=null?$"HP {status.Health:0}/{status.MaxHealth:0} · Stamina {status.Stamina:0}/{status.MaxStamina:0} · Tas Lv.{inventory.BackpackLevel}":"Player status tidak tersedia");
        Action("Pulihkan HP, stamina dan lapar",()=>status.ApplyFood(status.MaxHealth,status.MaxStamina,status.MaxHunger),status!=null);
        Action("Tambah 10.000 G",()=>ScoreManager.Instance.AddPoints(10000),ScoreManager.Instance!=null);
        Action("Upgrade tas satu level",()=>{if(!inventory.TryUpgradeBackpack())feedback.text="Tas sudah maksimum";});
        Action("Naikkan tier stamina",()=>status.TryUpgradeStamina(),status!=null);
        Action("Lengkapi semua alat yang belum dimiliki",GrantMissingTools);
        foreach(var tool in Enum.GetValues(typeof(PlayerToolType)).Cast<PlayerToolType>().Where(t=>t!=PlayerToolType.None))
        {
            var captured=tool;
            Action($"{PlayerToolHotbar.GetDisplayName(tool)} · level {(status!=null?status.GetToolLevel(tool):1)} → +1",()=>{status.SetToolLevel(captured,Mathf.Min(5,status.GetToolLevel(captured)+1));RefreshPage();},status!=null);
        }
    }
    void GrantMissingTools()
    {
        int added=0;
        foreach(var item in items.Where(i=>i.category==ItemCategory.Tool))
        {
            if(inventory.GetCount(item)>0 || ToolStorageService.Entries.Any(e=>e!=null&&e.item==item&&e.count>0) || HouseStorageService.Entries.Any(e=>e!=null&&e.item==item&&e.count>0))continue;
            if(!inventory.Add(item,1))break; added++;
        }
        feedback.text=$"Alat baru: {added}. Item yang sudah dimiliki dilewati.";
    }
    void ItemsPage()
    {
        Note("Semua item dari katalog game: tools, benih, material, pakan, obat, ikan, makanan dan hasil ternak. Tidak mengganti isi hotbar.");
        var row=Row("Item Search",56);
        var inputRect=GameplayHUDStyle.Rect("Cari item",row,Vector2.zero,Vector2.one); var surface=inputRect.gameObject.AddComponent<Image>();surface.color=new(.18f,.24f,.27f,1);
        var input=inputRect.gameObject.AddComponent<TMP_InputField>();var text=Label(inputRect,search,new(.025f,.05f),new(.975f,.95f));text.raycastTarget=true;input.textViewport=inputRect;input.textComponent=text;input.text=search;
        var placeholder=Label(inputRect,"Cari nama item / ID…",new(.025f,.05f),new(.975f,.95f));placeholder.color=new(.6f,.7f,.73f);input.placeholder=placeholder;
        input.onEndEdit.AddListener(value=>{if(search==value)return;search=value;itemPage=0;RefreshPage();});
        var options=Row("Item Filters",52);
        string[] groups={"Semua","Tool","Seed","Material","Food","Fish","AnimalProduct","Bait","General","Weapon","Quest"};
        var dropdownRect=GameplayHUDStyle.Rect("Category Filter",options,new(0,0),new(.48f,1));
        // A cycling filter keeps the touch target generous and avoids a second overlay.
        ButtonAt(dropdownRect,itemGroup,Vector2.zero,Vector2.one,()=>{itemGroup=groups[(Array.IndexOf(groups,itemGroup)+1)%groups.Length];itemPage=0;RefreshPage();});
        ButtonAt(options,$"Jumlah: x{amount}",new(.51f,0),Vector2.one,()=>{amount=amount==1?10:amount==10?99:1;RefreshPage();});
        var found=items.Where(i=>(itemGroup=="Semua"||i.category.ToString()==itemGroup) && (string.IsNullOrWhiteSpace(search)||i.itemName.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||i.Id.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        itemPage=Mathf.Clamp(itemPage,0,Mathf.Max(0,(found.Length-1)/24));
        Note($"{found.Length} item · Halaman {itemPage+1}/{Mathf.Max(1,(found.Length+23)/24)} · Ketuk item untuk menambahkan");
        var pager=Row("Pages",50);
        ButtonAt(pager,"‹ Sebelumnya",Vector2.zero,new(.48f,1),()=>{itemPage=Mathf.Max(0,itemPage-1);RefreshPage();});
        ButtonAt(pager,"Berikutnya ›",new(.52f,0),Vector2.one,()=>{itemPage=Mathf.Min(Mathf.Max(0,(found.Length-1)/24),itemPage+1);RefreshPage();});
        foreach(var item in found.Skip(itemPage*24).Take(24))
        {
            var value=item; int count=item.category==ItemCategory.Tool?1:amount;
            var itemRow=Row(item.Id,62);var button=ButtonAt(itemRow,$"{item.itemName}  +{count}",Vector2.zero,Vector2.one,()=>feedback.text=inventory.Add(value,count)?$"Ditambahkan: {value.itemName} x{count}":"Tas penuh; tidak ada item yang ditambahkan");
            var label=button.GetComponentInChildren<TMP_Text>();label.rectTransform.anchorMin=new(.12f,.05f);label.alignment=TextAlignmentOptions.MidlineLeft;
            if(item.icon!=null){var rect=GameplayHUDStyle.Rect("Icon",itemRow,new(.01f,.1f),new(.10f,.9f));var image=rect.gameObject.AddComponent<Image>();image.sprite=item.icon;image.preserveAspect=true;image.raycastTarget=false;}
        }
    }
    void AnimalsPage()
    {
        var animals=AnimalGrowthSystem.OwnedAnimals.Where(a=>a!=null&&a.gameObject.activeInHierarchy).OrderBy(a=>a.AnimalName).ToArray();
        Note($"Hewan milik player: {animals.Length}. Termasuk yang berada di kandang. Contoh hewan bawaan scene tidak diaktifkan.");
        var animalCatalog=Resources.Load<ShopCatalogSO>("Shops/Animal Catalog");
        var animalStore=FindObjectsByType<ShopFront>(FindObjectsSortMode.None).FirstOrDefault(s=>s.catalog==animalCatalog&&animalCatalog!=null);
        if(animalStore!=null)Launch("Buka toko hewan · beli hewan untuk testing",animalStore.Open);
        Action("Sembuhkan semua hewan",()=>{foreach(var animal in animals)animal.ApplyTestAction(AnimalTestAction.Heal);RefreshPage();},animals.Length>0);
        Action("Dewasakan semua hewan (kecuali telur di penetas)",()=>{foreach(var animal in animals)animal.ApplyTestAction(AnimalTestAction.MakeAdult);RefreshPage();},animals.Length>0);
        if(selectedAnimal==null||!animals.Contains(selectedAnimal))selectedAnimal=animals.FirstOrDefault();
        foreach(var animal in animals){var captured=animal;Action((selectedAnimal==animal?"● ":"")+animal.AnimalName+" · "+animal.Type+" · "+animal.GenderLabel+" · "+animal.GrowthStage+" · "+animal.HealthSummary,()=>{selectedAnimal=captured;RefreshAnimalsPage();});}
        if(selectedAnimal!=null)
        {
            var selected=selectedAnimal;Note("TERPILIH · "+selected.AnimalName+"\n"+selected.StatusSummary,112);
            var genderRow=Row("Jenis Kelamin",58);
            ButtonAt(genderRow,selected.IsFemale?"Betina (aktif)":"Ubah ke Betina",Vector2.zero,new(.48f,1),()=>RunAnimalAction("Jenis kelamin: Betina",selected,AnimalTestAction.MakeFemale));
            ButtonAt(genderRow,!selected.IsFemale?"Jantan (aktif)":"Ubah ke Jantan",new(.52f,0),Vector2.one,()=>RunAnimalAction("Jenis kelamin: Jantan",selected,AnimalTestAction.MakeMale));
            Note(selected.ProducesMilk
                ? "SUSU · "+(selected.HasProductReady?"Siap diperah":"Belum siap")+"\n"+(selected.HasProductReady?"Pilih Milker, tutup debug, lalu dekati hewan.":selected.MilkProductionBlockReason)
                : "PRODUK · "+(selected.HasProductReady?"Siap diambil":"Belum siap"),72);
            AnimalAction(selected.ProducesMilk?"Siap diperah · ON (dewasa + sehat + kenyang)":"Siapkan telur / wol (dewasa + sehat + kenyang)",selected,AnimalTestAction.ReadyProduct,!selected.ProducesMilk||selected.IsFemale);
            AnimalAction(selected.ProducesMilk?"Belum siap diperah · OFF (cooldown hari ini)":"Produk belum siap · OFF (mulai cooldown)",selected,AnimalTestAction.ClearProduct);
            AnimalAction("Sembuhkan langsung",selected,AnimalTestAction.Heal);
            AnimalAction("Jadikan dewasa",selected,AnimalTestAction.MakeAdult);
            AnimalAction("Kenyang dan bahagia",selected,AnimalTestAction.FillNeeds);
            AnimalAction("Jadikan sakit untuk tes obat",selected,AnimalTestAction.MakeSick);
        }
        foreach(var companion in FindObjectsByType<PersonalAnimal>(FindObjectsSortMode.None))
        {
            var captured=companion;Note("COMPANION · "+captured.DisplayName);
            foreach(var command in Enum.GetValues(typeof(CompanionCommand)).Cast<CompanionCommand>())
            {var value=command;Launch(captured.DisplayName+" · "+value,()=>captured.SetCommand(value,player.transform));}
            Action(captured.DisplayName+" · Kedekatan +100",()=>captured.AddHeart(100));
        }
    }
    void RefreshAnimalsPage()
    {
        float position=scroll.verticalNormalizedPosition;
        RefreshPage();
        scroll.verticalNormalizedPosition=position;
    }
    void AnimalAction(string title,AnimalGrowthSystem animal,AnimalTestAction action,bool enabled=true) => Action(title,()=>RunAnimalAction(title,animal,action),enabled);
    void RunAnimalAction(string title,AnimalGrowthSystem animal,AnimalTestAction action)
    {
        bool success=animal!=null&&animal.ApplyTestAction(action);RefreshAnimalsPage();feedback.text=success?title+" · selesai":"Tidak bisa: cek jenis kelamin / telur dalam penetas / hewan sudah hilang";
    }
    void WorldPage()
    {
        var weather=WeatherSystem.Instance;var season=SeasonVisualController.Instance;
        Note($"Hari {clock?.day} · {clock?.hour:00}:{clock?.minute:00} · {SeasonVisualController.CurrentSeason} · {(weather!=null?WeatherSystem.GetShortName(weather.CurrentWeather):"—")}");
        Launch("Hari berikutnya · proses reset harian normal",()=>clock?.AdvanceToNextDay());
        foreach(int hour in new[]{6,12,18,21,1,2}){int h=hour;Action($"Atur jam {h:00}:00",()=>{clock?.SetClockSameDay(h);RefreshPage();},clock!=null);}
        foreach(var value in new[]{CropSeason.Spring,CropSeason.Summer,CropSeason.Autumn,CropSeason.Winter}){var s=value;Action("Preview musim · "+s,()=>{season.SetTestSeason(s);RefreshPage();},season!=null);}
        Action("Musim kembali mengikuti kalender",()=>{season.FollowCalendar();RefreshPage();},season!=null);
        foreach(var value in Enum.GetValues(typeof(WeatherType)).Cast<WeatherType>()){var w=value;Action("Cuaca · "+WeatherSystem.GetShortName(w),()=>{weather.SetDebugWeather(w);RefreshPage();},weather!=null);}
        Action("Cuaca otomatis sesuai musim",()=>{weather.FollowSeasonWeather();RefreshPage();},weather!=null);
    }
    void FarmPage()
    {
        Note("Aksi kebun berlaku pada petak yang sudah dicangkul dalam radius 2 petak dari player; bangunan dan tanaman tetap ada.");
        Action("Sirami petak di sekitar player",()=>ApplyNearby(false));
        Action("Pupuk petak di sekitar player",()=>ApplyNearby(true));
        Note("Benih / pupuk / booster / pakan ikan bisa ditambahkan melalui Item & Tools. Pertumbuhan kalender ada di Waktu & Cuaca.");
        foreach(var pond in FindObjectsByType<FishPond>(FindObjectsSortMode.None))
        {
            var p=pond;Note(p.name+$" · {p.FishCount}/{p.Capacity} ikan");
            Action(p.name+" · Pertumbuhan +1 hari",()=>p.DebugAdvanceGrowth(false));
            Action(p.name+" · Naik ukuran",()=>p.DebugAdvanceGrowth(true));
            Launch(p.name+" · Buka panel kolam",p.OpenPanel);
        }
    }
    void ApplyNearby(bool fertilizer)
    {
        if(!FieldArea.TryGetAt(player.transform.position,out var field,out int x,out int z)){feedback.text="Player belum berada di field";return;}
        int count=0;
        for(int dx=-2;dx<=2;dx++)for(int dz=-2;dz<=2;dz++)
            if(fertilizer?field.TryFertilize(x+dx,z+dz,5):field.TryWater(x+dx,z+dz))count++;
        feedback.text=$"Petak berhasil diproses: {count}";
    }
    void BuildingsPage()
    {
        Note("Bangun gratis tetap menggunakan preview lokasi dan tukang. Pilih posisi yang valid, lalu konfirmasi seperti biasa.");
        foreach(var catalog in Resources.LoadAll<BuildingCatalogSO>("").OrderBy(c=>c.name))
            for(int i=0;i<catalog.Count;i++)
            {
                int index=i;var shared=catalog;var definition=catalog.GetAt(i);if(definition==null)continue;
                Launch("Bangun gratis · "+definition.name,()=>
                {
                    var site=PropertySite.GetOrCreateGlobalBuildSite(shared,player.transform.position);
                    if(site==null||!site.BeginBuildSelection(index,1,true))SaveLoadFeedback.Instance?.ShowMessage("Belum bisa memulai preview bangunan di lokasi ini");
                });
            }
        foreach(var site in PropertySite.ActiveSites.Where(s=>s!=null&&!s.IsEmpty).ToArray())
        {
            var s=site;Note(s.name+$" · Lv.{s.CurrentLevel} · {s.State}");
            Launch(s.name+" · Preview upgrade gratis",()=>{if(s.BeginUpgradePreview())s.EnableFreeDebugPreview();});
            Launch(s.name+" · Pindahkan",()=>s.BeginRelocationMode());
        }
        var interior=FindFirstObjectByType<HouseInteriorController>();
        if(interior!=null&&interior.IsPlayerInside)
        {
            for(int i=1;i<=interior.AvailableLayoutCount;i++){int level=i;Launch("Preview interior rumah Lv."+i,()=>interior.SetDebugLayout(level));}
            Launch("Interior rumah kembali ke level asli",interior.ClearDebugLayout);
        }
        else Note("Preview layout rumah tersedia setelah masuk rumah.");
    }
    void ProgressPage()
    {
        var village=VillageProgressionService.Instance;
        Action("Desa naik satu level",()=>{village.SetLevel(village.VillageLevel+1);RefreshPage();},village!=null);
        Action("Bypass persyaratan progres · "+(ProgressionRequirementSettings.BypassEnabled?"ON":"OFF"),()=>{ProgressionRequirementSettings.SetRuntimeBypass(!ProgressionRequirementSettings.BypassEnabled);RefreshPage();});
        var hud=FindFirstObjectByType<HUDManager>();
        Action("Petunjuk debug dunia · "+(HUDManager.DebugCluesEnabled?"ON":"OFF"),()=>{hud.SetDebugCluesEnabled(!hud.showDebugClues,false);RefreshPage();},hud!=null);
        foreach(var shop in FindObjectsByType<ShopFront>(FindObjectsSortMode.None)){var s=shop;Launch("Toko · "+(s.catalog!=null?s.catalog.name:s.name),s.Open);}
        foreach(var shop in FindObjectsByType<UpgradeShopFront>(FindObjectsSortMode.None)){var s=shop;Launch("Upgrade / Lumber · "+s.name,s.Open);}
        foreach(var modal in FindObjectsByType<DataDrivenModal>(FindObjectsSortMode.None)){var m=modal;Launch("Aktivitas · "+m.name,m.Open);}
        var quests=QuestService.Instance;
        var dialogue=DialogueService.Instance;
        var portraitPreview=Resources.Load<DialogueConversationSO>("Narrative/Testing/Dialogue_PortraitPreview");
        Launch("Dialog · Preview portrait dan 6 ekspresi",()=>dialogue.StartConversation(portraitPreview),dialogue!=null&&portraitPreview!=null);
        var minaQuest=Resources.Load<QuestDefinitionSO>("Narrative/Testing/Quest_WoodForMina");
        if(quests!=null&&minaQuest!=null)
        {
            string stage=quests.GetStatus(minaQuest) switch { QuestStatus.Active=>"Active",QuestStatus.ReadyToTurnIn=>"Ready",QuestStatus.Completed=>"Completed",_=>"Intro" };
            var minaConversation=Resources.Load<DialogueConversationSO>("Narrative/Testing/Dialogue_Mina_"+stage);
            Launch("Dialog · Bicara dengan Mina (sesuai progres misi)",()=>dialogue.StartConversation(minaConversation),dialogue!=null&&minaConversation!=null);
        }
        if(quests!=null)foreach(var quest in quests.Definitions)
        {
            var q=quest;Note("MISI · "+q.title+" · "+quests.GetStatus(q));
            Action("Mulai misi · "+q.title,()=>{quests.StartQuest(q);RefreshPage();});
        }
    }
    void SavesPage()
    {
        Note("Save dan Load menggunakan pemilih Slot 1–4 yang sama. Autosave tetap terpisah. Menu ini tidak menyimpan otomatis.");
        var saves=SaveManager.Instance;
        Launch("Simpan · pilih Slot 1–4",()=>saves.RequestSave(),saves!=null);
        Launch("Load · pilih save data",()=>saves.RequestLoad(),saves!=null);
    }
}
