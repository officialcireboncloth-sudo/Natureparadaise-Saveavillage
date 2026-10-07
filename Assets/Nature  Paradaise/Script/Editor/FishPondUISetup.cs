using UnityEditor;
using UnityEngine;
public static class FishPondUISetup
{
    [MenuItem("Nature Paradise/UI/Fish Pond/Select Image Slots")]
    static void SelectSlots()
    {
        const string path="Assets/Nature  Paradaise/Resources/UI/FishPondTheme.asset";
        var theme=AssetDatabase.LoadAssetAtPath<FishPondTheme>(path);if(theme==null){theme=ScriptableObject.CreateInstance<FishPondTheme>();AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();}
        Selection.activeObject=theme;EditorGUIUtility.PingObject(theme);
    }
}
