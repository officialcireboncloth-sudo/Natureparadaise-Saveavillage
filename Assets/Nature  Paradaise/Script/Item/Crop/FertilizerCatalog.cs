using System;
using UnityEngine;

[Serializable]
public sealed class FertilizerIngredient
{
    public ItemSO item;
    [Min(1)] public int amount = 1;
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
    public static FertilizerCatalog Load() => Resources.Load<FertilizerCatalog>("Catalogs/FertilizerCatalog");
}
