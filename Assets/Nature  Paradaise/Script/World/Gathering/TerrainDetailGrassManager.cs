using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TerrainDetailGrassPatchSaveData
{
    public string id;
    public float growth;
    public int stage;
    public int spawnDay;
    public int lastSpreadDay;
    public bool spreadBoosted;
}

[Serializable]
public sealed class TerrainDetailGrassAreaSaveData
{
    public string terrainId;
    public List<TerrainDetailGrassPatchSaveData> patches = new();
}

/// <summary>
/// Mengubah layer Terrain Paint Details terpilih menjadi rumput liar interaktif.
/// Detail tetap dirender Terrain; manager hanya mengelola blok density yang bisa disabit/grazing.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Terrain))]
public sealed class TerrainDetailGrassManager : MonoBehaviour
{
    [Serializable]
    public sealed class PrototypeSetting
    {
        [HideInInspector] public string prototypeName;
        [HideInInspector] public int prototypeIndex;
        [HideInInspector] public GameObject detailPrefab;
        [HideInInspector] public Texture2D detailTexture;
        [Tooltip("Aktif = bisa disabit/dimakan. Mati = hanya dekorasi dan tidak disentuh manager.")]
        public bool harvestable;
        [Min(1)] public int grassYield = 2;
    }

    sealed class PatchRecord
    {
        public string id;
        public int prototypeIndex;
        public int x;
        public int z;
        public int width;
        public int height;
        public Vector3 worldPosition;
        public float growth;
        public int stage = 2;
        public int spawnDay;
        public int lastSpreadDay = -1;
        public bool spreadBoosted;
        public bool blocked;
    }

    static readonly List<TerrainDetailGrassManager> ActiveManagers = new();

    [Header("References")]
    [SerializeField] Terrain targetTerrain;
    [SerializeField] ItemSO grassItem;
    [Tooltip("ID permanen dan unik untuk Terrain ini.")]
    [SerializeField] string terrainId;

    [Header("Paint Detail Mapping")]
    [SerializeField] PrototypeSetting[] prototypeSettings;
    [Tooltip("Satu blok ini dianggap satu gundukan dan menghasilkan maksimal satu drop stack.")]
    [SerializeField, Min(0.5f)] float patchWorldSize = 1.5f;
    [SerializeField, Min(1)] int minimumPaintedDensity = 1;

    [Header("Wild Regrowth")]
    [SerializeField, Min(1)] int regrowDays = 10;
    [SerializeField, Min(1)] int smallStageDay = 3;
    [SerializeField, Min(1)] int mediumStageDay = 6;
    [SerializeField, Range(0.05f, 1f)] float smallDensity = 0.3f;
    [SerializeField, Range(0.05f, 1f)] float mediumDensity = 0.65f;
    [SerializeField] bool useWeatherModifier = true;
    [SerializeField] bool useSeasonModifier = true;
    [SerializeField, Min(1)] int daysPerSeason = 28;
    [SerializeField] bool allowNeighbourSpreadBoost = true;
    [SerializeField, Range(0f, 1f)] float neighbourSpreadChance = 0.08f;

    readonly List<PatchRecord> records = new();
    readonly Dictionary<string, PatchRecord> recordById = new();
    readonly Dictionary<Vector2Int, List<int>> spatialCells = new();
    readonly Dictionary<int, PrototypeSetting> settingsByIndex = new();
    readonly Dictionary<int, int[,]> sourceLayers = new();
    readonly Dictionary<int, int[,]> runtimeLayers = new();
    TerrainData sourceData;
    TerrainData runtimeData;
    TerrainRuntimeDataHost runtimeHost;
    bool ownsRuntimeData;
    bool initialized;

