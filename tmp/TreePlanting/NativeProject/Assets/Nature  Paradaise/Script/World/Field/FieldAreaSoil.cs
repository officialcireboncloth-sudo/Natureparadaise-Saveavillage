using System;
using UnityEngine;

public sealed partial class FieldArea
{
    [Header("Whole Field Soil / Scanner")]
    [Tooltip("ID unik untuk scanner: 1 = Field 1, 2 = Field 2. Setelah duplicate, ubah ID ini. 0 mempertahankan soil tile legacy (mis. greenhouse). Jangan ubah ID setelah membuat save.")]
    [SerializeField, Min(0)] int fieldNumber;
    [Tooltip("Aturan bersama; kondisi dan penurunan kesuburan bisa di-tuning tanpa mengubah ukuran grid.")]
    [SerializeField] FieldSoilRulesSO fieldSoilRules;
    FieldSoilState fieldSoil;
    bool soilTransactionInProgress;
    int careSoilDay=-1,careSoilFertility;
    /// <summary>Same completed-day soil sample for crops and trees, regardless of event subscriber order.</summary>
    public int FertilityForCare(int calendarDay,int x,int z)
    {
        if(!UsesFieldSoil)return TryGetSnapshot(x,z,out var tile)?tile.SoilDurability:0;
        if(careSoilDay!=calendarDay)
        {careSoilFertility=SoilSnapshot.Fertility;careSoilDay=calendarDay;}
        return careSoilFertility;
    }

    public int FieldNumber => fieldNumber;
    public bool UsesFieldSoil => fieldNumber > 0;
    public string SoilDisplayName => UsesFieldSoil ? "Field " + fieldNumber : name;
    public bool IsSoilLocked => UsesFieldSoil && fieldSoil != null && fieldSoil.IsRestoring;
    public event Action<FieldArea> SoilChanged;
    public FieldSoilSnapshot SoilSnapshot
    {
        get
        {
            InitializeFieldSoil();
            return new FieldSoilSnapshot(this, fieldSoil, fieldSoilRules);
        }
    }

    void InitializeFieldSoil()
    {
        if (fieldSoilRules == null)
            fieldSoilRules = Resources.Load<FieldSoilRulesSO>("Profiles/Farming/Field Soil Rules");
        if (fieldSoil == null)
            fieldSoil = new FieldSoilState
            {
                fertility = Mathf.Clamp(fieldSoilRules != null ? fieldSoilRules.initialFertility : 80, 0, 80),
                lastProcessedDay = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : 1
            };
    }

    public static bool ValidateSoilIdentities(out string problem)
    {
        for (int i = 0; i < ActiveAreasInternal.Count; i++)
        {
            var field = ActiveAreasInternal[i];
            if (field == null) continue;
            if (string.IsNullOrWhiteSpace(field.FieldId))
            { problem = "Field ID belum diisi pada " + field.name; return false; }
            for (int j = i + 1; j < ActiveAreasInternal.Count; j++)
            {
                var other = ActiveAreasInternal[j];
                if (other == null) continue;
                if (field.FieldId == other.FieldId || field.UsesFieldSoil && other.UsesFieldSoil && field.fieldNumber == other.fieldNumber)
                { problem = $"Field ID kembar: {field.SoilDisplayName} ({field.name} / {other.name}). Ubah Field Number di Inspector."; return false; }
            }
        }
        problem = null;
        return true;
    }

    public static bool TryGetByNumber(int number, out FieldArea field)
    {
        field = null;
        if (number < 1) return false;
        foreach (var area in ActiveAreasInternal)
        {
            if (area == null || area.fieldNumber != number) continue;
            if (field != null) { field = null; return false; }
            field = area;
        }
        return field != null;
    }

