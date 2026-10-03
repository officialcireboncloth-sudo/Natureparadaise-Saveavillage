#if UNITY_EDITOR
using UnityEditor;
public static class FeedMakerUISetup
{
    [MenuItem("Nature Paradise/UI/Feed Maker/Select Image Slots")]
    public static void SelectSlots()
    {
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<FeedMakerTheme>("Assets/Nature  Paradaise/Resources/UI/FeedMakerTheme.asset");
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
#endif
