#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

/// <summary>Player Barn reference facade; shared atlas pages, preserved site/door/save identities.</summary>
public static class BarnExteriorCozySetup
{
    const string Root="Assets/Nature  Paradaise", Models=Root+"/mesh/Buildings/BarnExteriorCozy/", Textures=Root+"/Textures/Shared/Cozy/";
    const string Output="ArtSource/BarnExteriorCozy/UnityReview";
    static StringBuilder report;
    static int checks;
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;report.AppendLine("PASS "+message);}
    static bool Fixture=>Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
    static Vector3 V(JToken p)=>new((float)p[0],(float)p[1],(float)p[2]);
    static Mesh Store(Mesh mesh,string path)
    {
        var prior=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
        if(prior!=null){EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;}
        if(File.Exists(path))AssetDatabase.AddObjectToAsset(mesh,path);else AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Material EmblemMaterial()
    {
        string path=Textures+"NP_CozyEmblems_01_BaseColor.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.alphaIsTransparency=true;
        imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.anisoLevel=1;imp.maxTextureSize=512;imp.isReadable=false;
        foreach(string platform in new[]{"Android","iPhone"}){var settings=imp.GetPlatformTextureSettings(platform);settings.overridden=true;settings.maxTextureSize=512;settings.format=TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(settings);}imp.SaveAndReimport();
        var probe=new Texture2D(2,2);ImageConversion.LoadImage(probe,File.ReadAllBytes(path));
        Check(probe.width==512&&probe.height==256,"Generated cow emblem atlas is mobile-sized 512 x 256");
        Check(probe.GetPixel(0,0).a<.01f&&probe.GetPixel(256,128).a>.9f,"Generated emblem preserves transparent exterior and opaque cow");Object.DestroyImmediate(probe);
        string materialPath=Root+"/Material/Shared/NP_CozyEmblems_01.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(m==null){m=new Material(Shader.Find("Nature Paradise/Cozy Atlas")){name="NP_CozyEmblems_01"};AssetDatabase.CreateAsset(m,materialPath);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));m.SetTexture("_BumpMap",null);m.SetTexture("_MetallicGlossMap",null);m.SetColor("_BaseColor",Color.white);
        m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.SetFloat("_UseNormal",0);m.SetFloat("_UseMask",0);m.SetFloat("_Cull",0);
        m.SetFloat("_Wrap",.12f);m.SetFloat("_AmbientStrength",1);m.SetFloat("_Saturation",1);m.SetFloat("_Smoothness",.1f);
        m.shaderKeywords=new[]{"_ALPHATEST_ON"};m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    [MenuItem("Nature Paradise/Art/Apply Cozy Player Barn Exterior")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before applying exterior art.");
        report=new StringBuilder("COZY PLAYER BARN EXTERIOR\n");checks=0;Directory.CreateDirectory(Output);
        string definitionPath=Root+"/Resources/Buildings/Barn Building.asset";byte[] catalog=File.ReadAllBytes(definitionPath);
        var emblem=EmblemMaterial();
        var materials=new[]{"NP_CozySurfaces_01","NP_CozyFoliage_01","NP_CozyGlow_01","NP_CozySurfaces_02"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/"+n+".mat")).Concat(new[]{emblem}).ToDictionary(m=>m.name);
        Check(materials.Values.All(m=>m!=null&&m.GetTexture("_BaseMap")!=null),"Five shared materials resolve including generated cow decal");
        var solid=materials["NP_CozySurfaces_02"];
        var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/BarnExteriorCozy/BarnExterior_Cozy_Manifest.json"));
        var meshes=new Mesh[3];var slots=new Material[3][];
        for(int lod=0;lod<3;lod++)
        {
            string name="BarnExterior_Cozy_LOD"+lod,path=Models+name+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;
            imp.importCameras=false;imp.importLights=false;imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
            imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var m in materials.Values){imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),m);imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name+"-"+m.name),m);}imp.SaveAndReimport();
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);var f=imported.GetComponentsInChildren<MeshFilter>(true).Single();
            var markers=imported.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cozy")).ToDictionary(t=>t.name,t=>t.position);
            Check(new[]{"CozyOrigin","CozyRight","CozyForward","CozyUp"}.All(markers.ContainsKey),"LOD"+lod+": export axes present");
            var candidates=Enumerable.Range(0,4).SelectMany(i=>new[]{1f,-1f}.Select(sign=>Matrix4x4.Rotate(Quaternion.Euler(0,i*90,0))*Matrix4x4.Scale(new Vector3(sign,1,1))));
            var matrix=candidates.OrderBy(m=>Vector3.Distance(m.MultiplyPoint3x4(markers["CozyRight"]),Vector3.right)+Vector3.Distance(m.MultiplyPoint3x4(markers["CozyForward"]),Vector3.forward)+Vector3.Distance(m.MultiplyPoint3x4(markers["CozyUp"]),Vector3.up)).First();
            Check(Vector3.Distance(matrix.MultiplyPoint3x4(markers["CozyRight"]),Vector3.right)<.001f&&Vector3.Distance(matrix.MultiplyPoint3x4(markers["CozyForward"]),Vector3.forward)<.001f,"LOD"+lod+": unmirrored right and front axes");
            var transform=matrix*f.transform.localToWorldMatrix;var mesh=Object.Instantiate(f.sharedMesh);mesh.name=name;
            mesh.vertices=mesh.vertices.Select(transform.MultiplyPoint3x4).ToArray();mesh.normals=mesh.normals.Select(n=>transform.inverse.transpose.MultiplyVector(n).normalized).ToArray();
            float flip=transform.determinant<0?-1:1;
            mesh.tangents=mesh.tangents.Select(t=>{var v=transform.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*flip);}).ToArray();
            if(flip<0)for(int sub=0;sub<mesh.subMeshCount;sub++){var tri=mesh.GetTriangles(sub);for(int i=0;i<tri.Length;i+=3){int n=tri[i];tri[i]=tri[i+2];tri[i+2]=n;}mesh.SetTriangles(tri,sub);}
            mesh.RecalculateBounds();var entry=spec["lods"][lod];
            Check(Vector3.Distance(mesh.bounds.min,V(entry["min"]))<.003f&&Vector3.Distance(mesh.bounds.max,V(entry["max"]))<.003f,"LOD"+lod+": native mesh matches Blender bounds");
            Check(mesh.triangles.Length/3==(int)entry["triangles"],"LOD"+lod+": triangulation retained");
            Check(mesh.uv.Length==mesh.vertexCount&&mesh.uv.All(u=>!float.IsNaN(u.x)&&u.x>=0&&u.x<=1&&u.y>=0&&u.y<=1),"LOD"+lod+": complete atlas UVs");
            var remaps=f.GetComponent<MeshRenderer>().sharedMaterials;Check(remaps.All(m=>m!=null&&materials.ContainsKey(m.name)),"LOD"+lod+": no generated per-object material");
            if(Fixture)ValidateRoof(mesh,lod);
            slots[lod]=remaps.Select(m=>materials[m.name]).ToArray();meshes[lod]=Store(mesh,Models+"BarnExterior_Cozy_Runtime.asset");
        }
        Check(meshes[0].triangles.Length>meshes[1].triangles.Length&&meshes[1].triangles.Length>meshes[2].triangles.Length&&meshes[2].triangles.Length/3<6000,"Decreasing LODs and far mesh below 6000 triangles");
        for(int level=1;level<=5;level++)
        {
            string path=Root+"/Prefabs/Barn/Exterior/BarnExterior_Lv"+level+".prefab";
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long id);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var authored=root.GetComponent<BarnExteriorAuthoring>();Check(authored!=null,"Lv"+level+": existing exterior authoring");
                var entrance=authored.Entrance;var ep=entrance.localPosition;var er=entrance.localRotation;var scale=root.transform.localScale;
                var colliders=root.GetComponentsInChildren<Collider>(true);var colliderData=colliders.Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
                var model=authored.ModelRoot;var mp=model.localPosition;var ms=model.localScale;
                var bell=authored.Bell;Check(bell!=null&&bell.SwingPivot!=null&&bell.Approach!=null&&bell.PromptAnchor!=null,"Lv"+level+": modeled bell and interaction markers retained");
                foreach(Transform t in model.Cast<Transform>().ToArray())if(t!=bell.transform)Object.DestroyImmediate(t.gameObject);
                model.localRotation=Quaternion.identity;
                bell.transform.localPosition=new Vector3(-1.33f,0,-1.65f);bell.transform.localRotation=Quaternion.Euler(0,180,0);
                bell.Approach.position=model.TransformPoint(new Vector3(-4.35f,0,1.45f));bell.Approach.rotation=model.rotation*Quaternion.Euler(0,90,0);
                bell.PromptAnchor.position=model.TransformPoint(new Vector3(-3.88f,1.68f,1.45f));
                // Align the retained collider component with the new closed, centered shell.
                authored.BuildingCollision.center=mp+Vector3.Scale(new Vector3(0,1.62f,0),ms);
                authored.BuildingCollision.size=Vector3.Scale(new Vector3(7.12f,3.24f,5.52f),ms);
                var group=model.GetComponent<LODGroup>()??model.gameObject.AddComponent<LODGroup>();var lods=new LOD[3];
                for(int lod=0;lod<3;lod++)
                {
                    var child=new GameObject(meshes[lod].name).transform;child.SetParent(model,false);
                    child.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[lod];var r=child.gameObject.AddComponent<MeshRenderer>();r.sharedMaterials=slots[lod];r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
                    lods[lod]=new LOD(lod==0?.20f:lod==1?.085f:.012f,new[]{r});
                }
                group.fadeMode=LODFadeMode.None;group.animateCrossFading=false;group.SetLODs(lods);group.RecalculateBounds();
                Check(root.transform.localScale==scale&&model.localPosition==mp&&model.localScale==ms,"Lv"+level+": authored metre scale and site offset preserved");
                Check(entrance.localPosition==ep&&entrance.localRotation==er,"Lv"+level+": existing doorway position and rotation retained");
                Check(root.GetComponentsInChildren<Collider>(true).SequenceEqual(colliders),"Lv"+level+": existing collider component identities retained");
                Check(Vector3.Distance(bell.Approach.position,entrance.position)>1,"Lv"+level+": bell and entrance interaction zones separated");
                Check(!authored.BuildingCollision.bounds.Contains(bell.Approach.position+Vector3.up*.7f),"Lv"+level+": bell approach outside solid building");
                Check(!authored.BuildingCollision.bounds.Contains(entrance.position+Vector3.up),"Lv"+level+": approach anchor outside body collision");
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved,out string afterGuid,out long afterId);
                Check(guid==afterGuid&&id==afterId,"Lv"+level+": existing prefab GUID and root ID retained");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Check(File.ReadAllBytes(definitionPath).SequenceEqual(catalog),"Catalog costs, duration, footprint, capacities and GUID bindings unchanged");
        var messages=ShaderUtil.GetShaderMessages(solid.shader);Check(!messages.Any(m=>m.severity.ToString()=="Error"),"Cozy shader has no reported compilation errors");
        if(Fixture){Review();ValidateRestoration();}
        report.AppendLine("BARN_EXTERIOR_COZY_PASS checks="+checks+"; isolated native editor import, prefab and render verification; no production save/scene writes.");
        File.WriteAllText(Output+"/Audit.txt",report.ToString());
    }
    static void ValidateRoof(Mesh mesh,int lod)
    {
        var probe=new GameObject("Temporary roof coverage probe");var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
        bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=false;
        try
        {
            Physics.SyncTransforms();bool covered=true;
            foreach(float x in new[]{-3.2f,-2.5f,-1f,1f,2.5f,3.2f})
            foreach(float z in new[]{-2.8f,0f,2.8f})
                covered&=collider.Raycast(new Ray(new Vector3(x,10,z),Vector3.down),out var hit,10)&&hit.point.y>3.1f&&hit.normal.y>.3f;
            Check(covered,"LOD"+lod+": 18 roof samples closed and outward-facing, including both eaves");
        }
        finally{Physics.queriesHitBackfaces=previous;Object.DestroyImmediate(probe);}
    }
    static void Review()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Check(SaveManager.Instance==null,"Review has no production SaveManager");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Barn/Exterior/BarnExterior_Lv1.prefab");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var group=model.GetComponentInChildren<LODGroup>();group.ForceLOD(0);
        var body=model.GetComponent<BarnExteriorAuthoring>();Physics.SyncTransforms();
        var bell=body.Bell;bell.Ring();Check(bell.IsRinging&&bell.SwingPivot.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled),"Authored bell still rings with a visible modeled mesh");bell.enabled=false;
        var actor=new GameObject("Player capsule review").AddComponent<CharacterController>();actor.height=PlayerDimensions.Height;actor.radius=PlayerDimensions.Radius;actor.center=Vector3.up*actor.height/2;actor.skinWidth=PlayerDimensions.SkinWidth;actor.stepOffset=PlayerDimensions.StepOffset;
        actor.transform.position=body.Entrance.position+body.Entrance.forward*1.0f;Physics.SyncTransforms();
        for(int i=0;i<20;i++)actor.Move(-body.Entrance.forward*.04f);
        Check(Vector3.Distance(actor.transform.position,body.Entrance.position)<.4f,"Player capsule can reach the preserved door approach");Object.DestroyImmediate(actor.gameObject);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Review ground";ground.transform.localScale=Vector3.one*8;ground.transform.position=Vector3.down*.03f;
        var floor=new Material(Shader.Find("Universal Render Pipeline/Lit"));floor.color=new Color(.32f,.41f,.16f);ground.GetComponent<Renderer>().sharedMaterial=floor;
        var key=new GameObject("Review sun",typeof(Light)).GetComponent<Light>();key.type=LightType.Directional;key.intensity=1.15f;key.color=new Color(1,.98f,.94f);key.shadows=LightShadows.Soft;key.shadowStrength=.72f;key.transform.rotation=Quaternion.Euler(50,-145,0);RenderSettings.sun=key;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.79f,.85f,.92f);RenderSettings.ambientEquatorColor=new Color(.56f,.59f,.62f);RenderSettings.ambientGroundColor=new Color(.36f,.34f,.30f);RenderSettings.fog=false;
        var camera=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.62f,.68f,.49f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.GetUniversalAdditionalCameraData().renderShadows=true;
        foreach(var view in new[]{("Front",32f,180f),("ThreeQuarter",32f,205f),("Rear",30f,25f)})
        {group.ForceLOD(0);Render(model,camera,view.Item2,view.Item3,Output+"/Barn_"+view.Item1+".png");}
        for(int lod=1;lod<3;lod++){group.ForceLOD(lod);Render(model,camera,32,180,Output+"/Barn_LOD"+lod+".png");}
        group.ForceLOD(0);Render(model,camera,0,180,Output+"/Barn_Emblem.png",body.ModelRoot.TransformPoint(new Vector3(0,4.97f,3.05f)),1.1f*body.ModelRoot.lossyScale.y);
        Object.DestroyImmediate(model);Object.DestroyImmediate(ground);Object.DestroyImmediate(floor);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(key.gameObject);
    }
    static void Render(GameObject model,Camera camera,float pitch,float yaw,string path,Vector3? focus=null,float focusSize=0)
    {
        var group=model.GetComponentInChildren<LODGroup>();var renderers=group.GetLODs()[0].renderers;
        var b=renderers[0].bounds;camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);camera.transform.position=b.center-camera.transform.forward*20;
        var corners=from x in new[]{-1f,1f} from y in new[]{-1f,1f} from z in new[]{-1f,1f} select camera.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
        var p=corners.ToArray();float left=p.Min(v=>v.x),right=p.Max(v=>v.x),bottom=p.Min(v=>v.y),top=p.Max(v=>v.y);
        camera.transform.position+=camera.transform.right*((left+right)/2)+camera.transform.up*((bottom+top)/2);camera.orthographicSize=Mathf.Max((top-bottom)/2,(right-left)/2)*1.10f;
        if(focus.HasValue){camera.transform.position=focus.Value-camera.transform.forward*10;camera.orthographicSize=focusSize;}
        var rt=new RenderTexture(1200,1200,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,1200,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
    }
    static void ValidateRestoration()
    {
        var root=new GameObject("Restored Barn fixture");
        try
        {
            var site=root.AddComponent<PropertySite>();Invoke(site,"Awake");var home=root.AddComponent<AnimalHome>();home.site=site;
            var barrier=root.AddComponent<BoxCollider>();var portal=root.AddComponent<BarnInterior>();portal.ConfigureScene(home,root.transform,barrier);
            for(int level=1;level<=4;level++)foreach(float yaw in new[]{0f,37f,90f,180f,270f})
            {
                root.transform.SetPositionAndRotation(new Vector3(25,-300,35),Quaternion.Euler(0,yaw,0));
                Invoke(site,"Restore",new PropertySiteSaveData{buildingId="building.barn",state=BuildingConstructionState.Completed,currentLevel=level,unlocked=true});
                Invoke(portal,"RefreshExteriorAccess");Invoke(portal,"ResolveInteriorDestination");Physics.SyncTransforms();
                var authored=root.GetComponentInChildren<BarnExteriorAuthoring>();
                Check(authored!=null&&authored.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("BarnExterior_Cozy")),"Restore Lv"+level+" yaw "+yaw+": new art resolves through saved building ID");
                Check(home.Kind==AnimalHousingKind.Barn&&home.door==authored.Entrance&&home.Capacity>0,"Restore Lv"+level+" yaw "+yaw+": animal kind, capacity and doorway resolve");
                var point=home.Entry+Vector3.up*.7f;
                Check(Vector3.Distance(authored.BuildingCollision.ClosestPoint(point),point)>.1f&&!barrier.enabled,"Restore Lv"+level+" yaw "+yaw+": door approach clear of body and legacy blockers");
                Check((string)portal.GetType().GetField("interiorSceneName",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(portal)=="BarnInterior","Restore Lv"+level+" yaw "+yaw+": correct interior scene");
            }
        }
        finally{Object.DestroyImmediate(root);}
    }
    static void Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
    public static void RunBatch()
    {
        if(!Fixture)throw new InvalidOperationException("Use isolated review project.");
        try{Apply();if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");EditorApplication.Exit(0);}
        catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",report+"\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
