using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum FarmShopCategory
{
    Seeds,
    Fertilizer,
    Booster,
    Animals,
    Sell,
    Equipment,
    Bait
}

/// <summary>
/// Farm Shop bergaya farming game: kategori horizontal, grid produk 5x5,
/// detail item, harga, jumlah inventory, dan satu tombol transaksi utama.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Legacy Scene References")]
    public GameObject panel;
    public TMP_Text shopText;

    [Header("Runtime Data")]
    [SerializeField] ShopManager shop;
    [SerializeField] Inventory playerInventory;

    readonly List<ProductSlotView> productSlots = new(25);
    readonly List<ShopProduct> visibleProducts = new(25);
    readonly Dictionary<FarmShopCategory, Button> categoryButtons = new();
    RectTransform runtimeRoot;
    TMP_Text titleText;
    TMP_Text goldText;
    TMP_Text detailNameText;
    TMP_Text detailDescriptionText;
    TMP_Text detailPriceText;
    TMP_Text footerText;
    Image detailIcon;
    Button actionButton;
    TMP_Text actionButtonText;
    FarmShopCategory currentCategory = FarmShopCategory.Seeds;
    int selectedProductIndex;

    static readonly Color Parchment = new(0.91f, 0.82f, 0.61f, 0.98f);
    static readonly Color DarkWood = new(0.22f, 0.12f, 0.065f, 1f);
    static readonly Color MidWood = new(0.42f, 0.24f, 0.11f, 1f);
    static readonly Color LeafGreen = new(0.28f, 0.52f, 0.20f, 1f);
    static readonly Color Cream = new(1f, 0.96f, 0.81f, 1f);

    void Awake()
    {
        // GameplayUI dimuat secara additive di setiap map gameplay. Modal shop tidak
        // boleh ikut terlihat hanya karena scene UI selesai dimuat.
        Hide();
    }

    public void Show()
    {
        ResolveReferences();
        EnsureRuntimeUI();
        WorldInteractionPrompt.AcquireSuppression(this);
        if (panel != null) panel.SetActive(true);
        RefreshCatalog();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    /// <summary>Kompatibilitas pemanggil lama; UI grid mengambil data langsung dari ShopManager.</summary>
    public void SetText(string text)
    {
        if (runtimeRoot == null && shopText != null)
            shopText.text = text;
        else
            RefreshCatalog();
    }

    public bool IsVisible() => panel != null && panel.activeSelf;

    void Update()
    {
        if (!IsVisible() || runtimeRoot == null)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int next = ((int)currentCategory + 1) % 7;
            SelectCategory((FarmShopCategory)next);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-5);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(5);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ExecuteSelectedTransaction();
    }

    void ResolveReferences()
    {
        if (shop == null) shop = FindFirstObjectByType<ShopManager>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
    }

    void EnsureRuntimeUI()
    {
        if (runtimeRoot != null || panel == null || shopText == null)
            return;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1040f, 720f);

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null) panelImage = panel.AddComponent<Image>();
        panelImage.color = DarkWood;

        runtimeRoot = CreateRect("HarvestMoonShop_Runtime", panelRect);
        runtimeRoot.anchorMin = Vector2.zero;
        runtimeRoot.anchorMax = Vector2.one;
        runtimeRoot.offsetMin = new Vector2(12f, 12f);
        runtimeRoot.offsetMax = new Vector2(-12f, -12f);
        AddImage(runtimeRoot, Parchment);

        titleText = CreateText("Title", runtimeRoot, "MORNING DEW FARM SHOP", 30f, FontStyles.Bold,
            new Vector2(24f, -16f), new Vector2(600f, 44f));
        titleText.color = DarkWood;
        goldText = CreateText("Gold", runtimeRoot, "Gold: 0 G", 24f, FontStyles.Bold,
            new Vector2(745f, -18f), new Vector2(245f, 40f));
        goldText.alignment = TextAlignmentOptions.TopRight;
        goldText.color = DarkWood;

        BuildCategoryBar();
        BuildProductGrid();
        BuildDetailPanel();

        footerText = CreateText("Footer", runtimeRoot,
            "Tab: kategori • Arrow: pilih item • Enter/klik: transaksi • E: tutup", 17f,
            FontStyles.Normal, new Vector2(28f, -654f), new Vector2(960f, 32f));
        footerText.color = DarkWood;

        shopText.gameObject.SetActive(false);
    }

    void BuildCategoryBar()
    {
        FarmShopCategory[] categories =
        {
            FarmShopCategory.Seeds,
            FarmShopCategory.Fertilizer,
            FarmShopCategory.Booster,
            FarmShopCategory.Animals,
            FarmShopCategory.Sell,
            FarmShopCategory.Equipment,
            FarmShopCategory.Bait
        };
        string[] labels = { "SEEDS", "FERTILIZER", "BOOSTER", "ANIMALS", "SELL", "EQUIPMENT", "BAIT" };

        for (int i = 0; i < categories.Length; i++)
        {
            FarmShopCategory category = categories[i];
            Button button = CreateButton(
                $"Category_{category}", runtimeRoot, labels[i],
                new Vector2(24f + i * 138f, -72f), new Vector2(130f, 48f),
                () => SelectCategory(category));
            categoryButtons[category] = button;
        }
    }

    void BuildProductGrid()
    {
        RectTransform frame = CreateRect("ProductGridFrame", runtimeRoot);
        SetTopLeft(frame, new Vector2(24f, -134f), new Vector2(622f, 506f));
        AddImage(frame, new Color(0.30f, 0.17f, 0.08f, 0.22f));

        RectTransform grid = CreateRect("ProductGrid_5x5", frame);
        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = new Vector2(12f, 12f);
        grid.offsetMax = new Vector2(-12f, -12f);
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(110f, 86f);
        layout.spacing = new Vector2(9f, 9f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.childAlignment = TextAnchor.UpperLeft;

        for (int index = 0; index < 25; index++)
            productSlots.Add(CreateProductSlot(grid, index));
    }

    void BuildDetailPanel()
    {
        RectTransform detail = CreateRect("ItemDetail", runtimeRoot);
        SetTopLeft(detail, new Vector2(664f, -134f), new Vector2(330f, 506f));
        AddImage(detail, Cream);

        RectTransform iconRoot = CreateRect("IconFrame", detail);
        SetTopLeft(iconRoot, new Vector2(91f, -24f), new Vector2(148f, 148f));
        AddImage(iconRoot, new Color(0.74f, 0.88f, 0.60f, 1f));
        detailIcon = CreateRect("Icon", iconRoot).gameObject.AddComponent<Image>();
        RectTransform iconRect = detailIcon.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(16f, 16f);
        iconRect.offsetMax = new Vector2(-16f, -16f);
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;

        detailNameText = CreateText("ItemName", detail, "Pilih item", 24f, FontStyles.Bold,
            new Vector2(16f, -188f), new Vector2(298f, 38f));
        detailNameText.alignment = TextAlignmentOptions.Center;
        detailNameText.color = DarkWood;
        detailDescriptionText = CreateText("Description", detail, string.Empty, 17f, FontStyles.Normal,
            new Vector2(20f, -236f), new Vector2(290f, 126f));
        detailDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        detailDescriptionText.color = DarkWood;
        detailPriceText = CreateText("Price", detail, string.Empty, 20f, FontStyles.Bold,
            new Vector2(20f, -372f), new Vector2(290f, 34f));
        detailPriceText.alignment = TextAlignmentOptions.Center;
        detailPriceText.color = DarkWood;

        actionButton = CreateButton("Action", detail, "BUY", new Vector2(42f, -426f),
            new Vector2(246f, 56f), ExecuteSelectedTransaction);
        actionButtonText = actionButton.GetComponentInChildren<TMP_Text>();
    }

    void SelectCategory(FarmShopCategory category)
    {
        currentCategory = category;
        selectedProductIndex = 0;
        RefreshCatalog();
    }

    void RefreshCatalog()
    {
        if (runtimeRoot == null || shop == null)
            return;

        visibleProducts.Clear();
        switch (currentCategory)
        {
            case FarmShopCategory.Seeds:
                AddProduct(shop.seedItem, false);
                if (shop.sellsFarmEquipment)
                    foreach (ItemSO seed in shop.treeSeedItems) AddProduct(seed, false);
                break;
            case FarmShopCategory.Equipment:
                if (shop.sellsFarmEquipment)
                    foreach (ItemSO sprinkler in shop.sprinklerItems) AddProduct(sprinkler, false);
                break;
            case FarmShopCategory.Bait:
                foreach (ItemSO bait in shop.baitItems) AddProduct(bait, false);
                break;
            case FarmShopCategory.Fertilizer:
                for (int i = 0; i < shop.fertilizerItems.Count; i++) AddProduct(shop.fertilizerItems[i], false);
                break;
            case FarmShopCategory.Booster:
                foreach (ItemSO booster in shop.cropBoosterItems) AddProduct(booster, false);
                break;
            case FarmShopCategory.Animals:
                for (int i = 0; i < shop.animalOffers.Count; i++) AddAnimalProduct(shop.animalOffers[i]);
                AnimalCareCatalog care = AnimalCareCatalog.Load();
                if (care != null)
                {
                    AddProduct(care.fodder, false);
                    AddProduct(care.treat, false);
                    for (int level = 1; level <= 3; level++) AddProduct(care.Medicine((AnimalMedicineLevel)level), false);
                }
                break;
            case FarmShopCategory.Sell:
                AddProduct(shop.cabbageItem, true);
                AddProduct(shop.milkItem, true);
                AnimalCareCatalog products = AnimalCareCatalog.Load();
                if (products != null) foreach (ItemSO product in products.products) AddProduct(product, true);
                foreach (ItemSO seed in shop.treeSeedItems)
                    if (seed != null && seed.treeDefinition != null) AddProduct(seed.treeDefinition.fruitItem, true);
                break;
        }

        selectedProductIndex = visibleProducts.Count == 0
            ? -1
            : Mathf.Clamp(selectedProductIndex, 0, visibleProducts.Count - 1);

        for (int i = 0; i < productSlots.Count; i++)
            productSlots[i].Bind(
                i < visibleProducts.Count ? visibleProducts[i] : default,
                i,
                i == selectedProductIndex,
                SelectProduct
            );

        foreach (KeyValuePair<FarmShopCategory, Button> entry in categoryButtons)
        {
            ColorBlock colors = entry.Value.colors;
            colors.normalColor = entry.Key == currentCategory ? LeafGreen : MidWood;
            entry.Value.colors = colors;
        }

        goldText.text = ScoreManager.Instance != null ? $"Gold: {ScoreManager.Instance.points} G" : "Gold: --";
        RefreshDetails();
    }

    void AddProduct(ItemSO item, bool selling)
    {
        // Catalog dan slot legacy dapat menunjuk item yang sama.
        if (visibleProducts.Exists(product => product.Item == item && product.Selling == selling)) return;
        if (item != null && visibleProducts.Count < 25)
            visibleProducts.Add(new ShopProduct(item, selling));
    }

    void AddAnimalProduct(AnimalShopOffer offer)
    {
        if (offer != null && visibleProducts.Count < 25)
            visibleProducts.Add(new ShopProduct(offer));
    }

    void SelectProduct(int index)
    {
        if (index < 0 || index >= visibleProducts.Count)
            return;
        selectedProductIndex = index;
        RefreshCatalog();
    }

    void MoveSelection(int offset)
    {
        if (visibleProducts.Count == 0)
            return;
        selectedProductIndex = Mathf.Clamp(selectedProductIndex + offset, 0, visibleProducts.Count - 1);
        RefreshCatalog();
    }

    void RefreshDetails()
    {
        bool valid = selectedProductIndex >= 0 && selectedProductIndex < visibleProducts.Count;
        actionButton.interactable = valid;
        if (!valid)
        {
            detailNameText.text = "Belum ada item";
            detailDescriptionText.text = "Produk kategori ini belum tersedia.";
            detailPriceText.text = string.Empty;
            detailIcon.sprite = null;
            detailIcon.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        ShopProduct product = visibleProducts[selectedProductIndex];
        bool locked = IsLocked(product);
        int owned = product.Item != null && playerInventory != null ? playerInventory.GetCount(product.Item) : 0;
        detailNameText.text = product.DisplayName;
        detailDescriptionText.text = BuildDescription(product, owned, locked);
        detailPriceText.text = product.Selling ? $"Sell: {product.Price} G" : $"Price: {product.Price} G";
        detailIcon.sprite = product.Icon;
        detailIcon.color = product.Icon != null ? Color.white : LeafGreen;
        actionButton.interactable = !locked && (product.Selling ? owned > 0 : product.Price >= 0);
        actionButtonText.text = product.Selling ? "SELL 1" : locked ? "LOCKED" : "BUY 1";
    }

    string BuildDescription(ShopProduct product, int owned, bool locked)
    {
        if (locked)
        {
            if (product.Item != null && product.Item.IsFishingBait)
                return $"Terbuka pada Fishing Lv.{product.Item.requiredFishingLevel} dan Village Lv.{product.Item.requiredVillageLevel}.";
            return $"Terbuka pada Village Lv.{product.RequiredVillageLevel}.";
        }
        if (product.Animal != null)
        {
            AnimalShopOffer offer = product.Animal;
            int adultDays = offer.growthProfile != null
                ? offer.growthProfile.bornToAdultDays
                : AnimalGrowthProfileSO.DefaultBornToAdultDays(offer.animalType);
            int remainingDays = offer.offerKind == AnimalShopOfferKind.Egg
                ? adultDays
                : offer.growthProfile != null
                    ? offer.growthProfile.purchasedYoungToAdultDays
                    : AnimalGrowthProfileSO.DefaultPurchasedToAdultDays(offer.animalType);
            string source = offer.offerKind == AnimalShopOfferKind.Egg
                ? "Egg: mulai dari incubation lalu Hatchling"
                : "Anakan: sudah melewati sebagian growth";
            return $"{source}.\nAdult sekitar {remainingDays} growth days setelah fase awal. " +
                   "Growth hanya maju jika Fed + Healthy + Sheltered.";
        }

        ItemSO item = product.Item;
        if (product.Selling)
            return $"Jual hasil pertanian dari inventory.\n\nOwned: {owned}";
        if (item.IsSprinkler)
            return $"Auto water {new[] { 0, 4, 8, 24, 48 }[Mathf.Clamp(item.sprinklerLevel, 1, 4)]} tile/hari. P: pasang di farm; E: ambil kembali. Tanpa stamina.\n\nOwned: {owned}";
        if (item.treeDefinition != null)
            return $"P: tanam di tile farm kosong. Dewasa dalam {item.treeDefinition.matureDays} growth days. Siram saat muda; E untuk panen buah.\n\nOwned: {owned}";
        if (item.IsFertilizer)
            return $"Memulihkan +{item.SoilRestoreAmount} durability tanah (maksimum 80). Tidak bisa dijual.\n\nOwned: {owned}";
        if (item.IsCropBooster)
            return $"Booster kualitas Lv.{item.cropBoosterLevel}. Gunakan setiap hari; growth/yield dan soil tetap.\n\nOwned: {owned}";
        if (item.IsAnimalMedicine)
            return item.animalMedicineLevel switch
            {
                AnimalMedicineLevel.Basic => $"Untuk Unwell. Obat terlalu lemah hanya menurunkan tingkat penyakit.\n\nOwned: {owned}",
                AnimalMedicineLevel.Strong => $"Untuk Sick. Recovery dimulai setelah diberikan.\n\nOwned: {owned}",
                AnimalMedicineLevel.Premium => $"Untuk Severely Sick akibat cuaca ekstrem.\n\nOwned: {owned}",
                _ => $"Obat hewan.\n\nOwned: {owned}"
            };
        if (item.IsFishingBait)
            return $"Bite Speed +{item.baitBiteSpeedBonus * 100f:0}%. " +
                   $"Rare x{item.baitRareWeightMultiplier:0.##}, Legendary x{item.baitLegendaryWeightMultiplier:0.##}.\n" +
                   $"Butuh Fishing Lv.{item.requiredFishingLevel}. Terpakai 1 setiap cast.\n\nOwned: {owned}";
        if (item.category == ItemCategory.Seed)
            return $"Tanam pada tanah yang sudah dicangkul, lalu siram setiap hari.\n\nOwned: {owned}";
        return $"Item Farm Shop.\n\nOwned: {owned}";
    }

    bool IsLocked(ShopProduct product)
    {
        if (product.Animal != null)
            return VillageProgressionService.Instance != null &&
                   !VillageProgressionService.Instance.MeetsRequirement(product.Animal.requiredVillageLevel);

        ItemSO item = product.Item;
        if (item == null) return true;
        bool villageLocked = item.requiredVillageLevel > 1 && (VillageProgressionService.Instance == null ||
               !VillageProgressionService.Instance.MeetsRequirement(item.requiredVillageLevel));
        FishingSystem fishing = playerInventory != null ? playerInventory.GetComponent<FishingSystem>() : null;
        int fishingLevel = fishing != null ? fishing.FishingLevel : 1;
        bool fishingLocked = item.IsFishingBait &&
            !ProgressionRequirementSettings.MeetsFishingLevel(fishingLevel, item.requiredFishingLevel);
        return villageLocked || fishingLocked;
    }

    void ExecuteSelectedTransaction()
    {
        if (selectedProductIndex < 0 || selectedProductIndex >= visibleProducts.Count || shop == null)
            return;
        ShopProduct product = visibleProducts[selectedProductIndex];
        bool success = product.Animal != null
            ? shop.TryBuyAnimal(product.Animal)
            : product.Selling
                ? shop.TrySellItem(product.Item)
                : product.Item != null && product.Item.IsFishingBait
                    ? shop.TryBuyBait(product.Item)
                    : shop.TryBuyItem(product.Item);
        SaveLoadFeedback.Instance?.ShowMessage(success
            ? $"{(product.Selling ? "Dijual" : "Dibeli")}: {product.DisplayName}"
            : "Transaksi gagal: cek Gold, kapasitas, Village Level, atau inventory");
        RefreshCatalog();
    }

    ProductSlotView CreateProductSlot(RectTransform parent, int index)
    {
        RectTransform root = CreateRect($"ProductSlot_{index + 1:00}", parent);
        Image background = AddImage(root, Cream);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.75f, 0.91f, 0.58f, 1f);
        colors.selectedColor = LeafGreen;
        button.colors = colors;

        RectTransform iconRoot = CreateRect("Icon", root);
        SetTopLeft(iconRoot, new Vector2(8f, -7f), new Vector2(38f, 38f));
        Image icon = iconRoot.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TMP_Text label = CreateText("Label", root, string.Empty, 13f, FontStyles.Bold,
            new Vector2(5f, -49f), new Vector2(100f, 31f));
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = DarkWood;
        return new ProductSlotView(root, background, button, icon, label);
    }

    Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform root = CreateRect(name, parent);
        SetTopLeft(root, position, size);
        Image background = AddImage(root, MidWood);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.normalColor = MidWood;
        colors.highlightedColor = LeafGreen;
        colors.pressedColor = new Color(0.18f, 0.40f, 0.14f, 1f);
        colors.disabledColor = new Color(0.34f, 0.31f, 0.25f, 0.75f);
        button.colors = colors;
        TMP_Text text = CreateText("Label", root, label, 18f, FontStyles.Bold, Vector2.zero, size);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return button;
    }

    TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style,
        Vector2 position, Vector2 dimensions)
    {
        TMP_Text text = Instantiate(shopText, parent);
        text.name = name;
        text.gameObject.SetActive(true);
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform rect = text.rectTransform;
        SetTopLeft(rect, position, dimensions);
        rect.localScale = Vector3.one;
        return text;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Image AddImage(RectTransform root, Color color)
    {
        Image image = root.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    void OnDisable() => WorldInteractionPrompt.ReleaseSuppression(this);

    readonly struct ShopProduct
    {
        public readonly ItemSO Item;
        public readonly AnimalShopOffer Animal;
        public readonly bool Selling;
        public string DisplayName => Item != null ? Item.itemName : Animal?.displayName ?? string.Empty;
        public Sprite Icon => Item != null ? Item.icon : Animal?.icon;
        public int Price => Item != null ? (Selling ? Item.sellPrice : Item.buyPrice) : Animal?.price ?? 0;
        public int RequiredVillageLevel => Item != null ? Item.requiredVillageLevel : Animal?.requiredVillageLevel ?? 1;
        public bool IsValid => Item != null || Animal != null;

        public ShopProduct(ItemSO item, bool selling)
        {
            Item = item;
            Animal = null;
            Selling = selling;
        }

        public ShopProduct(AnimalShopOffer animal)
        {
            Item = null;
            Animal = animal;
            Selling = false;
        }
    }

    sealed class ProductSlotView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly Button button;
        readonly Image icon;
        readonly TMP_Text label;

        public ProductSlotView(RectTransform root, Image background, Button button, Image icon, TMP_Text label)
        {
            this.root = root;
            this.background = background;
            this.button = button;
            this.icon = icon;
            this.label = label;
        }

        public void Bind(ShopProduct product, int index, bool selected, System.Action<int> onSelected)
        {
            bool occupied = product.IsValid;
            button.interactable = occupied;
            button.onClick.RemoveAllListeners();
            if (occupied) button.onClick.AddListener(() => onSelected(index));
            icon.sprite = occupied ? product.Icon : null;
            icon.color = occupied && product.Icon != null
                ? Color.white
                : occupied ? LeafGreen : new Color(1f, 1f, 1f, 0f);
            label.text = occupied
                ? $"{product.DisplayName}\n{product.Price} G"
                : string.Empty;
            background.color = occupied
                ? selected ? LeafGreen : Cream
                : new Color(0.72f, 0.65f, 0.50f, 0.38f);
            label.color = occupied && selected ? Color.white : DarkWood;
            root.gameObject.SetActive(true);
        }
    }
}
