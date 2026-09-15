using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Fish Feed sementara untuk menguji Daily Growth tanpa menunggu Fishing Shop/Feed Maker.</summary>
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
        if (inventory == null || feed == null) return;
        granted = true;
        if (inventory.GetCount(feed) == 0)
        {
            inventory.Add(feed, 20);
            InventoryHotbarUI.TryShowTemporaryMessage("TEST FISH POND: Fish Feed x20 ditambahkan.", 3f);
        }
#endif
    }

    public static void EnsureTestFeed(Inventory inventory)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ItemSO feed = Resources.Load<ItemSO>("Items/Fish/Fish Feed");
        if (inventory != null && feed != null && inventory.GetCount(feed) == 0) inventory.Add(feed, 20);
#endif
    }
}
