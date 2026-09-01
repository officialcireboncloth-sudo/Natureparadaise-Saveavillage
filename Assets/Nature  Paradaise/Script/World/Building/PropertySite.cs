using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>State save satu slot properti yang dapat diisi, dipindahkan, atau dikosongkan.</summary>
[Serializable]
public sealed class PropertySiteSaveData
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
/// Slot properti tetap yang dapat menampung bangunan mana pun dari BuildingCatalogSO.
/// Lokasi tetap menjaga collision, pathfinding, dan biaya CPU tetap terprediksi untuk mobile,
/// sedangkan isi slot dapat dibangun, di-upgrade, direlokasi, didemolish, dan disimpan.
/// </summary>
[DisallowMultipleComponent]
public sealed class PropertySite : MonoBehaviour
{
    static readonly List<PropertySite> Registry = new();
    static PropertySite relocationSource;

    [Header("Identity")]
    [Tooltip("ID lokasi permanen untuk Save/Load. Harus unik dan tidak berubah setelah production save.")]
    [SerializeField] string siteId = "farm-property-01";
    [Tooltip("Daftar jenis bangunan yang dapat dipilih pada site kosong.")]
    [SerializeField] BuildingCatalogSO catalog;
    [SerializeField] bool unlocked = true;
    [SerializeField] bool startsCompleted;
    [Tooltip("Bangunan awal hanya dipakai bila Starts Completed aktif.")]
    [SerializeField] BuildingDefinitionSO startingBuilding;
    [SerializeField, Min(1)] int startingLevel = 1;

    [Header("Fixed Placement")]
    [Tooltip("Anchor posisi/rotasi bangunan. Jika kosong memakai transform site.")]
    [SerializeField] Transform buildingAnchor;
    [Tooltip("Aktifkan bila site berada di FieldArea dan harus memblokir tile farming.")]
    [SerializeField] bool reserveFieldFootprint = true;
    [SerializeField] FieldArea fieldArea;

    [Header("Placement Collision")]
    [Tooltip("Tinggi volume validasi di atas footprint. Floor/terrain diabaikan kecuali memiliki BuildingPlacementBlocker.")]
    [SerializeField, Min(0.5f)] float placementCheckHeight = 4f;
    [SerializeField] LayerMask placementCheckMask = ~0;

    [Header("Editable Scene Visuals")]
    [Tooltip("Marker site kosong. Mesh dapat diganti langsung dari Scene View.")]
    [SerializeField] GameObject availableMarker;
    [Tooltip("Visual selama konstruksi berlangsung.")]
    [SerializeField] GameObject constructionVisual;
    [Tooltip("Visual fallback per level apabila definition tidak mempunyai Completed Prefab.")]
    [SerializeField] List<GameObject> completedLevelVisuals = new();
    [SerializeField] Color validPreviewColor = new(0.2f, 0.95f, 0.4f, 0.48f);
    [SerializeField] Color invalidPreviewColor = new(1f, 0.18f, 0.12f, 0.55f);

