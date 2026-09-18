using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Jenis resource dunia beserta aturan interaksi tool yang sesuai.</summary>
public enum GatherableKind { Weed, Grass, Rock }

[Serializable]
/// <summary>Mendefinisikan kandidat item, jumlah, dan probabilitas drop resource.</summary>
public sealed class GatherableDrop
{
    public ItemSO item;
    [Min(1)] public int minimumAmount = 1;
    [Min(1)] public int maximumAmount = 1;
    [Range(0f, 1f)] public float chance = 1f;
}

[Serializable]
/// <summary>Data persisten satu resource dunia untuk proses save dan load.</summary>
public sealed class GatherableSaveData
{
    public string id;
    public bool depleted;
    public int currentDurability;
    public int respawnDay;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>
/// Mengelola weed, rumput, dan batu yang dapat dipanen, termasuk durability,
/// drop item, respawn berbasis hari, highlight target, dan persistensi.
/// </summary>
public sealed class WorldGatherable : MonoBehaviour
{
    static readonly List<WorldGatherable> Registry = new();
    public static IReadOnlyList<WorldGatherable> Active => Registry;

    [Header("Identity")]
    [SerializeField] string gatherableId = "gatherable-01";
    [SerializeField] GatherableKind kind = GatherableKind.Weed;

    [Header("Allowed Actions")]
    [SerializeField] bool canPullByHand = true;
    [SerializeField] bool canCutWithSickle = true;
    [SerializeField] bool canBreakWithHammer;
    [SerializeField, Min(1)] int minimumHammerLevel = 1;

    [Header("Durability")]
    [SerializeField, Min(1)] int maximumDurability = 1;
    [SerializeField, Min(1)] int hammerDamagePerHit = 1;

    [Header("Drops")]
    [SerializeField] List<GatherableDrop> drops = new();

    [Header("Respawn")]
    [SerializeField] bool canRespawn = true;
    [SerializeField, Min(1)] int minimumRespawnDays = 2;
    [SerializeField, Min(1)] int maximumRespawnDays = 4;

    [Header("Visual / Feedback Slots")]
    [SerializeField] Transform visualRoot;
    [SerializeField] Renderer[] targetRenderers;
    [SerializeField] Material[] crackMaterials;
    [SerializeField] AudioClip pullSound;
    [SerializeField] AudioClip cutSound;
    [SerializeField] AudioClip hammerSound;
    [SerializeField] ParticleSystem leafParticles;
    [SerializeField] ParticleSystem rockParticles;
    [SerializeField, Min(0f)] float promptHeight = 1.1f;

    Collider interactionCollider;
    LineRenderer highlight;
    int currentDurability;
    int respawnDay;
    bool depleted;

    public string Id => gatherableId;
    public GatherableKind Kind => kind;
    public bool IsAvailable => !depleted;
    public bool CanPull => !depleted && canPullByHand;
    public bool CanSickle => !depleted && canCutWithSickle;
    public bool CanHammer => !depleted && canBreakWithHammer;
    public int Durability => currentDurability;
    public int MinimumHammerLevel => minimumHammerLevel;
    public float PromptHeight => promptHeight;

    /// <summary>
    /// Titik permukaan resource yang paling dekat dengan player. Interaksi memakai titik ini
    /// supaya prefab besar atau prefab dengan pivot yang tidak berada di tengah tetap dapat
    /// ditargetkan secara konsisten.
    /// </summary>
    public Vector3 GetInteractionPoint(Vector3 observerPosition)
    {
        if (interactionCollider != null && interactionCollider.enabled)
            return interactionCollider.ClosestPoint(observerPosition);
        return transform.position;
    }

    public void ConfigureRuntimeGrass(string id, ItemSO fodder)
    {
        gatherableId = id;
        kind = GatherableKind.Grass;
        canPullByHand = false;
        canCutWithSickle = true;
        canBreakWithHammer = false;
        maximumDurability = currentDurability = 1;
        canRespawn = true;
        minimumRespawnDays = 2;
        maximumRespawnDays = 4;
        drops.Clear();
        if (fodder != null)
            drops.Add(new GatherableDrop { item = fodder, minimumAmount = 1, maximumAmount = 1, chance = 1f });
    }

