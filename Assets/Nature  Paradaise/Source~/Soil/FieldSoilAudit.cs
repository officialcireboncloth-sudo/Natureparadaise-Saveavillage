#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Run in the isolated CleanClone project only; never touches player save files.</summary>
public static class FieldSoilAudit
{
    [Serializable] sealed class Bundle { public List<FieldSaveData> fields; }
    static int checks;
    static StringBuilder report;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static void Invoke(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
        report.AppendLine("PASS " + message);
    }
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/CleanClone/")) throw new Exception("Isolated fixture only");
        report = new StringBuilder("FIELD SOIL — NATIVE UNITY COMPONENT, TRANSACTION AND JSON AUDIT\n");
        checks = 0;
        string output = "ArtSource/SoilAudit";
        Directory.CreateDirectory(output);
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Check(Object.FindFirstObjectByType<SaveManager>() == null, "No production save writer present");
            var rules = Resources.Load<FieldSoilRulesSO>("Profiles/Farming/Field Soil Rules");
            Check(rules != null && rules.initialFertility == 80, "Shared rules asset linked");
            Check(rules.GetCondition(19) == FieldSoilCondition.Poor && rules.GetCondition(20) == FieldSoilCondition.Medium
                && rules.GetCondition(49) == FieldSoilCondition.Medium && rules.GetCondition(50) == FieldSoilCondition.Good, "Condition boundaries");
            Check(rules.GetQualityContribution(10) == 10 && rules.GetQualityContribution(25) == 25 && rules.GetQualityContribution(80) == 40, "10/25/40 quality contribution");
            var inventory = new GameObject("Inventory fixture").AddComponent<Inventory>();
            Invoke(inventory, "Awake");
            FieldArea Create(int number, int columns, int rows)
            {
                var go = new GameObject("Field " + number);
                go.transform.position = new Vector3(number * 20, 0, 0);
                go.AddComponent<BoxCollider>();
                var field = go.AddComponent<FieldArea>();
                Set(field, "fieldNumber", number); Set(field, "columns", columns); Set(field, "rows", rows);
                Set(field, "fieldSoilRules", rules);
                Invoke(field, "Awake");
                return field;
            }
            var first = Create(1, 2, 3);
            var second = Create(2, 3, 2);
            var third = Create(3, 1, 1);
            var fourth = Create(4, 2, 2);
            var fields = new List<FieldArea>(); SoilScannerService.GetFields(fields);
            Check(fields.Count == 4 && fields.Select(f => f.FieldNumber).SequenceEqual(new[] {1,2,3,4}), "Scanner discovers and sorts exactly four independent field instances");
            Check(fields.Select(f => f.FieldId).Distinct().Count() == 4 && first.FieldId == "player-home-field", "Unique numbered save identities preserve original Field 1 key");
            Check(first.Columns == 2 && first.Rows == 3 && second.Columns == 3 && second.Rows == 2, "Manual rectangular grid sizes preserved");
            Check(first.GetComponent<BoxCollider>().isTrigger, "Grid volume stays non-physical");
            first.DebugSetFieldFertility(25); second.DebugSetFieldFertility(10);
            Check(SoilScannerService.TryScan(1, out var scanned, out _) && scanned.Fertility == 25 && scanned.DisplayName == "Field 1", "Scan reads selected instance, not global tile average");
            Check(second.SoilSnapshot.Fertility == 10 && third.SoilSnapshot.Fertility == 80, "Changing one field does not change the other fields");
            foreach (var entry in new[] {("Terra Natural",30), ("Terra Plus",20), ("Terra Ultra",10), ("Terra Miracle",3)})
            {
                var item = Resources.Load<ItemSO>("Items/Farming/Terra/" + entry.Item1);
                Check(item != null && item.IsSoilRestorer && item.soilRestorationDays == entry.Item2 && !item.IsFertilizer && !item.IsCropBooster,
                    entry.Item1 + " has correct duration and is distinct from crop fertilizer");
                first.DebugSetFieldFertility(25);
                Check(!first.TryBeginSoilRestoration(inventory, item, out _) && !first.IsSoilLocked, "Missing item cannot lock field");
                inventory.Add(item, 2);
                bool nestedAccepted = false;
                Action reentrant = () => nestedAccepted |= first.TryBeginSoilRestoration(inventory, item, out _);
                inventory.OnInventoryChanged += reentrant;
                Check(SoilScannerService.TryUse(1, inventory, item, out _), "Terra starts on selected field");
                inventory.OnInventoryChanged -= reentrant;
                Check(!nestedAccepted && inventory.GetCount(item) == 1, "Exactly one item consumed despite repeated/reentrant input");
                Check(first.IsSoilLocked && first.SoilSnapshot.RemainingDays == entry.Item2 && !second.IsSoilLocked, "Only selected field locked for full duration");
                Check(!first.TryBeginSoilRestoration(inventory, item, out _) && inventory.GetCount(item) == 1, "Repeated use cannot consume another item");
                Check(!first.TryHoe(0,0) && !first.TryWater(0,0) && !first.TryApplyEffect(0,0,new FieldEffect(FieldEffectType.Moisture,10))
                    && !first.CanPlaceFarmItem(0,0) && !first.CanReceiveSprinkler(0,0)
                    && !first.ValidateBuildingFootprint(0,0,1,1,null,out _), "Locked field rejects farming, watering, placement and building");
                Check(second.TryHoe(0,0) || second.TryGetSnapshot(0,0,out var existing) && existing.State == TileState.Hoed, "Other field still usable");
                var snapshot = first.CaptureSaveData();
                string json = JsonUtility.ToJson(snapshot);
                first.RestoreSaveData(JsonUtility.FromJson<FieldSaveData>(json));
                Check(first.IsSoilLocked && first.SoilSnapshot.RemainingDays == entry.Item2, "Real Unity JSON roundtrip preserves restoration");
                int day = snapshot.soil.lastProcessedDay;
                first.AdvanceFieldSoilToDay(day);
                Check(first.SoilSnapshot.RemainingDays == entry.Item2, "Reload/same day never consumes another restoration day");
                first.AdvanceFieldSoilToDay(day + entry.Item2 - 1);
                Check(first.IsSoilLocked && first.SoilSnapshot.RemainingDays == 1 && first.SoilSnapshot.Fertility == 25, "No early unlock or early fertility reward");
                first.AdvanceFieldSoilToDay(day + entry.Item2);
                Check(!first.IsSoilLocked && first.SoilSnapshot.Fertility == 80 && first.CanHoe(0,0), "Restores to exactly 80 and unlocks at deadline");
                Check(!first.TryBeginSoilRestoration(inventory,item,out _) && inventory.GetCount(item) == 1, "Full soil cannot waste Terra");
                inventory.Remove(item,1);
                snapshot.soil.fertility = 1;
                Check(first.SoilSnapshot.Fertility == 80, "Save snapshot cannot mutate live soil");
            }
            var crop = ScriptableObject.CreateInstance<CropDataSO>(); crop.cropId = "audit.crop"; crop.daysUntilFirstHarvest = 1;
            Set(first, "defaultCrop", crop);
            first.DebugSetFieldFertility(25); first.TryHoe(0,0);
            Check(first.TryPlant(0,0,crop), "Can plant on medium soil");
            var ultra = Resources.Load<ItemSO>("Items/Farming/Terra/Terra Ultra"); inventory.Add(ultra,1);
            Check(!first.TryBeginSoilRestoration(inventory,ultra,out _) && inventory.GetCount(ultra) == 1, "Occupied crop blocks restoration without consuming item");
            Check(!first.TryFertilize(0,0,5) && first.SoilSnapshot.Fertility == 25, "Legacy instant fertilizer cannot bypass whole-field restoration");
            Check(first.TryApplyCropBooster(0,0,3) && first.SoilSnapshot.Fertility == 25, "Crop fertilizer remains independent from field soil");
            first.TryWater(0,0);
            first.AdvanceFieldSoilToDay(first.CaptureSaveData().soil.lastProcessedDay + 1);
            Check(first.SoilSnapshot.Fertility == 24, "Occupied field loses one point per day, not one per tile");
            first.DebugSetFieldFertility(0); first.TryHoe(1,0);
            Check(first.TryPlant(1,0,crop), "Poor zero-fertility soil affects quality without adding an undeclared planting ban");
            first.DebugSetFieldFertility(60); first.TryConsumeCrop(0,0); first.TryConsumeCrop(1,0);
            var bundle = new Bundle { fields = FieldArea.CaptureAll() };
            string saved = JsonUtility.ToJson(bundle);
            first.DebugSetFieldFertility(1); second.DebugSetFieldFertility(1);
            FieldArea.RestoreAll(JsonUtility.FromJson<Bundle>(saved).fields);
            Check(first.SoilSnapshot.Fertility == 60 && second.SoilSnapshot.Fertility == 10, "All fields restored by ID, independently of iteration order");
            second.TryHoe(1,0);
            FieldArea.RestoreAll(new List<FieldSaveData> { first.CaptureSaveData() });
            Check(second.SoilSnapshot.Fertility == 80 && !second.IsSoilLocked && second.CanHoe(1,0),
                "Field absent from older slot resets instead of inheriting another slot's soil and tiles");
            var oldSave = new FieldSaveData { fieldId = fourth.FieldId, columns = 2, rows = 2 };
            oldSave.tiles.Add(new FieldTileSaveData { x=0,z=0,hasSoilDurability=true,soilDurability=0,state=TileState.Hoed });
            fourth.RestoreSaveData(oldSave);
            Check(fourth.SoilSnapshot.Fertility == 60 && fourth.TryGetSnapshot(0,0,out var migrated) && migrated.State == TileState.Hoed,
                "Old sparse save migrates average once without moving tiles");
            var damaged = fourth.CaptureSaveData(); damaged.soil.fertility = 999; damaged.soil.remainingDays = -4;
            fourth.RestoreSaveData(damaged);
            Check(fourth.SoilSnapshot.Fertility == 80 && !fourth.IsSoilLocked, "Malformed soil values clamped safely");
            int soilBefore = second.SoilSnapshot.Fertility;
            second.AdvanceFieldSoilToDay(second.CaptureSaveData().soil.lastProcessedDay + 10);
            Check(second.SoilSnapshot.Fertility == soilBefore, "Unused field does not lose fertility or auto-restore");
            var duplicate = Create(2,1,1);
            Check(!FieldArea.ValidateSoilIdentities(out _) && !SoilScannerService.TryScan(2,out _,out _), "Duplicate IDs produce no ambiguous scan");
            bool blocked = false; try { FieldArea.CaptureAll(); } catch (InvalidOperationException) { blocked = true; }
            Check(blocked, "Duplicate IDs cannot silently corrupt a save");
            Invoke(duplicate,"OnDisable"); Object.DestroyImmediate(duplicate.gameObject);
            Check(FieldArea.ValidateSoilIdentities(out _), "Fixing duplicate restores normal access");
            // Actual TimeManager daily event path, including a sleep-style boundary.
            foreach (var field in fields) Invoke(field,"OnEnable");
            var clock = new GameObject("Clock fixture").AddComponent<TimeManager>(); Invoke(clock,"Awake");
            int current = fields.Max(f=>f.CaptureSaveData().soil.lastProcessedDay); clock.day = current;
            third.DebugSetFieldFertility(10); inventory.Add(ultra,1);
            Check(third.TryBeginSoilRestoration(inventory,ultra,out _), "Normal calendar test starts Terra");
            clock.AdvanceToNextDay();
            Check(third.SoilSnapshot.RemainingDays == 9, "Actual sleep/calendar event advances restoration exactly once");
            int healthyFive = 0, poorFive = 0;
            for (int i=0;i<1000;i++)
            {
                CropQualityCare Care() => new CropQualityCare { days=5,wateredDays=5,boostedDays=5,bestBooster=3,minimumBooster=3,readyDay=1,roll=i/1000f };
                var care = Care();
                if(care.RollStars(crop,true,1,.4f)==5)healthyFive++;
                if(Care().RollStars(crop,false,1,.1f)==5)poorFive++;
                Check(care.RollStars(crop,true,1,.4f)==care.RollStars(crop,true,1,.4f), "Quality roll stable at sample " + i);
            }
            Check(healthyFive > 0 && healthyFive < 1000 && poorFive == 0, "Good soil improves eligible quality without guaranteeing five stars");
            var noCare = new CropQualityCare { days=5,wateredDays=4,roll=.999f };
            Check(noCare.RollStars(crop,true,1,.4f)==1, "Full soil does not replace watering and crop care");
            Check(Object.FindFirstObjectByType<SaveManager>() == null, "No player save writer created during audit");
            report.AppendLine("SOIL_AUDIT_PASS checks="+checks);
            File.WriteAllText(output+"/Audit.txt",report.ToString());
            EditorApplication.Exit(0);
        }
        catch(Exception error)
        {
            File.WriteAllText(output+"/Error.txt", report+"\n"+error);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
