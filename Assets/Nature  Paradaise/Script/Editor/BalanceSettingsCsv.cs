using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>CSV settings for existing authored data. Never edits runtime instances or visual references.</summary>
public static class BalanceSettingsCsv
{
    public const string Root = "Assets/Nature  Paradaise/Data/Balance/Settings";
    static readonly Dictionary<string,string> Groups = new()
    {
        {"AnimalGrowthProfileSO","Animals"},{"AnimalCareCatalog","Animals"},{"AnimalManureSettings","Animals"},
        {"ShopCatalogSO","Shops"},{"BlacksmithCatalog","ToolUpgrades"},{"TreeDefinition","Trees"},
        {"FeedMakerCatalog","FeedMaker"},{"BuildingDefinitionSO","Buildings"},
        {"QuestDefinitionSO","Quests"},{"ProgressionRequirementSettings","Progression"},
        {"FarmEquipmentCatalog","Equipment"},{"FertilizerCatalog","Fertilizer"},
        {"WorldGatherable","Gatherables"},{"WildGrassBalance","Gatherables"},{"AnimalController","Animals"},{"AnimalGrowthSystem","Animals"},{"WorldTree","Trees"}
    };
    static readonly string[] Columns={"asset_guid","object_id","asset_name","asset_path","setting","label","value","type","minimum","maximum","description"};
    sealed class Cell
    {
        public Object target; public string path, key, group, description;
        public double min=double.NegativeInfinity,max=double.PositiveInfinity;
    }
    public static string LastReport="Belum ada pemeriksaan. Export untuk mengambil data terbaru.";

