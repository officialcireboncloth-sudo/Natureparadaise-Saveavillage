using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class WorldMapSaveData
{
    public List<string> discoveredAreaIds = new();
    public List<string> exploredFogCells = new();
    public List<string> unlockedMarkerIds = new();
    public bool hasWaypoint;
    public float waypointNormalizedX;
    public float waypointNormalizedY;
    public string waypointLabel;
    public bool miniMapEnabled;
    public List<string> unlockedFastTravelPointIds = new();
    public string waypointMapId;
}

/// <summary>
/// Model runtime untuk map. Service mengecek area hanya empat kali per detik dan tidak
/// melakukan rendering; UI dapat dimatikan sepenuhnya tanpa menghentikan discovery.
/// </summary>
[DefaultExecutionOrder(-150)]
public sealed class WorldMapService : MonoBehaviour
{
    public static WorldMapService Instance { get; private set; }

    readonly HashSet<string> discoveredAreaIds = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> unlockedMarkerIds = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> unlockedFastTravelPointIds = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> exploredFogCells = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Transform> fastTravelTargetCache = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> missingFastTravelTargets = new(StringComparer.OrdinalIgnoreCase);
    MapDefinitionSO definition;
    Transform player;
    Bounds worldBounds;
    float nextAreaCheck;
    string currentAreaId;
    bool hasWaypoint;
    Vector2 waypointNormalized;
    string waypointLabel;
    bool miniMapEnabled;
    MapSceneDefinition currentSceneMap;
    string currentMapId;

    public MapDefinitionSO Definition => definition;
    public Bounds WorldBounds => worldBounds;
    public Transform Player => player;
    public string CurrentAreaName { get; private set; } = "World";
    public bool HasWaypoint => hasWaypoint;
    public Vector2 WaypointNormalized => waypointNormalized;
    public string WaypointLabel => waypointLabel;
    public bool MiniMapEnabled => miniMapEnabled;
    public string CurrentMapId => string.IsNullOrWhiteSpace(currentMapId) ? definition?.mapId : currentMapId;
    public string CurrentMapDisplayName => currentSceneMap != null ? currentSceneMap.displayName : definition?.displayName ?? "World Map";
    public IReadOnlyList<MapRegionDefinition> CurrentRegions => currentSceneMap != null ? currentSceneMap.regions : definition.regions;
    public IReadOnlyList<MapStaticMarkerDefinition> CurrentStaticMarkers => currentSceneMap != null ? currentSceneMap.staticMarkers : definition.staticMarkers;
    public Sprite CurrentBackground => currentSceneMap != null ? currentSceneMap.background : definition.background;
    public Color CurrentBackgroundColor => currentSceneMap != null ? currentSceneMap.backgroundColor : definition.backgroundColor;
    public bool UseTopDownCapture => currentSceneMap != null ? currentSceneMap.useTopDownWorldCapture : definition.useTopDownWorldCapture;
    public float MapRotationDegrees => currentSceneMap != null ? currentSceneMap.mapRotationDegrees : definition != null ? definition.mapRotationDegrees : 0f;
    public bool FogOfWarEnabled => currentSceneMap != null ? currentSceneMap.fogOfWarEnabled : definition != null && definition.fogOfWarEnabled;
    public int FogColumns => definition != null ? Mathf.Max(1, definition.fogColumns) : 30;
    public int FogRows => definition != null ? Mathf.Max(1, definition.fogRows) : 18;
    public bool IsWorldMap => currentSceneMap == null;
    public bool WaypointIsOnCurrentMap => hasWaypoint && string.Equals(waypointMapId, CurrentMapId, StringComparison.OrdinalIgnoreCase);
    public IEnumerable<MapFastTravelPointDefinition> CurrentFastTravelPoints
    {
        get
        {
            if (definition?.fastTravelPoints == null) yield break;
            foreach (MapFastTravelPointDefinition point in definition.fastTravelPoints)
                if (point != null && string.Equals(point.mapId, CurrentMapId, StringComparison.OrdinalIgnoreCase))
                    yield return point;
        }
    }
    public int MarkerRevision { get; private set; }

