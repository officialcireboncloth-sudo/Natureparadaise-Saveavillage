using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

// Engine boundaries only. CropDataSO, FieldTileData, CropQualityCare and the
// FieldArea debug implementation are compiled from the actual runtime sources.
namespace UnityEngine
{
    public class ScriptableObject { public string name; }
    public class GameObject { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x = x; this.y = y; } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x * b, a.y * b, a.z * b);
    }
    public struct Quaternion
    {
        public static Quaternion identity => new();
        public static Quaternion Euler(Vector3 v) => new();
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Clamp(int a, int min, int max) => Math.Clamp(a, min, max);
        public static float Clamp(float a, float min, float max) => Math.Clamp(a, min, max);
        public static float Clamp01(float a) => Math.Clamp(a, 0, 1);
    }
}
public class ItemSO { }
public class FieldSoilState { }
public static class GameScheduleData
{
    public static void Date(int day, out int year, out int season, out int date)
    { year = 1; season = 0; date = day; }
}
public static class PropertySite { public static bool DebugShortcutsEnabled = true; }
public class TimeManager { public static TimeManager Instance = new(); public int day = 17; }
public class FieldCropView
{
    public int Stage = -1;
    public CropDataSO Crop;
    public CropLifecycleState State;
    public void ApplyStage(int stage, CropDataSO crop, CropLifecycleState state)
    { Stage = stage; Crop = crop; State = state; }
}
public sealed partial class FieldArea
{
    readonly FieldTileData[] tiles;
    readonly FieldCropView[] cropViews;
    public bool IsSoilLocked;
    public int Changed, Refreshed;
    public FieldArea(params FieldTileData[] planted)
    { tiles = planted; cropViews = planted.Select(_ => new FieldCropView()).ToArray(); }
    public FieldTileData Tile(int x) => tiles[x];
    public FieldCropView View(int x) => cropViews[x];
    bool TryGetIndex(int x, int z, out int index)
    { index = x; return z == 0 && x >= 0 && x < tiles.Length; }
    void RefreshViewAt(int index) => Refreshed++;
    void NotifyChanged(int x, int z) => Changed++;
    public static int GetSoilLevel(int value) => 0;
    public static SoilDurabilityStatus GetSoilStatus(int value) => SoilDurabilityStatus.Normal;
}

