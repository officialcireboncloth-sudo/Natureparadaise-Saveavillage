#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Membuat asset House Lv.1-Lv.4, dummy exterior, Carpenter, dan scene interior editable
/// hanya ketika menu setup dijalankan secara eksplisit.
/// </summary>
public static class HouseSystemSetup
{
    const string SpaciousInteriorSessionKey = "NatureParadise.House.SpaciousTwoRoomKitchen.V2";
    const string HouseAssetPath = "Assets/Nature  Paradaise/Resources/Buildings/Player House Building.asset";
    const string InteriorScenePath = "Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
    const string WorldRootName = "PlayerHouse_Editable";
    const string HouseMaterialFolder = "Assets/Nature  Paradaise/Material/House";
    const string RefrigeratorBodyMaterialPath = HouseMaterialFolder + "/m_RefrigeratorDummyBody.mat";
    const string RefrigeratorDoorMaterialPath = HouseMaterialFolder + "/m_RefrigeratorDummyDoor.mat";
    const string RefrigeratorHandleMaterialPath = HouseMaterialFolder + "/m_RefrigeratorDummyHandle.mat";

    [InitializeOnLoadMethod]
    static void QueueRefrigeratorDummyUpgrade()
    {
        if (SessionState.GetBool(SpaciousInteriorSessionKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(InteriorScenePath) == null) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= InstallSpaciousInteriorAfterPlayMode;
                EditorApplication.playModeStateChanged += InstallSpaciousInteriorAfterPlayMode;
                return;
            }
            SessionState.SetBool(SpaciousInteriorSessionKey, true);
            InstallKitchenAndTwoRoomLayout();
        };
    }

    static void InstallSpaciousInteriorAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= InstallSpaciousInteriorAfterPlayMode;
        if (SessionState.GetBool(SpaciousInteriorSessionKey, false)) return;
        SessionState.SetBool(SpaciousInteriorSessionKey, true);
        InstallKitchenAndTwoRoomLayout();
    }

    [InitializeOnLoadMethod]
    static void QueueAquariumDummy()
    {
        const string sessionKey = "NatureParadise.House.AquariumDummy.V1";
        if (SessionState.GetBool(sessionKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(InteriorScenePath) == null) return;
            SessionState.SetBool(sessionKey, true);
            InstallAquariumDummy();
        };
    }

    [MenuItem("Nature Paradise/House/Setup or Update Player House",false,100)]
    public static void SetupAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[HOUSE SETUP] Hentikan Play Mode sebelum menjalankan setup.");
            return;
        }

        BuildingDefinitionSO definition = GetOrCreateHouseDefinition();
        CreateInteriorSceneIfMissing();
        InstallToolStorageChest();
        InstallRefrigerator();
        InstallKitchenAndTwoRoomLayout();
        InstallAquariumDummy();
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

        ItemSO wood = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resources/Items/Materials/Wood.asset");
        ItemSO stone = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resources/Items/Materials/Stone.asset");
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
        // Player memakai CharacterController setinggi 2 m dengan center Y=0, jadi pivot
        // harus berada di atas permukaan lantai agar kapsul tidak mulai dalam keadaan overlap.
        entry.transform.localPosition = new Vector3(0f, 1.15f, -3.5f);
        entry.AddComponent<PlayerSpawnPoint>().Configure("house-interior-entry");

        GameObject exitDoor = CreatePrimitiveChild(
            root.transform, "HouseInteriorExitDoor_Editable", PrimitiveType.Cube,
            new Vector3(0f, 1.2f, -4.5f), new Vector3(2.4f, 2.4f, 0.25f));
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
        float width = InteriorWidth(level);
        float depth = InteriorDepth(level);
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
        GameObject toolStorage = CreatePrimitiveChild(layout.transform, "ToolStorageChest_Editable", PrimitiveType.Cube,
            new Vector3(-3.2f, 0.8f, -1.6f), new Vector3(1.4f, 1.6f, 1f));
        toolStorage.AddComponent<ToolStorageChest>();
        if (level >= 2)
        {
            GameObject kitchen = CreatePrimitiveChild(layout.transform, "Kitchen_MeshSlot", PrimitiveType.Cube,
                new Vector3(width * 0.27f, 0.8f, 1.2f), new Vector3(3.8f, 1.6f, 0.8f));
            kitchen.AddComponent<KitchenSet>();
            GameObject refrigerator = CreatePrimitiveChild(layout.transform, "Refrigerator_MeshSlot", PrimitiveType.Cube,
                new Vector3(width * 0.39f, 1.2f, depth * 0.3f), new Vector3(1f, 2.4f, 1f));
            refrigerator.AddComponent<Refrigerator>();
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

    public static void InstallToolStorageChest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[TOOL STORAGE] Hentikan Play Mode sebelum memasang peti.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(InteriorScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        Scene previousActive = SceneManager.GetActiveScene();
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Additive);

        int installed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name != "BasicStorage_MeshSlot" && candidate.name != "ToolStorageChest_Editable")
                continue;
            candidate.name = "ToolStorageChest_Editable";
            if (candidate.GetComponent<ToolStorageChest>() == null)
            {
                Undo.AddComponent<ToolStorageChest>(candidate.gameObject);
                installed++;
            }
        }

        if (installed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (openedForSetup)
            EditorSceneManager.CloseScene(scene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            SceneManager.SetActiveScene(previousActive);

        Debug.Log($"[TOOL STORAGE] Siap di HouseInterior. Komponen baru: {installed}.");
    }

    [MenuItem("Nature Paradise/House/Install Refrigerator", false, 125)]
    public static void InstallRefrigerator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[REFRIGERATOR] Hentikan Play Mode sebelum memasang Refrigerator.");
            return;
        }
        Scene scene = SceneManager.GetSceneByPath(InteriorScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        Scene previousActive = SceneManager.GetActiveScene();
        if (openedForSetup) scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Additive);
        int installed = 0;
        bool changed = false;
        Transform[] layouts = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name.StartsWith("InteriorLayout_Lv"))
            .ToArray();
        foreach (Transform layout in layouts)
        {
            Transform candidate = layout.Find("Refrigerator_MeshSlot");
            if (candidate == null)
            {
                candidate = CreatePrimitiveChild(layout, "Refrigerator_MeshSlot", PrimitiveType.Cube,
                    new Vector3(4f, 1.2f, 1.8f), new Vector3(1f, 2.4f, 1f)).transform;
                changed = true;
            }
            if (candidate.GetComponent<Refrigerator>() == null)
            {
                Undo.AddComponent<Refrigerator>(candidate.gameObject);
                installed++;
                changed = true;
            }
            changed |= EnsureRefrigeratorDummyVisual(candidate);
        }
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        Debug.Log($"[REFRIGERATOR] Dummy siap pada seluruh layout House. Komponen baru: {installed}.");
    }

    [MenuItem("Nature Paradise/House/Install Kitchen + Two Room Layout", false, 126)]
    public static void InstallKitchenAndTwoRoomLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[KITCHEN] Hentikan Play Mode sebelum memperbarui interior.");
            return;
        }
        Scene scene = SceneManager.GetSceneByPath(InteriorScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        Scene previousActive = SceneManager.GetActiveScene();
        if (openedForSetup) scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Additive);
        if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Any(candidate => candidate.name == "HouseInterior_SpaciousTwoRoomLayout_V2"))
        {
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            return;
        }
        bool changed = false;
        Transform[] layouts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name.StartsWith("InteriorLayout_Lv")).ToArray();
        foreach (Transform layout in layouts)
        {
            int level = ParseLayoutLevel(layout.name);
            float width = InteriorWidth(level);
            float depth = InteriorDepth(level);
            float dividerX = width * 0.055f;
            float doorwayCenterZ = -depth * 0.17f;
            float doorwayWidth = Mathf.Lerp(3.2f, 5.5f, (level - 1) / 3f);
            float doorwayBottom = doorwayCenterZ - doorwayWidth * 0.5f;
            float doorwayTop = doorwayCenterZ + doorwayWidth * 0.5f;
            float roomBottom = -depth * 0.5f;
            float roomTop = depth * 0.5f;
            float frontDividerLength = Mathf.Max(0.5f, doorwayBottom - roomBottom);
            float backDividerLength = Mathf.Max(0.5f, roomTop - doorwayTop);
            changed |= SetSlot(layout, "Floor_MeshSlot", Vector3.zero, new Vector3(width, 0.2f, depth));
            changed |= SetSlot(layout, "BackWall_MeshSlot", new Vector3(0f, 1.5f, depth * 0.5f), new Vector3(width, 3f, 0.2f));
            changed |= SetSlot(layout, "LeftWall_MeshSlot", new Vector3(-width * 0.5f, 1.5f, 0f), new Vector3(0.2f, 3f, depth));
            changed |= SetSlot(layout, "RightWall_MeshSlot", new Vector3(width * 0.5f, 1.5f, 0f), new Vector3(0.2f, 3f, depth));
            changed |= SetSlot(layout, "RoomDivider_Back_MeshSlot",
                new Vector3(dividerX, 1.5f, doorwayTop + backDividerLength * 0.5f),
                new Vector3(0.18f, 3f, backDividerLength));
            changed |= SetSlot(layout, "RoomDivider_Front_MeshSlot",
                new Vector3(dividerX, 1.5f, roomBottom + frontDividerLength * 0.5f),
                new Vector3(0.18f, 3f, frontDividerLength));
            changed |= SetSlot(layout, "Bed_MeshSlot", new Vector3(-width * 0.28f, 0.45f, depth * 0.25f), new Vector3(2.2f, 0.7f, 1.2f));
            changed |= SetSlot(layout, "TV_MeshSlot", new Vector3(-width * 0.15f, 1f, depth * 0.38f), new Vector3(1.5f, 1.5f, 0.35f));
            changed |= SetSlot(layout, "ToolStorageChest_Editable", new Vector3(-width * 0.38f, 0.8f, -depth * 0.25f), new Vector3(1.4f, 1.6f, 1f));
            if (level >= 2)
            {
                Transform kitchen = EnsureSlot(layout, "Kitchen_MeshSlot", new Vector3(width * 0.27f, 0.8f, 1.2f), new Vector3(3.8f, 1.6f, 0.8f), ref changed);
                if (kitchen.GetComponent<KitchenSet>() == null) { Undo.AddComponent<KitchenSet>(kitchen.gameObject); changed = true; }
                Transform fridge = EnsureSlot(layout, "Refrigerator_MeshSlot", new Vector3(width * 0.39f, 1.2f, depth * 0.3f), new Vector3(1f, 2.4f, 1f), ref changed);
                if (fridge.GetComponent<Refrigerator>() == null) { Undo.AddComponent<Refrigerator>(fridge.gameObject); changed = true; }
                changed |= SetSlot(layout, "KitchenCounter_MeshSlot", new Vector3(width * 0.27f, 0.55f, depth * 0.39f), new Vector3(3.8f, 1.1f, 0.65f));
                changed |= SetSlot(layout, "DiningTable_MeshSlot", new Vector3(width * 0.22f, 0.55f, -depth * 0.2f), new Vector3(2.4f, 1.1f, 1.5f));
            }
            if (level >= 3)
                changed |= SetSlot(layout, "ExtraBedroom_MeshSlot",
                    new Vector3(-width * 0.23f, 1f, depth * 0.45f), new Vector3(4f, 2f, 0.2f));
            if (level >= 4)
                changed |= SetSlot(layout, "TrophyRoom_MeshSlot",
                    new Vector3(width * 0.25f, 1f, depth * 0.45f), new Vector3(4f, 2f, 0.2f));
        }
        Transform houseRoot = scene.GetRootGameObjects().Select(root => root.transform)
            .FirstOrDefault(root => root.name == "HouseInterior_Editable");
        if (houseRoot != null)
        {
            GameObject marker = new("HouseInterior_SpaciousTwoRoomLayout_V2");
            marker.transform.SetParent(houseRoot, false);
            marker.SetActive(false);
            changed = true;
        }
        if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        Debug.Log("[KITCHEN] Interior dua ruangan V2 diperlebar; pintu dan lorong Kitchen sudah lega.");
    }

    [MenuItem("Nature Paradise/House/Install Aquarium Test Furniture", false, 127)]
    public static void InstallAquariumDummy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[AQUARIUM] Hentikan Play Mode sebelum memasang dummy Aquarium.");
            return;
        }
        Scene scene = SceneManager.GetSceneByPath(InteriorScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        Scene previousActive = SceneManager.GetActiveScene();
        if (openedForSetup) scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Additive);
        bool changed = false;
        Transform[] layouts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name.StartsWith("InteriorLayout_Lv")).ToArray();
        foreach (Transform layout in layouts)
        {
            int level = ParseLayoutLevel(layout.name);
            float width = InteriorWidth(level);
            Transform aquarium = layout.Find("Aquarium_TestFurniture_Editable");
            if (aquarium == null)
            {
                aquarium = CreatePrimitiveChild(layout, "Aquarium_TestFurniture_Editable", PrimitiveType.Cube,
                    new Vector3(-width * 0.36f, 1.1f, 0.6f), new Vector3(3f, 1.8f, 0.8f)).transform;
                changed = true;
            }
            changed |= SetTransform(aquarium, new Vector3(-width * 0.36f, 1.1f, 0.6f), new Vector3(3f, 1.8f, 0.8f));
            Aquarium component = aquarium.GetComponent<Aquarium>();
            if (component == null) { component = Undo.AddComponent<Aquarium>(aquarium.gameObject); changed = true; }
            // Satu logical aquarium dipakai seluruh visual level rumah agar isi tidak hilang saat upgrade.
            component.Configure("house.aquarium.test.main", AquariumSize.Medium);
            EditorUtility.SetDirty(component);
        }
        if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        Debug.Log("[AQUARIUM] Dummy Medium siap pada seluruh level HouseInterior.");
    }

    static int ParseLayoutLevel(string layoutName)
    {
        for (int level = 1; level <= 4; level++) if (layoutName.Contains($"Lv{level}")) return level;
        return 1;
    }

    // Lv.4 sengaja jauh lebih luas karena karakter final lebih besar dari dummy awal.
    static float InteriorWidth(int level) => Mathf.Clamp(level, 1, 4) switch
    {
        1 => 14f,
        2 => 18f,
        3 => 23f,
        _ => 28f
    };

    static float InteriorDepth(int level) => Mathf.Clamp(level, 1, 4) switch
    {
        1 => 10f,
        2 => 14f,
        3 => 17f,
        _ => 20f
    };

    static Transform EnsureSlot(Transform parent, string slotName, Vector3 position, Vector3 scale, ref bool changed)
    {
        Transform result = parent.Find(slotName);
        if (result == null) { result = CreatePrimitiveChild(parent, slotName, PrimitiveType.Cube, position, scale).transform; changed = true; }
        changed |= SetTransform(result, position, scale);
        return result;
    }

    static bool SetSlot(Transform parent, string slotName, Vector3 position, Vector3 scale)
    {
        bool changed = false;
        Transform result = EnsureSlot(parent, slotName, position, scale, ref changed);
        return changed | SetTransform(result, position, scale);
    }

    static bool SetTransform(Transform target, Vector3 position, Vector3 scale)
    {
        if (target.localPosition == position && target.localScale == scale && target.localRotation == Quaternion.identity) return false;
        Undo.RecordObject(target, "Update House Interior Layout");
        target.localPosition = position; target.localRotation = Quaternion.identity; target.localScale = scale;
        return true;
    }

    static bool EnsureRefrigeratorDummyVisual(Transform refrigerator)
    {
        Material body = GetOrCreateDummyMaterial(RefrigeratorBodyMaterialPath, new Color(0.55f, 0.72f, 0.78f), 0.35f);
        Material door = GetOrCreateDummyMaterial(RefrigeratorDoorMaterialPath, new Color(0.82f, 0.93f, 0.96f), 0.5f);
        Material handle = GetOrCreateDummyMaterial(RefrigeratorHandleMaterialPath, new Color(0.08f, 0.12f, 0.15f), 0.65f);
        bool changed = false;
        Renderer bodyRenderer = refrigerator.GetComponent<Renderer>();
        if (bodyRenderer != null && bodyRenderer.sharedMaterial != body)
        {
            bodyRenderer.sharedMaterial = body;
            changed = true;
        }
        changed |= EnsureRefrigeratorPart(refrigerator, "DummyVisual_UpperDoor",
            new Vector3(0f, 0.22f, -0.515f), new Vector3(0.88f, 0.35f, 0.045f), door);
        changed |= EnsureRefrigeratorPart(refrigerator, "DummyVisual_LowerDoor",
            new Vector3(0f, -0.23f, -0.515f), new Vector3(0.88f, 0.43f, 0.045f), door);
        changed |= EnsureRefrigeratorPart(refrigerator, "DummyVisual_DoorDivider",
            new Vector3(0f, 0.015f, -0.55f), new Vector3(0.9f, 0.022f, 0.055f), handle);
        changed |= EnsureRefrigeratorPart(refrigerator, "DummyVisual_Handle",
            new Vector3(0.32f, 0.18f, -0.575f), new Vector3(0.075f, 0.16f, 0.065f), handle);
        return changed;
    }

    static bool EnsureRefrigeratorPart(Transform parent, string partName, Vector3 position,
        Vector3 scale, Material material)
    {
        if (parent.Find(partName) != null) return false;
        GameObject part = CreatePrimitiveChild(parent, partName, PrimitiveType.Cube, position, scale);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
        return true;
    }

    static Material GetOrCreateDummyMaterial(string path, Color color, float smoothness)
    {
        EnsureEditorFolder(HouseMaterialFolder);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureEditorFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureEditorFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
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
