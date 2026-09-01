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

    /// <summary>True jika item memiliki setidaknya satu aksi world dan perlu divisualkan di tangan.</summary>
    public bool HasHeldWorldAction => canDropToWorld || canPlaceInWorld;
}
