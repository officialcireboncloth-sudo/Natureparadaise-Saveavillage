using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Data save satu lokasi bangunan tetap.</summary>
[Serializable]
public sealed class BuildingSiteSaveData
{
    public string siteId;
    public string buildingId;
    public BuildingConstructionState state;
    public int currentLevel;
    public int pendingLevel;
    public int completionDay;
    public bool unlocked;
}

/// <summary>
/// Lokasi bangunan tetap yang menangani preview, transaksi, konstruksi berbasis hari,
/// upgrade pada anchor yang sama, visual scene, occupancy FieldArea, dan Save/Load.
/// </summary>
[DisallowMultipleComponent]
public sealed class BuildingSite : MonoBehaviour
{
    static readonly List<BuildingSite> Registry = new();

    [Header("Identity")]
    [Tooltip("ID lokasi permanen. Posisi dapat digeser tanpa mengubah ID ini.")]
    [SerializeField] string siteId = "farm-shed-site-01";
    [SerializeField] BuildingDefinitionSO definition;
    [SerializeField] bool unlocked = true;
    [SerializeField] bool startsCompleted;
    [SerializeField, Min(1)] int startingLevel = 1;

    [Header("Fixed Placement")]
    [Tooltip("Anchor posisi/rotasi bangunan. Jika kosong memakai transform site.")]
    [SerializeField] Transform buildingAnchor;
    [SerializeField, Min(1)] int footprintWidth = 2;
    [SerializeField, Min(1)] int footprintDepth = 2;
    [SerializeField] bool reserveFieldFootprint = true;
    [SerializeField] FieldArea fieldArea;

    [Header("Editable Scene Visuals")]
    [Tooltip("Marker site kosong yang dapat diganti mesh-nya langsung di Scene View.")]
    [SerializeField] GameObject availableMarker;
    [Tooltip("Visual scaffold selama konstruksi.")]
    [SerializeField] GameObject constructionVisual;
    [Tooltip("Urutan visual final: index 0 untuk Lv.1, index 1 untuk Lv.2, dan seterusnya.")]
    [SerializeField] List<GameObject> completedLevelVisuals = new();
    [SerializeField] Color validPreviewColor = new(0.2f, 0.95f, 0.4f, 0.48f);
    [SerializeField] Color invalidPreviewColor = new(1f, 0.18f, 0.12f, 0.55f);

    [Header("Interaction Prototype")]
    [Tooltip("Interaksi sementara pada site. Menu Carpenter nantinya memanggil API yang sama.")]
    [SerializeField] KeyCode previewKey = KeyCode.E;
    [SerializeField] KeyCode confirmKey = KeyCode.C;
    [SerializeField] KeyCode upgradeKey = KeyCode.U;
    [SerializeField] KeyCode cancelKey = KeyCode.Escape;
    [SerializeField, Min(0.5f)] float interactionRadius = 3f;
    [SerializeField, Min(0f)] float promptHeight = 1.8f;

    Inventory playerInventory;
    Transform playerTransform;
    BuildingConstructionState state;
    int currentLevel;
    int pendingLevel;
    int completionDay;
    bool previewActive;
    int previewLevel;
    bool previewLocationValid;
    GameObject previewObject;
    Material previewMaterial;

    public string SiteId => siteId;
    public BuildingDefinitionSO Definition => definition;
    public BuildingConstructionState State => state;
    public int CurrentLevel => currentLevel;
    public int CompletionDay => completionDay;
    public bool IsUnlocked => unlocked;

    void Awake()
    {
        if (buildingAnchor == null)
            buildingAnchor = transform;

        playerInventory = FindFirstObjectByType<Inventory>();
        playerTransform = playerInventory != null ? playerInventory.transform : null;
        state = startsCompleted ? BuildingConstructionState.Completed : BuildingConstructionState.Available;
        currentLevel = startsCompleted ? Mathf.Max(1, startingLevel) : 0;
        ApplyVisualState();
    }

    void OnEnable()
    {
        if (!Registry.Contains(this))
            Registry.Add(this);
        TimeManager.OnDay += HandleDayChanged;
    }

    void OnDisable()
    {
        TimeManager.OnDay -= HandleDayChanged;
        Registry.Remove(this);
        CancelPreview();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            playerInventory = FindFirstObjectByType<Inventory>();
            playerTransform = playerInventory != null ? playerInventory.transform : null;
            if (playerTransform == null)
                return;
        }

