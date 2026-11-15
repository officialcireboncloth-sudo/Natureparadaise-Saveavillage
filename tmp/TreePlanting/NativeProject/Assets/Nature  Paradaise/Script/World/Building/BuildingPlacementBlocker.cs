using UnityEngine;

/// <summary>
/// Menandai collider environment seperti air, jalan, quest object, atau dekorasi penting
/// sebagai penghalang pembangunan tanpa bergantung pada nama GameObject.
/// </summary>
[DisallowMultipleComponent]
public sealed class BuildingPlacementBlocker : MonoBehaviour
{
    [SerializeField] string blockerLabel = "Environment";
    public string BlockerLabel => string.IsNullOrWhiteSpace(blockerLabel) ? "Environment" : blockerLabel;
}
