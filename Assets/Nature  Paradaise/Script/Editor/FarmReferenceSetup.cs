#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Install the approved farm as one movable prefab; no building/save-system replacement.</summary>
[InitializeOnLoad]
public static class FarmReferenceSetup
{
    public const string Root="Assets/Nature  Paradaise";
    public const string Models=Root+"/mesh/Buildings/FarmBuildingsFence_A";
    public const string Prefabs=Root+"/Prefabs/FarmReference";
    public const string GroupPath=Prefabs+"/FarmReference_Group.prefab";
    public const string MapPath=Root+"/Map/Scenes/World/Map.unity";
    public const string Output="ArtSource/FarmBuildingsFence_A/Previews";
    const string Request="ArtSource/FarmBuildingsFence_A/apply-unity-v2.request";
    const string AtlasPath=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials/TFP_Atlas_1A.mat";
    static bool busy;static double nextCheck;
    static StringBuilder report;
    static Material atlas;
    static Quaternion adapter;
    static readonly string[] Names={"Farm_Lumbung_A","Farm_BarnStore_B","Fence_Post","Fence_Rails_1m","Fence_Rails_2m","Fence_Corner_90","Fence_Junction_T","Fence_Gate_2m","Fence_Gate_3m"};
    static readonly Dictionary<string,GameObject> modules=new();
    static FarmReferenceSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<nextCheck)return;
        nextCheck=EditorApplication.timeSinceStartup+1;
        // Refresh this setup tool after edits even when the Editor stays in the background.
        string source=Root+"/Script/Editor/FarmReferenceSetup.cs";
        if(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode &&
           File.GetLastWriteTimeUtc(source)>File.GetLastWriteTimeUtc(typeof(FarmReferenceSetup).Assembly.Location))
        {AssetDatabase.Refresh();return;}
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try
        {
            // Consume one explicit local request, never auto-install on every domain reload.
            string consumed=Output+"/ApplyRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".txt";
            Directory.CreateDirectory(Output);File.Move(Request,consumed);
            Apply();
        }
        catch(Exception e){File.WriteAllText(Output+"/UnityApplyError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Check(bool ok,string text)
    {
        if(!ok)throw new InvalidOperationException("Farm reference: "+text);
        report.AppendLine("PASS "+text);
    }
    static Transform Child(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    static Bounds Measure(GameObject root)
    {
        bool first=true;Bounds b=default;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
        foreach(Vector3 v in f.sharedMesh.vertices)
        {
            Vector3 p=root.transform.InverseTransformPoint(f.transform.TransformPoint(v));
            if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
        }
        return b;
    }
    static void Box(Transform parent,string name,Vector3 center,Vector3 size)
    {
        // Source right/left is mirrored in Unity when the authored front faces +Z.
        if(parent.parent && parent.parent.name.StartsWith("Farm_"))center.x=-center.x;
        var c=Child(parent,name).gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;
    }
    static GameObject Visual(string name,Quaternion rotation)
    {
        var root=new GameObject(name);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/"+name+".fbx"));
        model.name="Geometry";model.transform.SetParent(root.transform,false);model.transform.localRotation=rotation*model.transform.localRotation;
        return root;
    }
    [MenuItem("Nature Paradise/Farm Reference/Apply Group to Map",false,130)]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying the farm group.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs+"/Modules");
        report=new StringBuilder("FARM REFERENCE UNITY APPLY\n");modules.Clear();
        var manifest=JObject.Parse(File.ReadAllText(Output+"/MeshManifest.json"));
        atlas=AssetDatabase.LoadAssetAtPath<Material>(AtlasPath);Check(atlas!=null,"Original Toon Farm Pack atlas material available");
        Check(atlas.shader!=null && atlas.shader.isSupported,"Existing atlas shader supported by editor renderer: "+atlas.shader.name);
        foreach(string name in Names)
        {
            string path=Models+"/"+name+".fbx";
            File.Copy("ArtSource/FarmBuildingsFence_A/Exports/"+name+".fbx",path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;
            importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
            importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TFP_Atlas_1A"),atlas);
            importer.SaveAndReimport();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(asset && asset.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),name+" reuses shared atlas material");
            int triangles=asset.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3);
            var entry=manifest["assets"].Single(a=>(string)a["asset"]==name);
            Check(triangles==(int)entry["triangles"],name+" native import triangles "+triangles);
        }
        // Resolve actual FBX conversion from asymmetric front bounds AND the store ridge offset.
        // This keeps cream barn's open left nook and store's wider right canopy on the right sides.
        var probe=Visual("Farm_BarnStore_B",Quaternion.identity);float best=float.PositiveInfinity;
        Quaternion importedRotation=probe.transform.GetChild(0).localRotation;
        foreach(int yaw in new[]{0,90,180,270})
        {
            probe.transform.GetChild(0).localRotation=Quaternion.Euler(0,yaw,0)*importedRotation;
            Bounds b=Measure(probe);var entry=manifest["assets"].Single(a=>(string)a["asset"]=="Farm_BarnStore_B");
            var lo=entry["bounds"][0];var hi=entry["bounds"][1];
            var expectedMin=new Vector3(-(float)hi[0],(float)lo[2],-(float)hi[1]);
            var expectedMax=new Vector3(-(float)lo[0],(float)hi[2],-(float)lo[1]);
            var top=probe.GetComponentsInChildren<MeshFilter>(true).SelectMany(f=>f.sharedMesh.vertices.Select(v=>probe.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).Where(v=>v.y>5.39f).ToArray();
            float score=Vector3.Distance(b.min,expectedMin)+Vector3.Distance(b.max,expectedMax)+(top.Length==0?100:Mathf.Abs(top.Average(v=>v.x)-.5f));
            report.AppendLine("Orientation yaw "+yaw+": bounds "+b.min+" / "+b.max+", ridge points "+top.Length+", score "+score);
            if(score<best){best=score;adapter=Quaternion.Euler(0,yaw,0);}
        }
        File.WriteAllText(Output+"/UnityOrientationDiagnostics.txt",report.ToString());
        Object.DestroyImmediate(probe);Check(best<.015f,"FBX orientation matches source sides, roof ridge and front +Z (error "+best+")");
        foreach(string name in Names)
        {
            var go=Visual(name,name.StartsWith("Farm_")?adapter:Quaternion.identity);var body=Child(go.transform,"Colliders");
            if(name=="Farm_Lumbung_A")
            {
                Box(body,"MainBody",new Vector3(0,1.5f,0),new Vector3(4,3,5));
                Box(body,"RightWing",new Vector3(2.75f,1.1f,0),new Vector3(1.5f,2.2f,5));
                Box(body,"NookOuterWall",new Vector3(-3.48f,1.1f,0),new Vector3(.12f,2.2f,5));
                Box(body,"NookRearWall",new Vector3(-2.75f,1.1f,-2.48f),new Vector3(1.5f,2.2f,.12f));
            }
            else if(name=="Farm_BarnStore_B")
            {
                Box(body,"MainBody",new Vector3(-.5f,1.55f,0),new Vector3(5,3.1f,6));
                Box(body,"LeftWing",new Vector3(-3.75f,1.15f,0),new Vector3(1.5f,2.3f,6));
                Box(body,"CanopyRearWall",new Vector3(3.25f,1.1f,-2.98f),new Vector3(2.5f,2.2f,.12f));
                foreach(float z in new[]{-3f,0f,3f})Box(body,"CanopyPost_"+z,new Vector3(4.4f,1.1f,z),new Vector3(.22f,2.2f,.22f));
            }
            else if(name.StartsWith("Fence_Rails"))
            {
                float pitch=name.Contains("2m")?2:1;
                Box(body,"RailBarrier",new Vector3(pitch/2,.6f,0),new Vector3(pitch-.24f,1.1f,.12f));
                Bounds bounds=Measure(go);Check(Mathf.Abs(bounds.min.x-.12f)<.002f && Mathf.Abs(bounds.max.x-(pitch-.12f))<.002f,name+" positive-X pitch and no duplicate posts");
            }
            else
            {
                // Leaf colliders live on the same node as leaf geometry, preserving hinge motion.
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    var c=filter.gameObject.AddComponent<BoxCollider>();c.center=filter.sharedMesh.bounds.center;c.size=filter.sharedMesh.bounds.size;
                }
            }
            if(name.StartsWith("Fence_Gate"))
            {
                int expected=name.Contains("3m")?2:1;
                var hinges=go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Hinge_")).ToArray();
                Check(hinges.Length==expected,name+" keeps "+expected+" separate hinge pivots");
            }
            string dest=Prefabs+"/Modules/"+name+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,dest);Object.DestroyImmediate(go);modules[name]=prefab;
        }
        var group=BuildGroup();ValidateGroup(group);
        var saved=PrefabUtility.SaveAsPrefabAsset(group,GroupPath);Object.DestroyImmediate(group);
        AssetDatabase.SaveAssets();
        var map=SceneManager.GetSceneByPath(MapPath);
        if(!map.isLoaded)map=EditorSceneManager.OpenScene(MapPath,OpenSceneMode.Additive);
        var existing=map.GetRootGameObjects().FirstOrDefault(o=>o.name=="FarmReference_Group");
        if(!existing)
        {
            existing=(GameObject)PrefabUtility.InstantiatePrefab(saved,map);Undo.RegisterCreatedObjectUndo(existing,"Place farm reference group");
            existing.transform.position=InitialPosition(map);
            report.AppendLine("Created one Map group at "+existing.transform.position);
        }
        else report.AppendLine("Existing Map group transform preserved: "+existing.transform.position);
        ValidateGroup(existing);
        string backup="ArtSource/FarmBuildingsFence_A/Revisions/Map_BeforeFarmGroup.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(MapPath,backup);
        EditorSceneManager.MarkSceneDirty(map);Check(EditorSceneManager.SaveScene(map),"Map saved with one movable group");
        Selection.activeGameObject=existing;
        var sceneView=SceneView.lastActiveSceneView;
        if(sceneView)sceneView.LookAtDirect(existing.transform.position+Vector3.up*1.5f,Quaternion.Euler(48,-145,0),33);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Output+"/UnityImportAudit.txt",report.ToString()+"\nUNITY_APPLY_PASS\n");
        File.WriteAllText(Output+"/UnityApplyResult.json",new JObject{["status"]="PASS",["prefab"]=GroupPath,["scene"]=MapPath,["group"]="FarmReference_Group",["position"]=new JArray(existing.transform.position.x,existing.transform.position.y,existing.transform.position.z),["newRuntimeTextures"]=0,["modules"]=Names.Length,["groupOnly"]=true}.ToString());
        try{RenderReview(saved);}catch(Exception e){File.WriteAllText(Output+"/UnityRenderError.txt",e.ToString());Debug.LogWarning("Farm applied; render preview failed: "+e.Message);}
        Debug.Log("FARM_REFERENCE_APPLY_PASS: select FarmReference_Group to move/rotate everything together.");
    }

    sealed class Node
    {
        public Vector2 p;public readonly HashSet<Vector2> directions=new();
    }
    static readonly Dictionary<Vector2,Node> nodes=new();
    static readonly HashSet<Vector2> gatePosts=new();
    static GameObject Place(string name,Transform parent,Vector2 p,Vector2 direction,string label)
    {
        var o=(GameObject)PrefabUtility.InstantiatePrefab(modules[name]);o.name=label;o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(-p.x,0,p.y);
        o.transform.localRotation=name.StartsWith("Farm_")?Quaternion.identity:Quaternion.FromToRotation(Vector3.right,new Vector3(-direction.x,0,direction.y));return o;
    }
    static void Connect(Vector2 p,Vector2 direction)
    {
        if(!nodes.TryGetValue(p,out var n)){n=new Node{p=p};nodes.Add(p,n);}n.directions.Add(direction);
    }
    static void Run(Transform parent,Vector2 a,Vector2 b)
    {
        Vector2 dir=(b-a).normalized;int length=Mathf.RoundToInt(Vector2.Distance(a,b));
        Check(Mathf.Abs(Vector2.Distance(a,b)-length)<.001f,"Fence run uses whole-metre pitch "+a+" -> "+b);
        int step=0;
        while(step<length)
        {
            int pitch=length-step>=2?2:1;Vector2 p=a+dir*step,q=p+dir*pitch;
            Place("Fence_Rails_"+pitch+"m",parent,p,dir,"Rails_"+p.x+"_"+p.y+"_"+pitch+"m");
            Connect(p,dir);Connect(q,-dir);step+=pitch;
        }
    }
    static void Gate(Transform parent,Vector2 a,Vector2 direction,string label)
    {
        Place("Fence_Gate_2m",parent,a,direction,label);gatePosts.Add(a);gatePosts.Add(a+direction*2);
    }
    static GameObject BuildGroup()
    {
        nodes.Clear();gatePosts.Clear();var root=new GameObject("FarmReference_Group");
        var buildings=Child(root.transform,"Buildings");
        Place("Farm_Lumbung_A",buildings,new Vector2(-6,-6.5f),Vector2.right,"Lumbung_Krem");
        Place("Farm_BarnStore_B",buildings,new Vector2(4.5f,-6),Vector2.right,"Barn_Store");
        var outer=Child(root.transform,"Fence_Perimeter");var rails=Child(outer,"Rails");var posts=Child(outer,"Posts");
        var garden=Child(root.transform,"Fence_Garden");
        // 24 x 24 enclosure, front +Z. Main gate aligns with the store's door at X=4.
        Run(rails,new Vector2(-12,-12),new Vector2(12,-12));
        Run(rails,new Vector2(12,-12),new Vector2(12,12));
        // Split at T junctions, guaranteeing one shared post at each garden connection.
        Run(rails,new Vector2(-12,-12),new Vector2(-12,0));Run(rails,new Vector2(-12,0),new Vector2(-12,12));
        Run(rails,new Vector2(-12,12),new Vector2(-4,12));Run(rails,new Vector2(-4,12),new Vector2(3,12));
        Run(rails,new Vector2(5,12),new Vector2(12,12));Gate(outer,new Vector2(3,12),Vector2.right,"Gate_Front_2m");
        Run(garden,new Vector2(-12,0),new Vector2(-4,0));Run(garden,new Vector2(-4,0),new Vector2(-4,6));
        Gate(garden,new Vector2(-4,6),Vector2.up,"Gate_Garden_2m");Run(garden,new Vector2(-4,8),new Vector2(-4,12));
        foreach(var n in nodes.Values)
        {
            if(gatePosts.Contains(n.p))continue;
            string module=n.directions.Count>=3?"Fence_Junction_T":n.directions.Count==2 && Vector2.Dot(n.directions.First(),n.directions.Last())>-.5f?"Fence_Corner_90":"Fence_Post";
            var facing=n.directions.OrderBy(d=>Mathf.Atan2(d.y,d.x)).First();
            Place(module,posts,n.p,facing,module+"_"+n.p.x+"_"+n.p.y);
        }
        return root;
    }
    static void ValidateGroup(GameObject group)
    {
        Check(group.transform.Find("Buildings").childCount==2,"Two buildings share the group parent");
        Check(group.transform.Find("Fence_Perimeter/Gate_Front_2m") && group.transform.Find("Fence_Garden/Gate_Garden_2m"),"Reference perimeter gate and garden gate present");
        Check(group.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),"Entire farm uses original Toonpack atlas");
        Check(group.GetComponentsInChildren<Transform>(true).All(t=>Vector3.Distance(t.localScale,Vector3.one)<.001f),"Every module keeps metre scale; no stretched fence panels");
        var colliders=group.GetComponentsInChildren<Collider>(true);Check(colliders.Length>70,"Buildings/fences have simple solid colliders, including separate leaves");
        // Moving/rotating the parent leaves every child local transform unchanged.
        var locals=group.GetComponentsInChildren<Transform>(true).Select(t=>(t,t.localPosition,t.localRotation)).ToArray();
        Vector3 old=group.transform.position;Quaternion oldR=group.transform.rotation;
        try
        {
            group.transform.position+=new Vector3(13,2,-9);group.transform.rotation=Quaternion.Euler(0,90,0)*oldR;
            Check(locals.All(e=>e.t==group.transform||Vector3.Distance(e.t.localPosition,e.localPosition)<.00001f && Quaternion.Angle(e.t.localRotation,e.localRotation)<.001f),"Parent move/rotate preserves all child placements");
        }
        finally{group.transform.SetPositionAndRotation(old,oldR);}
        report.AppendLine("Group meshes "+group.GetComponentsInChildren<MeshFilter>(true).Length+", triangles "+group.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3));
    }
    static Vector3 InitialPosition(Scene map)
    {
        var terrains=map.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Terrain>(true)).Where(t=>t.terrainData && t.terrainData.size.x>32 && t.terrainData.size.z>32).OrderByDescending(t=>t.terrainData.size.x*t.terrainData.size.z).ToArray();
        if(terrains.Length==0)return new Vector3(35,0,35);
        var terrain=terrains[0];Vector3 origin=terrain.transform.position,size=terrain.terrainData.size;
        var blockers=map.GetRootGameObjects().Where(o=>o.name!="FarmReference_Group").SelectMany(o=>o.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled && !c.isTrigger && !(c is TerrainCollider)).ToArray();
        Vector3 best=origin+new Vector3(size.x*.5f,0,size.z*.5f);float bestScore=float.PositiveInfinity;
        foreach(float fx in new[]{.3f,.7f,.5f,.2f,.8f})foreach(float fz in new[]{.3f,.7f,.5f,.2f,.8f})
        {
            Vector3 p=origin+new Vector3(Mathf.Clamp(size.x*fx,15,size.x-15),0,Mathf.Clamp(size.z*fz,15,size.z-15));
            var heights=new List<float>();foreach(float dx in new[]{-12f,0,12f})foreach(float dz in new[]{-12f,0,12f})heights.Add(terrain.SampleHeight(p+new Vector3(dx,0,dz))+origin.y);
            p.y=heights.Max()+.02f;var footprint=new Bounds(p+Vector3.up*3,new Vector3(25,6,25));
            float score=blockers.Count(c=>c.bounds.Intersects(footprint))*100+(heights.Max()-heights.Min())*20+Vector2.Distance(new Vector2(p.x,p.z),new Vector2(origin.x+size.x*.5f,origin.z+size.z*.5f))*.01f;
            if(score<bestScore){bestScore=score;best=p;}
        }
        report.AppendLine("Initial editable staging position; clearance/flatness score "+bestScore);return best;
    }
    static void RenderReview(GameObject prefab)
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
        RenderTexture rt=null;GameObject root=null;
        try
        {
            root=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(root,scene);
            var sun=new GameObject("Review Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);
            sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(50,-35,0);
            var camera=new GameObject("Farm Review Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);
            camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=20;camera.nearClipPlane=.1f;camera.farClipPlane=200;
            camera.transform.position=new Vector3(28,34,38);camera.transform.LookAt(new Vector3(0,1,0));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.54f,.64f,.56f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(1500,1200,24,RenderTextureFormat.ARGB32);rt.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Output+"/Unity_FarmReference_Group.png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally
        {
            if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
}
#endif
