using System;
using UnityEngine;

public enum FishRarity : byte { Common, Uncommon, Rare, Legendary }
public enum FishingWaterType : byte { River, Lake, Pond, Ocean }

[Flags]
public enum FishingWaterMask : byte
{
    None = 0,
    River = 1 << 0,
    Lake = 1 << 1,
    Pond = 1 << 2,
    Ocean = 1 << 3,
    All = River | Lake | Pond | Ocean
}

[Flags]
public enum FishingWeatherMask
{
    None = 0,
    Sunny = 1 << (int)WeatherType.Sunny,
    PartlyCloudy = 1 << (int)WeatherType.PartlyCloudy,
    Heatwave = 1 << (int)WeatherType.Heatwave,
    Drizzle = 1 << (int)WeatherType.Drizzle,
    Rain = 1 << (int)WeatherType.Rain,
    HeavyRain = 1 << (int)WeatherType.HeavyRain,
    WindRainStorm = 1 << (int)WeatherType.WindRainStorm,
    Cyclone = 1 << (int)WeatherType.Cyclone,
    Thunderstorm = 1 << (int)WeatherType.Thunderstorm,
    Snow = 1 << (int)WeatherType.Snow,
    Blizzard = 1 << (int)WeatherType.Blizzard,
    All = (1 << 11) - 1
}

[CreateAssetMenu(menuName = "Game/Fishing/Fish Definition")]
/// <summary>Data balance satu ikan. Visual item tetap berada pada ItemSO.</summary>
public sealed class FishDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string fishId = "fish.new";
    public ItemSO item;
    public FishRarity rarity;

    [Header("Availability")]
    public FishingWaterMask waterTypes = FishingWaterMask.All;
    public CropSeason seasons = CropSeason.All;
    public FishingWeatherMask weather = FishingWeatherMask.All;
    [Range(0, 23)] public int startHour;
    [Range(0, 24)] public int endHour = 24;
    [Min(1)] public int requiredRodLevel = 1;
    [Min(0.01f)] public float encounterWeight = 1f;

    [Header("Catch")]
    [Min(0.1f)] public float minimumBiteWait = 2f;
    [Min(0.1f)] public float maximumBiteWait = 5f;
    [Min(0.2f)] public float hookWindow = 1.25f;
    [Range(0.08f, 0.8f)] public float catchZoneSize = 0.38f;
    [Min(0.05f)] public float fishMoveSpeed = 0.35f;
    [Min(0.01f)] public float progressGainPerSecond = 0.28f;
    [Min(0f)] public float progressLossPerSecond = 0.12f;
    [Min(3f)] public float timeLimit = 24f;

    [Header("Size")]
    [Min(0.1f)] public float minimumSizeCm = 12f;
    [Min(0.1f)] public float maximumSizeCm = 35f;

    public bool IsAvailable(FishingWaterType waterType, int day, int hour, WeatherType currentWeather, int rodLevel)
    {
        FishingWaterMask location = (FishingWaterMask)(1 << (int)waterType);
        CropSeason season = CropDataSO.GetSeasonForDay(Mathf.Max(1, day), 28);
        bool timeAllowed = startHour <= endHour
            ? hour >= startHour && hour < endHour
            : hour >= startHour || hour < endHour;
        return item != null && item.category == ItemCategory.Fish &&
               (waterTypes & location) != 0 && (seasons & season) != 0 &&
               (weather & (FishingWeatherMask)(1 << (int)currentWeather)) != 0 &&
               timeAllowed && rodLevel >= requiredRodLevel;
    }

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(fishId)) fishId = $"fish.{name.ToLowerInvariant().Replace(' ', '_')}";
        maximumBiteWait = Mathf.Max(minimumBiteWait, maximumBiteWait);
        maximumSizeCm = Mathf.Max(minimumSizeCm, maximumSizeCm);
        encounterWeight = Mathf.Max(0.01f, encounterWeight);
    }
}
