using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class RefrigeratorEntry
{
    public ItemSO item;
    [Min(1)] public int count = 1;
    [Range(0, 5)] public int qualityStars;
    [Min(0f)] public float fishSizeCm;

    public string DisplayName => item == null ? string.Empty : new ItemStack
    {
        item = item,
        count = count,
        qualityStars = qualityStars,
        fishSizeCm = fishSizeCm
    }.DisplayName;
}

[Serializable]
public sealed class RefrigeratorEntrySaveData
{
    public string itemId;
    public string assetName;
    public string itemName;
    public int count;
    public int qualityStars;
    public float fishSizeCm;
}

/// <summary>
/// Penyimpanan makanan global milik rumah. Data tidak menempel pada scene interior,
/// sehingga isi tetap tersedia ketika HouseInterior di-unload.
/// </summary>
public static class RefrigeratorService
{
    static readonly List<RefrigeratorEntry> EntriesInternal = new();

    public static IReadOnlyList<RefrigeratorEntry> Entries => EntriesInternal;
    public static event Action Changed;
    public static int Level => Mathf.Clamp(PlayerHouseController.Instance != null
        ? PlayerHouseController.Instance.RefrigeratorLevel : 0, 0, 4);
    public static bool IsUnlocked => Level > 0 && HouseFeatureService.IsUnlocked("house.refrigerator");
    public static int MaxSlots => CapacityForLevel(Level);
    public static int UsedSlots { get { Normalize(); return EntriesInternal.Count; } }
    public static int AvailableSlots => Mathf.Max(0, MaxSlots - UsedSlots);

    public static int CapacityForLevel(int level) => level switch
    {
        1 => 24,
        2 => 40,
        3 => 60,
        >= 4 => 80,
        _ => 0
    };

    public static bool CanStore(ItemSO item, int amount, int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (!IsUnlocked || item == null || !item.CanStoreInRefrigerator || amount <= 0)
            return false;
        Normalize();
        int free = 0;
        int stackLimit = item.StackLimit;
        for (int index = 0; index < EntriesInternal.Count; index++)
        {
            RefrigeratorEntry entry = EntriesInternal[index];
            if (Compatible(entry, item, qualityStars, fishSizeCm))
                free += Mathf.Max(0, stackLimit - entry.count);
        }
        free += Mathf.Max(0, MaxSlots - EntriesInternal.Count) * stackLimit;
        return free >= amount;
    }

    public static bool Store(ItemSO item, int amount, int qualityStars = 0, float fishSizeCm = 0f)
    {
        qualityStars = Mathf.Clamp(qualityStars, 0, 5);
        fishSizeCm = Mathf.Max(0f, fishSizeCm);
        if (!CanStore(item, amount, qualityStars, fishSizeCm))
            return false;

        int remaining = amount;
        int stackLimit = item.StackLimit;
        for (int index = 0; index < EntriesInternal.Count && remaining > 0; index++)
        {
            RefrigeratorEntry entry = EntriesInternal[index];
            if (!Compatible(entry, item, qualityStars, fishSizeCm) || entry.count >= stackLimit)
                continue;
            int moved = Mathf.Min(remaining, stackLimit - entry.count);
            entry.count += moved;
            remaining -= moved;
        }
        while (remaining > 0)
        {
            int moved = Mathf.Min(remaining, stackLimit);
            EntriesInternal.Add(new RefrigeratorEntry
            {
                item = item,
                count = moved,
                qualityStars = qualityStars,
                fishSizeCm = fishSizeCm
            });
            remaining -= moved;
        }
        Changed?.Invoke();
        return true;
    }

    public static bool Take(int index, int amount, out ItemStack stack)
    {
        stack = null;
        Normalize();
        if (index < 0 || index >= EntriesInternal.Count || amount <= 0)
            return false;
        RefrigeratorEntry entry = EntriesInternal[index];
        int moved = Mathf.Min(amount, entry.count);
        stack = new ItemStack
        {
            item = entry.item,
            count = moved,
            qualityStars = entry.qualityStars,
            fishSizeCm = entry.fishSizeCm
        };
        entry.count -= moved;
        if (entry.count <= 0)
            EntriesInternal.RemoveAt(index);
        Changed?.Invoke();
        return true;
    }

    public static int GetCount(ItemSO item)
    {
        if (item == null) return 0;
        Normalize();
        return EntriesInternal.Where(entry => entry.item == item).Sum(entry => entry.count);
    }

