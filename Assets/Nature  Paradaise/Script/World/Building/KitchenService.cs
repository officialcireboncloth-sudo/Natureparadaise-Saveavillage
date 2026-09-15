using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Progress recipe dan transaksi memasak yang tetap hidup saat interior di-unload.</summary>
public static class KitchenService
{
    static readonly HashSet<string> Learned = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, int> Collection = new(StringComparer.OrdinalIgnoreCase);
    static KitchenRecipeSO[] cachedRecipes;
    public static event Action Changed;
    public static IReadOnlyList<KitchenRecipeSO> Recipes => cachedRecipes ??=
        Resources.LoadAll<KitchenRecipeSO>("Cooking/Recipes").Where(recipe => recipe != null)
            .OrderBy(recipe => recipe.requiredKitchenLevel).ThenBy(recipe => recipe.DisplayName).ToArray();

    // House Lv.4 langsung memberi Kitchen Lv.4 supaya seluruh equipment tersedia pada rumah maksimum.
    public static int Level
    {
        get
        {
            int actualHouse = PlayerHouseController.Instance != null ? PlayerHouseController.Instance.CurrentLevel : 1;
            int house = ProgressionRequirementSettings.EffectiveHouseLevel(actualHouse);
            return house < 2 ? 0 : house >= 4 ? 4 : house - 1;
        }
    }
    public static bool IsUnlocked => Level > 0 && HouseFeatureService.IsUnlocked("house.kitchen");
    public static KitchenEquipment AvailableEquipment => Level switch
    {
        1 => KitchenEquipment.Stove | KitchenEquipment.Pot | KitchenEquipment.FryingPan,
        2 => KitchenEquipment.Stove | KitchenEquipment.Pot | KitchenEquipment.FryingPan |
             KitchenEquipment.Oven | KitchenEquipment.CuttingBoard,
        >= 3 => KitchenEquipment.All,
        _ => KitchenEquipment.None
    };

    public static bool IsLearned(KitchenRecipeSO recipe)
    {
        EnsureDefaults();
        return recipe != null && (ProgressionRequirementSettings.BypassEnabled || Learned.Contains(recipe.Id));
    }
    public static bool Learn(KitchenRecipeSO recipe)
    {
        if (recipe == null || !Learned.Add(recipe.Id)) return false;
        Changed?.Invoke(); return true;
    }
    public static int GetCookedCount(KitchenRecipeSO recipe) =>
        recipe != null && Collection.TryGetValue(recipe.Id, out int count) ? count : 0;

