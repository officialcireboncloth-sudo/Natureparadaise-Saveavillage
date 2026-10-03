using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MainMenuTitleMode { Text, Image }

/// <summary>Main menu scene mandiri. Seluruh visual dan audio dapat diganti lewat prefab.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] string gameplayScene = "Map";

    [Header("Editable Background")]
    [SerializeField] Image background;
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Color backgroundTint = new(0.06f, 0.12f, 0.14f, 1f);

    [Header("Editable Title")]
    [SerializeField] MainMenuTitleMode titleMode = MainMenuTitleMode.Text;
    [SerializeField] TMP_Text titleText;
    [SerializeField] string gameTitle = "NATURE PARADISE";
    [SerializeField] Image titleImage;
    [SerializeField] Sprite titleSprite;

    [Header("Responsive Layout")]
    [SerializeField] bool responsiveLayout = true;
    [SerializeField, Min(0.5f)] float mobileAspectBreakpoint = 1.2f;

    [Header("Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject playPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject overwritePanel;
    [SerializeField] Button startButton;
    [SerializeField] Button loadButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button newGameButton;
    [SerializeField] Button playLoadButton;
    [SerializeField] Button playBackButton;
    [SerializeField] Button settingsBackButton;
    [SerializeField] Button overwriteConfirmButton;
    [SerializeField] Button overwriteCancelButton;

    [Header("Audio Settings")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider ambientSlider;
    [SerializeField] Slider mainSlider;
    [SerializeField] TMP_Text masterValue;
    [SerializeField] TMP_Text ambientValue;
    [SerializeField] TMP_Text mainValue;

    [Header("Main Menu Audio Slots")]
    [SerializeField] AudioSource backgroundAudioSource;
    [SerializeField] AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] float backgroundMusicVolume = 0.75f;
    [SerializeField] AudioSource uiAudioSource;
    [SerializeField] AudioClip buttonHoverSound;
    [SerializeField] AudioClip buttonClickSound;
    [SerializeField, Range(0f, 1f)] float uiSoundVolume = 0.85f;

    bool loading;
    Vector2Int lastLayoutScreen;

    void Awake()
    {
        ApplyEditableVisuals();
        ApplyResponsiveLayout();
        BindButtons();
        BindSliders();
        ShowMain();
        StartBackgroundMusic();
    }

    void OnEnable() => GameAudio.VolumesChanged += RefreshAudio;
    void OnDisable() => GameAudio.VolumesChanged -= RefreshAudio;

    void Update()
    {
        if (responsiveLayout && (lastLayoutScreen.x != Screen.width || lastLayoutScreen.y != Screen.height))
            ApplyResponsiveLayout();
        if (Input.GetKeyDown(KeyCode.Escape)) Back();
    }

    void ApplyResponsiveLayout()
    {
        lastLayoutScreen = new Vector2Int(Screen.width, Screen.height);
        if (!responsiveLayout || Screen.height <= 0) return;

        bool mobile = (float)Screen.width / Screen.height < mobileAspectBreakpoint;
        ApplyTitleLayout(titleText != null ? titleText.rectTransform : null, mobile);
        ApplyTitleLayout(titleImage != null ? titleImage.rectTransform : null, mobile);
        ApplyPanelLayout(mainPanel, mobile);
        ApplyPanelLayout(playPanel, mobile);
        ApplyPanelLayout(settingsPanel, mobile);
        ApplyPanelLayout(overwritePanel, mobile);
        if (titleText != null)
            titleText.alignment = mobile ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
    }

    static void ApplyTitleLayout(RectTransform rect, bool mobile)
    {
        if (rect == null) return;
        rect.anchorMin = mobile ? new Vector2(0.08f, 0.78f) : new Vector2(0.06f, 0.72f);
        rect.anchorMax = mobile ? new Vector2(0.92f, 0.95f) : new Vector2(0.58f, 0.92f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void ApplyPanelLayout(GameObject panel, bool mobile)
    {
        if (panel == null || panel.transform is not RectTransform rect) return;
        rect.anchorMin = mobile ? new Vector2(0.07f, 0.08f) : new Vector2(0.62f, 0.16f);
        rect.anchorMax = mobile ? new Vector2(0.93f, 0.75f) : new Vector2(0.94f, 0.84f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    void ApplyEditableVisuals()
    {
        if (background != null)
        {
            background.sprite = backgroundSprite;
            background.color = backgroundTint;
            background.preserveAspect = backgroundSprite != null;
        }
        bool textTitle = titleMode == MainMenuTitleMode.Text;
        if (titleText != null)
        {
            titleText.gameObject.SetActive(textTitle);
            titleText.text = gameTitle;
        }
        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(!textTitle);
            titleImage.sprite = titleSprite;
            titleImage.preserveAspect = true;
        }
    }

    void BindButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(PlayClickSound);
        startButton?.onClick.AddListener(ShowPlay);
        settingsButton?.onClick.AddListener(ShowSettings);
        exitButton?.onClick.AddListener(ExitDesktop);
        newGameButton?.onClick.AddListener(RequestNewGame);
        playLoadButton?.onClick.AddListener(LoadGame);
        playBackButton?.onClick.AddListener(ShowMain);
        settingsBackButton?.onClick.AddListener(ShowMain);
        overwriteConfirmButton?.onClick.AddListener(ConfirmNewGame);
        overwriteCancelButton?.onClick.AddListener(ShowPlay);
    }

    void BindSliders()
    {
        ConfigureSlider(masterSlider, GameAudio.MasterVolume, GameAudio.SetMaster);
        ConfigureSlider(ambientSlider, GameAudio.AmbientVolume, GameAudio.SetAmbient);
        ConfigureSlider(mainSlider, GameAudio.MainVolume, GameAudio.SetMain);
        RefreshVolumeLabels();
    }

    static void ConfigureSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(action);
    }

    public void ShowMain() => Show(mainPanel, startButton);
    public void ShowPlay() => Show(playPanel, newGameButton);
    public void ShowSettings() => Show(settingsPanel, masterSlider);

    void Show(GameObject panel, Selectable selection)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (playPanel != null) playPanel.SetActive(panel == playPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (overwritePanel != null) overwritePanel.SetActive(panel == overwritePanel);
        if (loadButton != null) loadButton.interactable = SaveManager.SaveExists();
        if (playLoadButton != null) playLoadButton.interactable = SaveManager.SaveExists();
        EventSystem.current?.SetSelectedGameObject(selection != null ? selection.gameObject : null);
    }

    void Back()
    {
        if (mainPanel != null && mainPanel.activeSelf) return;
        ShowMain();
    }

    void RequestNewGame()
    {
        if (SaveManager.SaveExists()) Show(overwritePanel, overwriteCancelButton);
        else ConfirmNewGame();
    }

    void ConfirmNewGame()
    {
        SaveManager.DeleteSaveFile();
        StartCoroutine(Launch(false));
    }

    void LoadGame()
    {
        if (SaveManager.SaveExists()) StartCoroutine(Launch(true));
    }

    IEnumerator Launch(bool loadSave)
    {
        if (loading) yield break;
        loading = true;
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null) canvas.enabled = false;
        DontDestroyOnLoad(gameObject);
        yield return new WaitForSecondsRealtime(buttonClickSound != null ? Mathf.Min(0.18f, buttonClickSound.length) : 0.05f);
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
        if (operation == null)
        {
            loading = false;
            if (canvas != null) canvas.enabled = true;
            yield break;
        }
        while (!operation.isDone) yield return null;
        yield return null;
        yield return null;
        if (loadSave)
        {
            SaveManager manager = SaveManager.Instance != null ? SaveManager.Instance : FindFirstObjectByType<SaveManager>();
            manager?.LoadGame();
        }
        Destroy(gameObject);
    }

    void ExitDesktop()
    {
        StartCoroutine(ExitAfterClick());
    }

    IEnumerator ExitAfterClick()
    {
        yield return new WaitForSecondsRealtime(buttonClickSound != null ? Mathf.Min(0.18f, buttonClickSound.length) : 0.05f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartBackgroundMusic()
    {
        if (backgroundAudioSource == null || backgroundMusic == null) return;
        backgroundAudioSource.clip = backgroundMusic;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.playOnAwake = false;
        RefreshAudio();
        backgroundAudioSource.Play();
    }

    void RefreshAudio()
    {
        if (backgroundAudioSource != null)
            backgroundAudioSource.volume = backgroundMusicVolume * GameAudio.AmbientVolume;
        RefreshVolumeLabels();
    }

    void RefreshVolumeLabels()
    {
        if (masterValue != null) masterValue.text = $"{Mathf.RoundToInt(GameAudio.MasterVolume * 100f)}%";
        if (ambientValue != null) ambientValue.text = $"{Mathf.RoundToInt(GameAudio.AmbientVolume * 100f)}%";
        if (mainValue != null) mainValue.text = $"{Mathf.RoundToInt(GameAudio.MainVolume * 100f)}%";
    }

    public void PlayHoverSound() => GameAudio.PlayOneShot(uiAudioSource, buttonHoverSound, GameAudioBus.Main, uiSoundVolume);
    public void PlayClickSound() => GameAudio.PlayOneShot(uiAudioSource, buttonClickSound, GameAudioBus.Main, uiSoundVolume);
}
