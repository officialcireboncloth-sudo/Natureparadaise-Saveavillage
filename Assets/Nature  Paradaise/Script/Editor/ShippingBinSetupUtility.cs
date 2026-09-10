#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Setup manual; tidak berjalan otomatis dan tidak membuat ulang object yang dihapus.</summary>
public static class ShippingBinSetupUtility
{
    const string MapScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
    const string VisualPrefabPath = "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Props/Exterior Props/TFP_Wooden_Crate_04A.prefab";
    const string WorldPrefabFolder = "Assets/Nature  Paradaise/Prefabs/World";
    const string ShippingBinPrefabPath = WorldPrefabFolder + "/ShippingBin.prefab";

    [MenuItem("Nature Paradise/Shipping/Setup Shipping Bin Near Player Home")]
    public static void SetupFromMenu() => Setup();

    public static void SetupFromCommandLine() => Setup();

    static void Setup()
    {
        if (!File.Exists(MapScenePath)) throw new FileNotFoundException("Map.unity tidak ditemukan.", MapScenePath);
        EditorSceneManager.SaveOpenScenes();
        EnsureFolder("Assets/Nature  Paradaise/Prefabs", "World");
        GameObject prefab = CreateOrUpdatePrefab();

        Scene scene = SceneManager.GetSceneByPath(MapScenePath);
        bool wasLoaded = scene.isLoaded;
        if (!wasLoaded) scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Additive);

        ShippingBin existing = FindComponentInScene<ShippingBin>(scene);
        if (existing == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "ShippingBin_PlayerHome_Editable";
            instance.transform.SetParent(GetOrCreatePath(scene, "30_WORLD", "Buildings", "Player Home Utilities"), true);
            instance.transform.SetPositionAndRotation(FindPlacement(scene), Quaternion.Euler(0f, 25f, 0f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[SHIPPING SETUP] ShippingBin dibuat dekat PlayerHomeSpawn.");
    }

    static GameObject CreateOrUpdatePrefab()
    {
        GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        if (visual == null) throw new FileNotFoundException("Visual Shipping Bin Toon Farm tidak ditemukan.", VisualPrefabPath);

        GameObject root = new("ShippingBin");
        root.AddComponent<ShippingBin>();
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.55f, 0f);
        collider.size = new Vector3(1.65f, 1.1f, 1.45f);

        GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visual, root.transform);
        visualInstance.name = "Visual_ToonFarm_ShippingCrate";
        visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        visualInstance.transform.localScale = Vector3.one * 1.35f;

        GameObject result = PrefabUtility.SaveAsPrefabAsset(root, ShippingBinPrefabPath);
        Object.DestroyImmediate(root);
        return result;
    }

    static Vector3 FindPlacement(Scene scene)
    {
        PlayerSpawnPoint home = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerSpawnPoint>(true))
            .FirstOrDefault(point => point.SpawnId == "player-home");
        Vector3 target = home != null ? home.transform.position + new Vector3(4f, 0f, 2f) : Vector3.zero;
        Terrain terrain = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
            .FirstOrDefault(candidate => ContainsXZ(candidate, target));
        if (terrain != null) target.y = terrain.SampleHeight(target) + terrain.transform.position.y;
        return target;
    }

    static bool ContainsXZ(Terrain terrain, Vector3 position)
    {
        if (terrain == null || terrain.terrainData == null) return false;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return position.x >= origin.x && position.x <= origin.x + size.x &&
               position.z >= origin.z && position.z <= origin.z + size.z;
    }

    static Transform GetOrCreatePath(Scene scene, params string[] names)
    {
        Transform current = null;
        foreach (string name in names)
        {
            Transform next;
            if (current == null)
            {
                GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == name);
                if (root == null) { root = new GameObject(name); SceneManager.MoveGameObjectToScene(root, scene); }
                next = root.transform;
            }
            else
            {
                next = current.Cast<Transform>().FirstOrDefault(child => child.name == name);
                if (next == null) { next = new GameObject(name).transform; next.SetParent(current, false); }
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
            if (result != null) return result;
        }
        return null;
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
