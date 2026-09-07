using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Streaming Terrain Paint Trees: visual jauh tetap TreeInstance, hanya pohon terdekat
/// memakai WorldTree pooled. Data Terrain asli tidak pernah dimodifikasi saat runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class TerrainTreeManager : MonoBehaviour
{
    static readonly List<TerrainTreeManager> EnabledManagers = new();
    static int sharedFrame = -1;
    static int sharedSwitches;
    [Serializable]
    public sealed class PrototypeSetting
    {
        public GameObject terrainPrefab;
        [Tooltip("Matikan untuk pohon dekorasi yang tidak pernah bisa ditebang.")]
        public bool choppable = true;
        [Tooltip("Aktif: setelah roboh langsung hilang dan menunggu regrow. Mati: tunggul harus ditebang juga.")]
        public bool removeStumpAfterFelling = true;
        [Tooltip("Prefab root WorldTree + Collider, dengan visual yang sama seperti Terrain.")]
        public WorldTree interactivePrefab;
    }

    [Serializable]
    public sealed class TreeRecord
    {
        public string id;
        public int terrainInstanceIndex;
        public int prototypeIndex;
        public Vector3 worldPosition;
        public float rotationRadians;
        public float widthScale;
        public float heightScale;
        public TreeGrowthState state = TreeGrowthState.Standing;
        public int durability;
        public int respawnDay;
        public TreeProgress progress;
        [NonSerialized] public Vector3 displayedScale = Vector3.one;
        [NonSerialized] public WorldTree activeObject;
        [NonSerialized] public bool terrainVisible = true;
    }

    [Header("References")]
    [SerializeField] Terrain targetTerrain;
    [Tooltip("Kosong = cari PlayerController secara otomatis.")]
    [SerializeField] Transform player;
    [Tooltip("ID permanen, harus berbeda untuk setiap Terrain manager.")]
    [SerializeField] string terrainId;
    [Header("Prototype Mapping")]
    [SerializeField] PrototypeSetting[] prototypeSettings;

    [Header("Streaming")]
    [SerializeField, Min(1f)] float activateDistance = 15f;
    [Tooltip("Hysteresis: pohon aktif tidak langsung berganti saat player di tepi radius.")]
    [SerializeField, Min(1f)] float deactivateDistance = 18f;
    [Tooltip("Batas total aktif seluruh Terrain manager. Gunakan nilai sama pada setiap tile.")]
    [SerializeField, Min(1)] int maxActiveTrees = 50;
    [SerializeField, Min(0.05f)] float checkInterval = 0.25f;
    [FormerlySerializedAs("maxActivationsPerCheck")]
    [SerializeField, Min(1)] int maxSwitchesPerFrame = 5;
    [Tooltip("Batas pemeriksaan kandidat/regrow per frame, termasuk area yang sangat padat.")]
    [SerializeField, Min(16)] int maxRecordChecksPerFrame = 256;
    [Tooltip("Batas object inactive yang disimpan untuk dipakai ulang.")]
    [SerializeField, Min(0)] int maxPooledTrees = 50;
    [Header("Debug")]
    [SerializeField] bool logSetup = true;

    readonly List<TreeRecord> records = new();
    readonly List<TreeRecord> active = new();
    readonly List<TreeRecord> candidates = new();
    readonly List<float> candidateDistances = new();
    readonly List<TreeRecord> desired = new();
    readonly HashSet<TreeRecord> desiredSet = new();
    readonly Dictionary<int, PrototypeSetting> settingByPrototype = new();
    readonly Dictionary<Vector2Int, List<TreeRecord>> cells = new();
    readonly Dictionary<int, Stack<WorldTree>> pools = new();
    readonly Queue<TreeRecord> visualQueue = new();
    readonly HashSet<TreeRecord> queuedVisuals = new();
    TerrainData sourceData;
    TerrainData runtimeData;
    TerrainData sourceColliderData;
    TerrainCollider terrainCollider;
    TerrainRuntimeDataHost runtimeHost;
    bool ownsRuntimeData;
    Transform runtimeRoot;
    TreeInstance[] originalInstances;
    float nextCheckTime;
    float cellSize;
    bool scanning;
    Vector3 scanPosition;
    Vector2Int scanMin, scanMax;
    int scanX, scanZ, scanIndex;
    int maintenanceIndex = -1;
    int pooledCount;
    int today = 1;
    bool initialized;

    public IReadOnlyList<TreeRecord> Records => records;
    public int ActiveTreeCount => active.Count;
    public int PooledTreeCount => pooledCount;
    public int LastFrameSwitches { get; private set; }
    public static int TotalActiveTreeCount
    {
        get
        {
            int count = 0;
            foreach (TerrainTreeManager manager in EnabledManagers)
                if (manager != null) count += manager.active.Count;
            return count;
        }
    }

    void Awake()
    {
        if (targetTerrain == null) targetTerrain = GetComponent<Terrain>();
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError("[TerrainTreeManager] Isi Target Terrain terlebih dahulu.", this);
            enabled = false;
            return;
        }
        runtimeHost = targetTerrain.GetComponent<TerrainRuntimeDataHost>();
        terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (runtimeHost != null && runtimeHost.EnsureInitialized())
        {
            sourceData = runtimeHost.SourceData;
            runtimeData = runtimeHost.RuntimeData;
        }
        else
        {
            sourceData = targetTerrain.terrainData;
            if (terrainCollider != null) sourceColliderData = terrainCollider.terrainData;
            runtimeData = Instantiate(sourceData);
            runtimeData.name = sourceData.name + " (Tree Streaming Runtime)";
            ownsRuntimeData = true;
        }
        originalInstances = sourceData.treeInstances;
        runtimeRoot = new GameObject("InteractiveTrees_Runtime").transform;
        runtimeRoot.SetParent(transform, false);
        // Root pool tidak mewarisi scale/rotation manager agar ukuran sama dengan Terrain tree.
        runtimeRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        runtimeRoot.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, transform.lossyScale.x),
            1f / Mathf.Max(0.0001f, transform.lossyScale.y),
            1f / Mathf.Max(0.0001f, transform.lossyScale.z));
        BuildRecords();
        initialized = true;
        if (logSetup)
            Debug.Log($"[TerrainTreeManager] {records.Count} records; radius {activateDistance}m, " +
                      $"cap {maxActiveTrees}, budget {maxSwitchesPerFrame}/frame.", this);
    }

    void OnEnable()
    {
        TimeManager.OnDay += HandleDayChanged;
        TimeManager.OnBeforeDayChange += AdvanceGrowth;
        if (!initialized) return;
        if (!EnabledManagers.Contains(this)) EnabledManagers.Add(this);
        if (runtimeHost == null)
        {
            targetTerrain.terrainData = runtimeData;
            if (terrainCollider != null) terrainCollider.terrainData = runtimeData;
        }
        HandleDayChanged();
        nextCheckTime = 0f;
    }

    void OnDisable()
    {
        TimeManager.OnDay -= HandleDayChanged;
        TimeManager.OnBeforeDayChange -= AdvanceGrowth;
        if (!initialized) return;
        for (int i = active.Count - 1; i >= 0; i--) Deactivate(active[i], true);
        EnabledManagers.Remove(this);
        if (runtimeHost == null)
        {
            if (targetTerrain != null) targetTerrain.terrainData = sourceData;
            if (terrainCollider != null) terrainCollider.terrainData = sourceColliderData;
        }
        scanning = false;
        desired.Clear();
        desiredSet.Clear();
    }

    void OnDestroy()
    {
        // Hanya salinan runtime milik manager; bukan asset Terrain atau prefab pengguna.
        if (runtimeRoot != null) Destroy(runtimeRoot.gameObject);
        if (ownsRuntimeData && runtimeData != null) Destroy(runtimeData);
    }

    void BuildRecords()
    {
        TreePrototype[] prototypes = sourceData.treePrototypes;
        if (prototypeSettings != null)
            foreach (PrototypeSetting setting in prototypeSettings)
                if (setting != null && setting.terrainPrefab != null)
                    for (int i = 0; i < prototypes.Length; i++)
                        if (prototypes[i].prefab == setting.terrainPrefab)
                            settingByPrototype[i] = setting;

        cellSize = Mathf.Max(1f, deactivateDistance);
        string prefix = string.IsNullOrEmpty(terrainId)
            ? gameObject.scene.path + "/" + targetTerrain.name + "/" + targetTerrain.transform.position
            : terrainId;
        Dictionary<string, int> duplicates = new();
        for (int i = 0; i < originalInstances.Length; i++)
        {
            TreeInstance tree = originalInstances[i];
            if (!settingByPrototype.TryGetValue(tree.prototypeIndex, out PrototypeSetting setting) ||
                !setting.choppable || setting.interactivePrefab == null) continue;

            // Posisi + nama prototype, bukan index array: repaint pohon lain tidak menggeser ID.
            string key = FormattableString.Invariant(
                $"{prefix}/{setting.terrainPrefab.name}/{tree.position.x:R}/{tree.position.y:R}/{tree.position.z:R}");
            duplicates.TryGetValue(key, out int duplicate);
            duplicates[key] = duplicate + 1;
            TreeRecord record = new()
            {
                id = key + "/" + duplicate,
                terrainInstanceIndex = i,
                prototypeIndex = tree.prototypeIndex,
                worldPosition = targetTerrain.transform.position + Vector3.Scale(tree.position, sourceData.size),
                rotationRadians = tree.rotation,
                widthScale = tree.widthScale,
                heightScale = tree.heightScale,
                durability = setting.interactivePrefab.StandingDurability,
                progress = TreeProgress.Create(DefinitionFor(setting.interactivePrefab), TimeManager.Instance != null ? TimeManager.Instance.day : 1, true)
            };
            records.Add(record);
            Vector2Int cell = Cell(record.worldPosition);
            if (!cells.TryGetValue(cell, out List<TreeRecord> bucket))
                cells[cell] = bucket = new List<TreeRecord>();
            bucket.Add(record);
            if (!pools.ContainsKey(record.prototypeIndex))
                pools.Add(record.prototypeIndex, new Stack<WorldTree>());
        }
    }

    void Update()
    {
        if (!initialized) return;
        LastFrameSwitches = 0;
        if (player == null && Time.time >= nextCheckTime)
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
            else nextCheckTime = Time.time + 1f;
        }

        // Maksimum 50 record aktif: tidak membuat TreeSaveData baru setiap frame.
        for (int i = 0; i < active.Count; i++) PullState(active[i]);
        ProcessMaintenance();
        if (player != null)
        {
            if (!scanning && Time.time >= nextCheckTime) BeginScan();
            if (scanning) ContinueScan();
        }
        ProcessSwitches();
    }

    Vector2Int Cell(Vector3 position) => new(
        Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));

    void BeginScan()
    {
        scanning = true;
        scanPosition = player.position;
        scanMin = Cell(scanPosition - Vector3.one * deactivateDistance);
        scanMax = Cell(scanPosition + Vector3.one * deactivateDistance);
        scanX = scanMin.x;
        scanZ = scanMin.y;
        scanIndex = 0;
        candidates.Clear();
        candidateDistances.Clear();
    }

    void ContinueScan()
    {
        int budget = Mathf.Max(16, maxRecordChecksPerFrame);
        while (scanZ <= scanMax.y && budget-- > 0)
        {
            if (!cells.TryGetValue(new Vector2Int(scanX, scanZ), out List<TreeRecord> bucket) ||
                scanIndex >= bucket.Count)
            {
                scanIndex = 0;
                if (++scanX > scanMax.x) { scanX = scanMin.x; scanZ++; }
                continue;
            }
            TreeRecord record = bucket[scanIndex++];
            if (record.state == TreeGrowthState.Depleted) continue;
            float distance = (record.worldPosition - scanPosition).sqrMagnitude;
            float radius = record.activeObject != null ? deactivateDistance : activateDistance;
            if (distance > radius * radius) continue;
            // Bias kecil mempertahankan object aktif agar tidak thrash saat jarak hampir sama.
            float score = Mathf.Sqrt(distance) - (record.activeObject != null ? 1f : 0f);
            int index = candidateDistances.BinarySearch(score);
            if (index < 0) index = ~index;
            if (index >= maxActiveTrees) continue;
            candidates.Insert(index, record);
            candidateDistances.Insert(index, score);
            if (candidates.Count > maxActiveTrees)
            {
                candidates.RemoveAt(candidates.Count - 1);
                candidateDistances.RemoveAt(candidateDistances.Count - 1);
            }
        }
        if (scanZ <= scanMax.y) return;
        scanning = false;
        nextCheckTime = Time.time + checkInterval;
        desired.Clear();
        desiredSet.Clear();
        // Abaikan snapshot lama jika player teleport saat scan berlangsung.
        if ((player.position - scanPosition).sqrMagnitude > activateDistance * activateDistance)
        {
            nextCheckTime = 0f;
            return;
        }
        desired.AddRange(candidates);
        foreach (TreeRecord record in desired) desiredSet.Add(record);
    }

    void ProcessSwitches()
    {
        if (sharedFrame != Time.frameCount)
        {
            sharedFrame = Time.frameCount;
            sharedSwitches = 0;
        }
        int budget = Mathf.Max(1, maxSwitchesPerFrame);
        for (int i = active.Count - 1; i >= 0 && sharedSwitches < budget; i--)
        {
            TreeRecord record = active[i];
            if (record.activeObject != null && record.activeObject.IsTransitioning) continue;
            bool tooFar = player == null ||
                (record.worldPosition - player.position).sqrMagnitude > deactivateDistance * deactivateDistance;
            if (record.state == TreeGrowthState.Depleted || tooFar || !desiredSet.Contains(record))
            {
                Deactivate(record, false);
                CountSwitch();
            }
        }
        // Regrow/load visual updates memakai budget yang sama dengan aktivasi/deaktivasi.
        int visualChecks = Mathf.Max(16, maxRecordChecksPerFrame);
        while (visualQueue.Count > 0 && sharedSwitches < budget && visualChecks-- > 0)
        {
            TreeRecord record = visualQueue.Dequeue();
            queuedVisuals.Remove(record);
            if (SetTerrainVisible(record, record.activeObject == null && record.state == TreeGrowthState.Standing))
                CountSwitch();
        }
        if (player == null) return;
        for (int i = 0; i < desired.Count && sharedSwitches < budget && TotalActiveTreeCount < maxActiveTrees; i++)
        {
            TreeRecord record = desired[i];
            if (record.activeObject != null || record.state == TreeGrowthState.Depleted ||
                (record.worldPosition - player.position).sqrMagnitude > activateDistance * activateDistance) continue;
            Activate(record);
            CountSwitch();
        }
    }

    void CountSwitch()
    {
        LastFrameSwitches++;
        sharedSwitches++;
    }

    void Activate(TreeRecord record)
    {
        WorldTree prefab = settingByPrototype[record.prototypeIndex].interactivePrefab;
        Stack<WorldTree> pool = pools[record.prototypeIndex];
        WorldTree tree;
        if (pool.Count > 0) { tree = pool.Pop(); pooledCount--; }
        else tree = Instantiate(prefab, runtimeRoot);
        tree.SetManagedIdentity(record.id, settingByPrototype[record.prototypeIndex].removeStumpAfterFelling);
        tree.transform.SetPositionAndRotation(record.worldPosition,
            Quaternion.Euler(0f, record.rotationRadians * Mathf.Rad2Deg, 0f) * prefab.transform.localRotation);
        tree.transform.localScale = Vector3.Scale(prefab.transform.localScale,
            new Vector3(record.widthScale, record.heightScale, record.widthScale));
        // Prefab aktif diperlukan agar Awake selesai sebelum Restore (divalidasi oleh setup).
        tree.Restore(new TreeSaveData
        {
            id = record.id, state = record.state,
            durability = record.durability, respawnDay = record.respawnDay, progress = record.progress
        });
        tree.gameObject.SetActive(true);
        record.activeObject = tree;
        active.Add(record);
        SetTerrainVisible(record, false);
    }

    void Deactivate(TreeRecord record, bool finishTransition)
    {
        WorldTree tree = record.activeObject;
        if (tree != null)
        {
            if (finishTransition) tree.FinishPendingFall();
            PullState(record);
            tree.gameObject.SetActive(false);
            if (pooledCount < maxPooledTrees)
            {
                pools[record.prototypeIndex].Push(tree);
                pooledCount++;
            }
            else Destroy(tree.gameObject);
        }
        record.activeObject = null;
        active.Remove(record);
        SetTerrainVisible(record, record.state == TreeGrowthState.Standing);
    }

    static void PullState(TreeRecord record)
    {
        WorldTree tree = record.activeObject;
        if (tree == null) return;
        record.state = tree.GrowthState;
        record.durability = tree.Durability;
        record.respawnDay = tree.RespawnDay;
        record.progress = tree.Progress;
    }

    static TreeDefinition DefinitionFor(WorldTree tree) => tree.definition != null
        ? tree.definition : Resources.Load<TreeDefinition>("Trees/Wild Small");

    void AdvanceGrowth()
    {
        if (!initialized) return;
        int day = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        WeatherType weather = WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : WeatherType.Sunny;
        // Hanya data ringan per hari; perubahan object/mesh tetap melalui budget switch per frame.
        foreach (TreeRecord record in records)
        {
            PullState(record);
            if (record.state != TreeGrowthState.Standing) continue;
            TreeDefinition definition = DefinitionFor(settingByPrototype[record.prototypeIndex].interactivePrefab);
            record.progress ??= TreeProgress.Create(definition, day, true);
            record.progress.Advance(definition, day, weather, FarmPlacement.Covered(record.worldPosition), UnityEngine.Random.value);
            if (record.activeObject != null)
                record.activeObject.RefreshGrowthVisual();
            QueueVisual(record);
        }
    }

    Vector3 GrowthScale(TreeRecord record)
    {
        TreeDefinition definition = DefinitionFor(settingByPrototype[record.prototypeIndex].interactivePrefab);
        int index = definition != null && record.progress != null ? definition.StageIndex(record.progress.growth) : -1;
        return index >= 0 ? definition.stages[index].scale : Vector3.one;
    }

    void HandleDayChanged()
    {
        today = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        maintenanceIndex = 0;
        nextCheckTime = 0f;
    }

    void ProcessMaintenance()
    {
        if (maintenanceIndex < 0) return;
        int remaining = Mathf.Max(16, maxRecordChecksPerFrame);
        while (maintenanceIndex < records.Count && remaining-- > 0)
        {
            TreeRecord record = records[maintenanceIndex++];
            if (record.state != TreeGrowthState.Depleted || record.respawnDay <= 0 ||
                record.respawnDay == int.MaxValue || today < record.respawnDay) continue;
            record.state = TreeGrowthState.Standing;
            record.durability = settingByPrototype[record.prototypeIndex].interactivePrefab.StandingDurability;
            record.respawnDay = 0;
            record.progress = TreeProgress.Create(DefinitionFor(settingByPrototype[record.prototypeIndex].interactivePrefab), today, false);
            if (record.activeObject != null)
                record.activeObject.Restore(new TreeSaveData
                {
                    id = record.id, state = record.state, durability = record.durability, progress = record.progress
                });
            QueueVisual(record);
            nextCheckTime = 0f;
        }
        if (maintenanceIndex >= records.Count) maintenanceIndex = -1;
    }

    void QueueVisual(TreeRecord record)
    {
        bool visible = record.activeObject == null && record.state == TreeGrowthState.Standing;
        if (record.terrainVisible == visible && record.displayedScale == GrowthScale(record)) return;
        if (queuedVisuals.Add(record)) visualQueue.Enqueue(record);
    }

    bool SetTerrainVisible(TreeRecord record, bool visible)
    {
        Vector3 scale = GrowthScale(record);
        if (record.terrainVisible == visible && record.displayedScale == scale) return false;
        TreeInstance instance = originalInstances[record.terrainInstanceIndex];
        if (!visible) { instance.widthScale = 0f; instance.heightScale = 0f; }
        else { instance.widthScale *= scale.x; instance.heightScale *= scale.y; }
        runtimeData.SetTreeInstance(record.terrainInstanceIndex, instance);
        record.terrainVisible = visible;
        record.displayedScale = scale;
        return true;
    }

    public List<TreeSaveData> CaptureManagedTrees()
    {
        List<TreeSaveData> result = new(records.Count);
        foreach (TreeRecord record in records)
        {
            // Selesaikan jatuh sebelum snapshot supaya reward tidak hilang jika save saat animasi.
            if (record.activeObject != null) record.activeObject.FinishPendingFall();
            PullState(record);
            result.Add(new TreeSaveData
            {
                id = record.id, state = record.state,
                durability = record.durability, respawnDay = record.respawnDay, progress = record.progress?.Copy()
            });
        }
        return result;
    }

    public void RestoreManagedTrees(List<TreeSaveData> data)
    {
        Dictionary<string, TreeSaveData> byId = new();
        if (data != null)
            foreach (TreeSaveData saved in data)
                if (saved != null && !string.IsNullOrEmpty(saved.id)) byId[saved.id] = saved;
        foreach (TreeRecord record in records)
        {
            WorldTree prefab = settingByPrototype[record.prototypeIndex].interactivePrefab;
            if (!byId.TryGetValue(record.id, out TreeSaveData saved))
                byId.TryGetValue($"{targetTerrain.name}_tree_{record.terrainInstanceIndex}", out saved);
            record.state = saved != null ? saved.state : TreeGrowthState.Standing;
            record.durability = saved != null ? saved.durability : prefab.StandingDurability;
            if (record.state != TreeGrowthState.Depleted && record.durability <= 0)
                record.durability = record.state == TreeGrowthState.Stump ? prefab.StumpDurability : prefab.StandingDurability;
            record.respawnDay = saved != null ? saved.respawnDay : 0;
            record.progress = saved?.progress?.Copy() ?? TreeProgress.Create(DefinitionFor(prefab), TimeManager.Instance != null ? TimeManager.Instance.day : 1, true);
            if (record.activeObject != null)
                record.activeObject.Restore(new TreeSaveData
                {
                    id = record.id, state = record.state,
                    durability = record.durability, respawnDay = record.respawnDay, progress = record.progress
                });
            QueueVisual(record);
        }
        HandleDayChanged();
        scanning = false;
        desired.Clear();
        desiredSet.Clear();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(terrainId)) terrainId = Guid.NewGuid().ToString("N");
        activateDistance = Mathf.Max(1f, activateDistance);
        deactivateDistance = Mathf.Max(activateDistance + 1f, deactivateDistance);
        maxActiveTrees = Mathf.Max(1, maxActiveTrees);
        maxSwitchesPerFrame = Mathf.Max(1, maxSwitchesPerFrame);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, activateDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, deactivateDistance);
    }
#endif
}
