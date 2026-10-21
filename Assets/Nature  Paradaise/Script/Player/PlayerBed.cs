using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>
/// Interaksi kasur yang menampilkan prompt lokal dan meminta <see cref="PlayerLifeCycle"/>
/// menjalankan proses tidur ketika player berada dalam jangkauan.
/// </summary>
public sealed class PlayerBed : MonoBehaviour
{
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] string prompt = "Tekan E untuk tidur";
    [SerializeField, Min(0.5f)] float interactionRadius = 2f;
    [SerializeField, Min(0f)] float promptHeight = 1.15f;
    [Tooltip("Aktifkan hanya jika object bed memakai collider interaksi terpisah sebagai trigger.")]
    [SerializeField] bool useTriggerCollider;

    [Header("Bed Animation Placement")]
    [Tooltip("Player root position and facing for the bed wake animation; editable in the scene.")]
    [SerializeField] Transform sleepPose;
    [Tooltip("Safe standing position beside the bed after waking.")]
    [SerializeField] Transform wakeStandPoint;

    PlayerLifeCycle nearbyPlayer;
    PlayerLifeCycle playerCandidate;

    void Awake()
    {
        Collider bedCollider = GetComponent<Collider>();
        bedCollider.isTrigger = useTriggerCollider;
    }

    void Start()
    {
        playerCandidate = FindFirstObjectByType<PlayerLifeCycle>();
    }

    void Update()
    {
        // Fallback jarak membuat interaksi tetap bekerja pada setup CharacterController
        // yang tidak mengirim callback trigger. Satu perhitungan ringan per frame per bed.
        if (playerCandidate != null)
        {
            bool isInRange = CanInteract(playerCandidate.transform);

            if (isInRange)
            {
                nearbyPlayer = playerCandidate;
                var bounds = GetComponent<Collider>().bounds;
                float distance = PlayerInteractionTarget.SurfaceDistance(playerCandidate.transform, transform);
                WorldInteractionPrompt.Request(this, bounds.center + Vector3.up * promptHeight, prompt, distance);
            }
            else if (nearbyPlayer == playerCandidate)
            {
                nearbyPlayer = null;
            }
        }

        if (nearbyPlayer != null && CanInteract(nearbyPlayer.transform) && PlayerInteractionTarget.PressPrompt(this, interactKey))
            Sleep();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerLifeCycle player = other.GetComponentInParent<PlayerLifeCycle>();
        if (player == null)
            return;

        nearbyPlayer = player;
        playerCandidate = player;
    }

    void OnTriggerExit(Collider other)
    {
        PlayerLifeCycle player = other.GetComponentInParent<PlayerLifeCycle>();
        if (player != null && player == nearbyPlayer)
            nearbyPlayer = null;
    }

    // Method public ini bisa langsung dipasang ke Button mobile.
    public void Sleep()
    {
        if (nearbyPlayer == null || nearbyPlayer.IsBusy || !CanInteract(nearbyPlayer.transform))
            return;

        BedRestMenu.Show(nearbyPlayer, sleepPose, wakeStandPoint);
    }

    /// <summary>Distance to the bed surface, independent of the player's facing or the imported mesh pivot.</summary>
    public bool CanInteract(Transform player)
    {
        if(player == null || WorldInteractionPrompt.IsSuppressed) return false;
        var controller = player.GetComponent<PlayerController>();
        if(controller != null && controller.IsMovementLocked) return false;
        var bounds = GetComponent<Collider>().bounds;
        float dx = Mathf.Max(0f, Mathf.Abs(player.position.x - bounds.center.x) - bounds.extents.x);
        float dz = Mathf.Max(0f, Mathf.Abs(player.position.z - bounds.center.z) - bounds.extents.z);
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        return distance <= interactionRadius && Mathf.Abs(player.position.y - bounds.center.y) <= 2.5f;
    }

    /// <summary>Mencari permukaan kasur pada geometri visual, tanpa memakai puncak collider yang mencakup kepala ranjang.</summary>
    public bool TryGetMattressSurface(Vector3 center, out float height)
    {
        height = float.NegativeInfinity;
        Vector3 origin = new(center.x, GetComponent<Collider>().bounds.max.y + 1f, center.z);
        // Hanya saat mulai tidur: tidak menambah collider atau scan mesh pada setiap frame mobile.
        foreach (var filter in GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable) continue;
            var renderer = filter.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled) continue;
            Vector3 rayOrigin = filter.transform.InverseTransformPoint(origin);
            Vector3 direction = filter.transform.InverseTransformVector(Vector3.down);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], edge1 = vertices[triangles[i + 1]] - a, edge2 = vertices[triangles[i + 2]] - a;
                Vector3 cross = Vector3.Cross(direction, edge2);
                float determinant = Vector3.Dot(edge1, cross);
                if (Mathf.Abs(determinant) < .000001f) continue;
                float inverse = 1f / determinant;
                Vector3 offset = rayOrigin - a;
                float u = Vector3.Dot(offset, cross) * inverse;
                Vector3 q = Vector3.Cross(offset, edge1);
                float v = Vector3.Dot(direction, q) * inverse;
                float distance = Vector3.Dot(edge2, q) * inverse;
                if (u < 0f || v < 0f || u + v > 1f || distance < 0f) continue;
                height = Mathf.Max(height, filter.transform.TransformPoint(rayOrigin + direction * distance).y);
            }
        }
        return !float.IsNegativeInfinity(height);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureTestingBedExists()
    {
        // Jika interior rumah tersedia, kasur aslinya memang belum aktif saat world baru dimuat.
        if (Application.CanStreamedLevelBeLoaded("HouseInterior") || FindFirstObjectByType<PlayerBed>() != null)
            return;

        PlayerLifeCycle player = FindFirstObjectByType<PlayerLifeCycle>();
        if (player == null)
            return;

        Vector3 bedPosition = player.transform.position + Vector3.right * 2.5f;
        if (PlayerSpawnPoint.TryGet("player-home", out PlayerSpawnPoint home))
            bedPosition = home.transform.position + home.transform.right * 2.5f;

        GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bed.name = "PlayerBed_DebugRuntime";
        bed.transform.position = bedPosition + Vector3.up * 0.3f;
        bed.transform.localScale = new Vector3(2f, 0.6f, 1f);
        bed.AddComponent<PlayerBed>();
        Debug.Log("[PLAYER] Bed testing dibuat dekat PlayerHomeSpawn.");
    }
}
