using UnityEngine;

/// <summary>
/// Menjalankan transaksi buy/sell terhadap inventory dan saldo player.
/// Harga dan identitas barang berasal dari <see cref="ItemSO"/> agar UI tidak menyimpan aturan ekonomi.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Player")]
    public Inventory playerInv;

    [Header("Items Sold By Shop")]
    public ItemSO seedItem;

    [Header("Items Bought By Shop")]
    public ItemSO cabbageItem;
    public ItemSO milkItem;

    void Awake()
    {
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
    }

    // =====================================================
    // BUY
    // =====================================================

    /// <summary>Membeli seed sebanyak jumlah yang diminta jika saldo dan inventory mencukupi.</summary>
    public bool BuySeed(int amount = 1)
    {
        if (seedItem == null)
        {
            Debug.LogWarning("[SHOP] Seed Item belum di-assign.");
            return false;
        }

        return Buy(seedItem, amount);
    }

    bool Buy(ItemSO item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SHOP] Player Inventory belum ditemukan."
            );
            return false;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "[SHOP] ScoreManager tidak ditemukan."
            );
            return false;
        }

        int totalPrice = item.buyPrice * amount;

        // Cek Gold
        if (!ScoreManager.Instance.TrySpendPoints(totalPrice))
            return false;

        // Tambahkan item
        playerInv.Add(item, amount);

        Debug.Log(
            $"[SHOP] Bought {item.itemName} x{amount} " +
            $"for {totalPrice} Gold."
        );

        return true;
    }

    // =====================================================
    // SELL CABBAGE
    // =====================================================

    /// <summary>Menjual cabbage dari inventory menggunakan harga ItemSO.</summary>
    public bool SellCabbage(int amount = 1)
    {
        if (cabbageItem == null)
        {
            Debug.LogWarning(
                "[SHOP] Cabbage Item belum di-assign."
            );
            return false;
        }

        return Sell(cabbageItem, amount);
    }

    // =====================================================
    // SELL MILK
    // =====================================================

    /// <summary>Menjual milk dari inventory menggunakan harga ItemSO.</summary>
    public bool SellMilk(int amount = 1)
    {
        if (milkItem == null)
        {
            Debug.LogWarning(
                "[SHOP] Milk Item belum di-assign."
            );
            return false;
        }

        return Sell(milkItem, amount);
    }

    // =====================================================
    // GENERIC SELL
    // =====================================================

    bool Sell(ItemSO item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SHOP] Player Inventory belum ditemukan."
            );
            return false;
        }

        // Cek inventory
        if (playerInv.GetCount(item) < amount)
        {
            Debug.Log(
                $"[SHOP] Tidak punya {item.itemName} " +
                $"untuk dijual."
            );

            return false;
        }

        int totalPrice = item.sellPrice * amount;

        // Hapus item dari inventory
        if (!playerInv.Remove(item, amount))
            return false;

        // Tambahkan Gold
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddPoints(totalPrice);
        }
        else
        {
            Debug.LogWarning(
                "[SHOP] ScoreManager tidak ditemukan."
            );
        }

        Debug.Log(
            $"[SHOP] Sold {item.itemName} x{amount} " +
            $"for {totalPrice} Gold."
        );

        return true;
    }
}
