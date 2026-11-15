using UnityEngine;

/// <summary>Shared, editable worker and construction-site appearance. Durations remain in building data/CSV.</summary>
[CreateAssetMenu(menuName = "Game/Building/Construction Worker Settings")]
public sealed class ConstructionWorkerSettings : ScriptableObject
{
    [Header("Builder")]
    [Tooltip("Optional character prefab. An Animator can use Walking and Working bool parameters.")]
    public GameObject workerPrefab;
    public GameObject hammerPrefab;
    [Tooltip("Reusable Humanoid action IDs. Missing builder-specific clips use temporary contact poses.")]
    public HumanoidActionLibrary humanoidActions;
    [Min(.1f)] public float workerScale = 1f;
    [Min(.1f)] public float walkingSpeed = 2.4f;
    [Header("Working hours")]
    [Range(0, 23)] public int workStarts = 8;
    [Range(1, 24)] public int workEnds = 17;
    [Header("Site materials")]
    public Material wood, stone, roof, workerClothes, workerSkin, workerHat;
    [Header("Universal open-top kit (metres, atlas material on imported models)")]
    public GameObject tarpPanel, supportPost, workBench, lumberStack, toolCrate, workingBoard, sawPrefab;
    [Min(.1f)] public float panelWidth = 2f;
    [Min(.1f)] public float panelHeight = 2.4f;
    [Min(0)] public float siteClearance = .65f;
    [Tooltip("Tinggi pagar terpal, bukan tinggi atap bangunan. Struktur rendah tetap memakai pagar lebih rendah.")]
    [Range(1.2f,2.4f)] public float screenHeight = 1.8f;
    [Tooltip("Dua jalur keluar berlawanan selalu terbuka; tidak ada collider kotak penuh yang menjebak player.")]
    [Min(1.5f)] public float exitWidth = 2.2f;
    public bool screenCollision = true;
    [Range(.08f,.3f)] public float screenCollisionThickness = .16f;
    [Header("Completion presentation")]
    [Min(.5f)] public float revealSeconds = 2.6f;
    public Shader revealShader, dustShader;
    [Range(0, 2)] public float dustIntensity = .65f;
    [Header("World progress display")]
    [Tooltip("Overlay text shader referenced by this asset so player builds retain it.")]
    public Shader progressTextShader;
    [Min(5)] public float displayDistance = 42f;
    [Min(0)] public float barHeight = 1.2f;
    [Range(160, 420)] public float barPixelWidth = 260f;
    public static ConstructionWorkerSettings Load() => Resources.Load<ConstructionWorkerSettings>("Buildings/Construction Worker Settings");
    void OnValidate() => workEnds = Mathf.Max(workStarts + 1, workEnds);
}
