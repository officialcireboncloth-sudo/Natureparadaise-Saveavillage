using UnityEngine;

[CreateAssetMenu(menuName="Game/Animal Manure Settings")]
public sealed class AnimalManureSettings : ScriptableObject
{
    public const float CollectionSeconds=1f;
    [Tooltip("Satu jadwal acak per hewan per hari. Kotoran lama harus dibersihkan sebelum muncul yang baru.")]
    [Range(0,23)] public float dailyEarliestHour=6f;
    [Range(0,23.99f)] public float dailyLatestHour=22f;
    [Min(.5f)] public float pickupRadius=2.4f;
    public GameObject manurePrefab;
}
