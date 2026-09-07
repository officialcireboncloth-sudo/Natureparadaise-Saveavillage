using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[SelectionBase]
/// <summary>
/// Pemilik grid farming modular dengan ukuran manual. Mengelola data tile, simulasi batch,
/// visual tanah/tanaman, interaksi fitur eksternal, serta save/load tanpa Update per tile.
/// </summary>
public sealed class FieldArea : MonoBehaviour
{
    static readonly List<FieldArea> ActiveAreasInternal = new List<FieldArea>();

    public static IReadOnlyList<FieldArea> ActiveAreas => ActiveAreasInternal;

    [Header("Identity")]
    [SerializeField] string fieldId = "player-home-field";

    [Header("Manual Grid Size")]
    [SerializeField, Min(1)] int columns = 10;
    [SerializeField, Min(1)] int rows = 10;
    [SerializeField, Min(0.25f)] float cellSize = 1f;
    [SerializeField] bool allowCrops = true;
    [SerializeField] bool allowBuildings = true;
    [SerializeField] bool showGridGizmo = true;
    [SerializeField] BoxCollider areaCollider;

    [Header("Field Surface")]
    [SerializeField] Transform fieldSurface;
    [SerializeField] Vector2 fieldSurfaceBaseSize = new Vector2(10f, 10f);
    [SerializeField] float fieldSurfaceYOffset;

    [Header("Simulation")]
    [SerializeField] SoilProfileSO soilProfile;
    [SerializeField] CropDataSO defaultCrop;
    [SerializeField] List<CropDataSO> cropCatalog = new List<CropDataSO>();
    [SerializeField, Min(1)] int daysPerSeason = 28;

    [Header("Visual Templates")]
    [SerializeField] GameObject cropPrefab;
    [SerializeField] GameObject hoeMarkPrefab;

    [Header("Soil Visual State")]
    [SerializeField] Material normalSoilMaterial;
    [Tooltip("Material lama dipertahankan agar scene lama tetap kompatibel; visual baru memakai property per tile.")]
    [SerializeField, HideInInspector] Material wateredSoilMaterial;
    [SerializeField, HideInInspector] Material fertilizedSoilMaterial;
    [SerializeField, HideInInspector] Material wateredAndFertilizedSoilMaterial;
    [SerializeField, Range(0, 100)] int wateredVisualThreshold = 50;
    [SerializeField, HideInInspector, Range(0, 100)] int fertilizedVisualThreshold = 75;

    [Header("Mobile LOD")]
    [SerializeField, Min(4)] int chunkSize = 8;
    [SerializeField, Min(1f)] float visualDistance = 45f;
    [SerializeField, Min(0.1f)] float lodCheckInterval = 0.25f;
    [SerializeField] Transform lodTarget;

    FieldTileData[] tiles;
    GameObject[] hoeViews;
    FieldCropView[] cropViews;
    bool[] activeFlags;
    readonly List<int> activeTileIndices = new List<int>();
    readonly Stack<GameObject> hoePool = new Stack<GameObject>();
    readonly Stack<FieldCropView> cropPool = new Stack<FieldCropView>();
    Transform visualRoot;
    float nextLodCheckTime;
    MaterialPropertyBlock soilVisualProperties;

    static readonly int WetnessProperty = Shader.PropertyToID("_Wetness");
    static readonly int FertilizedProperty = Shader.PropertyToID("_Fertilized");
    static readonly int SmoothnessProperty = Shader.PropertyToID("_Smoothness");

    public string FieldId => fieldId;
    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public int ChunkSize => chunkSize;
    public Vector2 WorldSize => new Vector2(columns * cellSize, rows * cellSize);

    public static int GetSoilLevel(int durability)
    {
        if (durability >= 65) return 5;
        if (durability >= 49) return 4;
        if (durability >= 33) return 3;
        if (durability >= 17) return 2;
        return 1;
    }

    public static SoilDurabilityStatus GetSoilStatus(int durability)
    {
        return GetSoilLevel(durability) switch
        {
            5 => SoilDurabilityStatus.VeryFertile,
            4 => SoilDurabilityStatus.Fertile,
            3 => SoilDurabilityStatus.Normal,
            2 => SoilDurabilityStatus.LowFertility,
            _ => SoilDurabilityStatus.Barren
        };
    }

    public static string GetSoilStatusName(SoilDurabilityStatus status)
    {
        return status switch
        {
            SoilDurabilityStatus.VeryFertile => "Sangat Subur",
            SoilDurabilityStatus.Fertile => "Subur",
            SoilDurabilityStatus.Normal => "Normal",
            SoilDurabilityStatus.LowFertility => "Kurang Subur",
            _ => "Tandus"
        };
    }

    public event Action<FieldArea, Vector2Int> TileChanged;
    public event Action<CropHarvestResult> CropHarvested;

    void Awake()
    {
        Register();
        ResolveReferences();
        InitializeData();
        EnsureVisualRoot();
    }

    void OnEnable()
    {
        Register();
        TimeManager.OnHour += HandleHourChanged;
        TimeManager.OnBeforeDayChange += HandleDayChanged;
        WeatherSystem.CurrentWeatherChanged += HandleCurrentWeatherChanged;
    }

    void OnDisable()
    {
        TimeManager.OnHour -= HandleHourChanged;
        TimeManager.OnBeforeDayChange -= HandleDayChanged;
        WeatherSystem.CurrentWeatherChanged -= HandleCurrentWeatherChanged;
        ActiveAreasInternal.Remove(this);
    }

    void Update()
    {
        // LOD hanya memeriksa jarak pada interval, bukan setiap frame. Data tanah tetap aktif
        // ketika visual dimatikan sehingga player tidak dapat menghentikan simulasi dengan menjauh.
        if (lodTarget == null || Time.unscaledTime < nextLodCheckTime)
            return;

        nextLodCheckTime = Time.unscaledTime + lodCheckInterval;
        bool shouldShow = (lodTarget.position - transform.position).sqrMagnitude <= visualDistance * visualDistance;
        if (visualRoot != null && visualRoot.gameObject.activeSelf != shouldShow)
            visualRoot.gameObject.SetActive(shouldShow);
    }

    void Register()
    {
        if (!ActiveAreasInternal.Contains(this))
            ActiveAreasInternal.Add(this);
    }

