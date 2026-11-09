using UnityEngine;

[CreateAssetMenu(menuName = "Game/Field/Field Soil Rules")]
public sealed class FieldSoilRulesSO : ScriptableObject
{
    [Range(0, 80)] public int initialFertility = 80;
    [Tooltip("Satu penalti per field yang berisi tanaman per hari, bukan per tile.")]
    [Min(0)] public int lossPerOccupiedDay = 1;
    [Header("Condition boundaries (draft tuning)")]
    [Range(1, 79)] public int mediumStartsAt = 20;
    [Range(2, 80)] public int goodStartsAt = 50;
    [Header("Contribution to harvest quality, percent")]
    [Range(0, 100)] public int poorContribution = 10;
    [Range(0, 100)] public int mediumContribution = 25;
    [Range(0, 100)] public int goodContribution = 40;

    public static FieldSoilCondition DefaultCondition(int value) => value >= 50 ? FieldSoilCondition.Good
        : value >= 20 ? FieldSoilCondition.Medium : FieldSoilCondition.Poor;
    public FieldSoilCondition GetCondition(int value) => value >= Mathf.Clamp(goodStartsAt, Mathf.Clamp(mediumStartsAt, 1, 79) + 1, 80)
        ? FieldSoilCondition.Good : value >= Mathf.Clamp(mediumStartsAt, 1, 79)
            ? FieldSoilCondition.Medium : FieldSoilCondition.Poor;
    public float GetQualityContribution(int value) => Mathf.Clamp(value, 0, 80) / 80f * 40f;

    void OnValidate()
    {
        initialFertility = Mathf.Clamp(initialFertility, 0, 80);
        lossPerOccupiedDay = Mathf.Max(0, lossPerOccupiedDay);
        mediumStartsAt = Mathf.Clamp(mediumStartsAt, 1, 79);
        goodStartsAt = Mathf.Clamp(goodStartsAt, mediumStartsAt + 1, 80);
    }
}
