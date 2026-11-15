using System;
using UnityEngine;

[Serializable]
public sealed class FeedIngredient
{
    public ItemSO item;
    [Min(1)] public int amount=1;
    public string displayName;
    [Tooltip("Terima ikan ukuran Small dari spesies apa pun, termasuk gabungan beberapa spesies.")]
    public bool anySmallFish;
    public string DisplayName=>!string.IsNullOrWhiteSpace(displayName)?displayName:item!=null?item.itemName:"Bahan";
    public bool Matches(ItemStack stack)=>stack?.item!=null&&stack.count>0&&
        (anySmallFish?stack.item.category==ItemCategory.Fish&&stack.fishSizeCm>0&&
            FishMeasurement.GetSizeTier(stack.item,stack.fishSizeCm)==FishSizeTier.Small:stack.item==item);
}

[Serializable]
public sealed class FeedRecipe
{
    public string id;
    [Tooltip("Seluruh bahan ini wajib dipenuhi. Input/Mixed Inputs dipertahankan untuk resep legacy.")]
    public FeedIngredient[] ingredients;
    public bool HasIngredients=>ingredients!=null&&ingredients.Length>0;
    public int TotalInputCount
    {
        get
        {
            if(!HasIngredients)return inputCount;
            long total=0;
            foreach(var ingredient in ingredients)
            {
                if(ingredient==null||ingredient.amount<1||!ingredient.anySmallFish&&ingredient.item==null)return 0;
                total+=ingredient.amount;if(total>int.MaxValue)return 0;
            }
            return (int)total;
        }
    }
    public ItemSO input;
    [Tooltip("Opsional: terima kombinasi item dalam daftar ini hingga Input Count terpenuhi.")]
    public ItemSO[] mixedInputs;
    [Min(1)] public int inputCount = 5;
    [Min(1)] public int outputCount = 5;
    [Min(0.1f)] public float hours = 4;
    [Tooltip("Fish feed recipe. Existing false values remain livestock feed unless Poultry is enabled.")]
    public bool producesFishFeed;
    public bool producesPoultryFeed;
    public FeedKind Kind=>producesFishFeed?FeedKind.Fish:producesPoultryFeed?FeedKind.Poultry:FeedKind.Livestock;
}

[CreateAssetMenu(menuName = "Nature Paradise/Barn/Feed Maker Catalog")]
public sealed class FeedMakerCatalog : ScriptableObject
{
    public ItemSO animalFeed;
    public ItemSO poultryFeed;
    public ItemSO fishFeed;
    public ItemSO Item(FeedKind kind)=>kind switch {FeedKind.Livestock=>animalFeed,FeedKind.Poultry=>poultryFeed,FeedKind.Fish=>fishFeed,_=>null};
    public ItemSO upgradeWood;
    public ItemSO upgradeStone;
    public FeedRecipe[] recipes;
    // A complete batch must fit even at level 1 (livestock requires 13 ingredient units).
    public int MinimumInputCapacity
    {
        get {int capacity=10;if(recipes!=null)foreach(var recipe in recipes)if(recipe!=null)capacity=Math.Max(capacity,recipe.TotalInputCount);return capacity;}
    }
}
