using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stubs hanya untuk menguji source data/gameplay murni di luar proses Unity.
namespace UnityEngine
{
    public class ScriptableObject { }
    public class GameObject { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class MinAttribute : Attribute { public MinAttribute(float n) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x=a; y=b; z=c; }
        public static Vector3 one => new Vector3(1,1,1);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Vector2Int
    {
        public int x,y;
        public Vector2Int(int a,int b) {x=a;y=b;}
    }
    public static class Mathf
    {
        public static int Clamp(int a,int b,int c) => Math.Min(c,Math.Max(a,b));
        public static int Abs(int a) => Math.Abs(a);
        public static float Abs(float a) => Math.Abs(a);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static float Min(float a,float b) => Math.Min(a,b);
    }
}
public class ItemSO {public int sprinklerLevel; public bool IsSprinkler => sprinklerLevel>0;}
[Flags] public enum CropSeason {None=0,Spring=1,Summer=2,Autumn=4,Winter=8,All=15}
public class CropDataSO {public static CropSeason GetSeasonForDay(int day) => (CropSeason)(1 << (((day-1)/28)%4));}
public enum WeatherType {Sunny,Rain,Storm}
public class WeatherSystem
{
    public static WeatherSystem Instance;
    public bool IsRainToday;
    public static bool IsRainWeather(WeatherType weather) => weather==WeatherType.Rain;
    public static bool IsStormWeather(WeatherType weather) => weather==WeatherType.Storm;
}
public class FakeTransform {public Vector3 position;}
public class PlacedWorldItem
{
    public static List<PlacedWorldItem> Active = new List<PlacedWorldItem>();
    public bool IsInstalledFarmItem;
    public ItemSO Item;
    public FakeTransform transform = new FakeTransform();
}
public class FieldArea
{
    public float CellSize => 1;
    public static FieldArea Instance = new FieldArea();
    public HashSet<string> watered = new HashSet<string>();
    public static bool TryGetAt(Vector3 point,out FieldArea field,out int x,out int z)
    {field=Instance;x=(int)point.x;z=(int)point.z;return x>=0&&x<20&&z>=0&&z<20;}
    public bool CanPlaceFarmItem(int x,int z) => !FarmPlacement.Occupied(this,x,z);
    public Vector3 GridToWorld(int x,int z) => new Vector3(x,0,z);
    public void WaterBySprinkler(int x,int z) {if(x>=0&&x<20&&z>=0&&z<20)watered.Add(x+","+z);}
}
class TreeSprinklerTests
{
    static int assertions;
    static void Check(bool value,string name) {assertions++;if(!value)throw new Exception(name);}
    static void Main()
    {
        for(int level=1;level<=4;level++)
        {
            var offsets=FarmPlacement.Offsets(level).ToArray();
            Check(offsets.Length==new[]{0,4,8,24,48}[level],"exact coverage");
            Check(offsets.All(o=>o.x!=0||o.y!=0),"center excluded");
            Check(offsets.Select(o=>o.x+","+o.y).Distinct().Count()==offsets.Length,"no duplicates");
            PlacedWorldItem.Active.Clear();
            var sprinkler=new PlacedWorldItem {IsInstalledFarmItem=true,Item=new ItemSO {sprinklerLevel=level}};
            sprinkler.transform.position=new Vector3(10,0,10);
            PlacedWorldItem.Active.Add(sprinkler);
            FieldArea.Instance.watered.Clear();
            FarmPlacement.WaterField(FieldArea.Instance);
            Check(FieldArea.Instance.watered.Count==offsets.Length,"watering exact pattern");
            Check(FarmPlacement.Occupied(FieldArea.Instance,10,10),"center occupied");
            sprinkler.IsInstalledFarmItem=false;
            Check(!FarmPlacement.Occupied(FieldArea.Instance,10,10),"pickup/drop frees tile");
        }
        PlacedWorldItem.Active[0].IsInstalledFarmItem=true;
        WeatherSystem.Instance=new WeatherSystem {IsRainToday=true};
        FieldArea.Instance.watered.Clear();FarmPlacement.WaterField(FieldArea.Instance);
        Check(FieldArea.Instance.watered.Count==0,"rain skips sprinkler");
        var profile=new TreeDefinition {matureDays=56,waterUntilGrowthDay=42,fruitItem=new ItemSO(),fruitSeasons=CropSeason.All};
        var tree=TreeProgress.Create(profile,1,false);
        for(int day=1;day<=56;day++)tree.Advance(profile,day,WeatherType.Sunny,true,1);
        Check(tree.Mature(profile)&&tree.age==56,"56 watered days mature");
        tree.Advance(profile,56,WeatherType.Sunny,true,1);
        Check(tree.age==56&&tree.fruitProgress==0,"duplicate daily event ignored");
        for(int day=57;day<=61;day++)tree.Advance(profile,day,WeatherType.Sunny,true,1);
        Check(tree.fruitStage==TreeFruitStage.Ready,"fruit cycle ready");
        profile.fruitSeasons=CropSeason.Spring;
        tree.Advance(profile,62,WeatherType.Sunny,true,1);
        Check(tree.fruitStage==TreeFruitStage.Dormant&&tree.Mature(profile),"off season alive without fruit");
        var dry=TreeProgress.Create(profile,1,false);
        dry.Advance(profile,1,WeatherType.Sunny,false,1);
        Check(dry.growth==1&&dry.health==100,"one missed day grace");
        dry.Advance(profile,2,WeatherType.Sunny,false,1);
        Check(dry.growth==1.25f,"repeated drought slows");
        dry.Advance(profile,3,WeatherType.Rain,false,1);
        Check(dry.growth==2.25f&&dry.dryDays==0,"rain recovers");
        var copy=dry.Copy();copy.growth=0;
        Check(dry.growth==2.25f,"independent save snapshot");
        foreach(int duration in new[]{28,42,56,84})
        {
            profile.matureDays=duration;
            var t=TreeProgress.Create(profile,4,false);
            for(int day=4;day<4+duration;day++)t.Advance(profile,day,WeatherType.Rain,false,1);
            Check(t.Mature(profile)&&t.age==duration,"modular duration / late planting");
        }
        Console.WriteLine(assertions+" tree/sprinkler assertions passed (pure logic, not Play Mode).");
    }
}
