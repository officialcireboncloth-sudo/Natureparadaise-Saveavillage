using UnityEditor;

public static class WeatherForecastTVUISetup
{
    [MenuItem("Nature Paradise/UI/Television/Select Image Slots")]
    static void SelectTheme()
    {
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<WeatherForecastTVTheme>(
            "Assets/Nature  Paradaise/Resources/UI/WeatherForecastTVTheme.asset");
        if(Selection.activeObject!=null)EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
