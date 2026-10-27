using System;
using UnityEngine;

[Serializable]
public sealed class FeedRecipe
{
    public string id;
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
}
