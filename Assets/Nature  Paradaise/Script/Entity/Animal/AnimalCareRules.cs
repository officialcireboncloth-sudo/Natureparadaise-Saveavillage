/// <summary>Aturan murni kandang, jadwal turnout dan porsi pakan untuk gameplay serta tes.</summary>
public static class AnimalCareRules
{
    public static bool Fits(bool bird, AnimalHousingKind kind, int residents, int capacity) =>
        kind == (bird ? AnimalHousingKind.Coop : AnimalHousingKind.Barn) && residents >= 0 && residents < capacity;
    public static bool CanTurnOut(bool born, bool healthy, bool goodWeather, int hour, bool hasHome, bool housed) =>
        born && healthy && goodWeather && hasHome && housed && hour >= 6 && hour < 18;
    public static bool ConsumeFeed(ref int stock, bool born, bool fed)
    {
        if (stock <= 0 || !born || fed) return false;
        stock--;
        return true;
    }
}
