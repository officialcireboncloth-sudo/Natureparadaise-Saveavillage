using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Placeholder loop peningkatan kondisi desa. Player menyerahkan material dan Gold,
/// menerima Condition Point, lalu VillageProgressionService menaikkan level otomatis.
/// </summary>
[DisallowMultipleComponent]
public sealed class VillageImprovementBoard : MonoBehaviour
{
    [SerializeField, Min(1f)] float interactionRadius = 4f;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode contributeKey = KeyCode.C;
    [SerializeField] KeyCode debugCompleteKey = KeyCode.F9;
    [SerializeField] ItemSO wood;
    [SerializeField] ItemSO stone;

    PlayerController player;
    Inventory inventory;
    bool panelOpen;
    Rect windowRect = new(0f, 0f, 560f, 430f);
    string feedback = "Pilih kontribusi untuk memperbaiki kondisi desa.";

    void Awake()
    {
        wood ??= Resources.Load<ItemSO>("Items/Materials/Wood");
        stone ??= Resources.Load<ItemSO>("Items/Materials/Stone");
    }

    void Update()
    {
        ResolvePlayer();
        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel();
            else if (Input.GetKeyDown(contributeKey)) Contribute(false);
            else if (Input.GetKeyDown(debugCompleteKey)) Contribute(true);
            return;
        }

