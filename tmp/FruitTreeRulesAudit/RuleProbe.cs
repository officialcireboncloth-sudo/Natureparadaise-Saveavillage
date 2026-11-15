using System;

static class RuleProbe
{
    static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException(label);
    }

    static void Main()
    {
        foreach (var test in new[] {(0, 80, 1), (1, 80, 2), (49, 80, 2), (50, 80, 3),
                                     (79, 80, 3), (80, 80, 4), (99, 80, 4),
                                     (100, 79, 4), (100, 80, 5)})
        {
            var care = new FruitTreeCare { version = 1, days = 100, boostedDays = test.Item1,
                                           minimumSoil = test.Item2 };
            Check(care.Stars == test.Item3, $"Coverage {test.Item1}, soil {test.Item2}");
        }
        var cycle = new FruitTreeCare();
        Check(cycle.ApplyFertilizer(1, 3), "Fertilize");
        for (int day = 1; day <= 2; day++)
        {
            cycle.RecordDay(85, day);
            cycle.FinishDay();
        }
        Check(cycle.Stars == 5 && cycle.boostedDays == 2, "One application covers multiple days");
        var next = cycle.NextCycle();
        Check(next.days == 0 && next.boostedDays == 0 && next.fertilizerDaysRemaining == 1,
              "Reset care but keep fertilizer still in effect");
        Check(HarvestRules.Succeeds(.0099, .01) && !HarvestRules.Succeeds(.01, .01), "1% threshold");
        Console.WriteLine("Passed fertilizer boundaries, new-cycle reset and mutation threshold.");
    }
}
