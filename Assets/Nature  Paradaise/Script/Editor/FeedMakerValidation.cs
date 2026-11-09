#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Disposable machine/inventory fixtures; never read or write a player save.</summary>
public static class FeedMakerValidation
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    [Serializable] sealed class BagState {public List<ItemStack> slots;}
    static string State(Inventory bag)=>JsonUtility.ToJson(new BagState{slots=bag.slots});
    static void Empty(Inventory bag)=>bag.RestoreContents(0,Enumerable.Repeat<ItemStack>(null,bag.CapacityAtBackpackLevel(0)).ToList());
    static void Give(Inventory bag,FeedRecipe recipe,int batches=1,int omit=-1)
    {
        for(int i=0;i<recipe.ingredients.Length;i++)
        {
            if(i==omit)continue;var ingredient=recipe.ingredients[i];
            float size=ingredient.anySmallFish?FishMeasurement.FindDefinition(ingredient.item).minimumSizeCm:0;
            Check(bag.Add(ingredient.item,ingredient.amount*batches,0,size),"Fixture receives "+ingredient.DisplayName);
        }
    }
    [MenuItem("Nature Paradise/UI/Feed Maker/Validate Recipes and Shop")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Run outside Play mode");
        checks=0;var oldClock=TimeManager.Instance;var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var actor=new GameObject("Feed recipe fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var clockActor=new GameObject("Feed fixture clock");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clockActor,scene);clockActor.SetActive(false);
            var clock=clockActor.AddComponent<TimeManager>();TimeManager.Instance=clock;clock.day=1;clock.hour=6;clock.minute=0;
            var bag=actor.AddComponent<Inventory>();Empty(bag);
            var catalog=Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog");
            Check(catalog!=null&&catalog.recipes.Length==3,"Three approved recipes");
            var supermarket=Resources.Load<ShopCatalogSO>("Shops/Minimarket Catalog");
            foreach(string id in new[]{"item.feed_concentrate","item.bread"})
            {
                var item=ItemCatalog.Resolve(id);
                Check(item!=null&&supermarket.Contains(item),"Supermarket stocks "+id);
            }
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var recipe in catalog.recipes)
            {
                Check(recipe.HasIngredients&&recipe.outputCount==10,"Ten-unit multi-ingredient recipe "+recipe.id);
                Check(FeedTypes.Kind(catalog.Item(recipe.Kind))==recipe.Kind,"Output feed kind matches "+recipe.id);
                var machineActor=new GameObject(recipe.id);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(machineActor,scene);
                var machine=machineActor.AddComponent<FeedMaker>();machine.Configure(null,catalog);
                int index=Array.IndexOf(catalog.recipes,recipe);
                Check(machine.InputCapacity>=recipe.TotalInputCount,"Level 1 accepts full recipe "+recipe.id);
                for(int missing=0;missing<recipe.ingredients.Length;missing++)
                {
                    Empty(bag);Give(bag,recipe,omit:missing);string before=State(bag);
                    Check(!machine.Queue(bag,index)&&State(bag)==before&&machine.Jobs.Count==0,"Missing ingredient is atomic "+recipe.id+" "+missing);
                }
                Empty(bag);Give(bag,recipe);int events=0;bool reentered=false;
                Action changed=()=>{events++;reentered=machine.Queue(bag,index);};bag.OnInventoryChanged+=changed;
                Check(machine.Queue(bag,index),"Queue approved recipe "+recipe.id);bag.OnInventoryChanged-=changed;
                Check(events==1&&!reentered&&machine.Jobs.Count==1,"One commit and one job "+recipe.id);
                Check(machine.Inputs==recipe.TotalInputCount&&machine.Jobs[0].output==10,"Job stores actual ingredient and output counts");
                var snapshot=(FeedMakerSaveData)typeof(FeedMaker).GetMethod("Capture",flags).Invoke(machine,null);
                var saved=JsonUtility.FromJson<FeedMakerSaveData>(JsonUtility.ToJson(snapshot));
                Check(saved.jobs[0].Kind==recipe.Kind&&saved.jobs[0].output==10,"JSON preserves feed recipe job");
                machine.Advance(machine.CurrentGameHour+recipe.hours);
                Check(machine.Ready(recipe.Kind)==10&&machine.Jobs.Count==0,"Completed batch yields exactly ten "+recipe.id);
                Empty(bag);Check(machine.CollectFeed(bag,recipe.Kind)&&bag.GetCount(catalog.Item(recipe.Kind))==1&&machine.Ready(recipe.Kind)==9,"Collect remains one unit per interaction");
            }
            // An existing save job is self-contained; removing old recipe choices must not change its output.
            var legacyActor=new GameObject("Legacy feed job fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(legacyActor,scene);
            var legacy=legacyActor.AddComponent<FeedMaker>();legacy.Configure(null,catalog);
            var oldSave=new FeedMakerSaveData{level=1,outputCapacity=60,jobs=new List<FeedJob>{new FeedJob{recipeId="grass",inputs=5,output=5,hours=4,finish=10}}};
            typeof(FeedMaker).GetMethod("Restore",flags).Invoke(legacy,new object[]{oldSave});legacy.Advance(10);
            Check(legacy.AnimalFeedOutput==5&&legacy.Jobs.Count==0,"Legacy queued jobs keep their original output");
            Debug.Log("[FEED RECIPES] PASS "+checks+" native integration checks");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);TimeManager.Instance=oldClock;}
    }
}
#endif
