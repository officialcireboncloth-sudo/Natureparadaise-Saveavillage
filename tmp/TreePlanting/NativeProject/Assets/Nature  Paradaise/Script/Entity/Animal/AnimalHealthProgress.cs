using System;

// Critical ditambahkan di akhir agar nilai enum Recovering pada save lama tetap kompatibel.
public enum AnimalIllnessStage { Healthy, Mild, Severe, Recovering, Critical }
public enum AnimalMedicineResult { Rejected, Improved, Recovering }

/// <summary>Balancing health harian. Peluang 0..1; durasi dalam hari game.</summary>
[Serializable]
public sealed class AnimalHealthRules
{
    public int hungryDaysBeforeRisk = 3;
    public float hungerSicknessChance = 0.35f;
    public int rainyDaysBeforeRisk = 2;
    public float rainSicknessChance = 0.15f;
    public float drizzleSicknessChance = 0.05f;
    public float heavyRainSicknessChance = 0.3f;
    public float stormSicknessChance = 0.5f;
    public float extremeWeatherSicknessChance = 0.8f;
    public int nightRiskStartsAtHour = 20;
    public float nightSicknessChance = 0.12f;
    public int untreatedDaysToSevere = 3;
    public int severeUntreatedDaysToCritical = 2;
    public int recoveryDays = 2;
    public int severeRecoveryDays = 3;
    public int immunityDays = 3;
}

