using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Lookup tunggal ItemSO berdasarkan ID stabil dengan fallback untuk save lama.</summary>
public static class ItemCatalog
{
    static ItemSO[] items;
    static Dictionary<string, ItemSO> byId;

    public static IReadOnlyList<ItemSO> All
    {
        get { EnsureLoaded(); return items; }
    }

    public static ItemSO Resolve(string itemId, string legacyAssetName = null, string legacyDisplayName = null)
    {
        EnsureLoaded();
        if (!string.IsNullOrWhiteSpace(itemId) && byId.TryGetValue(itemId.Trim(), out ItemSO exact))
            return exact;
        for (int i = 0; i < items.Length; i++)
        {
            ItemSO candidate = items[i];
            if (candidate == null) continue;
            if ((!string.IsNullOrEmpty(legacyAssetName) && candidate.name == legacyAssetName) ||
                (!string.IsNullOrEmpty(legacyDisplayName) && candidate.itemName == legacyDisplayName))
                return candidate;
        }
        return null;
    }

    public static string LegacyId(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName)) return "item.unknown";
        StringBuilder result = new("item.");
        bool separator = false;
        foreach (char character in assetName.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (separator && result[result.Length - 1] != '.') result.Append('_');
                result.Append(character);
                separator = false;
            }
            else separator = true;
        }
        return result.Length > 5 ? result.ToString() : "item.unknown";
    }

    public static void Invalidate()
    {
        items = null;
        byId = null;
    }

    static void EnsureLoaded()
    {
        if (items != null && byId != null) return;
        items = Resources.LoadAll<ItemSO>(string.Empty);
        byId = new Dictionary<string, ItemSO>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < items.Length; i++)
        {
            ItemSO item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
            if (!byId.TryAdd(item.Id, item))
                Debug.LogError($"[ITEM CATALOG] Item ID duplikat '{item.Id}': {byId[item.Id].name} dan {item.name}.");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Invalidate();
}
