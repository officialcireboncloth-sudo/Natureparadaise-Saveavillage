using System;
using UnityEngine;

/// <summary>Data Village Progression yang disimpan bersama save game.</summary>
[Serializable]
public sealed class VillageProgressSaveData
{
    public int level = 1;
    public int conditionPoints;
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
    [SerializeField, Min(0)] int conditionPoints;
    [Tooltip("Total Condition Point untuk mencapai Lv.2, Lv.3, dan Lv.4.")]
    [SerializeField] int[] levelThresholds = { 100, 250, 450 };

    public int VillageLevel => villageLevel;
    public int ConditionPoints => conditionPoints;
    public int MaximumLevel => Mathf.Max(1, ValidThresholds.Length + 1);
    public int NextLevelThreshold => villageLevel >= MaximumLevel ? conditionPoints : ValidThresholds[villageLevel - 1];
    public int CurrentLevelFloor => villageLevel <= 1 ? 0 : ValidThresholds[Mathf.Clamp(villageLevel - 2, 0, ValidThresholds.Length - 1)];
    public int EffectiveVillageLevel => ProgressionRequirementSettings.EffectiveVillageLevel(villageLevel);
    public event Action<int> LevelChanged;
    public event Action<int, int> ProgressChanged;

    int[] ValidThresholds => levelThresholds != null && levelThresholds.Length > 0
        ? levelThresholds
        : new[] { 100, 250, 450 };

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
        int validated = Mathf.Clamp(level, 1, MaximumLevel);
        if (validated <= villageLevel)
            return;
        villageLevel = validated;
        conditionPoints = Mathf.Max(conditionPoints, CurrentLevelFloor);
        LevelChanged?.Invoke(villageLevel);
        ProgressChanged?.Invoke(conditionPoints, NextLevelThreshold);
    }

    /// <summary>Menambah kondisi desa dan otomatis menangani beberapa kenaikan level.</summary>
    public int AddConditionPoints(int amount, string source = null)
    {
        if (amount <= 0) return 0;
        int previousLevel = villageLevel;
        conditionPoints = Mathf.Max(0, conditionPoints + amount);
        while (villageLevel < MaximumLevel && conditionPoints >= ValidThresholds[villageLevel - 1])
            villageLevel++;
        ProgressChanged?.Invoke(conditionPoints, NextLevelThreshold);
        if (villageLevel != previousLevel)
        {
            LevelChanged?.Invoke(villageLevel);
            SaveLoadFeedback.Instance?.ShowMessage($"Village naik ke Lv.{villageLevel}!");
        }
        Debug.Log($"[VILLAGE] +{amount} Condition Point{(string.IsNullOrWhiteSpace(source) ? string.Empty : $" dari {source}")}. Total {conditionPoints}, Lv.{villageLevel}.");
        return villageLevel - previousLevel;
    }

    public bool MeetsRequirement(int requiredLevel) =>
        ProgressionRequirementSettings.MeetsVillageLevel(villageLevel, requiredLevel);

    public VillageProgressSaveData Capture() => new() { level = villageLevel, conditionPoints = conditionPoints };

    public void Restore(VillageProgressSaveData data)
    {
        villageLevel = Mathf.Max(1, data?.level ?? 1);
        villageLevel = Mathf.Clamp(villageLevel, 1, MaximumLevel);
        conditionPoints = Mathf.Max(CurrentLevelFloor, data?.conditionPoints ?? 0);
        LevelChanged?.Invoke(villageLevel);
        ProgressChanged?.Invoke(conditionPoints, NextLevelThreshold);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null || FindFirstObjectByType<VillageProgressionService>() != null)
            return;
        new GameObject("VillageProgression_Runtime").AddComponent<VillageProgressionService>();
    }
}
