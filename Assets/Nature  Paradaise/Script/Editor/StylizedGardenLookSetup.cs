using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Art-directed daylight built on the pack's toon shaders; preserves terrain paint and grass population.</summary>
public static class StylizedGardenLookSetup
{
    const string Root = "Assets/Nature  Paradaise";

    [MenuItem("Nature Paradise/World/Apply Stylized Garden Look")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Apply in Edit Mode.");
        var lighting = Resources.Load<GardenLightingProfile>("World/Garden Lighting");
        lighting.sunIntensity = 1.5f;
        lighting.sunColor = new Color(1f, .97f, .88f);
        lighting.ambientSky = new Color(.76f, .86f, 1f);
        lighting.ambientEquator = new Color(.72f, .79f, .83f);
        lighting.ambientGround = new Color(.48f, .53f, .38f);
        lighting.shadowStrength = .65f;
        lighting.reflectionIntensity = .15f;
        lighting.minimumDayElevation = 28f;
        lighting.sunnyCloudStrength = .25f;
        lighting.updateAmbientProbe = true;
        lighting.stylizedNightAmbient = new Color(.55f, .68f, .86f);
        lighting.stylizedMoonIntensity = .7f;
        lighting.useTimeOfDayPalette = true;
        lighting.morningSun = new Color(1f, .98f, .9f);
        lighting.morningSky = new Color(.88f, .89f, .82f);
        lighting.morningEquator = new Color(.8f, .81f, .75f);
        lighting.morningGround = new Color(.55f, .57f, .42f);
        lighting.eveningSun = new Color(1f, .68f, .43f);
        lighting.eveningSky = new Color(.9f, .72f, .6f);
        lighting.eveningEquator = new Color(.82f, .65f, .52f);
        lighting.eveningGround = new Color(.58f, .45f, .34f);
        lighting.moonColor = new Color(.57f, .75f, 1f);
        lighting.nightShadowStrength = .4f;
        lighting.noonSunMultiplier = 1.55f;
        lighting.noonElevation = 82f;
        lighting.noonBloomIntensity = .42f;
        lighting.noonBloomThreshold = .85f;
        lighting.noonExposureBoost = .16f;
        lighting.shadowBias = .025f;
        lighting.shadowNormalBias = .15f;
        EditorUtility.SetDirty(lighting);

        var volume = lighting.postProcessing;
        if (volume.TryGet<ColorLookup>(out var lookup))
        { lookup.contribution.Override(.25f); EditorUtility.SetDirty(lookup); }
        if (volume.TryGet<Vignette>(out var vignette))
        { vignette.intensity.Override(.08f); EditorUtility.SetDirty(vignette); }
        if (volume.TryGet<Bloom>(out var bloom))
        { bloom.threshold.Override(1.1f); bloom.intensity.Override(.18f); EditorUtility.SetDirty(bloom); }
        if (!volume.TryGet<ColorAdjustments>(out var grade))
        {
            grade = volume.Add<ColorAdjustments>();
            AssetDatabase.AddObjectToAsset(grade, volume);
        }
        grade.postExposure.Override(.08f);
        grade.contrast.Override(4f);
        grade.saturation.Override(8f);
        EditorUtility.SetDirty(grade); EditorUtility.SetDirty(volume);

        // Refresh the derived detail materials, never the source pack materials.
        var palette = new[] {
            ("01A", new Color(.25f,.40f,.17f), new Color(.18f,.30f,.075f)),
            ("03A", new Color(.32f,.48f,.20f), new Color(.16f,.30f,.065f)),
            ("04A", new Color(.32f,.43f,.15f), new Color(.23f,.32f,.07f)),
            ("05A", new Color(.49f,.56f,.25f), new Color(.20f,.34f,.09f)),
            ("06A", new Color(.40f,.52f,.21f), new Color(.19f,.31f,.08f)) };
        foreach (var entry in palette)
        {
            string path = Root + "/Prefabs/World/TerrainDetails/TFP_Grass_Patch_" + entry.Item1 + " Detail.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) continue;
            material.SetColor("_Color1", entry.Item2);
            material.SetColor("_Color2", entry.Item3);
            EditorUtility.SetDirty(material);
        }
        var spring = Resources.Load<SeasonVisualProfile>("Profiles/Seasons/Spring Season");
        spring.vegetationTint = Color.white;
        spring.lightingTint = Color.white;
        spring.sunMultiplier = 1;
        spring.ambientMultiplier = 1;
        EditorUtility.SetDirty(spring);
        string mapPath = Root + "/Map/Scenes/World/Map.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(mapPath);
        bool opened = !scene.isLoaded;
        if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(mapPath,
            UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            GardenWorldSetup.ApplyLightingToScene(scene, lighting);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var sun in root.GetComponentsInChildren<Light>(true))
                    if (sun.type == LightType.Directional)
                    {
                        var data = sun.GetComponent<UniversalAdditionalLightData>() ?? sun.gameObject.AddComponent<UniversalAdditionalLightData>();
                        data.usePipelineSettings = false;
                        EditorUtility.SetDirty(data);
                    }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pipeline != null)
        {
            pipeline.shadowDistance = 100f;
            pipeline.mainLightShadowmapResolution = 4096;
            EditorUtility.SetDirty(pipeline);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/StylizedGardenLookAudit.txt", "Stylized Garden: warm sun 1.5, cool sky fill, shadows .65, minimum daylight elevation 28, LUT .25, saturation +8. Grass paint/population unchanged.\n");
    }
}
