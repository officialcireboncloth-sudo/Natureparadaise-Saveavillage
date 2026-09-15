using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum KitchenEquipment
{
    None = 0, Stove = 1 << 0, FryingPan = 1 << 1, Pot = 1 << 2,
    Oven = 1 << 3, CuttingBoard = 1 << 4, Blender = 1 << 5,
    All = Stove | FryingPan | Pot | Oven | CuttingBoard | Blender
}

[CreateAssetMenu(menuName = "Game/House/Kitchen Recipe", fileName = "New Kitchen Recipe")]
public sealed class KitchenRecipeSO : ScriptableObject
{
    [Header("Identity")]
    public string recipeId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    [Header("Recipe")]
    public List<KitchenIngredientRequirement> ingredients = new();
    public ItemSO resultItem;
    [Min(1)] public int resultAmount = 1;
    [Range(1, 4)] public int requiredKitchenLevel = 1;
    public KitchenEquipment requiredEquipment = KitchenEquipment.Stove;
    [Min(0)] public int baseCookingMinutes = 10;
    public bool learnedByDefault;
    public string Id => string.IsNullOrWhiteSpace(recipeId)
        ? $"recipe.{name.Trim().ToLowerInvariant().Replace(' ', '_')}" : recipeId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}

[Serializable]
public sealed class KitchenCookedEntrySaveData { public string recipeId; public int count; }

[Serializable]
public sealed class KitchenProgressSaveData
{
    public bool initialized;
    public List<string> learnedRecipeIds = new();
    public List<KitchenCookedEntrySaveData> cookingCollection = new();
}

