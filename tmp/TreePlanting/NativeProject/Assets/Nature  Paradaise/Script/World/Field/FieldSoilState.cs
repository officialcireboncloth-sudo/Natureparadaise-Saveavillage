using System;

public enum FieldSoilCondition { Poor, Medium, Good }

/// <summary>One persistent soil record per field, independent of its tile count.</summary>
[Serializable]
public sealed class FieldSoilState
{
    public const int MaximumFertility = 80;
    public int fertility = MaximumFertility;
    public int remainingDays;
    public int lastProcessedDay = 1;
    public int restorationStartedDay;
    public string restorationItemId;

    public bool IsRestoring => remainingDays > 0;

    public FieldSoilState Copy()
    {
        var copy = (FieldSoilState)MemberwiseClone();
        copy.Normalize();
        return copy;
    }

    public void Normalize()
    {
        fertility = Math.Max(0, Math.Min(MaximumFertility, fertility));
        remainingDays = Math.Max(0, remainingDays);
        lastProcessedDay = Math.Max(1, lastProcessedDay);
        if (!IsRestoring) restorationItemId = null;
    }

    public bool BeginRestoration(int duration, string itemId, int today)
    {
        Normalize();
        if (duration < 1 || IsRestoring || fertility >= MaximumFertility) return false;
        remainingDays = duration;
        restorationItemId = itemId;
        restorationStartedDay = Math.Max(1, today);
        lastProcessedDay = Math.Max(lastProcessedDay, restorationStartedDay);
        return true;
    }

    /// <summary>Idempotent at each calendar boundary; loading never consumes another day.</summary>
    public bool AdvanceToDay(int day, bool hasCrops, int lossPerOccupiedDay)
    {
        Normalize();
        if (day <= lastProcessedDay) return false;
        int elapsed = day - lastProcessedDay;
        lastProcessedDay = day;
        if (IsRestoring)
        {
            remainingDays = Math.Max(0, remainingDays - elapsed);
            if (!IsRestoring)
            {
                fertility = MaximumFertility;
                restorationItemId = null;
            }
        }
        else if (hasCrops)
        {
            long loss = (long)Math.Max(0, lossPerOccupiedDay) * elapsed;
            fertility = (int)Math.Max(0L, fertility - loss);
        }
        return true;
    }
}

/// <summary>Read-only scanner data. Percent is absolute fertility, e.g. 25 / 80.</summary>
public readonly struct FieldSoilSnapshot
{
    public readonly int FieldNumber, Fertility, RemainingDays;
    public readonly string FieldId, DisplayName, RestorationItemId;
    public readonly FieldSoilCondition Condition;
    public readonly float QualityContributionPercent;
    public bool IsRestoring => RemainingDays > 0;
    public int MaximumFertility => FieldSoilState.MaximumFertility;

    public FieldSoilSnapshot(FieldArea field, FieldSoilState state, FieldSoilRulesSO rules)
    {
        FieldNumber = field.FieldNumber;
        FieldId = field.FieldId;
        DisplayName = field.SoilDisplayName;
        Fertility = state.fertility;
        RemainingDays = state.remainingDays;
        RestorationItemId = state.restorationItemId;
        Condition = rules != null ? rules.GetCondition(Fertility) : FieldSoilRulesSO.DefaultCondition(Fertility);
        QualityContributionPercent = rules != null ? rules.GetQualityContribution(Fertility)
            : Math.Max(0, Math.Min(80, Fertility)) / 80f * 40f;
    }

    public string ConditionName => Condition == FieldSoilCondition.Good ? "Bagus"
        : Condition == FieldSoilCondition.Medium ? "Sedang" : "Buruk";
}
