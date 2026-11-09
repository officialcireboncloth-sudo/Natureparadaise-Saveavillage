using UnityEngine;

public sealed partial class FieldArea
{
    /// <summary>Skips a planted crop's own visual stages without simulating a calendar day.</summary>
    public bool DebugAdvanceCropStage(int x, int z, bool harvestReady)
    {
        if (!PropertySite.DebugShortcutsEnabled || IsSoilLocked || tiles == null ||
            !TryGetIndex(x, z, out int index) || index >= tiles.Length)
            return false;

        int today = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        if (!CropStageDebug.TryAdvance(ref tiles[index], harvestReady, today))
            return false;

        RefreshViewAt(index);
        if (cropViews[index] != null)
            cropViews[index].ApplyStage(tiles[index].growthStage, tiles[index].crop, tiles[index].cropState);
        NotifyChanged(x, z);
        return true;
    }
}

/// <summary>Shared stage mutation; keeps the save state and harvest eligibility in sync.</summary>
internal static class CropStageDebug
{
    internal static bool TryAdvance(ref FieldTileData tile, bool harvestReady, int today)
    {
        if (!tile.HasCrop || tile.cropState == CropLifecycleState.HarvestReady ||
            tile.cropState == CropLifecycleState.Dead || tile.cropState == CropLifecycleState.Withered)
            return false;

        CropDataSO crop = tile.crop;
        int lastStage = crop.StageCount - 1;
        int currentStage = Mathf.Clamp(tile.growthStage, 0, lastStage);
        bool regrowing = tile.cropState == CropLifecycleState.Regrowing;
        if (!regrowing)
            currentStage = Mathf.Max(currentStage, crop.GetStageForGrowth(tile.growthDays));
        int nextStage = harvestReady ? lastStage : Mathf.Min(lastStage, currentStage + 1);

        if (nextStage == lastStage)
        {
            tile.growthDays = crop.TotalGrowthDays;
            tile.growthStage = (byte)lastStage;
            tile.cropState = CropLifecycleState.HarvestReady;
            tile.regrowDaysRemaining = 0f;
            tile.qualityCare ??= new CropQualityCare();
            if (tile.qualityCare.readyDay < 0)
                tile.qualityCare.readyDay = Mathf.Max(1, today);
        }
        else if (regrowing)
        {
            // First-harvest growthDays is already full. Shorten only the regrow timer,
            // spreading its remaining time across the remaining visual transitions.
            int stepsLeft = lastStage - currentStage;
            tile.regrowDaysRemaining *= (stepsLeft - 1f) / stepsLeft;
            tile.growthStage = (byte)nextStage;
        }
        else
        {
            // Invert the crop's actual relative schedule, including fractional days.
            // Keep the upper bound to avoid float rounding back to the previous stage.
            float low = 0f, high = crop.TotalGrowthDays;
            for (int i = 0; i < 32; i++)
            {
                float middle = low + (high - low) * .5f;
                if (crop.GetStageForGrowth(middle) < nextStage) low = middle;
                else high = middle;
            }
            // An incomplete visual schedule cannot represent this transition.
            if (crop.GetStageForGrowth(high) < nextStage)
                return false;
            tile.growthDays = Mathf.Max(tile.growthDays, high);
            tile.growthStage = (byte)crop.GetStageForGrowth(tile.growthDays);
        }

        // No water, fertilizer, quality samples, RNG rolls, inventory or clock changes.
        tile.dirty = true;
        return true;
    }
}
