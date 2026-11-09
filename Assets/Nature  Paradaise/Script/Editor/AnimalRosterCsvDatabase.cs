#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Imports authored animal rows; empty prices are unknown, never free offers.</summary>
public static class AnimalRosterCsvDatabase
{
    const string Root="Assets/Nature  Paradaise/";
    static int Price(Dictionary<string,string> row,string key,out bool known)
    {
        string value=row[key];known=!string.IsNullOrWhiteSpace(value);
        if(!known)return 0;
        if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out int result)||result<0)
            throw new InvalidOperationException(row["animal_id"]+": invalid "+key);
        return result;
    }
    static List<AnimalRosterEntry> Prepare(out AnimalRosterCatalog catalog)
    {
        catalog=AnimalRosterCatalog.Load();if(catalog==null)throw new InvalidOperationException("Animal roster asset missing");
        var rows=BalanceCsvDatabase.Csv.Read(Root+"Data/Balance/Animals.csv",out var headers,out var error);
        if(error!=null)throw new InvalidOperationException(error);
        foreach(string key in new[]{"animal_id","display_name","species","purpose","buy_price","adult_sell_price","benefit","product_item_id","product_sell_price"})
            if(!headers.Contains(key))throw new InvalidOperationException("Missing column "+key);
        var prepared=new List<AnimalRosterEntry>();var ids=new HashSet<string>();
        foreach(var row in rows)
        {
            string id=row["animal_id"];if(!ids.Add(id)||string.IsNullOrWhiteSpace(id))throw new InvalidOperationException("Missing/duplicate animal ID");
            var old=catalog.Find(id);if(old==null)throw new InvalidOperationException("Configure species/profile binding for "+id+" first");
            if(!Enum.TryParse(row["purpose"],out AnimalPurpose purpose))throw new InvalidOperationException("Unknown purpose "+id);
            var entry=new AnimalRosterEntry{id=id,displayName=row["display_name"],species=row["species"],benefit=row["benefit"],purpose=purpose,
                hasRuntimeType=old.hasRuntimeType,runtimeType=old.runtimeType,isDefault=old.isDefault,growthProfile=old.growthProfile,animalPrefab=old.animalPrefab,product=old.product};
            entry.buyPrice=Price(row,"buy_price",out entry.hasBuyPrice);entry.adultSellPrice=Price(row,"adult_sell_price",out entry.hasAdultSellPrice);
            entry.productPrice=Price(row,"product_sell_price",out entry.hasProductPrice);
            string productId=row["product_item_id"];
            if(!string.IsNullOrWhiteSpace(productId))
            {
                entry.product=Resources.LoadAll<ItemSO>("").FirstOrDefault(p=>p.Id==productId);
                if(entry.product==null)throw new InvalidOperationException("Unknown product "+productId);
            }
            else entry.product=null;
            prepared.Add(entry);
        }
        return prepared;
    }
    static void RequireEdit(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first");}
    public static string LastReport {get;private set;}="";
    [MenuItem("Nature Paradise/Data CSV/Animals/Validate")]
    public static void Validate()
    {
        RequireEdit();var prepared=Prepare(out _);LastReport=prepared.Count+" baris hewan valid; belum diterapkan.";
        Debug.Log("[ANIMAL CSV] "+LastReport);
    }
    [MenuItem("Nature Paradise/Data CSV/Animals/Export")]
    public static void Export()
    {
        RequireEdit();var catalog=AnimalRosterCatalog.Load();if(catalog?.entries==null)throw new InvalidOperationException("Animal roster missing");
        string P(bool known,int value)=>known?value.ToString(CultureInfo.InvariantCulture):"";
        string[] headers={"source_no","animal_id","display_name","species","purpose","buy_price","adult_sell_price","benefit","product_item_id","product_sell_price","runtime_type","profile_path"};
        var rows=catalog.entries.Select((e,i)=>new[]{(i+1).ToString(CultureInfo.InvariantCulture),e.id,e.displayName,e.species,e.purpose.ToString(),
            P(e.hasBuyPrice,e.buyPrice),P(e.hasAdultSellPrice,e.adultSellPrice),e.benefit,e.product!=null?e.product.Id:"",P(e.hasProductPrice,e.productPrice),
            e.hasRuntimeType?((int)e.runtimeType).ToString(CultureInfo.InvariantCulture):"",e.growthProfile!=null?AssetDatabase.GetAssetPath(e.growthProfile):""});
        BalanceCsvDatabase.Csv.Write(Root+"Data/Balance/Animals.csv",headers,rows);AssetDatabase.Refresh();
        LastReport=catalog.entries.Length+" baris hewan diekspor.";Debug.Log("[ANIMAL CSV] "+LastReport);
    }
    [MenuItem("Nature Paradise/Data CSV/Import Animal Roster")]
    public static void Import()
    {
        RequireEdit();var prepared=Prepare(out var catalog);
        Undo.RecordObject(catalog,"Import Animal Roster");catalog.entries=prepared.ToArray();EditorUtility.SetDirty(catalog);
        foreach(var entry in prepared)
        {
            if(entry.product!=null)
            {Undo.RecordObject(entry.product,"Import Animal Product Price");entry.product.sellPrice=entry.hasProductPrice?entry.productPrice:0;entry.product.isAnimalProduct=true;EditorUtility.SetDirty(entry.product);}
            if(entry.growthProfile==null)continue;
            var profile=entry.growthProfile;Undo.RecordObject(profile,"Import Animal Profile Price");
            profile.variantId=entry.id;profile.salePrice??=new AnimalSalePriceSettings();profile.salePrice.baseSellPrice=entry.hasAdultSellPrice?entry.adultSellPrice:0;
            profile.productionEnabled=entry.purpose==AnimalPurpose.Product;profile.productItem=entry.product;profile.sellOnlyAsAdult=true;
            profile.purchaseSex=entry.purpose==AnimalPurpose.Breeding?AnimalSexPolicy.Male:profile.productionEnabled?AnimalSexPolicy.Female:AnimalSexPolicy.Any;
            EditorUtility.SetDirty(profile);
        }
        var shop=Resources.Load<ShopCatalogSO>("Shops/Animal Catalog");
        if(shop!=null)
        {
            Undo.RecordObject(shop,"Import Animal Shop Prices");
            foreach(var tab in shop.tabs)
                for(int i=tab.animals.Count-1;i>=0;i--)
                {
                    var offer=tab.animals[i];if(offer==null||offer.offerKind!=AnimalShopOfferKind.Young)continue;
                    var entry=catalog.Find(offer.growthProfile!=null?offer.growthProfile.variantId:null)??catalog.DefaultFor(offer.animalType);
                    if(entry==null)continue;
                    if(!entry.hasBuyPrice){tab.animals.RemoveAt(i);continue;}
                    offer.price=entry.buyPrice;offer.growthProfile=entry.growthProfile;offer.productItem=entry.product;offer.displayName="Anak "+entry.displayName;
                }
            EditorUtility.SetDirty(shop);
        }
        foreach(var asset in prepared.SelectMany(e=>new UnityEngine.Object[]{e.product,e.growthProfile}).Where(o=>o!=null).Append(catalog).Append(shop).Where(o=>o!=null).Distinct())AssetDatabase.SaveAssetIfDirty(asset);
        LastReport=prepared.Count+" baris hewan diterapkan; harga kosong tetap belum ditetapkan.";
        Debug.Log("[ANIMAL CSV] Imported "+prepared.Count+" rows; blank prices remain unset. Growth durations and model bindings retained.");
    }
}
#endif
