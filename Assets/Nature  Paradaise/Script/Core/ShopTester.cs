using UnityEngine;

/// <summary>Helper development untuk menguji transaksi shop melalui shortcut keyboard.</summary>
public class ShopTester : MonoBehaviour
{
    public ShopManager shop;
    [SerializeField] KeyCode debugBuySeedKey = KeyCode.F8;

    void Update()
    {
        if (Input.GetKeyDown(debugBuySeedKey))
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
