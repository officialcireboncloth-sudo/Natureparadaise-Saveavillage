#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Updates stand/bin art while retaining the existing gameplay component, identity and scene bindings.</summary>
public static class MarketPropsCozySetup
{
    const string Root="Assets/Nature  Paradaise", Output="ArtSource/MarketPropsCozy/UnityReview";
    static StringBuilder report; static int checks;
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);report.AppendLine("PASS "+message);checks++;}
    static Vector3 V(JToken p)=>new Vector3((float)p[0],(float)p[1],(float)p[2]);
    static bool Fixture=>Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
    static Bounds Measure(Mesh mesh,Matrix4x4 matrix)
    {
        var b=new Bounds(matrix.MultiplyPoint3x4(mesh.vertices[0]),Vector3.zero);
        foreach(var p in mesh.vertices)b.Encapsulate(matrix.MultiplyPoint3x4(p));return b;
    }
    static Mesh StoreMesh(Mesh mesh,string path)
    {
        var meshes=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
        var prior=meshes.FirstOrDefault(m=>m.name==mesh.name);
        if(prior!=null){EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;}
        if(meshes.Length==0&&!File.Exists(path))AssetDatabase.CreateAsset(mesh,path);else AssetDatabase.AddObjectToAsset(mesh,path);
        return mesh;
    }
    public static void Apply(string kind)
    {
        if(kind=="MarketStand" && File.Exists(Root+"/Source~/MarketCart/MarketCart_Manifest.json")){MarketCartCozySetup.Apply();return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply in Edit Mode.");
        report=new StringBuilder("COZY MARKET PROPS — "+kind+"\n");checks=0;Directory.CreateDirectory(Output);
        string model=Root+"/mesh/World/"+kind+"/"+kind+"_A.fbx";
        string runtime=Root+"/mesh/World/"+kind+"/"+kind+"_Cozy_Runtime.asset";
        string visualPath=Root+"/Prefabs/World/"+kind+"/"+kind+"Visual.prefab";
        string gameplayPath=Root+"/Prefabs/World/"+kind+".prefab";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/NP_CozySurfaces_01.mat");
        Check(mat!=null&&mat.GetTexture("_BaseMap")!=null,"Existing shared Cozy atlas material resolves");
        var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/MarketPropsCozy/"+kind+"_Manifest.json"));
        AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);
        var imp=(ModelImporter)AssetImporter.GetAtPath(model);
        imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;
        imp.importCameras=false;imp.importLights=false;imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
        imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kind+"_A-"+mat.name),mat);imp.SaveAndReimport();
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var frames=imported.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cozy")).ToDictionary(t=>t.name,t=>t.position);
        Check(new[]{"CozyOrigin","CozyRight","CozyForward","CozyUp"}.All(frames.ContainsKey),"Explicit export coordinate frame available");
        var matrices=Enumerable.Range(0,4).SelectMany(i=>new[]{1f,-1f}.Select(sign=>Matrix4x4.Rotate(Quaternion.Euler(0,i*90,0))*Matrix4x4.Scale(new Vector3(sign,1,1))));
        var matrix=matrices.OrderBy(m=>Vector3.Distance(m.MultiplyPoint3x4(frames["CozyRight"]),Vector3.right)+Vector3.Distance(m.MultiplyPoint3x4(frames["CozyForward"]),Vector3.forward)+Vector3.Distance(m.MultiplyPoint3x4(frames["CozyUp"]),Vector3.up)).First();
        Check(Vector3.Distance(matrix.MultiplyPoint3x4(frames["CozyRight"]),Vector3.right)<.001f&&Vector3.Distance(matrix.MultiplyPoint3x4(frames["CozyForward"]),Vector3.forward)<.001f,"Front and right remain unmirrored in Unity");
        var filters=imported.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name);
        var normalized=new Dictionary<string,Mesh>();
        foreach(var entry in spec["meshes"])
        {
            string name=(string)entry["name"];var f=filters[name];var transform=matrix*f.transform.localToWorldMatrix;
            var mesh=Object.Instantiate(f.sharedMesh);mesh.name=name;
            mesh.vertices=mesh.vertices.Select(transform.MultiplyPoint3x4).ToArray();
            mesh.normals=mesh.normals.Select(n=>transform.inverse.transpose.MultiplyVector(n).normalized).ToArray();
            float flip=transform.determinant<0?-1:1;
            mesh.tangents=mesh.tangents.Select(t=>{var v=transform.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*flip);}).ToArray();
            if(flip<0)for(int sub=0;sub<mesh.subMeshCount;sub++){var tri=mesh.GetTriangles(sub);for(int i=0;i<tri.Length;i+=3){int n=tri[i];tri[i]=tri[i+2];tri[i+2]=n;}mesh.SetTriangles(tri,sub);}
            mesh.RecalculateBounds();var expected=new Bounds();expected.SetMinMax(V(entry["min"]),V(entry["max"]));
            Check(Vector3.Distance(mesh.bounds.min,expected.min)<.002f&&Vector3.Distance(mesh.bounds.max,expected.max)<.002f,name+": native bounds match Blender metres");
            Check(mesh.triangles.Length/3==(int)entry["triangles"],name+": triangle count retained");
            Check(mesh.subMeshCount==1&&mesh.uv.All(u=>!float.IsNaN(u.x)&&u.x>=0&&u.x<=1&&u.y>=0&&u.y<=1),name+": one atlas slot and valid UVs");
            normalized[name]=StoreMesh(mesh,runtime);
        }
        bool bin=kind=="ShippingBin";float scale=bin?1:MarketStandArtSetup.VisualScale;
        var root=new GameObject(kind+"Visual");
        try
        {
            var visual=new GameObject("Cozy Atlas Model").transform;visual.SetParent(root.transform,false);visual.localScale=Vector3.one*scale;
            Transform pivot=null;
            if(bin){pivot=new GameObject("LidPivot_Unity").transform;pivot.SetParent(visual,false);pivot.localPosition=V(spec["hingeUnity"]);}
            var renderers=new List<Renderer>();
            foreach(var pair in normalized)
            {
                bool lid=pair.Key.Contains("_Lid_");var child=new GameObject(pair.Key).transform;child.SetParent(lid?pivot:visual,false);
                var mesh=pair.Value;
                if(lid)
                {
                    var copy=Object.Instantiate(mesh);copy.name=mesh.name+"_AtHinge";var hinge=V(spec["hingeUnity"]);copy.vertices=copy.vertices.Select(v=>v-hinge).ToArray();copy.RecalculateBounds();mesh=StoreMesh(copy,runtime);
                }
                child.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=child.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;renderers.Add(r);
            }
            var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(bin?.10f:.18f,renderers.Where(r=>r.name.EndsWith("LOD0")).ToArray()),new LOD(.015f,renderers.Where(r=>r.name.EndsWith("LOD1")).ToArray())});group.fadeMode=LODFadeMode.None;group.RecalculateBounds();
            if(bin)
            {
                var presentation=root.AddComponent<ShippingBinPresentation>();presentation.Configure(pivot);
                var so=new SerializedObject(presentation);so.FindProperty("openAngle").floatValue=(float)spec["openAngle"];so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root,visualPath);
        }
        finally{Object.DestroyImmediate(root);}
        var gameplay=PrefabUtility.LoadPrefabContents(gameplayPath);
        try
        {
            Component component=bin?(Component)gameplay.GetComponent<ShippingBin>():gameplay.GetComponent<MarketStand>();
            string state=EditorJsonUtility.ToJson(component);var position=gameplay.transform.localPosition;var rotation=gameplay.transform.localRotation;var oldScale=gameplay.transform.localScale;
            foreach(var t in gameplay.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Visual_")).ToArray())Object.DestroyImmediate(t.gameObject);
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(visualPath),gameplay.transform);art.name=bin?"Visual_AtlasShippingBin":"Visual_AtlasMarketStand";
            var collider=gameplay.GetComponent<BoxCollider>();
            collider.center=bin?new Vector3(0,.44f,0):new Vector3(0,.54f,0)*scale;
            collider.size=bin?new Vector3(1.77f,.88f,1.06f):new Vector3(3.30f,1.08f,1.4f)*scale;
            Check(EditorJsonUtility.ToJson(component)==state,"Gameplay settings, identity, inventory and stock untouched");
            Check(gameplay.transform.localPosition==position&&gameplay.transform.localRotation==rotation&&gameplay.transform.localScale==oldScale,"Root placement and scale unchanged");
            PrefabUtility.SaveAsPrefabAsset(gameplay,gameplayPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(gameplay);}
        imp.isReadable=false;imp.SaveAndReimport();AssetDatabase.SaveAssets();
        if(Application.isBatchMode&&Fixture)ValidateAndRender(kind,spec);
        File.WriteAllText(Output+"/"+kind+"_Audit.txt",report+$"CHECKS {checks}; COZY_PROP_APPLY_PASS\n");
    }
    static void ValidateAndRender(string kind,JObject spec)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/World/"+kind+".prefab"));
        bool bin=kind=="ShippingBin";
        Check(root.GetComponentsInChildren<LODGroup>().Length==1,"One LOD owner; no nested duplicate registration");
        var group=root.GetComponentInChildren<LODGroup>();group.ForceLOD(0);
        var all=root.GetComponentsInChildren<MeshRenderer>();Check(all.Length==(bin?4:2),"Only intended near/far meshes remain");
        Check(all.All(r=>r.sharedMaterials.Length==1&&r.sharedMaterial.name=="NP_CozySurfaces_01"&&!ShaderUtil.ShaderHasError(r.sharedMaterial.shader)),"Every renderer uses the compiled shared Cozy shader/atlas");
        Check(group.GetLODs()[1].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length)<group.GetLODs()[0].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length),"Distance LOD lowers geometry cost");
        var bounds=new Bounds();bool first=true;
        foreach(var r in group.GetLODs()[0].renderers){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        Check(bounds.size.x<(bin?1.8f:2.9f)&&bounds.size.y<(bin?1.0f:2.3f),"Compact world dimensions retained: "+bounds.size);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.76f,.81f,.88f);RenderSettings.ambientEquatorColor=new Color(.53f,.55f,.57f);RenderSettings.ambientGroundColor=new Color(.35f,.32f,.28f);RenderSettings.fog=false;
        var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(RenderSettings.ambientEquatorColor.linear*.7f);sh.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor.linear,.45f);sh.AddDirectionalLight(Vector3.down,RenderSettings.ambientGroundColor.linear,.35f);RenderSettings.ambientProbe=sh;
        var sun=new GameObject("Review key").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.05f;sun.color=new Color(1,.96f,.89f);sun.transform.rotation=Quaternion.Euler(48,160,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.70f;sun.shadowBias=.02f;sun.shadowNormalBias=.08f;RenderSettings.sun=sun;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.position=Vector3.down*.022f;var floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.SetColor("_BaseColor",new Color(.78f,.76f,.69f));floorMat.SetFloat("_Smoothness",0);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
        var camera=new GameObject("Review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=bin?1.16f:1.8f;camera.farClipPlane=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.78f,.72f);
        Vector3 target=bin?new Vector3(0,.65f,0):new Vector3(0,1.10f,0);
        camera.transform.position=target+(bin?new Vector3(2.5f,1.55f,3.8f):new Vector3(3.0f,1.7f,5.5f));camera.transform.LookAt(target);
        ShaderUtil.allowAsyncCompilation=false;Capture(camera,kind+"_Closed");
        if(bin)
        {
            var presentation=root.GetComponentInChildren<ShippingBinPresentation>();var body=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.Contains("Body_LOD0"));var before=body.bounds;
            presentation.Configure(presentation.LidPivot);presentation.enabled=false;presentation.SetOpen(true);Physics.SyncTransforms();
            Check(Mathf.Abs(Quaternion.Angle(Quaternion.identity,presentation.LidPivot.localRotation)-72)<.01f,"Lid opens 72 degrees about the rear hinge");
            var lid=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.Contains("Lid_LOD0"));Check(lid.bounds.max.y>1.45f,"Open lid clears the chest interior");
            Check(before==body.bounds,"Opening lid leaves body stationary");group.RecalculateBounds();
            camera.orthographicSize=1.40f;target=new Vector3(0,.93f,0);camera.transform.position=target+new Vector3(2.5f,1.55f,3.8f);camera.transform.LookAt(target);Capture(camera,kind+"_Open");
            presentation.SetOpen(false);Check(Quaternion.Angle(presentation.LidPivot.localRotation,Quaternion.identity)<.01f,"Lid closes to rest without positional drift");
        }
        group.ForceLOD(1);Capture(camera,kind+"_LOD1");
        Object.DestroyImmediate(root);Object.DestroyImmediate(floorMat);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    static void Capture(Camera camera,string name)
    {
        const int w=1200,h=1050;camera.aspect=(float)w/h;var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if(!Fixture)throw new InvalidOperationException("Batch review must use the isolated fixture.");
        try
        {
            Apply("MarketStand");Apply("ShippingBin");
            var path=Root+"/Map/Scenes/World/Map.unity";var before=File.ReadAllText(path);EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            Check(Object.FindObjectsByType<MarketStand>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(s=>s.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&(f.sharedMesh.name=="MarketStand_Body_LOD0"||f.sharedMesh.name=="MarketCart_Cart_LOD0"))),"Actual Map stand resolves the new art");
            Check(Object.FindObjectsByType<ShippingBin>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(s=>s.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&f.sharedMesh.name=="ShippingBin_Body_LOD0")),"Actual Map shipping bin resolves the new art");
            Check(File.ReadAllText(path)==before,"Map scene was not rewritten");File.WriteAllText(Output+"/SceneBindings_Audit.txt",report+"SCENE_BINDINGS_PASS\n");
            if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");EditorApplication.Exit(0);
        }
        catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",report+"\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
