using UnityEditor;
using UnityEngine;

public static class FishingUISetup
{
    [MenuItem("Nature Paradise/UI/Fishing/Select Image Slots")]
    static void SelectSlots()
    {
        const string path="Assets/Nature  Paradaise/Resources/UI/FishingUITheme.asset";
        var theme=AssetDatabase.LoadAssetAtPath<FishingUITheme>(path);
        if(theme==null){theme=ScriptableObject.CreateInstance<FishingUITheme>();AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();}
        Selection.activeObject=theme;EditorGUIUtility.PingObject(theme);
    }
}
