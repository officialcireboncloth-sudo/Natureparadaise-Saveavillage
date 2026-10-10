using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual-only construction scenery and capsule builder. Never enables unfinished building gameplay.</summary>
public sealed class ConstructionProjectVisuals
{
    readonly ConstructionWorkerSettings settings;
    readonly List<Material> ownedMaterials = new();
    readonly BuildingLevelDefinition level;
    readonly bool renovation;
    readonly Transform root;
    readonly Vector3 size;
    readonly Material wood, stone, roof, clothes, skin, hat;
    GameObject stageRoot, finalSource;
    int stage = -1;
    Transform worker, hammerPivot, leftLeg, rightLeg, body;
    Animator animator;
    Canvas bar;
    TMP_Text title, status;
    RectTransform fill;
    readonly Vector3 sitePosition;
    readonly Quaternion siteRotation;
    readonly Vector3 buildingPosition, workPosition;

    public Transform Worker => worker;
    public int Stage => stage;
    public ConstructionProjectVisuals(Transform owner, Transform anchor, Bounds bounds, Vector3 work,
        BuildingLevelDefinition target, GameObject finalVisual, bool upgrading, ConstructionWorkerSettings profile)
    {
        settings = profile; level = target; finalSource = finalVisual; renovation = upgrading;
        size = bounds.size; buildingPosition=anchor.position;workPosition=work;
        sitePosition = new Vector3(bounds.center.x,anchor.position.y,bounds.center.z); siteRotation = anchor.rotation;
        root = new GameObject("Construction_Visuals_Runtime").transform;
        root.SetParent(owner, false); root.SetPositionAndRotation(sitePosition, siteRotation);
        // Generated parts use world metres even when the authored building root has a scale.
        Vector3 scale = root.lossyScale;
        root.localScale = new Vector3(1 / Mathf.Max(.001f, scale.x), 1 / Mathf.Max(.001f, scale.y), 1 / Mathf.Max(.001f, scale.z));
        wood = Material(profile?.wood, new Color(.45f,.27f,.13f));
        stone = Material(profile?.stone, new Color(.57f,.61f,.58f));
        roof = Material(profile?.roof, new Color(.43f,.22f,.16f));
        clothes = Material(profile?.workerClothes, new Color(.23f,.36f,.42f));
        skin = Material(profile?.workerSkin, new Color(.91f,.76f,.56f));
        hat = Material(profile?.workerHat, new Color(.77f,.55f,.22f));
        BuildWorker(); BuildBar();
    }
    Material Material(Material assigned, Color color)
    {
        if (assigned != null) return assigned;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color }; ownedMaterials.Add(material); return material;
    }
    static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Collider>().enabled = false;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go.transform;
    }
    public static GameObject VisualClone(GameObject source, Transform parent)
    {
        // Instantiate under an inactive parent so cloned gameplay never receives OnEnable.
        bool active = parent.gameObject.activeSelf; parent.gameObject.SetActive(false);
        var clone = Object.Instantiate(source, parent); clone.name = source.name + "_ConstructionOnly";
        foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var light in clone.GetComponentsInChildren<Light>(true)) light.enabled = false;
        clone.SetActive(true); parent.gameObject.SetActive(active); return clone;
    }
    void BuildWorker()
    {
        var go = new GameObject("Lumber_Builder_Runtime"); worker = go.transform;
        worker.SetParent(root, false); worker.localScale = Vector3.one * (settings != null ? settings.workerScale : 1f);
        if (settings != null && settings.workerPrefab != null)
        {
            var model = VisualClone(settings.workerPrefab, worker); animator = model.GetComponentInChildren<Animator>();
            return;
        }
        body = Part(worker,"Builder_Capsule",PrimitiveType.Capsule,new Vector3(0,.96f,0),new Vector3(.57f,.66f,.5f),clothes);
        Part(worker,"Head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),Vector3.one*.38f,skin);
        Part(worker,"HatBrim",PrimitiveType.Cylinder,new Vector3(0,1.85f,0),new Vector3(.65f,.025f,.65f),hat);
        Part(worker,"HatCrown",PrimitiveType.Cylinder,new Vector3(0,1.94f,0),new Vector3(.40f,.09f,.4f),hat);
        leftLeg = new GameObject("LeftLegSwing").transform; leftLeg.SetParent(worker,false); leftLeg.localPosition=new Vector3(-.15f,.62f,0);
        rightLeg = new GameObject("RightLegSwing").transform; rightLeg.SetParent(worker,false); rightLeg.localPosition=new Vector3(.15f,.62f,0);
        Part(leftLeg,"Boot",PrimitiveType.Capsule,new Vector3(0,-.27f,0),new Vector3(.19f,.27f,.2f),clothes);
        Part(rightLeg,"Boot",PrimitiveType.Capsule,new Vector3(0,-.27f,0),new Vector3(.19f,.27f,.2f),clothes);
        Part(worker,"LeftArm",PrimitiveType.Capsule,new Vector3(-.38f,1.15f,.12f),new Vector3(.17f,.29f,.17f),skin);
        hammerPivot = new GameObject("HammerSwing").transform; hammerPivot.SetParent(worker,false);
        hammerPivot.localPosition = new Vector3(.34f,1.38f,0);
        Part(hammerPivot,"RightArm",PrimitiveType.Capsule,new Vector3(0,-.18f,.12f),new Vector3(.17f,.25f,.17f),skin).localRotation=Quaternion.Euler(-30,0,0);
        Part(hammerPivot,"HammerHandle",PrimitiveType.Cylinder,new Vector3(0,-.1f,.39f),new Vector3(.055f,.28f,.055f),wood).localRotation=Quaternion.Euler(75,0,0);
        Part(hammerPivot,"HammerHead",PrimitiveType.Cube,new Vector3(0,.01f,.63f),new Vector3(.35f,.16f,.15f),stone);
        if(settings != null && settings.hammerPrefab != null)
        {
            hammerPivot.Find("HammerHead").gameObject.SetActive(false); hammerPivot.Find("HammerHandle").gameObject.SetActive(false);
            var hammer=VisualClone(settings.hammerPrefab,hammerPivot); hammer.transform.localPosition=new Vector3(0,-.1f,.39f);
        }
    }
    public void Animate(bool walking, bool working, float time)
    {
        if(animator != null)
        {
            foreach(var parameter in animator.parameters)
                if(parameter.type == AnimatorControllerParameterType.Bool && (parameter.name == "Walking" || parameter.name == "Working"))
                    animator.SetBool(parameter.name,parameter.name == "Walking" ? walking : working);
            animator.speed = walking || working ? 1 : 0;
        }
        if(body == null) return;
        float gait=walking ? Mathf.Sin(time*8)*24 : 0;
        leftLeg.localRotation=Quaternion.Euler(gait,0,0); rightLeg.localRotation=Quaternion.Euler(-gait,0,0);
        body.localPosition=new Vector3(0,.96f+(walking ? Mathf.Abs(Mathf.Sin(time*8))*.035f : 0),0);
        float swing=working ? Mathf.Lerp(-90,35,Mathf.Pow((Mathf.Sin(time*7)+1)*.5f,.65f)) : walking ? -gait*.4f : 0;
        hammerPivot.localRotation=Quaternion.Euler(swing,0,0);
    }
    void BuildBar()
    {
        var go=new GameObject("Construction_Progress",typeof(RectTransform),typeof(Canvas));
        go.transform.SetParent(root,false); bar=go.GetComponent<Canvas>();bar.renderMode=RenderMode.WorldSpace;
        var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(420,108);rect.localScale=Vector3.one*.008f;
        GameplayHUDStyle.Surface(rect,GameplayHUDStyle.Modal,12);
        title=GameplayHUDStyle.Text("Project",rect,"",26,new Vector2(.05f,.60f),new Vector2(.95f,.94f));title.alignment=TextAlignmentOptions.Center;
        status=GameplayHUDStyle.Text("Stage",rect,"",22,new Vector2(.05f,.30f),new Vector2(.95f,.61f));status.alignment=TextAlignmentOptions.Center;status.color=GameplayHUDStyle.Muted;
        var track=GameplayHUDStyle.Rect("Track",rect,new Vector2(.06f,.12f),new Vector2(.94f,.24f));
        GameplayHUDStyle.Surface(track,GameplayHUDStyle.Card,6);
        fill=GameplayHUDStyle.Rect("Progress",track,Vector2.zero,Vector2.one);GameplayHUDStyle.Surface(fill,GameplayHUDStyle.Accent,6);
        foreach(var graphic in go.GetComponentsInChildren<Graphic>())
        {
            graphic.raycastTarget=false;
            // Progress is a HUD label: scaffold poles must not cut through its text.
            var text=graphic as TMP_Text;
            var overlay=new Material(text != null ? text.fontSharedMaterial : graphic.material);
            Shader textOverlay=settings != null && settings.progressTextShader != null ? settings.progressTextShader : Shader.Find("TextMeshPro/Distance Field Overlay");
            if(graphic is TMP_Text && textOverlay != null)
                overlay.shader=textOverlay;
            overlay.SetInt("unity_GUIZTestMode",(int)UnityEngine.Rendering.CompareFunction.Always);
            if(text != null)text.fontSharedMaterial=overlay;else graphic.material=overlay;
            ownedMaterials.Add(overlay);
        }
    }
    public void UpdateBar(string name, string label, float progress)
    {
        var camera=Camera.main;
        Vector3 position=workPosition+Vector3.up*(2.1f+(settings != null ? settings.barHeight : 1.2f));
        bool visible=camera != null && camera.WorldToViewportPoint(position).z > 0 && (camera.transform.position-position).sqrMagnitude < Mathf.Pow(settings != null ? settings.displayDistance : 42,2);
        bar.gameObject.SetActive(visible);
        if(!visible)return;
        bar.transform.SetPositionAndRotation(position,camera.transform.rotation);
        float depth=Vector3.Dot(position-camera.transform.position,camera.transform.forward);
        float visibleHeight=camera.orthographic ? camera.orthographicSize*2 : 2*Mathf.Max(.1f,depth)*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
        bar.transform.localScale=Vector3.one*(visibleHeight/Mathf.Max(1,camera.pixelHeight)*(settings != null ? settings.barPixelWidth : 260)/420);
        title.text=$"{name}  ·  {progress:P0}"; status.text=label; fill.anchorMax=new Vector2(Mathf.Max(.001f,progress),1);
    }
    public void SetStage(float progress)
    {
        int index=Mathf.Clamp(Mathf.FloorToInt(progress*4),0,3); if(index==stage)return; stage=index;
        if(stageRoot!=null){stageRoot.SetActive(false);Object.Destroy(stageRoot);}
        stageRoot=new GameObject($"Stage_{index}_Runtime");stageRoot.transform.SetParent(root,false);
        if(level?.constructionStagePrefabs != null && index<level.constructionStagePrefabs.Count && level.constructionStagePrefabs[index]!=null)
        {VisualClone(level.constructionStagePrefabs[index],stageRoot.transform);return;}
        float w=Mathf.Max(2,size.x),d=Mathf.Max(2,size.z),h=Mathf.Clamp(size.y,2.6f,7);
        // Renovations keep the existing house in place; scaffolding grows around its perimeter.
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=new(x*(w*.5f+.35f),0,z*(d*.5f+.35f));
            Part(stageRoot.transform,"SiteStake",PrimitiveType.Cylinder,corner+Vector3.up*.4f,new Vector3(.12f,.4f,.12f),wood);
            if(index>=1)
            {
                Part(stageRoot.transform,"FramePost",PrimitiveType.Cube,corner+Vector3.up*h*.5f,new Vector3(.16f,h,.16f),wood);
            }
        }
        if(index>=1)
            for(int z=-1;z<=1;z+=2)Part(stageRoot.transform,"ScaffoldRail",PrimitiveType.Cube,new Vector3(0,h*.56f,z*(d*.5f+.35f)),new Vector3(w+.85f,.12f,.14f),wood);
        if(index>=2)
        {
            // Platforms and ladder make the wall/roof work visibly different from the bare frame.
            for(int z=-1;z<=1;z+=2)
                Part(stageRoot.transform,"ScaffoldPlatform",PrimitiveType.Cube,new Vector3(0,h*.3f,z*(d*.5f+.4f)),new Vector3(index==3?w*.6f:w+.8f,.09f,.65f),wood);
            float ladderX=-w*.5f-.7f;
            for(int x=-1;x<=1;x+=2)Part(stageRoot.transform,"LadderSide",PrimitiveType.Cube,new Vector3(ladderX+x*.24f,h*.18f,-d*.5f-.6f),new Vector3(.09f,h*.4f,.09f),wood).localRotation=Quaternion.Euler(-12,0,0);
            for(int n=1;n<8;n++)Part(stageRoot.transform,"LadderRung",PrimitiveType.Cube,new Vector3(ladderX,n*h*.048f,-d*.5f-.6f),new Vector3(.55f,.065f,.08f),wood);
            if(index==3)
                for(int n=0;n<3;n++)Part(stageRoot.transform,"FinishingSupplies",PrimitiveType.Cylinder,new Vector3(-w*.5f-1.3f,.17f,d*.2f+n*.45f),new Vector3(.25f,.17f,.25f),n==0?roof:stone);
        }
        for(int n=0;n<5;n++)Part(stageRoot.transform,"LumberStack",PrimitiveType.Cube,new Vector3(-w*.5f-1.1f,.12f+n*.1f,d*.2f),new Vector3(.65f,.09f,1.5f),wood);
        if(!renovation)
        {
            for(int n=0;n<Mathf.CeilToInt(w);n++)for(int z=-1;z<=1;z+=2)
                Part(stageRoot.transform,"FoundationBlock",PrimitiveType.Cube,new Vector3(-w*.5f+n+.5f,.12f,z*d*.5f),new Vector3(.94f,.24f,.36f),stone);
            if(index>=2 && finalSource!=null)
            {
                var final=VisualClone(finalSource,stageRoot.transform);final.transform.SetPositionAndRotation(buildingPosition,siteRotation);
                if(index==2) foreach(var renderer in final.GetComponentsInChildren<Renderer>(true))
                { string part=renderer.name.ToLowerInvariant();if(part.Contains("roof")||part.Contains("door")||part.Contains("window"))renderer.enabled=false; }
            }
            else if(index>=2)
            {
                for(int z=-1;z<=1;z+=2)Part(stageRoot.transform,"WallPlanks",PrimitiveType.Cube,new Vector3(0,h*.35f,z*d*.5f),new Vector3(w,h*.65f,.14f),wood);
                for(int x=-1;x<=1;x+=2)Part(stageRoot.transform,"SidePlanks",PrimitiveType.Cube,new Vector3(x*w*.5f,h*.35f,0),new Vector3(.14f,h*.65f,d),wood);
                if(index==3)Part(stageRoot.transform,"RoofFinishing",PrimitiveType.Cube,new Vector3(0,h*.75f,0),new Vector3(w+.3f,.18f,d+.3f),roof);
            }
        }
        // Builder works at this bench at every stage, rather than swinging into empty space.
        {
            var bench=new GameObject("BuilderWorkBench").transform;bench.SetParent(stageRoot.transform,false);
            Vector3 facing=sitePosition-workPosition;facing.y=0;
            bench.SetPositionAndRotation(workPosition+facing.normalized*.65f,facing.sqrMagnitude>.01f?Quaternion.LookRotation(facing):siteRotation);
            Part(bench,"WorkBenchTop",PrimitiveType.Cube,new Vector3(0,.92f,0),new Vector3(1.4f,.12f,.55f),wood);
            Part(bench,"WorkingPlank",PrimitiveType.Cube,new Vector3(0,1.04f,0),new Vector3(.65f,.10f,.3f),wood);
            for(int x=-1;x<=1;x+=2)Part(bench,"WorkBenchLeg",PrimitiveType.Cube,new Vector3(x*.5f,.43f,0),new Vector3(.12f,.86f,.22f),wood);
        }
    }
    public void HideSite(){if(stageRoot!=null)stageRoot.SetActive(false);if(bar!=null)bar.gameObject.SetActive(false);}
    public void Dispose(){if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}foreach(var material in ownedMaterials)Object.Destroy(material);}
}
