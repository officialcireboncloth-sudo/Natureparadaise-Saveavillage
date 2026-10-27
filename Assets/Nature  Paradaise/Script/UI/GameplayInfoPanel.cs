using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One safe-area information dock above tools: current action plus three queued, fading notifications.</summary>
[DefaultExecutionOrder(12000)]
public sealed class GameplayInfoPanel : MonoBehaviour
{
    sealed class Notice
    {
        public Inventory inventory; public ItemSO item; public long amount;
        public string message; public float age, duration; public Sprite icon;
    }
    sealed class Row { public RectTransform root; public Image icon; public TMP_Text text; public CanvasGroup group; }
    sealed class Hint { public Object owner; public string text; public int frame, priority; public float distance; public bool allowSuppression; }
    static GameplayInfoPanel instance;
    static readonly Regex SizeTags = new(@"</?size[^>]*>", RegexOptions.IgnoreCase);
    readonly List<Notice> active = new(3);
    readonly Queue<Notice> pending = new();
    readonly List<Row> rows = new(3);
    readonly Dictionary<Object,Hint> hints = new();
    readonly List<Object> staleHints = new();
    readonly HashSet<RectTransform> tools = new();
    readonly Vector3[] corners = new Vector3[4];
    Canvas canvas;
    RectTransform safe, dock, actionRoot, sideDock;
    TMP_Text hintText, sideText;
    CanvasGroup sideGroup;
    Notice sideNotice;
    Object sideInfoOwner;
    int sideInfoFrame=-10;
    string sideInfoTitle,sideInfoBody,sideInfoFooter,sideInfoText;
    RectTransform sideIconRoot;
    Material minimalTextMaterial;
    int interactionFrame = -10;
    float interactionHeight = 54;
    const float Duration = 2.5f, Fade = .5f, Width = 640;