        if (player == null) return;
        float distance = Vector3.Distance(player.transform.position, transform.position);
        if (distance > interactionRadius) return;
        VillageProgressionService village = VillageProgressionService.Instance;
        string level = village != null ? $"Lv.{village.VillageLevel}" : "--";
        WorldInteractionPrompt.Request(this, transform, $"{interactKey}: Village Improvement Board — {level}", distance, 1.7f);
        if (Input.GetKeyDown(interactKey)) OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "VILLAGE IMPROVEMENT — PLACEHOLDER");
    }

    void DrawWindow(int id)
    {
        VillageProgressionService village = VillageProgressionService.Instance;
        if (village == null)
        {
            GUILayout.Label("VillageProgressionService belum tersedia.");
            if (GUILayout.Button("CLOSE", GUILayout.Height(38f))) ClosePanel();
            GUI.DragWindow();
            return;
        }

        int level = village.VillageLevel;
        GUILayout.Label($"Actual Village Level: {level} / {village.MaximumLevel}");
        GUILayout.Label($"Effective Requirement Level: {village.EffectiveVillageLevel}");
        GUILayout.Label($"Condition: {village.ConditionPoints} / {(level >= village.MaximumLevel ? "MAX" : village.NextLevelThreshold.ToString())}");

        float progress = level >= village.MaximumLevel ? 1f : Mathf.InverseLerp(village.CurrentLevelFloor, village.NextLevelThreshold, village.ConditionPoints);
        Rect bar = GUILayoutUtility.GetRect(500f, 24f);
        GUI.Box(bar, string.Empty);
        Rect fill = new(bar.x + 2f, bar.y + 2f, Mathf.Max(0f, (bar.width - 4f) * progress), bar.height - 4f);
        GUI.Box(fill, $"{progress:P0}");

        GUILayout.Space(10f);
        if (level >= village.MaximumLevel)
        {
            GUILayout.Label("Semua proyek placeholder selesai. Kondisi desa sudah maksimal.");
        }
        else
        {
            GetRequirements(level, out int woodNeeded, out int stoneNeeded, out int goldNeeded, out int pointReward);
            int ownedWood = inventory != null && wood != null ? inventory.GetCount(wood) : 0;
            int ownedStone = inventory != null && stone != null ? inventory.GetCount(stone) : 0;
            int ownedGold = ScoreManager.Instance != null ? ScoreManager.Instance.points : 0;
            GUILayout.Label($"NEXT PROJECT: {ProjectName(level)}");
            GUILayout.Label($"Wood  {ownedWood}/{woodNeeded}     Stone  {ownedStone}/{stoneNeeded}     Gold  {ownedGold}/{goldNeeded}");
            GUILayout.Label($"Reward: +{pointReward} Village Condition Point");
            GUILayout.Space(8f);
            GUI.enabled = HasRequirements(woodNeeded, stoneNeeded, goldNeeded);
            if (GUILayout.Button($"CONTRIBUTE [{contributeKey}]", GUILayout.Height(42f))) Contribute(false);
            GUI.enabled = true;
            if (GUILayout.Button($"DEBUG COMPLETE FREE [{debugCompleteKey}]", GUILayout.Height(38f))) Contribute(true);
        }

        GUILayout.Space(8f);
        bool bypass = ProgressionRequirementSettings.BypassEnabled;
        GUILayout.Label(bypass
            ? "Requirement Bypass: ON — fitur lain membaca test Village Lv.99."
            : "Requirement Bypass: OFF — fitur lain membaca Actual Village Level.");
        if (GUILayout.Button(bypass ? "TEST ACTUAL REQUIREMENTS (SESSION)" : "BYPASS REQUIREMENTS (SESSION)", GUILayout.Height(34f)))
        {
            ProgressionRequirementSettings.SetRuntimeBypass(!bypass);
            feedback = !bypass ? "Requirement bypass aktif untuk sesi ini." : "Requirement aktual aktif untuk sesi ini.";
        }
        GUILayout.Label(feedback);
        if (GUILayout.Button("CLOSE", GUILayout.Height(34f))) ClosePanel();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void Contribute(bool debugFree)
    {
        VillageProgressionService village = VillageProgressionService.Instance;
        if (village == null || village.VillageLevel >= village.MaximumLevel) return;
        GetRequirements(village.VillageLevel, out int woodNeeded, out int stoneNeeded, out int goldNeeded, out int pointReward);
        if (!debugFree)
        {
            if (!HasRequirements(woodNeeded, stoneNeeded, goldNeeded))
            {
                feedback = "Material atau Gold belum cukup.";
                return;
            }
            inventory.Remove(wood, woodNeeded);
            inventory.Remove(stone, stoneNeeded);
            ScoreManager.Instance.TrySpendPoints(goldNeeded);
        }

        int oldLevel = village.VillageLevel;
        int reward = debugFree ? Mathf.Max(1, village.NextLevelThreshold - village.ConditionPoints) : pointReward;
        village.AddConditionPoints(reward, debugFree ? "Village Board DEBUG" : ProjectName(oldLevel));
        feedback = village.VillageLevel > oldLevel
            ? $"Proyek selesai. Village naik ke Lv.{village.VillageLevel}."
            : $"Kontribusi diterima: +{reward} Condition Point.";
    }

    bool HasRequirements(int woodNeeded, int stoneNeeded, int goldNeeded) =>
        inventory != null && wood != null && stone != null && ScoreManager.Instance != null &&
        inventory.GetCount(wood) >= woodNeeded && inventory.GetCount(stone) >= stoneNeeded &&
        ScoreManager.Instance.points >= goldNeeded;

    static void GetRequirements(int level, out int woodNeeded, out int stoneNeeded, out int goldNeeded, out int pointReward)
    {
        level = Mathf.Max(1, level);
        woodNeeded = 5 * level;
        stoneNeeded = 3 * level;
        goldNeeded = 100 * level;
        pointReward = 50 + (level - 1) * 25;
    }

    static string ProjectName(int level) => level switch
    {
        1 => "Perbaiki Papan dan Jalan Desa",
        2 => "Perbaiki Jembatan Desa",
        3 => "Renovasi Balai Desa",
        _ => "Peningkatan Desa"
    };

    void ResolvePlayer()
    {
        if (player != null && inventory != null) return;
        player = FindFirstObjectByType<PlayerController>();
        inventory = player != null ? player.GetComponent<Inventory>() : null;
        inventory ??= FindFirstObjectByType<Inventory>();
    }

    void OpenPanel()
    {
        panelOpen = true;
        windowRect.position = new Vector2((Screen.width - windowRect.width) * 0.5f, (Screen.height - windowRect.height) * 0.5f);
        player?.AcquireMovementLock(this);
        TimeManager.Instance?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
    }

    void ClosePanel()
    {
        if (!panelOpen) return;
        panelOpen = false;
        player?.ReleaseMovementLock(this);
        TimeManager.Instance?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    void OnDisable() => ClosePanel();
}

/// <summary>Membuat board dummy dekat NPCSeller saat scene Map dimuat.</summary>
public sealed class VillageImprovementBoardBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (FindFirstObjectByType<VillageImprovementBoardBootstrap>() != null) return;
        GameObject host = new("VillageImprovementBoardBootstrap_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<VillageImprovementBoardBootstrap>();
    }

    void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;
    void Start() => StartCoroutine(SpawnNextFrame(SceneManager.GetActiveScene()));
    void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(SpawnNextFrame(scene));

    IEnumerator SpawnNextFrame(Scene scene)
    {
        yield return null;
        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.name, "Map", System.StringComparison.OrdinalIgnoreCase) ||
            FindFirstObjectByType<VillageImprovementBoard>() != null) yield break;
        NPCSeller seller = FindFirstObjectByType<NPCSeller>();
        if (seller == null) yield break;

        Vector3 ground = seller.transform.position + seller.transform.right * 4f - seller.transform.forward * 3f;
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Bounds bounds = terrain.terrainData.bounds;
            bounds.center += terrain.transform.position;
            if (ground.x < bounds.min.x || ground.x > bounds.max.x || ground.z < bounds.min.z || ground.z > bounds.max.z) continue;
            ground.y = terrain.SampleHeight(ground) + terrain.transform.position.y;
            break;
        }

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "VillageImprovementBoard_Placeholder";
        board.transform.SetPositionAndRotation(ground + Vector3.up * 1.15f,
            Quaternion.Euler(0f, seller.transform.eulerAngles.y, 0f));
        board.transform.localScale = new Vector3(2.6f, 2.2f, 0.28f);
        Renderer renderer = board.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material material = new(shader) { name = "VillageBoard_Placeholder_Runtime" };
                material.color = new Color(0.24f, 0.12f, 0.045f, 1f);
                renderer.material = material;
            }
        }
        board.AddComponent<VillageImprovementBoard>();
        SceneManager.MoveGameObjectToScene(board, scene);
    }
}
