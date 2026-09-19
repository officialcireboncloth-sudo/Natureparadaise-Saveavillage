using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class FishPondFishData
{
    public string itemId;
    public string assetName;
    public string itemName;
    public int qualityStars;
    public float sizeCm;
    public float weightKg;
    public int growthDays;
}

[Serializable]
public sealed class FishPondSaveData
{
    public string pondId;
    public int level = 1;
    public bool fedForGrowth;
    public int feedUnitsUsed;
    public int feedStock;
    public int lastGrowthDay;
    public Vector3 position;
    public Vector3 eulerAngles;
    public List<FishPondFishData> fish = new();
}

/// <summary>State persisten dan simulasi harian seluruh Fish Pond, termasuk saat scene Map tidak aktif.</summary>
public static class FishPondService
{
    static readonly Dictionary<string, FishPondSaveData> Records = new(StringComparer.Ordinal);
    public static event Action Changed;

    public static int Capacity(int level) => Mathf.Clamp(level, 1, 4) switch
    {
        1 => 4,
        2 => 8,
        3 => 12,
        _ => 16
    };

    public static int FeedCapacity(int level) => Mathf.Clamp(level, 1, 4) switch
    {
        1 => 4,
        2 => 8,
        3 => 12,
        _ => 16
    };

    public static FishPondSaveData GetOrCreate(string pondId, int level, Transform owner)
    {
        if (!Records.TryGetValue(pondId, out FishPondSaveData record))
        {
            record = new FishPondSaveData { pondId = pondId };
            Records.Add(pondId, record);
        }
        record.level = Mathf.Clamp(level, 1, 4);
        record.feedStock = Mathf.Clamp(record.feedStock, 0, FeedCapacity(record.level));
        if (owner != null) { record.position = owner.position; record.eulerAngles = owner.eulerAngles; }
        record.fish ??= new List<FishPondFishData>();
        return record;
    }

    public static bool HasFishForSite(string siteId) =>
        Records.TryGetValue(IdForSite(siteId), out FishPondSaveData record) && record.fish?.Count > 0;

    public static void TransferSite(string sourceSiteId, string destinationSiteId)
    {
        string sourceId = IdForSite(sourceSiteId);
        string destinationId = IdForSite(destinationSiteId);
        if (sourceId == destinationId || !Records.TryGetValue(sourceId, out FishPondSaveData record)) return;
        Records.Remove(sourceId);
        record.pondId = destinationId;
        Records[destinationId] = record;
        Changed?.Invoke();
    }

    public static void RemoveForSite(string siteId)
    {
        Records.Remove(IdForSite(siteId));
        Changed?.Invoke();
    }

    public static string IdForSite(string siteId) => $"fishpond.{siteId}";

    public static void AdvanceDay(int completedDay)
    {
        int grown = 0;
        foreach (FishPondSaveData pond in Records.Values)
        {
            if (pond == null || pond.lastGrowthDay >= completedDay) continue;
            pond.lastGrowthDay = completedDay;
            int fishCount = pond.fish?.Count ?? 0;
            int feedNeeded = Mathf.Max(1, Mathf.CeilToInt(fishCount / 10f));
            bool hasFeed = pond.fedForGrowth;
            if (!hasFeed && fishCount > 0 && pond.feedStock >= feedNeeded)
            {
                pond.feedStock -= feedNeeded;
                pond.feedUnitsUsed = feedNeeded;
                hasFeed = true;
            }
            if (!hasFeed || fishCount == 0)
            {
                pond.fedForGrowth = false;
                pond.feedUnitsUsed = 0;
                continue;
            }

            foreach (FishPondFishData fish in pond.fish)
            {
                ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
                FishSizeTier tier = FishMeasurement.GetSizeTier(item, fish.sizeCm);
                if (tier == FishSizeTier.Jumbo) continue;
                FishDefinitionSO definition = FishMeasurement.FindDefinition(item);
                fish.growthDays++;
                int needed = definition != null ? definition.GetPondGrowthDays(tier) :
                    tier == FishSizeTier.Small ? 7 : tier == FishSizeTier.Medium ? 10 : 14;
                if (fish.growthDays < needed) continue;
                fish.sizeCm = definition != null ? definition.GetNextPondSize(tier) :
                    tier == FishSizeTier.Small ? 22f : tier == FishSizeTier.Medium ? 35f : 50f;
                fish.weightKg = FishMeasurement.EstimateWeightKg(item, fish.sizeCm);
                fish.growthDays = 0;
                grown++;
            }
            pond.fedForGrowth = false;
        }
        if (grown > 0) SaveLoadFeedback.Instance?.ShowMessage($"Fish Pond: {grown} ikan naik ukuran");
        Changed?.Invoke();
    }

