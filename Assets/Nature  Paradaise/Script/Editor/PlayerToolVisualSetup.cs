using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds meter-sized hand-grip prefabs from the imported tools, preserving source FBX and materials.</summary>
public static class PlayerToolVisualSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Prop/Tools";
    const string Prefabs=Root+"/Prefabs/Player/Tools";
    static bool rebuild;
    [MenuItem("Nature Paradise/Player/Apply Revised Tool Fit (Updated Player FBX)")]
    public static void ApplyRevisedToolFit()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before calibrating tool assets.");
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        if(catalog==null)throw new InvalidOperationException("Tool catalog missing");
        foreach(var pair in new[]{("Hoe",1.5f),("Axe",1.85f),("Hammer",1.8f),("WateringCan",.85f)})
        {
            var entry=catalog.Find(pair.Item1);
            if(entry==null)throw new InvalidOperationException("Missing tool "+pair.Item1);
            var filters=entry.prefab.GetComponentsInChildren<MeshFilter>(true);
            Bounds bounds=new Bounds();bool first=true;
            foreach(var filter in filters)
            {
                var b=filter.sharedMesh.bounds;var matrix=entry.prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    Vector3 v=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                    if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);
                }
            }
            float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            entry.scale=pair.Item2*PlayerDimensions.ReferenceScale/size;
        }
        foreach(string id in new[]{"Axe","Hammer","Sickle"})CalibrateGatheringAction(catalog.Find(id));
        SickleSweepSetup.Apply();
        CalibrateWateringGrip(catalog.Find("WateringCan"));
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("[HeldTools] Updated FBX fit applied: Original tool/body proportions at the shared player reference scale.");
    }
    public static void RebuildGenerated(){rebuild=true;try{Apply();}finally{rebuild=false;}}
    [MenuItem("Nature Paradise/Player/Create Imported Tool Visuals")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Prefabs);Directory.CreateDirectory(Models+"/Gameplay");Directory.CreateDirectory(Root+"/Resources/Player");AssetDatabase.Refresh();
        string path=Root+"/Resources/Player/Player Tool Visual Catalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(path);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<PlayerToolVisualCatalog>();AssetDatabase.CreateAsset(catalog,path);}
        AddWorkTools(catalog);
        AddCuttingTools(catalog);
        Add(catalog,"WateringCan",PlayerToolType.WateringCan,.85f,new Vector3(.08f,.77f,0f),Quaternion.Euler(0f,180f,0f),new Vector3(0f,0f,180f));
        var watering=catalog.Find("WateringCan");
        if(watering!=null && (rebuild || !watering.workGrip))CalibrateWateringGrip(watering);
        Add(catalog,"FishingRod",PlayerToolType.FishingRod,1.85f,new Vector3(.20f,.18f,.38f),Quaternion.Inverse(Quaternion.LookRotation(new Vector3(-.49f,.80f,-.68f),Vector3.up)));
        Add(catalog,"Shears",PlayerToolType.Shears,.32f,new Vector3(0f,.11f,-.15f),Quaternion.Euler(0f,90f,0f));
        Add(catalog,"Brush",PlayerToolType.None,.22f,new Vector3(0f,.44f,0f),Quaternion.identity,new Vector3(0f,0f,180f));
        Add(catalog,"Milker",PlayerToolType.Milker,.45f,new Vector3(-.26f,.54f,0f),Quaternion.identity,new Vector3(0f,0f,180f));
        // Mining currently uses Hammer; keep Pickaxe available as an Inspector alternative.
        Add(catalog,"Pickaxe",PlayerToolType.None,1.10f,new Vector3(-.36f,.34f,0f),Quaternion.Euler(0f,-90f,0f));
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("[HeldTools] Catalog + ten grip prefabs ready");
    }
    [MenuItem("Nature Paradise/Player/Rebuild Hoe and Axe Grips")]
    public static void RebuildWorkTools()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        if(catalog==null){Apply();return;}
        rebuild=true;try{AddWorkTools(catalog);}finally{rebuild=false;}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    [MenuItem("Nature Paradise/Player/Rebuild Hoe Grip")]
    public static void RebuildHoeGrip()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        if(catalog==null){Apply();return;}
        rebuild=true;try{AddWorkTools(catalog,"Hoe");}finally{rebuild=false;}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    static void AddWorkTools(PlayerToolVisualCatalog catalog,string onlyId=null)
    {
        // The imported hoe has its blade at the LOW end of Y; the axe at the HIGH end.
        // Canonical +Z runs along the shaft towards the head; +Y is the cutting edge.
        if(onlyId==null || onlyId=="Hoe")Add(catalog,"Hoe",PlayerToolType.Hoe,1.5f,new Vector3(-.10f,.73f,0f),Quaternion.Inverse(Quaternion.LookRotation(Vector3.down,Vector3.left)));
        if(onlyId==null || onlyId=="Axe")Add(catalog,"Axe",PlayerToolType.Axe,1.85f,new Vector3(-.10f,.37f,0f),Quaternion.Inverse(Quaternion.LookRotation(Vector3.up,Vector3.right)));
        foreach(var id in new[]{"Hoe","Axe"})
        {
            if(onlyId!=null && onlyId!=id)continue;
            var entry=catalog.Find(id);if(entry==null || (!rebuild && entry.workGrip))continue;
            entry.workGrip=true;entry.twoHandsWhileCarrying=id=="Axe";
            entry.handPosition=Vector3.zero;
            entry.secondHandGrip=id=="Axe"?new Vector3(0f,0f,-.30f):new Vector3(0f,.15f,-.2325f);
            entry.carryDirection=id=="Axe"?new Vector3(.55f,.3f,.78f):new Vector3(0f,-.75f,.66f);
            CalibrateCarryGrips(catalog,id);
            if(id=="Axe")CalibrateGatheringAction(entry);
        }
    }
    [MenuItem("Nature Paradise/Player/Rebuild Hammer and Sickle Grips")]
    public static void RebuildCuttingTools()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        if(catalog==null){Apply();return;}
        rebuild=true;try{AddCuttingTools(catalog);}finally{rebuild=false;}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    static void AddCuttingTools(PlayerToolVisualCatalog catalog)
    {
        // Hammer: +Z along shaft, +Y normal to an end striking face (source +X).
        Add(catalog,"Hammer",PlayerToolType.Hammer,1.8f,new Vector3(.00013f,.30f,0f),Quaternion.Inverse(Quaternion.LookRotation(Vector3.up,Vector3.right)));
        // Keep the compact pack sickle without replacing the authored inventory icon.
        if(rebuild||catalog.Find("Sickle")==null)SickleSweepSetup.Apply();
        foreach(string id in new[]{"Hammer"})
        {
            var entry=catalog.Find(id);if(entry==null || (!rebuild && entry.workGrip))continue;
            entry.workGrip=true;entry.handPosition=Vector3.zero;
            entry.twoHandsDuringAction=id=="Hammer";entry.twoHandsWhileCarrying=id=="Hammer";
            entry.secondHandGrip=new Vector3(0f,0f,-.22f);
            entry.carryDirection=id=="Hammer"?new Vector3(.3f,.6f,.75f):new Vector3(0f,-.75f,.66f);
            CalibrateCarryGrips(catalog,id);
            CalibrateGatheringAction(entry);
            FarmActionEffectsSetup.RefreshHammerAnchor();
        }
    }
    [MenuItem("Nature Paradise/Player/Correct Hammer Strike Grip")]
    public static void CorrectHammerStrikeGrip()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before correcting the hammer grip.");
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        var entry=catalog!=null?catalog.Find(PlayerToolType.Hammer):null;
        if(entry==null)throw new InvalidOperationException("Hammer visual entry missing");
        CalibrateGatheringAction(entry);
        FarmActionEffectsSetup.RefreshHammerAnchor();
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    static void CalibrateGatheringAction(PlayerToolVisualCatalog.Entry entry)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        var set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
        var clip=set==null?null:entry.tool==PlayerToolType.Hammer?set.hammeringRock:entry.tool==PlayerToolType.Axe?set.choppingTree:set.sickle;
        if(source==null || clip==null)return;
        var preview=UnityEngine.Object.Instantiate(source);
        try
        {
            var rig=preview.GetComponentsInChildren<Animator>(true).First(x=>x.runtimeAnimatorController!=null);rig.enabled=false;
            float contact=Mathf.Min(clip.length,entry.tool==PlayerToolType.Hammer?.68f:entry.tool==PlayerToolType.Axe?.55f:.85f);
            clip.SampleAnimation(rig.gameObject,contact);
            var hand=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");
            var left=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:LeftHand");
            Vector3 shaft=entry.tool==PlayerToolType.Sickle?(preview.transform.forward-Vector3.up*.15f).normalized:(PalmPosition(hand)-PalmPosition(left)).normalized;
            Vector3 normal=Vector3.ProjectOnPlane(Vector3.up,shaft);
            Quaternion cuttingPose=Quaternion.LookRotation(shaft,entry.tool==PlayerToolType.Hammer?-normal:Vector3.Cross(shaft,normal));
            entry.handEuler=(Quaternion.Inverse(hand.rotation)*cuttingPose).eulerAngles;
            if(entry.tool!=PlayerToolType.Sickle)
            {
                Quaternion sampledRotation=hand.rotation;
                float separation=Vector3.Distance(PalmPosition(hand),PalmPosition(left));
                clip.SampleAnimation(rig.gameObject,Mathf.Max(0,contact-.025f));Vector3 before=PalmPosition(hand);
                clip.SampleAnimation(rig.gameObject,Mathf.Min(clip.length,contact+.025f));Vector3 after=PalmPosition(hand);
                Vector3 face=Vector3.ProjectOnPlane(after-before,shaft).normalized;
                if(entry.tool==PlayerToolType.Hammer)
                {
                    shaft=Vector3.ProjectOnPlane(shaft,preview.transform.right).normalized;
                    face=Vector3.Cross(preview.transform.right,shaft).normalized;
                }
                if(face.sqrMagnitude<.1f)throw new InvalidOperationException("No swing velocity at contact for "+entry.id);
                entry.actionPlaneNormal=preview.transform.InverseTransformDirection(Vector3.Cross(shaft,face).normalized);
                cuttingPose=Quaternion.LookRotation(shaft,face);
                entry.handEuler=(Quaternion.Inverse(sampledRotation)*cuttingPose).eulerAngles;
                entry.secondHandGrip=new Vector3(0,0,-separation/entry.scale);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    static Vector3 PalmPosition(Transform hand)
    {
        var knuckles=hand.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("HandIndex1") || t.name.EndsWith("HandMiddle1") || t.name.EndsWith("HandRing1")).ToArray();
        return knuckles.Length==0?hand.position:Vector3.Lerp(hand.position,knuckles.Aggregate(Vector3.zero,(v,t)=>v+t.position)/knuckles.Length,.75f);
    }
    static void CalibrateWateringGrip(PlayerToolVisualCatalog.Entry entry)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        var set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
        if(entry==null || source==null || set==null || set.wateringPlant==null)throw new InvalidOperationException("Watering calibration source missing");
        var preview=UnityEngine.Object.Instantiate(source);
        try
        {
            var rig=preview.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController!=null);rig.enabled=false;
            var hand=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");
            Quaternion canonical=Quaternion.Inverse(Quaternion.LookRotation(Vector3.right,Vector3.up));
            set.idle.SampleAnimation(rig.gameObject,.25f);
            entry.carryHandEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(preview.transform.forward,Vector3.up)*canonical).eulerAngles;
            set.wateringPlant.SampleAnimation(rig.gameObject,Mathf.Min(set.wateringPlant.length,1.36f));
            entry.handEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation((preview.transform.forward-Vector3.up*.65f).normalized,Vector3.up)*canonical).eulerAngles;
            entry.handPosition=Vector3.zero;entry.workGrip=true;entry.twoHandsDuringAction=false;entry.twoHandsWhileCarrying=false;
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    [MenuItem("Nature Paradise/Player/Calibrate Carried Tool Grips")]
    public static void CalibrateCarriedToolGrips()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        if(catalog==null)return;
        CalibrateCarryGrips(catalog,null);
        var watering=catalog.Find("WateringCan");
        if(watering!=null && watering.workGrip)CalibrateWateringGrip(watering);
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    static void CalibrateCarryGrips(PlayerToolVisualCatalog catalog,string onlyId)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        var animations=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
        if(source==null || animations==null || animations.idle==null)return;
        var preview=UnityEngine.Object.Instantiate(source);
        try
        {
            var rig=preview.GetComponentsInChildren<Animator>(true).FirstOrDefault(x=>x.runtimeAnimatorController!=null);
            if(rig==null)return;
            rig.enabled=false;animations.idle.SampleAnimation(rig.gameObject,.25f);
            var hand=preview.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="mixamorig:RightHand");
            if(hand==null)return;
            foreach(var entry in catalog.entries.Where(x=>x!=null && x.workGrip && x.tool!=PlayerToolType.WateringCan && (onlyId==null || x.id==onlyId)))
            {
                Vector3 shaft=preview.transform.TransformDirection(entry.carryDirection).normalized;
                Quaternion carry=Quaternion.LookRotation(shaft,Vector3.ProjectOnPlane(Vector3.down,shaft));
                entry.carryHandEuler=(Quaternion.Inverse(hand.rotation)*carry).eulerAngles;
                entry.handPosition=Vector3.zero;
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Add(PlayerToolVisualCatalog catalog,string id,PlayerToolType tool,float longestSize,Vector3 sourceGrip,Quaternion orientation,Vector3? handEuler=null)
    {
        longestSize*=PlayerDimensions.ReferenceScale;
        var entry=catalog.Find(id);if(entry!=null && !rebuild)return; // Preserve Inspector edits on subsequent setup runs.
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/Tool_"+id+".fbx");if(source==null)throw new InvalidOperationException("Missing tool "+id);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source);
        var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        float scale=longestSize/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
        var root=new GameObject("Held_"+id);
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            var child=new GameObject("Model");child.transform.SetParent(root.transform,false);
            Matrix4x4 matrix=Matrix4x4.TRS(-(orientation*sourceGrip)*scale,orientation,Vector3.one*scale)*filter.transform.localToWorldMatrix;
            Mesh mesh=BuildMesh(filter.sharedMesh,matrix);
            mesh.name=id+"_"+root.transform.childCount;string meshPath=Models+"/Gameplay/"+mesh.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);
            else
            {
                // Rebuild the GPU buffers too; CopySerialized alone can leave a stale preview mesh.
                existing.Clear();existing.indexFormat=mesh.indexFormat;existing.name=mesh.name;
                existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.subMeshCount=mesh.subMeshCount;
                for(int s=0;s<mesh.subMeshCount;s++)existing.SetTriangles(mesh.GetTriangles(s),s);
                existing.bounds=mesh.bounds;existing.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);
            }
            child.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=child.AddComponent<MeshRenderer>();mr.sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;mr.shadowCastingMode=ShadowCastingMode.On;
            Debug.Log("[HeldTools] "+id+" vertices "+filter.sharedMesh.vertexCount+" -> "+mesh.vertexCount+", size="+longestSize+"m");
        }
        if(id=="FishingRod")
        {
            // FBX includes a hanging line; use the actual rod endpoint, not the decorative hook.
            var tip=new GameObject("RodTip").transform;tip.SetParent(root.transform,false);tip.localPosition=orientation*(new Vector3(-.29f,.98f,-.30f)-sourceGrip)*scale;
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/Held_"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(model);
        if(entry!=null)catalog.entries.Remove(entry);
        catalog.entries.Add(new PlayerToolVisualCatalog.Entry{id=id,tool=tool,prefab=prefab,handEuler=handEuler??new Vector3(0f,-90f,90f)});
    }

    // Cluster only dense imported surfaces. UV bins retain atlas seams; normals are averaged within each cluster.
    // A 256px texture cannot represent the discarded subpixel surface detail on a hand-sized model.
    static Mesh BuildMesh(Mesh original,Matrix4x4 matrix)
    {
        var positions=original.vertices;var normals=original.normals;var uv=original.uv;
        for(int i=0;i<positions.Length;i++){positions[i]=matrix.MultiplyPoint3x4(positions[i]);if(normals.Length==positions.Length)normals[i]=matrix.MultiplyVector(normals[i]).normalized;}
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};
        if(original.vertexCount<=40000)
        {
            mesh.vertices=positions;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=original.subMeshCount;
            for(int s=0;s<original.subMeshCount;s++)mesh.SetTriangles(original.GetTriangles(s),s);
        }
        else
        {
            var b=new Bounds(positions[0],Vector3.zero);foreach(var v in positions)b.Encapsulate(v);float cell=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))/100f;
            var indices=new Dictionary<(int,int,int,int,int),int>();var vp=new List<Vector3>();var vn=new List<Vector3>();var vt=new List<Vector2>();var counts=new List<int>();int[] remap=new int[positions.Length];
            for(int i=0;i<positions.Length;i++)
            {
                Vector3 p=positions[i];Vector2 t=uv.Length==positions.Length?uv[i]:Vector2.zero;
                var key=(Mathf.RoundToInt(p.x/cell),Mathf.RoundToInt(p.y/cell),Mathf.RoundToInt(p.z/cell),Mathf.RoundToInt(t.x*128),Mathf.RoundToInt(t.y*128));
                if(!indices.TryGetValue(key,out int n)){n=vp.Count;indices.Add(key,n);vp.Add(Vector3.zero);vn.Add(Vector3.zero);vt.Add(Vector2.zero);counts.Add(0);}
                remap[i]=n;vp[n]+=p;vn[n]+=normals.Length==positions.Length?normals[i]:Vector3.up;vt[n]+=t;counts[n]++;
            }
            for(int i=0;i<vp.Count;i++){vp[i]/=counts[i];vn[i]=vn[i].normalized;vt[i]/=counts[i];}
            mesh.SetVertices(vp);mesh.SetNormals(vn);mesh.SetUVs(0,vt);mesh.subMeshCount=original.subMeshCount;
            for(int s=0;s<original.subMeshCount;s++)
            {
                var input=original.GetTriangles(s);var output=new List<int>(input.Length);
                for(int i=0;i<input.Length;i+=3){int a=remap[input[i]],b1=remap[input[i+1]],c=remap[input[i+2]];if(a==b1||b1==c||a==c)continue;output.Add(a);output.Add(b1);output.Add(c);}
                mesh.SetTriangles(output,s);
            }
        }
        mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
}
