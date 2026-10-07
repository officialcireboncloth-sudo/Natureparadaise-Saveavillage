using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/World/Terrain Vegetation Profile")]
public sealed class TerrainVegetationProfile : ScriptableObject
{
    [Serializable]
    public sealed class Variant
    {
        [Tooltip("Single mesh prefab. Setup can extract a Terrain detail prefab from a pack LOD prefab.")]
        public GameObject prefab;
        [Min(0)] public float weight = 1;
        public Vector2 widthScale = new(.9f, 1.15f);
        public Vector2 heightScale = new(.85f, 1.15f);
    }
    [Serializable]
    public sealed class LayerRule
    {
        public string name = "Grass";
        public bool enabled = true;
        public TerrainLayer terrainLayer;
        [Tooltip("Optional: match every Terrain Layer using this diffuse texture.")]
        public Texture2D diffuseTexture;
        [Range(0, 1)] public float minimumPaintWeight = .65f;
        [Tooltip("When enabled, density is the total number of clumps per square metre, shared between variants. Native coverage is calibrated for each mesh size.")]
        public bool useWorldDensity = true;
        [Min(0)] public float density = .14f;
        [Range(0, 90)] public float maximumSlope = 35;
        [Min(.1f)] public float clusterSize = 8;
        [Range(0, 1)] public float clusterVariation = .15f;
        public List<Variant> variants = new();
    }
    public int seed = 7331;
    [Min(1)] public float drawDistance = 80;
    [Range(0, 1)] public float densityMultiplier = 1;
    [Tooltip("Used only if terrain has no detail grid. Existing hand-painted details are preserved.")]
    public int initialResolution = 512;
    [Range(8, 128)] public int patchResolution = 16;
    public List<LayerRule> rules = new();
}
