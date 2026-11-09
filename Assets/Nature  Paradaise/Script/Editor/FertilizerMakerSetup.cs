#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Authored recipes, stable legacy output identities, mutation items and the plain-box station.</summary>
[InitializeOnLoad]
public static class FertilizerMakerSetup
{
    public const string Root = "Assets/Nature  Paradaise";
    public const string Output = "ArtSource/FertilizerMaker/Review";
    static double nextCheck;
    static FertilizerMakerSetup() { EditorApplication.update += ProcessRequest; }
    static void ProcessRequest()
    {
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        const string request = "ArtSource/FertilizerMaker/Apply.request";
        if (!File.Exists(request)) return;
        // Wait until the saved editor source has actually compiled before consuming a request.
        var compiled=File.GetLastWriteTimeUtc(typeof(FertilizerMakerSetup).Assembly.Location);
        if (compiled<File.GetLastWriteTimeUtc(Root+"/Script/Editor/FertilizerMakerSetup.cs") ||
            compiled<File.GetLastWriteTimeUtc(Root+"/Script/Editor/FertilizerMakerValidation.cs")) return;
        string command=File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            if(command!="audit")Apply();
            if(command=="export"){BalanceCsvDatabase.Export();FishingCsvDatabase.Export();BalanceCsvDatabase.Validate();}
            FertilizerMakerValidation.Run();
            if(command=="install")PlaceInCurrentScene();
        }
        catch (Exception error) { Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",error.ToString());Debug.LogException(error); }
    }
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;
        Folder(Path.GetDirectoryName(path).Replace('\\','/'));asset=ScriptableObject.CreateInstance<T>();asset.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(asset,path);return asset;
    }
    static void Save(Object asset){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
    [MenuItem("Nature Paradise/Soil/Place Fertilizer Maker In Current Scene")]
    public static void PlaceInCurrentScene()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var prior=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FertilizerProcessor>(true)).FirstOrDefault();
        if(prior!=null)Undo.RegisterFullObjectHierarchyUndo(prior.gameObject,"Upgrade Fertilizer Maker");
        var machine=FertilizerMakerInstaller.Install(scene);
        if(machine==null)throw new InvalidOperationException("Field 1 is required before placing the maker.");
        if(prior==null)Undo.RegisterCreatedObjectUndo(machine.gameObject,"Place Fertilizer Maker");
        Selection.activeGameObject=machine.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        SceneView.lastActiveSceneView?.FrameSelected();
    }
    static ItemSO Material(string file,string id,string name)
    {
        var item=Asset<ItemSO>(Root+"/Resources/Items/Materials/Fertilizer/"+file+".asset");
        item.itemId=id;item.itemName=name;item.category=ItemCategory.Material;item.maxStack=99;item.canDropToWorld=true;Save(item);return item;
    }
    static ItemSO Fish(string file,string id,string name)
    {
        var item=Asset<ItemSO>(Root+"/Resources/Items/Fish/"+file+".asset");
        item.itemId=id;item.itemName=name;item.category=ItemCategory.Fish;item.maxStack=16;item.canDropToWorld=true;
        item.refrigeratorCategory=RefrigeratorCategory.Fish;Save(item);return item;
    }
    static void StockChemicals(params ItemSO[] items)
    {
        string[] descriptions={
            "Mineral untuk membantu memperkuat jaringan tanaman. Bahan pembuatan Pupuk Plus, Super, dan Premium.",
            "Sumber fosfor yang mendukung pertumbuhan akar serta pembentukan bunga dan buah. Bahan pembuatan Pupuk Plus, Super, dan Premium.",
            "Sumber nitrogen yang membantu pertumbuhan daun dan batang. Bahan pembuatan Pupuk Super dan Premium.",
            "Nutrisi yang membantu pembentukan dan kualitas hasil panen. Bahan pembuatan Pupuk Premium."};
        var fallback=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Textures/UI/Icons/FertilizerMaker/icon_fertilizer_mineral.png");
        for(int i=0;i<items.Length;i++)
        {
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Textures/UI/Icons/FertilizerMaker/icon_chemical_"+items[i].name.ToLowerInvariant()+".png");
            items[i].inventoryDescription=descriptions[i];
            if(items[i].buyPrice<=0)items[i].buyPrice=100;
            if(items[i].icon==null||items[i].icon==fallback)items[i].icon=icon!=null?icon:fallback;
            Save(items[i]);
        }
        var shop=Resources.Load<ShopCatalogSO>("Shops/Minimarket Catalog");
        if(shop==null)throw new InvalidOperationException("Minimarket catalog missing");
        var tab=shop.tabs.FirstOrDefault(t=>t.title=="Perlengkapan Pertanian");
        if(tab==null)
        {
            tab=new ShopCatalogTab{title="Perlengkapan Pertanian"};
            int index=shop.tabs.FindIndex(t=>t.title=="Umpan"||t.sellInventory);
            shop.tabs.Insert(index<0?shop.tabs.Count:index,tab);
        }
        foreach(var item in items)if(!tab.items.Contains(item))tab.items.Add(item);
        Save(shop);
    }
    static FertilizerIngredient I(ItemSO item,int amount,string label=null,ItemSO alternative=null,FishSizeTier? size=null)
        =>new(){item=item,amount=amount,displayName=label,alternatives=alternative!=null?new[]{alternative}:Array.Empty<ItemSO>(),requireFishSize=size.HasValue,fishSize=size??FishSizeTier.Small};
    static void FishDefinition(ItemSO item,bool ocean)
    {
        var fish=Asset<FishDefinitionSO>(Root+"/Resources/Fishing/"+item.name+".asset");
        fish.fishId="fish."+item.name.ToLowerInvariant();fish.item=item;fish.waterTypes=ocean?FishingWaterMask.Ocean:FishingWaterMask.River|FishingWaterMask.Lake|FishingWaterMask.Pond;
        fish.seasons=CropSeason.All;fish.weather=FishingWeatherMask.All;fish.startHour=0;fish.endHour=24;
        fish.requiredRodLevel=ocean?3:1;fish.rarity=ocean?FishRarity.Rare:FishRarity.Uncommon;fish.encounterWeight=ocean?.5f:.8f;
        fish.minimumSizeCm=ocean?80:15;fish.maximumSizeCm=ocean?300:75;
        fish.pondMediumSizeCm=ocean?110:25;fish.pondLargeSizeCm=ocean?180:45;fish.pondJumboSizeCm=ocean?260:65;Save(fish);
    }
    [MenuItem("Nature Paradise/Soil/Setup Fertilizer Maker")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring fertilizer data.");
        Directory.CreateDirectory(Output);
        foreach(var events in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))
            if(events.name=="Fertilizer Event System" && events.transform.childCount==0)Object.DestroyImmediate(events.gameObject);
        var catalog=FertilizerCatalog.Load();if(catalog==null)throw new InvalidOperationException("Fertilizer catalog missing");
        string[] files={"Crop Booster 20","Organic Crop Booster","Premium Crop Booster","Deluxe Crop Booster"};
        string[] names={"Pupuk Dasar","Pupuk Plus","Pupuk Super","Pupuk Premium"};int[] durations={1,1,3,7};
        var fertilizers=new ItemSO[4];
        for(int i=0;i<4;i++)
        {
            var item=AssetDatabase.LoadAssetAtPath<ItemSO>(Root+"/Resources/Items/Farming/"+files[i]+".asset");
            if(item==null)throw new InvalidOperationException(files[i]+" missing");
            item.itemName=names[i];item.cropBoosterLevel=i+1;item.cropBoosterDurationDays=durations[i];item.equippedTool=PlayerToolType.CropBooster;
            item.inventoryDescription=$"Pupuk kualitas tanaman. Efek aktif {durations[i]} hari termasuk hari pemakaian. Menyumbang bagian pupuk dari kualitas panen (maks. 40%).";
            Save(item);fertilizers[i]=item;
        }
        ItemSO Load(string path)=>Resources.Load<ItemSO>("Items/"+path);
        var tilapia=Load("Fish/Tilapia");var tuna=Load("Fish/Tuna");var livestock=Load("Materials/Livestock Manure");var poultry=Load("Materials/Poultry Manure");
        var koi=Fish("Koi","item.koi","Koi");var marlin=Fish("Marlin","item.marlin","Marlin");FishDefinition(koi,false);FishDefinition(marlin,true);
        var shell=Material("Sea Shell","item.sea_shell","Kerang laut");var ca=Material("Calcium","item.calcium","Kalsium (Ca)");
        var p=Material("Phosphate","item.phosphate","Fosfat (P)");var urea=Material("Urea","item.urea","Urea");var k=Material("Potassium","item.potassium","Kalium (K)");
        StockChemicals(ca,p,urea,k);
        FertilizerIngredient Manure(int amount)=>I(livestock,amount,"Kotoran hewan",poultry);
        FertilizerRecipe Recipe(int i,params FertilizerIngredient[] ingredients)=>new(){id="maker."+new[]{"basic","plus","super","premium"}[i],output=fertilizers[i],outputCount=1,productionHours=0,ingredients=ingredients};
        catalog.cropBoosters=fertilizers;
        catalog.makerRecipes=new[]{Recipe(0,I(tilapia,1,"Tilapia kecil",size:FishSizeTier.Small),Manure(1),I(shell,1)),
            Recipe(1,I(koi,1),Manure(2),I(ca,1),I(p,1)),
            Recipe(2,I(tuna,1,"Tuna sedang",size:FishSizeTier.Medium),Manure(5),I(ca,1),I(p,1),I(urea,1)),
            Recipe(3,I(marlin,1,"Marlin besar",size:FishSizeTier.Large),Manure(10),I(ca,1),I(p,1),I(urea,1),I(k,1))};Save(catalog);
        foreach(string guid in AssetDatabase.FindAssets("t:CropDataSO",new[]{Root}))
        {
            var crop=AssetDatabase.LoadAssetAtPath<CropDataSO>(AssetDatabase.GUIDToAssetPath(guid));if(crop.produceItem==null)continue;
            if(crop.mutationProduceItem==null)
            {
                string path=Root+"/Resources/Items/Crops/Mutations/"+crop.produceItem.name+" Mutation.asset";
                var mutation=AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if(mutation==null){Folder(Path.GetDirectoryName(path).Replace('\\','/'));mutation=Object.Instantiate(crop.produceItem);AssetDatabase.CreateAsset(mutation,path);}
                mutation.name=Path.GetFileNameWithoutExtension(path);mutation.itemId=crop.produceItem.Id+"_mutation";mutation.itemName=crop.produceItem.itemName+" Mutasi";mutation.isMutatedCrop=true;
                mutation.inventoryDescription="Hasil mutasi langka. Harga jual 3x hasil normal pada bintang yang sama.";Save(mutation);crop.mutationProduceItem=mutation;Save(crop);
            }
            crop.mutationProduceItem.isMutatedCrop=true;
            crop.mutationProduceItem.mutationSourceItem=crop.produceItem;
            crop.mutationProduceItem.inventoryDescription="Hasil mutasi langka. Harga jual 3x hasil normal pada bintang yang sama.";
            Save(crop.mutationProduceItem);
        }
        Asset<FertilizerMakerTheme>(Root+"/Resources/UI/Fertilizer Maker Theme.asset");
        string prefabPath=Root+"/Resources/World/FertilizerMaker_Dummy.prefab";Folder(Root+"/Resources/World");
        var root=new GameObject("FertilizerMaker_Dummy");
        try
        {
            root.AddComponent<FertilizerProcessor>();var collider=root.AddComponent<BoxCollider>();collider.center=new(0,.6f,0);collider.size=new(1.05f,1.2f,.85f);
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name="Visual_Dummy_ReplaceLater";box.transform.SetParent(root.transform,false);
            box.transform.localPosition=new(0,.6f,0);box.transform.localScale=collider.size;Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/SoilScanner/Scanner Cream.mat");
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{Object.DestroyImmediate(root);}
        if(FertilizerMakerArtSetup.IsAvailable)FertilizerMakerArtSetup.Apply();
        ItemCatalog.Invalidate();Debug.Log("FERTILIZER_MAKER_SETUP_PASS");
    }
}
#endif
