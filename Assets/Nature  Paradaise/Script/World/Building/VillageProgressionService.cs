using System;
using UnityEngine;

/// <summary>Data Village Progression yang disimpan bersama save game.</summary>
[Serializable]
public sealed class VillageProgressSaveData
{
    public int level = 1;
}

/// <summary>
/// Sumber tunggal Village Level untuk requirement rumah dan properti publik.
/// Service ringan ini disiapkan agar sistem misi desa nantinya cukup mengubah satu nilai.
/// </summary>
[DisallowMultipleComponent]
public sealed class VillageProgressionService : MonoBehaviour
{
    public static VillageProgressionService Instance { get; private set; }

    [SerializeField, Min(1)] int villageLevel = 1;

    public int VillageLevel => villageLevel;
    public event Action<int> LevelChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Menaikkan Village Level tanpa mengizinkan progression mundur.</summary>
    public void SetLevel(int level)
    {
        int validated = Mathf.Max(1, level);
        if (validated <= villageLevel)
            return;
        villageLevel = validated;
        LevelChanged?.Invoke(villageLevel);
    }

    public bool MeetsRequirement(int requiredLevel) => villageLevel >= Mathf.Max(0, requiredLevel);

    public VillageProgressSaveData Capture() => new() { level = villageLevel };

    public void Restore(VillageProgressSaveData data)
    {
        villageLevel = Mathf.Max(1, data?.level ?? 1);
        LevelChanged?.Invoke(villageLevel);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null || FindFirstObjectByType<VillageProgressionService>() != null)
            return;
        new GameObject("VillageProgression_Runtime").AddComponent<VillageProgressionService>();
    }
}
