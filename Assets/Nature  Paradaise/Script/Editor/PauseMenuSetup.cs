using UnityEditor;
using UnityEngine;

public static class PauseMenuSetup
{
    [MenuItem("Nature Paradise/UI/Pause Menu/Select Image Slots and Keybind")]
    static void SelectTheme()
    {
        var theme = AssetDatabase.LoadAssetAtPath<PauseMenuTheme>("Assets/Nature  Paradaise/Resources/UI/PauseMenuTheme.asset");
        Selection.activeObject = theme;
        if (theme != null) EditorGUIUtility.PingObject(theme);
    }
}
