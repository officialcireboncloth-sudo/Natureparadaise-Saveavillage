using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Paint mask -> native instanced Terrain details. No GameObject per grass instance.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Terrain))]
public sealed class TerrainLayerVegetation : MonoBehaviour
{
    [Serializable] public struct Binding { public int prototype; public int rule; public int variant; }
    public TerrainVegetationProfile profile;
    [SerializeField, HideInInspector] List<Binding> bindings = new();
    [SerializeField, HideInInspector] List<int> ownedSlots = new();
    Terrain terrain;
    TerrainData authoredData, runtimeData;
    RectInt pending;
    bool hasPending;
    float rebuildAt;
    TerrainVegetationProfile matchedProfile;
    List<int>[] matchedLayers;
    public IReadOnlyList<Binding> Bindings => bindings;
    public void CopyManagedSlotsFrom(TerrainLayerVegetation source)
    {
        bindings = new List<Binding>(source.bindings);
        ownedSlots = new List<int>(source.ownedSlots);
    }

    void OnEnable()
    {
        terrain = GetComponent<Terrain>();
        CaptureLayerMatches();
        if (terrain.terrainData != null) EnsureWritableData();
        ApplyCulling();
        TerrainCallbacks.textureChanged += TextureChanged;
        TerrainCallbacks.heightmapChanged += HeightChanged;
    }
    void OnDisable()
    {
        TerrainCallbacks.textureChanged -= TextureChanged;
        TerrainCallbacks.heightmapChanged -= HeightChanged;
        if (runtimeData != null)
        {
            terrain.terrainData = authoredData;
            var collider = GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = authoredData;
            Destroy(runtimeData); runtimeData = null;
        }
    }
    void ApplyCulling()
    {
        if (terrain == null || profile == null) return;
        float distance = Mathf.Max(1, profile.drawDistance), density = Mathf.Clamp01(profile.densityMultiplier);
        if (!Mathf.Approximately(terrain.detailObjectDistance, distance)) terrain.detailObjectDistance = distance;
        if (!Mathf.Approximately(terrain.detailObjectDensity, density)) terrain.detailObjectDensity = density;
    }
    void Update()
    {
        ApplyCulling();
        if (hasPending && Time.realtimeSinceStartup >= rebuildAt)
        {
            hasPending = false;
            GenerateRegion(pending);
        }
    }
    void TextureChanged(Terrain changed, string textureName, RectInt region, bool synced)
    {
        if (changed != terrain || profile == null || textureName != TerrainData.AlphamapTextureName) return;
        var data = terrain.terrainData;
        QueueRegion(region, data.alphamapWidth, data.alphamapHeight);
    }
    void HeightChanged(Terrain changed, RectInt region, bool synced)
    {
        if (changed != terrain || profile == null) return;
        QueueRegion(region, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
    }
    void QueueRegion(RectInt region, int width, int height)
    {
        int res = terrain.terrainData.detailResolution;
        if (res == 0) return;
        int x0 = Mathf.Clamp(Mathf.FloorToInt((region.xMin - 1f) / width * res), 0, res);
        int y0 = Mathf.Clamp(Mathf.FloorToInt((region.yMin - 1f) / height * res), 0, res);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((region.xMax + 1f) / width * res), 0, res);
        int y1 = Mathf.Clamp(Mathf.CeilToInt((region.yMax + 1f) / height * res), 0, res);
        var dirty = new RectInt(x0, y0, x1 - x0, y1 - y0);
        if (hasPending) dirty = new RectInt(Mathf.Min(pending.xMin, x0), Mathf.Min(pending.yMin, y0),
            Mathf.Max(pending.xMax, x1) - Mathf.Min(pending.xMin, x0), Mathf.Max(pending.yMax, y1) - Mathf.Min(pending.yMin, y0));
        pending = dirty; hasPending = true; rebuildAt = Time.realtimeSinceStartup + .2f;
    }
    void EnsureWritableData()
    {
        if (!Application.isPlaying || runtimeData != null) return;
        authoredData = terrain.terrainData;
        runtimeData = Instantiate(authoredData);
        runtimeData.name = authoredData.name + " (runtime vegetation)";
        terrain.terrainData = runtimeData;
        var collider = GetComponent<TerrainCollider>();
        if (collider != null) collider.terrainData = runtimeData;
    }
    void CaptureLayerMatches()
    {
        if (profile == null || terrain == null || terrain.terrainData == null) return;
        matchedProfile = profile;
        matchedLayers = new List<int>[profile.rules.Count];
        var layers = terrain.terrainData.terrainLayers;
        for (int r = 0; r < profile.rules.Count; r++)
        {
            matchedLayers[r] = new List<int>(); var rule = profile.rules[r];
            for (int l = 0; l < layers.Length; l++)
                if (layers[l] != null && ((rule.terrainLayer != null && layers[l] == rule.terrainLayer) ||
                    (rule.diffuseTexture != null && layers[l].diffuseTexture == rule.diffuseTexture))) matchedLayers[r].Add(l);
        }
        // SeasonVisualController replaces layers with runtime colour/snow variants.
        // The painted channels remain the same; retain their authored indices through seasons.
    }

