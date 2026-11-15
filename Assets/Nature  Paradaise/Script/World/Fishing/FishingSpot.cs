using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>Zona mandiri yang menerima cast dan menentukan pool ikan.</summary>
public sealed class FishingSpot : MonoBehaviour
{
    static readonly List<FishingSpot> Registry = new();
    const float MinimumCastDistance = 1f;
    const float LandingOffset = 0.06f;
    const int RingSegments = 32;

    [SerializeField, Min(0.5f), Tooltip("Radius zona dalam meter. Position menentukan pusat dan tinggi zona.")]
    float radius = 1.5f;
    [SerializeField, Min(MinimumCastDistance)] float maximumCastDistance = 12f;
    [SerializeField] string locationName;

    [SerializeField, Tooltip("Tipe perairan untuk menyaring daftar ikan zona ini.")]
    FishingWaterType waterType = FishingWaterType.Lake;
    [SerializeField, Tooltip("Kosong memakai semua ikan yang cocok dengan tipe air. Isi untuk membatasi spesies di spot ini.")]
    List<FishDefinitionSO> fishPool = new();
    [Header("Junk Loot")]
    [SerializeField, Range(0f, 1f)] float baseJunkChance = 0.12f;
    [SerializeField] List<ItemSO> junkPool = new();
    LineRenderer zoneRing;
    Material zoneMaterial;
    Vector3 ringCenter;
    float ringRadius;

    public string LocationName => string.IsNullOrWhiteSpace(locationName) ? waterType.ToString() : locationName;
    public FishingWaterType WaterType => waterType;
    public IReadOnlyList<FishDefinitionSO> FishPool => fishPool;
    public float MaximumCastDistance => maximumCastDistance;
    public float Radius => radius;
    public Vector3 LandingCenter => transform.position + Vector3.up * LandingOffset;
    IEnumerable<FishDefinitionSO> FishDefinitions => fishPool.Count > 0
        ? fishPool
        : Resources.LoadAll<FishDefinitionSO>("Fishing");

    public void ConfigureLocation(FishingWaterType type, string displayName, float zoneRadius = 1.5f)
    {
        waterType = type;
        locationName = displayName;
        radius = Mathf.Max(0.5f, zoneRadius);
    }

    void OnEnable()
    {
        Registry.Add(this);
        if (zoneRing != null) zoneRing.enabled = true;
    }

    void OnDisable()
    {
        Registry.Remove(this);
        if (zoneRing != null) zoneRing.enabled = false;
    }

    void Start()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;

        zoneMaterial = new Material(shader)
        {
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10
        };
        zoneRing = gameObject.AddComponent<LineRenderer>();
        zoneRing.sharedMaterial = zoneMaterial;
        zoneRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        zoneRing.receiveShadows = false;
        zoneRing.useWorldSpace = true;
        zoneRing.loop = true;
        zoneRing.widthMultiplier = 0.045f;
        zoneRing.startColor = zoneRing.endColor = new Color(0.65f, 0.94f, 0.9f, 0.82f);
        zoneRing.positionCount = RingSegments;
        UpdateRing();
    }

    void LateUpdate() => UpdateRing();

    void UpdateRing()
    {
        if (zoneRing == null) return;
        Vector3 center = LandingCenter;
        if (center == ringCenter && radius == ringRadius) return;

        ringCenter = center;
        ringRadius = radius;
        for (int index = 0; index < RingSegments; index++)
        {
            float angle = index * (2f * Mathf.PI / RingSegments);
            zoneRing.SetPosition(index, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
    }

    void OnDestroy()
    {
        if (zoneRing != null) Destroy(zoneRing);
        if (zoneMaterial != null) Destroy(zoneMaterial);
    }

    public static bool TryFindCastPoint(Transform player, Vector3 facing, out FishingSpot spot, out Vector3 point)
        => TryFindCastPoint(player, facing, 1f, out spot, out point, out _);

    public static bool TryFindCastPoint(Transform player, Vector3 facing, float power, out FishingSpot spot, out Vector3 point, out float reach)
    {
        spot = null;
        point = default;
        reach = 0f;
        if (player == null) return false;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f)
        {
            facing = player.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f) return false;
        }
        facing.Normalize();
        Vector3 origin = player.position;
        float firstDistance = float.PositiveInfinity;
        float lastDistance = 0f;
        float nearestHeightDifference = float.PositiveInfinity;
        // Intersect the horizontal aim with each zone's circle in world space.
        foreach (FishingSpot entry in Registry)
        {
            if (entry == null) continue;
            Vector3 offset = entry.transform.position - origin;
            float along = offset.x * facing.x + offset.z * facing.z;
            float across = offset.x * facing.z - offset.z * facing.x;
            float halfSpanSquared = entry.radius * entry.radius - across * across;
            if (halfSpanSquared < 0f) continue;

            float halfSpan = Mathf.Sqrt(halfSpanSquared);
            float near = Mathf.Max(MinimumCastDistance, along - halfSpan);
            float far = Mathf.Min(entry.maximumCastDistance, along + halfSpan);
            float heightDifference = Mathf.Abs(offset.y);
            if (near > far || near > firstDistance || (near == firstDistance && heightDifference >= nearestHeightDifference)) continue;

            spot = entry;
            firstDistance = near;
            lastDistance = far;
            nearestHeightDifference = heightDifference;
        }

        if (spot == null) return false;
        reach = Mathf.Clamp01(power);
        point = origin + facing * Mathf.Lerp(firstDistance, lastDistance, reach);
        point.y = spot.LandingCenter.y;
        return true;
    }

    public List<FishDefinitionSO> GetEligibleFish(int day, int hour, WeatherType weather, int rodLevel)
    {
        List<FishDefinitionSO> result = new();
        foreach (FishDefinitionSO definition in FishDefinitions)
            if (definition != null && definition.IsAvailable(waterType, day, hour, weather, rodLevel)) result.Add(definition);
        return result;
    }

    internal List<FishDefinitionSO> GetRareFishForTesting()
    {
        List<FishDefinitionSO> result = new();
        foreach (FishDefinitionSO definition in FishDefinitions)
            if (definition != null && definition.rarity >= FishRarity.Rare && definition.CanSpawnIn(waterType))
                result.Add(definition);
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
}
