using System;
using UnityEngine;

[Serializable]
public sealed class FertilizerIngredient
{
    public ItemSO item;
    [Min(1)] public int amount = 1;
    public ItemSO[] alternatives;
    public string displayName;
    public bool requireFishSize;
    public FishSizeTier fishSize;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? item != null ? item.itemName : "Bahan" : displayName;
    public bool Matches(ItemStack stack)
    {
        if (stack?.item == null || stack.count < 1) return false;
        bool matches = stack.item == item || alternatives != null && Array.IndexOf(alternatives, stack.item) >= 0;
        return matches && (!requireFishSize || stack.fishSizeCm > 0 &&
            FishMeasurement.GetSizeTier(stack.item, stack.fishSizeCm) == fishSize);
    }
}

[Serializable]
public sealed class FertilizerRecipe
{
    public string id;
    public ItemSO output;
    [Min(1)] public int outputCount = 1;
    [Min(1)] public int productionHours = 2;
    public FertilizerIngredient[] ingredients;
}

/// <summary>Referensi tetap untuk shop, processor, dan item Save/Load.</summary>
[CreateAssetMenu(menuName = "Game/Fertilizer Catalog")]
public sealed class FertilizerCatalog : ScriptableObject
{
    public ItemSO[] soilFertilizers;
    public ItemSO[] cropBoosters;
    public FertilizerRecipe[] recipes;
    [Tooltip("Empat resep Pembuat Pupuk. Recipes lama disimpan untuk antrean pada save lama.")]
    public FertilizerRecipe[] makerRecipes;
    public static FertilizerCatalog Load() => Resources.Load<FertilizerCatalog>("Catalogs/FertilizerCatalog");
}
