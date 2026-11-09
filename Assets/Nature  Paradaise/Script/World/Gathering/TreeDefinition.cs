using System;
using UnityEngine;

public enum TreeLifeStage { Seed, Sprout, Sapling, Young, Growing, Mature, FruitGrowing, HarvestReady }
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
    [Min(1)] public int matureDays = 60;
    public bool wildTree;
    public ItemSO woodItem;
    public ItemSO fruitItem;
    public bool useFruitCare;
    public ItemSO mutationFruitItem;
    [Range(0f,1f)] public float fruitMutationChance=.01f;
    public int[] fruitAmountsByStars={5,7,10,15,20};
    public bool UsesFruitCare=>useFruitCare&&!wildTree&&fruitItem!=null;
    public int HarvestAmount(int stars)=>UsesFruitCare&&fruitAmountsByStars!=null&&fruitAmountsByStars.Length==5
        ?Mathf.Max(1,fruitAmountsByStars[Mathf.Clamp(stars,1,5)-1]):Mathf.Max(1,fruitAmount);
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

    public int StageIndex(float progress,TreeFruitStage fruit=TreeFruitStage.Dormant)
    {
        if (stages == null || stages.Length == 0) return -1;
        if(UsesFruitCare)
        {
            if(fruit==TreeFruitStage.Ready)return Math.Min(4,stages.Length-1);
            if(fruit==TreeFruitStage.Growing)return Math.Min(3,stages.Length-1);
            if(stages.Length>1&&progress<stages[1].fromGrowthDay)return 0;
            return stages.Length>2&&progress>=stages[2].fromGrowthDay?2:Math.Min(1,stages.Length-1);
        }
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
    public Vector3 VisualScale(float growth,TreeFruitStage fruit)
    {
        int index=StageIndex(growth,fruit);if(index<0)return Vector3.one;
        float multiplier=UsesFruitCare&&index==2?.45f+.55f*Mathf.Clamp01((growth-stages[2].fromGrowthDay)/Mathf.Max(1,matureDays-stages[2].fromGrowthDay)):1;
        return stages[index].scale*multiplier;
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
    public FruitTreeCare care;
    // A legacy tree cannot receive perfect credit for growth that predates the ledger.
    public FruitTreeCare Care=>care??=new FruitTreeCare
    {version=growth>0?1:0,days=Mathf.CeilToInt(Mathf.Max(0,growth)),minimumSoil=growth>0?0:100};
    public TreeProgress Copy(){var copy=(TreeProgress)MemberwiseClone();copy.care=care?.Copy();return copy;}
    public bool Mature(TreeDefinition definition) => growth >= definition.matureDays;

    public static TreeProgress Create(TreeDefinition definition, int day, bool mature) => new()
    {
        plantedDay = mature && definition != null ? day - definition.matureDays : day,
        age = mature && definition != null ? definition.matureDays : 0,
        lastProcessedDay = day - 1,
        growth = mature && definition != null ? definition.matureDays : 0
    };

    public void Advance(TreeDefinition definition, int day, WeatherType weather, bool sprinkler, float damageRoll,int soilFertility=0)
    {
        if (definition == null || day <= lastProcessedDay) return;
        lastProcessedDay = day;
        age = Mathf.Max(0, day - plantedDay + 1);
        if(definition.UsesFruitCare)
        {
            // Old saves with forming/ready fruit already describe mature trees.
            if(fruitStage!=TreeFruitStage.Dormant)growth=Mathf.Max(growth,definition.matureDays);
            bool matureBefore=Mature(definition);
            bool season=(definition.fruitSeasons&CropDataSO.GetSeasonForDay(day))!=0;
            bool active=!matureBefore||season&&fruitStage!=TreeFruitStage.Ready;
            if(active)Care.RecordDay(soilFertility,day);
            Care.FinishDay();
            if(!matureBefore)
            {growth=Mathf.Min(definition.matureDays,growth+1);return;}
            if(!season){fruitProgress=0;fruitStage=TreeFruitStage.Dormant;return;}
            if(fruitStage==TreeFruitStage.Ready)return;
            fruitProgress++;
            fruitStage=fruitProgress>=definition.floweringDays+definition.fruitGrowthDays?TreeFruitStage.Ready:
                fruitProgress>=definition.floweringDays?TreeFruitStage.Growing:TreeFruitStage.Flowering;
            return;
        }
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
