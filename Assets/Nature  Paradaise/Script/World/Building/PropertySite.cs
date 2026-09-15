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
    public bool hasPlacementPose;
    public Vector3 placementPosition;
    public Quaternion placementRotation;
}

/// <summary>
/// Controller state satu bangunan dari BuildingCatalogSO. Instance global dibuat oleh Build Menu
/// milik Player, lalu menyimpan konstruksi, posisi, upgrade, relocation, dan demolition.
/// </summary>
[DisallowMultipleComponent]
public sealed class PropertySite : MonoBehaviour
{
    const string GlobalSitePrefix = "farm-world-building-";
    const string LegacyFishPondSiteId = "farm-fish-pond-builder-01";
    static readonly List<PropertySite> Registry = new();
    static PropertySite relocationSource;

    [Header("Identity")]
    [Tooltip("ID lokasi permanen untuk Save/Load. Harus unik dan tidak berubah setelah production save.")]
    [SerializeField] string siteId = "farm-property-01";
    [Tooltip("Daftar jenis bangunan yang dapat dipilih pada site kosong.")]
    [SerializeField] BuildingCatalogSO catalog;
    [SerializeField] bool unlocked = true;
    [SerializeField, HideInInspector] bool globalBuildInstance;
    [SerializeField] bool startsCompleted;
    [Tooltip("Bangunan awal hanya dipakai bila Starts Completed aktif.")]
    [SerializeField] BuildingDefinitionSO startingBuilding;
    [SerializeField, Min(1)] int startingLevel = 1;

    [Header("Placement")]
    [Tooltip("Anchor posisi/rotasi bangunan. Jika kosong memakai transform site.")]
    [SerializeField] Transform buildingAnchor;
    [Tooltip("Mode opsional untuk site khusus. Nonaktif berarti bangunan terkunci pada anchor site.")]
    [SerializeField] bool allowFreePlacement;
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
    [Tooltip("Menampilkan lokasi yang disediakan untuk membangun selama site masih kosong.")]
    [SerializeField] bool showAvailableMarker = true;
    [Tooltip("Warna marker lokasi build. Material scene asli tidak diubah.")]
    [SerializeField] Color availableMarkerColor = new(0.18f, 0.9f, 0.32f, 0.58f);
    [Tooltip("Visual selama konstruksi berlangsung.")]
    [SerializeField] GameObject constructionVisual;
    [Tooltip("Visual fallback per level apabila definition tidak mempunyai Completed Prefab.")]
    [SerializeField] List<GameObject> completedLevelVisuals = new();
    [SerializeField] Color validPreviewColor = new(0.2f, 0.95f, 0.4f, 0.48f);
    [SerializeField] Color invalidPreviewColor = new(1f, 0.18f, 0.12f, 0.55f);

