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
    string requestedAction;
    float actionSecondsRemaining;
    public bool IsPlayingAction => !string.IsNullOrEmpty(requestedAction);
    public string CurrentAction => motion?.ActionId ?? "Idle";

    void OnEnable()
    {
        requestedAction = null;
        actionSecondsRemaining = 0;
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
        if (IsPlayingAction)
        {
            actionSecondsRemaining -= Mathf.Min(dt, .1f);
            if (actionSecondsRemaining <= 0) ResumeLocomotion();
        }
        motion.Play(IsPlayingAction ? requestedAction : speed > .025f ? "Walk" : "Idle");
        bool cart = requestedAction == "Cart_Push" || requestedAction == "Cart_Pull";
        float referenceSpeed = cart ? requestedAction == "Cart_Pull" ? .43f : .75f : sourceWalkSpeed;
        motion.Evaluate(dt, Mathf.Clamp(speed / referenceSpeed, cart ? 0 : .1f, 3));
    }
    /// <summary>Plays an assigned Humanoid action without Idle/Walk replacing it next frame.
    /// Non-looping actions return to locomotion; looping actions wait for ResumeLocomotion.</summary>
    public bool PlayAction(string id, bool holdLastFrame = false)
    {
        var entry = actions != null ? actions.Find(id) : null;
        if (motion == null || entry?.clip == null || !entry.clip.isHumanMotion || !motion.HasClip(id)) return false;
        if (id == "Idle" || id == "Walk") { ResumeLocomotion(); return false; }
        requestedAction = id;
        actionSecondsRemaining = entry.loop || holdLastFrame ? float.PositiveInfinity : entry.clip.length / Mathf.Max(.05f, entry.speed);
        motion.Restart(id);
        return true;
    }
    public void ResumeLocomotion() { requestedAction = null; actionSecondsRemaining = 0; }
    void OnDisable() { motion?.Dispose(); motion = null; ResumeLocomotion(); }
}
