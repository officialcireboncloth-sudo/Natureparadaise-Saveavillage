using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FishCollectionEntrySaveData
{
    public string fishId;
    public string itemId;
    public int caughtCount;
    public float largestSizeCm;
}

/// <summary>Encyclopedia ikan persisten, terpisah dari inventory.</summary>
public static class FishCollectionService
{
    static readonly Dictionary<string, FishCollectionEntrySaveData> Entries = new();
    public static event Action CollectionChanged;
    public static IReadOnlyCollection<FishCollectionEntrySaveData> Collection => Entries.Values;

    public static void Record(FishDefinitionSO fish, float sizeCm)
    {
        if (fish == null || string.IsNullOrWhiteSpace(fish.fishId)) return;
        if (!Entries.TryGetValue(fish.fishId, out FishCollectionEntrySaveData entry))
        {
            entry = new FishCollectionEntrySaveData { fishId = fish.fishId, itemId = fish.item != null ? fish.item.Id : string.Empty };
            Entries.Add(fish.fishId, entry);
        }
        entry.caughtCount++;
        entry.largestSizeCm = Mathf.Max(entry.largestSizeCm, sizeCm);
        CollectionChanged?.Invoke();
    }

    public static List<FishCollectionEntrySaveData> Capture()
    {
        List<FishCollectionEntrySaveData> result = new(Entries.Count);
        foreach (FishCollectionEntrySaveData entry in Entries.Values)
            result.Add(new FishCollectionEntrySaveData { fishId = entry.fishId, itemId = entry.itemId, caughtCount = entry.caughtCount, largestSizeCm = entry.largestSizeCm });
        return result;
    }

    public static void Restore(List<FishCollectionEntrySaveData> data)
    {
        Entries.Clear();
        if (data != null)
            foreach (FishCollectionEntrySaveData entry in data)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.fishId)) Entries[entry.fishId] = entry;
        CollectionChanged?.Invoke();
    }

    public static void Clear()
    {
        Entries.Clear();
        CollectionChanged?.Invoke();
    }
}
