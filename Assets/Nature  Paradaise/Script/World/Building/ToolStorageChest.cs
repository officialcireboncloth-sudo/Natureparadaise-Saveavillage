using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class ToolStorageEntry
{
    public ItemSO item;
    [Min(1)] public int count = 1;
}

[Serializable]
public sealed class ToolStorageEntrySaveData
{
    public string itemId;
    public string assetName;
    public string itemName;
    public int count;
}

/// <summary>
/// Data Tool Storage global. Data sengaja tidak menempel pada scene interior supaya isi peti
/// tetap hidup ketika HouseInterior di-unload dan dapat diakses oleh peti rumah lainnya.
/// </summary>
public static class ToolStorageService
{
    static readonly List<ToolStorageEntry> EntriesInternal = new();

    public static IReadOnlyList<ToolStorageEntry> Entries => EntriesInternal;
    public static event Action Changed;

    public static int TotalCount => EntriesInternal.Sum(entry => entry != null ? Mathf.Max(0, entry.count) : 0);

    public static bool Store(ItemSO item, int amount)
    {
        if (item == null || !item.CanStoreInToolStorage || amount <= 0)
            return false;

        ToolStorageEntry entry = EntriesInternal.Find(candidate => candidate != null && candidate.item == item);
        if (entry == null)
        {
            entry = new ToolStorageEntry { item = item, count = 0 };
            EntriesInternal.Add(entry);
        }
        entry.count += amount;
        Changed?.Invoke();
        return true;
    }

    public static bool Take(int index, int amount, out ItemSO item, out int moved)
    {
        item = null;
        moved = 0;
        Normalize();
        if (index < 0 || index >= EntriesInternal.Count || amount <= 0)
            return false;

        ToolStorageEntry entry = EntriesInternal[index];
        item = entry.item;
        moved = Mathf.Min(amount, entry.count);
        entry.count -= moved;
        if (entry.count <= 0)
            EntriesInternal.RemoveAt(index);
        Changed?.Invoke();
        return moved > 0;
    }

    public static void Return(ItemSO item, int amount)
    {
        Store(item, amount);
    }

    public static List<ToolStorageEntrySaveData> Capture()
    {
        Normalize();
        return EntriesInternal.Select(entry => new ToolStorageEntrySaveData
        {
            itemId = entry.item.Id,
            assetName = entry.item.name,
            itemName = entry.item.itemName,
            count = entry.count
        }).ToList();
    }

    public static void Restore(List<ToolStorageEntrySaveData> saved)
    {
        EntriesInternal.Clear();
        if (saved != null)
        {
            foreach (ToolStorageEntrySaveData data in saved)
            {
                if (data == null || data.count <= 0)
                    continue;
                ItemSO item = ItemCatalog.Resolve(data.itemId, data.assetName, data.itemName);
                if (item == null || !item.CanStoreInToolStorage)
                {
                    Debug.LogWarning($"[TOOL STORAGE] Tool '{data.itemName}' dari save tidak ditemukan atau bukan Tool.");
                    continue;
                }
                StoreWithoutNotify(item, data.count);
            }
        }
        Changed?.Invoke();
    }

    public static void Clear()
    {
        EntriesInternal.Clear();
        Changed?.Invoke();
    }

    static void StoreWithoutNotify(ItemSO item, int amount)
    {
        ToolStorageEntry entry = EntriesInternal.Find(candidate => candidate != null && candidate.item == item);
        if (entry == null)
        {
            entry = new ToolStorageEntry { item = item, count = 0 };
            EntriesInternal.Add(entry);
        }
        entry.count += amount;
    }

    static void Normalize()
    {
        EntriesInternal.RemoveAll(entry => entry == null || entry.item == null ||
            !entry.item.CanStoreInToolStorage || entry.count <= 0);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        EntriesInternal.Clear();
        Changed = null;
    }
}

/// <summary>
/// Peti interaktif untuk memindahkan Tool antara Inventory dan storage global rumah.
/// UI hanya menerima ItemSO kategori Tool; level upgrade tetap berasal dari PlayerStatusSystem.
/// </summary>
[DisallowMultipleComponent]
public sealed class ToolStorageChest : MonoBehaviour
{
    static readonly List<ToolStorageChest> Active = new();

    [Header("Interaction")]
    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.2f;
    [SerializeField, Min(0f)] float promptHeight = 1.4f;

    string feedback = string.Empty;
    PlayerStatusSystem playerStatus;
    bool panelOpen => StorageChestUI.Instance != null && StorageChestUI.Instance.Chest == this;
    public Inventory PlayerInventory => playerInventory;
    public string Feedback => feedback;
    public static bool BlocksWorldPointer => StorageChestUI.IsOpen;
    void Awake()
    {
        EnsureStorageRackVisual();
        ResolvePlayer();


    }

