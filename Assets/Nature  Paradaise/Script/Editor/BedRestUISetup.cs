using UnityEditor;

public static class BedRestUISetup
{
    [MenuItem("Nature Paradise/UI/Bed Rest/Select Image Slots")]
    static void SelectTheme()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<BedRestTheme>(
            "Assets/Nature  Paradaise/Resources/UI/BedRestTheme.asset");
        if (Selection.activeObject != null) EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
