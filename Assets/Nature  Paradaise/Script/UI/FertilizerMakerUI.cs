using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Reference layout in a 1672 x 941 design space; actual scene remains behind the glass panels.</summary>
[DefaultExecutionOrder(-9400)]
public sealed class FertilizerMakerUI : MonoBehaviour
{
    public static FertilizerMakerUI Instance { get; private set; }
    public FertilizerProcessor Machine { get; private set; }
    public int SelectedIndex { get; private set; }
    public int Quantity { get; private set; } = 1;
    public bool CanProduce => craft != null && craft.interactable;
    readonly List<Canvas> hidden = new();
    readonly Dictionary<Transform,Vector2> dimensions = new();
    readonly List<Button> choices = new();
    readonly List<RectTransform> rows = new();
    readonly List<TMP_Text> counts = new(), ingredientLabels = new();
    readonly List<Graphic> ingredientIcons = new();
    Inventory bag; PlayerController player; TimeManager clock; FertilizerMakerTheme theme;
    TMP_Text title, quantityLabel, outputLabel, feedback, enterHint;
    RectTransform detailIcon, outputIcon;
    Button craft, minus, plus, legacy;
    GameObject ownedEventSystem;
    bool closing, dirty; float priorScale; bool priorCursor; CursorLockMode priorLock;
    FertilizerRecipe[] Recipes => Machine != null && Machine.Catalog != null ? Machine.Catalog.makerRecipes : null;
    FertilizerRecipe Selected => Recipes != null && SelectedIndex < Recipes.Length ? Recipes[SelectedIndex] : null;
    static readonly Color Text = new(.94f,.95f,.91f), Muted = new(.69f,.72f,.66f), Good = new(.73f,.89f,.61f), Bad = new(.96f,.37f,.32f);
    static readonly Color[] Bands = { new(.22f,.43f,.62f),new(.29f,.49f,.32f),new(.69f,.19f,.20f),new(.78f,.56f,.12f) };
    const float DividerThickness = 1.5f;

