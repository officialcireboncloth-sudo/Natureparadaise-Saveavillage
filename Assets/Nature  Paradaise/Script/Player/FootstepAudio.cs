using System;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// Memutar langkah berdasarkan jarak tempuh dan memilih variasi suara sesuai permukaan tanah.
/// Sistem berbasis jarak menjaga cadence konsisten saat kecepatan movement berubah.
/// </summary>
public sealed class FootstepAudio : MonoBehaviour
{
    [Serializable]
    /// <summary>Pemetaan Physics Material/tag permukaan ke kumpulan audio langkah.</summary>
    public sealed class SurfaceProfile
    {
        public string label = "Grass";
        [Tooltip("Opsional: cocok berdasarkan Physics Material.")]
        public PhysicsMaterial physicsMaterial;
        [Tooltip("Opsional: cocok berdasarkan Tag collider, misalnya Grass atau Wood.")]
        public string surfaceTag;
        public AudioClip[] clips;
        [Range(0f, 2f)] public float volumeMultiplier = 1f;
    }

    [SerializeField] PlayerController movement;
    [SerializeField] AudioSource audioSource;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField, Min(0.1f)] float groundCheckDistance = 1.4f;
    [SerializeField, Min(0.1f)] float walkStepDistance = 1.25f;
    [SerializeField, Min(0.1f)] float runStepDistance = 1.65f;
    [SerializeField, Min(0.1f)] float sprintStepDistance = 1.9f;
    [SerializeField, Range(0f, 1f)] float baseVolume = 0.65f;
    [SerializeField] AudioClip[] defaultClips;
    [SerializeField] SurfaceProfile[] surfaces;

    float travelledDistance;
    int lastClipIndex = -1;

    void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 14f;
    }

    void Update()
    {
        if (movement == null || !movement.IsGrounded || movement.CurrentSpeed < 0.15f)
        {
            travelledDistance = 0f;
            return;
        }

        travelledDistance += movement.CurrentSpeed * Time.deltaTime;
        float requiredDistance = movement.CurrentMode switch
        {
            PlayerController.MovementMode.Sprint => sprintStepDistance,
            PlayerController.MovementMode.Run => runStepDistance,
            _ => walkStepDistance
        };

        if (travelledDistance < requiredDistance)
            return;

        travelledDistance %= requiredDistance;
        PlayFootstep();
    }

    // Bisa dipanggil langsung dari Animation Event untuk sinkronisasi animasi final.
    public void PlayFootstep()
    {
        AudioClip[] clips = defaultClips;
        float volumeMultiplier = 1f;
        if (Physics.Raycast(transform.position + Vector3.up * 0.35f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            SurfaceProfile profile = FindProfile(hit.collider);
            if (profile != null && profile.clips != null && profile.clips.Length > 0)
            {
                clips = profile.clips;
                volumeMultiplier = profile.volumeMultiplier;
            }
        }

        if (clips == null || clips.Length == 0)
            return;

        int index = clips.Length == 1 ? 0 : UnityEngine.Random.Range(0, clips.Length);
        if (index == lastClipIndex && clips.Length > 1)
            index = (index + 1) % clips.Length;
        lastClipIndex = index;
        if (clips[index] != null)
            audioSource.PlayOneShot(clips[index], baseVolume * volumeMultiplier);
    }

    SurfaceProfile FindProfile(Collider ground)
    {
        if (surfaces == null)
            return null;

        for (int i = 0; i < surfaces.Length; i++)
        {
            SurfaceProfile profile = surfaces[i];
            if (profile == null) continue;
            if (profile.physicsMaterial != null && ground.sharedMaterial == profile.physicsMaterial)
                return profile;
            if (!string.IsNullOrWhiteSpace(profile.surfaceTag) && ground.tag == profile.surfaceTag)
                return profile;
        }
        return null;
    }
}