    [Header("Interaction")]
    [Tooltip("Membuka pilihan build atau menerima relocate pada site kosong.")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode previousBuildingKey = KeyCode.Q;
    [SerializeField] KeyCode nextBuildingKey = KeyCode.R;
    [SerializeField] KeyCode confirmKey = KeyCode.C;
    [SerializeField] KeyCode debugConfirmKey = KeyCode.F9;
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
    bool previewRelocatesSelf;
    bool previewAllowsCatalogNavigation;
    bool previewDebugInstant;
    int previewStartedFrame = -1;
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
    Material availableMarkerMaterial;
    GameObject runtimeCompletedVisual;
    readonly Collider[] placementOverlapBuffer = new Collider[48];

    public string SiteId => siteId;
    public static IReadOnlyList<PropertySite> ActiveSites => Registry;
    public Transform BuildingAnchor => buildingAnchor != null ? buildingAnchor : transform;
    public BuildingCatalogSO Catalog => catalog;
    public BuildingDefinitionSO ActiveDefinition => activeDefinition;
    public BuildingConstructionState State => state;
    public int CurrentLevel => currentLevel;
    public int CompletionDay => completionDay;
    public bool IsUnlocked => unlocked;
    public bool IsEmpty => state == BuildingConstructionState.Available && activeDefinition == null;
    public bool IsPreviewActive => previewActive;
    public bool IsGlobalBuildInstance => globalBuildInstance;
    public static bool DebugShortcutsEnabled => Application.isEditor || Debug.isDebugBuild;

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

    /// <summary>
    /// Menyediakan controller dunia kosong untuk satu placement dari Build Menu milik Player.
    /// Controller yang sudah berisi bangunan tetap hidup sebagai pemilik state/save instance itu.
    /// </summary>
    public static PropertySite GetOrCreateGlobalBuildSite(BuildingCatalogSO sharedCatalog, Vector3 playerPosition)
    {
        if (sharedCatalog == null || sharedCatalog.Count == 0)
            return null;

        for (int index = 0; index < Registry.Count; index++)
        {
            PropertySite candidate = Registry[index];
            if (candidate == null || !candidate.globalBuildInstance || !candidate.IsEmpty || candidate.previewActive)
                continue;
            candidate.catalog = sharedCatalog;
            candidate.transform.position = playerPosition;
            return candidate;
        }

        return CreateGlobalBuildSite(NextGlobalSiteId(), sharedCatalog, playerPosition);
    }

    static PropertySite CreateGlobalBuildSite(string id, BuildingCatalogSO sharedCatalog, Vector3 position)
    {
        GameObject root = GameObject.Find("PlacedFarmBuildings_Runtime");
        if (root == null)
        {
            root = new GameObject("PlacedFarmBuildings_Runtime");
            GameObject buildings = GameObject.Find("30_WORLD/Buildings");
            if (buildings != null)
                root.transform.SetParent(buildings.transform, false);
        }

        GameObject instance = new($"FarmBuilding_{id}");
        instance.transform.SetParent(root.transform, false);
        instance.transform.position = position;
        PropertySite site = instance.AddComponent<PropertySite>();
        site.siteId = id;
        site.catalog = sharedCatalog;
        site.globalBuildInstance = true;
        site.allowFreePlacement = true;
        site.reserveFieldFootprint = false;
        site.fieldArea = null;
        site.buildingAnchor = instance.transform;
        site.showAvailableMarker = false;
        site.availableMarker = null;
        site.unlocked = true;
        site.startsCompleted = false;
        site.activeDefinition = null;
        site.state = BuildingConstructionState.Available;
        site.currentLevel = 0;
        site.ApplyVisualState();
        return site;
    }

    static string NextGlobalSiteId()
    {
        int next = 1;
        for (int index = 0; index < Registry.Count; index++)
        {
            string id = Registry[index] != null ? Registry[index].siteId : null;
            if (string.IsNullOrEmpty(id) || !id.StartsWith(GlobalSitePrefix, StringComparison.Ordinal))
                continue;
            if (int.TryParse(id.Substring(GlobalSitePrefix.Length), out int value))
                next = Mathf.Max(next, value + 1);
        }
        return $"{GlobalSitePrefix}{next:D4}";
    }

    static bool IsDynamicWorldSiteId(string id) =>
        !string.IsNullOrWhiteSpace(id) &&
        (id.StartsWith(GlobalSitePrefix, StringComparison.Ordinal) || id == LegacyFishPondSiteId);

    void Awake()
    {
        if (buildingAnchor == null)
            buildingAnchor = transform;

        playerInventory = FindFirstObjectByType<Inventory>();
        playerTransform = playerInventory != null ? playerInventory.transform : null;

        ConfigureAvailableMarkerVisual();

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

    void OnDestroy()
    {
        DestroySafely(availableMarkerMaterial);
        availableMarkerMaterial = null;
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

        // Instance kosong ini hanya dikontrol oleh Build Menu global pada Player.
        if (globalBuildInstance && IsEmpty)
            return;

        // Relocate harus dapat dibatalkan walaupun player sudah berjalan menjauhi source site.
        if (relocationSource == this && Input.GetKeyDown(cancelKey))
        {
            CancelRelocationMode();
            return;
        }

        Transform interactionAnchor = IsEmpty ? transform : buildingAnchor;
        float squaredDistance = (playerTransform.position - interactionAnchor.position).sqrMagnitude;
        if (!previewActive && !demolishConfirmationActive && !PlayerInteractionTarget.Contains(playerTransform, interactionAnchor))
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
            if (PlayerInteractionTarget.Press(playerTransform, buildingAnchor, interactKey))
                BeginRelocationPreview();
            else if (Input.GetKeyDown(cancelKey))
                CancelRelocationMode();
            return;
        }

        string firstName = FirstCatalogDefinition != null ? FirstCatalogDefinition.displayName : "Catalog kosong";
        string actionLabel = allowFreePlacement
            ? $"{interactKey}: Buka Construction Menu ({firstName})"
            : $"{interactKey}: Build di sini ({firstName})";
        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            actionLabel,
            distance,
            promptHeight
        );
        if (!PlayerInteractionTarget.Press(playerTransform, buildingAnchor, interactKey))
            return;

        PlayerBuildMenu menu = playerInventory != null
            ? playerInventory.GetComponent<PlayerBuildMenu>()
            : null;
        if (menu != null)
            menu.OpenForSite(this);
        else
            BeginBuildSelection();
    }