    void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
        ClosePanel();
    }

    void Update()
    {
        ResolvePlayer();
        if (playerInventory == null)
            return;

        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey))
                ClosePanel();
            return;
        }

        if (!PlayerInteractionTarget.ContainsPickup(playerInventory.transform, transform, interactionRadius))
            return;

        var house = GetComponentInParent<HouseInteriorController>();
        if(house != null)
            foreach(var bed in house.GetComponentsInChildren<PlayerBed>())
                if(bed.CanInteract(playerInventory.transform)) return;

        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform,
            $"{interactKey}: Rak Penyimpanan | {ToolStorageService.TotalCount} tool tersimpan", distance, promptHeight);
        if (PlayerInteractionTarget.PressPickup(playerInventory.transform, transform, interactKey, interactionRadius))
            OpenPanel();
    }

    public static bool HasCloserRack(Transform player, float distance)
    {
        foreach(var rack in Active)
        {
            if(rack == null || !PlayerInteractionTarget.ContainsPickup(player,rack.transform,rack.interactionRadius)) continue;
            Vector3 delta=rack.transform.position-player.position;delta.y=0;
            if(delta.magnitude < distance) return true;
        }
        return false;
    }

    public bool TryStoreFromSlot(int slotIndex, int amount)
    {
        ResolvePlayer();
        ItemStack stack = playerInventory != null ? playerInventory.GetSlot(slotIndex) : null;
        if (stack == null || stack.item == null || stack.count <= 0 || amount <= 0 ||
            !stack.item.CanStoreInToolStorage)
        {
            feedback = "Hanya Tool yang dapat disimpan di peti ini.";
            return false;
        }

        int moved = Mathf.Min(amount, stack.count);
        ItemSO item = stack.item;
        if (!playerInventory.RemoveFromSlot(slotIndex, moved))
            return false;
        if (!ToolStorageService.Store(item, moved))
        {
            playerInventory.Add(item, moved, stack.qualityStars, stack.fishSizeCm);
            return false;
        }
        feedback = $"{ToolLabel(item)} disimpan.";
        return true;
    }

    public bool TryTake(int storageIndex, int amount)
    {
        ResolvePlayer();
        IReadOnlyList<ToolStorageEntry> entries = ToolStorageService.Entries;
        if (playerInventory == null || storageIndex < 0 || storageIndex >= entries.Count || amount <= 0)
            return false;
        ToolStorageEntry entry = entries[storageIndex];
        if (entry == null || entry.item == null || !playerInventory.CanAdd(entry.item, Mathf.Min(amount, entry.count)))
        {
            feedback = "Inventory penuh. Kosongkan satu slot untuk mengambil Tool.";
            return false;
        }

        if (!ToolStorageService.Take(storageIndex, amount, out ItemSO item, out int moved))
            return false;
        if (!playerInventory.Add(item, moved))
        {
            ToolStorageService.Return(item, moved);
            feedback = "Tool gagal dipindahkan; data dikembalikan ke storage.";
            return false;
        }
        feedback = $"{ToolLabel(item)} diambil ke Inventory.";
        return true;
    }

    public void StoreAllTools()
    {
        ResolvePlayer();
        if (playerInventory == null)
            return;
        int moved = 0;
        for (int index = playerInventory.slots.Count - 1; index >= 0; index--)
        {
            ItemStack stack = playerInventory.GetSlot(index);
            if (stack == null || stack.item == null || stack.count <= 0 || !stack.item.CanStoreInToolStorage)
                continue;
            int count = stack.count;
            if (TryStoreFromSlot(index, count))
                moved += count;
        }
        feedback = moved > 0 ? $"{moved} Tool disimpan." : "Tidak ada Tool yang bisa disimpan.";
    }

    public void TakeAllTools()
    {
        ResolvePlayer();
        int moved = 0;
        bool inventoryFull = false;
        for (int index = ToolStorageService.Entries.Count - 1; index >= 0; index--)
        {
            ToolStorageEntry entry = ToolStorageService.Entries[index];
            int count = entry != null ? entry.count : 0;
            if (count <= 0)
                continue;
            if (TryTake(index, count))
                moved += count;
            else
                inventoryFull = true;
        }
        feedback = inventoryFull
            ? $"{moved} Tool diambil. Inventory penuh untuk sisanya."
            : moved > 0 ? $"{moved} Tool diambil." : "Tool Storage masih kosong.";
    }

    string ToolLabel(ItemSO item)
    {
        if (item == null)
            return "Tool hilang";
        int level = playerStatus != null ? playerStatus.GetToolLevel(item.equippedTool) : 1;
        string tier = level switch { 1 => "Basic", 2 => "Copper", 3 => "Silver", 4 => "Gold", _ => $"Lv.{level}" };
        return $"{item.itemName} — {tier} Lv.{level}";
    }

    void ResolvePlayer()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory>();
        if (playerStatus == null && playerInventory != null)
            playerStatus = playerInventory.GetComponent<PlayerStatusSystem>();
    }

    public void OpenPanel()
    {
        ResolvePlayer();
        if (!panelOpen && playerInventory != null && !WorldInteractionPrompt.IsSuppressed && !GameplayPauseMenu.BlocksGameplayInput)
            StorageChestUI.Show(this);
    }

    void ClosePanel()
    {
        if (panelOpen) StorageChestUI.Instance.Close();
    }
    void EnsureStorageRackVisual()
    {
        // Visual DummyRack disimpan langsung sebagai child scene agar desainer dapat
        // memindahkan, memutar, dan mengubah skalanya dari Hierarchy sebelum Play.
        Renderer placeholder = GetComponent<Renderer>();
        if (placeholder != null) placeholder.enabled = false;
    }
}