    /// <summary>Mengurangi bahan tanpa memperhatikan quality; quality rendah dipakai lebih dahulu.</summary>
    public static bool Consume(ItemSO item, int amount)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount)
            return false;
        int remaining = amount;
        foreach (RefrigeratorEntry entry in EntriesInternal
                     .Where(candidate => candidate.item == item)
                     .OrderBy(candidate => candidate.qualityStars)
                     .ThenBy(candidate => candidate.fishSizeCm)
                     .ToList())
        {
            int moved = Mathf.Min(remaining, entry.count);
            entry.count -= moved;
            remaining -= moved;
            if (remaining <= 0) break;
        }
        Normalize();
        Changed?.Invoke();
        return true;
    }

    public static void Sort()
    {
        Normalize();
        EntriesInternal.Sort((left, right) =>
        {
            int group = left.item.refrigeratorCategory.CompareTo(right.item.refrigeratorCategory);
            if (group != 0) return group;
            int name = string.Compare(left.item.itemName, right.item.itemName, StringComparison.OrdinalIgnoreCase);
            if (name != 0) return name;
            int quality = left.qualityStars.CompareTo(right.qualityStars);
            return quality != 0 ? quality : left.fishSizeCm.CompareTo(right.fishSizeCm);
        });
        Changed?.Invoke();
    }

    public static List<RefrigeratorEntrySaveData> Capture()
    {
        Normalize();
        return EntriesInternal.Select(entry => new RefrigeratorEntrySaveData
        {
            itemId = entry.item.Id,
            assetName = entry.item.name,
            itemName = entry.item.itemName,
            count = entry.count,
            qualityStars = entry.qualityStars,
            fishSizeCm = entry.fishSizeCm
        }).ToList();
    }

    public static void Restore(List<RefrigeratorEntrySaveData> saved)
    {
        EntriesInternal.Clear();
        if (saved != null)
        {
            foreach (RefrigeratorEntrySaveData data in saved)
            {
                if (data == null || data.count <= 0) continue;
                ItemSO item = ItemCatalog.Resolve(data.itemId, data.assetName, data.itemName);
                if (item == null || !item.CanStoreInRefrigerator)
                {
                    Debug.LogWarning($"[REFRIGERATOR] Item '{data.itemName}' dari save tidak ditemukan atau bukan makanan.");
                    continue;
                }
                AddWithoutCapacity(item, data.count, data.qualityStars, data.fishSizeCm);
            }
        }
        Normalize();
        Changed?.Invoke();
    }

    public static void Clear()
    {
        EntriesInternal.Clear();
        Changed?.Invoke();
    }

    static void AddWithoutCapacity(ItemSO item, int amount, int qualityStars, float fishSizeCm)
    {
        int remaining = amount;
        while (remaining > 0)
        {
            int moved = Mathf.Min(remaining, item.StackLimit);
            EntriesInternal.Add(new RefrigeratorEntry
            {
                item = item,
                count = moved,
                qualityStars = Mathf.Clamp(qualityStars, 0, 5),
                fishSizeCm = Mathf.Max(0f, fishSizeCm)
            });
            remaining -= moved;
        }
    }

    static bool Compatible(RefrigeratorEntry entry, ItemSO item, int qualityStars, float fishSizeCm) =>
        entry != null && entry.item == item && entry.qualityStars == qualityStars &&
        Mathf.Abs(entry.fishSizeCm - fishSizeCm) < 0.01f;

    static void Normalize() => EntriesInternal.RemoveAll(entry => entry == null || entry.item == null ||
        !entry.item.CanStoreInRefrigerator || entry.count <= 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        EntriesInternal.Clear();
        Changed = null;
    }
}

[Serializable]
public sealed class KitchenIngredientRequirement
{
    public ItemSO item;
    [Min(1)] public int amount = 1;
}

/// <summary>API bahan bersama Inventory + Refrigerator untuk sistem resep Kitchen.</summary>
public static class KitchenIngredientService
{
    public static int GetAvailableCount(Inventory inventory, ItemSO item) =>
        (inventory != null ? inventory.GetCount(item) : 0) + RefrigeratorService.GetCount(item);

    public static bool HasIngredients(Inventory inventory, IEnumerable<KitchenIngredientRequirement> requirements)
    {
        if (inventory == null || requirements == null) return false;
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in requirements
                     .Where(requirement => requirement != null && requirement.item != null && requirement.amount > 0)
                     .GroupBy(requirement => requirement.item))
            if (GetAvailableCount(inventory, group.Key) < group.Sum(requirement => requirement.amount))
                return false;
        return true;
    }

    /// <summary>
    /// Transaksi atomik untuk cooking: validasi seluruh bahan dahulu, lalu pakai Inventory
    /// sebelum Refrigerator. Caller harus memastikan slot output tersedia sebelum memanggil.
    /// </summary>
    public static bool TryConsume(Inventory inventory, IEnumerable<KitchenIngredientRequirement> requirements)
    {
        if (inventory == null || requirements == null) return false;
        List<IGrouping<ItemSO, KitchenIngredientRequirement>> groups = requirements
            .Where(requirement => requirement != null && requirement.item != null && requirement.amount > 0)
            .GroupBy(requirement => requirement.item).ToList();
        if (groups.Count == 0) return false;
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in groups)
            if (GetAvailableCount(inventory, group.Key) < group.Sum(requirement => requirement.amount))
                return false;
        foreach (IGrouping<ItemSO, KitchenIngredientRequirement> group in groups)
        {
            int remaining = group.Sum(requirement => requirement.amount);
            int fromInventory = Mathf.Min(remaining, inventory.GetCount(group.Key));
            if (fromInventory > 0)
            {
                inventory.Remove(group.Key, fromInventory);
                remaining -= fromInventory;
            }
            if (remaining > 0)
                RefrigeratorService.Consume(group.Key, remaining);
        }
        return true;
    }
}

