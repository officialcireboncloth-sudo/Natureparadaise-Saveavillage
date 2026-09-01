#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Membuat asset House Lv.1-Lv.4, dummy exterior, Carpenter, dan scene interior editable.
/// Setup aman dijalankan ulang karena selalu mencari stable object/asset name terlebih dahulu.
/// </summary>
public static class HouseSystemSetup
{
    const string HouseAssetPath = "Assets/Nature  Paradaise/Resource/Player House Building.asset";
    const string InteriorScenePath = "Assets/Nature  Paradaise/HouseInterior.unity";
    const string WorldRootName = "PlayerHouse_Editable";

    [InitializeOnLoadMethod]
    static void ScheduleSetup()
    {
        EditorApplication.delayCall += TryAutomaticSetup;
    }

    static void TryAutomaticSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "TestingScene")
            return;
        SetupAll();
    }

    [MenuItem("Nature Paradise/Setup Player House System")]
    public static void SetupAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[HOUSE SETUP] Hentikan Play Mode sebelum menjalankan setup.");
            return;
        }

        BuildingDefinitionSO definition = GetOrCreateHouseDefinition();
        CreateInteriorSceneIfMissing();
        SetupWorldObjects(definition);
        AddInteriorToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[HOUSE SETUP] Player House, Carpenter, dan HouseInterior siap.");
    }

    static BuildingDefinitionSO GetOrCreateHouseDefinition()
    {
        BuildingDefinitionSO existing = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(HouseAssetPath);
        if (existing != null)
            return existing;

        ItemSO wood = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resource/Wood.asset");
        ItemSO stone = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resource/Stone.asset");
        BuildingDefinitionSO house = ScriptableObject.CreateInstance<BuildingDefinitionSO>();
        house.buildingId = "building.player-house";
        house.displayName = "Player House";
        house.category = BuildingCategory.House;
        house.footprintWidth = 6;
        house.footprintDepth = 5;
        house.canRelocate = false;
        house.canDemolish = false;
        house.levels = new List<BuildingLevelDefinition>
        {
            CreateLevel(1, 0, 0, 0, 0, wood, stone,
                "house.bed", "house.tv", "house.basic-storage"),
            CreateLevel(2, 250, 12, 8, 2, wood, stone,
                "house.kitchen", "house.refrigerator", "house.dining-area"),
            CreateLevel(3, 750, 28, 20, 4, wood, stone,
                "house.extra-bedroom", "house.large-living-room", "house.furniture-tier-3"),
            CreateLevel(4, 1800, 55, 40, 7, wood, stone,
                "house.master-bedroom", "house.trophy-room", "house.furniture-tier-4")
        };
        house.levels[2].requiredVillageLevel = 2;
        house.levels[3].requiredVillageLevel = 4;
        AssetDatabase.CreateAsset(house, HouseAssetPath);
        return house;
    }

    static BuildingLevelDefinition CreateLevel(
        int level,
        int gold,
        int woodAmount,
        int stoneAmount,
        int days,
        ItemSO wood,
        ItemSO stone,
        params string[] unlocks)
    {
        BuildingLevelDefinition result = new()
        {
            level = level,
            goldCost = gold,
            constructionDays = days,
            capacity = level * 8,
            unlockIds = unlocks.ToList()
        };
        if (wood != null && woodAmount > 0)
            result.materialCosts.Add(new BuildingMaterialCost { item = wood, amount = woodAmount });
        if (stone != null && stoneAmount > 0)
            result.materialCosts.Add(new BuildingMaterialCost { item = stone, amount = stoneAmount });
        return result;
    }

    static void SetupWorldObjects(BuildingDefinitionSO definition)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "TestingScene")
            return;
        GameObject existing = GameObject.Find(WorldRootName);
        if (existing != null)
            return;

        Vector3 basePosition = new(-18.5f, 0f, 4.2f);
        PlayerSpawnPoint homeSpawn = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None)
            .FirstOrDefault(point => point.SpawnId == "player-home");
        if (homeSpawn != null)
            basePosition = homeSpawn.transform.position + Vector3.forward * 4f;

        GameObject root = new(WorldRootName);
        Undo.RegisterCreatedObjectUndo(root, "Setup Player House");
        root.transform.position = basePosition;
        List<GameObject> exteriors = new();
        for (int level = 1; level <= 4; level++)
            exteriors.Add(CreateExteriorDummy(root.transform, level));

        GameObject construction = CreatePrimitiveChild(
            root.transform, "HouseConstructionVisual_Editable", PrimitiveType.Cube,
            new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 6f));
        construction.SetActive(false);

        PlayerHouseController controller = root.AddComponent<PlayerHouseController>();
        controller.Configure(definition, exteriors, construction);

        GameObject door = CreatePrimitiveChild(
            root.transform, "HouseEntranceDoor_Editable", PrimitiveType.Cube,
            new Vector3(0f, 1f, -2.65f), new Vector3(1.2f, 2f, 0.25f));
        HouseScenePortal entrancePortal = door.AddComponent<HouseScenePortal>();
        entrancePortal.Configure(false, "HouseInterior", "house-interior-entry", "player-house-exit");

        GameObject exitSpawnObject = new("PlayerHouseExitSpawn");
        exitSpawnObject.transform.SetParent(root.transform, false);
        exitSpawnObject.transform.localPosition = new Vector3(0f, 0f, -4.2f);
        exitSpawnObject.transform.localRotation = Quaternion.identity;
        exitSpawnObject.AddComponent<PlayerSpawnPoint>().Configure("player-house-exit");

        GameObject carpenter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        carpenter.name = "Carpenter_Dummy_Editable";
        carpenter.transform.position = basePosition + new Vector3(6f, 1f, -2f);
        carpenter.AddComponent<CarpenterNPC>();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
    }

    static GameObject CreateExteriorDummy(Transform parent, int level)
    {
        GameObject layout = new($"HouseExterior_Lv{level}_Editable");
        layout.transform.SetParent(parent, false);
        float width = 4.5f + level * 0.8f;
        float depth = 3.6f + level * 0.65f;
        CreatePrimitiveChild(layout.transform, "Body_MeshSlot", PrimitiveType.Cube,
            new Vector3(0f, 1.25f, 0f), new Vector3(width, 2.5f, depth));
        CreatePrimitiveChild(layout.transform, "Roof_MeshSlot", PrimitiveType.Cube,
            new Vector3(0f, 2.8f, 0f), new Vector3(width + 0.5f, 0.45f, depth + 0.5f));
        layout.SetActive(level == 1);
        return layout;
    }

    static void CreateInteriorSceneIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(InteriorScenePath) != null)
            return;

        Scene previousActive = SceneManager.GetActiveScene();
        Scene interior = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        interior.name = "HouseInterior";
        GameObject root = new("HouseInterior_Editable");
        root.transform.position = new Vector3(10000f, 0f, 10000f);
        HouseInteriorController controller = root.AddComponent<HouseInteriorController>();

        List<GameObject> layouts = new();
        for (int level = 1; level <= 4; level++)
            layouts.Add(CreateInteriorLayout(root.transform, level));
        controller.Configure(layouts);

        GameObject entry = new("HouseInteriorEntrySpawn");
        entry.transform.SetParent(root.transform, false);
        entry.transform.localPosition = new Vector3(0f, 0f, -3.5f);
        entry.AddComponent<PlayerSpawnPoint>().Configure("house-interior-entry");

        GameObject exitDoor = CreatePrimitiveChild(
            root.transform, "HouseInteriorExitDoor_Editable", PrimitiveType.Cube,
            new Vector3(0f, 1f, -4.5f), new Vector3(1.2f, 2f, 0.25f));
        HouseScenePortal exitPortal = exitDoor.AddComponent<HouseScenePortal>();
        exitPortal.Configure(true, "HouseInterior", "house-interior-entry", "player-house-exit");

        GameObject lightObject = new("Interior Directional Light", typeof(Light));
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        lightObject.GetComponent<Light>().type = LightType.Directional;
        lightObject.GetComponent<Light>().intensity = 1.1f;

        EditorSceneManager.SaveScene(interior, InteriorScenePath);
        EditorSceneManager.CloseScene(interior, true);
        if (previousActive.IsValid())
            SceneManager.SetActiveScene(previousActive);
    }

    static GameObject CreateInteriorLayout(Transform parent, int level)
    {
        GameObject layout = new($"InteriorLayout_Lv{level}_Editable");
        layout.transform.SetParent(parent, false);
        float width = 8f + level * 2f;
        float depth = 7f + level * 1.5f;
        CreatePrimitiveChild(layout.transform, "Floor_MeshSlot", PrimitiveType.Cube,
            Vector3.zero, new Vector3(width, 0.2f, depth));
        CreatePrimitiveChild(layout.transform, "BackWall_MeshSlot", PrimitiveType.Cube,
            new Vector3(0f, 1.5f, depth * 0.5f), new Vector3(width, 3f, 0.2f));
        CreatePrimitiveChild(layout.transform, "LeftWall_MeshSlot", PrimitiveType.Cube,
            new Vector3(-width * 0.5f, 1.5f, 0f), new Vector3(0.2f, 3f, depth));
        CreatePrimitiveChild(layout.transform, "RightWall_MeshSlot", PrimitiveType.Cube,
            new Vector3(width * 0.5f, 1.5f, 0f), new Vector3(0.2f, 3f, depth));

        GameObject bed = CreatePrimitiveChild(layout.transform, "Bed_MeshSlot", PrimitiveType.Cube,
            new Vector3(-2.5f, 0.45f, 1.8f), new Vector3(2.2f, 0.7f, 1.2f));
        bed.AddComponent<PlayerBed>();
        GameObject tv = CreatePrimitiveChild(layout.transform, "TV_MeshSlot", PrimitiveType.Cube,
            new Vector3(2.5f, 1f, 2.4f), new Vector3(1.5f, 1.5f, 0.35f));
        tv.AddComponent<WeatherForecastTV>();
        CreatePrimitiveChild(layout.transform, "BasicStorage_MeshSlot", PrimitiveType.Cube,
            new Vector3(-3.2f, 0.8f, -1.6f), new Vector3(1.4f, 1.6f, 1f));
        if (level >= 2)
        {
            CreatePrimitiveChild(layout.transform, "Kitchen_MeshSlot", PrimitiveType.Cube,
                new Vector3(2.6f, 0.8f, 0.5f), new Vector3(3f, 1.6f, 0.8f));
            CreatePrimitiveChild(layout.transform, "Refrigerator_MeshSlot", PrimitiveType.Cube,
                new Vector3(4f, 1.2f, 1.8f), new Vector3(1f, 2.4f, 1f));
        }
        if (level >= 3)
            CreatePrimitiveChild(layout.transform, "ExtraBedroom_MeshSlot", PrimitiveType.Cube,
                new Vector3(-4f, 1f, 3.2f), new Vector3(2.5f, 2f, 0.2f));
        if (level >= 4)
            CreatePrimitiveChild(layout.transform, "TrophyRoom_MeshSlot", PrimitiveType.Cube,
                new Vector3(4.5f, 1f, 3.5f), new Vector3(2.8f, 2f, 0.2f));

        layout.SetActive(level == 1);
        return layout;
    }

    static GameObject CreatePrimitiveChild(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale)
    {
        GameObject result = GameObject.CreatePrimitive(primitive);
        result.name = name;
        result.transform.SetParent(parent, false);
        result.transform.localPosition = localPosition;
        result.transform.localRotation = Quaternion.identity;
        result.transform.localScale = localScale;
        return result;
    }

    static void AddInteriorToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == InteriorScenePath))
            return;
        scenes.Add(new EditorBuildSettingsScene(InteriorScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
