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

/// <summary>Player Coop Lv1 art, continuous six-cell feeder and original single-egg incubator bindings.</summary>
public static class CoopInteriorCozySetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Buildings/CoopInteriorCozy/";
    const string Prefab=Root+"/Prefabs/Coop/Interior/CoopInteriorLayout_Lv1.prefab";
    const string Output="ArtSource/CoopInteriorCozy/UnityReview";
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

    [MenuItem("Nature Paradise/Art/Apply Cozy Coop Interior Level 1")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Output);if(File.Exists(Output+"/Audit.txt"))File.Delete(Output+"/Audit.txt");report=new StringBuilder("COZY COOP LV1 — NATIVE UNITY\n");checks=0;
        string scene=Root+"/Map/Scenes/Interiors/CoopInterior.unity",sceneBefore=File.ReadAllText(scene);
        var protectedFiles=Enumerable.Range(2,3).Select(i=>Root+"/Prefabs/Coop/Interior/CoopInteriorLayout_Lv"+i+".prefab")
            .Concat(new[]{Root+"/Resources/Buildings/Coop Building.asset"}).ToDictionary(p=>p,File.ReadAllText);
        materials=new[]{"NP_CozySurfaces_01","NP_CozyFoliage_01","NP_CozyGlow_01"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/"+n+".mat")).ToArray();
        Check(materials.All(m=>m!=null),"Existing shared atlas materials resolve");
        var glass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/mesh/Buildings/CoopInterior_A/CoopIncubatorGlass.mat");
        Check(glass!=null,"Existing shared transparent incubator glass resolves");materials=materials.Concat(new[]{glass}).ToArray();
        string groundPath=Models+"NP_CozyCoopEarth.mat";var ground=AssetDatabase.LoadAssetAtPath<Material>(groundPath);
        if(!ground){ground=new Material(materials[0]);AssetDatabase.CreateAsset(ground,groundPath);}else ground.CopyPropertiesFromMaterial(materials[0]);
        ground.name="NP_CozyCoopEarth";ground.SetColor("_BaseColor",new Color(1.50f,1.65f,1.05f,1));ground.SetFloat("_Saturation",.78f);ground.SetFloat("_BumpScale",.15f);EditorUtility.SetDirty(ground);
        materials=materials.Concat(new[]{ground}).ToArray();
        var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/CoopInteriorCozy/CoopInterior_Cozy_Lv1_Manifest.json"));
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
            var incubator=root.GetComponentInChildren<EggIncubator>(true);Check(incubator!=null,"Existing functional incubator retained");
            var serialized=new SerializedObject(incubator);var cover=(GameObject)serialized.FindProperty("coverVisual").objectReferenceValue;
            var egg=(GameObject)serialized.FindProperty("eggVisual").objectReferenceValue;var use=(Transform)serialized.FindProperty("interactionPoint").objectReferenceValue;
            var release=(Transform)serialized.FindProperty("releasePoint").objectReferenceValue;var body=incubator.transform.Find("Body");
            var incubatorSettings=EditorJsonUtility.ToJson(incubator);
            var anchors=binding.AnimalSpots.Concat(new[]{binding.FeedingRoot,binding.FeedMakerPoint,root.transform,incubator.transform,incubator.Nest,use,release}).ToArray();
            var before=anchors.ToDictionary(t=>t,t=>(t.position,t.rotation,t.localScale));
            var ids=slots.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId).ToArray();
            Check(slots.Length==6&&fills.Distinct().Count()==6,"Lv1 has exactly six independent feed portions");
            var catalog=Resources.Load<BuildingDefinitionSO>("Buildings/Coop Building");Check(catalog.GetLevel(1).capacity==slots.Length,"Feed count matches building catalog");
            var continuous=slots[0].parent;continuous.localPosition=new Vector3(0,0,2.2f);
            for(int i=0;i<6;i++)
            {
                slots[i].SetParent(continuous,false);slots[i].localPosition=new Vector3((i-2.5f)*.5f,.24f,0);slots[i].localRotation=Quaternion.identity;
                fills[i].transform.SetParent(continuous,false);fills[i].transform.localPosition=new Vector3((i-2.5f)*.5f,.24f,0);fills[i].transform.localRotation=Quaternion.identity;fills[i].transform.localScale=Vector3.one;
            }
            var troughCollider=continuous.Find("TroughCollider").GetComponent<BoxCollider>();troughCollider.center=new Vector3(0,.22f,0);troughCollider.size=new Vector3(3.12f,.44f,.66f);
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true).ToArray())
            {if(fills.Contains(f.GetComponent<MeshRenderer>())||f.gameObject==cover||f.gameObject==egg||f.transform==body)continue;var r=f.GetComponent<MeshRenderer>();if(r)Object.DestroyImmediate(r);Object.DestroyImmediate(f);}
            foreach(var l in root.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l);
            foreach(var t in binding.FeedingRoot.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="CozyTroughGeometry").ToArray())Object.DestroyImmediate(t.gameObject);
            var previous=root.transform.Find("CozyVisuals");if(previous)Object.DestroyImmediate(previous.gameObject);
            var visuals=Child(root.transform,"CozyVisuals");
            // Old prop colliders must not remain invisible obstacles after the new set dressing is installed.
            foreach(var c in root.GetComponentsInChildren<BoxCollider>(true).ToArray())
                if(c!=troughCollider&&!new[]{"Floor","SideWall","BackWall","FrontStub"}.Contains(c.name))Object.DestroyImmediate(c);
            string assetPath=Models+"CoopInterior_Cozy_Lv1_Runtime.asset";bool exists=File.Exists(assetPath);
            var saved=AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>().ToDictionary(m=>m.name);
            MeshRenderer architecture=null;
            foreach(var entry in spec["modules"])
            {
                string module=(string)entry["module"];var input=imported[module];var matrix=axis*input.transform.localToWorldMatrix;
                var b=BoundsOf(input.sharedMesh,matrix);float error=Vector3.Distance(b.min,V(entry["min"]))+Vector3.Distance(b.max,V(entry["max"]));
                Check(error<.02f,module+": canonical metres and correct orientation (error "+error+", actual "+b.min+" / "+b.max+", expected "+V(entry["min"])+" / "+V(entry["max"])+")");
                Check(input.sharedMesh.triangles.Length/3==(int)entry["triangles"],module+": native triangle count");
                Transform target;
                if(module.StartsWith("Fill"))target=fills[int.Parse(module.Substring(4))-1].transform;
                else if(module=="Trough")target=Child(continuous,"CozyTroughGeometry");
                else if(module=="Cover")target=cover.transform;
                else if(module=="Egg")target=egg.transform;
                else if(module=="IncubatorBody")target=body;
                else target=Child(visuals,module);
                var local=target.worldToLocalMatrix*root.transform.localToWorldMatrix*matrix;var mesh=Object.Instantiate(input.sharedMesh);mesh.name="CoopInteriorCozy_"+module;
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
                renderer.enabled=true;
                if(module=="Floor")renderer.sharedMaterial=ground;
                if(module=="Cover"){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                if(module=="Architecture")architecture=renderer;
            }
            var blockers=Child(visuals,"CozyPropColliders");
            foreach(var specCollider in spec["colliders"]){var c=Child(blockers,(string)specCollider["name"]).gameObject.AddComponent<BoxCollider>();c.center=V(specCollider["center"]);c.size=V(specCollider["size"]);}
            // A shared tiny cookie projects four panes without adding decorative floor meshes.
            string cookiePath=Models+"CoopWindowCookie.asset";var cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);
            if(!cookie){cookie=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="CoopWindowCookie",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};AssetDatabase.CreateAsset(cookie,cookiePath);}
            var pixels=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float edge=Mathf.Clamp01(Mathf.Min(Mathf.Min(x-13,50-x),Mathf.Min(y-13,50-y))/2f);float cross=Mathf.Clamp01((Mathf.Min(Mathf.Abs(x-31.5f),Mathf.Abs(y-31.5f))-1.6f)/1.2f);pixels[y*64+x]=new Color(edge*cross,edge*cross,edge*cross,edge*cross);}
            cookie.SetPixels(pixels);cookie.Apply();EditorUtility.SetDirty(cookie);
            var windowLights=new List<Light>();var warmLights=new List<Light>();
            foreach(int side in new[]{-1,1})
            {
                var t=Child(visuals,"WindowDaylight_"+side);t.localPosition=new Vector3(side*4.8f,2.1f,5.8f);t.LookAt(root.transform.TransformPoint(new Vector3(side*1.8f,.02f,4.2f)));
                var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=11;l.spotAngle=55;l.innerSpotAngle=42;l.shadowCustomResolution=256;l.shadowBias=.015f;l.shadowNormalBias=.025f;l.cookie=cookie;windowLights.Add(l);
                var w=Child(visuals,"WarmLanternFill_"+side);w.localPosition=new Vector3(side*3.0f,2.5f,4);l=w.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=9;l.color=new Color(1,.80f,.54f);l.shadows=LightShadows.None;warmLights.Add(l);
            }
            var lighting=visuals.gameObject.AddComponent<BarnInteriorCozyLighting>();lighting.Configure(windowLights.ToArray(),warmLights.ToArray(),architecture,Array.IndexOf(architecture.sharedMaterials,materials[2]),.45f);lighting.ApplyLighting(10,WeatherType.Sunny);
            foreach(var fill in fills)fill.gameObject.SetActive(false);
            egg.SetActive(false);cover.SetActive(true);
            Check(anchors.All(t=>Vector3.Distance(t.position,before[t].position)<.0001f&&t.rotation==before[t].rotation&&t.localScale==before[t].localScale),"Animal, machine, incubation/release anchors and root scale unchanged");
            Check(ids.SequenceEqual(slots.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId)),"Existing feed slot object IDs retained");
            Check(binding.FeedFills.SequenceEqual(fills),"Existing fill renderer references retained for save/consumption");
            Check(EditorJsonUtility.ToJson(incubator)==incubatorSettings,"Incubation duration, interaction and placement bindings unchanged");
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);Check(AssetDatabase.AssetPathToGUID(Prefab)==guid,"Original prefab GUID retained");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceSynchronousImport);Check(protectedFiles.All(p=>File.ReadAllText(p.Key)==p.Value),"Levels 2–4 and capacity catalog unchanged");Check(File.ReadAllText(scene)==sceneBefore,"Coop scene left unchanged");
        if(Application.isBatchMode&&Fixture)ValidateAndRender();
        foreach(var j in spec["modules"]){var i=(ModelImporter)AssetImporter.GetAtPath(Models+(string)j["name"]+".fbx");i.isReadable=false;i.SaveAndReimport();}
        File.WriteAllText(Output+"/Audit.txt",report+$"CHECKS {checks}; COOP_COZY_APPLY_PASS\n");
    }

    static void ValidateAndRender()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var binding=root.GetComponent<BarnInteriorLayout>();Physics.SyncTransforms();
        foreach(var p in new[]{new Vector3(0,0,.95f),new Vector3(0,0,3.5f),new Vector3(0,0,6),new Vector3(-2.7f,0,5.9f),new Vector3(3.5f,0,6),new Vector3(3.5f,0,6.22f),new Vector3(3.5f,0,5.7f)}.Concat(binding.AnimalSpots.Select(t=>root.transform.InverseTransformPoint(t.position))))
        {var point=root.transform.TransformPoint(p);Check(!Physics.CheckCapsule(point+Vector3.up*.30f,point+Vector3.up*1.4f,.22f,~0,QueryTriggerInteraction.Ignore),"Player/animal capsule clear at "+p);}
        Check(binding.FeedingSlots.Length==6&&binding.FeedFills.Distinct().Count()==6,"Six separate physical portions in one continuous feeder");
        Check(binding.FeedingSlots.Select(t=>t.parent).Distinct().Count()==1,"All portions share one physical trough access collider");
        Check(root.GetComponentsInChildren<MeshRenderer>(true).Length==14,"Fourteen grouped renderers; old artwork removed");
        Check(root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3)<26000,"Lv1 geometry stays below 26k triangles including all grain and egg");
        // Use the actual existing controller to test fill visibility and all-sided access.
        var holder=new GameObject("Controller regression");var controller=holder.AddComponent<BarnInteriorSceneController>();root.transform.SetParent(holder.transform,true);controller.Configure(new List<GameObject>{root},null,binding.AnimalSpots);
        var homeObject=new GameObject("Validation home");var home=homeObject.AddComponent<AnimalHome>();home.capacity=6;home.kind=AnimalHousingKind.Coop;home.homeId="cozy.coop.validation";
        typeof(BarnInteriorSceneController).GetMethod("ApplyLayout",Flags).Invoke(controller,new object[]{home,1});
        for(int count=0;count<=6;count++)
        {
            typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,count);
            typeof(BarnInteriorSceneController).GetMethod("RefreshFeedVisual",Flags).Invoke(controller,new object[]{home});
            Check(binding.FeedFills.Count(r=>r.gameObject.activeSelf)==count,"Stock "+count+" shows exactly "+count+" portions");
        }
        typeof(AnimalHome).GetField("fodderStock",Flags).SetValue(home,1);typeof(BarnInteriorSceneController).GetMethod("RefreshFeedVisual",Flags).Invoke(controller,new object[]{home});
        Check(binding.FeedFills.Count(r=>r.gameObject.activeSelf)==1,"Consumption hides only emptied portions");
        foreach(var z in new[]{1.4f,3f})foreach(var x in new[]{-1f,1f})
        {object[] args={root.transform.TransformPoint(new Vector3(x,0,z)),0f};var selected=typeof(BarnInteriorSceneController).GetMethod("ClosestTroughAccess",Flags).Invoke(controller,args);Check(selected!=null&&(float)args[1]<1.0f,"Front/back feeding access at "+x+","+z);}
        foreach(var x in new[]{-2.2f,2.2f})
        {object[] args={root.transform.TransformPoint(new Vector3(x,0,2.2f)),0f};var selected=typeof(BarnInteriorSceneController).GetMethod("ClosestTroughAccess",Flags).Invoke(controller,args);Check(selected!=null&&(float)args[1]<1.0f,"Side feeding access at "+x);}
        var incubator=root.GetComponentInChildren<EggIncubator>();var incData=new SerializedObject(incubator);
        var egg=(GameObject)incData.FindProperty("eggVisual").objectReferenceValue;var cover=(GameObject)incData.FindProperty("coverVisual").objectReferenceValue;
        Check(incubator.IncubationDays==3&&incubator.State==EggIncubatorState.Empty&&!egg.activeSelf,"Single egg incubation is empty by default; three-day duration retained");
        var probeActor=new GameObject("Incubator interaction probe");probeActor.transform.position=root.transform.TransformPoint(new Vector3(3.5f,0,6));
        Check(incubator.InRange(probeActor.transform),"Incubator reachable from the original player stand point");Object.DestroyImmediate(probeActor);
        var eggBounds=egg.GetComponent<MeshRenderer>().bounds;
        Check(Vector2.Distance(new Vector2(eggBounds.center.x,eggBounds.center.z),new Vector2(incubator.Nest.position.x,incubator.Nest.position.z))<.01f,"Egg is centred on the real nest anchor");
        foreach(var m in materials)Check(!ShaderUtil.ShaderHasError(m.shader),"Atlas shader compiles: "+m.name);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.72f,.80f,.87f);RenderSettings.ambientEquatorColor=new Color(.48f,.50f,.48f);RenderSettings.ambientGroundColor=new Color(.28f,.25f,.20f);RenderSettings.fog=false;
        var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(RenderSettings.ambientEquatorColor.linear*.8f);probe.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor.linear,.45f);RenderSettings.ambientProbe=probe;
        var sun=new GameObject("Review soft fill").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.62f;sun.color=new Color(1,.96f,.87f);sun.transform.rotation=Quaternion.Euler(52,25,0);sun.shadows=LightShadows.Soft;
        var camera=new GameObject("Coop review camera").AddComponent<Camera>();camera.fieldOfView=43;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.83f,.79f,.68f);
        camera.transform.position=new Vector3(0,16,-11);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.7f,4)));
        ShaderUtil.allowAsyncCompilation=false;
        foreach(var r in binding.FeedFills)r.gameObject.SetActive(false);Capture(camera,"Coop_Lv1_Empty");
        foreach(var r in binding.FeedFills)r.gameObject.SetActive(true);egg.SetActive(true);Capture(camera,"Coop_Lv1_Full_IncubatingPreview");
        var closedPosition=cover.transform.localPosition;var closedRotation=cover.transform.localRotation;var closedBounds=cover.GetComponent<MeshRenderer>().bounds;
        typeof(EggIncubator).GetMethod("SetCoverPose",Flags).Invoke(incubator,new object[]{1f});
        Check(cover.GetComponent<MeshRenderer>().bounds.max.y>closedBounds.max.y+.15f,"Domed cover opens above the nest around its rear hinge");
        var openGeometry=BoundsOf(cover.GetComponent<MeshFilter>().sharedMesh,cover.transform.localToWorldMatrix);
        Check(openGeometry.max.z<root.transform.TransformPoint(new Vector3(0,0,7.90f)).z,"Open dome clears the solid rear wall (mesh max Z "+openGeometry.max.z+")");
        Capture(camera,"Coop_Lv1_CoverOpen");
        typeof(EggIncubator).GetMethod("SetCoverPose",Flags).Invoke(incubator,new object[]{0f});
        Check(Vector3.Distance(cover.transform.localPosition,closedPosition)<.0001f&&Quaternion.Angle(cover.transform.localRotation,closedRotation)<.001f,"Domed cover returns exactly to its closed pose");
        camera.transform.position=new Vector3(-10,13,-8);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.7f,4)));Capture(camera,"Coop_Lv1_Detail");
        var lighting=root.GetComponentInChildren<BarnInteriorCozyLighting>();var window=root.GetComponentsInChildren<Light>().First(l=>l.name.StartsWith("Window"));lighting.ApplyLighting(10,WeatherType.Sunny);float clear=window.intensity;
        lighting.ApplyLighting(10,WeatherType.HeavyRain);Check(window.intensity<clear*.2f,"Interior window daylight responds to heavy rain");
        lighting.ApplyLighting(22,WeatherType.Sunny);Check(window.intensity<1,"Night switches to low moonlight");sun.intensity=.15f;Capture(camera,"Coop_Lv1_Night");
        Object.DestroyImmediate(homeObject);Object.DestroyImmediate(holder);
        var path=Root+"/Map/Scenes/Interiors/CoopInterior.unity";var before=File.ReadAllText(path);EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var layouts=Object.FindObjectsByType<BarnInteriorLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        File.WriteAllText(Output+"/SceneBindings.txt",string.Join("\n",layouts.Select(l=>l.name+" | "+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l.gameObject)+" | "+string.Join(",",l.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh?f.sharedMesh.name:"NULL")))));
        var layout=layouts.First(l=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l.gameObject)==Prefab);
        Check(layout.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&f.sharedMesh.name=="CoopInteriorCozy_Architecture"),"Actual Coop scene resolves new Lv1 art");
        Check(Object.FindObjectsByType<BarnInteriorLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==4,"All four upgrade layouts still bound");
        Check(File.ReadAllText(path)==before,"Actual scene review did not rewrite the scene");
        string barnPath=Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv1.prefab";var barnBefore=File.ReadAllText(barnPath);
        var barn=PrefabUtility.LoadPrefabContents(barnPath);
        try
        {
            var barnLight=barn.GetComponentInChildren<BarnInteriorCozyLighting>(true);barnLight.ApplyLighting(10,WeatherType.Sunny);
            Check(barn.GetComponentsInChildren<Light>(true).Where(l=>l.name.StartsWith("Window")).All(l=>Mathf.Abs(l.intensity-28)<.01f),"Existing Barn daylight retains its original strength");
        }
        finally{PrefabUtility.UnloadPrefabContents(barn);}
        Check(File.ReadAllText(barnPath)==barnBefore,"Barn prefab remains unchanged");
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
