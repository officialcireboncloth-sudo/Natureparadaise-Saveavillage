using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>A broad-leaf weed silhouette reserved for harvestable grass.</summary>
public static class WildGrassVisualSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Target = Root + "/Resources/World/Wild Grass.prefab";

    [MenuItem("Nature Paradise/Grass/Apply Distinct Harvestable Weed")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply in Edit Mode.");
        Directory.CreateDirectory("Library/WildGrassVisualBackups");
        if (!File.Exists("Library/WildGrassVisualBackups/Wild Grass.prefab"))
            File.Copy(Target, "Library/WildGrassVisualBackups/Wild Grass.prefab");
        var root = PrefabUtility.LoadPrefabContents(Target);
        try
        {
            foreach (var group in root.GetComponents<LODGroup>()) Object.DestroyImmediate(group);
            while (root.transform.childCount > 0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            root.name = "Wild Grass - Harvestable Weed";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            AddPlant(root, "TFP_Leafy_Plant_04A", new Vector3(-.18f,0,.08f), .8f, 25);
            AddPlant(root, "TFP_Leafy_Plant_04A", new Vector3(.24f,0,-.1f), .55f, 150);
            AddPlant(root, "TFP_Leafy_Plant_01A", new Vector3(.08f,0,.25f), .65f, 285);
            PrefabUtility.SaveAsPrefabAsset(root, Target);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // Authored FieldArea references take priority over the Resources fallback.
        string mapPath = Root + "/Map/Scenes/World/Map.unity";
        var scene = SceneManager.GetSceneByPath(mapPath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(mapPath, OpenSceneMode.Additive);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Target);
            foreach (var field in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FieldArea>(true)))
            {
                var serialized = new SerializedObject(field);
                serialized.FindProperty("wildGrassPrefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
    }

    static void AddPlant(GameObject root, string name, Vector3 position, float height, float yaw)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Pack/Toon Series/Toon Farm Pack/Prefabs/Vegetation/Grass/" + name + ".prefab");
        var filter = source.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.EndsWith("LOD0"));
        var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        child.transform.SetParent(root.transform, false);
        child.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
        var bounds = filter.sharedMesh.bounds;
        float scale = height / Mathf.Max(.01f, bounds.size.y);
        child.transform.localRotation = Quaternion.Euler(0,yaw,0);
        child.transform.localScale = Vector3.one * scale;
        child.transform.localPosition = position - Vector3.up * bounds.min.y * scale;
    }
}
