using UnityEditor;
using UnityEngine;
public static class AnimalBellUISetup
{
    [MenuItem("Nature Paradise/UI/Animal Bell/Select Image Slots")]
    static void SelectSlots()
    {
        const string path="Assets/Nature  Paradaise/Resources/UI/AnimalBellTheme.asset";
        var theme=AssetDatabase.LoadAssetAtPath<AnimalBellTheme>(path);if(theme==null){theme=ScriptableObject.CreateInstance<AnimalBellTheme>();AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();}
        Selection.activeObject=theme;EditorGUIUtility.PingObject(theme);
    }
}
