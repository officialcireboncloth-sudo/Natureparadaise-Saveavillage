using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Solid body for the visual Lumber owner; navigation explicitly ignores its own body.</summary>
[DisallowMultipleComponent]
public sealed class ConstructionWorkerBody : MonoBehaviour
{
    [SerializeField, HideInInspector] CapsuleCollider body;
    readonly Collider[] overlaps = new Collider[32];

    public static ConstructionWorkerBody AddTo(Transform worker)
    {
        var result = worker.GetComponent<ConstructionWorkerBody>();
        if (result == null) result = worker.gameObject.AddComponent<ConstructionWorkerBody>();
        result.EnsureBody();
        return result;
    }

    void Awake() => EnsureBody();
    void EnsureBody()
    {
        if (body != null) return;
        body = GetComponent<CapsuleCollider>();
        if (body == null) body = gameObject.AddComponent<CapsuleCollider>();
        body.height = 2.1f; body.radius = .34f; body.center = Vector3.up * 1.05f;
        body.enabled = false;
    }
    void OnEnable() { if (body != null) body.enabled = false; }
    void LateUpdate() { if (body != null && !body.enabled) Refresh(); }

    // Loading/dispatch can spawn the owner on an actor. Wait for clearance instead of ejecting them.
    public void Refresh()
    {
        if (body == null || !isActiveAndEnabled) return;
        Physics.SyncTransforms();
        var scale = transform.lossyScale;
        float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float half = Mathf.Max(0, body.height * Mathf.Abs(scale.y) * .5f - radius);
        var center = transform.TransformPoint(body.center);
        int count = gameObject.scene.GetPhysicsScene().OverlapCapsule(center - Vector3.up * half,
            center + Vector3.up * half, radius + .025f, overlaps, ~0, QueryTriggerInteraction.Ignore);
        bool occupied = count == overlaps.Length;
        for (int i = 0; i < count && !occupied; i++)
        {
            var other = overlaps[i];
            if (other == null || other.transform.IsChildOf(transform)) continue;
            occupied = other is CharacterController || other.GetComponentInParent<PlayerController>() != null ||
                other.GetComponentInParent<AnimalController>() != null || other.attachedRigidbody != null;
        }
        body.enabled = !occupied;
    }
}
