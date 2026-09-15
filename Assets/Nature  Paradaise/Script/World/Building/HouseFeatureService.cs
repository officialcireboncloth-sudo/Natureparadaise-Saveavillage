using System;
using System.Collections.Generic;

/// <summary>
/// Query terpusat untuk fasilitas rumah. Cooking, refrigerator, marriage, dan furniture
/// tidak perlu mengetahui aturan level; semuanya cukup memeriksa stable feature ID.
/// </summary>
public static class HouseFeatureService
{
    public static event Action FeaturesChanged;

    public static bool IsUnlocked(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId) || PlayerHouseController.Instance == null)
            return false;
        return GetUnlockedFeatureIds().Contains(featureId);
    }

    public static List<string> GetUnlockedFeatureIds()
    {
        List<string> result = new();
        PlayerHouseController house = PlayerHouseController.Instance;
        if (house?.Definition?.levels == null)
            return result;

        int effectiveLevel = ProgressionRequirementSettings.EffectiveHouseLevel(house.CurrentLevel);
        for (int index = 0; index < house.Definition.levels.Count; index++)
        {
            BuildingLevelDefinition level = house.Definition.levels[index];
            if (level == null || level.level > effectiveLevel || level.unlockIds == null)
                continue;
            for (int unlockIndex = 0; unlockIndex < level.unlockIds.Count; unlockIndex++)
            {
                string id = level.unlockIds[unlockIndex];
                if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id))
                    result.Add(id);
            }
        }
        return result;
    }

    public static void NotifyHouseLevelChanged() => FeaturesChanged?.Invoke();
}
