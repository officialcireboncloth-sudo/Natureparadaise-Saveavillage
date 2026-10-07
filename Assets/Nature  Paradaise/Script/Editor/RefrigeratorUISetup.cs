using UnityEditor;
using UnityEngine;
public static class RefrigeratorUISetup
{
    [MenuItem("Nature Paradise/UI/Refrigerator/Select Image Slots")]
    static void SelectSlots()
    {
        const string path="Assets/Nature  Paradaise/Resources/UI/RefrigeratorTheme.asset";
        var theme=AssetDatabase.LoadAssetAtPath<StorageChestTheme>(path);
        if(theme==null){theme=ScriptableObject.CreateInstance<StorageChestTheme>();AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();}
        Selection.activeObject=theme;EditorGUIUtility.PingObject(theme);
    }
}
