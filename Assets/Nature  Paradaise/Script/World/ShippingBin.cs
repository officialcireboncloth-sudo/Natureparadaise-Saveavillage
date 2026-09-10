using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class ShippingBinListing
{
    public ItemSO item;
    [Min(1)] public int count = 1;
    [Range(0, 5)] public int qualityStars;
    [Min(0f)] public float fishSizeCm;
    [Min(1)] public int unitPriceSnapshot = 1;
    [Min(1)] public int shippingDay = 1;

    public int EstimatedTotal => Mathf.Max(0, count) * Mathf.Max(0, unitPriceSnapshot);
    public string DisplayName => item == null ? "Item hilang" : new ItemStack
    {
        item = item,
        count = count,
        qualityStars = qualityStars,
        fishSizeCm = fishSizeCm
    }.DisplayName;
}

[Serializable]
public sealed class ShippingBinListingSaveData
{
    public string assetName;
    public string itemName;
    public int count;
    public int qualityStars;
    public float fishSizeCm;
    public int unitPriceSnapshot;
    public int shippingDay;
}

[Serializable]
public sealed class ShippingBinSaveData
{
    public string id;
    public List<ShippingBinListingSaveData> listings;
    public int lastProcessedDay;
    public int lastCropSales;
    public int lastFishSales;
    public int lastAnimalProductSales;
    public int lastOtherSales;
    public int lastShippingTotal;
    public int lifetimeRevenue;
}