    void ResolveReferences()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<BoxCollider>();

        if (fieldSurface == null)
            fieldSurface = transform.Find("FieldSurface");

        if (lodTarget == null)
        {
            Inventory inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null)
                lodTarget = inventory.transform;
        }

        if (defaultCrop != null && !cropCatalog.Contains(defaultCrop))
            cropCatalog.Add(defaultCrop);

        SyncColliderToGrid();
        SyncSurfaceToGrid();
    }

    void InitializeData()
    {
        int count = columns * rows;
        tiles = new FieldTileData[count];
        hoeViews = new GameObject[count];
        cropViews = new FieldCropView[count];
        activeFlags = new bool[count];
        activeTileIndices.Clear();

        for (int i = 0; i < count; i++)
            tiles[i] = CreateDefaultTile();
    }

    FieldTileData CreateDefaultTile()
    {
        byte maximumDurability = soilProfile != null ? soilProfile.MaximumDurability : (byte)80;
        return new FieldTileData
        {
            state = TileState.Empty,
            soilQuality = 100,
            fertility = soilProfile != null ? soilProfile.InitialFertility : (byte)60,
            moisture = soilProfile != null ? soilProfile.InitialMoisture : (byte)35,
            cropHealth = 100,
            soilDurability = maximumDurability
        };
    }

    void EnsureVisualRoot()
    {
        if (visualRoot != null)
            return;

        GameObject root = new GameObject("FieldVisuals_Runtime");
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
    }

    /// <summary>Mencangkul satu tile kosong tanpa mengubah durability tanah.</summary>
    public bool TryHoe(int x, int z)
    {
        if (!CanHoe(x, z) || !TryGetIndex(x, z, out int index))
            return false;

        FieldTileData tile = tiles[index];
        tile.state = TileState.Hoed;
        tile.dirty = true;
        tiles[index] = tile;
        if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsRainToday)
        {
            MarkWateredAtIndex(
                index,
                CropWaterSource.Rain,
                WeatherSystem.GetRainMoistureAmount(WeatherSystem.Instance.CurrentWeather)
            );
        }
        SetActive(index, true);
        ShowHoeView(index, x, z);
        if (FarmPlacement.Covered(this, x, z)) WaterBySprinkler(x, z);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Memeriksa batas grid, occupancy, crop, dan aturan tile sebelum hoe.</summary>
    public bool CanHoe(int x, int z)
    {
        return !FarmPlacement.Occupied(this, x, z) && TryGetIndex(x, z, out int index) && tiles[index].state == TileState.Empty;
    }

    public bool CanPlaceFarmItem(int x, int z) => allowCrops &&
        !FarmPlacement.Occupied(this, x, z) && TryGetIndex(x, z, out int index) &&
        (tiles[index].state == TileState.Empty || tiles[index].state == TileState.Hoed);

    public void WaterBySprinkler(int x, int z)
    {
        if (!IsEditableSoilTile(x, z, out int index) || tiles[index].waterSourcesToday != CropWaterSource.None) return;
        MarkWateredAtIndex(index, CropWaterSource.Sprinkler, 35);
        NotifyChanged(x, z);
    }

    /// <summary>Validasi bersama untuk preview bibit dan transaksi penanaman.</summary>
    public bool CanPlant(int x, int z, CropDataSO crop)
    {
        return allowCrops && crop != null && !FarmPlacement.Occupied(this, x, z) &&
            TryGetIndex(x, z, out int index) && tiles[index].state == TileState.Hoed &&
            tiles[index].soilDurability > 0;
    }

    /// <summary>Menanam crop pada tile hoed yang kosong.</summary>
    public bool TryPlant(int x, int z, CropDataSO crop = null)
    {
        crop = crop != null ? crop : defaultCrop;
        if (!CanPlant(x, z, crop) || !TryGetIndex(x, z, out int index))
        {
            return false;
        }

        FieldTileData tile = tiles[index];
        tile.state = TileState.Planted;
        tile.crop = crop;
        tile.growthDays = 0f;
        tile.growthStage = 0;
        tile.cropHealth = 100;
        tile.cropState = CropLifecycleState.Growing;
        tile.consecutiveDryDays = 0;
        tile.recoveryWateredDays = 0;
        tile.growthBoosterPercent = 0;
        tile.qualityCare = new CropQualityCare();
        tile.regrowDaysRemaining = 0f;
        tile.soilRestDays = 0;
        tile.careSamples = 0;
        tile.totalMoisture = 0;
        tile.totalFertility = 0;
        tile.totalSoilQuality = 0;
        tile.buildingId = null;
        tile.dirty = true;
        tiles[index] = tile;

        SetActive(index, true);
        ShowCropView(index, x, z);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Menambah moisture satu tile; nilai negatif memakai default SoilProfile.</summary>
    public bool TryWater(int x, int z, int amount = -1)
    {
        if (WorldTree.WaterAt(GridToWorld(x, z), cellSize * 0.45f)) return true;
        if (!IsEditableSoilTile(x, z, out int index))
            return false;

        int value = amount >= 0 ? amount : soilProfile != null ? soilProfile.waterAmount : 35;
        MarkWateredAtIndex(index, CropWaterSource.WateringCan, value);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Memulihkan durability tanah tanpa menggantikan air atau booster kualitas.</summary>
    public bool TryFertilize(int x, int z, int fertilizerLevel = 1, int restoreAmount = 0)
    {
        if (!IsEditableSoilTile(x, z, out int index))
            return false;

        FieldTileData tile = tiles[index];
        if (tile.state != TileState.Hoed && !tile.HasCrop)
            return false;
        int maximum = soilProfile != null ? soilProfile.MaximumDurability : 80;
        int restore = restoreAmount > 0 ? restoreAmount : fertilizerLevel switch
        { 1 => 10, 2 => 20, 3 => 35, 4 => 50, 5 => 80, _ => 0 };
        if (restore <= 0 || tile.soilDurability >= maximum) return false;
        tile.soilDurability = (byte)Mathf.Min(maximum, tile.soilDurability + restore);
        tile.soilQuality = DurabilityToQuality(tile.soilDurability, maximum);
        tile.fertilizedForCurrentCycle = true;
        tile.dirty = true;
        tiles[index] = tile;
        NotifyChanged(x, z);
        return true;
    }

    public bool CanFertilize(int x, int z)
    {
        if (!IsEditableSoilTile(x, z, out int index))
            return false;
        if (tiles[index].state != TileState.Hoed && !tiles[index].HasCrop)
            return false;
        int maximum = soilProfile != null ? soilProfile.MaximumDurability : 80;
        return tiles[index].soilDurability < maximum;
    }

    /// <summary>Menerapkan satu booster kualitas per hari pada tanaman yang masih tumbuh.</summary>
    public bool TryApplyCropBooster(int x, int z, int level)
    {
        if (!TryGetIndex(x, z, out int index) || !tiles[index].HasCrop || level < 1 || level > 5)
            return false;
        FieldTileData tile = tiles[index];
        if (tile.cropState == CropLifecycleState.Dead || tile.cropState == CropLifecycleState.HarvestReady)
            return false;
        tile.qualityCare ??= new CropQualityCare();
        if (tile.qualityCare.boosterToday > 0) return false;
        tile.qualityCare.boosterToday = level;
        tile.dirty = true;
        tiles[index] = tile;
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Hook sistem hama: riwayat kerusakan tidak hilang setelah pest disembuhkan.</summary>
    public void ReportCropPest(int x, int z)
    {
        if (!TryGetIndex(x, z, out int index) || !tiles[index].HasCrop) return;
        FieldTileData tile = tiles[index];
        tile.qualityCare ??= new CropQualityCare();
        tile.qualityCare.pestDamage = true;
        tile.dirty = true;
        tiles[index] = tile;
        NotifyChanged(x, z);
    }

    /// <summary>API sprinkler: menyiram seluruh tile dalam radius dunia dan menandai sumber air.</summary>
    public int ApplySprinkler(Vector3 worldCenter, float radius, int moistureAmount = 35)
    {
        int watered = 0;
        float radiusSquared = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            IndexToCoordinate(index, out int x, out int z);
            Vector3 delta = GridToWorld(x, z) - worldCenter;
            delta.y = 0f;
            if (delta.sqrMagnitude > radiusSquared || !IsEditableSoilTile(x, z, out _))
                continue;
            MarkWateredAtIndex(index, CropWaterSource.Sprinkler, moistureAmount);
            NotifyChanged(x, z);
            watered++;
        }
        return watered;
    }

    bool IsEditableSoilTile(int x, int z, out int index)
    {
        if (!TryGetIndex(x, z, out index))
            return false;

        TileState state = tiles[index].state;
        return state == TileState.Hoed || state == TileState.Planted;
    }

    /// <summary>Menerapkan efek modular ke seluruh tile aktif secara batch.</summary>
    public void ApplyAreaEffect(FieldEffect effect)
    {
        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            ApplyEffectAtIndex(index, effect);
            IndexToCoordinate(index, out int x, out int z);
            NotifyChanged(x, z);
        }
    }

    /// <summary>Menerapkan satu efek modular pada koordinat tile tertentu.</summary>
    public bool TryApplyEffect(int x, int z, FieldEffect effect)
    {
        if (!TryGetIndex(x, z, out int index))
            return false;

        ApplyEffectAtIndex(index, effect);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Menghapus crop karena dimakan hewan atau fitur eksternal.</summary>
    public bool TryConsumeCrop(int x, int z)
    {
        if (!TryGetIndex(x, z, out int index) || !tiles[index].HasCrop)
            return false;

        FieldTileData tile = tiles[index];
        tile.state = TileState.Hoed;
        tile.crop = null;
        tile.growthDays = 0f;
        tile.growthStage = 0;
        tile.cropHealth = 100;
        tile.fertilizedForCurrentCycle = false;
        tile.careSamples = 0;
        tile.totalMoisture = 0;
        tile.totalFertility = 0;
        tile.totalSoilQuality = 0;
        tile.dirty = true;
        tiles[index] = tile;

        HideCropView(index);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Memanen crop matang, menghitung grade/yield, lalu memasukkan hasil ke inventory.</summary>
    public bool TryHarvest(int x, int z, Inventory inventory, out CropGrade grade)
    {
        grade = CropGrade.D;
        if (inventory == null || !TryGetIndex(x, z, out int index))
            return false;

        FieldTileData tile = tiles[index];
        if (!tile.HasCrop || tile.cropState != CropLifecycleState.HarvestReady)
            return false;

        tile.qualityCare ??= new CropQualityCare();
        tiles[index] = tile;
        grade = CalculateGrade(tile);
        int amount = Mathf.Max(1, tile.crop.baseYield);
        ItemSO item = tile.crop.produceItem;
        if (item == null || !inventory.Add(item, amount, (int)grade + 1))
            return false;

        CropHarvestResult result = new CropHarvestResult(
            fieldId,
            new Vector2Int(x, z),
            item,
            amount,
            grade
        );

        CropDataSO harvestedCrop = tile.crop;
        // Durability sudah dibebankan satu kali per hari selama crop menempati tanah.
        // Panen tidak memberi penalti tambahan agar masa pakai tetap tepat 80 hari.
        if (harvestedCrop.regrowsAfterHarvest)
        {
            tile.qualityCare = new CropQualityCare();
            tile.cropState = CropLifecycleState.Regrowing;
            tile.regrowDaysRemaining = Mathf.Max(1, harvestedCrop.regrowDays);
            tile.growthStage = (byte)harvestedCrop.GetRegrowStage();
            tile.waterSourcesToday = CropWaterSource.None;
            tile.consecutiveDryDays = 0;
            tile.recoveryWateredDays = 0;
        }
        else
        {
            tile.state = TileState.Hoed;
            tile.crop = null;
            tile.growthDays = 0f;
            tile.growthStage = 0;
            tile.cropHealth = 100;
            tile.fertilizedForCurrentCycle = false;
            tile.cropState = CropLifecycleState.Growing;
            tile.waterSourcesToday = CropWaterSource.None;
            tile.consecutiveDryDays = 0;
            tile.recoveryWateredDays = 0;
            tile.growthBoosterPercent = 0;
        tile.qualityCare = new CropQualityCare();
            tile.regrowDaysRemaining = 0f;
            tile.careSamples = 0;
            tile.totalMoisture = 0;
            tile.totalFertility = 0;
            tile.totalSoilQuality = 0;
        }
        tile.dirty = true;
        tiles[index] = tile;

        if (tile.HasCrop && cropViews[index] != null)
            cropViews[index].ApplyStage(tile.growthStage, tile.crop, tile.cropState);
        else
            HideCropView(index);
        NotifyChanged(x, z);
        CropHarvested?.Invoke(result);
        return true;
    }

    /// <summary>Memvalidasi footprint bangunan terhadap batas dan occupancy grid.</summary>
    public bool CanPlaceBuilding(int startX, int startZ, int width, int depth)
    {
        if (!allowBuildings || width < 1 || depth < 1)
            return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + depth; z++)
            {
                if (FarmPlacement.Occupied(this, x, z) || !TryGetIndex(x, z, out int index) || tiles[index].state != TileState.Empty)
                    return false;
            }
        }

        return true;
    }

    /// <summary>Menandai footprint bangunan pada grid menggunakan ID persisten.</summary>
    public bool TryPlaceBuilding(int startX, int startZ, int width, int depth, string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || !CanPlaceBuilding(startX, startZ, width, depth))
            return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + depth; z++)
            {
                int index = ToIndex(x, z);
                FieldTileData tile = tiles[index];
                tile.state = TileState.Building;
                tile.buildingId = buildingId;
                tile.dirty = true;
                tiles[index] = tile;
                SetActive(index, true);
                NotifyChanged(x, z);
            }
        }

        return true;
    }

    /// <summary>Melepas seluruh tile yang dimiliki building ID tertentu.</summary>
    public bool TryRemoveBuilding(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return false;

        bool removedAny = false;
        for (int index = 0; index < tiles.Length; index++)
        {
            FieldTileData tile = tiles[index];
            if (tile.state != TileState.Building || tile.buildingId != buildingId)
                continue;

            tile.state = TileState.Empty;
            tile.buildingId = null;
            tile.dirty = true;
            tiles[index] = tile;
            SetActive(index, false);
            IndexToCoordinate(index, out int x, out int z);
            NotifyChanged(x, z);
            removedAny = true;
        }

        return removedAny;
    }

    /// <summary>Mengambil snapshot read-only satu tile untuk UI atau fitur eksternal.</summary>
    public bool TryGetSnapshot(int x, int z, out FieldTileSnapshot snapshot)
    {
        if (!TryGetIndex(x, z, out int index))
        {
            snapshot = default;
            return false;
        }

        snapshot = new FieldTileSnapshot(tiles[index]);
        return true;
    }

    /// <summary>Mengambil view crop runtime tanpa memberi akses langsung ke array tile.</summary>
    public bool TryGetCropView(int x, int z, out FieldCropView cropView)
    {
        if (!TryGetIndex(x, z, out int index) || cropViews == null)
        {
            cropView = null;
            return false;
        }

        cropView = cropViews[index];
        return cropView != null;
    }

    /// <summary>Memotong crop farming dalam radius sickle dan mengembalikan jumlah yang terkena.</summary>
    public static int CutCropsInRadius(Vector3 worldCenter, float radius)
    {
        float radiusSquared = radius * radius;
        int cut = 0;
        for (int areaIndex = 0; areaIndex < ActiveAreasInternal.Count; areaIndex++)
        {
            FieldArea area = ActiveAreasInternal[areaIndex];
            if (area == null || area.tiles == null) continue;
            for (int index = 0; index < area.tiles.Length; index++)
            {
                FieldTileData tile = area.tiles[index];
                if (tile.state != TileState.Planted || tile.crop == null) continue;
                area.IndexToCoordinate(index, out int x, out int z);
                Vector3 delta = area.GridToWorld(x, z) - worldCenter;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSquared) continue;

                tile.state = TileState.Hoed;
                tile.crop = null;
                tile.growthDays = 0f;
                tile.growthStage = 0;
                tile.cropHealth = 100;
                tile.fertilizedForCurrentCycle = false;
                tile.dirty = true;
                area.tiles[index] = tile;
                area.HideCropView(index);
                area.NotifyChanged(x, z);
                cut++;
            }
        }
        return cut;
    }

    /// <summary>Mengubah koordinat tile menjadi titik tengah world-space.</summary>
    public Vector3 GridToWorld(int x, int z)
    {
        Vector3 local = new Vector3(
            -columns * cellSize * 0.5f + (x + 0.5f) * cellSize,
            0f,
            -rows * cellSize * 0.5f + (z + 0.5f) * cellSize
        );
        return transform.TransformPoint(local);
    }

    /// <summary>Mengubah world-space ke koordinat tile jika masih berada dalam area.</summary>
    public bool WorldToGrid(Vector3 worldPosition, out int x, out int z)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        x = Mathf.FloorToInt((local.x + columns * cellSize * 0.5f) / cellSize);
        z = Mathf.FloorToInt((local.z + rows * cellSize * 0.5f) / cellSize);
        return InBounds(x, z);
    }

    /// <summary>Mencari FieldArea aktif yang memiliki posisi world tertentu.</summary>
    public static bool TryGetAt(Vector3 worldPosition, out FieldArea field, out int x, out int z)
    {
        for (int i = ActiveAreasInternal.Count - 1; i >= 0; i--)
        {
            FieldArea candidate = ActiveAreasInternal[i];
            if (candidate != null && candidate.WorldToGrid(worldPosition, out x, out z))
            {
                field = candidate;
                return true;
            }
        }

        field = null;
        x = -1;
        z = -1;
        return false;
    }

    void HandleHourChanged()
    {
        int evaporation = soilProfile != null ? soilProfile.hourlyEvaporation : 2;
        if (evaporation <= 0)
            return;

        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            FieldTileData tile = tiles[index];
            if (tile.state != TileState.Hoed && tile.state != TileState.Planted)
                continue;

            byte previous = tile.moisture;
            tile.moisture = AddClamped(tile.moisture, -evaporation);
            if (previous == tile.moisture)
                continue;

            tile.dirty = true;
            tiles[index] = tile;
            IndexToCoordinate(index, out int x, out int z);
            NotifyChanged(x, z);
        }
    }

    void HandleCurrentWeatherChanged(WeatherType weather)
    {
        int rainAmount = WeatherSystem.GetRainMoistureAmount(weather);
        if (rainAmount <= 0)
            return;

        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            FieldTileData tile = tiles[index];
            if (tile.state != TileState.Hoed && tile.state != TileState.Planted)
                continue;

            MarkWateredAtIndex(index, CropWaterSource.Rain, rainAmount);
            IndexToCoordinate(index, out int x, out int z);
            NotifyChanged(x, z);
        }
    }

    void HandleDayChanged()
    {
        FarmPlacement.WaterField(this);
        // Hanya activeTileIndices yang diproses. Tile kosong tidak ikut loop harian agar field
        // besar tetap murah di perangkat mobile dan tidak membutuhkan Update per tanaman.
        int fertilityRecovery = soilProfile != null ? soilProfile.dailyFertilityRecovery : 0;
        int maximumDurability = soilProfile != null ? soilProfile.MaximumDurability : 80;
        int durabilityLoss = soilProfile != null ? Mathf.Max(1, soilProfile.durabilityLossPerCropDay) : 1;
        int restDaysPerRecovery = soilProfile != null ? Mathf.Max(1, soilProfile.restDaysPerRecovery) : 5;
        int durabilityRecovery = soilProfile != null ? Mathf.Max(1, soilProfile.durabilityRecoveredPerRestCycle) : 20;

        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            FieldTileData tile = tiles[index];
            bool changed = false;

            if (tile.HasCrop)
            {
                tile.soilRestDays = 0;
                tile.soilDurability = (byte)Mathf.Max(0, tile.soilDurability - durabilityLoss);
                tile.soilQuality = DurabilityToQuality(tile.soilDurability, maximumDurability);
                ProcessCropDay(ref tile);
                changed = true;
            }
            else if (tile.state == TileState.Hoed)
            {
                tile.waterSourcesToday = CropWaterSource.None;
                if (fertilityRecovery > 0)
                    tile.fertility = AddClamped(tile.fertility, fertilityRecovery);

                if (tile.soilDurability < maximumDurability)
                {
                    tile.soilRestDays = (byte)Mathf.Min(byte.MaxValue, tile.soilRestDays + 1);
                    if (tile.soilRestDays >= restDaysPerRecovery)
                    {
                        tile.soilRestDays = (byte)(tile.soilRestDays - restDaysPerRecovery);
                        tile.soilDurability = (byte)Mathf.Min(
                            maximumDurability,
                            tile.soilDurability + durabilityRecovery
                        );
                        tile.soilQuality = DurabilityToQuality(tile.soilDurability, maximumDurability);
                    }
                }
                else
                {
                    tile.soilRestDays = 0;
                }

                tile.dirty = true;
                changed = true;
            }

            if (!changed)
                continue;

            tiles[index] = tile;
            IndexToCoordinate(index, out int x, out int z);

            if (tile.HasCrop && cropViews[index] != null)
                cropViews[index].ApplyStage(tile.growthStage, tile.crop, tile.cropState);

            NotifyChanged(x, z);
        }
    }

    void ProcessCropDay(ref FieldTileData tile)
    {
        CropDataSO crop = tile.crop;
        bool watered = tile.waterSourcesToday != CropWaterSource.None;
        CropSeason currentSeason = CropDataSO.GetSeasonForDay(
            TimeManager.Instance != null ? TimeManager.Instance.day : 1,
            daysPerSeason
        );

        tile.qualityCare ??= new CropQualityCare();
        CropQualityCare care = tile.qualityCare;
        care.correctSeason &= crop.SupportsSeason(currentSeason);
        if (tile.cropState != CropLifecycleState.HarvestReady && tile.cropState != CropLifecycleState.Dead)
        {
            care.days++;
            if (watered) care.wateredDays++;
            if (care.boosterToday > 0)
            {
                care.boostedDays++;
                care.minimumBooster = Mathf.Min(care.minimumBooster, care.boosterToday);
                care.bestBooster = Mathf.Max(care.bestBooster, care.boosterToday);
            }
        }
        tile.careSamples++;
        tile.totalMoisture += tile.moisture;
        tile.totalFertility += tile.fertility;
        tile.totalSoilQuality += tile.soilQuality;

        if (!crop.SupportsSeason(currentSeason))
        {
            if (crop.outOfSeasonBehavior == OutOfSeasonCropBehavior.Die)
            {
                tile.cropState = CropLifecycleState.Dead;
                tile.cropHealth = 0;
            }
            else if (crop.outOfSeasonBehavior == OutOfSeasonCropBehavior.Wither)
            {
                tile.cropState = CropLifecycleState.Withered;
                tile.cropHealth = AddClamped(tile.cropHealth, -crop.healthLossWhenDry);
            }
            FinishCropDay(ref tile);
            return;
        }

        if (tile.cropState == CropLifecycleState.Dead ||
            tile.cropState == CropLifecycleState.HarvestReady)
        {
            FinishCropDay(ref tile);
            return;
        }

        if (tile.soilDurability <= 0)
        {
            tile.cropState = CropLifecycleState.Withered;
            tile.cropHealth = AddClamped(tile.cropHealth, -crop.healthLossWhenDry);
            FinishCropDay(ref tile);
            return;
        }

        if (!watered)
        {
            tile.consecutiveDryDays = (byte)Mathf.Min(byte.MaxValue, tile.consecutiveDryDays + 1);
            tile.recoveryWateredDays = 0;
            tile.cropHealth = AddClamped(tile.cropHealth, -crop.healthLossWhenDry);
            if (tile.consecutiveDryDays >= crop.dryDaysBeforeWither)
                tile.cropState = CropLifecycleState.Withered;
            FinishCropDay(ref tile);
            return;
        }

        tile.consecutiveDryDays = 0;
        tile.cropHealth = AddClamped(tile.cropHealth, 2);
        if (tile.cropState == CropLifecycleState.Withered)
        {
            tile.recoveryWateredDays = (byte)Mathf.Min(byte.MaxValue, tile.recoveryWateredDays + 1);
            if (tile.recoveryWateredDays < crop.wateredDaysToRecover)
            {
                FinishCropDay(ref tile);
                return;
            }
            tile.recoveryWateredDays = 0;
            tile.cropState = tile.regrowDaysRemaining > 0f
                ? CropLifecycleState.Regrowing
                : CropLifecycleState.Growing;
        }

        int maximumDurability = soilProfile != null ? soilProfile.MaximumDurability : 80;
        float soilFactor = Mathf.Lerp(
            0.45f,
            1f,
            Mathf.Clamp01(tile.soilDurability / (float)maximumDurability)
        );
        float fertilityFactor = crop.minimumFertility <= 0
            ? 1f
            : Mathf.Lerp(0.65f, 1f, Mathf.Clamp01((float)tile.fertility / crop.minimumFertility));
        float weatherFactor = WeatherSystem.Instance != null
            ? WeatherSystem.GetCropGrowthMultiplier(WeatherSystem.Instance.CurrentWeather)
            : 1f;
        float growthAmount = soilFactor * fertilityFactor * weatherFactor;

        if (tile.cropState == CropLifecycleState.Regrowing)
        {
            tile.regrowDaysRemaining = Mathf.Max(0f, tile.regrowDaysRemaining - growthAmount);
            if (tile.regrowDaysRemaining <= 0f)
            {
                tile.cropState = CropLifecycleState.HarvestReady;
                tile.growthStage = (byte)Mathf.Max(0, crop.StageCount - 1);
            }
        }
        else
        {
            tile.growthDays = Mathf.Min(crop.TotalGrowthDays, tile.growthDays + growthAmount);
            tile.growthStage = (byte)crop.GetStageForGrowth(tile.growthDays);
            if (tile.growthDays >= crop.TotalGrowthDays)
                tile.cropState = CropLifecycleState.HarvestReady;
        }

        tile.fertility = AddClamped(tile.fertility, -crop.dailyFertilityUse);
        FinishCropDay(ref tile);
    }

    static void FinishCropDay(ref FieldTileData tile)
    {
        if (tile.qualityCare != null)
        {
            tile.qualityCare.boosterToday = 0;
            if (tile.cropState == CropLifecycleState.HarvestReady && tile.qualityCare.readyDay < 0)
                tile.qualityCare.readyDay = (TimeManager.Instance != null ? TimeManager.Instance.day : 1) + 1;
        }
        tile.waterSourcesToday = CropWaterSource.None;
        tile.dirty = true;
    }

    CropGrade CalculateGrade(FieldTileData tile)
    {
        tile.qualityCare ??= new CropQualityCare();
        int today = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        return (CropGrade)(tile.qualityCare.RollStars(tile.crop, tile.soilDurability >= 49, today) - 1);
    }

    void ApplyEffectAtIndex(int index, FieldEffect effect)
    {
        FieldTileData tile = tiles[index];
        switch (effect.type)
        {
            case FieldEffectType.Moisture:
                tile.moisture = AddClamped(tile.moisture, effect.amount);
                if (effect.amount > 0)
                    tile.waterSourcesToday |= CropWaterSource.External;
                break;
            case FieldEffectType.Fertility:
                tile.fertility = AddClamped(tile.fertility, effect.amount);
                break;
            case FieldEffectType.SoilQuality:
                tile.soilQuality = AddClamped(tile.soilQuality, effect.amount);
                break;
            case FieldEffectType.CropHealth:
                tile.cropHealth = AddClamped(tile.cropHealth, effect.amount);
                break;
            case FieldEffectType.GrowthBooster:
                // Enum legacy tetap terbaca; tidak lagi memberikan bonus growth.
                break;
        }

        tile.dirty = true;
        tiles[index] = tile;
    }

    void MarkWateredAtIndex(int index, CropWaterSource source, int moistureAmount)
    {
        FieldTileData tile = tiles[index];
        tile.moisture = AddClamped(tile.moisture, Mathf.Max(0, moistureAmount));
        tile.waterSourcesToday |= source;
        tile.dirty = true;
        tiles[index] = tile;
    }

    static byte AddClamped(byte value, int amount)
    {
        return (byte)Mathf.Clamp(value + amount, 0, 100);
    }

    static int GetMinimumDurabilityForLevel(int level)
    {
        return level switch
        {
            >= 5 => 65,
            4 => 49,
            3 => 33,
            2 => 17,
            _ => 0
        };
    }

    static byte DurabilityToQuality(int durability, int maximumDurability)
    {
        float normalized = Mathf.Clamp01(durability / (float)Mathf.Max(1, maximumDurability));
        return (byte)Mathf.RoundToInt(normalized * 100f);
    }

    void SetActive(int index, bool active)
    {
        if (activeFlags[index] == active)
            return;

        activeFlags[index] = active;
        if (active)
        {
            activeTileIndices.Add(index);
            return;
        }

        int position = activeTileIndices.IndexOf(index);
        if (position < 0)
            return;

        int last = activeTileIndices.Count - 1;
        activeTileIndices[position] = activeTileIndices[last];
        activeTileIndices.RemoveAt(last);
    }

    void ShowHoeView(int index, int x, int z)
    {
        if (hoeMarkPrefab == null || hoeViews[index] != null)
            return;

        EnsureVisualRoot();
        GameObject view = hoePool.Count > 0 ? hoePool.Pop() : Instantiate(hoeMarkPrefab);
        view.transform.SetParent(visualRoot, true);
        view.transform.SetPositionAndRotation(GridToWorld(x, z), transform.rotation);
        view.SetActive(true);
        hoeViews[index] = view;
        UpdateSoilMaterial(index);
    }

    void HideHoeView(int index)
    {
        GameObject view = hoeViews[index];
        if (view == null)
            return;

        view.SetActive(false);
        hoePool.Push(view);
        hoeViews[index] = null;
    }

    void ShowCropView(int index, int x, int z)
    {
        if (cropPrefab == null || cropViews[index] != null)
            return;

        EnsureVisualRoot();
        FieldCropView view;
        if (cropPool.Count > 0)
        {
            view = cropPool.Pop();
        }
        else
        {
            GameObject cropObject = Instantiate(cropPrefab);
            view = cropObject.GetComponent<FieldCropView>();
            if (view == null)
                view = cropObject.AddComponent<FieldCropView>();
        }

        view.transform.SetParent(visualRoot, true);
        view.transform.SetPositionAndRotation(GridToWorld(x, z) + transform.up * 0.01f, transform.rotation);
        view.gameObject.SetActive(true);
        view.Initialize(this, x, z, tiles[index].crop, tiles[index].growthStage, tiles[index].cropState);
        cropViews[index] = view;
    }

    void HideCropView(int index)
    {
        FieldCropView view = cropViews[index];
        if (view == null)
            return;

        view.gameObject.SetActive(false);
        cropPool.Push(view);
        cropViews[index] = null;
    }

    void RefreshViewAt(int index)
    {
        IndexToCoordinate(index, out int x, out int z);
        FieldTileData tile = tiles[index];

        if (tile.state == TileState.Hoed || tile.state == TileState.Planted)
            ShowHoeView(index, x, z);
        else
            HideHoeView(index);

        if (tile.HasCrop)
            ShowCropView(index, x, z);
        else
            HideCropView(index);
    }

    void UpdateSoilMaterial(int index)
    {
        GameObject view = hoeViews[index];
        if (view == null)
            return;

        Renderer soilRenderer = view.GetComponentInChildren<Renderer>();
        if (soilRenderer == null)
            return;

        FieldTileData tile = tiles[index];
        bool watered = tile.moisture >= wateredVisualThreshold;
        bool fertilized = tile.fertilizedForCurrentCycle;

        // Semua tile memakai satu shared material. State visual ditulis melalui
        // MaterialPropertyBlock sehingga tidak membuat material instance per tile.
        if (normalSoilMaterial != null && soilRenderer.sharedMaterial != normalSoilMaterial)
            soilRenderer.sharedMaterial = normalSoilMaterial;

        soilVisualProperties ??= new MaterialPropertyBlock();
        soilRenderer.GetPropertyBlock(soilVisualProperties);
        float wetness = watered
            ? Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(wateredVisualThreshold, 100f, tile.moisture))
            : 0f;
        soilVisualProperties.SetFloat(WetnessProperty, wetness);
        soilVisualProperties.SetFloat(FertilizedProperty, fertilized ? 1f : 0f);

        // Fallback untuk material URP/Lit lama: wetness masih terbaca sebagai smoothness,
        // sedangkan butiran pupuk memerlukan shader Soil State.
        soilVisualProperties.SetFloat(SmoothnessProperty, Mathf.Lerp(0.03f, 0.58f, wetness));
        soilRenderer.SetPropertyBlock(soilVisualProperties);
    }

    /// <summary>Membuat snapshot serializable seluruh tile field.</summary>
    public FieldSaveData CaptureSaveData()
    {
        FieldSaveData data = new FieldSaveData
        {
            fieldId = fieldId,
            columns = columns,
            rows = rows
        };

        FieldTileData defaults = CreateDefaultTile();
        for (int index = 0; index < tiles.Length; index++)
        {
            FieldTileData tile = tiles[index];
            bool changed = tile.dirty || tile.state != TileState.Empty ||
                           tile.soilQuality != defaults.soilQuality ||
                           tile.soilDurability != defaults.soilDurability ||
                           tile.soilRestDays != defaults.soilRestDays ||
                           tile.fertility != defaults.fertility ||
                           tile.moisture != defaults.moisture;
            if (!changed)
                continue;

            IndexToCoordinate(index, out int x, out int z);
            data.tiles.Add(new FieldTileSaveData
            {
                x = x,
                z = z,
                state = tile.state,
                soilQuality = tile.soilQuality,
                fertility = tile.fertility,
                moisture = tile.moisture,
                cropId = tile.crop != null ? tile.crop.cropId : null,
                growthDays = tile.growthDays,
                growthStage = tile.growthStage,
                cropHealth = tile.cropHealth,
                careSamples = tile.careSamples,
                totalMoisture = tile.totalMoisture,
                totalFertility = tile.totalFertility,
                totalSoilQuality = tile.totalSoilQuality,
                buildingId = tile.buildingId,
                hasAdvancedGrowthData = true,
                cropState = tile.cropState,
                waterSourcesToday = tile.waterSourcesToday,
                consecutiveDryDays = tile.consecutiveDryDays,
                recoveryWateredDays = tile.recoveryWateredDays,
                growthBoosterPercent = 0,
                qualityCare = tile.qualityCare?.Copy(),
                regrowDaysRemaining = tile.regrowDaysRemaining,
                hasSoilDurability = true,
                soilDurability = tile.soilDurability,
                soilRestDays = tile.soilRestDays,
                fertilizedForCurrentCycle = tile.fertilizedForCurrentCycle
            });
        }

        return data;
    }

    /// <summary>Memulihkan grid dan membangun ulang visual dari save data.</summary>
    public void RestoreSaveData(FieldSaveData data)
    {
        if (data == null || !string.Equals(data.fieldId, fieldId, StringComparison.Ordinal))
            return;

        ResetRuntimeData();
        if (data.tiles == null)
            return;

        for (int i = 0; i < data.tiles.Count; i++)
        {
            FieldTileSaveData saved = data.tiles[i];
            if (!TryGetIndex(saved.x, saved.z, out int index))
                continue;

            FieldTileData tile = CreateDefaultTile();
            tile.state = saved.state;
            tile.soilQuality = saved.soilQuality;
            tile.fertility = saved.fertility;
            tile.moisture = saved.moisture;
            tile.crop = ResolveCrop(saved.cropId);
            tile.growthDays = saved.growthDays;
            tile.qualityCare = saved.qualityCare?.Copy() ?? new CropQualityCare();
            tile.growthStage = saved.growthStage;
            tile.cropHealth = saved.cropHealth;
            tile.careSamples = saved.careSamples;
            tile.totalMoisture = saved.totalMoisture;
            tile.totalFertility = saved.totalFertility;
            tile.totalSoilQuality = saved.totalSoilQuality;
            tile.buildingId = saved.buildingId;

            int maximumDurability = soilProfile != null ? soilProfile.MaximumDurability : 80;
            if (saved.hasSoilDurability)
            {
                tile.soilDurability = (byte)Mathf.Clamp(saved.soilDurability, 0, maximumDurability);
                tile.soilRestDays = saved.soilRestDays;
                tile.fertilizedForCurrentCycle = saved.fertilizedForCurrentCycle;
            }
            else
            {
                // Save lama belum menyimpan durability; mulai penuh agar pemain tidak dirugikan.
                tile.soilDurability = (byte)maximumDurability;
                tile.soilRestDays = 0;
            }
            tile.soilQuality = DurabilityToQuality(tile.soilDurability, maximumDurability);

            if (saved.hasAdvancedGrowthData)
            {
                tile.cropState = saved.cropState;
                tile.waterSourcesToday = saved.waterSourcesToday;
                tile.consecutiveDryDays = saved.consecutiveDryDays;
                tile.recoveryWateredDays = saved.recoveryWateredDays;
                tile.growthBoosterPercent = 0; // Growth booster lama tidak dibawa ke kualitas.
                tile.regrowDaysRemaining = saved.regrowDaysRemaining;
                if (tile.crop != null && tile.cropState == CropLifecycleState.HarvestReady)
                    tile.growthStage = (byte)Mathf.Max(0, tile.crop.StageCount - 1);
            }
            else if (tile.crop != null)
            {
                // Save lama belum memiliki watered/lifecycle; inferensi menjaga crop matang tetap panenable.
                tile.cropState = tile.growthDays >= tile.crop.TotalGrowthDays
                    ? CropLifecycleState.HarvestReady
                    : CropLifecycleState.Growing;
                tile.waterSourcesToday = tile.moisture >= tile.crop.idealMoistureMin
                    ? CropWaterSource.External
                    : CropWaterSource.None;
            }

            if (tile.state == TileState.Planted && tile.crop == null)
                tile.state = TileState.Hoed;

            tiles[index] = tile;
            SetActive(index, tile.state != TileState.Empty);
            RefreshViewAt(index);
            NotifyChanged(saved.x, saved.z);
        }
    }

    void ResetRuntimeData()
    {
        if (cropViews != null)
        {
            for (int i = 0; i < cropViews.Length; i++)
            {
                HideCropView(i);
                HideHoeView(i);
            }
        }

        InitializeData();
    }

    CropDataSO ResolveCrop(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return null;

        if (defaultCrop != null && defaultCrop.cropId == cropId)
            return defaultCrop;

        for (int i = 0; i < cropCatalog.Count; i++)
        {
            CropDataSO crop = cropCatalog[i];
            if (crop != null && crop.cropId == cropId)
                return crop;
        }

        return null;
    }

    public static List<FieldSaveData> CaptureAll()
    {
        List<FieldSaveData> result = new List<FieldSaveData>(ActiveAreasInternal.Count);
        for (int i = 0; i < ActiveAreasInternal.Count; i++)
        {
            if (ActiveAreasInternal[i] != null)
                result.Add(ActiveAreasInternal[i].CaptureSaveData());
        }
        return result;
    }

    /// <summary>Memulihkan seluruh FieldArea terdaftar berdasarkan field ID.</summary>
    public static void RestoreAll(List<FieldSaveData> data)
    {
        if (data == null)
            return;

        for (int areaIndex = 0; areaIndex < ActiveAreasInternal.Count; areaIndex++)
        {
            FieldArea area = ActiveAreasInternal[areaIndex];
            if (area == null)
                continue;

            for (int saveIndex = 0; saveIndex < data.Count; saveIndex++)
            {
                if (data[saveIndex] != null && data[saveIndex].fieldId == area.fieldId)
                {
                    area.RestoreSaveData(data[saveIndex]);
                    break;
                }
            }
        }
    }

    bool TryGetIndex(int x, int z, out int index)
    {
        if (!InBounds(x, z))
        {
            index = -1;
            return false;
        }

        index = ToIndex(x, z);
        return true;
    }

    int ToIndex(int x, int z) => z * columns + x;

    void IndexToCoordinate(int index, out int x, out int z)
    {
        x = index % columns;
        z = index / columns;
    }

    bool InBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < columns && z < rows;
    }

    void NotifyChanged(int x, int z)
    {
        if (TryGetIndex(x, z, out int index))
            UpdateSoilMaterial(index);

        TileChanged?.Invoke(this, new Vector2Int(x, z));
    }

    void SyncColliderToGrid()
    {
        if (areaCollider == null)
            return;

        areaCollider.center = Vector3.zero;
        areaCollider.size = new Vector3(columns * cellSize, 0.2f, rows * cellSize);
    }

    void SyncSurfaceToGrid()
    {
        if (fieldSurface == null)
            return;

        float baseWidth = Mathf.Max(0.01f, fieldSurfaceBaseSize.x);
        float baseDepth = Mathf.Max(0.01f, fieldSurfaceBaseSize.y);

        fieldSurface.localPosition = new Vector3(0f, fieldSurfaceYOffset, 0f);
        fieldSurface.localRotation = Quaternion.identity;
        fieldSurface.localScale = new Vector3(
            columns * cellSize / baseWidth,
            1f,
            rows * cellSize / baseDepth
        );
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize = Mathf.Max(0.25f, cellSize);
        chunkSize = Mathf.Max(4, chunkSize);
        visualDistance = Mathf.Max(1f, visualDistance);
        lodCheckInterval = Mathf.Max(0.1f, lodCheckInterval);
        wateredVisualThreshold = Mathf.Clamp(wateredVisualThreshold, 0, 100);
        fertilizedVisualThreshold = Mathf.Clamp(fertilizedVisualThreshold, 0, 100);

        if (areaCollider == null)
            areaCollider = GetComponent<BoxCollider>();

        if (fieldSurface == null)
            fieldSurface = transform.Find("FieldSurface");

        SyncColliderToGrid();
        SyncSurfaceToGrid();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGridGizmo || columns < 1 || rows < 1 || cellSize <= 0f)
            return;

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.82f, 0.15f, 0.75f);

        float width = columns * cellSize;
        float depth = rows * cellSize;
        Vector3 origin = new Vector3(-width * 0.5f, 0.03f, -depth * 0.5f);

        for (int x = 0; x <= columns; x++)
        {
            Vector3 start = origin + Vector3.right * (x * cellSize);
            Gizmos.DrawLine(start, start + Vector3.forward * depth);
        }

        for (int z = 0; z <= rows; z++)
        {
            Vector3 start = origin + Vector3.forward * (z * cellSize);
            Gizmos.DrawLine(start, start + Vector3.right * width);
        }

        Gizmos.matrix = previous;
    }
#endif
}
