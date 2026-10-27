using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Two bounded particle batches, sampled surface impacts and no per-drop physics.</summary>
[DisallowMultipleComponent]
public sealed class SoftRainEffect : MonoBehaviour
{
    struct Impact { public Vector3 point, normal; public float time, alpha; }
    readonly Impact[] impacts = new Impact[256];
    readonly RaycastHit[] surfaceHits = new RaycastHit[16];
    ParticleSystem drops, splashes;
    RainVisualProfile profile;
    int nextImpact;
    float strength, emissionDebt, probeDebt, elapsed;
    Vector3 lastCameraPosition;
    Camera previousCamera;
    System.Random random = new(18427);
    public int DropCount => drops != null ? drops.particleCount : 0;
    public int SplashCount => splashes != null ? splashes.particleCount : 0;
    public int LastSurfaceProbeCount { get; private set; }

    /// <summary>Resources carry the shader references into builds; no runtime material copies.</summary>
    public void Initialize(RainVisualProfile visuals)
    {
        profile = visuals;
        drops = CreateBatch("Short water drops", profile.dropMaterial, true);
        splashes = CreateBatch("Small surface splashes", profile.splashMaterial, false);
    }

    ParticleSystem CreateBatch(string label, Material material, bool falling)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0; main.startLifetime = 1; main.startSize = 1;
        main.maxParticles = falling
            ? (Application.isMobilePlatform ? profile.mobileDropLimit : profile.desktopDropLimit)
            : (Application.isMobilePlatform ? profile.mobileSplashLimit : profile.desktopSplashLimit);
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = false;
        var color = particles.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, falling ? .12f : .18f),
                new GradientAlphaKey(1, .5f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        if (!falling)
        {
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .3f, 1, 1));
        }
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = falling ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.HorizontalBillboard;
        renderer.velocityScale = 0; renderer.lengthScale = falling ? profile.lengthScale : 1;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        particles.Play();
        return particles;
    }

    /// <summary>Called by WeatherSystem. Settings only affect presentation, including Off.</summary>
    public void Tick(Camera camera, Transform player, WeatherType weather, bool outdoors, float deltaTime)
    {
        LastSurfaceProbeCount = 0;
        if (profile == null || camera == null || !outdoors || !isActiveAndEnabled)
        { Clear(); return; }
        if (previousCamera != camera || (camera.transform.position - lastCameraPosition).sqrMagnitude > 144)
            Clear();
        previousCamera = camera; lastCameraPosition = camera.transform.position;
        float dt = Mathf.Clamp(deltaTime, 0, .1f);
        elapsed += dt;
        int mode = Mathf.Clamp(GameplayUISettings.Current.rainEffects, 0, 2);
        if (mode == 0) { Clear(); return; }
        float density = profile.Density(weather, mode, Application.isMobilePlatform);
        strength = Mathf.MoveTowards(strength, profile.Rate(weather) * density,
            profile.stormRate * dt / Mathf.Max(.5f, profile.transitionSeconds));
        ProcessImpacts();
        emissionDebt = Mathf.Min(emissionDebt + strength * dt, 24);
        probeDebt = Mathf.Min(probeDebt + profile.surfaceProbesPerSecond * dt, 4);
        int probes = Mathf.Min(4, Mathf.FloorToInt(probeDebt));
        for (int i = 0; i < probes && emissionDebt >= 1; i++)
        {
            probeDebt -= 1;
            // A steady screen distribution keeps zoom changes from multiplying the particle count.
            Vector3 viewport = new(Range(.025f, .975f), Range(.035f, .965f), 0);
            Ray ray = camera.ViewportPointToRay(viewport);
            LastSurfaceProbeCount++;
            int hits = Physics.RaycastNonAlloc(ray, surfaceHits, 300, profile.surfaceMask, QueryTriggerInteraction.Ignore);
            int nearest = -1; float distance = float.MaxValue;
            for (int h = 0; h < hits; h++)
            {
                var hit = surfaceHits[h];
                if (hit.normal.y < .55f || hit.distance >= distance ||
                    (player != null && hit.collider.transform.IsChildOf(player))) continue;
                nearest = h; distance = hit.distance;
            }
            if (nearest < 0) continue;
            RaycastHit surface = surfaceHits[nearest];
            int cluster = Mathf.Min(5, Mathf.FloorToInt(emissionDebt));
            for (int j = 0; j < cluster; j++)
            {
                emissionDebt -= 1;
                // Reuse this sampled point, with staggered heights rather than inaccurate offset impacts.
                EmitDrop(surface.point, surface.normal, camera, player, weather, mode, j == 0);
            }
        }
    }

    void EmitDrop(Vector3 point, Vector3 normal, Camera camera, Transform player, WeatherType weather, int mode, bool splash)
    {
        Vector3 velocity = profile.Velocity(weather);
        float lifetime = profile.fallHeight * Range(.65f, 1) / -velocity.y;
        Vector3 start = point - velocity * lifetime;
        if (Vector3.Distance(camera.transform.position, start) < 3) return;
        float alpha = profile.opacity;
        if (mode == 1) alpha *= .85f;
        if (player != null)
        {
            Vector3 p = camera.WorldToViewportPoint(player.position + Vector3.up);
            Vector3 d = camera.WorldToViewportPoint(start);
            // Reduce visual clutter over the actor without carving a visible hole in the rain.
            if (Vector2.Distance(p, d) < .075f) alpha *= .35f;
        }
        var tint = profile.waterTint; tint.a = alpha;
        float width = profile.width * Range(.75f, 1.1f);
        float shortEdgeScale = Mathf.Max(.75f, Mathf.Min(camera.pixelWidth, camera.pixelHeight) / 720f);
        float depth = Mathf.Max(camera.nearClipPlane, Vector3.Dot(start - camera.transform.position, camera.transform.forward));
        float visibleHeight = camera.orthographic ? camera.orthographicSize * 2f :
            2f * depth * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
        // World-space drops retain depth/occlusion while staying at least about two pixels wide on small screens.
        float minimumWidth = profile.minimumDropWidthPixels * shortEdgeScale * visibleHeight / Mathf.Max(1, camera.pixelHeight);
        width = Mathf.Max(width, minimumWidth);
        var emit = new ParticleSystem.EmitParams { position = start, velocity = velocity,
            startLifetime = lifetime, startSize = width, startColor = (Color32)tint };
        drops.Emit(emit, 1);
        if (!splash) return;
        impacts[nextImpact] = new Impact { point = point + normal * .045f, normal = normal,
            time = elapsed + lifetime, alpha = alpha * .9f };
        nextImpact = (nextImpact + 1) % impacts.Length;
    }

    void ProcessImpacts()
    {
        for (int i = 0; i < impacts.Length; i++)
        {
            Impact impact = impacts[i];
            if (impact.time <= 0 || impact.time > elapsed) continue;
            var tint = profile.waterTint; tint.a = impact.alpha;
            var emit = new ParticleSystem.EmitParams { position = impact.point, velocity = Vector3.zero,
                startLifetime = Range(.18f, .3f), startSize = profile.splashSize * Range(.7f, 1.25f),
                startColor = (Color32)tint };
            splashes.Emit(emit, 1);
            impacts[i] = default;
        }
    }

    float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    void OnDisable() => Clear();

    void Clear()
    {
        drops?.Clear(); splashes?.Clear();
        System.Array.Clear(impacts, 0, impacts.Length);
        strength = emissionDebt = probeDebt = 0;
    }
}