/// <summary>
/// Penjualan harian dekat rumah. Barang bisa ditarik kembali selama hari berjalan,
/// lalu seluruh isi dikunci, dijual, dan dibayar saat Daily Reset.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShippingBin : MonoBehaviour
{
    static readonly List<ShippingBin> Active = new();

    [Header("Identity")]
    [SerializeField] string binId = "player-home-shipping-bin-01";

    [Header("Interaction")]
    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0f)] float promptHeight = 1.4f;

    [Header("Valuable Item Confirmation")]
    [SerializeField, Min(1)] int highValueUnitPrice = 1000;

    [Header("Pending Shipping")]
    [SerializeField] List<ShippingBinListing> listings = new();

    [Header("Last Daily Shipping")]
    [SerializeField] int lastProcessedDay;
    [SerializeField] int lastCropSales;
    [SerializeField] int lastFishSales;
    [SerializeField] int lastAnimalProductSales;
    [SerializeField] int lastOtherSales;
    [SerializeField] int lastShippingTotal;
    [SerializeField] int lifetimeRevenue;

    readonly Rect defaultWindowRect = new(0f, 0f, 1020f, 690f);
    readonly Dictionary<int, string> amountInputs = new();
    Rect windowRect;
    Vector2 inventoryScroll;
    Vector2 binScroll;
    bool panelOpen;
    string feedback = string.Empty;
    int confirmationSlot = -1;
    int confirmationAmount;
    Coroutine summaryRoutine;

    public string BinId => binId;
    public IReadOnlyList<ShippingBinListing> Listings => listings;
    public int PendingItemCount => listings.Sum(entry => entry != null ? Mathf.Max(0, entry.count) : 0);
    public int PendingEstimatedValue => listings.Sum(entry => entry != null ? entry.EstimatedTotal : 0);
    public int LastShippingTotal => lastShippingTotal;
    public int LifetimeRevenue => lifetimeRevenue;

    public static bool BlocksWorldPointer
    {
        get
        {
            Vector2 pointer = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return Active.Any(bin => bin != null && bin.panelOpen && bin.windowRect.Contains(pointer));
        }
    }

    void Awake()
    {
        ResolveInventory();
        NormalizeListings();
        windowRect = defaultWindowRect;
        CenterWindow();
    }

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
        TimeManager.OnBeforeDayChange += ProcessDailyShipping;
        TimeManager.OnDay += ShowDailySummary;
    }

    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= ProcessDailyShipping;
        TimeManager.OnDay -= ShowDailySummary;
        Active.Remove(this);
        if (summaryRoutine != null) StopCoroutine(summaryRoutine);
        summaryRoutine = null;
        ClosePanel();
    }

    void Update()
    {
        ResolveInventory();
        if (playerInventory == null) return;

        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel();
            return;
        }

        if (!PlayerInteractionTarget.Contains(playerInventory.transform, transform)) return;
        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform,
            $"{interactKey}: Shipping Bin | {PendingItemCount} item | Est. {PendingEstimatedValue} G",
            distance, promptHeight);
        if (PlayerInteractionTarget.Press(playerInventory.transform, transform, interactKey)) OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect.width = Mathf.Max(650f, Mathf.Min(defaultWindowRect.width, Screen.width - 24f));
        windowRect.height = Mathf.Max(440f, Mathf.Min(defaultWindowRect.height, Screen.height - 24f));
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "SHIPPING BIN — DAILY SHIPPING");
    }

    void DrawWindow(int id)
    {
        GUILayout.Label($"Pending: {PendingItemCount} item   |   Estimasi: {PendingEstimatedValue} G   |   " +
                        $"Pembayaran: Daily Reset   |   Pendapatan total: {lifetimeRevenue} G");
        GUILayout.Label("Barang masih dapat diambil kembali sebelum tidur atau pergantian hari.");
        if (lastShippingTotal > 0)
            GUILayout.Label($"Penjualan hari terakhir: {lastShippingTotal:N0} G");
        GUILayout.Space(6f);

        bool awaitingConfirmation = confirmationSlot >= 0;
        GUI.enabled = !awaitingConfirmation;
        GUILayout.BeginHorizontal();
        DrawInventoryColumn();
        GUILayout.Space(12f);
        DrawBinColumn();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        if (!string.IsNullOrWhiteSpace(feedback)) GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(32f))) ClosePanel();
        GUI.enabled = true;

        if (awaitingConfirmation) DrawConfirmation();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void DrawInventoryColumn()
    {
        float columnWidth = (windowRect.width - 42f) * 0.5f;
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(columnWidth));
        GUILayout.Label("INVENTORY — masukkan barang");
        inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
        bool found = false;

        if (playerInventory != null)
        {
            for (int index = 0; index < playerInventory.slots.Count; index++)
            {
                ItemStack stack = playerInventory.GetSlot(index);
                if (stack == null || stack.item == null || stack.count <= 0 || !stack.item.CanSellAtMarket) continue;
                found = true;
                int unitPrice = stack.item.GetMarketSellPrice(stack.qualityStars, stack.fishSizeCm);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{stack.DisplayName} x{stack.count} — {unitPrice} G/item");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sell 1", GUILayout.Height(30f))) RequestDeposit(index, 1);
                string current = amountInputs.TryGetValue(index, out string input) ? input : "1";
                current = GUILayout.TextField(current, 4, GUILayout.Width(42f), GUILayout.Height(30f));
                amountInputs[index] = current;
                if (GUILayout.Button("Sell Amount", GUILayout.Height(30f)))
                    RequestDeposit(index, ParseAmount(current, stack.count));
                if (GUILayout.Button("Sell Stack", GUILayout.Height(30f))) RequestDeposit(index, stack.count);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        if (!found) GUILayout.Label("Tidak ada barang sellable di Inventory.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    void DrawBinColumn()
    {
        float columnWidth = (windowRect.width - 42f) * 0.5f;
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(columnWidth));
        GUILayout.Label($"SHIPPING BIN — {listings.Count} kelompok");
        binScroll = GUILayout.BeginScrollView(binScroll);
        if (listings.Count == 0) GUILayout.Label("Bin masih kosong.");

        for (int index = listings.Count - 1; index >= 0; index--)
        {
            ShippingBinListing listing = listings[index];
            if (listing == null || listing.item == null || listing.count <= 0) continue;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{listing.DisplayName} x{listing.count}\n" +
                            $"{listing.unitPriceSnapshot} G/item — Est. {listing.EstimatedTotal} G");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take Back 1", GUILayout.Height(30f))) TryWithdraw(index, 1);
            if (GUILayout.Button("Take Back Stack", GUILayout.Height(30f))) TryWithdraw(index, listing.count);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    void DrawConfirmation()
    {
        ItemStack stack = playerInventory != null ? playerInventory.GetSlot(confirmationSlot) : null;
        if (stack == null || stack.item == null)
        {
            ClearConfirmation();
            return;
        }

        Rect box = new(windowRect.width * 0.2f, windowRect.height * 0.31f,
            windowRect.width * 0.6f, 155f);
        GUI.Box(box, string.Empty);
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 14f, box.width - 32f, box.height - 28f));
        GUILayout.Label($"Sell this valuable item?\n{stack.DisplayName} x{confirmationAmount}");
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel", GUILayout.Height(34f))) ClearConfirmation();
        if (GUILayout.Button("Sell", GUILayout.Height(34f)))
        {
            int slot = confirmationSlot;
            int amount = confirmationAmount;
            ClearConfirmation();
            TryDeposit(slot, amount);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void RequestDeposit(int slotIndex, int amount)
    {
        ItemStack stack = playerInventory != null ? playerInventory.GetSlot(slotIndex) : null;
        if (stack == null || stack.item == null || amount <= 0) return;
        int unitPrice = stack.item.GetMarketSellPrice(stack.qualityStars, stack.fishSizeCm);
        if (stack.item.requiresSellConfirmation || unitPrice >= highValueUnitPrice)
        {
            confirmationSlot = slotIndex;
            confirmationAmount = Mathf.Min(amount, stack.count);
            return;
        }
        TryDeposit(slotIndex, amount);
    }

    public bool TryDeposit(int slotIndex, int amount)
    {
        ResolveInventory();
        ItemStack stack = playerInventory != null ? playerInventory.GetSlot(slotIndex) : null;
        if (stack == null || stack.item == null || stack.count <= 0 || amount <= 0 || !stack.item.CanSellAtMarket)
        {
            feedback = "Barang ini tidak dapat dijual melalui Shipping Bin.";
            return false;
        }

        int moved = Mathf.Min(amount, stack.count);
        ItemSO item = stack.item;
        int quality = stack.qualityStars;
        float fishSize = stack.fishSizeCm;
        int priceSnapshot = item.GetMarketSellPrice(quality, fishSize);
        int day = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : 1;
        if (!playerInventory.RemoveFromSlot(slotIndex, moved)) return false;

        ShippingBinListing listing = FindCompatible(item, quality, fishSize, priceSnapshot);
        if (listing == null)
        {
            listing = new ShippingBinListing
            {
                item = item,
                count = 0,
                qualityStars = quality,
                fishSizeCm = fishSize,
                unitPriceSnapshot = priceSnapshot,
                shippingDay = day
            };
            listings.Add(listing);
        }
        listing.count += moved;
        feedback = $"{listing.DisplayName} x{moved} dimasukkan — estimasi {priceSnapshot * moved} G.";
        return true;
    }

    public bool TryWithdraw(int listingIndex, int amount)
    {
        ResolveInventory();
        if (playerInventory == null || listingIndex < 0 || listingIndex >= listings.Count || amount <= 0) return false;
        ShippingBinListing listing = listings[listingIndex];
        if (listing == null || listing.item == null || listing.count <= 0) return false;
        int moved = Mathf.Min(amount, listing.count);
        if (!playerInventory.CanAdd(listing.item, moved, listing.qualityStars, listing.fishSizeCm))
        {
            feedback = "Tas tidak mempunyai ruang untuk mengambil barang ini.";
            return false;
        }
        if (!playerInventory.Add(listing.item, moved, listing.qualityStars, listing.fishSizeCm)) return false;

        string itemName = listing.DisplayName;
        listing.count -= moved;
        if (listing.count <= 0) listings.RemoveAt(listingIndex);
        feedback = $"{itemName} x{moved} dikembalikan ke Inventory.";
        return true;
    }

    void ProcessDailyShipping()
    {
        NormalizeListings();
        int day = TimeManager.Instance != null ? Mathf.Max(1, TimeManager.Instance.day) : lastProcessedDay + 1;
        if (lastProcessedDay == day) return;
        if (listings.Count > 0 && ScoreManager.Instance == null)
        {
            Debug.LogWarning("[SHIPPING] Pembayaran ditunda karena ScoreManager belum tersedia.");
            return;
        }

        lastCropSales = lastFishSales = lastAnimalProductSales = lastOtherSales = 0;
        foreach (ShippingBinListing listing in listings)
        {
            if (listing == null || listing.item == null || listing.count <= 0) continue;
            int value = listing.EstimatedTotal;
            QuestEventHub.Publish(QuestObjectiveType.Ship, listing.item.name, listing.count, listing.item);
            switch (SummaryCategory(listing.item))
            {
                case 0: lastCropSales += value; break;
                case 1: lastFishSales += value; break;
                case 2: lastAnimalProductSales += value; break;
                default: lastOtherSales += value; break;
            }
        }

        lastShippingTotal = lastCropSales + lastFishSales + lastAnimalProductSales + lastOtherSales;
        if (lastShippingTotal > 0)
        {
            ScoreManager.Instance?.AddPoints(lastShippingTotal);
            lifetimeRevenue += lastShippingTotal;
        }
        listings.Clear();
        lastProcessedDay = day;
        ClearConfirmation();
        Debug.Log($"[SHIPPING] Day {day}: sold for {lastShippingTotal} G.");
    }

    void ShowDailySummary()
    {
        if (lastShippingTotal <= 0) return;
        if (summaryRoutine != null) StopCoroutine(summaryRoutine);
        summaryRoutine = StartCoroutine(ShowSummaryAfterWakeMessage());
    }

    IEnumerator ShowSummaryAfterWakeMessage()
    {
        // PlayerLifeCycle menampilkan "Bangun" pada frame yang sama dengan OnDay.
        // Tunda laporan agar pesan bangun tidak menimpa rincian penjualan.
        yield return new WaitForSecondsRealtime(0.2f);
        SaveLoadFeedback.Instance?.ShowMessage(BuildSummary());
        summaryRoutine = null;
    }

    public string BuildSummary()
    {
        StringBuilder result = new("DAILY SHIPPING");
        if (lastCropSales > 0) result.Append($"\nCrops: {lastCropSales:N0} G");
        if (lastFishSales > 0) result.Append($"\nFish: {lastFishSales:N0} G");
        if (lastAnimalProductSales > 0) result.Append($"\nAnimal Products: {lastAnimalProductSales:N0} G");
        if (lastOtherSales > 0) result.Append($"\nOther: {lastOtherSales:N0} G");
        result.Append($"\nTotal Sales: {lastShippingTotal:N0} G");
        return result.ToString();
    }

    static int SummaryCategory(ItemSO item)
    {
        if (item.category == ItemCategory.Fish) return 1;
        if (item.category == ItemCategory.AnimalProduct || item.isAnimalProduct) return 2;
        if (item.category == ItemCategory.Food || item.category == ItemCategory.Seed) return 0;
        return 3;
    }

    static int ParseAmount(string value, int maximum) =>
        int.TryParse(value, out int amount) ? Mathf.Clamp(amount, 1, Mathf.Max(1, maximum)) : 1;

    ShippingBinListing FindCompatible(ItemSO item, int quality, float fishSize, int price) => listings.Find(entry =>
        entry != null && entry.item == item && entry.qualityStars == quality &&
        Mathf.Abs(entry.fishSizeCm - fishSize) < 0.01f && entry.unitPriceSnapshot == price);

    void NormalizeListings()
    {
        listings ??= new List<ShippingBinListing>();
        listings.RemoveAll(entry => entry == null || entry.item == null || entry.count <= 0 || !entry.item.CanSellAtMarket);
    }

    void ResolveInventory()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
    }

    void OpenPanel()
    {
        if (panelOpen) return;
        panelOpen = true;
        feedback = string.Empty;
        CenterWindow();
        WorldInteractionPrompt.AcquireSuppression(this);
        TimeManager.Instance?.AcquirePause(this);
        playerInventory?.GetComponent<PlayerController>()?.AcquireMovementLock(this);
    }

    void ClosePanel()
    {
        if (!panelOpen) return;
        panelOpen = false;
        ClearConfirmation();
        WorldInteractionPrompt.ReleaseSuppression(this);
        TimeManager.Instance?.ReleasePause(this);
        playerInventory?.GetComponent<PlayerController>()?.ReleaseMovementLock(this);
    }

    void ClearConfirmation()
    {
        confirmationSlot = -1;
        confirmationAmount = 0;
    }

    void CenterWindow()
    {
        windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f);
        windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f);
    }

    public ShippingBinSaveData Capture()
    {
        NormalizeListings();
        return new ShippingBinSaveData
        {
            id = binId,
            lastProcessedDay = lastProcessedDay,
            lastCropSales = lastCropSales,
            lastFishSales = lastFishSales,
            lastAnimalProductSales = lastAnimalProductSales,
            lastOtherSales = lastOtherSales,
            lastShippingTotal = lastShippingTotal,
            lifetimeRevenue = lifetimeRevenue,
            listings = listings.Select(entry => new ShippingBinListingSaveData
            {
                assetName = entry.item.name,
                itemName = entry.item.itemName,
                count = entry.count,
                qualityStars = entry.qualityStars,
                fishSizeCm = entry.fishSizeCm,
                unitPriceSnapshot = entry.unitPriceSnapshot,
                shippingDay = entry.shippingDay
            }).ToList()
        };
    }

    public void Restore(ShippingBinSaveData data)
    {
        listings.Clear();
        if (data?.listings != null)
        {
            ItemSO[] catalog = Resources.LoadAll<ItemSO>(string.Empty);
            foreach (ShippingBinListingSaveData saved in data.listings)
            {
                if (saved == null || saved.count <= 0) continue;
                ItemSO item = Array.Find(catalog, candidate => candidate != null &&
                    (candidate.name == saved.assetName || candidate.itemName == saved.itemName));
                if (item == null || !item.CanSellAtMarket) continue;
                listings.Add(new ShippingBinListing
                {
                    item = item,
                    count = saved.count,
                    qualityStars = Mathf.Clamp(saved.qualityStars, 0, 5),
                    fishSizeCm = Mathf.Max(0f, saved.fishSizeCm),
                    unitPriceSnapshot = saved.unitPriceSnapshot > 0
                        ? saved.unitPriceSnapshot
                        : item.GetMarketSellPrice(saved.qualityStars, saved.fishSizeCm),
                    shippingDay = Mathf.Max(1, saved.shippingDay)
                });
            }
        }
        lastProcessedDay = data?.lastProcessedDay ?? 0;
        lastCropSales = Mathf.Max(0, data?.lastCropSales ?? 0);
        lastFishSales = Mathf.Max(0, data?.lastFishSales ?? 0);
        lastAnimalProductSales = Mathf.Max(0, data?.lastAnimalProductSales ?? 0);
        lastOtherSales = Mathf.Max(0, data?.lastOtherSales ?? 0);
        lastShippingTotal = Mathf.Max(0, data?.lastShippingTotal ?? 0);
        lifetimeRevenue = Mathf.Max(0, data?.lifetimeRevenue ?? 0);
        NormalizeListings();
    }

    public static List<ShippingBinSaveData> CaptureAll() =>
        FindObjectsByType<ShippingBin>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(bin => bin.Capture()).ToList();

    public static void RestoreAll(List<ShippingBinSaveData> saved)
    {
        foreach (ShippingBin bin in FindObjectsByType<ShippingBin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            bin.Restore(saved?.Find(entry => entry != null && entry.id == bin.binId));
    }

    void OnValidate() => highValueUnitPrice = Mathf.Max(1, highValueUnitPrice);
}
