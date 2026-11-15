using UnityEngine;

/// <summary>
/// Penanda opsional untuk permukaan tempat item dapat diletakkan.
/// Tanah/lantai datar tetap valid tanpa komponen ini; pasang komponen ini untuk
/// mengizinkan atau melarang meja, display, peti, dan permukaan khusus lainnya.
/// </summary>
[DisallowMultipleComponent]
public sealed class ItemPlacementSurface : MonoBehaviour
{
    [Tooltip("Matikan untuk menolak Place pada object ini walaupun permukaannya datar.")]
    [SerializeField] bool allowItemPlacement = true;
    [Tooltip("Menandai bahwa hasil Place harus diam tanpa Rigidbody. Sistem Place saat ini selalu stabil; field ini disiapkan untuk mode display/container berikutnya.")]
    [SerializeField] bool forceStablePlacement = true;

    public bool AllowItemPlacement => allowItemPlacement;
    public bool ForceStablePlacement => forceStablePlacement;
}
