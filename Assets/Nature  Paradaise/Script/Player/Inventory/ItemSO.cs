using UnityEngine;

/// <summary>Kategori item untuk sorting inventory dan aturan gameplay.</summary>
public enum ItemCategory
{
    General,
    Seed,
    Food,
    Material,
    Tool,
    Weapon,
    Fish,
    AnimalProduct,
    Quest
}

/// <summary>Status kesiapan makanan yang menentukan apakah item dapat langsung dikonsumsi.</summary>
public enum FoodPreparation
{
    NotFood,
    Raw,
    ReadyToEat
}

/// <summary>Level pupuk yang menentukan kenaikan level durability tanah.</summary>
public enum FertilizerLevel : byte
{
    None,
    Basic,
    Quality,
    Premium,
    Deluxe,
    Divine
}

/// <summary>Kekuatan obat ternak. None berarti item biasa.</summary>
public enum AnimalMedicineLevel : byte
{
    None,
    Basic,
    Strong,
    Premium
}

[CreateAssetMenu(menuName = "Game/Item")]
/// <summary>
/// Sumber data utama item untuk inventory, icon UI, stack, harga, tool mapping,
/// model world/held, dan efek makanan. Asset ini direferensikan oleh save melalui identitasnya.
/// </summary>
public class ItemSO : ScriptableObject
{
    [Tooltip("Nama yang ditampilkan pada inventory, prompt pickup, dan feedback gameplay.")]
    public string itemName;
    [Tooltip("Sprite untuk slot inventory, hotbar, dan drag preview.")]
    public Sprite icon;
    [Tooltip("Harga beli satu item. Nilai 0 berarti tidak dijual.")]
    public int buyPrice;
    [Tooltip("Harga jual satu item. Nilai 0 berarti tidak dapat dijual normal.")]
    public int sellPrice;

    [Header("Inventory")]
    [Tooltip("Dipakai untuk sorting dan validasi fitur seperti eating atau tool selection.")]
    public ItemCategory category = ItemCategory.General;
    public bool isAnimalProduct;
    [Tooltip("Key Item tidak dapat dijual melalui shop, shipping, atau Market Stand.")]
    public bool isKeyItem;
    [Tooltip("Blokir item ini dari seluruh sistem penjualan meskipun Sell Price lebih dari 0.")]
    public bool isNotSellable;
    [Tooltip("Minta konfirmasi sebelum item dimasukkan ke Shipping Bin.")]
    public bool requiresSellConfirmation;
    [Tooltip("Jumlah maksimum dalam satu slot inventory.")]
    [Min(1)] public int maxStack = 16;
    [Tooltip("Jika item ini diletakkan di hotbar, tool ini yang aktif. None untuk item biasa.")]
    public PlayerToolType equippedTool = PlayerToolType.None;

    [Header("Animal Medicine")]
    [Tooltip("Level obat ternak. Basic untuk Unwell, Strong untuk Sick, Premium untuk Severely Sick.")]
    public AnimalMedicineLevel animalMedicineLevel = AnimalMedicineLevel.None;

    [Header("Seed")]
    [Tooltip("Tanaman yang dihasilkan oleh bibit ini. Isi untuk setiap ItemSO berkategori Seed.")]
    public CropDataSO seedCrop;

    [Header("Fertilizer")]
    [Tooltip("Tingkat pupuk tanah. Quality adalah Organic pada asset lama.")]
    public FertilizerLevel fertilizerLevel = FertilizerLevel.None;
    [Tooltip("Village Level minimum agar pupuk ini dapat dibeli dan digunakan.")]
    [Min(1)] public int requiredVillageLevel = 1;

    [Min(0)] public int soilRestoreAmount;

    [Header("Crop Quality Booster")]
    [Range(1, 5)] public int cropBoosterLevel = 1;
    // Field legacy dipertahankan untuk kompatibilitas asset, tidak memengaruhi growth.
    [HideInInspector] public int cropBoosterPercent;
    public int SoilRestoreAmount => soilRestoreAmount > 0 ? soilRestoreAmount :
        fertilizerLevel switch { FertilizerLevel.Basic => 10, FertilizerLevel.Quality => 20,
            FertilizerLevel.Premium => 35, FertilizerLevel.Deluxe => 50, FertilizerLevel.Divine => 80, _ => 0 };