        float squaredDistance = (playerTransform.position - transform.position).sqrMagnitude;
        if (squaredDistance > interactionRadius * interactionRadius)
        {
            if (previewActive)
                CancelPreview();
            return;
        }

        HandleInteraction(Mathf.Sqrt(squaredDistance));
    }

    void HandleInteraction(float distance)
    {
        if (!unlocked || definition == null)
            return;

        if (previewActive)
        {
            string validity = previewLocationValid ? "VALID" : "TERHALANG";
            BuildingLevelDefinition previewDefinition = definition.GetLevel(previewLevel);
            string requirements = BuildRequirementLabel(previewDefinition);
            WorldInteractionPrompt.Request(
                this,
                buildingAnchor,
                $"{definition.displayName} Lv.{previewLevel} [{validity}]\n{requirements}\n{confirmKey}: Bangun  {cancelKey}: Batal",
                distance,
                promptHeight
            );

            if (Input.GetKeyDown(cancelKey))
                CancelPreview();
            else if (Input.GetKeyDown(confirmKey))
                ConfirmPreview();
            return;
        }

        if (state == BuildingConstructionState.Available)
        {
            WorldInteractionPrompt.Request(this, buildingAnchor, $"{previewKey}: Preview {definition.displayName}", distance, promptHeight);
            if (Input.GetKeyDown(previewKey))
                BeginPreview(1);
        }
        else if (state == BuildingConstructionState.UnderConstruction)
        {
            int remaining = Mathf.Max(0, completionDay - CurrentDay);
            WorldInteractionPrompt.Request(this, buildingAnchor, $"Construction: {remaining} hari lagi", distance, promptHeight);
        }
        else if (state == BuildingConstructionState.Completed && definition.HasUpgradeAfter(currentLevel))
        {
            WorldInteractionPrompt.Request(this, buildingAnchor, $"{upgradeKey}: Upgrade {definition.displayName} Lv.{currentLevel + 1}", distance, promptHeight);
            if (Input.GetKeyDown(upgradeKey))
                BeginPreview(currentLevel + 1);
        }
    }

    /// <summary>Memulai preview fixed-site untuk build atau level upgrade tertentu.</summary>
    public bool BeginPreview(int targetLevel)
    {
        if (!unlocked || definition == null || definition.GetLevel(targetLevel) == null)
            return false;
        if (state == BuildingConstructionState.UnderConstruction)
            return false;
        if (state == BuildingConstructionState.Available && targetLevel != 1)
            return false;
        if (state == BuildingConstructionState.Completed && targetLevel != currentLevel + 1)
            return false;

        CancelPreview();
        previewActive = true;
        previewLevel = targetLevel;
        previewLocationValid = ValidateFixedLocation(targetLevel);
        CreatePreviewVisual(targetLevel, previewLocationValid);
        return true;
    }

    /// <summary>Menjalankan transaksi dan memulai konstruksi dari preview aktif.</summary>
    public bool ConfirmPreview()
    {
        if (!previewActive || !previewLocationValid || definition == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Lokasi bangunan tidak valid");
            return false;
        }

        BuildingLevelDefinition target = definition.GetLevel(previewLevel);
        string reason = null;
        if (target == null || !CanAfford(target, out reason))
        {
            SaveLoadFeedback.Instance?.ShowMessage(reason ?? "Resource tidak cukup");
            return false;
        }

        bool isNewBuilding = state == BuildingConstructionState.Available;
        if (isNewBuilding && !TryReserveFootprint())
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Footprint bangunan terhalang");
            return false;
        }

        if (!SpendCost(target))
        {
            if (isNewBuilding)
                ReleaseFootprint();
            SaveLoadFeedback.Instance?.ShowMessage("Transaksi konstruksi gagal");
            return false;
        }

        pendingLevel = previewLevel;
        completionDay = CurrentDay + Mathf.Max(0, target.constructionDays);
        state = BuildingConstructionState.UnderConstruction;
        CancelPreview();
        ApplyVisualState();

        if (target.constructionDays <= 0)
            CompleteConstruction();
        else
            SaveLoadFeedback.Instance?.ShowMessage($"{definition.displayName} selesai hari ke-{completionDay}");

        SaveManager.Instance?.SaveGame();
        return true;
    }

    /// <summary>Membatalkan preview tanpa mengubah resource atau state bangunan.</summary>
    public void CancelPreview()
    {
        previewActive = false;
        previewLevel = 0;
        DestroyPreviewVisual();
    }

    void HandleDayChanged()
    {
        if (state == BuildingConstructionState.UnderConstruction && CurrentDay >= completionDay)
            CompleteConstruction();
    }

    void CompleteConstruction()
    {
        currentLevel = Mathf.Max(1, pendingLevel);
        pendingLevel = 0;
        completionDay = 0;
        state = BuildingConstructionState.Completed;
        ApplyVisualState();
        SaveLoadFeedback.Instance?.ShowMessage($"{definition?.displayName ?? "Bangunan"} Lv.{currentLevel} selesai");
    }

    bool ValidateFixedLocation(int targetLevel)
    {
        if (state == BuildingConstructionState.Completed)
            return targetLevel == currentLevel + 1;
        if (!reserveFieldFootprint)
            return true;
        if (!TryResolveFieldCoordinates(out FieldArea area, out int startX, out int startZ))
            return false;
        return area.CanPlaceBuilding(startX, startZ, footprintWidth, footprintDepth);
    }

    bool TryReserveFootprint()
    {
        if (!reserveFieldFootprint)
            return true;
        return TryResolveFieldCoordinates(out FieldArea area, out int x, out int z) &&
               area.TryPlaceBuilding(x, z, footprintWidth, footprintDepth, siteId);
    }

    void ReleaseFootprint()
    {
        if (reserveFieldFootprint && fieldArea != null)
            fieldArea.TryRemoveBuilding(siteId);
    }

    bool TryResolveFieldCoordinates(out FieldArea area, out int startX, out int startZ)
    {
        area = fieldArea;
        int centerX;
        int centerZ;
        Vector3 position = buildingAnchor != null ? buildingAnchor.position : transform.position;

        if (area != null)
        {
            if (!area.WorldToGrid(position, out centerX, out centerZ))
            {
                startX = startZ = 0;
                return false;
            }
        }
        else if (!FieldArea.TryGetAt(position, out area, out centerX, out centerZ))
        {
            startX = startZ = 0;
            return false;
        }

        fieldArea = area;
        startX = centerX - footprintWidth / 2;
        startZ = centerZ - footprintDepth / 2;
        return true;
    }

    bool CanAfford(BuildingLevelDefinition level, out string reason)
        => BuildingCostUtility.CanAfford(level, playerInventory, out reason);

    bool SpendCost(BuildingLevelDefinition level)
        => BuildingCostUtility.TrySpend(level, playerInventory);

    static Dictionary<ItemSO, int> AggregateCosts(BuildingLevelDefinition level)
    {
        Dictionary<ItemSO, int> totals = new();
        for (int index = 0; index < level.materialCosts.Count; index++)
        {
            BuildingMaterialCost cost = level.materialCosts[index];
            if (cost?.item == null || cost.amount <= 0)
                continue;
            totals.TryGetValue(cost.item, out int current);
            totals[cost.item] = current + cost.amount;
        }
        return totals;
    }

    static string BuildCostLabel(BuildingLevelDefinition level)
    {
        Dictionary<ItemSO, int> totals = AggregateCosts(level);
        return totals.Count > 0
            ? $"{level.goldCost}G + {totals.Count} material"
            : $"{level.goldCost}G";
    }

    /// <summary>Menampilkan jumlah resource player dibanding requirement fixed building site.</summary>
    string BuildRequirementLabel(BuildingLevelDefinition level)
        => BuildingCostUtility.BuildRequirementLabel(level, playerInventory);

    void ApplyVisualState()
    {
        if (availableMarker != null)
            availableMarker.SetActive(state == BuildingConstructionState.Available);
        if (constructionVisual != null)
            constructionVisual.SetActive(state == BuildingConstructionState.UnderConstruction);

        for (int index = 0; index < completedLevelVisuals.Count; index++)
        {
            GameObject visual = completedLevelVisuals[index];
            if (visual != null)
                visual.SetActive(state == BuildingConstructionState.Completed && index == currentLevel - 1);
        }
    }

    void CreatePreviewVisual(int targetLevel, bool valid)
    {
        BuildingLevelDefinition level = definition.GetLevel(targetLevel);
        GameObject source = level?.completedPrefab;
        if (source == null && targetLevel - 1 >= 0 && targetLevel - 1 < completedLevelVisuals.Count)
            source = completedLevelVisuals[targetLevel - 1];

        previewObject = source != null
            ? Instantiate(source, buildingAnchor.position, buildingAnchor.rotation)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        previewObject.name = $"BuildingPreview_{definition.displayName}_Lv{targetLevel}";
        previewObject.SetActive(true);
        if (source == null)
        {
            previewObject.transform.SetPositionAndRotation(buildingAnchor.position + Vector3.up * 0.5f, buildingAnchor.rotation);
            previewObject.transform.localScale = new Vector3(footprintWidth, 1f, footprintDepth);
        }

        foreach (Collider itemCollider in previewObject.GetComponentsInChildren<Collider>(true))
            itemCollider.enabled = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        previewMaterial = new Material(shader);
        ConfigurePreviewMaterial(previewMaterial);
        foreach (Renderer itemRenderer in previewObject.GetComponentsInChildren<Renderer>(true))
        {
            int count = Mathf.Max(1, itemRenderer.sharedMaterials.Length);
            Material[] materials = new Material[count];
            for (int index = 0; index < count; index++) materials[index] = previewMaterial;
            itemRenderer.sharedMaterials = materials;
            itemRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        ApplyPreviewColor(valid);
    }

    void ApplyPreviewColor(bool valid)
    {
        if (previewMaterial != null)
            previewMaterial.color = valid ? validPreviewColor : invalidPreviewColor;
    }

    static void ConfigurePreviewMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void DestroyPreviewVisual()
    {
        if (previewObject != null)
        {
            if (Application.isPlaying) Destroy(previewObject); else DestroyImmediate(previewObject);
        }
        if (previewMaterial != null)
        {
            if (Application.isPlaying) Destroy(previewMaterial); else DestroyImmediate(previewMaterial);
        }
        previewObject = null;
        previewMaterial = null;
    }

    int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;

    /// <summary>Mengambil snapshot seluruh fixed site aktif.</summary>
    public static List<BuildingSiteSaveData> CaptureAll()
    {
        List<BuildingSiteSaveData> result = new(Registry.Count);
        for (int index = 0; index < Registry.Count; index++)
        {
            BuildingSite site = Registry[index];
            if (site == null || string.IsNullOrWhiteSpace(site.siteId))
                continue;
            result.Add(new BuildingSiteSaveData
            {
                siteId = site.siteId,
                buildingId = site.definition != null ? site.definition.buildingId : string.Empty,
                state = site.state,
                currentLevel = site.currentLevel,
                pendingLevel = site.pendingLevel,
                completionDay = site.completionDay,
                unlocked = site.unlocked
            });
        }
        return result;
    }

    /// <summary>Memulihkan state site berdasarkan Site ID; transform scene tetap menjadi sumber posisi.</summary>
    public static void RestoreAll(List<BuildingSiteSaveData> data)
    {
        if (data == null)
            return;
        for (int dataIndex = 0; dataIndex < data.Count; dataIndex++)
        {
            BuildingSiteSaveData saved = data[dataIndex];
            for (int siteIndex = 0; siteIndex < Registry.Count; siteIndex++)
            {
                BuildingSite site = Registry[siteIndex];
                if (site == null || site.siteId != saved.siteId)
                    continue;
                site.Restore(saved);
                break;
            }
        }
    }

    void Restore(BuildingSiteSaveData data)
    {
        CancelPreview();
        unlocked = data.unlocked;
        state = data.state;
        currentLevel = Mathf.Max(0, data.currentLevel);
        pendingLevel = Mathf.Max(0, data.pendingLevel);
        completionDay = Mathf.Max(0, data.completionDay);

        // Jika load dilakukan setelah tanggal selesai, bangunan langsung diselesaikan.
        if (state == BuildingConstructionState.UnderConstruction && CurrentDay >= completionDay)
            CompleteConstruction();
        else
            ApplyVisualState();
    }

    void OnDrawGizmosSelected()
    {
        Transform anchor = buildingAnchor != null ? buildingAnchor : transform;
        Gizmos.color = unlocked ? new Color(0.2f, 0.9f, 0.4f, 0.35f) : new Color(1f, 0.2f, 0.15f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(anchor.position, anchor.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.up * 0.05f, new Vector3(footprintWidth, 0.1f, footprintDepth));
        Gizmos.DrawWireCube(Vector3.up * 0.5f, new Vector3(footprintWidth, 1f, footprintDepth));
    }
}