    public static List<FishPondSaveData> Capture() => Records.Values.Select(Clone).ToList();

    public static void Restore(List<FishPondSaveData> saved)
    {
        Records.Clear();
        foreach (FishPondSaveData source in saved ?? new List<FishPondSaveData>())
            if (source != null && !string.IsNullOrWhiteSpace(source.pondId)) Records[source.pondId] = Clone(source);
        Changed?.Invoke();
    }

    public static void Clear() { Records.Clear(); Changed?.Invoke(); }
    public static void NotifyChanged() => Changed?.Invoke();

    static FishPondSaveData Clone(FishPondSaveData source)
    {
        FishPondSaveData clone = new()
        {
            pondId = source.pondId,
            level = source.level,
            fedForGrowth = source.fedForGrowth,
            feedUnitsUsed = source.feedUnitsUsed,
            feedStock = source.feedStock,
            lastGrowthDay = source.lastGrowthDay,
            position = source.position,
            eulerAngles = source.eulerAngles
        };
        foreach (FishPondFishData fish in source.fish ?? new List<FishPondFishData>())
            clone.fish.Add(new FishPondFishData
            {
                itemId = fish.itemId, assetName = fish.assetName, itemName = fish.itemName,
                qualityStars = fish.qualityStars, sizeCm = fish.sizeCm,
                weightKg = fish.weightKg, growthDays = fish.growthDays
            });
        return clone;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterDailyGrowth()
    {
        Records.Clear();
        Changed = null;
        TimeManager.OnBeforeDayChange -= HandleBeforeDayChange;
        TimeManager.OnBeforeDayChange += HandleBeforeDayChange;
    }

    static void HandleBeforeDayChange() => AdvanceDay(TimeManager.Instance != null ? TimeManager.Instance.day : 1);
}

/// <summary>Storage ikan hidup dan UI pond. Data dimiliki service agar aman saat visual level diganti.</summary>
[DisallowMultipleComponent]
public sealed class FishPond : MonoBehaviour
{
    [SerializeField] string pondId;
    [SerializeField, Range(1, 4)] int fallbackLevel = 1;
    [SerializeField] ItemSO fishFeed;
    [SerializeField, Min(1)] int fishPerFeedUnit = 10;
    [SerializeField, Range(1, 10)] int maximumVisibleFish = 8;
    [SerializeField, Min(0.5f)] float interactionRadius = 3.5f;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [Header("Editable Feed Stations")]
    [SerializeField] Transform feedingPoint;
    [SerializeField] FeedMaker feedMaker;
    [SerializeField, Min(1f)] float feedInteractionRadius = 2.4f;

    readonly List<Transform> visualFish = new();
    readonly List<Vector3> visualOrigins = new();
    Rect windowRect = new(0f, 0f, 940f, 650f);
    Vector2 inventoryScroll;
    Vector2 pondScroll;
    FishPondSaveData record;
    Inventory inventory;
    PlayerController player;
    PropertySite site;
    Transform visualRoot;
    bool panelOpen;
    string feedback = string.Empty;
    int draggedFeedSlot = -1;
    string draggedFeedLabel;
    WorldDebugStatusLabel combinedDebugLabel;

    public int Level => site != null && site.CurrentLevel > 0 ? site.CurrentLevel : fallbackLevel;
    public int Capacity => FishPondService.Capacity(Level);
    public int FishCount => record?.fish?.Count ?? 0;
    int FeedNeeded => Mathf.Max(1, Mathf.CeilToInt(FishCount / (float)Mathf.Max(1, fishPerFeedUnit)));
    public int FeedCapacity => FishPondService.FeedCapacity(Level);
    public int FeedStock => record?.feedStock ?? 0;
    public int FeedSpace => Mathf.Max(0, FeedCapacity - FeedStock);

