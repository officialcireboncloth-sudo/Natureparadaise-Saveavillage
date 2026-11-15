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
[Serializable] public class WeatherDateRecord { public int absoluteDay; public bool useOverride; public WeatherType weather; public string note; }
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
    public const int MaxDaysPerSeason=31, DaysPerYear=120, AuthoredYears=10, AuthoredDays=DaysPerYear*AuthoredYears;
    static readonly int[] MonthLengths={31,28,31,30};
    static readonly string[] Names={"Musim Hujan","Musim Panas","Musim Gugur","Musim Salju"};
    static readonly string[] EnglishNames={"Spring","Summer","Autumn","Winter"};
    public List<CalendarDateRecord> dates=new();
    public List<WeatherDateRecord> weather=new();
    public List<CalendarEventRecord> events=new();
    public int calendarVersion;
    public static GameScheduleData Load()=>Resources.Load<GameScheduleData>("Game Schedules");
    /// <summary>Length of one of the four game months; no civil twelve-month calendar is used.</summary>
    public static int DaysInSeason(int season)=>MonthLengths[Mathf.Clamp(season,0,3)];
    /// <summary>Converts a valid game date to elapsed days, preserving the clock/save day counter.</summary>
    public static int AbsoluteDay(int year,int season,int day)
    {
        if(year<1||season<0||season>3||day<1||day>DaysInSeason(season))throw new ArgumentOutOfRangeException("Invalid game date");
        int result=(year-1)*DaysPerYear+day;for(int i=0;i<season;i++)result+=MonthLengths[i];return result;
    }
    public static string SeasonName(int season)=>Names[Mathf.Clamp(season,0,3)];
    public static string SeasonName(CropSeason season)=>SeasonName(season switch{CropSeason.Summer=>1,CropSeason.Autumn=>2,CropSeason.Winter=>3,_=>0});
    public static string SeasonEnglishName(int season)=>EnglishNames[Mathf.Clamp(season,0,3)];
    /// <summary>Resolves an elapsed day into the shared 31/28/31/30 calendar.</summary>
    public static void Date(int absolute,out int year,out int season,out int day)
    {
        int elapsed=Mathf.Max(0,absolute-1);year=elapsed/DaysPerYear+1;day=elapsed%DaysPerYear;season=0;
        while(season<3&&day>=MonthLengths[season])day-=MonthLengths[season++];day++;
    }
    public static int SeasonIndex(int absolute){Date(absolute,out int year,out int season,out _);return(year-1)*4+season;}
    public bool TryWeather(int absoluteDay,out WeatherType value)
    {
        var row=weather.FirstOrDefault(r=>r.absoluteDay==absoluteDay&&r.useOverride);
        value=row!=null?row.weather:WeatherType.Sunny;return row!=null;
    }
    public IEnumerable<CalendarEventRecord> Events(int year,int season,int day)=>events.Where(e=>(e.year==0||e.year==year)&&e.season==season&&e.day==day);
}
