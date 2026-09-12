#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Import/export data balancing CSV. Referensi visual tetap dimiliki ScriptableObject.</summary>
public static class BalanceCsvDatabase
{
    const string Root = "Assets/Nature  Paradaise/Data/Balance";
    const string ItemsPath = Root + "/Items.csv";
    const string CropsPath = Root + "/Crops.csv";
    const string NewItemsRoot = "Assets/Nature  Paradaise/Resources/Items/CSV";
    const string NewCropsRoot = "Assets/Nature  Paradaise/Resources/Profiles/Farming/CSV";

    static readonly string[] ItemColumns =
    {
        "item_id","asset_path","display_name","category","buy_price","sell_price","max_stack",
        "is_animal_product","is_key_item","is_not_sellable","requires_sell_confirmation","equipped_tool",
        "medicine_level","fertilizer_level","required_village_level","soil_restore_amount","crop_booster_level",
        "can_drop_to_world","can_place_in_world","sprinkler_level","is_edible","food_preparation",
        "health_restore","stamina_restore","hunger_restore","seed_crop_id"
    };

    // refrigerator_category bersifat opsional untuk membaca CSV lama. Export baru
    // selalu menulis kolom ini sehingga balancing selanjutnya dapat dilakukan dari CSV.
    static readonly string[] ItemExportColumns = ItemColumns.Concat(new[] { "refrigerator_category" }).ToArray();

    static readonly string[] CropColumns =
    {
        "crop_id","asset_path","seed_item_id","produce_item_id","allowed_seasons","out_of_season_behavior",
        "ideal_moisture_min","ideal_moisture_max","minimum_fertility","daily_fertility_use",
        "soil_depletion_on_harvest","base_yield","wind_vulnerability","dry_days_before_wither","watered_days_to_recover",
        "health_loss_when_dry","days_until_first_harvest","regrows_after_harvest","regrow_days","regrow_stage",
        "quality_weights","minimum_booster_for_stars","harvest_grace_days","soil_weight","moisture_weight",
        "fertility_weight","health_weight"
    };

    sealed class ItemRow
    {
        public string id, path, displayName, seedCropId;
        public ItemCategory category;
        public int buyPrice, sellPrice, maxStack, requiredVillageLevel, soilRestoreAmount, cropBoosterLevel, sprinklerLevel;
        public bool animalProduct, keyItem, notSellable, confirmSale, drop, place, edible;
        public PlayerToolType equippedTool;
        public AnimalMedicineLevel medicine;
        public FertilizerLevel fertilizer;
        public FoodPreparation foodPreparation;
        public float healthRestore, staminaRestore, hungerRestore;
        public bool hasRefrigeratorCategory;
        public RefrigeratorCategory refrigeratorCategory;
    }

    sealed class CropRow
    {
        public string id, path, seedItemId, produceItemId;
        public CropSeason seasons;
        public OutOfSeasonCropBehavior outOfSeason;
        public int moistureMin, moistureMax, fertility, dailyFertility, soilDepletion, yield, dryDays, recoveryDays,
            dryHealthLoss, harvestDays, regrowDays, regrowStage, graceDays;
        public bool regrows;
        public float[] qualityWeights;
        public int[] minimumBoosters;
        public float windVulnerability, soilWeight, moistureWeight, fertilityWeight, healthWeight;
    }

    [MenuItem("Nature Paradise/Data CSV/Export Items and Crops")]
    public static void Export()
    {
        Directory.CreateDirectory(Root);
        List<ItemSO> items = LoadAssets<ItemSO>();
        List<CropDataSO> crops = LoadAssets<CropDataSO>();
        EnsureStableIds(items, crops);

        var itemRows = items.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase).Select(item => new[]
        {
            item.Id, PathOf(item), item.itemName, item.category.ToString(), I(item.buyPrice), I(item.sellPrice), I(item.StackLimit),
            B(item.isAnimalProduct), B(item.isKeyItem), B(item.isNotSellable), B(item.requiresSellConfirmation), item.equippedTool.ToString(),
            item.animalMedicineLevel.ToString(), item.fertilizerLevel.ToString(), I(item.requiredVillageLevel), I(item.soilRestoreAmount), I(item.cropBoosterLevel),
            B(item.canDropToWorld), B(item.canPlaceInWorld), I(item.sprinklerLevel), B(item.isEdible), item.foodPreparation.ToString(),
            F(item.healthRestore), F(item.staminaRestore), F(item.hungerRestore), item.seedCrop != null ? item.seedCrop.cropId : string.Empty,
            item.refrigeratorCategory.ToString()
        });
        Csv.Write(ItemsPath, ItemExportColumns, itemRows);

