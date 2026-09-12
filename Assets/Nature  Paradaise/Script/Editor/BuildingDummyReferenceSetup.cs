#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Memasang FBX dummy melalui AssetDatabase agar Unity menulis local file ID GameObject
/// yang benar. Assignment YAML manual pada model import dapat menunjuk sub-object non-GameObject.
/// </summary>
public static class BuildingDummyReferenceSetup
{
    const string BarnDefinitionPath = "Assets/Nature  Paradaise/Resources/Buildings/Barn Building.asset";
    const string HouseDefinitionPath = "Assets/Nature  Paradaise/Resources/Buildings/Player House Building.asset";
    const string BarnModelPath = "Assets/Nature  Paradaise/mesh/Dummy/BarnDummy.fbx";
    const string HouseModelPath = "Assets/Nature  Paradaise/mesh/Dummy/HouseDummy.fbx";

    public static void ApplyBarnReference()
    {
        if (!AssignModelToAllLevels(BarnDefinitionPath, BarnModelPath)) return;

        AssetDatabase.SaveAssets();
        Debug.Log("[BUILDING] BarnDummy berhasil dipasang ke Building Definition.");
    }

    public static void ApplyHouseReference()
    {
        if (!AssignModelToAllLevels(HouseDefinitionPath, HouseModelPath)) return;

        AssetDatabase.SaveAssets();
        Debug.Log("[BUILDING] HouseDummy berhasil dipasang ke Building Definition.");
    }

    static bool AssignModelToAllLevels(string definitionPath, string modelPath)
    {
        BuildingDefinitionSO definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(definitionPath);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (definition == null || model == null)
        {
            Debug.LogWarning($"[BUILDING] Gagal memasang dummy. Definition/model tidak ditemukan: {definitionPath} | {modelPath}");
            return false;
        }

        SerializedObject serializedDefinition = new(definition);
        SerializedProperty levels = serializedDefinition.FindProperty("levels");

        // Kosongkan lebih dahulu. Unity dapat menganggap legacy model-container reference
        // sama dengan root GameObject walaupun local file ID yang tersimpan berbeda tipe.
        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty completedPrefab = levels
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("completedPrefab");
            completedPrefab.objectReferenceValue = null;
        }
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        serializedDefinition.Update();

        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty completedPrefab = levels
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("completedPrefab");
            completedPrefab.objectReferenceValue = model;
        }

        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return true;
    }
}
#endif
