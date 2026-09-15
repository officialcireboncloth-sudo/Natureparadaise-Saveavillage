using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Kategori bangunan untuk filtering site, UI Carpenter, dan aturan gameplay.</summary>
public enum BuildingCategory
{
    House,
    Animal,
    Storage,
    Production,
    Utility,
    Public
}

public enum AnimalHousingKind { None, Barn, Coop }

/// <summary>Aturan area untuk preview free-placement sebuah bangunan.</summary>
public enum BuildingPlacementArea : byte
{
    OutsideField,
    FieldOnly,
    Anywhere
}

/// <summary>State persisten satu Property Site.</summary>
public enum BuildingConstructionState
{
    Available,
    UnderConstruction,
    Completed
}

/// <summary>Satu kebutuhan material untuk membangun atau meng-upgrade.</summary>
[Serializable]
public sealed class BuildingMaterialCost
{
    [Tooltip("Item material yang dikonsumsi saat konstruksi dikonfirmasi.")]
    public ItemSO item;
    [Tooltip("Jumlah material yang dibutuhkan.")]
    [Min(1)] public int amount = 1;
}

/// <summary>Konfigurasi biaya, durasi, visual, kapasitas, dan unlock satu level bangunan.</summary>
[Serializable]
public sealed class BuildingLevelDefinition
{
    [Min(1)] public int level = 1;
    [Tooltip("Village Level minimum sebelum level bangunan ini dapat dibeli.")]
    [Min(0)] public int requiredVillageLevel;
    [Tooltip("Gold yang dibayar ketika level ini mulai dibangun.")]
    [Min(0)] public int goldCost;
    [Tooltip("Jumlah pergantian hari sebelum konstruksi selesai.")]
    [Min(0)] public int constructionDays = 1;
    public List<BuildingMaterialCost> materialCosts = new();
    [Tooltip("Prefab final opsional. Jika kosong, BuildingSite memakai visual scene yang dapat diedit.")]
    public GameObject completedPrefab;
    [Tooltip("Kapasitas generik, misalnya jumlah hewan atau slot storage.")]
    [Min(0)] public int capacity;
    [Tooltip("ID fitur yang dibuka ketika level selesai, misalnya kitchen atau storage.")]
    public List<string> unlockIds = new();
}

/// <summary>
/// Data reusable satu jenis bangunan. Asset ini tidak menyimpan state instance;
/// posisi, level, dan progress konstruksi dimiliki oleh BuildingSite.
/// </summary>
[CreateAssetMenu(menuName = "Game/Building/Building Definition")]
public sealed class BuildingDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID stabil untuk Save/Load. Jangan diubah setelah dipakai save production.")]
    public string buildingId = "building.shed";
    public string displayName = "Shed";
    public Sprite icon;
    public BuildingCategory category = BuildingCategory.Utility;
    public AnimalHousingKind animalHousing;
    public AnimalHousingKind HousingKind => animalHousing != AnimalHousingKind.None ? animalHousing :
        buildingId == "building.barn" ? AnimalHousingKind.Barn : buildingId == "building.coop" ? AnimalHousingKind.Coop : AnimalHousingKind.None;

    [Header("Footprint")]
    [Tooltip("Lebar area grid yang harus kosong ketika bangunan ditempatkan.")]
    [Min(1)] public int footprintWidth = 3;
    [Tooltip("Kedalaman area grid yang harus kosong ketika bangunan ditempatkan.")]
    [Min(1)] public int footprintDepth = 3;

    [Header("Placement Orientation")]
    [Tooltip("Arah awal bangunan pada sumbu Y. Preview tidak mengambil arah hadap player; gunakan T untuk memutarnya.")]
    [Range(-180f, 180f)] public float defaultPlacementYaw;

    [Header("Rules")]
    [Tooltip("Outside Field menolak area tanam, Field Only wajib di area tanam, dan Anywhere dapat ditempatkan pada permukaan kosong mana pun.")]
    public BuildingPlacementArea placementArea = BuildingPlacementArea.OutsideField;
    public bool canRelocate = true;
    public bool canDemolish = true;
    [Tooltip("Level bangunan dalam urutan naik. Level pertama harus bernilai 1.")]
    public List<BuildingLevelDefinition> levels = new();

    /// <summary>Mengambil konfigurasi level tertentu atau null jika tidak tersedia.</summary>
    public BuildingLevelDefinition GetLevel(int level)
    {
        for (int index = 0; index < levels.Count; index++)
            if (levels[index] != null && levels[index].level == level)
                return levels[index];
        return null;
    }

    /// <summary>True jika definisi memiliki level setelah current level.</summary>
    public bool HasUpgradeAfter(int currentLevel) => GetLevel(currentLevel + 1) != null;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            buildingId = $"building.{name.ToLowerInvariant().Replace(' ', '-')}";

        footprintWidth = Mathf.Max(1, footprintWidth);
        footprintDepth = Mathf.Max(1, footprintDepth);

        for (int index = 0; index < levels.Count; index++)
        {
            if (levels[index] == null)
                continue;
            levels[index].level = Mathf.Max(1, levels[index].level);
            levels[index].requiredVillageLevel = Mathf.Max(0, levels[index].requiredVillageLevel);
            levels[index].goldCost = Mathf.Max(0, levels[index].goldCost);
            levels[index].constructionDays = Mathf.Max(0, levels[index].constructionDays);
        }
    }
}
