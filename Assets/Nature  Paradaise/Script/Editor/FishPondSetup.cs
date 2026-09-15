#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Membuat asset modular dan titik Construction Menu untuk Fish Pond free-placement.</summary>
public static class FishPondSetup
{
    const string DefinitionPath = "Assets/Nature  Paradaise/Resources/Buildings/Fish Pond Building.asset";
    const string CatalogPath = "Assets/Nature  Paradaise/Resources/Buildings/Field Building Catalog.asset";
    const string FeedPath = "Assets/Nature  Paradaise/Resources/Items/Fish/Fish Feed.asset";
    const string PrefabFolder = "Assets/Nature  Paradaise/Prefabs/FishPond";
    const string MaterialFolder = "Assets/Nature  Paradaise/Material/World/FishPond";
    const string MapPath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";

    [InitializeOnLoadMethod]
    static void QueueSetup()
    {
        const string sessionKey = "NatureParadise.FishPond.Setup.V2";
        if (SessionState.GetBool(sessionKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(MapPath) == null) return;
            SessionState.SetBool(sessionKey, true);
            SetupAll();
        };
    }

    [MenuItem("Nature Paradise/Barn/Setup Fish Pond System", false, 140)]
    public static void SetupAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[FISH POND] Hentikan Play Mode sebelum setup.");
            return;
        }

        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        ItemSO feed = AssetDatabase.LoadAssetAtPath<ItemSO>(FeedPath);
        Material water = GetOrCreateMaterial($"{MaterialFolder}/FishPondWater.mat", new Color(0.08f, 0.48f, 0.82f, 0.78f));
        Material rim = GetOrCreateMaterial($"{MaterialFolder}/FishPondRim.mat", new Color(0.34f, 0.22f, 0.12f));

        GameObject[] prefabs = new GameObject[4];
        for (int level = 1; level <= 4; level++) prefabs[level - 1] = CreateOrUpdatePrefab(level, feed, water, rim);
        BuildingDefinitionSO definition = CreateOrUpdateDefinition(prefabs);
        AddToFieldCatalog(definition);
        RemoveLegacyBuilderSite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FISH POND] Prefab Lv.1-4, growth storage, dan Field Build Menu Player siap.");
    }

    static GameObject CreateOrUpdatePrefab(int level, ItemSO feed, Material water, Material rim)
    {
        string path = $"{PrefabFolder}/FishPond_Lv{level}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        GameObject root = new($"FishPond_Lv{level}");
        try
        {
            float size = 4f + (level - 1) * 0.65f;
            FishPond pond = root.AddComponent<FishPond>();
            pond.Configure(string.Empty, level, feed);

            GameObject waterObject = Primitive("Water_ReplaceMe", root.transform,
                new Vector3(0f, 0.05f, 0f), new Vector3(size, 0.12f, size), PrimitiveType.Cylinder, water);
            Object.DestroyImmediate(waterObject.GetComponent<Collider>());
            float half = size * 0.5f;
            Primitive("Rim_Left_ReplaceMe", root.transform, new Vector3(-half, 0.22f, 0f), new Vector3(0.35f, 0.45f, size + 0.35f), PrimitiveType.Cube, rim);
            Primitive("Rim_Right_ReplaceMe", root.transform, new Vector3(half, 0.22f, 0f), new Vector3(0.35f, 0.45f, size + 0.35f), PrimitiveType.Cube, rim);
            Primitive("Rim_Front_ReplaceMe", root.transform, new Vector3(0f, 0.22f, -half), new Vector3(size, 0.45f, 0.35f), PrimitiveType.Cube, rim);
            Primitive("Rim_Back_ReplaceMe", root.transform, new Vector3(0f, 0.22f, half), new Vector3(size, 0.45f, 0.35f), PrimitiveType.Cube, rim);

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static BuildingDefinitionSO CreateOrUpdateDefinition(GameObject[] prefabs)
    {
        BuildingDefinitionSO definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(DefinitionPath);
        bool created = definition == null;
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BuildingDefinitionSO>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        Undo.RecordObject(definition, "Setup Fish Pond Definition");
        if (created)
        {
            definition.buildingId = "building.fish-pond";
            definition.displayName = "Fish Pond";
            definition.category = BuildingCategory.Production;
        }
        definition.placementArea = BuildingPlacementArea.FieldOnly;
        if (!created)
        {
            for (int index = 0; index < definition.levels.Count && index < prefabs.Length; index++)
                if (definition.levels[index] != null && definition.levels[index].completedPrefab == null)
                    definition.levels[index].completedPrefab = prefabs[index];
            EditorUtility.SetDirty(definition);
            return definition;
        }
        definition.footprintWidth = 5;
        definition.footprintDepth = 5;
        definition.canRelocate = true;
        definition.canDemolish = true;
        ItemSO wood = Resources.Load<ItemSO>("Items/Materials/Wood");
        ItemSO stone = Resources.Load<ItemSO>("Items/Materials/Stone");
        int[] capacities = { 20, 40, 70, 100 };
        int[] gold = { 500, 1000, 1800, 3000 };
        int[] days = { 2, 3, 4, 5 };
        definition.levels = new List<BuildingLevelDefinition>();
        for (int level = 1; level <= 4; level++)
        {
            BuildingLevelDefinition entry = new()
            {
                level = level,
                goldCost = gold[level - 1],
                constructionDays = days[level - 1],
                capacity = capacities[level - 1],
                completedPrefab = prefabs[level - 1],
                unlockIds = new List<string> { $"fish-pond.level-{level}" }
            };
            entry.materialCosts.Add(new BuildingMaterialCost { item = wood, amount = 10 + level * 5 });
            entry.materialCosts.Add(new BuildingMaterialCost { item = stone, amount = 8 + level * 4 });
            definition.levels.Add(entry);
        }
        EditorUtility.SetDirty(definition);
        return definition;
    }

    static BuildingCatalogSO AddToFieldCatalog(BuildingDefinitionSO definition)
    {
        BuildingCatalogSO catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalogSO>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BuildingCatalogSO>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        SerializedObject serialized = new(catalog);
        SerializedProperty buildings = serialized.FindProperty("buildings");
        bool exists = false;
        for (int index = 0; index < buildings.arraySize; index++)
            if (buildings.GetArrayElementAtIndex(index).objectReferenceValue == definition)
                exists = true;
        if (!exists)
        {
            int index = buildings.arraySize;
            buildings.InsertArrayElementAtIndex(index);
            buildings.GetArrayElementAtIndex(index).objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    static void RemoveLegacyBuilderSite()
    {
        Scene scene = SceneManager.GetSceneByPath(MapPath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        GameObject legacy = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(item => item.gameObject)
            .FirstOrDefault(item => item.name is "FishPondConstructionSite_Editable" or "FishPondBuilderSite_Editable");
        if (legacy != null)
        {
            Undo.DestroyObjectImmediate(legacy);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (opened) EditorSceneManager.CloseScene(scene, true);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
    }

    static Transform FindOrCreate(Scene scene, string objectName, Transform parent)
    {
        if (parent != null)
        {
            Transform child = parent.Find(objectName);
            if (child != null) return child;
        }
        else
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == objectName) return root.transform;
        }
        GameObject created = new(objectName);
        if (parent != null) created.transform.SetParent(parent, false);
        else SceneManager.MoveGameObjectToScene(created, scene);
        return created.transform;
    }

    static GameObject Primitive(string objectName, Transform parent, Vector3 position, Vector3 scale, PrimitiveType type, Material material)
    {
        GameObject result = GameObject.CreatePrimitive(type);
        result.name = objectName;
        result.transform.SetParent(parent, false);
        result.transform.localPosition = position;
        result.transform.localScale = scale;
        Renderer renderer = result.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return result;
    }

    static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void EnsureFolder(string path)
    {
        string current = "Assets";
        string[] parts = path.Substring("Assets/".Length).Split('/');
        foreach (string part in parts)
        {
            string next = $"{current}/{part}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
#endif
