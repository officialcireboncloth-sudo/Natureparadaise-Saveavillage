using System.Collections.Generic;
using UnityEngine;

/// <summary>Mengaktifkan satu layout interior berdasarkan House Level yang telah selesai.</summary>
[DisallowMultipleComponent]
public sealed class HouseInteriorController : MonoBehaviour
{
    [Tooltip("Index 0 untuk Lv.1, index 1 untuk Lv.2, dan seterusnya.")]
    [SerializeField] List<GameObject> levelLayouts = new();

    void OnEnable() => HouseFeatureService.FeaturesChanged += RefreshLayout;
    void OnDisable() => HouseFeatureService.FeaturesChanged -= RefreshLayout;
    void Start() => RefreshLayout();

    /// <summary>Menyegarkan layout setelah load atau perubahan level rumah.</summary>
    public void RefreshLayout()
    {
        int level = PlayerHouseController.Instance != null
            ? PlayerHouseController.Instance.CurrentLevel
            : 1;
        for (int index = 0; index < levelLayouts.Count; index++)
            if (levelLayouts[index] != null)
                levelLayouts[index].SetActive(index == level - 1);
        EnsureInteractiveFixtures();
    }

    public void Configure(List<GameObject> layouts)
    {
        levelLayouts = layouts ?? new List<GameObject>();
        RefreshLayout();
    }

    void EnsureInteractiveFixtures()
    {
        if (!Application.isPlaying) return;
        foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            if (candidate.name == "Refrigerator_MeshSlot" && candidate.GetComponent<Refrigerator>() == null)
                candidate.gameObject.AddComponent<Refrigerator>();
    }
}