    public static int GetMaxBatch(Inventory inventory, KitchenRecipeSO recipe)
    {
        if (inventory == null || recipe?.ingredients == null || recipe.ingredients.Count == 0) return 0;
        int max = int.MaxValue;
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in recipe.ingredients
                     .Where(value => value?.item != null && value.amount > 0).GroupBy(value => value.item))
        {
            int perCook = group.Sum(value => value.amount);
            max = Mathf.Min(max, KitchenIngredientService.GetAvailableCount(inventory, group.Key) / perCook);
        }
        return max == int.MaxValue ? 0 : Mathf.Max(0, max);
    }

    public static bool CanCook(Inventory inventory, KitchenRecipeSO recipe, int batches, out string reason)
    {
        reason = string.Empty;
        if (!IsUnlocked) { reason = "Kitchen terbuka pada House Lv.2."; return false; }
        if (recipe == null || recipe.resultItem == null) { reason = "Data recipe belum lengkap."; return false; }
        if (!IsLearned(recipe)) { reason = "Recipe belum dipelajari."; return false; }
        if (Level < recipe.requiredKitchenLevel) { reason = $"Perlu Kitchen Lv.{recipe.requiredKitchenLevel}."; return false; }
        if ((AvailableEquipment & recipe.requiredEquipment) != recipe.requiredEquipment)
        { reason = $"Perlu {FormatEquipment(recipe.requiredEquipment)}."; return false; }
        if (batches <= 0) { reason = "Jumlah masak tidak valid."; return false; }
        if (GetMaxBatch(inventory, recipe) < batches) { reason = "Bahan tidak cukup di Inventory + Refrigerator."; return false; }
        int quality = CalculateOutputQuality(inventory, recipe, batches);
        if (!CanFitOutputAfterConsumption(inventory, recipe, batches, quality))
        { reason = "Inventory penuh untuk hasil masakan."; return false; }
        return true;
    }

    public static bool TryCook(Inventory inventory, KitchenRecipeSO recipe, int batches, out string feedback)
    {
        if (!CanCook(inventory, recipe, batches, out feedback)) return false;
        int quality = CalculateOutputQuality(inventory, recipe, batches);
        List<KitchenIngredientRequirement> scaled = recipe.ingredients
            .Where(value => value?.item != null && value.amount > 0)
            .Select(value => new KitchenIngredientRequirement { item = value.item, amount = value.amount * batches }).ToList();
        if (!KitchenIngredientService.TryConsume(inventory, scaled))
        { feedback = "Bahan berubah sebelum transaksi selesai."; return false; }
        int output = recipe.resultAmount * batches;
        if (!inventory.Add(recipe.resultItem, output, quality))
        {
            foreach (KitchenIngredientRequirement ingredient in scaled) inventory.Add(ingredient.item, ingredient.amount);
            feedback = "Inventory berubah; bahan dikembalikan."; return false;
        }
        Collection[recipe.Id] = GetCookedCount(recipe) + output;
        QuestEventHub.Publish(QuestObjectiveType.Cook, recipe.Id, output, recipe.resultItem);
        int minutes = batches <= 1 ? recipe.baseCookingMinutes : recipe.baseCookingMinutes + (batches - 1) * 5;
        TimeManager.Instance?.AdvanceMinutes(minutes);
        feedback = $"{recipe.DisplayName} x{output} selesai ({quality}★, +{minutes} menit).";
        Changed?.Invoke(); return true;
    }

    public static KitchenProgressSaveData Capture()
    {
        EnsureDefaults();
        return new KitchenProgressSaveData { initialized = true, learnedRecipeIds = Learned.OrderBy(id => id).ToList(),
            cookingCollection = Collection.Select(pair => new KitchenCookedEntrySaveData
                { recipeId = pair.Key, count = pair.Value }).ToList() };
    }

    public static void Restore(KitchenProgressSaveData data)
    {
        Learned.Clear(); Collection.Clear();
        if (data?.initialized == true)
        {
            foreach (string id in data.learnedRecipeIds ?? new List<string>()) if (!string.IsNullOrWhiteSpace(id)) Learned.Add(id);
            foreach (KitchenCookedEntrySaveData entry in data.cookingCollection ?? new List<KitchenCookedEntrySaveData>())
                if (entry != null && !string.IsNullOrWhiteSpace(entry.recipeId) && entry.count > 0) Collection[entry.recipeId] = entry.count;
        }
        EnsureDefaults(); Changed?.Invoke();
    }
    public static void Clear() { Learned.Clear(); Collection.Clear(); EnsureDefaults(); Changed?.Invoke(); }
    static void EnsureDefaults() { foreach (KitchenRecipeSO recipe in Recipes) if (recipe.learnedByDefault) Learned.Add(recipe.Id); }

    static int CalculateOutputQuality(Inventory inventory, KitchenRecipeSO recipe, int batches)
    {
        float points = 0f; int units = 0;
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in recipe.ingredients
                     .Where(value => value?.item != null && value.amount > 0).GroupBy(value => value.item))
        {
            int needed = group.Sum(value => value.amount) * batches;
            for (int index = inventory.slots.Count - 1; index >= 0 && needed > 0; index--)
            {
                ItemStack stack = inventory.slots[index];
                if (stack?.item != group.Key || stack.count <= 0) continue;
                int used = Mathf.Min(needed, stack.count); points += stack.qualityStars * used; units += used; needed -= used;
            }
            foreach (RefrigeratorEntry entry in RefrigeratorService.Entries.Where(value => value.item == group.Key)
                         .OrderBy(value => value.qualityStars))
            {
                if (needed <= 0) break;
                int used = Mathf.Min(needed, entry.count); points += entry.qualityStars * used; units += used; needed -= used;
            }
        }
        return units <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(points / units), 0, 5);
    }

    static bool CanFitOutputAfterConsumption(Inventory inventory, KitchenRecipeSO recipe, int batches, int outputQuality)
    {
        int[] counts = inventory.slots.Select(stack => stack?.item != null ? stack.count : 0).ToArray();
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in recipe.ingredients
                     .Where(value => value?.item != null && value.amount > 0).GroupBy(value => value.item))
        {
            int neededFromInventory = Mathf.Min(group.Sum(value => value.amount) * batches, inventory.GetCount(group.Key));
            for (int index = inventory.slots.Count - 1; index >= 0 && neededFromInventory > 0; index--)
            {
                ItemStack stack = inventory.slots[index];
                if (stack?.item != group.Key || counts[index] <= 0) continue;
                int used = Mathf.Min(neededFromInventory, counts[index]);
                counts[index] -= used; neededFromInventory -= used;
            }
        }
        int free = 0;
        for (int index = 0; index < inventory.slots.Count; index++)
        {
            ItemStack stack = inventory.slots[index];
            if (counts[index] <= 0) free += recipe.resultItem.StackLimit;
            else if (stack.item == recipe.resultItem && stack.qualityStars == outputQuality && Mathf.Abs(stack.fishSizeCm) < 0.01f)
                free += Mathf.Max(0, recipe.resultItem.StackLimit - counts[index]);
        }
        return free >= recipe.resultAmount * batches;
    }

    public static string FormatEquipment(KitchenEquipment equipment) => equipment == KitchenEquipment.None ? "Tanpa alat khusus" :
        string.Join(", ", Enum.GetValues(typeof(KitchenEquipment)).Cast<KitchenEquipment>()
            .Where(value => value != KitchenEquipment.None && value != KitchenEquipment.All && equipment.HasFlag(value)));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Learned.Clear(); Collection.Clear(); cachedRecipes = null; Changed = null; }
}
