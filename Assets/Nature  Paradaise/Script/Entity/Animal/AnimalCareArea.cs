using UnityEngine;

/// <summary>Pasang pada trigger pasture atau shelter; lokasi zona tetap diatur manual oleh designer.</summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class AnimalCareArea : MonoBehaviour
{
    [Tooltip("Aktif untuk kandang; mati untuk area rumput grazing.")]
    public bool providesShelter;
    void Reset() => GetComponent<BoxCollider>().isTrigger = true;
    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }
    void OnTriggerStay(Collider other)
    {
        AnimalGrowthSystem animal = other.GetComponentInParent<AnimalGrowthSystem>();
        if (animal == null) return;
        animal.SetSheltered(providesShelter);
        // Nutrisi grazing diberikan oleh AnimalRoutine setelah rumput benar-benar dikonsumsi.
    }
    void OnTriggerExit(Collider other)
    {
        if (providesShelter) other.GetComponentInParent<AnimalGrowthSystem>()?.SetSheltered(false);
    }
}
