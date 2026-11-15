using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persediaan telur sementara untuk menguji animasi penetasan di Editor dan development build.</summary>
public static class EggIncubatorTestGrant
{
    const int TestEggCount = 5;
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
        if (granted || scene.name.Contains("MainMenu", System.StringComparison.OrdinalIgnoreCase) ||
            scene.name.Contains("Fishing", System.StringComparison.OrdinalIgnoreCase)) return;
        granted = EnsureTestEggs(Object.FindFirstObjectByType<Inventory>());
#endif
    }

    /// <summary>Isi sampai lima, tanpa menimpa slot atau menambah duplikat saat dipanggil ulang setelah load.</summary>
    public static bool EnsureTestEggs(Inventory inventory)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ItemSO egg = AnimalCareCatalog.Load()?.Product(AnimalType.Chicken);
        if (inventory == null || egg == null) return false;
        int missing = TestEggCount - inventory.GetCount(egg);
        if (missing <= 0) return true;
        if (!inventory.Add(egg, missing)) return false;
        InventoryHotbarUI.TryShowTemporaryMessage("TEST PENETASAN: 5 telur ayam tersedia di tas. Pindahkan ke hotbar untuk dipakai.", 4f);
        return true;
#else
        return false;
#endif
    }
}
