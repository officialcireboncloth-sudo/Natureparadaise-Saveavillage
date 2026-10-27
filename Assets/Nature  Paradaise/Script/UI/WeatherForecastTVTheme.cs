using System;
using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Weather Forecast TV Theme")]
public sealed class WeatherForecastTVTheme : ScriptableObject
{
    [Header("Optional image slots")]
    public Sprite panel, logo, leafIcon, forecastIcon, newsIcon;
    public Sprite rainIcon, windIcon, villageNewsIcon;
    [Header("Clear skies after sunset (optional)")]
    public Sprite clearNightIcon, partlyCloudyNightIcon;
    public WeatherArtwork[] weatherIcons=Array.Empty<WeatherArtwork>();
    [Serializable] public sealed class WeatherArtwork { public WeatherType weather; public Sprite icon; }
    public Sprite Icon(WeatherType weather, bool atNight=false)
    {
        if(atNight && weather==WeatherType.Sunny)return clearNightIcon;
        if(atNight && weather==WeatherType.PartlyCloudy)return partlyCloudyNightIcon;
        foreach(var entry in weatherIcons)if(entry!=null&&entry.weather==weather)return entry.icon;
        return null;
    }
}
