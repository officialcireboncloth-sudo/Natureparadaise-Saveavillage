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
        if (prefab == null || prefab.GetComponent<MainMenuController>().layoutVersion < 2) prefab = BuildPrefab();
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
        bool existing = File.Exists(PrefabPath);
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) :
            new GameObject("MainMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(MainMenuController), typeof(AudioSource));
        try
        {
            // Preserve the root file IDs, scene references and user-assigned artwork/audio.
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background Image Slot", root.transform, new Color(0.055f, 0.12f, 0.13f, 1f));
            Stretch(background.rectTransform);
            AspectRatioFitter fit = background.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.enabled = false;

            GameObject shade = new("Background Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shade.transform.SetParent(root.transform, false);
            Stretch(shade.GetComponent<RectTransform>());
            shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
            shade.GetComponent<Image>().raycastTarget = false;

            GameObject safe = new("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(root.transform, false);
            Stretch(safe.GetComponent<RectTransform>());

            TMP_Text title = CreateText("Title Text", safe.transform, "NATURE PARADISE", 72, FontStyles.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.35f, 0.66f);
            titleRect.anchorMax = new Vector2(0.65f, 0.92f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.SetActive(false);
            title.color = new Color(0.93f, 1f, 0.88f, 1f);

            Image titleImage = CreateImage("Logo Image Slot", safe.transform, Color.clear);
            RectTransform titleImageRect = titleImage.rectTransform;
            titleImageRect.anchorMin = new Vector2(0.35f, 0.66f);
            titleImageRect.anchorMax = new Vector2(0.65f, 0.92f);
            titleImageRect.offsetMin = titleImageRect.offsetMax = Vector2.zero;
            titleImage.preserveAspect = true;

            TMP_Text tagline = CreateText("Tagline", safe.transform, "Desa yang Lebih Hijau, Masa Depan yang Lebih Baik", 20, FontStyles.Italic);
            tagline.alignment = TextAlignmentOptions.Center;
            Anchors(tagline.rectTransform, new(.30f, .615f), new(.70f, .655f));
            TMP_Text quote = CreateText("Corner Quote", safe.transform, "\"Desa kecil,\n harapan besar.\"", 24, FontStyles.Italic);
            quote.alignment = TextAlignmentOptions.Center;
            Anchors(quote.rectTransform, new(.81f, .89f), new(.97f, .98f));

            GameObject main = CreatePanel("Main Panel", safe.transform);
            Button start = CreateButton(main.transform, "Mulai Game Baru", false, MainMenuIcon.Kind.Play);
            Button continueGame = CreateButton(main.transform, "Lanjutkan", true, MainMenuIcon.Kind.Sprout);
            Button settings = CreateButton(main.transform, "Pengaturan", true, MainMenuIcon.Kind.Settings);
            Button credits = CreateButton(main.transform, "Kredit", true, MainMenuIcon.Kind.People);
            Button exit = CreateButton(main.transform, "Keluar", true, MainMenuIcon.Kind.Exit);

            GameObject play = CreatePanel("Start Panel", safe.transform);
            CreateHeader(play.transform, "START");
            Button newGame = CreateButton(play.transform, "NEW GAME");
            Button load = CreateButton(play.transform, "LOAD GAME");
            Button playBack = CreateButton(play.transform, "BACK", true);
            play.SetActive(false);

            GameObject settingsPanel = CreatePanel("Settings Panel", safe.transform);
            CreateHeader(settingsPanel.transform, "Pengaturan");
            (Slider master, TMP_Text masterValue) = CreateSliderRow(settingsPanel.transform, "Volume Utama");
            (Slider ambient, TMP_Text ambientValue) = CreateSliderRow(settingsPanel.transform, "Musik / Alam");
            (Slider mainSlider, TMP_Text mainValue) = CreateSliderRow(settingsPanel.transform, "Efek Suara");
            TMP_Text languageNote = CreateText("Language Availability", settingsPanel.transform, "Bahasa: Indonesia", 18, FontStyles.Normal);
            languageNote.alignment = TextAlignmentOptions.Center;
            SetLayout(languageNote.gameObject, 26f);
            Button settingsBack = CreateButton(settingsPanel.transform, "Kembali", true);
            settingsPanel.SetActive(false);

            GameObject overwrite = CreatePanel("Overwrite Confirmation Panel", safe.transform);
            CreateHeader(overwrite.transform, "Game Baru");
            TMP_Text warning = CreateText("Warning", overwrite.transform,
                "Save lama akan dihapus. Mulai permainan baru?", 28, FontStyles.Normal);
            warning.alignment = TextAlignmentOptions.Center;
            warning.textWrappingMode = TextWrappingModes.Normal;
            SetLayout(warning.gameObject, 110f);
            Button confirm = CreateButton(overwrite.transform, "Ya, Mulai Game Baru");
            Button cancel = CreateButton(overwrite.transform, "Batal", true);
            overwrite.SetActive(false);

            GameObject creditsPanel = CreatePanel("Credits Panel", safe.transform);
            CreateHeader(creditsPanel.transform, "Kredit");
            TMP_Text creditsText = CreateText("Credits Content", creditsPanel.transform,
                "Nature Paradise\nSave a Village\n\nDesa yang Lebih Hijau,\nMasa Depan yang Lebih Baik", 25, FontStyles.Normal);
            creditsText.alignment = TextAlignmentOptions.Center;
            SetLayout(creditsText.gameObject, 180f);
            Button creditsBack = CreateButton(creditsPanel.transform, "Kembali", true);
            creditsPanel.SetActive(false);

            Image footer = CreateRounded("Footer", safe.transform, new Color(.24f, .31f, .32f, .65f));
            Anchors(footer.rectTransform, new(.34f, .06f), new(.66f, .145f));
            Button language = FooterCell(footer.transform, "Bahasa", "Indonesia", MainMenuIcon.Kind.Globe, 0, out TMP_Text languageValue);
            Button graphics = FooterCell(footer.transform, "Grafik", "High", MainMenuIcon.Kind.Monitor, 1, out TMP_Text graphicsValue);
            Button version = FooterCell(footer.transform, "Versi", "1.0.0", MainMenuIcon.Kind.Info, 2, out TMP_Text versionValue);
            version.interactable = false;
            for (int i = 1; i <= 2; i++)
            {
                Image divider = CreateImage("Divider", footer.transform, new Color(1, 1, 1, .25f));
                Anchors(divider.rectTransform, new(i / 3f, .2f), new(i / 3f, .8f));
                divider.rectTransform.sizeDelta = new Vector2(1, 0);
            }

            AudioSource ambientSource = root.GetComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            AudioSource[] sources = root.GetComponents<AudioSource>();
            AudioSource uiSource = sources.Length > 1 ? sources[1] : root.AddComponent<AudioSource>();
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
            Assign(data, "loadButton", continueGame);
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
            Assign(data, "creditsPanel", creditsPanel);
            Assign(data, "creditsButton", credits);
            Assign(data, "creditsBackButton", creditsBack);
            Assign(data, "footer", footer.rectTransform);
            Assign(data, "tagline", tagline.rectTransform);
            Assign(data, "cornerQuote", quote.gameObject);
            Assign(data, "languageButton", language);
            Assign(data, "graphicsButton", graphics);
            Assign(data, "languageValue", languageValue);
            Assign(data, "graphicsValue", graphicsValue);
            Assign(data, "versionValue", versionValue);
            data.FindProperty("layoutVersion").intValue = 2;
            data.FindProperty("titleMode").enumValueIndex = (int)MainMenuTitleMode.Image;
            data.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return saved;
        }
        finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
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
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuRoundedImage),
            typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.355f, 0.215f);
        rect.anchorMax = new Vector2(0.645f, 0.60f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(.22f, .31f, .33f, .68f);
        image.raycastTarget = true;
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 12, 12);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panel;
    }

    static TMP_Text CreateHeader(Transform parent, string value)
    {
        TMP_Text text = CreateText("Header", parent, value, 42, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.9f, 1f, 0.8f, 1f);
        SetLayout(text.gameObject, 45f);
        return text;
    }

    static Button CreateButton(Transform parent, string label, bool secondary = false, MainMenuIcon.Kind? icon = null)
    {
        GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuRoundedImage),
            typeof(Button), typeof(LayoutElement), typeof(MainMenuButtonAudio));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        // Unity multiplies ColorBlock by the graphic color; white keeps state colors exact.
        image.color = Color.white;
        ((MainMenuRoundedImage)image).radius = 16;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(.39f, .48f, .50f, .5f);
        colors.highlightedColor = new Color(.34f, .63f, .37f, .72f);
        colors.pressedColor = new Color(.27f, .50f, .30f, .8f);
        // A click or initial EventSystem selection must not leave a button green after hover ends.
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(.30f, .34f, .35f, .35f);
        button.colors = colors;
        TMP_Text text = CreateText("Label", buttonObject.transform, label, 26, FontStyles.Normal);
        Anchors(text.rectTransform, new(.24f, .08f), new(.87f, .92f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 26;
        if (icon.HasValue) CreateIcon(buttonObject.transform, icon.Value, new(.07f, .22f), new(.18f, .78f));
        CreateIcon(buttonObject.transform, MainMenuIcon.Kind.Chevron, new(.90f, .24f), new(.96f, .76f));
        SetLayout(buttonObject, 72f);
        buttonObject.GetComponent<LayoutElement>().minHeight = 36;
        return button;
    }

    static Image CreateRounded(string name, Transform parent, Color color)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuRoundedImage));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        return image;
    }

    static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void CreateIcon(Transform parent, MainMenuIcon.Kind kind, Vector2 min, Vector2 max)
    {
        GameObject obj = new(kind + " Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(MainMenuIcon));
        obj.transform.SetParent(parent, false);
        MainMenuIcon icon = obj.GetComponent<MainMenuIcon>(); icon.kind = kind; icon.raycastTarget = false;
        Anchors(obj.GetComponent<RectTransform>(), min, max);
    }

    static Button FooterCell(Transform parent, string caption, string value, MainMenuIcon.Kind kind, int index, out TMP_Text valueText)
    {
        Image image = CreateImage(caption, parent, Color.clear);
        image.raycastTarget = true;
        Anchors(image.rectTransform, new(index / 3f, 0), new((index + 1) / 3f, 1));
        Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        image.gameObject.AddComponent<MainMenuButtonAudio>();
        CreateIcon(image.transform, kind, new(.08f, .25f), new(.27f, .75f));
        TMP_Text label = CreateText("Caption", image.transform, caption, 16, FontStyles.Normal);
        Anchors(label.rectTransform, new(.35f, .5f), new(.95f, .8f));
        valueText = CreateText("Value", image.transform, value, 16, FontStyles.Normal);
        Anchors(valueText.rectTransform, new(.35f, .18f), new(.95f, .5f));
        label.enableAutoSizing = valueText.enableAutoSizing = true;
        label.fontSizeMin = valueText.fontSizeMin = 11;
        label.fontSizeMax = valueText.fontSizeMax = 16;
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
