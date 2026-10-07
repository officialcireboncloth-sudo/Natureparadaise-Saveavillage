using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Field/Soil Visual Profile")]
public sealed class FieldSoilVisualProfile : ScriptableObject
{
    [Header("Field — empty farmable ground")]
    public Texture2D fieldTexture;
    [ColorUsage(false, true)] public Color fieldTint = new Color(1.3f, 1.1f, .85f);
    [Tooltip("Texture repeat size in field-local metres, independent of grid rotation and dimensions.")]
    [Min(.1f)] public float fieldTextureSize = 8;
    [Range(0, 1)] public float fieldTextureStrength = 1;
    [Header("Hoed — dry tile")]
    public Texture2D hoedTexture;
    [ColorUsage(false, true)] public Color hoedTint = new Color(1.3f, 1.15f, .95f);
    [Min(.1f)] public float hoedTextureSize = 2;
    [Range(0, .3f)] public float furrows = .09f;
    [Header("Hoed — watered tile")]
    public Texture2D wateredTexture;
    [Tooltip("Align the watered texture grooves with the dry texture. One turn is 90 degrees.")]
    [Range(0, 3)] public int wateredTextureQuarterTurns = 1;
    public Color wetMultiplier = new Color(.78f, .74f, .70f, 1);
    [Range(0, 1)] public float drySmoothness = .03f;
    [Range(0, 1)] public float wetSmoothness = .35f;
    [Tooltip("Wet texture, tint and gloss fade into dry hoed soil near each tile edge, in metres.")]
    [Range(.01f, .6f)] public float wetEdgeBlend = .28f;

    public void Apply(Material material, bool field)
    {
        var texture = field ? fieldTexture : hoedTexture;
        material.SetTexture("_GroundMap", texture);
        material.SetFloat("_TextureAmount", texture != null ? (field ? fieldTextureStrength : 1) : 0);
        material.SetColor("_BaseColor", field ? fieldTint : hoedTint);
        float size = Mathf.Max(.1f, field ? fieldTextureSize : hoedTextureSize);
        material.SetVector("_GroundUVTransform", new Vector4(1 / size, 1 / size, 0, 0));
        material.SetTexture("_WetGroundMap", wateredTexture);
        float angle = wateredTextureQuarterTurns * Mathf.PI * .5f;
        float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
        material.SetVector("_WetGroundUVRotation", new Vector4(cosine, -sine, sine, cosine));
        material.SetFloat("_WetTextureAmount", !field && wateredTexture != null ? 1 : 0);
        material.SetColor("_WetColorMultiplier", wetMultiplier);
        material.SetFloat("_Furrows", field ? 0 : furrows);
        material.SetFloat("_DrySmoothness", drySmoothness);
        material.SetFloat("_WetSmoothness", wetSmoothness);
        material.SetFloat("_WetEdgeWidth", wetEdgeBlend);
        material.SetFloat("_TileBlend", field ? 0 : 1);
        material.SetTexture("_FieldGroundMap", fieldTexture);
        material.SetColor("_FieldBaseColor", fieldTint);
        material.SetFloat("_FieldUVScale", 1 / Mathf.Max(.1f, fieldTextureSize));
        material.SetFloat("_FieldTextureAmount", fieldTexture != null ? fieldTextureStrength : 0);
    }
}
