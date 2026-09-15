using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full map dan mini map berbagi WorldMapService. Tidak memakai kamera/RenderTexture:
/// background adalah satu sprite dan marker hanya diperbarui saat UI terlihat.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class WorldMapUI : MonoBehaviour
{
    static WorldMapUI instance;
    const float MapWidth = 1500f;
    const float MapHeight = 800f;

    [SerializeField] KeyCode toggleKey = KeyCode.P;
    [SerializeField] bool showMobileMapButton = true;

    readonly List<MarkerView> markerViews = new();
    Canvas canvas;
    GameObject safeRoot;
    GameObject fullPanel;
    RectTransform viewport;
    RectTransform mapContent;
    RectTransform playerMarker;
    RectTransform waypointMarker;
    MapFogOfWarGraphic fogGraphic;
    TMP_Text areaText;
    TMP_Text detailText;
    GameObject miniPanel;
    RectTransform miniPlayer;
    RectTransform miniWaypoint;
    RectTransform northArrow;
    GameObject waypointHud;
    RectTransform waypointHudArrow;
    TMP_Text waypointHudText;
    TMP_Text areaToast;
    CanvasGroup areaToastGroup;
    PlayerController lockedPlayer;
    float nextMarkerUpdate;
    float toastUntil;
    int builtMarkerRevision = -1;
    QuestService boundQuestService;
    bool showLocations = true;
    bool showNpcs = true;
    bool showQuests = true;
    MapFastTravelPointDefinition selectedFastTravel;
    GameObject fastTravelConfirmation;
    TMP_Text fastTravelConfirmationText;
    MapRegionDefinition focusedArea;

    public static bool IsOpen => instance != null && instance.fullPanel != null && instance.fullPanel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (instance != null) return;
        GameObject host = new("WorldMapUI_Runtime");
        instance = host.AddComponent<WorldMapUI>();
        DontDestroyOnLoad(host);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void Start()
    {
        BindService();
        SetOpen(false);
        RefreshSceneVisibility();
    }

    void OnDestroy()
    {
        if (WorldMapService.Instance != null)
        {
            WorldMapService.Instance.AreaEntered -= HandleAreaEntered;
            WorldMapService.Instance.MapStateChanged -= HandleMapStateChanged;
            WorldMapService.Instance.FastTravelUnlocked -= HandleFastTravelUnlocked;
            WorldMapService.Instance.FogChanged -= HandleFogChanged;
        }
        if (boundQuestService != null) boundQuestService.QuestChanged -= HandleQuestChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SetModalLock(false);
        if (instance == this) instance = null;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindService();
        RefreshSceneVisibility();
    }

    void RefreshSceneVisibility()
    {
        bool gameplay = !string.Equals(SceneManager.GetActiveScene().name, "MainMenu", System.StringComparison.OrdinalIgnoreCase);
        if (safeRoot != null) safeRoot.SetActive(gameplay);
        if (!gameplay) SetOpen(false);
    }

    void BindService()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service == null) return;
        service.AreaEntered -= HandleAreaEntered;
        service.MapStateChanged -= HandleMapStateChanged;
        service.FastTravelUnlocked -= HandleFastTravelUnlocked;
        service.FogChanged -= HandleFogChanged;
        service.AreaEntered += HandleAreaEntered;
        service.MapStateChanged += HandleMapStateChanged;
        service.FastTravelUnlocked += HandleFastTravelUnlocked;
        service.FogChanged += HandleFogChanged;
        if (boundQuestService != QuestService.Instance)
        {
            if (boundQuestService != null) boundQuestService.QuestChanged -= HandleQuestChanged;
            boundQuestService = QuestService.Instance;
            if (boundQuestService != null) boundQuestService.QuestChanged += HandleQuestChanged;
        }
        BuildMapLayout();
        RefreshMiniMapVisibility();
    }

    void Update()
    {
        if (safeRoot == null || !safeRoot.activeInHierarchy) return;
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) SetOpen(false);
            else if (!WorldInteractionPrompt.IsSuppressed) SetOpen(true);
        }
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);

        if (areaToastGroup != null)
        {
            float remaining = toastUntil - Time.unscaledTime;
            areaToastGroup.alpha = Mathf.Clamp01(remaining > 0.35f ? 1f : remaining / 0.35f);
            areaToastGroup.gameObject.SetActive(remaining > 0f);
        }

        WorldMapService service = WorldMapService.Instance;
        if (service == null || (!IsOpen && !service.MiniMapEnabled) || Time.unscaledTime < nextMarkerUpdate) return;
        nextMarkerUpdate = Time.unscaledTime + Mathf.Max(0.05f, service.Definition.markerUpdateInterval);
        if (builtMarkerRevision != service.MarkerRevision) BuildMarkerViews();
        UpdateMarkers();
    }

    public void SetOpen(bool open)
    {
        if (fullPanel == null) return;
        if (!open) SelectFastTravel(null);
        fullPanel.SetActive(open);
        if (open) SetModalLock(true);
        else if (lockedPlayer != null) StartCoroutine(ReleaseModalLockNextFrame());
        else SetModalLock(false);
        if (open)
        {
            WorldMapCaptureCamera.Invalidate();
            BindService();
            BuildMarkerViews();
            if (WorldMapService.Instance != null && WorldMapService.Instance.IsWorldMap)
                FocusCurrentArea();
            else
                CenterOnPlayer();
            UpdateMarkers();
        }
    }

    IEnumerator ReleaseModalLockNextFrame()
    {
        // P juga dipakai Pet/Place. Pertahankan lock sampai seluruh Update pada frame
        // penutupan selesai agar input yang sama tidak bocor ke gameplay.
        yield return null;
        if (!IsOpen) SetModalLock(false);
    }

    void SetModalLock(bool locked)
    {
        if (locked)
        {
            lockedPlayer = FindFirstObjectByType<PlayerController>();
            lockedPlayer?.AcquireMovementLock(this);
            TimeManager.Instance?.AcquirePause(this);
            WorldInteractionPrompt.AcquireSuppression(this);
        }
        else
        {
            lockedPlayer?.ReleaseMovementLock(this);
            lockedPlayer = null;
            TimeManager.Instance?.ReleasePause(this);
            WorldInteractionPrompt.ReleaseSuppression(this);
        }
    }

    void HandleAreaEntered(string area)
    {
        if (areaToast == null) return;
        areaToast.text = $"Entering: {area}";
        toastUntil = Time.unscaledTime + 2.8f;
        areaToastGroup.gameObject.SetActive(true);
        if (IsOpen && WorldMapService.Instance != null && WorldMapService.Instance.IsWorldMap)
            FocusCurrentArea();
    }

    void HandleFastTravelUnlocked(string pointName)
    {
        if (areaToast == null) return;
        areaToast.text = $"Fast Travel Unlocked: {pointName}";
        toastUntil = Time.unscaledTime + 3.2f;
        areaToastGroup.gameObject.SetActive(true);
    }

    void HandleMapStateChanged()
    {
        if (WorldMapService.Instance != null && !WorldMapService.Instance.IsWorldMap) focusedArea = null;
        BuildMapLayout();
        BuildMarkerViews();
        RefreshMiniMapVisibility();
    }

    void HandleQuestChanged(QuestDefinitionSO quest, QuestStatus status) => BuildMarkerViews();

    void HandleFogChanged() => fogGraphic?.RefreshFog();

    void BuildMapLayout()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service == null || mapContent == null) return;
        for (int i = mapContent.childCount - 1; i >= 0; i--)
            Destroy(mapContent.GetChild(i).gameObject);

        RectTransform artworkRect = CreateRect("MapArtwork", mapContent);
        Stretch(artworkRect);
        bool showingWorldCapture = service.UseTopDownCapture && IsOpen;
        if (showingWorldCapture)
        {
            RawImage capture = artworkRect.gameObject.AddComponent<RawImage>();
            capture.color = Color.white;
            capture.texture = WorldMapCaptureCamera.Request(service);
            capture.raycastTarget = false;
        }
        else
        {
            Image background = AddImage(artworkRect, service.CurrentBackgroundColor);
            background.sprite = service.CurrentBackground;
            background.preserveAspect = service.CurrentBackground != null;
            background.raycastTarget = false;
        }

        foreach (MapRegionDefinition region in service.CurrentRegions)
        {
            if (region == null) continue;
            bool discovered = service.IsAreaDiscovered(region.areaId);
            RectTransform regionRect = CreateRect($"Area_{region.areaId}", mapContent);
            regionRect.anchorMin = region.normalizedRect.min;
            regionRect.anchorMax = region.normalizedRect.max;
            regionRect.offsetMin = regionRect.offsetMax = Vector2.zero;
            Color discoveredColor = region.color;
            if (showingWorldCapture) discoveredColor.a = 0.16f;
            AddImage(regionRect, discovered ? discoveredColor : Color.clear).raycastTarget = false;
            TMP_Text label = AddText(CreateRect("Label", regionRect), discovered ? region.displayName : string.Empty, 22, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
        }

        RectTransform fogRect = CreateRect("ExplorationFog", mapContent);
        Stretch(fogRect);
        fogRect.gameObject.AddComponent<CanvasRenderer>();
        fogGraphic = fogRect.gameObject.AddComponent<MapFogOfWarGraphic>();
        float fogOpacity = service.Definition != null ? service.Definition.unexploredFogOpacity : 0.92f;
        fogGraphic.Configure(service, service.FogColumns, service.FogRows, new Color(0.005f, 0.008f, 0.01f, fogOpacity));

        waypointMarker = CreateMarkerText("WaypointMarker", mapContent, "X", new Color(1f, 0.75f, 0.15f, 1f), 34f);
        playerMarker = CreateArrow("PlayerMarker", mapContent, new Color(0.15f, 0.85f, 1f, 1f), 44f);
        BuildMarkerViews();
    }

    void BuildMarkerViews()
    {
        if (mapContent == null || WorldMapService.Instance == null) return;
        foreach (MarkerView marker in markerViews)
            if (marker.root != null) Destroy(marker.root.gameObject);
        markerViews.Clear();

        WorldMapService service = WorldMapService.Instance;
        foreach (MapStaticMarkerDefinition marker in service.CurrentStaticMarkers)
        {
            if (marker == null || !service.IsMarkerUnlocked(marker.markerId) || !CategoryVisible(marker.category)) continue;
            Transform sceneTarget = marker.followSceneObject ? FindSceneObject(marker.sceneObjectName) : null;
            if (marker.followSceneObject && sceneTarget == null && marker.hideIfSceneObjectMissing) continue;
            AddMarkerView(marker.markerId, marker.displayName, marker.category, marker.normalizedPosition,
                null, marker.icon, sceneTarget);
        }
        foreach (WorldMapMarker marker in WorldMapMarker.All)
        {
            if (marker == null || !marker.IsVisible || !service.IsSceneOnCurrentMap(marker.gameObject.scene) ||
                !CategoryVisible(marker.Category)) continue;
            AddMarkerView(marker.Id, marker.DisplayName, marker.Category, Vector2.zero, marker, marker.Icon, null);
        }
        foreach (MapFastTravelPointDefinition point in service.CurrentFastTravelPoints)
        {
            if (point == null || !service.IsFastTravelUnlocked(point.pointId) || !showLocations) continue;
            Vector2 normalized = point.normalizedPosition;
            if (service.TryResolveFastTravelPosition(point, out Vector3 worldPosition, out _))
                normalized = service.WorldToNormalized(worldPosition);
            AddFastTravelMarker(point, normalized);
        }
        builtMarkerRevision = service.MarkerRevision;
    }

    void AddFastTravelMarker(MapFastTravelPointDefinition point, Vector2 normalized)
    {
        RectTransform root = CreateRect($"FastTravel_{point.pointId}", mapContent);
        root.sizeDelta = new Vector2(34f, 34f);
        Button button = root.gameObject.AddComponent<Button>();
        AddCircle(root, CategoryColor(MapMarkerCategory.FastTravel));
        button.onClick.AddListener(() => SelectFastTravel(point));
        markerViews.Add(new MarkerView { root = root, normalized = normalized, category = MapMarkerCategory.FastTravel });
    }

    void SelectFastTravel(MapFastTravelPointDefinition point)
    {
        selectedFastTravel = point;
        if (fastTravelConfirmation != null) fastTravelConfirmation.SetActive(point != null);
        if (fastTravelConfirmationText != null && point != null)
            fastTravelConfirmationText.text = $"Fast travel ke {point.displayName}?";
        if (detailText != null && point != null)
            detailText.text = $"Tujuan dipilih: {point.displayName}";
    }

    void ConfirmFastTravel()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service == null || selectedFastTravel == null) return;
        if (!service.TryFastTravel(selectedFastTravel, out string reason))
        {
            detailText.text = reason;
            return;
        }
        selectedFastTravel = null;
        if (fastTravelConfirmation != null) fastTravelConfirmation.SetActive(false);
        SetOpen(false);
    }

    void AddMarkerView(string id, string label, MapMarkerCategory category, Vector2 normalized,
        WorldMapMarker source, Sprite icon, Transform sceneTarget)
    {
        RectTransform root = CreateRect($"Marker_{id}", mapContent);
        root.sizeDelta = new Vector2(34f, 34f);
        Button button = root.gameObject.AddComponent<Button>();
        AddCircle(root, CategoryColor(category));
        if (icon != null)
        {
            RectTransform iconRect = CreateRect("IconSlot", root);
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(5f, 5f);
            iconRect.offsetMax = new Vector2(-5f, -5f);
            Image iconImage = AddImage(iconRect, Color.white);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        string capturedLabel = label;
        button.onClick.AddListener(() =>
        {
            SelectFastTravel(null);
            Vector2 point = source != null
                ? WorldMapService.Instance.WorldToNormalized(source.WorldPosition)
                : sceneTarget != null ? WorldMapService.Instance.WorldToNormalized(sceneTarget.position) : normalized;
            bool active = WorldMapService.Instance.ToggleWaypoint(point, capturedLabel);
            detailText.text = active ? $"Waypoint: {capturedLabel}" : "Waypoint dihapus";
        });
        markerViews.Add(new MarkerView
        {
            root = root,
            normalized = normalized,
            source = source,
            sceneTarget = sceneTarget,
            category = category
        });
    }

    void UpdateMarkers()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service == null || playerMarker == null) return;
        SetNormalizedPosition(playerMarker, service.PlayerNormalized);
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -service.PlayerMapYaw);
        float inverseMapScale = 1f / Mathf.Max(0.01f, mapContent.localScale.x);
        playerMarker.localScale = Vector3.one * inverseMapScale;
        waypointMarker.gameObject.SetActive(service.WaypointIsOnCurrentMap);
        if (service.WaypointIsOnCurrentMap)
        {
            SetNormalizedPosition(waypointMarker, service.WaypointNormalized);
            waypointMarker.localScale = Vector3.one * inverseMapScale;
        }
        string viewLabel = focusedArea != null ? focusedArea.displayName
            : service.IsWorldMap ? "WORLD" : service.CurrentAreaName;
        areaText.text = $"{service.CurrentMapDisplayName}  |  {viewLabel}";
        if (service.WaypointIsOnCurrentMap) detailText.text = $"Waypoint: {service.WaypointLabel}";

        if (waypointHud != null)
        {
            waypointHud.SetActive(service.WaypointIsOnCurrentMap && !IsOpen);
            if (service.WaypointIsOnCurrentMap && service.Player != null)
            {
                Vector3 target = service.NormalizedToWorld(service.WaypointNormalized);
                Vector3 delta = target - service.Player.position;
                delta.y = 0f;
                float bearing = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                waypointHudArrow.localRotation = Quaternion.Euler(0f, 0f, -(bearing - service.PlayerYaw));
                waypointHudText.text = $"{service.WaypointLabel}  {delta.magnitude:0}m";
            }
        }

        foreach (MarkerView marker in markerViews)
        {
            if (marker.root == null) continue;
            Vector2 point = marker.source != null
                ? service.WorldToNormalized(marker.source.WorldPosition)
                : marker.sceneTarget != null ? service.WorldToNormalized(marker.sceneTarget.position) : marker.normalized;
            SetNormalizedPosition(marker.root, point);
            marker.root.localScale = Vector3.one * inverseMapScale;
        }
        waypointMarker.SetAsLastSibling();
        playerMarker.SetAsLastSibling();
        if (miniPlayer != null)
        {
            SetNormalizedPosition(miniPlayer, service.PlayerNormalized, 220f, 160f);
            miniPlayer.localRotation = Quaternion.Euler(0f, 0f, -service.PlayerMapYaw);
            miniWaypoint.gameObject.SetActive(service.WaypointIsOnCurrentMap);
            if (service.WaypointIsOnCurrentMap) SetNormalizedPosition(miniWaypoint, service.WaypointNormalized, 220f, 160f);
        }
        if (northArrow != null)
            northArrow.localRotation = Quaternion.Euler(0f, 0f, service.MapRotationDegrees);
    }

    void CenterOnPlayer()
    {
        if (mapContent == null || WorldMapService.Instance == null) return;
        float defaultZoom = WorldMapService.Instance.Definition != null
            ? Mathf.Max(1.15f, WorldMapService.Instance.Definition.minimumZoom)
            : 1.15f;
        mapContent.localScale = Vector3.one * defaultZoom;
        Vector2 normalized = WorldMapService.Instance.PlayerNormalized;
        Vector2 desired = -new Vector2((normalized.x - 0.5f) * MapWidth * defaultZoom,
            (normalized.y - 0.5f) * MapHeight * defaultZoom);
        Vector2 excess = Vector2.Max(Vector2.zero, (mapContent.sizeDelta * defaultZoom - viewport.rect.size) * 0.5f);
        desired.x = Mathf.Clamp(desired.x, -excess.x, excess.x);
        desired.y = Mathf.Clamp(desired.y, -excess.y, excess.y);
        mapContent.anchoredPosition = desired;
    }

    void FocusCurrentArea()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service == null) return;
        foreach (MapRegionDefinition region in service.CurrentRegions)
        {
            if (region != null && service.IsAreaDiscovered(region.areaId) &&
                string.Equals(region.displayName, service.CurrentAreaName, System.StringComparison.OrdinalIgnoreCase))
            {
                FocusArea(region);
                return;
            }
        }
        ShowWholeMap();
    }

    void FocusArea(MapRegionDefinition region)
    {
        if (region == null || mapContent == null || viewport == null) return;
        focusedArea = region;
        MapDefinitionSO definition = WorldMapService.Instance?.Definition;
        float min = definition != null ? definition.minimumZoom : 0.8f;
        float max = definition != null ? definition.maximumZoom : 3f;
        float fitX = 0.82f / Mathf.Max(0.05f, region.normalizedRect.width);
        float fitY = 0.82f / Mathf.Max(0.05f, region.normalizedRect.height);
        float scale = Mathf.Clamp(Mathf.Min(fitX, fitY), min, max);
        mapContent.localScale = new Vector3(scale, scale, 1f);
        Vector2 center = region.normalizedRect.center;
        mapContent.anchoredPosition = -new Vector2((center.x - 0.5f) * MapWidth * scale,
            (center.y - 0.5f) * MapHeight * scale);
        areaText.text = $"{WorldMapService.Instance.CurrentMapDisplayName}  |  {region.displayName}";
    }

    void ShowWholeMap()
    {
        focusedArea = null;
        if (mapContent == null) return;
        mapContent.localScale = Vector3.one;
        mapContent.anchoredPosition = Vector2.zero;
        if (areaText != null && WorldMapService.Instance != null)
            areaText.text = $"{WorldMapService.Instance.CurrentMapDisplayName}  |  WORLD";
    }

    void ToggleMiniMap()
    {
        WorldMapService service = WorldMapService.Instance;
        if (service != null) service.SetMiniMapEnabled(!service.MiniMapEnabled);
    }

    void ToggleFilter(MapMarkerCategory group)
    {
        if (group == MapMarkerCategory.NPC) showNpcs = !showNpcs;
        else if (group == MapMarkerCategory.QuestTarget) showQuests = !showQuests;
        else showLocations = !showLocations;
        BuildMarkerViews();
    }

    bool CategoryVisible(MapMarkerCategory category)
    {
        if (category == MapMarkerCategory.NPC) return showNpcs;
        if (category == MapMarkerCategory.QuestAvailable || category == MapMarkerCategory.QuestTarget || category == MapMarkerCategory.Story)
            return showQuests;
        return showLocations;
    }

    void RefreshMiniMapVisibility()
    {
        if (miniPanel != null) miniPanel.SetActive(WorldMapService.Instance != null && WorldMapService.Instance.MiniMapEnabled);
    }

    void BuildUI()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        gameObject.layer = uiLayer >= 0 ? uiLayer : 5;
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 445;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        safeRoot = CreateRect("SafeArea", transform).gameObject;
        Stretch(safeRoot.GetComponent<RectTransform>());
        safeRoot.AddComponent<SafeAreaFitter>();

        Button open = CreateButton("OpenMapButton", safeRoot.transform, "MAP [P]", new Vector2(-90f, -155f), new Vector2(142f, 54f));
        RectTransform openRect = open.GetComponent<RectTransform>();
        openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
        openRect.pivot = new Vector2(1f, 1f);
        open.onClick.AddListener(() => SetOpen(true));
        open.gameObject.SetActive(showMobileMapButton || !Application.isMobilePlatform);

        fullPanel = CreateRect("FullMapPanel", safeRoot.transform).gameObject;
        Stretch(fullPanel.GetComponent<RectTransform>());
        AddImage(fullPanel.GetComponent<RectTransform>(), new Color(0.015f, 0.022f, 0.02f, 0.97f));

        areaText = AddText(CreateRect("Title", fullPanel.transform), "WORLD MAP", 32, TextAlignmentOptions.Left);
        SetTopLeft(areaText.rectTransform, new Vector2(56f, -36f), new Vector2(950f, 54f));
        detailText = AddText(CreateRect("Detail", fullPanel.transform), "Tap marker untuk membuat waypoint", 20, TextAlignmentOptions.Right);
        SetTopRight(detailText.rectTransform, new Vector2(-56f, -38f), new Vector2(650f, 48f));

        CreateLegendItem(fullPanel.transform, "LOCATION", CategoryColor(MapMarkerCategory.Location), new Vector2(230f, -94f));
        CreateLegendItem(fullPanel.transform, "SHOP", CategoryColor(MapMarkerCategory.Shop), new Vector2(420f, -94f));
        CreateLegendItem(fullPanel.transform, "NPC", CategoryColor(MapMarkerCategory.NPC), new Vector2(570f, -94f));
        CreateLegendItem(fullPanel.transform, "QUEST", CategoryColor(MapMarkerCategory.QuestTarget), new Vector2(700f, -94f));
        CreateLegendItem(fullPanel.transform, "FAST TRAVEL", CategoryColor(MapMarkerCategory.FastTravel), new Vector2(860f, -94f));
        TMP_Text north = AddText(CreateRect("North", fullPanel.transform), "N", 24, TextAlignmentOptions.Center);
        north.color = new Color(0.85f, 0.95f, 0.9f, 1f);
        SetTopLeft(north.rectTransform, new Vector2(1120f, -91f), new Vector2(32f, 34f));
        northArrow = CreateArrow("NorthDirection", fullPanel.transform, north.color, 24f);
        SetTopLeft(northArrow, new Vector2(1156f, -88f), new Vector2(16f, 28f));

        viewport = CreateRect("MapViewport", fullPanel.transform);
        viewport.anchorMin = new Vector2(0.5f, 0.5f);
        viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.sizeDelta = new Vector2(MapWidth, MapHeight);
        viewport.anchoredPosition = new Vector2(0f, -5f);
        AddImage(viewport, new Color(0.04f, 0.07f, 0.055f, 1f));
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        mapContent = CreateRect("MapContent", viewport);
        mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.sizeDelta = new Vector2(MapWidth, MapHeight);
        mapContent.anchoredPosition = Vector2.zero;
        WorldMapViewportInput input = viewport.gameObject.AddComponent<WorldMapViewportInput>();
        input.Configure(mapContent, () => WorldMapService.Instance?.Definition);

        Button close = CreateButton("Close", fullPanel.transform, "CLOSE", new Vector2(-56f, 38f), new Vector2(150f, 50f));
        close.onClick.AddListener(() => SetOpen(false));
        Button center = CreateButton("Center", fullPanel.transform, "CENTER PLAYER", new Vector2(-224f, 38f), new Vector2(190f, 50f));
        center.onClick.AddListener(CenterOnPlayer);
        Button clear = CreateButton("ClearWaypoint", fullPanel.transform, "CLEAR PIN", new Vector2(-432f, 38f), new Vector2(180f, 50f));
        clear.onClick.AddListener(() => WorldMapService.Instance?.ClearWaypoint());
        Button mini = CreateButton("MiniMap", fullPanel.transform, "MINI MAP", new Vector2(56f, 38f), new Vector2(180f, 50f), false);
        mini.onClick.AddListener(ToggleMiniMap);
        Button locations = CreateButton("Locations", fullPanel.transform, "LOCATIONS", new Vector2(250f, 38f), new Vector2(155f, 50f), false);
        locations.onClick.AddListener(() => ToggleFilter(MapMarkerCategory.Location));
        Button npcs = CreateButton("NPCs", fullPanel.transform, "NPC", new Vector2(419f, 38f), new Vector2(115f, 50f), false);
        npcs.onClick.AddListener(() => ToggleFilter(MapMarkerCategory.NPC));
        Button quests = CreateButton("Quests", fullPanel.transform, "QUEST", new Vector2(548f, 38f), new Vector2(125f, 50f), false);
        quests.onClick.AddListener(() => ToggleFilter(MapMarkerCategory.QuestTarget));
        Button wholeMap = CreateButton("WholeMap", fullPanel.transform, "WORLD", new Vector2(687f, 38f), new Vector2(110f, 50f), false);
        wholeMap.onClick.AddListener(ShowWholeMap);
        Button currentArea = CreateButton("CurrentArea", fullPanel.transform, "AREA", new Vector2(811f, 38f), new Vector2(105f, 50f), false);
        currentArea.onClick.AddListener(FocusCurrentArea);

        fastTravelConfirmation = CreateRect("FastTravelConfirmation", fullPanel.transform).gameObject;
        RectTransform travelRect = fastTravelConfirmation.GetComponent<RectTransform>();
        travelRect.anchorMin = travelRect.anchorMax = new Vector2(0.5f, 0.5f);
        travelRect.sizeDelta = new Vector2(520f, 180f);
        AddImage(travelRect, new Color(0.025f, 0.06f, 0.05f, 0.98f));
        fastTravelConfirmationText = AddText(CreateRect("Question", travelRect), "Fast travel?", 24, TextAlignmentOptions.Center);
        SetTopLeft(fastTravelConfirmationText.rectTransform, new Vector2(20f, -22f), new Vector2(480f, 58f));
        Button confirmTravel = CreateButton("ConfirmTravel", travelRect, "TRAVEL", new Vector2(72f, 24f), new Vector2(170f, 52f), false);
        confirmTravel.onClick.AddListener(ConfirmFastTravel);
        Button cancelTravel = CreateButton("CancelTravel", travelRect, "CANCEL", new Vector2(-72f, 24f), new Vector2(170f, 52f));
        cancelTravel.onClick.AddListener(() => SelectFastTravel(null));
        fastTravelConfirmation.SetActive(false);

        miniPanel = CreateRect("MiniMapPanel", safeRoot.transform).gameObject;
        RectTransform miniRect = miniPanel.GetComponent<RectTransform>();
        miniRect.anchorMin = miniRect.anchorMax = new Vector2(1f, 1f);
        miniRect.pivot = new Vector2(1f, 1f);
        miniRect.anchoredPosition = new Vector2(-34f, -230f);
        miniRect.sizeDelta = new Vector2(240f, 200f);
        AddImage(miniRect, new Color(0.02f, 0.05f, 0.04f, 0.88f));
        RectTransform miniMap = CreateRect("MiniMap", miniRect);
        miniMap.anchorMin = miniMap.anchorMax = new Vector2(0.5f, 0.5f);
        miniMap.sizeDelta = new Vector2(220f, 160f);
        miniMap.anchoredPosition = new Vector2(0f, -10f);
        AddImage(miniMap, new Color(0.16f, 0.28f, 0.18f, 1f));
        miniPlayer = CreateArrow("Player", miniMap, new Color(0.25f, 0.9f, 1f, 1f), 20f);
        miniWaypoint = CreateMarkerText("Waypoint", miniMap, "X", new Color(1f, 0.75f, 0.15f, 1f), 20f);

        waypointHud = CreateRect("WaypointHUD", safeRoot.transform).gameObject;
        RectTransform waypointHudRect = waypointHud.GetComponent<RectTransform>();
        waypointHudRect.anchorMin = waypointHudRect.anchorMax = new Vector2(0.5f, 1f);
        waypointHudRect.pivot = new Vector2(0.5f, 1f);
        waypointHudRect.anchoredPosition = new Vector2(0f, -28f);
        waypointHudRect.sizeDelta = new Vector2(420f, 62f);
        AddImage(waypointHudRect, new Color(0.02f, 0.05f, 0.04f, 0.84f));
        waypointHudArrow = CreateArrow("Direction", waypointHudRect, new Color(1f, 0.75f, 0.15f, 1f), 30f);
        waypointHudArrow.anchorMin = waypointHudArrow.anchorMax = new Vector2(0f, 0.5f);
        waypointHudArrow.anchoredPosition = new Vector2(34f, 0f);
        waypointHudText = AddText(CreateRect("Label", waypointHudRect), "Waypoint", 19, TextAlignmentOptions.Left);
        waypointHudText.rectTransform.anchorMin = new Vector2(0f, 0f);
        waypointHudText.rectTransform.anchorMax = new Vector2(1f, 1f);
        waypointHudText.rectTransform.offsetMin = new Vector2(64f, 4f);
        waypointHudText.rectTransform.offsetMax = new Vector2(-12f, -4f);
        waypointHud.SetActive(false);

        RectTransform toast = CreateRect("AreaNameToast", safeRoot.transform);
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0.78f);
        toast.sizeDelta = new Vector2(560f, 64f);
        AddImage(toast, new Color(0.01f, 0.02f, 0.015f, 0.8f));
        areaToast = AddText(CreateRect("Text", toast), string.Empty, 27, TextAlignmentOptions.Center);
        Stretch(areaToast.rectTransform);
        areaToastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toast.gameObject.SetActive(false);
    }

    static RectTransform CreateArrow(string name, Transform parent, Color color, float size)
    {
        RectTransform root = CreateRect(name, parent);
        root.sizeDelta = new Vector2(size * 0.72f, size * 1.25f);
        RectTransform triangle = CreateRect("DirectionTriangle", root);
        Stretch(triangle);
        triangle.gameObject.AddComponent<CanvasRenderer>();
        MapTriangleGraphic graphic = triangle.gameObject.AddComponent<MapTriangleGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        return root;
    }

    static RectTransform CreateMarkerText(string name, Transform parent, string symbol, Color color, float size)
    {
        RectTransform root = CreateRect(name, parent);
        root.sizeDelta = new Vector2(size, size);
        TMP_Text text = AddText(CreateRect("Text", root), symbol, Mathf.RoundToInt(size), TextAlignmentOptions.Center);
        text.color = color;
        Stretch(text.rectTransform);
        return root;
    }

    static MapCircleGraphic AddCircle(RectTransform rect, Color color)
    {
        if (rect.gameObject.GetComponent<CanvasRenderer>() == null)
            rect.gameObject.AddComponent<CanvasRenderer>();
        MapCircleGraphic circle = rect.gameObject.AddComponent<MapCircleGraphic>();
        circle.color = color;
        return circle;
    }

    static void CreateLegendItem(Transform parent, string label, Color color, Vector2 position)
    {
        RectTransform root = CreateRect($"Legend_{label}", parent);
        SetTopLeft(root, position, new Vector2(180f, 30f));
        RectTransform dot = CreateRect("Color", root);
        dot.anchorMin = dot.anchorMax = new Vector2(0f, 0.5f);
        dot.pivot = new Vector2(0f, 0.5f);
        dot.anchoredPosition = Vector2.zero;
        dot.sizeDelta = new Vector2(18f, 18f);
        AddCircle(dot, color).raycastTarget = false;
        TMP_Text text = AddText(CreateRect("Label", root), label, 16, TextAlignmentOptions.Left);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(28f, 0f);
        text.rectTransform.offsetMax = Vector2.zero;
    }

    static void SetNormalizedPosition(RectTransform rect, Vector2 normalized, float width = MapWidth, float height = MapHeight)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((normalized.x - 0.5f) * width, (normalized.y - 0.5f) * height);
    }

    static Color CategoryColor(MapMarkerCategory category) => category switch
    {
        MapMarkerCategory.Shop => new Color(0.95f, 0.65f, 0.18f, 1f),
        MapMarkerCategory.NPC => new Color(0.35f, 0.75f, 1f, 1f),
        MapMarkerCategory.QuestAvailable => new Color(1f, 0.85f, 0.15f, 1f),
        MapMarkerCategory.QuestTarget => new Color(1f, 0.28f, 0.25f, 1f),
        MapMarkerCategory.Story => new Color(0.9f, 0.35f, 1f, 1f),
        MapMarkerCategory.FastTravel => new Color(0.25f, 1f, 0.48f, 1f),
        _ => new Color(0.9f, 0.9f, 0.82f, 1f)
    };

    static Transform FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;
        Transform[] candidates = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform fallback = null;
        foreach (Transform candidate in candidates)
        {
            if (candidate == null || candidate.name != objectName || !candidate.gameObject.scene.IsValid() ||
                (WorldMapService.Instance != null && !WorldMapService.Instance.IsSceneOnCurrentMap(candidate.gameObject.scene))) continue;
            if (candidate.gameObject.activeInHierarchy) return candidate;
            fallback ??= candidate;
        }
        return fallback;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text AddText(RectTransform rect, string value, int size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableAutoSizing = size > 22;
        text.fontSizeMin = Mathf.Max(12, size - 8);
        text.fontSizeMax = size;
        return text;
    }

    static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, bool right = true)
    {
        RectTransform root = CreateRect(name, parent);
        root.anchorMin = root.anchorMax = right ? new Vector2(1f, 0f) : Vector2.zero;
        root.pivot = right ? new Vector2(1f, 0f) : Vector2.zero;
        root.anchoredPosition = position;
        root.sizeDelta = size;
        AddImage(root, new Color(0.14f, 0.28f, 0.22f, 0.96f));
        Button button = root.gameObject.AddComponent<Button>();
        TMP_Text text = AddText(CreateRect("Text", root), label, 18, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    sealed class MarkerView
    {
        public RectTransform root;
        public Vector2 normalized;
        public WorldMapMarker source;
        public Transform sceneTarget;
        public MapMarkerCategory category;
    }
}

/// <summary>Drag, mouse wheel, dan pinch zoom untuk viewport map tanpa Update tetap.</summary>
public sealed class WorldMapViewportInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler, IPointerClickHandler
{
    RectTransform content;
    System.Func<MapDefinitionSO> definition;
    Vector2 lastPinchCenter;
    float lastPinchDistance;

    public void Configure(RectTransform target, System.Func<MapDefinitionSO> definitionProvider)
    {
        content = target;
        definition = definitionProvider;
    }

    void Update()
    {
        if (content == null || Input.touchCount != 2) { lastPinchDistance = 0f; return; }
        Touch a = Input.GetTouch(0);
        Touch b = Input.GetTouch(1);
        Vector2 center = (a.position + b.position) * 0.5f;
        float distance = Vector2.Distance(a.position, b.position);
        if (lastPinchDistance > 1f)
        {
            Zoom((distance - lastPinchDistance) * 0.008f);
            content.anchoredPosition += center - lastPinchCenter;
            ClampContent();
        }
        lastPinchDistance = distance;
        lastPinchCenter = center;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (content == null || Input.touchCount > 1) return;
        content.anchoredPosition += eventData.delta;
        ClampContent();
    }

    public void OnScroll(PointerEventData eventData) => Zoom(eventData.scrollDelta.y * 0.12f);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (content == null || WorldMapService.Instance == null || eventData.dragging) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position,
                eventData.pressEventCamera, out Vector2 local)) return;
        Vector2 normalized = new(local.x / content.rect.width + content.pivot.x,
            local.y / content.rect.height + content.pivot.y);
        if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f) return;
        WorldMapService.Instance.ToggleWaypoint(normalized, "Custom Waypoint");
    }

    void Zoom(float delta)
    {
        if (content == null) return;
        MapDefinitionSO settings = definition?.Invoke();
        float min = settings != null ? settings.minimumZoom : 0.8f;
        float max = settings != null ? settings.maximumZoom : 3f;
        float scale = Mathf.Clamp(content.localScale.x + delta, min, max);
        content.localScale = new Vector3(scale, scale, 1f);
        ClampContent();
    }

    void ClampContent()
    {
        if (content == null || transform is not RectTransform viewport) return;
        Vector2 excess = Vector2.Max(Vector2.zero, (content.sizeDelta * content.localScale.x - viewport.rect.size) * 0.5f);
        Vector2 position = content.anchoredPosition;
        position.x = Mathf.Clamp(position.x, -excess.x, excess.x);
        position.y = Mathf.Clamp(position.y, -excess.y, excess.y);
        content.anchoredPosition = position;
    }
}
