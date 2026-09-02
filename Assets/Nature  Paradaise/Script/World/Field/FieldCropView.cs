using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// View ringan satu tanaman field. Pertumbuhan dihitung terpusat oleh <see cref="FieldArea"/>
/// sehingga komponen ini hanya memperbarui mesh, skala, dan stage visual.
/// </summary>
public sealed class FieldCropView : MonoBehaviour
{
    FieldArea owner;
    CropDataSO definition;
    Renderer[] baseRenderers;
    MaterialPropertyBlock propertyBlock;
    GameObject stageVisual;
    GameObject activeStagePrefab;
    int gridX;
    int gridZ;

    void Awake()
    {
        baseRenderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>Menghubungkan view dengan owner tile dan menerapkan stage awal.</summary>
    public void Initialize(
        FieldArea field,
        int x,
        int z,
        CropDataSO crop,
        int stage,
        CropLifecycleState state = CropLifecycleState.Growing)
    {
        owner = field;
        gridX = x;
        gridZ = z;
        definition = crop;

        // The prototype crop component uses Update per plant. FieldArea owns
        // growth now, so the legacy component is kept for reference but disabled.
        CropInstance legacyCrop = GetComponent<CropInstance>();
        if (legacyCrop != null)
            legacyCrop.enabled = false;

        ApplyStage(stage, crop, state);
    }

    /// <summary>Memperbarui visual tanaman tanpa menjalankan simulasi pertumbuhan sendiri.</summary>
    public void ApplyStage(
        int stage,
        CropDataSO crop,
        CropLifecycleState state = CropLifecycleState.Growing)
    {
        definition = crop != null ? crop : definition;
        if (definition == null || owner == null)
            return;

        transform.localScale = definition.GetStageScale(stage) * owner.CellSize;

        GameObject stagePrefab = definition.GetStagePrefab(stage);
        if (activeStagePrefab != stagePrefab)
        {
            if (stageVisual != null)
                Destroy(stageVisual);
            stageVisual = stagePrefab != null ? Instantiate(stagePrefab, transform) : null;
            if (stageVisual != null)
            {
                stageVisual.name = $"Stage_{stage}_{definition.GetStageName(stage)}";
                stageVisual.transform.SetLocalPositionAndRotation(
                    definition.GetStageOffset(stage),
                    definition.GetStageRotation(stage)
                );
                stageVisual.transform.localScale = Vector3.one;
            }
            activeStagePrefab = stagePrefab;

            if (baseRenderers != null)
                foreach (Renderer renderer in baseRenderers)
                    if (renderer != null) renderer.enabled = stagePrefab == null;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;
            renderer.GetPropertyBlock(propertyBlock);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
            {
                bool mature = stage >= definition.StageCount - 1;
                Color cropColor = state switch
                {
                    CropLifecycleState.Withered => new Color(0.48f, 0.34f, 0.16f),
                    CropLifecycleState.Dead => new Color(0.22f, 0.18f, 0.14f),
                    _ => mature ? Color.yellow : Color.green
                };
                propertyBlock.SetColor("_Color", cropColor);
            }
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>Meneruskan permintaan harvest ke FieldArea pemilik tile.</summary>
    public bool TryHarvest(Inventory inventory, out CropGrade grade)
    {
        if (owner == null)
        {
            grade = CropGrade.D;
            return false;
        }

        return owner.TryHarvest(gridX, gridZ, inventory, out grade);
    }
}
