using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Upgrades authored calendar data and its existing prefab without opening the world scene.</summary>
public static class CalendarWeatherRevisionSetup
{
    [MenuItem("Nature Paradise/Data CSV/Schedules and Cafe/Upgrade Four Month Calendar")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play before upgrading calendar assets.");
        var schedule=AssetDatabase.LoadAssetAtPath<GameScheduleData>(ScheduleCafeCsvDatabase.SchedulePath);
        if(schedule==null)throw new System.InvalidOperationException("Existing game schedule is required.");
        if(schedule.calendarVersion<2||schedule.dates.Count!=GameScheduleData.AuthoredDays)
        {
            var priorDates=schedule.dates.ToDictionary(row=>(row.year,row.season,row.day));
            var priorWeather=schedule.weather.ToDictionary(row=>row.absoluteDay);
            var dates=new List<CalendarDateRecord>();var weather=new List<WeatherDateRecord>();
            for(int absolute=1;absolute<=GameScheduleData.AuthoredDays;absolute++)
            {
                GameScheduleData.Date(absolute,out int year,out int season,out int day);priorDates.TryGetValue((year,season,day),out var oldDate);
                dates.Add(new(){absoluteDay=absolute,year=year,season=season,day=day,weekday=(absolute-1)%7,note=oldDate?.note??""});
                WeatherDateRecord oldWeather=null;if(oldDate!=null)priorWeather.TryGetValue(oldDate.absoluteDay,out oldWeather);
                weather.Add(new(){absoluteDay=absolute,useOverride=oldWeather?.useOverride??false,weather=oldWeather?.weather??WeatherType.Sunny});
            }
            schedule.dates=dates;schedule.weather=weather;schedule.calendarVersion=2;EditorUtility.SetDirty(schedule);
        }
        const string profilePath="Assets/Nature  Paradaise/Resources/Profiles/Season Weather Probabilities.asset";
        if(AssetDatabase.LoadAssetAtPath<SeasonWeatherProfile>(profilePath)==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SeasonWeatherProfile>(),profilePath);
        string prefabPath="Assets/Nature  Paradaise/Resources/UI/House Calendar.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try{var calendar=root.GetComponent<CalendarUI>();if(calendar.uiRoot!=null)Object.DestroyImmediate(calendar.uiRoot);calendar.Build();PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        // Export only schedules; unrelated cafe CSV edits remain untouched.
        ScheduleCafeCsvDatabase.ExportCalendar(schedule);
        WeatherCsvDatabase.Export();
        AssetDatabase.Refresh();
    }
}