    [Header("Prototype Interaction")]
    [Tooltip("Membuka pilihan build atau menerima relocate pada site kosong.")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode previousBuildingKey = KeyCode.Q;
    [SerializeField] KeyCode nextBuildingKey = KeyCode.R;
    [SerializeField] KeyCode confirmKey = KeyCode.C;
    [SerializeField] KeyCode upgradeKey = KeyCode.U;
    [SerializeField] KeyCode relocateKey = KeyCode.M;
    [SerializeField] KeyCode demolishKey = KeyCode.X;
    [SerializeField] KeyCode cancelKey = KeyCode.Escape;
    [SerializeField, Min(0.5f)] float interactionRadius = 3f;
    [SerializeField, Min(0f)] float promptHeight = 2.2f;

    Inventory playerInventory;
    Transform playerTransform;
    BuildingDefinitionSO activeDefinition;
    BuildingConstructionState state;
    int currentLevel;
    int pendingLevel;
    int completionDay;

    bool previewActive;
    bool previewIsRelocation;
    bool previewAllowsCatalogNavigation;
    int previewLevel;
    int previewCatalogIndex;
    BuildingDefinitionSO previewDefinition;
    bool previewLocationValid;
    bool demolishConfirmationActive;
    GameObject previewObject;
    Material previewMaterial;
    GameObject runtimeCompletedVisual;
    readonly Collider[] placementOverlapBuffer = new Collider[48];

    public string SiteId => siteId;
    public BuildingCatalogSO Catalog => catalog;
    public BuildingDefinitionSO ActiveDefinition => activeDefinition;
    public BuildingConstructionState State => state;
    public int CurrentLevel => currentLevel;
    public int CompletionDay => completionDay;
    public bool IsUnlocked => unlocked;
    public bool IsEmpty => state == BuildingConstructionState.Available && activeDefinition == null;

    void Awake()
    {
        if (buildingAnchor == null)
            buildingAnchor = transform;

        playerInventory = FindFirstObjectByType<Inventory>();
        playerTransform = playerInventory != null ? playerInventory.transform : null;

        activeDefinition = startsCompleted ? startingBuilding : null;
        state = startsCompleted && activeDefinition != null
            ? BuildingConstructionState.Completed
            : BuildingConstructionState.Available;
        currentLevel = state == BuildingConstructionState.Completed ? Mathf.Max(1, startingLevel) : 0;
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
        if (relocationSource == this)
            relocationSource = null;
        CancelPreview();
    }

    void Update()
    {
        ResolvePlayerIfNeeded();
        if (playerTransform == null)
            return;

        // Relocate harus dapat dibatalkan walaupun player sudah berjalan menjauhi source site.
        if (relocationSource == this && Input.GetKeyDown(cancelKey))
        {
            CancelRelocationMode();
            return;
        }

        float squaredDistance = (playerTransform.position - transform.position).sqrMagnitude;
        if (squaredDistance > interactionRadius * interactionRadius)
        {
            if (previewActive)
                CancelPreview();
            demolishConfirmationActive = false;
            return;
        }

        HandleInteraction(Mathf.Sqrt(squaredDistance));
    }

    void ResolvePlayerIfNeeded()
    {
        if (playerTransform != null)
            return;
        playerInventory = FindFirstObjectByType<Inventory>();
        playerTransform = playerInventory != null ? playerInventory.transform : null;
    }

    void HandleInteraction(float distance)
    {
        if (!unlocked)
            return;

        if (demolishConfirmationActive)
        {
            WorldInteractionPrompt.Request(
                this,
                buildingAnchor,
                $"Demolish {activeDefinition?.displayName}?  {confirmKey}: Ya  {cancelKey}: Batal",
                distance,
                promptHeight
            );
            if (Input.GetKeyDown(confirmKey))
                ConfirmDemolish();
            else if (Input.GetKeyDown(cancelKey))
                demolishConfirmationActive = false;
            return;
        }

        if (previewActive)
        {
            HandlePreviewInteraction(distance);
            return;
        }

        if (IsEmpty)
            HandleEmptySiteInteraction(distance);
        else if (state == BuildingConstructionState.UnderConstruction)
            HandleConstructionInteraction(distance);
        else if (state == BuildingConstructionState.Completed)
            HandleCompletedInteraction(distance);
    }

    void HandleEmptySiteInteraction(float distance)
    {
        if (relocationSource != null && relocationSource != this)
        {
            string buildingName = relocationSource.activeDefinition != null
                ? relocationSource.activeDefinition.displayName
                : "Bangunan";
            WorldInteractionPrompt.Request(
                this,
                buildingAnchor,
                $"{interactKey}: Pindahkan {buildingName} ke sini  {cancelKey}: Batal",
                distance,
                promptHeight
            );
            if (Input.GetKeyDown(interactKey))
                BeginRelocationPreview();
            else if (Input.GetKeyDown(cancelKey))
                CancelRelocationMode();
            return;
        }

        string firstName = FirstCatalogDefinition != null ? FirstCatalogDefinition.displayName : "Catalog kosong";
        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{interactKey}: Pilih bangunan ({firstName})",
            distance,
            promptHeight
        );
        if (Input.GetKeyDown(interactKey))
            BeginBuildSelection();
    }

