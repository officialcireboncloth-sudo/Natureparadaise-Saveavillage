using System;
class AnimalHealthTests
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static void Main()
    {
        var rules = new AnimalHealthRules { hungerSicknessChance = 1, rainSicknessChance = 1 };
        var state = new AnimalHealthProgress();
        state.EndDay(rules, 1, false, true, 0, 0);
        state.EndDay(rules, 2, false, true, 0, 0);
        Check(state.Healthy, "hunger grace");
        state.EndDay(rules, 3, false, true, 0, 0);
        Check(state.stage == AnimalIllnessStage.Mild, "third hungry day");
        state.EndDay(rules, 3, false, true, 0, 0);
        Check(state.untreatedDays == 0, "duplicate reset");
        for (int day = 4; day <= 6; day++) state.EndDay(rules, day, true, true, 0, 0);
        Check(state.stage == AnimalIllnessStage.Severe, "untreated escalation");
        Check(state.Treat(rules, 7) && state.recoveryRemaining == 3, "severe medicine");
        Check(!state.Treat(rules, 7), "no repeat medicine");
        state.EndDay(rules, 7, true, true, 0, 0);
        Check(state.recoveryRemaining == 3, "no partial treatment day");
        state.EndDay(rules, 8, false, true, 0, 0);
        state.EndDay(rules, 9, true, false, 0, 0);
        Check(state.recoveryRemaining == 3, "care needed");
        state.Expose(true, false);
        state.EndDay(rules, 10, true, true, 0, 0);
        Check(state.recoveryRemaining == 3, "rain interrupts recovery even after boarding");
        for (int day = 11; day <= 13; day++) state.EndDay(rules, day, true, true, 0, 0);
        Check(state.Healthy && state.immunityRemaining == 3, "recovered immunity");
        for (int day = 14; day <= 16; day++)
        {
            state.Expose(true, true);
            state.EndDay(rules, day, false, false, 1, 0);
            Check(state.Healthy, "three full immune days");
        }
        state.EndDay(rules, 17, false, true, 0, 0);
        Check(!state.Healthy, "immunity expires");
        state = new AnimalHealthProgress();
        state.Expose(true, false);
        state.EndDay(rules, 1, true, true, 0, 0);
        Check(state.Healthy, "first rain day grace");
        var restored = state.Copy();
        restored.Expose(true, false);
        restored.EndDay(rules, 2, true, true, 0, 0);
        Check(!restored.Healthy && state.Healthy, "saved rain streak and independent copy");
        Check(restored.Treat(rules, 2) && restored.recoveryRemaining == 2, "mild recovery length");
        state = new AnimalHealthProgress();
        state.Expose(false, true);
        state.EndDay(rules, 1, true, true, 1, 0.5);
        Check(!state.Healthy, "storm risk retained");
        state = new AnimalHealthProgress();
        rules.hungerSicknessChance = rules.rainSicknessChance = 0;
        for (int day = 1; day <= 100; day++)
        {
            state.Expose(true, false);
            state.EndDay(rules, day, false, false, 0, 0);
            Check(state.Healthy, "disabled risk");
        }
        Check(!state.Treat(rules, 101), "healthy no medicine");
        state = new AnimalHealthProgress();
        state.ExposeWeather(true, false, 1);
        state.EndDay(rules, 1, true, false, 0, 0, 0.5);
        Check(!state.Healthy, "heavy/extreme direct weather risk");
        state = new AnimalHealthProgress();
        rules.nightSicknessChance = 1;
        state.ExposeNight();
        state.EndDay(rules, 1, true, false, 0, 1, 0.5);
        Check(!state.Healthy, "late outside risk");
        state = new AnimalHealthProgress();
        state.ExposeWeather(true, true, float.NaN);
        state.EndDay(rules, 1, true, false, 0, 0, 0.5);
        Check(state.Healthy, "invalid modular chance is safe");
        state.Expose(true, true);
        restored = state.Copy();
        Check(restored.rainExposure && restored.stormExposure, "save retains same-day exposure");
        restored.EndDay(rules, 1, false, false, 1, 0);
        Check(restored.rainExposure, "duplicate reset does not clear new exposure");
        Console.WriteLine(checks + " animal health assertions passed (pure logic, not Play Mode).");
    }
}
