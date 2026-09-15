using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Build Menu Field milik Player. B dapat membukanya tanpa marker dunia; pilihan menu membuat
/// PropertySite runtime dan hanya menerima bangunan yang footprint-nya valid pada FieldArea.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerBuildMenu : MonoBehaviour
{
    [SerializeField] KeyCode openKey = KeyCode.B;
    [SerializeField] KeyCode confirmKey = KeyCode.Return;

    readonly List<Button> buildingButtons = new();
    Inventory inventory;
    PlayerController movement;
    TimeManager timeManager;
    PropertySite selectedSite;
    [SerializeField, Tooltip("Katalog bangunan khusus Field yang tampil ketika Player menekan B.")]
    BuildingCatalogSO catalog;
    GameObject canvasObject;
    GameObject panelObject;
    TMP_Text detailText;
    TMP_Text footerText;
    TMP_Text titleText;
    int selectedIndex;
    int selectedLevel = 1;
    int openedFrame = -1;
    bool isOpen;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        movement = GetComponent<PlayerController>();
        catalog = Resources.Load<BuildingCatalogSO>("Buildings/Field Building Catalog");
    }

    void Update()
    {
        if (!isOpen)
        {
            if (Input.GetKeyDown(openKey))
                OpenGlobal();
            return;
        }

        // B yang membuka menu tidak boleh langsung dibaca kembali sebagai perintah tutup
        // apabila Update menu kebetulan berjalan setelah Update PropertySite pada frame sama.
        if (Time.frameCount == openedFrame)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(openKey))
            Close();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            Select(selectedIndex - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Select(selectedIndex + 1);
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            ChangeLevel(1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            ChangeLevel(-1);
        else if (PropertySite.DebugShortcutsEnabled && Input.GetKeyDown(KeyCode.F9))
            StartPlacement(true);
        else if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(KeyCode.C))
            StartPlacement();
    }

    void OpenGlobal()
    {
        if (FindFirstObjectByType<FieldArea>() == null)
            return;
        catalog = Resources.Load<BuildingCatalogSO>("Buildings/Field Building Catalog");
        if (catalog == null || catalog.Count == 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Field Building Catalog kosong");
            return;
        }
        if (PropertySite.HasActivePlacementPreview() || (movement != null && movement.IsMovementLocked))
            return;

        PropertySite site = PropertySite.GetOrCreateGlobalBuildSite(catalog, transform.position);
        OpenForSite(site);
    }

    /// <summary>Membuka katalog pada controller placement yang dipilih.</summary>
    public bool OpenForSite(PropertySite site)
    {
        if (isOpen || site == null || !site.IsUnlocked || !site.IsEmpty ||
            PropertySite.HasActivePlacementPreview() ||
            (movement != null && movement.IsMovementLocked))
        {
            return false;
        }

        selectedSite = site;

        catalog = selectedSite.Catalog;
        if (catalog == null || catalog.Count == 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Building Catalog kosong");
            return false;
        }

        EnsureUI();
        titleText.text = selectedSite.IsGlobalBuildInstance ? "FIELD BUILD MENU" : "FIXED FARM CONSTRUCTION";
        RebuildButtons();
        isOpen = true;
        openedFrame = Time.frameCount;
        panelObject.SetActive(true);
        movement?.AcquireMovementLock(this);
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
        timeManager?.AcquirePause(this);
        WorldInteractionPrompt.AcquireSuppression(this);
        selectedLevel = 1;
        Select(0);
        return true;
    }

    void Close()
    {
        isOpen = false;
        if (panelObject != null)
            panelObject.SetActive(false);
        movement?.ReleaseMovementLock(this);
        timeManager?.ReleasePause(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    void StartPlacement(bool debugInstant = false)
    {
        if (selectedSite == null || catalog == null ||
            selectedIndex < 0 || selectedIndex >= catalog.Count)
            return;

        if (!selectedSite.BeginBuildSelection(selectedIndex, selectedLevel, debugInstant))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Placement bangunan gagal dimulai");
            return;
        }
        Close();
    }

    void Select(int index)
    {
        if (catalog == null || catalog.Count == 0)
            return;

        int nextIndex = (index % catalog.Count + catalog.Count) % catalog.Count;
        if (nextIndex != selectedIndex)
            selectedLevel = 1;
        selectedIndex = nextIndex;
        for (int buttonIndex = 0; buttonIndex < buildingButtons.Count; buttonIndex++)
        {
            Image image = buildingButtons[buttonIndex].GetComponent<Image>();
            if (image != null)
                image.color = buttonIndex == selectedIndex
                    ? new Color(0.31f, 0.55f, 0.27f, 1f)
                    : new Color(0.30f, 0.20f, 0.12f, 1f);
        }

        BuildingDefinitionSO definition = catalog.GetAt(selectedIndex);
        int maximumLevel = GetMaximumLevel(definition);
        selectedLevel = Mathf.Clamp(selectedLevel, 1, maximumLevel);
        BuildingLevelDefinition level = definition?.GetLevel(selectedLevel);
        detailText.text = definition == null
            ? "Data bangunan tidak tersedia"
            : $"<b>{definition.displayName}</b>\n" +
              $"Level: {selectedLevel} / {maximumLevel}   Kapasitas: {Mathf.Max(0, level?.capacity ?? 0)}\n" +
              $"Ukuran: {definition.footprintWidth} x {definition.footprintDepth}\n" +
              $"Konstruksi: {Mathf.Max(0, level?.constructionDays ?? 0)} hari\n\n" +
              BuildingCostUtility.BuildRequirementLabel(level, inventory);
    }

    void ChangeLevel(int direction)
    {
        BuildingDefinitionSO definition = catalog != null ? catalog.GetAt(selectedIndex) : null;
        selectedLevel = Mathf.Clamp(selectedLevel + direction, 1, GetMaximumLevel(definition));
        Select(selectedIndex);
    }

    static int GetMaximumLevel(BuildingDefinitionSO definition)
    {
        int maximum = 1;
        if (definition?.levels == null)
            return maximum;
        for (int index = 0; index < definition.levels.Count; index++)
            if (definition.levels[index] != null)
                maximum = Mathf.Max(maximum, definition.levels[index].level);
        return maximum;
    }

    void EnsureUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject("ConstructionMenuCanvas_Runtime", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 460;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("ConstructionMenu", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900f, 540f);
        panelObject = panel.gameObject;
        AddImage(panel, new Color(0.12f, 0.075f, 0.035f, 0.98f));

        titleText = CreateText("Title", panel, "FIELD BUILD MENU", 32f, TextAlignmentOptions.Center);
        SetRect(titleText.rectTransform, new Vector2(20f, -18f), new Vector2(-20f, -76f));

        RectTransform cards = CreateRect("BuildingCards", panel);
        cards.anchorMin = new Vector2(0f, 1f);
        cards.anchorMax = new Vector2(1f, 1f);
        cards.pivot = new Vector2(0.5f, 1f);
        cards.anchoredPosition = new Vector2(0f, -88f);
        cards.sizeDelta = new Vector2(-40f, 150f);
        HorizontalLayoutGroup layout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        detailText = CreateText("BuildingDetails", panel, string.Empty, 23f, TextAlignmentOptions.TopLeft);
        SetRect(detailText.rectTransform, new Vector2(44f, -260f), new Vector2(-44f, -450f));
        detailText.textWrappingMode = TextWrappingModes.Normal;

        footerText = CreateText("Footer", panel,
            PropertySite.DebugShortcutsEnabled
                ? "A/D: Pilih   W/S: Level   C: Placement Normal   F9: Placement DEBUG Gratis   B/Esc: Tutup"
                : "A/D: Pilih   W/S: Level   C/Enter: Mulai Placement   B/Esc: Tutup", 19f,
            TextAlignmentOptions.Center);
        SetRect(footerText.rectTransform, new Vector2(24f, -474f), new Vector2(-24f, -520f));
        panelObject.SetActive(false);
    }

    void RebuildButtons()
    {
        for (int index = 0; index < buildingButtons.Count; index++)
            if (buildingButtons[index] != null)
                Destroy(buildingButtons[index].gameObject);
        buildingButtons.Clear();

        Transform cards = panelObject.transform.Find("BuildingCards");
        for (int index = 0; index < catalog.Count; index++)
        {
            int capturedIndex = index;
            BuildingDefinitionSO definition = catalog.GetAt(index);
            RectTransform card = CreateRect($"Building_{index}", cards);
            Image background = AddImage(card, new Color(0.30f, 0.20f, 0.12f, 1f));
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => Select(capturedIndex));
            TMP_Text label = CreateText("Label", card,
                definition != null ? definition.displayName : "Kosong", 24f, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            buildingButtons.Add(button);
        }
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject created = new(name, typeof(RectTransform));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Image AddImage(RectTransform parent, Color color)
    {
        Image image = parent.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = new Color(0.96f, 0.88f, 0.68f, 1f);
        text.alignment = alignment;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    static void SetRect(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(minOffset.x, maxOffset.y);
        rect.offsetMax = new Vector2(maxOffset.x, minOffset.y);
    }

    void OnDisable()
    {
        if (isOpen)
            Close();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        Inventory playerInventory = FindFirstObjectByType<Inventory>();
        if (playerInventory != null && playerInventory.GetComponent<PlayerBuildMenu>() == null)
            playerInventory.gameObject.AddComponent<PlayerBuildMenu>();
    }
}
