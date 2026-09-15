using UnityEngine;

/// <summary>
/// Satu tempat untuk mengaktifkan atau melewati requirement progression.
/// Nilai requirement asli tetap berada pada item, recipe, quest, dan building.
/// </summary>
[CreateAssetMenu(fileName = "Progression Requirement Settings", menuName = "Game/Settings/Progression Requirements")]
public sealed class ProgressionRequirementSettings : ScriptableObject
{
    const string ResourcePath = "Settings/Progression Requirement Settings";
    static ProgressionRequirementSettings instance;

    [Header("Testing")]
    [Tooltip("Aktifkan untuk mengetes semua fitur tanpa menaikkan progression save game.")]
    public bool bypassProgressionRequirements = true;

    [Tooltip("Level rumah efektif selama bypass. Tidak mengubah level rumah pada save.")]
    [Range(1, 4)] public int testHouseLevel = 4;
    [Tooltip("Level desa/kota efektif selama bypass. Tidak mengubah level pada save.")]
    [Min(1)] public int testVillageLevel = 99;
    [Tooltip("Level fishing efektif selama bypass. Tidak mengubah XP fishing pada save.")]
    [Min(1)] public int testFishingLevel = 99;

    public static ProgressionRequirementSettings Instance => instance ??= Resources.Load<ProgressionRequirementSettings>(ResourcePath);
    // Jika asset hilang pada build, requirement tetap aktif (fail closed).
    public static bool BypassEnabled => Instance != null && Instance.bypassProgressionRequirements;

    public static int EffectiveHouseLevel(int actualLevel) => BypassEnabled
        ? Mathf.Max(Mathf.Max(1, actualLevel), Instance != null ? Instance.testHouseLevel : 4)
        : Mathf.Max(1, actualLevel);

    public static int EffectiveVillageLevel(int actualLevel) => BypassEnabled
        ? Mathf.Max(Mathf.Max(1, actualLevel), Instance != null ? Instance.testVillageLevel : 99)
        : Mathf.Max(1, actualLevel);

    public static int EffectiveFishingLevel(int actualLevel) => BypassEnabled
        ? Mathf.Max(Mathf.Max(1, actualLevel), Instance != null ? Instance.testFishingLevel : 99)
        : Mathf.Max(1, actualLevel);

    public static bool MeetsHouseLevel(int actualLevel, int requiredLevel) =>
        EffectiveHouseLevel(actualLevel) >= Mathf.Max(0, requiredLevel);

    public static bool MeetsVillageLevel(int actualLevel, int requiredLevel) =>
        EffectiveVillageLevel(actualLevel) >= Mathf.Max(0, requiredLevel);

    public static bool MeetsFishingLevel(int actualLevel, int requiredLevel) =>
        EffectiveFishingLevel(actualLevel) >= Mathf.Max(0, requiredLevel);
}