    public int PatchCount => records.Count;
    public int AvailablePatchCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < records.Count; i++)
                if (records[i].stage >= 2 && !records[i].blocked) count++;
            return count;
        }
    }

    void Awake()
    {
        if (targetTerrain == null) targetTerrain = GetComponent<Terrain>();
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError("[WildGrass] Target Terrain atau TerrainData belum tersedia.", this);
            enabled = false;
            return;
        }

        runtimeHost = GetComponent<TerrainRuntimeDataHost>();
        if (runtimeHost != null && runtimeHost.EnsureInitialized())
        {
            sourceData = runtimeHost.SourceData;
            runtimeData = runtimeHost.RuntimeData;
        }
        else
        {
            sourceData = targetTerrain.terrainData;
            runtimeData = Instantiate(sourceData);
            runtimeData.name = sourceData.name + " (Wild Grass Runtime)";
            targetTerrain.terrainData = runtimeData;
            TerrainCollider collider = targetTerrain.GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = runtimeData;
            ownsRuntimeData = true;
        }

        BuildRecords();
        initialized = true;
        Debug.Log($"[WildGrass] {records.Count} gundukan dari Paint Details siap; " +
                  $"{AvailablePatchCount} bisa dipanen.", this);
    }

    void OnEnable()
    {
        TimeManager.OnBeforeDayChange += AdvanceDay;
        if (initialized && !ActiveManagers.Contains(this)) ActiveManagers.Add(this);
    }

    void Start()
    {
        if (initialized && !ActiveManagers.Contains(this)) ActiveManagers.Add(this);
        if (initialized) RefreshFarmBlockers(true);
    }

    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= AdvanceDay;
        ActiveManagers.Remove(this);
        if (!initialized) return;
        RestorePaintedLayers();
        if (ownsRuntimeData && targetTerrain != null && targetTerrain.terrainData == runtimeData)
            targetTerrain.terrainData = sourceData;
    }

    void OnDestroy()
    {
        if (ownsRuntimeData && runtimeData != null) Destroy(runtimeData);
    }

    void BuildRecords()
    {
        DetailPrototype[] prototypes = sourceData.detailPrototypes;
        if (prototypeSettings != null)
        {
            for (int i = 0; i < prototypeSettings.Length; i++)
            {
                PrototypeSetting setting = prototypeSettings[i];
                if (setting == null || !setting.harvestable) continue;
                int resolved = ResolvePrototype(setting, prototypes);
                if (resolved >= 0) settingsByIndex[resolved] = setting;
            }
        }

        string prefix = string.IsNullOrEmpty(terrainId)
            ? gameObject.scene.path + "/" + targetTerrain.name + "/" + targetTerrain.transform.position
            : terrainId;
        int pixelsX = Mathf.Max(1, Mathf.RoundToInt(patchWorldSize * sourceData.detailWidth / sourceData.size.x));
        int pixelsZ = Mathf.Max(1, Mathf.RoundToInt(patchWorldSize * sourceData.detailHeight / sourceData.size.z));

        foreach (KeyValuePair<int, PrototypeSetting> pair in settingsByIndex)
        {
            int prototypeIndex = pair.Key;
            int[,] source = sourceData.GetDetailLayer(
                0, 0, sourceData.detailWidth, sourceData.detailHeight, prototypeIndex);
            sourceLayers[prototypeIndex] = source;
            runtimeLayers[prototypeIndex] = (int[,])source.Clone();

            for (int z = 0; z < sourceData.detailHeight; z += pixelsZ)
            for (int x = 0; x < sourceData.detailWidth; x += pixelsX)
            {
                int width = Mathf.Min(pixelsX, sourceData.detailWidth - x);
                int height = Mathf.Min(pixelsZ, sourceData.detailHeight - z);
                int density = 0;
                for (int iz = z; iz < z + height; iz++)
                for (int ix = x; ix < x + width; ix++) density += source[iz, ix];
                if (density < minimumPaintedDensity) continue;

                float nx = Mathf.Clamp01((x + width * 0.5f) / sourceData.detailWidth);
                float nz = Mathf.Clamp01((z + height * 0.5f) / sourceData.detailHeight);
                string name = string.IsNullOrEmpty(pair.Value.prototypeName)
                    ? "Detail" + prototypeIndex : pair.Value.prototypeName;
                PatchRecord record = new()
                {
                    id = $"{prefix}/{name}/{x}/{z}",
                    prototypeIndex = prototypeIndex,
                    x = x,
                    z = z,
                    width = width,
                    height = height,
                    growth = regrowDays,
                    stage = 2,
                    worldPosition = targetTerrain.transform.position + new Vector3(
                        nx * sourceData.size.x,
                        sourceData.GetInterpolatedHeight(nx, nz),
                        nz * sourceData.size.z)
                };
                int index = records.Count;
                records.Add(record);
                recordById[record.id] = record;
                Vector2Int cell = Cell(record.worldPosition);
                if (!spatialCells.TryGetValue(cell, out List<int> bucket))
                    spatialCells[cell] = bucket = new List<int>();
                bucket.Add(index);
            }
        }
    }

    static int ResolvePrototype(PrototypeSetting setting, DetailPrototype[] prototypes)
    {
        if (setting.prototypeIndex >= 0 && setting.prototypeIndex < prototypes.Length)
        {
            DetailPrototype indexed = prototypes[setting.prototypeIndex];
            if ((setting.detailPrefab != null && indexed.prototype == setting.detailPrefab) ||
                (setting.detailTexture != null && indexed.prototypeTexture == setting.detailTexture) ||
                (setting.detailPrefab == null && setting.detailTexture == null))
                return setting.prototypeIndex;
        }
        for (int i = 0; i < prototypes.Length; i++)
        {
            if (setting.detailPrefab != null && prototypes[i].prototype == setting.detailPrefab) return i;
            if (setting.detailTexture != null && prototypes[i].prototypeTexture == setting.detailTexture) return i;
        }
        return -1;
    }

    Vector2Int Cell(Vector3 position) => new(
        Mathf.FloorToInt(position.x / Mathf.Max(0.5f, patchWorldSize)),
        Mathf.FloorToInt(position.z / Mathf.Max(0.5f, patchWorldSize)));

    public static int CutAllInRadius(Vector3 center, float radius)
    {
        int total = 0;
        TerrainDetailGrassManager[] snapshot = ActiveManagers.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) total += snapshot[i].CutInRadius(center, radius);
        return total;
    }

    int CutInRadius(Vector3 center, float radius)
    {
        if (!initialized || grassItem == null) return 0;
        HashSet<int> dirtyLayers = new();
        int cut = 0;
        float radiusSquared = radius * radius;
        Vector2Int min = Cell(center - new Vector3(radius, 0f, radius));
        Vector2Int max = Cell(center + new Vector3(radius, 0f, radius));
        for (int z = min.y; z <= max.y; z++)
        for (int x = min.x; x <= max.x; x++)
        {
            if (!spatialCells.TryGetValue(new Vector2Int(x, z), out List<int> bucket)) continue;
            for (int i = 0; i < bucket.Count; i++)
            {
                PatchRecord record = records[bucket[i]];
                Vector3 delta = record.worldPosition - center;
                delta.y = 0f;
                if (record.stage < 2 || record.blocked || delta.sqrMagnitude > radiusSquared) continue;
                Deplete(record);
                dirtyLayers.Add(record.prototypeIndex);
                int amount = settingsByIndex.TryGetValue(record.prototypeIndex, out PrototypeSetting setting)
                    ? Mathf.Max(1, setting.grassYield) : 2;
                PlacedWorldItem.Spawn(grassItem, amount, record.worldPosition + Vector3.up * 0.15f,
                    Quaternion.Euler(0f, Deterministic01(record.id, CurrentDay) * 360f, 0f), false);
                cut++;
            }
        }
        ApplyDirtyLayers(dirtyLayers);
        return cut;
    }

    public bool IsPatchAvailable(int index) => initialized && index >= 0 && index < records.Count &&
        records[index].stage >= 2 && !records[index].blocked;

    public Vector3 GetPatchPosition(int index) => index >= 0 && index < records.Count
        ? records[index].worldPosition : transform.position;

    public bool TryGraze(int index)
    {
        if (!IsPatchAvailable(index)) return false;
        PatchRecord record = records[index];
        Deplete(record);
        ApplyLayer(record.prototypeIndex);
        return true;
    }

    public static bool TryFindNearest(Vector3 animalPosition, Vector3 homePosition, float homeRadius,
        out TerrainDetailGrassManager manager, out int patchIndex, out Vector3 patchPosition)
    {
        manager = null;
        patchIndex = -1;
        patchPosition = default;
        float best = float.PositiveInfinity;
        float homeRadiusSquared = homeRadius * homeRadius;
        TerrainDetailGrassManager[] snapshot = ActiveManagers.ToArray();
        for (int m = 0; m < snapshot.Length; m++)
        {
            TerrainDetailGrassManager candidateManager = snapshot[m];
            if (candidateManager == null || !candidateManager.initialized) continue;
            for (int i = 0; i < candidateManager.records.Count; i++)
            {
                PatchRecord record = candidateManager.records[i];
                if (record.stage < 2 || record.blocked) continue;
                Vector3 homeDelta = record.worldPosition - homePosition;
                homeDelta.y = 0f;
                if (homeDelta.sqrMagnitude > homeRadiusSquared) continue;
                Vector3 animalDelta = record.worldPosition - animalPosition;
                animalDelta.y = 0f;
                float distance = animalDelta.sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                manager = candidateManager;
                patchIndex = i;
                patchPosition = record.worldPosition;
            }
        }
        return manager != null;
    }

    void Deplete(PatchRecord record)
    {
        record.growth = 0f;
        record.stage = -1;
        record.spawnDay = CurrentDay;
        record.lastSpreadDay = -1;
        record.spreadBoosted = false;
        SetPatchDensity(record, 0f);
    }

    void AdvanceDay()
    {
        if (!initialized) return;
        int nextDay = CurrentDay + 1;
        float modifier = GrowthModifier(nextDay);
        HashSet<int> dirtyLayers = new();

        RefreshFarmBlockers(false, dirtyLayers);

        for (int i = 0; i < records.Count; i++)
        {
            PatchRecord record = records[i];
            if (record.stage >= 2) continue;
            int previousStage = record.stage;
            record.growth = Mathf.Min(regrowDays, record.growth + modifier);
            record.stage = StageFor(record.growth);
            if (record.stage != previousStage)
            {
                SetPatchDensity(record, record.blocked ? 0f : DensityFor(record.stage));
                dirtyLayers.Add(record.prototypeIndex);
            }
        }

        if (allowNeighbourSpreadBoost)
        {
            for (int i = 0; i < records.Count; i++)
            {
                PatchRecord source = records[i];
                if (source.stage < 2 || source.blocked ||
                    Deterministic01(source.id, nextDay) > neighbourSpreadChance) continue;
                int targetIndex = FindRegrowingNeighbour(i);
                if (targetIndex < 0) continue;
                PatchRecord target = records[targetIndex];
                int previousStage = target.stage;
                target.growth = Mathf.Min(regrowDays, target.growth + 1f);
                target.stage = StageFor(target.growth);
                target.lastSpreadDay = nextDay;
                target.spreadBoosted = true;
                if (target.stage != previousStage)
                {
                    SetPatchDensity(target, DensityFor(target.stage));
                    dirtyLayers.Add(target.prototypeIndex);
                }
            }
        }
        ApplyDirtyLayers(dirtyLayers);
    }

    int FindRegrowingNeighbour(int sourceIndex)
    {
        PatchRecord source = records[sourceIndex];
        Vector2Int center = Cell(source.worldPosition);
        float maxDistance = patchWorldSize * 2.1f;
        float maxDistanceSquared = maxDistance * maxDistance;
        for (int z = center.y - 2; z <= center.y + 2; z++)
        for (int x = center.x - 2; x <= center.x + 2; x++)
        {
            if (!spatialCells.TryGetValue(new Vector2Int(x, z), out List<int> bucket)) continue;
            for (int i = 0; i < bucket.Count; i++)
            {
                int index = bucket[i];
                if (index == sourceIndex) continue;
                PatchRecord candidate = records[index];
                if (candidate.prototypeIndex != source.prototypeIndex || candidate.stage >= 2 || candidate.blocked) continue;
                Vector3 delta = candidate.worldPosition - source.worldPosition;
                delta.y = 0f;
                if (delta.sqrMagnitude <= maxDistanceSquared) return index;
            }
        }
        return -1;
    }

    int StageFor(float growth)
    {
        if (growth >= regrowDays) return 2;
        if (growth >= mediumStageDay) return 1;
        if (growth >= smallStageDay) return 0;
        return -1;
    }

    float DensityFor(int stage) => stage switch
    {
        0 => smallDensity,
        1 => mediumDensity,
        2 => 1f,
        _ => 0f
    };

    float GrowthModifier(int day)
    {
        float value = 1f;
        if (useWeatherModifier && WeatherSystem.Instance != null)
        {
            value *= WeatherSystem.Instance.CurrentWeather switch
            {
                WeatherType.Heatwave => 0.8f,
                WeatherType.Drizzle => 1.1f,
                WeatherType.Rain => 1.25f,
                WeatherType.HeavyRain => 1.4f,
                WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => 1.5f,
                WeatherType.Snow => 0.25f,
                WeatherType.Blizzard => 0.05f,
                _ => 1f
            };
        }
        if (useSeasonModifier)
        {
            value *= CropDataSO.GetSeasonForDay(day, daysPerSeason) switch
            {
                CropSeason.Spring => 1.25f,
                CropSeason.Autumn => 0.75f,
                CropSeason.Winter => 0.1f,
                _ => 1f
            };
        }
        return value;
    }

    void SetPatchDensity(PatchRecord record, float scale)
    {
        int[,] source = sourceLayers[record.prototypeIndex];
        int[,] current = runtimeLayers[record.prototypeIndex];
        for (int z = record.z; z < record.z + record.height; z++)
        for (int x = record.x; x < record.x + record.width; x++)
        {
            if (scale <= 0f || source[z, x] <= 0) current[z, x] = 0;
            else current[z, x] = Mathf.Max(1, Mathf.RoundToInt(source[z, x] * scale));
        }
    }

    void ApplyDirtyLayers(HashSet<int> dirtyLayers)
    {
        foreach (int prototypeIndex in dirtyLayers) ApplyLayer(prototypeIndex);
    }

    void ApplyLayer(int prototypeIndex)
    {
        if (runtimeData != null && runtimeLayers.TryGetValue(prototypeIndex, out int[,] layer))
            runtimeData.SetDetailLayer(0, 0, prototypeIndex, layer);
    }

    void RestorePaintedLayers()
    {
        if (runtimeData == null) return;
        foreach (KeyValuePair<int, int[,]> pair in sourceLayers)
            runtimeData.SetDetailLayer(0, 0, pair.Key, pair.Value);
    }

    int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;

    static bool CanExistAt(Vector3 position)
    {
        return !FieldArea.TryGetAt(position, out FieldArea field, out int x, out int z) || field.CanHoe(x, z);
    }

    void RefreshFarmBlockers(bool force, HashSet<int> dirtyLayers = null)
    {
        HashSet<int> localDirty = dirtyLayers ?? new HashSet<int>();
        for (int i = 0; i < records.Count; i++)
        {
            PatchRecord record = records[i];
            bool blocked = !CanExistAt(record.worldPosition);
            if (!force && blocked == record.blocked) continue;
            record.blocked = blocked;
            SetPatchDensity(record, blocked ? 0f : DensityFor(record.stage));
            localDirty.Add(record.prototypeIndex);
        }
        if (dirtyLayers == null) ApplyDirtyLayers(localDirty);
    }

    static float Deterministic01(string id, int day)
    {
        unchecked
        {
            uint hash = 2166136261;
            string text = id + "/" + day;
            for (int i = 0; i < text.Length; i++) hash = (hash ^ text[i]) * 16777619;
            return (hash & 0x00ffffff) / 16777215f;
        }
    }

    public TerrainDetailGrassAreaSaveData Capture()
    {
        TerrainDetailGrassAreaSaveData result = new() { terrainId = terrainId };
        for (int i = 0; i < records.Count; i++)
        {
            PatchRecord record = records[i];
            if (record.stage >= 2 && !record.spreadBoosted) continue;
            result.patches.Add(new TerrainDetailGrassPatchSaveData
            {
                id = record.id,
                growth = record.growth,
                stage = record.stage,
                spawnDay = record.spawnDay,
                lastSpreadDay = record.lastSpreadDay,
                spreadBoosted = record.spreadBoosted
            });
        }
        return result;
    }

    public static List<TerrainDetailGrassAreaSaveData> CaptureAll()
    {
        List<TerrainDetailGrassAreaSaveData> result = new();
        TerrainDetailGrassManager[] managers = FindObjectsByType<TerrainDetailGrassManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
            if (managers[i] != null && managers[i].initialized) result.Add(managers[i].Capture());
        return result;
    }

    public static void RestoreAll(List<TerrainDetailGrassAreaSaveData> data)
    {
        TerrainDetailGrassManager[] managers = FindObjectsByType<TerrainDetailGrassManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++) managers[i]?.Restore(FindArea(data, managers[i].terrainId));
    }

    static TerrainDetailGrassAreaSaveData FindArea(List<TerrainDetailGrassAreaSaveData> data, string id)
    {
        if (data == null) return null;
        for (int i = 0; i < data.Count; i++) if (data[i] != null && data[i].terrainId == id) return data[i];
        return null;
    }

    void Restore(TerrainDetailGrassAreaSaveData data)
    {
        if (!initialized) return;
        foreach (KeyValuePair<int, int[,]> pair in sourceLayers)
            runtimeLayers[pair.Key] = (int[,])pair.Value.Clone();
        for (int i = 0; i < records.Count; i++)
        {
            records[i].growth = regrowDays;
            records[i].stage = 2;
            records[i].spawnDay = 0;
            records[i].lastSpreadDay = -1;
            records[i].spreadBoosted = false;
        }
        if (data != null && data.patches != null)
        {
            for (int i = 0; i < data.patches.Count; i++)
            {
                TerrainDetailGrassPatchSaveData saved = data.patches[i];
                if (saved == null || !recordById.TryGetValue(saved.id, out PatchRecord record)) continue;
                record.growth = Mathf.Clamp(saved.growth, 0f, regrowDays);
                record.stage = StageFor(record.growth);
                record.spawnDay = saved.spawnDay;
                record.lastSpreadDay = saved.lastSpreadDay;
                record.spreadBoosted = saved.spreadBoosted;
                SetPatchDensity(record, DensityFor(record.stage));
            }
        }
        for (int i = 0; i < records.Count; i++)
            SetPatchDensity(records[i], records[i].blocked ? 0f : DensityFor(records[i].stage));
        foreach (int prototypeIndex in runtimeLayers.Keys) ApplyLayer(prototypeIndex);
    }

    void OnValidate()
    {
        patchWorldSize = Mathf.Max(0.5f, patchWorldSize);
        regrowDays = Mathf.Max(1, regrowDays);
        smallStageDay = Mathf.Clamp(smallStageDay, 1, regrowDays);
        mediumStageDay = Mathf.Clamp(mediumStageDay, smallStageDay, regrowDays);
        daysPerSeason = Mathf.Max(1, daysPerSeason);
    }
}
