using System;
using UnityEngine;

/// <summary>Snapshot immutable yang dibagikan ke seluruh modul ketika cuaca aktif berubah.</summary>
public readonly struct WeatherImpactSnapshot
{
    public readonly WeatherType Weather;
    public readonly int Day;
    public readonly float SunMultiplier;
    public readonly float AmbientMultiplier;
    public readonly float SkyExposureMultiplier;
    public readonly Color LightingTint;
    public readonly float FogDensity;
    public readonly float PrecipitationIntensity;
    public readonly float WindIntensity;
    public readonly bool NpcOutdoorActivitiesAllowed;
    public readonly float FarmingMultiplier;
    public readonly float HuntingMultiplier;
    public readonly float FishingMultiplier;

    WeatherImpactSnapshot(WeatherType weather, int day, float sun, float ambient, float exposure,
        Color tint, float fog, float precipitation, float wind, bool npcOutdoorAllowed,
        float farming, float hunting, float fishing)
    {
        Weather = weather;
        Day = day;
        SunMultiplier = sun;
        AmbientMultiplier = ambient;
        SkyExposureMultiplier = exposure;
        LightingTint = tint;
        FogDensity = fog;
        PrecipitationIntensity = precipitation;
        WindIntensity = wind;
        NpcOutdoorActivitiesAllowed = npcOutdoorAllowed;
        FarmingMultiplier = farming;
        HuntingMultiplier = hunting;
        FishingMultiplier = fishing;
    }

    public static WeatherImpactSnapshot Create(WeatherSystem system)
    {
        WeatherType weather = system != null ? system.CurrentWeather : WeatherType.Sunny;
        int day = system != null ? system.CurrentWeatherDay : 1;
        float sun = 1f, ambient = 1f, exposure = 1f;
        Color tint = Color.white;
        system?.GetLightingModifiers(out sun, out ambient, out exposure, out tint);

        return weather switch
        {
            WeatherType.PartlyCloudy => New(weather, day, sun, ambient, exposure, tint, 0.001f, 0f, 0.1f, true, 1f, 1.05f, 1.05f),
            WeatherType.Heatwave => New(weather, day, sun, ambient, exposure, tint, 0f, 0f, 0.15f, true, 0.8f, 0.8f, 0.75f),
            WeatherType.Drizzle => New(weather, day, sun, ambient, exposure, tint, 0.003f, 0.3f, 0.15f, true, 1f, 0.95f, 1.1f),
            WeatherType.Rain => New(weather, day, sun, ambient, exposure, tint, 0.006f, 0.55f, 0.25f, true, 1f, 0.8f, 1.2f),
            WeatherType.HeavyRain => New(weather, day, sun, ambient, exposure, tint, 0.011f, 0.85f, 0.45f, false, 1f, 0.45f, 0.7f),
            WeatherType.WindRainStorm => New(weather, day, sun, ambient, exposure, tint, 0.016f, 1f, 0.85f, false, 1f, 0.15f, 0.2f),
            WeatherType.Cyclone => New(weather, day, sun, ambient, exposure, tint, 0.022f, 1f, 1f, false, 1f, 0f, 0f),
            WeatherType.Thunderstorm => New(weather, day, sun, ambient, exposure, tint, 0.018f, 1f, 0.75f, false, 1f, 0.1f, 0.15f),
            WeatherType.Snow => New(weather, day, sun, ambient, exposure, tint, 0.004f, 0.35f, 0.1f, true, 1f, 1f, 1f),
            WeatherType.Blizzard => New(weather, day, sun, ambient, exposure, tint, 0.024f, 1f, 1f, false, 1f, 0.1f, 0.1f),
            _ => New(weather, day, sun, ambient, exposure, tint, 0f, 0f, 0f, true, 1f, 1f, 1f)
        };
    }

    static WeatherImpactSnapshot New(WeatherType weather, int day, float sun, float ambient, float exposure,
        Color tint, float fog, float precipitation, float wind, bool npcOutdoorAllowed,
        float farming, float hunting, float fishing)
        => new(weather, day, sun, ambient, exposure, tint, fog, precipitation, wind,
            npcOutdoorAllowed, farming, hunting, fishing);
}

/// <summary>
/// Dispatcher cuaca lintas sistem. Channel dipanggil berurutan sesuai flow desain dan
/// satu receiver yang error tidak menghentikan modul berikutnya.
/// </summary>
public static class WeatherImpactFlow
{
    public static WeatherImpactSnapshot Current { get; private set; }
    public static bool HasCurrent { get; private set; }

    public static event Action<WeatherImpactSnapshot> LightingUpdated;
    public static event Action<WeatherImpactSnapshot> SkyUpdated;
    public static event Action<WeatherImpactSnapshot> AudioUpdated;
    public static event Action<WeatherImpactSnapshot> NpcUpdated;
    public static event Action<WeatherImpactSnapshot> FarmingUpdated;
    public static event Action<WeatherImpactSnapshot> HuntingUpdated;
    public static event Action<WeatherImpactSnapshot> FishingUpdated;
    public static event Action<WeatherImpactSnapshot> GameplayReady;

    public static void Publish(WeatherImpactSnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        InvokeSafe(LightingUpdated, snapshot, "Lighting");
        InvokeSafe(SkyUpdated, snapshot, "Sky");
        InvokeSafe(AudioUpdated, snapshot, "Audio");
        InvokeSafe(NpcUpdated, snapshot, "NPC");
        InvokeSafe(FarmingUpdated, snapshot, "Farming");
        InvokeSafe(HuntingUpdated, snapshot, "Hunting");
        InvokeSafe(FishingUpdated, snapshot, "Fishing");
        InvokeSafe(GameplayReady, snapshot, "GameplayReady");
    }

    static void InvokeSafe(Action<WeatherImpactSnapshot> handlers, WeatherImpactSnapshot snapshot, string channel)
    {
        if (handlers == null) return;
        foreach (Delegate entry in handlers.GetInvocationList())
        {
            try { ((Action<WeatherImpactSnapshot>)entry)(snapshot); }
            catch (Exception exception) { Debug.LogException(new Exception($"Weather impact channel {channel} gagal.", exception)); }
        }
    }
}
