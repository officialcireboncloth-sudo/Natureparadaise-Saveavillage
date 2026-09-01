using UnityEngine;

/// <summary>Helper development untuk menguji transaksi shop melalui shortcut keyboard.</summary>
public class ShopTester : MonoBehaviour
{
    public ShopManager shop;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("[TEST] Buy Seed");
            shop.BuySeed(1);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[TEST] Sell Cabbage");
            shop.SellCabbage(1);
        }
    }
}
