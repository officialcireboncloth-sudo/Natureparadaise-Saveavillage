using UnityEngine;

/// <summary>Membangun arena test fishing mandiri dari scene kosong agar mudah diganti artist.</summary>
[ExecuteAlways]
public sealed class FishingTestSceneBootstrap : MonoBehaviour
{
    [SerializeField] Color groundColor = new(0.28f, 0.55f, 0.24f);
    [SerializeField] Color bankColor = new(0.48f, 0.34f, 0.19f);
    [SerializeField] Color waterColor = new(0.12f, 0.55f, 0.82f, 0.82f);

    void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= EnsureEditorLayout;
            UnityEditor.EditorApplication.delayCall += EnsureEditorLayout;
            return;
        }
#endif
        EnsureLayout();
    }

#if UNITY_EDITOR
    void OnDisable() => UnityEditor.EditorApplication.delayCall -= EnsureEditorLayout;

    void EnsureEditorLayout()
    {
        if (this == null || Application.isPlaying) return;
        bool alreadyBuilt = transform.Find("Fishing_Test_Layout_Editable") != null;
        EnsureLayout();
        if (!alreadyBuilt && transform.Find("Fishing_Test_Layout_Editable") != null)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    /// <summary>Membuat hierarchy editable sekali; aman dipanggil lagi saat scene dibuka.</summary>
    public void EnsureLayout()
    {
        if (transform.Find("Fishing_Test_Layout_Editable") != null) return;
        // Menangani Play Mode yang sudah berjalan memakai versi bootstrap lama.
        // Jangan membuat arena kedua di samping hierarchy runtime yang sudah ada.
        if (Application.isPlaying && FindFirstObjectByType<PlayerController>() != null) return;

        Transform layout = new GameObject("Fishing_Test_Layout_Editable").transform;
        layout.SetParent(transform, false);

        Material grass = MakeMaterial("FishingTest_Grass", groundColor);
        Material bank = MakeMaterial("FishingTest_Bank", bankColor);
        Material waterMaterial = MakeMaterial("FishingTest_Water", waterColor);

        Transform environment = new GameObject("30_WORLD").transform;
        environment.SetParent(layout, false);
        BuildGround(environment, grass, bank);
        GameObject water = Cube("Fishing_Water_Pond", environment, new Vector3(0f, -0.42f, 7f), new Vector3(10f, 0.25f, 8f), waterMaterial);
        water.name = "Fishing_Water_Pond";
        water.AddComponent<FishingSpot>();

        Transform cameraLighting = new GameObject("20_CAMERA_LIGHTING").transform;
        cameraLighting.SetParent(layout, false);
        GameObject cameraObject = new("Main Camera");
        cameraObject.transform.SetParent(cameraLighting, false);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 11f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();
        TopDownCameraFollow follow = cameraObject.AddComponent<TopDownCameraFollow>();

        Transform playerGroup = new GameObject("10_PLAYER").transform;
        playerGroup.SetParent(layout, false);
        GameObject player = new("Player_FishingTest");
        player.transform.SetParent(playerGroup, false);
        player.name = "Player_FishingTest";
        player.SetActive(false);
        player.transform.position = new Vector3(0f, 0.05f, -2.7f);
        player.transform.rotation = Quaternion.identity;

        GameObject playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerVisual.name = "Visual_Dummy_Editable";
        playerVisual.transform.SetParent(player.transform, false);
        playerVisual.transform.localPosition = Vector3.up;
        CapsuleCollider primitiveCollider = playerVisual.GetComponent<CapsuleCollider>();
        if (primitiveCollider != null) primitiveCollider.enabled = false;
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.45f;
        controller.center = Vector3.up;
        Inventory inventory = player.AddComponent<Inventory>();
        player.AddComponent<PlayerToolHotbar>();
        player.AddComponent<PlayerStatusSystem>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerGatheringTool>();
        player.AddComponent<PlayerAnimalCarry>();
        player.AddComponent<FishingSystem>();
        player.AddComponent<InventoryHotbarUI>();
        player.SetActive(true);

        ItemSO rod = Resources.Load<ItemSO>("Items/Tools/Fishing Rod");
        if (rod != null) inventory.TrySetSlot(0, rod, 1);
        ItemSO[] bait = Resources.LoadAll<ItemSO>("Items/Fishing/Bait");
        System.Array.Sort(bait, (left, right) => left.fishingBaitLevel.CompareTo(right.fishingBaitLevel));
        for (int i = 0; i < bait.Length && i < 4; i++) inventory.TrySetSlot(i + 1, bait[i], 20);
        follow.SetTarget(player.transform, true);

        GameObject systems = new("00_SYSTEMS");
        systems.transform.SetParent(layout, false);
        systems.AddComponent<TimeManager>();
        systems.AddComponent<ScoreManager>();
        systems.AddComponent<WeatherSystem>();

        GameObject lightObject = new("Sun_Test");
        lightObject.transform.SetParent(cameraLighting, false);
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        CreateSign(environment, new Vector3(0f, 0.9f, -5.1f), "FISHING TEST\n1: Rod | 2-5: Bait + F equip | F: Cast/Hook | Hold F: Reel | O: Fishdex");
    }

    void BuildGround(Transform root, Material grass, Material bank)
    {
        // Empat slab membentuk lubang sungguhan; raycast fishing tidak akan salah membaca tanah di bawah air.
        Cube("Ground_Left", root, new Vector3(-10.5f, -0.5f, 5f), new Vector3(11f, 1f, 28f), grass);
        Cube("Ground_Right", root, new Vector3(10.5f, -0.5f, 5f), new Vector3(11f, 1f, 28f), grass);
        Cube("Ground_PlayerSide", root, new Vector3(0f, -0.5f, -5f), new Vector3(10f, 1f, 8f), grass);
        Cube("Ground_Back", root, new Vector3(0f, -0.5f, 17f), new Vector3(10f, 1f, 12f), grass);
        Cube("Bank_Front", root, new Vector3(0f, 0.15f, 2f), new Vector3(10.8f, 0.3f, 0.5f), bank);
        Cube("Bank_Back", root, new Vector3(0f, 0.15f, 11.9f), new Vector3(10.8f, 0.3f, 0.5f), bank);
        Cube("Bank_Left", root, new Vector3(-5.15f, 0.15f, 7f), new Vector3(0.5f, 0.3f, 10f), bank);
        Cube("Bank_Right", root, new Vector3(5.15f, 0.15f, 7f), new Vector3(0.5f, 0.3f, 10f), bank);
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = name;
        target.transform.SetParent(parent, false);
        target.transform.position = position;
        target.transform.localScale = scale;
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return target;
    }

    static Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new(shader) { name = name, color = color };
        if (color.a < 0.99f)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = 3000;
        }
        return material;
    }

    static void CreateSign(Transform parent, Vector3 position, string text)
    {
        GameObject sign = new("Fishing_Test_Instructions");
        sign.transform.SetParent(parent, false);
        sign.transform.position = position;
        TextMesh label = sign.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.characterSize = 0.08f;
        label.color = Color.white;
        sign.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
    }
}
