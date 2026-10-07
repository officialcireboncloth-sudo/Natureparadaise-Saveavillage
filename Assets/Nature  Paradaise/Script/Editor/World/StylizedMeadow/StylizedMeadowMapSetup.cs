using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Apply the authored meadow kit to Map without repainting terrain channels.</summary>
public static class StylizedMeadowMapSetup
{
    const string Kit = "Assets/Nature  Paradaise/Art/StylizedMeadow";
    const string Map = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";

    [MenuItem("Nature Paradise/World/Stylized Meadow/Apply To Map")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply in Edit Mode.");
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Kit + "/Terrain/MeadowGrass.terrainlayer");
        var source = AssetDatabase.LoadAssetAtPath<TerrainVegetationProfile>(Kit + "/Profiles/MeadowVegetation.asset");
        var active = Resources.Load<TerrainVegetationProfile>("World/Terrain Vegetation");
        if (layer == null || source == null || active == null) throw new InvalidOperationException("Build meadow assets first.");
        var prior = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(Map);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(Map, OpenSceneMode.Additive);
        try
        {
            var terrains = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).Where(t => t.terrainData != null).ToArray();
            if (terrains.Length == 0) throw new InvalidOperationException("Map contains no terrain.");
            var previousLayers = active.rules.Where(r => r.enabled && r.terrainLayer != null).Select(r => r.terrainLayer).ToArray();
            if (!terrains.Any(t => t.terrainData.terrainLayers.Any(l => IsGrass(l, previousLayers))))
                throw new InvalidOperationException("No grass paint channel found; nothing was changed.");
            var knownGrassChannels = terrains.SelectMany(t => t.terrainData.terrainLayers.Select((l, i) => new { l, i }))
                .Where(x => IsGrass(x.l, previousLayers)).Select(x => x.i).Distinct().ToArray();

            // Keep the active profile GUID so all terrain and runtime references remain valid.
            EditorUtility.CopySerialized(source, active);
            active.name = "Terrain Vegetation";
            active.initialResolution = 512;
            active.drawDistance = 80;
            EditorUtility.SetDirty(active);
            string backup = "Library/MeadowMapBackups";
            Directory.CreateDirectory(backup);
            string sceneBackup = backup + "/Map.before-meadow.unity";
            if (!File.Exists(sceneBackup)) File.Copy(Map, sceneBackup);
            var audit = new StringBuilder("Meadow Map application: painted channel weights and heights preserved.\n");
            int changed = 0;
            foreach (var terrain in terrains)
            {
                var data = terrain.terrainData;
                var layers = data.terrainLayers;
                string path = AssetDatabase.GetAssetPath(data);
                string copy = backup + "/" + AssetDatabase.AssetPathToGUID(path) + ".asset";
                if (!File.Exists(copy)) File.Copy(path, copy);
                int grassChannels = 0;
                for (int i = 0; i < layers.Length; i++)
                    if (IsGrass(layers[i], previousLayers) ||
                        // Repair the missing reference only when all authored grass channels agree on one index.
                        layers[i] == null && knownGrassChannels.Length == 1 && knownGrassChannels[0] == i)
                    { layers[i] = layer; grassChannels++; }
                if (grassChannels == 0) continue;
                // Same channel positions: no alphamap/height edits and dirt/path layers retain their assets.
                data.terrainLayers = layers;
                var vegetation = terrain.GetComponent<TerrainLayerVegetation>() ?? terrain.gameObject.AddComponent<TerrainLayerVegetation>();
                vegetation.profile = active;
                vegetation.Rebuild();
                EditorUtility.SetDirty(data); EditorUtility.SetDirty(terrain); EditorUtility.SetDirty(vegetation);
                changed++;
                audit.AppendLine($"{terrain.name}: grassChannels={grassChannels}, bindings={vegetation.Bindings.Count}, cull={terrain.detailObjectDistance}m, layer={layer.name}");
                foreach (var binding in vegetation.Bindings)
                {
                    var p = data.detailPrototypes[binding.prototype];
                    if (!p.Validate(out string error)) throw new InvalidOperationException(error);
                    if (p.prototype == null || !AssetDatabase.GetAssetPath(p.prototype).StartsWith(Kit + "/Prefabs/"))
                        throw new InvalidOperationException("Unexpected meadow prefab.");
                }
            }
            if (!AssetDatabase.IsValidFolder(Kit + "/Scenes")) AssetDatabase.CreateFolder(Kit, "Scenes");
            string oldScene = Kit + "/StylizedMeadowPreview.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(oldScene) != null)
            {
                string error = AssetDatabase.MoveAsset(oldScene, Kit + "/Scenes/StylizedMeadowPreview.unity");
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            audit.AppendLine("Terrains applied=" + changed);
            foreach(var rule in active.rules) audit.AppendLine($"{rule.name}: density={rule.density}/m2, width={rule.variants.FirstOrDefault()?.widthScale}, height={rule.variants.FirstOrDefault()?.heightScale}");
            File.WriteAllText("ArtSource/StylizedMeadow/Previews/MapApplicationAudit.txt", audit.ToString());
            Debug.Log("[MEADOW] Applied custom grass texture and vegetation to " + changed + " Map terrains.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (prior.isLoaded) SceneManager.SetActiveScene(prior); }
    }
    static bool IsGrass(TerrainLayer layer, TerrainLayer[] previous) => layer != null &&
        (layer.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
         layer.diffuseTexture != null && layer.diffuseTexture.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 || previous.Contains(layer));
}
