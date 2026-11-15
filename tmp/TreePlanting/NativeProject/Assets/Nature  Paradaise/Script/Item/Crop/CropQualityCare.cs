using System;

/// <summary>Exact daily care ledger; only presentation rounds the 40/40/20 contributions.</summary>
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
    public float mutationRoll=-1f, doubleYieldRoll=-1f;
    public bool harvestRollsReady;
    public int qualityVersion, targetGrowthDays;
    public long soilFertilityTotal;
    public int fertilizerDaysRemaining;
    public int lastSampleDay = -1;
    public CropQualityCare Copy() => (CropQualityCare)MemberwiseClone();
    public bool FertilizerActive => boosterToday > 0;
    public int ActiveDaysRemaining => FertilizerActive ? Math.Max(1, fertilizerDaysRemaining) : 0;
    public int DenominatorDays => Math.Max(1, Math.Max(targetGrowthDays, days));
    public double FertilizerPercent => 40d * Math.Min(days, Math.Max(0, boostedDays)) / DenominatorDays;
    public double SoilPercent => 40d * Math.Min(80L * days, Math.Max(0, soilFertilityTotal)) / (80L * DenominatorDays);
    public double WaterPercent => 20d * Math.Min(days, Math.Max(0, wateredDays)) / DenominatorDays;
    public double QualityPercent => Math.Min(100d, FertilizerPercent + SoilPercent + WaterPercent);
    public bool IsPerfect => qualityVersion > 0 && days >= Math.Max(1, targetGrowthDays) &&
        wateredDays == days && boostedDays == days && soilFertilityTotal == 80L * days;

    public bool ApplyFertilizer(int level, int durationDays)
    {
        if (FertilizerActive || level < 1 || durationDays < 1) return false;
        boosterToday = level; fertilizerDaysRemaining = durationDays; return true;
    }
    public void RecordDay(bool watered, int soilFertility, int expectedDays, int calendarDay)
    {
        if (lastSampleDay == calendarDay) return;
        lastSampleDay = calendarDay; qualityVersion = 1;
        if (targetGrowthDays <= 0) targetGrowthDays = Math.Max(1, expectedDays);
        days++;
        if (watered) wateredDays++;
        if (FertilizerActive)
        {
            boostedDays++;
            minimumBooster = Math.Min(minimumBooster, boosterToday);
            bestBooster = Math.Max(bestBooster, boosterToday);
        }
        // Unrecorded soil history in old saves remains zero, never invented perfect care.
        soilFertilityTotal += Math.Max(0, Math.Min(80, soilFertility));
    }
    public void FinishDay()
    {
        fertilizerDaysRemaining = Math.Max(0, ActiveDaysRemaining - 1);
        if (fertilizerDaysRemaining == 0) boosterToday = 0;
    }
    public int Stars
    {
        get
        {
            if (IsPerfect) return 5;
            long numerator = Math.Min(days, Math.Max(0, boostedDays)) * 3200L +
                Math.Min(80L * days, Math.Max(0, soilFertilityTotal)) * 40L +
                Math.Min(days, Math.Max(0, wateredDays)) * 1600L;
            long denominator = 80L * DenominatorDays;
            return numerator < 40L * denominator ? 1 : numerator < 60L * denominator ? 2 :
                numerator < 80L * denominator ? 3 : 4;
        }
    }

    public int RollStars(CropDataSO crop, bool fertile, int today)
        => RollStars(crop, fertile, today, 0f);

    public int RollStars(CropDataSO crop, bool fertile, int today, float soilContribution)
    {
        if (targetGrowthDays <= 0 && crop != null) targetGrowthDays = Math.Max(1, (int)Math.Ceiling(crop.TotalGrowthDays));
        return Stars;
    }
}
