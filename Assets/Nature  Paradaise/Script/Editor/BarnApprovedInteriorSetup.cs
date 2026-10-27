#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>One-shot import, binding and validation of the approved four-stage barn interior.</summary>
[InitializeOnLoad]
public static class BarnApprovedInteriorSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Buildings/BarnInterior_A";
    const string Prefabs=Root+"/Prefabs/Barn/Interior";
    const string ScenePath=Root+"/Map/Scenes/Interiors/BarnInterior.unity";
    const string Output="ArtSource/BarnInterior_A/Unity";
    const string Request="ArtSource/BarnInterior_A/apply-unity.request";
    const string ReviewRequest="ArtSource/BarnInterior_A/verify-unity.request";
    const string AtlasPath=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials/TFP_Atlas_1A.mat";
    const float LayoutScale=1.4f;
    static readonly int[] Capacity={4,8,14,20};
    static readonly float[] Width={12,16,20,24},Depth={10,12,14,16},FeedY={4.5f,5.5f,6,6.7f};
    static readonly float[][] TroughX={new[]{-2.3f,2.3f},new[]{-4f,0,4},new[]{-6f,-2,2,6},new[]{-8f,-4,0,4,8}};
    static bool busy;static double next;static Material atlas;static StringBuilder log;
    static BarnApprovedInteriorSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        if((!File.Exists(Request)&&!File.Exists(ReviewRequest))||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try
        {
            Directory.CreateDirectory(Output);
            bool apply=File.Exists(Request);string request=apply?Request:ReviewRequest;
            File.Move(request,Output+"/"+(apply?"ApplyRequest_":"VerifyRequest_")+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
            if(apply)Apply();else VerifySaved();
        }
        catch(Exception e){File.WriteAllText(Output+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Require(bool condition,string message)
    {if(!condition)throw new InvalidOperationException("Barn interior: "+message);log.AppendLine("PASS "+message);}
    static Transform Child(Transform parent,string name,Vector3 local=default)
    {var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=local;return o.transform;}
    static void Box(Transform parent,string name,Vector3 centre,Vector3 size)
    {var c=Child(parent,name).gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;}
    static Bounds BoundsOf(GameObject root)
    {
        var b=new Bounds();bool first=true;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))foreach(var v in f.sharedMesh.vertices)
        {Vector3 p=f.transform.TransformPoint(v);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
        return b;
    }
    static Bounds FilterBounds(MeshFilter f,Transform relative)
    {
        var matrix=relative.worldToLocalMatrix*f.transform.localToWorldMatrix;var b=new Bounds(matrix.MultiplyPoint3x4(f.sharedMesh.vertices[0]),Vector3.zero);
        foreach(var v in f.sharedMesh.vertices)b.Encapsulate(matrix.MultiplyPoint3x4(v));return b;
    }
    static string Key(MeshFilter f)
    {
        string n=f.name;var t=Regex.Match(n,"^FeedTrough_(\\d+)");
        if(t.Success)return "Trough_"+t.Groups[1].Value;
        var fill=Regex.Match(n,"^FeedFill_(\\d+)_(\\d+)");
        if(fill.Success)return "Fill_"+fill.Groups[1].Value+"_"+fill.Groups[2].Value;
        if(n.Contains("_Floor"))return "Floor";
        if(n.StartsWith("N01_")||n.Contains("FrontCutaway"))return "Shell";
        if(n.StartsWith("N03_")||n.StartsWith("N04_")||n.StartsWith("N06_"))return "Machine_"+n;
        if(n.StartsWith("N05_")||n.StartsWith("DropPipe")||n.Contains("Connection")||n.Contains("TankBridge"))return "Distribution";
        return "PropsAndStraw";
    }
    static Mesh Combine(IEnumerable<MeshFilter> filters,Transform frame,string name)
    {
        var mesh=new Mesh{name=name};mesh.indexFormat=IndexFormat.UInt16;
        mesh.CombineMeshes(filters.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=frame.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
        mesh.RecalculateBounds();MeshUtility.Optimize(mesh);return mesh;
    }
    static int AddOpenBackfaces(Mesh mesh)
    {
        // Blender previews open surfaces from both sides; Unity's shared material
        // culls backfaces. Duplicate only open connected patches of machinery.
        var vertices=mesh.vertices;var triangles=mesh.triangles;int count=triangles.Length/3;
        string Point(int n){Vector3 v=vertices[n];return Mathf.RoundToInt(v.x*100000)+","+Mathf.RoundToInt(v.y*100000)+","+Mathf.RoundToInt(v.z*100000);}
        string Edge(int a,int b){string x=Point(a),y=Point(b);return string.CompareOrdinal(x,y)<0?x+"|"+y:y+"|"+x;}
        var edges=new Dictionary<string,List<int>>();var keys=new string[count,3];
        for(int t=0;t<count;t++)for(int k=0;k<3;k++)
        {string edge=Edge(triangles[t*3+k],triangles[t*3+(k+1)%3]);keys[t,k]=edge;if(!edges.TryGetValue(edge,out var adjacent))edges[edge]=adjacent=new List<int>();adjacent.Add(t);}
        var done=new HashSet<int>();var reverse=new List<int>();
        for(int t=0;t<count;t++)
        {
            if(done.Contains(t))continue;var component=new List<int>();var queue=new Queue<int>();queue.Enqueue(t);done.Add(t);bool open=false;
            while(queue.Count>0)
            {
                int current=queue.Dequeue();component.Add(current);
                for(int k=0;k<3;k++)
                {var adjacent=edges[keys[current,k]];if(adjacent.Count==1)open=true;foreach(int other in adjacent)if(done.Add(other))queue.Enqueue(other);}
            }
            if(open)reverse.AddRange(component);
        }
        if(reverse.Count==0)return 0;
        var vlist=vertices.ToList();var normals=mesh.normals.ToList();var uv=mesh.uv.ToList();var output=triangles.ToList();var map=new Dictionary<int,int>();
        int Back(int old)
        {if(map.TryGetValue(old,out int index))return index;index=vlist.Count;map[old]=index;vlist.Add(vertices[old]);normals.Add(-mesh.normals[old]);uv.Add(mesh.uv[old]);return index;}
        foreach(int t in reverse){output.Add(Back(triangles[t*3+2]));output.Add(Back(triangles[t*3+1]));output.Add(Back(triangles[t*3]));}
        mesh.Clear();mesh.SetVertices(vlist);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(output,0);mesh.RecalculateBounds();return reverse.Count;
    }
    static GameObject Build(int i,Scene preview,JToken spec,out JObject report)
    {
        int level=i+1;string stem="BarnInterior_Lv"+level;string path=Models+"/"+stem+".fbx";
        File.Copy("ArtSource/BarnInterior_A/Exports/"+stem+".fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
        importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;
        importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TFP_Atlas_1A"),atlas);importer.SaveAndReimport();
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(imported,"Imported "+stem);
        var source=(GameObject)PrefabUtility.InstantiatePrefab(imported,preview);source.name="Source";
        Quaternion initial=source.transform.rotation,chosen=initial;float best=float.PositiveInfinity;
        var expected=new Bounds(new Vector3(0,1.61f,Depth[i]/2),new Vector3(Width[i]+.27f,3.58f,Depth[i]+.23f));
        var orientations=new JArray();
        foreach(int pitch in new[]{0,90,180,270})foreach(int yaw in new[]{0,90,180,270})foreach(int roll in new[]{0,90,180,270})
        {
            source.transform.rotation=Quaternion.Euler(pitch,yaw,roll)*initial;var b=BoundsOf(source);
            float error=Vector3.Distance(b.min,expected.min)+Vector3.Distance(b.max,expected.max);
            orientations.Add(new JObject{["rotation"]=source.transform.eulerAngles.ToString(),["min"]=b.min.ToString(),["max"]=b.max.ToString(),["error"]=error});
            if(error<best){best=error;chosen=source.transform.rotation;}
        }
        File.WriteAllText(Output+"/Orientation_Lv"+level+".json",orientations.ToString());
        source.transform.rotation=chosen;Require(best<.09f,stem+" metre bounds, rear +Z, entrance at origin (error "+best+")");
        var filters=source.GetComponentsInChildren<MeshFilter>(true);
        Require(filters.Sum(f=>f.sharedMesh.triangles.Length/3)==(int)spec["triangles"],stem+" imported triangle count");
        Require(source.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),stem+" shared Toonpack material");
        var root=new GameObject("Layout_Lv"+level+"_Editable");SceneManager.MoveGameObjectToScene(root,preview);
        var visuals=Child(root.transform,"Visuals_Optimized");var troughRoot=Child(root.transform,"FeedingTroughs_Editable");
        var collisions=Child(root.transform,"Collision_Editable");var sockets=Child(root.transform,"Sockets_Editable");var animals=Child(root.transform,"AnimalSpots_Editable");
        string meshPath=Models+"/"+stem+"_Runtime.asset";var meshAsset=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(meshAsset)AssetDatabase.DeleteAsset(meshPath);
        var groups=filters.GroupBy(Key).OrderBy(g=>g.Key).ToArray();var troughs=new Dictionary<int,Transform>();var fills=new SortedDictionary<int,Renderer>();bool firstMesh=true;int backfaceTriangles=0;
        foreach(var group in groups)
        {
            Transform target=visuals;string key=group.Key;
            if(key.StartsWith("Trough_"))
            {
                int number=int.Parse(key.Substring(7));target=Child(troughRoot,key,new Vector3(TroughX[i][number-1],0,FeedY[i]));troughs[number]=target;
            }
            else if(key.StartsWith("Fill_"))continue;
            var go=Child(target,key+"_Geometry");var mesh=Combine(group,go,stem+"_"+key);
            if(key.StartsWith("Machine_"))backfaceTriangles+=AddOpenBackfaces(mesh);
            if(firstMesh){AssetDatabase.CreateAsset(mesh,meshPath);firstMesh=false;}else AssetDatabase.AddObjectToAsset(mesh,meshPath);
            go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=atlas;
        }
        foreach(var group in groups.Where(g=>g.Key.StartsWith("Fill_")))
        {
            var parts=group.Key.Split('_');int number=int.Parse(parts[1]),slot=int.Parse(parts[2]);var target=troughs[number];
            var b=FilterBounds(group.First(),root.transform);var fill=Child(target,group.Key);fill.position=root.transform.TransformPoint(new Vector3(b.center.x,0,b.center.z));
            var mesh=Combine(group,fill,stem+"_"+group.Key);AssetDatabase.AddObjectToAsset(mesh,meshPath);
            fill.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=fill.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=atlas;
            fills[(number-1)*3+slot-1]=renderer;
        }
        Require(fills.Count==(level+1)*3,stem+" correct compartment count");
        var slots=new Transform[Capacity[i]];var fillRefs=new Renderer[Capacity[i]];
        for(int k=0;k<slots.Length;k++)
        {
            int physical=k*fills.Count/slots.Length;Renderer fill=fills[physical];Transform trough=fill.transform.parent;
            slots[k]=Child(trough,"FeedPortion_"+(k+1).ToString("00"));slots[k].position=fill.transform.position+Vector3.up*.36f;fillRefs[k]=fill;
        }
        foreach(var t in troughs.Values)Box(t,"TroughCollider",new Vector3(0,.325f,0),new Vector3(.9f,.65f,3.6f));
        Box(collisions,"Floor",new Vector3(0,-.09f,Depth[i]/2),new Vector3(Width[i],.18f,Depth[i]));
        foreach(int side in new[]{-1,1})
        {
            Box(collisions,"SideWall",new Vector3(side*(Width[i]/2+.06f),1.7f,Depth[i]/2),new Vector3(.14f,3.4f,Depth[i]));
            Box(collisions,"FrontStub",new Vector3(side*(Width[i]/4+.5f),.3f,0),new Vector3(Width[i]/2-1,.6f,.18f));
        }
        Box(collisions,"BackWall",new Vector3(0,1.7f,Depth[i]+.06f),new Vector3(Width[i],3.4f,.14f));
        foreach(var f in filters.Where(f=>Key(f).StartsWith("Machine_")||f.name.StartsWith("R_Barrel")||f.name.StartsWith("A_Storage")||f.name.StartsWith("A_Water")||f.name.StartsWith("A_Supply")||f.name.StartsWith("A_Sink")||f.name.StartsWith("A_BottleCounter")))
        {var b=FilterBounds(f,root.transform);Box(collisions,f.name+"_Collider",b.center,b.size);}
        Transform importedMaker=source.GetComponentsInChildren<Transform>(true).First(t=>t.name.StartsWith("FeedMaker_Position"));
        var maker=Child(sockets,"FeedMaker_Position");maker.position=importedMaker.position;
        Child(sockets,"Entrance");Child(sockets,"PlayerSpawn",new Vector3(0,.15f,1.7f));
        var lanes=new List<float>{TroughX[i][0]-1.55f};for(int k=1;k<TroughX[i].Length;k++)lanes.Add((TroughX[i][k-1]+TroughX[i][k])/2);lanes.Add(TroughX[i].Last()+1.55f);
        var candidates=new List<Vector3>();foreach(float y in new[]{FeedY[i]-1,FeedY[i]+1})foreach(float x in lanes)candidates.Add(new Vector3(x,.05f,y));
        for(int row=0;candidates.Count<Capacity[i];row++)for(int k=0;k<5;k++)candidates.Add(new Vector3((k-2)*3.3f,.05f,1.5f+row*1.65f));
        var spots=Enumerable.Range(0,Capacity[i]).Select(k=>Child(animals,"AnimalSlot_"+(k+1).ToString("00"),candidates[k])).ToArray();
        root.AddComponent<BarnInteriorLayout>().EditorConfigure(troughRoot,slots,fillRefs,spots,maker);
        var runtimeFilters=root.GetComponentsInChildren<MeshFilter>(true);int tris=runtimeFilters.Sum(f=>f.sharedMesh.triangles.Length/3);
        Require(tris==(int)spec["triangles"]+backfaceTriangles,stem+" combined geometry preserves every triangle plus "+backfaceTriangles+" open-surface backs");
        Require(root.GetComponentsInChildren<MeshRenderer>(true).Length<=(level==4?32:28),stem+" renderers reduced from "+filters.Length+" to "+runtimeFilters.Length);
        foreach(var fill in fills.Values)fill.gameObject.SetActive(false);
        root.transform.localScale=Vector3.one*LayoutScale;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/BarnInteriorLayout_Lv"+level+".prefab");
        report=new JObject{["level"]=level,["triangles"]=tris,["openSurfaceBackTriangles"]=backfaceTriangles,["vertices"]=runtimeFilters.Sum(f=>f.sharedMesh.vertexCount),["renderers"]=runtimeFilters.Length,["originalRenderers"]=filters.Length,["logicalFeedSlots"]=slots.Length,["visualCompartments"]=fills.Count,["animalSlots"]=spots.Length,["colliders"]=root.GetComponentsInChildren<BoxCollider>().Length,["prefab"]=AssetDatabase.GetAssetPath(prefab)};
        Object.DestroyImmediate(root);Object.DestroyImmediate(source);return prefab;
    }
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Invoke(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void ValidateBindings(GameObject[] prefabs,Scene preview)
    {
        var holder=new GameObject("BindingRegression");SceneManager.MoveGameObjectToScene(holder,preview);var controller=holder.AddComponent<BarnInteriorSceneController>();
        var layouts=prefabs.Select(p=>(GameObject)PrefabUtility.InstantiatePrefab(p,preview)).ToList();
        foreach(var l in layouts)l.transform.SetParent(holder.transform,false);
        controller.Configure(layouts,null,Array.Empty<Transform>());
        var homeObject=new GameObject("ValidationHome");SceneManager.MoveGameObjectToScene(homeObject,preview);var home=homeObject.AddComponent<AnimalHome>();
        for(int i=0;i<4;i++)
        {
            home.capacity=Capacity[i];Invoke(controller,"ApplyLayout",null,i+1);var binding=layouts[i].GetComponent<BarnInteriorLayout>();
            Require(layouts.Count(l=>l.activeSelf)==1,"Lv"+(i+1)+" exactly one active layout");
            Require(holder.GetComponentsInChildren<Transform>(true).All(t=>t.name!="TroughContainer"&&t.name!="FeedFill"),"Lv"+(i+1)+" no primitive trough duplication");
            typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,0);
            Invoke(controller,"RefreshFeedVisual",home);
            Require(binding.FeedFills.All(r=>!r.gameObject.activeSelf),"Lv"+(i+1)+" zero stock hides authored fills");
            var occupied=(HashSet<int>)typeof(BarnInteriorSceneController).GetField("occupiedFeedSlots",Flags).GetValue(controller);
            for(int k=0;k<Capacity[i];k++)
            {
                object[] args={binding.FeedingSlots[k].position,0f};var selected=(Transform)controller.GetType().GetMethod("ClosestFeedingSlot",Flags).Invoke(controller,args);
                int chosen=Array.IndexOf(binding.FeedingSlots,selected);
                Require(chosen>=0&&!occupied.Contains(chosen),"Lv"+(i+1)+" portion "+(k+1)+" reachable, including shared compartments");
                occupied.Add(chosen);typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,k+1);Invoke(controller,"RefreshFeedVisual",home);
            }
            Require(occupied.Count==Capacity[i]&&home.FeedSpace==0,"Lv"+(i+1)+" original storage capacity retained");
            Require(binding.FeedFills.All(r=>r.gameObject.activeSelf),"Lv"+(i+1)+" full stock displays every bound compartment");
            // Verify stock reduction and repeated refresh do not resurrect an empty portion.
            typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,1);Invoke(controller,"RefreshFeedVisual",home);Invoke(controller,"RefreshFeedVisual",home);
            Require(binding.FeedFills.Distinct().Count(r=>r.gameObject.activeSelf)==1,"Lv"+(i+1)+" consumption updates shared fill once");
        }
        Object.DestroyImmediate(homeObject);Object.DestroyImmediate(holder);
        // Coop uses the same controller with no binding component and must keep its legacy path.
        var coop=new GameObject("LegacyCoopRegression");SceneManager.MoveGameObjectToScene(coop,preview);var fallback=coop.AddComponent<BarnInteriorSceneController>();
        var layout=Child(coop.transform,"LegacyLayout");fallback.Configure(new List<GameObject>{layout.gameObject},null,Array.Empty<Transform>());Invoke(fallback,"ApplyLayout",null,1);
        Require(coop.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="TroughContainer")==4,"Coop legacy four-slot fallback preserved");Object.DestroyImmediate(coop);
    }
    static void Render(GameObject prefab,int i)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);foreach(var f in model.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Fill_")))f.gameObject.SetActive(true);
            var sun=new GameObject("Preview Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(50,-30,0);
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.orthographic=true;camera.orthographicSize=Width[i]*(i<2?.63f:.56f);camera.nearClipPlane=.05f;camera.farClipPlane=150;
            camera.transform.position=new Vector3(Width[i]*.52f,Width[i]*.95f,-Depth[i]*.72f);camera.transform.LookAt(new Vector3(0,.3f,Depth[i]*.52f));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.57f,.63f,.59f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;var img=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);img.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Output+"/Unity_Lv"+(i+1)+".png",img.EncodeToPNG());Object.DestroyImmediate(img);
        }
        finally{if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
    [MenuItem("Nature Paradise/Barn/Verify Approved Interior A and Show Prefab")]
    public static void VerifySaved()
    {
        log=new StringBuilder("BARN INTERIOR A SAVED ASSET VALIDATION\n");
        var prefabs=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/BarnInteriorLayout_Lv"+i+".prefab")).ToArray();
        Require(prefabs.All(p=>p!=null),"Four saved level prefabs exist");var preview=EditorSceneManager.NewPreviewScene();
        try{ValidateBindings(prefabs,preview);}finally{EditorSceneManager.ClosePreviewScene(preview);}
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BarnInteriorSceneController>(true)).Single();var root=controller.transform;
        var layouts=Enumerable.Range(1,4).Select(i=>root.Find("Layout_Lv"+i+"_Editable").gameObject).ToArray();var active=layouts.Select(l=>l.activeSelf).ToArray();
        var entry=root.Find("BarnInteriorEntrySpawn");
        var serialized=new SerializedObject(controller);var references=serialized.FindProperty("levelLayouts");
        Require(references.arraySize==4,"Controller stores four level references");
        for(int i=0;i<4;i++)
        {
            Require(references.GetArrayElementAtIndex(i).objectReferenceValue==layouts[i],"Lv"+(i+1)+" controller references saved prefab instance");
            for(int k=0;k<4;k++)layouts[k].SetActive(k==i);Physics.SyncTransforms();var binding=layouts[i].GetComponent<BarnInteriorLayout>();
            var colliders=layouts[i].GetComponentsInChildren<BoxCollider>();var floor=colliders.Single(c=>c.name=="Floor");
            Require(floor.bounds.Contains(new Vector3(entry.position.x,floor.bounds.center.y,entry.position.z)),"Lv"+(i+1)+" entry above authored floor");
            foreach(var spot in binding.AnimalSpots)
                Require(colliders.Where(c=>c!=floor).All(c=>!c.bounds.Contains(spot.position+Vector3.up*.5f)),"Lv"+(i+1)+" active animal marker clears colliders");
            foreach(var f in layouts[i].GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=f.sharedMesh;Require(mesh&&mesh.normals.Length==mesh.vertexCount&&mesh.uv.Length==mesh.vertexCount,"Lv"+(i+1)+" saved mesh UV and normals present: "+mesh.name);
                Require(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Lv"+(i+1)+" finite vertex positions: "+mesh.name);
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath(Models+"/BarnInterior_Lv"+(i+1)+".fbx");Require(!importer.isReadable,"Lv"+(i+1)+" source FBX CPU copy disabled");
        }
        for(int i=0;i<4;i++)layouts[i].SetActive(active[i]);
        Require(new SerializedObject(entry.GetComponent<PlayerSpawnPoint>()).FindProperty("spawnId").stringValue=="barn-interior-entry","Portal spawn ID retained");
        var exit=root.Find("BarnExitDoor_Editable");Require(exit.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled)&&exit.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Exit marker has no cube renderer or blocker");
        EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Validated default level 1 scene saved");
        // Leaving this scene additive alongside Map would start its controller
        // before entering a portal. Review the prefab in isolation instead.
        var map=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(map.isLoaded)SceneManager.SetActiveScene(map);
        EditorSceneManager.CloseScene(scene,true);
        EditorApplication.delayCall+=()=>PrefabStageUtility.OpenPrefab(Prefabs+"/BarnInteriorLayout_Lv1.prefab");
        File.WriteAllText(Output+"/SavedAssetValidation.txt",log+"\nBARN_INTERIOR_SAVED_VALIDATION_PASS\n");
        File.WriteAllText(Output+"/SavedAssetValidation.json",new JObject{["status"]="PASS",["levelsTested"]=4,["collidersTestedActive"]=true,["entryFloorTested"]=true,["feedCapacityTested"]=new JArray(Capacity),["coopFallbackTested"]=true,["mapSaved"]=false,["playModeTested"]=false,["review"]=Prefabs+"/BarnInteriorLayout_Lv1.prefab"}.ToString());
        Debug.Log("BARN_INTERIOR_SAVED_VALIDATION_PASS — prefab review isolated, Map remains available for Play.");
    }
    [MenuItem("Nature Paradise/Barn/Apply Approved Interior A")]
    public static void Apply()
    {
        log=new StringBuilder("BARN INTERIOR A UNITY APPLY\n");Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Editor is stopped");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs);
        string revisions="ArtSource/BarnInterior_A/Revisions/"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");Directory.CreateDirectory(revisions);
        File.Copy(ScenePath,revisions+"/BarnInterior.unity");for(int i=1;i<=4;i++)File.Copy(Prefabs+"/BarnInteriorLayout_Lv"+i+".prefab",revisions+"/BarnInteriorLayout_Lv"+i+".prefab");
        atlas=AssetDatabase.LoadAssetAtPath<Material>(AtlasPath);Require(atlas&&atlas.shader&&atlas.shader.isSupported,"Existing atlas material supported");
        var manifest=JObject.Parse(File.ReadAllText("ArtSource/BarnInterior_A/Previews/MeshManifest.json"));var prefabs=new GameObject[4];var results=new JArray();var preview=EditorSceneManager.NewPreviewScene();
        try{for(int i=0;i<4;i++){prefabs[i]=Build(i,preview,manifest["levels"][i],out var record);results.Add(record);}ValidateBindings(prefabs,preview);}
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        AssetDatabase.SaveAssets();for(int i=0;i<4;i++)Render(prefabs[i],i);
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        Require(!EditorSceneManager.IsPreviewScene(scene),"Real BarnInterior scene open");
        var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BarnInteriorSceneController>(true)).Single();var root=controller.transform;
        var layouts=new List<GameObject>();
        for(int i=0;i<4;i++)
        {
            string name="Layout_Lv"+(i+1)+"_Editable";var old=root.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
            var layout=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);layout.name=name;layout.transform.SetParent(root,false);layout.transform.localPosition=new Vector3(0,-200,-10);layout.SetActive(i==0);layouts.Add(layout);
        }
        foreach(string name in new[]{"AnimalSpots_Editable","FeedingTroughSlots_Editable"}){var old=root.Find(name);if(old){old.gameObject.SetActive(false);old.name="Legacy_"+name;}}
        var entry=root.Find("BarnInteriorEntrySpawn");Require(entry,"Existing entry spawn retained");entry.localPosition=new Vector3(0,-200,-10)+new Vector3(0,.15f,1.7f)*LayoutScale;entry.localRotation=Quaternion.identity;
        var exit=root.Find("BarnExitDoor_Editable");Require(exit,"Existing exit marker retained");exit.localPosition=new Vector3(0,-200,-10)+new Vector3(0,.7f,.35f)*LayoutScale;
        foreach(var r in exit.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var c in exit.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var camera=root.Find("BarnCameraAngle_Editable");Require(camera,"Existing camera marker retained");camera.localRotation=Quaternion.Euler(55,0,0);
        camera.localPosition=new Vector3(0,-200,-10)+new Vector3(0,5,2)*LayoutScale;
        controller.Configure(layouts,exit,Array.Empty<Transform>(),null,camera);EditorUtility.SetDirty(controller);
        var framing=new SerializedObject(controller);framing.FindProperty("cameraDistance").floatValue=18f;framing.ApplyModifiedPropertiesWithoutUndo();
        Physics.SyncTransforms();
        foreach(var layout in layouts)
        {
            var binding=layout.GetComponent<BarnInteriorLayout>();var colliders=layout.GetComponentsInChildren<BoxCollider>(true);
            foreach(var spot in binding.AnimalSpots)
                Require(colliders.Where(c=>c.name!="Floor").All(c=>!c.bounds.Contains(spot.position+Vector3.up*.5f)),layout.name+" animal spot clears solid geometry");
        }
        Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ScenePath),"BarnInterior registered in build settings");
        EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"BarnInterior saved with four connected prefabs");
        foreach(int i in Enumerable.Range(0,4)){var imp=(ModelImporter)AssetImporter.GetAtPath(Models+"/BarnInterior_Lv"+(i+1)+".fbx");imp.isReadable=false;imp.SaveAndReimport();}
        AssetDatabase.SaveAssets();Selection.activeGameObject=layouts[0];SceneManager.SetActiveScene(scene);
        var view=SceneView.lastActiveSceneView;if(view)view.LookAtDirect(new Vector3(0,-199,Depth[0]/2-10),Quaternion.Euler(52,0,0),12);
        File.WriteAllText(Output+"/UnityImportAudit.txt",log+"\nBARN_INTERIOR_APPLY_PASS\n");File.WriteAllText(Output+"/UnityApplyResult.json",new JObject{["status"]="PASS",["scene"]=ScenePath,["levels"]=results,["capacityChanged"]=false,["mapSceneChanged"]=false,["legacyCoopValidated"]=true,["backup"]=revisions}.ToString());
        Debug.Log("BARN_INTERIOR_APPLY_PASS — 4 stages, shared atlas, original capacity, no cube duplicates.");
    }
}
#endif
