using UnityEngine;

/// <summary>Shared, editable worker and construction-site appearance. Durations remain in building data/CSV.</summary>
[CreateAssetMenu(menuName = "Game/Building/Construction Worker Settings")]
public sealed class ConstructionWorkerSettings : ScriptableObject
{
    [Header("Builder")]
    [Tooltip("Optional character prefab. An Animator can use Walking and Working bool parameters.")]
    public GameObject workerPrefab;
    public GameObject hammerPrefab;
    [Min(.1f)] public float workerScale = 1f;
    [Min(.1f)] public float walkingSpeed = 2.4f;
    [Header("Working hours")]
    [Range(0, 23)] public int workStarts = 8;
    [Range(1, 24)] public int workEnds = 17;
    [Header("Site materials")]
    public Material wood, stone, roof, workerClothes, workerSkin, workerHat;
    [Header("World progress display")]
    [Tooltip("Overlay text shader referenced by this asset so player builds retain it.")]
    public Shader progressTextShader;
    [Min(5)] public float displayDistance = 42f;
    [Min(0)] public float barHeight = 1.2f;
    [Range(160, 420)] public float barPixelWidth = 260f;
    public static ConstructionWorkerSettings Load() => Resources.Load<ConstructionWorkerSettings>("Buildings/Construction Worker Settings");
    void OnValidate() => workEnds = Mathf.Max(workStarts + 1, workEnds);
}
