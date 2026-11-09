using System;
using UnityEngine;

// Minimal engine stand-ins. The rules under test are the actual runtime source files.
namespace UnityEngine
{
    public class ScriptableObject { public string name; }
    public class GameObject { }
    public class Sprite { }
    public class CreateAssetMenuAttribute:Attribute { public string menuName; }
    public class HeaderAttribute:Attribute { public HeaderAttribute(string s){} }
    public class TooltipAttribute:Attribute { public TooltipAttribute(string s){} }
    public class TextAreaAttribute:Attribute { public TextAreaAttribute(int a,int b){} }
    public class HideInInspector:Attribute { }
    public class MinAttribute:Attribute { public MinAttribute(float n){} }
    public class RangeAttribute:Attribute { public RangeAttribute(float a,float b){} }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));
        public static float Clamp01(float a)=>Math.Min(1,Math.Max(0,a));
        public static int RoundToInt(float a)=>(int)Math.Round(a,MidpointRounding.ToEven);
        public static int CeilToInt(float a)=>(int)Math.Ceiling(a);
    }
}
public enum PlayerToolType { None,Fertilizer,CropBooster,Seed }
public enum FeedKind { None }
public enum FishRarity { Common,Uncommon,Rare,Legendary }
public static class ItemCatalog {public static string LegacyId(string name)=>name;}
public static class FishMeasurement {public static float SizePriceMultiplier(ItemSO item,float size)=>1.6f;}
[Flags] public enum CropSeason {None=0,Spring=1,Summer=2,Autumn=4,Winter=8,All=15}
public class CropDataSO {public static CropSeason GetSeasonForDay(int day)=>(CropSeason)(1<<(((day-1)/28)%4));}
public enum WeatherType {Sunny,Rain,Storm}
public static class WeatherSystem
{
    public static bool IsRainWeather(WeatherType w)=>w==WeatherType.Rain;
    public static bool IsStormWeather(WeatherType w)=>w==WeatherType.Storm;
}

