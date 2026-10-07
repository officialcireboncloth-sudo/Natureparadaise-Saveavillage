using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-9500)]
public sealed class FeedMakerUI : MonoBehaviour
{
    public static FeedMakerUI Instance { get; private set; }
    public FeedMaker Machine { get; private set; }
    Inventory inventory;
    FeedMakerTheme theme;
    readonly List<int> recipes=new();
    readonly GameObject[] markers=new GameObject[3];
    readonly Image[] materialIcons=new Image[3];
    TMP_Text status,title,description,ingredients,result,quantity,total,queueLabel,outputs,feedback,recipeLabel,upgradeLabel;
    Image bag,product;
    RectTransform queueContent;
    Button start,take,upgrade;
    int category,recipePosition,batches=1;
    float previousScale,nextRefresh;
    bool released;
    public int SelectedRecipeIndex=>recipes.Count==0?-1:recipes[recipePosition];
    public int ProductionQuantity=>batches;
    FeedRecipe Recipe=>SelectedRecipeIndex<0?null:Machine.Catalog.recipes[SelectedRecipeIndex];
    public static void Show(FeedMaker machine,Inventory source)
    {
        if(Instance!=null||machine==null||source==null)return;
        var ui=new GameObject("Feed Maker UI",typeof(RectTransform)).AddComponent<FeedMakerUI>();
        Instance=ui;ui.Machine=machine;ui.inventory=source;ui.previousScale=Time.timeScale;Time.timeScale=0;
        ui.Build();ui.SelectCategory(machine.HasDedicatedOutput?(machine.DedicatedFishFeed?2:1):0);
    }
    void Update()
    {
        if(released)return;
        if(Machine==null||inventory==null){Dispose();return;}
        if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){Dispose();return;}
        if(Input.GetKeyDown(KeyCode.A))CycleCategory(-1);
        if(Input.GetKeyDown(KeyCode.D))CycleCategory(1);
        if(Input.GetKeyDown(KeyCode.W))ChangeQuantity(1);
        if(Input.GetKeyDown(KeyCode.S))ChangeQuantity(-1);
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Produce();
        if(Input.GetKeyDown(KeyCode.F))Collect();
    }
    bool Allowed(int value)=>!Machine.HasDedicatedOutput||(value==2)==Machine.DedicatedFishFeed;
    void CycleCategory(int direction){for(int n=1;n<=3;n++){int value=(category+direction*n+6)%3;if(Allowed(value)){SelectCategory(value);return;}}}
    public void SelectCategory(int value)
    {
        value=Mathf.Clamp(value,0,2);if(!Allowed(value))return;category=value;recipePosition=0;batches=1;recipes.Clear();
        var list=Machine.Catalog?.recipes;if(list!=null)for(int i=0;i<list.Length;i++)if(list[i]!=null&&list[i].producesFishFeed==(category==2))recipes.Add(i);
        for(int i=0;i<3;i++)markers[i].SetActive(i==category);Refresh();GameplayInput.ConsumeCurrentFrame();
    }
    public void ChangeRecipe(int direction){if(recipes.Count==0)return;recipePosition=(recipePosition+direction+recipes.Count)%recipes.Count;batches=1;Refresh();}
    public void ChangeQuantity(int delta){batches=Mathf.Clamp(batches+delta,1,99);Refresh();}
    public int Produce()
    {
        int index=SelectedRecipeIndex,count=0;if(index>=0)for(int i=0;i<batches;i++){if(!Machine.Queue(inventory,index))break;count++;}
        feedback.text=count>0?$"{count} produksi ditambahkan ke antrean.":"Bahan tidak cukup atau kapasitas mesin penuh.";Refresh();GameplayInput.ConsumeCurrentFrame();return count;
    }
    public bool Collect()
    {
        bool success=category==2?Machine.CollectFishFeed(inventory):Machine.Collect(inventory);
        feedback.text=success?"Satu pakan masuk ke tas.":"Belum ada hasil atau tas penuh.";Refresh();GameplayInput.ConsumeCurrentFrame();return success;
    }
    public void Upgrade(){feedback.text=Machine.Upgrade(inventory)?"Mesin berhasil ditingkatkan.":"Bahan atau gold tidak cukup.";Refresh();GameplayInput.ConsumeCurrentFrame();}
    public void Dispose()
    {
        if(released)return;released=true;if(Instance==this)Instance=null;Time.timeScale=previousScale;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        Machine?.Close();GameplayInput.ConsumeCurrentFrame();gameObject.SetActive(false);Destroy(gameObject);
    }
    void OnDisable()=>Dispose();void OnDestroy()=>Dispose();
    void Build()
    {
        theme=Resources.Load<FeedMakerTheme>("UI/FeedMakerTheme");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=465;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_FeedMaker",typeof(EventSystem),typeof(StandaloneInputModule));
        Rect("Dim World",transform,0,0,1,1).gameObject.AddComponent<Image>().color=new(0,0,0,.18f);
        var safe=Rect("Safe Area",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel=Rect("Feed Maker Panel",safe,.482f,.095f,.985f,.985f);
        if(theme?.panel!=null){var image=panel.gameObject.AddComponent<Image>();image.sprite=theme.panel;image.type=Image.Type.Sliced;} else Surface(panel,GameplayHUDStyle.Modal);
        Art("Leaf Image Slot",panel,theme?.leafIcon,.025f,.94f,.08f,.982f);
        Text(panel,"MESIN PEMBUAT PAKAN",33,.09f,.935f,.64f,.99f).fontStyle=FontStyles.Bold;
        status=Text(panel,"",18,.66f,.935f,.975f,.99f);
        string[] names={"Pakan Unggas","Pakan Ternak","Pakan Ikan"};
        for(int i=0;i<3;i++)
        {
            int tab=i;float left=.025f+i*.322f;var button=Button(panel,"",left,.833f,left+.311f,.922f,()=>SelectCategory(tab));button.interactable=Allowed(i);
            Art("Bag Image Slot",button.transform,theme?.Bag(i),.025f,.09f,.27f,.91f);Text(button.transform,names[i],24,.29f,.05f,.97f,.95f);
            markers[i]=Rect("Selected Underline",button.transform,.08f,.025f,.92f,.045f).gameObject;var mark=markers[i].AddComponent<Image>();mark.color=new(.70f,.84f,.91f);mark.raycastTarget=false;
        }
        var card=Rect("Recipe",panel,.025f,.479f,.975f,.815f);Surface(card,GameplayHUDStyle.Card);
        bag=Art("Large Bag Illustration Slot",card,null,.025f,.10f,.32f,.94f);
        title=Text(card,"",31,.345f,.83f,.98f,.97f);description=Text(card,"",19,.345f,.70f,.98f,.83f);
        var material=Rect("Ingredients",card,.345f,.13f,.715f,.67f);Surface(material,GameplayHUDStyle.Card);
        Text(material,"BAHAN",20,.045f,.82f,.94f,.97f);ingredients=Text(material,"",20,.24f,.06f,.94f,.82f); for(int i=0;i<3;i++)materialIcons[i]=Art("Ingredient Image Slot "+i,material,null,.035f,.57f-i*.21f,.20f,.78f-i*.21f);
        Text(card,"→",40,.725f,.29f,.79f,.58f);
        var output=Rect("Recipe Output",card,.80f,.13f,.97f,.67f);Surface(output,GameplayHUDStyle.Card);
        product=Art("Product Image Slot",output,null,.10f,.39f,.90f,.94f);result=Text(output,"",19,.035f,.04f,.965f,.40f);result.alignment=TextAlignmentOptions.Midline;
        recipeLabel=Text(panel,"",19,.11f,.443f,.57f,.478f);
        Button(panel,"‹",.025f,.440f,.09f,.479f,()=>ChangeRecipe(-1));Button(panel,"›",.58f,.440f,.64f,.479f,()=>ChangeRecipe(1));
        Text(panel,"Jumlah Produksi",21,.045f,.393f,.30f,.437f);
        Button(panel,"−",.04f,.328f,.10f,.394f,()=>ChangeQuantity(-1));quantity=Text(panel,"",31,.11f,.328f,.20f,.394f);quantity.alignment=TextAlignmentOptions.Midline;
        Button(panel,"+",.21f,.328f,.27f,.394f,()=>ChangeQuantity(1));total=Text(panel,"",19,.30f,.324f,.65f,.426f);
        start=Button(panel,"Mulai Produksi",.66f,.334f,.96f,.414f,()=>Produce());
        Art("Production Icon Slot",start.transform,theme?.productionIcon,.02f,.15f,.17f,.85f);
        var queue=Rect("Production Queue",panel,.025f,.075f,.665f,.307f);Surface(queue,GameplayHUDStyle.Card);
        queueLabel=Text(queue,"",20,.025f,.83f,.975f,.99f);
        var scrollRect=Rect("Scroll",queue,.02f,.035f,.98f,.82f);scrollRect.gameObject.AddComponent<Image>().color=new(0,0,0,.01f);scrollRect.gameObject.AddComponent<RectMask2D>();
        var scroll=scrollRect.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=scrollRect;
        queueContent=Rect("Rows",scrollRect,0,0,1,1);queueContent.pivot=new(.5f,1);queueContent.anchorMin=new(0,1);queueContent.anchorMax=new(1,1);scroll.content=queueContent;
        var ready=Rect("Ready Output",panel,.685f,.075f,.975f,.307f);Surface(ready,GameplayHUDStyle.Card);
        Text(ready,"Hasil Produksi",20,.065f,.80f,.95f,.96f);Art("Collect Image Slot",ready,theme?.collectIcon,.35f,.51f,.65f,.77f);
        outputs=Text(ready,"",20,.065f,.30f,.935f,.56f);take=Button(ready,"Ambil Hasil x1",.055f,.055f,.945f,.29f,()=>Collect());
        feedback=Text(panel,"",17,.035f,.035f,.975f,.072f);
        upgrade=Button(panel,"",.025f,.002f,.975f,.035f,Upgrade);upgradeLabel=Text(upgrade.transform,"",16,.02f,0,.98f,1);
        Text(safe,"A / D  Pilih Pakan    W / S  Jumlah    Enter  Produksi    F  Ambil Hasil    Esc  Tutup",21,.50f,.025f,.935f,.08f);
        Button(safe,"Tutup",.94f,.03f,.985f,.075f,Dispose);Art("Logo Image Slot",safe,theme?.logo,.025f,.025f,.25f,.105f);
    }
    void Refresh()
    {
        if(Machine==null)return;
        int running=0;foreach(var job in Machine.Jobs)if(job.finish>=0)running++;
        status.text=$"{(running==0?"Siap Digunakan":"Sedang Memproses")} · Lv.{Machine.Level}\nBahan {Machine.Inputs}/{Machine.InputCapacity}";
        string label=category==2?"Pakan Ikan":category==1?"Pakan Ternak":"Pakan Unggas";title.text=label;
        description.text=category==2?"Pakan untuk ikan.":"Pakan hewan universal untuk unggas dan ternak.";
        SetArt(bag,theme?.Bag(category));SetArt(product,theme?.Product(category));
        var recipe=Recipe;int available=0;var lines=new List<string>();var seen=new HashSet<ItemSO>();
        foreach(var icon in materialIcons)SetArt(icon,null);
        if(recipe!=null)
        {
            var allowed=recipe.mixedInputs!=null&&recipe.mixedInputs.Length>0?recipe.mixedInputs:new[]{recipe.input};
            foreach(var item in allowed)if(item!=null&&seen.Add(item)){int count=inventory.GetCount(item);available+=count;if(lines.Count<3)SetArt(materialIcons[lines.Count],item.icon);lines.Add($"{item.itemName}: {count}");}
            int required=recipe.inputCount*batches;ingredients.text=string.Join("\n",lines)+$"\nTotal perlu: {required}";
            result.text=$"{label}\nx{recipe.outputCount}";recipeLabel.text=$"Resep {recipePosition+1}/{recipes.Count} · {recipe.input?.itemName}";
            double hours=recipe.hours/new[]{1d,1.25d,1.6d,2d}[Machine.Level-1];
            total.text=$"Total hasil: x{recipe.outputCount*batches}\nProses per batch: {Duration(hours)}";
            start.interactable=available>=recipe.inputCount&&Machine.Inputs+recipe.inputCount<=Machine.InputCapacity;
        }
        else {ingredients.text="Resep belum tersedia.";result.text="";recipeLabel.text="Tidak ada resep";total.text="";start.interactable=false;}
        quantity.text=batches.ToString();queueLabel.text=$"ANTREAN PRODUKSI · {Machine.Jobs.Count} batch";
        for(int i=queueContent.childCount-1;i>=0;i--){var child=queueContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
        int rows=Math.Max(3,Machine.Jobs.Count);queueContent.sizeDelta=new(0,rows*52);
        for(int i=0;i<rows;i++)
        {
            var row=Rect("Queue Slot "+(i+1),queueContent,0,1,1,1);row.offsetMin=new(0,-(i+1)*52+4);row.offsetMax=new(0,-i*52-4);Surface(row,GameplayHUDStyle.Card);
            if(i>=Machine.Jobs.Count){Text(row,$"{i+1}     Slot Kosong",18,.025f,.1f,.96f,.9f);continue;}
            var job=Machine.Jobs[i];Art("Queue Bag Slot",row,theme?.Bag(job.producesFishFeed?2:1),.08f,.12f,.18f,.88f);
            Text(row,$"{i+1}",20,.025f,.1f,.07f,.9f);Text(row,$"{(job.producesFishFeed?"Pakan Ikan":"Pakan Hewan")} x{job.output}",19,.20f,.46f,.97f,.96f);
            float progress=job.finish<0?0:Mathf.Clamp01((float)(1-(job.finish-Machine.CurrentGameHour)/job.hours));
            var track=Rect("Progress Track",row,.20f,.20f,.54f,.35f);Surface(track,new(.35f,.43f,.47f));var fill=Rect("Progress",track,0,0,progress,1);Surface(fill,new(.51f,.78f,.55f));
            Text(row,job.finish<0?"Menunggu":$"{progress:P0} · Sisa {Duration(Math.Max(0,job.finish-Machine.CurrentGameHour))}",16,.57f,.04f,.98f,.47f);
        }
        outputs.text=$"Hewan: {Machine.AnimalFeedOutput}   Ikan: {Machine.FishFeedOutput}\nTotal {Machine.Output}/{Machine.OutputCapacity}";
        take.interactable=category==2?Machine.FishFeedOutput>0:Machine.AnimalFeedOutput>0;
        upgrade.interactable=Machine.Level<4;upgradeLabel.text=Machine.Level>=4?"Level mesin maksimum":$"Upgrade Lv.{Machine.Level+1}: {Machine.Level*5} {Machine.Catalog?.upgradeWood?.itemName} + {Machine.Level*3} {Machine.Catalog?.upgradeStone?.itemName} + {Machine.Level*100} G";
    }
    static string Duration(double hours){int minutes=(int)Math.Ceiling(hours*60);return minutes>=60?$"{minutes/60} Jam {minutes%60} Menit":$"{minutes} Menit";}
    static void SetArt(Image image,Sprite sprite){image.sprite=sprite;image.enabled=sprite!=null;}
    static RectTransform Rect(string name,Transform parent,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Rect(name,parent,new(x1,y1),new(x2,y2));
    static void Surface(RectTransform rect,Color color)=>GameplayHUDStyle.Surface(rect,color,14).raycastTarget=true;
    static TMP_Text Text(Transform parent,string value,float size,float x1,float y1,float x2,float y2)=>GameplayHUDStyle.Text("Text",parent,value,size,new(x1,y1),new(x2,y2));
    static Image Art(string name,Transform parent,Sprite sprite,float x1,float y1,float x2,float y2){var image=Rect(name,parent,x1,y1,x2,y2).gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;SetArt(image,sprite);return image;}
    static Button Button(Transform parent,string label,float x1,float y1,float x2,float y2,UnityEngine.Events.UnityAction action)
    {
        var rect=Rect(label,parent,x1,y1,x2,y2);var image=GameplayHUDStyle.Surface(rect,Color.white,12);image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;
        colors.normalColor=new(.23f,.31f,.36f,.9f);colors.selectedColor=colors.normalColor;colors.highlightedColor=new(.29f,.53f,.36f,.95f);colors.pressedColor=new(.21f,.39f,.27f);colors.disabledColor=new(.20f,.26f,.30f,.60f);colors.fadeDuration=0;button.colors=colors;GameplayHUDStyle.ButtonStates(button);button.navigation=new Navigation{mode=Navigation.Mode.None};
        rect.gameObject.AddComponent<MainMenuButtonAudio>();button.onClick.AddListener(action);
        if(!string.IsNullOrEmpty(label)){var text=Text(rect,label,23,.025f,.05f,.975f,.95f);text.alignment=TextAlignmentOptions.Midline;}return button;
    }
}



