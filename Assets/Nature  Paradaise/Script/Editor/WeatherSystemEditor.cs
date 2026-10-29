using UnityEditor;
using UnityEngine;

/// <summary>Explicit Inspector weather changes become manual overrides rather than being silently regenerated on Play.</summary>
[CustomEditor(typeof(WeatherSystem))]
public sealed class WeatherSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        int previous=serializedObject.FindProperty("currentWeather").enumValueIndex;
        int previousMode=serializedObject.FindProperty("forecastMode").enumValueIndex;
        EditorGUI.BeginChangeCheck();DrawPropertiesExcluding(serializedObject,"m_Script");bool changed=EditorGUI.EndChangeCheck();
        var mode=serializedObject.FindProperty("forecastMode");var current=serializedObject.FindProperty("currentWeather");
        bool weatherChanged=current.enumValueIndex!=previous;
        bool manual=weatherChanged||mode.enumValueIndex==(int)WeatherForecastMode.Manual;
        if(weatherChanged||mode.enumValueIndex!=previousMode&&manual)
        {mode.enumValueIndex=(int)WeatherForecastMode.Manual;serializedObject.FindProperty("manualWeather").enumValueIndex=current.enumValueIndex;}
        var selected=(WeatherType)current.enumValueIndex;bool modeChanged=mode.enumValueIndex!=previousMode;
        serializedObject.ApplyModifiedProperties();
        if(changed&&Application.isPlaying&&(weatherChanged||modeChanged))
        {var weather=(WeatherSystem)target;if(manual)weather.SetDebugWeather(selected);else weather.FollowSeasonWeather();}
        var active=(WeatherSystem)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Sumber cuaca: "+active.ForecastSource+"\nCSV yang valid otomatis diimpor setelah file disimpan dan Unity melakukan Refresh. Manual tetap mengalahkan CSV sampai Automatic dipilih.",manual?MessageType.Warning:MessageType.Info);
        if(GUILayout.Button("Gunakan CSV / Probabilitas Musim (Automatic)"))
        {
            if(Application.isPlaying)active.FollowSeasonWeather();
            else
            {
                serializedObject.Update();serializedObject.FindProperty("forecastMode").enumValueIndex=(int)WeatherForecastMode.Automatic;
                serializedObject.FindProperty("currentWeatherDay").intValue=-1;serializedObject.ApplyModifiedProperties();
            }
        }
        if(GUILayout.Button("Import Ulang CSV Cuaca"))WeatherCsvDatabase.Import();
    }
}
