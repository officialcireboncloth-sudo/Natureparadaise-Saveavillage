using System;

namespace UnityEngine
{
    public class ScriptableObject {}
    public class GameObject {}
    public class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) {} }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public struct Vector3
    {
        public static Vector3 one => new Vector3();
        public static Vector3 operator *(Vector3 value, float scale) => value;
    }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Clamp(int x, int a, int b) => Math.Min(b, Math.Max(a, x));
        public static float Clamp01(float x) => Math.Min(1, Math.Max(0, x));
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
    }
}

public class ItemSO {}
[Flags] public enum CropSeason { None=0, Spring=1, Summer=2, All=15 }
public enum WeatherType { Sunny }
static class WeatherSystem
{
    public static bool IsRainWeather(WeatherType weather) => false;
    public static bool IsStormWeather(WeatherType weather) => false;
}
static class CropDataSO
{
    public static CropSeason GetSeasonForDay(int day) => day < 29 ? CropSeason.Spring : CropSeason.Summer;
}

static class Probe
{
    static void Require(bool passed, string label)
    {
        if (!passed) throw new Exception(label);
    }
    static void Main()
    {
        var definition = new TreeDefinition
        {
            useFruitCare = true, fruitItem = new ItemSO(), matureDays = 60,
            floweringDays = 2, fruitGrowthDays = 3, fruitSeasons = CropSeason.Summer,
            stages = new[] { new TreeStageVisual(), new TreeStageVisual { fromGrowthDay = 6 },
                             new TreeStageVisual { fromGrowthDay = 20 }, new TreeStageVisual(), new TreeStageVisual() }
        };
        var progress = TreeProgress.Create(definition, 1, false);
        foreach (int expected in new[] { 1, 2, 3, 4 })
        {
            Require(progress.TryDebugAdvanceFruitStage(definition, false), "step advances");
            Require(definition.StageIndex(progress.growth, progress.fruitStage) == expected, "one visual stage");
        }
        Require(progress.fruitProgress == 5 && progress.growth == 60 && progress.lastProcessedDay == 0,
            "ready without changing calendar");
        Require(progress.Care.days == 0 && progress.Care.Stars == 1, "skipping did not create care samples");
        Require(!progress.TryDebugAdvanceFruitStage(definition, false), "ready is terminal");
        progress.Advance(definition, 2, WeatherType.Sunny, false, 1);
        Require(progress.fruitStage == TreeFruitStage.Dormant, "normal growth still respects season");

        var regrowing = TreeProgress.Create(definition, 30, true);
        regrowing.fruitStage = TreeFruitStage.Flowering;
        regrowing.care = new FruitTreeCare { version=1, days=2, boostedDays=2, minimumSoil=80 };
        Require(regrowing.TryDebugAdvanceFruitStage(definition, true) && regrowing.fruitStage == TreeFruitStage.Ready,
            "direct ready during regrowth");
        Require(regrowing.Care.days == 2 && regrowing.Care.Stars == 5, "real care preserved");

        var legacy = TreeProgress.Create(definition, 1, false);
        legacy.growth = 4;
        Require(legacy.TryDebugAdvanceFruitStage(definition, true) && legacy.Care.days == 4,
            "legacy care derives from real growth before skip");
        definition.wildTree = true;
        Require(!TreeProgress.Create(definition, 1, false).TryDebugAdvanceFruitStage(definition, true),
            "wild tree rejected");
        Console.WriteLine("PASS four visual advances, direct ready, ready no-op, save progress, care and seasonal reset.");
    }
}
