using UnityEngine;

/// <summary>Timing in normalized animation space, shared by the can pose and water emission.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/Player/Watering Action Profile")]
public sealed class WateringActionProfile : ScriptableObject
{
    [Range(.02f, .5f)] public float pourStart = .12f;
    [Range(.5f, .95f)] public float pourEnd = .72f;
    [Range(.75f, 1f)] public float recoveryEnd = .92f;
    [Tooltip("2.8-second Watering Plant clip plays at state speed 2. Only used without a valid animator.")]
    [Min(.2f)] public float fallbackDuration = 1.4f;
    [Range(.05f, .3f)] public float dropFlightTime = .18f;
    [Min(.1f)] public float animationEntryTimeout = .5f;
    [Min(2)] public float actionTimeout = 8;
    static WateringActionProfile current;
    public static WateringActionProfile Current
    {
        get
        {
            if(current == null) current = Resources.Load<WateringActionProfile>("Profiles/Watering Action");
            if(current == null) { current = CreateInstance<WateringActionProfile>(); current.hideFlags = HideFlags.HideAndDontSave; }
            return current;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => current = null;

    public float PoseWeight(float phase) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.01f, pourStart, phase)) *
        (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(pourEnd, recoveryEnd, phase)));

    /// <summary>Includes the incoming state during a blend; normalized progress follows actual animator speed.</summary>
    public static bool TryPhase(Animator animator, out float phase)
    {
        phase = 0;
        if(animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return false;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if(animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if(next.IsName("Watering Plant")) state = next;
        }
        if(!state.IsName("Watering Plant")) return false;
        phase = Mathf.Clamp01(state.normalizedTime);
        return true;
    }
}
