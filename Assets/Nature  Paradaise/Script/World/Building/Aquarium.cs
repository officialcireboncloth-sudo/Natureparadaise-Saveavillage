using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AquariumSize : byte { Small, Medium, Large, Grand }

[Serializable]
public sealed class AquariumFishData
{
    public string itemId;
    public string assetName;
    public string itemName;
    public int qualityStars;
    public float sizeCm;
    public float weightKg;
}

[Serializable]
public sealed class AquariumSaveData
{
    public string aquariumId;
    public AquariumSize size;
    public string sceneName;
    public Vector3 position;
    public Vector3 eulerAngles;
    public string background = "Blue";
    public string substrate = "Gravel";
    public string plant = "None";
    public string rock = "None";
    public string decoration = "None";
    public List<AquariumFishData> fish = new();
}

/// <summary>Data seluruh Aquarium tetap hidup saat HouseInterior di-unload.</summary>
public static class AquariumService
{
    static readonly Dictionary<string, AquariumSaveData> Records = new(StringComparer.OrdinalIgnoreCase);
    public static event Action Changed;

    public static AquariumSaveData GetOrCreate(string id, AquariumSize size, Transform owner)
    {
        if (!Records.TryGetValue(id, out AquariumSaveData record))
        {
            record = new AquariumSaveData { aquariumId = id, size = size };
            Records[id] = record;
        }
        record.size = size;
        if (owner != null)
        {
            record.sceneName = owner.gameObject.scene.name;
            record.position = owner.position;
            record.eulerAngles = owner.eulerAngles;
        }
        record.fish ??= new List<AquariumFishData>();
        return record;
    }

    public static int Capacity(AquariumSize size) => size switch
    { AquariumSize.Small => 5, AquariumSize.Medium => 10, AquariumSize.Large => 20, AquariumSize.Grand => 30, _ => 5 };

    public static int GetStoredCount(ItemSO item) => item == null ? 0 : Records.Values.Sum(record =>
        (record.fish ?? new List<AquariumFishData>()).Count(fish => fish != null &&
            string.Equals(fish.itemId, item.Id, StringComparison.OrdinalIgnoreCase)));

    public static List<AquariumSaveData> Capture() => Records.Values.Select(Clone).ToList();
    public static void Restore(List<AquariumSaveData> saved)
    {
        Records.Clear();
        foreach (AquariumSaveData data in saved ?? new List<AquariumSaveData>())
            if (data != null && !string.IsNullOrWhiteSpace(data.aquariumId)) Records[data.aquariumId] = Clone(data);
        Changed?.Invoke();
    }
    public static void NotifyChanged() => Changed?.Invoke();
    public static void Clear() { Records.Clear(); Changed?.Invoke(); }

    static AquariumSaveData Clone(AquariumSaveData source) => new()
    {
        aquariumId = source.aquariumId, size = source.size, sceneName = source.sceneName,
        position = source.position, eulerAngles = source.eulerAngles,
        background = source.background, substrate = source.substrate, plant = source.plant,
        rock = source.rock, decoration = source.decoration,
        fish = (source.fish ?? new List<AquariumFishData>()).Where(value => value != null).Select(value => new AquariumFishData
        {
            itemId = value.itemId, assetName = value.assetName, itemName = value.itemName,
            qualityStars = value.qualityStars, sizeCm = value.sizeCm, weightKg = value.weightKg
        }).ToList()
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Records.Clear(); Changed = null; }
}

