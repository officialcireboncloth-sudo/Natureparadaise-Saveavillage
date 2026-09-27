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
}
