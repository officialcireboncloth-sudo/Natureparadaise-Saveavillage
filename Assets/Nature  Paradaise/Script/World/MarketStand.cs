using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class MarketStandListing
{
    public ItemSO item;
    [Min(1)] public int count = 1;
    [Range(0, 5)] public int qualityStars;
    [Min(0f)] public float fishSizeCm;
    public int listedAbsoluteHour;

    public int UnitPrice => item != null ? item.GetMarketSellPrice(qualityStars, fishSizeCm) : 0;
    public string DisplayName => item == null ? "Item hilang" : new ItemStack
    {
        item = item,
        count = count,
        qualityStars = qualityStars,
        fishSizeCm = fishSizeCm
    }.DisplayName;
}

[Serializable]
public sealed class MarketStandListingSaveData
{
    public string itemId;
    public string assetName;
    public string itemName;
    public int count;
    public int qualityStars;
    public float fishSizeCm;
    public int listedAbsoluteHour;
}

[Serializable]
public sealed class MarketStandSaveData
{
    public string id;
    public List<MarketStandListingSaveData> listings;
    public int lastProcessedAbsoluteHour;
    public int lifetimeRevenue;
}

/// <summary>
/// Storage jual pasif. Player menitipkan item sellable, lalu satu simulasi calon
/// pembeli berjalan setiap jam pasar walaupun player berada jauh dari stand.
/// </summary>
[DisallowMultipleComponent]
public sealed class MarketStand : MonoBehaviour
{
    static readonly List<MarketStand> Active = new();

    [Header("Identity")]
    [SerializeField] string standId = "village-market-stand-01";

