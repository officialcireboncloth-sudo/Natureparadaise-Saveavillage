using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class GardenWorldSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Pack = Root + "/Pack/Toon Series/Toon Farm Pack";
    const string World = Root + "/Resources/World";
    const string Details = Root + "/Prefabs/World/TerrainDetails";
    const string MapPath = Root + "/Map/Scenes/World/Map.unity";

    [MenuItem("Nature Paradise/World/Apply Demo Garden Lighting and Grass")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Apply in Edit Mode.");
        Directory.CreateDirectory(World); Directory.CreateDirectory(Details); AssetDatabase.Refresh();
        var lighting = GetOrCreate<GardenLightingProfile>(World + "/Garden Lighting.asset");
        // Exact daylight values from Demo_Gardens. Runtime retains dawn/night/season/weather transitions.
        lighting.sunIntensity = 1.2f; lighting.sunColor = Color.white;
        lighting.noonRotation = new Quaternion(.28601384f, .43326485f, -.35320875f, .77828103f);
        lighting.ambientSky = new Color(.7924528f, .72890705f, .72890705f);
        lighting.ambientEquator = new Color(.6603774f, .6261125f, .6261125f);
        lighting.ambientGround = new Color(.21698111f, .1622855f, .046057317f);
        lighting.shadowStrength = .9f; lighting.shadowBias = .05f; lighting.shadowNormalBias = .4f;
        lighting.reflectionIntensity = 0;
        lighting.minimumDayElevation = 3;
        lighting.sunnyCloudStrength = 1;
        lighting.updateAmbientProbe = false;
        lighting.useTimeOfDayPalette = false;
        lighting.skybox = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("5896895507e0b5c4885f9d45da257599"));
        string volumePath = World + "/Garden Post Processing.asset";
        if (!File.Exists(volumePath)) AssetDatabase.CopyAsset(Pack + "/Post Processing/TFP_Day_2.asset", volumePath);
        lighting.postProcessing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
        // Keep the demo grading/bloom, without lens colour fringing during gameplay.
        if (lighting.postProcessing.TryGet<ChromaticAberration>(out var chromatic))
        { chromatic.intensity.Override(0); EditorUtility.SetDirty(chromatic); }
        EditorUtility.SetDirty(lighting);

        // Restore pack's smooth ramp used by the demo, including generated runtime props.
        var ramp = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "/Textures/TFP_Toon_Ramp_1F.psd");
        var style = Resources.Load<ToonWorldStyle>("World/Toon World Style");
        if (style != null) { style.lightingRamp = ramp; EditorUtility.SetDirty(style); }
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".mat")) continue;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && material.HasProperty("_TextureRamp") && material.shader.name.StartsWith("Toon/"))
            { material.SetTexture("_TextureRamp", ramp); EditorUtility.SetDirty(material); }
        }
        var variants = new List<GameObject>();
        foreach (string name in new[] { "TFP_Grass_Patch_01A", "TFP_Grass_Patch_04A", "TFP_Grass_Patch_05A" })
            variants.Add(MakeDetailPrefab(name));
        var profile = GetOrCreate<TerrainVegetationProfile>(World + "/Terrain Vegetation.asset");

        var prior = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(MapPath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            ApplyLightingToScene(scene, lighting);
            var terrains = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
            var referenceLayers = terrains.FirstOrDefault(t=>t.terrainData != null && t.terrainData.terrainLayers.All(l=>l!=null))?.terrainData.terrainLayers;
            if (referenceLayers != null)
                foreach(var terrain in terrains.Where(t=>t.terrainData!=null))
                {
                    var layers=terrain.terrainData.terrainLayers;
                    if(layers.Length!=referenceLayers.Length || layers.All(l=>l!=null))continue;
                    string source=AssetDatabase.GetAssetPath(terrain.terrainData);
                    Directory.CreateDirectory("Library/GardenTerrainBackups");
                    string backup="Library/GardenTerrainBackups/"+AssetDatabase.AssetPathToGUID(source)+".asset";
                    if(!File.Exists(backup))File.Copy(source,backup);
                    for(int i=0;i<layers.Length;i++)if(layers[i]==null)layers[i]=referenceLayers[i];
                    terrain.terrainData.terrainLayers=layers; EditorUtility.SetDirty(terrain.terrainData);
                }
            if (profile.rules.Count == 0)
            {
                var layers = terrains.Where(t => t.terrainData != null).SelectMany(t => t.terrainData.terrainLayers).Where(l => l != null).Distinct();
                foreach (var layer in layers)
                {
                    bool grass = layer.name.ToLowerInvariant().Contains("grass");
                    var rule = new TerrainVegetationProfile.LayerRule { name = layer.name, terrainLayer = layer, enabled = grass };
                    if (grass) for (int i = 0; i < variants.Count; i++)
                        rule.variants.Add(new TerrainVegetationProfile.Variant {
                            prefab = variants[i], weight = i == 0 ? 1 : 2,
                            widthScale = new Vector2(.9f, 1.15f), heightScale = new Vector2(.85f, 1.15f) });
                    profile.rules.Add(rule);
                }
            }
            EditorUtility.SetDirty(profile);
            foreach (var field in roots.SelectMany(r => r.GetComponentsInChildren<FieldArea>(true)))
                if (field.GetComponent<TerrainVegetationExclusion>() == null) field.gameObject.AddComponent<TerrainVegetationExclusion>();
            // Explicit editable exclusion boxes for authored buildings and their entrances.
            foreach (var site in roots.SelectMany(r => r.GetComponentsInChildren<BuildingSite>(true)))
            {
                if (site.transform.Find("Grass Exclusion") != null) continue;
                var renderers = site.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x < 1 || bounds.size.z < 1) continue;
                var go = new GameObject("Grass Exclusion"); go.transform.SetParent(site.transform, false);
                go.transform.position = bounds.center;
                var box = go.AddComponent<BoxCollider>(); box.isTrigger = true;
                Vector3 scale = go.transform.lossyScale;
                box.size = new Vector3((bounds.size.x + 1) / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                    1, (bounds.size.z + 1) / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                go.AddComponent<TerrainVegetationExclusion>();
            }
            var report = new StringBuilder(); var processed = new Dictionary<TerrainData, TerrainLayerVegetation>();
            foreach (var terrain in terrains)
            {
                var data = terrain.terrainData; if (data == null) continue;
                var vegetation = terrain.GetComponent<TerrainLayerVegetation>() ?? terrain.gameObject.AddComponent<TerrainLayerVegetation>();
                vegetation.profile = profile;
                // Some Terrain tiles share data. Generate once; their mask/detail grids remain identical.
                if (processed.TryGetValue(data, out var shared))
                {
                    // A painted mask may be shared, but exclusions are world-position dependent.
                    // Give this tile its own copy without changing heights/layers or original source data.
                    string directory = Root + "/Map/Terrain/Vegetation";
                    Directory.CreateDirectory(directory); AssetDatabase.Refresh();
                    string path = directory + "/" + terrain.name + "_" + GlobalObjectId.GetGlobalObjectIdSlow(terrain).targetObjectId + ".asset";
                    var copy = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                    if (copy == null) { copy = Object.Instantiate(data); AssetDatabase.CreateAsset(copy, path); }
                    vegetation.CopyManagedSlotsFrom(shared); terrain.terrainData = copy;
                    var collider = terrain.GetComponent<TerrainCollider>(); if (collider != null) collider.terrainData = copy;
                    data = copy;
                }
                else
                {
                    string path = AssetDatabase.GetAssetPath(data);
                    Directory.CreateDirectory("Library/GardenTerrainBackups");
                    string backup = "Library/GardenTerrainBackups/" + AssetDatabase.AssetPathToGUID(path) + ".asset";
                    if (!File.Exists(backup)) File.Copy(path, backup);
                }
                vegetation.Rebuild(); processed[data] = vegetation;
                terrain.detailObjectDistance = profile.drawDistance; terrain.detailObjectDensity = profile.densityMultiplier;
                var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/World/m_MainTerrain.mat");
                if (terrainMaterial != null)
                {
                    // Existing masks have smooth transitions; the layers have no height mask maps.
                    if (terrainMaterial.HasProperty("_EnableHeightBlend")) terrainMaterial.SetFloat("_EnableHeightBlend", 0);
                    terrainMaterial.DisableKeyword("_TERRAIN_BLEND_HEIGHT"); EditorUtility.SetDirty(terrainMaterial);
                    terrain.materialTemplate = terrainMaterial;
                }
                EditorUtility.SetDirty(terrain); EditorUtility.SetDirty(vegetation);
                report.AppendLine($"{terrain.name}: size={data.size}, details={data.detailResolution}, scatter={data.detailScatterMode}, managed={vegetation.Bindings.Count}");
                foreach (var layer in data.terrainLayers) report.AppendLine("  Layer: " + (layer != null ? layer.name : "null"));
            }
            var look = roots.SelectMany(r => r.GetComponentsInChildren<GardenWorldLook>(true)).FirstOrDefault();
            if (look == null)
            {
                var go = new GameObject("World Look - Demo Garden"); SceneManager.MoveGameObjectToScene(go, scene);
                look = go.AddComponent<GardenWorldLook>();
            }
            look.profile = lighting;
            var volume = look.GetComponent<Volume>(); volume.isGlobal = true; volume.priority = 5;
            volume.sharedProfile = lighting.postProcessing; volume.weight = 1;
            foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
            {
                var additional = camera.GetComponent<UniversalAdditionalCameraData>() ?? camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                additional.renderPostProcessing = true; camera.allowHDR = true;
                additional.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                additional.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(additional); EditorUtility.SetDirty(camera);
            }
            EditorUtility.SetDirty(look); EditorUtility.SetDirty(volume);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Library/GardenWorldSetupAudit.txt", report.ToString());
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (prior.isLoaded) SceneManager.SetActiveScene(prior); }
    }
    public static void ApplyLightingToScene(Scene scene, GardenLightingProfile lighting)
    {
        var prior = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            RenderSettings.skybox = lighting.skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = lighting.ambientSky;
            RenderSettings.ambientEquatorColor = lighting.ambientEquator;
            RenderSettings.ambientGroundColor = lighting.ambientGround;
            RenderSettings.reflectionIntensity = lighting.reflectionIntensity;
            var sun = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional);
            if (sun != null)
            {
                sun.enabled = true; sun.color = lighting.sunColor; sun.intensity = lighting.sunIntensity;
                sun.transform.rotation = lighting.noonRotation; sun.shadows = LightShadows.Soft;
                sun.shadowStrength = lighting.shadowStrength; sun.shadowBias = lighting.shadowBias;
                sun.shadowNormalBias = lighting.shadowNormalBias; RenderSettings.sun = sun;
                EditorUtility.SetDirty(sun); EditorUtility.SetDirty(sun.transform);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
        finally { if (prior.isLoaded) SceneManager.SetActiveScene(prior); }
    }
    static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
        return value;
    }
    public static GameObject MakeDetailPrefab(string name, string vegetationFolder = "Grass", string lod = "LOD0")
    {
        string target = Details + "/" + name + " Detail.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(target);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "/Prefabs/Vegetation/" + vegetationFolder + "/" + name + ".prefab");
        if (source == null) throw new System.InvalidOperationException("Missing vegetation prefab: " + name);
        var filter = source.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.name.EndsWith(lod)) ?? source.GetComponentInChildren<MeshFilter>(true);
        var renderer = filter.GetComponent<MeshRenderer>();
        var go = new GameObject(name + " Detail");
        try
        {
            go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var output = go.AddComponent<MeshRenderer>();
            string materialPath = Details + "/" + name + " Detail.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(renderer.sharedMaterial) { name = name + " Detail" }; AssetDatabase.CreateAsset(material, materialPath); }
            material.enableInstancing = true;
            if (material.HasProperty("_TerrainGrass"))
            {
                material.SetFloat("_TerrainGrass", 1);
                material.EnableKeyword("NP_TERRAIN_GRASS");
            }
            EditorUtility.SetDirty(material);
            output.sharedMaterial = material; output.shadowCastingMode = ShadowCastingMode.Off; output.receiveShadows = true;
            return PrefabUtility.SaveAsPrefabAsset(go, target);
        }
        finally { Object.DestroyImmediate(go); }
    }
}

[CustomEditor(typeof(TerrainLayerVegetation))]
public sealed class TerrainLayerVegetationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Terrain paint controls the mask. Native Terrain detail patches handle instancing, random scale/yaw and distance/frustum culling. Rebuild after changing profile variants/density.", MessageType.Info);
        if (GUILayout.Button("Rebuild From Terrain Paint")) { ((TerrainLayerVegetation)target).Rebuild(); AssetDatabase.SaveAssets(); }
    }
}
[CustomEditor(typeof(TerrainVegetationProfile))]
public sealed class TerrainVegetationProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Apply Rules to Loaded Terrains"))
        {
            foreach (var vegetation in Object.FindObjectsByType<TerrainLayerVegetation>(FindObjectsSortMode.None))
                if (vegetation.profile == target) vegetation.Rebuild();
            AssetDatabase.SaveAssets();
        }
    }
}
