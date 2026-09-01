using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog reusable untuk daftar bangunan yang boleh dipilih pada Property Site.
/// Asset ini hanya menyimpan referensi konfigurasi; state bangunan tetap dimiliki BuildingSite.
/// </summary>
[CreateAssetMenu(menuName = "Game/Building/Building Catalog")]
public sealed class BuildingCatalogSO : ScriptableObject
{
    [Tooltip("Urutan daftar juga menjadi urutan pilihan Previous/Next pada prototype interaction.")]
    [SerializeField] List<BuildingDefinitionSO> buildings = new();

    public int Count => buildings != null ? buildings.Count : 0;

    /// <summary>Mengambil definition berdasarkan index dan mengembalikan null bila tidak valid.</summary>
    public BuildingDefinitionSO GetAt(int index)
    {
        return index >= 0 && index < Count ? buildings[index] : null;
    }

    /// <summary>Mencari definition menggunakan ID stabil yang disimpan SaveGame.</summary>
    public BuildingDefinitionSO FindById(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildings == null)
            return null;

        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingDefinitionSO candidate = buildings[index];
            if (candidate != null && candidate.buildingId == buildingId)
                return candidate;
        }

        return null;
    }

    /// <summary>Mengembalikan index definition, atau nol sebagai fallback pilihan awal.</summary>
    public int IndexOf(BuildingDefinitionSO definition)
    {
        if (definition == null || buildings == null)
            return 0;

        int index = buildings.IndexOf(definition);
        return index >= 0 ? index : 0;
    }

    void OnValidate()
    {
        if (buildings == null)
            buildings = new List<BuildingDefinitionSO>();

        // Definition null dan duplikat dibuang agar navigasi catalog konsisten.
        HashSet<BuildingDefinitionSO> unique = new();
        for (int index = buildings.Count - 1; index >= 0; index--)
        {
            BuildingDefinitionSO definition = buildings[index];
            if (definition == null || !unique.Add(definition))
                buildings.RemoveAt(index);
        }
    }
}
