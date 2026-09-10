using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>
/// Trigger pickup yang memindahkan stack world ke inventory, menampilkan prompt lokal,
/// dan membuat outline ringan yang mengikuti bentuk mesh item.
/// </summary>
public sealed class WorldItemPickup : MonoBehaviour
{
    const int HintCircleSegments = 32;
    const int HintArcSegments = 10;

    [SerializeField] ItemSO item;
    [SerializeField, Min(1)] int amount = 1;
    public int QualityStars { get; set; }
    [SerializeField] bool autoPickup;
    [SerializeField] KeyCode pickupKey = KeyCode.E;
    [SerializeField, Min(0.1f)] float pickupRadius = 2.5f;
    [SerializeField, Min(0f)] float promptHeight = 0.75f;

    [Header("Subtle Pickup Hint")]
    [Tooltip("Menampilkan penanda halus di bawah item yang dapat diambil.")]
    [SerializeField] bool showPickupHint = true;
    [Tooltip("Opsional. Isi dengan prefab visual custom; jika kosong, sistem membuat ring tipis otomatis.")]
    [SerializeField] GameObject pickupHintPrefab;
    [SerializeField] Color idleHintColor = new(0.72f, 0.95f, 0.9f, 0.52f);
    [SerializeField] Color nearbyHintColor = new(0.82f, 1f, 0.92f, 0.9f);
    [SerializeField, Min(0.05f)] float hintRadius = 0.48f;
    [SerializeField, Min(0.005f)] float hintWidth = 0.045f;
    [SerializeField, Range(0f, 0.15f)] float hintPulseAmount = 0.055f;
    [SerializeField, Min(0f)] float hintPulseSpeed = 1.25f;
    [Tooltip("Kecepatan arc dekoratif mengitari item. Arc bergerak lebih cepat saat player mendekat.")]
    [SerializeField, Min(0f)] float accentRotationSpeed = 24f;
    [Tooltip("Ketebalan outline yang mengikuti bentuk mesh item.")]
    [SerializeField, Range(0.002f, 0.08f)] float shapeOutlineWidth = 0.025f;

    Inventory nearbyInventory;
    Inventory playerCandidate;
    static readonly List<WorldItemPickup> Active = new();
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
    GameObject destroyTarget;
    Transform hintRoot;
    Transform hintAccentRoot;
    LineRenderer hintLine;
    LineRenderer hintAccentLine;
    readonly List<Renderer> shapeOutlineRenderers = new();
    readonly List<GameObject> shapeOutlineObjects = new();
    MaterialPropertyBlock outlineProperties;

