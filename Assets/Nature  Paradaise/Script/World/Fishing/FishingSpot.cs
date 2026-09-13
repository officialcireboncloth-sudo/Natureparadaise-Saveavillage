using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
/// <summary>Collider air yang menerima cast dan menentukan pool ikan berdasarkan lokasi.</summary>
public sealed class FishingSpot : MonoBehaviour
{
    static readonly List<FishingSpot> Registry = new();

    [SerializeField] FishingWaterType waterType = FishingWaterType.Lake;
    [SerializeField] List<FishDefinitionSO> fishPool = new();
    [Header("Junk Loot")]
    [SerializeField, Range(0f, 1f)] float baseJunkChance = 0.12f;
    [SerializeField] List<ItemSO> junkPool = new();
    [SerializeField, Min(1f)] float maximumCastDistance = 12f;
    Collider spotCollider;

    public FishingWaterType WaterType => waterType;
    public IReadOnlyList<FishDefinitionSO> FishPool => fishPool;
    public float MaximumCastDistance => maximumCastDistance;

    void Awake() => spotCollider = GetComponent<Collider>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() => Registry.Remove(this);

    public static bool TryFindCastPoint(Transform player, Vector3 facing, out FishingSpot spot, out Vector3 point)
    {
        spot = null;
        point = default;
        if (player == null) return false;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = player.forward;
        facing.Normalize();

        float furthest = 0f;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null) furthest = Mathf.Max(furthest, Registry[i].maximumCastDistance);

        // Ambil permukaan paling atas di depan player. Terrain akan menang di daratan,
        // sedangkan collider FishingSpot menang saat sample sudah berada di atas air.
        for (float distance = furthest; distance >= 2f; distance -= 0.5f)
        {
            Vector3 sample = player.position + facing * distance + Vector3.up * 30f;
            if (!Physics.Raycast(sample, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            FishingSpot candidate = hit.collider.GetComponentInParent<FishingSpot>();
            if (candidate == null || distance > candidate.maximumCastDistance) continue;
            spot = candidate;
            point = hit.point + Vector3.up * 0.06f;
            return true;
        }
        return false;
    }

    public List<FishDefinitionSO> GetEligibleFish(int day, int hour, WeatherType weather, int rodLevel)
    {
        FishDefinitionSO[] source = fishPool.Count > 0
            ? fishPool.ToArray()
            : Resources.LoadAll<FishDefinitionSO>("Fishing");
        List<FishDefinitionSO> result = new();
        foreach (FishDefinitionSO definition in source)
            if (definition != null && definition.IsAvailable(waterType, day, hour, weather, rodLevel)) result.Add(definition);
        return result;
    }

    public bool TryRollJunk(ItemSO bait, out ItemSO junk)
    {
        junk = null;
        ItemSO[] source = junkPool.Count > 0 ? junkPool.ToArray() : Resources.LoadAll<ItemSO>("Items/Fishing/Junk");
        if (source.Length == 0) return false;
        float reduction = bait != null && bait.IsFishingBait ? Mathf.Clamp01(bait.baitJunkReduction) : 0f;
        if (Random.value >= Mathf.Clamp01(baseJunkChance) * (1f - reduction)) return false;
        junk = source[Random.Range(0, source.Length)];
        return junk != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallRuntimeMapper()
    {
        GameObject host = new("FishingSpotMapper_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<FishingSpotMapper>();
    }

    sealed class FishingSpotMapper : MonoBehaviour
    {
        void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;
        void Start() => MapWaterColliders();
        void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => MapWaterColliders();

        static void MapWaterColliders()
        {
            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Collider candidate in colliders)
            {
                if (candidate == null || candidate.GetComponentInParent<FishingSpot>() != null) continue;
                string objectName = candidate.name.ToLowerInvariant();
                if (objectName == "water" || objectName.Contains("fishing_water"))
                    candidate.gameObject.AddComponent<FishingSpot>();
            }
        }
    }
}