    [ContextMenu("Rebuild From Terrain Paint")]
    public void Rebuild()
    {
        terrain = GetComponent<Terrain>();
        if (profile == null || terrain.terrainData == null) return;
        if (!Application.isPlaying || matchedProfile != profile || matchedLayers == null || matchedLayers.Length != profile.rules.Count) CaptureLayerMatches();
        EnsureWritableData();
        var data = terrain.terrainData;
        if (data.detailResolution == 0)
            data.SetDetailResolution(Mathf.Clamp(profile.initialResolution, 32, 2048), Mathf.Clamp(profile.patchResolution, 8, 128));
        var prototypes = new List<DetailPrototype>(data.detailPrototypes);
        foreach (int slot in ownedSlots)
            if (slot >= 0 && slot < prototypes.Count) data.SetDetailLayer(0, 0, slot, new int[data.detailHeight, data.detailWidth]);
        bindings.Clear();
        int used = 0;
        for (int r = 0; r < profile.rules.Count; r++)
        {
            var rule = profile.rules[r];
            for (int v = 0; v < rule.variants.Count; v++)
            {
                var variant = rule.variants[v];
                if (variant.prefab == null || variant.weight <= 0) continue;
                var prototype = new DetailPrototype
                {
                    prototype = variant.prefab, usePrototypeMesh = true, useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = Mathf.Max(.01f, Mathf.Min(variant.widthScale.x, variant.widthScale.y)),
                    maxWidth = Mathf.Max(.01f, Mathf.Max(variant.widthScale.x, variant.widthScale.y)),
                    minHeight = Mathf.Max(.01f, Mathf.Min(variant.heightScale.x, variant.heightScale.y)),
                    maxHeight = Mathf.Max(.01f, Mathf.Max(variant.heightScale.x, variant.heightScale.y)),
                    noiseSeed = profile.seed + r * 101 + v, noiseSpread = .3f,
                    density = rule.useWorldDensity ? 1 : Mathf.Max(.01f, rule.density),
                    healthyColor = Color.white, dryColor = Color.white,
                    holeEdgePadding = .1f, positionJitter = 1, alignToGround = .4f
                };
                if (!prototype.Validate(out string message)) { Debug.LogWarning(message, this); continue; }
                int index;
                if (used < ownedSlots.Count && ownedSlots[used] < prototypes.Count)
                { index = ownedSlots[used]; prototypes[index] = prototype; }
                else { index = prototypes.Count; prototypes.Add(prototype); ownedSlots.Add(index); }
                used++;
                bindings.Add(new Binding { prototype = index, rule = r, variant = v });
            }
        }
        data.detailPrototypes = prototypes.ToArray();
        // When every slot is ours, store actual clump counts. Native coverage quantizes
        // low fractional densities; count mode retains the requested sparse distribution.
        // Never change scatter semantics of pre-existing unmanaged hand-painted details.
        if (bindings.Count > 0 && bindings.TrueForAll(b => profile.rules[b.rule].useWorldDensity) &&
            prototypes.Count == ownedSlots.Count && ownedSlots.TrueForAll(i => i >= 0 && i < prototypes.Count))
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
        if (data.detailScatterMode == DetailScatterMode.CoverageMode)
        {
            // Unity coverage is mesh-size dependent. Convert a world density once per rebuild
            // so different clump meshes do not multiply or skew the requested population.
            foreach (var binding in bindings)
            {
                var rule = profile.rules[binding.rule];
                if (!rule.useWorldDensity) continue;
                float coverage = data.ComputeDetailCoverage(binding.prototype);
                var prototype = prototypes[binding.prototype];
                prototype.density = Mathf.Max(.001f, rule.density / Mathf.Max(.0001f, coverage));
                prototypes[binding.prototype] = prototype;
            }
            data.detailPrototypes = prototypes.ToArray();
        }
        // InstanceCount supports existing hand-painted count layers without changing scatter mode.
        GenerateRegion(new RectInt(0, 0, data.detailWidth, data.detailHeight));
        ApplyCulling();
    }

