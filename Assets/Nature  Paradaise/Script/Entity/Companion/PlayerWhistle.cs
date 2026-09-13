using UnityEngine;

/// <summary>Input whistle player. Hanya PersonalAnimal yang dapat merespons.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerWhistle : MonoBehaviour
{
    [SerializeField] KeyCode whistleKey = KeyCode.V;
    [SerializeField, Min(0.1f)] float cooldown = 1f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip whistleClip;
    [SerializeField] Animator animator;
    [SerializeField] string whistleTrigger = "Whistle";
    float nextWhistleTime;
    bool hasTrigger;
    static AudioClip fallbackWhistle;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == whistleTrigger)
                    { hasTrigger = true; break; }
    }

    void Update()
    {
        if (!Input.GetKeyDown(whistleKey) || Time.unscaledTime < nextWhistleTime || WorldInteractionPrompt.IsSuppressed) return;
        Whistle();
    }

    public bool Whistle()
    {
        nextWhistleTime = Time.unscaledTime + cooldown;
        if (hasTrigger) animator.SetTrigger(whistleTrigger);
        GameAudio.PlayOneShot(audioSource, whistleClip != null ? whistleClip : GetFallbackWhistle(), GameAudioBus.Main);
        PersonalAnimal target = FindTarget();
        if (target == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Tidak ada personal companion yang dapat dipanggil.");
            return false;
        }
        target.Call(transform);
        SaveLoadFeedback.Instance?.ShowMessage($"Bersiul memanggil {target.DisplayName}...");
        return true;
    }

    PersonalAnimal FindTarget()
    {
        PersonalAnimal nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (PersonalAnimal animal in PersonalAnimal.Active)
        {
            if (animal == null || !animal.IsTamed) continue;
            if (animal.IsActiveCompanion) return animal;
            float distance = Vector3.SqrMagnitude(animal.transform.position - transform.position);
            if (distance < nearestDistance) { nearestDistance = distance; nearest = animal; }
        }
        return nearest;
    }

    // Nada pendek bawaan membuat fitur langsung terdengar saat belum ada AudioClip produksi.
    // Mengisi Whistle Clip di Inspector otomatis menggantikan nada ini.
    static AudioClip GetFallbackWhistle()
    {
        if (fallbackWhistle != null) return fallbackWhistle;
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = t / duration;
            float frequency = Mathf.Lerp(1450f, 1950f, Mathf.SmoothStep(0f, 1f, normalized));
            float fade = Mathf.Sin(Mathf.PI * normalized);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.28f;
        }
        fallbackWhistle = AudioClip.Create("Whistle_Default", sampleCount, 1, sampleRate, false);
        fallbackWhistle.SetData(samples, 0);
        return fallbackWhistle;
    }
}
