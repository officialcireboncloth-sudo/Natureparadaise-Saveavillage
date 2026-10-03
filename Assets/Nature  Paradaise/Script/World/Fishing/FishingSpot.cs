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

    [SerializeField] string locationName;
    public string LocationName => string.IsNullOrWhiteSpace(locationName) ? waterType.ToString() : locationName;
    public FishingWaterType WaterType => waterType;
    public IReadOnlyList<FishDefinitionSO> FishPool => fishPool;
    public float MaximumCastDistance => maximumCastDistance;

    public void ConfigureLocation(FishingWaterType type, string displayName)
    {
        waterType = type;
        locationName = displayName;
    }

    void Awake() => spotCollider = GetComponent<Collider>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() => Registry.Remove(this);

    public static bool TryFindCastPoint(Transform player, Vector3 facing, out FishingSpot spot, out Vector3 point)
        => TryFindCastPoint(player, facing, 1f, out spot, out point, out _);

    public static bool TryFindCastPoint(Transform player, Vector3 facing, float power, out FishingSpot spot, out Vector3 point, out float reach)
    {
        spot=null; point=default; reach=0f;
        if(player==null) return false;
        facing.y=0f;
        if(facing.sqrMagnitude<0.01f) facing=player.forward;
        facing.Normalize();
        float limit=0f;
        foreach(var entry in Registry) if(entry!=null) limit=Mathf.Max(limit,entry.maximumCastDistance);
        List<Vector3> points=new();
        List<FishingSpot> spots=new();
        // Stop at the first shoreline after entering water, never skip across land.
        for(float distance=1f;distance<=limit;distance+=0.25f)
        {
            Vector3 sample=player.position+facing*distance+Vector3.up*30f;
            FishingSpot candidate=null;
            RaycastHit hit=default;
            if(Physics.Raycast(sample,Vector3.down,out hit,100f,~0,QueryTriggerInteraction.Ignore))
                candidate=hit.collider.GetComponentInParent<FishingSpot>();
            bool valid=candidate!=null && candidate.isActiveAndEnabled && distance<=candidate.maximumCastDistance;
            if(spot==null) { if(!valid) continue; spot=candidate; }
            // Fish regions sharing continuous water are not shorelines.
            // Resolve the loot region at the landing point, not the first water sample.
            if(!valid) break;
            points.Add(hit.point+Vector3.up*0.06f);
            spots.Add(candidate);
        }
        if(points.Count==0) { spot=null; return false; }
        int index=Mathf.RoundToInt(Mathf.Clamp01(power)*(points.Count-1));
        point=points[index];
        spot=spots[index];
        reach=points.Count>1?(float)index/(points.Count-1):0f;
        return true;
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
                    {
                    FishingSpot spot=candidate.gameObject.AddComponent<FishingSpot>();
                    string context=candidate.transform.parent!=null ? candidate.transform.parent.name.ToLowerInvariant()+objectName : objectName;
                    if(context.Contains("river")) spot.waterType=FishingWaterType.River;
                    else if(context.Contains("ocean") || context.Contains("sea") || context.Contains("beach")) spot.waterType=FishingWaterType.Ocean;
                    else if(context.Contains("pond")) spot.waterType=FishingWaterType.Pond;
                }
            }
        }
    }
}
