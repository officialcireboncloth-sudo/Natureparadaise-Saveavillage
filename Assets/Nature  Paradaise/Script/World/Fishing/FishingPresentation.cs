using System.Collections.Generic;
using UnityEngine;

/// <summary>Bone-following rod, bend, splash and temporary catch presentation.</summary>
[DefaultExecutionOrder(1200)]
public sealed class FishingPresentation : MonoBehaviour
{
    FishingSystem owner;
    PlayerHeldTools heldTools;
    PlayerToolVisualCatalog.Entry rodEntry;
    Animator animator;
    Transform rightHand, leftHand, rod, catchVisual;
    Transform importedRodTip;
    Mesh importedRodMesh;
    MeshFilter importedRodFilter;
    Vector3[] rodRestVertices, rodVertices, rodRestNormals, rodNormals;
    Vector3 rodRestTip;
    float previousBend=-1f;
    LineRenderer rodCurve;
    LineRenderer castMarker;
    Material bobberMaterial;
    readonly List<Material> materials = new();
    readonly List<Material> catchMaterials = new();
    public Vector3 RodTip => importedRodTip != null ? importedRodTip.position : rodCurve != null ? rod.TransformPoint(rodCurve.GetPosition(rodCurve.positionCount - 1)) : transform.position + Vector3.up * 2f;

