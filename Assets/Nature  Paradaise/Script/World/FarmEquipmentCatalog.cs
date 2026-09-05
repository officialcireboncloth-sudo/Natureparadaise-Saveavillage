using UnityEngine;

/// <summary>Katalog toko; tidak mendaftarkan resep crafting sprinkler.</summary>
[CreateAssetMenu(menuName = "Game/Farming/Equipment Catalog")]
public sealed class FarmEquipmentCatalog : ScriptableObject
{
    public ItemSO[] sprinklers;
    public ItemSO[] treeSeeds;
    public static FarmEquipmentCatalog Load() => Resources.Load<FarmEquipmentCatalog>("Catalogs/FarmEquipmentCatalog");
}
