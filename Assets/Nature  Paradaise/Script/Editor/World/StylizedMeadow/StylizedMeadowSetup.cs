using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Build the custom meadow kit and an isolated native Terrain preview.</summary>
public static class StylizedMeadowSetup
{
    const string Root="Assets/Nature  Paradaise/Art/StylizedMeadow";
    const string ScenePath=Root+"/Scenes/StylizedMeadowPreview.unity";
    static readonly string[] Names={"Grass_Rosette_A","Grass_Rosette_B","Grass_Rosette_C","Bush_Round_A","Bush_Round_B","Flowers_White","Flowers_Yellow"};

    [MenuItem("Nature Paradise/World/Stylized Meadow/Build Assets and Preview")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Build in Edit Mode.");
        AssetDatabase.Refresh();
        foreach(var name in Names)
        foreach(var suffix in new[]{"","_Low"})
        {
            var importer=AssetImporter.GetAtPath(Root+"/Models/"+name+suffix+".fbx") as ModelImporter;
            if(importer==null)throw new FileNotFoundException(name+suffix);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.importNormals=ModelImporterNormals.Import;importer.isReadable=true;importer.SaveAndReimport();
        }
        foreach(var name in new[]{"GrassGround_Albedo","LeafPalette_Albedo"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+name+".png");
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;
            importer.wrapMode=name.StartsWith("Grass")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var shader=Shader.Find("Nature Paradise/Meadow Vegetation");
        if(shader==null)throw new InvalidOperationException("Meadow shader was not imported.");
        var material=Get<Material>(Root+"/Materials/MeadowLeaves.mat",()=>new Material(shader));
        material.shader=shader;material.enableInstancing=true;material.SetTexture("_TextureSample",Load<Texture2D>("Textures/LeafPalette_Albedo.png"));
        material.SetColor("_Color",Color.white);material.SetColor("_ShadowColor",new Color(.52f,.69f,.48f));
        material.SetFloat("_ShadeStrength",.32f);material.SetFloat("_ShadeSoftness",.22f);material.SetFloat("_CastShadowStrength",.7f);
        material.SetFloat("_Brightness",1.02f);material.SetFloat("_AmbientInfluence",.18f);material.SetFloat("_RimStrength",.02f);material.SetFloat("_SkinEnabled",0);
        material.SetFloat("_WindStrength",.025f);material.SetFloat("_WindSpeed",1.4f);EditorUtility.SetDirty(material);
        var report=new StringBuilder("Stylized meadow: metre-scale authored meshes, bottom pivots, one shared material.\n");
        foreach(var name in Names)
        {
            var high=Mesh(name);var low=Mesh(name+"_Low");
            if(high.bounds.size.y>1||high.bounds.size.x>2)throw new InvalidOperationException("Unexpected FBX scale/axis: "+name+" "+high.bounds);
            var root=new GameObject(name);var lod=root.AddComponent<LODGroup>();
            var a=Child(root,high,material,"LOD0");var b=Child(root,low,material,"LOD1");
            lod.SetLODs(new[]{new LOD(.18f,new Renderer[]{a}),new LOD(.035f,new Renderer[]{b})});lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            var detail=new GameObject(name+" Detail");detail.AddComponent<MeshFilter>().sharedMesh=low;
            var renderer=detail.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(detail,Root+"/Prefabs/"+name+"_Detail.prefab");UnityEngine.Object.DestroyImmediate(detail);
            report.AppendLine($"{name}: LOD0={high.triangles.Length/3} triangles, LOD1={low.triangles.Length/3}, bounds={high.bounds.size}");
        }
        var layer=Get<TerrainLayer>(Root+"/Terrain/MeadowGrass.terrainlayer",()=>new TerrainLayer());
        layer.diffuseTexture=Load<Texture2D>("Textures/GrassGround_Albedo.png");layer.tileSize=new Vector2(18,18);layer.normalMapTexture=null;layer.smoothness=0;layer.metallic=0;EditorUtility.SetDirty(layer);
        var profile=Get<TerrainVegetationProfile>(Root+"/Profiles/MeadowVegetation.asset",()=>ScriptableObject.CreateInstance<TerrainVegetationProfile>());
        profile.seed=918;profile.drawDistance=50;profile.densityMultiplier=1;profile.initialResolution=128;profile.patchResolution=16;profile.rules.Clear();
        AddRule(profile,layer,"Short broad-leaf grass",.075f,Names.Take(3).ToArray(),new Vector2(2f,2.8f),new Vector2(1.8f,2.3f));
        AddRule(profile,layer,"Sparse round bushes",.0035f,Names.Skip(3).Take(2).ToArray(),new Vector2(1.4f,1.9f),new Vector2(1.35f,1.75f));
        AddRule(profile,layer,"White flowers",.012f,new[]{Names[5]},new Vector2(1.65f,2.05f));
        AddRule(profile,layer,"Yellow flowers",.008f,new[]{Names[6]},new Vector2(1.65f,2.05f));
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        CreatePreview(layer,profile);
        var errors=ShaderUtil.GetShaderMessages(shader);foreach(var error in errors)report.AppendLine(error.severity+": "+error.message);
        File.WriteAllText("ArtSource/StylizedMeadow/Previews/UnityImportAudit.txt",report.ToString());
        Debug.Log("[MEADOW] Asset kit and isolated preview ready. "+ScenePath);
    }
    static Mesh Mesh(string name)
    {
        var filter=Load<GameObject>("Models/"+name+".fbx").GetComponentInChildren<MeshFilter>();
        // Terrain details draw a mesh directly and cannot apply FBX node transforms.
        // Bake the complete imported transform into project-owned meshes once.
        var baked=UnityEngine.Object.Instantiate(filter.sharedMesh);baked.name=name;
        var transform=filter.transform.localToWorldMatrix;
        baked.vertices=baked.vertices.Select(transform.MultiplyPoint3x4).ToArray();
        baked.normals=baked.normals.Select(n=>transform.inverse.transpose.MultiplyVector(n).normalized).ToArray();
        if(transform.determinant<0){var indices=baked.triangles;for(int i=0;i<indices.Length;i+=3){int temp=indices[i];indices[i]=indices[i+1];indices[i+1]=temp;}baked.triangles=indices;}
        baked.RecalculateBounds();
        if(!AssetDatabase.IsValidFolder(Root+"/Meshes"))AssetDatabase.CreateFolder(Root,"Meshes");
        string path=Root+"/Meshes/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(baked,path);return baked;}
        EditorUtility.CopySerialized(baked,existing);UnityEngine.Object.DestroyImmediate(baked);EditorUtility.SetDirty(existing);return existing;
    }
    static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(Root+"/"+path)??throw new FileNotFoundException(path);
    static T Get<T>(string path,Func<T> create)where T:UnityEngine.Object
    {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null){asset=create();AssetDatabase.CreateAsset(asset,path);}return asset;}
    static MeshRenderer Child(GameObject parent,Mesh mesh,Material material,string name)
    {var go=new GameObject(name);go.transform.SetParent(parent.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;return r;}
    static void AddRule(TerrainVegetationProfile profile,TerrainLayer layer,string name,float density,string[] models,Vector2 scales,Vector2? heights=null)
    {
        var rule=new TerrainVegetationProfile.LayerRule{name=name,terrainLayer=layer,enabled=true,useWorldDensity=true,density=density,minimumPaintWeight=.72f,maximumSlope=32,clusterSize=4,clusterVariation=.12f};
        foreach(var model in models)rule.variants.Add(new TerrainVegetationProfile.Variant{prefab=Load<GameObject>("Prefabs/"+model+"_Detail.prefab"),weight=1,widthScale=scales,heightScale=heights??scales});
        profile.rules.Add(rule);
    }
    static void CreatePreview(TerrainLayer layer,TerrainVegetationProfile profile)
    {
        if(!AssetDatabase.IsValidFolder(Root+"/Scenes"))AssetDatabase.CreateFolder(Root,"Scenes");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var data=Get<TerrainData>(Root+"/Terrain/MeadowPreview.asset",()=>new TerrainData());
            data.heightmapResolution=33;data.size=new Vector3(12,1,12);data.alphamapResolution=128;data.terrainLayers=new[]{layer};
            data.SetHeights(0,0,new float[33,33]);var paint=new float[128,128,1];for(int z=0;z<128;z++)for(int x=0;x<128;x++)paint[z,x,0]=1;data.SetAlphamaps(0,0,paint);
            data.SetDetailResolution(128,16);
            var origin=new Vector3(-2000,0,-2000);
            var ground=Terrain.CreateTerrainGameObject(data);ground.layer=30;ground.name="Meadow Terrain — paint grass layer to scatter";ground.transform.position=origin+new Vector3(-6,0,-6);
            var vegetation=ground.AddComponent<TerrainLayerVegetation>();vegetation.profile=profile;vegetation.Rebuild();
            var light=new GameObject("Soft Daylight").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.98f,.9f);light.intensity=1.2f;light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.transform.rotation=Quaternion.Euler(65,-35,0);light.cullingMask=1<<30;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.65f,.78f,.86f);RenderSettings.ambientEquatorColor=new Color(.55f,.64f,.50f);RenderSettings.ambientGroundColor=new Color(.32f,.38f,.28f);
            var camera=new GameObject("Meadow Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=8.4f;
            camera.transform.position=origin+new Vector3(10,14,-15);camera.transform.LookAt(origin);camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.81f,.72f);camera.allowHDR=true;camera.cullingMask=1<<30;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.GetUniversalAdditionalCameraData().volumeLayerMask=1<<30;
            var source=Resources.Load<VolumeProfile>("World/Garden Post Processing");if(source!=null){var volume=new GameObject("World Post Processing").AddComponent<Volume>();volume.gameObject.layer=30;volume.isGlobal=true;volume.sharedProfile=source;}
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            var otherLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.gameObject.scene!=scene&&l.enabled).ToArray();
            try{foreach(var other in otherLights)other.enabled=false;Render(camera,"ArtSource/StylizedMeadow/Previews/MeadowKit_Unity.png");}
            finally{foreach(var other in otherLights)if(other!=null)other.enabled=true;}
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(prior.isLoaded)SceneManager.SetActiveScene(prior);}
    }
    static void Render(Camera camera,string path)
    {
        var rt=new RenderTexture(1440,1000,24);rt.Create();var old=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
