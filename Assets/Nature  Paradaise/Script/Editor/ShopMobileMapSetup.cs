#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>One-shot authoring of the approved mobile shops and existing modular fence in Map.</summary>
[InitializeOnLoad]
public static class ShopMobileMapSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Buildings/ShopMobile_A";
    const string Prefabs=Root+"/Prefabs/ShopsMobile";
    const string MapPath=Root+"/Map/Scenes/World/Map.unity";
    const string Output="ArtSource/ShopBuildings_Unity/Previews";
    const string Request="ArtSource/ShopBuildings_Unity/apply.request";
    const string AtlasPath=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials/TFP_Atlas_1A.mat";
    const string FencePath=Root+"/Prefabs/FarmReference/Modules/";
    static bool busy;static double nextCheck;
    static Material atlas;static StringBuilder log;
    static readonly Dictionary<string,GameObject> modules=new();
    static readonly Dictionary<string,JToken> records=new();
    static readonly Dictionary<string,Quaternion> adapters=new();
    static readonly string[] AssetNames={"ChickenStore_Mobile_A","ChickenCoop_Mobile_A","ChickenCoop_Mobile_WeatherVane_Optional","MiniMarket_A"};
    static ShopMobileMapSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<nextCheck)return;nextCheck=EditorApplication.timeSinceStartup+1;
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try
        {
            Directory.CreateDirectory(Output);
            File.Move(Request,Output+"/ApplyRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Apply();
        }
        catch(Exception e){File.WriteAllText(Output+"/UnityApplyError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Require(bool ok,string message)
    {
        if(!ok)throw new InvalidOperationException("Shop mobile: "+message);
        log.AppendLine("PASS "+message);
    }
    static Transform Child(Transform p,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(p,false);return t;
    }
    static Bounds Measure(GameObject root)
    {
        bool first=true;Bounds b=default;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))foreach(var v in f.sharedMesh.vertices)
        {
            Vector3 p=root.transform.InverseTransformPoint(f.transform.TransformPoint(v));
            if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
        }
        return b;
    }
    static Bounds Expected(JToken entry)
    {
        var lo=entry["bounds"][0];var hi=entry["bounds"][1];
        Vector3 min=new(-(float)hi[0],(float)lo[2],-(float)hi[1]);
        Vector3 max=new(-(float)lo[0],(float)hi[2],-(float)lo[1]);
        return new Bounds((min+max)*.5f,max-min);
    }
    static GameObject Visual(string name,Quaternion rotation)
    {
        var go=new GameObject(name);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/"+name+".fbx"));
        model.name="Geometry";model.transform.SetParent(go.transform,false);model.transform.localRotation=rotation*model.transform.localRotation;
        return go;
    }
    static void ImportModels()
    {
        var chicken=JObject.Parse(File.ReadAllText("ArtSource/ChickenStore_A_Mobile/Previews/MeshManifest.json"));
        foreach(var a in chicken["assets"])records[(string)a["name"]]=a;
        records[AssetNames[2]]=chicken["optionalOrnament"];
        records["MiniMarket_A"]=JObject.Parse(File.ReadAllText("ArtSource/MiniMarket_A/Previews/MeshManifest.json"));
        using(var sha=SHA256.Create())
        {
            string psd=Root+"/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd";
            string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(psd))).Replace("-","").ToLowerInvariant();
            Require(hash==(string)chicken["atlasSHA256"] && hash==(string)records["MiniMarket_A"]["atlasSHA256"],"Original Toonpack PSD hash unchanged");
        }
        foreach(string name in AssetNames)
        {
            string folder=name=="MiniMarket_A"?"MiniMarket_A":"ChickenStore_A_Mobile";
            string path=Models+"/"+name+".fbx";File.Copy("ArtSource/"+folder+"/Exports/"+name+".fbx",path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TFP_Atlas_1A"),atlas);importer.SaveAndReimport();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(asset && asset.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),name+" shared atlas remapped");
            int triangles=asset.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3);
            Require(triangles==(int)records[name]["triangles"],name+" native triangle count "+triangles);
            log.AppendLine(name+" Unity mesh vertices "+asset.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.vertexCount));
            var probe=Visual(name,Quaternion.identity);var geometry=probe.transform.GetChild(0);Quaternion imported=geometry.localRotation;
            float best=float.PositiveInfinity;Quaternion chosen=Quaternion.identity;Bounds expected=Expected(records[name]);
            foreach(int yaw in new[]{0,90,180,270})
            {
                geometry.localRotation=Quaternion.Euler(0,yaw,0)*imported;Bounds b=Measure(probe);
                float error=Vector3.Distance(b.min,expected.min)+Vector3.Distance(b.max,expected.max);
                if(error<best){best=error;chosen=Quaternion.Euler(0,yaw,0);}
            }
            Object.DestroyImmediate(probe);Require(best<.01f,name+" scale/sides/front +Z match original bounds; error "+best);adapters[name]=chosen;
        }
    }
    static void Box(Transform p,string name,Vector3 center,Vector3 size)
    {
        var c=Child(p,name).gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;
    }
    static void MakeBuildingModules()
    {
        foreach(string name in AssetNames)
        {
            var go=Visual(name,adapters[name]);
            if(name=="ChickenStore_Mobile_A")Box(go.transform,"BodyCollider",new Vector3(0,1.5f,0),new Vector3(8,3,6));
            else if(name=="ChickenCoop_Mobile_A")Box(go.transform,"BodyCollider",new Vector3(0,.875f,0),new Vector3(3,1.75f,2.8f));
            else if(name=="MiniMarket_A")Box(go.transform,"BodyCollider",new Vector3(0,1.975f,0),new Vector3(10,3.95f,7));
            int hingeExpected=name=="ChickenStore_Mobile_A"?1:name=="MiniMarket_A"?2:0;
            var hinges=go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Hinge")).ToArray();
            Require(hinges.Length==hingeExpected,name+" keeps separate door hinges "+hingeExpected);
            foreach(var hinge in hinges)
            {
                var leaf=hinge.GetComponentsInChildren<MeshFilter>(true).Single();var collider=leaf.gameObject.AddComponent<BoxCollider>();
                collider.center=leaf.sharedMesh.bounds.center;collider.size=leaf.sharedMesh.bounds.size;
                var before=leaf.transform.position;var staticBody=go.GetComponentsInChildren<MeshFilter>(true).First(f=>f!=leaf).transform.position;
                var rotation=hinge.localRotation;hinge.localRotation=rotation*Quaternion.Euler(0,0,25);
                Require(Vector3.Distance(staticBody,go.GetComponentsInChildren<MeshFilter>(true).First(f=>f!=leaf).transform.position)<.0001f,name+" door pivot leaves static body fixed");
                hinge.localRotation=rotation;
            }
            modules[name]=PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/Modules/"+name+".prefab");Object.DestroyImmediate(go);
        }
        foreach(string name in new[]{"Fence_Post","Fence_Corner_90","Fence_Junction_T","Fence_Rails_1m","Fence_Rails_2m","Fence_Gate_2m"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FencePath+name+".prefab");Require(prefab,name+" existing modular prefab available");modules[name]=prefab;
        }
    }
    sealed class Node {public Vector2 p;public readonly HashSet<Vector2> directions=new();}
    static readonly Dictionary<Vector2,Node> nodes=new();static readonly HashSet<Vector2> gatePosts=new();
    static GameObject Place(string key,Transform parent,Vector2 plan,Vector2 direction,string label)
    {
        var o=(GameObject)PrefabUtility.InstantiatePrefab(modules[key]);o.name=label;o.transform.SetParent(parent,false);o.transform.localPosition=new Vector3(-plan.x,0,plan.y);
        o.transform.localRotation=key.StartsWith("Fence_")?Quaternion.FromToRotation(Vector3.right,new Vector3(-direction.x,0,direction.y)):Quaternion.identity;
        return o;
    }
    static void Connect(Vector2 p,Vector2 direction)
    {
        if(!nodes.TryGetValue(p,out var n)){n=new Node{p=p};nodes.Add(p,n);}n.directions.Add(direction);
    }
    static void Run(Transform rails,Vector2 a,Vector2 b)
    {
        var dir=(b-a).normalized;float distance=Vector2.Distance(a,b);int length=Mathf.RoundToInt(distance);Require(Mathf.Abs(distance-length)<.001f,"Fence run uses whole metre pitch "+a+" to "+b);
        for(int i=0;i<length;)
        {
            int pitch=length-i>=2?2:1;var p=a+dir*i;var q=p+dir*pitch;
            Place("Fence_Rails_"+pitch+"m",rails,p,dir,"Rails_"+p.x+"_"+p.y+"_"+pitch+"m");Connect(p,dir);Connect(q,-dir);i+=pitch;
        }
    }
    static void Gate(Transform p,Vector2 a,string label)
    {
        Place("Fence_Gate_2m",p,a,Vector2.right,label);gatePosts.Add(a);gatePosts.Add(a+Vector2.right*2);
    }
    static GameObject ChickenGroup()
    {
        var root=new GameObject("ChickenStore_Mobile_Group");var buildings=Child(root.transform,"Buildings");
        Place(AssetNames[0],buildings,new Vector2(-2.25f,-7.5f),Vector2.right,"Chicken_Store_Mobile");
        var coop=Place(AssetNames[1],buildings,new Vector2(4.45f,-7.5f),Vector2.right,"Coop_Companion_Mobile");
        Place(AssetNames[2],coop.transform,Vector2.zero,Vector2.right,"Optional_WeatherVane");
        nodes.Clear();gatePosts.Clear();var outer=Child(root.transform,"Fence_Perimeter");var rails=Child(outer,"Rails");var posts=Child(root.transform,"Fence_SharedPosts");var pen=Child(root.transform,"Fence_ChickenPen");
        Run(rails,new Vector2(-12,-12),new Vector2(12,-12));Run(rails,new Vector2(-12,-12),new Vector2(-12,12));
        Run(rails,new Vector2(12,-12),new Vector2(12,1));Run(rails,new Vector2(12,1),new Vector2(12,12));
        Run(rails,new Vector2(-12,12),new Vector2(-3,12));Gate(outer,new Vector2(-3,12),"Gate_Main_2m");
        Run(rails,new Vector2(-1,12),new Vector2(2,12));Run(rails,new Vector2(2,12),new Vector2(6,12));Gate(outer,new Vector2(6,12),"Gate_ChickenPen_2m");Run(rails,new Vector2(8,12),new Vector2(12,12));
        Run(pen,new Vector2(2,1),new Vector2(12,1));Run(pen,new Vector2(2,1),new Vector2(2,12));
        foreach(var n in nodes.Values)
        {
            if(gatePosts.Contains(n.p))continue;
            string module=n.directions.Count>=3?"Fence_Junction_T":n.directions.Count==2 && Vector2.Dot(n.directions.First(),n.directions.Last())>-.5f?"Fence_Corner_90":"Fence_Post";
            var facing=n.directions.OrderBy(d=>Mathf.Atan2(d.y,d.x)).First();Place(module,posts,n.p,facing,module+"_"+n.p.x+"_"+n.p.y);
        }
        Require(posts.Cast<Transform>().Select(t=>t.localPosition).Distinct().Count()==posts.childCount,"Fence endpoints share one post; no duplicate junction posts");
        return root;
    }
    static GameObject MarketGroup()
    {
        var root=new GameObject("MiniMarket_Group");Place("MiniMarket_A",Child(root.transform,"Buildings"),Vector2.zero,Vector2.right,"MiniMarket_Mobile");return root;
    }
    static void Validate(GameObject root,bool chicken)
    {
        Require(root.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),root.name+" all renderers share original atlas");
        Require(root.GetComponentsInChildren<Transform>(true).All(t=>Vector3.Distance(t.localScale,Vector3.one)<.001f),root.name+" metre scales; fence modules unstretched");
        Require(root.GetComponentsInChildren<MeshCollider>(true).Length==0,root.name+" simple box collision only");
        Require(root.GetComponentsInChildren<Light>(true).Length==0,root.name+" no runtime lights");
        if(chicken)Require(root.transform.Find("Fence_Perimeter/Gate_Main_2m") && root.transform.Find("Fence_Perimeter/Gate_ChickenPen_2m"),"Chicken reference main gate and pen gate present");
        else Require(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Fence_")),"Minimarket reference has no enclosure fence");
        var transforms=root.GetComponentsInChildren<Transform>(true).Select(t=>(t,t.localPosition,t.localRotation)).ToArray();var old=root.transform.position;var oldR=root.transform.rotation;
        try
        {
            root.transform.SetPositionAndRotation(old+new Vector3(7,2,4),Quaternion.Euler(0,63,0)*oldR);
            Require(transforms.All(p=>p.t==root.transform || Vector3.Distance(p.t.localPosition,p.localPosition)<.00001f && Quaternion.Angle(p.t.localRotation,p.localRotation)<.001f),root.name+" single parent moves/rotates all parts together");
        }
        finally{root.transform.SetPositionAndRotation(old,oldR);}
    }
    static Vector3 Stage(Scene map,Vector3 anchor,bool chicken)
    {
        var terrains=map.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Terrain>(true)).Where(t=>t.terrainData).ToArray();
        var blockers=map.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled && !c.isTrigger && !(c is TerrainCollider)).ToArray();
        Vector3 half=chicken?new Vector3(13,3,13):new Vector3(7,3,6);float bestScore=float.PositiveInfinity;Vector3 best=anchor+new Vector3(36,0,0);
        foreach(float dx in new[]{36f,-36f,65f,-65f,0})foreach(float dz in new[]{0f,36f,-36f,65f,-65f})
        {
            if(dx==0 && dz==0)continue;var p=anchor+new Vector3(dx,0,dz);var heights=new List<float>();
            foreach(float x in new[]{-half.x,0,half.x})foreach(float z in new[]{-half.z,0,half.z})
            {
                var q=p+new Vector3(x,0,z);var terrain=terrains.FirstOrDefault(t=>q.x>=t.transform.position.x && q.z>=t.transform.position.z && q.x<=t.transform.position.x+t.terrainData.size.x && q.z<=t.transform.position.z+t.terrainData.size.z);
                if(terrain)heights.Add(terrain.SampleHeight(q)+terrain.transform.position.y);
            }
            if(terrains.Length>0 && heights.Count!=9)continue;
            p.y=(heights.Count>0?heights.Max():anchor.y)+.02f;var bounds=new Bounds(p+Vector3.up*3,half*2);
            float score=blockers.Count(c=>c.bounds.Intersects(bounds))*1000+(heights.Count>0?heights.Max()-heights.Min():0)*30+Vector2.Distance(new Vector2(p.x,p.z),new Vector2(anchor.x,anchor.z))*.01f;
            if(score<bestScore){bestScore=score;best=p;}
        }
        log.AppendLine("Staging score "+bestScore+" at "+best);return best;
    }
    static GameObject PlaceInMap(Scene map,GameObject prefab,Vector3 anchor,bool chicken)
    {
        var existing=map.GetRootGameObjects().SingleOrDefault(o=>o.name==prefab.name);
        if(existing){log.AppendLine("Preserved existing "+prefab.name+" transform "+existing.transform.position);return existing;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,map);Undo.RegisterCreatedObjectUndo(go,"Place mobile shop group");go.transform.position=Stage(map,anchor,chicken);return go;
    }
    [MenuItem("Nature Paradise/Shops/Apply Mobile Chicken Store and MiniMarket to Map",false,132)]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying shop groups.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs+"/Modules");Directory.CreateDirectory("ArtSource/ShopBuildings_Unity/Revisions");
        log=new StringBuilder("MOBILE SHOP MAP APPLY\n");modules.Clear();records.Clear();adapters.Clear();atlas=AssetDatabase.LoadAssetAtPath<Material>(AtlasPath);
        Require(atlas && atlas.shader && atlas.shader.isSupported,"Existing atlas material and shader supported");
        var map=SceneManager.GetSceneByPath(MapPath);if(!map.isLoaded)map=EditorSceneManager.OpenScene(MapPath,OpenSceneMode.Additive);
        string backup="ArtSource/ShopBuildings_Unity/Revisions/Map_BeforeMobileShops_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".unity";File.Copy(MapPath,backup);
        ImportModels();MakeBuildingModules();
        var cg=ChickenGroup();Validate(cg,true);var cp=PrefabUtility.SaveAsPrefabAsset(cg,Prefabs+"/ChickenStore_Mobile_Group.prefab");Object.DestroyImmediate(cg);
        var mg=MarketGroup();Validate(mg,false);var mp=PrefabUtility.SaveAsPrefabAsset(mg,Prefabs+"/MiniMarket_Group.prefab");Object.DestroyImmediate(mg);AssetDatabase.SaveAssets();
        var farm=map.GetRootGameObjects().FirstOrDefault(o=>o.name=="FarmReference_Group");Vector3 anchor=farm?farm.transform.position:new Vector3(212,18.5f,478);
        var chicken=PlaceInMap(map,cp,anchor,true);Validate(chicken,true);
        var market=PlaceInMap(map,mp,chicken.transform.position,false);Validate(market,false);
        Require(map.GetRootGameObjects().Count(o=>o.name==cp.name)==1 && map.GetRootGameObjects().Count(o=>o.name==mp.name)==1,"Exactly one instance of each shop group in Map");
        var results=new JArray();
        foreach(var root in new[]{chicken,market})results.Add(new JObject{["group"]=root.name,["position"]=new JArray(root.transform.position.x,root.transform.position.y,root.transform.position.z),["triangles"]=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3),["vertices"]=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.vertexCount),["renderers"]=root.GetComponentsInChildren<MeshRenderer>(true).Length,["boxColliders"]=root.GetComponentsInChildren<BoxCollider>(true).Length});
        EditorSceneManager.MarkSceneDirty(map);Require(EditorSceneManager.SaveScene(map),"Map saved with both movable prefab roots");
        // Release CPU copies for these new game meshes after native geometry validation.
        foreach(var name in AssetNames){var importer=(ModelImporter)AssetImporter.GetAtPath(Models+"/"+name+".fbx");importer.isReadable=false;importer.SaveAndReimport();}
        Require(AssetNames.All(n=>!((ModelImporter)AssetImporter.GetAtPath(Models+"/"+n+".fbx")).isReadable),"New imported model CPU read/write copies disabled after validation");
        AssetDatabase.SaveAssets();Selection.objects=new Object[]{chicken,market};
        var view=SceneView.lastActiveSceneView;if(view)view.LookAtDirect((chicken.transform.position+market.transform.position)*.5f+Vector3.up*2,Quaternion.Euler(48,-145,0),40);
        File.WriteAllText(Output+"/UnityImportAudit.txt",log+"\nUNITY_SHOPS_APPLY_PASS\n");
        File.WriteAllText(Output+"/UnityApplyResult.json",new JObject{["status"]="PASS",["scene"]=MapPath,["backup"]=backup,["groups"]=results,["sharedMaterial"]=AtlasPath,["newRuntimeTextures"]=0,["gameplayRegistrationChanged"]=false}.ToString());
        foreach(var prefab in new[]{cp,mp})try{RenderReview(prefab);}catch(Exception e){File.WriteAllText(Output+"/RenderError_"+prefab.name+".txt",e.ToString());}
        Debug.Log("UNITY_SHOPS_APPLY_PASS: ChickenStore_Mobile_Group and MiniMarket_Group saved in Map.");
    }
    static void RenderReview(GameObject prefab)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        try
        {
            var root=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(root,scene);
            var sun=new GameObject("Review Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(50,-35,0);
            var camera=new GameObject("Review Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.orthographic=true;camera.orthographicSize=prefab.name.StartsWith("Chicken")?19:9;
            camera.transform.position=new Vector3(25,29,36);camera.transform.LookAt(prefab.name.StartsWith("Chicken")?new Vector3(0,1,0):new Vector3(0,2,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.54f,.64f,.56f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;rt=new RenderTexture(1400,1100,24,RenderTextureFormat.ARGB32);rt.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Output+"/Unity_"+prefab.name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