    public void Bind(FishingSystem system, Animator rig)
    {
        owner = system; animator = rig;
        heldTools=GetComponent<PlayerHeldTools>();
        rightHand = Bone(HumanBodyBones.RightHand, "mixamorig:RightHand");
        leftHand = Bone(HumanBodyBones.LeftHand, "mixamorig:LeftHand");
    }
    Transform Bone(HumanBodyBones bone, string name)
    {
        if(animator == null) return null;
        if(animator.isHuman) return animator.GetBoneTransform(bone);
        foreach(Transform child in animator.GetComponentsInChildren<Transform>(true)) if(child.name == name) return child;
        return null;
    }
    Material MakeMaterial(Color color, bool temporary = false)
    {
        Material material = ToonWorldStyle.CreateMaterial(color); (temporary ? catchMaterials : materials).Add(material); return material;
    }
    GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type); part.name = name;
        part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
        part.GetComponent<Collider>().enabled = false;
        part.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color, parent == catchVisual); return part;
    }
    public GameObject CreateRod()
    {
        var entry=Resources.Load<PlayerToolVisualCatalog>(PlayerToolVisualCatalog.ResourcePath)?.Find(PlayerToolType.FishingRod);
        if(entry!=null)
        {
            rodEntry=entry;
            rod=Instantiate(entry.prefab,transform,false).transform;rod.name="HeldTool_FishingRod";
            importedRodTip=rod.Find("RodTip");
            importedRodFilter=rod.GetComponentInChildren<MeshFilter>();
            if(importedRodFilter!=null && importedRodTip!=null)
            {
                importedRodMesh=Instantiate(importedRodFilter.sharedMesh);importedRodFilter.sharedMesh=importedRodMesh;importedRodMesh.MarkDynamic();
                rodRestVertices=importedRodMesh.vertices;rodVertices=new Vector3[rodRestVertices.Length];rodRestNormals=importedRodMesh.normals;rodNormals=new Vector3[rodRestNormals.Length];rodRestTip=importedRodTip.localPosition;
            }
            LateUpdate();return rod.gameObject;
        }
        rod = new GameObject("Fishing Rod").transform;
        rod.SetParent(transform,false);
        Part(rod,"Handle",PrimitiveType.Cylinder,new Vector3(0f,0f,0.18f),new Vector3(0.055f,0.2f,0.055f),new Color(0.3f,0.16f,0.06f)).transform.localRotation=Quaternion.Euler(90f,0f,0f);
        rodCurve = new GameObject("Bendable Rod").AddComponent<LineRenderer>();
        rodCurve.transform.SetParent(rod,false); rodCurve.useWorldSpace=false; rodCurve.positionCount=16;
        rodCurve.startWidth=0.045f; rodCurve.endWidth=0.009f;
        Material lineMaterial = new(Shader.Find("Sprites/Default")); materials.Add(lineMaterial); rodCurve.sharedMaterial=lineMaterial;
        rodCurve.startColor=new Color(0.45f,0.23f,0.08f); rodCurve.endColor=new Color(0.8f,0.62f,0.3f);
        LateUpdate(); return rod.gameObject;
    }
    void LateUpdate()
    {
        if(owner == null) return;
        UpdateCastMarker();
        // A hidden rod must not reposition the shared palm socket of the equipped work tool.
        if(rod != null && rod.gameObject.activeInHierarchy)
        {
            Vector3 facing = owner.RodFacing;
            facing.y = 0f;
            if(facing.sqrMagnitude < 0.01f) facing = transform.forward;
            Quaternion carryRotation = Quaternion.LookRotation(facing.normalized, Vector3.up) * Quaternion.Euler(-owner.CarryRodElevation, 0f, 0f);
            if(rodEntry!=null && heldTools!=null && heldTools.GripPoint!=null)
            {
                heldTools.SnapToGrip(rod,rodEntry);
                if(owner.State!=FishingState.Idle)rod.localRotation=owner.RodGripRotation;
            }
            else
            {
                rod.rotation=rightHand!=null?rightHand.rotation*owner.RodGripRotation:carryRotation;
                Vector3 gripPosition=rightHand!=null?rightHand.position:transform.position+Vector3.up*1.5f;
                rod.position=importedRodTip!=null?gripPosition:gripPosition-rod.forward*.18f;
                Vector3 parentScale=transform.lossyScale;
                rod.localScale=new Vector3(1f/Mathf.Max(.0001f,Mathf.Abs(parentScale.x)),1f/Mathf.Max(.0001f,Mathf.Abs(parentScale.y)),1f/Mathf.Max(.0001f,Mathf.Abs(parentScale.z)));
            }
            float bend=owner.State==FishingState.Charging?owner.CastPower*0.45f:owner.State==FishingState.Casting?(0.2f+owner.CastPower*0.6f):owner.IsReeling?0.32f:0.05f;
            if(rodCurve!=null)for(int i=0;i<16;i++) { float t=i/15f; rodCurve.SetPosition(i,new Vector3(0f,-bend*t*t,2.1f*t)); }
            if(importedRodMesh!=null && rod.gameObject.activeInHierarchy && Mathf.Abs(previousBend-bend)>.002f) BendImportedRod(bend);
        }
        if(catchVisual != null)
        {
            catchVisual.position = rightHand != null && leftHand != null ? (rightHand.position+leftHand.position)*0.5f : transform.position+Vector3.up*1.7f+transform.forward*0.6f;
            catchVisual.rotation=transform.rotation;
        }
    }
    void BendImportedRod(float bend)
    {
        previousBend=bend;float length=Mathf.Max(.1f,rodRestTip.z);
        for(int i=0;i<rodVertices.Length;i++)
        {
            float t=Mathf.Clamp01(rodRestVertices[i].z/length);rodVertices[i]=rodRestVertices[i]+Vector3.down*(bend*t*t);
            if(i<rodNormals.Length){Vector3 n=rodRestNormals[i];n.z+=2f*bend*t/length*n.y;rodNormals[i]=n.normalized;}
        }
        importedRodMesh.vertices=rodVertices;if(rodNormals.Length==rodVertices.Length)importedRodMesh.normals=rodNormals;importedRodMesh.RecalculateBounds();
        importedRodTip.localPosition=rodRestTip+Vector3.down*bend;
    }
    public void ShowCatch(ItemSO item)
    {
        HideCatch(); if(item==null) return;
        catchVisual=new GameObject("Fishing Catch Display").transform;
        if(item.worldPrefab != null)
        {
            GameObject visual=Instantiate(item.worldPrefab,catchVisual,false);
            foreach(Collider collider in visual.GetComponentsInChildren<Collider>()) collider.enabled=false;
            foreach(Rigidbody body in visual.GetComponentsInChildren<Rigidbody>()) { body.isKinematic=true; body.detectCollisions=false; }
            foreach(MonoBehaviour behaviour in visual.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled=false;
            visual.transform.localScale=item.worldScale;
        }
        else if(item.category==ItemCategory.Fish)
        {
            Part(catchVisual,"Fish Body",PrimitiveType.Sphere,Vector3.zero,new Vector3(0.65f,0.23f,0.18f),new Color(0.42f,0.68f,0.65f));
            Part(catchVisual,"Tail",PrimitiveType.Cube,new Vector3(-0.36f,0f,0f),new Vector3(0.16f,0.23f,0.06f),new Color(0.23f,0.42f,0.4f));
            Part(catchVisual,"Eye",PrimitiveType.Sphere,new Vector3(0.22f,0.035f,-0.085f),Vector3.one*0.04f,Color.black);
        }
        else if(item.Id.Contains("tin_can"))
            Part(catchVisual,"Tin Can",PrimitiveType.Cylinder,Vector3.zero,new Vector3(0.22f,0.16f,0.22f),new Color(0.55f,0.58f,0.6f));
        else if(item.Id.Contains("fish_bone"))
        {
            Part(catchVisual,"Spine",PrimitiveType.Cube,Vector3.zero,new Vector3(0.5f,0.035f,0.035f),Color.white);
            for(int i=0;i<5;i++) Part(catchVisual,"Rib",PrimitiveType.Cube,new Vector3(-0.18f+i*0.09f,0f,0f),new Vector3(0.025f,0.02f,0.24f),Color.white);
        }
        else
        {
            Color color=item.Id.Contains("sock")?new Color(0.65f,0.67f,0.48f):new Color(0.35f,0.22f,0.12f);
            Part(catchVisual,"Foot",PrimitiveType.Cube,Vector3.zero,new Vector3(0.35f,0.12f,0.19f),color);
            if(!item.Id.Contains("slipper")) Part(catchVisual,"Ankle",PrimitiveType.Cube,new Vector3(-0.12f,0.13f,0f),new Vector3(0.12f,0.22f,0.17f),color);
        }
        LateUpdate();
    }
    public void StyleBobber(GameObject bobber)
    {
        if (bobberMaterial == null)
        {
            bobberMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            bobberMaterial.color = new Color(1f, 0.25f, 0.08f);
            materials.Add(bobberMaterial);
        }
        bobber.GetComponent<Renderer>().sharedMaterial = bobberMaterial;
    }
    void UpdateCastMarker()
    {
        bool visible = owner.State == FishingState.Charging && owner.HasCastPreview;
        if (!visible) { if (castMarker != null) castMarker.enabled = false; return; }
        if (castMarker == null)
        {
            castMarker = new GameObject("Cast Landing Preview_Runtime").AddComponent<LineRenderer>();
            castMarker.transform.SetParent(transform, false);
            castMarker.useWorldSpace = true;
            castMarker.loop = true;
            castMarker.positionCount = 32;
            castMarker.startWidth = castMarker.endWidth = .035f;
            Material markerMaterial = new(Shader.Find("Sprites/Default"));
            materials.Add(markerMaterial);
            castMarker.sharedMaterial = markerMaterial;
        }
        castMarker.enabled = true;
        castMarker.startColor = castMarker.endColor = Color.Lerp(new Color(.2f, .85f, 1f), new Color(1f, .75f, .15f), owner.CastPower);
        Vector3 center = owner.CastPreviewPoint + Vector3.up * .04f;
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2f / 32f;
            castMarker.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .3f);
        }
    }
    public void HideCatch() { if(catchVisual!=null) Destroy(catchVisual.gameObject); catchVisual=null; foreach(var material in catchMaterials) if(material!=null) Destroy(material); catchMaterials.Clear(); }
    public void Splash(Vector3 position)
    {
        GameObject splash=new("Fishing Landing Splash"); splash.transform.position=position;
        ParticleSystem particles=splash.AddComponent<ParticleSystem>();
        particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main; main.loop=false; main.duration=0.3f; main.startLifetime=0.4f; main.startSpeed=1.4f; main.startSize=0.09f; main.startColor=new Color(0.75f,0.92f,1f,0.8f); main.gravityModifier=0.5f;
        var emission=particles.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0f,14)});
        var shape=particles.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=45f; shape.radius=0.15f;
        splash.transform.rotation=Quaternion.Euler(-90f,0f,0f);
        var renderer=particles.GetComponent<ParticleSystemRenderer>(); if(rodCurve!=null) renderer.sharedMaterial=rodCurve.sharedMaterial;
        else { Material splashMaterial=new(Shader.Find("Sprites/Default")); materials.Add(splashMaterial); renderer.sharedMaterial=splashMaterial; }
        particles.Play(); Destroy(splash,1.2f);
    }
    void OnDestroy() { HideCatch(); if(rod!=null) Destroy(rod.gameObject); if(importedRodMesh!=null) Destroy(importedRodMesh); foreach(var material in materials) if(material!=null) Destroy(material); }
}
