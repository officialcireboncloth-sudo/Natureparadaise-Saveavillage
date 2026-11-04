#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Reference Lv1 pond art; retains saved pond identity, compact scale and Toonpack water.</summary>
[InitializeOnLoad]
public static class FishPondCozySetup
{
    const string Root="Assets/Nature  Paradaise", Models=Root+"/mesh/Buildings/FishPondCozy/";
    const string Prefab=Root+"/Prefabs/FishPond/FishPond_Lv1.prefab", Output="ArtSource/FishPondCozy/UnityReview";
    const string PlayKey="NatureParadise.PondCozy.PlayAudit";
    static StringBuilder report;static int checks;static double started;static bool tested;
    static bool Fixture=>Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
    static Vector3 V(JToken p)=>new((float)p[0],(float)p[1],(float)p[2]);
    static Vector2 V2(JToken p)=>new((float)p[0],(float)p[1]);
    static void Check(bool okay,string label){if(!okay)throw new InvalidOperationException(label);checks++;report.AppendLine("PASS "+label);}
    static FishPondCozySetup(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
    static void State(PlayModeStateChange s)
    {
        if(!SessionState.GetBool(PlayKey,false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;tested=false;}
        if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(PlayKey,false);EditorApplication.Exit(File.Exists(Output+"/PlayError.txt")?1:0);}
    }
    static Mesh Store(Mesh mesh)
    {
        string path=Models+"FishPond_Cozy_Runtime.asset";var old=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
        if(old!=null){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
        if(File.Exists(path))AssetDatabase.AddObjectToAsset(mesh,path);else AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Material PondSurface()
    {
        const string folder=Root+"/Textures/Shared/Cozy/";
        foreach(string suffix in new[]{"BaseColor","Normal","SurfaceMask"})
        {
            string path=folder+"NP_CozySurfaces_03_"+suffix+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=suffix=="BaseColor";imp.alphaSource=TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.anisoLevel=2;imp.maxTextureSize=suffix=="BaseColor"?1024:512;imp.isReadable=false;
            foreach(string platform in new[]{"Android","iPhone"}){var settings=imp.GetPlatformTextureSettings(platform);settings.overridden=true;settings.maxTextureSize=imp.maxTextureSize;settings.format=TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(settings);}imp.SaveAndReimport();
        }
        string matPath=Root+"/Material/Shared/NP_CozySurfaces_03.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!m){m=new Material(Shader.Find("Nature Paradise/Cozy Atlas")){name="NP_CozySurfaces_03"};AssetDatabase.CreateAsset(m,matPath);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"NP_CozySurfaces_03_BaseColor.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"NP_CozySurfaces_03_Normal.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"NP_CozySurfaces_03_SurfaceMask.png"));
        m.SetColor("_BaseColor",Color.white);m.SetFloat("_AmbientStrength",.78f);m.SetFloat("_BumpScale",.45f);m.SetFloat("_Wrap",.08f);m.SetFloat("_Saturation",.96f);m.SetFloat("_UseNormal",1);m.SetFloat("_UseMask",1);m.SetFloat("_Smoothness",1);m.enableInstancing=true;m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);
        Check(m.GetTexture("_BaseMap").width==1024&&m.GetTexture("_BumpMap").width==512,"Pond surface atlas uses mobile 1024/512 sizes");return m;
    }
    static Material PondWater()
    {
        var old=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/World/FishPond/FishPondWater.mat");string path=Root+"/Material/World/FishPond/FishPondWater_Cozy.mat";var water=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!water){water=new Material(old);AssetDatabase.CreateAsset(water,path);}else water.CopyPropertiesFromMaterial(old);water.name="FishPondWater_Cozy";
        water.SetColor("_ShallowColor",new Color(.075f,.29f,.27f,1));water.SetColor("_DeepColor",new Color(.025f,.14f,.15f,1));water.SetColor("_FresnelColor",new Color(.23f,.40f,.38f,1));
        water.SetFloat("_ShallowColorDepth",.30f);water.SetFloat("_Opacity",.64f);water.SetFloat("_OpacityDepth",.75f);water.SetFloat("_FoamOpacity",.08f);water.SetFloat("_FoamDistance",.014f);water.SetFloat("_FresnelIntensity",.12f);water.SetFloat("_ReflectionsOpacity",.075f);EditorUtility.SetDirty(water);
        Check(water.shader==old.shader&&water.GetTexture("_NormalMap")==old.GetTexture("_NormalMap"),"Pond teal water retains Toonpack shader and normal texture");return water;
    }
    [MenuItem("Nature Paradise/Art/Apply Cozy Fish Pond Level 1")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before changing pond art.");
        report=new StringBuilder("COZY FISH POND LV1\n");checks=0;Directory.CreateDirectory(Output);
        var protectedPaths=new[]{Root+"/Resources/Buildings/Fish Pond Building.asset",Root+"/Prefabs/FishPond/FishPond_Lv2.prefab",Root+"/Prefabs/FishPond/FishPond_Lv3.prefab",Root+"/Material/World/FishPond/FishPondWater.mat"};
        var originalBytes=protectedPaths.Select(File.ReadAllBytes).ToArray();
        var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/FishPondCozy/FishPond_Cozy_Manifest.json"));
        Check(((JArray)spec["woodUV"]).Count>30,"Timbers have metre-based grain UV records");
        var atlasRects=JObject.Parse(File.ReadAllText(Root+"/Textures/Shared/Cozy/NP_CozyAtlasSlots_03.json"))["slots"].Select(s=>s["uvSafe"].Select(v=>(float)v).ToArray()).ToArray();
        var surface=PondSurface();var foliage=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/NP_CozyFoliage_01.mat");var materials=new[]{surface,foliage}.ToDictionary(m=>m.name);
        var water=PondWater();
        Check(water!=null&&water.shader.name=="Toon/CustomWaterSimple","Separate water uses existing Toonpack shader");
        materials.Add("TFP_Water_1A",water);var meshes=new Mesh[4];var slots=new Material[4][];
        for(int i=0;i<4;i++)
        {
            string name=i==3?"FishPond_Cozy_Water":"FishPond_Cozy_LOD"+i,path=Models+name+".fbx";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(ModelImporter)AssetImporter.GetAtPath(path);
            imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;
            imp.importCameras=false;imp.importLights=false;imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
            imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var m in materials){imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.Key),m.Value);imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name+"-"+m.Key),m.Value);}imp.SaveAndReimport();
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);var f=imported.GetComponentsInChildren<MeshFilter>(true).Single();
            var axes=imported.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cozy")).ToDictionary(t=>t.name,t=>t.position);
            var candidates=Enumerable.Range(0,4).SelectMany(y=>new[]{1f,-1f}.Select(sign=>Matrix4x4.Rotate(Quaternion.Euler(0,y*90,0))*Matrix4x4.Scale(new Vector3(sign,1,1))));
            var matrix=candidates.OrderBy(m=>Vector3.Distance(m.MultiplyPoint3x4(axes["CozyRight"]),Vector3.right)+Vector3.Distance(m.MultiplyPoint3x4(axes["CozyForward"]),Vector3.forward)+Vector3.Distance(m.MultiplyPoint3x4(axes["CozyUp"]),Vector3.up)).First();
            Check(Vector3.Distance(matrix.MultiplyPoint3x4(axes["CozyRight"]),Vector3.right)<.001f&&Vector3.Distance(matrix.MultiplyPoint3x4(axes["CozyForward"]),Vector3.forward)<.001f,"Import "+i+": front and handedness correct");
            var tr=matrix*f.transform.localToWorldMatrix;var mesh=Object.Instantiate(f.sharedMesh);mesh.name=name;
            mesh.vertices=mesh.vertices.Select(tr.MultiplyPoint3x4).ToArray();mesh.normals=mesh.normals.Select(n=>tr.inverse.transpose.MultiplyVector(n).normalized).ToArray();float flip=tr.determinant<0?-1:1;
            mesh.tangents=mesh.tangents.Select(t=>{var d=tr.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(d.x,d.y,d.z,t.w*flip);}).ToArray();
            if(flip<0)for(int sub=0;sub<mesh.subMeshCount;sub++){var t=mesh.GetTriangles(sub);for(int n=0;n<t.Length;n+=3){int a=t[n];t[n]=t[n+2];t[n+2]=a;}mesh.SetTriangles(t,sub);}mesh.RecalculateBounds();
            var entry=i==3?spec["water"]:spec["lods"][i];Check(mesh.triangles.Length/3==(int)entry["triangles"],"Import "+i+": triangle count retained");
            Check(Vector3.Distance(mesh.bounds.min,V(entry["min"]))<.003f&&Vector3.Distance(mesh.bounds.max,V(entry["max"]))<.003f,"Import "+i+": Blender bounds retained");
            Check(mesh.uv.Length==mesh.vertexCount,"Import "+i+": complete UVs");
            var remaps=f.GetComponent<MeshRenderer>().sharedMaterials;
            Check(remaps.All(m=>m!=null&&materials.Values.Contains(m)),"Import "+i+": shared materials resolve");
            if(i<3)
            {
                bool safe=true;var uv=mesh.uv;
                for(int sub=0;sub<mesh.subMeshCount;sub++)if(remaps[sub]==surface)
                {var tri=mesh.GetTriangles(sub);for(int n=0;n<tri.Length;n+=3)safe&=atlasRects.Any(r=>Enumerable.Range(0,3).All(j=>{var t=uv[tri[n+j]];return t.x>=r[0]-.0001f&&t.y>=r[1]-.0001f&&t.x<=r[2]+.0001f&&t.y<=r[3]+.0001f;}));}
                Check(safe,"LOD"+i+": every surface triangle stays inside a single padded atlas slot");
            }
            if(i<3)Check(mesh.uv.All(uv=>uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1),"LOD"+i+": UVs inside atlas");
            else Check(mesh.vertices.All(v=>Mathf.Abs(v.y-.65f)<.0001f)&&mesh.normals.All(n=>n.y>.999f),"Water is flat with upward facing normals, independent UVs");
            meshes[i]=Store(mesh);slots[i]=remaps;
        }
        Check(meshes[0].triangles.Length>meshes[1].triangles.Length&&meshes[1].triangles.Length>meshes[2].triangles.Length&&meshes[2].triangles.Length/3<4000,"Three decreasing LODs; far mesh below 4000 triangles");
        var siteSize=FishPondExterior.PlacementSizeMeters(1)/FishPondExterior.WorldScale;
        Check(meshes.Take(3).All(m=>m.bounds.min.x>-siteSize.x/2&&m.bounds.max.x<siteSize.x/2&&m.bounds.min.z>-siteSize.y/2&&m.bounds.max.z<siteSize.y/2),"All solid geometry fits the existing compact placement envelope");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab,out string guid,out long id);
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var scale=root.transform.localScale;var pond=root.GetComponent<FishPond>();var before=new SerializedObject(pond);string savedId=before.FindProperty("pondId").stringValue;
            var old=root.transform.Find("Model_Approved");if(old)Object.DestroyImmediate(old.gameObject);
            var model=new GameObject("Model_Approved");model.transform.SetParent(root.transform,false);var group=model.AddComponent<LODGroup>();var lods=new LOD[3];
            var w=new GameObject("Water_ToonPack");w.transform.SetParent(model.transform,false);w.AddComponent<MeshFilter>().sharedMesh=meshes[3];var wr=w.AddComponent<MeshRenderer>();wr.sharedMaterial=water;wr.shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<3;i++)
            {
                var part=new GameObject("Opaque_LOD"+i);part.transform.SetParent(model.transform,false);part.AddComponent<MeshFilter>().sharedMesh=meshes[i];var r=part.AddComponent<MeshRenderer>();r.sharedMaterials=slots[i];
                if(i==0)part.AddComponent<MeshCollider>().sharedMesh=meshes[0];lods[i]=new LOD(new[]{.22f,.085f,.008f}[i],new Renderer[]{r,wr});
            }
            group.SetLODs(lods);group.fadeMode=LODFadeMode.None;group.RecalculateBounds();
            var stations=spec["stations"];var entry=root.transform.Find("PondInteraction_Dock");entry.localPosition=V(stations["pond"]);
            root.transform.Find("FishFeedTrough_Editable").localPosition=V(stations["feeding"]);
            root.transform.Find("FishPondFeedMaker_Editable").localPosition=V(stations["maker"]);
            root.GetComponent<FishPondExterior>().EditorConfigure(entry,spec["waterPolygon"].Select(V2).ToArray(),V2(spec["dockMinimum"]),V2(spec["dockMaximum"]));
            foreach(Transform t in root.transform.Cast<Transform>().Where(t=>t.name.StartsWith("FX_")).ToArray())Object.DestroyImmediate(t.gameObject);
            foreach(string key in new[]{"inlet","splash"}){var anchor=new GameObject("FX_"+key+"_ToonPack");anchor.transform.SetParent(root.transform,false);anchor.transform.localPosition=V(stations[key]);}
            Check(root.transform.localScale==scale&&scale==Vector3.one*FishPondExterior.WorldScale,"Existing compact .4 root scale retained");
            Check(new SerializedObject(pond).FindProperty("pondId").stringValue==savedId,"Persistent pond identity retained");
            Check(pond.GetComponentInChildren<FeedMaker>(true)!=null,"Existing feed machine component retained");
            Check(group.GetLODs().All(l=>l.renderers.Contains(wr)),"One shared water mesh follows all three LODs");
            var ext=root.GetComponent<FishPondExterior>();for(int i=0;i<18;i++)Check(ext.IsSwimPoint(ext.SwimOrigin(i)),"Fish spawn "+i+" within water, clear of deck");
            Check(root.GetComponentsInChildren<Collider>(true).All(c=>!c.transform.IsChildOf(w.transform)),"Water is a render surface, no water collision");
            var saved=PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved,out string newGuid,out long newId);Check(guid==newGuid&&id==newId,"Existing prefab GUID and root file ID retained");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();for(int i=0;i<protectedPaths.Length;i++)Check(File.ReadAllBytes(protectedPaths[i]).SequenceEqual(originalBytes[i]),"Protected catalog/higher levels/water unchanged: "+Path.GetFileName(protectedPaths[i]));
        if(Fixture)Review(spec);
        report.AppendLine("FISH_POND_COZY_PASS checks="+checks);File.WriteAllText(Output+"/Audit.txt",report.ToString());
    }
    static void Review(JObject spec)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Check(SaveManager.Instance==null,"Isolated review has no SaveManager");
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var group=model.GetComponentInChildren<LODGroup>();group.ForceLOD(0);
        var c=model.GetComponentInChildren<MeshCollider>();Physics.SyncTransforms();
        Check(c.Raycast(new Ray(model.transform.TransformPoint(new Vector3(-3.7f,2,0)),Vector3.down),out var bed,2)&&Mathf.Abs(bed.point.y-.04f)<.005f&&bed.normal.y>.99f,"Textured pebble bed faces up underneath the transparent water");
        foreach(var p in new[]{new Vector3(.55f,5,1.65f),new Vector3(.55f,5,3.20f),new Vector3(.55f,5,3.52f),new Vector3(.55f,5,3.84f)})
            Check(c.Raycast(new Ray(model.transform.TransformPoint(p),Vector3.down),out var hit,4)&&hit.normal.y>.95f,"Walkable deck/stair surface at "+p.z);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.transform.localScale=Vector3.one*6;ground.transform.position=Vector3.down*.027f;
        var floor=new Material(Shader.Find("Universal Render Pipeline/Lit"));floor.color=new Color(.28f,.36f,.17f);ground.GetComponent<Renderer>().sharedMaterial=floor;
        var actor=new GameObject("Player stair collision check").AddComponent<CharacterController>();actor.height=PlayerDimensions.Height;actor.radius=PlayerDimensions.Radius;actor.center=Vector3.up*actor.height/2;actor.skinWidth=PlayerDimensions.SkinWidth;actor.stepOffset=PlayerDimensions.StepOffset;
        actor.transform.position=model.transform.TransformPoint(new Vector3(.55f,.03f,4.5f));Physics.SyncTransforms();
        for(int i=0;i<66;i++)actor.Move(new Vector3(0,-.04f,-.014f));
        Check(actor.transform.position.y>.32f&&Vector3.Distance(actor.transform.position,model.GetComponent<FishPondExterior>().InteractionPoint.position)<.30f,"Actual player capsule climbs three stairs and reaches dock interaction");Object.DestroyImmediate(actor.gameObject);
        var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.color=new Color(1,.98f,.94f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.transform.rotation=Quaternion.Euler(50,-145,0);RenderSettings.sun=sun;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.79f,.85f,.92f);RenderSettings.ambientEquatorColor=new Color(.56f,.59f,.62f);RenderSettings.ambientGroundColor=new Color(.36f,.34f,.30f);RenderSettings.fog=false;
        var camera=new GameObject("Review camera").AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.55f,.36f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderShadows=true;
        foreach(var v in new[]{("Front",47f,180f),("ThreeQuarter",40f,205f),("Rear",40f,20f),("Top",89f,180f)}){group.ForceLOD(0);Render(group,camera,v.Item2,v.Item3,Output+"/Pond_"+v.Item1+".png");}
        for(int i=1;i<3;i++){group.ForceLOD(i);Render(group,camera,47,180,Output+"/Pond_LOD"+i+".png");}
        Object.DestroyImmediate(model);Object.DestroyImmediate(ground);Object.DestroyImmediate(floor);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(sun.gameObject);
    }
    static void Render(LODGroup group,Camera camera,float pitch,float yaw,string path)
    {
        var b=group.GetLODs()[0].renderers[0].bounds;camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);camera.transform.position=b.center-camera.transform.forward*20;
        var corners=(from x in new[]{-1f,1f} from y in new[]{-1f,1f} from z in new[]{-1f,1f} select camera.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)))).ToArray();
        float left=corners.Min(v=>v.x),right=corners.Max(v=>v.x),bottom=corners.Min(v=>v.y),top=corners.Max(v=>v.y);
        camera.transform.position+=camera.transform.right*((left+right)/2)+camera.transform.up*((bottom+top)/2);camera.orthographicSize=Mathf.Max((top-bottom)/2,(right-left)/2/1.2727f)*1.12f;
        var rt=new RenderTexture(1400,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var prior=RenderTexture.active;camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
        var im=new Texture2D(1400,1100,TextureFormat.RGB24,false,false);im.ReadPixels(new Rect(0,0,1400,1100),0,0);im.Apply();File.WriteAllBytes(path,im.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(im);Object.DestroyImmediate(rt);
    }
    public static void RunBatch()
    {
        if(!Fixture)throw new InvalidOperationException("Use isolated review project.");
        try{Apply();if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");EditorApplication.Exit(0);}
        catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",report+"\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void RunPlayBatch()
    {
        if(!Fixture)throw new InvalidOperationException("Isolated review only.");
        Directory.CreateDirectory(Output);if(File.Exists(Output+"/PlayError.txt"))File.Delete(Output+"/PlayError.txt");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);for(int i=1;i<=3;i++){var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/FishPond/FishPond_Lv"+i+".prefab"));o.name="FishPond_Lv"+i;o.transform.position=Vector3.right*(i-1)*15;}
        SessionState.SetBool(PlayKey,true);tested=false;started=EditorApplication.timeSinceStartup;EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(PlayKey,false)||!EditorApplication.isPlaying||tested||EditorApplication.timeSinceStartup-started<2)return;tested=true;
        report=new StringBuilder("COZY POND NATIVE PLAY\n");checks=0;
        try
        {
            Check(SaveManager.Instance==null,"No production save writer");var ponds=Object.FindObjectsByType<FishPond>(FindObjectsSortMode.None).OrderBy(p=>p.Level).ToArray();Check(ponds.Length==3,"All three levels instantiate");
            var fish=Resources.Load<ItemSO>("Items/Fish/Tilapia");Check(fish!=null,"Existing Tilapia item resolves");
            var records=FishPondService.Capture();foreach(var r in records){r.fish.Add(new FishPondFishData{itemId=fish.Id,assetName=fish.name,itemName=fish.itemName,sizeCm=20,weightKg=.3f,qualityStars=2});r.feedStock=1;}FishPondService.Restore(records);
            var actor=new GameObject("Audit inventory");var inventory=actor.AddComponent<Inventory>();
            foreach(var pond in ponds)
            {
                typeof(FishPond).GetField("inventory",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pond,inventory);
                var ext=pond.GetComponent<FishPondExterior>();Check(ext.IsSwimPoint(ext.SwimOrigin(0)),"Lv"+pond.Level+": fish in valid water");
                var maker=pond.GetComponentInChildren<FeedMaker>();Check(maker!=null&&maker.DedicatedFishFeed&&maker.HasDedicatedOutput,"Lv"+pond.Level+": feed maker initialized");
                pond.OpenPanel();Check(pond.IsPanelOpen,"Lv"+pond.Level+": pond UI opens");pond.ClosePanel();
                inventory.Add(fish,1,2,20,.3f);int index=inventory.slots.FindIndex(s=>s?.item==fish&&s.count>0),before=pond.FishCount;pond.AddFish(index);Check(pond.FishCount==before+1,"Lv"+pond.Level+": deposit fish");pond.TakeFish(pond.FishCount-1);Check(pond.FishCount==before,"Lv"+pond.Level+": withdraw fish");
            }
            var snapshot=FishPondService.Capture();string id=snapshot.First(r=>r.level==1).pondId;var first=ponds[0];Object.DestroyImmediate(first.gameObject);
            var replacement=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));replacement.name="FishPond_Lv1";replacement.GetComponent<FishPond>().Configure(id,1,Resources.Load<ItemSO>("Items/Fish/Fish Feed"));FishPondService.Restore(snapshot);
            Check(replacement.GetComponent<FishPond>().FishCount==1,"Replacing Lv1 visual and restoring records retains fish");Check(replacement.GetComponent<FishPond>().FeedStock==1,"Feed survives restore");
            report.AppendLine("POND_COZY_PLAY_PASS checks="+checks);
        }
        catch(Exception e){File.WriteAllText(Output+"/PlayError.txt",report+"\n"+e);Debug.LogException(e);}
        finally{File.WriteAllText(Output+"/PlayAudit.txt",report.ToString());EditorApplication.isPlaying=false;}
    }
}
#endif
