using UnityEditor;

public static class InventoryUISetup
{
    [MenuItem("Nature Paradise/UI/Inventory/Select Image Slots")]
    static void SelectTheme()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<InventoryTheme>(
            "Assets/Nature  Paradaise/Resources/UI/InventoryTheme.asset");
        if (Selection.activeObject != null) EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