    public static RectTransform ActionRoot => Ensure().actionRoot;
    /// <summary>Shared subtle text outline for the background-free bottom HUD, including touch prompts.</summary>
    public static void StyleMinimalText(TMP_Text text)
    {
        if(text==null)return;
        var panel=Ensure();
        if(panel.minimalTextMaterial==null&&text.fontSharedMaterial!=null)
        {
            panel.minimalTextMaterial=new Material(text.fontSharedMaterial){name="Gameplay HUD readable text"};
            if(panel.minimalTextMaterial.HasProperty("_OutlineWidth"))panel.minimalTextMaterial.SetFloat("_OutlineWidth",.16f);
            if(panel.minimalTextMaterial.HasProperty("_OutlineColor"))panel.minimalTextMaterial.SetColor("_OutlineColor",new Color(.025f,.035f,.025f,.85f));
            panel.minimalTextMaterial.EnableKeyword("OUTLINE_ON");
        }
        if(panel.minimalTextMaterial!=null)text.fontSharedMaterial=panel.minimalTextMaterial;
    }
    public static float ContentWidth => Mathf.Max(160,Mathf.Min(Width,Ensure().safe.rect.width-32));
    /// <summary>Registers the actual tools rectangle so the dock clears it at any canvas scale or safe inset.</summary>
    public static void RegisterTools(RectTransform rect){if(rect!=null)Ensure().tools.Add(rect);}
    public static void ReportInteraction(float height){var panel=Ensure();panel.interactionFrame=Time.frameCount;panel.interactionHeight=Mathf.Clamp(height,54,152);panel.dock.gameObject.SetActive(true);panel.actionRoot.gameObject.SetActive(true);}
    /// <summary>Refresh each frame while an action/tool/activity is relevant. Higher-priority activity replaces a generic tool hint.</summary>
    public static void RequestHint(Object owner,string text,int priority=10,float distance=0,bool allowSuppression=false)
    {
        if(owner==null||string.IsNullOrWhiteSpace(text))return;
        var panel=Ensure();if(!panel.hints.TryGetValue(owner,out var hint)){hint=new Hint{owner=owner};panel.hints.Add(owner,hint);}
        hint.text=SizeTags.Replace(text,"").Trim();hint.frame=Time.frameCount;hint.priority=priority;hint.distance=distance;hint.allowSuppression=allowSuppression;
    }
    public static void Notify(string message,float duration=Duration)
    {
        if(string.IsNullOrWhiteSpace(message))return;var panel=Ensure();message=Regex.Replace(message,@"\s+"," ").Trim();
        foreach(var notice in panel.active)if(notice.age<notice.duration&&notice.item==null&&notice.message==message){notice.age=0;return;}
        foreach(var notice in panel.pending)if(notice.item==null&&notice.message==message)return;
        panel.Enqueue(new Notice{message=message,duration=Mathf.Clamp(duration,2,3)});
    }
    /// <summary>Brief status at the bottom right beside tools, with a safe-area fallback above wide toolbars.</summary>
    public static void NotifyBesideTools(string message,float duration=Duration)
    {
        if(string.IsNullOrWhiteSpace(message))return;
        var panel=Ensure();
        panel.sideNotice=new Notice{message=Regex.Replace(message,@"\s+"," ").Trim(),duration=Mathf.Clamp(duration,2,3)};
    }
    /// <summary>Refresh a trough information card beside tools; temporary result/error notices take priority in the same space.</summary>
    public static void RequestBesideToolsInfo(Object owner,string title,string body,string footer)
    {
        if(owner==null||string.IsNullOrEmpty(title))return;
        var panel=Ensure();panel.sideInfoOwner=owner;panel.sideInfoFrame=Time.frameCount;
        if(panel.sideInfoTitle==title&&panel.sideInfoBody==body&&panel.sideInfoFooter==footer)return;
        panel.sideInfoTitle=title;panel.sideInfoBody=body;panel.sideInfoFooter=footer;
        panel.sideInfoText=$"<b>{title}</b>\n<size=18>{body}</size>\n<size=17><color=#BAC7D1>{footer}</color></size>";
    }
    public static void NotifyItem(Inventory inventory,ItemSO item,int amount)
    {
        if(inventory==null||item==null||amount<=0)return;var panel=Ensure();
        foreach(var notice in panel.active)if(notice.age<notice.duration&&SameItem(notice,inventory,item)){notice.amount+=amount;notice.age=0;return;}
        foreach(var notice in panel.pending)if(notice.age<notice.duration&&SameItem(notice,inventory,item)){notice.amount+=amount;return;}
        panel.Enqueue(new Notice{inventory=inventory,item=item,icon=item.icon,amount=amount,duration=Duration});
    }
    static bool SameItem(Notice notice,Inventory inventory,ItemSO item)=>notice.item!=null&&notice.inventory==inventory&&
        (notice.item==item||!string.IsNullOrEmpty(item.itemId)&&notice.item.itemId==item.itemId);
    void Enqueue(Notice notice){if(active.Count<3&&pending.Count==0)active.Add(notice);else pending.Enqueue(notice);}
    static GameplayInfoPanel Ensure()
    {
        if(instance!=null)return instance;
        instance=FindFirstObjectByType<GameplayInfoPanel>();
        if(instance==null)instance=new GameObject("Gameplay Information Dock",typeof(RectTransform)).AddComponent<GameplayInfoPanel>();
        return instance;
    }
    void Awake()
    {
        if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);
        canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;gameObject.AddComponent<GraphicRaycaster>();
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        safe=GameplayHUDStyle.Rect("SafeArea",transform,Vector2.zero,Vector2.one);safe.gameObject.AddComponent<SafeAreaFitter>();
        dock=GameplayHUDStyle.Rect("Bottom Information",safe,new Vector2(.5f,0),new Vector2(.5f,0));dock.pivot=new Vector2(.5f,0);dock.sizeDelta=new Vector2(Width,54);
        actionRoot=GameplayHUDStyle.Rect("Current Action",dock,new Vector2(0,0),new Vector2(1,0));actionRoot.pivot=new Vector2(.5f,0);actionRoot.sizeDelta=new Vector2(0,54);
        hintText=GameplayHUDStyle.Text("Tool or Activity",actionRoot,"",22,Vector2.zero,Vector2.one);hintText.color=Color.white;hintText.alignment=TextAlignmentOptions.Center;
        StyleMinimalText(hintText);
        hintText.rectTransform.offsetMin=new Vector2(16,8);hintText.rectTransform.offsetMax=new Vector2(-16,-8);hintText.textWrappingMode=TextWrappingModes.Normal;
        for(int i=0;i<3;i++)
        {
            var rect=GameplayHUDStyle.Rect("Notification "+(i+1),dock,new Vector2(0,0),new Vector2(1,0));rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,44);
            var iconRect=GameplayHUDStyle.Rect("Item Icon",rect,new Vector2(0,.5f),new Vector2(0,.5f));iconRect.sizeDelta=new Vector2(32,32);iconRect.anchoredPosition=new Vector2(30,0);
            var icon=iconRect.gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;
            var text=GameplayHUDStyle.Text("Notification",rect,"",21,Vector2.zero,Vector2.one);text.color=Color.white;text.textWrappingMode=TextWrappingModes.Normal;
            text.alignment=TextAlignmentOptions.Center;StyleMinimalText(text);
            var group=rect.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            rows.Add(new Row{root=rect,icon=icon,text=text,group=group});rect.gameObject.SetActive(false);
        }
        dock.gameObject.SetActive(false);
        sideDock=GameplayHUDStyle.Rect("Beside Tools Notice",safe,Vector2.zero,Vector2.zero);
        sideDock.pivot=Vector2.zero;
        sideText=GameplayHUDStyle.Text("Status",sideDock,"",21,Vector2.zero,Vector2.one);
        sideText.color=Color.white;sideText.alignment=TextAlignmentOptions.Center;sideText.textWrappingMode=TextWrappingModes.Normal;
        StyleMinimalText(sideText);
        sideText.rectTransform.offsetMin=new Vector2(16,10);sideText.rectTransform.offsetMax=new Vector2(-16,-10);
        sideIconRoot=GameplayHUDStyle.Rect("Trough Icon",sideDock,new Vector2(0,.5f),new Vector2(0,.5f));
        sideIconRoot.sizeDelta=new Vector2(54,54);sideIconRoot.anchoredPosition=new Vector2(42,0);
        var troughIcon=sideIconRoot.gameObject.AddComponent<BarnHUDIcon>();troughIcon.symbol=BarnHUDIcon.Symbol.Trough;troughIcon.raycastTarget=false;
        sideIconRoot.gameObject.SetActive(false);
        sideGroup=sideDock.gameObject.AddComponent<CanvasGroup>();sideGroup.interactable=false;sideGroup.blocksRaycasts=false;
        sideDock.gameObject.SetActive(false);
        Canvas.willRenderCanvases += AlignToTools;
    }
    void LateUpdate()
    {
        Hint selected=null;staleHints.Clear();
        foreach(var pair in hints)
        {
            var hint=pair.Value;
            if(pair.Key==null||hint.frame<Time.frameCount-1||pair.Key is Behaviour behaviour&&!behaviour.isActiveAndEnabled){staleHints.Add(pair.Key);continue;}
            if(WorldInteractionPrompt.IsSuppressed&&(!hint.allowSuppression||WorldInteractionPrompt.IsSuppressedExcept(hint.owner)))continue;
            if(selected==null||hint.priority>selected.priority||hint.priority==selected.priority&&hint.distance<selected.distance)selected=hint;
        }
        foreach(var key in staleHints)hints.Remove(key);
        bool hidden=GameplayPauseMenu.IsOpen||WorldInteractionPrompt.IsSuppressed&&selected==null;
        if(hidden){dock.gameObject.SetActive(false);sideDock.gameObject.SetActive(false);return;}
        if(sideNotice!=null)
        {
            sideNotice.age+=Time.unscaledDeltaTime;
            if(sideNotice.age>=sideNotice.duration)sideNotice=null;
        }
        bool sideInfo=sideInfoFrame==Time.frameCount&&sideInfoOwner!=null&&
            (!(sideInfoOwner is Behaviour infoOwner)||infoOwner.isActiveAndEnabled);
        bool showInfo=sideNotice==null&&sideInfo;
        sideDock.gameObject.SetActive(sideNotice!=null||showInfo);sideIconRoot.gameObject.SetActive(showInfo);
        sideText.alignment=showInfo?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.Center;
        sideText.rectTransform.offsetMin=new Vector2(showInfo?88:16,10);
        if(sideNotice!=null){sideText.text=sideNotice.message;sideGroup.alpha=Mathf.Clamp01((sideNotice.duration-sideNotice.age)/Fade);}
        else if(showInfo){sideText.text=sideInfoText;sideGroup.alpha=1;}
        for(int i=active.Count-1;i>=0;i--){active[i].age+=Time.unscaledDeltaTime;if(active[i].age>=active[i].duration)active.RemoveAt(i);}
        while(active.Count<3&&pending.Count>0)active.Add(pending.Dequeue());
        bool interaction=interactionFrame==Time.frameCount&&WorldInteractionPrompt.HasDisplayedPrompt;
        bool hasAction=interaction||selected!=null;
        hintText.gameObject.SetActive(!interaction&&selected!=null);
        if(selected!=null)hintText.text=selected.text;
        float width=Mathf.Max(160,Mathf.Min(Width,safe.rect.width-32));
        float actionHeight=interaction?interactionHeight:selected!=null?Mathf.Clamp(hintText.GetPreferredValues(selected.text,width-32,0).y+18,48,112):0;
        actionRoot.sizeDelta=new Vector2(0,actionHeight);actionRoot.gameObject.SetActive(hasAction);
        for(int i=0;i<rows.Count;i++)
        {
            bool visible=i<active.Count;var row=rows[i];row.root.gameObject.SetActive(visible);if(!visible)continue;
            var notice=active[i];row.root.anchoredPosition=new Vector2(0,actionHeight+(hasAction?4:0)+i*48);
            row.icon.sprite=notice.icon;row.icon.enabled=notice.icon!=null;
            row.text.text=notice.item!=null?(notice.icon==null?notice.item.itemName+" ":"")+$"<b>x{notice.amount}</b>":notice.message;
            float labelWidth=Mathf.Min(width-80,row.text.GetPreferredValues(row.text.text,width-80,0).x+4);
            float start=(width-labelWidth-40)*.5f;
            row.icon.rectTransform.anchoredPosition=new Vector2(start+16,0);
            row.text.rectTransform.offsetMin=new Vector2(notice.icon!=null?start+40:16,4);
            row.text.rectTransform.offsetMax=new Vector2(notice.icon!=null?-(width-start-40-labelWidth):-16,-4);
            row.group.alpha=Mathf.Clamp01((notice.duration-notice.age)/Fade);
        }
        float height=actionHeight+active.Count*48+(hasAction&&active.Count>0?4:0);
        dock.sizeDelta=new Vector2(width,Mathf.Max(44,height));
        dock.anchoredPosition=new Vector2(0,ToolsTop()+12);
        dock.gameObject.SetActive(hasAction||active.Count>0);
        AlignSideNotice();
    }
    float ToolsTop()
    {
        float top=12;tools.RemoveWhere(x=>x==null);
        foreach(var rect in tools)
        {
            if(!rect.gameObject.activeInHierarchy)continue;bool hidden=false;
            for(Transform p=rect;p!=null;p=p.parent)if(p.TryGetComponent<CanvasGroup>(out var group)&&group.alpha<.01f){hidden=true;break;}
            if(hidden)continue;
            var toolCanvas=rect.GetComponentInParent<Canvas>();Camera camera=toolCanvas!=null&&toolCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?toolCanvas.worldCamera:null;
            rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Vector2 screen=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local))top=Mathf.Max(top,local.y-safe.rect.yMin);
            }
        }
        return Mathf.Min(top,Mathf.Max(12,safe.rect.height-dock.sizeDelta.y-24));
    }
    bool TryGetToolsBounds(out Rect bounds)
    {
        bounds=default;bool found=false;tools.RemoveWhere(x=>x==null);
        foreach(var rect in tools)
        {
            if(!rect.gameObject.activeInHierarchy)continue;bool hidden=false;
            for(Transform p=rect;p!=null;p=p.parent)if(p.TryGetComponent<CanvasGroup>(out var group)&&group.alpha<.01f){hidden=true;break;}
            if(hidden)continue;
            var toolCanvas=rect.GetComponentInParent<Canvas>();
            Camera camera=toolCanvas!=null&&toolCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?toolCanvas.worldCamera:null;
            rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Vector2 screen=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local))continue;
                local-=safe.rect.min;
                if(!found){bounds=new Rect(local,Vector2.zero);found=true;}
                else bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,local.x),Mathf.Min(bounds.yMin,local.y),Mathf.Max(bounds.xMax,local.x),Mathf.Max(bounds.yMax,local.y));
            }
        }
        return found;
    }
    void AlignSideNotice()
    {
        if(sideDock==null||!sideDock.gameObject.activeInHierarchy)return;
        bool hasTools=TryGetToolsBounds(out Rect toolBounds);
        float gap=hasTools?safe.rect.width-toolBounds.xMax-28:340;
        bool beside=gap>=220;
        float width=Mathf.Max(160,Mathf.Min(340,beside?gap:safe.rect.width-32));
        bool info=sideIconRoot.gameObject.activeSelf;
        float height=Mathf.Max(info?98:54,sideText.GetPreferredValues(sideText.text,width-(info?104:32),0).y+20);
        float x=hasTools&&beside?toolBounds.xMax+12:safe.rect.width-width-16;
        float y=hasTools?(beside?Mathf.Max(12,toolBounds.yMin):toolBounds.yMax+12):12;
        if(dock.gameObject.activeInHierarchy)
        {
            Rect center=new(safe.rect.width*.5f-dock.sizeDelta.x*.5f,dock.anchoredPosition.y,dock.sizeDelta.x,dock.sizeDelta.y);
            if(center.Overlaps(new Rect(x,y,width,height)))y=center.yMax+12;
        }
        sideDock.sizeDelta=new Vector2(width,height);
        sideDock.anchoredPosition=new Vector2(x,Mathf.Clamp(y,12,Mathf.Max(12,safe.rect.height-height-16)));
    }
    void AlignToTools()
    {
        if(dock!=null&&dock.gameObject.activeInHierarchy)dock.anchoredPosition=new Vector2(0,ToolsTop()+12);
        AlignSideNotice();
    }
    void OnDestroy(){Canvas.willRenderCanvases-=AlignToTools;if(minimalTextMaterial!=null)Destroy(minimalTextMaterial);if(instance==this)instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>instance=null;
}