/// <summary>State persisten penyakit; tidak bergantung Unity agar aturan dapat diuji langsung.</summary>
[Serializable]
public sealed class AnimalHealthProgress
{
    public AnimalIllnessStage stage;
    public int hungryDays;
    public int rainyDays;
    public int untreatedDays;
    public int recoveryRemaining;
    public int immunityRemaining;
    public int lastDailyDay = -1;
    public int treatmentDay = -1;
    public bool rainExposure;
    public bool stormExposure;
    public bool extremeExposure;
    public bool nightExposure;
    public float weatherExposureRisk;
    public bool Healthy => stage == AnimalIllnessStage.Healthy;
    public bool CanTreat => stage == AnimalIllnessStage.Mild || stage == AnimalIllnessStage.Severe || stage == AnimalIllnessStage.Critical;
    public AnimalHealthProgress Copy() => (AnimalHealthProgress)MemberwiseClone();
    public void Expose(bool rain, bool storm) { rainExposure |= rain; stormExposure |= storm; }
    public void ExposeWeather(bool rain, bool storm, float risk, bool extreme = false)
    {
        Expose(rain, storm);
        extremeExposure |= extreme;
        if (!float.IsNaN(risk) && !float.IsInfinity(risk)) weatherExposureRisk = Math.Max(weatherExposureRisk, risk);
    }
    public void ExposeNight() => nightExposure = true;
    /// <summary>Dipakai bencana yang menurut desain selalu membuat hewan di luar sakit.</summary>
    public void ForceIllness(AnimalIllnessStage minimumStage)
    {
        if (minimumStage is AnimalIllnessStage.Healthy or AnimalIllnessStage.Recovering) return;
        if (stage == AnimalIllnessStage.Critical) return;
        if (stage == AnimalIllnessStage.Recovering || (int)stage < (int)minimumStage)
            stage = minimumStage;
        untreatedDays = 0;
        recoveryRemaining = 0;
        immunityRemaining = 0;
    }
    public bool Treat(AnimalHealthRules rules, int day)
    {
        if (!CanTreat) return false;
        recoveryRemaining = Math.Max(1, stage == AnimalIllnessStage.Severe ? rules.severeRecoveryDays : rules.recoveryDays);
        stage = AnimalIllnessStage.Recovering;
        treatmentDay = day;
        return true;
    }
    public AnimalMedicineResult Treat(AnimalHealthRules rules, int day, AnimalMedicineLevel medicineLevel)
    {
        if (!CanTreat || medicineLevel == AnimalMedicineLevel.None) return AnimalMedicineResult.Rejected;
        int requiredLevel = stage == AnimalIllnessStage.Critical ? 3 : stage == AnimalIllnessStage.Severe ? 2 : 1;
        int suppliedLevel = (int)medicineLevel;
        treatmentDay = day;
        untreatedDays = 0;
        if (suppliedLevel < requiredLevel)
        {
            stage = stage == AnimalIllnessStage.Critical ? AnimalIllnessStage.Severe : AnimalIllnessStage.Mild;
            recoveryRemaining = 0;
            return AnimalMedicineResult.Improved;
        }

        // Obat yang tepat memerlukan satu Daily Reset dengan pakan dan kandang.
        recoveryRemaining = 1;
        stage = AnimalIllnessStage.Recovering;
        return AnimalMedicineResult.Recovering;
    }
    static double Chance(float value) => float.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
    public void EndDay(AnimalHealthRules rules, int day, bool fed, bool sheltered, float stormChance, double roll)
        => EndDay(rules, day, fed, sheltered, stormChance, rules.nightSicknessChance, roll);
    public void EndDay(AnimalHealthRules rules, int day, bool fed, bool sheltered, float legacyStormChance, float nightChance, double roll)
    {
        if (day <= lastDailyDay) return;
        lastDailyDay = day;
        bool rain = rainExposure, storm = stormExposure, extreme = extremeExposure, night = nightExposure;
        double directWeatherRisk = Chance(weatherExposureRisk);
        rainExposure = stormExposure = extremeExposure = nightExposure = false;
        weatherExposureRisk = 0;
        hungryDays = fed ? 0 : Math.Min(1000000, hungryDays + 1);
        rainyDays = rain ? Math.Min(1000000, rainyDays + 1) : 0;
        if (stage == AnimalIllnessStage.Recovering)
        {
            // Satu hari recovery selesai pada Daily Reset berikutnya jika care terpenuhi.
            if (day >= treatmentDay && fed && sheltered && !rain && !storm)
                recoveryRemaining = Math.Max(0, recoveryRemaining - 1);
            if (recoveryRemaining == 0)
            {
                stage = AnimalIllnessStage.Healthy;
                immunityRemaining = Math.Max(0, rules.immunityDays);
                untreatedDays = hungryDays = rainyDays = 0;
            }
            return;
        }
        if (!Healthy)
        {
            untreatedDays = Math.Min(1000000, untreatedDays + 1);
            int severeAt = Math.Max(1, rules.untreatedDaysToSevere);
            if (stage == AnimalIllnessStage.Mild && untreatedDays >= severeAt)
            {
                stage = AnimalIllnessStage.Severe;
                untreatedDays = 0;
            }
            else if (stage == AnimalIllnessStage.Severe && untreatedDays >= Math.Max(1, rules.severeUntreatedDaysToCritical))
            {
                stage = AnimalIllnessStage.Critical;
                untreatedDays = 0;
            }
            return;
        }
        // Tiga hari tanpa pakan adalah kegagalan care absolut.
        // Immunity hanya melindungi penyakit dari cuaca/malam, bukan kelaparan.
        if (hungryDays >= Math.Max(1, rules.hungryDaysBeforeRisk))
        {
            stage = AnimalIllnessStage.Mild;
            untreatedDays = 0;
            return;
        }

        if (immunityRemaining > 0) { immunityRemaining--; return; }

        // Gabungkan risiko penyakit lingkungan tanpa menjumlahkannya hingga melewati 100%.
        double safe = 1;
        if (directWeatherRisk > 0)
        {
            // Risiko cuaca eksplisit (25/60/90%) sudah mewakili paparan hari ini.
            // Jangan kalikan lagi dengan rainyDays saat hujan terjadi berurutan.
            safe *= 1 - directWeatherRisk;
        }
        else
        {
            if (rainyDays >= Math.Max(1, rules.rainyDaysBeforeRisk)) safe *= 1 - Chance(rules.rainSicknessChance);
            if (storm) safe *= 1 - Chance(legacyStormChance); // Kompatibilitas save/tes lama.
        }
        if (night) safe *= 1 - Chance(nightChance);
        if (roll < 1 - safe)
        {
            stage = extreme ? AnimalIllnessStage.Critical : storm ? AnimalIllnessStage.Severe : AnimalIllnessStage.Mild;
            untreatedDays = 0;
        }
    }
}
