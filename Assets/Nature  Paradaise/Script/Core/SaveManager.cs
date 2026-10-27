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

    public static string DefaultSavePath => SaveSlots.GetPath(1);
    private string SavePath => SaveSlots.GetPath(SaveSlots.ActiveSlot);
    Coroutine locationRestore;
    public bool IsLoading => locationRestore!=null;

    public static bool SaveExists() => SaveSlots.HasAnySave;

    public static void DeleteSaveFile()
    {
        DeleteSaveFile(SaveSlots.ActiveSlot);
    }

    /// <summary>Resets only the selected save and session services; the other slots remain intact.</summary>
    public static void DeleteSaveFile(int slot)
    {
        if (!SaveSlots.IsValid(slot)) return;
        string path = SaveSlots.GetPath(slot);
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
        ToolStorageService.Clear();
        RefrigeratorService.Clear();
        HouseStorageService.Clear();
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
        public bool hasPlayerLocation;
        public PlayerLocationSaveData savedPlayerLocation;
        public bool hasStarterPitchfork;

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
        public List<RefrigeratorEntrySaveData> houseStorage;
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
        public List<BuildingSiteSaveData> fixedBuildingSites;

        // PLAYER HOUSE dan progression desa disimpan terpisah karena rumah tidak dapat
        // direlokasi/didemolish seperti Property Site farm.
        public PlayerHouseSaveData playerHouse;
        public VillageProgressSaveData villageProgress;

        // WEED / GRASS / ROCK WORLD STATE
        public List<GatherableSaveData> gatherables;
        public List<TerrainDetailGrassAreaSaveData> terrainDetailGrass;

        // ITEM YANG DI-DROP/PLACE DAN KONDISI POHON/TUNGGUL
        public List<PlacedItemSaveData> placedItems;
        public List<LoosePickupSaveData> loosePickups;
        public List<TreeSaveData> trees;
        public List<FertilizerProcessorSaveData> fertilizerProcessors;
        public List<FeedMakerSaveData> feedMakers;
        public List<FeedSiloSaveData> feedSilos;

        // HEWAN: umur, growth progress, care, health, produksi, trait, dan posisi.
        public List<AnimalSaveData> animals;
        public List<EggIncubatorSaveData> eggIncubators;
        public AnimalManureSaveData animalManure;
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
        public bool hasStaminaProgression;
        public int staminaSkillTier, staminaBerryTier;
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

    /// <summary>Writes only the separate autosave; gameplay callbacks never overwrite manual slots.</summary>
    public void SaveGame()
    {
        SaveSnapshot(SaveSlots.AutoSaveSlot);
    }

    /// <summary>Manual save entry point for buttons, the save book and keyboard shortcuts.</summary>
    public void RequestSave()
    {
        if (!IsLoading && (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsTransitioning))
            SaveSlotMenu.Show(SaveSlotMenuMode.Save, slot => { if (this != null) SaveToSlot(slot); });
    }

    /// <summary>Opens the same four-slot picker used by Continue.</summary>
    public void RequestLoad()
    {
        if (!IsLoading && (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsTransitioning))
            SaveSlotMenu.Show(SaveSlotMenuMode.Load, slot => { if (this != null) LoadFromSlot(slot); });
    }

    /// <summary>Writes a full atomic snapshot to one slot, selecting it only after the write succeeds.</summary>
    public bool SaveToSlot(int slot)
    {
        if (!SaveSlots.IsValid(slot)) return false;
        return SaveSnapshot(slot);
    }

    bool SaveSnapshot(int slot)
    {
        string targetPath = SaveSlots.GetPath(slot);
        if(IsLoading || SceneTransitionManager.Instance!=null && SceneTransitionManager.Instance.IsTransitioning)
        {SaveLoadFeedback.Instance?.ShowMessage("Tunggu perpindahan lokasi selesai sebelum menyimpan.");return false;}
        // -------------------------
        // CHECK INVENTORY
        // -------------------------

        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SAVE] Player Inventory tidak ditemukan."
            );

            return false;
        }

        // -------------------------
        // CHECK SCORE MANAGER
        // -------------------------

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "[SAVE] ScoreManager tidak ditemukan."
            );

            return false;
        }

        // -------------------------
        // CHECK TIME MANAGER
        // -------------------------

        if (TimeManager.Instance == null)
        {
            Debug.LogWarning(
                "[SAVE] TimeManager tidak ditemukan."
            );

            return false;
        }

        // -------------------------
        // CREATE SAVE DATA
        // -------------------------

        playerInv.GetComponent<FishingSystem>()?.CommitPendingCatchForSave();

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

        // Retain exterior coordinates for legacy readers and unavailable-interior fallback.
        // The separate location record below restores the actual room and local pose.
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
        data.hasPlayerLocation=true;
        data.savedPlayerLocation=SceneTransitionManager.Instance!=null?SceneTransitionManager.Instance.CaptureLocation(player):
            new PlayerLocationSaveData{worldPosition=player.position,worldEuler=player.eulerAngles};
        data.hasStarterPitchfork=playerInv.HasStarterPitchfork;

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
        data.houseStorage = HouseStorageService.Capture();
        data.kitchen = KitchenService.Capture();
        data.aquariums = AquariumService.Capture();
        data.fishPonds = FishPondService.Capture();

        data.fields =
            FieldArea.CaptureAll();

        data.fixedBuildingSites = BuildingSite.CaptureAll();
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
        data.loosePickups=WorldItemPickup.CaptureLoose();


        data.eggIncubators = EggIncubator.CaptureAll();
        data.animals =
            AnimalGrowthSystem.CaptureAll();
        data.animalHomes = AnimalHome.CaptureAll();
        data.animalManure = AnimalManureSystem.Capture();
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
            data.hasStaminaProgression=true;
            data.staminaSkillTier=playerStatus.StaminaSkillTier;
            data.staminaBerryTier=playerStatus.StaminaBerryTier;
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

        try
        {
            string pending=targetPath+".tmp";
            File.WriteAllText(pending,json);
            if(File.Exists(targetPath))File.Replace(pending,targetPath,targetPath+".bak");
            else File.Move(pending,targetPath);
        }
        catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is NotSupportedException)
        {
            Debug.LogError($"[SAVE] Gagal menulis save; file sebelumnya dipertahankan. {error.Message}");
            (feedback!=null?feedback:SaveLoadFeedback.Instance)?.ShowMessage("Gagal menyimpan. Save sebelumnya tetap dipertahankan.");
            return false;
        }

        if (slot != SaveSlots.AutoSaveSlot) SaveSlots.Select(slot);

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
            feedback.ShowMessage(slot == SaveSlots.AutoSaveSlot ? "Autosave tersimpan" : $"Tersimpan di Slot {slot}");
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
            $"Path: {targetPath}\n" +
            "[SAVE] ========================="
        );
        return true;
    }

    // =====================================================
    // LOAD GAME
    // =====================================================

    /// <summary>Membaca save, memigrasikan layout lama, lalu memulihkan sistem dalam urutan dependensi.</summary>
    public void LoadGame() => LoadFromSlot(SaveSlots.ActiveSlot);

    /// <summary>Loads a manual save or the separate autosave after validating all saved data.</summary>
    public void LoadFromSlot(int slot)
    {
        if (!SaveSlots.IsReadable(slot)) return;
        string sourcePath = SaveSlots.GetPath(slot);
        if(IsLoading || SceneTransitionManager.Instance!=null && SceneTransitionManager.Instance.IsTransitioning)return;
        // -------------------------
        // CHECK SAVE
        // -------------------------

        if (!File.Exists(sourcePath))
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

        SaveData data;string json;
        try{json=File.ReadAllText(sourcePath);data=JsonUtility.FromJson<SaveData>(json);}
        catch(Exception error) when(error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
        {RejectLoad("File save tidak bisa dibaca: "+error.Message);return;}

        if (data == null)
        {
            Debug.LogWarning(
                "[LOAD] Save data tidak valid."
            );

            return;
        }

        // JsonUtility materializes missing lists as empty lists. Missing old sections must not erase current objects.
        if(!HasSaveSection(json,"placedItems"))data.placedItems=null;
        if(!HasSaveSection(json,"loosePickups"))data.loosePickups=null;
        if(data.hasSlotInventory && !HasSaveSection(json,"inventorySlots") ||
           !data.hasSlotInventory && !HasSaveSection(json,"seed") && !HasSaveSection(json,"cabbage") && !HasSaveSection(json,"milk"))
        {RejectLoad("Bagian inventory pada save tidak lengkap.");return;}

        // Resolve every saved item and plan the complete bag before changing money, time or world objects.
        if(!TryPrepareInventory(data,out int restoredBagLevel,out List<ItemStack> restoredSlots,out string loadProblem) ||
           !ValidateWorldItems(data,out loadProblem))
        {RejectLoad(loadProblem);return;}

        locationRestore=StartCoroutine(LoadRoutine(data,slot,restoredBagLevel,restoredSlots));
    }

    IEnumerator LoadRoutine(SaveData data,int slot,int restoredBagLevel,List<ItemStack> restoredSlots)
    {
        var clock=TimeManager.Instance;
        var movement=playerInv.GetComponent<PlayerController>();
        LoadingScreen.Show(this);
        clock?.AcquirePause(this);movement?.AcquireMovementLock(this);WorldInteractionPrompt.AcquireSuppression(this);
        try
        {
        // Draw the black cover before replacing buildings, animals, inventory or the player pose.
        yield return null;
        SaveSlots.Select(slot);

        // -------------------------
        // LOAD GOLD
        // -------------------------

        playerInv.GetComponent<PlayerToolApproach>()?.Cancel();
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

        SeasonVisualController.RefreshFromCalendar();

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
        playerInv.RestoreContents(restoredBagLevel,restoredSlots);

        // -------------------------
        // LOAD INVENTORY
        // -------------------------

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
        BuildingSite.RestoreAll(data.fixedBuildingSites);
        FishPondService.Restore(data.fishPonds);

        VillageProgressionService.Instance?.Restore(
            data.villageProgress
        );

        HouseStorageService.IsRestoringSave=true;
        try
        {
            PlayerHouseController.Instance?.Restore(data.playerHouse);
            RefrigeratorService.Restore(data.refrigerator);
            HouseStorageService.Restore(data.houseStorage);
        }
        finally { HouseStorageService.IsRestoringSave=false; }
        HouseStorageService.SortForHouseLevel();
        KitchenService.Restore(data.kitchen);
        playerInv.RestoreStarterPitchfork(data.hasStarterPitchfork);
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
        WorldItemPickup.RestoreLoose(data.loosePickups);

        FertilizerProcessor.RestoreAll(data.fertilizerProcessors);
        FeedMaker.RestoreAll(data.feedMakers);
        FeedSilo.RestoreAll(data.feedSilos);
        BarnInterior.ResetVisit();
        WorldTree.RestoreAll(data.trees);
        foreach (TerrainTreeManager manager in FindObjectsByType<TerrainTreeManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            manager.RestoreManagedTrees(data.trees);

        AnimalHome.RestoreAll(data.animalHomes);
        EggIncubator.RestoreAll(data.eggIncubators);
        AnimalGrowthSystem.RestoreAll(
            data.animals
        );
        EggIncubator.AdvanceAll(EggIncubator.CurrentDay);
        PersonalAnimal.RestoreAll(data.personalAnimals);
        AnimalManureSystem.Restore(data.animalManure);
        TameableCreature.RestoreAll(data.tameableCreatures);
        MarketStand.RestoreAll(data.marketStands);
        ShippingBin.RestoreAll(data.shippingBins);
        // Inventory dan Village Level harus pulih lebih dahulu karena menjadi condition quest/dialogue.
        QuestService.Instance?.Restore(data.questSystem);
        DialogueService.Instance?.Restore(data.dialogueSystem);
        WorldMapService.Instance?.Restore(data.worldMap);
        // Development grants belong to a new-session loadout, never to restoration of saved inventory.

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

            playerStatus?.RestoreStaminaProgression(data.hasStaminaProgression?data.staminaSkillTier:0, data.hasStaminaProgression?data.staminaBerryTier:0);
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

        yield return RestoreSavedLocation(data);
        // Animal LateUpdate and additive UI/layout setup finish before the final cover is released.
        yield return null;
        }
        finally
        {
            clock?.ReleasePause(this);movement?.ReleaseMovementLock(this);WorldInteractionPrompt.ReleaseSuppression(this);
            LoadingScreen.Hide(this);
            locationRestore=null;
        }
    }

    IEnumerator RestoreSavedLocation(SaveData data)
    {
        var movement=playerInv.GetComponent<PlayerController>();
            if(movement!=null)
            {
                var transition=SceneTransitionManager.Instance;
                if(transition==null)transition=new GameObject("SceneTransitionManager_Runtime").AddComponent<SceneTransitionManager>();
                var location=data.hasPlayerLocation && data.savedPlayerLocation!=null?data.savedPlayerLocation:
                    new PlayerLocationSaveData{worldPosition=new Vector3(data.playerX,data.playerY,data.playerZ),worldEuler=playerInv.transform.eulerAngles};
                yield return transition.RestoreLocation(location);
            }
            else yield return RestorePlayerPosition(new Vector3(data.playerX,data.playerY,data.playerZ));
            ReportLoaded(data);
    }

    void OnDisable()
    {
        if(locationRestore!=null)StopCoroutine(locationRestore);
        locationRestore=null;
        TimeManager.Instance?.ReleasePause(this);
        if(playerInv!=null)playerInv.GetComponent<PlayerController>()?.ReleaseMovementLock(this);
        WorldInteractionPrompt.ReleaseSuppression(this);
        LoadingScreen.Hide(this);
    }

    void ReportLoaded(SaveData data)
    {

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
            feedback.ShowMessage(SaveSlots.ActiveSlot == SaveSlots.AutoSaveSlot ? "Autosave dimuat" : $"Slot {SaveSlots.ActiveSlot} dimuat");
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

    static bool HasSaveSection(string json,string key)=>System.Text.RegularExpressions.Regex.IsMatch(json,"\""+key+"\"\\s*:");

    bool TryPrepareInventory(SaveData data,out int level,out List<ItemStack> restored,out string problem)
    {
        level=data.inventoryLayoutVersion>=1?data.backpackLevel:playerInv.BackpackLevel;
        restored=new List<ItemStack>(new ItemStack[playerInv.CapacityAtBackpackLevel(level)]);
        problem=null;
        if(data.day<1 || data.hour<0 || data.hour>23 || data.minute<0 || data.minute>59)
        {problem="Waktu pada save tidak valid.";return false;}
        var entries=data.inventorySlots;
        if(data.hasSlotInventory && entries==null)
        {problem="Bagian inventory pada save tidak lengkap.";return false;}
        if(!data.hasSlotInventory)
        {
            entries=new List<InventorySlotSaveData>();
            ItemSO[] legacy={seedItem,cabbageItem,milkItem};int[] counts={data.seed,data.cabbage,data.milk};
            for(int i=0;i<legacy.Length;i++)if(counts[i]>0)
            {
                if(legacy[i]==null){problem="Referensi item save legacy belum tersedia.";return false;}
                entries.Add(new InventorySlotSaveData{itemId=legacy[i].Id,count=counts[i]});
            }
        }
        var overflow=new List<ItemStack>();
        foreach(var saved in entries)
        {
            if(saved==null || saved.count==0)continue;
            if(saved.count<0){problem="Jumlah item pada save tidak valid.";return false;}
            var item=ResolveSavedItem(saved);
            if(item==null){problem=$"Aset item belum tersedia: {saved.itemId} ({saved.assetName}).";return false;}
            var stack=new ItemStack{item=item,count=saved.count,qualityStars=Mathf.Clamp(saved.qualityStars,0,5),
                fishSizeCm=Mathf.Max(0,saved.fishSizeCm),fishWeightKg=Mathf.Max(0,saved.fishWeightKg)};
            int target=data.inventoryLayoutVersion<3 && saved.slotIndex>=4?saved.slotIndex+4:saved.slotIndex;
            if(data.inventoryLayoutVersion>=1 && target>=0 && target<restored.Count && restored[target]==null)
            {int first=Mathf.Min(stack.count,item.StackLimit);restored[target]=CopyStack(stack,first);stack.count-=first;}
            if(stack.count>0)overflow.Add(stack);
        }
        foreach(var stack in overflow)
        {
            foreach(var current in restored)
                if(current!=null && current.item==stack.item && current.qualityStars==stack.qualityStars &&
                   current.fishSizeCm==stack.fishSizeCm && current.fishWeightKg==stack.fishWeightKg)
                {int moved=Mathf.Min(stack.count,Mathf.Max(0,current.item.StackLimit-current.count));current.count+=moved;stack.count-=moved;}
            for(int i=0;i<restored.Count && stack.count>0;i++)if(restored[i]==null)
            {int moved=Mathf.Min(stack.count,stack.item.StackLimit);restored[i]=CopyStack(stack,moved);stack.count-=moved;}
            if(stack.count>0){problem="Kapasitas tas tidak cukup untuk seluruh isi save. Isi saat ini dipertahankan.";return false;}
        }
        return true;
    }

    static ItemStack CopyStack(ItemStack source,int count)=>new ItemStack{item=source.item,count=count,
        qualityStars=source.qualityStars,fishSizeCm=source.fishSizeCm,fishWeightKg=source.fishWeightKg};

    bool ValidateWorldItems(SaveData data,out string problem)
    {
        problem=null;
        if(data.fields!=null)foreach(var saved in data.fields)
            foreach(var field in FieldArea.ActiveAreas)
                if(field!=null && saved!=null && field.FieldId==saved.fieldId && !field.CanRestoreCrops(saved,out problem))return false;
        if(data.placedItems!=null)foreach(var saved in data.placedItems)
            if(saved!=null && (saved.amount<=0 || ItemCatalog.Resolve(saved.itemId,saved.assetName,saved.itemName)==null))
            {problem=$"Aset item dunia belum tersedia: {saved.itemId} ({saved.assetName}).";return false;}
        if(data.loosePickups!=null)foreach(var saved in data.loosePickups)
            if(saved!=null && (saved.amount<=0 || ItemCatalog.Resolve(saved.itemId,saved.assetName,saved.itemName)==null))
            {problem=$"Aset drop belum tersedia: {saved.itemId} ({saved.assetName}).";return false;}
        return true;
    }

    void RejectLoad(string problem)
    {
        Debug.LogWarning($"[LOAD] Dibatalkan sebelum state berubah. {problem} Path: {SavePath}");
        (feedback!=null?feedback:SaveLoadFeedback.Instance)?.ShowMessage("Load dibatalkan: "+problem);
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
        if (!SaveSlots.Exists(SaveSlots.ActiveSlot))
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
        return SaveSlots.Exists(SaveSlots.ActiveSlot);
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
