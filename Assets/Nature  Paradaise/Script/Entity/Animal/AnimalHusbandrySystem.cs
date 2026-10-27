using UnityEngine;

/// <summary>Menghubungkan bangunan dan hewan lama/runtime tanpa mengedit asset scene.</summary>
public sealed class AnimalHusbandrySystem : MonoBehaviour
{
    float nextScan;
    public static void Scan()
    {
        // Authored Map portals may live beside their home; include them when repairing runtime housing.
        var portals = FindObjectsByType<BarnInterior>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PropertySite site in PropertySite.ActiveSites)
        {
            if (site == null || site.ActiveDefinition == null || site.ActiveDefinition.HousingKind == AnimalHousingKind.None) continue;
            AnimalHome home = site.GetComponent<AnimalHome>();
            if (home == null) home = site.gameObject.AddComponent<AnimalHome>();
            home.site = site;
            bool hasPortal = false;
            foreach (var portal in portals)
                if (portal != null && portal.home == home) { hasPortal = true; break; }
            if (!hasPortal)
            {
                var access = new GameObject("HousingInteriorPortal_Runtime");
                access.transform.SetParent(site.transform, false);
                access.AddComponent<BarnInterior>().ConfigureScene(home, home.door);
            }
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
