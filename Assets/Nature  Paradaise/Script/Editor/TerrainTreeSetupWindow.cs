#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Setup eksplisit dari menu; tidak membuat object saat Editor reload.</summary>
public sealed class TerrainTreeSetupWindow : EditorWindow
{
    Terrain terrain;
    WorldTree sourceTree;
    Transform player;
    ItemSO wood;
    Vector2 scroll;

    [MenuItem("Nature Paradise/Trees/Setup Terrain Trees")]
    static void Open()
    {
        TerrainTreeSetupWindow window = GetWindow<TerrainTreeSetupWindow>("Terrain Trees");
        if (Selection.activeGameObject != null)
            window.terrain = Selection.activeGameObject.GetComponent<Terrain>();
        window.wood = AssetDatabase.LoadAssetAtPath<ItemSO>(
            AssetDatabase.GUIDToAssetPath("98910cf31f154315bef29b4f50bde6ad"));
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "Paint pohon memakai Terrain > Paint Trees terlebih dahulu. Setup ini menambah manager " +
            "dan prefab interaktif baru untuk prototype yang belum dipetakan. Asset visual asli " +
            "dan pohon manual tidak diubah. Mapping yang sudah ada dipertahankan.", MessageType.Info);
        terrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", terrain, typeof(Terrain), true);
        sourceTree = (WorldTree)EditorGUILayout.ObjectField(
            new GUIContent("Copy Chop Settings", "Opsional: pohon yang script tebangnya sudah bekerja."),
            sourceTree, typeof(WorldTree), true);
        wood = (ItemSO)EditorGUILayout.ObjectField("Wood Drop", wood, typeof(ItemSO), false);
        player = (Transform)EditorGUILayout.ObjectField("Player (optional)", player, typeof(Transform), true);
        EditorGUILayout.LabelField("Defaults: 15m / release 18m / 50 active / 5 switches per frame");
        EditorGUILayout.LabelField("Regrow defaults: 4-7 game days; edit on generated WorldTree prefab.");
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying ||
                   terrain == null || terrain.terrainData == null || wood == null))
        {
            if (GUILayout.Button("Setup Selected Terrain"))
                Setup(terrain, sourceTree, wood, player);
        }
        EditorGUILayout.HelpBox(
            "Setelah setup: pada Prototype Settings matikan Choppable untuk pohon dekorasi. " +
            "Remove Stump After Felling = pohon langsung hilang setelah roboh. " +
            "Ulangi untuk tile Terrain lain; gunakan cap/budget sama karena batas dibagi antarmanager.", MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    public static void Setup(Terrain target, WorldTree source, ItemSO woodItem, Transform playerTarget)
    {
        if (Application.isPlaying || target == null || target.terrainData == null || woodItem == null)
            return;
        TreePrototype[] prototypes = target.terrainData.treePrototypes;
        if (prototypes.Length == 0)
        {
            Debug.LogWarning("[Trees] Tambahkan prototype lewat Terrain > Paint Trees > Edit Trees dahulu.", target);
            return;
        }
        if (target.GetComponent<TerrainRuntimeDataHost>() == null)
            Undo.AddComponent<TerrainRuntimeDataHost>(target.gameObject);
        TerrainTreeManager manager = target.GetComponent<TerrainTreeManager>();
        bool newlyAdded = manager == null;
        if (newlyAdded) manager = Undo.AddComponent<TerrainTreeManager>(target.gameObject);
        Undo.RecordObject(manager, "Setup Terrain Trees");
        SerializedObject settings = new(manager);
        settings.FindProperty("targetTerrain").objectReferenceValue = target;
        if (playerTarget != null) settings.FindProperty("player").objectReferenceValue = playerTarget;
        if (newlyAdded)
        {
            settings.FindProperty("terrainId").stringValue = Guid.NewGuid().ToString("N");
            settings.FindProperty("activateDistance").floatValue = 15f;
            settings.FindProperty("deactivateDistance").floatValue = 18f;
            settings.FindProperty("maxActiveTrees").intValue = 50;
            settings.FindProperty("maxSwitchesPerFrame").intValue = 5;
        }
        SerializedProperty mappings = settings.FindProperty("prototypeSettings");
        foreach (TreePrototype prototype in prototypes)
        {
            if (prototype.prefab == null) continue;
            bool mapped = false;
            for (int i = 0; i < mappings.arraySize; i++)
                if (mappings.GetArrayElementAtIndex(i).FindPropertyRelative("terrainPrefab").objectReferenceValue == prototype.prefab)
                    mapped = true;
            if (mapped) continue;
            if (prototype.prefab.GetComponentInChildren<Renderer>(true) == null)
            {
                Debug.LogWarning($"[Trees] {prototype.prefab.name} tidak memiliki Renderer; dilewati.", prototype.prefab);
                continue;
            }
            WorldTree interactive = prototype.prefab.GetComponent<WorldTree>();
            if (interactive == null)
                interactive = CreateInteractivePrefab(prototype.prefab, source, woodItem);
            if (!interactive.gameObject.activeSelf || !interactive.enabled ||
                interactive.GetComponent<Collider>() == null)
            {
                Debug.LogWarning("[Trees] Prefab interaktif harus aktif dan memiliki root Collider.", interactive);
                continue;
            }
            int index = mappings.arraySize++;
            SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
            mapping.FindPropertyRelative("terrainPrefab").objectReferenceValue = prototype.prefab;
            mapping.FindPropertyRelative("interactivePrefab").objectReferenceValue = interactive;
            mapping.FindPropertyRelative("choppable").boolValue = true;
            mapping.FindPropertyRelative("removeStumpAfterFelling").boolValue = true;
        }
        settings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        Selection.activeGameObject = target.gameObject;
        Debug.Log("[Trees] Setup selesai. Periksa mapping/collider prefab, lalu Save Scene. " +
                  "Tidak ada TerrainData atau object manual yang dihapus.", manager);
    }

    static WorldTree CreateInteractivePrefab(GameObject model, WorldTree source, ItemSO woodItem)
    {
        // Hanya salinan sementara yang dibersihkan setelah prefab baru berhasil disimpan.
        GameObject root = new(model.name + "_Interactive");
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root.transform, false);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;
            visual.SetActive(true);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            collider.size = new Vector3(
                Mathf.Clamp(bounds.size.x * 0.15f, 0.3f, 1.5f),
                Mathf.Max(1f, bounds.size.y * 0.65f),
                Mathf.Clamp(bounds.size.z * 0.15f, 0.3f, 1.5f));
            collider.center = new Vector3(bounds.center.x, bounds.min.y + collider.size.y * 0.5f, bounds.center.z);
            // Collider pada child visual tidak boleh tetap menghalangi setelah pohon hilang.
            foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true))
                childCollider.enabled = false;
            foreach (WorldTree childTree in visual.GetComponentsInChildren<WorldTree>(true))
                childTree.enabled = false;
            WorldTree tree = root.AddComponent<WorldTree>();
            if (source != null) EditorUtility.CopySerialized(source, tree);
            tree.enabled = true;
            SerializedObject configuration = new(tree);
            configuration.FindProperty("treeId").stringValue = "terrain-prefab";
            configuration.FindProperty("woodItem").objectReferenceValue = woodItem;
            configuration.FindProperty("standingVisual").objectReferenceValue = visual.transform;
            configuration.FindProperty("stumpVisual").objectReferenceValue = null;
            configuration.FindProperty("highlightRenderers").arraySize = 0;
            configuration.FindProperty("woodChipParticles").objectReferenceValue = null;
            if (source != null)
            {
                SerializedObject sourceConfiguration = new(source);
                Transform sourceStump = sourceConfiguration.FindProperty("stumpVisual").objectReferenceValue as Transform;
                if (sourceStump != null)
                {
                    Transform stump = Instantiate(sourceStump, root.transform);
                    stump.name = "Stump";
                    stump.gameObject.SetActive(false);
                    configuration.FindProperty("stumpVisual").objectReferenceValue = stump;
                }
            }
            configuration.ApplyModifiedPropertiesWithoutUndo();
            root.transform.localScale = model.transform.localScale;
            root.transform.localRotation = model.transform.localRotation;
            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Nature  Paradaise/Resources/Prefabs/Trees/" + model.name + "_Interactive.prefab");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved.GetComponent<WorldTree>();
        }
        finally
        {
            DestroyImmediate(root);
        }
    }
}
#endif