    public bool CanBeginSoilRestoration(out string reason)
    {
        InitializeFieldSoil();
        reason = null;
        if (!UsesFieldSoil) reason = "Atur Field Number terlebih dahulu";
        else if (!ValidateSoilIdentities(out reason)) return false;
        else if (IsSoilLocked) reason = "Field sedang dalam penyuburan";
        else if (fieldSoil.fertility >= 80) reason = "Kesuburan tanah sudah maksimal";
        else if (tiles == null) reason = "Field belum siap";
        else
        {
            foreach (var tile in tiles)
                if (tile.HasCrop || tile.state == TileState.Building)
                { reason = "Kosongkan tanaman dan bangunan sebelum penyuburan"; break; }
            if (reason == null)
                for (int z = 0; z < rows && reason == null; z++)
                    for (int x = 0; x < columns; x++)
                        if (FarmPlacement.Occupied(this, x, z))
                        { reason = "Pindahkan pohon atau perlengkapan tanam sebelum penyuburan"; break; }
        }
        return reason == null;
    }

    /// <summary>Shared scanner transaction: one Terra consumed only after every validation passes.</summary>
    public bool TryBeginSoilRestoration(Inventory inventory, ItemSO restorer, out string reason)
    {
        reason = null;
        if (soilTransactionInProgress) { reason = "Penyuburan sedang diproses"; return false; }
        if (inventory == null || restorer == null || !restorer.IsSoilRestorer)
        { reason = "Pilih penyubur Terra dari tas"; return false; }
        if (!CanBeginSoilRestoration(out reason)) return false;
        if (inventory.GetCount(restorer) < 1) { reason = "Penyubur tidak ada di tas"; return false; }
        soilTransactionInProgress = true;
        try
        {
            if (!inventory.Remove(restorer, 1)) { reason = "Penyubur tidak ada di tas"; return false; }
            int today = TimeManager.Instance != null ? TimeManager.Instance.day : fieldSoil.lastProcessedDay;
            if (!fieldSoil.BeginRestoration(restorer.soilRestorationDays, restorer.Id, today))
            {
                inventory.Add(restorer, 1);
                reason = "Penyuburan belum dapat dimulai";
                return false;
            }
            SoilChanged?.Invoke(this);
            return true;
        }
        finally { soilTransactionInProgress = false; }
    }

    public void AdvanceFieldSoilToDay(int day)
    {
        if (!UsesFieldSoil) return;
        InitializeFieldSoil();
        FertilityForCare(day-1,0,0);
        bool occupied = false;
        if (tiles != null)
            foreach (int index in activeTileIndices)
                if (tiles[index].HasCrop) { occupied = true; break; }
        if (fieldSoil.AdvanceToDay(day, occupied, fieldSoilRules != null ? fieldSoilRules.lossPerOccupiedDay : 1))
            SoilChanged?.Invoke(this);
    }

    void RestoreFieldSoil(FieldSaveData data)
    {
        careSoilDay=-1;
        if (!UsesFieldSoil) return;
        InitializeFieldSoil();
        if (data.soil != null) fieldSoil = data.soil.Copy();
        else
        {
            // Old saves keep crop/tile coordinates and receive a conservative field-wide average.
            long total = (long)80 * columns * rows;
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (var tile in data.tiles)
                if (tile != null && tile.hasSoilDurability && InBounds(tile.x, tile.z) && seen.Add(ToIndex(tile.x, tile.z)))
                    total += Mathf.Clamp(tile.soilDurability, 0, 80) - 80;
            fieldSoil = new FieldSoilState
            {
                fertility = (int)(total / Math.Max(1, columns * rows)),
                lastProcessedDay = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : 1
            };
        }
        SoilChanged?.Invoke(this);
    }

    public bool DebugSetFieldFertility(int value)
    {
        if (!PropertySite.DebugShortcutsEnabled || !UsesFieldSoil || IsSoilLocked) return false;
        InitializeFieldSoil();
        fieldSoil.fertility = Mathf.Clamp(value, 0, 80);
        SoilChanged?.Invoke(this);
        return true;
    }
}
