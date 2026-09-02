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
    Deluxe
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
    [Tooltip("Jumlah maksimum dalam satu slot inventory.")]
    [Min(1)] public int maxStack = 16;
    [Tooltip("Jika item ini diletakkan di hotbar, tool ini yang aktif. None untuk item biasa.")]
    public PlayerToolType equippedTool = PlayerToolType.None;

    [Header("Seed")]
    [Tooltip("Tanaman yang dihasilkan oleh bibit ini. Isi untuk setiap ItemSO berkategori Seed.")]
    public CropDataSO seedCrop;

    [Header("Fertilizer")]
    [Tooltip("Basic +1 level, Quality +2, Premium +3, dan Deluxe langsung maksimum.")]
    public FertilizerLevel fertilizerLevel = FertilizerLevel.None;
    [Tooltip("Village Level minimum agar pupuk ini dapat dibeli dan digunakan.")]
    [Min(1)] public int requiredVillageLevel = 1;

    [Header("Crop Booster")]
    [Tooltip("Persentase pengurangan durasi growth. Nilai 20 mengubah 5 growth days menjadi sekitar 4 hari.")]
    [Range(0, 80)] public int cropBoosterPercent;

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
        equippedTool == PlayerToolType.CropBooster && cropBoosterPercent > 0;

    public bool IsSeed =>
        category == ItemCategory.Seed || equippedTool == PlayerToolType.Seed;

    /// <summary>True jika item memiliki setidaknya satu aksi world dan perlu divisualkan di tangan.</summary>
    public bool HasHeldWorldAction => canDropToWorld || canPlaceInWorld || IsSeed;
}
