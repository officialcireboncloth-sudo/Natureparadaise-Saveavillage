using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TerrainBushPreset
{
    [MenuItem("Nature Paradise/World/Apply Sparse Terrain Bushes")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply in Edit Mode.");
        var profile = Resources.Load<TerrainVegetationProfile>("World/Terrain Vegetation");
        if (profile == null) throw new InvalidOperationException("Terrain Vegetation profile is missing.");
        var a = GardenWorldSetup.MakeDetailPrefab("TFP_Bush_01A", "Trees", "LOD1");
        var b = GardenWorldSetup.MakeDetailPrefab("TFP_Bush_02A", "Trees", "LOD1");
        var grass = profile.rules.First(r => r.enabled && r.terrainLayer != null &&
            r.name.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0);
        var bushes = profile.rules.FirstOrDefault(r => r.name == "Sparse Bushes on Grass");
        if (bushes == null) { bushes = new TerrainVegetationProfile.LayerRule(); profile.rules.Add(bushes); }
        bushes.name = "Sparse Bushes on Grass";
        bushes.enabled = true;
        bushes.terrainLayer = grass.terrainLayer;
        bushes.diffuseTexture = grass.diffuseTexture;
        bushes.minimumPaintWeight = .85f;
        bushes.useWorldDensity = true;
        bushes.density = .006f; // Approximately one bush per 167 square metres of fully painted grass.
        bushes.maximumSlope = 28;
        bushes.clusterSize = 20;
        bushes.clusterVariation = .2f;
        bushes.variants.Clear();
        foreach (var prefab in new[] { a, b })
            bushes.variants.Add(new TerrainVegetationProfile.Variant {
                prefab = prefab, weight = 1,
                widthScale = new Vector2(.65f, 1.05f), heightScale = new Vector2(.65f, 1.05f) });
        EditorUtility.SetDirty(profile);

        const string path = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
        var prior = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var audit = new StringBuilder("Bush density: .006/m2, scale .65..1.05, grass paint >= .85; native distance/frustum culling.\n");
        try
        {
            foreach (var vegetation in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainLayerVegetation>(true)))
            {
                if (vegetation.profile != profile) continue;
                vegetation.Rebuild();
                var data = vegetation.GetComponent<Terrain>().terrainData;
                long count = 0;
                foreach (var binding in vegetation.Bindings.Where(x => profile.rules[x.rule] == bushes))
                {
                    var prototype = data.detailPrototypes[binding.prototype];
                    if (!prototype.Validate(out string error)) throw new InvalidOperationException(error);
                    var map = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, binding.prototype);
                    foreach (int n in map) count += n;
                }
                audit.AppendLine(vegetation.name + ": bush instances=" + count + ", drawDistance=" + vegetation.GetComponent<Terrain>().detailObjectDistance);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Library/TerrainBushPresetAudit.txt", audit.ToString());
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (prior.isLoaded) SceneManager.SetActiveScene(prior);
        }
    }
}