/// <summary>Furniture Aquarium interaktif. Mesh root dapat diganti tanpa memindahkan data ikan.</summary>
[DisallowMultipleComponent]
public sealed class Aquarium : MonoBehaviour
{
    [SerializeField] string aquariumId;
    [SerializeField] AquariumSize aquariumSize = AquariumSize.Medium;
    [SerializeField, Range(1, 12)] int maximumVisibleFish = 10;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.6f;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0f)] float promptHeight = 1.5f;

    readonly List<Transform> visualFish = new();
    readonly List<Vector3> visualOrigins = new();
    Rect windowRect = new(0f, 0f, 980f, 650f);
    Vector2 inventoryScroll;
    Vector2 aquariumScroll;
    Inventory inventory;
    PlayerController player;
    AquariumSaveData record;
    Transform visualRoot;
    bool panelOpen;
    float feedAnimationUntil;
    string feedback = string.Empty;

    public string AquariumId => aquariumId;
    public int Capacity => AquariumService.Capacity(aquariumSize);
    public int FishCount => record?.fish?.Count ?? 0;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(aquariumId)) aquariumId = BuildStableId();
        if (!Application.isPlaying) return;
        ResolvePlayer(); ResolveRecord(); RebuildVisuals(); CenterWindow();
    }
    void OnEnable() { if (!Application.isPlaying) return; AquariumService.Changed += HandleServiceChanged; ResolveRecord(); RebuildVisuals(); }
    void OnDisable() { if (!Application.isPlaying) return; AquariumService.Changed -= HandleServiceChanged; ClosePanel(); }

    public void Configure(string id, AquariumSize size)
    {
        aquariumId = string.IsNullOrWhiteSpace(id) ? BuildStableId() : id;
        aquariumSize = size;
        if (Application.isPlaying) { ResolveRecord(); RebuildVisuals(); }
    }

    void Update()
    {
        AnimateFish(); ResolvePlayer();
        if (inventory == null) return;
        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel();
            return;
        }
        if (!PlayerInteractionTarget.ContainsPickup(inventory.transform, transform, interactionRadius)) return;
        float distance = Vector3.Distance(inventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform, $"{interactKey}: Aquarium — {FishCount}/{Capacity} fish", distance, promptHeight);
        if (PlayerInteractionTarget.PressPickup(inventory.transform, transform, interactKey, interactionRadius)) OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect.width = Mathf.Clamp(Screen.width - 24f, 650f, 980f);
        windowRect.height = Mathf.Clamp(Screen.height - 24f, 450f, 650f);
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow,
            $"AQUARIUM {aquariumSize.ToString().ToUpperInvariant()} — FISH {FishCount}/{Capacity}");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        DrawInventory(); GUILayout.Space(10f); DrawAquarium();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Feed Fish (Optional)")) { feedAnimationUntil = Time.unscaledTime + 4f; feedback = "Ikan berkumpul. Feeding tidak wajib."; }
        if (GUILayout.Button("View")) { feedback = $"Menampilkan {visualFish.Count} dari {FishCount} ikan tersimpan."; }
        GUILayout.EndHorizontal();
        DrawDecorations();
        if (!string.IsNullOrWhiteSpace(feedback)) GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(32f))) ClosePanel();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void DrawInventory()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.45f));
        GUILayout.Label("ADD FISH — INVENTORY");
        inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
        bool found = false;
        for (int index = 0; inventory != null && index < inventory.slots.Count; index++)
        {
            ItemStack stack = inventory.GetSlot(index);
            if (stack?.item == null || stack.item.category != ItemCategory.Fish || stack.count <= 0) continue;
            found = true; GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{FishLabel(stack.item, stack.qualityStars, stack.fishSizeCm)} x{stack.count}");
            bool previous = GUI.enabled; GUI.enabled = FishCount < Capacity;
            if (GUILayout.Button("Add", GUILayout.Width(58f))) AddFish(index);
            GUI.enabled = previous; GUILayout.EndHorizontal();
        }
        if (!found) GUILayout.Label("Tidak ada ikan di Inventory.");
        GUILayout.EndScrollView(); GUILayout.EndVertical();
    }

    void DrawAquarium()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.55f));
        GUILayout.Label("TAKE FISH — AQUARIUM");
        aquariumScroll = GUILayout.BeginScrollView(aquariumScroll);
        if (record?.fish != null)
        {
            for (int index = record.fish.Count - 1; index >= 0; index--)
            {
                AquariumFishData fish = record.fish[index];
                ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{fish.itemName} | {QualityLabel(fish.qualityStars)} | {FishMeasurement.GetSizeTier(item, fish.sizeCm)} | {fish.weightKg:0.00} kg");
                if (GUILayout.Button("Take", GUILayout.Width(58f))) TakeFish(index);
                GUILayout.EndHorizontal();
            }
        }
        if (FishCount == 0) GUILayout.Label("Aquarium masih kosong.");
        GUILayout.EndScrollView(); GUILayout.EndVertical();
    }

    void DrawDecorations()
    {
        bool changed = false;
        GUILayout.BeginHorizontal(GUI.skin.box);
        if (GUILayout.Button($"Background: {record.background}")) { record.background = Next(record.background, "Blue", "River", "Ocean", "Dark"); changed = true; }
        if (GUILayout.Button($"Base: {record.substrate}")) { record.substrate = Next(record.substrate, "Gravel", "Sand", "Black Sand"); changed = true; }
        if (GUILayout.Button($"Plant: {record.plant}")) { record.plant = Next(record.plant, "None", "Grass", "Fern", "Coral"); changed = true; }
        if (GUILayout.Button($"Rock: {record.rock}")) { record.rock = Next(record.rock, "None", "Small", "Cave"); changed = true; }
        if (GUILayout.Button($"Decor: {record.decoration}")) { record.decoration = Next(record.decoration, "None", "Wood", "Castle", "Treasure"); changed = true; }
        GUILayout.EndHorizontal();
        if (changed) Commit();
    }

    void AddFish(int slotIndex)
    {
        ItemStack stack = inventory?.GetSlot(slotIndex);
        if (stack?.item == null || stack.item.category != ItemCategory.Fish || FishCount >= Capacity) return;
        ItemSO item = stack.item; int quality = stack.qualityStars; float size = Mathf.Max(1f, stack.fishSizeCm);
        if (!inventory.RemoveFromSlot(slotIndex, 1)) return;
        record.fish.Add(new AquariumFishData { itemId = item.Id, assetName = item.name, itemName = item.itemName,
            qualityStars = quality, sizeCm = size, weightKg = stack.fishWeightKg > 0f ? stack.fishWeightKg : FishMeasurement.EstimateWeightKg(item, size) });
        feedback = $"{item.itemName} dimasukkan ke Aquarium."; Commit();
    }

    void TakeFish(int index)
    {
        if (record?.fish == null || index < 0 || index >= record.fish.Count || inventory == null) return;
        AquariumFishData fish = record.fish[index];
        ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
        if (item == null) { feedback = "Asset ikan tidak ditemukan."; return; }
        if (!inventory.CanAdd(item, 1, fish.qualityStars, fish.sizeCm, fish.weightKg)) { feedback = "Inventory penuh."; return; }
        if (!inventory.Add(item, 1, fish.qualityStars, fish.sizeCm, fish.weightKg)) return;
        record.fish.RemoveAt(index); feedback = $"{item.itemName} diambil kembali."; Commit();
    }

    void Commit()
    {
        record.position = transform.position; record.eulerAngles = transform.eulerAngles;
        AquariumService.NotifyChanged(); RebuildVisuals();
    }

    void ResolveRecord() => record = AquariumService.GetOrCreate(aquariumId, aquariumSize, transform);
    void HandleServiceChanged() { ResolveRecord(); RebuildVisuals(); }

    void RebuildVisuals()
    {
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        visualFish.Clear(); visualOrigins.Clear();
        visualRoot = new GameObject("FishVisuals_Runtime").transform;
        visualRoot.SetParent(transform, false);
        ApplyDecorationVisual();
        if (record?.fish == null) return;
        int visible = Mathf.Min(maximumVisibleFish, record.fish.Count);
        for (int index = 0; index < visible; index++)
        {
            AquariumFishData fish = record.fish[index];
            ItemSO item = ItemCatalog.Resolve(fish.itemId, fish.assetName, fish.itemName);
            GameObject model = item != null && item.worldPrefab != null ? Instantiate(item.worldPrefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = $"FishVisual_{index}_{fish.itemName}"; model.transform.SetParent(visualRoot, false);
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            float scale = SizeScale(item, fish.sizeCm);
            model.transform.localScale = new Vector3(0.18f, 0.08f, 0.08f) * scale;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Vector3 origin = new(-0.38f + (index % 4) * 0.25f, -0.1f + (index % 3) * 0.12f, -0.42f);
            model.transform.localPosition = origin; visualFish.Add(model.transform); visualOrigins.Add(origin);
            Renderer renderer = model.GetComponentInChildren<Renderer>();
            if (renderer != null && item?.worldPrefab == null) renderer.material.color = Color.HSVToRGB(Mathf.Abs((item?.Id ?? fish.itemName).GetHashCode() % 1000) / 1000f, 0.65f, 0.95f);
        }
    }

    void ApplyDecorationVisual()
    {
        Transform old = transform.Find("AquariumDecor_Runtime");
        if (old != null) Destroy(old.gameObject);
        Transform decor = new GameObject("AquariumDecor_Runtime").transform;
        decor.SetParent(transform, false);
        Color water = record.background switch
        { "River" => new Color(0.18f, 0.65f, 0.48f), "Ocean" => new Color(0.06f, 0.42f, 0.85f),
          "Dark" => new Color(0.05f, 0.08f, 0.16f), _ => new Color(0.12f, 0.7f, 0.9f) };
        Renderer tank = GetComponent<Renderer>();
        if (tank != null) { tank.material.color = new Color(water.r, water.g, water.b, 0.72f); }
        Color baseColor = record.substrate == "Sand" ? new Color(0.78f, 0.65f, 0.38f) :
            record.substrate == "Black Sand" ? new Color(0.08f, 0.08f, 0.09f) : new Color(0.35f, 0.32f, 0.3f);
        DecorCube(decor, "Substrate", new Vector3(0f, -0.43f, -0.02f), new Vector3(0.9f, 0.09f, 0.82f), baseColor);
        if (record.plant != "None")
            DecorCube(decor, $"Plant_{record.plant}", new Vector3(0.3f, -0.24f, -0.18f), new Vector3(0.06f, 0.38f, 0.06f), Color.green);
        if (record.rock != "None")
            DecorCube(decor, $"Rock_{record.rock}", new Vector3(-0.28f, -0.32f, -0.16f), new Vector3(0.2f, 0.2f, 0.18f), Color.gray);
        if (record.decoration != "None")
            DecorCube(decor, $"Decoration_{record.decoration}", new Vector3(0f, -0.28f, 0.1f), new Vector3(0.16f, 0.28f, 0.16f), new Color(0.85f, 0.56f, 0.15f));
    }

    static void DecorCube(Transform parent, string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
        result.name = objectName; result.transform.SetParent(parent, false);
        result.transform.localPosition = position; result.transform.localScale = scale;
        Collider collider = result.GetComponent<Collider>(); if (collider != null) collider.enabled = false;
        Renderer renderer = result.GetComponent<Renderer>(); if (renderer != null) renderer.material.color = color;
    }

    void AnimateFish()
    {
        float speed = Time.unscaledTime < feedAnimationUntil ? 3.2f : 1f;
        for (int index = 0; index < visualFish.Count; index++)
        {
            Transform fish = visualFish[index]; if (fish == null) continue;
            Vector3 origin = visualOrigins[index]; float phase = Time.unscaledTime * speed + index * 0.83f;
            fish.localPosition = origin + new Vector3(Mathf.Sin(phase) * 0.14f, Mathf.Sin(phase * 1.7f) * 0.035f, 0f);
            Vector3 scale = fish.localScale; scale.x = Mathf.Abs(scale.x) * (Mathf.Cos(phase) >= 0f ? 1f : -1f); fish.localScale = scale;
        }
    }

    void OpenPanel() { panelOpen = true; CenterWindow(); player?.AcquireMovementLock(this); TimeManager.Instance?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this); }
    void ClosePanel() { if (!panelOpen) return; panelOpen = false; player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this); }
    void ResolvePlayer() { if (inventory == null) inventory = FindFirstObjectByType<Inventory>(); if (player == null && inventory != null) player = inventory.GetComponent<PlayerController>(); }
    void CenterWindow() { windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f); windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f); }
    string BuildStableId() => $"aquarium.{gameObject.scene.name}.{TransformPath(transform)}".ToLowerInvariant().Replace(' ', '_');
    static string TransformPath(Transform value) { string path = value.name; while (value.parent != null) { value = value.parent; path = value.name + "/" + path; } return path; }
    static string FishLabel(ItemSO item, int quality, float size) => $"{item.itemName} | {QualityLabel(quality)} | {FishMeasurement.GetSizeTier(item, size)} | {FishMeasurement.EstimateWeightKg(item, size):0.00} kg";
    static string QualityLabel(int quality) => quality <= 0 ? "Normal" : $"{Mathf.Clamp(quality, 1, 5)}★";
    static float SizeScale(ItemSO item, float size) => FishMeasurement.GetSizeTier(item, size) switch
    { FishSizeTier.Small => 0.7f, FishSizeTier.Medium => 1f, FishSizeTier.Large => 1.3f, _ => 1.6f };
    static string Next(string current, params string[] values) { int index = Array.IndexOf(values, current); return values[(index + 1 + values.Length) % values.Length]; }
}
