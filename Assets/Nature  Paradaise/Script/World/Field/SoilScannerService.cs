using System.Collections.Generic;

/// <summary>Shared read/action API for the future machine, touch UI and testing menu.</summary>
public static class SoilScannerService
{
    public static void GetFields(List<FieldArea> results)
    {
        results.Clear();
        foreach (var field in FieldArea.ActiveAreas)
            if (field != null && field.UsesFieldSoil) results.Add(field);
        results.Sort((a, b) => a.FieldNumber.CompareTo(b.FieldNumber));
    }

    public static bool TryScan(int fieldNumber, out FieldSoilSnapshot snapshot, out string reason)
    {
        snapshot = default;
        if (!FieldArea.ValidateSoilIdentities(out reason)) return false;
        if (!FieldArea.TryGetByNumber(fieldNumber, out var field))
        { reason = "Field tidak ditemukan atau ID kembar"; return false; }
        snapshot = field.SoilSnapshot;
        return true;
    }

    public static bool TryUse(int fieldNumber, Inventory inventory, ItemSO restorer, out string reason)
    {
        if (!FieldArea.TryGetByNumber(fieldNumber, out var field))
        { reason = "Field tidak ditemukan atau ID kembar"; return false; }
        return field.TryBeginSoilRestoration(inventory, restorer, out reason);
    }
}