    void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (visualRoot == null) visualRoot = transform;
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        if (string.IsNullOrWhiteSpace(gatherableId) || gatherableId == "gatherable-01")
        {
            Vector3 position = transform.position;
            gatherableId = FormattableString.Invariant(
                $"{gameObject.scene.name}:{kind}:{position.x:0.###}:{position.y:0.###}:{position.z:0.###}");
        }
        currentDurability = Mathf.Max(1, maximumDurability);
    }

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
        TimeManager.OnDay += HandleDayChanged;
    }

    void OnDisable()
    {
        Registry.Remove(this);
        TimeManager.OnDay -= HandleDayChanged;
    }

    /// <summary>Mengubah highlight target resource.</summary>
    public void SetHighlighted(bool value)
    {
        if (!IsAvailable && value) return;
        EnsureHighlight();
        if (highlight != null) highlight.gameObject.SetActive(value);
    }

    /// <summary>Mencoba mencabut resource dengan tangan sesuai aturan jenisnya.</summary>
    public bool Pull(PlayerGatheringTool player)
    {
        if (!CanPull) return false;
        PlayFeedback(pullSound, leafParticles);
        Deplete(player, true);
        return true;
    }

    /// <summary>Mencoba memotong resource menggunakan sickle.</summary>
    public bool Cut(PlayerGatheringTool player)
    {
        if (!CanSickle) return false;
        PlayFeedback(cutSound, leafParticles);
        Deplete(player, false);
        ItemSO fodder = AnimalCareCatalog.Load()?.fodder;
        if (kind == GatherableKind.Grass && fodder != null && !drops.Exists(drop => drop != null && drop.item == fodder))
            PlacedWorldItem.Spawn(fodder, 1, transform.position + Vector3.up * 0.35f, Quaternion.identity, false);
        return true;
    }

    /// <summary>Satu patch rumput habis dimakan; tidak membuat pickup bagi player.</summary>
    public bool TryGraze()
    {
        if (kind != GatherableKind.Grass || depleted) return false;
        Deplete(null, false, false);
        return true;
    }

    /// <summary>Menerapkan damage hammer jika jenis dan level tool valid.</summary>
    public bool Hammer(PlayerGatheringTool player, int hammerLevel)
    {
        if (!CanHammer || hammerLevel < minimumHammerLevel) return false;
        currentDurability = Mathf.Max(0, currentDurability - Mathf.Max(1, hammerDamagePerHit));
        PlayFeedback(hammerSound, rockParticles);
        ApplyCrackVisual();
        if (currentDurability <= 0) Deplete(player, false);
        return true;
    }

    void Deplete(PlayerGatheringTool player, bool offerFirstDropToHands, bool spawnDrops = true)
    {
        depleted = true;
        SetHighlighted(false);
        SetVisualActive(false);
        if (interactionCollider != null) interactionCollider.enabled = false;
        int today = TimeManager.Instance != null ? TimeManager.Instance.day : 1;
        respawnDay = canRespawn ? today + UnityEngine.Random.Range(minimumRespawnDays, Mathf.Max(minimumRespawnDays, maximumRespawnDays) + 1) : int.MaxValue;

        bool handsUsed = false;
        if (!spawnDrops) return;
        for (int i = 0; i < drops.Count; i++)
        {
            GatherableDrop drop = drops[i];
            if (drop == null || drop.item == null || UnityEngine.Random.value > drop.chance) continue;
            int amount = UnityEngine.Random.Range(Mathf.Max(1, drop.minimumAmount), Mathf.Max(drop.minimumAmount, drop.maximumAmount) + 1);
            if (offerFirstDropToHands && !handsUsed && player != null && player.TryCarry(drop.item, amount))
                handsUsed = true;
            else if (drop.item == AnimalCareCatalog.Load()?.fodder)
                PlacedWorldItem.Spawn(drop.item, amount, transform.position + Vector3.up * 0.35f, Quaternion.identity, false);
            else
                SpawnLoosePickup(drop.item, amount, transform.position + Vector3.up * 0.35f);
        }
    }

    /// <summary>Membuat pickup fisik sementara untuk drop resource.</summary>
    public static void SpawnLoosePickup(ItemSO item, int amount, Vector3 position,
        int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (item == null || amount <= 0) return;
        // Scatter offset can land uphill from the source tree. Keep the drop above terrain.
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain.terrainData == null) continue;
            Vector3 local = position - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;
            position.y = Mathf.Max(position.y, terrain.SampleHeight(position) + terrain.transform.position.y + 0.25f);
        }
        GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        drop.name = $"Pickup_{item.itemName}";
        drop.transform.position = position;
        drop.transform.localScale = Vector3.one * 0.38f;
        WorldItemPickup pickup = drop.AddComponent<WorldItemPickup>();
        pickup.Initialize(item, amount, false, qualityStars, fishSizeCm);
    }

    void HandleDayChanged()
    {
        if (!depleted || !canRespawn || TimeManager.Instance == null || TimeManager.Instance.day < respawnDay) return;
        depleted = false;
        currentDurability = Mathf.Max(1, maximumDurability);
        SetVisualActive(true);
        if (interactionCollider != null) interactionCollider.enabled = true;
        ApplyCrackVisual();
    }

    void ApplyCrackVisual()
    {
        if (crackMaterials == null || crackMaterials.Length == 0 || targetRenderers == null) return;
        float damage = 1f - (float)currentDurability / Mathf.Max(1, maximumDurability);
        int index = Mathf.Clamp(Mathf.FloorToInt(damage * crackMaterials.Length), 0, crackMaterials.Length - 1);
        Material material = crackMaterials[index];
        if (material == null) return;
        for (int i = 0; i < targetRenderers.Length; i++)
            if (targetRenderers[i] != null) targetRenderers[i].sharedMaterial = material;
    }

    void SetVisualActive(bool value)
    {
        if (visualRoot != null && visualRoot != transform)
        {
            visualRoot.gameObject.SetActive(value);
            return;
        }

        if (targetRenderers == null) return;
        for (int i = 0; i < targetRenderers.Length; i++)
            if (targetRenderers[i] != null) targetRenderers[i].enabled = value;
    }

    void PlayFeedback(AudioClip clip, ParticleSystem particles)
    {
        if (clip != null) GameAudio.PlayClipAtPoint(clip, transform.position, GameAudioBus.Main);
        if (particles != null) particles.Play();
    }

    void EnsureHighlight()
    {
        if (highlight != null) return;
        GameObject indicator = new("GatherHighlight");
        indicator.transform.SetParent(transform, false);
        highlight = indicator.AddComponent<LineRenderer>();
        highlight.loop = true;
        highlight.positionCount = 4;
        highlight.useWorldSpace = true;
        highlight.startWidth = highlight.endWidth = 0.055f;
        highlight.startColor = highlight.endColor = new Color(1f, 0.86f, 0.15f, 1f);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) highlight.material = new Material(shader);
        Bounds bounds = interactionCollider != null ? interactionCollider.bounds : new Bounds(transform.position, Vector3.one);
        float y = bounds.min.y + 0.06f;
        highlight.SetPosition(0, new Vector3(bounds.min.x, y, bounds.min.z));
        highlight.SetPosition(1, new Vector3(bounds.max.x, y, bounds.min.z));
        highlight.SetPosition(2, new Vector3(bounds.max.x, y, bounds.max.z));
        highlight.SetPosition(3, new Vector3(bounds.min.x, y, bounds.max.z));
        indicator.SetActive(false);
    }

    public GatherableSaveData Capture() => new() { id = gatherableId, depleted = depleted, currentDurability = currentDurability, respawnDay = respawnDay };

    /// <summary>Mengembalikan state depletion, durability, dan respawn resource.</summary>
    public void Restore(GatherableSaveData data)
    {
        if (data == null) return;
        depleted = data.depleted;
        currentDurability = Mathf.Clamp(data.currentDurability, 0, Mathf.Max(1, maximumDurability));
        respawnDay = data.respawnDay;
        SetVisualActive(!depleted);
        if (interactionCollider != null) interactionCollider.enabled = !depleted;
        ApplyCrackVisual();
    }

    public static List<GatherableSaveData> CaptureAll()
    {
        List<GatherableSaveData> data = new(Registry.Count);
        for (int i = 0; i < Registry.Count; i++) if (Registry[i] != null) data.Add(Registry[i].Capture());
        return data;
    }

    /// <summary>Memulihkan seluruh gatherable terdaftar dari save.</summary>
    public static void RestoreAll(List<GatherableSaveData> data)
    {
        if (data == null) return;
        Dictionary<string, GatherableSaveData> map = new();
        for (int i = 0; i < data.Count; i++) if (data[i] != null && !string.IsNullOrEmpty(data[i].id)) map[data[i].id] = data[i];
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && map.TryGetValue(Registry[i].gatherableId, out GatherableSaveData saved)) Registry[i].Restore(saved);
    }
}
