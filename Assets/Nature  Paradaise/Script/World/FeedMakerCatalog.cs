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
    [Tooltip("Aktif berarti recipe menghasilkan Fish Feed; nonaktif menghasilkan Universal Animal Feed.")]
    public bool producesFishFeed;
}

[CreateAssetMenu(menuName = "Nature Paradise/Barn/Feed Maker Catalog")]
public sealed class FeedMakerCatalog : ScriptableObject
{
    public ItemSO animalFeed;
    public ItemSO fishFeed;
    public ItemSO upgradeWood;
    public ItemSO upgradeStone;
    public FeedRecipe[] recipes;
}
