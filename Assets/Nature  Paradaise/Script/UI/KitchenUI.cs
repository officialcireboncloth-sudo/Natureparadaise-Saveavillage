using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-9500)]
public sealed class KitchenUI : MonoBehaviour
{
    public static KitchenUI Instance {get;private set;}
    public KitchenSet Stove {get;private set;}
    public KitchenRecipeSO SelectedRecipe => selected>=0&&selected<KitchenService.Recipes.Count?KitchenService.Recipes[selected]:null;
    public int Batches => batches;
    Inventory inventory;PlayerController player;TimeManager clock;KitchenTheme theme;
    int selected,batches=1;bool released,dirty=true;float previousScale,nextRefresh;
    bool previousCursor;CursorLockMode previousLock;
    Canvas mapCanvas;bool previousMapEnabled;
    ScrollRect recipeScroll;RectTransform recipeContent,ingredientContent,ingredientViewport;
    readonly List<Button> recipeButtons=new();
    readonly List<Image> recipeImages=new();
    sealed class IngredientRow { public ItemSO item; public TMP_Text count; public Image status; }
    readonly List<IngredientRow> ingredientRows=new();
    TMP_Text title,description,requirements,amount,result,feedback;
    Image resultImage;Button cookButton;string message="";
    static readonly Color Gray=new(.23f,.30f,.33f,.94f);
    public static void Show(KitchenSet stove)
    {
        if(Instance!=null||stove==null||stove.PlayerInventory==null||!stove.isActiveAndEnabled||!KitchenService.IsUnlocked||WorldInteractionPrompt.IsSuppressed)return;
        var ui=new GameObject("Kitchen UI",typeof(RectTransform)).AddComponent<KitchenUI>();Instance=ui;ui.Stove=stove;ui.inventory=stove.PlayerInventory;
        ui.player=ui.inventory.GetComponent<PlayerController>();ui.clock=TimeManager.Instance;ui.previousScale=Time.timeScale;
        ui.previousCursor=Cursor.visible;ui.previousLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        ui.player?.AcquireMovementLock(ui);ui.clock?.AcquirePause(ui);WorldInteractionPrompt.AcquireSuppression(ui);Time.timeScale=0;
        ui.inventory.OnInventoryChanged+=ui.MarkDirty;RefrigeratorService.Changed+=ui.MarkDirty;KitchenService.Changed+=ui.MarkDirty;
        GameplayInput.ConsumeCurrentFrame();ui.Build();ui.Refresh();
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(true);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(true);
        ui.mapCanvas=FindFirstObjectByType<WorldMapUI>()?.GetComponent<Canvas>();if(ui.mapCanvas!=null){ui.previousMapEnabled=ui.mapCanvas.enabled;ui.mapCanvas.enabled=false;}
    }
    void MarkDirty()=>dirty=true;
    void Update()
    {
        if(released)return;
        if(Stove==null||!Stove.isActiveAndEnabled||inventory==null||!KitchenService.IsUnlocked){Close();return;}
        if(dirty||Time.unscaledTime>=nextRefresh){dirty=false;nextRefresh=Time.unscaledTime+.25f;Refresh();}
#if UNITY_EDITOR
        var focus=UnityEditor.EditorWindow.focusedWindow;if(focus==null||focus.GetType().Name!="GameView")return;
#else
        if(!Application.isFocused)return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
        if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))SelectRecipe(selected-1);
        if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))SelectRecipe(selected+1);
        if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))ChangeQuantity(-1);
        if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))ChangeQuantity(1);
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Cook();
    }
    public void SelectRecipe(int index)
    {
        selected=Mathf.Clamp(index,0,Mathf.Max(0,KitchenService.Recipes.Count-1));batches=1;message="";Refresh();
        float visible=recipeScroll.viewport.rect.height;
        float overflow=Mathf.Max(0,recipeContent.rect.height-visible);
        recipeScroll.verticalNormalizedPosition=overflow<=0?1:1-Mathf.Clamp01((selected*112f-visible*.4f)/overflow);
        GameplayInput.ConsumeCurrentFrame();
    }
    public void ChangeQuantity(int delta){batches=Mathf.Clamp(batches+delta,1,99);message="";Refresh();GameplayInput.ConsumeCurrentFrame();}
    public bool Cook()
    {
        bool success=KitchenService.TryCook(inventory,SelectedRecipe,batches,out message);Refresh();GameplayInput.ConsumeCurrentFrame();return success;
    }
    public void Close()
    {
        if(released)return;released=true;
        if(inventory!=null)inventory.OnInventoryChanged-=MarkDirty;RefrigeratorService.Changed-=MarkDirty;KitchenService.Changed-=MarkDirty;
        player?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);Time.timeScale=previousScale;
        Cursor.visible=previousCursor;Cursor.lockState=previousLock;if(Instance==this)Instance=null;
        FindFirstObjectByType<HUDManager>()?.SetModalContextVisible(false);
        FindFirstObjectByType<GameplayPauseMenu>()?.SetModalContextVisible(false);
        if(mapCanvas!=null)mapCanvas.enabled=previousMapEnabled;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        GameplayInput.ConsumeCurrentFrame();gameObject.SetActive(false);Destroy(gameObject);
    }
    void OnDisable()=>Close();void OnDestroy()=>Close();
    void Build()
    {
        theme=Resources.Load<KitchenTheme>("UI/KitchenTheme");
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=470;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();if(EventSystem.current==null)new GameObject("EventSystem_Kitchen",typeof(EventSystem),typeof(StandaloneInputModule));
        R("Dim",transform,0,0,1,1).gameObject.AddComponent<Image>().color=new(0,0,0,.22f);
        var safe=R("Safe Area",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
        var panel=R("Cooking Panel",safe,.45f,.02f,.985f,.99f);
        if(theme?.panel!=null){var image=panel.gameObject.AddComponent<Image>();image.sprite=theme.panel;image.type=Image.Type.Sliced;image.raycastTarget=true;}
        else Surface(panel,new(.12f,.20f,.24f,.94f),16);
        Art("Leaf Image Slot",panel,theme?.leafIcon,.025f,.915f,.095f,.978f);T(panel,"MEMASAK",42,.11f,.92f,.92f,.987f);
        T(panel,"RESEP",24,.04f,.845f,.33f,.89f);
        recipeScroll=Scroll(panel,"Recipes",.03f,.12f,.355f,.842f,out var recipeViewport,out recipeContent);
        BuildRecipes();
        R("Divider",panel,.368f,.12f,.369f,.875f).gameObject.AddComponent<Image>().color=new(.6f,.73f,.77f,.4f);
        resultImage=Art("Result Item Illustration Slot",panel,null,.397f,.635f,.70f,.885f);
        title=T(panel,"",31,.695f,.77f,.97f,.85f);description=T(panel,"",20,.695f,.66f,.97f,.768f);
        T(panel,"BAHAN",24,.405f,.585f,.96f,.627f);
        Scroll(panel,"Ingredients",.402f,.292f,.96f,.579f,out ingredientViewport,out ingredientContent);
        T(panel,"Bahan tersedia dari Tas + Kulkas",19,.405f,.261f,.96f,.29f);
        requirements=T(panel,"",18,.405f,.208f,.96f,.259f);
        T(panel,"Jumlah",23,.405f,.166f,.65f,.21f);
        B(panel,"−",.405f,.107f,.46f,.159f,()=>ChangeQuantity(-1));amount=T(panel,"1",26,.47f,.107f,.575f,.159f);amount.alignment=TextAlignmentOptions.Center;
        B(panel,"+",.585f,.107f,.64f,.159f,()=>ChangeQuantity(1));cookButton=B(panel,"Masak",.69f,.105f,.96f,.18f,()=>Cook());
        Art("Cook Image Slot",cookButton.transform,theme?.cookIcon,.04f,.15f,.22f,.85f);
        result=T(panel,"",18,.405f,.073f,.96f,.102f);feedback=T(panel,"",17,.04f,.039f,.97f,.072f);
        T(panel,"W / S  Pilih Resep     A / D  Jumlah     Enter  Masak     Esc  Tutup",18,.04f,.004f,.87f,.035f);
        B(panel,"Tutup",.88f,.004f,.97f,.037f,Close);Canvas.ForceUpdateCanvases();
    }
    void BuildRecipes()
    {
        foreach(Transform child in recipeContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}recipeButtons.Clear();recipeImages.Clear();
        recipeContent.sizeDelta=new(0,KitchenService.Recipes.Count*112f);
        for(int i=0;i<KitchenService.Recipes.Count;i++)
        {
            int index=i;var row=R("Recipe "+i,recipeContent,0,1,1,1);row.pivot=new(0,1);row.sizeDelta=new(0,104);row.anchoredPosition=new(0,-i*112);
            var button=B(row,"",0,0,1,1,()=>SelectRecipe(index));recipeButtons.Add(button);
            recipeImages.Add(Art("Shared Result Icon",row,null,.02f,.08f,.38f,.92f));
            var label=button.GetComponentInChildren<TMP_Text>();label.rectTransform.anchorMin=new(.42f,.08f);label.rectTransform.anchorMax=new(.98f,.92f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;label.alignment=TextAlignmentOptions.MidlineLeft;
        }
    }
    void Refresh()
    {
        if(title==null)return;if(recipeButtons.Count!=KitchenService.Recipes.Count)BuildRecipes();
        selected=Mathf.Clamp(selected,0,Mathf.Max(0,KitchenService.Recipes.Count-1));var recipe=SelectedRecipe;
        bool learned=recipe!=null&&KitchenService.IsLearned(recipe);
        for(int i=0;i<recipeButtons.Count;i++)
        {
            var data=KitchenService.Recipes[i];bool known=KitchenService.IsLearned(data);var button=recipeButtons[i];
            var surface=(MainMenuRoundedImage)button.targetGraphic;surface.borderColor=i==selected?new Color(.55f,.95f,1):new Color(.7f,.78f,.8f,.28f);surface.borderWidth=i==selected?2:1;
            Paint(recipeImages[i],known?data.ResultIcon:theme?.lockedRecipeIcon);
            button.GetComponentInChildren<TMP_Text>().text=known?data.DisplayName:"???";
        }
        title.text=learned?recipe.DisplayName:recipe==null?"Belum ada resep":"???";
        description.text=learned?recipe.description:"Pelajari resep untuk melihat bahan dan hasilnya.";
        Paint(resultImage,learned?recipe.ResultIllustration:theme?.lockedRecipeIcon);amount.text=batches.ToString();
        var ingredients=learned&&recipe.ingredients!=null?recipe.ingredients.Where(v=>v?.item!=null&&v.amount>0).GroupBy(v=>v.item).ToArray():null;
        int count=ingredients?.Length??0;
        bool rebuild=ingredientRows.Count!=count;
        if(!rebuild)for(int i=0;i<count;i++)if(ingredientRows[i].item!=ingredients[i].Key){rebuild=true;break;}
        if(rebuild)
        {
            foreach(Transform child in ingredientContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}ingredientRows.Clear();
            ingredientContent.sizeDelta=new(0,count*72f);
            for(int i=0;i<count;i++)
            {
                var item=ingredients[i].Key;
                var row=R("Ingredient "+item.Id,ingredientContent,0,1,1,1);row.pivot=new(0,1);row.sizeDelta=new(0,66);row.anchoredPosition=new(0,-i*72);Surface(row,new(.27f,.34f,.37f,.75f),10);
                Art("Shared Ingredient Icon",row,item.icon,.02f,.08f,.14f,.92f);T(row,item.itemName,22,.18f,.06f,.65f,.94f);
                ingredientRows.Add(new IngredientRow{item=item,count=T(row,"",23,.67f,.06f,.88f,.94f),status=Art("Requirement Status Image Slot",row,null,.9f,.20f,.98f,.8f)});
            }
        }
        for(int i=0;i<count;i++)
        {
            var group=ingredients[i];var view=ingredientRows[i];int have=KitchenIngredientService.GetAvailableCount(inventory,group.Key);long need=(long)group.Sum(v=>v.amount)*batches;bool enough=have>=need;
            view.count.text=$"{have} / {need}";view.count.color=enough?new(.63f,.86f,.64f):new(.96f,.57f,.53f);
            Paint(view.status,enough?theme?.sufficientIcon:theme?.missingIcon);
            Paint(view.count.transform.parent.Find("Shared Ingredient Icon").GetComponent<Image>(),group.Key.icon);
        }
        bool allowed=KitchenService.CanCook(inventory,recipe,batches,out string reason);cookButton.interactable=allowed;
        requirements.text=learned?$"Kitchen Lv.{recipe.requiredKitchenLevel} • {KitchenService.FormatEquipment(recipe.requiredEquipment)}\nWaktu: {recipe.baseCookingMinutes+(batches-1)*5} menit": "";
        result.text=learned?$"Hasil: {recipe.resultItem?.itemName??"Belum diisi"} x{(long)recipe.resultAmount*batches} • Grade {KitchenService.CalculateOutputQuality(inventory,recipe,batches)}/5":"";
        feedback.text=!string.IsNullOrEmpty(message)?message:allowed?"":reason;
    }
    static RectTransform R(string n,Transform p,float x,float y,float X,float Y)=>GameplayHUDStyle.Rect(n,p,new(x,y),new(X,Y));
    static void Surface(RectTransform r,Color c,float radius){var image=GameplayHUDStyle.Surface(r,c,radius);image.borderWidth=1;image.borderColor=new(.65f,.77f,.82f,.5f);image.raycastTarget=true;}
    static TMP_Text T(Transform p,string s,int size,float x,float y,float X,float Y)=>GameplayHUDStyle.Text("Text",p,s,size,new(x,y),new(X,Y));
    static Image Art(string n,Transform p,Sprite s,float x,float y,float X,float Y){var i=R(n,p,x,y,X,Y).gameObject.AddComponent<Image>();i.preserveAspect=true;i.raycastTarget=false;Paint(i,s);return i;}
    static void Paint(Image i,Sprite s){if(i==null)return;i.sprite=s;i.enabled=s!=null;}
    static Button B(Transform p,string s,float x,float y,float X,float Y,UnityEngine.Events.UnityAction action)
    {
        var r=R(s+" Button",p,x,y,X,Y);Surface(r,Color.white,10);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.navigation=new Navigation{mode=Navigation.Mode.None};
        var c=b.colors;c.normalColor=Gray;c.selectedColor=Gray;c.highlightedColor=new(.37f,.58f,.40f);c.pressedColor=new(.25f,.44f,.30f);c.disabledColor=new(.16f,.22f,.25f,.6f);b.colors=c;b.onClick.AddListener(action);
        var label=T(r,s,24,.04f,.08f,.96f,.92f);label.alignment=TextAlignmentOptions.Center;return b;
    }
    static ScrollRect Scroll(Transform parent,string name,float x,float y,float X,float Y,out RectTransform viewport,out RectTransform content)
    {
        var area=R(name,parent,x,y,X,Y);var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=32;
        viewport=R("Viewport",area,0,0,1,1);viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color=Color.clear;
        content=R("Content",viewport,0,1,1,1);content.pivot=new(0,1);scroll.viewport=viewport;scroll.content=content;return scroll;
    }
}
