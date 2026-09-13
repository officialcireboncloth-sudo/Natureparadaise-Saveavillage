#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Import/export atomik untuk balance ikan. Item ikan tetap dikelola Items.csv.</summary>
public static class FishingCsvDatabase
{
    const string CsvPath = "Assets/Nature  Paradaise/Data/Balance/Fish.csv";
    const string NewAssetFolder = "Assets/Nature  Paradaise/Resources/Fishing/CSV";
    static readonly string[] Columns = {
        "fish_id","asset_path","item_id","rarity","water_types","seasons","weather",
        "start_hour","end_hour","required_rod_level","encounter_weight","minimum_bite_wait",
        "maximum_bite_wait","hook_window","catch_zone_size","fish_move_speed",
        "progress_gain_per_second","progress_loss_per_second","time_limit","minimum_size_cm","maximum_size_cm"
    };

    [MenuItem("Nature Paradise/Data CSV/Export Fish")]
    public static void Export()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath) ?? "Assets");
        StringBuilder csv = new();
        csv.AppendLine(string.Join(",", Columns));
        foreach (FishDefinitionSO fish in LoadFish().OrderBy(candidate => candidate.fishId, StringComparer.Ordinal))
        {
            string path = AssetDatabase.GetAssetPath(fish);
            string[] values = {
                fish.fishId,path,fish.item != null ? fish.item.Id : string.Empty,fish.rarity.ToString(),
                Flags(fish.waterTypes),Flags(fish.seasons),Flags(fish.weather),fish.startHour.ToString(),fish.endHour.ToString(),
                fish.requiredRodLevel.ToString(),F(fish.encounterWeight),F(fish.minimumBiteWait),F(fish.maximumBiteWait),
                F(fish.hookWindow),F(fish.catchZoneSize),F(fish.fishMoveSpeed),F(fish.progressGainPerSecond),
                F(fish.progressLossPerSecond),F(fish.timeLimit),F(fish.minimumSizeCm),F(fish.maximumSizeCm)
            };
            csv.AppendLine(string.Join(",", values.Select(Escape)));
        }
        File.WriteAllText(CsvPath, csv.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"[FISH CSV] Export selesai: {LoadFish().Length} ikan ke {CsvPath}");
    }

    [MenuItem("Nature Paradise/Data CSV/Validate Fish")]
    public static void ValidateMenu()
    {
        if (TryRead(out List<Row> rows, out List<string> errors))
            Debug.Log($"[FISH CSV] Valid: {rows.Count} ikan, seluruh ID dan reference item tersedia.");
        else LogErrors(errors);
    }

    [MenuItem("Nature Paradise/Data CSV/Import Fish")]
    public static void Import()
    {
        if (!TryRead(out List<Row> rows, out List<string> errors)) { LogErrors(errors); return; }
        Directory.CreateDirectory(NewAssetFolder);
        Dictionary<string, FishDefinitionSO> existing = LoadFish()
            .Where(fish => !string.IsNullOrWhiteSpace(fish.fishId))
            .GroupBy(fish => fish.fishId).ToDictionary(group => group.Key, group => group.First());
        int created = 0;
        int updated = 0;
        foreach (Row row in rows)
        {
            FishDefinitionSO fish = null;
            if (!string.IsNullOrWhiteSpace(row.path)) fish = AssetDatabase.LoadAssetAtPath<FishDefinitionSO>(row.path);
            if (fish == null) existing.TryGetValue(row.id, out fish);
            if (fish == null)
            {
                fish = ScriptableObject.CreateInstance<FishDefinitionSO>();
                string file = Sanitize(string.IsNullOrWhiteSpace(row.id) ? "Fish" : row.id) + ".asset";
                AssetDatabase.CreateAsset(fish, AssetDatabase.GenerateUniqueAssetPath($"{NewAssetFolder}/{file}"));
                created++;
            }
            else { Undo.RecordObject(fish, "Import Fish CSV"); updated++; }

            fish.fishId = row.id;
            fish.item = row.item;
            fish.rarity = row.rarity;
            fish.waterTypes = row.water;
            fish.seasons = row.seasons;
            fish.weather = row.weather;
            fish.startHour = row.startHour;
            fish.endHour = row.endHour;
            fish.requiredRodLevel = row.rodLevel;
            fish.encounterWeight = row.weight;
            fish.minimumBiteWait = row.minBite;
            fish.maximumBiteWait = row.maxBite;
            fish.hookWindow = row.hookWindow;
            fish.catchZoneSize = row.zone;
            fish.fishMoveSpeed = row.moveSpeed;
            fish.progressGainPerSecond = row.gain;
            fish.progressLossPerSecond = row.loss;
            fish.timeLimit = row.timeLimit;
            fish.minimumSizeCm = row.minSize;
            fish.maximumSizeCm = row.maxSize;
            EditorUtility.SetDirty(fish);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FISH CSV] Import selesai: {created} baru, {updated} diperbarui.");
    }

    [MenuItem("Nature Paradise/Fishing/Mark Selected Water as Fishing Spot")]
    static void MarkSelectedWater()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponent<Collider>() == null)
        {
            EditorUtility.DisplayDialog("Fishing Spot", "Pilih GameObject air yang memiliki Collider.", "OK");
            return;
        }
        FishingSpot spot = selected.GetComponent<FishingSpot>();
        if (spot == null) spot = Undo.AddComponent<FishingSpot>(selected);
        EditorGUIUtility.PingObject(spot);
        Selection.activeObject = spot;
        Debug.Log($"[FISHING] '{selected.name}' siap sebagai Fishing Spot. Atur Water Type dan pool melalui Inspector.");
    }

    [MenuItem("Nature Paradise/Fishing/Open Fishing Test Scene")]
    static void OpenFishingTestScene()
    {
        const string path = "Assets/Nature  Paradaise/Map/Scenes/Testing/FishingTestScene.unity";
        if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            FishingTestSceneBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<FishingTestSceneBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.EnsureLayout();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = bootstrap.gameObject;
                EditorGUIUtility.PingObject(bootstrap.gameObject);
            }
        }
    }

    [MenuItem("Nature Paradise/Fishing/Validate Setup")]
    static void ValidateFishingSetup()
    {
        FishDefinitionSO[] fish = LoadFish();
        ItemSO rod = LoadItems().FirstOrDefault(item => item.equippedTool == PlayerToolType.FishingRod);
        if (rod == null) Debug.LogError("[FISHING] Fishing Rod ItemSO tidak ditemukan.");
        if (fish.Length == 0) Debug.LogError("[FISHING] Belum ada FishDefinitionSO.");
        if (rod != null && fish.Length > 0)
            Debug.Log($"[FISHING] Setup data valid: rod '{rod.itemName}', {fish.Length} definisi ikan. Water bernama 'Water' dipetakan otomatis saat runtime.");
    }

    static bool TryRead(out List<Row> rows, out List<string> errors)
    {
        rows = new List<Row>();
        errors = new List<string>();
        if (!File.Exists(CsvPath)) { errors.Add($"File tidak ditemukan: {CsvPath}"); return false; }
        string[] lines = File.ReadAllLines(CsvPath);
        if (lines.Length == 0) { errors.Add("CSV kosong."); return false; }
        List<string> header = Split(lines[0]);
        foreach (string column in Columns) if (!header.Contains(column)) errors.Add($"Kolom wajib hilang: {column}");
        if (errors.Count > 0) return false;
        Dictionary<string, ItemSO> items = LoadItems().Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First());
        HashSet<string> ids = new(StringComparer.Ordinal);

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;
            List<string> cells = Split(lines[lineIndex]);
            Dictionary<string, string> source = new();
            for (int i = 0; i < header.Count; i++) source[header[i]] = i < cells.Count ? cells[i].Trim() : string.Empty;
            string label = $"Baris {lineIndex + 1}";
            Row row = new() { id = Required(source, "fish_id", label, errors), path = source["asset_path"] };
            if (!ids.Add(row.id)) errors.Add($"{label}: fish_id duplikat '{row.id}'.");
            string itemId = Required(source, "item_id", label, errors);
            if (!items.TryGetValue(itemId, out row.item)) errors.Add($"{label}: item_id '{itemId}' tidak ditemukan.");
            else if (row.item.category != ItemCategory.Fish) errors.Add($"{label}: item_id '{itemId}' bukan category Fish.");
            ParseEnum(source,"rarity",label,errors,out row.rarity);
            ParseFlags(source,"water_types",label,errors,out row.water);
            ParseFlags(source,"seasons",label,errors,out row.seasons);
            ParseFlags(source,"weather",label,errors,out row.weather);
            row.startHour = I(source,"start_hour",label,errors,0,23);
            row.endHour = I(source,"end_hour",label,errors,0,24);
            row.rodLevel = I(source,"required_rod_level",label,errors,1,99);
            row.weight = N(source,"encounter_weight",label,errors,0.01f,10000f);
            row.minBite = N(source,"minimum_bite_wait",label,errors,0.1f,3600f);
            row.maxBite = N(source,"maximum_bite_wait",label,errors,row.minBite,3600f);
            row.hookWindow = N(source,"hook_window",label,errors,0.2f,30f);
            row.zone = N(source,"catch_zone_size",label,errors,0.08f,0.8f);
            row.moveSpeed = N(source,"fish_move_speed",label,errors,0.05f,10f);
            row.gain = N(source,"progress_gain_per_second",label,errors,0.01f,10f);
            row.loss = N(source,"progress_loss_per_second",label,errors,0f,10f);
            row.timeLimit = N(source,"time_limit",label,errors,3f,600f);
            row.minSize = N(source,"minimum_size_cm",label,errors,0.1f,10000f);
            row.maxSize = N(source,"maximum_size_cm",label,errors,row.minSize,10000f);
            rows.Add(row);
        }
        return errors.Count == 0;
    }

    static T[] FindAssets<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}")
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToArray();
    static FishDefinitionSO[] LoadFish() => FindAssets<FishDefinitionSO>();
    static ItemSO[] LoadItems() => FindAssets<ItemSO>();
    static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    static string Flags<T>(T value) where T : Enum => value.ToString().Replace(", ", "|");
    static string Required(Dictionary<string,string> row,string key,string label,List<string> errors) { string value=row[key]; if(string.IsNullOrWhiteSpace(value)) errors.Add($"{label}: '{key}' wajib diisi."); return value; }
    static int I(Dictionary<string,string> row,string key,string label,List<string> errors,int min,int max) { if(!int.TryParse(row[key],NumberStyles.Integer,CultureInfo.InvariantCulture,out int value)||value<min||value>max) errors.Add($"{label}: '{key}' harus {min}..{max}."); return Mathf.Clamp(value,min,max); }
    static float N(Dictionary<string,string> row,string key,string label,List<string> errors,float min,float max) { if(!float.TryParse(row[key],NumberStyles.Float,CultureInfo.InvariantCulture,out float value)||value<min||value>max) errors.Add($"{label}: '{key}' harus {min}..{max}."); return Mathf.Clamp(value,min,max); }
    static void ParseEnum<T>(Dictionary<string,string> row,string key,string label,List<string> errors,out T value) where T:struct,Enum { if(!Enum.TryParse(row[key],true,out value)) errors.Add($"{label}: enum '{key}' tidak dikenal: {row[key]}"); }
    static void ParseFlags<T>(Dictionary<string,string> row,string key,string label,List<string> errors,out T value) where T:struct,Enum { string text=row[key].Replace('|',','); if(!Enum.TryParse(text,true,out value)) errors.Add($"{label}: flags '{key}' tidak dikenal: {row[key]}"); }
    static string Escape(string value) { value ??= string.Empty; return value.IndexOfAny(new[]{',','"','\n','\r'}) >= 0 ? $"\"{value.Replace("\"","\"\"")}\"" : value; }
    static List<string> Split(string line) { List<string> result=new(); StringBuilder cell=new(); bool quoted=false; for(int i=0;i<line.Length;i++){char c=line[i]; if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){result.Add(cell.ToString());cell.Clear();}else cell.Append(c);} result.Add(cell.ToString()); return result; }
    static string Sanitize(string value) { foreach(char invalid in Path.GetInvalidFileNameChars()) value=value.Replace(invalid,'_'); return value.Replace('.','_'); }
    static void LogErrors(List<string> errors) => Debug.LogError("[FISH CSV] Import dibatalkan.\n- "+string.Join("\n- ",errors));

    sealed class Row
    {
        public string id,path; public ItemSO item; public FishRarity rarity; public FishingWaterMask water; public CropSeason seasons; public FishingWeatherMask weather;
        public int startHour,endHour,rodLevel; public float weight,minBite,maxBite,hookWindow,zone,moveSpeed,gain,loss,timeLimit,minSize,maxSize;
    }
}
#endif
