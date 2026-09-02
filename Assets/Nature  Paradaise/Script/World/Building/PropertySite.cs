using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public bool hasPlacementPose;
    public Vector3 placementPosition;
    public Quaternion placementRotation;
}

/// <summary>
/// Slot properti yang dapat menampung bangunan mana pun dari BuildingCatalogSO.
/// Pemilihan dimulai dari marker site, lalu bangunan dapat ditempatkan bebas di luar FieldArea,
/// di-upgrade, direlokasi, didemolish, dan disimpan.
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

    [Header("Placement")]
    [Tooltip("Anchor posisi/rotasi bangunan. Jika kosong memakai transform site.")]
    [SerializeField] Transform buildingAnchor;
    [Tooltip("Preview bangunan mengikuti posisi di depan player dan tidak dibatasi ke anchor awal.")]
    [SerializeField] bool allowFreePlacement = true;
    [Tooltip("Jarak preview dari player ketika Free Placement aktif.")]
    [SerializeField, Min(1f)] float freePlacementDistance = 4f;
    [Tooltip("Ukuran grid snap dunia untuk menjaga posisi bangunan tetap rapi.")]
    [SerializeField, Min(0.25f)] float placementGridSize = 1f;
    [Tooltip("Rotasi preview setiap kali tombol Rotate ditekan.")]
    [SerializeField, Range(15f, 180f)] float rotationStep = 90f;
    [SerializeField] KeyCode rotatePreviewKey = KeyCode.T;
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
    [Tooltip("Marker hanya diperlukan untuk mode prototype lama. Shortcut Build tetap bekerja saat marker disembunyikan.")]
    [SerializeField] bool showAvailableMarker;
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
    bool previewUsesFreePlacement;
    Vector3 previewPlacementPosition;
    Quaternion previewPlacementRotation = Quaternion.identity;
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
    public bool IsPreviewActive => previewActive;

    public static bool TryGetAvailableBuildSite(out PropertySite result)
    {
        result = null;
        for (int index = 0; index < Registry.Count; index++)
        {
            PropertySite candidate = Registry[index];
            if (candidate == null || !candidate.unlocked || !candidate.IsEmpty ||
                candidate.catalog == null || candidate.catalog.Count == 0)
                continue;

            if (result == null || string.CompareOrdinal(candidate.siteId, result.siteId) < 0)
                result = candidate;
        }
        return result != null;
    }

    public static bool HasActivePlacementPreview()
    {
        for (int index = 0; index < Registry.Count; index++)
            if (Registry[index] != null && Registry[index].previewActive)
                return true;
        return false;
    }

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

        if (previewActive && previewUsesFreePlacement)
        {
            UpdateFreePlacementPreview();
            float previewDistance = previewObject != null
                ? Vector3.Distance(playerTransform.position, previewObject.transform.position)
                : freePlacementDistance;
            HandlePreviewInteraction(previewDistance);
            return;
        }

        // Relocate harus dapat dibatalkan walaupun player sudah berjalan menjauhi source site.
        if (relocationSource == this && Input.GetKeyDown(cancelKey))
        {
            CancelRelocationMode();
            return;
        }

        Transform interactionAnchor = IsEmpty ? transform : buildingAnchor;
        float squaredDistance = (playerTransform.position - interactionAnchor.position).sqrMagnitude;
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
        BuildingLevelDefinition level = previewDefinition != null
            ? previewDefinition.GetLevel(previewLevel)
            : null;
        bool canAfford = previewIsRelocation || CanAfford(level, out _);
        string validity = !previewLocationValid
            ? "TEMPAT TIDAK VALID"
            : canAfford ? "VALID" : "RESOURCE KURANG";
        string requirements = previewIsRelocation
            ? "<color=#58D982>Tanpa biaya relocate</color>"
            : BuildRequirementLabel(level);
        string navigation = previewAllowsCatalogNavigation
            ? $"{previousBuildingKey}/{nextBuildingKey}: Pilih  "
            : string.Empty;

        Transform promptAnchor = previewObject != null ? previewObject.transform : buildingAnchor;
        string placementControls = previewUsesFreePlacement
            ? $"Gerakkan player: Pindah preview  {rotatePreviewKey}: Putar\n"
            : string.Empty;
        WorldInteractionPrompt.Request(
            this,
            promptAnchor,
            $"{previewDefinition?.displayName ?? "Building"} Lv.{previewLevel} [{validity}]\n{requirements}\n" +
            $"{placementControls}{navigation}{confirmKey}: Konfirmasi  {cancelKey}: Batal",
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
        previewUsesFreePlacement = allowFreePlacement;
        InitializeFreePlacementPose();
        return RefreshPreview();
    }

    /// <summary>Membuka placement langsung pada pilihan katalog dari Build Menu global.</summary>
    public bool BeginBuildSelection(int catalogIndex)
    {
        int maximumIndex = catalog != null ? Mathf.Max(0, catalog.Count - 1) : 0;
        previewCatalogIndex = Mathf.Clamp(catalogIndex, 0, maximumIndex);
        return BeginBuildSelection();
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
        previewUsesFreePlacement = false;
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
        previewUsesFreePlacement = allowFreePlacement;
        InitializeFreePlacementPose();
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
        if (isNewBuilding && !previewUsesFreePlacement && !TryReserveFootprint(previewDefinition))
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
        if (previewUsesFreePlacement)
            buildingAnchor.SetPositionAndRotation(previewPlacementPosition, previewPlacementRotation);
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
        previewUsesFreePlacement = false;
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
        InitializeFreePlacementRotation();
        RefreshPreview();
    }

    bool RefreshPreview()
    {
        DestroyPreviewVisual();
        if (previewDefinition == null || previewDefinition.GetLevel(previewLevel) == null)
            return false;

        previewActive = true;
        previewLocationValid = ValidatePreviewLocation(previewDefinition);
        CreatePreviewVisual(
            previewDefinition,
            previewLevel,
            previewLocationValid && CanAffordPreview()
        );
        return true;
    }

    void InitializeFreePlacementPose()
    {
        if (!previewUsesFreePlacement || playerTransform == null)
            return;

        InitializeFreePlacementRotation();
        UpdateFreePlacementPose();
    }

    void InitializeFreePlacementRotation()
    {
        if (!previewUsesFreePlacement)
            return;

        // Relocate mempertahankan arah bangunan asal. Bangunan baru selalu memakai
        // arah default definition sehingga hasilnya tidak bergantung pada arah player.
        float yaw = previewIsRelocation && relocationSource != null && relocationSource.buildingAnchor != null
            ? relocationSource.buildingAnchor.eulerAngles.y
            : previewDefinition != null ? previewDefinition.defaultPlacementYaw : 0f;
        previewPlacementRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void UpdateFreePlacementPreview()
    {
        if (Input.GetKeyDown(rotatePreviewKey))
            previewPlacementRotation *= Quaternion.Euler(0f, rotationStep, 0f);

        UpdateFreePlacementPose();
        previewLocationValid = ValidatePreviewLocation(previewDefinition);
        ApplyPreviewTransform();
        ApplyPreviewColor(previewLocationValid && CanAffordPreview());
    }

    bool CanAffordPreview()
    {
        if (previewIsRelocation)
            return true;
        BuildingLevelDefinition level = previewDefinition?.GetLevel(previewLevel);
        return CanAfford(level, out _);
    }

    void UpdateFreePlacementPose()
    {
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 candidate = playerTransform.position + forward * freePlacementDistance;
        candidate.x = Mathf.Round(candidate.x / placementGridSize) * placementGridSize;
        candidate.z = Mathf.Round(candidate.z / placementGridSize) * placementGridSize;
        candidate.y = buildingAnchor.position.y;
        previewPlacementPosition = candidate;
    }

    void ApplyPreviewTransform()
    {
        if (previewObject == null)
            return;

        float verticalOffset = previewDefinition != null &&
                               previewDefinition.GetLevel(previewLevel)?.completedPrefab == null
            ? 0.5f
            : 0f;
        previewObject.transform.SetPositionAndRotation(
            previewPlacementPosition + Vector3.up * verticalOffset,
            previewPlacementRotation
        );
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
        if (!previewUsesFreePlacement && !TryReserveFootprint(movedDefinition))
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Destination site terhalang");
            return false;
        }

        int movedLevel = source.currentLevel;
        Vector3 destinationPosition = previewUsesFreePlacement
            ? previewPlacementPosition
            : buildingAnchor.position;
        Quaternion destinationRotation = previewUsesFreePlacement
            ? previewPlacementRotation
            : buildingAnchor.rotation;
        source.ReleaseFootprint();
        source.ResetToEmpty(false);

        buildingAnchor.SetPositionAndRotation(destinationPosition, destinationRotation);
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

    bool ValidatePreviewLocation(BuildingDefinitionSO definition)
    {
        if (previewUsesFreePlacement)
        {
            // Farm Field selalu eksklusif untuk tanah pertanian. Pengecekan sampling
            // mencakup seluruh footprint, bukan hanya titik tengah bangunan.
            if (OverlapsAnyFieldArea(definition, previewPlacementPosition, previewPlacementRotation))
                return false;
            return !HasPhysicalPlacementBlocker(
                definition,
                1f,
                previewPlacementPosition,
                previewPlacementRotation
            );
        }

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

        return !HasPhysicalPlacementBlocker(definition, cellSize, buildingAnchor.position, buildingAnchor.rotation);
    }

    static bool OverlapsAnyFieldArea(
        BuildingDefinitionSO definition,
        Vector3 position,
        Quaternion rotation)
    {
        if (definition == null)
            return true;

        float halfWidth = Mathf.Max(0.5f, definition.footprintWidth * 0.5f);
        float halfDepth = Mathf.Max(0.5f, definition.footprintDepth * 0.5f);
        const float sampleSpacing = 0.5f;
        int widthSamples = Mathf.Max(1, Mathf.CeilToInt(halfWidth * 2f / sampleSpacing));
        int depthSamples = Mathf.Max(1, Mathf.CeilToInt(halfDepth * 2f / sampleSpacing));

        for (int x = 0; x <= widthSamples; x++)
        {
            float localX = Mathf.Lerp(-halfWidth, halfWidth, x / (float)widthSamples);
            for (int z = 0; z <= depthSamples; z++)
            {
                float localZ = Mathf.Lerp(-halfDepth, halfDepth, z / (float)depthSamples);
                Vector3 sample = position + rotation * new Vector3(localX, 0f, localZ);
                if (FieldArea.TryGetAt(sample, out _, out _, out _))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// NonAlloc overlap hanya menolak object gameplay yang memang memblokir pembangunan.
    /// Terrain/floor biasa tidak ditolak sehingga project tidak membutuhkan layer ground khusus.
    /// </summary>
    bool HasPhysicalPlacementBlocker(
        BuildingDefinitionSO definition,
        float cellSize,
        Vector3 position,
        Quaternion rotation)
    {
        if (definition == null || buildingAnchor == null)
            return true;
        Vector3 halfExtents = new(
            definition.footprintWidth * cellSize * 0.5f,
            placementCheckHeight * 0.5f,
            definition.footprintDepth * cellSize * 0.5f);
        Vector3 center = position + rotation * Vector3.up * halfExtents.y;
        int count = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            placementOverlapBuffer,
            rotation,
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
            availableMarker.SetActive(IsEmpty && showAvailableMarker);
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
            try
            {
                runtimeCompletedVisual = Instantiate(
                    completedPrefab,
                    buildingAnchor.position,
                    buildingAnchor.rotation,
                    buildingAnchor
                );
                runtimeCompletedVisual.name = $"{activeDefinition.displayName}_Lv{currentLevel}_Runtime";
            }
            catch (InvalidCastException exception)
            {
                Debug.LogWarning($"[BUILDING] Prefab final {activeDefinition.displayName} tidak valid. {exception.Message}");
            }
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

        Vector3 position = previewUsesFreePlacement ? previewPlacementPosition : buildingAnchor.position;
        Quaternion rotation = previewUsesFreePlacement ? previewPlacementRotation : buildingAnchor.rotation;
        bool usedFallbackPrimitive = false;

        if (source != null)
        {
            try
            {
                previewObject = Instantiate(source, position, rotation);
            }
            catch (InvalidCastException exception)
            {
                Debug.LogWarning($"[BUILDING] Prefab {definition.displayName} tidak valid; memakai preview cube. {exception.Message}");
            }
        }

        if (previewObject == null)
        {
            previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            usedFallbackPrimitive = true;
        }
        previewObject.name = $"BuildingPreview_{definition.displayName}_Lv{targetLevel}";
        previewObject.SetActive(true);

        if (usedFallbackPrimitive)
        {
            previewObject.transform.SetPositionAndRotation(position + Vector3.up * 0.5f, rotation);
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
                unlocked = site.unlocked,
                hasPlacementPose = true,
                placementPosition = site.buildingAnchor.position,
                placementRotation = site.buildingAnchor.rotation
            });
        }
        return result;
    }

    /// <summary>Memulihkan state dan pose free placement berdasarkan Site ID.</summary>
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
        if (data.hasPlacementPose && activeDefinition != null)
            buildingAnchor.SetPositionAndRotation(data.placementPosition, data.placementRotation);

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
        freePlacementDistance = Mathf.Max(1f, freePlacementDistance);
        placementGridSize = Mathf.Max(0.25f, placementGridSize);
        rotationStep = Mathf.Clamp(rotationStep, 15f, 180f);
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

/// <summary>
/// Build Menu global: B membuka katalog tanpa mengharuskan player mendatangi marker.
/// PropertySite tetap menjadi slot state internal untuk construction dan Save/Load.
/// </summary>
[DisallowMultipleComponent]
public sealed class ConstructionShortcutMenu : MonoBehaviour
{
    [SerializeField] KeyCode openKey = KeyCode.B;
    [SerializeField] KeyCode confirmKey = KeyCode.Return;

    readonly List<Button> buildingButtons = new();
    Inventory inventory;
    InventoryHotbarUI hotbar;
    PlayerController movement;
    TimeManager timeManager;
    PropertySite selectedSite;
    BuildingCatalogSO catalog;
    GameObject canvasObject;
    GameObject panelObject;
    TMP_Text detailText;
    TMP_Text footerText;
    int selectedIndex;
    bool isOpen;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        hotbar = GetComponent<InventoryHotbarUI>();
        movement = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!isOpen)
        {
            if (Input.GetKeyDown(openKey) && !IsSeedSelected() &&
                (movement == null || !movement.IsMovementLocked) &&
                !PropertySite.HasActivePlacementPreview())
                Open();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(openKey))
            Close();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            Select(selectedIndex - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Select(selectedIndex + 1);
        else if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(KeyCode.C))
            StartPlacement();
    }

    bool IsSeedSelected()
    {
        if (hotbar == null)
            hotbar = GetComponent<InventoryHotbarUI>();
        return hotbar != null && hotbar.SelectedItem != null && hotbar.SelectedItem.IsSeed;
    }

    void Open()
    {
        if (!PropertySite.TryGetAvailableBuildSite(out selectedSite))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada slot bangunan kosong");
            return;
        }

        catalog = selectedSite.Catalog;
        if (catalog == null || catalog.Count == 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Building Catalog kosong");
            return;
        }

        EnsureUI();
        RebuildButtons();
        isOpen = true;
        panelObject.SetActive(true);
        movement?.AcquireMovementLock(this);
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
        timeManager?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        Select(0);
    }

    void Close()
    {
        isOpen = false;
        if (panelObject != null)
            panelObject.SetActive(false);
        movement?.ReleaseMovementLock(this);
        timeManager?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    void StartPlacement()
    {
        if (selectedSite == null || catalog == null ||
            selectedIndex < 0 || selectedIndex >= catalog.Count)
            return;

        if (!selectedSite.BeginBuildSelection(selectedIndex))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Placement bangunan gagal dimulai");
            return;
        }
        Close();
    }

    void Select(int index)
    {
        if (catalog == null || catalog.Count == 0)
            return;

        selectedIndex = (index % catalog.Count + catalog.Count) % catalog.Count;
        for (int buttonIndex = 0; buttonIndex < buildingButtons.Count; buttonIndex++)
        {
            Image image = buildingButtons[buttonIndex].GetComponent<Image>();
            if (image != null)
                image.color = buttonIndex == selectedIndex
                    ? new Color(0.31f, 0.55f, 0.27f, 1f)
                    : new Color(0.30f, 0.20f, 0.12f, 1f);
        }

        BuildingDefinitionSO definition = catalog.GetAt(selectedIndex);
        BuildingLevelDefinition level = definition?.GetLevel(1);
        detailText.text = definition == null
            ? "Data bangunan tidak tersedia"
            : $"<b>{definition.displayName}</b>\n" +
              $"Ukuran: {definition.footprintWidth} x {definition.footprintDepth}\n" +
              $"Konstruksi: {Mathf.Max(0, level?.constructionDays ?? 0)} hari\n\n" +
              BuildingCostUtility.BuildRequirementLabel(level, inventory);
    }

    void EnsureUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject("ConstructionMenuCanvas_Runtime", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 460;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("ConstructionMenu", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900f, 540f);
        panelObject = panel.gameObject;
        AddImage(panel, new Color(0.12f, 0.075f, 0.035f, 0.98f));

        TMP_Text title = CreateText("Title", panel, "CARPENTER — BUILD MENU", 32f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(20f, -18f), new Vector2(-20f, -76f));

        RectTransform cards = CreateRect("BuildingCards", panel);
        cards.anchorMin = new Vector2(0f, 1f);
        cards.anchorMax = new Vector2(1f, 1f);
        cards.pivot = new Vector2(0.5f, 1f);
        cards.anchoredPosition = new Vector2(0f, -88f);
        cards.sizeDelta = new Vector2(-40f, 150f);
        HorizontalLayoutGroup layout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        detailText = CreateText("BuildingDetails", panel, string.Empty, 23f, TextAlignmentOptions.TopLeft);
        SetRect(detailText.rectTransform, new Vector2(44f, -260f), new Vector2(-44f, -450f));
        detailText.textWrappingMode = TextWrappingModes.Normal;

        footerText = CreateText("Footer", panel,
            "A/D atau Panah: Pilih   Enter/C: Mulai Placement   B/Esc: Tutup", 19f,
            TextAlignmentOptions.Center);
        SetRect(footerText.rectTransform, new Vector2(24f, -474f), new Vector2(-24f, -520f));
        panelObject.SetActive(false);
    }

    void RebuildButtons()
    {
        for (int index = 0; index < buildingButtons.Count; index++)
            if (buildingButtons[index] != null)
                Destroy(buildingButtons[index].gameObject);
        buildingButtons.Clear();

        Transform cards = panelObject.transform.Find("BuildingCards");
        for (int index = 0; index < catalog.Count; index++)
        {
            int capturedIndex = index;
            BuildingDefinitionSO definition = catalog.GetAt(index);
            RectTransform card = CreateRect($"Building_{index}", cards);
            Image background = AddImage(card, new Color(0.30f, 0.20f, 0.12f, 1f));
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => Select(capturedIndex));
            TMP_Text label = CreateText("Label", card,
                definition != null ? definition.displayName : "Kosong", 24f, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            buildingButtons.Add(button);
        }
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject created = new(name, typeof(RectTransform));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Image AddImage(RectTransform parent, Color color)
    {
        Image image = parent.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = new Color(0.96f, 0.88f, 0.68f, 1f);
        text.alignment = alignment;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    static void SetRect(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(minOffset.x, maxOffset.y);
        rect.offsetMax = new Vector2(maxOffset.x, minOffset.y);
    }

    void OnDisable()
    {
        if (isOpen)
            Close();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        Inventory playerInventory = FindFirstObjectByType<Inventory>();
        if (playerInventory != null && playerInventory.GetComponent<ConstructionShortcutMenu>() == null)
            playerInventory.gameObject.AddComponent<ConstructionShortcutMenu>();
    }
}
