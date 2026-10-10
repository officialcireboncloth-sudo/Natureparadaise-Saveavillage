using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hotbar mengikuti slot Inventory, dengan icon, jumlah stack dan info alat terpilih.
/// sehingga stack, perpindahan item, dan Save/Load memakai satu sumber data.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public sealed class InventoryHotbarUI : MonoBehaviour
{
    static InventoryHotbarUI instance;
    [SerializeField] bool referenceHUDStyle = true;
    [Header("Optional PNG Sprites")]
    [SerializeField] Sprite hotbarPanelSprite;
    [SerializeField] Sprite slotSprite;
    [SerializeField] Sprite selectedSlotSprite;

    [Header("Minimalist Fallback Colors")]
    [SerializeField] Color panelColor = new(0.025f, 0.032f, 0.045f, 0.92f);
    [SerializeField] Color slotColor = new(0.10f, 0.12f, 0.16f, 0.96f);
    [SerializeField] Color selectedColor = new(0.18f, 0.72f, 0.82f, 1f);

    Inventory inventory;
    PlayerToolHotbar toolHotbar;
    PlayerEatingSystem eatingSystem;
    FishingSystem fishingSystem;
    WateringCanSystem wateringCan;
    readonly List<SlotView> views = new();
    TMP_Text actionHint;
    RectTransform hintRoot;
    RectTransform waterFill;
    CanvasGroup pauseVisibility;
    Coroutine temporaryHintRoutine;
    string temporaryHint;
    int selectedIndex;

    public int SelectedIndex => selectedIndex;
    public ItemStack SelectedStack => inventory != null ? inventory.GetSlot(selectedIndex) : null;
    public ItemSO SelectedItem => SelectedStack?.item;
    public event Action<int> SelectionChanged;

    void Awake()
    {
        instance = this;
        inventory = GetComponent<Inventory>();
        toolHotbar = GetComponent<PlayerToolHotbar>();
        if (toolHotbar == null) toolHotbar = gameObject.AddComponent<PlayerToolHotbar>();
        eatingSystem = GetComponent<PlayerEatingSystem>();
        fishingSystem = GetComponent<FishingSystem>();
        wateringCan = GetComponent<WateringCanSystem>();
        if (wateringCan == null) wateringCan = gameObject.AddComponent<WateringCanSystem>();
    }

    void OnEnable()
    {
        GameplayUISettings.Applied += Refresh;
        if (inventory != null) inventory.OnInventoryChanged += Refresh;
        if (wateringCan != null) wateringCan.Changed += RefreshActionHint;
    }

    void OnDisable()
    {
        GameplayUISettings.Applied -= Refresh;
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (wateringCan != null) wateringCan.Changed -= RefreshActionHint;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        // Pastikan data inventory siap terlepas dari urutan Start antar-komponen.
        inventory.EnsureStartupLoadout();
        BuildUI();
        SelectSlot(0);
    }

    void Update()
    {
        if (fishingSystem == null) fishingSystem = GetComponent<FishingSystem>();
        bool hidden = DataDrivenModal.Active != null || GameplayPauseMenu.IsOpen || (GetComponent<InventoryUI>()?.IsOpen ?? false) || (fishingSystem != null && fishingSystem.isActiveAndEnabled && fishingSystem.State != FishingState.Idle);
        if (pauseVisibility != null) { pauseVisibility.alpha = hidden ? 0f : 1f; pauseVisibility.interactable = !hidden; pauseVisibility.blocksRaycasts = !hidden; }
        if (WorldInteractionPrompt.IsSuppressed) return;
        if (GameplayInput.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        else if (GameplayInput.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
    }

    public void SelectSlot(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, inventory.HotbarSlotCount - 1);
        EquipSelectedItem();
        RefreshActionHint();
        Refresh();
        SelectionChanged?.Invoke(selectedIndex);
    }

    public void Refresh()
    {
        if (inventory == null || views.Count == 0) return;
        for (int i = 0; i < views.Count; i++)
        {
            SlotView view = views[i];
            bool selected = i == selectedIndex;
            view.Background.sprite = selected && selectedSlotSprite != null ? selectedSlotSprite : slotSprite;
            view.Background.type = view.Background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            view.Background.color = referenceHUDStyle ? selected ? new Color(.40f,.49f,.34f,.72f) : GameplayHUDStyle.Slot : selected ? selectedColor : slotColor;
            if (view.Background is MainMenuRoundedImage rounded)
            {
                rounded.borderColor = Color.clear;
                rounded.borderWidth = 0f;
                rounded.SetVerticesDirty();
            }

            ItemStack stack = inventory.GetSlot(i);
            bool hasItem = stack?.item != null && stack.count > 0;
            view.Icon.enabled = hasItem && stack.item.icon != null;
            view.Icon.sprite = hasItem ? stack.item.icon : null;
            view.Label.text = hasItem && stack.count > 1 ? stack.count.ToString() : "";
            view.Fallback.text = hasItem && stack.item.icon == null && GameplayUISettings.Current.showItemNames ? stack.DisplayName : "";
        }
        EquipSelectedItem();
        RefreshActionHint();
    }

    void EquipSelectedItem()
    {
        ItemStack stack = inventory.GetSlot(selectedIndex);
        PlayerToolType selectedTool = PlayerToolType.None;
        if (stack?.item != null)
        {
            selectedTool = stack.item.equippedTool;
            if (selectedTool == PlayerToolType.None && stack.item.category == ItemCategory.Seed)
                selectedTool = PlayerToolType.Seed;
            if (stack.item.category == ItemCategory.Food)
                eatingSystem?.SetSelectedFood(stack.item);
        }
        toolHotbar.SelectTool(selectedTool);
    }

    void RefreshActionHint()
    {
        if (actionHint == null || toolHotbar == null) return;
        if (!string.IsNullOrEmpty(temporaryHint))
        {
            if (hintRoot != null) hintRoot.gameObject.SetActive(true);
            if (waterFill != null) waterFill.parent.gameObject.SetActive(false);
            actionHint.text = $"<b>{temporaryHint}</b>";
            return;
        }
        ItemSO item = SelectedItem;
        if (hintRoot != null)
        {
            hintRoot.gameObject.SetActive(item != null || !string.IsNullOrEmpty(temporaryHint));
            float width = inventory.HotbarSlotCount * 88f + (inventory.HotbarSlotCount - 1) * 8f + 20f;
            hintRoot.anchoredPosition = new Vector2(Mathf.Clamp(-width * .5f + 10 + selectedIndex * 96f, -width * .5f, Mathf.Max(-width * .5f, width * .5f - 300f)), 160f);
        }
        if (waterFill != null)
        {
            waterFill.parent.gameObject.SetActive(toolHotbar.SelectedTool == PlayerToolType.WateringCan);
            waterFill.anchorMax = new Vector2(wateringCan != null ? (float)wateringCan.CurrentWater / wateringCan.MaximumWater : 1f, 1f);
        }
        if (fishingSystem == null) fishingSystem = GetComponent<FishingSystem>();
        AnimalCareCatalog animalCare = AnimalCareCatalog.Load();
        string itemName = item != null ? item.itemName : "Slot kosong";
        PlayerStatusSystem status = GetComponent<PlayerStatusSystem>();
        if (item != null && item.equippedTool != PlayerToolType.None && status != null)
            itemName += $" Lv. {status.GetToolLevel(item.equippedTool)}";
        string action = toolHotbar.SelectedTool switch
        {
            PlayerToolType.Hoe => "F: cangkul tanah",
            PlayerToolType.Seed => "F: tanam bibit",
            PlayerToolType.WateringCan => GetWateringCanHint(),
            PlayerToolType.Fertilizer => "F: pupuk tanah",
            PlayerToolType.Sickle => "F: sabit rumput atau tanaman",
            PlayerToolType.Hammer => "F: hancurkan batu",
            PlayerToolType.Axe => "F: tebang pohon",
            PlayerToolType.FishingRod => $"Hold F: Power Cast / Release  |  Bait: {(fishingSystem != null ? fishingSystem.EquippedBaitLabel : "No Bait")}",
            PlayerToolType.CropBooster => "F: gunakan crop booster",
            PlayerToolType.Shears => "Dekati domba lalu G: cukur bulu",
            PlayerToolType.Pitchfork => "Dekati kotoran hewan lalu E: ambil kotoran",
            _ when item != null && item.IsAnimalMedicine => "Dekati hewan sakit lalu tekan F: Give Medicine",
            _ when item != null && item.IsFishingBait =>
                fishingSystem != null && fishingSystem.EquippedBait == item
                    ? "F: lepas bait dari Fishing Rod"
                    : "F: pasang bait pada Fishing Rod",
            _ when item != null && animalCare != null && item == animalCare.fodder => "Dekati box pakan lalu tekan F: isi 1",
            _ when item != null && item.itemId == "item.grass" => "Dekati box pakan lalu tekan F: isi 1",
            _ when item != null && item.itemId == "item.fish_feed" => "Dekati tempat pakan kolam lalu tekan F: isi 1",
            _ when item != null && animalCare != null && item == animalCare.treat => "Dekati hewan lalu tekan F: beri treat",
            _ when item != null && item.category == ItemCategory.Food => "C: makan item terpilih",
            _ when item != null && item.canPlaceInWorld && item.canDropToWorld => "P: letakkan  |  G: jatuhkan  |  Shift: seluruh stack",
            _ when item != null && item.canPlaceInWorld => "P: letakkan item",
            _ when item != null && item.canDropToWorld => "G: jatuhkan item",
            _ => "Pilih slot 1–8 untuk melihat aksi"
        };
        actionHint.text = toolHotbar.SelectedTool == PlayerToolType.WateringCan && wateringCan != null
            ? $"{itemName}\n{wateringCan.CurrentWater} / {wateringCan.MaximumWater}   <size=70%>Air · F: Siram</size>"
            : $"{itemName}\n<size=70%>{action}</size>";
    }

    string GetWateringCanHint()
    {
        return wateringCan != null
            ? $"F: siram tanah atau tanaman  |  Air {wateringCan.CurrentWater}/{wateringCan.MaximumWater}"
            : "F: siram tanah atau tanaman  |  Air 100/100";
    }

    /// <summary>Memakai panel petunjuk hotbar untuk feedback agar tidak muncul dua kotak UI.</summary>
    public static bool TryShowTemporaryMessage(string message, float duration)
    {
        if (instance == null || instance.actionHint == null || string.IsNullOrWhiteSpace(message))
            return false;
        instance.ShowTemporaryMessage(message, duration);
        return true;
    }

    void ShowTemporaryMessage(string message, float duration)
    {
        if (temporaryHintRoutine != null) StopCoroutine(temporaryHintRoutine);
        temporaryHint = message;
        RefreshActionHint();
        temporaryHintRoutine = StartCoroutine(ClearTemporaryMessage(Mathf.Max(0.2f, duration)));
    }

    IEnumerator ClearTemporaryMessage(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        temporaryHint = null;
        temporaryHintRoutine = null;
        RefreshActionHint();
    }

    void BuildUI()
    {
        int count = inventory.HotbarSlotCount;
        const float slotWidth = 88f;
        const float slotHeight = 88f;
        const float gap = 8f;
        float totalWidth = count * slotWidth + (count - 1) * gap + 20f;

        GameObject canvasObject = new("InventoryHotbarCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        pauseVisibility = canvasObject.AddComponent<CanvasGroup>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 235;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f) / .82f;
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform safe = GameplayHUDStyle.Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        canvasObject.transform.SetParent(transform, false);

        RectTransform hintPanel = CreateRect("Selected Tool Info", safe);
        hintRoot = hintPanel;
        hintPanel.anchorMin = hintPanel.anchorMax = new Vector2(0.5f, 0f);
        hintPanel.pivot = new Vector2(0f, 0f);
        hintPanel.anchoredPosition = new Vector2(0f, 160f);
        hintPanel.sizeDelta = new Vector2(300f, 82f);
        Image hintBackground = GameplayHUDStyle.Surface(hintPanel, GameplayHUDStyle.Panel);
        hintBackground.raycastTarget = false;
        actionHint = CreateRect("ActionText", hintPanel).gameObject.AddComponent<TextMeshProUGUI>();
        actionHint.rectTransform.anchorMin = Vector2.zero;
        actionHint.rectTransform.anchorMax = Vector2.one;
        actionHint.rectTransform.offsetMin = new Vector2(14f, 16f);
        actionHint.rectTransform.offsetMax = new Vector2(-14f, -6f);
        actionHint.font = TMP_Settings.defaultFontAsset;
        actionHint.fontSize = 20f;
        actionHint.color = Color.white;
        actionHint.alignment = TextAlignmentOptions.Center;
        actionHint.textWrappingMode = TextWrappingModes.Normal;
        actionHint.enableAutoSizing = true; actionHint.fontSizeMin = 12; actionHint.fontSizeMax = 20;
        actionHint.raycastTarget = false;

        RectTransform waterTrack = GameplayHUDStyle.Rect("Water Gauge", hintPanel, new(.08f,.07f), new(.92f,.13f));
        GameplayHUDStyle.Surface(waterTrack, new Color(.08f,.16f,.20f,.9f), 3);
        waterFill = GameplayHUDStyle.Rect("Fill", waterTrack, Vector2.zero, Vector2.one);
        GameplayHUDStyle.Surface(waterFill, GameplayHUDStyle.Accent, 3).borderWidth = 0;

        RectTransform panel = CreateRect("HotbarPanel", safe);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 36f);
        panel.sizeDelta = new Vector2(totalWidth, slotHeight + 20f);
        Image panelImage = hotbarPanelSprite != null ? panel.gameObject.AddComponent<Image>() : GameplayHUDStyle.Surface(panel, GameplayHUDStyle.Panel);
        panelImage.sprite = hotbarPanelSprite;
        panelImage.type = hotbarPanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = referenceHUDStyle ? GameplayHUDStyle.Panel : panelColor;

        for (int i = 0; i < count; i++)
        {
            int captured = i;
            RectTransform slot = CreateRect($"QuickSlot_{i + 1}", panel);
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 0.5f);
            slot.pivot = new Vector2(0f, 0.5f);
            slot.anchoredPosition = new Vector2(10f + i * (slotWidth + gap), 0f);
            slot.sizeDelta = new Vector2(slotWidth, slotHeight);
            Image background = slotSprite != null ? slot.gameObject.AddComponent<Image>() : GameplayHUDStyle.Surface(slot, GameplayHUDStyle.Slot);
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            // Selection is owned by selectedIndex, not the EventSystem's last focused button.
            // HUD surfaces default to decorative/non-raycastable, but these are actual controls.
            background.raycastTarget = true;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { if (!WorldInteractionPrompt.IsSuppressed) SelectSlot(captured); });
            GameplayHUDStyle.Text("Key", slot, (i + 1).ToString(), 18, new(.09f,.72f), new(.4f,.98f));

            RectTransform iconRect = CreateRect("Icon", slot);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .5f);
            iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = new Vector2(0f, -3f);
            iconRect.sizeDelta = new Vector2(62f, 62f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI label = CreateRect("Label", slot).gameObject.AddComponent<TextMeshProUGUI>();
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(4f, 3f);
            label.rectTransform.offsetMax = new Vector2(-5f, -5f);
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomRight;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            TMP_Text fallback = GameplayHUDStyle.Text("Missing Icon Item Name", slot, "", 15, new(.08f,.15f), new(.92f,.75f));
            fallback.alignment = TextAlignmentOptions.Center;
            views.Add(new SlotView(background, icon, label, fallback));
        }
        RefreshActionHint();
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject target = new(name, typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null && inventory.GetComponent<InventoryHotbarUI>() == null)
            inventory.gameObject.AddComponent<InventoryHotbarUI>();
    }

    /// <summary>Cache background, icon, dan label satu slot hotbar.</summary>
    sealed class SlotView
    {
        public readonly Image Background;
        public readonly Image Icon;
        public readonly TMP_Text Label;
        public readonly TMP_Text Fallback;
        public SlotView(Image background, Image icon, TMP_Text label, TMP_Text fallback) { Background = background; Icon = icon; Label = label; Fallback = fallback; }
    }
}
