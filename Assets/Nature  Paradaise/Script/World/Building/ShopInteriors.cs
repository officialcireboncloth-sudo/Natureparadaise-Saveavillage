using System;

/// <summary>Scene and spawn identifiers shared by the shop doors and the testing menu.</summary>
public static class ShopInteriors
{
    public readonly struct Room
    {
        public Room(string key, string label, string exteriorModule = null)
        {
            Key = key;
            Label = label;
            ExteriorModule = exteriorModule;
        }

        public string Key { get; }
        public string Label { get; }
        public string ExteriorModule { get; }
        public string SceneName => Key + "ShopInterior";
        public string EntryId => "shop-" + Key.ToLowerInvariant() + "-entry";
        public bool HasExterior => !string.IsNullOrEmpty(ExteriorModule);
    }

    public static readonly Room[] Rooms =
    {
        new("Chicken", "Toko Ayam", "ChickenStore_Mobile_A"),
        new("Cattle", "Toko Sapi"),
        new("Blacksmith", "Pandai Besi"),
        new("Lumber", "Toko Bangunan"),
        new("Cafe", "Kafe & Rumah Makan"),
        new("Coffee", "Coffee Shop"),
        new("Supermarket", "Supermarket", "MiniMarket_A")
    };

    public static bool IsShopScene(string sceneName) =>
        Array.Exists(Rooms, room => room.SceneName == sceneName);
}
