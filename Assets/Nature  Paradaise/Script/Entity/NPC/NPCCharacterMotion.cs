using UnityEngine;

/// <summary>Retargets shared player actions onto the NPC's own skinned Humanoid rig, without player input or root motion.</summary>
[DisallowMultipleComponent]
public sealed class NPCCharacterMotion : MonoBehaviour
{
    [Header("Shared animation")]
    public Animator character;
    public HumanoidActionLibrary actions;
    [Tooltip("World walking speed represented by the source walking clip.")]
    [Min(.1f)] public float sourceWalkSpeed = 1.6f;
    HumanoidActionPlayer motion;
    Vector3 previousPosition;
    public string CurrentAction => motion?.ActionId ?? "Idle";

    void OnEnable()
    {
        previousPosition = transform.position;
        if (character == null) character = GetComponentInChildren<Animator>();
        if (character != null && character.avatar != null && character.avatar.isValid && character.avatar.isHuman && actions != null)
            motion = new HumanoidActionPlayer(character, actions);
    }
    void LateUpdate()
    {
        Vector3 delta = transform.position - previousPosition; delta.y = 0;
        previousPosition = transform.position;
        if (motion == null) return;
        float dt = TimeManager.Instance != null && TimeManager.Instance.IsPaused ? 0 : Time.deltaTime;
        float speed = dt > .0001f && delta.magnitude < 1 ? delta.magnitude / dt : 0;
        motion.Play(speed > .025f ? "Walk" : "Idle");
        motion.Evaluate(dt, Mathf.Clamp(speed / sourceWalkSpeed, .1f, 3));
    }
    void OnDisable() { motion?.Dispose(); motion = null; }
}
