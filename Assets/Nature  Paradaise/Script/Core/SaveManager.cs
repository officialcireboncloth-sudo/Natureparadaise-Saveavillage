using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Koordinator save/load JSON untuk status player, inventory, waktu, cuaca, field,
/// resource gathering, pohon, hewan, dan placed item. Menangani kompatibilitas layout save lama.
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

    public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, "savegame.json");
    private string SavePath => DefaultSavePath;

    public static bool SaveExists() => File.Exists(DefaultSavePath);

    public static void DeleteSaveFile()
    {
        if (File.Exists(DefaultSavePath)) File.Delete(DefaultSavePath);
        ToolStorageService.Clear();
        RefrigeratorService.Clear();
        KitchenService.Clear();
        AquariumService.Clear();
        FishPondService.Clear();
        FishCollectionService.Clear();
        WorldMapService.Instance?.ResetProgress();
    }

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

        // WATERING CAN: has flag menjaga save lama tetap dimulai dalam kondisi penuh.
        public bool hasWateringCanState;
        public int wateringCanWater;

        // WEATHER (hasWeather menjaga kompatibilitas save lama)
        public bool hasWeather;
        public int weatherSeed;
        public int weatherDay;
        public WeatherType currentWeather;
        public WeatherType tomorrowWeather;
        public WeatherType previousWeather;
        public int weatherCleanupDay = -1;

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

        // TOOL STORAGE: satu storage global yang dapat diakses dari peti mana pun di rumah.
        public List<ToolStorageEntrySaveData> toolStorage;
        // REFRIGERATOR: quality dan fish size dipertahankan per stack.
        public List<RefrigeratorEntrySaveData> refrigerator;
        // KITCHEN: recipe yang dipelajari dan cooking collection.
        public KitchenProgressSaveData kitchen;
        // AQUARIUM: furniture per-instance, dekorasi, serta fish quality/size/weight.
        public List<AquariumSaveData> aquariums;
        // FISH POND: ikan per-ekor, growth progress, level, dan status pakan harian.
        public List<FishPondSaveData> fishPonds;

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
        public List<TerrainDetailGrassAreaSaveData> terrainDetailGrass;

        // ITEM YANG DI-DROP/PLACE DAN KONDISI POHON/TUNGGUL
        public List<PlacedItemSaveData> placedItems;
        public List<TreeSaveData> trees;
        public List<FertilizerProcessorSaveData> fertilizerProcessors;
        public List<FeedMakerSaveData> feedMakers;
        public List<FeedSiloSaveData> feedSilos;

        // HEWAN: umur, growth progress, care, health, produksi, trait, dan posisi.
        public List<AnimalSaveData> animals;
        public List<AnimalHomeSaveData> animalHomes;
        // PERSONAL COMPANION terpisah dari ternak produksi Barn/Coop.
        public List<PersonalAnimalSaveData> personalAnimals;
        public List<TameableCreatureSaveData> tameableCreatures;

        // MARKET STAND: stok jual pasif, waktu simulasi terakhir, dan pendapatan total.
        public List<MarketStandSaveData> marketStands;
        // SHIPPING BIN: barang pending, snapshot harga, dan laporan penjualan terakhir.
        public List<ShippingBinSaveData> shippingBins;
        // FISH COLLECTION: jumlah tangkapan dan rekor ukuran per jenis ikan.
        public List<FishCollectionEntrySaveData> fishCollection;
        // FISHING: bait terpasang dan progression skill. Gate menjaga kompatibilitas save lama.
        public bool hasFishingProgress;
        public string equippedFishingBaitItemId;
        public int fishingLevel;
        public int fishingExperience;
        // NARRATIVE: progres objective quest dan flag percakapan disimpan terpisah dari UI.
        public QuestSystemSaveData questSystem;
        public DialogueSystemSaveData dialogueSystem;
        // MAP: discovery, marker unlock, waypoint, dan pilihan mini map.
        public WorldMapSaveData worldMap;

        // PLAYER STATUS (hasPlayerStatus menjaga kompatibilitas save lama)
        public bool hasPlayerStatus;
        public float playerHealth;
        public float playerStamina;
        public float playerHunger;
        public bool hasExtendedPlayerStatus;
        public float playerMaxHealth;
        public float playerMaxStamina;
        public float playerMaxHunger;
        public bool hungerEnabled;
        public string playerLocation;
        public List<PlayerToolLevelData> toolLevels;
        public bool hasStatusEffectSlots;
        public List<PlayerStatusEffectSaveData> activeBuffs;
        public List<PlayerStatusEffectSaveData> activeDebuffs;
    }

    [Serializable]
    /// <summary>Representasi satu slot inventory beserta identitas asset dan jumlah stack.</summary>
    public class InventorySlotSaveData
    {
        public string itemId;
        public string assetName;
        public string itemName;
        public int count;
        public int slotIndex;
        public int qualityStars;
        public float fishSizeCm;
        public float fishWeightKg;
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

        // Interior additive tidak dimuat saat startup. Simpan posisi world terakhir agar
        // load berikutnya tidak menaruh player di koordinat interior tanpa lantai.
        Vector3 savedPlayerPosition = player.position;
        if (BarnInterior.TryGetReturnPosition(out Vector3 barnReturnPosition)) savedPlayerPosition = barnReturnPosition;
        if (SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.TryGetWorldReturnPosition(out Vector3 worldReturnPosition))
        {
            savedPlayerPosition = worldReturnPosition;
        }

        data.playerX =
            savedPlayerPosition.x;

        data.playerY =
            savedPlayerPosition.y;

        data.playerZ =
            savedPlayerPosition.z;

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
            data.previousWeather = WeatherSystem.Instance.PreviousWeather;
            data.weatherCleanupDay = WeatherSystem.Instance.IsCommunityCleanupDay
                ? WeatherSystem.Instance.CurrentWeatherDay
                : -1;
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
        // Versi 3: hotbar bertambah dari empat menjadi delapan slot.
        data.inventoryLayoutVersion = 3;
        data.backpackLevel = playerInv.BackpackLevel;
        data.inventorySlots = new List<InventorySlotSaveData>();
        WateringCanSystem wateringCan = playerInv.GetComponent<WateringCanSystem>();
        data.hasWateringCanState = wateringCan != null;
        data.wateringCanWater = wateringCan != null ? wateringCan.CurrentWater : 100;
        for (int i = 0; i < playerInv.slots.Count; i++)
        {
            ItemStack stack = playerInv.slots[i];
            if (stack == null || stack.item == null || stack.count <= 0)
                continue;

            data.inventorySlots.Add(new InventorySlotSaveData
            {
                itemId = stack.item.Id,
                assetName = stack.item.name,
                itemName = stack.item.itemName,
                count = stack.count,
                qualityStars = stack.qualityStars,
                fishSizeCm = stack.fishSizeCm,
                fishWeightKg = stack.fishWeightKg,
                slotIndex = i
            });
        }

        data.toolStorage = ToolStorageService.Capture();
        data.refrigerator = RefrigeratorService.Capture();
        data.kitchen = KitchenService.Capture();
        data.aquariums = AquariumService.Capture();
        data.fishPonds = FishPondService.Capture();

        data.fields =
            FieldArea.CaptureAll();

        data.buildings =
            PropertySite.CaptureAll();

        data.playerHouse =
            PlayerHouseController.Instance != null ? PlayerHouseController.Instance.Capture() : null;

        data.villageProgress =
            VillageProgressionService.Instance != null ? VillageProgressionService.Instance.Capture() : null;

        // Tree capture menyelesaikan drop yang masih tertunda sebelum pickup disnapshot.
        data.fertilizerProcessors = FertilizerProcessor.CaptureAll();
        data.feedMakers = FeedMaker.CaptureAll();
        data.feedSilos = FeedSilo.CaptureAll();
        data.trees = WorldTree.CaptureAll();
        foreach (TerrainTreeManager manager in FindObjectsByType<TerrainTreeManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            data.trees.AddRange(manager.CaptureManagedTrees());

        data.gatherables =
            WorldGatherable.CaptureAll();

        data.terrainDetailGrass =
            TerrainDetailGrassManager.CaptureAll();

        data.placedItems =
            PlacedWorldItem.CaptureAll();


        data.animals =
            AnimalGrowthSystem.CaptureAll();
        data.animalHomes = AnimalHome.CaptureAll();
        data.personalAnimals = PersonalAnimal.CaptureAll();
        data.tameableCreatures = TameableCreature.CaptureAll();
        data.marketStands = MarketStand.CaptureAll();
        data.shippingBins = ShippingBin.CaptureAll();
        data.fishCollection = FishCollectionService.Capture();
        FishingSystem fishingSystem = playerInv.GetComponent<FishingSystem>();
        if (fishingSystem != null)
        {
            data.hasFishingProgress = true;
            data.equippedFishingBaitItemId = fishingSystem.EquippedBait != null
                ? fishingSystem.EquippedBait.Id
                : string.Empty;
            data.fishingLevel = fishingSystem.FishingLevel;
            data.fishingExperience = fishingSystem.FishingExperience;
        }
        data.questSystem = QuestService.Instance?.Capture();
        data.dialogueSystem = DialogueService.Instance?.Capture();
        data.worldMap = WorldMapService.Instance?.Capture();

        if (playerStatus != null)
        {
            data.hasPlayerStatus = true;
            data.playerHealth = playerStatus.Health;
            data.playerStamina = playerStatus.Stamina;
            data.playerHunger = playerStatus.Hunger;
            data.hasExtendedPlayerStatus = true;
            data.playerMaxHealth = playerStatus.MaxHealth;
            data.playerMaxStamina = playerStatus.MaxStamina;
            data.playerMaxHunger = playerStatus.MaxHunger;
            data.hungerEnabled = playerStatus.HungerEnabled;
            data.playerLocation = playerStatus.PlayerLocation;
            data.toolLevels = playerStatus.CaptureToolLevels();
            data.hasStatusEffectSlots = true;
            data.activeBuffs = playerStatus.CaptureStatusEffects(PlayerStatusEffectType.Buff);
            data.activeDebuffs = playerStatus.CaptureStatusEffects(PlayerStatusEffectType.Debuff);
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
                    data.tomorrowWeather,
                    data.previousWeather,
                    data.weatherCleanupDay
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
                    // Layout versi 1/2 memakai empat hotbar. Geser slot tas empat
                    // posisi agar isi tas lama tidak berubah menjadi hotbar baru.
                    int targetSlot = data.inventoryLayoutVersion < 3 && savedSlot.slotIndex >= 4
                        ? savedSlot.slotIndex + 4
                        : savedSlot.slotIndex;
                    if (!playerInv.TrySetSlot(targetSlot, item, savedSlot.count, savedSlot.qualityStars, savedSlot.fishSizeCm, savedSlot.fishWeightKg))
                        playerInv.Add(item, savedSlot.count, savedSlot.qualityStars, savedSlot.fishSizeCm, savedSlot.fishWeightKg);
                }
                else
                {
                    playerInv.Add(item, savedSlot.count, savedSlot.qualityStars, savedSlot.fishSizeCm, savedSlot.fishWeightKg);
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

        ToolStorageService.Restore(data.toolStorage);

        WateringCanSystem wateringCan = playerInv.GetComponent<WateringCanSystem>();
        if (wateringCan == null) wateringCan = playerInv.gameObject.AddComponent<WateringCanSystem>();
        wateringCan.Restore(data.hasWateringCanState ? data.wateringCanWater : wateringCan.MaximumWater);

        FishingSystem fishingSystem = playerInv.GetComponent<FishingSystem>();
        if (fishingSystem != null)
        {
            fishingSystem.RestoreProgress(
                data.hasFishingProgress ? data.equippedFishingBaitItemId : string.Empty,
                data.hasFishingProgress ? data.fishingLevel : 1,
                data.hasFishingProgress ? data.fishingExperience : 0);
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
        FishPondService.Restore(data.fishPonds);

        VillageProgressionService.Instance?.Restore(
            data.villageProgress
        );

        PlayerHouseController.Instance?.Restore(
            data.playerHouse
        );

        // Restore setelah House agar kapasitas/unlock Refrigerator sudah memakai level save.
        RefrigeratorService.Restore(data.refrigerator);
        KitchenService.Restore(data.kitchen);
        AquariumService.Restore(data.aquariums);
        FishCollectionService.Restore(data.fishCollection);

        WorldGatherable.RestoreAll(
            data.gatherables
        );

        TerrainDetailGrassManager.RestoreAll(
            data.terrainDetailGrass
        );

        PlacedWorldItem.RestoreAll(
            data.placedItems
        );

        FertilizerProcessor.RestoreAll(data.fertilizerProcessors);
        FeedMaker.RestoreAll(data.feedMakers);
        FeedSilo.RestoreAll(data.feedSilos);
        BarnInterior.ResetVisit();
        WorldTree.RestoreAll(data.trees);
        foreach (TerrainTreeManager manager in FindObjectsByType<TerrainTreeManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            manager.RestoreManagedTrees(data.trees);

        AnimalHome.RestoreAll(data.animalHomes);
        AnimalGrowthSystem.RestoreAll(
            data.animals
        );
        PersonalAnimal.RestoreAll(data.personalAnimals);
        TameableCreature.RestoreAll(data.tameableCreatures);
        MarketStand.RestoreAll(data.marketStands);
        ShippingBin.RestoreAll(data.shippingBins);
        // Inventory dan Village Level harus pulih lebih dahulu karena menjadi condition quest/dialogue.
        QuestService.Instance?.Restore(data.questSystem);
        DialogueService.Instance?.Restore(data.dialogueSystem);
        WorldMapService.Instance?.Restore(data.worldMap);
        AquariumTestFishGrant.EnsureTestFish(playerInv);
        FishPondTestGrant.EnsureTestFeed(playerInv);

        if (data.hasPlayerStatus)
        {
            if (playerStatus == null)
                playerStatus = playerInv.GetComponent<PlayerStatusSystem>();

            if (data.hasExtendedPlayerStatus)
            {
                playerStatus?.RestoreStatusConfiguration(
                    data.playerMaxHealth,
                    data.playerMaxStamina,
                    data.playerMaxHunger,
                    data.hungerEnabled
                );
                playerStatus?.RestoreToolLevels(data.toolLevels);
            }

            playerStatus?.RestoreSavedState(
                data.playerHealth,
                data.playerStamina,
                data.playerHunger
            );

            if (data.hasStatusEffectSlots)
                playerStatus?.RestoreStatusEffects(data.activeBuffs, data.activeDebuffs);
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

        ItemSO catalogItem = ItemCatalog.Resolve(savedSlot.itemId, savedSlot.assetName, savedSlot.itemName);
        if (catalogItem != null)
            return catalogItem;

        ItemSO[] knownItems = { seedItem, cabbageItem, milkItem };
        for (int i = 0; i < knownItems.Length; i++)
        {
            ItemSO item = knownItems[i];
            if (item != null &&
                (item.name == savedSlot.assetName || item.itemName == savedSlot.itemName))
                return item;
        }

        // Mencakup ItemSO tambahan yang sudah direferensikan scene/prefab/catalog.
        FertilizerCatalog.Load(); // Muat juga item resep pada scene tanpa shop aktif.
        FarmEquipmentCatalog.Load(); // Muat sprinkler, bibit pohon, dan buah untuk restore.
        AnimalCareCatalog.Load();
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
        if (!SaveExists())
        {
            Debug.Log(
                "[SAVE] Tidak ada save game."
            );

            return;
        }

        DeleteSaveFile();

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
        return SaveExists();
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