    void Awake()
    {
        site = GetComponentInParent<PropertySite>();
        if (fishFeed == null) fishFeed = Resources.Load<ItemSO>("Items/Fish/Fish Feed");
        if (string.IsNullOrWhiteSpace(pondId)) pondId = site != null
            ? FishPondService.IdForSite(site.SiteId)
            : $"fishpond.{gameObject.scene.name}.{TransformPath(transform)}".ToLowerInvariant().Replace(' ', '_');
        if (!Application.isPlaying) return;
        EnsureFeedStations();
        ResolvePlayer(); ResolveRecord(); RebuildVisuals(); CenterWindow();
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        FishPondService.Changed += HandleChanged;
        ResolveRecord(); RebuildVisuals();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        FishPondService.Changed -= HandleChanged;
        ClosePanel();
    }

    public void Configure(string id, int level, ItemSO feed)
    {
        pondId = id;
        fallbackLevel = Mathf.Clamp(level, 1, 4);
        fishFeed = feed;
    }

#if UNITY_EDITOR
    public void EditorConfigureFeedStations(Transform trough, FeedMaker machine)
    {
        feedingPoint = trough;
        feedMaker = machine;
    }
#endif

    void Update()
    {
        AnimateFish(); ResolvePlayer();
        RefreshFeedDebugLabel();
        if (inventory == null) return;
        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel();
            return;
        }
        float troughDistance = feedingPoint != null
            ? Vector3.Distance(inventory.transform.position, feedingPoint.position) : float.PositiveInfinity;
        float makerDistance = feedMaker != null
            ? Vector3.Distance(inventory.transform.position, feedMaker.transform.position) : float.PositiveInfinity;
        if (feedMaker != null && makerDistance <= feedInteractionRadius && makerDistance <= troughDistance)
        {
            WorldInteractionPrompt.Request(this, feedMaker.transform,
                $"E: Kelola Feed Maker\nF: Ambil hasil — Animal {feedMaker.AnimalFeedOutput} | Fish {feedMaker.FishFeedOutput}",
                makerDistance, 1.3f);
            if (PlayerInteractionTarget.PressPickup(inventory.transform, feedMaker.transform, KeyCode.F, feedInteractionRadius))
            {
                bool animal = feedMaker.Collect(inventory);
                bool fish = feedMaker.CollectFishFeed(inventory);
                SaveLoadFeedback.Instance?.ShowMessage(animal || fish ? "Hasil Feed Maker masuk Inventory." : "Belum ada hasil / Inventory penuh.");
            }
            else if (PlayerInteractionTarget.PressPickup(inventory.transform, feedMaker.transform, interactKey, feedInteractionRadius))
                feedMaker.OpenFor(inventory);
            return;
        }
        if (feedingPoint != null && troughDistance <= feedInteractionRadius)
        {
            WorldInteractionPrompt.Request(this, feedingPoint,
                $"{interactKey}: Tempat Pakan Ikan — {FeedStock}/{FeedCapacity}\nButuh {FeedNeeded}/hari | {FeedRemainingText()}",
                troughDistance, 1.25f);
            if (PlayerInteractionTarget.PressPickup(inventory.transform, feedingPoint, interactKey, feedInteractionRadius)) OpenPanel();
            return;
        }
        if (!PlayerInteractionTarget.ContainsPickup(inventory.transform, transform, interactionRadius)) return;
        float distance = Vector3.Distance(inventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform,
            $"{interactKey}: Fish Pond Lv.{Level} — {FishCount}/{Capacity} | {(record?.fedForGrowth == true ? "Fed" : "Needs Feed")}",
            distance, 1.2f);
        if (PlayerInteractionTarget.PressPickup(inventory.transform, transform, interactKey, interactionRadius)) OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect.width = Mathf.Clamp(Screen.width - 24f, 650f, 940f);
        windowRect.height = Mathf.Clamp(Screen.height - 24f, 460f, 650f);
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow,
            $"FISH POND LV.{Level} — FISH {FishCount}/{Capacity}");
    }

    void DrawWindow(int id)
    {
        DrawFeedStorage();
        GUILayout.Label(FishCount > 0
            ? $"Pertumbuhan membutuhkan {FeedNeeded} Fish Feed saat 00:00. {FeedRemainingText()}"
            : "Masukkan ikan untuk mulai; stok pakan tidak berkurang selama kolam kosong.");
        GUILayout.BeginHorizontal();
        DrawInventory(); GUILayout.Space(10f); DrawPond();
        GUILayout.EndHorizontal();
        if (!string.IsNullOrWhiteSpace(feedback)) GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(30f))) ClosePanel();
        DrawDraggedFeedGhost();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void DrawFeedStorage()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.5f));
        GUILayout.Label("INVENTORY — FISH FEED");
        bool found = false;
        for (int index = 0; inventory != null && index < inventory.slots.Count; index++)
        {
            ItemStack stack = inventory.GetSlot(index);
            if (stack?.item == null || stack.item != fishFeed || stack.count <= 0) continue;
            found = true;
            GUILayout.BeginHorizontal(GUI.skin.box);
            Rect dragRect = GUILayoutUtility.GetRect(new GUIContent($"{stack.DisplayName} x{stack.count}"), GUI.skin.box,
                GUILayout.ExpandWidth(true), GUILayout.Height(44f));
            GUI.Box(dragRect, $"{stack.DisplayName} x{stack.count}\nDRAG", GUI.skin.box);
            HandleFeedDragSource(dragRect, index, stack);
            if (GUILayout.Button("+1", GUILayout.Width(44f), GUILayout.Height(44f))) DepositFeed(index, 1);
            if (GUILayout.Button("All", GUILayout.Width(48f), GUILayout.Height(44f))) DepositFeed(index, stack.count);
            GUILayout.EndHorizontal();
        }
        if (!found) GUILayout.Label("Fish Feed tidak ada di Inventory.");
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        GUILayout.Label($"TEMPAT PAKAN IKAN — {FeedStock}/{FeedCapacity}");
        Rect targetRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.box,
            GUILayout.ExpandWidth(true), GUILayout.Height(62f));
        Color previous = GUI.color;
        GUI.color = FeedSpace > 0 ? new Color(0.72f, 1f, 0.72f) : new Color(1f, 0.58f, 0.58f);
        GUI.Box(targetRect, FeedSpace > 0
            ? $"DROP FISH FEED DI SINI\nSisa kapasitas {FeedSpace}"
            : $"PENUH\n{FeedStock}/{FeedCapacity}");
        GUI.color = previous;
        HandleFeedDrop(targetRect);
        if (GUILayout.Button($"Ambil Fish Feed x{FeedStock}")) WithdrawFeed();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    void DrawInventory()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.43f));
        GUILayout.Label("ADD FISH — INVENTORY");
        inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
        bool found = false;
        for (int index = 0; inventory != null && index < inventory.slots.Count; index++)
        {
            ItemStack stack = inventory.GetSlot(index);
            if (stack?.item == null || stack.item.category != ItemCategory.Fish || stack.count <= 0) continue;
            found = true;
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{FishLabel(stack.item, stack.qualityStars, stack.fishSizeCm, 0)} x{stack.count}");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = FishCount < Capacity;
            if (GUILayout.Button("Add", GUILayout.Width(56f))) AddFish(index);
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }
        if (!found) GUILayout.Label("Tidak ada ikan di Inventory.");
        GUILayout.EndScrollView(); GUILayout.EndVertical();
    }

    void DrawPond()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.57f));
        GUILayout.Label("TAKE FISH — POND");
        pondScroll = GUILayout.BeginScrollView(pondScroll);
        if (record?.fish != null)
        {
            for (int index = record.fish.Count - 1; index >= 0; index--)
            {
                FishPondFishData fish = record.fish[index];
                ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(FishLabel(item, fish.qualityStars, fish.sizeCm, fish.growthDays));
                if (GUILayout.Button("Take", GUILayout.Width(56f))) TakeFish(index);
                GUILayout.EndHorizontal();
            }
        }
        if (FishCount == 0) GUILayout.Label("Pond masih kosong.");
        GUILayout.EndScrollView(); GUILayout.EndVertical();
    }

    void AddFish(int slotIndex)
    {
        ItemStack stack = inventory?.GetSlot(slotIndex);
        if (stack?.item == null || stack.item.category != ItemCategory.Fish || FishCount >= Capacity) return;
        ItemSO item = stack.item;
        int quality = stack.qualityStars;
        FishDefinitionSO definition = FishMeasurement.FindDefinition(item);
        float size = stack.fishSizeCm > 0f ? stack.fishSizeCm : definition != null ? definition.minimumSizeCm : 15f;
        float weight = stack.fishWeightKg > 0f ? stack.fishWeightKg : FishMeasurement.EstimateWeightKg(item, size);
        if (!inventory.RemoveFromSlot(slotIndex, 1)) return;
        record.fish.Add(new FishPondFishData
        {
            itemId = item.Id, assetName = item.name, itemName = item.itemName,
            qualityStars = quality, sizeCm = size, weightKg = weight
        });
        feedback = $"{item.itemName} dimasukkan ke pond.";
        Commit();
    }

    void TakeFish(int index)
    {
        if (record?.fish == null || index < 0 || index >= record.fish.Count || inventory == null) return;
        FishPondFishData fish = record.fish[index];
        ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
        if (item == null) { feedback = "Asset ikan tidak ditemukan."; return; }
        if (!inventory.CanAdd(item, 1, fish.qualityStars, fish.sizeCm, fish.weightKg)) { feedback = "Inventory penuh."; return; }
        if (!inventory.Add(item, 1, fish.qualityStars, fish.sizeCm, fish.weightKg)) return;
        record.fish.RemoveAt(index);
        feedback = $"{item.itemName} diambil kembali.";
        Commit();
    }

    void DepositFeed(int slotIndex, int amount)
    {
        ItemStack stack = inventory?.GetSlot(slotIndex);
        if (stack?.item == null || stack.item != fishFeed || amount <= 0) { feedback = "Hanya Fish Feed yang dapat dimasukkan."; return; }
        int moved = Mathf.Min(amount, stack.count, FeedSpace);
        if (moved <= 0 || !inventory.RemoveFromSlot(slotIndex, moved)) { feedback = "Tempat pakan penuh."; return; }
        record.feedStock += moved;
        feedback = $"Fish Feed x{moved} dimasukkan. Stok {FeedStock}/{FeedCapacity}.";
        Commit();
    }

    void WithdrawFeed()
    {
        if (fishFeed == null || inventory == null || FeedStock <= 0 || !inventory.Add(fishFeed, FeedStock))
        { feedback = "Stok kosong atau Inventory penuh."; return; }
        int moved = FeedStock;
        record.feedStock = 0;
        feedback = $"Fish Feed x{moved} dikembalikan ke Inventory.";
        Commit();
    }

    void HandleFeedDragSource(Rect rect, int slotIndex, ItemStack stack)
    {
        Event current = Event.current;
        if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition)) return;
        draggedFeedSlot = slotIndex;
        draggedFeedLabel = $"{stack.DisplayName} x{stack.count}";
        current.Use();
    }

    void HandleFeedDrop(Rect rect)
    {
        Event current = Event.current;
        if (draggedFeedSlot < 0 || current.type != EventType.MouseUp || current.button != 0) return;
        if (rect.Contains(current.mousePosition))
        {
            ItemStack stack = inventory?.GetSlot(draggedFeedSlot);
            DepositFeed(draggedFeedSlot, stack?.count ?? 0);
            current.Use();
        }
        draggedFeedSlot = -1;
        draggedFeedLabel = null;
    }

    void DrawDraggedFeedGhost()
    {
        if (draggedFeedSlot < 0) return;
        Event current = Event.current;
        if (current.type == EventType.MouseUp) { draggedFeedSlot = -1; draggedFeedLabel = null; return; }
        GUI.Box(new Rect(current.mousePosition.x + 12f, current.mousePosition.y + 12f, 160f, 34f),
            draggedFeedLabel ?? "Fish Feed");
        if (current.type == EventType.MouseDrag) current.Use();
    }

    string FishLabel(ItemSO item, int quality, float size, int progress)
    {
        FishSizeTier tier = FishMeasurement.GetSizeTier(item, size);
        FishDefinitionSO definition = FishMeasurement.FindDefinition(item);
        int target = tier == FishSizeTier.Jumbo ? 0 : definition != null ? definition.GetPondGrowthDays(tier) :
            tier == FishSizeTier.Small ? 7 : tier == FishSizeTier.Medium ? 10 : 14;
        float weight = FishMeasurement.EstimateWeightKg(item, size);
        string growth = tier == FishSizeTier.Jumbo ? "MAX" : $"Growth {progress}/{target}";
        return $"{item?.itemName ?? "Unknown Fish"} | {QualityLabel(quality)} | {tier} | {size:0.#} cm | {weight:0.00} kg | {growth}";
    }

    void Commit()
    {
        record.level = Level;
        record.position = transform.position;
        record.eulerAngles = transform.eulerAngles;
        FishPondService.NotifyChanged();
        RebuildVisuals();
    }

    void ResolveRecord() => record = FishPondService.GetOrCreate(pondId, Level, transform);
    void HandleChanged() { ResolveRecord(); RebuildVisuals(); }

    void RebuildVisuals()
    {
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        visualFish.Clear(); visualOrigins.Clear();
        visualRoot = new GameObject("FishVisuals_Runtime").transform;
        visualRoot.SetParent(transform, false);
        if (record?.fish == null) return;
        int visible = Mathf.Min(maximumVisibleFish, record.fish.Count);
        for (int index = 0; index < visible; index++)
        {
            FishPondFishData fish = record.fish[index];
            ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
            GameObject model = item != null && item.worldPrefab != null
                ? Instantiate(item.worldPrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = $"FishVisual_{index}_{fish.itemName}";
            model.transform.SetParent(visualRoot, false);
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            float scale = FishMeasurement.GetSizeTier(item, fish.sizeCm) switch
            { FishSizeTier.Small => 0.7f, FishSizeTier.Medium => 1f, FishSizeTier.Large => 1.3f, _ => 1.6f };
            model.transform.localScale = new Vector3(0.32f, 0.12f, 0.12f) * scale;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            float angle = index / (float)Mathf.Max(1, visible) * Mathf.PI * 2f;
            Vector3 origin = new(Mathf.Cos(angle) * 0.32f, 0.05f, Mathf.Sin(angle) * 0.32f);
            model.transform.localPosition = origin;
            visualFish.Add(model.transform); visualOrigins.Add(origin);
            Renderer renderer = model.GetComponentInChildren<Renderer>();
            if (renderer != null && item?.worldPrefab == null)
                renderer.material.color = Color.HSVToRGB(StableHue(item?.Id ?? fish.itemName), 0.62f, 0.95f);
        }
    }

    void AnimateFish()
    {
        for (int index = 0; index < visualFish.Count; index++)
        {
            Transform fish = visualFish[index]; if (fish == null) continue;
            float phase = Time.unscaledTime * (0.65f + index * 0.035f) + index;
            Vector3 origin = visualOrigins[index];
            fish.localPosition = origin + new Vector3(Mathf.Sin(phase) * 0.18f, Mathf.Sin(phase * 1.4f) * 0.03f, Mathf.Cos(phase) * 0.12f);
            fish.localRotation = Quaternion.Euler(0f, -phase * Mathf.Rad2Deg, 90f);
        }
    }

    void ResolvePlayer()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (player == null && inventory != null) player = inventory.GetComponent<PlayerController>();
    }

    void EnsureFeedStations()
    {
        if (feedingPoint == null) feedingPoint = transform.Find("FishFeedTrough_Editable");
        if (feedingPoint == null)
        {
            GameObject trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trough.name = "FishFeedTrough_Editable";
            trough.transform.SetParent(transform, false);
            trough.transform.localPosition = new Vector3(3.25f + (Level - 1) * 0.35f, 0.35f, -1.75f);
            trough.transform.localScale = new Vector3(1.15f, 0.7f, 1.35f);
            feedingPoint = trough.transform;
        }
        if (feedMaker == null) feedMaker = GetComponentInChildren<FeedMaker>(true);
        if (feedMaker == null)
        {
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.name = "FishPondFeedMaker_Editable";
            machine.transform.SetParent(transform, false);
            machine.transform.localPosition = new Vector3(3.25f + (Level - 1) * 0.35f, 0.8f, 1.75f);
            machine.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
            feedMaker = machine.AddComponent<FeedMaker>();
        }
        feedMaker.Configure($"feedmaker.{pondId}", Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog"));
        feedMaker.SetContextLabel("FISH POND");
        feedMaker.SetWorldDebugVisible(false);
        Transform oldFeedLabel = feedingPoint.Find("FishFeed_DebugLabel");
        if (oldFeedLabel != null) oldFeedLabel.gameObject.SetActive(false);
        combinedDebugLabel = WorldDebugStatusLabel.GetOrCreate(transform, "FishPond_DebugLabel", new Vector3(0f, 2.35f, 0f));
        combinedDebugLabel?.SetColor(new Color(0.3f, 0.95f, 1f, 1f));
    }

    string FeedRemainingText()
    {
        if (FishCount <= 0) return "Tidak berkurang (kolam kosong)";
        if (FeedStock < FeedNeeded) return $"Kurang {FeedNeeded - FeedStock}; tidak cukup untuk 00:00";
        int fullDays = FeedStock / FeedNeeded;
        int untilReset = TimeManager.Instance == null ? 0 :
            24 * 60 - (TimeManager.Instance.hour * 60 + TimeManager.Instance.minute);
        int totalMinutes = Mathf.Max(0, untilReset) + Mathf.Max(0, fullDays - 1) * 24 * 60;
        return $"Habis sekitar {GameTimeDebugText.FormatMinutes(totalMinutes)} ({fullDays} hari pakan)";
    }

    void RefreshFeedDebugLabel()
    {
        if (combinedDebugLabel == null)
            combinedDebugLabel = WorldDebugStatusLabel.GetOrCreate(transform, "FishPond_DebugLabel", new Vector3(0f, 2.35f, 0f));
        combinedDebugLabel?.SetColor(new Color(0.3f, 0.95f, 1f, 1f));
        feedMaker?.SetWorldDebugVisible(false);
        Transform oldFeedLabel = feedingPoint != null ? feedingPoint.Find("FishFeed_DebugLabel") : null;
        if (oldFeedLabel != null) oldFeedLabel.gameObject.SetActive(false);

        string makerStatus = feedMaker != null ? feedMaker.CompactDebugStatus : "EMPTY";
        int feedPercent = FeedCapacity > 0 ? Mathf.RoundToInt(FeedStock * 100f / FeedCapacity) : 0;
        string feedStatus = FeedStock <= 0
            ? "EMPTY"
            : $"ADA {FeedStock}/{FeedCapacity} ({feedPercent}%) | Habis {FeedEstimateShort()}";
        string pondStatus = FishCount <= 0 ? $"EMPTY 0/{Capacity}" : $"ADA {FishCount}/{Capacity}";
        combinedDebugLabel?.SetText($"FISH POND Lv.{Level}\n"+
            $"Feed Maker: {makerStatus}\n"+
            $"Tempat Makan: {feedStatus}\n"+
            $"Kolam: {pondStatus}");
    }

    string FeedEstimateShort()
    {
        if (FishCount <= 0) return "-";
        if (FeedStock < FeedNeeded) return "< 1 hari";
        int fullDays = FeedStock / FeedNeeded;
        int untilReset = TimeManager.Instance == null ? 0 :
            24 * 60 - (TimeManager.Instance.hour * 60 + TimeManager.Instance.minute);
        int minutes = Mathf.Max(0, untilReset) + Mathf.Max(0, fullDays - 1) * 24 * 60;
        return GameTimeDebugText.FormatMinutes(minutes);
    }

    void OpenPanel()
    {
        panelOpen = true; CenterWindow();
        player?.AcquireMovementLock(this); TimeManager.Instance?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this);
    }

    void ClosePanel()
    {
        if (!panelOpen) return;
        panelOpen = false;
        draggedFeedSlot = -1;
        draggedFeedLabel = null;
        player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this);
    }

    void CenterWindow()
    {
        windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f);
        windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f);
    }

    static float StableHue(string value)
    {
        uint hash = 2166136261;
        foreach (char character in value ?? string.Empty) hash = (hash ^ character) * 16777619;
        return hash % 1000 / 1000f;
    }

    static string QualityLabel(int quality) => quality <= 0 ? "Normal" : $"{Mathf.Clamp(quality, 1, 5)}★";
    static string TransformPath(Transform value)
    {
        string path = value.name;
        while (value.parent != null) { value = value.parent; path = value.name + "/" + path; }
        return path;
    }
}
