using System;
using System.Collections.Generic;
using UnityEngine;

public enum AnimalType { Chicken, Duck, Goat, Sheep, Cow }
public enum AnimalBirthSource { HatchedOnFarm, BornOnFarm, PurchasedYoung, PurchasedEgg }
public enum AnimalGrowthStage { Egg, Pregnancy, Hatchling, Newborn, Baby, Young, Adolescent, Adult }
public enum AnimalHealthState { Healthy, Sick }
public enum AnimalShopOfferKind { Egg, Young }

/// <summary>Harga jual hewan hidup; terpisah dari harga beli toko dan harga produknya.</summary>
[Serializable]
public sealed class AnimalSalePriceSettings
{
    [Min(0), Tooltip("Harga jual pada 0 Heart, dalam Gold.")]
    public int baseSellPrice = 1000;
    [Min(0f), Tooltip("Bonus per Heart penuh. 0.1 = +10% dari harga dasar; 0 menonaktifkan bonus.")]
    public float bonusPerHeart = 0.1f;

    public float Multiplier(int heartLevel)
    {
        float bonus = float.IsNaN(bonusPerHeart) || float.IsInfinity(bonusPerHeart) ? 0f : Math.Max(0f, bonusPerHeart);
        return 1f + Math.Max(0, Math.Min(10, heartLevel)) * bonus;
    }

    public int Calculate(int heartLevel)
    {
        if (baseSellPrice <= 0) return 0;
        double price = baseSellPrice * (double)Multiplier(heartLevel);
        return (int)Math.Min(int.MaxValue, Math.Round(price, MidpointRounding.AwayFromZero));
    }
}

[Serializable]
public sealed class AnimalShopOffer
{
    public string displayName;
    public Sprite icon;
    public AnimalType animalType;
    public AnimalShopOfferKind offerKind;
    [Min(0)] public int price = 500;
    [Min(1)] public int requiredVillageLevel = 1;
    public AnimalGrowthProfileSO growthProfile;
    public GameObject animalPrefab;
    [Tooltip("Produk saat Adult. Boleh kosong sampai asset produk spesies tersedia.")]
    public ItemSO productItem;
}

[Serializable]
public sealed class AnimalGrowthStageSlot
{
    public AnimalGrowthStage stage = AnimalGrowthStage.Baby;
    [Min(0)] public int startsOnGrowthDay;
    [Tooltip("Prefab model opsional. Jika kosong, model dummy bawaan tetap dipakai.")]
    public GameObject modelPrefab;
    public Vector3 scale = Vector3.one;
    [Tooltip("Offset model terhadap pusat collider hewan.")]
    public Vector3 localOffset;
    [Tooltip("Koreksi rotasi model tanpa mengubah arah root hewan.")]
    public Vector3 localEulerAngles;
    public RuntimeAnimatorController animatorController;
    public AudioClip voiceClip;
}

[Serializable]
public sealed class AnimalConditionVisualSlot
{
    public AnimalIllnessStage condition = AnimalIllnessStage.Mild;
    [Tooltip("Model pengganti opsional. Kosong = model growth tetap dipakai.")]
    public GameObject modelPrefab;
    public Vector3 scale = Vector3.one;
    public Vector3 localOffset;
    public Vector3 localEulerAngles;
    [Tooltip("Bisa dipakai tanpa Model Prefab untuk mengganti controller Animator pada model growth.")]
    public RuntimeAnimatorController animatorController;
}

/// <summary>
/// Profile modular satu spesies untuk durasi growth/production dan model setiap stage.
/// </summary>
[CreateAssetMenu(fileName = "Animal Growth Profile", menuName = "Nature Paradise/Animal/Growth Profile")]
public sealed class AnimalGrowthProfileSO : ScriptableObject
{
    public AnimalType animalType = AnimalType.Cow;
    [Header("Animal Sale Price")]
    public AnimalSalePriceSettings salePrice = new();
    [Min(0)] public int incubationOrPregnancyDays = 7;
    [Min(1)] public int bornToAdultDays = 56;
    [Min(1)] public int purchasedYoungToAdultDays = 28;
    public ItemSO productItem;
    [Min(1)] public int productionIntervalDays = 1;
    public List<AnimalGrowthStageSlot> stageSlots = new();
    [Header("Condition Visual Slots (Optional)")]
    [Tooltip("Slot Mild/Severe/Recovering. Kosong tidak membuat dummy dan mempertahankan visual growth.")]
    public List<AnimalConditionVisualSlot> conditionVisualSlots = new();

    public AnimalGrowthStageSlot GetStageSlot(AnimalGrowthStage stage)
    {
        if (stageSlots == null) return null;
        for (int i = 0; i < stageSlots.Count; i++)
            if (stageSlots[i] != null && stageSlots[i].stage == stage)
                return stageSlots[i];
        return null;
    }

    public AnimalConditionVisualSlot GetConditionSlot(AnimalIllnessStage condition)
    {
        if (conditionVisualSlots == null) return null;
        for (int i = 0; i < conditionVisualSlots.Count; i++)
            if (conditionVisualSlots[i] != null && conditionVisualSlots[i].condition == condition)
                return conditionVisualSlots[i];
        return null;
    }

    public static bool IsBird(AnimalType type) => type is AnimalType.Chicken or AnimalType.Duck;

    public static int DefaultBornToAdultDays(AnimalType type) => type switch
    {
        AnimalType.Chicken => 28,
        AnimalType.Duck => 28,
        AnimalType.Goat => 42,
        AnimalType.Sheep => 42,
        _ => 56
    };

    public static int DefaultPurchasedToAdultDays(AnimalType type) => type switch
    {
        AnimalType.Chicken => 14,
        AnimalType.Duck => 14,
        AnimalType.Goat => 21,
        AnimalType.Sheep => 21,
        _ => 28
    };

    public static AnimalGrowthStage DefaultStage(AnimalType type, int growthDays, int adultDays)
    {
        if (growthDays >= adultDays) return AnimalGrowthStage.Adult;
        float progress = Mathf.Clamp01(growthDays / Mathf.Max(1f, adultDays));
        if (progress >= 0.78f) return AnimalGrowthStage.Adolescent;
        if (progress >= 0.48f) return AnimalGrowthStage.Young;
        if (progress >= 0.22f) return AnimalGrowthStage.Baby;
        return IsBird(type) ? AnimalGrowthStage.Hatchling : AnimalGrowthStage.Newborn;
    }

    void OnValidate()
    {
        incubationOrPregnancyDays = Mathf.Max(0, incubationOrPregnancyDays);
        bornToAdultDays = Mathf.Max(1, bornToAdultDays);
        purchasedYoungToAdultDays = Mathf.Clamp(purchasedYoungToAdultDays, 1, bornToAdultDays);
        productionIntervalDays = Mathf.Max(1, productionIntervalDays);
    }
}
