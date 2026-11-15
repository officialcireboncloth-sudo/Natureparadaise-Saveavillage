using UnityEngine;

/// <summary>Configured stylized shading shared by authored and generated opaque world props.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/World/Toon World Style")]
public sealed class ToonWorldStyle : ScriptableObject
{
    public Shader opaqueShader;
    public Texture2D lightingRamp;
    static ToonWorldStyle cached;
    public static ToonWorldStyle Current => cached != null ? cached : cached = Resources.Load<ToonWorldStyle>("World/Toon World Style");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => cached = null;

    public static Material CreateMaterial(Color color)
    {
        var style = Current;
        bool toon = color.a >= .99f && style != null && style.opaqueShader != null;
        var shader = toon ? style.opaqueShader : Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader) { color = color };
        if (toon)
        {
            if (material.HasProperty("_TextureRamp")) material.SetTexture("_TextureRamp", style.lightingRamp);
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
        }
        return material;
    }
}
