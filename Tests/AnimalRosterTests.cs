using System;
using System.Collections.Generic;
public class ItemSO { }
public enum AnimalIllnessStage {Healthy,Mild,Severe,Recovering}
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public class GameObject { }
    public class RuntimeAnimatorController { }
    public class AudioClip { }
    public struct Vector3 {public static Vector3 one=>new Vector3();}
    public class MinAttribute:Attribute {public MinAttribute(float v){} }
    public class TooltipAttribute:Attribute {public TooltipAttribute(string v){} }
    public class HeaderAttribute:Attribute {public HeaderAttribute(string v){} }
    public class CreateAssetMenuAttribute:Attribute {public string fileName,menuName;}
    public static class Resources {public static T Load<T>(string p) where T:class=>null;}
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));
        public static float Clamp01(float v)=>Math.Min(1,Math.Max(0,v));
    }
}
static class AnimalRosterTests
{
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static void Main()
    {
        Check((int)AnimalType.Chicken==0&&(int)AnimalType.Duck==1&&(int)AnimalType.Goat==2&&(int)AnimalType.Sheep==3&&(int)AnimalType.Cow==4,"Existing enum/save identities retained");
        Check((int)AnimalType.Pig==5&&!AnimalGrowthProfileSO.IsBird(AnimalType.Pig),"Pig uses livestock care");
        var milk=new ItemSO();var profile=new AnimalGrowthProfileSO{animalType=AnimalType.Cow,variantId="animal.cow_dairy",productItem=milk};
        var cow=new AnimalRosterEntry{id=profile.variantId,displayName="Sapi Perah",hasBuyPrice=true,buyPrice=6400,hasAdultSellPrice=true,adultSellPrice=4000,
            hasRuntimeType=true,runtimeType=AnimalType.Cow,isDefault=true,growthProfile=profile,product=milk};
        var pig=new AnimalRosterEntry{id="animal.pig_meat",displayName="Babi Ternak",hasBuyPrice=true,buyPrice=4800,hasAdultSellPrice=true,adultSellPrice=9000,
            hasRuntimeType=true,runtimeType=AnimalType.Pig,isDefault=true,growthProfile=new AnimalGrowthProfileSO{animalType=AnimalType.Pig,productionEnabled=false,sellOnlyAsAdult=true}};
        var unset=new AnimalRosterEntry{id="animal.goat_meat",hasRuntimeType=true,runtimeType=AnimalType.Goat,isDefault=true,growthProfile=new AnimalGrowthProfileSO()};
        var horse=new AnimalRosterEntry{id="animal.horse_riding",hasBuyPrice=true,buyPrice=12000};
        var catalog=new AnimalRosterCatalog{entries=new[]{cow,pig,unset,horse}};
        Check(cow.CanPurchase&&pig.CanPurchase,"Supported, priced species enabled");
        Check(!unset.CanPurchase&&unset.buyPrice==0&&!unset.hasBuyPrice,"Blank price never becomes a free animal");
        Check(!horse.CanPurchase,"Companions cannot enter barn shop through a numeric price alone");
        Check(catalog.Find(cow.id)==cow&&catalog.Find("missing")==null&&catalog.Find(null)==null,"Stable roster lookup");
        Check(catalog.DefaultFor(AnimalType.Cow)==cow&&catalog.DefaultFor(AnimalType.Chicken)==null,"Defaults are explicit");
        var old=new AnimalShopOffer{animalType=AnimalType.Cow,price=5000};var egg=new AnimalShopOffer{animalType=AnimalType.Chicken,offerKind=AnimalShopOfferKind.Egg,price=500};
        old.offerKind=AnimalShopOfferKind.Young;
        var offers=new List<AnimalShopOffer>{old,egg};catalog.ApplyOffers(offers);
        Check(old.price==6400&&old.growthProfile==profile&&old.productItem==milk,"Legacy offers receive source prices/profile");
        Check(egg.price==500&&offers.Count==3,"Egg prices retained, only supported pig added");
        catalog.ApplyOffers(offers);Check(offers.Count==3,"Offer application is idempotent");
        var restore=cow.RestoreOffer();Check(restore.growthProfile.variantId==cow.id&&restore.productItem==milk&&restore.animalType==AnimalType.Cow,"Restore retains variant binding");
        foreach(int price in new[]{4000,750,2500,3000,1000,9000})
        {
            var sale=new AnimalSalePriceSettings{baseSellPrice=price};
            Check(sale.Calculate(0)==price,"CSV price at zero hearts "+price);
            Check(sale.Calculate(10)==2*price,"Existing heart bonus retained "+price);
        }
        var unknown=new AnimalSalePriceSettings{baseSellPrice=0};Check(unknown.Calculate(10)==0,"Unknown sale price cannot acquire a bonus value");
        Console.WriteLine("PASS "+checks+" animal roster/price checks");
    }
}
