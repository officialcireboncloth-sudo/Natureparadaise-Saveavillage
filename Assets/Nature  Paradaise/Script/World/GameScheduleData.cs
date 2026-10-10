using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CalendarEventKind { Festival, Birthday, SeasonDay }
[Serializable] public class CalendarDateRecord
{
    public int absoluteDay, year, season, day, weekday;
    public string note;
}
[Serializable] public class WeatherDateRecord { public int absoluteDay; public bool useOverride; public WeatherType weather; }
[Serializable] public class CalendarEventRecord
{
    public string id, title, description, location;
    [Tooltip("0 = setiap tahun; 1–10 = tahun khusus.")] public int year;
    public int season, day;
    public float startHour=9,endHour=17;
    public CalendarEventKind kind;
    public Sprite illustration, icon;
}
[CreateAssetMenu(menuName="Nature Paradise/Data/Game Schedules")]
public class GameScheduleData : ScriptableObject
{
    public const int DaysPerSeason=28, DaysPerYear=112, AuthoredYears=10;
    public List<CalendarDateRecord> dates=new();
    public List<WeatherDateRecord> weather=new();
    public List<CalendarEventRecord> events=new();
    public static GameScheduleData Load()=>Resources.Load<GameScheduleData>("Game Schedules");
    public static int AbsoluteDay(int year,int season,int day)=>(year-1)*DaysPerYear+season*DaysPerSeason+day;
    public static string SeasonName(int season)=>new[]{"Musim Semi","Musim Panas","Musim Gugur","Musim Dingin"}[Mathf.Clamp(season,0,3)];
    public static void Date(int absolute,out int year,out int season,out int day){int d=Mathf.Max(0,absolute-1);year=d/DaysPerYear+1;season=d%DaysPerYear/DaysPerSeason;day=d%DaysPerSeason+1;}
    public bool TryWeather(int absoluteDay,out WeatherType value)
    {
        var row=weather.FirstOrDefault(r=>r.absoluteDay==absoluteDay&&r.useOverride);
        value=row!=null?row.weather:WeatherType.Sunny;return row!=null;
    }
    public IEnumerable<CalendarEventRecord> Events(int year,int season,int day)=>events.Where(e=>(e.year==0||e.year==year)&&e.season==season&&e.day==day);
}
