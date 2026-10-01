using UnityEngine;

[CreateAssetMenu(menuName="Game/Animal Manure Settings")]
public sealed class AnimalManureSettings : ScriptableObject
{
    [Min(.5f)] public float minimumHours=3f;
    [Min(.5f)] public float maximumHours=6f;
    [Tooltip("Produksi berhenti sementara ketika batas ini tercapai; kotoran lama tidak dihapus.")]
    [Min(1)] public int maximumPilesPerAnimal=6;
    [Min(.5f)] public float pickupRadius=2.4f;
    [Min(.1f)] public float pickupAnimationSeconds=1.1f;
    public GameObject manurePrefab;
}
