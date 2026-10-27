#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Install approved pond meshes into the existing level prefabs and shared building definition.</summary>
public static class FishPondApprovedExteriorSetup
{
    public const string Root="Assets/Nature  Paradaise";
    public const string Models=Root+"/mesh/Buildings/FishPond_A";
    public const string Prefabs=Root+"/Prefabs/FishPond";
    public const string DefinitionPath=Root+"/Resources/Buildings/Fish Pond Building.asset";
    public const string Output="ArtSource/FishPond_A/Previews";
    public const string ReviewScene=Root+"/Map/Scenes/Testing/FishPond_A_Review.unity";
    const string Pack=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials";
    static readonly string[] Suffix={"High","Medium","Low","Water"};
    static JObject spec;
    static StringBuilder report;
    [MenuItem("Nature Paradise/Fish Pond/Apply Compact Field Scale",false,124)]
    public static void ApplyCompactScale()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before resizing pond prefabs.");
        for(int level=1;level<=3;level++)
        {
            string path=$"{Prefabs}/FishPond_Lv{level}.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localScale=Vector3.one*FishPondExterior.WorldScale;
                var so=new SerializedObject(root.GetComponent<FishPond>());
                so.FindProperty("interactionRadius").floatValue=3.5f*FishPondExterior.WorldScale;
                so.FindProperty("feedInteractionRadius").floatValue=Mathf.Max(1,2.4f*FishPondExterior.WorldScale);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static Vector3 FromApproved(JToken p)=>new(-(float)p[0],(float)p[1],-(float)p[2]);
    static Vector3 ExpectedSize(JToken p)=>new((float)p[0],(float)p[2],(float)p[1]);
    static Transform Child(Transform parent,string name){var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
    static void Require(bool value,string label){if(!value)throw new InvalidOperationException("Pond A: "+label);report.AppendLine("PASS "+label);}
    static Bounds Measure(Mesh m,Quaternion rotation)
    {var b=new Bounds(rotation*m.vertices[0],Vector3.zero);foreach(var v in m.vertices)b.Encapsulate(rotation*v);return b;}

    [MenuItem("Nature Paradise/Fish Pond/Apply Approved Pond A",false,120)]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying pond assets.");
        report=new StringBuilder("FISH POND A UNITY APPLY\n");
        spec=JObject.Parse(File.ReadAllText("ArtSource/FishPond_A/References/FishPond_A_Approved.json"));
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);
        string backup="ArtSource/FishPond_A/Revisions/BeforeUnityApply";Directory.CreateDirectory(backup);
        foreach(string path in Directory.GetFiles(Prefabs).Concat(new[]{DefinitionPath,Root+"/Material/World/FishPond/FishPondWater.mat"}))
        {string target=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(target))File.Copy(path,target);}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var atlas=AssetDatabase.LoadAssetAtPath<Material>(Pack+"/TFP_Atlas_1A.mat");
        var sourceWater=AssetDatabase.LoadAssetAtPath<Material>(Pack+"/TFP_Water_1A.mat");
        Require(atlas && sourceWater && sourceWater.shader.name=="Toon/CustomWaterSimple","Existing Toon Pack atlas and water available");
        string waterPath=Root+"/Material/World/FishPond/FishPondWater.mat";
        var water=AssetDatabase.LoadAssetAtPath<Material>(waterPath);
        if(!water){water=new Material(sourceWater);AssetDatabase.CreateAsset(water,waterPath);}
        else{water.shader=sourceWater.shader;water.CopyPropertiesFromMaterial(sourceWater);}
        water.name="FishPondWater";
        foreach(var p in (JObject)spec["materials"]["proposedPondWaterOverrides"])water.SetFloat(p.Key,(float)p.Value);
        TuneWater(water);
        water.DisableKeyword("_WAVES_ON"); // Flat water edge; normal-map ripples remain animated.
        EditorUtility.SetDirty(water);
        Require(water.GetTexture("_NormalMap")==sourceWater.GetTexture("_NormalMap") && water.GetTexture("_NoiseTexture")==sourceWater.GetTexture("_NoiseTexture"),"Water reuses pack normal/noise textures");
        var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(DefinitionPath);Require(definition!=null,"Stable pond building definition");
        for(int lv=1;lv<=3;lv++)
        {
            JToken m=spec["models"][lv-1];var meshes=new Mesh[4];var rotations=new Quaternion[4];
            var lo=FromApproved(m["visualBounds"]["maxXYZ"]);var hi=FromApproved(m["visualBounds"]["minXYZ"]);
            var expected=new Bounds((lo+hi)/2,ExpectedSize(m["visualBounds"]["sizeWDH"]));
            expected.min=new Vector3(lo.x,hi.y,lo.z);expected.max=new Vector3(hi.x,lo.y,hi.z);
            for(int lod=0;lod<4;lod++)
            {
                string file=$"FishPond_A_Lv{lv}_{Suffix[lod]}.fbx",path=Models+"/"+file;
                File.Copy("ArtSource/FishPond_A/Exports/"+file,path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;
                importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.importCameras=false;importer.importLights=false;
                importer.addCollider=false;importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;
                importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),lod==3?"TFP_Water_1A":"TFP_Atlas_1A"),lod==3?water:atlas);
                importer.SaveAndReimport();
                var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=imported.GetComponentsInChildren<MeshFilter>(true).Single();meshes[lod]=filter.sharedMesh;
                Require(meshes[lod].subMeshCount==1 && imported.GetComponentsInChildren<MeshRenderer>(true).Single().sharedMaterial==(lod==3?water:atlas),file+" single mesh/material remapped");
                Require(Vector3.Distance(filter.transform.lossyScale,Vector3.one)<.0001f && filter.transform.position.sqrMagnitude<.00001f,file+" metre scale and ground pivot");
                if(lod==0)
                {
                    rotations[0]=Enumerable.Range(0,4).Select(y=>Quaternion.Euler(0,y*90,0)*filter.transform.rotation)
                        .OrderBy(q=>Vector3.Distance(Measure(meshes[0],q).min,expected.min)+Vector3.Distance(Measure(meshes[0],q).max,expected.max)).First();
                    Bounds b=Measure(meshes[0],rotations[0]);Require(Vector3.Distance(b.min,expected.min)+Vector3.Distance(b.max,expected.max)<.003f,file+" approved asymmetrical bounds and front +Z");
                }
                else rotations[lod]=rotations[0];
                if(lod==3)
                {
                    Require(meshes[lod].triangles.Length==42,file+" 14 triangles");
                    Require(meshes[lod].vertices.All(v=>Mathf.Abs((rotations[lod]*v).y-.65f)<.0001f),file+" water height");
                    Require(meshes[lod].normals.All(n=>(rotations[lod]*n).y>.999f),file+" upward normals for pack backface culling");
                    foreach(JToken q in m["waterPolygonXZ"])
                    {Vector3 want=new(-(float)q[0],.65f,-(float)q[1]);Require(meshes[lod].vertices.Any(v=>Vector3.Distance(rotations[lod]*v,want)<.0001f),file+" approved boundary "+want);}
                }
                report.AppendLine(file+" triangles "+meshes[lod].triangles.Length/3);
            }
            GameObject prefab=Build(lv,m,meshes,rotations,atlas,water);
            var entry=definition.GetLevel(lv);Require(entry!=null,"Existing cost data Lv"+lv);
            entry.completedPrefab=prefab;entry.capacity=(int)m["capacity"];
            Validate(prefab,lv,m,atlas,water);
        }
        definition.levels.RemoveAll(l=>l!=null && l.level>3);
        var map=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Single);
        ConfigureFootprint(definition,map);
        var follow=map.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<TopDownCameraFollow>(true)).FirstOrDefault();
        if(follow)definition.defaultPlacementYaw=Mathf.DeltaAngle(0,follow.Yaw+180);
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
        CreateReviewScene();
        ValidateMigration();
        File.WriteAllText(Output+"/UnityImportAudit.txt",report+"\nAPPLY PASS\n");
        var manifest=JObject.Parse(File.ReadAllText(Output+"/MeshManifest.json"));manifest["status"]="APPLIED_TO_UNITY_VALIDATED";manifest["unityAssetsModified"]=true;
        File.WriteAllText(Output+"/MeshManifest.json",manifest.ToString());
        Debug.Log("[Pond A] APPLY PASS: Lv1-3, 50/100/250 fish, existing pack water shader, LODs and dock interaction.");
    }

    static GameObject Build(int lv,JToken m,Mesh[] meshes,Quaternion[] rotations,Material atlas,Material water)
    {
        string path=$"{Prefabs}/FishPond_Lv{lv}.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadAssetAtPath<GameObject>(path),out string guid,out long id);
        try
        {
            foreach(var t in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);
            foreach(var c in root.GetComponents<Collider>())Object.DestroyImmediate(c);
            root.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one*FishPondExterior.WorldScale;
            var model=Child(root.transform,"Model_Approved");var group=model.gameObject.AddComponent<LODGroup>();var lods=new LOD[3];
            var waterObject=Child(model,"Water_ToonPack");waterObject.localRotation=rotations[3];waterObject.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[3];
            var waterRenderer=waterObject.gameObject.AddComponent<MeshRenderer>();waterRenderer.sharedMaterial=water;waterRenderer.shadowCastingMode=ShadowCastingMode.Off;
            for(int lod=0;lod<3;lod++)
            {
                var t=Child(model,"Opaque_LOD"+lod);t.localRotation=rotations[lod];t.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[lod];
                var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=atlas;
                lods[lod]=new LOD(new[]{.22f,.085f,.008f}[lod],new Renderer[]{r,waterRenderer});
                if(lod==0)t.gameObject.AddComponent<MeshCollider>().sharedMesh=meshes[0];
            }
            group.fadeMode=LODFadeMode.None;group.SetLODs(lods);group.RecalculateBounds();
            var stations=m["stations"];
            var point=Child(root.transform,"PondInteraction_Dock");point.localPosition=FromApproved(stations["pondInteractionXYZ"]);
            var barrel=Child(root.transform,"FishFeedTrough_Editable");barrel.localPosition=FromApproved(stations["storage"]["centerXYZ"]);
            var makerPoint=Child(root.transform,"FishPondFeedMaker_Editable");makerPoint.localPosition=FromApproved(stations["maker"]["centerXYZ"]);
            var maker=makerPoint.gameObject.AddComponent<FeedMaker>();maker.SetDedicatedOutput(true);maker.SetExternalInteraction(true);maker.SetWorldDebugVisible(false);
            var so=new SerializedObject(maker);so.FindProperty("catalog").objectReferenceValue=Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog");so.ApplyModifiedPropertiesWithoutUndo();
            var pond=root.GetComponent<FishPond>();pond.Configure(string.Empty,lv,Resources.Load<ItemSO>("Items/Fish/Fish Feed"));pond.EditorConfigureFeedStations(barrel,maker);
            so=new SerializedObject(pond);so.FindProperty("maximumVisibleFish").intValue=(int)m["maximumVisibleFish"];so.FindProperty("interactionRadius").floatValue=3.5f*FishPondExterior.WorldScale;so.FindProperty("feedInteractionRadius").floatValue=Mathf.Max(1,2.4f*FishPondExterior.WorldScale);so.ApplyModifiedPropertiesWithoutUndo();
            var exterior=root.GetComponent<FishPondExterior>()??root.AddComponent<FishPondExterior>();
            var polygon=m["waterPolygonXZ"].Select(v=>new Vector2(-(float)v[0],-(float)v[1])).ToArray();var d=m["dock"];float x=-(float)d["centerXYZ"][0],w=(float)d["width"];
            exterior.EditorConfigure(point,polygon,new Vector2(x-w/2,-(float)d["backZ"]),new Vector2(x+w/2,-(float)d["frontZ"]));
            foreach(var p in m["parts"].Where(p=>(string)p["group"]=="sketchWaterFx"))
            {var t=Child(root.transform,"FX_"+(string)p["name"]);t.localPosition=FromApproved(p["verticesXYZ"][0]);}
            if(lv>=2){var t=Child(root.transform,"AutoFeeder_PelletEmitter");t.localPosition=FromApproved(stations["autoFeeder"]["emitterXYZ"]);t.localRotation=Quaternion.LookRotation(Vector3.back);}
            var saved=PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved,out string savedGuid,out long savedId);
            Require(guid==savedGuid && id==savedId,"Prefab Lv"+lv+" existing GUID/root retained");return saved;
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static void Validate(GameObject p,int lv,JToken m,Material atlas,Material water)
    {
        var group=p.GetComponentInChildren<LODGroup>();Require(group.GetLODs().Length==3,"Lv"+lv+" three Blender LODs");
        Require(p.GetComponentsInChildren<MeshRenderer>(true).Length==4,"Lv"+lv+" one opaque per LOD and shared water");
        Require(group.GetLODs().All(l=>l.renderers.Length==2 && l.renderers.Any(r=>r.sharedMaterial==water)),"Lv"+lv+" water follows every LOD and culls together");
        Require(p.GetComponentsInChildren<FeedMaker>(true).Length==1,"Lv"+lv+" one bound feed maker");
        Require(!p.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("ReplaceMe")),"Lv"+lv+" no prototype renderers");
        Require(p.GetComponentsInChildren<MeshCollider>(true).Single().sharedMesh==group.GetLODs()[0].renderers[0].GetComponent<MeshFilter>().sharedMesh,"Lv"+lv+" fixed high collision for stairs and basin");
        Require(p.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterial==atlas||r.sharedMaterial==water),"Lv"+lv+" only existing atlas/pack water shader");
        var ext=p.GetComponent<FishPondExterior>();
        Require(Vector3.Distance(ext.InteractionPoint.localPosition,FromApproved(m["stations"]["pondInteractionXYZ"]))<.001f,"Lv"+lv+" approved dock E marker");
        for(int fish=0;fish<(int)m["maximumVisibleFish"];fish++)Require(ext.IsSwimPoint(ext.SwimOrigin(fish)),"Lv"+lv+" fish "+fish+" inside water and outside dock");
    }

    static void ValidateMigration()
    {
        var saved=FishPondService.Capture();
        try
        {
            var old=new FishPondSaveData{pondId="audit.pond",level=4,feedStock=1};
            for(int i=0;i<300;i++)old.fish.Add(new FishPondFishData{itemId="audit."+i,qualityStars=3,sizeCm=25,growthDays=12});
            FishPondService.Restore(new System.Collections.Generic.List<FishPondSaveData>{old});var now=FishPondService.Capture().Single();
            Require(now.level==3 && now.fish.Count==300 && now.fish[299].itemId=="audit.299" && now.feedStock==1,"Legacy Lv4->3 retains every over-capacity fish and feed");
            Require(FishPondService.Capacity(1)==50 && FishPondService.Capacity(2)==100 && FishPondService.Capacity(3)==250,"Approved capacities 50/100/250");
        }
        finally{FishPondService.Restore(saved);}
    }

    static void CreateReviewScene()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Review ground";ground.transform.position=new Vector3(0,-.10f,0);ground.transform.localScale=new Vector3(90,.16f,45);
        // A studio plane must sample a safe palette cell, never display the entire atlas.
        var mesh=Object.Instantiate(ground.GetComponent<MeshFilter>().sharedMesh);
        var uv=(JArray)spec["materials"]["swatches"]["leaf"]["safeUvBottomLeft"];
        mesh.uv=Enumerable.Repeat(new Vector2(((float)uv[0]+(float)uv[2])/2,((float)uv[1]+(float)uv[3])/2),mesh.vertexCount).ToArray();
        string floorPath=Models+"/PondReviewGround.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(floorPath);
        if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        else{AssetDatabase.CreateAsset(mesh,floorPath);saved=mesh;}
        ground.GetComponent<MeshFilter>().sharedMesh=saved;
        ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Pack+"/TFP_Atlas_1A.mat");
        for(int lv=1;lv<=3;lv++)
        {var p=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/FishPond_Lv{lv}.prefab"),scene);p.transform.position=new Vector3((lv-2)*24,0,0);}
        var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.8f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.67f,.71f);
        var camera=new GameObject("Pond review camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=13;camera.nearClipPlane=.1f;camera.farClipPlane=200;
        camera.transform.position=new Vector3(-12,20,24);camera.transform.LookAt(new Vector3(-1.5f,.4f,0));
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.4f,.46f);
        var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderPostProcessing=false;
        EditorSceneManager.SaveScene(scene,ReviewScene);
        var rt=new RenderTexture(1500,1100,24,RenderTextureFormat.ARGB32);
        try
        {
            for(int lv=1;lv<=3;lv++)
            {
                var p=scene.GetRootGameObjects().First(o=>o.GetComponent<FishPond>() && o.GetComponent<FishPond>().Level==lv);
                camera.orthographicSize=lv==3?9.8f:lv==2?8.4f:7.4f;
                camera.transform.position=p.transform.position+new Vector3(-12,20,24);camera.transform.LookAt(p.transform.position+new Vector3(-1.5f,.4f,0));
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Output+$"/FishPond_A_Lv{lv}_Unity.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }
        }
        finally{rt.Release();Object.DestroyImmediate(rt);}
        report.AppendLine("Native Unity URP renders captured with depth and opaque textures; testing scene saved.");
    }

    static void TuneWater(Material water)
    {
        water.SetColor("_ShallowColor",new Color(.10f,.47f,.62f,1));
        water.SetColor("_DeepColor",new Color(.04f,.24f,.36f,1));
        water.SetColor("_FresnelColor",new Color(.37f,.66f,.75f,1));
        water.SetFloat("_FresnelIntensity",.18f);water.SetFloat("_Opacity",.5f);
    }

    static void ConfigureFootprint(BuildingDefinitionSO definition,UnityEngine.SceneManagement.Scene map)
    {
        // FieldOnly dimensions are grid cells, while the approved site dimensions are metres.
        var field=map.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<FieldArea>(true)).FirstOrDefault();
        float cell=field!=null?field.CellSize:1;
        var size=FishPondExterior.PlacementSizeMeters(3);
        definition.footprintWidth=Mathf.CeilToInt(size.x/cell);definition.footprintDepth=Mathf.CeilToInt(size.y/cell);
        EditorUtility.SetDirty(definition);
        report.AppendLine($"Largest compact site: {definition.footprintWidth}x{definition.footprintDepth} field cells of {cell} m ({size.x}x{size.y} m).");
    }

    [MenuItem("Nature Paradise/Fish Pond/Refresh Water Review",false,121)]
    public static void RefreshReview()
    {
        spec=JObject.Parse(File.ReadAllText("ArtSource/FishPond_A/References/FishPond_A_Approved.json"));report=new StringBuilder();
        var water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/World/FishPond/FishPondWater.mat");TuneWater(water);EditorUtility.SetDirty(water);AssetDatabase.SaveAssets();
        var map=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Single);
        ConfigureFootprint(AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(DefinitionPath),map);AssetDatabase.SaveAssets();
        CreateReviewScene();Debug.Log("[Pond A] Water review refreshed.");
    }
}
#endif
