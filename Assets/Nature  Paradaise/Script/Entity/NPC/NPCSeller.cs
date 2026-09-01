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
        if (distance > radius)
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

        if (Input.GetKeyDown(interactKey))
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

        string text =
            "FARM SHOP\n\n" +

            $"Gold: {gold}\n\n" +

            $"Seed: {seedCount}\n" +
            $"Cabbage: {cabbageCount}\n" +
            $"Milk: {milkCount}\n\n" +

            $"Q - Buy Seed ({seedPrice}G)\n" +
            $"R - Sell Cabbage ({cabbagePrice}G)\n" +
            $"T - Sell Milk ({milkPrice}G)\n\n" +

            "E - Close";

        shopUI.SetText(text);
    }
}