    static Material sharedHintMaterial;
    static Material sharedShapeOutlineMaterial;
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        // MaterialPropertyBlock membungkus resource native Unity sehingga wajib dibuat
        // setelah MonoBehaviour selesai dikonstruksi, bukan pada field initializer.
        outlineProperties = new MaterialPropertyBlock();
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        EnsurePickupHint();
    }

    /// <summary>Mengisi data stack untuk pickup yang dibuat saat runtime.</summary>
    public void Initialize(ItemSO itemData, int itemAmount = 1, bool pickUpAutomatically = false)
    {
        item = itemData;
        amount = Mathf.Max(1, itemAmount);
        autoPickup = pickUpAutomatically;
    }

    /// <summary>Menentukan root yang ikut dihapus ketika trigger pickup berada pada child.</summary>
    public void SetDestroyTarget(GameObject target)
    {
        destroyTarget = target;
        RebuildPickupHint();
    }

    void Update()
    {
        if (playerCandidate == null) playerCandidate = FindFirstObjectByType<Inventory>();
        Transform target = destroyTarget != null ? destroyTarget.transform : transform;
        nearbyInventory = playerCandidate != null && PlayerInteractionTarget.ContainsPickup(playerCandidate.transform, target, pickupRadius)
            ? playerCandidate : null;
        UpdatePickupHint();

        if (nearbyInventory == null)
            return;
        float ownDistance = (transform.position - nearbyInventory.transform.position).sqrMagnitude;
        foreach (WorldItemPickup other in Active)
        {
            if (other == null || other == this || other.amount <= 0 || other.item == null) continue;
            Transform otherTarget = other.destroyTarget != null ? other.destroyTarget.transform : other.transform;
            if (!PlayerInteractionTarget.ContainsPickup(nearbyInventory.transform, otherTarget, other.pickupRadius)) continue;
            float otherDistance = (other.transform.position - nearbyInventory.transform.position).sqrMagnitude;
            if (otherDistance < ownDistance || (Mathf.Approximately(otherDistance, ownDistance) && other.GetInstanceID() < GetInstanceID())) return;
        }
        if (autoPickup) { TryPickup(); return; }

        float distance = Vector3.Distance(nearbyInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(
            this,
            transform,
            $"{pickupKey} - Ambil {item?.itemName ?? "item"}",
            distance,
            promptHeight
        );

        if (PlayerInteractionTarget.PressPickup(nearbyInventory.transform, target, pickupKey, pickupRadius))
            TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null)
            return;

        playerCandidate = inventory;
    }

    void OnTriggerExit(Collider other)
    {
        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == nearbyInventory)
            nearbyInventory = null;
    }

    // Bisa dipanggil tombol interaksi mobile/raycast.
    /// <summary>Memindahkan seluruh stack ke inventory lalu menghapus representasi world jika berhasil.</summary>
    public bool TryPickup()
    {
        if (nearbyInventory == null || item == null || amount <= 0)
            return false;
        if (!PlayerInteractionTarget.ContainsPickup(nearbyInventory.transform, destroyTarget != null ? destroyTarget.transform : transform, pickupRadius)) return false;

        if (!nearbyInventory.Add(item, amount, QualityStars))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh");
            return false;
        }

        int collectedAmount = amount;
        SaveLoadFeedback.Instance?.ShowMessage($"Mengambil {item.itemName} x{amount}");
        QuestEventHub.Publish(QuestObjectiveType.Collect, item.name, collectedAmount, item);
        amount = 0;
        if (destroyTarget != null) destroyTarget.SetActive(false);
        Destroy(destroyTarget != null ? destroyTarget : gameObject);
        return true;
    }

    /// <summary>
    /// Membuat ring sederhana jika artist belum memasang prefab hint custom.
    /// Visual dibuat sebagai child agar selalu mengikuti item tanpa mengubah fisika item.
    /// </summary>
    void EnsurePickupHint()
    {
        if (!showPickupHint || hintRoot != null)
            return;

        Transform visualOwner = destroyTarget != null ? destroyTarget.transform : transform;
        Bounds visualBounds = FindVisualBounds(visualOwner);

        // Pilihan utama: outline mengikuti silhouette mesh. Ring hanya dipakai sebagai fallback.
        if (TryBuildShapeOutline(visualOwner))
        {
            GameObject shapeRoot = new("PickupHint_ShapeOutline");
            hintRoot = shapeRoot.transform;
            hintRoot.SetParent(visualOwner, false);
            ApplyHintColor();
            return;
        }

        GameObject rootObject = new("PickupHint_Subtle");
        hintRoot = rootObject.transform;
        hintRoot.SetParent(visualOwner, false);
        hintRoot.localPosition = visualOwner.InverseTransformPoint(
            new Vector3(visualBounds.center.x, visualBounds.min.y + 0.065f, visualBounds.center.z)
        );

        if (pickupHintPrefab != null)
        {
            Instantiate(pickupHintPrefab, hintRoot, false);
            return;
        }

        hintLine = rootObject.AddComponent<LineRenderer>();
        hintLine.loop = true;
        hintLine.useWorldSpace = false;
        hintLine.positionCount = HintCircleSegments;
        hintLine.widthMultiplier = hintWidth;
        hintLine.numCornerVertices = 2;
        hintLine.numCapVertices = 2;
        // View alignment menjaga ketebalan ring tetap terbaca dari kamera top-down.
        hintLine.alignment = LineAlignment.View;
        hintLine.textureMode = LineTextureMode.Stretch;
        hintLine.sharedMaterial = GetSharedHintMaterial();
        hintLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hintLine.receiveShadows = false;
        hintLine.sortingOrder = 20;

        GameObject accentObject = new("PickupHint_NearbyAccent");
        hintAccentRoot = accentObject.transform;
        hintAccentRoot.SetParent(hintRoot, false);
        hintAccentLine = accentObject.AddComponent<LineRenderer>();
        hintAccentLine.loop = false;
        hintAccentLine.useWorldSpace = false;
        hintAccentLine.positionCount = HintArcSegments;
        hintAccentLine.widthMultiplier = hintWidth * 0.72f;
        hintAccentLine.numCornerVertices = 4;
        hintAccentLine.numCapVertices = 4;
        hintAccentLine.alignment = LineAlignment.View;
        hintAccentLine.textureMode = LineTextureMode.Stretch;
        hintAccentLine.sharedMaterial = GetSharedHintMaterial();
        hintAccentLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hintAccentLine.receiveShadows = false;
        hintAccentLine.sortingOrder = 21;

        // Ring berada pada bidang XZ dan hanya menjadi petunjuk visual, bukan collider baru.
        for (int index = 0; index < HintCircleSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / HintCircleSegments;
            hintLine.SetPosition(index, new Vector3(Mathf.Cos(angle) * hintRadius, 0f, Mathf.Sin(angle) * hintRadius));
        }

        // Arc pendek memberi gerakan yang lebih menarik tanpa membuat efek loot beam.
        float accentRadius = hintRadius + 0.085f;
        float arcLength = Mathf.PI * 0.56f;
        for (int index = 0; index < HintArcSegments; index++)
        {
            float angle = index * arcLength / (HintArcSegments - 1);
            hintAccentLine.SetPosition(index, new Vector3(Mathf.Cos(angle) * accentRadius, 0f, Mathf.Sin(angle) * accentRadius));
        }

        ApplyHintColor();
    }

    void UpdatePickupHint()
    {
        if (!showPickupHint)
            return;

        EnsurePickupHint();
        if (hintRoot == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * hintPulseSpeed) * hintPulseAmount;
        // Outline mesh tidak ikut dibesarkan supaya bentuk dan posisi item tetap presisi.
        if (shapeOutlineRenderers.Count == 0)
            hintRoot.localScale = Vector3.one * pulse;

        if (hintAccentRoot != null)
        {
            float proximityMultiplier = nearbyInventory != null ? 1.8f : 1f;
            hintAccentRoot.Rotate(0f, accentRotationSpeed * proximityMultiplier * Time.unscaledDeltaTime, 0f, Space.Self);
        }

        ApplyHintColor();
    }

    void ApplyHintColor()
    {
        if (shapeOutlineRenderers.Count > 0)
        {
            // Pengaman untuk domain reload atau object lama yang sudah berada di scene.
            outlineProperties ??= new MaterialPropertyBlock();
            Color shapeColor = nearbyInventory != null ? nearbyHintColor : idleHintColor;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * hintPulseSpeed) * hintPulseAmount;
            float proximityWidth = nearbyInventory != null ? 1.35f : 1f;
            outlineProperties.Clear();
            outlineProperties.SetColor(OutlineColorId, shapeColor);
            outlineProperties.SetFloat(OutlineWidthId, shapeOutlineWidth * proximityWidth * pulse);

            foreach (Renderer outlineRenderer in shapeOutlineRenderers)
            {
                if (outlineRenderer != null)
                    outlineRenderer.SetPropertyBlock(outlineProperties);
            }

            return;
        }

        if (hintLine == null)
            return;

        Color color = nearbyInventory != null ? nearbyHintColor : idleHintColor;
        hintLine.startColor = color;
        hintLine.endColor = color;

        // Arc tetap samar dari jauh dan menjadi jelas saat item masuk jangkauan pickup.
        if (hintAccentLine != null)
        {
            bool isNearby = nearbyInventory != null;
            float accentAlpha = isNearby ? 0.95f : 0.62f;
            Color accentColor = new(color.r, color.g, color.b, accentAlpha);
            hintAccentLine.startColor = accentColor;
            hintAccentLine.endColor = accentColor;
        }
    }

    void RebuildPickupHint()
    {
        foreach (GameObject outlineObject in shapeOutlineObjects)
        {
            if (outlineObject != null)
                Destroy(outlineObject);
        }

        shapeOutlineObjects.Clear();
        shapeOutlineRenderers.Clear();

        if (hintRoot != null)
            Destroy(hintRoot.gameObject);

        hintRoot = null;
        hintAccentRoot = null;
        hintLine = null;
        hintAccentLine = null;
        EnsurePickupHint();
    }

    /// <summary>
    /// Menyalin mesh renderer sebagai kulit luar tipis. Material outline hanya menggambar
    /// sisi belakang mesh yang diperbesar sehingga hasilnya mengikuti bentuk objek.
    /// </summary>
    bool TryBuildShapeOutline(Transform visualOwner)
    {
        Material outlineMaterial = GetSharedShapeOutlineMaterial();
        if (outlineMaterial == null)
            return false;

        MeshRenderer[] sourceRenderers = visualOwner.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer.gameObject.name.EndsWith("_PickupOutline"))
                continue;

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            GameObject outlineObject = new($"{sourceRenderer.gameObject.name}_PickupOutline");
            outlineObject.layer = sourceRenderer.gameObject.layer;
            Transform outlineTransform = outlineObject.transform;
            outlineTransform.SetParent(sourceRenderer.transform, false);

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
            Material[] outlineMaterials = new Material[materialCount];
            for (int index = 0; index < materialCount; index++)
                outlineMaterials[index] = outlineMaterial;

            outlineRenderer.sharedMaterials = outlineMaterials;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            shapeOutlineObjects.Add(outlineObject);
            shapeOutlineRenderers.Add(outlineRenderer);
        }

        return shapeOutlineRenderers.Count > 0;
    }

    Bounds FindVisualBounds(Transform visualOwner)
    {
        Renderer visualRenderer = visualOwner.GetComponentInChildren<Renderer>();
        if (visualRenderer != null)
            return visualRenderer.bounds;

        return GetComponent<Collider>().bounds;
    }

    static Material GetSharedHintMaterial()
    {
        if (sharedHintMaterial != null)
            return sharedHintMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        sharedHintMaterial = new Material(shader)
        {
            name = "Pickup Hint Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };
        return sharedHintMaterial;
    }

    static Material GetSharedShapeOutlineMaterial()
    {
        if (sharedShapeOutlineMaterial != null)
            return sharedShapeOutlineMaterial;

        Shader shader = Shader.Find("NatureParadise/PickupShapeOutline");
        if (shader == null)
            return null;

        sharedShapeOutlineMaterial = new Material(shader)
        {
            name = "Pickup Shape Outline Runtime Material",
            hideFlags = HideFlags.HideAndDontSave
        };
        return sharedShapeOutlineMaterial;
    }
}
