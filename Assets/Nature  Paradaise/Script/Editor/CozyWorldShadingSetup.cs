#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Shared atlas shading and restrained outdoor grading. Explicit editor operation only.</summary>
public static class CozyWorldShadingSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Output = "ArtSource/CozyShadingReview";
    public static void ConfigureMaterials()
    {
        var shader = Shader.Find("Nature Paradise/Cozy Atlas");
        if (shader == null) throw new InvalidOperationException("Cozy Atlas shader missing.");
        foreach (string name in new[] { "NP_CozySurfaces_01", "NP_CozyFoliage_01" })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/Shared/" + name + ".mat");
            if (mat == null) throw new InvalidOperationException(name + " missing.");
            bool foliage = name.Contains("Foliage");
            mat.shader = shader;
            mat.shaderKeywords = foliage ? new[] { "_ALPHATEST_ON" } : new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP" };
            mat.SetFloat("_AlphaClip", foliage ? 1 : 0);
            mat.SetFloat("_UseNormal", foliage ? 0 : 1); mat.SetFloat("_UseMask", foliage ? 0 : 1);
            mat.SetFloat("_BumpScale", .55f); mat.SetFloat("_Smoothness", 1);
            mat.SetFloat("_Wrap", foliage ? .22f : .12f);
            mat.SetFloat("_Saturation", .97f); mat.SetFloat("_AmbientStrength", 1);
            mat.SetFloat("_Cull", foliage ? 0 : 2); mat.SetFloat("_Cutoff", .42f);
            mat.SetOverrideTag("RenderType", foliage ? "TransparentCutout" : "Opaque");
            mat.renderQueue = foliage ? 2450 : 2000; mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }
        // The existing glow material retains emission, including the interior time/weather property block.
    }
    [MenuItem("Nature Paradise/World/Apply Cozy Atlas Shading")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply in Edit Mode.");
        ConfigureMaterials();
        var p = Resources.Load<GardenLightingProfile>("World/Garden Lighting");
        p.sunIntensity = 1.15f; p.sunColor = new Color(1,.98f,.94f);
        p.shadowStrength = .72f; p.noonShadowStrength = .76f;
        p.ambientSky = new Color(.76f,.81f,.88f); p.ambientEquator = new Color(.53f,.55f,.57f); p.ambientGround = new Color(.35f,.32f,.28f);
        p.morningSun = new Color(1,.94f,.83f); p.morningSky = new Color(.81f,.83f,.84f);
        p.morningEquator = new Color(.58f,.56f,.52f); p.morningGround = new Color(.37f,.34f,.28f);
        p.noonSky = new Color(.79f,.85f,.92f); p.noonEquator = new Color(.56f,.59f,.62f); p.noonGround = new Color(.36f,.34f,.30f);
        p.noonSunMultiplier = 1.08f; p.noonElevation = 68;
        p.noonBloomIntensity = .10f; p.noonBloomThreshold = 1.2f; p.noonExposureBoost = .02f;
        EditorUtility.SetDirty(p);
        var volume = p.postProcessing;
        if (volume.TryGet<ColorAdjustments>(out var grade))
        { grade.postExposure.Override(0); grade.contrast.Override(3); grade.saturation.Override(0); EditorUtility.SetDirty(grade); }
        if (volume.TryGet<ColorLookup>(out var lut)) { lut.contribution.Override(0); EditorUtility.SetDirty(lut); }
        if (volume.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.08f); bloom.threshold.Override(1.2f); EditorUtility.SetDirty(bloom); }
        if (!volume.TryGet<Tonemapping>(out var tone)) { tone = volume.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone,volume); }
        tone.mode.Override(TonemappingMode.Neutral); EditorUtility.SetDirty(tone); EditorUtility.SetDirty(volume);
        // Keep authored edit-mode light consistent with runtime, without overwriting an open unsaved scene.
        for (int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path != Root + "/Map/Scenes/World/Map.unity" && scene.path != Root + "/Map/Scenes/World/Map_GameplayPreview.unity") continue;
            GardenWorldSetup.ApplyLightingToScene(scene,p);
            foreach (var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional))
            { light.useColorTemperature = false; EditorUtility.SetDirty(light); }
        }
        AssetDatabase.SaveAssets();
    }
    static void Probe()
    {
        var sh = new SphericalHarmonicsL2(); sh.AddAmbientLight(RenderSettings.ambientEquatorColor.linear*.7f);
        sh.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor.linear,.45f);
        sh.AddDirectionalLight(Vector3.down,RenderSettings.ambientGroundColor.linear,.35f); RenderSettings.ambientProbe = sh;
    }
    static void Capture(Camera camera, string name)
    {
        const int w=1400,h=1000;
        // An sRGB target encodes the linear project output correctly for a PNG preview.
        camera.aspect=(float)w/h; var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); rt.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest { destination=rt };
        RenderPipeline.SubmitRenderRequest(camera,request); RenderPipeline.SubmitRenderRequest(camera,request);
        var old=RenderTexture.active; RenderTexture.active=rt; var image=new Texture2D(w,h,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,w,h),0,0); image.Apply(); File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=old; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
    }
    public static void RunBatch()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/")) throw new InvalidOperationException("Review requires isolated fixture.");
        Directory.CreateDirectory(Output);
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Single);
            var store=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Barn_Store" || t.name=="Farm_BarnStore_B");
            var renders=store.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.Contains("LOD0")).ToArray();
            var b=renders[0].bounds; foreach(var r in renders) b.Encapsulate(r.bounds);
            foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled=false;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled=false;
            var cam=new GameObject("Cozy shading review").AddComponent<Camera>(); cam.orthographic=true;
            cam.orthographicSize=b.size.y*.88f; cam.nearClipPlane=.1f; cam.farClipPlane=300;
            cam.transform.position=b.center+store.TransformDirection(new Vector3(-.3f,.42f,1).normalized)*b.size.magnitude*1.45f;
            cam.transform.LookAt(b.center); cam.clearFlags=CameraClearFlags.Skybox;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var sun=RenderSettings.sun;
            File.WriteAllText(Output+"/Bindings.txt",string.Join("\n",renders.SelectMany(r=>r.sharedMaterials).Select(m=>m.name+" | "+m.shader.name+" | "+AssetDatabase.GetAssetPath(m)))+"\n"+
                string.Join("\n",UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Select(t=>t.name+" | "+AssetDatabase.GetAssetPath(t.terrainData)+" | "+string.Join(",",t.terrainData.terrainLayers.Select(l=>l==null?"NULL":l.name)))));
            Probe(); Capture(cam,"Map_Before");
            Apply(); Probe(); Capture(cam,"Map_After");
            // Same map camera and scene, then sample the runtime midday and night palettes.
            var p=Resources.Load<GardenLightingProfile>("World/Garden Lighting");
            sun.intensity=p.sunIntensity*p.noonSunMultiplier; sun.color=p.sunColor;
            RenderSettings.ambientSkyColor=p.noonSky; RenderSettings.ambientEquatorColor=p.noonEquator; RenderSettings.ambientGroundColor=p.noonGround; Probe(); Capture(cam,"Map_Noon");
            sun.intensity=p.stylizedMoonIntensity; sun.color=p.moonColor;
            RenderSettings.ambientSkyColor=p.stylizedNightAmbient; RenderSettings.ambientEquatorColor=p.stylizedNightAmbient*.85f; RenderSettings.ambientGroundColor=p.stylizedNightAmbient*.65f;
            Probe(); Capture(cam,"Map_Night");
            var boundMaterials=renders.SelectMany(r=>r.sharedMaterials).ToArray(); var storePosition=store.position;
            // The same shared shader also serves the newly reworked interior and its window spotlight.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var interior=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PlayerHouse/Interior/HouseInteriorLayout_Lv1.prefab"));
            foreach(var c in interior.GetComponentsInChildren<Camera>(true)) c.enabled=false;
            var local=interior.GetComponentInChildren<HouseInteriorCozyLighting>(); local.ApplyLighting(10,WeatherType.Sunny);
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.72f,.8f,.87f); RenderSettings.ambientEquatorColor=new Color(.48f,.5f,.48f); RenderSettings.ambientGroundColor=new Color(.28f,.25f,.2f); Probe();
            var fill=new GameObject("Review fill").AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.55f; fill.color=new Color(1,.96f,.87f); fill.transform.rotation=Quaternion.Euler(55,20,0); fill.shadows=LightShadows.Soft; RenderSettings.sun=fill;
            var indoor=new GameObject("Interior preview").AddComponent<Camera>(); indoor.fieldOfView=40; indoor.nearClipPlane=.05f; indoor.farClipPlane=80;
            indoor.clearFlags=CameraClearFlags.SolidColor; indoor.backgroundColor=new Color(.78f,.77f,.69f);
            indoor.transform.position=new Vector3(2.6f,8.705f,-3.375f); indoor.transform.rotation=Quaternion.Euler(54,0,0);
            Capture(indoor,"Interior_After");
            var shader=Shader.Find("Nature Paradise/Cozy Atlas");
            if(boundMaterials.Where(m=>m.name=="NP_CozySurfaces_01").Any(m=>m.shader!=shader)) throw new Exception("Map renderer uses a different surface material.");
            var messages=ShaderUtil.GetShaderMessages(shader);
            if(ShaderUtil.ShaderHasError(shader)) throw new Exception(string.Join("\n",messages.Select(m=>m.message)));
            foreach(var name in new[]{"NP_CozySurfaces_01","NP_CozyFoliage_01"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/"+name+".mat");
                if(m.shader!=shader || m.GetTexture("_BaseMap")==null) throw new Exception("Material not configured: "+name);
            }
            if(!volumeReady(p)) throw new Exception("Neutral grading not active.");
            if(File.Exists(Output+"/Error.txt")) File.Delete(Output+"/Error.txt");
            File.WriteAllText(Output+"/Audit.txt","COZY_SHADING_PASS\nNative map: "+storePosition+" bounds "+b+"\nLinear colour, atlas base/normal/mask preserved; two shared materials; no additional screen passes or texture assets.\nMap day/noon/night and house interior rendered with native Unity.\nShader messages: "+string.Join("\n",messages.Select(m=>m.message)));
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static bool volumeReady(GardenLightingProfile p) => p.postProcessing.TryGet<Tonemapping>(out var tone) && tone.mode.value==TonemappingMode.Neutral;
}
#endif
