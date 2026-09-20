using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persediaan sementara untuk menguji Fish Pond dan alur Feed Maker.</summary>
public static class FishPondTestGrant
{
    static bool granted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        granted = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void TryInitialGrant() => TryGrant(SceneManager.GetActiveScene());

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryGrant(scene);

    static void TryGrant(Scene scene)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (granted || scene.name.Contains("Fishing", System.StringComparison.OrdinalIgnoreCase) ||
            scene.name.Contains("MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;
        Inventory inventory = Object.FindFirstObjectByType<Inventory>();
        ItemSO feed = Resources.Load<ItemSO>("Items/Fish/Fish Feed");
        ItemSO fish = Resources.Load<ItemSO>("Items/Fish/Tilapia");
        ItemSO grass = Resources.Load<ItemSO>("Items/Materials/Grass");
        ItemSO medicine = AnimalCareCatalog.Load()?.Medicine(AnimalMedicineLevel.Basic);
        if (inventory == null) return;
        granted = true;
        bool changed = false;
        if (feed != null && inventory.GetCount(feed) == 0)
        {
            changed |= inventory.Add(feed, 20);
        }
        if (grass != null && inventory.GetCount(grass) < 99)
            changed |= inventory.Add(grass, 99 - inventory.GetCount(grass));
        if (medicine != null && inventory.GetCount(medicine) < 5)
            changed |= inventory.Add(medicine, 5 - inventory.GetCount(medicine));
        changed |= EnsureDebugFish(inventory, fish);
        if (changed)
            InventoryHotbarUI.TryShowTemporaryMessage("DEBUG POND: Tilapia Small + Medium, Fish Feed, dan Medicine tersedia.", 3f);
#endif
    }

    public static void EnsureTestFeed(Inventory inventory)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ItemSO feed = Resources.Load<ItemSO>("Items/Fish/Fish Feed");
        if (inventory != null && feed != null && inventory.GetCount(feed) == 0) inventory.Add(feed, 20);
        ItemSO grass = Resources.Load<ItemSO>("Items/Materials/Grass");
        if (inventory != null && grass != null && inventory.GetCount(grass) < 99)
            inventory.Add(grass, 99 - inventory.GetCount(grass));
        ItemSO medicine = AnimalCareCatalog.Load()?.Medicine(AnimalMedicineLevel.Basic);
        if (inventory != null && medicine != null && inventory.GetCount(medicine) < 5)
            inventory.Add(medicine, 5 - inventory.GetCount(medicine));
        EnsureDebugFish(inventory, Resources.Load<ItemSO>("Items/Fish/Tilapia"));
#endif
    }

    static bool EnsureDebugFish(Inventory inventory, ItemSO fish)
    {
        if (inventory == null || fish == null) return false;
        FishDefinitionSO definition = FishMeasurement.FindDefinition(fish);
        float smallSize = definition != null ? definition.minimumSizeCm : 15f;
        float mediumSize = definition != null ? definition.PondMediumSizeCm : 25f;
        bool hasSmall = false;
        bool hasMedium = false;
        for (int index = 0; index < inventory.slots.Count; index++)
        {
            ItemStack stack = inventory.GetSlot(index);
            if (stack?.item != fish || stack.count <= 0) continue;
            FishSizeTier tier = FishMeasurement.GetSizeTier(fish, stack.fishSizeCm);
            hasSmall |= tier == FishSizeTier.Small;
            hasMedium |= tier == FishSizeTier.Medium;
        }

        bool changed = false;
        if (!hasSmall)
            changed |= inventory.Add(fish, 1, 0, smallSize, FishMeasurement.EstimateWeightKg(fish, smallSize));
        if (!hasMedium)
            changed |= inventory.Add(fish, 1, 0, mediumSize, FishMeasurement.EstimateWeightKg(fish, mediumSize));
        return changed;
    }
}
