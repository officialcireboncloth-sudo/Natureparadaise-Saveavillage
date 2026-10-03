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
                float distance = Vector3.Distance(playerCandidate.transform.position, bounds.ClosestPoint(playerCandidate.transform.position));
                WorldInteractionPrompt.Request(this, bounds.center + Vector3.up * promptHeight, prompt, distance);
            }
            else if (nearbyPlayer == playerCandidate)
            {
                nearbyPlayer = null;
            }
        }

        if (nearbyPlayer != null && CanInteract(nearbyPlayer.transform) && PlayerInteractionTarget.Press(interactKey))
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
        return distance <= interactionRadius && Mathf.Abs(player.position.y - bounds.center.y) <= 2.5f && !ToolStorageChest.HasCloserRack(player,distance) && !HouseStorageChest.HasCloserStorage(player,distance);
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
