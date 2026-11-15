using System.Collections.Generic;
using UnityEngine;

/// <summary>Aturan grid bersama untuk sprinkler dan bibit pohon persisten.</summary>
public static class FarmPlacement
{
    public static IEnumerable<Vector2Int> Offsets(int level)
    {
        level = Mathf.Clamp(level, 1, 5);
        int radius = level <= 2 ? 1 : level == 3 ? 2 : level == 4 ? 3 : 5;
        for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance == 0) continue;
                // Lv2 extends the four neighbours with two front diagonals.
                if (distance <= radius || level == 2 && z == 1 && Mathf.Abs(x) == 1)
                    yield return new Vector2Int(x, z);
            }
    }

    public static int CoverageCount(int level) => Mathf.Clamp(level, 1, 5) switch
    {
        1 => 4, 2 => 6, 3 => 12, 4 => 24, _ => 60
    };

    public static bool Occupied(FieldArea field, int x, int z)
    {
        foreach (PlacedWorldItem placed in PlacedWorldItem.Active)
            if (placed != null && placed.IsInstalledFarmItem &&
                FieldArea.TryGetAt(placed.transform.position, out FieldArea owner, out int px, out int pz) &&
                owner == field && px == x && pz == z) return true;
        return false;
    }

    public static bool CanPlace(ItemSO item, ref Vector3 point)
    {
        if (!FieldArea.TryGetAt(point, out FieldArea field, out int x, out int z) ||
            !field.CanPlaceFarmItem(x, z)) return false;
        if (Mathf.Abs(point.y - field.GridToWorld(x, z).y) > field.CellSize * 0.5f) return false;
        point = field.GridToWorld(x, z) + Vector3.up * 0.02f;
        return true;
    }

    public static bool Covered(FieldArea field, int x, int z)
    {
        foreach (PlacedWorldItem placed in PlacedWorldItem.Active)
        {
            if (placed == null || !placed.IsInstalledFarmItem || !placed.Item.IsSprinkler ||
                !FieldArea.TryGetAt(placed.transform.position, out FieldArea owner, out int sx, out int sz) || owner != field) continue;
            foreach (Vector2Int offset in Offsets(placed.Item.sprinklerLevel))
                if (sx + offset.x == x && sz + offset.y == z) return true;
        }
        return false;
    }

    public static bool Covered(Vector3 point) =>
        FieldArea.TryGetAt(point, out FieldArea field, out int x, out int z) && Covered(field, x, z);

    /// <summary>Dipanggil field sebelum growth dan sesudah reset, tanpa urutan event implisit.</summary>
    public static void WaterField(FieldArea field)
    {
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsRainToday && !field.IsWeatherProtected) return;
        foreach (PlacedWorldItem placed in PlacedWorldItem.Active)
        {
            if (placed == null || !placed.IsInstalledFarmItem || !placed.Item.IsSprinkler ||
                !FieldArea.TryGetAt(placed.transform.position, out FieldArea owner, out int x, out int z) || owner != field) continue;
            foreach (Vector2Int offset in Offsets(placed.Item.sprinklerLevel))
                field.WaterBySprinkler(x + offset.x, z + offset.y);
        }
    }
}