    [MenuItem("Nature Paradise/Data CSV/Settings/Export Current Settings")]
    public static void Export()
    {
        RequireEditMode();Directory.CreateDirectory(Root);
        foreach(var group in Cells().GroupBy(c=>c.group))
        {
            var rows=group.OrderBy(c=>c.key,StringComparer.Ordinal).Select(c=>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c.target,out string guid,out long id);
                var p=new SerializedObject(c.target).FindProperty(c.path);
                return new[]{guid,id.ToString(CultureInfo.InvariantCulture),c.target.name,AssetDatabase.GetAssetPath(c.target),c.path,p.displayName,
                    Value(p),p.propertyType.ToString(),Bound(c.min),Bound(c.max),c.description};
            });
            BalanceCsvDatabase.Csv.Write(Root+"/"+group.Key+".csv",Columns,rows);
        }
        AssetDatabase.Refresh();LastReport="Export selesai. Edit hanya kolom value, lalu Preview. File: "+Root;
        Debug.Log("[BALANCE CSV] "+LastReport);
    }
    static string Bound(double d)=>double.IsInfinity(d)?"":d.ToString("R",CultureInfo.InvariantCulture);
    static string Value(SerializedProperty p)=>p.propertyType switch
    {
        SerializedPropertyType.Integer=>p.longValue.ToString(CultureInfo.InvariantCulture),
        SerializedPropertyType.Float=>p.type=="double"?p.doubleValue.ToString("R",CultureInfo.InvariantCulture):p.floatValue.ToString("R",CultureInfo.InvariantCulture),
        _=>p.boolValue?"TRUE":"FALSE"
    };
    static void RequireEditMode(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Keluar dari Play Mode sebelum export/import balance.");}
    static IEnumerable<Cell> Cells()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/Nature  Paradaise/Resources"}))
        {
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
            if(asset!=null && Groups.ContainsKey(asset.GetType().Name))foreach(var c in Cells(asset))yield return c;
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Nature  Paradaise/Resources","Assets/Nature  Paradaise/Prefabs"}))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null && (c is WorldGatherable || c is AnimalController || c is AnimalGrowthSystem || c is WorldTree)))
                foreach(var c in Cells(component))yield return c;
        }
    }
    static IEnumerable<Cell> Cells(Object target)
    {
        var so=new SerializedObject(target);var p=so.GetIterator();
        while(p.Next(true))
        {
            if(p.propertyPath.StartsWith("m_") || p.name=="size" ||
                p.propertyType is not (SerializedPropertyType.Integer or SerializedPropertyType.Float or SerializedPropertyType.Boolean))continue;
            FieldInfo field=ResolveField(target.GetType(),p.propertyPath);
            if(field==null || field.GetCustomAttribute<HideInInspector>()!=null)continue;
            if(target is AnimalController && p.name!="productAmount" && p.name!="shearingAmount" && p.name!="maxHunger" && p.name!="cabbageHunger" && p.name!="milkHungerCost" && p.name!="milkProductionTime")continue;
            if(target is AnimalGrowthSystem && !p.propertyPath.StartsWith("healthRules.") && p.name!="dailyFullnessLoss")continue;
            if(target is WorldTree && !new[]{"minimumAxeLevel","standingDurability","stumpDurability","damagePerHit","standingWoodMinimum","standingWoodMaximum","stumpWoodMinimum","stumpWoodMaximum","canRespawn","minimumRespawnDays","maximumRespawnDays"}.Contains(p.name))continue;
            if(p.propertyPath.Contains("scale.") || p.propertyPath.Contains("Offset.") || p.propertyPath.Contains("EulerAngles.") || p.propertyPath.Contains("promptHeight"))continue;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target,out string guid,out long id);
            var c=new Cell{target=target,path=p.propertyPath,key=guid+":"+id+":"+p.propertyPath,group=Groups[target.GetType().Name]};
            var min=field.GetCustomAttribute<MinAttribute>();var range=field.GetCustomAttribute<RangeAttribute>();
            if(min!=null)c.min=min.min;if(range!=null){c.min=range.min;c.max=range.max;}
            if(p.propertyPath.StartsWith("healthRules."))
            {
                c.min=0;
                if(p.name.IndexOf("chance",StringComparison.OrdinalIgnoreCase)>=0)c.max=1;
                if(p.name=="nightRiskStartsAtHour")c.max=23;
            }
            var tooltip=field.GetCustomAttribute<TooltipAttribute>();
            c.description=tooltip?.tooltip ?? Explain(p.name);
            // Supply item/offer context for list entries, while retaining their structural keys.
            string parent=p.propertyPath.Contains('.')?p.propertyPath.Substring(0,p.propertyPath.LastIndexOf('.')):"";
            var owner=string.IsNullOrEmpty(parent)?null:so.FindProperty(parent);
            var item=owner?.FindPropertyRelative("item") ?? owner?.FindPropertyRelative("input");
            if(item?.propertyType==SerializedPropertyType.ObjectReference && item.objectReferenceValue!=null)c.description+=" Item: "+item.objectReferenceValue.name+".";
            var entryName=owner?.FindPropertyRelative("displayName");
            if(entryName?.propertyType==SerializedPropertyType.String && !string.IsNullOrEmpty(entryName.stringValue))c.description+=" Entri: "+entryName.stringValue+".";
            yield return c;
        }
    }
    static FieldInfo ResolveField(Type type,string path)
    {
        FieldInfo field=null;
        foreach(string part in path.Replace(".Array.data[","[").Split('.'))
        {
            string name=part.Split('[')[0];field=null;
            for(Type t=type;t!=null && field==null;t=t.BaseType)field=t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if(field==null)return null;type=field.FieldType;
            if(part.Contains('['))type=type.IsArray?type.GetElementType():type.GetGenericArguments().FirstOrDefault();
            if(type==null)return null;
        }
        return field;
    }
    static string Explain(string name)
    {
        if(name.IndexOf("chance",StringComparison.OrdinalIgnoreCase)>=0)return "Peluang 0..1; 0.25 = 25%.";
        if(name.IndexOf("price",StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf("gold",StringComparison.OrdinalIgnoreCase)>=0)return "Harga/biaya dalam Gold; bukan Rupiah. Harga override -1 mengikuti harga item.";
        if(name.IndexOf("days",StringComparison.OrdinalIgnoreCase)>=0)return "Durasi dalam hari game.";
        if(name.IndexOf("hours",StringComparison.OrdinalIgnoreCase)>=0)return "Durasi dalam jam game.";
        if(name.IndexOf("amount",StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf("count",StringComparison.OrdinalIgnoreCase)>=0)return "Jumlah unit item/hasil untuk entri ini.";
        if(name.IndexOf("capacity",StringComparison.OrdinalIgnoreCase)>=0)return "Kapasitas slot/unit sesuai jenis bangunan.";
        if(name.IndexOf("durability",StringComparison.OrdinalIgnoreCase)>=0)return "Ketahanan resource sebelum habis; berkurang saat dipukul.";
        if(name.IndexOf("Wood",StringComparison.OrdinalIgnoreCase)>=0)return "Batas jumlah Wood yang dijatuhkan batang/tunggul.";
        return "Pengaturan "+ObjectNames.NicifyVariableName(name)+" pada data game. Nilai awal berasal dari asset, bukan contoh. Lihat Inspector untuk konteks entri.";
    }
    static bool Prepare(out List<(Cell cell,string value)> changes,out List<string> errors)
    {
        changes=new();errors=new();var cells=Cells().ToDictionary(c=>c.key);var seen=new HashSet<string>();
        if(!Directory.Exists(Root)){errors.Add("Belum ada CSV. Jalankan Export Current Settings dahulu.");return false;}
        string[] files=Directory.GetFiles(Root,"*.csv");if(files.Length==0)errors.Add("Tidak ada file CSV.");
        foreach(string file in files)
        {
            List<Dictionary<string,string>> rows;
            try{rows=BalanceCsvDatabase.Csv.Read(file,out var headers,out var error);
                if(error!=null){errors.Add(file+": "+error);continue;}
                if(headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=headers.Length || Columns.Any(col=>!headers.Contains(col,StringComparer.OrdinalIgnoreCase))){errors.Add(file+": header hilang/duplikat.");continue;}}
            catch(Exception ex){errors.Add(file+": "+ex.Message);continue;}
            int line=1;
            foreach(var row in rows)
            {
                line++;string key=row["asset_guid"]+":"+row["object_id"]+":"+row["setting"];
                if(!seen.Add(key)){errors.Add(file+":"+line+" setting duplikat.");continue;}
                if(!cells.TryGetValue(key,out var cell)){errors.Add(file+":"+line+" asset/setting tidak ditemukan. Export ulang setelah struktur Inspector berubah.");continue;}
                var p=new SerializedObject(cell.target).FindProperty(cell.path);string value=row["value"].Trim();
                if(!Valid(cell,p,value,out string reason)){errors.Add(file+":"+line+" "+cell.path+": "+reason);continue;}
                if(Value(p)!=value)changes.Add((cell,value));
            }
        }
        // Pair validation uses the proposed values, including unchanged partners.
        var proposed=changes.ToDictionary(x=>x.cell.key,x=>x.value);
        foreach(var c in cells.Values)
        {
            string partner=c.path.Replace("minimum","maximum").Replace("Minimum","Maximum").Replace("defaultAmount","maximumAmount");
            if(partner==c.path)continue;string other=c.key.Substring(0,c.key.Length-c.path.Length)+partner;
            if(!cells.TryGetValue(other,out var maxCell))continue;
            double a=Number(c,proposed),b=Number(maxCell,proposed);
            if(a>b)errors.Add(c.target.name+": "+c.path+" harus <= "+partner);
        }
        return errors.Count==0;
    }
    static double Number(Cell c,Dictionary<string,string> proposed)=>double.Parse(proposed.TryGetValue(c.key,out string value)?value:Value(new SerializedObject(c.target).FindProperty(c.path)),CultureInfo.InvariantCulture);
    static bool Valid(Cell c,SerializedProperty p,string value,out string error)
    {
        error=null;
        if(p.propertyType==SerializedPropertyType.Boolean){if(bool.TryParse(value,out _))return true;error="Gunakan TRUE/FALSE.";return false;}
        if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out double n)||double.IsNaN(n)||double.IsInfinity(n)){error="Angka tidak valid; desimal memakai titik.";return false;}
        if(p.propertyType==SerializedPropertyType.Integer && (!int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out _))){error="Harus angka bulat 32-bit.";return false;}
        if(p.propertyType==SerializedPropertyType.Float && Math.Abs(n)>float.MaxValue){error="Angka terlalu besar.";return false;}
        if(n<c.min||n>c.max){error="Di luar batas Inspector: "+Bound(c.min)+" .. "+Bound(c.max);return false;}
        if(c.path.IndexOf("chance",StringComparison.OrdinalIgnoreCase)>=0 && (n<0||n>1)){error="Peluang harus 0..1.";return false;}
        return true;
    }
    [MenuItem("Nature Paradise/Data CSV/Settings/Preview Changes")]
    public static void PreviewMenu()=>Preview();
    public static bool Preview()
    {
        RequireEditMode();bool ok=Prepare(out var changes,out var errors);
        LastReport=ok?"VALID — "+changes.Count+" perubahan.\n"+string.Join("\n",changes.Take(100).Select(x=>x.cell.target.name+" / "+x.cell.path+": "+Value(new SerializedObject(x.cell.target).FindProperty(x.cell.path))+" → "+x.value)):
            "IMPORT DIBATALKAN\n"+string.Join("\n",errors);
        File.WriteAllText("Assets/Nature  Paradaise/Data/Balance/SettingsPreview.txt",LastReport);
        if(ok)Debug.Log("[BALANCE CSV] "+LastReport);else Debug.LogWarning("[BALANCE CSV] "+LastReport);return ok;
    }
    [MenuItem("Nature Paradise/Data CSV/Settings/Import Validated Settings")]
    public static void Import()
    {
        RequireEditMode();if(!Prepare(out var changes,out var errors)){LastReport=string.Join("\n",errors);Debug.LogError(LastReport);return;}
        if(changes.Count==0){LastReport="Tidak ada perubahan.";return;}
        string backup="BalanceBackups/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
        foreach(string asset in changes.Select(x=>AssetDatabase.GetAssetPath(x.cell.target)).Distinct())
        {string dest=Path.Combine(backup,asset);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(asset,dest);File.Copy(asset+".meta",dest+".meta");}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Import Balance Settings CSV");
        try
        {
            foreach(var target in changes.GroupBy(x=>x.cell.target))
            {
                Undo.RecordObject(target.Key,"Import Balance Settings CSV");var so=new SerializedObject(target.Key);
                foreach(var change in target){var p=so.FindProperty(change.cell.path);if(p.propertyType==SerializedPropertyType.Integer)p.intValue=int.Parse(change.value,CultureInfo.InvariantCulture);
                    else if(p.propertyType==SerializedPropertyType.Float){if(p.type=="double")p.doubleValue=double.Parse(change.value,CultureInfo.InvariantCulture);else p.floatValue=float.Parse(change.value,CultureInfo.InvariantCulture);}else p.boolValue=bool.Parse(change.value);}
                so.ApplyModifiedProperties();EditorUtility.SetDirty(target.Key);
            }
            AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);LastReport=changes.Count+" perubahan diterapkan. Backup: "+backup;Debug.Log("[BALANCE CSV] "+LastReport);
        }
        catch{Undo.RevertAllDownToGroup(group);AssetDatabase.SaveAssets();throw;}
    }
}

