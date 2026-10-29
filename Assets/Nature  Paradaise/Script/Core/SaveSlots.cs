using System;
using System.IO;
using UnityEngine;

/// <summary>Four manual saves and one separate autosave; slot 1 retains the legacy filename.</summary>
public static class SaveSlots
{
    public const int Count = 4;
    public const int AutoSaveSlot = Count + 1;
    public static int ActiveSlot { get; private set; } = 1;

    public static bool IsValid(int slot) => slot >= 1 && slot <= Count;
    /// <summary>Loadable destinations include the autosave, which manual save menus cannot overwrite.</summary>
    public static bool IsReadable(int slot) => IsValid(slot) || slot == AutoSaveSlot;

    /// <summary>Resolves a slot without changing the currently played save.</summary>
    public static string GetPath(int slot)
    {
        if (!IsReadable(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(Application.persistentDataPath, slot == AutoSaveSlot ? "autosave.json" :
            slot == 1 ? "savegame.json" : $"savegame_slot{slot}.json");
    }

    public static bool Exists(int slot) => IsReadable(slot) && File.Exists(GetPath(slot));
    public static bool HasAnySave
    {
        get { for (int slot = 1; slot <= AutoSaveSlot; slot++) if (Exists(slot)) return true; return false; }
    }

    /// <summary>Tracks the selected or restored save. Automatic writes always use their separate file.</summary>
    public static bool Select(int slot)
    {
        if (!IsReadable(slot)) return false;
        ActiveSlot = slot;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() => ActiveSlot = 1;

    [Serializable]
    sealed class Header
    {
        public int gold, day, hour, minute;
        public bool hasPlayerLocation;
        public PlayerLocationSaveData savedPlayerLocation;
    }

    /// <summary>Reads only the small header schema; inventory and world lists are not deserialized for the menu.</summary>
    public static SaveSlotInfo ReadInfo(int slot)
    {
        var info = new SaveSlotInfo { slot = slot, exists = Exists(slot) };
        if (!info.exists) return info;
        try
        {
            string path = GetPath(slot);
            var header = JsonUtility.FromJson<Header>(File.ReadAllText(path));
            if (header == null || header.day < 1 || header.hour < 0 || header.hour > 23 || header.minute < 0 || header.minute > 59)
                throw new InvalidDataException("Invalid save calendar");
            info.canLoad = true;
            info.day = header.day; info.hour = header.hour; info.minute = header.minute; info.gold = header.gold;
            info.savedAt = File.GetLastWriteTimeUtc(path).ToLocalTime();
            var location = header.hasPlayerLocation ? header.savedPlayerLocation : null;
            info.location = location != null && location.insideInterior ? InteriorLabel(location.interiorScene) : "Luar ruangan";
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            info.problem = "Data tidak dapat dibaca";
        }
        return info;
    }

    static string InteriorLabel(string scene)
    {
        switch (scene)
        {
            case "HouseInterior": return "Rumah";
            case "BarnInterior": return "Kandang ternak";
            case "CoopInterior": return "Kandang unggas";
            case "CaveInterior": return "Gua";
            default: return "Interior";
        }
    }
}

public sealed class SaveSlotInfo
{
    public int slot, day, hour, minute, gold;
    public bool exists, canLoad;
    public DateTime savedAt;
    public string location, problem;

    public string CalendarLabel
    {
        get
        {
            GameScheduleData.Date(day,out int year,out int season,out int date);
            return $"Hari {date} · {GameScheduleData.SeasonName(season)} · Th. {year} · {hour:00}:{minute:00}";
        }
    }
}
