using System.Collections.Generic;
using UnityEngine;

/// <summary>One outdoor lamp scheduler; no shadow maps or per-lamp Update loops.</summary>
[DisallowMultipleComponent]
public sealed class OutdoorNightLighting : MonoBehaviour
{
    sealed class Lamp
    {
        public Light light;
        public bool owned, wasEnabled;
        public float intensity;
    }

    readonly List<Lamp> lamps = new();
    readonly HashSet<int> registered = new();
    readonly List<Lamp> nearest = new();
    float nextScan, nextTick;
    Transform player;
    GardenLightingProfile profile;

    void OnEnable() { nextScan = nextTick = 0; }

    void Update()
    {
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + .1f;
        if (profile == null) profile = Resources.Load<GardenLightingProfile>("World/Garden Lighting");
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 2f;
            Discover();
        }
        float hour = TimeManager.Instance != null ? TimeManager.Instance.CurrentTimeHours : 12;
        float night = 1f - DayNightCycle.GetDaylightWeight(hour,
            profile != null ? profile.dawnHour : 5, profile != null ? profile.duskHour : 18);
        Apply(night, WeatherSystem.IsPlayerOutdoors(), Application.isMobilePlatform ? 4 : 8);
    }

    void Discover()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>()?.transform;
        // Authored street/house/shop lights already in the world keep their own colours/ranges.
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional || light.gameObject.scene != gameObject.scene ||
                registered.Contains(light.GetInstanceID())) continue;
            Register(light, false);
        }
        // Completed housing can appear after construction or Load. Anchors follow the actual door
        // rather than saved world axes, so relocated/rotated buildings still light their entrance.
        foreach (BarnExteriorAuthoring building in FindObjectsByType<BarnExteriorAuthoring>(FindObjectsSortMode.None))
            if (building.gameObject.scene == gameObject.scene) EnsureEntrance(building.Entrance);
        foreach (HouseScenePortal portal in FindObjectsByType<HouseScenePortal>(FindObjectsSortMode.None))
            if (portal.gameObject.scene == gameObject.scene) EnsureEntrance(portal.transform);
        foreach (ShopFront shop in FindObjectsByType<ShopFront>(FindObjectsSortMode.None))
            if (shop.gameObject.scene == gameObject.scene && shop.interactionEnabled) EnsureEntrance(shop.transform);
        lamps.RemoveAll(l => l.light == null);
    }

    void EnsureEntrance(Transform door)
    {
        if (door == null || door.Find("Night Entrance Light") != null) return;
        Vector3 position = door.position + Vector3.up * 1.85f + door.right * .9f + door.forward * .35f;
        foreach (Lamp lamp in lamps)
            if (lamp.light != null && Vector3.SqrMagnitude(lamp.light.transform.position - position) < 6.25f) return;
        var holder = new GameObject("Night Entrance Light");
        holder.transform.SetParent(door, false);
        holder.transform.position = position;
        Light light = holder.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .76f, .44f);
        light.intensity = .8f;
        light.range = 5f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        Register(light, true);
        light.enabled = false;
    }

    void Register(Light light, bool owned)
    {
        registered.Add(light.GetInstanceID());
        lamps.Add(new Lamp { light = light, owned = owned, wasEnabled = light.enabled, intensity = light.intensity });
    }

    void Apply(float night, bool outdoors, int budget)
    {
        nearest.Clear();
        Vector3 origin = player != null ? player.position : Camera.main != null ? Camera.main.transform.position : transform.position;
        foreach (Lamp lamp in lamps)
        {
            if (lamp.light == null) continue;
            lamp.light.enabled = false;
            if (!outdoors || night <= .001f || !lamp.light.gameObject.activeInHierarchy) continue;
            if (Vector3.SqrMagnitude(lamp.light.transform.position - origin) <= 1600f) nearest.Add(lamp);
        }
        nearest.Sort((a, b) => Vector3.SqrMagnitude(a.light.transform.position - origin)
            .CompareTo(Vector3.SqrMagnitude(b.light.transform.position - origin)));
        for (int i = 0; i < Mathf.Min(budget, nearest.Count); i++)
        {
            nearest[i].light.intensity = nearest[i].intensity * night;
            nearest[i].light.enabled = true;
        }
    }

    void OnDisable()
    {
        foreach (Lamp lamp in lamps)
            if (lamp.light != null)
            {
                lamp.light.enabled = !lamp.owned && lamp.wasEnabled;
                lamp.light.intensity = lamp.intensity;
            }
    }

    void OnDestroy()
    {
        foreach (Lamp lamp in lamps)
            if (lamp.owned && lamp.light != null) Destroy(lamp.light.gameObject);
    }
}