public class BalanceCsvWindow : EditorWindow
{
    Vector2 scroll;
    [MenuItem("Nature Paradise/Data CSV/Balance CSV Panel",false,0)]
    public static void Open(){var window=GetWindow<BalanceCsvWindow>("Balance CSV");window.minSize=new Vector2(600,640);
        var main=EditorGUIUtility.GetMainWindowPosition();window.position=new Rect(main.x+30,main.y+60,620,Mathf.Max(640,main.height-100));}
    void OnGUI()
    {
        EditorGUILayout.HelpBox("1. Export data terbaru → 2. Edit CSV UTF-8 → 3. Preview → 4. Import.\nSettings: edit hanya value. ID/path/setting jangan diubah. Export akan menimpa CSV, jadi import perubahan spreadsheet dahulu.",MessageType.Info);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
        if(GUILayout.Button("Export item + tanaman"))BalanceCsvDatabase.Export();
        if(GUILayout.Button("Validate item + tanaman"))BalanceCsvDatabase.Validate();
        if(GUILayout.Button("Import item + tanaman"))BalanceCsvDatabase.Import();
        if(GUILayout.Button("Export ikan"))FishingCsvDatabase.Export();
        if(GUILayout.Button("Validate ikan"))FishingCsvDatabase.ValidateMenu();
        if(GUILayout.Button("Import ikan"))FishingCsvDatabase.Import();
        if(GUILayout.Button("Export resep"))KitchenRecipeCsvDatabase.Export();
        if(GUILayout.Button("Validate resep"))KitchenRecipeCsvDatabase.Validate();
        if(GUILayout.Button("Import resep"))KitchenRecipeCsvDatabase.Import();
        if(GUILayout.Button("Export settings hewan / shop / drops / upgrade"))BalanceSettingsCsv.Export();
        if(GUILayout.Button("Preview perubahan settings"))BalanceSettingsCsv.Preview();
        if(GUILayout.Button("Import settings tervalidasi"))BalanceSettingsCsv.Import();
        if(GUILayout.Button("Export kalender / cuaca / festival / cafe"))ScheduleCafeCsvDatabase.Export();
        if(GUILayout.Button("Validate kalender / cuaca / festival / cafe"))ScheduleCafeCsvDatabase.Validate();
        if(GUILayout.Button("Import kalender / cuaca / festival / cafe"))ScheduleCafeCsvDatabase.Import();
        EditorGUI.EndDisabledGroup();
        if(GUILayout.Button("Buka folder CSV"))BalanceCsvDatabase.OpenFolder();
        if(GUILayout.Button("Buka panduan"))AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath("Assets/Nature  Paradaise/Data/Balance/PANDUAN.md"));
        scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.SelectableLabel(BalanceSettingsCsv.LastReport+"\n\nKalender / cafe:\n"+ScheduleCafeCsvDatabase.LastReport,GUILayout.MinHeight(300));EditorGUILayout.EndScrollView();
    }
}
