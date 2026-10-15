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

/// <summary>Imports the approved Coop kit and binds the existing interior controller.</summary>
[InitializeOnLoad]
public static class CoopApprovedInteriorSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Buildings/CoopInterior_A";
    const string Prefabs=Root+"/Prefabs/Coop/Interior";
    const string ScenePath=Root+"/Map/Scenes/Interiors/CoopInterior.unity";
    const string Definition=Root+"/Resources/Buildings/Coop Building.asset";
    const string Output="ArtSource/CoopInterior_A/Unity";
    const string Request="ArtSource/CoopInterior_A/apply-unity.request";
    const string VerifyRequest="ArtSource/CoopInterior_A/verify-unity.request";
    const string PackMaterials=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials";
    static readonly int[] Capacity={6,9,12,15};
    static readonly float[] Width={10,12,14,16},Depth={8,10,12,14};
    static readonly Vector3 Offset=new Vector3(0,-200,-10);
    static bool busy;static double next;static Material atlas,glass;static StringBuilder log;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static CoopApprovedInteriorSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        if((!File.Exists(Request)&&!File.Exists(VerifyRequest))||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try
        {
            Directory.CreateDirectory(Output);bool apply=File.Exists(Request);string path=apply?Request:VerifyRequest;
            File.Move(path,Output+"/"+(apply?"Apply_":"Verify_")+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
            if(apply)Apply();else VerifySaved();
        }
        catch(Exception e){File.WriteAllText(Output+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Require(bool ok,string message)
    {if(!ok)throw new InvalidOperationException("Coop interior: "+message);log.AppendLine("PASS "+message);}
    static Transform Child(Transform parent,string name,Vector3 position=default)
    {var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=position;return o.transform;}
    static void Box(Transform parent,string name,Vector3 centre,Vector3 size)
    {var c=Child(parent,name).gameObject.AddComponent<BoxCollider>();c.center=centre;c.size=size;}
    static Bounds FilterBounds(MeshFilter f,Transform relative=null)
    {
        Matrix4x4 m=(relative?relative.worldToLocalMatrix:Matrix4x4.identity)*f.transform.localToWorldMatrix;
        var b=new Bounds(m.MultiplyPoint3x4(f.sharedMesh.vertices[0]),Vector3.zero);
        foreach(var v in f.sharedMesh.vertices)b.Encapsulate(m.MultiplyPoint3x4(v));return b;
    }
    static Bounds BoundsOf(GameObject o)
    {var fs=o.GetComponentsInChildren<MeshFilter>(true);var b=FilterBounds(fs[0]);foreach(var f in fs.Skip(1))b.Encapsulate(FilterBounds(f));return b;}
    static string Key(MeshFilter f)
    {
        string n=f.name;
        if(Regex.IsMatch(n,"^Trough_\\d+"))return Regex.Match(n,"^Trough_\\d+").Value;
        if(Regex.IsMatch(n,"^Fill_\\d+_\\d+"))return Regex.Match(n,"^Fill_\\d+_\\d+").Value;
        if(n.Contains("_Hood"))return "GlassHood";
        if(n.StartsWith("Floor"))return "Floor";
        if(n.StartsWith("N01_")||n.StartsWith("FrontCutaway"))return "Shell";
        if(n.StartsWith("C02_"))return "FeedMaker";
        if(n.StartsWith("C03_")||n.StartsWith("IncubatorEgg_"))return "Incubator";
        if(n.StartsWith("C04_")||n.StartsWith("R_MayoJar")||n.StartsWith("R_RawEgg"))return "Mayonnaise";
        if(n.StartsWith("N05_")||n.StartsWith("C05_")||n.StartsWith("AutoHead_")||n.StartsWith("ConnectedPipe"))return "Distribution";
        return "PropsAndStraw";
    }
    static Mesh Combine(IEnumerable<MeshFilter> fs,Transform frame,string name)
    {
        var me=new Mesh{name=name,indexFormat=IndexFormat.UInt16};
        me.CombineMeshes(fs.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=frame.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
        me.RecalculateBounds();MeshUtility.Optimize(me);return me;
    }
    static void Materials()
    {
        atlas=AssetDatabase.LoadAssetAtPath<Material>(PackMaterials+"/TFP_Atlas_1A.mat");
        var existing=AssetDatabase.LoadAssetAtPath<Material>(PackMaterials+"/TFP_Glass_Windows_1B.mat");
        Require(atlas&&atlas.shader&&atlas.shader.isSupported,"Existing atlas shader supported");
        Require(existing&&existing.shader&&existing.shader.isSupported,"Existing transparent shader supported");
        string path=Models+"/CoopIncubatorGlass.mat";glass=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!glass){glass=new Material(existing);AssetDatabase.CreateAsset(glass,path);}else glass.CopyPropertiesFromMaterial(existing);
        glass.name="CoopIncubatorGlass";
        // The window bitmap contains window art; the curved hood needs a clean tint.
        foreach(string p in new[]{"_BaseMap","_MainTex"})if(glass.HasProperty(p))glass.SetTexture(p,null);
        foreach(string p in new[]{"_BaseColor","_Color"})if(glass.HasProperty(p))glass.SetColor(p,new Color(.80f,.91f,.93f,.19f));
        if(glass.HasProperty("_Cull"))glass.SetFloat("_Cull",2);
        if(glass.HasProperty("_Smoothness"))glass.SetFloat("_Smoothness",.2f);
        glass.renderQueue=3000;EditorUtility.SetDirty(glass);
        Require(glass.GetFloat("_Surface")==1&&glass.GetFloat("_ZWrite")==0,"Hood transparent with depth writes off");
    }
    static GameObject Build(int i,Scene preview,JToken spec,out JObject report)
    {
        int level=i+1;string stem="CoopInterior_Lv"+level,path=Models+"/"+stem+".fbx";
        File.Copy("ArtSource/CoopInterior_A/Exports/"+stem+".fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;
        imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;imp.importCameras=false;imp.importLights=false;
        imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Import;
        imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TFP_Atlas_1A"),atlas);
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Coop_Hood_Glass_ExistingPalette"),glass);imp.SaveAndReimport();
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(imported,"Imported "+stem);
        var source=(GameObject)PrefabUtility.InstantiatePrefab(imported,preview);
        var expected=new Bounds(new Vector3(0,1.61f,Depth[i]/2),new Vector3(Width[i]+.27f,3.58f,Depth[i]+.23f));
        Quaternion original=source.transform.rotation,chosen=original;float best=float.PositiveInfinity;
        foreach(int x in new[]{0,90,180,270})foreach(int y in new[]{0,90,180,270})foreach(int z in new[]{0,90,180,270})
        {source.transform.rotation=Quaternion.Euler(x,y,z)*original;var b=BoundsOf(source);float e=Vector3.Distance(b.min,expected.min)+Vector3.Distance(b.max,expected.max);if(e<best){best=e;chosen=source.transform.rotation;}}
        source.transform.rotation=chosen;Require(best<.25f,"Lv"+level+" upright metre bounds and entrance origin, error "+best);
        var filters=source.GetComponentsInChildren<MeshFilter>(true);var nodes=source.GetComponentsInChildren<Transform>(true);
        Require(filters.Sum(f=>f.sharedMesh.triangles.Length/3)==(int)spec["triangles"],"Lv"+level+" imported triangles match source");
        Require(filters.Count(f=>f.name.StartsWith("Trough_"))==level+1,"Lv"+level+" correct trough count");
        Require(filters.Count(f=>f.name.StartsWith("AutoHead_"))==(level>=3?level+1:0),"Lv"+level+" correct automatic head count");
        Require(filters.Count(f=>f.name.StartsWith("IncubatorEgg_"))==(level==4?3:1),"Lv"+level+" correct incubator egg count");
        var root=new GameObject("Layout_Lv"+level+"_Editable");SceneManager.MoveGameObjectToScene(root,preview);
        var visual=Child(root.transform,"Visuals_Optimized");var troughRoot=Child(root.transform,"FeedingTroughs_Editable");
        var collision=Child(root.transform,"Collision_Editable");var sockets=Child(root.transform,"Sockets_Editable");var animals=Child(root.transform,"AnimalSpots_Editable");
        string meshPath=Models+"/"+stem+"_Runtime.asset";if(AssetDatabase.LoadMainAssetAtPath(meshPath))AssetDatabase.DeleteAsset(meshPath);
        bool first=true;var troughs=new Dictionary<int,Transform>();var fills=new SortedDictionary<int,Renderer>();
        foreach(var group in filters.GroupBy(Key).OrderBy(g=>g.Key))
        {
            string key=group.Key;Transform frame=visual;
            if(key.StartsWith("Trough_"))
            {int number=int.Parse(key.Split('_')[1]);var b=FilterBounds(group.First(),root.transform);frame=Child(troughRoot,key,new Vector3(b.center.x,0,b.center.z));troughs[number]=frame;}
            if(key.StartsWith("Fill_"))continue;
            var go=Child(frame,key+"_Geometry");var me=Combine(group,go,stem+"_"+key);
            if(first){AssetDatabase.CreateAsset(me,meshPath);first=false;}else AssetDatabase.AddObjectToAsset(me,meshPath);
            go.gameObject.AddComponent<MeshFilter>().sharedMesh=me;var r=go.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=key=="GlassHood"?glass:atlas;
            if(key=="GlassHood"){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        }
        foreach(var group in filters.GroupBy(Key).Where(g=>g.Key.StartsWith("Fill_")))
        {
            string key=group.Key;var parts=key.Split('_');int t=int.Parse(parts[1]),slot=int.Parse(parts[2]);
            var b=FilterBounds(group.First(),root.transform);var go=Child(troughs[t],key);go.position=root.transform.TransformPoint(new Vector3(b.center.x,0,b.center.z));
            var me=Combine(group,go,stem+"_"+key);AssetDatabase.AddObjectToAsset(me,meshPath);go.gameObject.AddComponent<MeshFilter>().sharedMesh=me;
            var r=go.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=atlas;fills[(t-1)*3+slot-1]=r;
        }
        Require(fills.Count==Capacity[i],"Lv"+level+" one feed fill per requested capacity");
        var slots=new Transform[Capacity[i]];var fillRefs=new Renderer[Capacity[i]];
        for(int k=0;k<slots.Length;k++)
        {
            int t=k/3+1,slot=k%3+1;string socket=$"FeedSocket_{t:00}_{slot}";
            var n=nodes.Single(p=>p.name.StartsWith(socket));var point=Child(troughs[t],"FeedPortion_"+(k+1).ToString("00"));point.position=n.position;
            slots[k]=point;fillRefs[k]=fills[k];
        }
        var spots=nodes.Where(t=>t.name.StartsWith("AnimalSlot_")).OrderBy(t=>t.name,StringComparer.Ordinal).Select(t=>
        {var p=Child(animals,t.name);p.position=t.position+Vector3.up*.05f;return p;}).ToArray();
        Require(spots.Length==Capacity[i],"Lv"+level+" authored animal marker count");
        var machine=filters.Single(f=>f.name.StartsWith("C02_"));var maker=Child(sockets,"FeedMaker_Position");maker.position=machine.transform.position;
        Child(sockets,"Entrance");Child(sockets,"PlayerSpawn",new Vector3(0,.15f,1.4f));
        foreach(var t in troughs.Values)Box(t,"TroughCollider",new Vector3(0,.15f,0),new Vector3(1.5f,.3f,.5f));
        Box(collision,"Floor",new Vector3(0,-.09f,Depth[i]/2),new Vector3(Width[i],.18f,Depth[i]));
        Box(collision,"BackWall",new Vector3(0,1.7f,Depth[i]+.06f),new Vector3(Width[i],3.4f,.14f));
        foreach(int side in new[]{-1,1})
        {Box(collision,"SideWall",new Vector3(side*(Width[i]/2+.06f),1.7f,Depth[i]/2),new Vector3(.14f,3.4f,Depth[i]));Box(collision,"FrontStub",new Vector3(side*(Width[i]/4+.5f),.3f,0),new Vector3(Width[i]/2-1,.6f,.18f));}
        foreach(var f in filters.Where(f=>f.name.StartsWith("C02_")||f.name.StartsWith("C_Table_")||f.name.StartsWith("C06_")||f.name.StartsWith("R_Sack")||f.name.StartsWith("R_Crate")))
        {var b=FilterBounds(f,root.transform);Box(collision,f.name+"_Collider",b.center,b.size);}
        root.AddComponent<BarnInteriorLayout>().EditorConfigure(troughRoot,slots,fillRefs,spots,maker);
        var runtime=root.GetComponentsInChildren<MeshFilter>(true);Require(runtime.Sum(f=>f.sharedMesh.triangles.Length/3)==(int)spec["triangles"],"Lv"+level+" optimized geometry preserves every triangle");
        Require(runtime.Length<=34,"Lv"+level+" renderer count reduced: "+filters.Length+" -> "+runtime.Length);
        foreach(var r in fills.Values)r.gameObject.SetActive(false);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/CoopInteriorLayout_Lv"+level+".prefab");
        report=new JObject{["level"]=level,["capacity"]=Capacity[i],["triangles"]=runtime.Sum(f=>f.sharedMesh.triangles.Length/3),["vertices"]=runtime.Sum(f=>f.sharedMesh.vertexCount),["renderers"]=runtime.Length,["originalRenderers"]=filters.Length,["prefab"]=AssetDatabase.GetAssetPath(prefab),["axisError"]=best};
        Object.DestroyImmediate(root);Object.DestroyImmediate(source);return prefab;
    }
    static void Invoke(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void ValidateBindings(GameObject[] prefabs,Scene preview)
    {
        var holder=new GameObject("CoopBindingRegression");SceneManager.MoveGameObjectToScene(holder,preview);var c=holder.AddComponent<BarnInteriorSceneController>();
        var layouts=prefabs.Select(p=>(GameObject)PrefabUtility.InstantiatePrefab(p,preview)).ToList();foreach(var l in layouts)l.transform.SetParent(holder.transform,false);c.Configure(layouts,null,Array.Empty<Transform>());
        var ho=new GameObject("ValidationHome");SceneManager.MoveGameObjectToScene(ho,preview);var home=ho.AddComponent<AnimalHome>();
        for(int i=0;i<4;i++)
        {
            var binding=layouts[i].GetComponent<BarnInteriorLayout>();
            Invoke(c,"ApplyLayout",null,i+1);
            Require(binding.FeedingSlots.All(t=>t.gameObject.activeSelf)&&binding.AnimalSpots.All(t=>t.gameObject.activeSelf),"Lv"+(i+1)+" standalone preview uses authored Coop capacity");
            home.capacity=Capacity[i];Invoke(c,"ApplyLayout",home,i+1);
            Require(layouts.Count(l=>l.activeSelf)==1,"Lv"+(i+1)+" only one active layout");
            Require(holder.GetComponentsInChildren<Transform>(true).All(t=>t.name!="TroughContainer"&&t.name!="FeedFill"),"Lv"+(i+1)+" no generated primitive trough duplicates");
            var stock=typeof(AnimalHome).GetField("fodderStock",Flags);stock.SetValue(home,0);Invoke(c,"RefreshFeedVisual",home);
            Require(binding.FeedFills.All(r=>!r.gameObject.activeSelf),"Lv"+(i+1)+" empty feed hides fills");
            var occupied=(HashSet<int>)typeof(BarnInteriorSceneController).GetField("occupiedFeedSlots",Flags).GetValue(c);
            for(int k=0;k<Capacity[i];k++)
            {object[] args={binding.FeedingSlots[k].position,0f};var picked=(Transform)c.GetType().GetMethod("ClosestFeedingSlot",Flags).Invoke(c,args);Require(picked==binding.FeedingSlots[k],"Lv"+(i+1)+" feeding portion "+(k+1)+" reachable");occupied.Add(k);stock.SetValue(home,k+1);Invoke(c,"RefreshFeedVisual",home);}
            Require(home.FeedSpace==0&&binding.FeedFills.All(r=>r.gameObject.activeSelf),"Lv"+(i+1)+" full stock displays all portions");
            stock.SetValue(home,1);Invoke(c,"RefreshFeedVisual",home);Invoke(c,"RefreshFeedVisual",home);Require(binding.FeedFills.Count(r=>r.gameObject.activeSelf)==1,"Lv"+(i+1)+" stock reduction hides consumed portions");
            Physics.SyncTransforms();var cols=layouts[i].GetComponentsInChildren<BoxCollider>();
            foreach(var spot in binding.AnimalSpots)Require(cols.Where(x=>x.name!="Floor").All(x=>!x.bounds.Contains(spot.position+Vector3.up*.35f)),"Lv"+(i+1)+" animal marker clears collision: "+spot.name);
            foreach(var f in layouts[i].GetComponentsInChildren<MeshFilter>(true))
            {var m=f.sharedMesh;Require(m.normals.Length==m.vertexCount&&m.uv.Length==m.vertexCount&&m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Lv"+(i+1)+" saved mesh finite, UV and normals: "+m.name);}
        }
        Object.DestroyImmediate(ho);Object.DestroyImmediate(holder);
    }
    static void ValidateBarnCompatibility(Scene preview)
    {
        var holder=new GameObject("BarnCompatibility");SceneManager.MoveGameObjectToScene(holder,preview);
        var controller=holder.AddComponent<BarnInteriorSceneController>();var layouts=new List<GameObject>();
        for(int i=1;i<=4;i++)
        {var p=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv"+i+".prefab");Require(p,"Existing Barn prefab "+i);var l=(GameObject)PrefabUtility.InstantiatePrefab(p,preview);l.transform.SetParent(holder.transform,false);layouts.Add(l);}
        controller.Configure(layouts,null,Array.Empty<Transform>());
        for(int i=0;i<4;i++)
        {Invoke(controller,"ApplyLayout",null,i+1);var b=layouts[i].GetComponent<BarnInteriorLayout>();Require(b.FeedingSlots.Length==new[]{4,8,14,20}[i]&&b.FeedingSlots.All(t=>t.gameObject.activeSelf),"Barn authored capacity unchanged "+(i+1));}
        Object.DestroyImmediate(holder);
        holder=new GameObject("LegacyInteriorCompatibility");SceneManager.MoveGameObjectToScene(holder,preview);controller=holder.AddComponent<BarnInteriorSceneController>();var legacy=Child(holder.transform,"LegacyLayout");
        controller.Configure(new List<GameObject>{legacy.gameObject},null,Array.Empty<Transform>());Invoke(controller,"ApplyLayout",null,1);
        Require(holder.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="TroughContainer")==4,"Unbound legacy interior fallback retained");Object.DestroyImmediate(holder);
    }
    static void Render(GameObject prefab,int i,bool detail=false)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);foreach(var r in model.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Fill_")))r.gameObject.SetActive(true);
            var sun=new GameObject("Preview Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(50,-30,0);
            var cam=new GameObject("Preview Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,scene);cam.scene=scene;cam.orthographic=true;cam.orthographicSize=detail?2.1f:Width[i]*.66f;cam.nearClipPlane=.05f;cam.farClipPlane=100;
            cam.transform.position=detail?new Vector3(Width[i]/2-3,4.8f,Depth[i]-6):new Vector3(Width[i]*.12f,Width[i]*1.12f,-Depth[i]*.82f);
            cam.transform.LookAt(detail?new Vector3(Width[i]/2-2.3f,1.3f,Depth[i]-1.05f):new Vector3(0,.35f,Depth[i]*.5f));
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.57f,.63f,.59f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Output+"/Unity_Lv"+(i+1)+(detail?"_HoodDetail":"")+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void UpdateCapacity()
    {
        var d=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Definition);Require(d&&d.levels.Count==4,"Existing Coop definition has four levels");
        for(int i=0;i<4;i++)d.GetLevel(i+1).capacity=Capacity[i];EditorUtility.SetDirty(d);
        string path=Root+"/Data/Balance/Settings/Buildings.csv";string text=File.ReadAllText(path);
        for(int i=0;i<4;i++)
        {string pattern=@"(?m)^(?<head>[^\r\n]*,Coop Building,[^\r\n]*,levels\.Array\.data\["+i+@"\]\.capacity,Capacity,)\d+(?<tail>,[^\r\n]*)(?=\r?$)";Require(Regex.Matches(text,pattern).Count==1,"Balance CSV one Coop capacity row "+(i+1));text=Regex.Replace(text,pattern,m=>m.Groups["head"].Value+Capacity[i]+m.Groups["tail"].Value);}
        File.WriteAllText(path,text);AssetDatabase.ImportAsset(path);
    }
    [MenuItem("Nature Paradise/Coop/Apply Approved Interior A")]
    public static void Apply()
    {
        log=new StringBuilder("COOP INTERIOR A UNITY APPLY\n");Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs);
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Play Mode stopped");
        string backup="ArtSource/CoopInterior_A/Revisions/"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");Directory.CreateDirectory(backup);
        File.Copy(ScenePath,backup+"/CoopInterior.unity");File.Copy(Definition,backup+"/Coop Building.asset");File.Copy(Root+"/Data/Balance/Settings/Buildings.csv",backup+"/Buildings.csv");
        for(int i=1;i<=4;i++)File.Copy(Prefabs+"/CoopInteriorLayout_Lv"+i+".prefab",backup+"/CoopInteriorLayout_Lv"+i+".prefab");
        Materials();var manifest=JObject.Parse(File.ReadAllText("ArtSource/CoopInterior_A/Previews/MeshManifest.json"));var prefabs=new GameObject[4];var results=new JArray();var preview=EditorSceneManager.NewPreviewScene();
        try{for(int i=0;i<4;i++){prefabs[i]=Build(i,preview,manifest["levels"][i],out var rec);results.Add(rec);}ValidateBindings(prefabs,preview);}finally{EditorSceneManager.ClosePreviewScene(preview);}
        UpdateCapacity();AssetDatabase.SaveAssets();for(int i=0;i<4;i++)Render(prefabs[i],i);Render(prefabs[3],3,true);
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        Require(!scene.isDirty,"Coop scene has no unsaved user edits before apply");
        var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BarnInteriorSceneController>(true)).Single();var root=controller.transform;
        var layouts=new List<GameObject>();
        for(int i=0;i<4;i++)
        {string name="Layout_Lv"+(i+1)+"_Editable";var old=root.Find(name);if(old)Object.DestroyImmediate(old.gameObject);var l=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);l.name=name;l.transform.SetParent(root,false);l.transform.localPosition=Offset;l.SetActive(i==0);layouts.Add(l);}
        foreach(string name in new[]{"AnimalSpots_Editable","FeedingTroughSlots_Editable"}){var old=root.Find(name);if(old){old.gameObject.SetActive(false);old.name="Legacy_"+name;}}
        var entry=root.Find("CoopInteriorEntrySpawn");Require(entry,"Existing Coop entry spawn retained");entry.localPosition=Offset+new Vector3(0,.15f,1.4f);entry.localRotation=Quaternion.identity;
        var exit=root.Find("CoopExitDoor_Editable");Require(exit,"Existing Coop exit retained");exit.localPosition=Offset+new Vector3(0,.7f,.35f);
        foreach(var r in exit.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var c in exit.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var camera=root.Find("CoopCameraAngle_Editable");Require(camera,"Existing camera marker retained");camera.localRotation=Quaternion.Euler(55,0,0);camera.localPosition=Offset+new Vector3(0,5,2);
        controller.Configure(layouts,exit,Array.Empty<Transform>(),null,camera);EditorUtility.SetDirty(controller);
        Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ScenePath),"CoopInterior registered in build settings");
        EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Coop scene saved with four prefab instances");
        for(int i=0;i<4;i++){var imp=(ModelImporter)AssetImporter.GetAtPath(Models+"/CoopInterior_Lv"+(i+1)+".fbx");imp.isReadable=false;imp.SaveAndReimport();}
        AssetDatabase.SaveAssets();File.WriteAllText(Output+"/UnityImportAudit.txt",log.ToString());
        File.WriteAllText(Output+"/UnityApplyResult.json",new JObject{["status"]="PASS",["levels"]=results,["capacities"]=new JArray(Capacity),["backup"]=backup,["scene"]=ScenePath,["mapSaved"]=false,["playModeTested"]=false}.ToString());
        FinishReview(scene);Debug.Log("COOP_INTERIOR_APPLY_PASS — four levels, original atlas, local glass, authored feeds.");
    }
    static void FinishReview(Scene scene)
    {
        var map=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(map.isLoaded)SceneManager.SetActiveScene(map);
        EditorSceneManager.CloseScene(scene,true);
        EditorApplication.delayCall+=()=>
        {var stage=PrefabStageUtility.OpenPrefab(Prefabs+"/CoopInteriorLayout_Lv4.prefab");var v=SceneView.lastActiveSceneView;if(v){v.sceneLighting=false;v.LookAtDirect(new Vector3(0,1,Depth[3]/2),Quaternion.Euler(55,0,0),12);}};
    }
    [MenuItem("Nature Paradise/Coop/Verify Approved Interior A")]
    public static void VerifySaved()
    {
        log=new StringBuilder("COOP INTERIOR SAVED VALIDATION\n");var prefabs=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/CoopInteriorLayout_Lv"+i+".prefab")).ToArray();Require(prefabs.All(p=>p),"Four saved prefabs exist");
        var preview=EditorSceneManager.NewPreviewScene();try{ValidateBindings(prefabs,preview);ValidateBarnCompatibility(preview);}finally{EditorSceneManager.ClosePreviewScene(preview);}
        var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Definition);
        for(int i=0;i<4;i++)
        {Require(definition.GetLevel(i+1).capacity==Capacity[i],"Gameplay capacity "+Capacity[i]);var importer=(ModelImporter)AssetImporter.GetAtPath(Models+"/CoopInterior_Lv"+(i+1)+".fbx");Require(!importer.isReadable,"Source FBX CPU copy disabled "+(i+1));}
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BarnInteriorSceneController>(true)).Single();var root=controller.transform;
        var entry=root.Find("CoopInteriorEntrySpawn");Require(new SerializedObject(entry.GetComponent<PlayerSpawnPoint>()).FindProperty("spawnId").stringValue=="coop-interior-entry","Portal spawn ID retained");
        var so=new SerializedObject(controller);var refs=so.FindProperty("levelLayouts");Require(refs.arraySize==4,"Controller level references saved");
        for(int i=0;i<4;i++)
        {var l=root.Find("Layout_Lv"+(i+1)+"_Editable");Require(refs.GetArrayElementAtIndex(i).objectReferenceValue==l.gameObject,"Controller bound to prefab instance "+(i+1));Require(l.GetComponent<BarnInteriorLayout>().AnimalSpots.Length==Capacity[i],"Saved scene animal slots "+(i+1));var floor=l.GetComponentsInChildren<BoxCollider>(true).Single(c=>c.name=="Floor");var e=l.InverseTransformPoint(entry.position);Require(Mathf.Abs(e.x)<Width[i]/2&&e.z>0&&e.z<Depth[i]&&Mathf.Abs(e.y-.15f)<.01f,"Entry above authored floor "+(i+1));}
        File.WriteAllText(Output+"/SavedAssetValidation.txt",log+"\nCOOP_INTERIOR_SAVED_VALIDATION_PASS\n");File.WriteAllText(Output+"/SavedAssetValidation.json",new JObject{["status"]="PASS",["levelsTested"]=4,["capacities"]=new JArray(Capacity),["feedBindingsTested"]=true,["collidersTestedActive"]=true,["playModeTested"]=false}.ToString());
        FinishReview(scene);Debug.Log("COOP_INTERIOR_SAVED_VALIDATION_PASS");
    }
}
#endif
