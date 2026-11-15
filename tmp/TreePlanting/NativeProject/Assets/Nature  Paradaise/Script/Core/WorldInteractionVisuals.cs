using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One selected target ring and one batched mesh for nearby collectible glints.</summary>
[DefaultExecutionOrder(10010), DisallowMultipleComponent]
public sealed class WorldInteractionVisuals : MonoBehaviour
{
    [SerializeField, Min(.5f)] float revealDistance = 12f;
    [SerializeField, Range(1, 48)] int maximumGlints = 12;
    [Tooltip("Half-size in UI reference pixels at 720p; scales with screen height for mobile readability.")]
    [SerializeField, Range(4f, 24f)] float glintSizePixels = 7f;
    [SerializeField, HideInInspector] int readabilityRevision;
    [SerializeField] Color interactionColor = new(1f, .8f, .12f, .55f);
    [SerializeField] Color collectibleColor = new(.4f, 1f, .28f, .55f);

    static WorldInteractionVisuals instance;
    PlayerController player;
    Camera worldCamera;
    Mesh ringMesh, glintMesh;
    MeshRenderer ringRenderer, glintRenderer;
    readonly List<Vector3> vertices = new(1800);
    readonly List<Color> colors = new(1800);
    readonly List<int> triangles = new(4000);
    readonly List<Candidate> candidates = new(48);
    readonly RaycastHit[] groundHits = new RaycastHit[16];
    struct Candidate { public Vector3 point; public float distance, phase; }

