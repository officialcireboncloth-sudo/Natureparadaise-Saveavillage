using UnityEngine;

/// <summary>Interaksi NPC seller yang membuka panel shop dan meneruskan input transaksi.</summary>
public class NPCSeller : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float radius = 2f;

    [Header("Shop Controls")]
    public KeyCode buyKey = KeyCode.Q;
    public KeyCode sellCabbageKey = KeyCode.R;
    public KeyCode sellMilkKey = KeyCode.T;
    public KeyCode fertilizerLevel1Key = KeyCode.Alpha5;
    public KeyCode fertilizerLevel2Key = KeyCode.Alpha6;
    public KeyCode fertilizerLevel3Key = KeyCode.Alpha7;
    public KeyCode fertilizerLevel4Key = KeyCode.Alpha8;
    public KeyCode cropBoosterKey = KeyCode.Alpha9;

    [Header("References")]
    public Inventory playerInv;
    public ShopManager shop;
    public ShopUI shopUI;

    bool shopOpen = false;
    PlayerController movement;
    TimeManager timeManager;

    void Awake()
    {
        // Cari Inventory otomatis
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();

        // Cari ShopManager otomatis
        if (shop == null)
            shop = FindFirstObjectByType<ShopManager>();

        // Cari ShopUI otomatis
        if (shopUI == null)
            shopUI = FindFirstObjectByType<ShopUI>();

        movement = playerInv != null ? playerInv.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
    }

    void Start()
    {
        CloseShop();
    }

    void Update()
    {
        if (playerInv == null)
            return;

        // =====================================================
        // CEK JARAK PLAYER
        // =====================================================

        float distance = Vector3.Distance(
            transform.position,
            playerInv.transform.position
        );

        // Kalau player menjauh, tutup shop
        if (!shopOpen && !PlayerInteractionTarget.Contains(playerInv.transform, transform))
        {
            if (shopOpen)
                CloseShop();

            return;
        }

        if (!shopOpen)
        {
            WorldInteractionPrompt.Request(
                this,
                transform,
                "Tekan E untuk membuka toko",
                distance,
                1.65f
            );
        }

        // =====================================================
        // E = OPEN / CLOSE SHOP
        // =====================================================

        if (shopOpen ? Input.GetKeyDown(interactKey) : PlayerInteractionTarget.Press(playerInv.transform, transform, interactKey))
        {
            if (shopOpen)
                CloseShop();
            else
                OpenShop();

            return;
        }

        // Kalau shop belum dibuka,
        // Q/R/T tidak melakukan apa-apa.
        if (!shopOpen)
            return;

        // =====================================================
        // Q = BUY SEED
        // =====================================================

        if (Input.GetKeyDown(buyKey))
        {
            BuySeed();
            return;
        }

        // =====================================================
        // R = SELL CABBAGE
        // =====================================================

        if (Input.GetKeyDown(sellCabbageKey))
        {
            SellCabbage();
            return;
        }

        // =====================================================
        // T = SELL MILK
        // =====================================================

        if (Input.GetKeyDown(sellMilkKey))
        {
            SellMilk();
            return;
        }

        if (Input.GetKeyDown(fertilizerLevel1Key)) BuyFertilizer(1);
        else if (Input.GetKeyDown(fertilizerLevel2Key)) BuyFertilizer(2);
        else if (Input.GetKeyDown(fertilizerLevel3Key)) BuyFertilizer(3);
        else if (Input.GetKeyDown(fertilizerLevel4Key)) BuyFertilizer(4);
        else if (Input.GetKeyDown(cropBoosterKey)) BuyCropBooster();
    }

    // =====================================================
    // OPEN SHOP
    // =====================================================

    void OpenShop()
    {
        shopOpen = true;
        movement?.AcquireMovementLock(this);
        if (timeManager == null) timeManager = TimeManager.Instance;
        timeManager?.AcquirePause(this);

        if (shopUI != null)
        {
            shopUI.Show();
            UpdateShopUI();
        }

        Debug.Log(
            "[SHOP] Shop dibuka."
        );
    }

    // =====================================================
    // CLOSE SHOP
    // =====================================================

    void CloseShop()
    {
        shopOpen = false;
        movement?.ReleaseMovementLock(this);
        timeManager?.ReleasePause(this);

        if (shopUI != null)
            shopUI.Hide();

        Debug.Log(
            "[SHOP] Shop ditutup."
        );
    }

    void OnDisable() => CloseShop();

    // =====================================================
    // BUY SEED
    // =====================================================

    void BuySeed()
    {
        if (shop == null)
        {
            Debug.LogWarning(
                "[SHOP] ShopManager tidak ditemukan."
            );
            return;
        }

        bool success = shop.BuySeed(1);

        if (success)
        {
            Debug.Log(
                "[SHOP] Berhasil membeli 1 Seed."
            );
        }
        else
        {
            Debug.Log(
                "[SHOP] Gagal membeli Seed."
            );
        }

        UpdateShopUI();
    }

    void BuyFertilizer(int level)
    {
        if (shop == null)
            return;

        bool success = shop.BuyFertilizer(level);
        Debug.Log(success
            ? $"[SHOP] Berhasil membeli Fertilizer Lv.{level}."
            : $"[SHOP] Gagal membeli Fertilizer Lv.{level}.");
        UpdateShopUI();
    }

    void BuyCropBooster()
    {
        if (shop == null)
            return;
        bool success = shop.BuyCropBooster();
        Debug.Log(success ? "[SHOP] Crop Booster dibeli." : "[SHOP] Gagal membeli Crop Booster.");
        UpdateShopUI();
    }

    // =====================================================
    // SELL CABBAGE
    // =====================================================

    void SellCabbage()
    {
        if (shop == null)
        {
            Debug.LogWarning(
                "[SHOP] ShopManager tidak ditemukan."
            );
            return;
        }

        bool success = shop.SellCabbage(1);

        if (success)
        {
            Debug.Log(
                "[SHOP] Berhasil menjual 1 Cabbage."
            );
        }
        else
        {
            Debug.Log(
                "[SHOP] Tidak punya Cabbage."
            );
        }

        UpdateShopUI();
    }

    // =====================================================
    // SELL MILK
    // =====================================================

    void SellMilk()
    {
        if (shop == null)
        {
            Debug.LogWarning(
                "[SHOP] ShopManager tidak ditemukan."
            );
            return;
        }

        bool success = shop.SellMilk(1);

        if (success)
        {
            Debug.Log(
                "[SHOP] Berhasil menjual 1 Milk."
            );
        }
        else
        {
            Debug.Log(
                "[SHOP] Tidak punya Milk."
            );
        }

        UpdateShopUI();
    }

    // =====================================================
    // UPDATE SHOP UI
    // =====================================================

    void UpdateShopUI()
    {
        if (shopUI == null)
            return;

        if (shop == null)
            return;

        int seedCount = 0;
        int cabbageCount = 0;
        int milkCount = 0;
        int gold = 0;

        // =====================================================
        // INVENTORY COUNT
        // =====================================================

        if (playerInv != null)
        {
            if (shop.seedItem != null)
            {
                seedCount =
                    playerInv.GetCount(shop.seedItem);
            }

            if (shop.cabbageItem != null)
            {
                cabbageCount =
                    playerInv.GetCount(shop.cabbageItem);
            }

            if (shop.milkItem != null)
            {
                milkCount =
                    playerInv.GetCount(shop.milkItem);
            }
        }

        // =====================================================
        // GOLD
        // =====================================================

        if (ScoreManager.Instance != null)
        {
            gold = ScoreManager.Instance.points;
        }

        // =====================================================
        // PRICES
        // =====================================================

        int seedPrice = 0;
        int cabbagePrice = 0;
        int milkPrice = 0;

        if (shop.seedItem != null)
        {
            seedPrice =
                shop.seedItem.buyPrice;
        }

        if (shop.cabbageItem != null)
        {
            cabbagePrice =
                shop.cabbageItem.sellPrice;
        }

        if (shop.milkItem != null)
        {
            milkPrice =
                shop.milkItem.sellPrice;
        }

        // =====================================================
        // UI TEXT
        // =====================================================

        string fertilizerText = "";
        for (int level = 1; level <= 4; level++)
        {
            ItemSO fertilizer = shop.GetFertilizerItem(level);
            if (fertilizer == null)
                continue;

            bool unlocked = VillageProgressionService.Instance == null ||
                            VillageProgressionService.Instance.MeetsRequirement(fertilizer.requiredVillageLevel);
            string key = (level + 4).ToString();
            fertilizerText += unlocked
                ? $"{key} - Buy {fertilizer.itemName} ({fertilizer.buyPrice}G)\n"
                : $"{key} - {fertilizer.itemName} [Village Lv.{fertilizer.requiredVillageLevel}]\n";
        }

        string text =
            "FARM SHOP\n\n" +

            $"Gold: {gold}\n\n" +

            $"Seed: {seedCount}\n" +
            $"Cabbage: {cabbageCount}\n" +
            $"Milk: {milkCount}\n\n" +

            $"Q - Buy Seed ({seedPrice}G)\n" +
            $"R - Sell Cabbage ({cabbagePrice}G)\n" +
            $"T - Sell Milk ({milkPrice}G)\n\n" +

            fertilizerText +
            (shop.cropBoosterItem != null
                ? $"9 - Buy {shop.cropBoosterItem.itemName} ({shop.cropBoosterItem.buyPrice}G)\n\n"
                : "\n") +

            "E - Close";

        shopUI.SetText(text);
    }
}