static class CropStageDebugTests
{
    static int checks;
    static void Check(bool ok, string label)
    { checks++; if (!ok) throw new Exception(label); }
    static CropDataSO Crop(int days, params int[] schedule) => new()
    {
        daysUntilFirstHarvest = days,
        visualStages = schedule.Select((day, i) => new CropVisualStage
            { startsOnDay = day, stageName = "Stage " + i }).ToArray()
    };
    static FieldTileData Plant(CropDataSO crop) => new()
    {
        state = TileState.Planted, crop = crop, cropState = CropLifecycleState.Growing,
        cropHealth = 73, soilQuality = 61, fertility = 80, moisture = 55,
        surfaceWetness = .32f, soilDurability = 59, soilRestDays = 2,
        waterSourcesToday = CropWaterSource.Rain, consecutiveDryDays = 1,
        recoveryWateredDays = 1, growthBoosterPercent = 20, fertilizedForCurrentCycle = true,
        careSamples = 2, totalMoisture = 101, totalFertility = 152, totalSoilQuality = 120,
        qualityCare = new CropQualityCare
        {
            days = 2, wateredDays = 1, boostedDays = 2, boosterToday = 3,
            fertilizerDaysRemaining = 5, targetGrowthDays = (int)crop.TotalGrowthDays,
            soilFertilityTotal = 152, lastSampleDay = 16, qualityVersion = 1,
            roll = .41f, mutationRoll = .23f, doubleYieldRoll = .54f, harvestRollsReady = true
        }
    };
    static void UnchangedCare(CropQualityCare before, CropQualityCare after)
    {
        foreach (var field in typeof(CropQualityCare).GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (field.Name != "readyDay")
                Check(Equals(field.GetValue(before), field.GetValue(after)), "Care preserved: " + field.Name);
    }
    static void UnchangedTile(FieldTileData before, FieldTileData after)
    {
        string[] allowed = { "growthDays", "growthStage", "cropState", "regrowDaysRemaining", "dirty" };
        foreach (var field in typeof(FieldTileData).GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (!allowed.Contains(field.Name))
                Check(Equals(field.GetValue(before), field.GetValue(after)), "Tile preserved: " + field.Name);
    }
    static void WalkAllStages(CropDataSO crop)
    {
        var planted = Plant(crop);
        var care = planted.qualityCare.Copy();
        var field = new FieldArea(planted);
        int day = TimeManager.Instance.day;
        for (int stage = 1; stage < crop.StageCount; stage++)
        {
            float previous = field.Tile(0).growthDays;
            Check(field.DebugAdvanceCropStage(0, 0, false), crop.name + " advance " + stage);
            var tile = field.Tile(0);
            Check(tile.growthStage == stage, crop.name + " exactly one stage " + stage);
            Check(crop.GetStageForGrowth(tile.growthDays) == stage, "Growth clock and visual agree");
            Check(tile.growthDays > previous && tile.growthDays <= crop.TotalGrowthDays, "Monotonic growth");
            Check(tile.dirty && field.View(0).Stage == stage && field.View(0).Crop == crop, "Live view + dirty state");
            Check(tile.cropState == (stage == crop.StageCount - 1 ? CropLifecycleState.HarvestReady : CropLifecycleState.Growing), "Harvest state agrees");
            UnchangedTile(planted, tile);
            UnchangedCare(care, tile.qualityCare);
        }
        Check(field.Tile(0).growthDays == crop.TotalGrowthDays && field.Tile(0).regrowDaysRemaining == 0, "Final timing saved");
        Check(field.Tile(0).qualityCare.readyDay == day, "Ready today, no next-day grace offset");
        int changes = field.Changed;
        Check(!field.DebugAdvanceCropStage(0, 0, false) && !field.DebugAdvanceCropStage(0, 0, true), "Ready is idempotent");
        Check(field.Changed == changes && TimeManager.Instance.day == day, "No duplicate event or clock advance");
    }
    static void Main(string[] args)
    {
        // Exercise the current asset schedules too: five slots and legacy six slots.
        int profiles = 0;
        foreach (string asset in Directory.GetFiles(args[0], "* Crop.asset"))
        {
            string yaml = File.ReadAllText(asset);
            int days = int.Parse(Regex.Match(yaml, @"daysUntilFirstHarvest: (\d+)").Groups[1].Value);
            int[] schedule = Regex.Matches(yaml, @"startsOnDay: (\d+)")
                .Select(m => int.Parse(m.Groups[1].Value)).ToArray();
            var crop = Crop(days, schedule); crop.name = Path.GetFileNameWithoutExtension(asset);
            string[] blocks = Regex.Split(yaml, @"(?=  - stageName: )").Skip(1).ToArray();
            for (int i = 0; i < blocks.Length; i++)
            {
                var depth = Regex.Match(blocks[i], @"soilBurialDepth: ([\d.eE+-]+)");
                crop.visualStages[i].soilBurialDepth = depth.Success ? float.Parse(depth.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0f;
                var offset = Regex.Match(blocks[i], @"localOffset: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+), z: ([\d.eE+-]+)\}");
                if (offset.Success) crop.visualStages[i].localOffset = new Vector3(
                    float.Parse(offset.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(offset.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(offset.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                bool rootCrop = crop.name == "Onion Crop" || crop.name == "Radish Crop";
                if (rootCrop && i >= 3)
                {
                    bool onion = crop.name == "Onion Crop";
                    float scale = i == 3 ? (onion ? .54f : .625f) : 1f;
                    float low = (onion ? .07f : .075f) * scale, high = (onion ? .47f : .425f) * scale;
                    float soil = crop.visualStages[i].localOffset.y - crop.GetStageOffset(i).y;
                    Check(Math.Abs((high - soil) / (high - low) - .5f) < .0001f, "Actual field placement exposes upper bulb half: " + crop.name + " " + i);
                    Check(low - soil < 0 && high - soil > 0, "Bulb crosses soil plane while top stays visible");
                }
                else Check(Math.Abs(crop.GetStageOffset(i).y - crop.visualStages[i].localOffset.y) < .0001f, "Other crops and early roots retain placement");
            }
            Check(schedule.Length >= 2, "Authored schedule present");
            WalkAllStages(crop); profiles++;
            var tile = Plant(crop);
            Check(CropStageDebug.TryAdvance(ref tile, true, 17), "Direct final for " + crop.name);
            Check(tile.growthStage == crop.StageCount - 1 && new FieldTileSnapshot(tile).IsHarvestReady,
                "Final is harvestable for " + crop.name);
        }
        Check(profiles >= 26, "All 26 integrated crops exercised");
        foreach (int days in new[] { 1, 5, 7, 31, 10000 })
            WalkAllStages(Crop(days, 0, 1, 7, 13, 29));

        var five = Crop(5, 0, 1, 2, 3, 4);
        var six = Crop(19, 0, 1, 3, 7, 11, 17);
        var a = Plant(five); a.growthStage = 2; a.growthDays = 2.5f;
        var b = Plant(six); b.growthStage = 4; b.growthDays = 13;
        var mixed = new FieldArea(a, b, new FieldTileData());
        Check(mixed.DebugAdvanceCropStage(0, 0, false) && mixed.DebugAdvanceCropStage(1, 0, false), "Mixed field handled independently");
        Check(mixed.Tile(0).growthStage == 3 && mixed.Tile(0).cropState == CropLifecycleState.Growing,
            "Five-stage crop advances from its own progress");
        Check(mixed.Tile(1).growthStage == 5 && mixed.Tile(1).cropState == CropLifecycleState.HarvestReady,
            "Six-stage crop advances from its own progress");
        Check(!mixed.DebugAdvanceCropStage(2, 0, true) && !mixed.Tile(2).dirty, "Empty tile unchanged");

        foreach (var state in new[] { CropLifecycleState.Dead, CropLifecycleState.Withered, CropLifecycleState.HarvestReady })
        {
            var tile = Plant(five); tile.cropState = state;
            var field = new FieldArea(tile);
            Check(!field.DebugAdvanceCropStage(0, 0, false) && !field.DebugAdvanceCropStage(0, 0, true), "Skip " + state);
            Check(field.Changed == 0 && !field.Tile(0).dirty, "Skipped state has no side effects");
        }
        foreach (TileState state in Enum.GetValues(typeof(TileState)))
        {
            if (state == TileState.Planted) continue;
            var tile = Plant(five); tile.state = state;
            Check(!CropStageDebug.TryAdvance(ref tile, true, 17), "Never change non-planted " + state);
        }
        var blocked = new FieldArea(Plant(five));
        Check(!blocked.DebugAdvanceCropStage(-1, 0, true) && !blocked.DebugAdvanceCropStage(0, -1, true) &&
            !blocked.DebugAdvanceCropStage(1, 0, true), "Out of bounds safe");
        blocked.IsSoilLocked = true;
        Check(!blocked.DebugAdvanceCropStage(0, 0, true), "Restoring field locked");
        blocked.IsSoilLocked = false; PropertySite.DebugShortcutsEnabled = false;
        Check(!blocked.DebugAdvanceCropStage(0, 0, true), "Disabled in release build");
        PropertySite.DebugShortcutsEnabled = true;
        Check(blocked.Changed == 0 && blocked.Refreshed == 0, "Blocked operations never refresh");

        var regrow = Plant(five); regrow.growthDays = five.TotalGrowthDays;
        regrow.cropState = CropLifecycleState.Regrowing; regrow.growthStage = 1; regrow.regrowDaysRemaining = 3;
        var repeat = new FieldArea(regrow);
        for (int stage = 2; stage <= 4; stage++)
        {
            Check(repeat.DebugAdvanceCropStage(0, 0, false), "Regrow stage " + stage);
            var tile = repeat.Tile(0);
            Check(tile.growthStage == stage && tile.growthDays == five.TotalGrowthDays, "Regrow preserves first-harvest timing");
            Check(tile.regrowDaysRemaining == 4 - stage, "Regrow timer follows skip");
            Check(tile.cropState == (stage == 4 ? CropLifecycleState.HarvestReady : CropLifecycleState.Regrowing), "No premature regrow harvest");
        }
        var directRegrow = Plant(five); directRegrow.cropState = CropLifecycleState.Regrowing;
        directRegrow.regrowDaysRemaining = 3;
        Check(CropStageDebug.TryAdvance(ref directRegrow, true, 17) && directRegrow.regrowDaysRemaining == 0,
            "Direct harvest clears regrow timer");
        var single = Plant(Crop(8, 0)); single.qualityCare = null;
        Check(CropStageDebug.TryAdvance(ref single, false, 17) && single.cropState == CropLifecycleState.HarvestReady,
            "Single-stage crop can finish");
        Check(single.qualityCare.readyDay == 17 && single.qualityCare.Stars == 1, "Missing care gets no invented quality");
        var missing = Plant(Crop(5));
        Check(CropStageDebug.TryAdvance(ref missing, true, 17) && missing.growthStage == 0, "Missing visual slots still finish logically");
        var stale = Plant(five); stale.growthDays = 2.6f;
        Check(CropStageDebug.TryAdvance(ref stale, false, 17) && stale.growthStage == 3 && stale.growthDays >= 2.6f,
            "Stale saved visual cannot rewind growth");
        Console.WriteLine($"PASS {checks} crop debug checks; {profiles} authored crop schedules. Engine boundaries stubbed; no Play Mode.");
    }
}