static class HarvestSystemsTests
{
    static int checks;
    static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
    static FruitTreeCare History(int days,int boosted,int soil=80)
    {
        var c=new FruitTreeCare();
        for(int d=1;d<=days;d++){if(d<=boosted)c.ApplyFertilizer(1,1);c.RecordDay(soil,d);c.FinishDay();}
        return c;
    }
    static void Main()
    {
        Check(new FruitTreeCare().Stars==1,"Unrecorded save is conservative");
        foreach(var pair in new[]{(0,1),(1,2),(49,2),(50,3),(79,3),(80,4),(99,4),(100,5)})
            Check(History(100,pair.Item1).Stars==pair.Item2,"Coverage boundary "+pair.Item1);
        Check(History(60,60,79).Stars==4,"Full fertilizer with soil below 80 stays four stars");
        var lowSoil=History(59,59);lowSoil.ApplyFertilizer(1,1);lowSoil.RecordDay(79,60);
        Check(lowSoil.Stars==4,"A single low soil day blocks five stars");
        foreach(int duration in new[]{1,3,7})
        {
            var care=new FruitTreeCare();Check(care.ApplyFertilizer(4,duration),"Apply fertilizer "+duration);
            Check(!care.ApplyFertilizer(4,duration),"Cannot replace active fertilizer");
            for(int d=1;d<=duration;d++){care.RecordDay(80,d);care.FinishDay();}
            Check(care.boostedDays==duration&&care.Stars==5&&!care.FertilizerActive,"Full multi-day coverage "+duration);
            care.RecordDay(80,duration+1);Check(care.Stars<5,"Expired effect does not cover tomorrow");
        }
        var saved=History(2,2);saved.ApplyFertilizer(4,7);saved.mutationRoll=.004f;
        var copy=saved.Copy();copy.RecordDay(20,3);
        Check(saved.days==2&&saved.minimumSoil==80,"Copy owns its history");
        var next=saved.NextCycle();Check(next.days==0&&next.boostedDays==0&&next.Stars==1,"Harvest resets care");
        Check(next.FertilizerActive&&next.fertilizerDaysRemaining==7&&next.mutationRoll<0,"Harvest keeps effect and resets roll");
        next.RecordDay(80,3);next.RecordDay(0,3);Check(next.days==1&&next.minimumSoil==80,"Duplicate daily sample ignored");
        Check(!new FruitTreeCare().ApplyFertilizer(0,3)&&!new FruitTreeCare().ApplyFertilizer(1,0),"Invalid fertilizer rejected");

        var tree=new TreeDefinition{matureDays=60,useFruitCare=true,fruitItem=new ItemSO(),fruitSeasons=CropSeason.All,
            floweringDays=2,fruitGrowthDays=3,stages=new[]{
                new TreeStageVisual{fromGrowthDay=0},new TreeStageVisual{fromGrowthDay=6},new TreeStageVisual{fromGrowthDay=20},
                new TreeStageVisual{fromGrowthDay=60},new TreeStageVisual{fromGrowthDay=60}}};
        var progress=TreeProgress.Create(tree,1,false);
        for(int d=1;d<=59;d++){progress.Care.ApplyFertilizer(1,1);progress.Advance(tree,d,WeatherType.Storm,false,0,80);}
        Check(!progress.Mature(tree)&&progress.growth==59,"Immature on day 59 regardless drought or storm");
        progress.Care.ApplyFertilizer(1,1);progress.Advance(tree,60,WeatherType.Sunny,false,1,80);
        Check(progress.Mature(tree)&&progress.age==60&&progress.Care.days==60,"Exactly 60 growth days");
        progress.Advance(tree,60,WeatherType.Sunny,false,1,0);Check(progress.Care.days==60,"Daily tree advancement idempotent");
        for(int d=61;d<=65;d++){progress.Care.ApplyFertilizer(1,1);progress.Advance(tree,d,WeatherType.Sunny,false,1,80);}
        Check(progress.fruitStage==TreeFruitStage.Ready&&progress.Care.Stars==5,"Ready after configured fruit interval");
        int recorded=progress.Care.days;progress.Advance(tree,66,WeatherType.Sunny,false,1,0);
        Check(progress.Care.days==recorded&&progress.Care.Stars==5,"Waiting to collect does not dilute care");
        var cloned=progress.Copy();cloned.Care.minimumSoil=0;
        Check(progress.Care.minimumSoil==80,"Tree save copy is deep");
        var legacy=TreeProgress.Create(tree,1,true);
        Check(legacy.Care.days==60&&legacy.Care.boostedDays==0&&legacy.Care.minimumSoil==0,"Legacy growth gets no invented care credit");
        tree.fruitSeasons=CropSeason.Winter;
        progress.Advance(tree,67,WeatherType.Sunny,false,1,80);
        Check(progress.fruitStage==TreeFruitStage.Dormant&&progress.Care.days==recorded,"Off-season pauses care and fruiting");
        for(int stars=1;stars<=5;stars++)Check(tree.HarvestAmount(stars)==new[]{5,7,10,15,20}[stars-1],"Yield for star "+stars);
        Check(tree.StageIndex(0)==0&&tree.StageIndex(6)==1&&tree.StageIndex(20)==2,"Seed sprout young stages");
        Check(tree.StageIndex(60,TreeFruitStage.Growing)==3&&tree.StageIndex(60,TreeFruitStage.Ready)==4,"Fruit and harvest stages");
        tree.wildTree=true;tree.fruitItem=null;var wild=TreeProgress.Create(tree,1,false);
        wild.Advance(tree,1,WeatherType.Sunny,false,1,0);Check(wild.growth==1&&wild.care==null,"Wild tree keeps original growth");

        Check(HarvestRules.Succeeds(0,.01)&&HarvestRules.Succeeds(.00999,.01),"Mutation success within one percent");
        Check(!HarvestRules.Succeeds(.01,.01)&&!HarvestRules.Succeeds(1,.01),"Mutation failure on threshold");
        Check(!HarvestRules.Succeeds(-1,.01)&&!HarvestRules.Succeeds(0,0),"Invalid roll and zero chance rejected");
        for(int stars=1;stars<=4;stars++)Check(HarvestRules.CropAmount(3,stars,.2,0)==3,"Only five stars can double "+stars);
        Check(HarvestRules.CropAmount(3,5,.2,.19999)==6&&HarvestRules.CropAmount(3,5,.2,.2)==3,"Double harvest threshold");

        var normal=new ItemSO{sellPrice=100,category=ItemCategory.Food,sellPricesByStars=new[]{10,20,35,60,95}};
        var mutant=new ItemSO{isMutatedCrop=true,mutationSourceItem=normal,category=ItemCategory.Food};
        for(int stars=1;stars<=5;stars++)
        {
            Check(normal.GetMarketSellPrice(stars)==normal.sellPricesByStars[stars-1],"Exact star price "+stars);
            Check(mutant.GetMarketSellPrice(stars)==3*normal.GetMarketSellPrice(stars),"Mutation price "+stars);
        }
        normal.sellPricesByStars=null;Check(normal.GetMarketSellPrice(5)==200&&mutant.GetMarketSellPrice(5)==600,"Legacy fallback remains triple");
        mutant.isNotSellable=true;Check(mutant.GetMarketSellPrice(5)==0,"Sale restrictions retained");
        normal.isKeyItem=true;Check(normal.GetMarketSellPrice(5)==0,"Key items cannot sell");
        normal.isKeyItem=false;normal.sellPricesByStars=new[]{0,0,0,0,100};Check(normal.GetMarketSellPrice(1)==0,"Explicit zero is not silently increased");
        var milk=new ItemSO{sellPrice=400,category=ItemCategory.Food,isAnimalProduct=true};
        Check(milk.GetMarketSellPrice(0)==400&&milk.GetMarketSellPrice(1)==400,"Animal Normal quality uses listed base price");
        Check(milk.GetMarketSellPrice(2)==480&&milk.GetMarketSellPrice(5)==720,"Animal quality increments start after Normal");
        var wool=new ItemSO{sellPrice=650,category=ItemCategory.AnimalProduct};
        Check(wool.GetMarketSellPrice(1)==650,"AnimalProduct category also uses listed base price");
        Console.WriteLine("PASS "+checks+" harvest/tree/price checks");
    }
}
