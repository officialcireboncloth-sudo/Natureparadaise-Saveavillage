using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Aktivasi prefab/particle musiman tanpa melakukan Instantiate saat musim berubah.</summary>
[DisallowMultipleComponent]
public sealed class SeasonalObjectTarget : MonoBehaviour
{
    static readonly HashSet<SeasonalObjectTarget> Active = new();
    [SerializeField] GameObject[] spring = Array.Empty<GameObject>();
    [SerializeField] GameObject[] summer = Array.Empty<GameObject>();
    [SerializeField] GameObject[] autumn = Array.Empty<GameObject>();
    [SerializeField] GameObject[] winter = Array.Empty<GameObject>();

    void OnEnable()
    {
        Active.Add(this);
        Apply(SeasonVisualController.CurrentSeason);
    }

    void OnDisable() => Active.Remove(this);

    public void Apply(CropSeason season)
    {
        Set(spring, season == CropSeason.Spring);
        Set(summer, season == CropSeason.Summer);
        Set(autumn, season == CropSeason.Autumn);
        Set(winter, season == CropSeason.Winter);
    }

    static void Set(GameObject[] objects, bool active)
    {
        foreach (GameObject value in objects ?? Array.Empty<GameObject>())
            if (value != null && value.activeSelf != active) value.SetActive(active);
    }

    internal static void ApplyAll(CropSeason season)
    {
        foreach (SeasonalObjectTarget target in Active)
            if (target != null) target.Apply(season);
    }
}