    void HandleConstructionInteraction(float distance)
    {
        int remaining = Mathf.Max(0, completionDay - CurrentDay);
        string debugFinish = DebugShortcutsEnabled ? "  F9: DEBUG langsung selesai" : string.Empty;
        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{activeDefinition?.displayName ?? "Construction"}: {remaining} hari lagi{debugFinish}",
            distance,
            promptHeight
        );
        if (DebugShortcutsEnabled && Input.GetKeyDown(debugConfirmKey))
        {
            CompleteConstruction();
            SaveManager.Instance?.SaveGame();
        }
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
        string debugUpgrade = DebugShortcutsEnabled && activeDefinition != null && activeDefinition.HasUpgradeAfter(currentLevel)
            ? $"  {debugConfirmKey}: DEBUG Upgrade Instan"
            : string.Empty;

        WorldInteractionPrompt.Request(
            this,
            buildingAnchor,
            $"{activeDefinition?.displayName ?? "Building"} Lv.{currentLevel}  {upgrade}{relocate}{demolish}{debugUpgrade}",
            distance,
            promptHeight
        );

        if (DebugShortcutsEnabled && Input.GetKeyDown(debugConfirmKey) &&
            activeDefinition != null && activeDefinition.HasUpgradeAfter(currentLevel))
        {
            if (BeginUpgradePreview()) ConfirmPreview(true);
        }
        else if (Input.GetKeyDown(upgradeKey) && activeDefinition != null && activeDefinition.HasUpgradeAfter(currentLevel))
            BeginUpgradePreview();
        else if (Input.GetKeyDown(relocateKey) && activeDefinition != null && activeDefinition.canRelocate)
        {
            if (globalBuildInstance && allowFreePlacement)
                BeginSelfRelocationPreview();
            else
                BeginRelocationMode();
        }
        else if (Input.GetKeyDown(demolishKey) && activeDefinition != null && activeDefinition.canDemolish)
            demolishConfirmationActive = true;
    }

    void HandlePreviewInteraction(float distance)
    {
        BuildingLevelDefinition level = previewDefinition != null
            ? previewDefinition.GetLevel(previewLevel)
            : null;
        bool canAfford = previewIsRelocation || previewDebugInstant || CanAfford(level, out _);
        string validity = !previewLocationValid
            ? "TEMPAT TIDAK VALID"
            : canAfford ? "VALID" : "RESOURCE KURANG";
        string requirements = previewIsRelocation
            ? "<color=#58D982>Tanpa biaya relocate</color>"
            : previewDebugInstant
                ? "<color=#58D982>DEBUG: biaya 0, waktu 0</color>"
                : BuildRequirementLabel(level);
        string navigation = previewAllowsCatalogNavigation
            ? $"{previousBuildingKey}/{nextBuildingKey}: Pilih  "
            : string.Empty;

        Transform promptAnchor = previewObject != null ? previewObject.transform : buildingAnchor;
        int previewYaw = Mathf.RoundToInt(Mathf.Repeat(previewPlacementRotation.eulerAngles.y, 360f));
        string placementControls = previewUsesFreePlacement
            ? $"Gerakkan player: Pindah preview  {rotatePreviewKey}: Putar  Arah: {previewYaw} derajat\n"
            : string.Empty;
        string debugControls = DebugShortcutsEnabled && !previewDebugInstant && !previewIsRelocation
            ? $"  {debugConfirmKey}: Aktifkan DEBUG Gratis"
            : string.Empty;
        WorldInteractionPrompt.Request(
            this,
            promptAnchor,
            $"{previewDefinition?.displayName ?? "Building"} Lv.{previewLevel} [{validity}]\n{requirements}\n" +
            $"{placementControls}{navigation}{confirmKey}: Konfirmasi{debugControls}  {cancelKey}: Batal",
            distance,
            promptHeight
        );

        // Tombol C/Enter dari menu pemilihan tidak boleh sekaligus mengonfirmasi preview
        // pada frame yang sama; player harus melihat siluet sebelum membangun.
        if (Time.frameCount == previewStartedFrame)
            return;

        if (previewAllowsCatalogNavigation && Input.GetKeyDown(previousBuildingKey))
            CycleCatalog(-1);
        else if (previewAllowsCatalogNavigation && Input.GetKeyDown(nextBuildingKey))
            CycleCatalog(1);
        else if (DebugShortcutsEnabled && Input.GetKeyDown(debugConfirmKey))
        {
            previewDebugInstant = true;
            ApplyPreviewColor(previewLocationValid);
            SaveLoadFeedback.Instance?.ShowMessage("DEBUG konstruksi gratis aktif. Pilih lokasi lalu tekan C.");
        }
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
        previewRelocatesSelf = false;
        previewDebugInstant = false;
        previewUsesFreePlacement = allowFreePlacement;
        InitializeFreePlacementPose();
        return RefreshPreview();
    }

    /// <summary>Membuka placement langsung pada pilihan katalog dari menu site.</summary>
    public bool BeginBuildSelection(int catalogIndex)
    {
        int maximumIndex = catalog != null ? Mathf.Max(0, catalog.Count - 1) : 0;
        previewCatalogIndex = Mathf.Clamp(catalogIndex, 0, maximumIndex);
        return BeginBuildSelection();
    }

    /// <summary>Memulai placement dari Player Build Menu pada level yang dipilih.</summary>
    public bool BeginBuildSelection(int catalogIndex, int targetLevel, bool debugFree)
    {
        if (!IsEmpty || catalog == null || catalog.Count == 0)
            return false;
        previewCatalogIndex = Mathf.Clamp(catalogIndex, 0, catalog.Count - 1);
        previewDefinition = catalog.GetAt(previewCatalogIndex);
        previewLevel = Mathf.Max(1, targetLevel);
        if (previewDefinition == null || previewDefinition.GetLevel(previewLevel) == null)
            return false;
        previewAllowsCatalogNavigation = false;
        previewIsRelocation = false;
        previewRelocatesSelf = false;
        previewDebugInstant = debugFree && DebugShortcutsEnabled;
        previewUsesFreePlacement = allowFreePlacement;
        InitializeFreePlacementPose();
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
        previewRelocatesSelf = false;
        previewDebugInstant = false;
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

    Vector3 selfRelocationOriginalPosition;
    Quaternion selfRelocationOriginalRotation;
    FieldArea selfRelocationOriginalField;

    bool BeginSelfRelocationPreview()
    {
        if (!IsRelocatableNow || !allowFreePlacement)
            return false;

        selfRelocationOriginalPosition = buildingAnchor.position;
        selfRelocationOriginalRotation = buildingAnchor.rotation;
        selfRelocationOriginalField = fieldArea;
        ReleaseFootprint();
        previewDefinition = activeDefinition;
        previewLevel = currentLevel;
        previewAllowsCatalogNavigation = false;
        previewIsRelocation = true;
        previewRelocatesSelf = true;
        previewDebugInstant = false;
        previewUsesFreePlacement = true;
        if (runtimeCompletedVisual != null)
            runtimeCompletedVisual.SetActive(false);
        InitializeFreePlacementPose();
        if (RefreshPreview())
            return true;
        CancelPreview();
        return false;
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
        previewRelocatesSelf = false;
        previewDebugInstant = false;
        previewUsesFreePlacement = allowFreePlacement;
        InitializeFreePlacementPose();
        return RefreshPreview();
    }

    /// <summary>Menjalankan transaksi build/upgrade atau transfer relocate dari preview aktif.</summary>
    public bool ConfirmPreview(bool debugInstant = false)
    {
        if (!previewActive || !previewLocationValid || previewDefinition == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Lokasi bangunan tidak valid");
            return false;
        }

        if (previewIsRelocation)
            return previewRelocatesSelf ? ConfirmSelfRelocation() : ConfirmRelocation();

        bool useDebug = (debugInstant || previewDebugInstant) && DebugShortcutsEnabled;
        BuildingLevelDefinition targetLevel = previewDefinition.GetLevel(previewLevel);
        string reason = null;
        if (targetLevel == null || (!useDebug && !CanAfford(targetLevel, out reason)))
        {
            SaveLoadFeedback.Instance?.ShowMessage(reason ?? "Resource tidak cukup");
            return false;
        }

        bool isNewBuilding = IsEmpty;
        bool needsFieldReservation = !previewUsesFreePlacement ||
                                     previewDefinition.placementArea == BuildingPlacementArea.FieldOnly ||
                                     (previewDefinition.placementArea == BuildingPlacementArea.Anywhere && fieldArea != null);
        if (isNewBuilding && needsFieldReservation && !TryReserveFootprint(
                previewDefinition,
                previewUsesFreePlacement ? previewPlacementPosition : buildingAnchor.position))
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Footprint bangunan terhalang");
            return false;
        }

        if (!useDebug && !SpendCost(targetLevel))
        {
            if (isNewBuilding)
                ReleaseFootprint();
            SaveLoadFeedback.Instance?.ShowMessage("Transaksi konstruksi gagal");
            return false;
        }

        activeDefinition = previewDefinition;
        pendingLevel = previewLevel;
        completionDay = CurrentDay + (useDebug ? 0 : Mathf.Max(0, targetLevel.constructionDays));
        state = BuildingConstructionState.UnderConstruction;
        if (previewUsesFreePlacement)
            buildingAnchor.SetPositionAndRotation(previewPlacementPosition, previewPlacementRotation);
        CancelPreview();
        ApplyVisualState();

        if (useDebug || targetLevel.constructionDays <= 0)
        {
            CompleteConstruction();
            if (useDebug)
                SaveLoadFeedback.Instance?.ShowMessage($"[DEBUG] {activeDefinition.displayName} Lv.{currentLevel} gratis dan langsung selesai");
        }
        else
            SaveLoadFeedback.Instance?.ShowMessage($"{activeDefinition.displayName} selesai hari ke-{completionDay}");

        SaveManager.Instance?.SaveGame();
        return true;
    }

    /// <summary>Membatalkan preview tanpa mengubah inventory, gold, atau occupancy.</summary>
    public void CancelPreview()
    {
        if (previewRelocatesSelf)
        {
            buildingAnchor.SetPositionAndRotation(selfRelocationOriginalPosition, selfRelocationOriginalRotation);
            fieldArea = selfRelocationOriginalField;
            if (activeDefinition != null)
                TryReserveFootprint(activeDefinition, selfRelocationOriginalPosition);
            if (runtimeCompletedVisual != null)
                runtimeCompletedVisual.SetActive(true);
        }
        previewActive = false;
        previewIsRelocation = false;
        previewRelocatesSelf = false;
        previewAllowsCatalogNavigation = false;
        previewDebugInstant = false;
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
        previewStartedFrame = Time.frameCount;
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
        {
            float nextYaw = Mathf.Repeat(previewPlacementRotation.eulerAngles.y + rotationStep, 360f);
            previewPlacementRotation = Quaternion.Euler(0f, nextYaw, 0f);
            SaveLoadFeedback.Instance?.ShowMessage($"Arah bangunan: {Mathf.RoundToInt(nextYaw)} derajat");
        }

        UpdateFreePlacementPose();
        previewLocationValid = ValidatePreviewLocation(previewDefinition);
        ApplyPreviewTransform();
        ApplyPreviewColor(previewLocationValid && CanAffordPreview());
    }

    bool CanAffordPreview()
    {
        if (previewIsRelocation)
            return true;
        if (previewDebugInstant && DebugShortcutsEnabled)
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
        candidate.y = ResolvePlacementHeight(candidate, buildingAnchor.position.y);
        if (previewDefinition?.placementArea == BuildingPlacementArea.FieldOnly)
        {
            FieldArea targetField = fieldArea;
            if (targetField != null && targetField.WorldToGrid(candidate, out int fieldX, out int fieldZ))
                candidate = targetField.GridToWorld(fieldX, fieldZ);
            else if (FieldArea.TryGetAt(candidate, out targetField, out fieldX, out fieldZ))
            {
                fieldArea = targetField;
                candidate = targetField.GridToWorld(fieldX, fieldZ);
            }
        }
        else if (previewDefinition?.placementArea == BuildingPlacementArea.Anywhere)
        {
            if (FieldArea.TryGetAt(candidate, out FieldArea targetField, out int fieldX, out int fieldZ))
            {
                fieldArea = targetField;
                candidate = targetField.GridToWorld(fieldX, fieldZ);
            }
            else
                fieldArea = null;
        }
        previewPlacementPosition = candidate;
    }

    static float ResolvePlacementHeight(Vector3 position, float fallbackHeight)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int index = 0; index < terrains.Length; index++)
        {
            Terrain terrain = terrains[index];
            if (terrain == null || terrain.terrainData == null)
                continue;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x < origin.x || position.x > origin.x + size.x ||
                position.z < origin.z || position.z > origin.z + size.z)
                continue;

            return terrain.SampleHeight(position) + origin.y;
        }
        return fallbackHeight;
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
        bool needsFieldReservation = !previewUsesFreePlacement ||
                                     movedDefinition.placementArea == BuildingPlacementArea.FieldOnly ||
                                     (movedDefinition.placementArea == BuildingPlacementArea.Anywhere && fieldArea != null);
        if (needsFieldReservation && !TryReserveFootprint(
                movedDefinition,
                previewUsesFreePlacement ? previewPlacementPosition : buildingAnchor.position))
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

        AnimalHome.Transfer(source.SiteId, this);
        FishPondService.TransferSite(source.SiteId, SiteId);
        SaveLoadFeedback.Instance?.ShowMessage($"{activeDefinition.displayName} berhasil dipindahkan");
        SaveManager.Instance?.SaveGame();
        return true;
    }

    bool ConfirmSelfRelocation()
    {
        if (!previewRelocatesSelf || activeDefinition == null || !previewLocationValid)
            return false;
        if (!TryReserveFootprint(activeDefinition, previewPlacementPosition))
        {
            previewLocationValid = false;
            ApplyPreviewColor(false);
            SaveLoadFeedback.Instance?.ShowMessage("Lokasi pindah terhalang");
            return false;
        }

        previewRelocatesSelf = false;
        buildingAnchor.SetPositionAndRotation(previewPlacementPosition, previewPlacementRotation);
        CancelPreview();
        if (runtimeCompletedVisual != null)
            runtimeCompletedVisual.SetActive(true);
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
        if (FishPondService.HasFishForSite(siteId))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Ambil seluruh ikan dari Fish Pond sebelum demolish");
            demolishConfirmationActive = false;
            return;
        }
        if (AnimalHome.HasResidents(siteId) || (AnimalHome.Find(siteId)?.Fodder ?? 0) > 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Pindahkan hewan dan ambil sisa pakan sebelum demolish");
            demolishConfirmationActive = false;
            return;
        }
        if (state != BuildingConstructionState.Completed || activeDefinition == null || !activeDefinition.canDemolish)
            return;

        string buildingName = activeDefinition.displayName;
        bool wasFishPond = activeDefinition.buildingId == "building.fish-pond";
        ReleaseFootprint();
        ResetToEmpty(false);
        if (wasFishPond) FishPondService.RemoveForSite(siteId);
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
            if (definition.placementArea == BuildingPlacementArea.FieldOnly)
            {
                if (!TryResolveFieldCoordinates(definition, previewPlacementPosition,
                        out FieldArea pondField, out int startX, out int startZ) ||
                    !pondField.CanPlaceBuilding(startX, startZ, definition.footprintWidth, definition.footprintDepth))
                    return false;
                return !HasPhysicalPlacementBlocker(
                    definition,
                    pondField.CellSize,
                    previewPlacementPosition,
                    previewPlacementRotation
                );
            }
            if (definition.placementArea == BuildingPlacementArea.Anywhere && fieldArea != null)
            {
                if (!TryResolveFieldCoordinates(definition, previewPlacementPosition,
                        out FieldArea targetField, out int startX, out int startZ) ||
                    !targetField.CanPlaceBuilding(startX, startZ, definition.footprintWidth, definition.footprintDepth))
                    return false;
                return !HasPhysicalPlacementBlocker(
                    definition,
                    targetField.CellSize,
                    previewPlacementPosition,
                    previewPlacementRotation);
            }
            if (definition.placementArea == BuildingPlacementArea.Anywhere &&
                OverlapsAnyFieldArea(definition, previewPlacementPosition, previewPlacementRotation))
                return false;
            // Bangunan OutsideField menjaga area tanam tetap kosong. Anywhere, seperti
            // Fish Pond, boleh diletakkan di field atau area dunia lain yang valid.
            if (definition.placementArea == BuildingPlacementArea.OutsideField &&
                OverlapsAnyFieldArea(definition, previewPlacementPosition, previewPlacementRotation))
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
        if (reserveFieldFootprint || definition.placementArea == BuildingPlacementArea.FieldOnly)
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

    bool TryReserveFootprint(BuildingDefinitionSO definition, Vector3 position)
    {
        bool anywhereOnField = definition.placementArea == BuildingPlacementArea.Anywhere && fieldArea != null;
        if (!reserveFieldFootprint && definition.placementArea != BuildingPlacementArea.FieldOnly && !anywhereOnField)
            return true;
        return TryResolveFieldCoordinates(definition, position, out FieldArea area, out int x, out int z) &&
               area.TryPlaceBuilding(x, z, definition.footprintWidth, definition.footprintDepth, siteId);
    }

    void ReleaseFootprint()
    {
        if ((reserveFieldFootprint || activeDefinition?.placementArea == BuildingPlacementArea.FieldOnly ||
             activeDefinition?.placementArea == BuildingPlacementArea.Anywhere) && fieldArea != null)
            fieldArea.TryRemoveBuilding(siteId);
    }

    bool TryResolveFieldCoordinates(
        BuildingDefinitionSO definition,
        out FieldArea area,
        out int startX,
        out int startZ)
        => TryResolveFieldCoordinates(
            definition,
            buildingAnchor != null ? buildingAnchor.position : transform.position,
            out area,
            out startX,
            out startZ);

    bool TryResolveFieldCoordinates(
        BuildingDefinitionSO definition,
        Vector3 position,
        out FieldArea area,
        out int startX,
        out int startZ)
    {
        area = fieldArea;
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

    /// <summary>
    /// Marker memakai material runtime agar lokasi build mudah dibaca tanpa mengubah material
    /// dummy yang telah dipasang manual pada scene.
    /// </summary>
    void ConfigureAvailableMarkerVisual()
    {
        if (availableMarker == null)
            return;

        foreach (Collider markerCollider in availableMarker.GetComponentsInChildren<Collider>(true))
            markerCollider.enabled = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        if (shader == null)
            return;

        availableMarkerMaterial = new Material(shader)
        {
            name = $"{name}_BuildSiteMarker_Runtime"
        };
        ConfigurePreviewMaterial(availableMarkerMaterial);
        availableMarkerMaterial.color = availableMarkerColor;
        if (availableMarkerMaterial.HasProperty("_BaseColor"))
            availableMarkerMaterial.SetColor("_BaseColor", availableMarkerColor);

        foreach (Renderer markerRenderer in availableMarker.GetComponentsInChildren<Renderer>(true))
        {
            int materialCount = Mathf.Max(1, markerRenderer.sharedMaterials.Length);
            Material[] materials = new Material[materialCount];
            for (int index = 0; index < materialCount; index++)
                materials[index] = availableMarkerMaterial;
            markerRenderer.sharedMaterials = materials;
            markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
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

        if (previewUsesFreePlacement)
            CreatePreviewDirectionIndicator(definition, previewObject.transform);

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

    static void CreatePreviewDirectionIndicator(BuildingDefinitionSO definition, Transform parent)
    {
        // Dummy Fish Pond berbentuk simetris. Penanda ini membuat perubahan arah
        // tetap terlihat saat preview diputar, dan menunjukkan sisi depan bangunan.
        float forwardEdge = Mathf.Max(0.5f, definition.footprintDepth * 0.5f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "PreviewDirection_Forward";
        shaft.transform.SetParent(parent, false);
        shaft.transform.localPosition = new Vector3(0f, 1.25f, forwardEdge + 0.45f);
        shaft.transform.localScale = new Vector3(0.18f, 0.12f, 0.9f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "PreviewDirection_ArrowHead";
        head.transform.SetParent(parent, false);
        head.transform.localPosition = new Vector3(0f, 1.25f, forwardEdge + 0.95f);
        head.transform.localScale = new Vector3(0.65f, 0.14f, 0.35f);
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
            if (site == null || string.IsNullOrWhiteSpace(site.siteId) ||
                (site.globalBuildInstance && site.IsEmpty))
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

        BuildingCatalogSO sharedCatalog = Resources.Load<BuildingCatalogSO>("Buildings/Field Building Catalog");
        for (int dataIndex = 0; dataIndex < data.Count; dataIndex++)
        {
            PropertySiteSaveData saved = data[dataIndex];
            bool found = false;
            for (int siteIndex = 0; siteIndex < Registry.Count; siteIndex++)
            {
                PropertySite site = Registry[siteIndex];
                if (site == null || !site.MatchesSavedSite(saved.siteId))
                    continue;
                site.Restore(saved);
                found = true;
                break;
            }
            if (!found && IsDynamicWorldSiteId(saved.siteId) && sharedCatalog != null)
            {
                PropertySite created = CreateGlobalBuildSite(
                    saved.siteId,
                    sharedCatalog,
                    saved.hasPlacementPose ? saved.placementPosition : Vector3.zero);
                created.Restore(saved);
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
        if (activeDefinition?.placementArea == BuildingPlacementArea.Anywhere &&
            FieldArea.TryGetAt(buildingAnchor.position, out FieldArea restoredField, out _, out _))
            fieldArea = restoredField;

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
