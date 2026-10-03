using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuSetup
{
    const string PrefabPath = "Assets/Nature  Paradaise/Prefabs/UI/MainMenu.prefab";
    const string ScenePath = "Assets/Nature  Paradaise/Map/Scenes/UI/MainMenu.unity";
    const string GameplayScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
    const string DefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    static MainMenuSetup()
    {
        EditorApplication.delayCall += EnsureExists;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += EnsureExists;
        };
    }

    [MenuItem("Nature Paradise/UI/Main Menu/Setup Missing Assets", false, 100)]
    public static void EnsureExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));
        EnsureFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = BuildPrefab();
        if (!File.Exists(ScenePath)) BuildScene(prefab);
        EnsureBuildSettings();
    }

    [MenuItem("Nature Paradise/UI/Main Menu/Select Editable Prefab", false, 101)]
    static void SelectPrefab()
    {
        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    static GameObject BuildPrefab()
    {
        GameObject root = new("MainMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(MainMenuController), typeof(AudioSource));
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background", root.transform, new Color(0.055f, 0.12f, 0.13f, 1f));
            Stretch(background.rectTransform);

            GameObject shade = new("Background Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shade.transform.SetParent(root.transform, false);
            Stretch(shade.GetComponent<RectTransform>());
            shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.28f);
            shade.GetComponent<Image>().raycastTarget = false;

            GameObject safe = new("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(root.transform, false);
            Stretch(safe.GetComponent<RectTransform>());

            TMP_Text title = CreateText("Title Text", safe.transform, "NATURE PARADISE", 72, FontStyles.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.06f, 0.72f);
            titleRect.anchorMax = new Vector2(0.58f, 0.92f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            title.alignment = TextAlignmentOptions.Left;
            title.color = new Color(0.93f, 1f, 0.88f, 1f);

            Image titleImage = CreateImage("Title Image", safe.transform, Color.white);
            RectTransform titleImageRect = titleImage.rectTransform;
            titleImageRect.anchorMin = new Vector2(0.06f, 0.72f);
            titleImageRect.anchorMax = new Vector2(0.58f, 0.92f);
            titleImageRect.offsetMin = titleImageRect.offsetMax = Vector2.zero;
            titleImage.gameObject.SetActive(false);

            GameObject main = CreatePanel("Main Panel", safe.transform);
            TMP_Text mainHeader = CreateHeader(main.transform, "MAIN MENU");
            Button start = CreateButton(main.transform, "START");
            Button settings = CreateButton(main.transform, "SETTINGS");
            Button exit = CreateButton(main.transform, "EXIT TO DESKTOP");

            GameObject play = CreatePanel("Start Panel", safe.transform);
            CreateHeader(play.transform, "START");
            Button newGame = CreateButton(play.transform, "NEW GAME");
            Button load = CreateButton(play.transform, "LOAD GAME");
            Button playBack = CreateButton(play.transform, "BACK", true);
            play.SetActive(false);

            GameObject settingsPanel = CreatePanel("Settings Panel", safe.transform);
            CreateHeader(settingsPanel.transform, "AUDIO SETTINGS");
            (Slider master, TMP_Text masterValue) = CreateSliderRow(settingsPanel.transform, "MASTER");
            (Slider ambient, TMP_Text ambientValue) = CreateSliderRow(settingsPanel.transform, "AMBIENT / MUSIC");
            (Slider mainSlider, TMP_Text mainValue) = CreateSliderRow(settingsPanel.transform, "MAIN / SFX");
            Button settingsBack = CreateButton(settingsPanel.transform, "BACK", true);
            settingsPanel.SetActive(false);

            GameObject overwrite = CreatePanel("Overwrite Confirmation Panel", safe.transform);
            CreateHeader(overwrite.transform, "NEW GAME");
            TMP_Text warning = CreateText("Warning", overwrite.transform,
                "Save lama akan dihapus. Mulai permainan baru?", 28, FontStyles.Normal);
            warning.alignment = TextAlignmentOptions.Center;
            warning.textWrappingMode = TextWrappingModes.Normal;
            SetLayout(warning.gameObject, 110f);
            Button confirm = CreateButton(overwrite.transform, "YES, NEW GAME");
            Button cancel = CreateButton(overwrite.transform, "CANCEL", true);
            overwrite.SetActive(false);

            AudioSource ambientSource = root.GetComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            AudioSource uiSource = root.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;

            SerializedObject data = new(root.GetComponent<MainMenuController>());
            Assign(data, "background", background);
            Assign(data, "titleText", title);
            Assign(data, "titleImage", titleImage);
            Assign(data, "mainPanel", main);
            Assign(data, "playPanel", play);
            Assign(data, "settingsPanel", settingsPanel);
            Assign(data, "overwritePanel", overwrite);
            Assign(data, "startButton", start);
            Assign(data, "settingsButton", settings);
            Assign(data, "exitButton", exit);
            Assign(data, "newGameButton", newGame);
            Assign(data, "loadButton", load);
            Assign(data, "playLoadButton", load);
            Assign(data, "playBackButton", playBack);
            Assign(data, "settingsBackButton", settingsBack);
            Assign(data, "overwriteConfirmButton", confirm);
            Assign(data, "overwriteCancelButton", cancel);
            Assign(data, "masterSlider", master);
            Assign(data, "ambientSlider", ambient);
            Assign(data, "mainSlider", mainSlider);
            Assign(data, "masterValue", masterValue);
            Assign(data, "ambientValue", ambientValue);
            Assign(data, "mainValue", mainValue);
            Assign(data, "backgroundAudioSource", ambientSource);
            Assign(data, "uiAudioSource", uiSource);
            data.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return saved;
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void BuildScene(GameObject prefab)
    {
        Scene previous = SceneManager.GetActiveScene();
        bool replaceEmptyUntitledScene = string.IsNullOrEmpty(previous.path) && previous.rootCount == 0;
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            replaceEmptyUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
        scene.name = "MainMenu";
        SceneManager.SetActiveScene(scene);

        GameObject cameraObject = new("Main Menu Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.055f, 0.06f, 1f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        PrefabUtility.InstantiatePrefab(prefab);
        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!replaceEmptyUntitledScene)
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[MAIN MENU] Scene dan prefab modular dibuat: {ScenePath}");
    }

    static void EnsureBuildSettings()
    {
        string[] preferred =
        {
            ScenePath,
            GameplayScenePath,
            "Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity",
            "Assets/Nature  Paradaise/Map/Scenes/Interiors/BarnInterior.unity",
            "Assets/Nature  Paradaise/Map/Scenes/Testing/TestingScene.unity"
        };
        var existing = EditorBuildSettings.scenes;
        var result = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (string path in preferred)
            if (File.Exists(path)) result.Add(new EditorBuildSettingsScene(path, true));
        foreach (EditorBuildSettingsScene item in existing)
            if (result.TrueForAll(entry => entry.path != item.path)) result.Add(item);
        EditorBuildSettings.scenes = result.ToArray();
    }

    static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.62f, 0.16f);
        rect.anchorMax = new Vector2(0.94f, 0.84f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.025f, 0.045f, 0.05f, 0.9f);
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 38, 38);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panel;
    }

    static TMP_Text CreateHeader(Transform parent, string value)
    {
        TMP_Text text = CreateText("Header", parent, value, 42, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.9f, 1f, 0.8f, 1f);
        SetLayout(text.gameObject, 72f);
        return text;
    }

    static Button CreateButton(Transform parent, string label, bool secondary = false)
    {
        GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(LayoutElement), typeof(MainMenuButtonAudio));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = secondary ? new Color(0.14f, 0.19f, 0.2f, 1f) : new Color(0.18f, 0.48f, 0.33f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.75f, 0.85f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;
        TMP_Text text = CreateText("Label", buttonObject.transform, label, 27, FontStyles.Bold);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        SetLayout(buttonObject, 72f);
        return button;
    }

    static (Slider slider, TMP_Text value) CreateSliderRow(Transform parent, string label)
    {
        GameObject row = new(label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        SetLayout(row, 68f);

        TMP_Text caption = CreateText("Label", row.transform, label, 23, FontStyles.Bold);
        caption.alignment = TextAlignmentOptions.Left;
        SetLayout(caption.gameObject, 68f, 180f);

        GameObject sliderObject = new("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.transform.SetParent(row.transform, false);
        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        sliderLayout.preferredWidth = 210f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 50f;

        Image track = CreateImage("Track", sliderObject.transform, new Color(0.18f, 0.24f, 0.25f, 1f));
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(0f, 0.35f);
        trackRect.anchorMax = new Vector2(1f, 0.65f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

        GameObject fillArea = new("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = new Vector2(6f, 0f);
        fillAreaRect.offsetMax = new Vector2(-6f, 0f);
        Image fill = CreateImage("Fill", fillArea.transform, new Color(0.3f, 0.8f, 0.48f, 1f));
        Stretch(fill.rectTransform);

        GameObject handleArea = new("Handle Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());
        Image handle = CreateImage("Handle", handleArea.transform, new Color(0.92f, 1f, 0.9f, 1f));
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(28f, 46f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;

        TMP_Text value = CreateText("Value", row.transform, "100%", 22, FontStyles.Normal);
        value.alignment = TextAlignmentOptions.Right;
        SetLayout(value.gameObject, 68f, 64f);
        return (slider, value);
    }

    static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
        if (defaultFont != null) text.font = defaultFont;
        return text;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void SetLayout(GameObject target, float height, float width = -1f)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width >= 0f) layout.preferredWidth = width;
    }

    static void Assign(SerializedObject data, string property, Object value)
    {
        SerializedProperty field = data.FindProperty(property);
        if (field != null) field.objectReferenceValue = value;
    }

    static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }
}
