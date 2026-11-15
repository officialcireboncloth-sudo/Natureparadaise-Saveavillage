using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Memuat GameplayUI satu kali secara additive untuk seluruh scene gameplay.
/// Perpindahan Map ke interior tidak membuat UI dimuat ulang.
/// </summary>
public sealed class GameplayUIBootstrap : MonoBehaviour
{
    public const string SceneName = "GameplayUI";
    const string MainMenuSceneName = "MainMenu";

    static GameplayUIBootstrap instance;
    bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (instance != null) return;
        GameObject host = new("GameplayUIBootstrap_Runtime");
        instance = host.AddComponent<GameplayUIBootstrap>();
        DontDestroyOnLoad(host);
    }

    void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainMenuSceneName)
        {
            loading = false;
            Scene ui = SceneManager.GetSceneByName(SceneName);
            if (ui.IsValid() && ui.isLoaded) SceneManager.UnloadSceneAsync(ui);
            return;
        }

        if (scene.name == SceneName)
        {
            loading = false;
            OptimizeLoadedUI(scene);
            return;
        }

        EnsureLoaded();
    }

    void EnsureLoaded()
    {
        Scene ui = SceneManager.GetSceneByName(SceneName);
        if (loading || (ui.IsValid() && ui.isLoaded)) return;
        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogError($"[GAMEPLAY UI] Scene '{SceneName}' belum ada di Build Settings.");
            return;
        }
        StartCoroutine(LoadUI());
    }

    IEnumerator LoadUI()
    {
        loading = true;
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        if (operation != null) yield return operation;
        loading = false;
    }

    static void OptimizeLoadedUI(Scene uiScene)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.scene != uiScene) continue;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        IsolateModal(uiScene, "ShopPanel", 420);
        IsolateModal(uiScene, "DialoguePanel", 430);
        IsolateModal(uiScene, "QuestJournalPanel", 440);
        EnsureSingleEventSystem(uiScene);
    }

    static void IsolateModal(Scene uiScene, string objectName, int sortingOrder)
    {
        foreach (GameObject root in uiScene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name != objectName) continue;
                Canvas canvas = candidate.GetComponent<Canvas>();
                if (canvas == null) canvas = candidate.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
                if (candidate.GetComponent<GraphicRaycaster>() == null)
                    candidate.gameObject.AddComponent<GraphicRaycaster>();

                // Semua modal harus mulai tertutup. Controller pemiliknya akan
                // mengaktifkan panel kembali jika sebuah interaksi memang sedang berjalan.
                // Ini juga mencegah Farm Shop tampil di scene test yang tidak punya seller.
                candidate.gameObject.SetActive(false);
            }
        }
    }

    static void EnsureSingleEventSystem(Scene uiScene)
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems.Length == 0)
        {
            GameObject eventObject = new("EventSystem_GameplayUI", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventObject, uiScene);
            return;
        }
        EventSystem preferred = null;
        foreach (EventSystem system in systems)
            if (system.gameObject.scene != uiScene && system.gameObject.activeInHierarchy) { preferred = system; break; }
        if (preferred == null)
            foreach (EventSystem system in systems)
                if (system.gameObject.scene == uiScene) { preferred = system; break; }

        foreach (EventSystem system in systems)
            system.gameObject.SetActive(system == preferred);
    }
}
