using System;
namespace UnityEngine
{
    public static class Random { public static float value => 0.5f; }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp01(float x) => Math.Min(1, Math.Max(0, x));
    }
}
public class CropDataSO
{
    public int[] minimumBoosterForStars = { 0, 1, 2, 3, 3 };
    public float[] qualityWeights = { 50, 30, 14, 5, 1 };
    public int harvestGraceDays;
}
class FertilizerQualityTests
{
    static int assertions;
    static void Check(bool valid, string name)
    {
        assertions++;
        if (!valid) throw new Exception(name);
    }
    static CropQualityCare Care(int level) => new CropQualityCare
    {
        days = 5, wateredDays = 5, boostedDays = 5, minimumBooster = level,
        bestBooster = level, readyDay = 6, roll = 0.999f
    };
    static void Main()
    {
        var crop = new CropDataSO();
        Check(Care(0).RollStars(crop, true, 6) == 1, "No booster: one star");
        Check(Care(1).RollStars(crop, true, 6) == 2, "Basic ceiling");
        Check(Care(2).RollStars(crop, true, 6) == 3, "Organic ceiling");
        Check(Care(3).RollStars(crop, true, 6) == 5, "Premium draft can reach five");
        var dry = Care(5); dry.wateredDays = 4;
        Check(dry.RollStars(crop, true, 6) == 1, "Missed water");
        var missed = Care(5); missed.boostedDays = 4;
        Check(missed.RollStars(crop, true, 6) == 2, "Missed booster");
        var pest = Care(5); pest.pestDamage = true;
        Check(pest.RollStars(crop, true, 6) == 2, "Pest caps quality");
        Check(Care(5).RollStars(crop, false, 6) == 3, "Poor soil");
        Check(Care(5).RollStars(crop, true, 7) == 3, "Late harvest");
        var season = Care(5); season.correctSeason = false;
        Check(season.RollStars(crop, true, 6) == 1, "Wrong season");
        var snapshot = Care(4); var copy = snapshot.Copy(); copy.boosterToday = 3;
        Check(snapshot.boosterToday == 0, "Save snapshot independent");
        for (int i = 0; i < 1000; i++)
        {
            var care = Care(5); care.roll = i / 1000f;
            int first = care.RollStars(crop, true, 6);
            Check(first >= 1 && first <= 5, "Valid stars");
            Check(first == care.RollStars(crop, true, 6), "Retry preserves roll");
        }
        Console.WriteLine(assertions + " quality assertions passed (pure logic, not Unity Play Mode).");
    }
}
