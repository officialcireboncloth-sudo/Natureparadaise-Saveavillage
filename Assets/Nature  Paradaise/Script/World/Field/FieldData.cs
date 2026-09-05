using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Status penggunaan satu tile field.</summary>
public enum TileState : byte
{
    Empty,
    Hoed,
    Planted,
    Building,
    Path,
    Reserved
}

/// <summary>Grade kualitas hasil panen dari terendah D hingga tertinggi S.</summary>
public enum CropGrade : byte
{
    D,
    C,
    B,
    A,
    S
}

/// <summary>Kondisi lifecycle crop yang terpisah dari stage visual.</summary>
public enum CropLifecycleState : byte
{
    Growing,
    Withered,
    HarvestReady,
    Regrowing,
    Dead
}

[Flags]
public enum CropWaterSource : byte
{
    None = 0,
    WateringCan = 1 << 0,
    Sprinkler = 1 << 1,
    Rain = 1 << 2,
    External = 1 << 3
}

public enum SoilDurabilityStatus : byte
{
    Barren,
    LowFertility,
    Normal,
    Fertile,
    VeryFertile
}

/// <summary>Channel statistik tile yang dapat dimodifikasi fitur eksternal.</summary>
public enum FieldEffectType : byte
{
    Moisture,
    Fertility,
    SoilQuality,
    CropHealth,
    GrowthBooster
}

[Serializable]
/// <summary>Perubahan terukur yang dapat diterapkan secara modular ke satu tile.</summary>
public struct FieldEffect
{
    public FieldEffectType type;
    public int amount;

    public FieldEffect(FieldEffectType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}

[Serializable]
/// <summary>Data runtime ringkas untuk kondisi tanah, tanaman, dan pemakaian tile.</summary>
public struct FieldTileData
{
    public TileState state;
    public byte soilQuality;
    public byte fertility;
    public byte moisture;
    public CropDataSO crop;
    public float growthDays;
    public byte growthStage;
    public byte cropHealth;
    public CropLifecycleState cropState;
    public CropWaterSource waterSourcesToday;
    public byte consecutiveDryDays;
    public byte recoveryWateredDays;
    public byte growthBoosterPercent;
    public CropQualityCare qualityCare;
    public float regrowDaysRemaining;
    public byte soilDurability;
    public byte soilRestDays;
    public bool fertilizedForCurrentCycle;
    public ushort careSamples;
    public uint totalMoisture;
    public uint totalFertility;
    public uint totalSoilQuality;
    public string buildingId;
    public bool dirty;

    public bool HasCrop => state == TileState.Planted && crop != null;
}

public readonly struct FieldTileSnapshot
{
    public readonly TileState State;
    public readonly byte SoilQuality;
    public readonly byte Fertility;
    public readonly byte Moisture;
    public readonly CropDataSO Crop;
    public readonly float GrowthDays;
    public readonly byte GrowthStage;
    public readonly byte CropHealth;
    public readonly CropLifecycleState CropState;
    public readonly CropWaterSource WaterSourcesToday;
    public readonly byte ConsecutiveDryDays;
    public readonly byte GrowthBoosterPercent;
    public readonly int BoosterLevelToday;
    public readonly float RegrowDaysRemaining;
    public readonly byte SoilDurability;
    public readonly byte SoilRestDays;
    public readonly bool FertilizedForCurrentCycle;

    public bool WateredToday => WaterSourcesToday != CropWaterSource.None;
    public bool IsHarvestReady => CropState == CropLifecycleState.HarvestReady;
    public int SoilLevel => FieldArea.GetSoilLevel(SoilDurability);
    public SoilDurabilityStatus SoilStatus => FieldArea.GetSoilStatus(SoilDurability);

    public FieldTileSnapshot(FieldTileData data)
    {
        State = data.state;
        SoilQuality = data.soilQuality;
        Fertility = data.fertility;
        Moisture = data.moisture;
        Crop = data.crop;
        GrowthDays = data.growthDays;
        GrowthStage = data.growthStage;
        CropHealth = data.cropHealth;
        CropState = data.cropState;
        WaterSourcesToday = data.waterSourcesToday;
        ConsecutiveDryDays = data.consecutiveDryDays;
        GrowthBoosterPercent = data.growthBoosterPercent;
        BoosterLevelToday = data.qualityCare != null ? data.qualityCare.boosterToday : 0;
        RegrowDaysRemaining = data.regrowDaysRemaining;
        SoilDurability = data.soilDurability;
        SoilRestDays = data.soilRestDays;
        FertilizedForCurrentCycle = data.fertilizedForCurrentCycle;
    }
}

public readonly struct CropHarvestResult
{
    public readonly string FieldId;
    public readonly Vector2Int Coordinate;
    public readonly ItemSO Item;
    public readonly int Amount;
    public readonly CropGrade Grade;

    public CropHarvestResult(
        string fieldId,
        Vector2Int coordinate,
        ItemSO item,
        int amount,
        CropGrade grade)
    {
        FieldId = fieldId;
        Coordinate = coordinate;
        Item = item;
        Amount = amount;
        Grade = grade;
    }
}

[Serializable]
/// <summary>Snapshot serializable seluruh field.</summary>
public class FieldSaveData
{
    public string fieldId;
    public int columns;
    public int rows;
    public List<FieldTileSaveData> tiles = new List<FieldTileSaveData>();
}

[Serializable]
/// <summary>Representasi serializable satu tile dalam save file.</summary>
public class FieldTileSaveData
{
    public int x;
    public int z;
    public TileState state;
    public byte soilQuality;
    public byte fertility;
    public byte moisture;
    public string cropId;
    public float growthDays;
    public byte growthStage;
    public byte cropHealth;
    public ushort careSamples;
    public uint totalMoisture;
    public uint totalFertility;
    public uint totalSoilQuality;
    public string buildingId;
    public bool hasAdvancedGrowthData;
    public CropLifecycleState cropState;
    public CropWaterSource waterSourcesToday;
    public byte consecutiveDryDays;
    public byte recoveryWateredDays;
    public byte growthBoosterPercent;
    public CropQualityCare qualityCare;
    public float regrowDaysRemaining;
    public bool hasSoilDurability;
    public byte soilDurability;
    public byte soilRestDays;
    public bool fertilizedForCurrentCycle;
}