    public void RefreshWorldBounds(Bounds bounds)
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        var data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        int x0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - origin.x) / data.size.x * data.detailWidth), 0, data.detailWidth);
        int y0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - origin.z) / data.size.z * data.detailHeight), 0, data.detailHeight);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x - origin.x) / data.size.x * data.detailWidth), 0, data.detailWidth);
        int y1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z - origin.z) / data.size.z * data.detailHeight), 0, data.detailHeight);
        GenerateRegion(new RectInt(x0, y0, x1-x0, y1-y0));
    }

    void GenerateRegion(RectInt region)
    {
        if (profile == null || region.width <= 0 || region.height <= 0 || bindings.Count == 0) return;
        EnsureWritableData();
        var data = terrain.terrainData;
        // Small dirty region reads only nearby paint pixels; full rebuild is an explicit editor action.
        int ax = Mathf.Max(0, Mathf.FloorToInt((float)region.xMin / data.detailWidth * data.alphamapWidth));
        int ay = Mathf.Max(0, Mathf.FloorToInt((float)region.yMin / data.detailHeight * data.alphamapHeight));
        int aw = Mathf.Min(data.alphamapWidth - ax, Mathf.CeilToInt((float)region.width / data.detailWidth * data.alphamapWidth) + 2);
        int ah = Mathf.Min(data.alphamapHeight - ay, Mathf.CeilToInt((float)region.height / data.detailHeight * data.alphamapHeight) + 2);
        var paint = data.GetAlphamaps(ax, ay, aw, ah);
        var exclusions = FindObjectsByType<TerrainVegetationExclusion>(FindObjectsSortMode.None);
        var fields = FindObjectsByType<FieldArea>(FindObjectsSortMode.None);
        if (matchedProfile != profile || matchedLayers == null || matchedLayers.Length != profile.rules.Count) CaptureLayerMatches();
        float area = data.size.x / data.detailWidth * data.size.z / data.detailHeight;
        Vector3 origin = terrain.transform.position;
        foreach (var binding in bindings)
        {
            if (binding.rule >= profile.rules.Count || binding.prototype >= data.detailPrototypes.Length) continue;
            var rule = profile.rules[binding.rule];
            if (binding.variant >= rule.variants.Count) continue;
            var map = new int[region.height, region.width];
            float sum = 0;
            foreach (var variant in rule.variants) if (variant.prefab != null) sum += Mathf.Max(0, variant.weight);
            float variantWeight = sum > 0 ? rule.variants[binding.variant].weight / sum : 0;
            for (int z = 0; z < region.height; z++) for (int x = 0; x < region.width; x++)
            {
                if (!rule.enabled || rule.density <= 0 || variantWeight <= 0) continue;
                int dx = region.x + x, dz = region.y + z;
                float u = (dx + .5f) / data.detailWidth, v = (dz + .5f) / data.detailHeight;
                int px = Mathf.Clamp(Mathf.FloorToInt(u * data.alphamapWidth) - ax, 0, aw - 1);
                int pz = Mathf.Clamp(Mathf.FloorToInt(v * data.alphamapHeight) - ay, 0, ah - 1);
                float mask = 0;
                foreach (int l in matchedLayers[binding.rule]) if (l < paint.GetLength(2)) mask += paint[pz, px, l];
                if (mask < rule.minimumPaintWeight || data.GetSteepness(u, v) > rule.maximumSlope) continue;
                Vector3 point = origin + new Vector3(u * data.size.x, data.GetInterpolatedHeight(u, v), v * data.size.z);
                if (Blocked(point, exclusions, fields)) continue;
                float noise = Mathf.PerlinNoise(point.x / Mathf.Max(.1f, rule.clusterSize) + profile.seed * .013f,
                    point.z / Mathf.Max(.1f, rule.clusterSize) + binding.rule * 13.1f);
                float expected = rule.density * area * mask * variantWeight * Mathf.Lerp(1, noise * 2, rule.clusterVariation);
                // Coverage mode stores 0..255 coverage; instance mode stores instances per cell.
                if (data.detailScatterMode == DetailScatterMode.CoverageMode)
                    map[z,x] = Mathf.Clamp(Mathf.RoundToInt(mask * variantWeight * Mathf.Lerp(1, noise * 2, rule.clusterVariation) * 255), 0, 255);
                else
                    map[z,x] = Mathf.Clamp(Mathf.FloorToInt(expected) + (Random01(dx, dz, profile.seed + binding.rule * 101 + binding.variant) < expected % 1 ? 1 : 0), 0, 16);
            }
            data.SetDetailLayer(region.x, region.y, binding.prototype, map);
        }
        terrain.Flush();
#if UNITY_EDITOR
        if (!Application.isPlaying) { UnityEditor.EditorUtility.SetDirty(data); UnityEditor.EditorUtility.SetDirty(this); }
#endif
    }
    static bool Blocked(Vector3 point, TerrainVegetationExclusion[] exclusions, FieldArea[] fields)
    {
        foreach (var exclusion in exclusions) if (exclusion.Contains(point)) return true;
        foreach (var field in fields)
        {
            var box = field.GetComponent<BoxCollider>();
            if (box == null) continue;
            Vector3 local = box.transform.InverseTransformPoint(point) - box.center;
            if (Mathf.Abs(local.x) < box.size.x * .5f + .4f && Mathf.Abs(local.z) < box.size.z * .5f + .4f) return true;
        }
        return false;
    }
    public static float Random01(int x, int z, int seed)
    {
        unchecked { uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041); h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffffff) / 16777216f; }
    }
}
