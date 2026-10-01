using UnityEngine;

/// <summary>
/// Menampilkan pertumbuhan ulang wol pada model domba pack. Pack tidak menyediakan mesh
/// domba tercukur terpisah, jadi tahap awal memakai tint kulit pink lalu kembali ke warna
/// material asli secara bertahap selama 12 hari.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AnimalGrowthSystem))]
public sealed class SheepWoolVisual : MonoBehaviour
{
    [SerializeField] Color freshlyShearedTint = new(1f, 0.48f, 0.58f, 1f);
    [SerializeField, Range(0f, 1f)] float maximumSkinTint = 0.82f;

    AnimalGrowthSystem growth;
    Renderer[] renderers;
    Color[] originalColors;
    MaterialPropertyBlock block;

    void Awake()
    {
        growth = GetComponent<AnimalGrowthSystem>();
        CacheRenderers();
    }

    void OnEnable()
    {
        TimeManager.OnDay += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        TimeManager.OnDay -= Refresh;
        RestoreOriginalColors();
    }

    public void Refresh()
    {
        if (growth == null || growth.Type != AnimalType.Sheep) return;
        if (renderers == null || renderers.Length == 0) CacheRenderers();
        float skinAmount = (1f - growth.SheepWoolGrowth01) * maximumSkinTint;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null) continue;
            block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            Color color = Color.Lerp(originalColors[i], freshlyShearedTint, skinAmount);
            if (HasColorProperty(target, "_BaseColor")) block.SetColor("_BaseColor", color);
            else if (HasColorProperty(target, "_Color")) block.SetColor("_Color", color);
            target.SetPropertyBlock(block);
        }
    }

    void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
            originalColors[i] = material == null ? Color.white :
                material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        }
    }

    void RestoreOriginalColors()
    {
        if (renderers == null || originalColors == null) return;
        for (int i = 0; i < renderers.Length && i < originalColors.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null) continue;
            block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            if (HasColorProperty(target, "_BaseColor")) block.SetColor("_BaseColor", originalColors[i]);
            else if (HasColorProperty(target, "_Color")) block.SetColor("_Color", originalColors[i]);
            target.SetPropertyBlock(block);
        }
    }

    static bool HasColorProperty(Renderer renderer, string property)
    {
        Material material = renderer != null ? renderer.sharedMaterial : null;
        return material != null && material.HasProperty(property);
    }
}
