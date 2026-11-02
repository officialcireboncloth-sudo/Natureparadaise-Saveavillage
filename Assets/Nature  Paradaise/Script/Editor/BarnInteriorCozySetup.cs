#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Replaces Barn Lv1 art, retaining slot identities, root scale, machine and animal anchors.</summary>
public static class BarnInteriorCozySetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Buildings/BarnInteriorCozy/";
    const string Prefab=Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv1.prefab";
    const string Output="ArtSource/BarnInteriorCozy/UnityReview";
    static StringBuilder report;
    static int checks;
    static Material[] materials;
    static bool Fixture=>Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);report.AppendLine("PASS "+message);checks++;}
    static Vector3 V(JToken j)=>new Vector3((float)j[0],(float)j[1],(float)j[2]);
    static Bounds BoundsOf(Mesh m,Matrix4x4 mat)
    {var b=new Bounds(mat.MultiplyPoint3x4(m.vertices[0]),Vector3.zero);foreach(var v in m.vertices)b.Encapsulate(mat.MultiplyPoint3x4(v));return b;}
    static Transform Child(Transform parent,string name)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;

    [MenuItem("Nature Paradise/Art/Apply Cozy Barn Interior Level 1")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Output);report=new StringBuilder("COZY BARN LV1 — NATIVE UNITY\n");checks=0;
        string scene=Root+"/Map/Scenes/Interiors/BarnInterior.unity",sceneBefore=File.ReadAllText(scene);
        var protectedFiles=Enumerable.Range(2,3).Select(i=>Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv"+i+".prefab")
            .Concat(new[]{Root+"/Resources/Buildings/Barn Building.asset"}).ToDictionary(p=>p,File.ReadAllText);
        materials=new[]{"NP_CozySurfaces_01","NP_CozyFoliage_01","NP_CozyGlow_01"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/"+n+".mat")).ToArray();
        Check(materials.All(m=>m!=null),"Existing shared atlas materials resolve");
        var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/BarnInteriorCozy/BarnInterior_Cozy_Lv1_Manifest.json"));
        var imported=new Dictionary<string,MeshFilter>();
        foreach(var entry in spec["modules"])
        {
            string path=Models+(string)entry["name"]+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;
            imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;imp.importCameras=false;imp.importLights=false;imp.addCollider=false;
            imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.meshCompression=ModelImporterMeshCompression.Off;imp.isReadable=true;
            imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var m in materials)imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),m);
            imp.SaveAndReimport();
            var ff=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true);
            Check(ff.Length==1,(string)entry["module"]+": one grouped mesh");imported[(string)entry["module"]]=ff.Single();
        }
        var options=Enumerable.Range(0,4).SelectMany(i=>new[]{1,-1}.Select(sign=>Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,i*90,0),new Vector3(sign,1,1))));
        var axis=options.OrderBy(m=>spec["modules"].Sum(j=>{var f=imported[(string)j["module"]];var b=BoundsOf(f.sharedMesh,m*f.transform.localToWorldMatrix);return Vector3.Distance(b.min,V(j["min"]))+Vector3.Distance(b.max,V(j["max"]));})).First();
        var root=PrefabUtility.LoadPrefabContents(Prefab);string guid=AssetDatabase.AssetPathToGUID(Prefab);
        try
        {
            var binding=root.GetComponent<BarnInteriorLayout>();
            var slots=binding.FeedingSlots.ToArray();var fills=binding.FeedFills.ToArray();
            var anchors=slots.Concat(binding.AnimalSpots).Concat(new[]{binding.FeedingRoot,binding.FeedMakerPoint,root.transform}).ToArray();
            var before=anchors.ToDictionary(t=>t,t=>(t.position,t.rotation,t.localScale));
            var ids=slots.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId).ToArray();
            Check(slots.Length==4&&fills.Distinct().Count()==4,"Lv1 has exactly four independent feed portions");
            var catalog=Resources.Load<BuildingDefinitionSO>("Buildings/Barn Building");Check(catalog.GetLevel(1).capacity==slots.Length,"Feed count matches building catalog");
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true).ToArray())
            {if(fills.Contains(f.GetComponent<MeshRenderer>()))continue;var r=f.GetComponent<MeshRenderer>();if(r)Object.DestroyImmediate(r);Object.DestroyImmediate(f);}
            foreach(var l in root.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l);
            foreach(var t in binding.FeedingRoot.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="CozyTroughGeometry").ToArray())Object.DestroyImmediate(t.gameObject);
            var previous=root.transform.Find("CozyVisuals");if(previous)Object.DestroyImmediate(previous.gameObject);
            var visuals=Child(root.transform,"CozyVisuals");
            // Old prop colliders must not remain invisible obstacles after the new set dressing is installed.
            var collision=root.transform.Find("Collision_Editable");
            foreach(var c in root.GetComponentsInChildren<BoxCollider>(true).ToArray())
                if(c.transform.parent==collision && !new[]{"Floor","SideWall","BackWall","FrontStub"}.Contains(c.name))Object.DestroyImmediate(c.gameObject);
            foreach(var c in root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name.StartsWith("Feed Sack Collision")).ToArray())Object.DestroyImmediate(c.gameObject);
            string assetPath=Models+"BarnInterior_Cozy_Lv1_Runtime.asset";bool exists=File.Exists(assetPath);
            var saved=AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>().ToDictionary(m=>m.name);
            MeshRenderer architecture=null;
            foreach(var entry in spec["modules"])
            {
                string module=(string)entry["module"];var input=imported[module];var matrix=axis*input.transform.localToWorldMatrix;
                var b=BoundsOf(input.sharedMesh,matrix);Check(Vector3.Distance(b.min,V(entry["min"]))+Vector3.Distance(b.max,V(entry["max"]))<.02f,module+": canonical metres and correct orientation");
                Check(input.sharedMesh.triangles.Length/3==(int)entry["triangles"],module+": native triangle count");
                Transform target;
                if(module.StartsWith("Fill"))
                {
                    var ix=module.Substring(4).Split('_');int row=int.Parse(ix[0])-1,cell=int.Parse(ix[1])-1;
                    int index=row+cell*2;target=fills[index].transform;
                }
                else if(module.StartsWith("Trough"))target=Child(slots[int.Parse(module.Substring(6))-1].parent,"CozyTroughGeometry");
                else target=Child(visuals,module);
                var local=target.worldToLocalMatrix*root.transform.localToWorldMatrix*matrix;var mesh=Object.Instantiate(input.sharedMesh);mesh.name="BarnInteriorCozy_"+module;
                mesh.vertices=mesh.vertices.Select(local.MultiplyPoint3x4).ToArray();mesh.normals=mesh.normals.Select(n=>local.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                mesh.tangents=mesh.tangents.Select(t=>{var d=local.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(d.x,d.y,d.z,t.w*Mathf.Sign(local.determinant));}).ToArray();
                if(local.determinant<0)for(int s=0;s<mesh.subMeshCount;s++){var tris=mesh.GetTriangles(s);for(int k=0;k<tris.Length;k+=3)(tris[k+1],tris[k+2])=(tris[k+2],tris[k+1]);mesh.SetTriangles(tris,s);}
                mesh.RecalculateBounds();Check(mesh.uv.Length==mesh.vertexCount&&mesh.uv.All(uv=>uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1),module+": padded shared atlas UVs");
                if(saved.TryGetValue(mesh.name,out var old)){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}
                else if(!exists){AssetDatabase.CreateAsset(mesh,assetPath);exists=true;}else AssetDatabase.AddObjectToAsset(mesh,assetPath);
                var mf=target.GetComponent<MeshFilter>();if(!mf)mf=target.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=mesh;
                var renderer=target.GetComponent<MeshRenderer>();if(!renderer)renderer=target.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials=input.GetComponent<MeshRenderer>().sharedMaterials;
                Check(renderer.sharedMaterials.All(m=>materials.Contains(m)),module+": existing atlas materials only");
                if(module=="Architecture")architecture=renderer;
            }
            var blockers=Child(visuals,"CozyPropColliders");
            foreach(var specCollider in spec["colliders"]){var c=Child(blockers,(string)specCollider["name"]).gameObject.AddComponent<BoxCollider>();c.center=V(specCollider["center"]);c.size=V(specCollider["size"]);}
            // A shared tiny cookie projects four panes without adding decorative floor meshes.
            string cookiePath=Models+"BarnWindowCookie.asset";var cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);
            if(!cookie){cookie=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="BarnWindowCookie",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};AssetDatabase.CreateAsset(cookie,cookiePath);}
            var pixels=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float edge=Mathf.Clamp01(Mathf.Min(Mathf.Min(x-5,58-x),Mathf.Min(y-5,58-y))/2f);float cross=Mathf.Clamp01((Mathf.Min(Mathf.Abs(x-31.5f),Mathf.Abs(y-31.5f))-1.6f)/1.2f);pixels[y*64+x]=new Color(edge*cross,edge*cross,edge*cross,edge*cross);}
            cookie.SetPixels(pixels);cookie.Apply();EditorUtility.SetDirty(cookie);
            var windowLights=new List<Light>();var warmLights=new List<Light>();
            foreach(int side in new[]{-1,1})
            {
                var t=Child(visuals,"WindowDaylight_"+side);t.localPosition=new Vector3(side*5.78f,2.1f,7);t.LookAt(root.transform.TransformPoint(new Vector3(side*2.8f,.02f,5.65f)));
                var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=13;l.spotAngle=55;l.innerSpotAngle=42;l.shadowCustomResolution=256;l.shadowBias=.015f;l.shadowNormalBias=.025f;l.cookie=cookie;windowLights.Add(l);
                var w=Child(visuals,"WarmLanternFill_"+side);w.localPosition=new Vector3(side*3.8f,2.6f,4.8f);l=w.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=10;l.color=new Color(1,.76f,.48f);l.shadows=LightShadows.None;warmLights.Add(l);
            }
            var lighting=visuals.gameObject.AddComponent<BarnInteriorCozyLighting>();lighting.Configure(windowLights.ToArray(),warmLights.ToArray(),architecture,Array.IndexOf(architecture.sharedMaterials,materials[2]));lighting.ApplyLighting(10,WeatherType.Sunny);
            foreach(var fill in fills)fill.gameObject.SetActive(false);
            Check(anchors.All(t=>t.position==before[t].position&&t.rotation==before[t].rotation&&t.localScale==before[t].localScale),"Feed, animal, machine anchors and root scale unchanged");
            Check(ids.SequenceEqual(slots.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId)),"Existing feed slot object IDs retained");
            Check(binding.FeedFills.SequenceEqual(fills),"Existing fill renderer references retained for save/consumption");
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);Check(AssetDatabase.AssetPathToGUID(Prefab)==guid,"Original prefab GUID retained");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceSynchronousImport);Check(protectedFiles.All(p=>File.ReadAllText(p.Key)==p.Value),"Levels 2–4 and capacity catalog unchanged");Check(File.ReadAllText(scene)==sceneBefore,"Barn scene left unchanged");
        if(Application.isBatchMode&&Fixture)ValidateAndRender();
        foreach(var j in spec["modules"]){var i=(ModelImporter)AssetImporter.GetAtPath(Models+(string)j["name"]+".fbx");i.isReadable=false;i.SaveAndReimport();}
        File.WriteAllText(Output+"/Audit.txt",report+$"CHECKS {checks}; BARN_COZY_APPLY_PASS\n");
    }

    static void ValidateAndRender()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var binding=root.GetComponent<BarnInteriorLayout>();Physics.SyncTransforms();
        foreach(var p in new[]{new Vector3(0,0,1.7f),new Vector3(0,0,4.5f),new Vector3(0,0,7),new Vector3(-3.6f,0,4.5f),new Vector3(3.6f,0,4.5f)}.Concat(binding.AnimalSpots.Select(t=>root.transform.InverseTransformPoint(t.position))))
        {var point=root.transform.TransformPoint(p);Check(!Physics.CheckCapsule(point+Vector3.up*.30f,point+Vector3.up*1.4f,.22f,~0,QueryTriggerInteraction.Ignore),"Player/animal capsule clear at "+p);}
        Check(binding.FeedingSlots.Length==4&&binding.FeedFills.Distinct().Count()==4,"Four separate physical portions; no decorative extra slots");
        Check(root.GetComponentsInChildren<MeshRenderer>(true).Length==9,"Nine grouped renderers; old artwork removed");
        Check(root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3)<34000,"Lv1 geometry stays below 34k triangles including all hay portions");
        // Use the actual existing controller to test fill visibility and all-sided access.
        var holder=new GameObject("Controller regression");var controller=holder.AddComponent<BarnInteriorSceneController>();root.transform.SetParent(holder.transform,true);controller.Configure(new List<GameObject>{root},null,binding.AnimalSpots);
        var homeObject=new GameObject("Validation home");var home=homeObject.AddComponent<AnimalHome>();home.capacity=4;
        typeof(BarnInteriorSceneController).GetMethod("ApplyLayout",Flags).Invoke(controller,new object[]{home,1});
        for(int count=0;count<=4;count++)
        {
            typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,count);
            typeof(BarnInteriorSceneController).GetMethod("RefreshFeedVisual",Flags).Invoke(controller,new object[]{home});
            Check(binding.FeedFills.Count(r=>r.gameObject.activeSelf)==count,"Stock "+count+" shows exactly "+count+" portions");
        }
        typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,1);typeof(BarnInteriorSceneController).GetMethod("RefreshFeedVisual",Flags).Invoke(controller,new object[]{home});
        Check(binding.FeedFills.Count(r=>r.gameObject.activeSelf)==1,"Consumption hides only emptied portions");
        foreach(var z in new[]{3.1f,5.9f})foreach(var x in new[]{-2.3f,2.3f})
        {object[] args={root.transform.TransformPoint(new Vector3(x,0,z)),0f};var selected=typeof(BarnInteriorSceneController).GetMethod("ClosestTroughAccess",Flags).Invoke(controller,args);Check(selected!=null&&(float)args[1]<1.0f,"Front/back feeding access at "+x+","+z);}
        foreach(var x in new[]{-3.3f,-1.3f,1.3f,3.3f})
        {object[] args={root.transform.TransformPoint(new Vector3(x,0,4.5f)),0f};var selected=typeof(BarnInteriorSceneController).GetMethod("ClosestTroughAccess",Flags).Invoke(controller,args);Check(selected!=null&&(float)args[1]<1.0f,"Side feeding access at "+x);}
        foreach(var m in materials)Check(!ShaderUtil.ShaderHasError(m.shader),"Atlas shader compiles: "+m.name);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.72f,.80f,.87f);RenderSettings.ambientEquatorColor=new Color(.48f,.50f,.48f);RenderSettings.ambientGroundColor=new Color(.28f,.25f,.20f);RenderSettings.fog=false;
        var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear*.8f);probe.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor.linear,.45f);RenderSettings.ambientProbe=probe;
        var sun=new GameObject("Review soft fill").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.62f;sun.color=new Color(1,.96f,.87f);sun.transform.rotation=Quaternion.Euler(52,25,0);sun.shadows=LightShadows.Soft;
        var camera=new GameObject("Barn review camera").AddComponent<Camera>();camera.fieldOfView=43;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.78f,.77f,.69f);
        camera.transform.position=new Vector3(0,22,-13);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.8f,5)));
        ShaderUtil.allowAsyncCompilation=false;
        foreach(var r in binding.FeedFills)r.gameObject.SetActive(false);Capture(camera,"Barn_Lv1_Empty");
        foreach(var r in binding.FeedFills)r.gameObject.SetActive(true);Capture(camera,"Barn_Lv1_Full");
        camera.transform.position=new Vector3(-14,17,-10);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.7f,5)));Capture(camera,"Barn_Lv1_Detail");
        var lighting=root.GetComponentInChildren<BarnInteriorCozyLighting>();var window=root.GetComponentsInChildren<Light>().First(l=>l.name.StartsWith("Window"));lighting.ApplyLighting(10,WeatherType.Sunny);float clear=window.intensity;
        lighting.ApplyLighting(10,WeatherType.HeavyRain);Check(window.intensity<clear*.2f,"Interior window daylight responds to heavy rain");
        lighting.ApplyLighting(22,WeatherType.Sunny);Check(window.intensity<1,"Night switches to low moonlight");sun.intensity=.15f;Capture(camera,"Barn_Lv1_Night");
        Object.DestroyImmediate(homeObject);Object.DestroyImmediate(holder);
        var path=Root+"/Map/Scenes/Interiors/BarnInterior.unity";var before=File.ReadAllText(path);EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var layouts=Object.FindObjectsByType<BarnInteriorLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        File.WriteAllText(Output+"/SceneBindings.txt",string.Join("\n",layouts.Select(l=>l.name+" | "+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l.gameObject)+" | "+string.Join(",",l.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh?f.sharedMesh.name:"NULL")))));
        var layout=layouts.First(l=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l.gameObject)==Prefab);
        Check(layout.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&f.sharedMesh.name=="BarnInteriorCozy_Architecture"),"Actual Barn scene resolves new Lv1 art");
        Check(Object.FindObjectsByType<BarnInteriorLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==4,"All four upgrade layouts still bound");
        Check(File.ReadAllText(path)==before,"Actual scene review did not rewrite the scene");
    }
    static void Capture(Camera camera,string name)
    {
        const int w=1200,h=1050;camera.aspect=(float)w/h;var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if(!Fixture)throw new InvalidOperationException("Use isolated review fixture.");
        try{Apply();if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");EditorApplication.Exit(0);}
        catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",report+"\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
