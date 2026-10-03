using System;
using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/UI/Weather Forecast TV Theme")]
public sealed class WeatherForecastTVTheme : ScriptableObject
{
    [Header("Optional image slots")]
    public Sprite panel, logo, leafIcon, forecastIcon, newsIcon;
    public Sprite rainIcon, windIcon, villageNewsIcon;
    public WeatherArtwork[] weatherIcons=Array.Empty<WeatherArtwork>();
    [Serializable] public sealed class WeatherArtwork { public WeatherType weather; public Sprite icon; }
    public Sprite Icon(WeatherType weather)
    {
        foreach(var entry in weatherIcons)if(entry!=null&&entry.weather==weather)return entry.icon;
        return null;
    }
}
