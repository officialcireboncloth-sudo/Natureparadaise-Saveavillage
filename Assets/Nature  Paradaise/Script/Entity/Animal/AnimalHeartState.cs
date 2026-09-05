using System;

/// <summary>Balancing hubungan jangka panjang; terpisah dari mood harian hewan.</summary>
[Serializable]
public sealed class AnimalHeartRules
{
    public int petPoints = 8;
    public int feedingPoints = 5;
    public int grazingPoints = 3;
    public int healthyDayPoints = 2;
    public int favoriteTreatPoints = 10;
    public int medicinePoints = 8;
    public int consistentCarePoints = 3;
    public int hungryGraceDays = 1;
    public int ignoredGraceDays = 3;
    public int sickGraceDays = 1;
    public int hungerPenalty = 8;
    public int ignoredPenalty = 3;
    public int untreatedPenalty = 5;
    public int rainPenalty = 3;
    public int stormPenalty = 10;
}

/// <summary>State individual yang disalin ke save, termasuk streak dan batas reward harian.</summary>
[Serializable]
public sealed class AnimalHeartState
{
    public int points;
    public int hungryDays;
    public int ignoredDays;
    public int sickDays;
    public int goodCareDays;
    public int lastDailyDay = -1;
    public int lastFeedDay = -1;
    public int lastPetDay = -1;
    public int lastTreatDay = -1;
    public int lastMedicineDay = -1;
    public int grazingDay = -1;
    public int Level => Math.Max(0, Math.Min(1000, points)) / 100;
    public AnimalHeartState Copy() => (AnimalHeartState)MemberwiseClone();
    public void Add(int amount) => points = (int)Math.Max(0L, Math.Min(1000L, (long)points + amount));
    public bool RewardOnce(ref int lastDay, int day, int reward)
    {
        if (lastDay == day) return false;
        lastDay = day;
        Add(Math.Max(0, reward));
        return true;
    }

    public void EndDay(AnimalHeartRules rules, int day, bool fed, bool pet, bool healthy, bool outside, bool rain, bool storm)
    {
        if (day <= lastDailyDay) return;
        lastDailyDay = day;
        hungryDays = fed ? 0 : hungryDays + 1;
        ignoredDays = pet ? 0 : ignoredDays + 1;
        sickDays = healthy ? 0 : sickDays + 1;
        goodCareDays = fed && pet && healthy && !(outside && (rain || storm)) ? goodCareDays + 1 : 0;
        if (fed && healthy) Add(Math.Max(0, rules.healthyDayPoints));
        if (goodCareDays >= 3) Add(Math.Max(0, rules.consistentCarePoints));
        if (grazingDay == day && healthy) Add(Math.Max(0, rules.grazingPoints));
        if (hungryDays > Math.Max(0, rules.hungryGraceDays)) Add(-Math.Max(0, rules.hungerPenalty));
        if (ignoredDays > Math.Max(0, rules.ignoredGraceDays)) Add(-Math.Max(0, rules.ignoredPenalty));
        if (sickDays > Math.Max(0, rules.sickGraceDays)) Add(-Math.Max(0, rules.untreatedPenalty));
        if (outside && storm) Add(-Math.Max(0, rules.stormPenalty));
        else if (outside && rain) Add(-Math.Max(0, rules.rainPenalty));
    }

    public int RollQuality(float happiness, double roll)
    {
        int tier = Level < 3 ? 0 : Level < 5 ? 1 : Level < 7 ? 2 : Level < 9 ? 3 : 4;
        int[,] weights = { {90,9,1,0,0}, {60,30,9,1,0}, {20,40,32,7,1}, {8,18,36,32,6}, {3,7,20,42,28} };
        double mood = Math.Max(0, Math.Min(100, happiness)) / 100d;
        double total = 0;
        for (int i = 0; i < 5; i++) total += weights[tier,i] * Math.Pow(mood, i);
        double target = Math.Max(0, Math.Min(0.99999999, roll)) * total;
        for (int i = 0; i < 5; i++)
        {
            target -= weights[tier,i] * Math.Pow(mood, i);
            if (target < 0) return i + 1;
        }
        return 1;
    }
}
