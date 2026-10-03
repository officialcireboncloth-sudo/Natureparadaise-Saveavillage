using UnityEditor;

public static class AnimalInteractionUISetup
{
    [MenuItem("Nature Paradise/UI/Animal Interaction/Select Image Slots")]
    static void SelectTheme()
    {
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<AnimalInteractionTheme>(
            "Assets/Nature  Paradaise/Resources/UI/AnimalInteractionTheme.asset");
        if(Selection.activeObject!=null) EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