enum RefrigeratorFilter : byte { All, Crops, Fish, Animal, Ingredient, Food }

/// <summary>Interactable Refrigerator di HouseInterior dengan transfer dua arah.</summary>
[DisallowMultipleComponent]
public sealed class Refrigerator : MonoBehaviour
{
    static readonly List<Refrigerator> Active = new();

    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.2f;
    [SerializeField, Min(0f)] float promptHeight = 1.4f;

    readonly Rect defaultWindowRect = new(0f, 0f, 1040f, 650f);
    Rect windowRect;
    Vector2 inventoryScroll;
    Vector2 refrigeratorScroll;
    RefrigeratorFilter filter;
    bool panelOpen;
    string feedback = string.Empty;

    public static bool BlocksWorldPointer
    {
        get
        {
            Vector2 pointer = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return Active.Any(fridge => fridge != null && fridge.panelOpen && fridge.windowRect.Contains(pointer));
        }
    }

    void Awake()
    {
        ResolvePlayer();
        windowRect = defaultWindowRect;
        CenterWindow();
    }

    void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    void OnDisable() { Active.Remove(this); ClosePanel(); }

    void Update()
    {
        ResolvePlayer();
        if (playerInventory == null) return;
        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel();
            return;
        }
        if (!PlayerInteractionTarget.ContainsPickup(playerInventory.transform, transform, interactionRadius)) return;
        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        string prompt = RefrigeratorService.IsUnlocked
            ? $"{interactKey}: Refrigerator | {RefrigeratorService.UsedSlots}/{RefrigeratorService.MaxSlots} slot"
            : "Refrigerator terkunci — Upgrade House ke Lv.2";
        WorldInteractionPrompt.Request(this, transform, prompt, distance, promptHeight);
        if (!PlayerInteractionTarget.PressPickup(playerInventory.transform, transform, interactKey, interactionRadius)) return;
        if (!RefrigeratorService.IsUnlocked)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Kitchen dan Refrigerator terbuka pada House Lv.2.");
            return;
        }
        OpenPanel();
    }

    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect.width = Mathf.Max(680f, Mathf.Min(defaultWindowRect.width, Screen.width - 24f));
        windowRect.height = Mathf.Max(430f, Mathf.Min(defaultWindowRect.height, Screen.height - 24f));
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow,
            $"REFRIGERATOR LV.{RefrigeratorService.Level} — {RefrigeratorService.UsedSlots}/{RefrigeratorService.MaxSlots} SLOTS");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        foreach (RefrigeratorFilter value in Enum.GetValues(typeof(RefrigeratorFilter)))
            if (GUILayout.Toggle(filter == value, value.ToString().ToUpperInvariant(), GUI.skin.button)) filter = value;
        GUILayout.EndHorizontal();
        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        DrawInventoryColumn();
        GUILayout.Space(10f);
        DrawRefrigeratorColumn();
        GUILayout.EndHorizontal();
        if (!string.IsNullOrWhiteSpace(feedback)) GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(32f))) ClosePanel();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    void DrawInventoryColumn()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.5f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("PLAYER INVENTORY");
        if (GUILayout.Button("Transfer All Food", GUILayout.Width(132f))) TransferAllFood();
        GUILayout.EndHorizontal();
        inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
        bool found = false;
        for (int index = 0; playerInventory != null && index < playerInventory.slots.Count; index++)
        {
            ItemStack stack = playerInventory.GetSlot(index);
            if (!Visible(stack?.item) || stack == null || stack.count <= 0) continue;
            found = true;
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{stack.DisplayName} x{stack.count}", GUILayout.MinWidth(190f));
            if (GUILayout.Button("Store", GUILayout.Width(62f))) TryStore(index, 1);
            if (GUILayout.Button("Stack", GUILayout.Width(62f))) TryStore(index, stack.count);
            GUILayout.EndHorizontal();
        }
        if (!found) GUILayout.Label("Tidak ada makanan untuk filter ini.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    void DrawRefrigeratorColumn()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((windowRect.width - 38f) * 0.5f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("REFRIGERATOR");
        if (GUILayout.Button("Sort", GUILayout.Width(70f))) RefrigeratorService.Sort();
        GUILayout.EndHorizontal();
        refrigeratorScroll = GUILayout.BeginScrollView(refrigeratorScroll);
        bool found = false;
        for (int index = RefrigeratorService.Entries.Count - 1; index >= 0; index--)
        {
            RefrigeratorEntry entry = RefrigeratorService.Entries[index];
            if (entry == null || !Visible(entry.item)) continue;
            found = true;
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{entry.DisplayName} x{entry.count}", GUILayout.MinWidth(190f));
            if (GUILayout.Button("Take", GUILayout.Width(62f))) TryTake(index, 1);
            if (GUILayout.Button("Stack", GUILayout.Width(62f))) TryTake(index, entry.count);
            GUILayout.EndHorizontal();
        }
        if (!found) GUILayout.Label("Belum ada item untuk filter ini.");
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    public bool TryStore(int inventorySlot, int amount)
    {
        ResolvePlayer();
        ItemStack source = playerInventory != null ? playerInventory.GetSlot(inventorySlot) : null;
        if (source == null || source.item == null || source.count <= 0 || !source.item.CanStoreInRefrigerator)
        {
            feedback = "Item ini tidak dapat disimpan di Refrigerator.";
            return false;
        }
        int moved = Mathf.Min(amount, source.count);
        ItemSO item = source.item;
        int quality = source.qualityStars;
        float size = source.fishSizeCm;
        if (!RefrigeratorService.CanStore(item, moved, quality, size))
        {
            feedback = "Refrigerator penuh untuk stack ini.";
            return false;
        }
        if (!playerInventory.RemoveFromSlot(inventorySlot, moved)) return false;
        if (!RefrigeratorService.Store(item, moved, quality, size))
        {
            playerInventory.Add(item, moved, quality, size);
            return false;
        }
        feedback = $"{item.itemName} x{moved} disimpan.";
        return true;
    }

    public bool TryTake(int refrigeratorIndex, int amount)
    {
        ResolvePlayer();
        if (playerInventory == null || refrigeratorIndex < 0 || refrigeratorIndex >= RefrigeratorService.Entries.Count)
            return false;
        RefrigeratorEntry entry = RefrigeratorService.Entries[refrigeratorIndex];
        int moved = Mathf.Min(amount, entry.count);
        if (!playerInventory.CanAdd(entry.item, moved, entry.qualityStars, entry.fishSizeCm))
        {
            feedback = "Inventory penuh.";
            return false;
        }
        if (!RefrigeratorService.Take(refrigeratorIndex, moved, out ItemStack stack)) return false;
        if (!playerInventory.Add(stack.item, stack.count, stack.qualityStars, stack.fishSizeCm))
        {
            RefrigeratorService.Store(stack.item, stack.count, stack.qualityStars, stack.fishSizeCm);
            return false;
        }
        feedback = $"{stack.item.itemName} x{stack.count} diambil.";
        return true;
    }

    public void TransferAllFood()
    {
        ResolvePlayer();
        int moved = 0;
        for (int index = playerInventory != null ? playerInventory.slots.Count - 1 : -1; index >= 0; index--)
        {
            ItemStack stack = playerInventory.GetSlot(index);
            if (stack == null || stack.item == null || !stack.item.CanStoreInRefrigerator) continue;
            int count = stack.count;
            if (TryStore(index, count)) moved += count;
        }
        feedback = moved > 0 ? $"{moved} item makanan dipindahkan." : "Tidak ada item yang dapat dipindahkan atau Refrigerator penuh.";
    }

    bool Visible(ItemSO item)
    {
        if (item == null || !item.CanStoreInRefrigerator) return false;
        return filter switch
        {
            RefrigeratorFilter.Crops => item.refrigeratorCategory is RefrigeratorCategory.Crop or RefrigeratorCategory.Fruit,
            RefrigeratorFilter.Fish => item.refrigeratorCategory == RefrigeratorCategory.Fish,
            RefrigeratorFilter.Animal => item.refrigeratorCategory == RefrigeratorCategory.AnimalProduct,
            RefrigeratorFilter.Ingredient => item.refrigeratorCategory == RefrigeratorCategory.Ingredient,
            RefrigeratorFilter.Food => item.refrigeratorCategory is RefrigeratorCategory.CookedFood or RefrigeratorCategory.Drink,
            _ => true
        };
    }

    void ResolvePlayer()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>();
    }

    void OpenPanel()
    {
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
        WorldInteractionPrompt.ReleaseSuppression(this);
        TimeManager.Instance?.ReleasePause(this);
        playerInventory?.GetComponent<PlayerController>()?.ReleaseMovementLock(this);
    }

    void CenterWindow()
    {
        windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f);
        windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f);
    }
}