    public static bool Show(FertilizerProcessor machine, Inventory inventory)
    {
        if (Instance != null || machine == null || inventory == null || inventory.GetComponent<PlayerController>() == null) return false;
        var ui = new GameObject("Fertilizer Maker UI",typeof(RectTransform)).AddComponent<FertilizerMakerUI>();
        Instance = ui; ui.Machine=machine;ui.bag=inventory;ui.player=inventory.GetComponent<PlayerController>();ui.clock=TimeManager.Instance;
        ui.priorScale=Time.timeScale;ui.priorCursor=Cursor.visible;ui.priorLock=Cursor.lockState;
        ui.player.AcquireMovementLock(ui);ui.clock?.AcquirePause(ui);WorldInteractionPrompt.AcquireSuppression(ui);
        Time.timeScale=0;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        try
        {
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(c.enabled && c.renderMode!=RenderMode.WorldSpace){ui.hidden.Add(c);c.enabled=false;}
            ui.theme=Resources.Load<FertilizerMakerTheme>("UI/Fertilizer Maker Theme");
            ui.Build();inventory.OnInventoryChanged+=ui.MarkDirty;ui.Refresh();GameplayInput.ConsumeCurrentFrame();return true;
        }
        catch {ui.Close();throw;}
    }
    void MarkDirty()=>dirty=true;
    void Update()
    {
        if(closing)return;
        if(Machine==null||!Machine.isActiveAndEnabled||bag==null||player==null||LoadingScreen.IsVisible){Close();return;}
        if(dirty)Refresh();
#if UNITY_EDITOR
        if(UnityEditor.EditorWindow.focusedWindow==null||UnityEditor.EditorWindow.focusedWindow.GetType().Name!="GameView")return;
#endif
        if(GameplayInput.ConsumedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
        if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))SelectRecipe((SelectedIndex+3)%4);
        else if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))SelectRecipe((SelectedIndex+1)%4);
        else if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.Minus))SetQuantity(Quantity-1);
        else if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.Equals))SetQuantity(Quantity+1);
        else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Produce();
    }
    public void SelectRecipe(int index)
    {
        if(Recipes==null||index<0||index>=Recipes.Length)return;
        SelectedIndex=index;Quantity=1;feedback.text="";Refresh();Consume();
    }
    public void SetQuantity(int amount){Quantity=Mathf.Clamp(amount,1,FertilizerMakerService.MaximumBatch);feedback.text="";Refresh();Consume();}
    public bool Produce()
    {
        if(closing)return false;
        bool success=FertilizerMakerService.TryMake(bag,Selected,Quantity,out string message);
        Refresh();feedback.text=message;feedback.color=success?Good:Muted;Consume();return success;
    }
    static void Consume(){EventSystem.current?.SetSelectedGameObject(null);GameplayInput.ConsumeCurrentFrame();}
    void Refresh()
    {
        dirty=false;var recipe=Selected;bool valid=recipe?.output!=null;
        title.text=valid?recipe.output.itemName:"Resep belum tersedia";
        quantityLabel.text=Quantity.ToString();outputLabel.text=valid?$"{Quantity*recipe.outputCount}× {recipe.output.itemName}":"";
        for(int i=0;i<choices.Count;i++)
        {
            var surface=choices[i].GetComponent<MainMenuRoundedImage>();bool chosen=i==SelectedIndex;
            surface.color=chosen?(theme!=null?theme.selected:new(.22f,.29f,.20f,.82f)):new(.21f,.25f,.22f,.18f);
            surface.borderColor=chosen?(theme!=null?theme.selectedBorder:new(.79f,.91f,.70f,1)):new(.65f,.70f,.65f,.28f);
            surface.SetVerticesDirty();
        }
        ClearIcons(detailIcon);ClearIcons(outputIcon);
        if(valid){ItemIcon(detailIcon,recipe.output,SelectedIndex);ItemIcon(outputIcon,recipe.output,SelectedIndex);}
        int n=valid&&recipe.ingredients!=null?recipe.ingredients.Length:0;
        float rowHeight=192f/Mathf.Max(3,n);
        for(int i=0;i<rows.Count;i++)
        {
            bool shown=i<n;rows[i].gameObject.SetActive(shown);if(!shown)continue;
            float top=Mathf.Round(147+i*rowHeight),bottom=Mathf.Round(147+(i+1)*rowHeight);
            Place(rows[i],34,top,570,bottom-top,638,692);
            var ingredient=recipe.ingredients[i];int have=FertilizerMakerService.Available(bag,ingredient);long need=(long)ingredient.amount*Quantity;
            counts[i].text=$"{have} / {need}";counts[i].color=have>=need?Good:Bad;
            ingredientLabels[i].text=ingredient.DisplayName;ingredientLabels[i].color=have>=need?Text:Muted;
            ClearIcons(ingredientIcons[i].transform);
            var slot=(RectTransform)ingredientIcons[i].transform;dimensions[slot]=new(92,rowHeight-8);
            IngredientIcon(slot,ingredient,have>=need?1:.36f);
        }
        string reason="Data resep belum lengkap";
        craft.interactable=valid&&FertilizerMakerService.CanMake(bag,recipe,Quantity,out reason);
        craft.GetComponent<MainMenuRoundedImage>().color=craft.interactable?new(.33f,.45f,.27f,.95f):new(.45f,.50f,.48f,.50f);
        craft.GetComponentInChildren<TMP_Text>().color=craft.interactable?Text:new(.72f,.74f,.72f,.65f);
        enterHint.color=craft.interactable?Text:new(.65f,.69f,.64f,.65f);
        feedback.text=craft.interactable?"Bahan lengkap":reason;feedback.color=Muted;
        minus.interactable=Quantity>1;plus.interactable=Quantity<FertilizerMakerService.MaximumBatch;
        legacy.gameObject.SetActive(Machine.Jobs.Count>0);
    }
    static void ClearIcons(Transform slot)
    {
        for(int i=slot.childCount-1;i>=0;i--){var child=slot.GetChild(i).gameObject;child.SetActive(false);Release(child);}
    }
    void ItemIcon(RectTransform slot,ItemSO item,int index)
    {
        Sprite sprite=theme!=null&&theme.fertilizerIcons!=null&&index<theme.fertilizerIcons.Length?theme.fertilizerIcons[index]:null;
        if(sprite==null)sprite=item.icon;
        DrawIcon(slot,sprite,FertilizerMakerIcon.Kind.Sack,Bands[Mathf.Clamp(index,0,3)],1);
    }
    void IngredientIcon(RectTransform slot,FertilizerIngredient ingredient,float alpha)
    {
        var item=ingredient.item;var kind=item!=null&&item.category==ItemCategory.Fish?FertilizerMakerIcon.Kind.Fish:
            ingredient.alternatives!=null&&ingredient.alternatives.Length>0?FertilizerMakerIcon.Kind.Manure:
            item!=null&&item.Id.Contains("shell")?FertilizerMakerIcon.Kind.Shell:FertilizerMakerIcon.Kind.Mineral;
        Sprite sprite=item!=null?item.icon:null;
        if(sprite==null&&theme!=null)sprite=kind==FertilizerMakerIcon.Kind.Fish?theme.fishIcon:
            kind==FertilizerMakerIcon.Kind.Manure?theme.manureIcon:kind==FertilizerMakerIcon.Kind.Shell?theme.shellIcon:theme.mineralIcon;
        DrawIcon(slot,sprite,kind,Color.white,alpha);
    }
    void DrawIcon(RectTransform slot,Sprite sprite,FertilizerMakerIcon.Kind kind,Color band,float alpha)
    {
        var bounds=dimensions[slot];float size=Mathf.Min(bounds.x,bounds.y);
        var rect=sprite!=null?R("Icon",slot,0,0,bounds.x,bounds.y):
            R("Icon",slot,(bounds.x-size)*.5f,(bounds.y-size)*.5f,size,size);
        if(sprite!=null){var im=rect.gameObject.AddComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;im.color=new(1,1,1,alpha);}
        else{var icon=rect.gameObject.AddComponent<FertilizerMakerIcon>();icon.kind=kind;icon.band=band;icon.color=new(1,1,1,alpha);icon.raycastTarget=false;icon.SetAllDirty();}
    }
    void Build()
    {
        dimensions[transform]=new(1672,941);
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=725;canvas.pixelPerfect=true;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1672,941);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        if(EventSystem.current==null)ownedEventSystem=new GameObject("Fertilizer Event System",typeof(EventSystem),typeof(StandaloneInputModule));
        var backdrop=R("World Input Blocker",transform,0,0,1672,941).gameObject.AddComponent<Image>();backdrop.color=new(0,0,0,.035f);
        if(theme!=null&&theme.background!=null){backdrop.sprite=theme.background;backdrop.color=Color.white;}
        var safe=R("Safe Area",transform,0,0,1672,941);safe.gameObject.AddComponent<SafeAreaFitter>();
        var header=P("Header",safe,36,21,1608,53);
        var gear=R("Gear Icon",header,23,11,31,31).gameObject.AddComponent<MainMenuIcon>();gear.kind=MainMenuIcon.Kind.Settings;gear.color=Text;gear.raycastTarget=false;
        T("Title",header,"PEMBUAT PUPUK",26,75,4,850,45).characterSpacing=1.2f;
        var farm=T("Farm",header,"Ladang Utama",21,1325,4,258,45);farm.alignment=TextAlignmentOptions.Right;
        var left=P("Pilih Pupuk",safe,34,110,453,590);
        T("Heading",left,"Pilih Pupuk",31,25,14,400,48);
        for(int i=0;i<4;i++)
        {
            int index=i;float y=81+i*121;
            var button=B("Recipe "+i,left,"",23,y,410,104,()=>SelectRecipe(index));choices.Add(button);
            var icon=R("Fertilizer Icon Slot",button.transform,27,4,103,96);
            if(Recipes!=null&&i<Recipes.Length&&Recipes[i]?.output!=null)ItemIcon(icon,Recipes[i].output,i);
            T("Name",button.transform,new[]{"Pupuk Dasar","Pupuk Plus","Pupuk Super","Pupuk Premium"}[i],27,150,10,250,84);
        }
        var right=P("Recipe Detail",safe,1006,110,638,692);
        title=T("Fertilizer Name",right,"Pupuk Dasar",31,31,14,420,52);
        detailIcon=R("Detail Icon Slot",right,474,5,105,100);
        Line(right,31,90,408);T("Ingredients Title",right,"Bahan yang dibutuhkan",23,34,103,355,40);
        var needed=T("Count Heading",right,"Tersedia / Dibutuhkan",20,383,103,223,40);needed.alignment=TextAlignmentOptions.Right;
        for(int i=0;i<6;i++)
        {
            var row=R("Ingredient "+i,right,34,147+i*32,570,64);rows.Add(row);
            var icon=R("Ingredient Icon Slot",row,17,3,92,56).gameObject.AddComponent<Image>();icon.color=Color.clear;icon.raycastTarget=false;ingredientIcons.Add(icon);
            ingredientLabels.Add(T("Ingredient Name",row,"",24,154,2,287,60));
            var count=T("Available Required",row,"",26,443,2,110,60);count.alignment=TextAlignmentOptions.Right;counts.Add(count);
            // Stretch across the row, but keep a fixed thickness when its height changes.
            // Proportional vertical anchors used to shrink this to 0.5px in six-row recipes.
            var line=Line(row,0,0,570);
            line.anchorMin=Vector2.zero;line.anchorMax=Vector2.right;line.pivot=new(.5f,0);
            line.anchoredPosition=Vector2.zero;line.sizeDelta=new(0,DividerThickness);
        }
        feedback=T("Ingredient Status",right,"Bahan belum lengkap",21,37,342,561,46);
        Line(right,34,394,570);
        T("Output Heading",right,"Hasil produksi",25,35,412,500,44);
        outputIcon=R("Output Icon Slot",right,68,456,91,67);
        outputLabel=T("Output",right,"1× Pupuk Dasar",25,186,456,395,66);
        Line(right,34,528,570);
        T("Quantity Heading",right,"Jumlah",25,35,537,240,51);
        var quantity=P("Quantity Control",right,398,541,197,46);quantity.GetComponent<Image>().color=new(.33f,.39f,.36f,.30f);
        minus=B("Minus",quantity,"−",0,0,59,46,()=>SetQuantity(Quantity-1));
        quantityLabel=T("Quantity",quantity,"1",25,59,0,79,46);quantityLabel.alignment=TextAlignmentOptions.Center;
        plus=B("Plus",quantity,"+",138,0,59,46,()=>SetQuantity(Quantity+1));
        craft=B("Buat Pupuk",right,"Buat Pupuk",34,599,570,64,()=>Produce());
        craft.GetComponent<MainMenuRoundedImage>().color=new(.45f,.50f,.48f,.50f);
        legacy=B("Collect Legacy Queue",right,"Ambil hasil antrean lama",34,669,570,19,()=>{feedback.text=Machine.TryCollect()?"Hasil lama masuk tas":"Antrean lama belum selesai / tas penuh";dirty=true;Consume();});
        legacy.GetComponentInChildren<TMP_Text>().fontSize=15;legacy.gameObject.SetActive(false);
        var footer=P("Keyboard Controls",safe,562,866,545,43);
        Key(footer,"W",29,7,31,28);Key(footer,"S",61,7,29,28);T("Choose Hint",footer,"Pilih Pupuk",17,106,3,137,37);
        Key(footer,"Enter",237,7,62,28);enterHint=T("Craft Hint",footer,"Buat",17,315,3,69,37);
        var close=B("Tutup",footer,"",395,0,149,43,Close);close.GetComponent<Image>().color=Color.clear;
        Key(close.transform,"Esc",5,7,55,28);T("Close Hint",close.transform,"Tutup",17,78,3,70,37);
    }
    RectTransform R(string name,Transform parent,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        Vector2 size=dimensions[parent];Place(r,x,y,w,h,size.x,size.y);dimensions[r]=new(w,h);return r;
    }
    static void Place(RectTransform r,float x,float y,float w,float h,float pw,float ph)
    {r.anchorMin=new(x/pw,1-(y+h)/ph);r.anchorMax=new((x+w)/pw,1-y/ph);r.offsetMin=r.offsetMax=Vector2.zero;}
    RectTransform P(string name,Transform parent,float x,float y,float w,float h)
    {
        var r=R(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<MainMenuRoundedImage>();image.radius=19;
        image.borderWidth=1.3f;image.color=theme!=null?theme.panel:new(.145f,.19f,.17f,.96f);image.borderColor=theme!=null?theme.border:new(.64f,.70f,.64f,.45f);return r;
    }
    TMP_Text T(string name,Transform parent,string value,float font,float x,float y,float w,float h)
    {
        var t=R(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=font;t.color=Text;
        t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;return t;
    }
    Button B(string name,Transform parent,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction click)
    {
        var r=P(name,parent,x,y,w,h);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;b.onClick.AddListener(click);
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new(1.15f,1.15f,1.15f);colors.pressedColor=new(.8f,.9f,.8f);colors.disabledColor=new(.75f,.77f,.75f,.6f);b.colors=colors;
        if(!string.IsNullOrEmpty(label)){var text=T("Label",r,label,28,0,0,w,h);text.alignment=TextAlignmentOptions.Center;}
        return b;
    }
    void Key(Transform parent,string label,float x,float y,float w,float h)
    {
        var key=P("Key "+label,parent,x,y,w,h);var im=key.GetComponent<MainMenuRoundedImage>();im.radius=4;im.color=new(.27f,.33f,.29f,.65f);
        var text=T("Key Label",key,label,16,0,0,w,h);text.alignment=TextAlignmentOptions.Center;
    }
    RectTransform Line(Transform parent,float x,float y,float w)
    {
        var rect=R("Divider",parent,x,y,w,DividerThickness);var im=rect.gameObject.AddComponent<Image>();
        im.color=new(.72f,.76f,.69f,.40f);im.raycastTarget=false;return rect;
    }
    public void Close()
    {
        if(closing)return;closing=true;
        if(bag!=null)bag.OnInventoryChanged-=MarkDirty;
        player?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
        Time.timeScale=priorScale;Cursor.visible=priorCursor;Cursor.lockState=priorLock;
        foreach(var c in hidden)if(c!=null)c.enabled=true;
        if(ownedEventSystem!=null)Release(ownedEventSystem);
        if(Instance==this)Instance=null;Consume();gameObject.SetActive(false);Release(gameObject);
    }
    static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    void OnDisable(){if(Instance==this)Close();}
    void OnDestroy(){if(Instance==this)Close();}
}
