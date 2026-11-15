using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeatherProbability
{
    public WeatherType weather;
    [Range(0,100)] public int percent;
    public WeatherProbability(WeatherType weather,int percent){this.weather=weather;this.percent=percent;}
}
[Serializable]
public sealed class SeasonWeatherDistribution
{
    public CropSeason season;
    [Tooltip("Setiap musim berjumlah 100%. Probabilitas berlaku per hari, bukan jatah wajib per bulan.")]
    public List<WeatherProbability> probabilities=new();
}
/// <summary>Shared authored per-day weather probabilities for the four game months.</summary>
[CreateAssetMenu(menuName="Nature Paradise/Weather/Season Weather Profile")]
public sealed class SeasonWeatherProfile : ScriptableObject
{
    public List<SeasonWeatherDistribution> seasons=Defaults();
    static readonly List<SeasonWeatherDistribution> Fallback=Defaults();
    static List<SeasonWeatherDistribution> Defaults()=>new()
    {
        new(){season=CropSeason.Spring,probabilities=new(){new(WeatherType.Rain,50),new(WeatherType.Sunny,30),new(WeatherType.Drizzle,20)}},
        new(){season=CropSeason.Summer,probabilities=new(){new(WeatherType.Drizzle,10),new(WeatherType.Rain,15),new(WeatherType.Heatwave,75)}},
        new(){season=CropSeason.Autumn,probabilities=new(){new(WeatherType.Rain,10),new(WeatherType.Drizzle,5),new(WeatherType.Sunny,34),new(WeatherType.Cloudy,50),new(WeatherType.WindRainStorm,1)}},
        new(){season=CropSeason.Winter,probabilities=new(){new(WeatherType.Snow,60),new(WeatherType.Sunny,38),new(WeatherType.Blizzard,2)}}
    };
    /// <summary>Evaluates a percentile roll. An absent resource uses the same release defaults.</summary>
    public static WeatherType Evaluate(CropSeason season,int roll)
    {
        var profile=Resources.Load<SeasonWeatherProfile>("Profiles/Season Weather Probabilities");
        var rules=profile!=null?profile.seasons:Fallback;
        var row=rules.Find(rule=>rule.season==season)??Fallback.Find(rule=>rule.season==season)??Fallback[0];
        int total=0;foreach(var p in row.probabilities)total+=Mathf.Max(0,p.percent);
        if(total==0)return WeatherType.Sunny;
        int remaining=Mathf.Clamp(roll,0,99)*total/100;
        foreach(var p in row.probabilities){remaining-=Mathf.Max(0,p.percent);if(remaining<0)return p.weather;}
        return WeatherType.Sunny;
    }
}