    void OnEnable() => ApplyReadabilityDefaults();
    void ApplyReadabilityDefaults()
    {
        // Upgrade the runtime manager that survives a Play Mode script reload too.
        if (readabilityRevision >= 4) return;
        if (readabilityRevision < 3)
        {
            if (readabilityRevision < 2) revealDistance = Mathf.Max(revealDistance, 12f);
            glintSizePixels = Mathf.Min(glintSizePixels, 7f);
            maximumGlints = Mathf.Min(maximumGlints, 12);
        }
        interactionColor.a = Mathf.Min(interactionColor.a, .55f);
        collectibleColor.a = Mathf.Min(collectibleColor.a, .55f);
        readabilityRevision = 4;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureExists()
    {
        if (!Application.isPlaying || instance != null) return;
        instance = FindFirstObjectByType<WorldInteractionVisuals>();
        if (instance == null) new GameObject("WorldInteractionVisuals_Runtime").AddComponent<WorldInteractionVisuals>();
    }
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);
        ApplyReadabilityDefaults();
        // Vertices are written in world coordinates, independently of item scales and pooling parents.
        transform.SetParent(null); transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); transform.localScale = Vector3.one;
        EnsureRenderers();
    }
    bool EnsureRenderers()
    {
        if (ringRenderer != null && glintRenderer != null) return true;
        // Script reload can precede the import of a newly added Resources material.
        Material material = Resources.Load<Material>("UI/Interaction/WorldInteractionClue");
        if (material == null) return false;
        if (ringRenderer == null) ringRenderer = MakeRenderer("Selected interaction target", material, 5000, out ringMesh);
        if (glintRenderer == null) glintRenderer = MakeRenderer("Nearby collectible glints", material, 5001, out glintMesh);
        return true;
    }
    MeshRenderer MakeRenderer(string label, Material material, int order, out Mesh mesh)
    {
        var root = new GameObject(label); root.transform.SetParent(transform, false);
        mesh = new Mesh { name = label }; mesh.MarkDynamic();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.sortingOrder = order; renderer.enabled = false; return renderer;
    }
    void OnDisable() { if (ringRenderer != null) ringRenderer.enabled = false; if (glintRenderer != null) glintRenderer.enabled = false; }
    void OnDestroy()
    {
        if (ringMesh != null) Destroy(ringMesh); if (glintMesh != null) Destroy(glintMesh);
        if (instance == this) instance = null;
    }
    void LateUpdate()
    {
        if (!EnsureRenderers()) return;
        if (player == null || !player.isActiveAndEnabled) player = FindFirstObjectByType<PlayerController>();
        if (worldCamera == null || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        if (player == null || worldCamera == null || player.IsMovementLocked || WorldInteractionPrompt.IsSuppressed ||
            GameplayPauseMenu.BlocksGameplayInput || Time.timeScale <= 0f)
        { ringRenderer.enabled = glintRenderer.enabled = false; return; }
        DrawSelectedTarget();
        CollectNearby(); DrawGlints();
    }

    void DrawSelectedTarget()
    {
        ringRenderer.enabled = false;
        if (!WorldInteractionPrompt.TryGetDisplayedTarget(out Object owner, out Transform anchor, out Vector3 point)) return;
        if (anchor == null && owner is Component component) anchor = component.transform;
        // Player-owned use/eat/fishing/held-item text does not mark the player as a world target.
        if (anchor == null || anchor == player.transform || anchor.IsChildOf(player.transform)) return;
        float radius = .48f; bool collect = false;
        var crop = anchor.GetComponent<FieldCropView>();
        var pickup = owner as WorldItemPickup;
        var gatherable = anchor.GetComponent<WorldGatherable>();
        var tree = anchor.GetComponent<WorldTree>();
        if (crop != null)
        {
            point = crop.transform.position + crop.transform.up * .045f;
            radius = Mathf.Clamp(crop.Field != null ? crop.Field.CellSize * .25f : .48f, .24f, 1f);
            collect = crop.IsHarvestHintAvailable;
        }
        else if (pickup != null)
        { point = pickup.IndicatorGroundPosition; collect = pickup.IsPickupAvailable; }
        else
        {
            point = GroundPosition(anchor);
            var hotbar = player.GetComponent<PlayerToolHotbar>();
            collect = tree != null && tree.IsHarvestHintAvailable || gatherable != null &&
                (gatherable.CanPull || gatherable.CanSickle && hotbar != null && hotbar.SelectedTool == PlayerToolType.Sickle ||
                 gatherable.CanHammer && hotbar != null && hotbar.SelectedTool == PlayerToolType.Hammer);
        }
        ClearGeometry();
        Color color = collect ? collectibleColor : interactionColor;
        // A single quiet stroke marks the target without a thick halo or breathing animation.
        AddRing(point, radius, .022f, color);
        Upload(ringMesh, ringRenderer);
    }
    Vector3 GroundPosition(Transform target)
    {
        Vector3 point = target.position;
        int count = Physics.RaycastNonAlloc(point + Vector3.up * .35f, Vector3.down, groundHits, 3f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity; Vector3 found = point;
        for (int i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.collider == null || hit.transform == target || hit.transform.IsChildOf(target) || hit.transform.IsChildOf(player.transform)) continue;
            if (hit.distance >= best) continue; best = hit.distance; found = hit.point;
        }
        return found + Vector3.up * .04f;
    }

    void CollectNearby()
    {
        candidates.Clear();
        foreach (var pickup in WorldItemPickup.ActivePickups)
            if (pickup != null && pickup.IsCollectibleHintAvailable)
                Offer(pickup.VisualAnchor, pickup.CollectibleHintPosition, pickup.GetInstanceID());
        foreach (var crop in FieldCropView.Active)
            if (crop != null && crop.IsHarvestHintAvailable)
                Offer(crop.transform, crop.CollectibleHintPosition, crop.GetInstanceID());
        foreach (var resource in WorldGatherable.Active)
            if (resource != null && resource.isActiveAndEnabled && resource.IsAvailable && (resource.CanPull || resource.CanSickle || resource.CanHammer))
                Offer(resource.transform, resource.transform.position + Vector3.up * Mathf.Min(.65f, resource.PromptHeight * .5f), resource.GetInstanceID());
        foreach (var tree in WorldTree.Active)
            if (tree != null && tree.IsHarvestHintAvailable)
                Offer(tree.transform, tree.transform.position + Vector3.up * 1.3f, tree.GetInstanceID());
        var manure = AnimalManureSystem.Instance;
        if (manure != null && manure.isActiveAndEnabled && !manure.IsCollecting)
            foreach (var visual in manure.ActiveDropVisuals)
                if (visual != null && visual.activeInHierarchy)
                    Offer(visual.transform, visual.transform.position + Vector3.up * .25f, visual.GetInstanceID());
    }
    void Offer(Transform anchor, Vector3 point, int seed)
    {
        Vector3 delta = anchor.position - player.transform.position;
        float squared = delta.x * delta.x + delta.z * delta.z;
        if (Mathf.Abs(delta.y) > 2.5f || squared >= revealDistance * revealDistance) return;
        Vector3 viewport = worldCamera.WorldToViewportPoint(point);
        if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return;
        int index = candidates.Count;
        while (index > 0 && squared < candidates[index - 1].distance) index--;
        if (index >= maximumGlints) return;
        candidates.Insert(index, new Candidate { point = point, distance = squared, phase = (seed & 1023) * .061f });
        if (candidates.Count > maximumGlints) candidates.RemoveAt(candidates.Count - 1);
    }
    void DrawGlints()
    {
        ClearGeometry();
        foreach (var candidate in candidates)
        {
            float distance = Mathf.Sqrt(candidate.distance);
            // Stay readable through most of the larger discovery range; fade only at its boundary.
            float opacity = CollectibleClueRules.Opacity(distance, revealDistance, Time.unscaledTime, candidate.phase);
            if (opacity <= 0f) continue;
            float unitsPerPixel = worldCamera.orthographic ? worldCamera.orthographicSize * 2f / Mathf.Max(1, worldCamera.pixelHeight) :
                2f * Mathf.Max(.1f, Vector3.Dot(candidate.point - worldCamera.transform.position, worldCamera.transform.forward)) *
                Mathf.Tan(worldCamera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Max(1, worldCamera.pixelHeight);
            float uiScale = Mathf.Clamp(worldCamera.pixelHeight / 720f, .85f, 2.5f);
            float mobileScale = Application.isMobilePlatform ? 1.2f : 1f;
            float size = glintSizePixels * uiScale * mobileScale * unitsPerPixel;
            AddReadableStar(candidate.point, size, new Color(1f, .97f, .79f, opacity));
        }
        Upload(glintMesh, glintRenderer);
    }
    void ClearGeometry() { vertices.Clear(); colors.Clear(); triangles.Clear(); }
    void AddReadableStar(Vector3 point, float size, Color color)
    {
        // Keep the single clue readable on bright ground without a heavy outline.
        AddStar(point, size * 1.12f, new Color(.12f, .10f, .035f, color.a * .32f));
        AddStar(point, size, color);
    }
    void AddRing(Vector3 point, float radius, float width, Color color)
    {
        const int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 da = new(Mathf.Cos(a), 0, Mathf.Sin(a)), db = new(Mathf.Cos(b), 0, Mathf.Sin(b));
            AddQuad(point + da * (radius - width * .5f), point + da * (radius + width * .5f),
                point + db * (radius + width * .5f), point + db * (radius - width * .5f), color);
        }
    }
    void AddStar(Vector3 point, float size, Color color)
    {
        Vector3 right = worldCamera.transform.right * size, up = worldCamera.transform.up * size;
        // Four long points and four short insets make a crisp glint at small pixel sizes.
        int center = vertices.Count; vertices.Add(point); colors.Add(color);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * .25f, radius = i % 2 == 0 ? 1f : .226274f;
            vertices.Add(point + (up * Mathf.Cos(angle) + right * (.72f * Mathf.Sin(angle))) * radius);
            colors.Add(new Color(color.r, color.g, color.b, color.a * .9f));
        }
        for (int i = 0; i < 8; i++) { triangles.Add(center); triangles.Add(center + i + 1); triangles.Add(center + (i + 1) % 8 + 1); }
    }
    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        for (int i = 0; i < 4; i++) colors.Add(color);
        triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 3);
    }
    void Upload(Mesh mesh, MeshRenderer renderer)
    {
        renderer.enabled = vertices.Count > 0; mesh.Clear();
        if (vertices.Count == 0) return;
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
    }
}
