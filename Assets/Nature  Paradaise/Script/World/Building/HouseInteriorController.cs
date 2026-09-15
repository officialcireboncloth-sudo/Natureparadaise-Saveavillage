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
        int actualLevel = PlayerHouseController.Instance != null
            ? PlayerHouseController.Instance.CurrentLevel
            : 1;
        int level = ProgressionRequirementSettings.EffectiveHouseLevel(actualLevel);
        level = Mathf.Clamp(level, 1, Mathf.Max(1, levelLayouts.Count));
        for (int index = 0; index < levelLayouts.Count; index++)
            if (levelLayouts[index] != null)
                levelLayouts[index].SetActive(index == level - 1);
        AlignEntranceForLevel(level);
        EnsureInteractiveFixtures();
    }

    void AlignEntranceForLevel(int level)
    {
        float depth = Mathf.Clamp(level, 1, 4) switch
        {
            1 => 10f,
            2 => 14f,
            3 => 17f,
            _ => 20f
        };
        Transform entry = transform.Find("HouseInteriorEntrySpawn");
        if (entry != null)
            entry.localPosition = new Vector3(0f, 1.15f, -depth * 0.5f + 1.6f);
        Transform exitDoor = transform.Find("HouseInteriorExitDoor_Editable");
        if (exitDoor != null)
        {
            exitDoor.localPosition = new Vector3(0f, 1.2f, -depth * 0.5f + 0.3f);
            exitDoor.localScale = new Vector3(2.4f, 2.4f, 0.25f);
        }
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
        {
            if (candidate.name == "Refrigerator_MeshSlot" && candidate.GetComponent<Refrigerator>() == null)
                candidate.gameObject.AddComponent<Refrigerator>();
            if (candidate.name == "Kitchen_MeshSlot" && candidate.GetComponent<KitchenSet>() == null)
                candidate.gameObject.AddComponent<KitchenSet>();
            if (candidate.name == "Aquarium_TestFurniture_Editable")
            {
                Aquarium aquarium = candidate.GetComponent<Aquarium>();
                if (aquarium == null) aquarium = candidate.gameObject.AddComponent<Aquarium>();
                aquarium.Configure("house.aquarium.test.main", AquariumSize.Medium);
            }
        }
    }
}
