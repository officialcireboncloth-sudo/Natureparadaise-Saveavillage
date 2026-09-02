using System;
using System.Collections.Generic;
using UnityEngine;

public enum AnimalType { Chicken, Duck, Goat, Sheep, Cow }
public enum AnimalBirthSource { HatchedOnFarm, BornOnFarm, PurchasedYoung, PurchasedEgg }
public enum AnimalGrowthStage { Egg, Pregnancy, Hatchling, Newborn, Baby, Young, Adolescent, Adult }
public enum AnimalHealthState { Healthy, Sick }
public enum AnimalShopOfferKind { Egg, Young }

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

/// <summary>
/// Profile modular satu spesies untuk durasi growth/production dan model setiap stage.
/// </summary>
[CreateAssetMenu(fileName = "Animal Growth Profile", menuName = "Nature Paradise/Animal/Growth Profile")]
public sealed class AnimalGrowthProfileSO : ScriptableObject
{
    public AnimalType animalType = AnimalType.Cow;
    [Min(0)] public int incubationOrPregnancyDays = 7;
    [Min(1)] public int bornToAdultDays = 56;
    [Min(1)] public int purchasedYoungToAdultDays = 28;
    public ItemSO productItem;
    [Min(1)] public int productionIntervalDays = 1;
    public List<AnimalGrowthStageSlot> stageSlots = new();

    public AnimalGrowthStageSlot GetStageSlot(AnimalGrowthStage stage)
    {
        if (stageSlots == null) return null;
        for (int i = 0; i < stageSlots.Count; i++)
            if (stageSlots[i] != null && stageSlots[i].stage == stage)
                return stageSlots[i];
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
