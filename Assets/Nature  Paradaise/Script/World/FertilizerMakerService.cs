using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plan against copied stacks, then commit once. Failed recipes never consume or rewrite inventory.</summary>
public static class FertilizerMakerService
{
    static readonly HashSet<Inventory> Busy = new();
    public const int MaximumBatch = 99;
    public static int Available(Inventory bag, FertilizerIngredient ingredient)
    {
        if (bag == null || ingredient == null) return 0;
        int count = 0;
        foreach (var stack in bag.slots) if (ingredient.Matches(stack)) count += stack.count;
        return count;
    }
    public static bool CanMake(Inventory bag, FertilizerRecipe recipe, int batches, out string reason)
        => Plan(bag, recipe, batches, out _, out reason);
    public static bool TryMake(Inventory bag, FertilizerRecipe recipe, int batches, out string reason)
    {
        if (bag == null || !Busy.Add(bag)) { reason = "Produksi sedang diproses"; return false; }
        try
        {
            if (!Plan(bag, recipe, batches, out var prepared, out reason)) return false;
            bag.RestoreContents(bag.BackpackLevel, prepared);
            reason = $"{recipe.outputCount * batches}× {recipe.output.itemName} masuk tas";
            return true;
        }
        finally { Busy.Remove(bag); }
    }
    static bool Plan(Inventory bag, FertilizerRecipe recipe, int batches, out List<ItemStack> result, out string reason)
    {
        result = null; reason = "";
        if (bag == null || recipe?.output == null || recipe.ingredients == null || recipe.ingredients.Length == 0 || recipe.outputCount < 1)
        { reason = "Data resep belum lengkap"; return false; }
        if (batches < 1 || batches > MaximumBatch || (long)batches * recipe.outputCount > int.MaxValue)
        { reason = "Jumlah produksi tidak valid"; return false; }
        result = new List<ItemStack>(bag.Capacity);
        for (int i = 0; i < bag.Capacity; i++)
        {
            var s = i < bag.slots.Count ? bag.slots[i] : null;
            result.Add(s?.item == null || s.count <= 0 ? null : new ItemStack { item = s.item, count = s.count,
                qualityStars = s.qualityStars, fishSizeCm = s.fishSizeCm, fishWeightKg = s.fishWeightKg });
        }
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient?.item == null || ingredient.amount < 1 || (long)ingredient.amount * batches > int.MaxValue)
            { reason = "Data bahan belum lengkap"; return false; }
            int remaining = ingredient.amount * batches;
            // Consume eligible stacks only: wrong fish sizes and unrelated species remain intact.
            for (int i = result.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var s = result[i]; if (!ingredient.Matches(s)) continue;
                int used = Math.Min(remaining, s.count); s.count -= used; remaining -= used;
                if (s.count == 0) result[i] = null;
            }
            if (remaining > 0) { reason = "Bahan belum lengkap"; return false; }
        }
        int output = batches * recipe.outputCount;
        for (int i = 0; i < result.Count && output > 0; i++)
        {
            var s = result[i];
            if (s?.item != recipe.output || s.qualityStars != 0 || s.fishSizeCm != 0) continue;
            int added = Math.Min(output, Math.Max(0, recipe.output.StackLimit - s.count)); s.count += added; output -= added;
        }
        for (int i = 0; i < result.Count && output > 0; i++)
        {
            if (result[i] != null) continue;
            int added = Math.Min(output, recipe.output.StackLimit);
            result[i] = new ItemStack { item = recipe.output, count = added }; output -= added;
        }
        if (output > 0) { reason = "Ruang tas tidak cukup untuk hasil produksi"; return false; }
        return true;
    }
}
