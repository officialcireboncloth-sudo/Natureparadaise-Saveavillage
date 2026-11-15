using System.Collections.Generic;
using System.Text;

/// <summary>
/// Operasi biaya bersama untuk Property Site, Carpenter, dan Player House.
/// Menjaga validasi, tampilan requirement, dan transaksi memakai aturan yang sama.
/// </summary>
public static class BuildingCostUtility
{
    public static Dictionary<ItemSO, int> Aggregate(BuildingLevelDefinition level)
    {
        Dictionary<ItemSO, int> totals = new();
        if (level?.materialCosts == null)
            return totals;

        for (int index = 0; index < level.materialCosts.Count; index++)
        {
            BuildingMaterialCost cost = level.materialCosts[index];
            if (cost?.item == null || cost.amount <= 0)
                continue;
            totals.TryGetValue(cost.item, out int current);
            totals[cost.item] = current + cost.amount;
        }
        return totals;
    }

    public static bool CanAfford(BuildingLevelDefinition level, Inventory inventory, out string reason)
    {
        if (level == null)
        {
            reason = "Data upgrade tidak tersedia";
            return false;
        }

        int villageLevel = VillageProgressionService.Instance != null
            ? VillageProgressionService.Instance.EffectiveVillageLevel
            : ProgressionRequirementSettings.EffectiveVillageLevel(1);
        if (villageLevel < level.requiredVillageLevel)
        {
            reason = $"Village Level kurang: {villageLevel}/{level.requiredVillageLevel}";
            return false;
        }
        if (ScoreManager.Instance == null || ScoreManager.Instance.points < level.goldCost)
        {
            int owned = ScoreManager.Instance != null ? ScoreManager.Instance.points : 0;
            reason = $"Gold kurang: {owned}/{level.goldCost}";
            return false;
        }
        if (inventory == null)
        {
            reason = "Inventory player tidak ditemukan";
            return false;
        }

        foreach (KeyValuePair<ItemSO, int> cost in Aggregate(level))
        {
            int owned = inventory.GetCount(cost.Key);
            if (owned < cost.Value)
            {
                reason = $"{cost.Key.itemName} kurang: {owned}/{cost.Value}";
                return false;
            }
        }

        reason = null;
        return true;
    }

    /// <summary>Mengonsumsi Gold dan material secara atomik; kegagalan mengembalikan semua resource.</summary>
    public static bool TrySpend(BuildingLevelDefinition level, Inventory inventory)
    {
        if (!CanAfford(level, inventory, out _))
            return false;
        if (level.goldCost > 0 && !ScoreManager.Instance.TrySpendPoints(level.goldCost))
            return false;

        List<KeyValuePair<ItemSO, int>> removed = new();
        foreach (KeyValuePair<ItemSO, int> cost in Aggregate(level))
        {
            if (inventory.Remove(cost.Key, cost.Value))
            {
                removed.Add(cost);
                continue;
            }

            if (level.goldCost > 0)
                ScoreManager.Instance.AddPoints(level.goldCost);
            for (int index = 0; index < removed.Count; index++)
                inventory.Add(removed[index].Key, removed[index].Value);
            return false;
        }
        return true;
    }

    /// <summary>Membuat requirement multiline berwarna berdasarkan resource player saat ini.</summary>
    public static string BuildRequirementLabel(BuildingLevelDefinition level, Inventory inventory)
    {
        if (level == null)
            return "<color=#FF6673>Data biaya tidak tersedia</color>";

        StringBuilder label = new();
        int villageOwned = VillageProgressionService.Instance != null
            ? VillageProgressionService.Instance.EffectiveVillageLevel
            : ProgressionRequirementSettings.EffectiveVillageLevel(1);
        if (level.requiredVillageLevel > 0)
            Append(label, "Village Lv.", villageOwned, level.requiredVillageLevel);

        int goldOwned = ScoreManager.Instance != null ? ScoreManager.Instance.points : 0;
        Append(label, "Gold", goldOwned, level.goldCost);
        foreach (KeyValuePair<ItemSO, int> cost in Aggregate(level))
        {
            int owned = inventory != null ? inventory.GetCount(cost.Key) : 0;
            Append(label, cost.Key.itemName, owned, cost.Value);
        }
        return label.ToString();
    }

    static void Append(StringBuilder label, string name, int owned, int required)
    {
        if (label.Length > 0)
            label.Append('\n');
        string color = owned >= required ? "#58D982" : "#FF6673";
        label.Append("<color=").Append(color).Append('>')
            .Append(name).Append(' ').Append(owned).Append('/').Append(required)
            .Append("</color>");
    }
}
