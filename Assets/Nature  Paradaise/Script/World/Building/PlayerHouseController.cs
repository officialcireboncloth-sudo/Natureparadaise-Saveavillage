using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>State khusus rumah player yang tidak dapat direlokasi atau didemolish.</summary>
[Serializable]
public sealed class PlayerHouseSaveData
{
    public int currentLevel = 1;
    public int pendingLevel;
    public BuildingConstructionState state = BuildingConstructionState.Completed;
    public int completionDay;
    public int refrigeratorLevel;
    public List<string> unlockedRooms = new();
}

/// <summary>
/// Mengelola House Lv.1-Lv.4, biaya upgrade, durasi konstruksi, exterior visual,
/// dan feature unlock. Interior lama tetap tersedia selama upgrade berlangsung.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHouseController : MonoBehaviour
{
    public static PlayerHouseController Instance { get; private set; }

    [Header("House Definition")]
    [SerializeField] BuildingDefinitionSO houseDefinition;
    [SerializeField, Min(1)] int startingLevel = 1;

    [Header("Editable Exterior Visuals")]
    [Tooltip("Index 0 untuk Lv.1, index 1 untuk Lv.2, dan seterusnya.")]
    [SerializeField] List<GameObject> exteriorLevelVisuals = new();
    [SerializeField] GameObject constructionVisual;

    [Header("Runtime State")]
    [SerializeField, Min(1)] int currentLevel = 1;
    [SerializeField] int pendingLevel;
    [SerializeField] BuildingConstructionState state = BuildingConstructionState.Completed;
    [SerializeField] int completionDay;
    [SerializeField, Min(0)] int refrigeratorLevel;

    Inventory playerInventory;
    GameObject runtimeExteriorVisual;

    public BuildingDefinitionSO Definition => houseDefinition;
    public int CurrentLevel => currentLevel;
    public int PendingLevel => pendingLevel;
    public int CompletionDay => completionDay;
    public int RefrigeratorLevel => refrigeratorLevel;
    public BuildingConstructionState State => state;
    public bool IsUnderConstruction => state == BuildingConstructionState.UnderConstruction;
    public BuildingLevelDefinition NextLevel => houseDefinition?.GetLevel(currentLevel + 1);
    public bool HasNextLevel => NextLevel != null;

    /// <summary>Hook untuk progression/upgrade Kitchen terpisah. House Lv.2 wajib sudah terbuka.</summary>
    public bool SetRefrigeratorLevel(int level)
    {
        int target = Mathf.Clamp(level, 0, 4);
        if (target > 0 && !ProgressionRequirementSettings.MeetsHouseLevel(currentLevel, 2))
            return false;
        if (refrigeratorLevel == target)
            return true;
        refrigeratorLevel = target;
        HouseFeatureService.NotifyHouseLevelChanged();
        SaveManager.Instance?.SaveGame();
        return true;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentLevel = Mathf.Max(1, currentLevel > 0 ? currentLevel : startingLevel);
        playerInventory = FindFirstObjectByType<Inventory>();
        ApplyExteriorVisual();
    }

    void OnEnable() => TimeManager.OnDay += HandleDayChanged;
    void OnDisable() => TimeManager.OnDay -= HandleDayChanged;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public string BuildNextLevelRequirements()
        => BuildingCostUtility.BuildRequirementLabel(NextLevel, ResolveInventory());

    /// <summary>Memulai upgrade level berikutnya setelah Village, Gold, dan material tervalidasi.</summary>
    public bool TryStartNextUpgrade(out string reason)
    {
        if (IsUnderConstruction)
        {
            reason = $"Upgrade berjalan sampai hari ke-{completionDay}";
            return false;
        }
        BuildingLevelDefinition target = NextLevel;
        Inventory inventory = ResolveInventory();
        if (!BuildingCostUtility.CanAfford(target, inventory, out reason))
            return false;
        if (!BuildingCostUtility.TrySpend(target, inventory))
        {
            reason = "Transaksi upgrade rumah gagal";
            return false;
        }

        pendingLevel = target.level;
        completionDay = CurrentDay + Mathf.Max(0, target.constructionDays);
        state = BuildingConstructionState.UnderConstruction;
        ApplyExteriorVisual();
        if (target.constructionDays <= 0)
            CompleteUpgrade();
        else
            SaveLoadFeedback.Instance?.ShowMessage($"House Lv.{pendingLevel} selesai hari ke-{completionDay}");
        SaveManager.Instance?.SaveGame();
        reason = null;
        return true;
    }

    void HandleDayChanged()
    {
        if (IsUnderConstruction && CurrentDay >= completionDay)
            CompleteUpgrade();
    }

    void CompleteUpgrade()
    {
        currentLevel = Mathf.Max(currentLevel, pendingLevel);
        pendingLevel = 0;
        completionDay = 0;
        state = BuildingConstructionState.Completed;
        refrigeratorLevel = Mathf.Max(refrigeratorLevel, currentLevel >= 2 ? currentLevel - 1 : 0);
        ApplyExteriorVisual();
        HouseFeatureService.NotifyHouseLevelChanged();
        SaveLoadFeedback.Instance?.ShowMessage($"House Lv.{currentLevel} selesai di-upgrade");
        SaveManager.Instance?.SaveGame();
    }

    void ApplyExteriorVisual()
    {
        DestroyRuntimeExteriorVisual();
        GameObject completedPrefab = !IsUnderConstruction
            ? houseDefinition?.GetLevel(currentLevel)?.completedPrefab
            : null;

        for (int index = 0; index < exteriorLevelVisuals.Count; index++)
            if (exteriorLevelVisuals[index] != null)
                exteriorLevelVisuals[index].SetActive(
                    completedPrefab == null && !IsUnderConstruction && index == currentLevel - 1
                );
        if (constructionVisual != null)
            constructionVisual.SetActive(IsUnderConstruction);

        if (completedPrefab != null)
        {
            try
            {
                runtimeExteriorVisual = Instantiate(completedPrefab, transform);
            }
            catch (InvalidCastException exception)
            {
                Debug.LogWarning($"[HOUSE] Prefab exterior tidak valid; memakai visual scene fallback. {exception.Message}");
                for (int index = 0; index < exteriorLevelVisuals.Count; index++)
                    if (exteriorLevelVisuals[index] != null)
                        exteriorLevelVisuals[index].SetActive(index == currentLevel - 1);
                return;
            }
            runtimeExteriorVisual.name = $"PlayerHouse_Lv{currentLevel}_Runtime";
            runtimeExteriorVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    void DestroyRuntimeExteriorVisual()
    {
        if (runtimeExteriorVisual == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeExteriorVisual);
        else
            DestroyImmediate(runtimeExteriorVisual);
        runtimeExteriorVisual = null;
    }

    Inventory ResolveInventory()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory>();
        return playerInventory;
    }

    public PlayerHouseSaveData Capture() => new()
    {
        currentLevel = currentLevel,
        pendingLevel = pendingLevel,
        state = state,
        completionDay = completionDay,
        refrigeratorLevel = refrigeratorLevel,
        unlockedRooms = HouseFeatureService.GetUnlockedFeatureIds()
    };

    public void Restore(PlayerHouseSaveData data)
    {
        if (data == null)
            return;
        currentLevel = Mathf.Max(1, data.currentLevel);
        pendingLevel = Mathf.Max(0, data.pendingLevel);
        state = data.state;
        completionDay = Mathf.Max(0, data.completionDay);
        // Save lama belum mempunyai refrigeratorLevel. Turunkan level minimum dari
        // House progression agar House Lv.2+ tidak kembali mengunci Refrigerator.
        int progressionLevel = currentLevel >= 2 ? currentLevel - 1 : 0;
        refrigeratorLevel = Mathf.Clamp(Mathf.Max(data.refrigeratorLevel, progressionLevel), 0, 4);
        if (IsUnderConstruction && CurrentDay >= completionDay)
            CompleteUpgrade();
        else
            ApplyExteriorVisual();
        HouseFeatureService.NotifyHouseLevelChanged();
    }

    /// <summary>Dipakai setup editor untuk menghubungkan asset dan visual tanpa reflection.</summary>
    public void Configure(BuildingDefinitionSO definition, List<GameObject> exteriors, GameObject construction)
    {
        houseDefinition = definition;
        exteriorLevelVisuals = exteriors ?? new List<GameObject>();
        constructionVisual = construction;
        startingLevel = currentLevel = 1;
        state = BuildingConstructionState.Completed;
        ApplyExteriorVisual();
    }

    int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;
}