    [Header("Held / World Actions")]
    [Tooltip("Izinkan stack item ini dijatuhkan dari hotbar sebagai object physics.")]
    public bool canDropToWorld;
    [Tooltip("Izinkan item ini diletakkan secara stabil memakai placement preview.")]
    public bool canPlaceInWorld;

    [Header("World / Held Visual")]
    [Tooltip("Prefab opsional saat item dipegang, dijatuhkan, atau diletakkan. Jika kosong digunakan mesh debug sederhana.")]
    public GameObject worldPrefab;
    [Tooltip("Skala model saat dipegang, dijatuhkan, atau diletakkan di dunia.")]
    public Vector3 worldScale = Vector3.one * 0.4f;

    [Header("Farm Placement")]
    [Range(0, 4)] public int sprinklerLevel;
    public TreeDefinition treeDefinition;
    public bool IsSprinkler => sprinklerLevel > 0;
    public bool IsFarmPlacement => IsSprinkler || treeDefinition != null;

    [Header("Eating")]
    [Tooltip("Menandai item sebagai kandidat makanan; category tetap harus Food.")]
    public bool isEdible;
    [Tooltip("Hanya ReadyToEat yang dapat dikonsumsi langsung.")]
    public FoodPreparation foodPreparation = FoodPreparation.NotFood;
    [Tooltip("HP yang dipulihkan per satu item.")]
    [Min(0f)] public float healthRestore;
    [Tooltip("Stamina yang dipulihkan per satu item.")]
    [Min(0f)] public float staminaRestore;
    [Tooltip("Hunger/fullness yang dipulihkan per satu item.")]
    [Min(0f)] public float hungerRestore;

    public int StackLimit => Mathf.Max(1, maxStack);
    public bool CanConsume =>
        category == ItemCategory.Food &&
        isEdible &&
        foodPreparation == FoodPreparation.ReadyToEat;

    public bool IsRawFood =>
        category == ItemCategory.Food && foodPreparation == FoodPreparation.Raw;

    public bool IsFertilizer =>
        equippedTool == PlayerToolType.Fertilizer && fertilizerLevel != FertilizerLevel.None;

    public bool IsCropBooster =>
        equippedTool == PlayerToolType.CropBooster && cropBoosterLevel > 0;

    public bool IsSeed =>
        category == ItemCategory.Seed || equippedTool == PlayerToolType.Seed;

    public bool IsAnimalMedicine => animalMedicineLevel != AnimalMedicineLevel.None;

    /// <summary>Aturan tunggal untuk barang yang boleh dititipkan pada Market Stand.</summary>
    public bool CanSellAtMarket =>
        sellPrice > 0 &&
        !isKeyItem &&
        !isNotSellable &&
        category != ItemCategory.Tool &&
        category != ItemCategory.Quest &&
        equippedTool == PlayerToolType.None;

    /// <summary>
    /// Harga jual aktual. Setiap quality star menambah 20%; ikan memakai ukuran
    /// 5-100 cm untuk multiplier 0.75-1.75 jika ukuran tersedia.
    /// </summary>
    public int GetMarketSellPrice(int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (!CanSellAtMarket)
            return 0;

        float qualityMultiplier = 1f + Mathf.Clamp(qualityStars, 0, 5) * 0.2f;
        float sizeMultiplier = 1f;
        if (category == ItemCategory.Fish && fishSizeCm > 0f)
            sizeMultiplier = Mathf.Lerp(0.75f, 1.75f, Mathf.InverseLerp(5f, 100f, fishSizeCm));

        return Mathf.Max(1, Mathf.RoundToInt(sellPrice * qualityMultiplier * sizeMultiplier));
    }

    /// <summary>True jika item memiliki setidaknya satu aksi world dan perlu divisualkan di tangan.</summary>
    public bool HasHeldWorldAction => canDropToWorld || canPlaceInWorld || IsSeed;
}
