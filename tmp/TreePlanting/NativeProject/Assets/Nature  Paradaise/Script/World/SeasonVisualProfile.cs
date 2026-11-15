using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Nature Paradise/Season/Visual Profile")]
public sealed class SeasonVisualProfile : ScriptableObject
{
    public CropSeason season = CropSeason.Spring;

    [Header("Terrain and Vegetation")]
    [Tooltip("Kosong mempertahankan texture grass asli dan hanya menerapkan tint. Winter dapat memakai TL_Snow.")]
    public TerrainLayer grassLayerOverride;
    public Color terrainTint = Color.white;
    public Color vegetationTint = Color.white;
    [Range(0f, 1f)] public float dryness;
    [Range(0f, 1f)] public float snowAmount;

    [Header("Lighting")]
    public Color lightingTint = Color.white;
    [Min(0f)] public float sunMultiplier = 1f;
    [Min(0f)] public float ambientMultiplier = 1f;
    [Min(0f)] public float skyExposureMultiplier = 1f;
    [Tooltip("Indirect daylight fill, separate from the sun colour so shaded objects retain their colours.")]
    public Color ambientTint = Color.white;
    [Range(.5f, 1.2f)] public float shadowStrengthMultiplier = 1f;
    [Range(.65f, 1.1f)] public float sunElevationMultiplier = 1f;

    [Header("Season daylight hours")]
    [Range(4f, 9f)] public float sunriseHour = 6f;
    [Range(16f, 21f)] public float sunsetHour = 18f;

    [Header("Moon and night fill — independent of daytime warmth")]
    public Color nightTint = Color.white;
    [Range(.6f, 1.3f)] public float nightSunMultiplier = 1f;
    [Range(.6f, 1.3f)] public float nightAmbientMultiplier = 1f;
    [Range(.6f, 1.3f)] public float nightSkyExposureMultiplier = 1f;
}
