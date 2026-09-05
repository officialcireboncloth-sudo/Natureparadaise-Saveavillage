// Standalone test: compile with the real AnimalGrowthProfileSO.cs, not inside Assets.
using System;
class AnimalSalePriceTests
{
    static int checks;
    static void Check(bool value) { checks++; if (!value) throw new Exception("Failed check " + checks); }
    static void Main()
    {
        var settings = new AnimalSalePriceSettings();
        Check(settings.Calculate(0) == 1000);
        Check(settings.Calculate(5) == 1500);
        Check(settings.Calculate(10) == 2000);
        Check(settings.Calculate(-5) == 1000);
        Check(settings.Calculate(99) == 2000);
        settings.baseSellPrice = 2500; settings.bonusPerHeart = 0.2f;
        Check(settings.Calculate(5) == 5000);
        Check(settings.Calculate(10) == 7500);
        settings.bonusPerHeart = 0;
        Check(settings.Calculate(10) == 2500);
        settings.bonusPerHeart = -1;
        Check(settings.Calculate(10) == 2500);
        settings.bonusPerHeart = float.NaN;
        Check(settings.Calculate(10) == 2500);
        settings.bonusPerHeart = float.PositiveInfinity;
        Check(settings.Calculate(10) == 2500);
        settings.baseSellPrice = -1;
        Check(settings.Calculate(10) == 0);
        settings.baseSellPrice = int.MaxValue; settings.bonusPerHeart = float.MaxValue;
        Check(settings.Calculate(10) == int.MaxValue);
        settings.baseSellPrice = 5; settings.bonusPerHeart = 0.1f;
        Check(settings.Calculate(1) == 6);
        Console.WriteLine(checks + " animal sale price assertions passed (actual pricing code, Unity stubs).");
    }
}
public class ItemSO { }
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public class GameObject { }
    public class RuntimeAnimatorController { }
    public class AudioClip { }
    public struct Vector3 { public static Vector3 one => new Vector3(); }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static int Clamp(int v, int a, int b) => Math.Max(a,Math.Min(b,v));
        public static float Clamp01(float v) => Math.Max(0,Math.Min(1,v));
    }
}
