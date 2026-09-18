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
    TextMesh growthDebugLabel;
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

        RefreshGrowthDebug(state);
    }

    void LateUpdate()
    {
        if (growthDebugLabel == null || !growthDebugLabel.gameObject.activeInHierarchy) return;
        growthDebugLabel.transform.position = transform.position + Vector3.up * 1.35f;
        Camera camera = Camera.main;
        if (camera != null) growthDebugLabel.transform.rotation = camera.transform.rotation;
        Vector3 scale = transform.lossyScale;
        growthDebugLabel.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }

    void RefreshGrowthDebug(CropLifecycleState state)
    {
        bool isCabbage = definition != null &&
            (string.Equals(definition.cropId, "crop.cabbage", System.StringComparison.OrdinalIgnoreCase) ||
             definition.name.IndexOf("cabbage", System.StringComparison.OrdinalIgnoreCase) >= 0);
        bool visible = isCabbage && owner != null && owner.ShowCabbageGrowthDebug;
        if (!visible)
        {
            if (growthDebugLabel != null) growthDebugLabel.gameObject.SetActive(false);
            return;
        }

        if (growthDebugLabel == null)
        {
            GameObject labelObject = new("CabbageGrowthDebug_0-100");
            labelObject.transform.SetParent(transform, false);
            growthDebugLabel = labelObject.AddComponent<TextMesh>();
            growthDebugLabel.anchor = TextAnchor.LowerCenter;
            growthDebugLabel.alignment = TextAlignment.Center;
            growthDebugLabel.fontSize = 48;
            growthDebugLabel.characterSize = 0.075f;
            growthDebugLabel.fontStyle = FontStyle.Bold;
            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer != null) labelRenderer.sortingOrder = 5000;
        }

        growthDebugLabel.gameObject.SetActive(true);
        float growthDays = 0f;
        byte dryDays = 0;
        CropWaterSource waterSources = CropWaterSource.None;
        if (owner.TryGetSnapshot(gridX, gridZ, out FieldTileSnapshot snapshot))
        {
            growthDays = snapshot.GrowthDays;
            dryDays = snapshot.ConsecutiveDryDays;
            waterSources = snapshot.WaterSourcesToday;
        }
        int percent = state == CropLifecycleState.HarvestReady
            ? 100
            : Mathf.Clamp(Mathf.RoundToInt(growthDays / Mathf.Max(1f, definition.TotalGrowthDays) * 100f), 0, 99);
        int deathDay = Mathf.Max(definition.dryDaysBeforeWither + 1, definition.dryDaysBeforeDeath);
        string waterStatus = $"\nAIR: {GetWaterSourceLabel(waterSources)}";
        if (state == CropLifecycleState.Dead)
        {
            growthDebugLabel.text = "MATI\nBERSIHKAN: SICKLE";
            growthDebugLabel.color = new Color(1f, 0.18f, 0.12f);
        }
        else if (state == CropLifecycleState.Withered)
        {
            growthDebugLabel.text = $"{percent}%\nLAYU - KERING {dryDays}/{deathDay}{waterStatus}";
            growthDebugLabel.color = new Color(1f, 0.48f, 0.08f);
        }
        else
        {
            string dryStatus = dryDays > 0 ? $"\nKERING {dryDays}/{deathDay}" : string.Empty;
            growthDebugLabel.text = percent >= 100
                ? $"100%\nSIAP PANEN{dryStatus}{waterStatus}"
                : $"{percent}%{dryStatus}{waterStatus}";
            growthDebugLabel.color = dryDays > 0
                ? new Color(1f, 0.58f, 0.12f)
                : percent >= 100
                    ? new Color(0.35f, 1f, 0.25f)
                    : Color.Lerp(new Color(1f, 0.72f, 0.12f), Color.white, percent / 100f);
        }
    }

    /// <summary>Memperbarui clue tile setelah air, booster, atau cuaca mengubah data tanpa mengganti stage.</summary>
    public void RefreshDebug()
    {
        if (owner == null || !owner.TryGetSnapshot(gridX, gridZ, out FieldTileSnapshot snapshot))
            return;
        RefreshGrowthDebug(snapshot.CropState);
    }

    static string GetWaterSourceLabel(CropWaterSource sources)
    {
        if (sources == CropWaterSource.None) return "BELUM";

        System.Collections.Generic.List<string> labels = new(4);
        if ((sources & CropWaterSource.Rain) != 0) labels.Add("HUJAN");
        if ((sources & CropWaterSource.WateringCan) != 0) labels.Add("CAN");
        if ((sources & CropWaterSource.Sprinkler) != 0) labels.Add("SPRINKLER");
        if ((sources & CropWaterSource.External) != 0) labels.Add("EFEK");
        return string.Join("+", labels);
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
