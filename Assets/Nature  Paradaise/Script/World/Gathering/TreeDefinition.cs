using System;
using UnityEngine;

public enum TreeLifeStage { Seed, Sprout, Sapling, Young, Growing, Mature }
public enum TreeFruitStage { Dormant, Flowering, Growing, Ready }

[Serializable]
public sealed class TreeStageVisual
{
    public TreeLifeStage stage;
    [Min(0)] public int fromGrowthDay;
    public GameObject model;
    public Vector3 scale = Vector3.one;
}

/// <summary>Balancing dan slot model tiap spesies; umur kalender terpisah dari growth.</summary>
[CreateAssetMenu(menuName = "Game/Farming/Tree Definition")]
public sealed class TreeDefinition : ScriptableObject
{
    public string treeType;
    [Min(1)] public int matureDays = 56;
    public bool wildTree;
    public ItemSO woodItem;
    public ItemSO fruitItem;
    public CropSeason fruitSeasons = CropSeason.All;
    [Min(1)] public int floweringDays = 2;
    [Min(1)] public int fruitGrowthDays = 3;
    [Min(1)] public int fruitAmount = 1;
    [Min(0)] public int waterUntilGrowthDay = 42;
    [Min(0)] public int dryGraceDays = 1;
    [Range(0f, 1f)] public float dryGrowthMultiplier = 0.25f;
    [Range(0f, 1f)] public float youngStormDamageChance = 0.15f;
    [Min(0)] public int stormDamage = 10;
    public CropSeason acceleratedSeasons;
    [Min(1f)] public float seasonalGrowthMultiplier = 1.15f;
    public TreeStageVisual[] stages;

    public int StageIndex(float progress)
    {
        if (stages == null || stages.Length == 0) return -1;
        int best = 0;
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].stage == TreeLifeStage.Mature)
            {
                if (progress >= matureDays) return i;
                continue;
            }
            if (stages[i].fromGrowthDay <= progress && stages[i].fromGrowthDay >= stages[best].fromGrowthDay) best = i;
        }
        return best;
    }
}

/// <summary>Data ringan yang sama untuk pohon scene, item tertanam, dan record Terrain jauh.</summary>
[Serializable]
public sealed class TreeProgress
{
    public int plantedDay = 1;
    public int age;
    public float growth;
    public int lastProcessedDay;
    public int wateredDay = -1;
    public int dryDays;
    public int health = 100;
    public int fruitProgress;
    public TreeFruitStage fruitStage;
    public TreeProgress Copy() => (TreeProgress)MemberwiseClone();
    public bool Mature(TreeDefinition definition) => growth >= definition.matureDays;

    public static TreeProgress Create(TreeDefinition definition, int day, bool mature) => new()
    {
        plantedDay = mature && definition != null ? day - definition.matureDays : day,
        age = mature && definition != null ? definition.matureDays : 0,
        lastProcessedDay = day - 1,
        growth = mature && definition != null ? definition.matureDays : 0
    };

    public void Advance(TreeDefinition definition, int day, WeatherType weather, bool sprinkler, float damageRoll)
    {
        if (definition == null || day <= lastProcessedDay) return;
        lastProcessedDay = day;
        age = Mathf.Max(0, day - plantedDay + 1);
        bool watered = wateredDay == day || WeatherSystem.IsRainWeather(weather) || sprinkler || definition.wildTree;
        dryDays = watered ? 0 : dryDays + 1;
        bool mature = Mature(definition);
        if (!mature)
        {
            if (WeatherSystem.IsStormWeather(weather) && damageRoll < definition.youngStormDamageChance)
                health = Mathf.Max(1, health - definition.stormDamage);
            else if (watered) health = Mathf.Min(100, health + 5);
            float gain = growth >= definition.waterUntilGrowthDay || watered || dryDays <= definition.dryGraceDays
                ? 1f : definition.dryGrowthMultiplier;
            if ((definition.acceleratedSeasons & CropDataSO.GetSeasonForDay(day)) != 0)
                gain *= definition.seasonalGrowthMultiplier;
            growth = Mathf.Min(definition.matureDays, growth + gain * health / 100f);
            return;
        }
        if (definition.fruitItem == null || (definition.fruitSeasons & CropDataSO.GetSeasonForDay(day)) == 0)
        {
            fruitProgress = 0;
            fruitStage = TreeFruitStage.Dormant;
            return;
        }
        if (fruitStage == TreeFruitStage.Ready) return;
        fruitProgress++;
        fruitStage = fruitProgress >= definition.floweringDays + definition.fruitGrowthDays
            ? TreeFruitStage.Ready : fruitProgress >= definition.floweringDays ? TreeFruitStage.Growing : TreeFruitStage.Flowering;
    }
}
