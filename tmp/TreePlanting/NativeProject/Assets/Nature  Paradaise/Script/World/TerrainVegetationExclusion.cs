using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(BoxCollider))]
public sealed class TerrainVegetationExclusion : MonoBehaviour
{
    [Min(0)] public float padding = .4f;
    public bool Contains(Vector3 point)
    {
        var box = GetComponent<BoxCollider>();
        Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
        Vector3 size = box.size * .5f;
        return Mathf.Abs(local.x) <= size.x + padding / Mathf.Max(.001f, Mathf.Abs(box.transform.lossyScale.x)) &&
               Mathf.Abs(local.z) <= size.z + padding / Mathf.Max(.001f, Mathf.Abs(box.transform.lossyScale.z));
    }
}
