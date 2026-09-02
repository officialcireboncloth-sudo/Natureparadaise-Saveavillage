using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transisi asynchronous antara world dan interior. Interior dimuat additive agar seluruh
/// state world yang belum disimpan tidak reset; API portal tetap kompatibel dengan streaming nanti.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    const string RuntimeObjectName = "SceneTransitionManager_Runtime";

    Canvas fadeCanvas;
    Image fadeImage;
    PlayerController player;
    CharacterController characterController;
    TopDownCameraFollow cameraFollow;
    Vector3 returnPosition;
    Quaternion returnRotation;
    string loadedInteriorScene;
    bool transitioning;

    public bool IsTransitioning => transitioning;
    public bool IsInsideInterior => !string.IsNullOrEmpty(loadedInteriorScene);

    /// <summary>Mengambil posisi world terakhir tanpa memindahkan player keluar dari interior.</summary>
    public bool TryGetWorldReturnPosition(out Vector3 position)
    {
        position = returnPosition;
        return IsInsideInterior;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Memuat scene interior dan memindahkan player ke Spawn ID tujuan.</summary>
    public bool EnterInterior(string sceneName, string targetSpawnId)
    {
        if (transitioning || IsInsideInterior || string.IsNullOrWhiteSpace(sceneName))
            return false;
        StartCoroutine(EnterRoutine(sceneName, targetSpawnId));
        return true;
    }

    /// <summary>Keluar dari interior aktif dan mengembalikan player ke Spawn ID exterior.</summary>
    public bool ReturnToWorld(string exteriorSpawnId)
    {
        if (transitioning || !IsInsideInterior)
            return false;
        StartCoroutine(ReturnRoutine(exteriorSpawnId));
        return true;
    }

    IEnumerator EnterRoutine(string sceneName, string targetSpawnId)
    {
        transitioning = true;
        ResolvePlayer();
        if (player == null)
        {
            transitioning = false;
            yield break;
        }

        returnPosition = player.transform.position;
        returnRotation = player.transform.rotation;
        player.AcquireMovementLock(this);
        TimeManager.Instance?.AcquirePause(this);
        yield return Fade(1f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            ReleaseTransitionLocks();
            yield break;
        }
        while (!load.isDone)
            yield return null;

        loadedInteriorScene = sceneName;
        Scene interior = SceneManager.GetSceneByName(sceneName);
        if (interior.IsValid())
            SceneManager.SetActiveScene(interior);
        yield return null; // Memberi PlayerSpawnPoint satu frame untuk mendaftarkan ID.
        MovePlayerToSpawn(targetSpawnId, returnPosition, returnRotation);
        yield return Fade(0f);
        ReleaseTransitionLocks();
    }

    IEnumerator ReturnRoutine(string exteriorSpawnId)
    {
        transitioning = true;
        ResolvePlayer();
        player?.AcquireMovementLock(this);
        TimeManager.Instance?.AcquirePause(this);
        yield return Fade(1f);

        Scene interior = SceneManager.GetSceneByName(loadedInteriorScene);
        Scene worldScene = FindLoadedWorldScene(interior);
        if (worldScene.IsValid())
            SceneManager.SetActiveScene(worldScene);

        MovePlayerToSpawn(exteriorSpawnId, returnPosition, returnRotation);
        AsyncOperation unload = interior.IsValid() ? SceneManager.UnloadSceneAsync(interior) : null;
        if (unload != null)
            while (!unload.isDone)
                yield return null;
        loadedInteriorScene = null;

        yield return Fade(0f);
        ReleaseTransitionLocks();
    }

    void ReleaseTransitionLocks()
    {
        player?.ReleaseMovementLock(this);
        TimeManager.Instance?.ReleasePause(this);
        transitioning = false;
    }

    void ResolvePlayer()
    {
        if (player == null)
        {
            Inventory inventory = FindFirstObjectByType<Inventory>();
            player = inventory != null ? inventory.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
            characterController = player != null ? player.GetComponent<CharacterController>() : null;
        }

        if (cameraFollow == null)
        {
            Camera mainCamera = Camera.main;
            cameraFollow = mainCamera != null
                ? mainCamera.GetComponent<TopDownCameraFollow>()
                : FindFirstObjectByType<TopDownCameraFollow>();
        }
    }

    void MovePlayerToSpawn(string spawnId, Vector3 fallbackPosition, Quaternion fallbackRotation)
    {
        if (player == null)
            return;
        Vector3 position = fallbackPosition;
        Quaternion rotation = fallbackRotation;
        if (PlayerSpawnPoint.TryGet(spawnId, out PlayerSpawnPoint spawn))
        {
            position = spawn.transform.position;
            rotation = spawn.transform.rotation;
        }
        if (characterController != null)
            characterController.enabled = false;
        player.transform.SetPositionAndRotation(position, rotation);
        if (characterController != null)
            characterController.enabled = true;

        // World dan interior terpisah sangat jauh; smoothing lintas ruang tersebut akan
        // menampilkan area kosong selama beberapa frame jika kamera tidak ikut diteleport.
        cameraFollow?.SetTarget(player.transform, true);
    }

    static Scene FindLoadedWorldScene(Scene interior)
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene candidate = SceneManager.GetSceneAt(index);
            if (candidate.IsValid() && candidate.isLoaded && candidate != interior)
                return candidate;
        }
        return default;
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;
        fadeCanvas.gameObject.SetActive(true);
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = fadeImage.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            fadeImage.color = color;
            yield return null;
        }
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        fadeCanvas.gameObject.SetActive(targetAlpha > 0.001f);
    }

    void BuildFadeUI()
    {
        GameObject canvasObject = new("SceneFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        fadeCanvas = canvasObject.GetComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = true;
        canvasObject.SetActive(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null || FindFirstObjectByType<SceneTransitionManager>() != null)
            return;
        new GameObject(RuntimeObjectName).AddComponent<SceneTransitionManager>();
    }
}
