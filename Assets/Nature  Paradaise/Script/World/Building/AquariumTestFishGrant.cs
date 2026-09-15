using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Loadout development satu kali per Play untuk menguji Aquarium dari map utama.</summary>
public static class AquariumTestFishGrant
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
        EnsureTestFish(inventory);
#endif
    }

    public static void EnsureTestFish(Inventory inventory)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ItemSO fish = Resources.Load<ItemSO>("Items/Fish/Tilapia");
        if (inventory == null || fish == null || inventory.GetCount(fish) + AquariumService.GetStoredCount(fish) > 0) return;
        granted = true;
        inventory.Add(fish, 1, 0, 18f);
        inventory.Add(fish, 1, 2, 38f);
        inventory.Add(fish, 1, 4, 55f);
        InventoryHotbarUI.TryShowTemporaryMessage("TEST AQUARIUM: 3 Tilapia ditambahkan ke Inventory.", 3f);
#endif
    }
}
