using UnityEngine;

/// <summary>Animal Bell hanya mengatur ternak produksi yang memiliki AnimalRoutine.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAnimalBell : MonoBehaviour
{
    [SerializeField] KeyCode bellKey = KeyCode.Y;
    [SerializeField, Min(0.1f)] float cooldown = 1f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip bellClip;
    float nextUseTime;
    static AudioClip fallbackBell;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(bellKey) || Time.unscaledTime < nextUseTime ||
            WorldInteractionPrompt.IsSuppressed || GetComponent<PlayerController>().IsMovementLocked) return;
        Ring();
    }

    public void Ring()
    {
        nextUseTime = Time.unscaledTime + cooldown;
        audioSource?.PlayOneShot(bellClip != null ? bellClip : GetFallbackBell());

        AnimalHome selectedHome = BarnInterior.Current != null ? BarnInterior.Current.home : null;
        bool IsTarget(AnimalRoutine routine) => routine != null && routine.Home != null &&
                                                (selectedHome == null || routine.Home == selectedHome);
        bool anyOutside = AnimalRoutine.Active.Exists(routine => IsTarget(routine) && !routine.IsHoused);
        int changed = 0;
        if (anyOutside)
        {
            foreach (AnimalRoutine routine in AnimalRoutine.Active)
            {
                if (!IsTarget(routine) || routine.IsHoused) continue;
                routine.Recall();
                changed++;
            }
            SaveLoadFeedback.Instance?.ShowMessage($"Animal Bell: {changed} ternak dipanggil masuk kandang.");
            return;
        }

        foreach (AnimalRoutine routine in AnimalRoutine.Active)
            if (IsTarget(routine) && routine.Release()) changed++;
        SaveLoadFeedback.Instance?.ShowMessage(changed > 0
            ? $"Animal Bell: {changed} ternak keluar untuk grazing."
            : "Tidak ada ternak yang bisa keluar. Cek waktu, cuaca, kesehatan, dan kandang.");
    }

    static AudioClip GetFallbackBell()
    {
        if (fallbackBell != null) return fallbackBell;
        const int sampleRate = 44100;
        const float duration = 0.7f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float decay = Mathf.Exp(-5f * t);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) +
                          Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.45f) * decay * 0.2f;
        }
        fallbackBell = AudioClip.Create("AnimalBell_Default", count, 1, sampleRate, false);
        fallbackBell.SetData(samples, 0);
        return fallbackBell;
    }
}
