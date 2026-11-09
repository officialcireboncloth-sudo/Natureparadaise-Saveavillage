using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class ScriptableObject { }
    public class CreateAssetMenuAttribute:Attribute { public string menuName; }
    public class MinAttribute:Attribute {public MinAttribute(float v){} }
    public class TooltipAttribute:Attribute {public TooltipAttribute(string v){} }
}
public enum FeedKind {None,Livestock,Poultry,Fish}
public enum ItemCategory {Material,Fish}
public enum FishSizeTier {Small,Medium,Large,Jumbo}
public sealed class ItemSO {public string itemName;public ItemCategory category;public float mediumSize=25;public int StackLimit=>99;}
public sealed class ItemStack {public ItemSO item;public int count,qualityStars;public float fishSizeCm,fishWeightKg;}
public static class FishMeasurement
{
    public static FishSizeTier GetSizeTier(ItemSO item,float size)=>size<item.mediumSize?FishSizeTier.Small:FishSizeTier.Medium;
}
static class FeedRecipeTests
{
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static ItemSO Item(string name)=>new ItemSO{itemName=name};
    static FeedIngredient Ingredient(ItemSO item,int amount)=>new FeedIngredient{item=item,amount=amount};
    static List<ItemStack> Bag(params ItemStack[] entries)=>new List<ItemStack>(entries);
    static ItemStack Stack(ItemSO item,int count,float size=0,int stars=0,float weight=0)=>new ItemStack
        {item=item,count=count,fishSizeCm=size,qualityStars=stars,fishWeightKg=weight};
    static int Count(List<ItemStack> bag,ItemSO item){int n=0;foreach(var s in bag)if(s?.item==item)n+=s.count;return n;}
    static bool Prepare(List<ItemStack> bag,FeedRecipe recipe,int batches,out List<ItemStack> result)
        =>FeedRecipePlan.TryPrepare(bag,Math.Max(8,bag.Count),recipe,batches,out result,out _);
    static void Main()
    {
        var grass=Item("Rumput");var corn=Item("Jagung");var concentrate=Item("Konsentrat");
        var soy=Item("Kedelai");var snail=Item("Keong danau");var bread=Item("Roti");
        var tilapia=Item("Tilapia");tilapia.category=ItemCategory.Fish;
        var koi=Item("Koi");koi.category=ItemCategory.Fish;koi.mediumSize=30;
        var livestock=new FeedRecipe{ingredients=new[]{Ingredient(grass,10),Ingredient(corn,2),Ingredient(concentrate,1)},outputCount=10};
        var poultry=new FeedRecipe{ingredients=new[]{Ingredient(corn,5),Ingredient(soy,1),Ingredient(snail,1)},producesPoultryFeed=true,outputCount=10};
        var fish=new FeedRecipe{ingredients=new[]{new FeedIngredient{item=tilapia,amount=5,anySmallFish=true},Ingredient(bread,1)},producesFishFeed=true,outputCount=10};
        var recipes=new[]{livestock,poultry,fish};
        Check(livestock.Kind==FeedKind.Livestock&&poultry.Kind==FeedKind.Poultry&&fish.Kind==FeedKind.Fish,"Three distinct feed kinds");
        Check(livestock.TotalInputCount==13&&poultry.TotalInputCount==7&&fish.TotalInputCount==6,"Actual ingredient unit totals");
        Check(new FeedMakerCatalog{recipes=recipes}.MinimumInputCapacity==13,"All recipes fit level-one baseline");
        foreach(var recipe in recipes)
        {
            var bag=new List<ItemStack>();foreach(var i in recipe.ingredients)bag.Add(Stack(i.item,i.amount, i.anySmallFish?15:0));
            Check(Prepare(bag,recipe,1,out var result),"Complete recipe succeeds "+recipe.Kind);
            foreach(var s in result)Check(s==null,"Exact inputs removed "+recipe.Kind);
            foreach(var s in bag)Check(s.count>0,"Planning does not mutate source "+recipe.Kind);
            for(int missing=0;missing<recipe.ingredients.Length;missing++)
            {
                bag[missing].count--;
                Check(!Prepare(bag,recipe,1,out _),"Every ingredient mandatory "+recipe.Kind+" "+missing);
                Check(bag[missing].count==recipe.ingredients[missing].amount-1,"Failure leaves input unchanged");
                bag[missing].count++;
            }
        }
        Check(!Prepare(Bag(Stack(grass,13)),livestock,1,out _),"Extra grass never replaces corn/concentrate");
        var mixed=Bag(Stack(tilapia,2,15,5,.4f),Stack(koi,3,29,2,.7f),Stack(tilapia,9,25,4,1.2f),Stack(bread,1),Stack(corn,3));
        Check(FeedRecipePlan.Available(mixed,fish.ingredients[0])==5,"Mixed small species count together");
        Check(Prepare(mixed,fish,1,out var prepared),"Mixed fish recipe succeeds");
        Check(Count(prepared,tilapia)==9&&Count(prepared,koi)==0&&Count(prepared,bread)==0&&Count(prepared,corn)==3,"Only eligible fish and bread consumed");
        Check(prepared[2].qualityStars==4&&prepared[2].fishSizeCm==25&&prepared[2].fishWeightKg==1.2f,"Untouched fish metadata preserved");
        Check(!object.ReferenceEquals(prepared[2],mixed[2]),"Prepared stacks own their copies");
        Check(!fish.ingredients[0].Matches(Stack(tilapia,5,0)),"Unknown fish size rejected");
        Check(!fish.ingredients[0].Matches(Stack(tilapia,5,25)),"Exact medium boundary rejected");
        Check(fish.ingredients[0].Matches(Stack(tilapia,5,24.99f)),"Just below medium boundary accepted");
        Check(!fish.ingredients[0].Matches(Stack(corn,5,1)),"Ordinary item cannot be a fish");
        Check(!Prepare(Bag(Stack(tilapia,10,26),Stack(bread,1)),fish,1,out _),"Medium fish cannot substitute");
        Check(Prepare(Bag(Stack(tilapia,10,15),Stack(bread,2)),fish,2,out _),"Multi-batch exact consumption");
        var shortBatch=Bag(Stack(tilapia,10,15),Stack(bread,1));
        Check(!Prepare(shortBatch,fish,2,out _)&&shortBatch[0].count==10&&shortBatch[1].count==1,"Multi-batch shortage is atomic");
        var overlap=new FeedRecipe{ingredients=new[]{Ingredient(corn,3),Ingredient(corn,3)}};
        Check(!Prepare(Bag(Stack(corn,5)),overlap,1,out _),"Duplicate ingredient cannot double-count inventory");
        Check(!Prepare(Bag(Stack(corn,100)),overlap,1,out _),"Invalid over-limit legacy stack rejected before commit");
        var legacy=new FeedRecipe{input=corn,inputCount=3,mixedInputs=new[]{corn,grass}};
        Check(Prepare(Bag(Stack(corn,1),Stack(grass,2)),legacy,1,out _),"Legacy mixed recipes remain readable");
        Check(!Prepare(mixed,fish,0,out _)&&!Prepare(mixed,fish,100,out _),"Batch limits validated");
        Check(!Prepare(mixed,new FeedRecipe{ingredients=new FeedIngredient[]{null}},1,out _),"Malformed ingredient rejected");
        Check(!Prepare(mixed,new FeedRecipe{ingredients=new[]{Ingredient(corn,0)}},1,out _),"Zero required amount rejected");
        Check(!FeedRecipePlan.TryPrepare(mixed,1,fish,1,out _,out _),"Overflow inventory cannot be silently truncated");
        Console.WriteLine("PASS "+checks+" feed recipe checks");
    }
}
