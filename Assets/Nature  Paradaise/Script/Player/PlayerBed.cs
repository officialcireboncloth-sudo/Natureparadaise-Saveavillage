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
            float radiusSquared = interactionRadius * interactionRadius;
            bool isInRange = (playerCandidate.transform.position - transform.position).sqrMagnitude <= radiusSquared;

            if (isInRange)
            {
                nearbyPlayer = playerCandidate;
                float distance = Vector3.Distance(playerCandidate.transform.position, transform.position);
                WorldInteractionPrompt.Request(this, transform, prompt, distance, promptHeight);
            }
            else if (nearbyPlayer == playerCandidate)
            {
                nearbyPlayer = null;
            }
        }

        if (nearbyPlayer != null && Input.GetKeyDown(interactKey))
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
        if (nearbyPlayer == null || nearbyPlayer.IsBusy)
            return;

        nearbyPlayer.SleepAndSave();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureTestingBedExists()
    {
        if (FindFirstObjectByType<PlayerBed>() != null)
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
