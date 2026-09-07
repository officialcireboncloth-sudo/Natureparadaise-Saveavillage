#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Memetakan Terrain Paint Details ke sistem rumput liar tanpa mengubah asset visual sumber.</summary>
public sealed class TerrainDetailGrassSetupWindow : EditorWindow
{
    Terrain terrain;
    ItemSO grassItem;
    bool[] harvestable = Array.Empty<bool>();
    Vector2 scroll;

    [MenuItem("Nature Paradise/Grass/Setup Terrain Wild Grass")]
    static void Open()
    {
        TerrainDetailGrassSetupWindow window = GetWindow<TerrainDetailGrassSetupWindow>("Wild Grass");
        if (Selection.activeGameObject != null)
            window.terrain = Selection.activeGameObject.GetComponent<Terrain>();
        window.grassItem = AssetDatabase.LoadAssetAtPath<ItemSO>(
            AssetDatabase.GUIDToAssetPath("a33f7b52dbbf45b18c8796d0dfe5a91a"));
        window.SyncToggles();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "1. Tambahkan prototype dan scatter lewat Terrain > Paint Details.\n" +
            "2. Centang hanya layer rumput liar yang boleh disabit/dimakan.\n" +
            "3. Klik Setup, lalu Save Scene.\n\n" +
            "Layer detail yang tidak dicentang tetap menjadi dekorasi biasa.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        terrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", terrain, typeof(Terrain), true);
        if (EditorGUI.EndChangeCheck()) SyncToggles();
        grassItem = (ItemSO)EditorGUILayout.ObjectField("Grass Drop", grassItem, typeof(ItemSO), false);

        DetailPrototype[] prototypes = terrain != null && terrain.terrainData != null
            ? terrain.terrainData.detailPrototypes : Array.Empty<DetailPrototype>();
        if (harvestable.Length != prototypes.Length) SyncToggles();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Paint Detail Layers", EditorStyles.boldLabel);
        if (prototypes.Length == 0)
            EditorGUILayout.HelpBox("Belum ada Detail Prototype. Tambahkan dulu lewat Paint Details > Edit Details.", MessageType.Warning);
        for (int i = 0; i < prototypes.Length; i++)
        {
            DetailPrototype prototype = prototypes[i];
            string label = PrototypeName(prototype, i);
            harvestable[i] = EditorGUILayout.ToggleLeft(
                $"[{i}] {label}  — Harvestable Wild Grass", harvestable[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Default gameplay: satu blok 1.5m = satu gundukan; drop Grass x2.");
        EditorGUILayout.LabelField("Regrowth: kecil hari 3, sedang hari 6, penuh sekitar hari 10.");
        EditorGUILayout.LabelField("Hujan/musim memengaruhi laju; hewan dapat grazing dari layer terpilih.");

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || terrain == null ||
                   terrain.terrainData == null || prototypes.Length == 0 || grassItem == null))
        {
            if (GUILayout.Button("Setup Selected Terrain")) Setup();
        }
        EditorGUILayout.EndScrollView();
    }

    void SyncToggles()
    {
        DetailPrototype[] prototypes = terrain != null && terrain.terrainData != null
            ? terrain.terrainData.detailPrototypes : Array.Empty<DetailPrototype>();
        bool[] next = new bool[prototypes.Length];
        TerrainDetailGrassManager existing = terrain != null
            ? terrain.GetComponent<TerrainDetailGrassManager>() : null;
        if (existing != null)
        {
            SerializedProperty mappings = new SerializedObject(existing).FindProperty("prototypeSettings");
            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(i);
                int index = mapping.FindPropertyRelative("prototypeIndex").intValue;
                if (index >= 0 && index < next.Length)
                    next[index] = mapping.FindPropertyRelative("harvestable").boolValue;
            }
        }
        harvestable = next;
        Repaint();
    }

    void Setup()
    {
        if (terrain.GetComponent<TerrainRuntimeDataHost>() == null)
            Undo.AddComponent<TerrainRuntimeDataHost>(terrain.gameObject);
        TerrainDetailGrassManager manager = terrain.GetComponent<TerrainDetailGrassManager>();
        bool newlyAdded = manager == null;
        if (newlyAdded) manager = Undo.AddComponent<TerrainDetailGrassManager>(terrain.gameObject);
        Undo.RecordObject(manager, "Setup Terrain Wild Grass");

        SerializedObject serialized = new(manager);
        serialized.FindProperty("targetTerrain").objectReferenceValue = terrain;
        serialized.FindProperty("grassItem").objectReferenceValue = grassItem;
        if (newlyAdded) serialized.FindProperty("terrainId").stringValue = Guid.NewGuid().ToString("N");
        SerializedProperty mappings = serialized.FindProperty("prototypeSettings");
        DetailPrototype[] prototypes = terrain.terrainData.detailPrototypes;
        mappings.arraySize = prototypes.Length;
        for (int i = 0; i < prototypes.Length; i++)
        {
            DetailPrototype prototype = prototypes[i];
            SerializedProperty mapping = mappings.GetArrayElementAtIndex(i);
            mapping.FindPropertyRelative("prototypeName").stringValue = PrototypeName(prototype, i);
            mapping.FindPropertyRelative("prototypeIndex").intValue = i;
            mapping.FindPropertyRelative("detailPrefab").objectReferenceValue = prototype.prototype;
            mapping.FindPropertyRelative("detailTexture").objectReferenceValue = prototype.prototypeTexture;
            mapping.FindPropertyRelative("harvestable").boolValue = harvestable[i];
            mapping.FindPropertyRelative("grassYield").intValue = 2;
        }
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Selection.activeGameObject = terrain.gameObject;
        Debug.Log("[WildGrass] Setup selesai. Paint Details sumber tetap aman; " +
                  "perubahan density hanya terjadi pada salinan runtime saat Play.", manager);
    }

    static string PrototypeName(DetailPrototype prototype, int index)
    {
        if (prototype == null) return "Detail " + index;
        if (prototype.prototype != null) return prototype.prototype.name;
        if (prototype.prototypeTexture != null) return prototype.prototypeTexture.name;
        return "Detail " + index;
    }
}
#endif
