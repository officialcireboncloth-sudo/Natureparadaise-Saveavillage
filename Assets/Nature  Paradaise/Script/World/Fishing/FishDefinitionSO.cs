using System;
using UnityEngine;

public enum FishRarity : byte { Common, Uncommon, Rare, Legendary }
public enum FishingWaterType : byte { River, Lake, Pond, Ocean }

/// <summary>Berat ikan deterministik dari species dan panjang; dipakai Fishing, Inventory, dan Aquarium.</summary>
public static class FishMeasurement
{
    static FishDefinitionSO[] definitions;

    public static float EstimateWeightKg(ItemSO item, float sizeCm)
    {
        uint hash = 2166136261;
        string id = item != null ? item.Id : string.Empty;
        for (int index = 0; index < id.Length; index++) hash = (hash ^ id[index]) * 16777619;
        float speciesFactor = 0.85f + hash % 31 / 100f;
        return Mathf.Max(0.05f, Mathf.Pow(Mathf.Max(1f, sizeCm) / 30f, 3f) * 0.55f * speciesFactor);
    }

    public static FishDefinitionSO FindDefinition(ItemSO item)
    {
        if (item == null) return null;
        definitions ??= Resources.LoadAll<FishDefinitionSO>("Fishing");
        for (int index = 0; index < definitions.Length; index++)
            if (definitions[index] != null && definitions[index].item == item) return definitions[index];
        return null;
    }

    public static FishSizeTier GetSizeTier(ItemSO item, float sizeCm)
    {
        FishDefinitionSO definition = FindDefinition(item);
        float medium = definition != null ? definition.PondMediumSizeCm : 22f;
        float large = definition != null ? definition.PondLargeSizeCm : 35f;
        float jumbo = definition != null ? definition.PondJumboSizeCm : 50f;
        return sizeCm < medium ? FishSizeTier.Small : sizeCm < large ? FishSizeTier.Medium :
            sizeCm < jumbo ? FishSizeTier.Large : FishSizeTier.Jumbo;
    }

    public static float SizePriceMultiplier(ItemSO item, float sizeCm) => GetSizeTier(item, sizeCm) switch
    {
        FishSizeTier.Medium => 1.25f,
        FishSizeTier.Large => 1.6f,
        FishSizeTier.Jumbo => 2f,
        _ => 1f
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => definitions = null;
}

public enum FishSizeTier : byte { Small, Medium, Large, Jumbo }

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

    [Header("Fish Pond Growth")]
    [Tooltip("Batas panjang untuk berubah dari Small menjadi Medium.")]
    [Min(0.1f)] public float pondMediumSizeCm = 22f;
    [Min(0.1f)] public float pondLargeSizeCm = 35f;
    [Min(0.1f)] public float pondJumboSizeCm = 50f;
    [Tooltip("Hari makan yang diperlukan pada tiap tahap.")]
    [Min(1)] public int pondSmallGrowthDays = 7;
    [Min(1)] public int pondMediumGrowthDays = 10;
    [Min(1)] public int pondLargeGrowthDays = 14;

    public float PondMediumSizeCm => Mathf.Max(minimumSizeCm, pondMediumSizeCm);
    public float PondLargeSizeCm => Mathf.Max(PondMediumSizeCm + 0.1f, pondLargeSizeCm);
    public float PondJumboSizeCm => Mathf.Max(PondLargeSizeCm + 0.1f, pondJumboSizeCm);

    public int GetPondGrowthDays(FishSizeTier tier) => tier switch
    {
        FishSizeTier.Small => Mathf.Max(1, pondSmallGrowthDays),
        FishSizeTier.Medium => Mathf.Max(1, pondMediumGrowthDays),
        FishSizeTier.Large => Mathf.Max(1, pondLargeGrowthDays),
        _ => 0
    };

    public float GetNextPondSize(FishSizeTier tier) => tier switch
    {
        FishSizeTier.Small => PondMediumSizeCm,
        FishSizeTier.Medium => PondLargeSizeCm,
        FishSizeTier.Large => PondJumboSizeCm,
        _ => PondJumboSizeCm
    };

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
        pondMediumSizeCm = Mathf.Max(minimumSizeCm, pondMediumSizeCm);
        pondLargeSizeCm = Mathf.Max(pondMediumSizeCm + 0.1f, pondLargeSizeCm);
        pondJumboSizeCm = Mathf.Max(pondLargeSizeCm + 0.1f, pondJumboSizeCm);
        encounterWeight = Mathf.Max(0.01f, encounterWeight);
    }
}
