#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Integration fixtures in a disposable scene; never load or write a player save.</summary>
public static class HarvestSystemsValidation
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    static void Empty(Inventory bag)=>bag.RestoreContents(0,Enumerable.Repeat<ItemStack>(null,bag.CapacityAtBackpackLevel(0)).ToList());
    [Serializable] sealed class Snapshot {public List<ItemStack> slots;}
    static string State(Inventory bag)=>JsonUtility.ToJson(new Snapshot{slots=bag.slots});

    [MenuItem("Nature Paradise/Data CSV/Validate Harvest and Fruit Trees")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Run outside Play mode");
        checks=0;
        var scene=EditorSceneManager.NewPreviewScene();var garbage=new List<Object>();
        try
        {
            var actor=new GameObject("Harvest fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var bag=actor.AddComponent<Inventory>();Empty(bag);
            var normal=ScriptableObject.CreateInstance<ItemSO>();garbage.Add(normal);
            normal.itemId="item.test_fruit";normal.maxStack=99;normal.sellPrice=100;normal.category=ItemCategory.Food;
            normal.sellPricesByStars=new[]{10,20,30,40,50};
            var mutant=Object.Instantiate(normal);garbage.Add(mutant);mutant.itemId="item.test_mutant";
            mutant.isMutatedCrop=true;mutant.mutationSourceItem=normal;
            var filler=Object.Instantiate(normal);garbage.Add(filler);filler.itemId="item.test_filler";
            // One free slot can hold 19 normal fruit but cannot hold the additional mutation.
            bag.RestoreContents(0,Enumerable.Range(0,bag.Capacity).Select(i=>i==0?null:new ItemStack{item=filler,count=99}).ToList());
            var output=new[]{new ItemStack{item=normal,count=19,qualityStars=5},new ItemStack{item=mutant,count=1,qualityStars=5}};
            string before=State(bag);int events=0;Action changed=()=>events++;bag.OnInventoryChanged+=changed;
            Check(!bag.TryAddBatch(output)&&State(bag)==before&&events==0,"Split harvest is atomic when one slot short");
            Empty(bag);events=0;bool reentered=false;
            Action reentry=()=>{reentered=bag.TryAddBatch(output);};bag.OnInventoryChanged+=reentry;
            Check(bag.TryAddBatch(output),"Split harvest succeeds with capacity");bag.OnInventoryChanged-=reentry;
            Check(!reentered&&events==1&&bag.GetCount(normal)==19&&bag.GetCount(mutant)==1,"One commit, no duplicate reentry, exactly 20 fruit");
            bag.OnInventoryChanged-=changed;
            Check(mutant.GetMarketSellPrice(5)==150,"Mutation uses actual five-star price times three");
            var shop=actor.AddComponent<ShopManager>();shop.playerInv=bag;
            Check(shop.GetSellUnitPrice(normal)==50&&shop.GetSellUnitPrice(mutant)==150,"Shop quotes stack quality");
            var care=new FruitTreeCare();care.ApplyFertilizer(4,7);
            care.RecordDay(80,1);care.FinishDay();care.mutationRoll=.002f;care.mutationRollReady=true;
            var saved=JsonUtility.FromJson<FruitTreeCare>(JsonUtility.ToJson(care));
            Check(saved.Stars==5&&saved.fertilizerDaysRemaining==6&&saved.mutationRollReady&&saved.mutationRoll==care.mutationRoll,"JSON preserves fruit care, duration and retry roll");
            var legacy=JsonUtility.FromJson<CropQualityCare>("{\"qualityVersion\":1}");
            Check(!legacy.harvestRollsReady,"Old crop save requests fresh harvest rolls");
            foreach(string species in new[]{"Apple","Mango","Orange","Coconut"})
            {
                var profile=Resources.Load<TreeDefinition>("Trees/"+species);
                Check(profile!=null&&profile.UsesFruitCare&&profile.matureDays==60,"Configured fruit care "+species);
                Check(profile.stages.Length==5&&profile.HarvestAmount(5)==20,"Five stages and yield "+species);
                Check(profile.mutationFruitItem!=null&&profile.mutationFruitItem.mutationSourceItem==profile.fruitItem,"Mutation binding "+species);
                Check(ItemCatalog.Resolve(profile.mutationFruitItem.Id)==profile.mutationFruitItem,"Mutation save identity "+species);
            }
            // The actual WorldTree path must leave progress and normal fruit untouched on full inventory.
            var treeActor=new GameObject("Fruit tree fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(treeActor,scene);
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.transform.SetParent(treeActor.transform,false);
            treeActor.AddComponent<BoxCollider>();var tree=treeActor.AddComponent<WorldTree>();
            var definition=Object.Instantiate(Resources.Load<TreeDefinition>("Trees/Apple"));garbage.Add(definition);
            definition.fruitItem=normal;definition.mutationFruitItem=mutant;definition.fruitSeasons=CropSeason.All;
            tree.InitializePlanted("test.only",definition);
            var progress=tree.Progress;progress.growth=60;progress.fruitStage=TreeFruitStage.Ready;
            progress.care=new FruitTreeCare{version=1,days=60,boostedDays=60,minimumSoil=80,mutationRoll=0,mutationRollReady=true};
            bag.RestoreContents(0,Enumerable.Range(0,bag.Capacity).Select(i=>i==0?null:new ItemStack{item=filler,count=99}).ToList());
            before=State(bag);
            Check(!tree.TryHarvestFruit(bag)&&State(bag)==before&&progress.fruitStage==TreeFruitStage.Ready,"Failed tree harvest preserves cycle and bag");
            Empty(bag);Check(tree.TryHarvestFruit(bag),"Actual five-star fruit harvest succeeds");
            Check(bag.GetCount(normal)==19&&bag.GetCount(mutant)==1&&bag.slots.Where(s=>s?.item==normal||s?.item==mutant).All(s=>s.qualityStars==5),"Actual tree yields 19 normal plus one five-star mutation");
            Check(progress.Care.days==0&&!progress.Care.mutationRollReady&&progress.fruitStage==TreeFruitStage.Flowering,"Harvest starts fresh care and fruit interval");
            Debug.Log("[HARVEST VALIDATION] PASS "+checks+" native integration checks");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            foreach(var obj in garbage)if(obj!=null)Object.DestroyImmediate(obj);
        }
    }
}
#endif
