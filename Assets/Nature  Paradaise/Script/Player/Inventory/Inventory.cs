using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventory fixed-capacity. Satu ItemStack merepresentasikan satu slot grid.
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Backpack Grid")]
    [Tooltip("Jumlah slot tas awal. Default 16 ditampilkan sebagai 2 baris x 8 kolom.")]
    [SerializeField, Min(8)] int baseBagSlotCount = 16;
    [Tooltip("Bonus slot kumulatif per Bag Level. Default: 0, +8, +12, +16.")]
    [SerializeField] List<int> backpackBonusSlots = new() { 0, 8, 12, 16 };
    [Tooltip("Level tas aktif yang menentukan bonus kapasitas.")]
    [SerializeField, Min(0)] int backpackLevel;
    [SerializeField, Min(0)] int maximumBackpackLevel = 3;
    [SerializeField, Range(1, 8)] int hotbarSlotCount = 8;
    [Tooltip("Item awal game. Delapan item pertama masuk hotbar; sisanya masuk grid tas.")]
    [SerializeField] List<ItemSO> startingHotbarItems = new();

    // Dipertahankan agar data scene lama dengan field capacity tidak rusak.
    [SerializeField, HideInInspector] int capacity = 20;

    [Tooltip("Slot tetap. Delapan slot pertama adalah hotbar; sisanya adalah grid tas utama.")]
    public List<ItemStack> slots = new();

    public int BagColumnCount => 8;
    public int BagRowCount => Mathf.CeilToInt(MainCapacity / (float)BagColumnCount);
    public int MainCapacity => Mathf.Max(8, baseBagSlotCount) + GetBackpackBonus(backpackLevel);
    public int HotbarSlotCount => Mathf.Clamp(hotbarSlotCount, 1, 8);
    public int Capacity => MainCapacity + HotbarSlotCount;
    public int BackpackLevel => backpackLevel;
    public int UsedSlots
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
                if (IsValid(slots[i])) count++;
            return count;
        }
    }

    /// <summary>
    /// Dipanggil setiap kali isi inventory bertambah / berkurang / dibersihkan.
    /// HUD bisa berlangganan ke event ini.
    /// </summary>
    public event Action OnInventoryChanged;

    void Awake()
    {
        NormalizeSlots();
        GrantStartingHotbarIfEmpty();
    }

    void Start()
    {
        // Beberapa komponen legacy menormalisasi inventory pada Awake. Jalankan
        // sekali lagi setelah seluruh Awake selesai agar loadout game baru stabil.
        GrantStartingHotbarIfEmpty();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Menjamin ukuran slot dan loadout awal sudah siap sebelum UI pertama digambar.
    /// Aman dipanggil beberapa kali karena item awal hanya diberikan saat inventory kosong.
    /// </summary>
    public void EnsureStartupLoadout()
    {
        NormalizeSlots();
        GrantStartingHotbarIfEmpty();
    }

    /*------------- Tambah item -------------*/
    /// <summary>Menambahkan seluruh jumlah dengan mengisi stack kompatibel lalu slot kosong.</summary>
    public bool Add(ItemSO item, int amount = 1, int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (item == null || amount <= 0)
            return false;

        NormalizeSlots();
        int stackLimit = item.StackLimit;
        int freeSpace = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            ItemStack slot = slots[i];
            if (IsCompatible(slot, item, qualityStars, fishSizeCm))
                freeSpace += Mathf.Max(0, stackLimit - slot.count);
        }

        for (int i = 0; i < slots.Count; i++)
            if (!IsValid(slots[i])) freeSpace += stackLimit;
        if (freeSpace < amount)
            return false;

        int remaining = amount;
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            ItemStack slot = slots[i];
            if (!IsCompatible(slot, item, qualityStars, fishSizeCm) || slot.count >= stackLimit)
                continue;

            int added = Mathf.Min(remaining, stackLimit - slot.count);
            slot.count += added;
            remaining -= added;
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            if (IsValid(slots[i])) continue;
            int added = Mathf.Min(remaining, stackLimit);
            slots[i] = new ItemStack { item = item, count = added, qualityStars = qualityStars, fishSizeCm = fishSizeCm };
            remaining -= added;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /*------------- Kurangi / hapus item -------------*/
    /// <summary>Mengurangi jumlah item dari beberapa stack jika totalnya mencukupi.</summary>
    public bool Remove(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount)
            return false;

        int remaining = amount;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            ItemStack slot = slots[i];
            if (!IsValid(slot) || slot.item != item)
                continue;

            int removed = Mathf.Min(remaining, slot.count);
            slot.count -= removed;
            remaining -= removed;
            if (slot.count <= 0)
                slots[i] = null;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /*------------- Ambil jumlah item tertentu -------------*/
    /// <summary>Menghitung total item pada seluruh slot bag dan hotbar.</summary>
    public int GetCount(ItemSO item)
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
            if (IsValid(slots[i]) && slots[i].item == item)
                total += slots[i].count;
        return total;
    }

    /// <summary>Mengambil slot berdasarkan index atau null jika di luar kapasitas.</summary>
    public ItemStack GetSlot(int index)
    {
        return index >= 0 && index < slots.Count ? slots[index] : null;
    }

    /// <summary>Mensimulasikan kapasitas stack tanpa mengubah inventory.</summary>
    public bool CanAdd(ItemSO item, int amount = 1, int qualityStars = 0, float fishSizeCm = 0f)
    {
        if (item == null || amount <= 0)
            return false;

        NormalizeSlots();
        int freeSpace = 0;
        for (int i = 0; i < slots.Count; i++)
            if (!IsValid(slots[i]))
                freeSpace += item.StackLimit;
            else if (IsCompatible(slots[i], item, qualityStars, fishSizeCm))
                freeSpace += Mathf.Max(0, item.StackLimit - slots[i].count);
        return freeSpace >= amount;
    }

    /// <summary>Menyortir area bag berdasarkan kategori tanpa mengubah slot hotbar.</summary>
    public void SortByCategory()
    {
        List<ItemStack> occupied = new();
        // Hotbar tidak ikut disortir agar tool/item aktif tidak berpindah tanpa
        // persetujuan player. Hanya grid tas utama yang dirapikan.
        for (int i = HotbarSlotCount; i < slots.Count; i++)
            if (IsValid(slots[i])) occupied.Add(slots[i]);

        occupied.Sort((a, b) =>
        {
            int category = a.item.category.CompareTo(b.item.category);
            return category != 0
                ? category
                : string.Compare(a.item.itemName, b.item.itemName, StringComparison.OrdinalIgnoreCase);
        });
        for (int i = HotbarSlotCount; i < slots.Count; i++)
            slots[i] = null;
        for (int i = 0; i < occupied.Count && HotbarSlotCount + i < slots.Count; i++)
            slots[HotbarSlotCount + i] = occupied[i];
        OnInventoryChanged?.Invoke();
    }

    /*------------- Kosongkan seluruh inventory -------------*/
    public void Clear()
    {
        slots.Clear();
        while (slots.Count < Capacity) slots.Add(null);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Mengatur level tas berdasarkan tabel bonus slot modular.</summary>
    public void SetBackpackLevel(int level)
    {
        backpackLevel = Mathf.Clamp(level, 0, maximumBackpackLevel);
        NormalizeSlots();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Menambah level backpack dan membangun ulang kapasitas grid jika belum maksimum.</summary>
    public bool TryUpgradeBackpack()
    {
        if (backpackLevel >= maximumBackpackLevel)
            return false;
        SetBackpackLevel(backpackLevel + 1);
        return true;
    }

    /// <summary>
    /// Mengurangi stack dari slot tertentu. Dipakai held-item agar item yang terlihat
    /// di hotbar sama dengan stack yang benar-benar di-drop atau di-place.
    /// </summary>
    public bool RemoveFromSlot(int slotIndex, int amount = 1)
    {
        NormalizeSlots();
        if (slotIndex < 0 || slotIndex >= slots.Count || amount <= 0)
            return false;

        ItemStack slot = slots[slotIndex];
        if (!IsValid(slot) || slot.count < amount)
            return false;

        slot.count -= amount;
        if (slot.count <= 0)
            slots[slotIndex] = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Dipakai SaveManager untuk mengembalikan posisi slot persis.</summary>
    public bool TrySetSlot(int index, ItemSO item, int amount, int qualityStars = 0, float fishSizeCm = 0f)
    {
        NormalizeSlots();
        if (index < 0 || index >= Capacity || item == null || amount <= 0 || IsValid(slots[index]))
            return false;
        slots[index] = new ItemStack
        {
            item = item,
            count = Mathf.Min(amount, item.StackLimit),
            qualityStars = Mathf.Clamp(qualityStars, 0, 5),
            fishSizeCm = Mathf.Max(0f, fishSizeCm)
        };
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Menukar dua slot untuk drag-and-drop bag/hotbar.</summary>
    public bool SwapSlots(int first, int second)
    {
        NormalizeSlots();
        if (first < 0 || second < 0 || first >= Capacity || second >= Capacity || first == second)
            return false;
        (slots[first], slots[second]) = (slots[second], slots[first]);
        OnInventoryChanged?.Invoke();
        return true;
    }

    void GrantStartingHotbarIfEmpty()
    {
        if (UsedSlots > 0 || startingHotbarItems == null) return;
        int count = Mathf.Min(Capacity, startingHotbarItems.Count);
        for (int i = 0; i < count; i++)
        {
            ItemSO item = startingHotbarItems[i];
            if (item != null) slots[i] = new ItemStack { item = item, count = 1 };
        }
    }

    void NormalizeSlots()
    {
        baseBagSlotCount = Mathf.Max(8, baseBagSlotCount);
        maximumBackpackLevel = Mathf.Max(0, maximumBackpackLevel);
        backpackLevel = Mathf.Clamp(backpackLevel, 0, maximumBackpackLevel);
        hotbarSlotCount = 8;
        capacity = Capacity;

        if (slots.Count > Capacity)
            slots.RemoveRange(Capacity, slots.Count - Capacity);
        while (slots.Count < Capacity)
            slots.Add(null);

        // Data scene/save lama mungkin menyimpan satu stack di atas batas baru.
        for (int i = 0; i < slots.Count; i++)
        {
            ItemStack slot = slots[i];
            if (!IsValid(slot))
            {
                // Seragamkan slot rusak/legacy menjadi kosong agar UI tidak menerima
                // ItemStack tanpa ItemSO.
                slots[i] = null;
                continue;
            }

            int limit = slot.item.StackLimit;
            if (slot.count <= limit)
                continue;

            int overflow = slot.count - limit;
            slot.count = limit;
            for (int empty = 0; empty < slots.Count && overflow > 0; empty++)
            {
                if (IsValid(slots[empty])) continue;
                int split = Mathf.Min(overflow, limit);
                slots[empty] = new ItemStack
                {
                    item = slot.item,
                    count = split,
                    qualityStars = slot.qualityStars,
                    fishSizeCm = slot.fishSizeCm
                };
                overflow -= split;
            }

            // Jangan menghilangkan data lama jika kapasitas belum cukup.
            if (overflow > 0)
                slot.count += overflow;
        }
    }

    int GetBackpackBonus(int level)
    {
        int safeLevel = Mathf.Clamp(level, 0, maximumBackpackLevel);
        if (backpackBonusSlots != null && safeLevel < backpackBonusSlots.Count)
            return Mathf.Max(0, backpackBonusSlots[safeLevel]);

        // Fallback menjaga asset lama tetap mempunyai kapasitas masuk akal jika
        // tabel bonus belum lengkap di Inspector.
        return safeLevel * BagColumnCount;
    }

    static bool IsValid(ItemStack slot) => slot != null && slot.item != null && slot.count > 0;

    static bool IsCompatible(ItemStack slot, ItemSO item, int qualityStars, float fishSizeCm) =>
        IsValid(slot) && slot.item == item && slot.qualityStars == qualityStars &&
        Mathf.Abs(slot.fishSizeCm - fishSizeCm) < 0.01f;
}

/*=========================================================
 *  STRUKTUR ITEM + JUMLAH
 *========================================================*/
[System.Serializable]
/// <summary>Pasangan item dan jumlah yang disimpan pada satu slot inventory.</summary>
public class ItemStack
{
    public ItemSO item;
    public int count;
    [Range(0, 5)] public int qualityStars;
    [Min(0f)] public float fishSizeCm;
    public string DisplayName => item == null ? string.Empty : item.itemName +
        (qualityStars > 0 ? (item.category == ItemCategory.AnimalProduct || item.isAnimalProduct)
            ? $" [{AnimalCareCatalog.QualityName(qualityStars)}]" : $" [{qualityStars}★]" : string.Empty) +
        (item.category == ItemCategory.Fish && fishSizeCm > 0f ? $" [{fishSizeCm:0.#} cm]" : string.Empty);
}
