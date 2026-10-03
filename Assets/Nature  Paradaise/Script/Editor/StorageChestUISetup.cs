using UnityEditor;

public static class StorageChestUISetup
{
    [MenuItem("Nature Paradise/UI/Tool Storage/Select Image Slots")]
    static void SelectTheme()
    {
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<StorageChestTheme>(
            "Assets/Nature  Paradaise/Resources/UI/StorageChestTheme.asset");
        if(Selection.activeObject!=null)EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
