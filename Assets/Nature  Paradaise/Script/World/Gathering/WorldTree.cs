using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tahap persisten pohon dari berdiri hingga menunggu respawn.</summary>
public enum TreeGrowthState { Standing, Stump, Depleted }

/// <summary>State pohon/tunggul yang disimpan berdasarkan ID unik scene.</summary>
[Serializable]
public sealed class TreeSaveData
{
    public string id;
    public TreeGrowthState state;
    public int durability;
    public int respawnDay;
    public TreeProgress progress;
}

/// <summary>
/// Resource pohon modular: menerima damage Axe, berubah menjadi tunggul,
/// menjatuhkan kayu, lalu dapat habis dan tumbuh kembali.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WorldTree : MonoBehaviour
{
    static readonly List<WorldTree> Registry = new();
    public static IReadOnlyList<WorldTree> Active => Registry;

    [Header("Identity")]
    [SerializeField] string treeId = "tree-01";
    [Header("Growth / Fruit")]
    public TreeDefinition definition;
    [SerializeField] TreeProgress progress;
    Vector3 matureVisualScale;
    GameObject stageModel;
    int shownStage = -2;
    Inventory fruitInventory;
    public TreeProgress Progress => progress;
    public void RefreshGrowthVisual() => ApplyStateVisual();

    [Header("Axe Requirement")]
    [SerializeField, Min(1)] int minimumAxeLevel = 1;
    [SerializeField, Min(1)] int standingDurability = 5;
    [SerializeField, Min(1)] int stumpDurability = 3;
    [SerializeField, Min(1)] int damagePerHit = 1;

    [Header("Drops")]
    [SerializeField] ItemSO woodItem;
    [SerializeField, Min(1)] int standingWoodMinimum = 3;
    [SerializeField, Min(1)] int standingWoodMaximum = 5;
    [SerializeField, Min(1)] int stumpWoodMinimum = 1;
    [SerializeField, Min(1)] int stumpWoodMaximum = 2;

    [Header("Drop Placement")]
    [Tooltip("Jarak drop kayu batang dari pusat pohon agar pickup tidak terjebak collider tunggul.")]
    [SerializeField, Min(0.5f)] float standingDropRadius = 1.15f;
    [Tooltip("Jarak drop bonus kayu setelah tunggul dihancurkan.")]
    [SerializeField, Min(0.5f)] float stumpDropRadius = 0.85f;
    [SerializeField, Min(0f)] float dropHeight = 0.35f;

    [Header("Respawn")]
    [SerializeField] bool canRespawn = true;
    [SerializeField, Min(1)] int minimumRespawnDays = 4;
    [SerializeField, Min(1)] int maximumRespawnDays = 7;

    [Header("Editable Visual Slots")]
    [SerializeField] Transform standingVisual;
    [SerializeField] Transform stumpVisual;
    [SerializeField] Renderer[] highlightRenderers;
    [SerializeField] AudioClip axeImpactSound;
    [SerializeField] AudioClip fallingSound;
    [SerializeField] ParticleSystem woodChipParticles;
    [SerializeField, Min(0.1f)] float fallDuration = 0.75f;
    [Tooltip("Sudut roboh pohon. Nilai sekitar 80-90 derajat membuat batang jatuh mendekati tanah.")]
    [SerializeField, Range(45f, 100f)] float fallAngle = 82f;
    [SerializeField, Min(0f)] float promptHeight = 2.4f;

    Collider interactionCollider;
    BoxCollider boxCollider;
    Vector3 standingColliderSize;
    Vector3 standingColliderCenter;
    [SerializeField] Vector3 stumpColliderSize = new(1.1f, 0.6f, 1.1f);
    [SerializeField] Vector3 stumpColliderCenter = new(0f, 0.3f, 0f);
    LineRenderer highlight;
    Material highlightMaterial;
    TreeGrowthState state = TreeGrowthState.Standing;
    int durability;
    int respawnDay;
    bool transitioning;
    Quaternion standingRestRotation;
    Renderer[] standingRenderers;
    bool terrainManaged;
    bool clearAfterFelling;

    public bool IsAvailable => state != TreeGrowthState.Depleted && !transitioning;
    public bool IsStump => state == TreeGrowthState.Stump;
    public int Durability => durability;
    public int MinimumAxeLevel => minimumAxeLevel;
    public float PromptHeight => promptHeight;
    public TreeGrowthState GrowthState => state;
    public int RespawnDay => respawnDay;
    public int StandingDurability => Mathf.Max(1, standingDurability);
    public int StumpDurability => Mathf.Max(1, stumpDurability);
    public bool IsTransitioning => transitioning;

    /// <summary>Manager memiliki ID/save/daily reset; object ini hanya menangani interaksi dekat.</summary>
    public void SetManagedIdentity(string id, bool removeStumpAfterFelling = true)
    {
        treeId = id;
        terrainManaged = true;
        clearAfterFelling = removeStumpAfterFelling;
        TimeManager.OnDay -= HandleDayChanged;
        TimeManager.OnBeforeDayChange -= AdvanceGrowth;
    }

    void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        boxCollider = interactionCollider as BoxCollider;
        if (boxCollider != null)
        {
            standingColliderSize = boxCollider.size;
            standingColliderCenter = boxCollider.center;
        }
        ResolveStandingVisual();
        CreateFallPivotAtVisualBase();
        standingRestRotation = standingVisual.localRotation;
        standingRenderers = CollectStandingRenderers();
        matureVisualScale = standingVisual.localScale;
        if (definition == null) definition = Resources.Load<TreeDefinition>("Trees/Wild Small");
        progress ??= TreeProgress.Create(definition, CurrentDay, true);
        if (!HasValidRenderer(highlightRenderers))
            highlightRenderers = standingRenderers;
        durability = Mathf.Max(1, standingDurability);
        ApplyStateVisual();
    }

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
        if (!terrainManaged) TimeManager.OnDay += HandleDayChanged;
        if (!terrainManaged) TimeManager.OnBeforeDayChange += AdvanceGrowth;
    }

    void OnDisable()
    {
        Registry.Remove(this);
        TimeManager.OnDay -= HandleDayChanged;
        TimeManager.OnBeforeDayChange -= AdvanceGrowth;
        if (highlight != null) highlight.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (highlightMaterial != null) Destroy(highlightMaterial);
    }

    /// <summary>Mengaktifkan feedback target tanpa mengubah state gameplay pohon.</summary>
    public void SetHighlighted(bool value)
    {
        if (!IsAvailable && value) return;
        EnsureHighlight();
        if (value) RefreshHighlightBounds();
        if (highlight != null) highlight.gameObject.SetActive(value);
    }

    /// <summary>Menerapkan satu pukulan kapak jika level tool memenuhi syarat.</summary>
    public bool Chop(int axeLevel)
    {
        if (!IsAvailable || axeLevel < minimumAxeLevel) return false;
        if (state == TreeGrowthState.Standing && definition != null && !progress.Mature(definition)) return false;
        durability = Mathf.Max(0, durability - Mathf.Max(1, damagePerHit));
        if (axeImpactSound != null) AudioSource.PlayClipAtPoint(axeImpactSound, transform.position);
        if (woodChipParticles != null) woodChipParticles.Play();
        if (durability > 0 && state == TreeGrowthState.Standing && standingVisual != null)
            StartCoroutine(ShakeRoutine());
        if (durability <= 0)
        {
            if (state == TreeGrowthState.Standing) StartCoroutine(FallToStumpRoutine());
            else DepleteStump();
        }
        return true;
    }

    IEnumerator ShakeRoutine()
    {
        Quaternion start = standingVisual.localRotation;
        standingVisual.localRotation = start * Quaternion.Euler(0f, 0f, 3f);
        yield return new WaitForSeconds(0.06f);
        if (standingVisual != null) standingVisual.localRotation = start;
    }

    IEnumerator FallToStumpRoutine()
    {
        transitioning = true;
        SetHighlighted(false);
        if (fallingSound != null) AudioSource.PlayClipAtPoint(fallingSound, transform.position);
        Quaternion start = standingVisual != null ? standingVisual.localRotation : Quaternion.identity;
        Quaternion end = start * Quaternion.Euler(0f, 0f, fallAngle);
        float elapsed = 0f;
        while (standingVisual != null && elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            standingVisual.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(elapsed / fallDuration));
            yield return null;
        }

        // Fallback untuk tree lama yang mesh-nya berada langsung pada root. Root gameplay
        // harus kembali tegak agar collider dan visual tunggul tidak ikut rebah.
        if (standingVisual == transform)
            standingVisual.localRotation = standingRestRotation;

        CompleteFall();
    }

    /// <summary>Menyelesaikan animasi sebelum pooling/save supaya drop hanya dibuat sekali.</summary>
    public void FinishPendingFall()
    {
        if (!transitioning) return;
        StopAllCoroutines();
        CompleteFall();
    }

    void CompleteFall()
    {
        if (standingVisual == transform) standingVisual.localRotation = standingRestRotation;
        state = TreeGrowthState.Stump;
        durability = Mathf.Max(1, stumpDurability);
        transitioning = false;
        if (terrainManaged && clearAfterFelling)
        {
            state = TreeGrowthState.Depleted;
            durability = 0;
            ScheduleRespawn();
        }
        ApplyStateVisual();
        int dropped = SpawnWood(standingWoodMinimum, standingWoodMaximum, standingDropRadius);
        if (dropped > 0)
            SaveLoadFeedback.Instance?.ShowMessage(state == TreeGrowthState.Depleted
                ? $"Pohon ditebang: Wood x{dropped}."
                : $"Pohon tumbang: Wood x{dropped}. Tunggul memberi kayu bonus.");
    }

    void DepleteStump()
    {
        state = TreeGrowthState.Depleted;
        ScheduleRespawn();
        SetHighlighted(false);
        ApplyStateVisual();
        int dropped = SpawnWood(stumpWoodMinimum, stumpWoodMaximum, stumpDropRadius);
        if (dropped > 0)
            SaveLoadFeedback.Instance?.ShowMessage($"Tunggul hancur: Wood bonus x{dropped}");
    }

    int SpawnWood(int minimum, int maximum, float radius)
    {
        if (woodItem == null) return 0;
        int amount = UnityEngine.Random.Range(Mathf.Max(1, minimum), Mathf.Max(minimum, maximum) + 1);
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude < 0.01f) randomDirection = Vector2.right;
        randomDirection.Normalize();
        Vector3 offset = new(randomDirection.x * radius, dropHeight, randomDirection.y * radius);
        WorldGatherable.SpawnLoosePickup(woodItem, amount, transform.position + offset);
        return amount;
    }

    void ScheduleRespawn()
    {
        int today = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        respawnDay = canRespawn
            ? today + UnityEngine.Random.Range(minimumRespawnDays, Mathf.Max(minimumRespawnDays, maximumRespawnDays) + 1)
            : int.MaxValue;
    }

    void HandleDayChanged()
    {
        if (state != TreeGrowthState.Depleted || !canRespawn || TimeManager.Instance == null || TimeManager.Instance.day < respawnDay) return;
        state = TreeGrowthState.Standing;
        durability = Mathf.Max(1, standingDurability);
        respawnDay = 0;
        progress = TreeProgress.Create(definition, CurrentDay, false);
        if (standingVisual != null) standingVisual.localRotation = standingRestRotation;
        ApplyStateVisual();
    }

    void ApplyStateVisual()
    {
        ApplyGrowthVisual();
        bool standing = state == TreeGrowthState.Standing;
        bool stump = state == TreeGrowthState.Stump;
        if (standingVisual != null && standingVisual != transform) standingVisual.gameObject.SetActive(standing);
        else if (standingRenderers != null)
            for (int i = 0; i < standingRenderers.Length; i++) if (standingRenderers[i] != null) standingRenderers[i].enabled = standing;
        if (stumpVisual != null) stumpVisual.gameObject.SetActive(stump);
        if (interactionCollider != null) interactionCollider.enabled = state != TreeGrowthState.Depleted;
        if (boxCollider != null && state != TreeGrowthState.Depleted)
        {
            int stageIndex = definition != null && progress != null ? definition.StageIndex(progress.growth) : -1;
            Vector3 scale = stageIndex >= 0 ? definition.stages[stageIndex].scale : Vector3.one;
            boxCollider.size = stump ? stumpColliderSize : Vector3.Scale(standingColliderSize, scale);
            boxCollider.center = stump ? stumpColliderCenter : Vector3.Scale(standingColliderCenter, scale);
        }
    }

    /// <summary>
    /// Menentukan visual berdiri tanpa bergantung pada nama model. Jika artist menaruh beberapa
    /// child mesh langsung di root, semuanya digabung ke container runtime agar dapat dianimasikan
    /// dan disembunyikan sebagai satu unit tanpa mematikan WorldTree.
    /// </summary>
    void ResolveStandingVisual()
    {
        if (standingVisual != null && standingVisual != transform)
            return;

        List<Transform> candidates = new();
        for (int index = 0; index < transform.childCount; index++)
        {
            Transform child = transform.GetChild(index);
            if (child == null || child == stumpVisual)
                continue;
            if (child.GetComponentInChildren<Renderer>(true) != null)
                candidates.Add(child);
        }

        if (candidates.Count == 1)
        {
            standingVisual = candidates[0];
            return;
        }

        if (candidates.Count > 1)
        {
            GameObject group = new("StandingVisual_Runtime");
            group.transform.SetParent(transform, false);
            for (int index = 0; index < candidates.Count; index++)
                candidates[index].SetParent(group.transform, true);
            standingVisual = group.transform;
            return;
        }

        // Kompatibilitas scene lama yang meletakkan MeshRenderer langsung pada root.
        standingVisual = transform;
    }

    /// <summary>
    /// Membuat pivot animasi pada pangkal visual pohon. Model dari artist sering memiliki pivot
    /// di tengah mesh; memutarnya langsung hanya membuat pohon berputar tanpa terlihat roboh.
    /// Container ini dibuat saat runtime sehingga hierarchy scene dan prefab model tidak diubah.
    /// </summary>
    void CreateFallPivotAtVisualBase()
    {
        // Mesh yang berada langsung pada root tidak boleh dipindahkan karena root juga menyimpan
        // collider dan komponen gameplay. Model child/custom tetap mendapat pivot otomatis.
        if (standingVisual == null || standingVisual == transform)
            return;

        Renderer[] visualRenderers = standingVisual.GetComponentsInChildren<Renderer>(true);
        if (!TryCalculateRendererBounds(visualRenderers, out Bounds visualBounds))
            return;

        GameObject pivotObject = new("TreeFallPivot_Runtime");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(transform, false);

        // Bounds dihitung di world space agar pivot tetap akurat walaupun model memiliki offset,
        // rotasi, atau scale berbeda dari root WorldTree.
        pivot.position = new Vector3(visualBounds.center.x, visualBounds.min.y, visualBounds.center.z);
        pivot.rotation = transform.rotation;
        standingVisual.SetParent(pivot, true);
        standingVisual = pivot;
    }

    static bool TryCalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        if (renderers == null)
            return false;

        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    Renderer[] CollectStandingRenderers()
    {
        Renderer[] candidates = standingVisual != null
            ? standingVisual.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);
        if (stumpVisual == null)
            return candidates;

        List<Renderer> filtered = new(candidates.Length);
        for (int index = 0; index < candidates.Length; index++)
        {
            Renderer candidate = candidates[index];
            if (candidate != null && !candidate.transform.IsChildOf(stumpVisual))
                filtered.Add(candidate);
        }
        return filtered.ToArray();
    }

    static bool HasValidRenderer(Renderer[] renderers)
    {
        if (renderers == null)
            return false;
        for (int index = 0; index < renderers.Length; index++)
            if (renderers[index] != null)
                return true;
        return false;
    }

    void EnsureHighlight()
    {
        if (highlight != null) return;
        GameObject indicator = new("TreeHighlight");
        indicator.transform.SetParent(transform, false);
        highlight = indicator.AddComponent<LineRenderer>();
        highlight.loop = true;
        highlight.positionCount = 4;
        highlight.useWorldSpace = true;
        highlight.startWidth = highlight.endWidth = 0.065f;
        highlight.startColor = highlight.endColor = new Color(1f, 0.72f, 0.12f, 1f);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            highlightMaterial = new Material(shader);
            highlight.sharedMaterial = highlightMaterial;
        }
        RefreshHighlightBounds();
        indicator.SetActive(false);
    }

    void RefreshHighlightBounds()
    {
        if (highlight == null) return;
        Bounds bounds = interactionCollider != null ? interactionCollider.bounds : new Bounds(transform.position, Vector3.one);
        float y = bounds.min.y + 0.06f;
        highlight.SetPosition(0, new(bounds.min.x, y, bounds.min.z));
        highlight.SetPosition(1, new(bounds.max.x, y, bounds.min.z));
        highlight.SetPosition(2, new(bounds.max.x, y, bounds.max.z));
        highlight.SetPosition(3, new(bounds.min.x, y, bounds.max.z));
    }

    public TreeSaveData Capture() => new() { id = treeId, state = state, durability = durability, respawnDay = respawnDay, progress = progress?.Copy() };

    /// <summary>Mengembalikan durability, growth state, dan jadwal respawn satu pohon.</summary>
    public void Restore(TreeSaveData data)
    {
        if (data == null) return;
        treeId = data.id;
        progress = data.progress?.Copy() ?? TreeProgress.Create(definition, CurrentDay, true);
        StopAllCoroutines();
        if (standingVisual != null) standingVisual.localRotation = standingRestRotation;
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (woodChipParticles != null)
            woodChipParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        state = data.state;
        durability = Mathf.Max(0, data.durability);
        respawnDay = data.respawnDay;
        transitioning = false;
        ApplyStateVisual();
    }

    static int CurrentDay => TimeManager.Instance != null ? TimeManager.Instance.day : 1;

    public void InitializePlanted(string id, TreeDefinition profile)
    {
        treeId = id;
        definition = profile;
        woodItem = profile.woodItem;
        canRespawn = profile.wildTree;
        progress = TreeProgress.Create(profile, CurrentDay, false);
        if (stumpVisual == null)
        {
            GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "Stump_Runtime";
            stump.transform.SetParent(transform, false);
            stump.transform.localPosition = Vector3.up * 0.25f;
            stump.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            stump.GetComponent<Collider>().enabled = false;
            stumpVisual = stump.transform;
        }
        shownStage = -2;
        ApplyStateVisual();
    }

    void AdvanceGrowth()
    {
        if (state != TreeGrowthState.Standing || definition == null) return;
        progress.Advance(definition, CurrentDay, WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : WeatherType.Sunny,
            FarmPlacement.Covered(transform.position), UnityEngine.Random.value);
        ApplyStateVisual();
    }

    public static bool WaterAt(Vector3 point, float radius)
    {
        foreach (WorldTree tree in Registry)
        {
            Vector3 delta = tree.transform.position - point;
            delta.y = 0;
            if (tree.state != TreeGrowthState.Standing || delta.sqrMagnitude > radius * radius || tree.progress == null) continue;
            tree.progress.wateredDay = CurrentDay;
            return true;
        }
        return false;
    }

    void Update()
    {
        if (definition == null || progress == null || state != TreeGrowthState.Standing) return;
        if (fruitInventory == null) fruitInventory = FindFirstObjectByType<Inventory>();
        if (fruitInventory == null || (fruitInventory.GetComponent<PlayerController>()?.IsMovementLocked ?? false)) return;
        // Pohon dalam farm mengikuti satu tile target hoe, bukan semua pohon dalam radius.
        bool inField = FieldArea.TryGetAt(transform.position, out _, out _, out _);
        FarmingTool farming = fruitInventory.GetComponent<FarmingTool>();
        if (inField)
        {
            if (farming == null || !farming.IsCurrentTileTarget(transform.position)) return;
        }
        else if (!PlayerInteractionTarget.Contains(fruitInventory.transform, transform)) return;
        if (progress.fruitStage == TreeFruitStage.Ready && definition.fruitItem != null &&
            (definition.fruitSeasons & CropDataSO.GetSeasonForDay(CurrentDay)) != 0)
        {
            WorldInteractionPrompt.Request(this, transform, "E - Panen " + definition.fruitItem.itemName, 1f, promptHeight);
            if (PlayerInteractionTarget.Press(fruitInventory.transform, transform, KeyCode.E))
                TryHarvestFruit(fruitInventory);
        }
        else if (HUDManager.DebugCluesEnabled)
            WorldInteractionPrompt.Request(this, transform,
                $"{definition.treeType}: {progress.growth:0.#}/{definition.matureDays} growth | HP {progress.health} | {progress.fruitStage}", 5f, promptHeight);
        else if (inField)
            WorldInteractionPrompt.Request(this, transform, definition.treeType, 1f, promptHeight);
    }

    public bool TryHarvestFruit(Inventory inventory)
    {
        if (inventory == null || definition == null || definition.fruitItem == null || state != TreeGrowthState.Standing ||
            progress.fruitStage != TreeFruitStage.Ready || (definition.fruitSeasons & CropDataSO.GetSeasonForDay(CurrentDay)) == 0) return false;
        if (!inventory.Add(definition.fruitItem, Mathf.Max(1, definition.fruitAmount))) return false;
        progress.fruitProgress = 0;
        progress.fruitStage = TreeFruitStage.Flowering;
        return true;
    }

    void ApplyGrowthVisual()
    {
        if (definition == null || progress == null || standingVisual == null) return;
        int index = definition.StageIndex(progress.growth);
        if (index == shownStage) return;
        shownStage = index;
        if (stageModel != null) { stageModel.SetActive(false); Destroy(stageModel); }
        TreeStageVisual stage = index >= 0 ? definition.stages[index] : null;
        bool custom = stage?.model != null && standingVisual != transform;
        foreach (Renderer renderer in standingRenderers) if (renderer != null) renderer.enabled = !custom;
        if (standingVisual != transform)
            standingVisual.localScale = Vector3.Scale(matureVisualScale, stage != null ? stage.scale : Vector3.one);
        if (custom)
        {
            stageModel = Instantiate(stage.model, standingVisual);
            stageModel.transform.localPosition = Vector3.zero;
            foreach (Collider collider in stageModel.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
    }

    public static List<TreeSaveData> CaptureAll()
    {
        List<TreeSaveData> result = new(Registry.Count);
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && !Registry[i].terrainManaged)
            {
                Registry[i].FinishPendingFall();
                result.Add(Registry[i].Capture());
            }
        return result;
    }

    /// <summary>Mencocokkan dan memulihkan seluruh pohon terdaftar dari save.</summary>
    public static void RestoreAll(List<TreeSaveData> data)
    {
        if (data == null) return;
        Dictionary<string, TreeSaveData> map = new();
        for (int i = 0; i < data.Count; i++) if (data[i] != null && !string.IsNullOrEmpty(data[i].id)) map[data[i].id] = data[i];
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && !Registry[i].terrainManaged &&
                map.TryGetValue(Registry[i].treeId, out TreeSaveData saved)) Registry[i].Restore(saved);
    }
}
