using UnityEngine;

[CreateAssetMenu(menuName = "Nature Paradise/Field/Soil Visual Profile")]
public sealed class FieldSoilVisualProfile : ScriptableObject
{
    [Header("Cozy soil palette")]
    [Tooltip("Use the chosen colours as the palette; textures supply subtle detail instead of overriding the soil colour.")]
    public bool useCozyPalette;
    [Range(0, .5f)] public float textureContrast = .16f;
    [Range(0, .15f)] public float soilVariation = .055f;
    [Tooltip("Width of the field-to-terrain transition in metres.")]
    [Range(.05f, 2)] public float fieldFeather = .4f;
    [Header("Tile shape (fraction of cell size)")]
    [Range(.02f, .16f)] public float tileInset = .055f;
    [Range(.02f, .2f)] public float tileFeather = .065f;
    [Range(.05f, .3f)] public float tileCornerRadius = .18f;
    [Header("Surface drying — independent of crop-care flags")]
    [Tooltip("Dry moisture for old-save migration. The difference to Fully Wet Moisture also sets how long surface water evaporates: 35 to 70 with evaporation 2 gives 17.5 game hours. Crop moisture rules remain unchanged.")]
    [Range(0, 99)] public float dryMoisture = 35;
    [Tooltip("Wet moisture for old saves and drying duration. Every new watering starts with the darkest surface, including watering bone-dry soil.")]
    [Range(1, 100)] public float fullyWetMoisture = 70;

    public float VisualWetness(float moisture) => Mathf.SmoothStep(0, 1,
        Mathf.InverseLerp(dryMoisture, Mathf.Max(dryMoisture + 1, fullyWetMoisture), moisture));

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
    [Range(.01f, .6f)] public float wetEdgeBlend = .13f;

    public void Apply(Material material, bool field)
    {
        material.SetFloat("_CozyPalette", useCozyPalette ? 1 : 0);
        material.SetFloat("_TextureContrast", textureContrast);
        material.SetFloat("_SoilVariation", soilVariation);
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
