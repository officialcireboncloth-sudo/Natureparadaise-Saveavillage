using UnityEngine;

/// <summary>
/// Pasang komponen ini pada object interaktif tambahan (pohon, pintu, mesin, dll.)
/// agar prompt otomatis muncul di atas object saat Player mendekat.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldInteractablePromptSource : MonoBehaviour
{
    [SerializeField] string prompt = "Tekan E untuk berinteraksi";
    [SerializeField, Min(0.25f)] float interactionRadius = 2f;
    [SerializeField, Min(0f)] float promptHeight = 1.2f;
    [SerializeField] Transform promptAnchor;

    PlayerController player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
                return;
        }

        Transform anchor = promptAnchor != null ? promptAnchor : transform;
        float squaredDistance = (player.transform.position - transform.position).sqrMagnitude;
        if (!PlayerInteractionTarget.Contains(player.transform, transform))
            return;

        WorldInteractionPrompt.Request(this, anchor, prompt, Mathf.Sqrt(squaredDistance), promptHeight);
    }
}