        var cropRows = crops.OrderBy(crop => crop.cropId, StringComparer.OrdinalIgnoreCase).Select(crop => new[]
        {
            crop.cropId, PathOf(crop), Id(crop.seedItem), Id(crop.produceItem), Season(crop.allowedSeasons), crop.outOfSeasonBehavior.ToString(),
            I(crop.idealMoistureMin), I(crop.idealMoistureMax), I(crop.minimumFertility), I(crop.dailyFertilityUse),
            I(crop.soilDepletionOnHarvest), I(crop.baseYield), F(crop.windVulnerability), I(crop.dryDaysBeforeWither), I(crop.wateredDaysToRecover),
            I(crop.healthLossWhenDry), I(crop.daysUntilFirstHarvest), B(crop.regrowsAfterHarvest), I(crop.regrowDays), I(crop.regrowStage),
            Join(crop.qualityWeights), Join(crop.minimumBoosterForStars), I(crop.harvestGraceDays), F(crop.soilWeight), F(crop.moistureWeight),
            F(crop.fertilityWeight), F(crop.healthWeight)
        });
        Csv.Write(CropsPath, CropColumns, cropRows);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DATA CSV] Export selesai: {items.Count} item dan {crops.Count} crop di {Root}.");
    }

    [MenuItem("Nature Paradise/Data CSV/Validate Items and Crops")]
    public static void Validate()
    {
        if (TryRead(out List<ItemRow> items, out List<CropRow> crops, out List<string> errors))
            Debug.Log($"[DATA CSV] VALID: {items.Count} item dan {crops.Count} crop siap di-import.");
        else LogErrors(errors);
    }

    [MenuItem("Nature Paradise/Data CSV/Import Items and Crops")]
    public static void Import()
    {
        if (!TryRead(out List<ItemRow> itemRows, out List<CropRow> cropRows, out List<string> errors))
        {
            LogErrors(errors);
            return;
        }

        EnsureFolder(NewItemsRoot);
        EnsureFolder(NewCropsRoot);
        Dictionary<string, ItemSO> items = LoadAssets<ItemSO>().GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, CropDataSO> crops = LoadAssets<CropDataSO>().GroupBy(crop => crop.cropId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        int created = 0, updated = 0;

        foreach (ItemRow row in itemRows)
        {
            if (!items.TryGetValue(row.id, out ItemSO item))
            {
                item = !string.IsNullOrWhiteSpace(row.path) ? AssetDatabase.LoadAssetAtPath<ItemSO>(row.path) : null;
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemSO>();
                    item.name = SafeAssetName(row.displayName, row.id);
                    string path = NewAssetPath(row.path, NewItemsRoot, item.name);
                    AssetDatabase.CreateAsset(item, path);
                    created++;
                }
                items.Add(row.id, item);
            }
            else { Undo.RecordObject(item, "Import Item CSV"); updated++; }
            Apply(item, row);
            EditorUtility.SetDirty(item);
        }

        foreach (CropRow row in cropRows)
        {
            if (!crops.TryGetValue(row.id, out CropDataSO crop))
            {
                crop = !string.IsNullOrWhiteSpace(row.path) ? AssetDatabase.LoadAssetAtPath<CropDataSO>(row.path) : null;
                if (crop == null)
                {
                    crop = ScriptableObject.CreateInstance<CropDataSO>();
                    crop.name = SafeAssetName(row.id.Replace("crop.", string.Empty), row.id) + " Crop";
                    string path = NewAssetPath(row.path, NewCropsRoot, crop.name);
                    AssetDatabase.CreateAsset(crop, path);
                    created++;
                }
                crops.Add(row.id, crop);
            }
            else { Undo.RecordObject(crop, "Import Crop CSV"); updated++; }
            Apply(crop, row, items);
            EditorUtility.SetDirty(crop);
        }

        foreach (ItemRow row in itemRows)
        {
            ItemSO item = items[row.id];
            item.seedCrop = string.IsNullOrWhiteSpace(row.seedCropId) ? null : crops[row.seedCropId];
            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ItemCatalog.Invalidate();
        Debug.Log($"[DATA CSV] Import selesai: {created} asset baru, {updated} asset diperbarui. Visual reference tidak diubah.");
    }

    [MenuItem("Nature Paradise/Data CSV/Open Balance Folder")]
    public static void OpenFolder()
    {
        Directory.CreateDirectory(Root);
        EditorUtility.RevealInFinder(Path.GetFullPath(Root));
    }

    static void Apply(ItemSO item, ItemRow row)
    {
        item.itemId=row.id; item.itemName=row.displayName; item.category=row.category;
        item.buyPrice=row.buyPrice; item.sellPrice=row.sellPrice; item.maxStack=row.maxStack;
        item.isAnimalProduct=row.animalProduct; item.isKeyItem=row.keyItem; item.isNotSellable=row.notSellable;
        item.requiresSellConfirmation=row.confirmSale; item.equippedTool=row.equippedTool;
        item.animalMedicineLevel=row.medicine; item.fertilizerLevel=row.fertilizer;
        item.requiredVillageLevel=row.requiredVillageLevel; item.soilRestoreAmount=row.soilRestoreAmount;
        item.cropBoosterLevel=row.cropBoosterLevel; item.canDropToWorld=row.drop; item.canPlaceInWorld=row.place;
        item.sprinklerLevel=row.sprinklerLevel; item.isEdible=row.edible; item.foodPreparation=row.foodPreparation;
        item.healthRestore=row.healthRestore; item.staminaRestore=row.staminaRestore; item.hungerRestore=row.hungerRestore;
        if(row.hasRefrigeratorCategory) item.refrigeratorCategory=row.refrigeratorCategory;
    }

    static void Apply(CropDataSO crop, CropRow row, Dictionary<string, ItemSO> items)
    {
        crop.cropId=row.id;
        crop.seedItem=string.IsNullOrWhiteSpace(row.seedItemId)?null:items[row.seedItemId];
        crop.produceItem=string.IsNullOrWhiteSpace(row.produceItemId)?null:items[row.produceItemId];
        crop.allowedSeasons=row.seasons; crop.outOfSeasonBehavior=row.outOfSeason;
        crop.idealMoistureMin=row.moistureMin; crop.idealMoistureMax=row.moistureMax;
        crop.minimumFertility=row.fertility; crop.dailyFertilityUse=row.dailyFertility;
        crop.soilDepletionOnHarvest=row.soilDepletion; crop.baseYield=row.yield; crop.windVulnerability=row.windVulnerability;
        crop.dryDaysBeforeWither=row.dryDays; crop.wateredDaysToRecover=row.recoveryDays;
        crop.healthLossWhenDry=row.dryHealthLoss; crop.daysUntilFirstHarvest=row.harvestDays;
        crop.regrowsAfterHarvest=row.regrows; crop.regrowDays=row.regrowDays; crop.regrowStage=row.regrowStage;
        crop.qualityWeights=row.qualityWeights; crop.minimumBoosterForStars=row.minimumBoosters;
        crop.harvestGraceDays=row.graceDays; crop.soilWeight=row.soilWeight; crop.moistureWeight=row.moistureWeight;
        crop.fertilityWeight=row.fertilityWeight; crop.healthWeight=row.healthWeight;
    }

    static bool TryRead(out List<ItemRow> items, out List<CropRow> crops, out List<string> errors)
    {
        errors = new List<string>(); items = new List<ItemRow>(); crops = new List<CropRow>();
        List<Dictionary<string,string>> itemTable = ReadRequired(ItemsPath, ItemColumns, errors);
        List<Dictionary<string,string>> cropTable = ReadRequired(CropsPath, CropColumns, errors);
        HashSet<string> itemIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> cropIds = new(StringComparer.OrdinalIgnoreCase);
        for (int i=0;i<itemTable.Count;i++)
        {
            Dictionary<string,string> source=itemTable[i]; string label=$"Items.csv baris {i+2}";
            ItemRow row=new() {id=Required(source,"item_id",label,errors),path=source["asset_path"],displayName=Required(source,"display_name",label,errors),seedCropId=source["seed_crop_id"]};
            if(!string.IsNullOrEmpty(row.id) && !itemIds.Add(row.id)) errors.Add($"{label}: item_id duplikat '{row.id}'.");
            if(!string.IsNullOrEmpty(row.id) && !row.id.StartsWith("item.",StringComparison.OrdinalIgnoreCase)) errors.Add($"{label}: item_id harus diawali 'item.'.");
            ItemSO existing=!string.IsNullOrWhiteSpace(row.path)?AssetDatabase.LoadAssetAtPath<ItemSO>(row.path):null;
            if(existing!=null&&!string.IsNullOrWhiteSpace(existing.itemId)&&!string.Equals(existing.itemId,row.id,StringComparison.OrdinalIgnoreCase)) errors.Add($"{label}: item_id asset lama tidak boleh diubah dari '{existing.itemId}'.");
            row.category=E(source,"category",ItemCategory.General,label,errors); row.equippedTool=E(source,"equipped_tool",PlayerToolType.None,label,errors);
            row.medicine=E(source,"medicine_level",AnimalMedicineLevel.None,label,errors); row.fertilizer=E(source,"fertilizer_level",FertilizerLevel.None,label,errors);
            row.foodPreparation=E(source,"food_preparation",FoodPreparation.NotFood,label,errors);
            row.buyPrice=N(source,"buy_price",0,int.MaxValue,label,errors); row.sellPrice=N(source,"sell_price",0,int.MaxValue,label,errors);
            row.maxStack=N(source,"max_stack",1,9999,label,errors); row.requiredVillageLevel=N(source,"required_village_level",1,999,label,errors);
            row.soilRestoreAmount=N(source,"soil_restore_amount",0,100000,label,errors); row.cropBoosterLevel=N(source,"crop_booster_level",1,5,label,errors);
            row.sprinklerLevel=N(source,"sprinkler_level",0,4,label,errors);
            row.animalProduct=Bool(source,"is_animal_product",label,errors); row.keyItem=Bool(source,"is_key_item",label,errors);
            row.notSellable=Bool(source,"is_not_sellable",label,errors); row.confirmSale=Bool(source,"requires_sell_confirmation",label,errors);
            row.drop=Bool(source,"can_drop_to_world",label,errors); row.place=Bool(source,"can_place_in_world",label,errors); row.edible=Bool(source,"is_edible",label,errors);
            row.healthRestore=R(source,"health_restore",0,float.MaxValue,label,errors); row.staminaRestore=R(source,"stamina_restore",0,float.MaxValue,label,errors);
            row.hungerRestore=R(source,"hunger_restore",0,float.MaxValue,label,errors); items.Add(row);
            if(source.TryGetValue("refrigerator_category",out string refrigeratorCategory)&&!string.IsNullOrWhiteSpace(refrigeratorCategory))
            {
                row.hasRefrigeratorCategory=true;
                if(Enum.TryParse(refrigeratorCategory,true,out RefrigeratorCategory parsed)&&Enum.IsDefined(typeof(RefrigeratorCategory),parsed))
                    row.refrigeratorCategory=parsed;
                else errors.Add($"{label}: 'refrigerator_category' enum tidak dikenal: '{refrigeratorCategory}'.");
            }
        }
        for(int i=0;i<cropTable.Count;i++)
        {
            Dictionary<string,string> source=cropTable[i]; string label=$"Crops.csv baris {i+2}";
            CropRow row=new(){id=Required(source,"crop_id",label,errors),path=source["asset_path"],seedItemId=source["seed_item_id"],produceItemId=source["produce_item_id"]};
            if(!string.IsNullOrEmpty(row.id) && !cropIds.Add(row.id)) errors.Add($"{label}: crop_id duplikat '{row.id}'.");
            if(!string.IsNullOrEmpty(row.id) && !row.id.StartsWith("crop.",StringComparison.OrdinalIgnoreCase)) errors.Add($"{label}: crop_id harus diawali 'crop.'.");
            CropDataSO existing=!string.IsNullOrWhiteSpace(row.path)?AssetDatabase.LoadAssetAtPath<CropDataSO>(row.path):null;
            if(existing!=null&&!string.IsNullOrWhiteSpace(existing.cropId)&&!string.Equals(existing.cropId,row.id,StringComparison.OrdinalIgnoreCase)) errors.Add($"{label}: crop_id asset lama tidak boleh diubah dari '{existing.cropId}'.");
            row.seasons=E(source,"allowed_seasons",CropSeason.All,label,errors); row.outOfSeason=E(source,"out_of_season_behavior",OutOfSeasonCropBehavior.PauseGrowth,label,errors);
            row.moistureMin=N(source,"ideal_moisture_min",0,100,label,errors); row.moistureMax=N(source,"ideal_moisture_max",0,100,label,errors);
            row.fertility=N(source,"minimum_fertility",0,100,label,errors); row.dailyFertility=N(source,"daily_fertility_use",0,100000,label,errors);
            row.soilDepletion=N(source,"soil_depletion_on_harvest",0,100000,label,errors); row.yield=N(source,"base_yield",1,100000,label,errors);
            row.windVulnerability=R(source,"wind_vulnerability",0.1f,3f,label,errors);
            row.dryDays=N(source,"dry_days_before_wither",1,100000,label,errors); row.recoveryDays=N(source,"watered_days_to_recover",1,100000,label,errors);
            row.dryHealthLoss=N(source,"health_loss_when_dry",0,100,label,errors); row.harvestDays=N(source,"days_until_first_harvest",1,100000,label,errors);
            row.regrows=Bool(source,"regrows_after_harvest",label,errors); row.regrowDays=N(source,"regrow_days",1,100000,label,errors);
            row.regrowStage=N(source,"regrow_stage",-1,1000,label,errors); row.graceDays=N(source,"harvest_grace_days",0,100000,label,errors);
            row.qualityWeights=Floats(source,"quality_weights",5,label,errors); row.minimumBoosters=Ints(source,"minimum_booster_for_stars",5,label,errors);
            row.soilWeight=R(source,"soil_weight",0,1,label,errors); row.moistureWeight=R(source,"moisture_weight",0,1,label,errors);
            row.fertilityWeight=R(source,"fertility_weight",0,1,label,errors); row.healthWeight=R(source,"health_weight",0,1,label,errors);
            if(row.moistureMin>row.moistureMax) errors.Add($"{label}: ideal_moisture_min melebihi maximum.");
            crops.Add(row);
        }
        foreach(ItemRow row in items) if(!string.IsNullOrWhiteSpace(row.seedCropId)&&!cropIds.Contains(row.seedCropId)) errors.Add($"Item '{row.id}': seed_crop_id '{row.seedCropId}' tidak ditemukan.");
        foreach(CropRow row in crops)
        {
            if(!string.IsNullOrWhiteSpace(row.seedItemId)&&!itemIds.Contains(row.seedItemId)) errors.Add($"Crop '{row.id}': seed_item_id '{row.seedItemId}' tidak ditemukan.");
            if(!string.IsNullOrWhiteSpace(row.produceItemId)&&!itemIds.Contains(row.produceItemId)) errors.Add($"Crop '{row.id}': produce_item_id '{row.produceItemId}' tidak ditemukan.");
        }
        return errors.Count==0;
    }

    static List<Dictionary<string,string>> ReadRequired(string path,string[] columns,List<string> errors)
    {
        if(!File.Exists(path)){errors.Add($"File belum ada: {path}. Jalankan Export terlebih dahulu.");return new();}
        List<Dictionary<string,string>> rows=Csv.Read(path,out string[] headers,out string parseError);
        if(parseError!=null){errors.Add($"{path}: {parseError}");return new();}
        foreach(string column in columns) if(!headers.Contains(column,StringComparer.OrdinalIgnoreCase)) errors.Add($"{path}: kolom '{column}' tidak ditemukan.");
        return errors.Count==0?rows:new();
    }

    static void EnsureStableIds(List<ItemSO> items,List<CropDataSO> crops)
    {
        HashSet<string> ids=new(StringComparer.OrdinalIgnoreCase);
        foreach(ItemSO item in items.OrderBy(PathOf))
        {
            string id=string.IsNullOrWhiteSpace(item.itemId)?ItemCatalog.LegacyId(item.name):item.itemId.Trim();
            if(!ids.Add(id)) id += "_"+AssetDatabase.AssetPathToGUID(PathOf(item)).Substring(0,8);
            if(item.itemId==id) continue;
            Undo.RecordObject(item,"Assign Stable Item ID"); item.itemId=id; EditorUtility.SetDirty(item);
        }
        HashSet<string> cropIds=new(StringComparer.OrdinalIgnoreCase);
        foreach(CropDataSO crop in crops.OrderBy(PathOf))
        {
            string id=string.IsNullOrWhiteSpace(crop.cropId)?ItemCatalog.LegacyId(crop.name).Replace("item.","crop."):crop.cropId.Trim();
            if(!cropIds.Add(id)) id += "_"+AssetDatabase.AssetPathToGUID(PathOf(crop)).Substring(0,8);
            if(crop.cropId==id) continue;
            Undo.RecordObject(crop,"Assign Stable Crop ID"); crop.cropId=id; EditorUtility.SetDirty(crop);
        }
        ItemCatalog.Invalidate();
    }

    static List<T> LoadAssets<T>() where T:UnityEngine.Object => AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{"Assets/Nature  Paradaise/Resources"})
        .Select(guid=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(asset=>asset!=null).ToList();
    static string PathOf(UnityEngine.Object asset)=>AssetDatabase.GetAssetPath(asset).Replace('\\','/');
    static string Id(ItemSO item)=>item!=null?item.Id:string.Empty;
    static string I(int value)=>value.ToString(CultureInfo.InvariantCulture);
    static string F(float value)=>value.ToString("0.####",CultureInfo.InvariantCulture);
    static string B(bool value)=>value?"TRUE":"FALSE";
    static string Season(CropSeason value)=>value==(CropSeason)(-1)||value==CropSeason.All?"All":value.ToString();
    static string Join(float[] values)=>values==null?string.Empty:string.Join("|",values.Select(F));
    static string Join(int[] values)=>values==null?string.Empty:string.Join("|",values.Select(I));
    static string Required(Dictionary<string,string> row,string key,string label,List<string> errors){string value=row.TryGetValue(key,out string found)?found.Trim():string.Empty;if(value.Length==0)errors.Add($"{label}: '{key}' wajib diisi.");return value;}
    static int N(Dictionary<string,string> row,string key,int min,int max,string label,List<string> errors){if(!int.TryParse(row[key],NumberStyles.Integer,CultureInfo.InvariantCulture,out int value)||value<min||value>max){errors.Add($"{label}: '{key}' harus angka {min}..{max}.");return min;}return value;}
    static float R(Dictionary<string,string> row,string key,float min,float max,string label,List<string> errors){if(!float.TryParse(row[key],NumberStyles.Float,CultureInfo.InvariantCulture,out float value)||float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max){errors.Add($"{label}: '{key}' tidak valid.");return min;}return value;}
    static bool Bool(Dictionary<string,string> row,string key,string label,List<string> errors){string value=row[key].Trim();if(value.Equals("TRUE",StringComparison.OrdinalIgnoreCase)||value=="1"||value.Equals("YES",StringComparison.OrdinalIgnoreCase))return true;if(value.Equals("FALSE",StringComparison.OrdinalIgnoreCase)||value=="0"||value.Equals("NO",StringComparison.OrdinalIgnoreCase))return false;errors.Add($"{label}: '{key}' harus TRUE/FALSE.");return false;}
    static T E<T>(Dictionary<string,string> row,string key,T fallback,string label,List<string> errors) where T:struct,Enum {bool flags=Attribute.IsDefined(typeof(T),typeof(FlagsAttribute));if(Enum.TryParse(row[key],true,out T value)&&(flags||Enum.IsDefined(typeof(T),value)))return value;errors.Add($"{label}: '{key}' enum tidak dikenal: '{row[key]}'.");return fallback;}
    static float[] Floats(Dictionary<string,string> row,string key,int expected,string label,List<string> errors){string[] parts=row[key].Split('|');if(parts.Length!=expected){errors.Add($"{label}: '{key}' harus berisi {expected} angka dipisah |.");return new float[expected];}float[] result=new float[expected];for(int i=0;i<expected;i++)if(!float.TryParse(parts[i],NumberStyles.Float,CultureInfo.InvariantCulture,out result[i])||result[i]<0){errors.Add($"{label}: '{key}' berisi angka tidak valid.");break;}return result;}
    static int[] Ints(Dictionary<string,string> row,string key,int expected,string label,List<string> errors){string[] parts=row[key].Split('|');if(parts.Length!=expected){errors.Add($"{label}: '{key}' harus berisi {expected} angka dipisah |.");return new int[expected];}int[] result=new int[expected];for(int i=0;i<expected;i++)if(!int.TryParse(parts[i],NumberStyles.Integer,CultureInfo.InvariantCulture,out result[i])||result[i]<0){errors.Add($"{label}: '{key}' berisi angka tidak valid.");break;}return result;}
    static void LogErrors(List<string> errors){Debug.LogError("[DATA CSV] Import dibatalkan.\n- "+string.Join("\n- ",errors));}
    static string SafeAssetName(string preferred,string fallback){string value=string.IsNullOrWhiteSpace(preferred)?fallback:preferred;foreach(char invalid in Path.GetInvalidFileNameChars())value=value.Replace(invalid,'_');return value.Trim();}
    static string NewAssetPath(string requested,string fallbackRoot,string name){string path=(requested??string.Empty).Replace('\\','/');if(!path.StartsWith("Assets/Nature  Paradaise/Resources/",StringComparison.OrdinalIgnoreCase)||!path.EndsWith(".asset",StringComparison.OrdinalIgnoreCase))path=fallbackRoot+"/"+name+".asset";EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));return AssetDatabase.GenerateUniqueAssetPath(path);}
    static void EnsureFolder(string path){string[] parts=path.Split('/');string current=parts[0];for(int i=1;i<parts.Length;i++){string next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}}

    static class Csv
    {
        public static void Write(string path,string[] headers,IEnumerable<string[]> rows)
        {
            StringBuilder text=new(); text.AppendLine(string.Join(",",headers.Select(Escape)));
            foreach(string[] row in rows) text.AppendLine(string.Join(",",row.Select(Escape)));
            File.WriteAllText(path,text.ToString(),new UTF8Encoding(true));
        }
        public static List<Dictionary<string,string>> Read(string path,out string[] headers,out string error)
        {
            error=null; List<List<string>> records=new(); List<string> row=new(); StringBuilder field=new(); bool quoted=false;
            string text=File.ReadAllText(path,Encoding.UTF8);
            for(int i=0;i<text.Length;i++)
            {
                char c=text[i];
                if(quoted){if(c=='"'&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else if(c=='"')quoted=false;else field.Append(c);}
                else if(c=='"')quoted=true;
                else if(c==','){row.Add(field.ToString());field.Clear();}
                else if(c=='\n'){row.Add(field.ToString().TrimEnd('\r'));field.Clear();if(row.Any(value=>!string.IsNullOrWhiteSpace(value)))records.Add(row);row=new();}
                else field.Append(c);
            }
            if(quoted){headers=Array.Empty<string>();error="Tanda kutip CSV tidak ditutup.";return new();}
            if(field.Length>0||row.Count>0){row.Add(field.ToString().TrimEnd('\r'));records.Add(row);}
            if(records.Count==0){headers=Array.Empty<string>();error="File kosong.";return new();}
            headers=records[0].Select(value=>value.Trim().TrimStart('\uFEFF')).ToArray();
            var result=new List<Dictionary<string,string>>();
            for(int index=1;index<records.Count;index++)
            {
                if(records[index].Count!=headers.Length){error=$"Baris {index+1} memiliki {records[index].Count} kolom, seharusnya {headers.Length}.";return new();}
                var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                for(int column=0;column<headers.Length;column++)values[headers[column]]=records[index][column];
                result.Add(values);
            }
            return result;
        }
        static string Escape(string value){value??=string.Empty;return value.IndexOfAny(new[]{',','"','\r','\n'})>=0?'"'+value.Replace("\"","\"\"")+'"':value;}
    }
}
#endif
