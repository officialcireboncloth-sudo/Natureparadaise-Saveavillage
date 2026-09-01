using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Koordinator save/load JSON untuk status player, inventory, waktu, cuaca, field,
/// resource gathering, pohon, dan placed item. Menangani kompatibilitas layout save lama.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("References")]
    public Inventory playerInv;
    public PlayerStatusSystem playerStatus;

    [Header("Items")]
    public ItemSO seedItem;
    public ItemSO cabbageItem;
    public ItemSO milkItem;

    [Header("Feedback")]
    public SaveLoadFeedback feedback;

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "savegame.json"
        );

    // =====================================================
    // SAVE DATA
    // =====================================================

    [Serializable]
    /// <summary>Root schema save game. Penambahan field harus memiliki fallback untuk save lama.</summary>
    public class SaveData
    {
        // -------------------------
        // GOLD
        // -------------------------

        public int gold;

        // -------------------------
        // PLAYER POSITION
        // -------------------------

        public float playerX;
        public float playerY;
        public float playerZ;

        // -------------------------
        // TIME
        // -------------------------

        public int minute;
        public int hour;
        public int day;

        // WEATHER (hasWeather menjaga kompatibilitas save lama)
        public bool hasWeather;
        public int weatherSeed;
        public int weatherDay;
        public WeatherType currentWeather;
        public WeatherType tomorrowWeather;

        // -------------------------
        // INVENTORY
        // -------------------------

        public int seed;
        public int cabbage;
        public int milk;

        // Format inventory grid baru. Field lama di atas tetap disimpan untuk
        // kompatibilitas save versi awal.
        public bool hasSlotInventory;
        public int inventoryLayoutVersion;
        public int backpackLevel;
        public List<InventorySlotSaveData> inventorySlots;

        // -------------------------
        // MODULAR FIELD AREAS
        // -------------------------

        public List<FieldSaveData> fields;

        // PROPERTY SITES: jenis bangunan, level, state konstruksi, dan completion day.
        public List<PropertySiteSaveData> buildings;

        // PLAYER HOUSE dan progression desa disimpan terpisah karena rumah tidak dapat
        // direlokasi/didemolish seperti Property Site farm.
        public PlayerHouseSaveData playerHouse;
        public VillageProgressSaveData villageProgress;

        // WEED / GRASS / ROCK WORLD STATE
        public List<GatherableSaveData> gatherables;

        // ITEM YANG DI-DROP/PLACE DAN KONDISI POHON/TUNGGUL
        public List<PlacedItemSaveData> placedItems;
        public List<TreeSaveData> trees;

        // PLAYER STATUS (hasPlayerStatus menjaga kompatibilitas save lama)
        public bool hasPlayerStatus;
        public float playerHealth;
        public float playerStamina;
        public float playerHunger;
    }

    [Serializable]
    /// <summary>Representasi satu slot inventory beserta identitas asset dan jumlah stack.</summary>
    public class InventorySlotSaveData
    {
        public string assetName;
        public string itemName;
        public int count;
        public int slotIndex;
    }

    // =====================================================
    // AWAKE
    // =====================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cari Inventory otomatis
        if (playerInv == null)
        {
            playerInv =
                FindFirstObjectByType<Inventory>();
        }

        if (playerStatus == null && playerInv != null)
            playerStatus = playerInv.GetComponent<PlayerStatusSystem>();

        // Cari Feedback otomatis
        if (feedback == null)
        {
            feedback =
                FindFirstObjectByType<SaveLoadFeedback>(
                    FindObjectsInactive.Include
                );
        }
    }

    // =====================================================
    // SAVE GAME
    // =====================================================

    /// <summary>Mengambil snapshot seluruh sistem terdaftar dan menulis JSON secara atomik.</summary>
    public void SaveGame()
    {
        // -------------------------
        // CHECK INVENTORY
        // -------------------------

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SAVE] Player Inventory tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // CHECK SCORE MANAGER
        // -------------------------

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "[SAVE] ScoreManager tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // CHECK TIME MANAGER
        // -------------------------

        if (TimeManager.Instance == null)
        {
            Debug.LogWarning(
                "[SAVE] TimeManager tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // CREATE SAVE DATA
        // -------------------------

        SaveData data =
            new SaveData();

        // -------------------------
        // GOLD
        // -------------------------

        data.gold =
            ScoreManager.Instance.points;

        // -------------------------
        // PLAYER POSITION
        // -------------------------

        Transform player =
            playerInv.transform;

        data.playerX =
            player.position.x;

        data.playerY =
            player.position.y;

        data.playerZ =
            player.position.z;

        // -------------------------
        // TIME
        // -------------------------

        data.minute =
            TimeManager.Instance.minute;

        data.hour =
            TimeManager.Instance.hour;

        data.day =
            TimeManager.Instance.day;

        if (WeatherSystem.Instance != null)
        {
            data.hasWeather = true;
            data.weatherSeed = WeatherSystem.Instance.WorldWeatherSeed;
            data.weatherDay = WeatherSystem.Instance.CurrentWeatherDay;
            data.currentWeather = WeatherSystem.Instance.CurrentWeather;
            data.tomorrowWeather = WeatherSystem.Instance.TomorrowWeather;
        }

        // -------------------------
        // INVENTORY
        // -------------------------

        data.seed =
            GetItemCount(seedItem);

        data.cabbage =
            GetItemCount(cabbageItem);

        data.milk =
            GetItemCount(milkItem);

        data.hasSlotInventory = true;
        // Versi 2: kapasitas grid tas tidak lagi termasuk empat slot hotbar.
        data.inventoryLayoutVersion = 2;
        data.backpackLevel = playerInv.BackpackLevel;
        data.inventorySlots = new List<InventorySlotSaveData>();
        for (int i = 0; i < playerInv.slots.Count; i++)
        {
            ItemStack stack = playerInv.slots[i];
            if (stack == null || stack.item == null || stack.count <= 0)
                continue;

            data.inventorySlots.Add(new InventorySlotSaveData
            {
                assetName = stack.item.name,
                itemName = stack.item.itemName,
                count = stack.count,
                slotIndex = i
            });
        }

        data.fields =
            FieldArea.CaptureAll();

        data.buildings =
            PropertySite.CaptureAll();

        data.playerHouse =
            PlayerHouseController.Instance != null ? PlayerHouseController.Instance.Capture() : null;

        data.villageProgress =
            VillageProgressionService.Instance != null ? VillageProgressionService.Instance.Capture() : null;

        data.gatherables =
            WorldGatherable.CaptureAll();

        data.placedItems =
            PlacedWorldItem.CaptureAll();

        data.trees =
            WorldTree.CaptureAll();

        if (playerStatus != null)
        {
            data.hasPlayerStatus = true;
            data.playerHealth = playerStatus.Health;
            data.playerStamina = playerStatus.Stamina;
            data.playerHunger = playerStatus.Hunger;
        }

        // -------------------------
        // JSON
        // -------------------------

        string json =
            JsonUtility.ToJson(
                data,
                true
            );

        // -------------------------
        // WRITE FILE
        // -------------------------

        File.WriteAllText(
            SavePath,
            json
        );

        // -------------------------
        // FEEDBACK
        // -------------------------

        if (feedback == null)
        {
            feedback =
                FindFirstObjectByType<SaveLoadFeedback>(
                    FindObjectsInactive.Include
                );
        }

        if (feedback != null)
        {
            feedback.ShowSave();
        }
        else
        {
            Debug.LogWarning(
                "[SAVE] SaveLoadFeedback tidak ditemukan."
            );
        }

        // -------------------------
        // DEBUG
        // -------------------------

        Debug.Log(
            "[SAVE] =========================\n" +
            "[SAVE] GAME SAVED\n" +
            "[SAVE] =========================\n" +
            $"Gold: {data.gold}\n" +
            $"Seed: {data.seed}\n" +
            $"Cabbage: {data.cabbage}\n" +
            $"Milk: {data.milk}\n" +
            $"Day: {data.day}\n" +
            $"Time: {data.hour:00}:{data.minute:00}\n" +
            $"Player Position: " +
            $"X={data.playerX}, " +
            $"Y={data.playerY}, " +
            $"Z={data.playerZ}\n" +
            $"Path: {SavePath}\n" +
            "[SAVE] ========================="
        );
    }

    // =====================================================
    // LOAD GAME
    // =====================================================

    /// <summary>Membaca save, memigrasikan layout lama, lalu memulihkan sistem dalam urutan dependensi.</summary>
    public void LoadGame()
    {
        // -------------------------
        // CHECK SAVE
        // -------------------------

        if (!File.Exists(SavePath))
        {
            Debug.Log(
                "[LOAD] Belum ada save game."
            );

            return;
        }

        // -------------------------
        // CHECK INVENTORY
        // -------------------------

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[LOAD] Player Inventory tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // CHECK SCORE MANAGER
        // -------------------------

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "[LOAD] ScoreManager tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // CHECK TIME MANAGER
        // -------------------------

        if (TimeManager.Instance == null)
        {
            Debug.LogWarning(
                "[LOAD] TimeManager tidak ditemukan."
            );

            return;
        }

        // -------------------------
        // READ FILE
        // -------------------------

        string json =
            File.ReadAllText(
                SavePath
            );

        SaveData data =
            JsonUtility.FromJson<SaveData>(
                json
            );

        if (data == null)
        {
            Debug.LogWarning(
                "[LOAD] Save data tidak valid."
            );

            return;
        }

        // -------------------------
        // LOAD GOLD
        // -------------------------

        ScoreManager.Instance.points =
            data.gold;

        // -------------------------
        // LOAD TIME
        // -------------------------

        TimeManager.Instance.minute =
            data.minute;

        TimeManager.Instance.hour =
            data.hour;

        TimeManager.Instance.day =
            data.day;

        WeatherSystem weatherSystem = WeatherSystem.Instance != null
            ? WeatherSystem.Instance
            : FindFirstObjectByType<WeatherSystem>();
        if (weatherSystem != null)
        {
            if (data.hasWeather)
            {
                weatherSystem.RestoreForecast(
                    data.weatherDay,
                    data.weatherSeed,
                    data.currentWeather,
                    data.tomorrowWeather
                );
            }
            else
            {
                weatherSystem.EnsureForecastForDay(data.day);
            }
        }

        // -------------------------
        // CLEAR INVENTORY
        // -------------------------

        // Version gate mempertahankan kompatibilitas save sebelum sistem backpack/hotbar.
        // Jangan menghapus jalur legacy sampai format save production resmi dimigrasikan.
        if (data.inventoryLayoutVersion >= 1)
            playerInv.SetBackpackLevel(data.backpackLevel);
        playerInv.Clear();

        // -------------------------
        // LOAD INVENTORY
        // -------------------------

        if (data.hasSlotInventory && data.inventorySlots != null)
        {
            for (int i = 0; i < data.inventorySlots.Count; i++)
            {
                InventorySlotSaveData savedSlot = data.inventorySlots[i];
                ItemSO item = ResolveSavedItem(savedSlot);
                if (item == null)
                {
                    Debug.LogWarning($"[LOAD] Item '{savedSlot.itemName}' tidak ditemukan di catalog asset aktif.");
                    continue;
                }

                if (data.inventoryLayoutVersion >= 1)
                {
                    if (!playerInv.TrySetSlot(savedSlot.slotIndex, item, savedSlot.count))
                        AddItem(item, savedSlot.count);
                }
                else
                {
                    AddItem(item, savedSlot.count);
                }
            }
        }
        else
        {
            // Save lama sebelum inventory 3x3.
            AddItem(seedItem, data.seed);
            AddItem(cabbageItem, data.cabbage);
            AddItem(milkItem, data.milk);
        }

        // -------------------------
        // LOAD FIELD AREAS
        // -------------------------

        FieldArea.RestoreAll(
            data.fields
        );

        // PropertySite dipulihkan setelah FieldArea agar occupancy footprint sudah tersedia.
        PropertySite.RestoreAll(
            data.buildings
        );

        VillageProgressionService.Instance?.Restore(
            data.villageProgress
        );

        PlayerHouseController.Instance?.Restore(
            data.playerHouse
        );

        WorldGatherable.RestoreAll(
            data.gatherables
        );

        PlacedWorldItem.RestoreAll(
            data.placedItems
        );

        WorldTree.RestoreAll(
            data.trees
        );

        if (data.hasPlayerStatus)
        {
            if (playerStatus == null)
                playerStatus = playerInv.GetComponent<PlayerStatusSystem>();

            playerStatus?.RestoreSavedState(
                data.playerHealth,
                data.playerStamina,
                data.playerHunger
            );
        }

        // -------------------------
        // SAVED POSITION
        // -------------------------

        Vector3 savedPosition =
            new Vector3(
                data.playerX,
                data.playerY,
                data.playerZ
            );

        // -------------------------
        // RESTORE POSITION
        // -------------------------

        StartCoroutine(
            RestorePlayerPosition(
                savedPosition
            )
        );

        // -------------------------
        // FEEDBACK
        // -------------------------

        if (feedback == null)
        {
            feedback =
                FindFirstObjectByType<SaveLoadFeedback>(
                    FindObjectsInactive.Include
                );
        }

        if (feedback != null)
        {
            feedback.ShowLoad();
        }
        else
        {
            Debug.LogWarning(
                "[LOAD] SaveLoadFeedback tidak ditemukan."
            );
        }

        // -------------------------
        // DEBUG
        // -------------------------

        Debug.Log(
            "[LOAD] =========================\n" +
            "[LOAD] GAME LOADED\n" +
            "[LOAD] =========================\n" +
            $"Gold: {data.gold}\n" +
            $"Seed: {data.seed}\n" +
            $"Cabbage: {data.cabbage}\n" +
            $"Milk: {data.milk}\n" +
            $"Day: {data.day}\n" +
            $"Time: {data.hour:00}:{data.minute:00}\n" +
            $"Saved Player Position: " +
            $"X={data.playerX}, " +
            $"Y={data.playerY}, " +
            $"Z={data.playerZ}\n" +
            "[LOAD] ========================="
        );
    }

    // =====================================================
    // RESTORE PLAYER POSITION
    // =====================================================

    IEnumerator RestorePlayerPosition(
        Vector3 savedPosition
    )
    {
        // Tunggu satu frame
        yield return null;

        // -------------------------
        // CHARACTER CONTROLLER
        // -------------------------

        CharacterController controller =
            playerInv.GetComponent<CharacterController>();

        bool controllerWasEnabled = false;

        if (controller != null)
        {
            controllerWasEnabled =
                controller.enabled;

            controller.enabled = false;
        }

        // -------------------------
        // RIGIDBODY
        // -------------------------

        Rigidbody rb =
            playerInv.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.position =
                savedPosition;
        }

        // -------------------------
        // TRANSFORM
        // -------------------------

        playerInv.transform.position =
            savedPosition;

        // -------------------------
        // RE-ENABLE CONTROLLER
        // -------------------------

        if (controller != null)
        {
            controller.enabled =
                controllerWasEnabled;
        }

        // Tunggu satu frame lagi
        yield return null;

        // -------------------------
        // FORCE POSITION AGAIN
        // -------------------------

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.position =
                savedPosition;
        }

        playerInv.transform.position =
            savedPosition;

        Debug.Log(
            "[LOAD] Player position restored: " +
            savedPosition
        );
    }

    // =====================================================
    // GET ITEM COUNT
    // =====================================================

    ItemSO ResolveSavedItem(InventorySlotSaveData savedSlot)
    {
        if (savedSlot == null)
            return null;

        ItemSO[] knownItems = { seedItem, cabbageItem, milkItem };
        for (int i = 0; i < knownItems.Length; i++)
        {
            ItemSO item = knownItems[i];
            if (item != null &&
                (item.name == savedSlot.assetName || item.itemName == savedSlot.itemName))
                return item;
        }

        // Mencakup ItemSO tambahan yang sudah direferensikan scene/prefab/catalog.
        ItemSO[] loadedItems = Resources.FindObjectsOfTypeAll<ItemSO>();
        for (int i = 0; i < loadedItems.Length; i++)
        {
            ItemSO item = loadedItems[i];
            if (item.name == savedSlot.assetName || item.itemName == savedSlot.itemName)
                return item;
        }

        return null;
    }

    int GetItemCount(
        ItemSO item
    )
    {
        if (item == null)
        {
            Debug.LogWarning(
                "[SAVE] ItemSO belum di-assign."
            );

            return 0;
        }

        if (playerInv == null)
            return 0;

        return playerInv.GetCount(
            item
        );
    }

    // =====================================================
    // ADD ITEM
    // =====================================================

    void AddItem(
        ItemSO item,
        int amount
    )
    {
        if (item == null)
        {
            Debug.LogWarning(
                "[LOAD] ItemSO belum di-assign."
            );

            return;
        }

        if (amount <= 0)
            return;

        playerInv.Add(
            item,
            amount
        );
    }

    // =====================================================
    // DELETE SAVE
    // =====================================================

    /// <summary>Menghapus save aktif dari persistent data path.</summary>
    public void DeleteSave()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log(
                "[SAVE] Tidak ada save game."
            );

            return;
        }

        File.Delete(
            SavePath
        );

        Debug.Log(
            "[SAVE] Save game berhasil dihapus."
        );
    }

    // =====================================================
    // HAS SAVE
    // =====================================================

    /// <summary>True jika file save aktif tersedia.</summary>
    public bool HasSave()
    {
        return File.Exists(
            SavePath
        );
    }

    // =====================================================
    // GET SAVE PATH
    // =====================================================

    /// <summary>Mengembalikan lokasi save untuk debug atau dukungan teknis.</summary>
    public string GetSavePath()
    {
        return SavePath;
    }
}
