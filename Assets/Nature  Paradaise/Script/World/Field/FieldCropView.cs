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
    Renderer cachedRenderer;
    MaterialPropertyBlock propertyBlock;
    int gridX;
    int gridZ;

    void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>Menghubungkan view dengan owner tile dan menerapkan stage awal.</summary>
    public void Initialize(FieldArea field, int x, int z, CropDataSO crop, int stage)
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

        ApplyStage(stage, crop);
    }

    /// <summary>Memperbarui visual tanaman tanpa menjalankan simulasi pertumbuhan sendiri.</summary>
    public void ApplyStage(int stage, CropDataSO crop)
    {
        definition = crop != null ? crop : definition;
        if (definition == null || owner == null)
            return;

        transform.localScale = definition.GetStageScale(stage) * owner.CellSize;

        if (cachedRenderer == null)
            return;

        cachedRenderer.GetPropertyBlock(propertyBlock);
        if (cachedRenderer.sharedMaterial != null && cachedRenderer.sharedMaterial.HasProperty("_Color"))
        {
            bool mature = stage >= definition.StageCount - 1;
            propertyBlock.SetColor("_Color", mature ? Color.yellow : Color.green);
        }
        cachedRenderer.SetPropertyBlock(propertyBlock);
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
