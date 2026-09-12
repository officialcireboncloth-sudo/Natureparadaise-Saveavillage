using UnityEngine;

/// <summary>
/// Referensi authoring yang disimpan di prefab exterior Barn. Semua ukuran dan posisi
/// diatur manual di Prefab Mode dan hanya dibaca oleh runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class BarnExteriorAuthoring : MonoBehaviour
{
    [Header("Editable Prefab Parts")]
    [SerializeField] Transform modelRoot;
    [SerializeField] BoxCollider buildingCollision;
    [SerializeField] Transform entrance;

    public Transform ModelRoot => modelRoot != null ? modelRoot : transform;
    public BoxCollider BuildingCollision => buildingCollision;
    public Transform Entrance => entrance != null ? entrance : transform;

    public void Configure(Transform model, BoxCollider collision, Transform entranceMarker)
    {
        modelRoot = model;
        buildingCollision = collision;
        entrance = entranceMarker;
    }

    void OnDrawGizmosSelected()
    {
        if (entrance == null)
            return;

        Gizmos.color = new Color(0.15f, 1f, 0.3f, 0.95f);
        Gizmos.DrawSphere(entrance.position + Vector3.up * 0.15f, 0.3f);
        Gizmos.DrawLine(entrance.position, entrance.position + Vector3.up * 2f);
        Gizmos.DrawRay(entrance.position + Vector3.up, entrance.forward * 1.25f);
    }
}
