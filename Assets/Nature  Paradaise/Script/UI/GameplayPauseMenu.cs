using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Data-driven pause pages. Artwork is configured through the theme asset.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class GameplayPauseMenu : MonoBehaviour
{
    [SerializeField] PauseMenuTheme theme;
    [SerializeField] KeyCode editorDebugKey = KeyCode.F7;
    [SerializeField] bool allowEscapeInEditor;
    public static bool IsOpen { get; private set; }
    static int transitionFrame = -1;
    public static bool BlocksGameplayInput => IsOpen || Time.frameCount == transitionFrame;
    GameObject panel, overlay;
    RectTransform window, content;
    PlayerController player;
    TimeManager clock;
    float previousScale;
    bool open;
    int page, settingsPage, animalFilter, questFilter;
    AnimalGrowthSystem selectedAnimal;
    PersonalAnimal selectedCompanion;
    QuestDefinitionSO selectedQuest;
    GameplayUISettings draft;
    readonly List<Button> tabs = new();
    string[] pageNames = { "Ringkasan", "Hewan", "Kebunku", "Misi", "Pengaturan" };
    RectTransform menuLauncher;
    public void SetModalContextVisible(bool visible){if(menuLauncher!=null)menuLauncher.gameObject.SetActive(!visible);}
    Color Glass => draft != null && draft.highContrast ? new Color(.055f,.09f,.13f,.98f) : GameplayHUDStyle.Modal;

    void Start()
    {
        if (theme == null) theme = Resources.Load<PauseMenuTheme>("UI/PauseMenuTheme");
        if (theme != null) { editorDebugKey = theme.editorPauseKey; allowEscapeInEditor = theme.allowEscapeInEditor; }
        var menu = GameplayHUDStyle.Rect("Menu Button", transform.parent, Vector2.one, Vector2.one);
        menuLauncher=menu;menu.gameObject.SetActive(DataDrivenModal.Active==null);
        menu.pivot=Vector2.one; menu.sizeDelta=new Vector2(100,52); menu.anchoredPosition=new Vector2(-20,-20);
        MakeButton(menu, MenuKeyLabel()+"  Menu", Toggle);
        var root=GameplayHUDStyle.Rect("Pause Overlay",transform.parent,Vector2.zero,Vector2.one);
        var canvas=root.gameObject.AddComponent<Canvas>(); canvas.overrideSorting=true; canvas.sortingOrder=450;
        root.gameObject.AddComponent<GraphicRaycaster>();
        overlay=root.gameObject;
        var shade=root.gameObject.AddComponent<Image>(); shade.color=new Color(0,0,0,.38f); shade.raycastTarget=true;
        if(theme != null && theme.background != null) { var back=Artwork(root,"Background Image Slot",theme.background,Vector2.zero,Vector2.one); back.preserveAspect=false; back.transform.SetAsFirstSibling(); }
        window=GameplayHUDStyle.Rect("Pause Menu",root,new(.13f,.05f),new(.87f,.90f));
        var surface=GameplayHUDStyle.Surface(window,GameplayHUDStyle.Modal,12); surface.raycastTarget=true;
        if(theme != null && theme.panel != null) { surface.enabled=false; var art=Artwork(window,"Panel Image Slot",theme.panel,Vector2.zero,Vector2.one); art.preserveAspect=false; art.transform.SetAsFirstSibling(); }
        Sprite[] tabArt={theme?.summaryIcon,theme?.animalsIcon,theme?.farmIcon,theme?.questsIcon,theme?.settingsIcon};
        for(int i=0;i<5;i++) {
            int captured=i; var rect=GameplayHUDStyle.Rect(pageNames[i]+" Tab",window,new(.035f+i*.167f,.88f),new(.195f+i*.167f,.965f));
            Button tab=MakeButton(rect,pageNames[i],()=>ShowPage(captured)); tabs.Add(tab);
            if(tabArt[i]!=null) { Artwork(rect,"Icon Image Slot",tabArt[i],new(.04f,.18f),new(.24f,.82f)); tab.GetComponentInChildren<TMP_Text>().rectTransform.anchorMin=new Vector2(.27f,.05f); }
        }
        ButtonAt(window,"×",new(.925f,.88f),new(.985f,.965f),Close);
        content=GameplayHUDStyle.Rect("Page Content",window,new(.025f,.12f),new(.975f,.86f));
        panel=window.gameObject; overlay.SetActive(false);
    }
    string MenuKeyLabel() {
#if UNITY_EDITOR
        return editorDebugKey.ToString();
#else
        return "ESC";
#endif
    }
    bool HasInputFocus() {
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;
        return UnityEditor.EditorApplication.isPlaying && focus != null && focus.GetType().Name == "GameView";
#else
        return Application.isFocused;
#endif
    }
    void Update()
    {
        if(!HasInputFocus() || GameplayInput.ConsumedThisFrame) return;
        if(!open && AnimalController.CurrentCareAction != null && Input.GetKeyDown(KeyCode.Escape)) return;
#if UNITY_EDITOR
        bool pressed=Input.GetKeyDown(editorDebugKey) || (allowEscapeInEditor && Input.GetKeyDown(KeyCode.Escape));
#else
        bool pressed=Input.GetKeyDown(KeyCode.Escape);
#endif
        if(pressed && !InventoryUI.ConsumedCloseInputThisFrame && (open || !WorldInteractionPrompt.IsSuppressedExcept(AnimalController.CurrentCareAction))) Toggle();
    }
    void Toggle()
    {
        if(open) { Close(); return; }
        if(panel == null || IsOpen || WorldInteractionPrompt.IsSuppressedExcept(AnimalController.CurrentCareAction)) return;
        open=true; IsOpen=true; transitionFrame=Time.frameCount;
        previousScale=Time.timeScale; Time.timeScale=0;
        clock=TimeManager.Instance; clock?.AcquirePause(this);
        player=FindFirstObjectByType<PlayerController>(); player?.AcquireMovementLock(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        draft=GameplayUISettings.Current.Copy(); overlay.SetActive(true); ShowPage(page);
    }
    public void Close()
    {
        if(!open) return;
        open=false; IsOpen=false; transitionFrame=Time.frameCount;
        if(overlay != null) overlay.SetActive(false);
        Time.timeScale=previousScale; clock?.ReleasePause(this); player?.ReleaseMovementLock(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }
    void OnDisable()=>Close();
    void OnDestroy() { Close(); if(overlay != null) Destroy(overlay); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsOpen=false; transitionFrame=-1; }

    void ClearContent() {
        for(int i=content.childCount-1;i>=0;i--) { var child=content.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
    }
    void ShowPage(int next)
    {
        page=next; ClearContent();
        window.GetComponent<MainMenuRoundedImage>().color=Glass;
        for(int i=0;i<tabs.Count;i++) tabs[i].GetComponentInChildren<TMP_Text>().fontStyle=i==page?FontStyles.Bold:FontStyles.Normal;
        if(page==0) Summary(); else if(page==1) Animals(); else if(page==2) Farm(); else if(page==3) Quests(); else Settings();
        if(page!=4) ButtonAt(content,"Lanjutkan",new(.35f,-.12f),new(.65f,-.025f),Close,true);
    }
    TMP_Text Text(Transform parent,string text,Vector2 min,Vector2 max,float size=22) {
        var value=GameplayHUDStyle.Text("Text",parent,text,size,min,max);
        value.fontSizeMax=size*(draft?.textSize==2?1.15f:draft?.textSize==0?.9f:1);
        return value;
    }
    RectTransform Card(string name,Transform parent,Vector2 min,Vector2 max) {
        var rect=GameplayHUDStyle.Rect(name,parent,min,max); GameplayHUDStyle.Surface(rect,Glass,16); return rect;
    }
    Button MakeButton(RectTransform rect,string label,UnityEngine.Events.UnityAction action,bool green=false) {
        float alpha=draft != null?draft.opacity:.70f;
        var image=GameplayHUDStyle.Surface(rect,Color.white,14); image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors;
        colors.normalColor=new Color(.39f,.48f,.50f,alpha);
        colors.highlightedColor=new Color(.34f,.63f,.37f,alpha);
        colors.pressedColor=new Color(.27f,.50f,.30f,alpha);
        colors.selectedColor=colors.normalColor;
        colors.disabledColor=new Color(.30f,.34f,.35f,alpha*.5f);
        button.colors=colors;GameplayHUDStyle.ButtonStates(button);
        rect.gameObject.AddComponent<MainMenuButtonAudio>();
        var caption=Text(rect,label,new(.05f,.05f),new(.95f,.95f)); caption.alignment=TextAlignmentOptions.Center;
        button.onClick.AddListener(action); return button;
    }
    Button ButtonAt(Transform parent,string label,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,bool green=false) => MakeButton(GameplayHUDStyle.Rect(label,parent,min,max),label,action,green);
    Image Artwork(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max) {
        var rect=GameplayHUDStyle.Rect(name,parent,min,max); var image=rect.gameObject.AddComponent<Image>(); image.sprite=sprite; image.preserveAspect=true; image.color=sprite!=null?Color.white:Color.clear; image.raycastTarget=false; return image;
    }
    void ArtSlot(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max) {
        Card(name+" Frame",parent,min,max); Artwork(parent,name+" Image Slot",sprite,min,max);
        if(sprite==null) Text(parent,"Slot Gambar",min,max,16).alignment=TextAlignmentOptions.Center;
    }
    RectTransform ListArea(Transform parent,string name,Vector2 min,Vector2 max) {
        var viewport=Card(name,parent,min,max); viewport.GetComponent<Image>().raycastTarget=true; viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.scrollSensitivity=28;
        var list=GameplayHUDStyle.Rect("Rows",viewport,new(0,1),Vector2.one); list.pivot=new(.5f,1);
        var layout=list.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=8; layout.padding=new RectOffset(8,8,8,8); layout.childControlWidth=true; layout.childForceExpandWidth=true; layout.childControlHeight=true; layout.childForceExpandHeight=false;
        var fitter=list.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport; scroll.content=list; return list;
    }
    Button ListButton(RectTransform list,string label,UnityEngine.Events.UnityAction action) {
        var row=GameplayHUDStyle.Rect("Row",list,Vector2.zero,Vector2.one); row.gameObject.AddComponent<LayoutElement>().preferredHeight=84;
        var button=MakeButton(row,label,action); button.GetComponentInChildren<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
        return button;
    }
    void Summary() {
        Text(content,"Ringkasan",new(.01f,.88f),new(.99f,1),36);
        var left=Card("Player Summary",content,new(.01f,.06f),new(.48f,.83f));
        var status=player != null?player.GetComponent<PlayerStatusSystem>():FindFirstObjectByType<PlayerStatusSystem>();
        var sb=new StringBuilder("<b>Nature Paradise</b>\n\n");
        if(clock!=null) sb.AppendLine($"Hari {clock.day}   •   {clock.hour:00}:{clock.minute:00}\n");
        if(status!=null) sb.AppendLine($"Health  {status.Health:0}/{status.MaxHealth:0}\nStamina  {status.Stamina:0}/{status.MaxStamina:0}\n");
        if(ScoreManager.Instance!=null) sb.AppendLine($"Uang  {ScoreManager.Instance.points:N0} G");
        Text(left,sb.ToString(),new(.06f,.12f),new(.94f,.92f),26);
        var right=Card("Village Summary",content,new(.50f,.06f),new(.99f,.83f));
        var village=VillageProgressionService.Instance;
        Text(right,village!=null?$"<b>Desa</b>\n\nLevel {village.VillageLevel}\nCondition Point {village.ConditionPoints}\n\nHewan {FindObjectsByType<AnimalGrowthSystem>(FindObjectsSortMode.None).Count(a=>a.HasBeenBorn)}": "<b>Desa</b>\n\nBelum ada data desa di scene ini.",new(.06f,.12f),new(.94f,.92f),26);
    }
    void Animals() {
        Text(content,"Hewan",new(.01f,.88f),new(.99f,1),36);
        var all=FindObjectsByType<AnimalGrowthSystem>(FindObjectsSortMode.None).Where(a=>a.HasBeenBorn).ToArray();
        Text(Card("Animal Summary",content,new(.01f,.74f),new(.99f,.86f)), $"Hewan  {all.Length}      •      Unggas  {all.Count(a=>a.Type==AnimalType.Chicken || a.Type==AnimalType.Duck)}      •      Hewan Besar  {all.Count(a=>a.Type!=AnimalType.Chicken && a.Type!=AnimalType.Duck)}",new(.03f,0),new(.97f,1),24);
        var filters=Card("Filters",content,new(.01f,.02f),new(.22f,.71f));
        string[] labels={"Semua","Unggas","Hewan Besar","Hewan Pribadi"};
        for(int i=0;i<4;i++) { int captured=i; ButtonAt(filters,labels[i],new(.05f,.80f-i*.22f),new(.95f,.97f-i*.22f),()=>{animalFilter=captured;ShowPage(1);},animalFilter==i); }
        var list=ListArea(content,"Animal List",new(.235f,.02f),new(.60f,.71f));
        var detail=Card("Animal Details",content,new(.615f,.02f),new(.99f,.71f));
        if(animalFilter==3) {
            var pets=PersonalAnimal.Active.Where(a=>a!=null && a.IsTamed).ToArray();
            if(!pets.Contains(selectedCompanion)) selectedCompanion=pets.FirstOrDefault();
            foreach(var pet in pets) ListButton(list,$"{pet.DisplayName}\n{pet.Species}  •  Kasih Sayang {pet.HeartPoints}/1000",()=>{selectedCompanion=pet;ShowPage(1);});
            if(selectedCompanion==null) Text(detail,"Belum ada hewan pribadi.",new(.06f,.1f),new(.94f,.9f));
            else {
                ArtSlot(detail,"Companion Illustration",theme?.defaultAnimalIllustration,new(.06f,.35f),new(.94f,.75f));
                Text(detail,$"<b>{selectedCompanion.DisplayName}</b>\n{selectedCompanion.Species}",new(.06f,.77f),new(.94f,.95f),26);
                Text(detail,$"Kasih Sayang {selectedCompanion.HeartPoints}/1000\nPerintah {selectedCompanion.Command}",new(.06f,.05f),new(.94f,.30f));
            }
            return;
        }
        var visible=all.Where(a=>animalFilter==0 || (animalFilter==1)==(a.Type==AnimalType.Chicken || a.Type==AnimalType.Duck)).ToArray();
        if(!visible.Contains(selectedAnimal)) selectedAnimal=visible.FirstOrDefault();
        foreach(var animal in visible) {
            var row=ListButton(list,$"{animal.AnimalName}\n{animal.Type}  •  Level {animal.HeartLevel}    Kasih {animal.HeartPoints}/1000",()=>{selectedAnimal=animal;ShowPage(1);});
            row.GetComponentInChildren<TMP_Text>().rectTransform.anchorMin=new(.24f,.05f);
            Artwork(row.transform,"Animal Portrait Slot",theme?.AnimalSprite(animal.Type,false),new(.02f,.1f),new(.21f,.9f));
        }
        if(selectedAnimal==null) { Text(detail,"Belum ada hewan untuk kategori ini.",new(.06f,.2f),new(.94f,.8f)); return; }
        var a=selectedAnimal;
        Text(detail,$"<b>{a.AnimalName}</b>\n{a.Type} {a.GenderLabel}",new(.06f,.78f),new(.94f,.97f),26);
        ArtSlot(detail,"Animal Illustration",theme?.AnimalSprite(a.Type,true),new(.06f,.35f),new(.48f,.75f));
        Text(detail,$"Level {a.HeartLevel}\nUsia {a.AgeDays} hari\n{(a.IsSheltered?"Dalam Kandang":"Di Luar")}\n{a.HealthSummary}",new(.52f,.34f),new(.95f,.75f),20);
        Text(detail,$"Kasih Sayang {a.HeartPoints}/1000\nKenyang {a.Fullness:0}/100\nBahagia {a.Happiness:0}/100",new(.06f,.05f),new(.94f,.30f),22);
    }
    void Farm() {
        Text(content,"Kebunku",new(.01f,.88f),new(.99f,1),36);
        var card=Card("Farm Overview",content,new(.01f,.05f),new(.99f,.83f));
        Text(card,$"<b>Kalender & Kebun</b>\n\nMusim: {SeasonVisualController.CurrentSeason}\nArea kebun: {FindObjectsByType<FieldArea>(FindObjectsSortMode.None).Length}\n\nRawat tanaman, siram, dan panen lewat interaksi di dunia.",new(.05f,.1f),new(.95f,.90f),28);
    }
    void Quests() {
        Text(content,"Quest Warga",new(.01f,.88f),new(.99f,1),36);
        var service=QuestService.Instance;
        var quests=service!=null?service.Definitions.ToArray():new QuestDefinitionSO[0];
        string[] labels={"Aktif","Selesai","Gagal"};
        System.Func<QuestDefinitionSO,bool> matches=q=>questFilter==0?(service.GetStatus(q)==QuestStatus.Active || service.GetStatus(q)==QuestStatus.ReadyToTurnIn):service.GetStatus(q)==(questFilter==1?QuestStatus.Completed:QuestStatus.Failed);
        for(int i=0;i<3;i++) { int captured=i; int count=quests.Count(q=>i==0?(service.GetStatus(q)==QuestStatus.Active || service.GetStatus(q)==QuestStatus.ReadyToTurnIn):service.GetStatus(q)==(i==1?QuestStatus.Completed:QuestStatus.Failed)); ButtonAt(content,$"{labels[i]}  {count}",new(.01f+i*.20f,.77f),new(.195f+i*.20f,.86f),()=>{questFilter=captured;ShowPage(3);},i==questFilter); }
        var visible=quests.Where(matches).ToArray();
        var list=ListArea(content,"Quest List",new(.01f,.02f),new(.68f,.73f));
        if(!visible.Contains(selectedQuest)) selectedQuest=visible.FirstOrDefault();
        foreach(var quest in visible) {
            string progress=quest.objectives==null?"":string.Join("   ",quest.objectives.Where(o=>o!=null).Select(o=>$"{service.GetProgress(quest,o.objectiveId)}/{o.requiredAmount}"));
            var row=ListButton(list,$"{quest.title}\n{progress}    •    Hadiah {quest.goldReward:N0} G",()=>{selectedQuest=quest;ShowPage(3);});
            row.GetComponentInChildren<TMP_Text>().rectTransform.anchorMin=new(.14f,.05f);
            Artwork(row.transform,"NPC Portrait Slot",theme?.QuestSprite(quest.Id),new(.01f,.06f),new(.12f,.94f));
        }
        var detail=Card("Quest Details",content,new(.695f,.02f),new(.99f,.86f));
        if(selectedQuest==null) { Text(detail,"Belum ada misi untuk kategori ini.",new(.06f,.2f),new(.94f,.8f)); return; }
        var q=selectedQuest;
        ArtSlot(detail,"NPC Portrait",theme?.QuestSprite(q.Id),new(.06f,.70f),new(.43f,.95f));
        Text(detail,q.title,new(.47f,.70f),new(.95f,.95f),24);
        var sb=new StringBuilder(q.description+"\n\n<b>Permintaan</b>\n");
        if(q.objectives!=null) foreach(var objective in q.objectives.Where(o=>o!=null)) sb.AppendLine($"{objective.description}  {service.GetProgress(q,objective.objectiveId)}/{objective.requiredAmount}");
        sb.AppendLine($"\n<b>Hadiah</b>\n{q.goldReward:N0} G");
        if(q.itemRewards!=null) foreach(var reward in q.itemRewards) if(reward?.item!=null) sb.AppendLine($"{reward.amount} {reward.item.itemName}");
        var description=Text(detail,sb.ToString(),new(.06f,.15f),new(.94f,.66f),20); description.alignment=TextAlignmentOptions.TopLeft; description.overflowMode=TextOverflowModes.Ellipsis;
        if(service.GetStatus(q)==QuestStatus.Active || service.GetStatus(q)==QuestStatus.ReadyToTurnIn)
            ButtonAt(detail,"Lacak Quest",new(.05f,.02f),new(.95f,.12f),()=>{FindFirstObjectByType<QuestTrackerUI>()?.Track(q);},true);
    }
    void Settings() {
        Text(content,"Pengaturan",new(.01f,.88f),new(.99f,1),36);
        var side=Card("Settings Categories",content,new(.01f,.02f),new(.19f,.85f));
        string[] names={"Umum","Audio","Grafis","Kontrol"};
        for(int i=0;i<4;i++) { int captured=i; ButtonAt(side,names[i],new(.04f,.80f-i*.23f),new(.96f,.97f-i*.23f),()=>{settingsPage=captured;ShowPage(4);},i==settingsPage); }
        var center=Card("Settings Options",content,new(.205f,.02f),new(.695f,.85f));
        var right=Card("Accessibility",content,new(.71f,.36f),new(.99f,.85f));
        Text(center,names[settingsPage],new(.035f,.90f),new(.965f,.99f),27);
        Text(right,"Aksesibilitas",new(.06f,.82f),new(.94f,.98f),26);
        Cycle(right,"Ukuran Teks",.64f,new[]{"Kecil","Sedang","Besar"},()=>draft.textSize,v=>draft.textSize=v);
        ToggleRow(right,"Kontras Tinggi",.40f,()=>draft.highContrast,v=>draft.highContrast=v);
        ToggleRow(right,"Kurangi Gerakan Kamera",.15f,()=>draft.reduceCameraMotion,v=>draft.reduceCameraMotion=v);
        if(settingsPage==0) {
            var language=ButtonAt(center,"Bahasa    Indonesia",new(.035f,.79f),new(.965f,.88f),()=>{}); language.interactable=false;
            SliderRow(center,"Sensitivitas Kamera",.64f,()=>draft.cameraSensitivity,v=>draft.cameraSensitivity=v);
            Cycle(center,"Ukuran UI",.50f,new[]{"Kecil","Sedang","Besar"},()=>draft.uiSize,v=>draft.uiSize=v);
            SliderRow(center,"Transparansi Tombol",.36f,()=>draft.opacity,v=>draft.opacity=Mathf.Max(.2f,v));
            ToggleRow(center,"Tampilkan Nama Item",.22f,()=>draft.showItemNames,v=>draft.showItemNames=v);
            ToggleRow(center,"Konfirmasi Sebelum Keluar",.08f,()=>draft.confirmExit,v=>draft.confirmExit=v);
        } else if(settingsPage==1) {
            SliderRow(center,"Volume Utama",.72f,()=>draft.master,v=>draft.master=v);
            SliderRow(center,"Musik / Alam",.50f,()=>draft.music,v=>draft.music=v);
            SliderRow(center,"Efek Suara",.28f,()=>draft.effects,v=>draft.effects=v);
        } else if(settingsPage==2) {
            var levels=QualitySettings.names.Length>0?QualitySettings.names:new[]{"Default"};
            Cycle(center,"Kualitas",.70f,levels,()=>draft.quality,v=>draft.quality=v);
            Text(center,"Kualitas mengikuti preset grafik game.",new(.04f,.25f),new(.95f,.50f),22);
        } else {
            Text(center,$"Menu (Editor): {editorDebugKey}\nMenu (Build): ESC\n\nGerak: WASD\nHotbar: 1–8\nAksi Alat: F / Klik Kiri\nTas: Tab\nMisi: J\n\nKeybind editor dapat diatur pada Pause Menu Theme.",new(.04f,.04f),new(.96f,.85f),22);
        }
        ButtonAt(content,"Kembalikan Default",new(.01f,-.12f),new(.315f,-.025f),()=>{draft=new GameplayUISettings{quality=QualitySettings.GetQualityLevel()};ShowPage(4);});
        ButtonAt(content,"Menu Utama",new(.34f,-.12f),new(.645f,-.025f),RequestMainMenu);
        ButtonAt(content,"Terapkan",new(.67f,-.12f),new(.99f,-.025f),ApplySettings,true);
    }
    void Cycle(Transform parent,string label,float y,string[] values,System.Func<int> get,System.Action<int> set) {
        Text(parent,label,new(.04f,y),new(.52f,y+.12f),19);
        Button button=null; button=ButtonAt(parent,values[Mathf.Clamp(get(),0,values.Length-1)],new(.54f,y),new(.96f,y+.12f),()=>{set((get()+1)%values.Length);button.GetComponentInChildren<TMP_Text>().text=values[get()];});
    }
    void ToggleRow(Transform parent,string label,float y,System.Func<bool> get,System.Action<bool> set) {
        Text(parent,label,new(.04f,y),new(.62f,y+.12f),19);
        Button button=null; button=ButtonAt(parent,get()?"Aktif":"Nonaktif",new(.66f,y),new(.96f,y+.12f),()=>{set(!get());button.GetComponentInChildren<TMP_Text>().text=get()?"Aktif":"Nonaktif";},get());
    }
    void SliderRow(Transform parent,string label,float y,System.Func<float> get,System.Action<float> set) {
        Text(parent,label,new(.04f,y),new(.48f,y+.12f),19);
        var value=Text(parent,$"{get()*100:0}%",new(.85f,y),new(.98f,y+.12f),18);
        var rect=GameplayHUDStyle.Rect(label+" Slider",parent,new(.50f,y+.02f),new(.83f,y+.10f));
        var slider=rect.gameObject.AddComponent<Slider>();
        var track=GameplayHUDStyle.Rect("Track",rect,new(0,.4f),new(1,.6f)); GameplayHUDStyle.Surface(track,GameplayHUDStyle.Slot,4).borderWidth=0;
        var fill=GameplayHUDStyle.Rect("Fill",track,Vector2.zero,Vector2.one); GameplayHUDStyle.Surface(fill,GameplayHUDStyle.Accent,4).borderWidth=0;
        var handleArea=GameplayHUDStyle.Rect("Handle Area",rect,new(.02f,.5f),new(.98f,.5f));
        var handle=GameplayHUDStyle.Rect("Handle",handleArea,new(0,.5f),new(0,.5f)); handle.sizeDelta=new(22,22); var image=GameplayHUDStyle.Surface(handle,Color.white,11); image.raycastTarget=true;
        slider.targetGraphic=image; slider.fillRect=fill; slider.handleRect=handle; slider.minValue=0; slider.maxValue=1; slider.value=get();
        slider.onValueChanged.AddListener(v=>{set(v);value.text=$"{get()*100:0}%";});
    }
    void ApplySettings() {
        draft.Apply();
        ShowPage(4);
    }
    void RequestMainMenu() {
        if(!draft.confirmExit) { ReturnToMainMenu(); return; }
        ClearContent(); Text(content,"Kembali ke Menu Utama?\nProgress yang belum disimpan tidak akan tersimpan.",new(.1f,.40f),new(.9f,.85f),28).alignment=TextAlignmentOptions.Center;
        ButtonAt(content,"Batal",new(.15f,.10f),new(.45f,.28f),()=>ShowPage(4));
        ButtonAt(content,"Menu Utama",new(.55f,.10f),new(.85f,.28f),ReturnToMainMenu);
    }
    void ReturnToMainMenu() {
        if(!Application.CanStreamedLevelBeLoaded("MainMenu")) { Text(content,"Menu utama belum tersedia.",new(.1f,.30f),new(.9f,.38f)); return; }
        Close(); SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
    }
}

