using UnityEngine;

/// <summary>Menghubungkan bangunan dan hewan lama/runtime tanpa mengedit asset scene.</summary>
public sealed class AnimalHusbandrySystem : MonoBehaviour
{
    float nextScan;
    public static void Scan()
    {
        foreach (PropertySite site in PropertySite.ActiveSites)
        {
            if (site == null || site.ActiveDefinition == null || site.ActiveDefinition.HousingKind == AnimalHousingKind.None) continue;
            AnimalHome home = site.GetComponent<AnimalHome>();
            if (home == null) home = site.gameObject.AddComponent<AnimalHome>();
            home.site = site;
        }
        foreach (AnimalGrowthSystem animal in AnimalGrowthSystem.ActiveAnimals)
        {
            if (animal == null) continue;
            if (animal.GetComponent<AnimalRoutine>() == null) animal.gameObject.AddComponent<AnimalRoutine>();
            animal.GetComponent<AnimalController>().ApplyCareDefaults();
        }
    }
    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 2f;
        Scan();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindFirstObjectByType<AnimalHusbandrySystem>() == null)
            new GameObject("AnimalHusbandry_Runtime").AddComponent<AnimalHusbandrySystem>();
    }
}