    void HandleConstructionInteraction(float distance)
    {
        int remaining = Mathf.Max(0, completionDay - CurrentDay);
        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{activeDefinition?.displayName ?? "Construction"}: {remaining} hari lagi",
            distance,
            promptHeight
        );
    }

    void HandleCompletedInteraction(float distance)
    {
        string upgrade = activeDefinition != null && activeDefinition.HasUpgradeAfter(currentLevel)
            ? $"{upgradeKey}: Upgrade  "
            : string.Empty;
        string relocate = activeDefinition != null && activeDefinition.canRelocate
            ? $"{relocateKey}: Pindah  "
            : string.Empty;
        string demolish = activeDefinition != null && activeDefinition.canDemolish
            ? $"{demolishKey}: Demolish"
            : string.Empty;

        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{activeDefinition?.displayName ?? "Building"} Lv.{currentLevel}  {upgrade}{relocate}{demolish}",
            distance,
            promptHeight
        );

        if (Input.GetKeyDown(upgradeKey) && activeDefinition != null && activeDefinition.HasUpgradeAfter(currentLevel))
            BeginUpgradePreview();
        else if (Input.GetKeyDown(relocateKey) && activeDefinition != null && activeDefinition.canRelocate)
            BeginRelocationMode();
        else if (Input.GetKeyDown(demolishKey) && activeDefinition != null && activeDefinition.canDemolish)
            demolishConfirmationActive = true;
    }

    void HandlePreviewInteraction(float distance)
    {
        string validity = previewLocationValid ? "VALID" : "TERHALANG";
        BuildingLevelDefinition level = previewDefinition != null
            ? previewDefinition.GetLevel(previewLevel)
            : null;
        string requirements = previewIsRelocation
            ? "<color=#58D982>Tanpa biaya relocate</color>"
            : BuildRequirementLabel(level);
        string navigation = previewAllowsCatalogNavigation
            ? $"{previousBuildingKey}/{nextBuildingKey}: Pilih  "
            : string.Empty;

        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{previewDefinition?.displayName ?? "Building"} Lv.{previewLevel} [{validity}]\n{requirements}\n{navigation}{confirmKey}: Konfirmasi  {cancelKey}: Batal",
            distance,
            promptHeight
        );

        if (previewAllowsCatalogNavigation && Input.GetKeyDown(previousBuildingKey))
            CycleCatalog(-1);
        else if (previewAllowsCatalogNavigation && Input.GetKeyDown(nextBuildingKey))
            CycleCatalog(1);
        else if (Input.GetKeyDown(confirmKey))
            ConfirmPreview();
        else if (Input.GetKeyDown(cancelKey))
        {
            if (previewIsRelocation)
                CancelRelocationMode();
            else
                CancelPreview();
        }
    }

    /// <summary>Membuka pilihan bangunan catalog pada site kosong.</summary>
    public bool BeginBuildSelection()
    {
        if (!IsEmpty || catalog == null || catalog.Count == 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Building Catalog kosong");
            return false;
        }

        previewCatalogIndex = Mathf.Clamp(previewCatalogIndex, 0, catalog.Count - 1);
        previewDefinition = catalog.GetAt(previewCatalogIndex);
        previewLevel = 1;
        previewAllowsCatalogNavigation = true;
        previewIsRelocation = false;
        return RefreshPreview();
    }

    /// <summary>Memulai preview upgrade level berikutnya pada site yang sama.</summary>
    public bool BeginUpgradePreview()
    {
        if (state != BuildingConstructionState.Completed || activeDefinition == null ||
            !activeDefinition.HasUpgradeAfter(currentLevel))
            return false;

        previewDefinition = activeDefinition;
        previewLevel = currentLevel + 1;
        previewAllowsCatalogNavigation = false;
        previewIsRelocation = false;
        return RefreshPreview();
    }

    /// <summary>Menandai bangunan selesai sebagai source untuk dipindahkan ke site kosong.</summary>
    public bool BeginRelocationMode()
    {
        if (state != BuildingConstructionState.Completed || activeDefinition == null || !activeDefinition.canRelocate)
            return false;

        relocationSource = this;
        SaveLoadFeedback.Instance?.ShowMessage($"Pilih Property Site kosong untuk {activeDefinition.displayName}");
        return true;
    }

    /// <summary>Menampilkan preview bangunan source pada destination site ini.</summary>
    public bool BeginRelocationPreview()
    {
        if (!IsEmpty || relocationSource == null || relocationSource == this ||
            relocationSource.activeDefinition == null)
            return false;

        previewDefinition = relocationSource.activeDefinition;
        previewLevel = relocationSource.currentLevel;
        previewAllowsCatalogNavigation = false;
        previewIsRelocation = true;
        return RefreshPreview();
    }

    /// <summary>Menjalankan transaksi build/upgrade atau transfer relocate dari preview aktif.</summary>
    public bool ConfirmPreview()
    {
        if (!previewActive || !previewLocationValid || previewDefinition == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Lokasi bangunan tidak valid");
            return false;
        }

        if (previewIsRelocation)
            return ConfirmRelocation();

        BuildingLevelDefinition targetLevel = previewDefinition.GetLevel(previewLevel);
        string reason = null;
        if (targetLevel == null || !CanAfford(targetLevel, out reason))
        {
            SaveLoadFeedback.Instance?.ShowMessage(reason ?? "Resource tidak cukup");
            return false;
        }

        bool isNewBuilding = IsEmpty;
        if (isNewBuilding && !TryReserveFootprint(previewDefinition))
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Footprint bangunan terhalang");
            return false;
        }

        if (!SpendCost(targetLevel))
        {
            if (isNewBuilding)
                ReleaseFootprint();
            SaveLoadFeedback.Instance?.ShowMessage("Transaksi konstruksi gagal");
            return false;
        }

        activeDefinition = previewDefinition;
        pendingLevel = previewLevel;
        completionDay = CurrentDay + Mathf.Max(0, targetLevel.constructionDays);
        state = BuildingConstructionState.UnderConstruction;
        CancelPreview();
        ApplyVisualState();

        if (targetLevel.constructionDays <= 0)
            CompleteConstruction();
        else
            SaveLoadFeedback.Instance?.ShowMessage($"{activeDefinition.displayName} selesai hari ke-{completionDay}");

        SaveManager.Instance?.SaveGame();
        return true;
    }

    /// <summary>Membatalkan preview tanpa mengubah inventory, gold, atau occupancy.</summary>
    public void CancelPreview()
    {
        previewActive = false;
        previewIsRelocation = false;
        previewAllowsCatalogNavigation = false;
        previewLevel = 0;
        previewDefinition = null;
        DestroyPreviewVisual();
    }

    void CycleCatalog(int direction)
    {
        if (catalog == null || catalog.Count == 0)
            return;

        previewCatalogIndex = (previewCatalogIndex + direction) % catalog.Count;
        if (previewCatalogIndex < 0)
            previewCatalogIndex += catalog.Count;
        previewDefinition = catalog.GetAt(previewCatalogIndex);
        RefreshPreview();
    }

    bool RefreshPreview()
    {
        DestroyPreviewVisual();
        if (previewDefinition == null || previewDefinition.GetLevel(previewLevel) == null)
            return false;

        previewActive = true;
        previewLocationValid = ValidateFixedLocation(previewDefinition);
        CreatePreviewVisual(previewDefinition, previewLevel, previewLocationValid);
        return true;
    }

    bool ConfirmRelocation()
    {
        PropertySite source = relocationSource;
        if (source == null || source == this || !source.IsRelocatableNow || !IsEmpty)
        {
            CancelRelocationMode();
            SaveLoadFeedback.Instance?.ShowMessage("Relocate tidak lagi valid");
            return false;
        }

        BuildingDefinitionSO movedDefinition = source.activeDefinition;
        if (!TryReserveFootprint(movedDefinition))
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Destination site terhalang");
            return false;
        }

        int movedLevel = source.currentLevel;
        source.ReleaseFootprint();
        source.ResetToEmpty(false);

        activeDefinition = movedDefinition;
        currentLevel = movedLevel;
        pendingLevel = 0;
        completionDay = 0;
        state = BuildingConstructionState.Completed;
        CancelPreview();
        relocationSource = null;
        ApplyVisualState();

        SaveLoadFeedback.Instance?.ShowMessage($"{activeDefinition.displayName} berhasil dipindahkan");
        SaveManager.Instance?.SaveGame();
        return true;
    }

    void CancelRelocationMode()
    {
        for (int index = 0; index < Registry.Count; index++)
        {
            PropertySite site = Registry[index];
            if (site != null && site.previewIsRelocation)
                site.CancelPreview();
        }
        relocationSource = null;
        SaveLoadFeedback.Instance?.ShowMessage("Relocate dibatalkan");
    }

    void ConfirmDemolish()
    {
        if (state != BuildingConstructionState.Completed || activeDefinition == null || !activeDefinition.canDemolish)
            return;

        string buildingName = activeDefinition.displayName;
        ReleaseFootprint();
        ResetToEmpty(false);
        demolishConfirmationActive = false;
        SaveLoadFeedback.Instance?.ShowMessage($"{buildingName} sudah didemolish");
        SaveManager.Instance?.SaveGame();
    }

    void ResetToEmpty(bool releaseFootprint)
    {
        if (releaseFootprint)
            ReleaseFootprint();
        CancelPreview();
        activeDefinition = null;
        state = BuildingConstructionState.Available;
        currentLevel = 0;
        pendingLevel = 0;
        completionDay = 0;
        ApplyVisualState();
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
        SaveLoadFeedback.Instance?.ShowMessage($"{activeDefinition?.displayName ?? "Bangunan"} Lv.{currentLevel} selesai");
    }

    bool ValidateFixedLocation(BuildingDefinitionSO definition)
    {
        if (state == BuildingConstructionState.Completed && definition == activeDefinition)
            return true;
        float cellSize = fieldArea != null ? fieldArea.CellSize : 1f;
        if (reserveFieldFootprint)
        {
            if (!TryResolveFieldCoordinates(definition, out FieldArea area, out int startX, out int startZ))
                return false;
            if (!area.CanPlaceBuilding(startX, startZ, definition.footprintWidth, definition.footprintDepth))
                return false;
            cellSize = area.CellSize;
        }

        return !HasPhysicalPlacementBlocker(definition, cellSize);
    }

    /// <summary>
    /// NonAlloc overlap hanya menolak object gameplay yang memang memblokir pembangunan.
    /// Terrain/floor biasa tidak ditolak sehingga project tidak membutuhkan layer ground khusus.
    /// </summary>
    bool HasPhysicalPlacementBlocker(BuildingDefinitionSO definition, float cellSize)
    {
        if (definition == null || buildingAnchor == null)
            return true;
        Vector3 halfExtents = new(
            definition.footprintWidth * cellSize * 0.5f,
            placementCheckHeight * 0.5f,
            definition.footprintDepth * cellSize * 0.5f);
        Vector3 center = buildingAnchor.position + buildingAnchor.up * halfExtents.y;
        int count = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            placementOverlapBuffer,
            buildingAnchor.rotation,
            placementCheckMask,
            QueryTriggerInteraction.Ignore);

        for (int index = 0; index < count; index++)
        {
            Collider candidate = placementOverlapBuffer[index];
            placementOverlapBuffer[index] = null;
            if (candidate == null || candidate.transform.IsChildOf(transform))
                continue;
            if (candidate.GetComponentInParent<PlayerController>() != null)
                continue;

            PropertySite otherSite = candidate.GetComponentInParent<PropertySite>();
            if (otherSite != null)
            {
                if (otherSite != this && !otherSite.IsEmpty)
                    return true;
                continue;
            }
            BuildingSite fixedSite = candidate.GetComponentInParent<BuildingSite>();
            if (fixedSite != null && fixedSite.State != BuildingConstructionState.Available)
                return true;
            WorldTree tree = candidate.GetComponentInParent<WorldTree>();
            if (tree != null && tree.IsAvailable)
                return true;
            WorldGatherable gatherable = candidate.GetComponentInParent<WorldGatherable>();
            if (gatherable != null && gatherable.IsAvailable)
                return true;
            if (candidate.GetComponentInParent<PlacedWorldItem>() != null ||
                candidate.GetComponentInParent<BuildingPlacementBlocker>() != null)
                return true;
        }
        return false;
    }

    bool TryReserveFootprint(BuildingDefinitionSO definition)
    {
        if (!reserveFieldFootprint)
            return true;
        return TryResolveFieldCoordinates(definition, out FieldArea area, out int x, out int z) &&
               area.TryPlaceBuilding(x, z, definition.footprintWidth, definition.footprintDepth, siteId);
    }

    void ReleaseFootprint()
    {
        if (reserveFieldFootprint && fieldArea != null)
            fieldArea.TryRemoveBuilding(siteId);
    }

    bool TryResolveFieldCoordinates(
        BuildingDefinitionSO definition,
        out FieldArea area,
        out int startX,
        out int startZ)
    {
        area = fieldArea;
        Vector3 position = buildingAnchor != null ? buildingAnchor.position : transform.position;
        int centerX;
        int centerZ;

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
        startX = centerX - definition.footprintWidth / 2;
        startZ = centerZ - definition.footprintDepth / 2;
        return true;
    }

    bool CanAfford(BuildingLevelDefinition level, out string reason)
        => BuildingCostUtility.CanAfford(level, playerInventory, out reason);

    bool SpendCost(BuildingLevelDefinition level)
        => BuildingCostUtility.TrySpend(level, playerInventory);

    static Dictionary<ItemSO, int> AggregateCosts(BuildingLevelDefinition level)
    {
        Dictionary<ItemSO, int> totals = new();
        if (level?.materialCosts == null)
            return totals;

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
        if (level == null)
            return "data biaya tidak tersedia";
        Dictionary<ItemSO, int> totals = AggregateCosts(level);
        return totals.Count > 0
            ? $"{level.goldCost}G + {totals.Count} material"
            : $"{level.goldCost}G";
    }

    /// <summary>
    /// Membuat daftar resource real-time untuk preview build/upgrade. Merah berarti kurang,
    /// sedangkan hijau berarti jumlah milik player telah memenuhi requirement.
    /// </summary>
    string BuildRequirementLabel(BuildingLevelDefinition level)
        => BuildingCostUtility.BuildRequirementLabel(level, playerInventory);

    void ApplyVisualState()
    {
        DestroyRuntimeCompletedVisual();

        if (availableMarker != null)
            availableMarker.SetActive(IsEmpty);
        if (constructionVisual != null)
            constructionVisual.SetActive(state == BuildingConstructionState.UnderConstruction);

        for (int index = 0; index < completedLevelVisuals.Count; index++)
        {
            GameObject visual = completedLevelVisuals[index];
            if (visual != null)
                visual.SetActive(state == BuildingConstructionState.Completed && index == currentLevel - 1 &&
                                 GetCompletedPrefab(currentLevel) == null);
        }

        GameObject completedPrefab = GetCompletedPrefab(currentLevel);
        if (state == BuildingConstructionState.Completed && completedPrefab != null)
        {
            runtimeCompletedVisual = Instantiate(completedPrefab, buildingAnchor.position, buildingAnchor.rotation, buildingAnchor);
            runtimeCompletedVisual.name = $"{activeDefinition.displayName}_Lv{currentLevel}_Runtime";
        }
    }

    GameObject GetCompletedPrefab(int level)
    {
        return activeDefinition?.GetLevel(level)?.completedPrefab;
    }

    void CreatePreviewVisual(BuildingDefinitionSO definition, int targetLevel, bool valid)
    {
        GameObject source = definition.GetLevel(targetLevel)?.completedPrefab;
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
            previewObject.transform.localScale = new Vector3(definition.footprintWidth, 1f, definition.footprintDepth);
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
            for (int index = 0; index < count; index++)
                materials[index] = previewMaterial;
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
        DestroySafely(previewObject);
        DestroySafely(previewMaterial);
        previewObject = null;
        previewMaterial = null;
    }

    void DestroyRuntimeCompletedVisual()
    {
        DestroySafely(runtimeCompletedVisual);
        runtimeCompletedVisual = null;
    }

    static void DestroySafely(UnityEngine.Object target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    BuildingDefinitionSO FirstCatalogDefinition => catalog != null && catalog.Count > 0 ? catalog.GetAt(0) : null;
    bool IsRelocatableNow => state == BuildingConstructionState.Completed && activeDefinition != null && activeDefinition.canRelocate;
    int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;

    /// <summary>Mengambil snapshot seluruh Property Site aktif untuk SaveManager.</summary>
    public static List<PropertySiteSaveData> CaptureAll()
    {
        List<PropertySiteSaveData> result = new(Registry.Count);
        for (int index = 0; index < Registry.Count; index++)
        {
            PropertySite site = Registry[index];
            if (site == null || string.IsNullOrWhiteSpace(site.siteId))
                continue;
            result.Add(new PropertySiteSaveData
            {
                siteId = site.siteId,
                buildingId = site.activeDefinition != null ? site.activeDefinition.buildingId : string.Empty,
                state = site.state,
                currentLevel = site.currentLevel,
                pendingLevel = site.pendingLevel,
                completionDay = site.completionDay,
                unlocked = site.unlocked
            });
        }
        return result;
    }

    /// <summary>Memulihkan state berdasarkan Site ID; posisi tetap berasal dari scene.</summary>
    public static void RestoreAll(List<PropertySiteSaveData> data)
    {
        if (data == null)
            return;

        for (int dataIndex = 0; dataIndex < data.Count; dataIndex++)
        {
            PropertySiteSaveData saved = data[dataIndex];
            for (int siteIndex = 0; siteIndex < Registry.Count; siteIndex++)
            {
                PropertySite site = Registry[siteIndex];
                if (site == null || !site.MatchesSavedSite(saved.siteId))
                    continue;
                site.Restore(saved);
                break;
            }
        }
    }

    void Restore(PropertySiteSaveData data)
    {
        CancelPreview();
        demolishConfirmationActive = false;
        unlocked = data.unlocked;
        activeDefinition = ResolveDefinition(data.buildingId);
        state = activeDefinition == null ? BuildingConstructionState.Available : data.state;
        currentLevel = activeDefinition != null ? Mathf.Max(0, data.currentLevel) : 0;
        pendingLevel = activeDefinition != null ? Mathf.Max(0, data.pendingLevel) : 0;
        completionDay = activeDefinition != null ? Mathf.Max(0, data.completionDay) : 0;

        // Save yang dibuka setelah construction day langsung diselesaikan.
        if (state == BuildingConstructionState.UnderConstruction && CurrentDay >= completionDay)
            CompleteConstruction();
        else
            ApplyVisualState();
    }

    BuildingDefinitionSO ResolveDefinition(string buildingId)
    {
        BuildingDefinitionSO local = catalog != null ? catalog.FindById(buildingId) : null;
        if (local != null)
            return local;

        // Fallback lintas-site membantu migrasi save bila catalog dipindah ke shared asset lain.
        for (int index = 0; index < Registry.Count; index++)
        {
            BuildingCatalogSO candidateCatalog = Registry[index] != null ? Registry[index].catalog : null;
            BuildingDefinitionSO candidate = candidateCatalog != null ? candidateCatalog.FindById(buildingId) : null;
            if (candidate != null)
                return candidate;
        }
        return null;
    }

    bool MatchesSavedSite(string savedSiteId)
    {
        if (siteId == savedSiteId)
            return true;

        // Migrasi prototype lama: fixed Shed site menjadi Property Site pertama.
        return siteId == "farm-property-01" && savedSiteId == "farm-shed-site-01";
    }

    void OnValidate()
    {
        interactionRadius = Mathf.Max(0.5f, interactionRadius);
        promptHeight = Mathf.Max(0f, promptHeight);
        startingLevel = Mathf.Max(1, startingLevel);
    }

    void OnDrawGizmosSelected()
    {
        Transform anchor = buildingAnchor != null ? buildingAnchor : transform;
        BuildingDefinitionSO definition = activeDefinition != null ? activeDefinition : FirstCatalogDefinition;
        int width = definition != null ? Mathf.Max(1, definition.footprintWidth) : 3;
        int depth = definition != null ? Mathf.Max(1, definition.footprintDepth) : 3;
        Gizmos.color = unlocked
            ? new Color(0.2f, 0.9f, 0.4f, 0.35f)
            : new Color(1f, 0.2f, 0.15f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(anchor.position, anchor.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.up * 0.05f, new Vector3(width, 0.1f, depth));
        Gizmos.DrawWireCube(Vector3.up * 0.5f, new Vector3(width, 1f, depth));
    }
}
