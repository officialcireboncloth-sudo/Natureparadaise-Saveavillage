using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SeasonalRendererSlot
{
    public Renderer target;
    public Material[] spring;
    public Material[] summer;
    public Material[] autumn;
    public Material[] winter;

    public void Apply(CropSeason season)
    {
        if (target == null) return;
        Material[] selected = season switch
        {
            CropSeason.Summer => summer,
            CropSeason.Autumn => autumn,
            CropSeason.Winter => winter,
            _ => spring
        };
        if (selected != null && selected.Length > 0) target.sharedMaterials = selected;
    }
}

/// <summary>
/// Target modular untuk object yang benar-benar memerlukan material berbeda per musim.
/// sharedMaterials menjaga GPU instancing dan tidak membuat material instance per renderer.
/// </summary>
[DisallowMultipleComponent]
public sealed class SeasonalRendererTarget : MonoBehaviour
{
    static readonly HashSet<SeasonalRendererTarget> Active = new();
    [SerializeField] SeasonalRendererSlot[] renderers = Array.Empty<SeasonalRendererSlot>();

    void OnEnable()
    {
        Active.Add(this);
        Apply(SeasonVisualController.CurrentSeason);
    }

    void OnDisable() => Active.Remove(this);

    public void Apply(CropSeason season)
    {
        foreach (SeasonalRendererSlot slot in renderers ?? Array.Empty<SeasonalRendererSlot>())
            slot?.Apply(season);
    }

    internal static void ApplyAll(CropSeason season)
    {
        foreach (SeasonalRendererTarget target in Active)
            if (target != null) target.Apply(season);
    }
}
