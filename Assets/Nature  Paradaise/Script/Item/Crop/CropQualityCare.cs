using System;
using UnityEngine;

/// <summary>Riwayat satu siklus panen; data lama tanpa riwayat dimulai konservatif.</summary>
[Serializable]
public sealed class CropQualityCare
{
    public int days;
    public int wateredDays;
    public int boostedDays;
    public int boosterToday;
    public int minimumBooster = 5;
    public int bestBooster;
    public bool correctSeason = true;
    public bool pestDamage;
    public int readyDay = -1;
    public float roll = -1f;
    public CropQualityCare Copy() => (CropQualityCare)MemberwiseClone();

    public int RollStars(CropDataSO crop, bool fertile, int today)
    {
        if (roll < 0f) roll = UnityEngine.Random.value;
        bool dailyWater = days > 0 && wateredDays == days;
        bool dailyBooster = days > 0 && boostedDays == days;
        bool onTime = readyDay >= 0 && today <= readyDay + crop.harvestGraceDays;
        float total = 0f;
        float[] eligible = new float[5];
        for (int index = 0; index < 5; index++)
        {
            int requirement = crop.minimumBoosterForStars != null && index < crop.minimumBoosterForStars.Length
                ? crop.minimumBoosterForStars[index] : index == 0 ? 0 : index == 1 ? 1 : index == 2 ? 2 : 3;
            bool allowed = index == 0 || (correctSeason && dailyWater && bestBooster >= requirement &&
                (index < 2 || (dailyBooster && minimumBooster >= requirement && !pestDamage)) &&
                (index < 3 || (onTime && fertile)));
            if (!allowed) continue;
            float weight = crop.qualityWeights != null && index < crop.qualityWeights.Length
                ? Mathf.Max(0f, crop.qualityWeights[index]) : 1f;
            eligible[index] = weight * (1f + index * bestBooster * 0.2f);
            total += eligible[index];
        }
        float target = Mathf.Clamp01(roll) * total;
        if (total <= 0f) return 1;
        for (int index = 0; index < 5; index++)
        {
            if (eligible[index] <= 0f) continue;
            target -= eligible[index];
            if (target <= 0f) return index + 1;
        }
        return 1;
    }
}