    [Header("Interaction")]
    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0f)] float promptHeight = 2.2f;

    [Header("Storage")]
    [SerializeField, Min(1)] int listingCapacity = 12;
    [SerializeField] List<MarketStandListing> listings = new();

    [Header("Buyer Simulation")]
    [SerializeField, Range(0f, 1f)] float baseHourlyBuyerChance = 0.24f;
    [SerializeField, Range(0f, 0.25f)] float chancePerVillageLevel = 0.06f;
    [SerializeField, Range(0, 23)] int marketOpenHour = 8;
    [SerializeField, Range(0, 23)] int marketCloseHour = 20;
    [SerializeField, Min(1)] int maximumItemsPerSale = 3;

    [Header("Runtime State")]
    [SerializeField] int lastProcessedAbsoluteHour = -1;
    [SerializeField, Min(0)] int lifetimeRevenue;

    readonly Rect windowRectDefault = new(0f, 0f, 980f, 650f);
    Rect windowRect;
    Vector2 inventoryScroll;
    Vector2 standScroll;
    bool panelOpen;
    string feedback = string.Empty;

    public string StandId => standId;
    public IReadOnlyList<MarketStandListing> Listings => listings;
    public int LifetimeRevenue => lifetimeRevenue;
    public int TotalItemCount => listings.Sum(entry => entry != null ? Mathf.Max(0, entry.count) : 0);
    public float CurrentBuyerChance => Mathf.Clamp01(baseHourlyBuyerChance +
        Mathf.Max(0, CurrentVillageLevel - 1) * chancePerVillageLevel);
    int CurrentVillageLevel => VillageProgressionService.Instance != null
        ? VillageProgressionService.Instance.VillageLevel : 1;
    int CurrentAbsoluteHour => TimeManager.Instance == null ? 0 :
        Mathf.Max(0, (TimeManager.Instance.day - 1) * 24 + TimeManager.Instance.hour);

    public static bool BlocksWorldPointer
    {
        get
        {
            Vector2 pointer = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return Active.Any(stand => stand != null && stand.panelOpen && stand.windowRect.Contains(pointer));
        }
    }

    void Awake()
    {
        ResolveInventory();
        NormalizeListings();
        windowRect = windowRectDefault;
        CenterWindow();
        if (lastProcessedAbsoluteHour < 0)
            lastProcessedAbsoluteHour = CurrentAbsoluteHour;
    }

    void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
        TimeManager.OnHour += ProcessElapsedSales;
    }

    void Start() => ProcessElapsedSales();

    void OnDisable()
    {
        TimeManager.OnHour -= ProcessElapsedSales;
        Active.Remove(this);
        ClosePanel();
    }

    void Update()
    {
        ResolveInventory();
        if (playerInventory == null)
            return;

        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey))
                ClosePanel();
            return;
        }

        if (!PlayerInteractionTarget.Contains(playerInventory.transform, transform))
            return;

        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform,
            $"{interactKey}: Market Stand | Stok {TotalItemCount} item", distance, promptHeight);
        if (PlayerInteractionTarget.Press(playerInventory.transform, transform, interactKey))
            OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen)
            return;

        float width = Mathf.Min(windowRectDefault.width, Screen.width - 24f);
        float height = Mathf.Min(windowRectDefault.height, Screen.height - 24f);
        windowRect.width = Mathf.Max(620f, width);
        windowRect.height = Mathf.Max(420f, height);
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "MARKET STAND — STAND STORAGE");
    }

    void DrawWindow(int id)
    {
        GUILayout.Label($"Gold: {(ScoreManager.Instance != null ? ScoreManager.Instance.points : 0)} G   |   " +
                        $"Village Lv.{CurrentVillageLevel}   |   Peluang pembeli/jam: {CurrentBuyerChance:P0}   |   " +
                        $"Total pendapatan stand: {lifetimeRevenue} G");
        GUILayout.Label("Barang yang dititipkan tetap For Sale sampai laku atau diambil kembali.");
        GUILayout.Space(6f);

        GUILayout.BeginHorizontal();
        DrawInventoryColumn();
        GUILayout.Space(12f);
        DrawStandColumn();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        if (!string.IsNullOrWhiteSpace(feedback))
            GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(32f)))
            ClosePanel();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void DrawInventoryColumn()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 42f) * 0.5f));
        GUILayout.Label("INVENTORY — pilih barang untuk dijual");
        inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);

        bool found = false;
        if (playerInventory != null)
        {
            for (int index = 0; index < playerInventory.slots.Count; index++)
            {
                ItemStack stack = playerInventory.GetSlot(index);
                if (stack == null || stack.item == null || stack.count <= 0 || !stack.item.CanSellAtMarket)
                    continue;

                found = true;
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{stack.DisplayName} x{stack.count}\n{stack.item.GetMarketSellPrice(stack.qualityStars, stack.fishSizeCm)} G/item",
                    GUILayout.MinWidth(220f));
                if (GUILayout.Button("+1", GUILayout.Width(52f), GUILayout.Height(38f)))
                    TryDepositFromSlot(index, 1);
                if (GUILayout.Button("Semua", GUILayout.Width(70f), GUILayout.Height(38f)))
                    TryDepositFromSlot(index, stack.count);
                GUILayout.EndHorizontal();
            }
        }

        if (!found)
            GUILayout.Label("Tidak ada barang sellable. Tool, Quest Item, Key Item, dan item berharga 0 tidak diterima.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    void DrawStandColumn()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 42f) * 0.5f));
        GUILayout.Label($"FOR SALE — {listings.Count}/{listingCapacity} jenis, {TotalItemCount} item");
        standScroll = GUILayout.BeginScrollView(standScroll);

        if (listings.Count == 0)
            GUILayout.Label("Stand masih kosong.");

        for (int index = listings.Count - 1; index >= 0; index--)
        {
            MarketStandListing listing = listings[index];
            if (listing == null || listing.item == null || listing.count <= 0)
                continue;

            GUILayout.BeginHorizontal(GUI.skin.box);
            int hoursRemaining = Mathf.Max(0, 7 * 24 - (CurrentAbsoluteHour - listing.listedAbsoluteHour));
            GUILayout.Label($"{listing.DisplayName} x{listing.count}\n{listing.UnitPrice} G/item | maks. {Mathf.CeilToInt(hoursRemaining / 24f)} hari",
                GUILayout.MinWidth(220f));
            if (GUILayout.Button("Ambil 1", GUILayout.Width(68f), GUILayout.Height(38f)))
                TryWithdraw(index, 1);
            if (GUILayout.Button("Semua", GUILayout.Width(70f), GUILayout.Height(38f)))
                TryWithdraw(index, listing.count);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    public bool TryDepositFromSlot(int slotIndex, int amount)
    {
        ResolveInventory();
        ItemStack stack = playerInventory != null ? playerInventory.GetSlot(slotIndex) : null;
        if (stack == null || stack.item == null || stack.count <= 0 || amount <= 0 || !stack.item.CanSellAtMarket)
        {
            feedback = "Barang ini tidak dapat dijual di Market Stand.";
            return false;
        }

        int moved = Mathf.Min(amount, stack.count);
        MarketStandListing listing = FindCompatible(stack.item, stack.qualityStars, stack.fishSizeCm);
        if (listing == null && listings.Count >= listingCapacity)
        {
            feedback = "Slot Market Stand penuh.";
            return false;
        }

        ItemSO item = stack.item;
        int quality = stack.qualityStars;
        float fishSize = stack.fishSizeCm;
        if (!playerInventory.RemoveFromSlot(slotIndex, moved))
            return false;

        if (listing == null)
        {
            listing = new MarketStandListing
            {
                item = item,
                count = 0,
                qualityStars = quality,
                fishSizeCm = fishSize,
                listedAbsoluteHour = CurrentAbsoluteHour
            };
            listings.Add(listing);
        }

        listing.count += moved;
        feedback = $"{listing.DisplayName} x{moved} sekarang For Sale — {listing.UnitPrice} G/item.";
        return true;
    }

    public bool TryWithdraw(int listingIndex, int amount)
    {
        ResolveInventory();
        if (playerInventory == null || listingIndex < 0 || listingIndex >= listings.Count || amount <= 0)
            return false;

        MarketStandListing listing = listings[listingIndex];
        if (listing == null || listing.item == null || listing.count <= 0)
            return false;

        int moved = Mathf.Min(amount, listing.count);
        if (!playerInventory.CanAdd(listing.item, moved, listing.qualityStars, listing.fishSizeCm))
        {
            feedback = "Tas tidak mempunyai ruang untuk mengambil barang ini.";
            return false;
        }

        playerInventory.Add(listing.item, moved, listing.qualityStars, listing.fishSizeCm);
        listing.count -= moved;
        feedback = $"{listing.DisplayName} x{moved} dikembalikan ke Inventory.";
        if (listing.count <= 0)
            listings.RemoveAt(listingIndex);
        return true;
    }

    void ProcessElapsedSales()
    {
        if (TimeManager.Instance == null || ScoreManager.Instance == null)
            return;

        int now = CurrentAbsoluteHour;
        if (lastProcessedAbsoluteHour < 0 || lastProcessedAbsoluteHour > now)
            lastProcessedAbsoluteHour = now;

        int first = lastProcessedAbsoluteHour + 1;
        for (int absoluteHour = first; absoluteHour <= now; absoluteHour++)
        {
            int hourOfDay = absoluteHour % 24;
            if (hourOfDay >= marketOpenHour && hourOfDay < marketCloseHour)
                SimulateBuyer(absoluteHour == now);
        }

        // Semua listing yang mencapai batas tujuh hari diselesaikan pada tick yang sama.
        // Dengan begitu beberapa stack yang dipasang bersamaan tidak melewati deadline.
        int overdueCount = listings.Count(entry => entry != null && now - entry.listedAbsoluteHour >= 7 * 24);
        for (int i = 0; i < overdueCount; i++)
            SimulateBuyer(true);

        lastProcessedAbsoluteHour = now;
    }

    void SimulateBuyer(bool notify)
    {
        NormalizeListings();
        if (listings.Count == 0)
            return;

        int listingIndex = listings.FindIndex(entry => entry != null &&
            CurrentAbsoluteHour - entry.listedAbsoluteHour >= 7 * 24);
        bool guaranteedSale = listingIndex >= 0;
        if (!guaranteedSale)
        {
            // Barang minimal menginap satu hari agar tidak langsung laku sesaat setelah ditaruh.
            List<int> eligible = new();
            for (int i = 0; i < listings.Count; i++)
                if (CurrentAbsoluteHour - listings[i].listedAbsoluteHour >= 24) eligible.Add(i);
            if (eligible.Count == 0 || UnityEngine.Random.value > CurrentBuyerChance) return;
            listingIndex = eligible[UnityEngine.Random.Range(0, eligible.Count)];
        }
        MarketStandListing listing = listings[listingIndex];
        int sold = guaranteedSale ? listing.count :
            UnityEngine.Random.Range(1, Mathf.Min(maximumItemsPerSale, listing.count) + 1);
        int revenue = listing.UnitPrice * sold;
        if (revenue <= 0)
            return;

        listing.count -= sold;
        lifetimeRevenue += revenue;
        ScoreManager.Instance.AddPoints(revenue);
        string soldName = listing.DisplayName;
        QuestEventHub.Publish(QuestObjectiveType.MarketSale, listing.item.name, sold, listing.item);
        if (listing.count <= 0)
            listings.RemoveAt(listingIndex);

        if (notify)
            SaveLoadFeedback.Instance?.ShowMessage($"Market: {soldName} x{sold} terjual — +{revenue} G");
        Debug.Log($"[MARKET] {soldName} x{sold} sold for {revenue} G. Remaining stock: {TotalItemCount}.");
    }

    void OpenPanel()
    {
        if (panelOpen)
            return;
        panelOpen = true;
        feedback = string.Empty;
        CenterWindow();
        WorldInteractionPrompt.AcquireSuppression(this);
        TimeManager.Instance?.AcquirePause(this);
        playerInventory?.GetComponent<PlayerController>()?.AcquireMovementLock(this);
    }

    void ClosePanel()
    {
        if (!panelOpen)
            return;
        panelOpen = false;
        WorldInteractionPrompt.ReleaseSuppression(this);
        TimeManager.Instance?.ReleasePause(this);
        playerInventory?.GetComponent<PlayerController>()?.ReleaseMovementLock(this);
    }

    void CenterWindow()
    {
        windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f);
        windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f);
    }

    void ResolveInventory()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory>();
    }

    void NormalizeListings()
    {
        listings ??= new List<MarketStandListing>();
        listings.RemoveAll(entry => entry == null || entry.item == null || entry.count <= 0 || !entry.item.CanSellAtMarket);
        listingCapacity = Mathf.Max(1, listingCapacity);
    }

    MarketStandListing FindCompatible(ItemSO item, int qualityStars, float fishSizeCm) => listings.Find(entry =>
        entry != null && entry.item == item && entry.qualityStars == qualityStars &&
        Mathf.Abs(entry.fishSizeCm - fishSizeCm) < 0.01f);

    public MarketStandSaveData Capture()
    {
        NormalizeListings();
        return new MarketStandSaveData
        {
            id = standId,
            lastProcessedAbsoluteHour = lastProcessedAbsoluteHour,
            lifetimeRevenue = lifetimeRevenue,
            listings = listings.Select(entry => new MarketStandListingSaveData
            {
                itemId = entry.item.Id,
                assetName = entry.item.name,
                itemName = entry.item.itemName,
                count = entry.count,
                qualityStars = entry.qualityStars,
                fishSizeCm = entry.fishSizeCm,
                listedAbsoluteHour = entry.listedAbsoluteHour
            }).ToList()
        };
    }

    public void Restore(MarketStandSaveData data)
    {
        listings.Clear();
        if (data?.listings != null)
        {
            foreach (MarketStandListingSaveData saved in data.listings)
            {
                if (saved == null || saved.count <= 0)
                    continue;
                ItemSO item = ItemCatalog.Resolve(saved.itemId, saved.assetName, saved.itemName);
                if (item == null || !item.CanSellAtMarket)
                    continue;
                listings.Add(new MarketStandListing
                {
                    item = item,
                    count = saved.count,
                    qualityStars = Mathf.Clamp(saved.qualityStars, 0, 5),
                    fishSizeCm = Mathf.Max(0f, saved.fishSizeCm),
                    listedAbsoluteHour = saved.listedAbsoluteHour > 0 ? saved.listedAbsoluteHour : CurrentAbsoluteHour
                });
            }
        }

        lastProcessedAbsoluteHour = data != null ? data.lastProcessedAbsoluteHour : CurrentAbsoluteHour;
        lifetimeRevenue = Mathf.Max(0, data?.lifetimeRevenue ?? 0);
        NormalizeListings();
        ProcessElapsedSales();
    }

    public static List<MarketStandSaveData> CaptureAll() =>
        FindObjectsByType<MarketStand>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(stand => stand.Capture()).ToList();

    public static void RestoreAll(List<MarketStandSaveData> saved)
    {
        foreach (MarketStand stand in FindObjectsByType<MarketStand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            MarketStandSaveData data = saved?.Find(entry => entry != null && entry.id == stand.standId);
            stand.Restore(data);
        }
    }

    void OnValidate()
    {
        listingCapacity = Mathf.Max(1, listingCapacity);
        maximumItemsPerSale = Mathf.Max(1, maximumItemsPerSale);
        marketOpenHour = Mathf.Clamp(marketOpenHour, 0, 22);
        marketCloseHour = Mathf.Clamp(marketCloseHour, marketOpenHour + 1, 23);
    }
}