    public event Action<string> AreaEntered;
    public event Action MapStateChanged;
    public event Action<string> FastTravelUnlocked;
    public event Action FogChanged;
    string waypointMapId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null || FindFirstObjectByType<WorldMapService>() != null) return;
        new GameObject("WorldMapService_Runtime").AddComponent<WorldMapService>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        definition = Resources.Load<MapDefinitionSO>("Map/World Map");
        if (definition == null) definition = MapDefinitionSO.CreateRuntimeDefault();
        if (definition.sceneMaps == null || definition.sceneMaps.Count == 0)
            definition.sceneMaps = MapDefinitionSO.DefaultInteriorMaps();
        if (definition.fastTravelPoints == null || definition.fastTravelPoints.Count == 0)
            definition.fastTravelPoints = MapDefinitionSO.DefaultFastTravelPoints();
        ApplyDefinitionDefaults();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResolveMapContext(true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        fastTravelTargetCache.Clear();
        missingFastTravelTargets.Clear();
        ResolveMapContext(true);
        MarkerRevision++;
    }

    void Update()
    {
        ResolveMapContext(false);
        if (Time.unscaledTime < nextAreaCheck) return;
        nextAreaCheck = Time.unscaledTime + Mathf.Max(0.1f, definition.areaCheckInterval);
        ResolvePlayer();
        if (player == null) return;
        RevealFogAroundPlayer();
        DiscoverNearbyFastTravelPoints();
        Vector2 normalized = WorldToBaseNormalized(GetTrackedWorldPosition());
        MapRegionDefinition area = FindArea(normalized);
        if (area == null || string.Equals(currentAreaId, area.areaId, StringComparison.OrdinalIgnoreCase)) return;
        currentAreaId = area.areaId;
        CurrentAreaName = area.displayName;
        bool firstVisit = discoveredAreaIds.Add(area.areaId);
        if (firstVisit) MapStateChanged?.Invoke();
        AreaEntered?.Invoke(area.displayName);
    }

    void ApplyDefinitionDefaults()
    {
        foreach (MapRegionDefinition area in definition.regions)
            if (area != null && area.discoveredByDefault) discoveredAreaIds.Add(area.areaId);
        foreach (MapStaticMarkerDefinition marker in definition.staticMarkers)
            if (marker != null && marker.unlockedByDefault) unlockedMarkerIds.Add(marker.markerId);
        foreach (MapSceneDefinition map in definition.sceneMaps)
            if (map?.staticMarkers != null)
                foreach (MapStaticMarkerDefinition marker in map.staticMarkers)
                    if (marker != null && marker.unlockedByDefault) unlockedMarkerIds.Add(marker.markerId);
        foreach (MapFastTravelPointDefinition point in definition.fastTravelPoints)
            if (point != null && point.unlockedByDefault) unlockedFastTravelPointIds.Add(point.pointId);
        miniMapEnabled = definition.miniMapEnabledByDefault;
    }

    void ResolvePlayer()
    {
        if (player != null) return;
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null) player = controller.transform;
    }

    Vector3 GetTrackedWorldPosition()
    {
        if (player != null) return player.position;
        if (SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.TryGetWorldReturnPosition(out Vector3 worldPosition))
            return worldPosition;
        return currentSceneMap != null ? worldBounds.center : Vector3.zero;
    }

    void ResolveWorldBounds()
    {
        Vector2 min = currentSceneMap != null ? currentSceneMap.worldMin : definition != null ? definition.worldMin : Vector2.zero;
        Vector2 max = currentSceneMap != null ? currentSceneMap.worldMax : definition != null ? definition.worldMax : Vector2.one;
        worldBounds = new Bounds(new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f),
            new Vector3(Mathf.Max(1f, max.x - min.x), 1f, Mathf.Max(1f, max.y - min.y)));
        if (currentSceneMap != null)
        {
            if (currentSceneMap.autoBoundsFromScene && TryGetSceneBounds(currentSceneMap.sceneName, out Bounds sceneBounds))
                worldBounds = sceneBounds;
            return;
        }
        if (definition == null || !definition.autoBoundsFromTerrain) return;
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0) return;
        Bounds combined = terrains[0].terrainData.bounds;
        combined.center += terrains[0].transform.position;
        for (int i = 1; i < terrains.Length; i++)
        {
            Bounds bounds = terrains[i].terrainData.bounds;
            bounds.center += terrains[i].transform.position;
            combined.Encapsulate(bounds);
        }
        worldBounds = combined;
    }

    void ResolveMapContext(bool force)
    {
        string sceneName = SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInsideInterior
            ? SceneTransitionManager.Instance.CurrentInteriorSceneName
            : SceneManager.GetActiveScene().name;
        MapSceneDefinition next = definition?.FindSceneMap(sceneName);
        string nextId = next != null ? next.mapId : definition?.mapId ?? "world.main";
        if (!force && string.Equals(nextId, currentMapId, StringComparison.OrdinalIgnoreCase)) return;
        currentSceneMap = next;
        currentMapId = nextId;
        currentAreaId = string.Empty;
        CurrentAreaName = next != null ? next.displayName : definition?.displayName ?? "World";
        ResolveWorldBounds();
        MarkerRevision++;
        MapStateChanged?.Invoke();
    }

    static bool TryGetSceneBounds(string sceneName, out Bounds bounds)
    {
        bounds = default;
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded) return false;
        bool found = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!found) return false;
        Vector3 size = bounds.size;
        size.x = Mathf.Max(4f, size.x + 2f);
        size.z = Mathf.Max(4f, size.z + 2f);
        size.y = Mathf.Max(1f, size.y);
        bounds.size = size;
        return true;
    }

    public Vector2 WorldToNormalized(Vector3 world)
    {
        Vector2 projected = ProjectWorldDelta(new Vector2(world.x - worldBounds.center.x, world.z - worldBounds.center.z));
        GetProjectedSize(out float width, out float height);
        float x = Mathf.InverseLerp(-width * 0.5f, width * 0.5f, projected.x);
        float y = Mathf.InverseLerp(-height * 0.5f, height * 0.5f, projected.y);
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    Vector2 WorldToBaseNormalized(Vector3 world)
    {
        float x = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, world.x);
        float y = Mathf.InverseLerp(worldBounds.min.z, worldBounds.max.z, world.z);
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    public Vector3 NormalizedToWorld(Vector2 normalized)
    {
        GetProjectedSize(out float width, out float height);
        Vector2 projected = new(Mathf.Lerp(-width * 0.5f, width * 0.5f, normalized.x),
            Mathf.Lerp(-height * 0.5f, height * 0.5f, normalized.y));
        float radians = MapRotationDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float x = projected.x * cos + projected.y * sin;
        float z = -projected.x * sin + projected.y * cos;
        return new Vector3(worldBounds.center.x + x, worldBounds.center.y, worldBounds.center.z + z);
    }

    Vector2 ProjectWorldDelta(Vector2 delta)
    {
        float radians = MapRotationDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(delta.x * cos - delta.y * sin, delta.x * sin + delta.y * cos);
    }

    void GetProjectedSize(out float width, out float height)
    {
        float radians = MapRotationDegrees * Mathf.Deg2Rad;
        float absCos = Mathf.Abs(Mathf.Cos(radians));
        float absSin = Mathf.Abs(Mathf.Sin(radians));
        width = Mathf.Max(1f, worldBounds.size.x * absCos + worldBounds.size.z * absSin);
        height = Mathf.Max(1f, worldBounds.size.x * absSin + worldBounds.size.z * absCos);
    }

    public Vector2 PlayerNormalized => player != null ? WorldToNormalized(GetTrackedWorldPosition()) : new Vector2(0.5f, 0.5f);
    public float PlayerYaw => player != null ? player.eulerAngles.y : 0f;
    public float PlayerMapYaw => PlayerYaw - MapRotationDegrees;
    public bool IsAreaDiscovered(string id) => !string.IsNullOrWhiteSpace(id) && discoveredAreaIds.Contains(id);
    public bool IsMarkerUnlocked(string id) => !string.IsNullOrWhiteSpace(id) && unlockedMarkerIds.Contains(id);
    public bool IsFastTravelUnlocked(string id) => !string.IsNullOrWhiteSpace(id) && unlockedFastTravelPointIds.Contains(id);
    public bool IsFogCellExplored(string mapId, int x, int y) =>
        exploredFogCells.Contains(FogCellKey(mapId, x, y));
    public bool IsSceneOnCurrentMap(Scene scene) => scene.IsValid() &&
        (currentSceneMap != null
            ? string.Equals(scene.name, currentSceneMap.sceneName, StringComparison.OrdinalIgnoreCase)
            : definition.FindSceneMap(scene.name) == null);

    public void DiscoverArea(string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && discoveredAreaIds.Add(id)) MapStateChanged?.Invoke();
    }

    public void UnlockMarker(string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && unlockedMarkerIds.Add(id)) MapStateChanged?.Invoke();
    }

    public void UnlockFastTravelPoint(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !unlockedFastTravelPointIds.Add(id)) return;
        MarkerRevision++;
        MapStateChanged?.Invoke();
    }

    void RevealFogAroundPlayer()
    {
        if (!FogOfWarEnabled || player == null) return;
        Vector2 normalized = PlayerNormalized;
        int columns = FogColumns;
        int rows = FogRows;
        int centerX = Mathf.Clamp(Mathf.FloorToInt(normalized.x * columns), 0, columns - 1);
        int centerY = Mathf.Clamp(Mathf.FloorToInt(normalized.y * rows), 0, rows - 1);
        int radius = definition != null ? Mathf.Max(1, definition.fogRevealRadiusCells) : 2;
        bool changed = false;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            if (x < 0 || x >= columns || y < 0 || y >= rows) continue;
            if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) > radius * radius + 1) continue;
            changed |= exploredFogCells.Add(FogCellKey(CurrentMapId, x, y));
        }
        if (changed) FogChanged?.Invoke();
    }

    static string FogCellKey(string mapId, int x, int y) => $"{mapId}:{x}:{y}";

    public void SetWaypoint(Vector2 normalized, string label = "Custom Waypoint")
    {
        waypointNormalized = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
        waypointLabel = string.IsNullOrWhiteSpace(label) ? "Waypoint" : label;
        hasWaypoint = true;
        waypointMapId = CurrentMapId;
        MapStateChanged?.Invoke();
    }

    public bool ToggleWaypoint(Vector2 normalized, string label = "Custom Waypoint", float samePointTolerance = 0.02f)
    {
        normalized = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
        if (WaypointIsOnCurrentMap && Vector2.Distance(waypointNormalized, normalized) <= Mathf.Max(0.001f, samePointTolerance))
        {
            ClearWaypoint();
            return false;
        }
        SetWaypoint(normalized, label);
        return true;
    }

    public void ClearWaypoint()
    {
        if (!hasWaypoint) return;
        hasWaypoint = false;
        waypointLabel = string.Empty;
        waypointMapId = string.Empty;
        MapStateChanged?.Invoke();
    }

    public bool TryFastTravel(MapFastTravelPointDefinition point, out string reason)
    {
        reason = string.Empty;
        if (point == null || !IsFastTravelUnlocked(point.pointId))
        { reason = "Fast Travel Point belum ditemukan."; return false; }
        if (!TryResolveFastTravelPosition(point, out Vector3 position, out Quaternion rotation))
        { reason = "Tujuan Fast Travel belum tersedia di scene."; return false; }
        if (SceneTransitionManager.Instance == null ||
            !SceneTransitionManager.Instance.FastTravelToWorld(position, rotation))
        { reason = "Fast Travel sedang tidak tersedia."; return false; }
        ClearWaypoint();
        return true;
    }

    void DiscoverNearbyFastTravelPoints()
    {
        if (currentSceneMap != null || player == null) return;
        foreach (MapFastTravelPointDefinition point in definition.fastTravelPoints)
        {
            if (point == null || IsFastTravelUnlocked(point.pointId) ||
                !string.Equals(point.mapId, CurrentMapId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!TryResolveFastTravelPosition(point, out Vector3 position, out _) ||
                Vector3.Distance(player.position, position) > Mathf.Max(0.5f, point.discoveryRadius)) continue;
            unlockedFastTravelPointIds.Add(point.pointId);
            MarkerRevision++;
            FastTravelUnlocked?.Invoke(point.displayName);
            MapStateChanged?.Invoke();
        }
    }

    public bool TryResolveFastTravelPosition(MapFastTravelPointDefinition point, out Vector3 position, out Quaternion rotation)
    {
        position = point != null ? NormalizedToWorld(point.normalizedPosition) : Vector3.zero;
        rotation = Quaternion.identity;
        if (point == null) return false;
        Transform target = ResolveFastTravelTarget(point);
        if (target == null) return string.IsNullOrWhiteSpace(point.sceneObjectName);
        position = target.position + target.TransformVector(point.arrivalOffset);
        rotation = target.rotation;
        return true;
    }

    Transform ResolveFastTravelTarget(MapFastTravelPointDefinition point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.sceneObjectName)) return null;
        if (fastTravelTargetCache.TryGetValue(point.pointId, out Transform cached) && cached != null) return cached;
        if (missingFastTravelTargets.Contains(point.pointId)) return null;
        Transform target = FindSceneObject(point.sceneObjectName);
        if (target != null) fastTravelTargetCache[point.pointId] = target;
        else missingFastTravelTargets.Add(point.pointId);
        return target;
    }

    static Transform FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate != null && candidate.name == objectName && candidate.gameObject.scene.IsValid()) return candidate;
        return null;
    }

    public void SetMiniMapEnabled(bool enabled)
    {
        miniMapEnabled = enabled;
        MapStateChanged?.Invoke();
    }

    public void NotifyMarkerListChanged()
    {
        MarkerRevision++;
        MapStateChanged?.Invoke();
    }

    MapRegionDefinition FindArea(Vector2 normalized)
    {
        foreach (MapRegionDefinition area in CurrentRegions)
            if (area != null && area.normalizedRect.Contains(normalized)) return area;
        return null;
    }

    public WorldMapSaveData Capture() => new()
    {
        discoveredAreaIds = new List<string>(discoveredAreaIds),
        exploredFogCells = new List<string>(exploredFogCells),
        unlockedMarkerIds = new List<string>(unlockedMarkerIds),
        hasWaypoint = hasWaypoint,
        waypointNormalizedX = waypointNormalized.x,
        waypointNormalizedY = waypointNormalized.y,
        waypointLabel = waypointLabel,
        miniMapEnabled = miniMapEnabled,
        unlockedFastTravelPointIds = new List<string>(unlockedFastTravelPointIds),
        waypointMapId = waypointMapId
    };

    public void Restore(WorldMapSaveData data)
    {
        discoveredAreaIds.Clear();
        exploredFogCells.Clear();
        unlockedMarkerIds.Clear();
        unlockedFastTravelPointIds.Clear();
        ApplyDefinitionDefaults();
        if (data != null)
        {
            if (data.discoveredAreaIds != null)
                foreach (string id in data.discoveredAreaIds) if (!string.IsNullOrWhiteSpace(id)) discoveredAreaIds.Add(id);
            if (data.exploredFogCells != null)
                foreach (string id in data.exploredFogCells) if (!string.IsNullOrWhiteSpace(id)) exploredFogCells.Add(id);
            if (data.unlockedMarkerIds != null)
                foreach (string id in data.unlockedMarkerIds) if (!string.IsNullOrWhiteSpace(id)) unlockedMarkerIds.Add(id);
            hasWaypoint = data.hasWaypoint;
            waypointNormalized = new Vector2(data.waypointNormalizedX, data.waypointNormalizedY);
            waypointLabel = data.waypointLabel;
            miniMapEnabled = data.miniMapEnabled;
            waypointMapId = data.waypointMapId;
            if (hasWaypoint && string.IsNullOrWhiteSpace(waypointMapId)) waypointMapId = definition.mapId;
            if (data.unlockedFastTravelPointIds != null)
                foreach (string id in data.unlockedFastTravelPointIds)
                    if (!string.IsNullOrWhiteSpace(id)) unlockedFastTravelPointIds.Add(id);
        }
        MapStateChanged?.Invoke();
        FogChanged?.Invoke();
    }

    public void ResetProgress()
    {
        discoveredAreaIds.Clear();
        exploredFogCells.Clear();
        unlockedMarkerIds.Clear();
        unlockedFastTravelPointIds.Clear();
        hasWaypoint = false;
        waypointLabel = string.Empty;
        waypointMapId = string.Empty;
        currentAreaId = string.Empty;
        CurrentAreaName = "World";
        ApplyDefinitionDefaults();
        MapStateChanged?.Invoke();
        FogChanged?.Invoke();
    }
}
