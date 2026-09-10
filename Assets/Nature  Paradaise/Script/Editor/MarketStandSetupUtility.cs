#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketStandSetupUtility
{
    const string MapScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
    const string VisualPrefabPath = "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Market Presets/TFP_Produce_Stand_Preset_01.prefab";
    const string WorldPrefabFolder = "Assets/Nature  Paradaise/Prefabs/World";
    const string MarketStandPrefabPath = WorldPrefabFolder + "/MarketStand.prefab";
    [MenuItem("Nature Paradise/Market/Setup Market Stand In Main Map")]
    public static void SetupFromMenu() => Setup();

    public static void SetupFromCommandLine() => Setup();

    static void Setup()
    {
        if (!File.Exists(MapScenePath))
            throw new FileNotFoundException("Map.unity tidak ditemukan.", MapScenePath);

        EditorSceneManager.SaveOpenScenes();
        EnsureFolder("Assets/Nature  Paradaise/Prefabs", "World");
        GameObject prefab = CreateOrUpdatePrefab();

        Scene scene = SceneManager.GetSceneByPath(MapScenePath);
        bool wasLoaded = scene.isLoaded;
        if (!wasLoaded)
            scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Additive);

        MarketStand existing = FindComponentInScene<MarketStand>(scene);
        if (existing == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "MarketStand_01_Editable";
            Transform marketGroup = GetOrCreatePath(scene, "30_WORLD", "Buildings", "Market");
            instance.transform.SetParent(marketGroup, worldPositionStays: true);
            instance.transform.SetPositionAndRotation(FindPlacement(scene), Quaternion.Euler(0f, 180f, 0f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasLoaded)
            EditorSceneManager.CloseScene(scene, removeScene: true);

        Debug.Log("[MARKET SETUP] MarketStand prefab dan object Map selesai dibuat di 30_WORLD/Buildings/Market.");
    }

    static GameObject CreateOrUpdatePrefab()
    {
        GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        if (visual == null)
            throw new FileNotFoundException("Visual Toon Farm Market Stand tidak ditemukan.", VisualPrefabPath);

        GameObject root = new("MarketStand");
        root.AddComponent<MarketStand>();
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.15f, 0f);
        collider.size = new Vector3(3.4f, 2.3f, 2.3f);

        GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visual, root.transform);
        visualInstance.name = "Visual_ToonFarm_ProduceStand";
        visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        visualInstance.transform.localScale = Vector3.one;

        GameObject result = PrefabUtility.SaveAsPrefabAsset(root, MarketStandPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return result;
    }

    static Vector3 FindPlacement(Scene scene)
    {
        NPCSeller seller = FindComponentInScene<NPCSeller>(scene);
        Inventory player = FindComponentInScene<Inventory>(scene);
        Vector3 target = seller != null
            ? seller.transform.position + seller.transform.right * 4f + seller.transform.forward * 2f
            : player != null ? player.transform.position + new Vector3(6f, 0f, 4f) : Vector3.zero;

        Terrain terrain = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
            .FirstOrDefault(candidate => ContainsXZ(candidate, target));
        if (terrain != null)
            target.y = terrain.SampleHeight(target) + terrain.transform.position.y;
        return target;
    }

    static bool ContainsXZ(Terrain terrain, Vector3 position)
    {
        if (terrain == null || terrain.terrainData == null)
            return false;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return position.x >= origin.x && position.x <= origin.x + size.x &&
               position.z >= origin.z && position.z <= origin.z + size.z;
    }

    static Transform GetOrCreatePath(Scene scene, params string[] names)
    {
        Transform current = null;
        for (int index = 0; index < names.Length; index++)
        {
            Transform next;
            if (current == null)
            {
                GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == names[index]);
                if (root == null)
                {
                    root = new GameObject(names[index]);
                    SceneManager.MoveGameObjectToScene(root, scene);
                }
                next = root.transform;
            }
            else
            {
                next = current.Cast<Transform>().FirstOrDefault(child => child.name == names[index]);
                if (next == null)
                {
                    next = new GameObject(names[index]).transform;
                    next.SetParent(current, false);
                }
            }
            current = next;
        }
        return current;
    }

    static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }
        return null;
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
