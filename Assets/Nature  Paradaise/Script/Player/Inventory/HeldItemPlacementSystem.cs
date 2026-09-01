using UnityEngine;

/// <summary>
/// Menampilkan item hotbar terpilih di tangan dan menyediakan dua aksi terpisah:
/// Drop (fisika) serta Place (stabil dengan preview permukaan).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public sealed class HeldItemPlacementSystem : MonoBehaviour
{
    [Header("Input PC")]
    [SerializeField] KeyCode dropKey = KeyCode.G;
    [SerializeField] KeyCode placeKey = KeyCode.P;
    [Tooltip("Tahan tombol ini bersama Drop/Place untuk mengeluarkan seluruh stack.")]
    [SerializeField] KeyCode wholeStackModifier = KeyCode.LeftShift;

    [Header("Placement")]
    [SerializeField, Min(0.5f)] float placementDistance = 1.6f;
    [SerializeField, Min(0.5f)] float rayHeight = 2.5f;
    [SerializeField, Range(0f, 1f)] float minimumSurfaceUpDot = 0.65f;
    [SerializeField] LayerMask placementMask = ~0;
    [SerializeField, Min(0.02f)] float previewRefreshInterval = 0.05f;
    [Tooltip("Jika aktif, permukaan harus memiliki ItemPlacementSurface. Matikan agar terrain/lantai datar biasa tetap valid.")]
    [SerializeField] bool requireSurfaceMarker;
    [Tooltip("Ruang tambahan di sekitar model ketika memeriksa tabrakan placement.")]
    [SerializeField, Min(0f)] float clearancePadding = 0.025f;

    [Header("Drop Physics")]
    [Tooltip("Jarak awal item dari pusat player ketika dijatuhkan.")]
    [SerializeField, Min(0.25f)] float dropForwardOffset = 1.05f;
    [Tooltip("Ketinggian awal item agar dapat jatuh secara natural.")]
    [SerializeField, Min(0f)] float dropHeight = 0.85f;
    [Tooltip("Dorongan horizontal ringan ketika item dilepas.")]
    [SerializeField, Min(0f)] float dropForwardVelocity = 0.8f;

    [Header("Held Visual Slot")]
    [Tooltip("Anchor tangan opsional. Jika kosong dibuat otomatis pada Player.")]
    [SerializeField] Transform handAnchor;
    [SerializeField] Color validPreviewColor = new(0.2f, 1f, 0.35f, 0.55f);
    [SerializeField] Color invalidPreviewColor = new(1f, 0.18f, 0.12f, 0.55f);

    Inventory inventory;
    InventoryHotbarUI hotbarUI;
    PlayerController movement;
    GameObject heldVisual;
    GameObject previewVisual;
    Material previewMaterial;
    ItemSO shownItem;
    Vector3 placementPoint;
    Quaternion placementRotation = Quaternion.identity;
    bool placementValid;
    float nextPreviewRefresh;
    readonly Collider[] placementOverlapBuffer = new Collider[24];

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        hotbarUI = GetComponent<InventoryHotbarUI>();
        movement = GetComponent<PlayerController>();
        EnsureHandAnchor();
    }

    void OnEnable()
    {
        if (inventory != null) inventory.OnInventoryChanged += RefreshHeldVisual;
        if (hotbarUI != null) hotbarUI.SelectionChanged += HandleSlotChanged;
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= RefreshHeldVisual;
        if (hotbarUI != null) hotbarUI.SelectionChanged -= HandleSlotChanged;
    }

    void OnDestroy()
    {
        DestroyPreview();
    }

    void Start() => RefreshHeldVisual();

    void Update()
    {
        if (movement != null && movement.IsMovementLocked) return;
        ItemStack selected = GetSelectedStack();
        bool canDrop = CanDrop(selected);
        bool canPlace = CanPlace(selected);
        if (previewVisual != null) previewVisual.SetActive(canPlace);
        if (!canDrop && !canPlace) return;

        if (canPlace && Time.unscaledTime >= nextPreviewRefresh)
        {
            nextPreviewRefresh = Time.unscaledTime + previewRefreshInterval;
            RefreshPlacementPreview(selected.item);
        }

        // WorldInteractionPrompt membutuhkan request setiap frame. Validasi raycast tetap
        // memakai interval terpisah agar prompt stabil tanpa menambah beban physics.
        RequestActionPrompt(selected.item, canDrop, canPlace);

        if (canDrop && Input.GetKeyDown(dropKey)) DropSelected(Input.GetKey(wholeStackModifier));
        else if (canPlace && Input.GetKeyDown(placeKey)) PlaceSelected(Input.GetKey(wholeStackModifier));
    }

    /// <summary>Dipakai UI mobile untuk menjatuhkan satu item atau seluruh stack.</summary>
    public void RequestDrop(bool wholeStack = false) => DropSelected(wholeStack);

    /// <summary>Dipakai UI mobile untuk meletakkan satu item atau seluruh stack.</summary>
    public void RequestPlace(bool wholeStack = false) => PlaceSelected(wholeStack);

    void DropSelected(bool wholeStack)
    {
        ItemStack stack = GetSelectedStack();
        if (!CanDrop(stack)) return;
        int amount = wholeStack ? stack.count : 1;
        ItemSO item = stack.item;
        if (!inventory.RemoveFromSlot(hotbarUI.SelectedIndex, amount)) return;

        Vector3 facing = GetFacing();
        Vector3 position = transform.position + facing * dropForwardOffset + Vector3.up * dropHeight;
        PlacedWorldItem dropped = PlacedWorldItem.Spawn(item, amount, position, Quaternion.identity, true);
        if (dropped == null)
        {
            // Inventory dikembalikan jika pembuatan object gagal agar item tidak hilang.
            inventory.Add(item, amount);
            SaveLoadFeedback.Instance?.ShowMessage("Drop item gagal");
            return;
        }

        Rigidbody body = dropped.GetComponent<Rigidbody>();
        if (body != null)
            body.linearVelocity = facing * dropForwardVelocity;

        SaveLoadFeedback.Instance?.ShowMessage($"Drop {item.itemName} x{amount}");
    }

    void PlaceSelected(bool wholeStack)
    {
        ItemStack stack = GetSelectedStack();
        if (!CanPlace(stack)) return;
        if (!placementValid)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Permukaan tidak valid untuk menaruh item");
            return;
        }

        int amount = wholeStack ? stack.count : 1;
        ItemSO item = stack.item;
        if (!inventory.RemoveFromSlot(hotbarUI.SelectedIndex, amount)) return;
        PlacedWorldItem placed = PlacedWorldItem.Spawn(item, amount, placementPoint, placementRotation, false);
        if (placed == null)
        {
            inventory.Add(item, amount);
            SaveLoadFeedback.Instance?.ShowMessage("Place item gagal");
            return;
        }

        SaveLoadFeedback.Instance?.ShowMessage($"Place {item.itemName} x{amount}");
    }

    void RefreshPlacementPreview(ItemSO item)
    {
        EnsurePreview(item);
        Vector3 desired = transform.position + GetFacing() * placementDistance;
        Vector3 origin = desired + Vector3.up * rayHeight;
        placementValid = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, placementMask, QueryTriggerInteraction.Ignore);
        if (placementValid)
        {
            ItemPlacementSurface surface = hit.collider.GetComponentInParent<ItemPlacementSurface>();
            placementValid = hit.normal.y >= minimumSurfaceUpDot &&
                             (!requireSurfaceMarker || surface != null) &&
                             (surface == null || surface.AllowItemPlacement) &&
                             hit.collider.GetComponentInParent<PlacedWorldItem>() == null &&
                             hit.collider.GetComponentInParent<PlayerController>() == null;
            placementPoint = hit.point + Vector3.up * 0.02f;
            placementRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            placementPoint = desired;
            placementRotation = Quaternion.identity;
        }

        previewVisual.transform.SetPositionAndRotation(placementPoint, placementRotation);
        if (placementValid)
            placementValid = HasPlacementClearance(hit.collider);

        previewMaterial.color = placementValid ? validPreviewColor : invalidPreviewColor;
    }

    void RequestActionPrompt(ItemSO item, bool canDrop, bool canPlace)
    {
        if (item == null)
            return;

        string actions;
        if (canDrop && canPlace)
            actions = $"{placeKey}: Place  |  {dropKey}: Drop";
        else if (canPlace)
            actions = $"{placeKey}: Place";
        else
            actions = $"{dropKey}: Drop";

        Transform anchor = canPlace && previewVisual != null ? previewVisual.transform : heldVisual?.transform;
        if (anchor != null)
        {
            // Hint held-item berprioritas rendah. Prompt kasur, TV, pickup, atau tanaman
            // yang sedang didekati boleh menggantikannya tanpa saling berkedip.
            WorldInteractionPrompt.Request(this, anchor, $"{item.itemName}  -  {actions}  |  Shift: semua", 50f, 0.65f);
        }
    }

    void HandleSlotChanged(int _) => RefreshHeldVisual();

    void RefreshHeldVisual()
    {
        ItemStack stack = GetSelectedStack();
        ItemSO item = HasHeldWorldAction(stack) ? stack.item : null;
        if (shownItem == item) return;
        shownItem = item;
        if (heldVisual != null) Destroy(heldVisual);
        DestroyPreview();
        heldVisual = null;
        if (item == null) return;

        heldVisual = CreateVisual(item, handAnchor, $"Held_{item.itemName}");
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;
        if (item.canPlaceInWorld)
            EnsurePreview(item);
    }

    void EnsurePreview(ItemSO item)
    {
        if (previewVisual != null) return;

        previewVisual = new GameObject();
        previewVisual.name = $"PlacementPreview_{item.itemName}";

        // Preview memakai model item asli supaya footprint dan orientasinya mudah dinilai.
        GameObject visual = CreateVisual(item, previewVisual.transform, $"PreviewMesh_{item.itemName}");
        visual.transform.localPosition = Vector3.up * 0.35f;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        previewMaterial = new Material(shader);
        ConfigureTransparentPreviewMaterial(previewMaterial);

        foreach (Renderer itemRenderer in previewVisual.GetComponentsInChildren<Renderer>(true))
        {
            int materialCount = Mathf.Max(1, itemRenderer.sharedMaterials.Length);
            Material[] ghostMaterials = new Material[materialCount];
            for (int index = 0; index < materialCount; index++)
                ghostMaterials[index] = previewMaterial;
            itemRenderer.sharedMaterials = ghostMaterials;
            itemRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            itemRenderer.receiveShadows = false;
        }
    }

    /// <summary>
    /// Memastikan bounds visual preview tidak bertabrakan dengan bangunan, item lain,
    /// atau obstacle. Collider permukaan yang terkena raycast sengaja diabaikan.
    /// </summary>
    bool HasPlacementClearance(Collider surfaceCollider)
    {
        Renderer[] renderers = previewVisual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        Vector3 extents = bounds.extents + Vector3.one * clearancePadding;
        int hitCount = Physics.OverlapBoxNonAlloc(
            bounds.center,
            extents,
            placementOverlapBuffer,
            Quaternion.identity,
            placementMask,
            QueryTriggerInteraction.Ignore
        );

        ItemPlacementSurface acceptedSurface = surfaceCollider != null
            ? surfaceCollider.GetComponentInParent<ItemPlacementSurface>()
            : null;

        for (int index = 0; index < hitCount; index++)
        {
            Collider obstacle = placementOverlapBuffer[index];
            placementOverlapBuffer[index] = null;
            if (obstacle == null || obstacle == surfaceCollider)
                continue;
            if (acceptedSurface != null && obstacle.GetComponentInParent<ItemPlacementSurface>() == acceptedSurface)
                continue;
            if (obstacle.transform.IsChildOf(transform))
                continue;
            // Collider lantai tetangga di bawah model bukan obstacle. Ini penting untuk
            // permukaan yang tersusun dari beberapa tile/collider terpisah.
            if (obstacle.bounds.max.y <= bounds.min.y + clearancePadding * 2f + 0.02f)
                continue;
            return false;
        }

        return true;
    }

    static void ConfigureTransparentPreviewMaterial(Material material)
    {
        if (material == null)
            return;

        // URP Unlit perlu dipindahkan ke transparent queue agar alpha warna ghost bekerja.
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void DestroyPreview()
    {
        if (previewVisual != null)
        {
            if (Application.isPlaying) Destroy(previewVisual);
            else DestroyImmediate(previewVisual);
        }
        if (previewMaterial != null)
        {
            if (Application.isPlaying) Destroy(previewMaterial);
            else DestroyImmediate(previewMaterial);
        }
        previewVisual = null;
        previewMaterial = null;
    }

    static GameObject CreateVisual(ItemSO item, Transform parent, string objectName)
    {
        GameObject visual = item.worldPrefab != null ? Instantiate(item.worldPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = objectName;
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = item.worldScale == Vector3.zero ? Vector3.one * 0.4f : item.worldScale;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        return visual;
    }

    ItemStack GetSelectedStack()
    {
        if (inventory == null || hotbarUI == null) return null;
        return inventory.GetSlot(hotbarUI.SelectedIndex);
    }

    static bool HasHeldWorldAction(ItemStack stack) =>
        stack?.item != null && stack.count > 0 && stack.item.HasHeldWorldAction &&
        stack.item.category != ItemCategory.Tool && stack.item.equippedTool == PlayerToolType.None;

    static bool CanDrop(ItemStack stack) =>
        HasHeldWorldAction(stack) && stack.item.canDropToWorld;

    static bool CanPlace(ItemStack stack) =>
        HasHeldWorldAction(stack) && stack.item.canPlaceInWorld;

    Vector3 GetFacing()
    {
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        return facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward;
    }

    void EnsureHandAnchor()
    {
        if (handAnchor != null) return;
        GameObject anchor = new("HeldItemAnchor");
        handAnchor = anchor.transform;
        handAnchor.SetParent(transform, false);
        handAnchor.localPosition = new Vector3(0f, 1.35f, 0.42f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null && inventory.GetComponent<HeldItemPlacementSystem>() == null)
            inventory.gameObject.AddComponent<HeldItemPlacementSystem>();
    }
}
