using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
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

    [Header("Visual Templates")]
    [SerializeField] GameObject cropPrefab;
    [SerializeField] GameObject hoeMarkPrefab;

    [Header("Soil Material Cues")]
    [SerializeField] Material normalSoilMaterial;
    [SerializeField] Material wateredSoilMaterial;
    [SerializeField] Material fertilizedSoilMaterial;
    [SerializeField] Material wateredAndFertilizedSoilMaterial;
    [SerializeField, Range(0, 100)] int wateredVisualThreshold = 50;
    [SerializeField, Range(0, 100)] int fertilizedVisualThreshold = 75;

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

    public string FieldId => fieldId;
    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public int ChunkSize => chunkSize;
    public Vector2 WorldSize => new Vector2(columns * cellSize, rows * cellSize);

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
        TimeManager.OnDay += HandleDayChanged;
    }

    void OnDisable()
    {
        TimeManager.OnHour -= HandleHourChanged;
        TimeManager.OnDay -= HandleDayChanged;
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
        return new FieldTileData
        {
            state = TileState.Empty,
            soilQuality = soilProfile != null ? soilProfile.InitialSoilQuality : (byte)75,
            fertility = soilProfile != null ? soilProfile.InitialFertility : (byte)60,
            moisture = soilProfile != null ? soilProfile.InitialMoisture : (byte)35,
            cropHealth = 100
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

    /// <summary>Mencangkul satu tile valid dan menerapkan biaya kualitas tanah.</summary>
    public bool TryHoe(int x, int z)
    {
        if (!CanHoe(x, z) || !TryGetIndex(x, z, out int index))
            return false;

        FieldTileData tile = tiles[index];
        tile.state = TileState.Hoed;
        tile.dirty = true;
        tiles[index] = tile;
        SetActive(index, true);
        ShowHoeView(index, x, z);
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Memeriksa batas grid, occupancy, crop, dan aturan tile sebelum hoe.</summary>
    public bool CanHoe(int x, int z)
    {
        return TryGetIndex(x, z, out int index) && tiles[index].state == TileState.Empty;
    }

    /// <summary>Menanam crop pada tile hoed yang kosong.</summary>
    public bool TryPlant(int x, int z, CropDataSO crop = null)
    {
        crop = crop != null ? crop : defaultCrop;
        if (!allowCrops || crop == null || !TryGetIndex(x, z, out int index) ||
            tiles[index].state != TileState.Hoed)
        {
            return false;
        }

        FieldTileData tile = tiles[index];
        tile.state = TileState.Planted;
        tile.crop = crop;
        tile.growthDays = 0f;
        tile.growthStage = 0;
        tile.cropHealth = 100;
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
        if (!IsEditableSoilTile(x, z, out int index))
            return false;

        int value = amount >= 0 ? amount : soilProfile != null ? soilProfile.waterAmount : 35;
        ApplyEffectAtIndex(index, new FieldEffect(FieldEffectType.Moisture, value));
        NotifyChanged(x, z);
        return true;
    }

    /// <summary>Menambah fertility satu tile; nilai negatif memakai default SoilProfile.</summary>
    public bool TryFertilize(int x, int z, int amount = -1)
    {
        if (!IsEditableSoilTile(x, z, out int index))
            return false;

        int value = amount >= 0 ? amount : soilProfile != null ? soilProfile.fertilizerAmount : 25;
        ApplyEffectAtIndex(index, new FieldEffect(FieldEffectType.Fertility, value));
        NotifyChanged(x, z);
        return true;
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
        if (!tile.HasCrop || tile.growthDays < tile.crop.TotalGrowthDays)
            return false;

        grade = CalculateGrade(tile);
        int amount = Mathf.Max(1, tile.crop.baseYield);
        ItemSO item = tile.crop.produceItem;
        if (item == null || !inventory.Add(item, amount))
            return false;

        CropHarvestResult result = new CropHarvestResult(
            fieldId,
            new Vector2Int(x, z),
            item,
            amount,
            grade
        );

        tile.soilQuality = AddClamped(tile.soilQuality, -tile.crop.soilDepletionOnHarvest);
        tile.state = TileState.Hoed;
        tile.crop = null;
        tile.growthDays = 0f;
        tile.growthStage = 0;
        tile.cropHealth = 100;
        tile.careSamples = 0;
        tile.totalMoisture = 0;
        tile.totalFertility = 0;
        tile.totalSoilQuality = 0;
        tile.dirty = true;
        tiles[index] = tile;

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
                if (!TryGetIndex(x, z, out int index) || tiles[index].state != TileState.Empty)
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

    void HandleDayChanged()
    {
        // Hanya activeTileIndices yang diproses. Tile kosong tidak ikut loop harian agar field
        // besar tetap murah di perangkat mobile dan tidak membutuhkan Update per tanaman.
        int recovery = soilProfile != null ? soilProfile.dailyFertilityRecovery : 0;

        for (int i = 0; i < activeTileIndices.Count; i++)
        {
            int index = activeTileIndices[i];
            FieldTileData tile = tiles[index];

            if (tile.state == TileState.Hoed && recovery > 0)
            {
                tile.fertility = AddClamped(tile.fertility, recovery);
                tile.dirty = true;
                tiles[index] = tile;
            }

            if (!tile.HasCrop)
                continue;

            ProcessCropDay(ref tile);
            tiles[index] = tile;
            IndexToCoordinate(index, out int x, out int z);

            if (cropViews[index] != null)
                cropViews[index].ApplyStage(tile.growthStage, tile.crop);

            NotifyChanged(x, z);
        }
    }

    void ProcessCropDay(ref FieldTileData tile)
    {
        CropDataSO crop = tile.crop;
        tile.careSamples++;
        tile.totalMoisture += tile.moisture;
        tile.totalFertility += tile.fertility;
        tile.totalSoilQuality += tile.soilQuality;

        float moistureFactor;
        if (tile.moisture < crop.idealMoistureMin)
            moistureFactor = crop.idealMoistureMin <= 0 ? 1f : (float)tile.moisture / crop.idealMoistureMin;
        else if (tile.moisture > crop.idealMoistureMax)
            moistureFactor = Mathf.Clamp01(1f - (tile.moisture - crop.idealMoistureMax) / 100f);
        else
            moistureFactor = 1f;

        float fertilityFactor = crop.minimumFertility <= 0
            ? 1f
            : Mathf.Clamp01((float)tile.fertility / crop.minimumFertility);
        float soilFactor = tile.soilQuality / 100f;
        float growthFactor = Mathf.Clamp01(
            moistureFactor * 0.45f + fertilityFactor * 0.35f + soilFactor * 0.2f
        );

        if (growthFactor < 0.35f)
            tile.cropHealth = AddClamped(tile.cropHealth, -10);
        else if (growthFactor > 0.8f)
            tile.cropHealth = AddClamped(tile.cropHealth, 2);

        tile.growthDays = Mathf.Min(crop.TotalGrowthDays, tile.growthDays + growthFactor);
        tile.growthStage = (byte)crop.GetStageForGrowth(tile.growthDays);
        tile.fertility = AddClamped(tile.fertility, -crop.dailyFertilityUse);
        tile.dirty = true;
    }

    CropGrade CalculateGrade(FieldTileData tile)
    {
        float samples = Mathf.Max(1, tile.careSamples);
        float soil = tile.careSamples > 0 ? tile.totalSoilQuality / samples : tile.soilQuality;
        float moisture = tile.careSamples > 0 ? tile.totalMoisture / samples : tile.moisture;
        float fertility = tile.careSamples > 0 ? tile.totalFertility / samples : tile.fertility;
        float weight = Mathf.Max(0.01f,
            tile.crop.soilWeight + tile.crop.moistureWeight +
            tile.crop.fertilityWeight + tile.crop.healthWeight);

        float score = (
            soil * tile.crop.soilWeight +
            moisture * tile.crop.moistureWeight +
            fertility * tile.crop.fertilityWeight +
            tile.cropHealth * tile.crop.healthWeight
        ) / weight;

        if (score >= 90f) return CropGrade.S;
        if (score >= 75f) return CropGrade.A;
        if (score >= 55f) return CropGrade.B;
        if (score >= 35f) return CropGrade.C;
        return CropGrade.D;
    }

    void ApplyEffectAtIndex(int index, FieldEffect effect)
    {
        FieldTileData tile = tiles[index];
        switch (effect.type)
        {
            case FieldEffectType.Moisture:
                tile.moisture = AddClamped(tile.moisture, effect.amount);
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
        }

        tile.dirty = true;
        tiles[index] = tile;
    }

    static byte AddClamped(byte value, int amount)
    {
        return (byte)Mathf.Clamp(value + amount, 0, 100);
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
        view.Initialize(this, x, z, tiles[index].crop, tiles[index].growthStage);
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
        bool fertilized = tile.fertility >= fertilizedVisualThreshold;

        Material selectedMaterial;
        if (watered && fertilized)
            selectedMaterial = wateredAndFertilizedSoilMaterial;
        else if (watered)
            selectedMaterial = wateredSoilMaterial;
        else if (fertilized)
            selectedMaterial = fertilizedSoilMaterial;
        else
            selectedMaterial = normalSoilMaterial;

        if (selectedMaterial != null && soilRenderer.sharedMaterial != selectedMaterial)
            soilRenderer.sharedMaterial = selectedMaterial;
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
                buildingId = tile.buildingId
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
            tile.growthStage = saved.growthStage;
            tile.cropHealth = saved.cropHealth;
            tile.careSamples = saved.careSamples;
            tile.totalMoisture = saved.totalMoisture;
            tile.totalFertility = saved.totalFertility;
            tile.totalSoilQuality = saved.totalSoilQuality;
            tile.buildingId = saved.buildingId;

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
