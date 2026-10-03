using System.Collections.Generic;
using UnityEngine;

/// <summary>Mengaktifkan satu layout interior berdasarkan House Level yang telah selesai.</summary>
[DisallowMultipleComponent]
public sealed class HouseInteriorController : MonoBehaviour
{
    [Tooltip("Index 0 untuk Lv.1, index 1 untuk Lv.2, dan seterusnya.")]
    [SerializeField] List<GameObject> levelLayouts = new();
    [Tooltip("Use the entrance and furniture authored in the scene without rebuilding or repositioning them.")]
    [SerializeField] bool useAuthoredSceneLayout;
    [Header("Authored Level Preview")]
    [SerializeField, Range(1,4)] int previewLevel = 1;
    [SerializeField] List<Transform> levelEntryPoints = new();
    [SerializeField] List<Transform> levelExitPoints = new();
    public int PreviewLevel => previewLevel;
    int runtimeDebugLevel;
    public int RuntimeDebugLevel => runtimeDebugLevel;
    public int ActiveLayoutLevel { get; private set; }
    public int AvailableLayoutCount => Mathf.Min(4,levelLayouts.Count);
    public bool IsPlayerInside
    {
        get
        {
            if(!isActiveAndEnabled)return false;
            var transition=SceneTransitionManager.Instance;
            return transition!=null&&transition.IsInsideInterior
                ? transition.CurrentInteriorSceneName==gameObject.scene.name
                : UnityEngine.SceneManagement.SceneManager.GetActiveScene()==gameObject.scene;
        }
    }


    public bool UsesAuthoredSceneLayout => useAuthoredSceneLayout;

    void OnEnable(){HouseFeatureService.FeaturesChanged += RefreshLayout;if(Application.isPlaying)RefreshLayout();}
    void OnDisable(){HouseFeatureService.FeaturesChanged -= RefreshLayout;runtimeDebugLevel=0;}
    void Start(){RefreshLayout();if(GetComponent<HouseInteriorDebugUI>()==null)gameObject.AddComponent<HouseInteriorDebugUI>();}

    public void PreviewLayout(int level)
    {
        previewLevel = Mathf.Clamp(level, 1, 4);
        if (!Application.isPlaying) ApplyLayout(previewLevel);
    }



    /// <summary>Menyegarkan layout setelah load atau perubahan level rumah.</summary>
    public void RefreshLayout()
    {
        int actualLevel = PlayerHouseController.Instance != null
            ? PlayerHouseController.Instance.CurrentLevel
            : 1;
        int level = ProgressionRequirementSettings.EffectiveHouseLevel(actualLevel);
        ApplyLayout(Application.isPlaying ? (runtimeDebugLevel>0?runtimeDebugLevel:level) : previewLevel);
    }

    /// <summary>Session-only visual preview; does not change house progression or save data.</summary>
    public bool SetDebugLayout(int level)
    {
        if(!Application.isPlaying||!IsPlayerInside||level<0||level>AvailableLayoutCount)return false;
        var transition=SceneTransitionManager.Instance;
        var player=FindFirstObjectByType<PlayerController>();
        if((transition!=null&&transition.IsTransitioning)||WorldInteractionPrompt.IsSuppressed||GameplayPauseMenu.BlocksGameplayInput||(player!=null&&player.IsMovementLocked))return false;
        runtimeDebugLevel=level;RefreshLayout();
        // Return to the authored entrance when shrinking/swapping layouts, avoiding furniture/walls.
        var entry=transform.Find("HouseInteriorEntrySpawn");
        if(player!=null&&entry!=null)
        {
            Physics.SyncTransforms();var capsule=player.GetComponent<CharacterController>();
            Vector3 target=entry.position;
            if(capsule!=null&&PlayerLifeCycle.TryResolveStandingPoint(target,capsule,transform,out var grounded))target=grounded;
            bool enabled=capsule!=null&&capsule.enabled;
            if(enabled)capsule.enabled=false;
            player.transform.SetPositionAndRotation(target,entry.rotation);
            Physics.SyncTransforms();if(enabled)capsule.enabled=true;
        }
        GameplayInput.ConsumeCurrentFrame();return true;
    }
    public void ClearDebugLayout(){runtimeDebugLevel=0;RefreshLayout();}
    void ApplyLayout(int level)
    {
        level = Mathf.Clamp(level, 1, Mathf.Max(1, levelLayouts.Count));
        ActiveLayoutLevel=level;
        for (int index = 0; index < levelLayouts.Count; index++)
            if (levelLayouts[index] != null)
                levelLayouts[index].SetActive(index == level - 1);
        if (levelEntryPoints.Count >= level && levelEntryPoints[level-1] != null)
        {
            Transform entry = transform.Find("HouseInteriorEntrySpawn");
            if (entry != null) entry.SetPositionAndRotation(levelEntryPoints[level-1].position, levelEntryPoints[level-1].rotation);
        }
        if (levelExitPoints.Count >= level && levelExitPoints[level-1] != null)
        {
            Transform exit = transform.Find("HouseInteriorExitDoor_Editable");
            if (exit != null) exit.SetPositionAndRotation(levelExitPoints[level-1].position, levelExitPoints[level-1].rotation);
        }
        if (!useAuthoredSceneLayout)
        {
            AlignEntranceForLevel(level);
            EnsureInteractiveFixtures();
        }
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

    public void ConfigureAuthoredLevels(List<GameObject> layouts, List<Transform> entries, List<Transform> exits)
    {
        useAuthoredSceneLayout = true;
        levelLayouts = layouts;
        levelEntryPoints = entries;
        levelExitPoints = exits;
        PreviewLayout(1);
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
