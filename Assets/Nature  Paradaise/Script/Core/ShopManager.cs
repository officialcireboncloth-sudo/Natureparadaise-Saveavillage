using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menjalankan transaksi buy/sell terhadap inventory dan saldo player.
/// Harga dan identitas barang berasal dari <see cref="ItemSO"/> agar UI tidak menyimpan aturan ekonomi.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Player")]
    public Inventory playerInv;

    [Header("Items Sold By Shop")]
    public ItemSO seedItem;
    public List<ItemSO> fertilizerItems = new();
    public ItemSO cropBoosterItem;

    [Header("Animals Sold By Shop")]
    public List<AnimalShopOffer> animalOffers = new();
    [Tooltip("Lokasi hewan dikirim. Jika kosong, hewan muncul berdekatan dengan hewan farm pertama.")]
    public Transform animalDeliveryPoint;
    [Min(1)] public int maximumOwnedAnimals = 20;

    [Header("Items Bought By Shop")]
    public ItemSO cabbageItem;
    public ItemSO milkItem;

    void Awake()
    {
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        EnsureDefaultAnimalOffers();
    }

    void EnsureDefaultAnimalOffers()
    {
        animalOffers ??= new List<AnimalShopOffer>();
        if (animalOffers.Count > 0) return;

        animalOffers.Add(CreateDefaultOffer("Chicken Egg", AnimalType.Chicken, AnimalShopOfferKind.Egg, 500));
        animalOffers.Add(CreateDefaultOffer("Young Chicken", AnimalType.Chicken, AnimalShopOfferKind.Young, 1000));
        animalOffers.Add(CreateDefaultOffer("Duck Egg", AnimalType.Duck, AnimalShopOfferKind.Egg, 700));
        animalOffers.Add(CreateDefaultOffer("Young Duck", AnimalType.Duck, AnimalShopOfferKind.Young, 1400));
        animalOffers.Add(CreateDefaultOffer("Young Goat", AnimalType.Goat, AnimalShopOfferKind.Young, 3000));
        animalOffers.Add(CreateDefaultOffer("Young Sheep", AnimalType.Sheep, AnimalShopOfferKind.Young, 3500));
        AnimalShopOffer cow = CreateDefaultOffer("Young Cow", AnimalType.Cow, AnimalShopOfferKind.Young, 5000);
        cow.productItem = milkItem;
        animalOffers.Add(cow);
    }

    static AnimalShopOffer CreateDefaultOffer(
        string displayName,
        AnimalType type,
        AnimalShopOfferKind kind,
        int price)
    {
        return new AnimalShopOffer
        {
            displayName = displayName,
            animalType = type,
            offerKind = kind,
            price = price,
            requiredVillageLevel = 1
        };
    }

    // =====================================================
    // BUY
    // =====================================================

    /// <summary>Membeli seed sebanyak jumlah yang diminta jika saldo dan inventory mencukupi.</summary>
    public bool BuySeed(int amount = 1)
    {
        if (seedItem == null)
        {
            Debug.LogWarning("[SHOP] Seed Item belum di-assign.");
            return false;
        }

        return Buy(seedItem, amount);
    }

    public ItemSO GetFertilizerItem(int level)
    {
        for (int i = 0; i < fertilizerItems.Count; i++)
        {
            ItemSO item = fertilizerItems[i];
            if (item != null && item.IsFertilizer && (int)item.fertilizerLevel == level)
                return item;
        }

        return null;
    }

    /// <summary>Membeli pupuk sesuai level jika sudah terbuka oleh Village Level.</summary>
    public bool BuyFertilizer(int level, int amount = 1)
    {
        ItemSO item = GetFertilizerItem(level);
        if (item == null)
        {
            Debug.LogWarning($"[SHOP] Fertilizer Lv.{level} belum di-assign.");
            return false;
        }

        if (VillageProgressionService.Instance != null &&
            !VillageProgressionService.Instance.MeetsRequirement(item.requiredVillageLevel))
        {
            Debug.Log($"[SHOP] {item.itemName} terbuka pada Village Lv.{item.requiredVillageLevel}.");
            return false;
        }

        return Buy(item, amount);
    }

    public bool BuyCropBooster(int amount = 1)
    {
        if (cropBoosterItem == null || !cropBoosterItem.IsCropBooster)
        {
            Debug.LogWarning("[SHOP] Crop Booster belum di-assign.");
            return false;
        }
        return Buy(cropBoosterItem, amount);
    }

    public bool TryBuyItem(ItemSO item, int amount = 1) => Buy(item, amount);

    public bool TryBuyAnimal(AnimalShopOffer offer)
    {
        if (offer == null || offer.price < 0 || ScoreManager.Instance == null)
            return false;
        if (AnimalGrowthSystem.ActiveAnimalCount >= maximumOwnedAnimals)
        {
            SaveLoadFeedback.Instance?.ShowMessage($"Kapasitas hewan penuh ({maximumOwnedAnimals})");
            return false;
        }
        if (VillageProgressionService.Instance != null &&
            !VillageProgressionService.Instance.MeetsRequirement(offer.requiredVillageLevel))
            return false;
        if (offer.offerKind == AnimalShopOfferKind.Egg &&
            !AnimalGrowthProfileSO.IsBird(offer.animalType))
            return false;
        if (!ScoreManager.Instance.TrySpendPoints(offer.price))
            return false;

        AnimalGrowthSystem animal = SpawnAnimal(offer, GetAnimalDeliveryPosition(), true);
        if (animal != null)
        {
            Debug.Log($"[SHOP] Bought {offer.displayName} for {offer.price} Gold.");
            return true;
        }

        ScoreManager.Instance.AddPoints(offer.price);
        return false;
    }

    AnimalGrowthSystem SpawnAnimal(AnimalShopOffer offer, Vector3 position, bool initializePurchase)
    {
        GameObject animalObject = offer.animalPrefab != null
            ? Instantiate(offer.animalPrefab, position, Quaternion.identity)
            : CreateDummyAnimalObject(offer.animalType, position);
        if (animalObject == null) return null;

        AnimalController controller = animalObject.GetComponent<AnimalController>();
        if (controller == null) controller = animalObject.AddComponent<AnimalController>();
        AnimalGrowthSystem growth = animalObject.GetComponent<AnimalGrowthSystem>();
        if (growth == null) growth = animalObject.AddComponent<AnimalGrowthSystem>();

        if (initializePurchase)
        {
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
            growth.ConfigureShopPurchase(offer.animalType, offer.growthProfile, offer.offerKind, currentDay);
        }

        controller.ConfigureRuntime(playerInv, cabbageItem, offer.productItem, growth);
        return growth;
    }

    static GameObject CreateDummyAnimalObject(AnimalType type, Vector3 position)
    {
        GameObject animalObject = new($"Animal_{type}_Runtime");
        animalObject.transform.position = position;

        if (AnimalGrowthProfileSO.IsBird(type))
        {
            SphereCollider collider = animalObject.AddComponent<SphereCollider>();
            collider.radius = 0.55f;
            collider.center = new Vector3(0f, 0.55f, 0f);
        }
        else
        {
            CapsuleCollider collider = animalObject.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f;
            collider.height = 1.7f;
            collider.center = new Vector3(0f, 0.85f, 0f);
        }

        return animalObject;
    }

    Vector3 GetAnimalDeliveryPosition()
    {
        Vector3 origin;
        if (animalDeliveryPoint != null)
            origin = animalDeliveryPoint.position;
        else
        {
            AnimalGrowthSystem existingAnimal = FindFirstObjectByType<AnimalGrowthSystem>();
            origin = existingAnimal != null
                ? existingAnimal.transform.position
                : playerInv != null ? playerInv.transform.position + Vector3.forward * 3f : transform.position;
        }

        int index = AnimalGrowthSystem.ActiveAnimalCount;
        return origin + new Vector3((index % 4) * 1.8f, 0f, (index / 4) * 1.8f);
    }

    /// <summary>Membuat kembali hewan runtime yang tidak memiliki object scene saat load.</summary>
    public AnimalGrowthSystem RestoreAnimal(AnimalSaveData data)
    {
        if (data == null) return null;
        EnsureDefaultAnimalOffers();
        AnimalShopOffer offer = animalOffers.Find(candidate =>
            candidate != null && candidate.animalType == data.animalType &&
            ((data.birthSource == AnimalBirthSource.PurchasedEgg && candidate.offerKind == AnimalShopOfferKind.Egg) ||
             (data.birthSource != AnimalBirthSource.PurchasedEgg && candidate.offerKind == AnimalShopOfferKind.Young)));
        offer ??= CreateDefaultOffer(data.animalType.ToString(), data.animalType, AnimalShopOfferKind.Young, 0);
        if (data.animalType == AnimalType.Cow && offer.productItem == null) offer.productItem = milkItem;

        AnimalGrowthSystem growth = SpawnAnimal(offer, new Vector3(data.x, data.y, data.z), false);
        growth?.PrepareRestoreProfile(data.animalType, offer.growthProfile);
        growth?.Restore(data);
        return growth;
    }

    public bool TrySellItem(ItemSO item, int amount = 1) => Sell(item, amount);

    bool Buy(ItemSO item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SHOP] Player Inventory belum ditemukan."
            );
            return false;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "[SHOP] ScoreManager tidak ditemukan."
            );
            return false;
        }

        int totalPrice = item.buyPrice * amount;

        // Cek Gold
        if (!ScoreManager.Instance.TrySpendPoints(totalPrice))
            return false;

        // Jika inventory penuh, transaksi dibatalkan dan Gold dikembalikan.
        if (!playerInv.Add(item, amount))
        {
            ScoreManager.Instance.AddPoints(totalPrice);
            Debug.Log("[SHOP] Inventory penuh. Pembelian dibatalkan.");
            return false;
        }

        Debug.Log(
            $"[SHOP] Bought {item.itemName} x{amount} " +
            $"for {totalPrice} Gold."
        );

        return true;
    }

    // =====================================================
    // SELL CABBAGE
    // =====================================================

    /// <summary>Menjual cabbage dari inventory menggunakan harga ItemSO.</summary>
    public bool SellCabbage(int amount = 1)
    {
        if (cabbageItem == null)
        {
            Debug.LogWarning(
                "[SHOP] Cabbage Item belum di-assign."
            );
            return false;
        }

        return Sell(cabbageItem, amount);
    }

    // =====================================================
    // SELL MILK
    // =====================================================

    /// <summary>Menjual milk dari inventory menggunakan harga ItemSO.</summary>
    public bool SellMilk(int amount = 1)
    {
        if (milkItem == null)
        {
            Debug.LogWarning(
                "[SHOP] Milk Item belum di-assign."
            );
            return false;
        }

        return Sell(milkItem, amount);
    }

    // =====================================================
    // GENERIC SELL
    // =====================================================

    bool Sell(ItemSO item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SHOP] Player Inventory belum ditemukan."
            );
            return false;
        }

        // Cek inventory
        if (playerInv.GetCount(item) < amount)
        {
            Debug.Log(
                $"[SHOP] Tidak punya {item.itemName} " +
                $"untuk dijual."
            );

            return false;
        }

        int totalPrice = item.sellPrice * amount;

        // Hapus item dari inventory
        if (!playerInv.Remove(item, amount))
            return false;

        // Tambahkan Gold
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddPoints(totalPrice);
        }
        else
        {
            Debug.LogWarning(
                "[SHOP] ScoreManager tidak ditemukan."
            );
        }

        Debug.Log(
            $"[SHOP] Sold {item.itemName} x{amount} " +
            $"for {totalPrice} Gold."
        );

        return true;
    }
}
