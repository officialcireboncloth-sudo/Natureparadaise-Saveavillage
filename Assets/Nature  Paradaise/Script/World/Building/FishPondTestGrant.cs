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
        ItemSO grass = Resources.Load<ItemSO>("Items/Materials/Grass");
        if (inventory == null) return;
        granted = true;
        bool changed = false;
        if (feed != null && inventory.GetCount(feed) == 0)
        {
            changed |= inventory.Add(feed, 20);
        }
        if (grass != null && inventory.GetCount(grass) < 99)
            changed |= inventory.Add(grass, 99 - inventory.GetCount(grass));
        if (changed)
            InventoryHotbarUI.TryShowTemporaryMessage("DEBUG FEED: Grass x99 dan Fish Feed tersedia.", 3f);
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
#endif
    }
}
