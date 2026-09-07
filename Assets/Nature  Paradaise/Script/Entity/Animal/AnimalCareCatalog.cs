using UnityEngine;

/// <summary>Item care dan produk default per spesies; semua referensi dapat diganti artist/designer.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/Animal/Care Catalog")]
public sealed class AnimalCareCatalog : ScriptableObject
{
    public ItemSO fodder;
    public ItemSO treat;
    public ItemSO medicine;
    [Tooltip("Urutan Basic, Strong, Premium. Field Medicine lama tetap menjadi fallback Basic.")]
    public ItemSO[] medicines = new ItemSO[3];
    public ItemSO[] products = new ItemSO[5];
    public static AnimalCareCatalog Load() => Resources.Load<AnimalCareCatalog>("Catalogs/AnimalCareCatalog");
    public ItemSO Product(AnimalType type) => products != null && (int)type < products.Length ? products[(int)type] : null;
    public ItemSO Medicine(AnimalMedicineLevel level)
    {
        int index = (int)level - 1;
        if (medicines != null && index >= 0 && index < medicines.Length && medicines[index] != null)
            return medicines[index];
        return level == AnimalMedicineLevel.Basic ? medicine : null;
    }
    public static string QualityName(int quality) => quality switch
    { 2 => "Bronze", 3 => "Silver", 4 => "Gold", 5 => "Premium", _ => "Normal" };
}
