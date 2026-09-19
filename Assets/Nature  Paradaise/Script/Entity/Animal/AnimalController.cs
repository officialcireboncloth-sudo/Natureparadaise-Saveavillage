using UnityEngine;
using TMPro;

/// <summary>
/// Controller prototipe ternak untuk hunger, pemberian makan, produksi milk,
/// interaction range, dan sinkronisasi hasil dengan inventory player.
/// </summary>
public class AnimalController : MonoBehaviour
{
    static readonly System.Collections.Generic.List<AnimalController> Active = new();
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    [Header("Heart Care Items")]
    public ItemSO favoriteTreatItem;
    public ItemSO medicineItem;
    [Header("Growth System")]
    [SerializeField] AnimalGrowthSystem growth;
    [SerializeField] KeyCode petKey = KeyCode.P;
    [SerializeField] KeyCode debugNextStageKey = KeyCode.F10;

    [Header("Hunger")]
    public float maxHunger = 100f;
    public float hunger = 0f;

    [Header("Feeding")]
    [Tooltip("Berapa Hunger yang diberikan oleh 1 Cabbage.")]
    public float cabbageHunger = 50f;

    [Header("Milk Production")]
    [Tooltip("Berapa detik sampai hewan menghasilkan susu.")]
    public float milkProductionTime = 5f;

    [Tooltip("Hunger yang dikonsumsi untuk menghasilkan 1 Milk.")]
    public float milkHungerCost = 25f;

    [Header("Interaction")]
    public float interactionRadius = 2f;

    [InspectorName("Use Held Care Item Key")]
    public KeyCode feedKey = KeyCode.F;
    public KeyCode milkKey = KeyCode.G;

    [Header("Inventory")]
    public Inventory playerInv;

    [InspectorName("Animal Feed Item")]
    [Tooltip("Pakan olahan dari Feed Maker; nama field legacy dipertahankan agar referensi scene lama tetap terbaca.")]
    public ItemSO cabbageItem;

    [Tooltip("ItemSO Milk.")]
    public ItemSO milkItem;

    [Header("Debug UI")]
    public TMP_Text hungerText;

    bool milkReady = false;
    float milkTimer = 0f;

    ItemSO SelectedMedicine
    {
        get
        {
            InventoryHotbarUI hotbar = playerInv != null ? playerInv.GetComponent<InventoryHotbarUI>() : null;
            ItemSO selected = hotbar != null ? hotbar.SelectedItem : null;
            return selected != null && selected.IsAnimalMedicine ? selected : null;
        }
    }

    ItemSO SelectedItem => playerInv != null
        ? playerInv.GetComponent<InventoryHotbarUI>()?.SelectedItem
        : null;

    void Awake()
    {
        // Scene prototipe lama pernah menghubungkan debug text Cow ke label milik NPCSeller.
        // Jangan menulis status hewan ke UI yang bukan child dari hewan ini.
        if (hungerText != null && !hungerText.transform.IsChildOf(transform))
            hungerText = null;
        ApplyCareDefaults();
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        if (growth == null)
            growth = GetComponent<AnimalGrowthSystem>();
    }

    public void ConfigureRuntime(
        Inventory inventory,
        ItemSO feedItem,
        ItemSO productItem,
        AnimalGrowthSystem growthSystem)
    {
        playerInv = inventory;
        cabbageItem = feedItem;
        milkItem = productItem;
        growth = growthSystem;
        ApplyCareDefaults();
    }

    public void ApplyCareDefaults()
    {
        if (growth == null) growth = GetComponent<AnimalGrowthSystem>();
        AnimalCareCatalog catalog = AnimalCareCatalog.Load();
        if (catalog == null) return;
        if (cabbageItem == null || cabbageItem.itemName == "Cabbage" || cabbageItem.name == "Grass") cabbageItem = catalog.fodder;
        if (favoriteTreatItem == null) favoriteTreatItem = catalog.treat;
        if (medicineItem == null) medicineItem = catalog.medicine;
        if (milkItem == null && growth != null) milkItem = catalog.Product(growth.Type);
    }

    void Update()
    {
        if (growth == null) growth = GetComponent<AnimalGrowthSystem>();
        UpdateMilkProduction();
        UpdateDebugUI();
        if (playerInv == null)
            return;
        PlayerAnimalCarry animalCarry = playerInv.GetComponent<PlayerAnimalCarry>();
        if (animalCarry != null && animalCarry.IsCarrying(growth)) return;
        AnimalRoutine routine = GetComponent<AnimalRoutine>();
        bool housed = routine?.IsHoused ?? false;
        bool visitingThisHome = housed && BarnInterior.Current != null &&
                                BarnInterior.Current.home == routine?.Home;
        bool carryable = growth != null && growth.HasBeenBorn && AnimalGrowthProfileSO.IsBird(growth.Type);
        // Model housed disembunyikan di world, tetapi saat player berada di interior
        // kandang yang benar semua jenis hewan harus bisa di-pet/feed/inspect.
        if (housed && !visitingThisHome) return;
        if (playerInv.GetComponent<PlayerController>()?.IsMovementLocked ?? false) return;
        if (WorldInteractionPrompt.IsSuppressed) return;

        float distance = Vector3.Distance(
            transform.position,
            playerInv.transform.position
        );

        // Player terlalu jauh dari hewan
        if (!PlayerInteractionTarget.ContainsPickup(playerInv.transform, transform, interactionRadius))
            return;
        // Hanya hewan terdekat menerima satu tombol interaksi, bukan seluruh kandang.
        foreach (AnimalController other in Active)
        {
            if (other == this || other == null) continue;
            float otherDistance = Vector3.Distance(other.transform.position, playerInv.transform.position);
            if (PlayerInteractionTarget.ContainsPickup(playerInv.transform, other.transform, other.interactionRadius) &&
                (otherDistance < distance || (Mathf.Approximately(otherDistance, distance) && other.GetInstanceID() < GetInstanceID()))) return;
        }

        string interactionPrompt = $"{petKey}: Pet";
        if (carryable && animalCarry != null && !animalCarry.HasAnimal)
            interactionPrompt += "   E: Angkat";
        if (IsProductReady) interactionPrompt += $"   {milkKey}: Ambil produk";
        if (HUDManager.DebugCluesEnabled)
            interactionPrompt += $"   {debugNextStageKey}: Debug Next Stage";
        ItemSO selectedItem = SelectedItem;
        if (selectedItem != null && selectedItem == cabbageItem && growth != null && !growth.FedToday)
            interactionPrompt += $"   {feedKey}: Feed";
        else if (selectedItem != null && selectedItem == favoriteTreatItem && growth != null && growth.CanReceiveTreat)
            interactionPrompt += $"   {feedKey}: Give Treat";
        ItemSO selectedMedicine = SelectedMedicine;
        if (selectedMedicine != null && growth != null && growth.CanReceiveMedicine)
            interactionPrompt += $"   {feedKey}: Give Medicine ({selectedMedicine.animalMedicineLevel})";
        if (growth != null) interactionPrompt = growth.InfoSummary + "\n" + interactionPrompt;
        interactionPrompt += "   I: Animal Info";
        WorldInteractionPrompt.Request(this, transform, interactionPrompt, distance, 1.45f);

        HandleInput();
    }

    void HandleInput()
    {
        PlayerAnimalCarry animalCarry = playerInv.GetComponent<PlayerAnimalCarry>();
        if (growth != null && growth.HasBeenBorn && AnimalGrowthProfileSO.IsBird(growth.Type) &&
            animalCarry != null && !animalCarry.HasAnimal &&
            PlayerInteractionTarget.PressPickup(playerInv.transform, transform, KeyCode.E, interactionRadius))
        {
            animalCarry.TryPickup(growth);
            return;
        }

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, KeyCode.I))
        {
            AnimalCarePanel.Show(growth, null, playerInv);
            return;
        }
        // =========================
        // FEED CABBAGE
        // =========================

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, feedKey))
        {
            ItemSO selected = SelectedItem;
            if (selected != null && selected.IsAnimalMedicine)
                TryGiveMedicine();
            else if (selected != null && selected == favoriteTreatItem)
                TryGiveTreat();
            else if (selected != null && selected == cabbageItem)
                FeedCabbage();
        }

        // =========================
        // TAKE MILK
        // =========================

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, milkKey))
        {
            TakeMilk();
        }

        if (PlayerInteractionTarget.Press(playerInv.transform, transform, petKey))
            growth?.Pet();
        if (HUDManager.DebugCluesEnabled && Input.GetKeyDown(debugNextStageKey))
            growth?.DebugAdvanceToNextStage();
    }

    public void FeedCabbage()
    {
        if (growth != null && (!growth.HasBeenBorn || growth.FedToday)) return;
        if (cabbageItem == null)
        {
            Debug.LogWarning(
                "[ANIMAL] Cabbage Item belum di-assign."
            );
            return;
        }

        int cabbageCount = playerInv.GetCount(cabbageItem);

        if (cabbageCount <= 0)
        {
            Debug.Log(
                "[ANIMAL] Tidak punya Cabbage untuk memberi makan."
            );
            return;
        }

        // Hapus 1 Cabbage dari inventory
        if (!playerInv.Remove(cabbageItem, 1))
            return;

        // Tambahkan hunger
        hunger += cabbageHunger;

        // Clamp supaya tidak lebih dari max
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        growth?.RegisterFeeding(cabbageHunger, AnimalFoodSource.HandFeed);

        Debug.Log(
            $"[ANIMAL] Diberi makan {cabbageItem.itemName}. " +
            $"Hunger: {hunger}/{maxHunger}"
        );
    }

    void UpdateMilkProduction()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;
        // Growth adalah sumber nutrisi utama, termasuk makanan dari grazing tanpa FeedCabbage.
        if (growth != null) hunger = growth.Fullness;
        // Kalau susu sudah siap, jangan produksi lagi
        // sampai player mengambilnya.
        if (IsProductReady)
            return;

        // Hewan hanya memulai produksi setelah Adult dan kondisi hari ini ideal.
        if (growth != null && !growth.IsProductionEligible)
        {
            milkTimer = 0f;
            return;
        }

        // Tidak punya Hunger → tidak bisa menghasilkan susu
        if (hunger <= 0f)
        {
            milkTimer = 0f;
            return;
        }

        milkTimer += Time.deltaTime;

        if (milkTimer >= milkProductionTime)
        {
            milkTimer = 0f;

            // Konsumsi Hunger
            hunger -= milkHungerCost;
            hunger = Mathf.Max(hunger, 0f);

            // Susu siap diambil
            milkReady = true;
            growth?.SetProductReady(true);

            Debug.Log(
                $"[ANIMAL] Milk READY! " +
                $"Hunger: {hunger}/{maxHunger}"
            );
        }
    }

    public void TakeMilk()
    {
        if (!IsProductReady)
        {
            Debug.Log(
                "[ANIMAL] Milk belum siap."
            );
            return;
        }

        if (milkItem == null)
        {
            Debug.LogWarning(
                "[ANIMAL] Milk Item belum di-assign."
            );
            return;
        }

        // Grade ditentukan saat produk siap, bukan sesudah status produksi direset.
        int quality = growth != null ? growth.ProductQualityLevel : 1;
        if (playerInv == null || !playerInv.Add(milkItem, 1, quality))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Inventory penuh; produk tetap tersimpan pada hewan");
            return;
        }

        milkReady = false;
        growth?.MarkProductCollected();
        PlayerPickupNotification.ShowItem(playerInv, milkItem, 1);

        SaveLoadFeedback.Instance?.ShowMessage($"{milkItem.itemName} ({AnimalCareCatalog.QualityName(quality)}) berhasil diambil");

        // Kalau Hunger masih > 0,
        // produksi susu berikutnya dimulai lagi.
        milkTimer = 0f;
    }

    void UpdateDebugUI()
    {
        if (hungerText == null)
            return;

        // Status rinci hewan adalah debug clue dan mengikuti master toggle HUD (F9).
        hungerText.gameObject.SetActive(HUDManager.DebugCluesEnabled);
        if (!HUDManager.DebugCluesEnabled)
            return;

        string milkStatus = IsProductReady
            ? "READY TO MILK!"
            : "Milk: Not Ready";

        hungerText.text = growth != null
            ? $"{growth.StatusSummary}\n{milkStatus}"
            : $"Hunger: {Mathf.RoundToInt(hunger)}/{Mathf.RoundToInt(maxHunger)}\n{milkStatus}";
    }

    void Start()
    {
        UpdateDebugUI();
    }

    bool IsProductReady => growth != null ? growth.HasProductReady : milkReady;

    public bool TryGiveTreat()
    {
        if (growth == null || !growth.CanReceiveTreat || favoriteTreatItem == null || playerInv == null ||
            !playerInv.Remove(favoriteTreatItem, 1)) return false;
        growth.GiveFavoriteTreat();
        SaveLoadFeedback.Instance?.ShowMessage($"{growth.AnimalName} menyukai treat ini");
        return true;
    }

    public bool TryGiveMedicine()
    {
        return TryAdministerMedicine(SelectedMedicine);
    }

    /// <summary>Dipakai panel kandang: pilih obat terendah yang cukup, atau stok terkuat jika semuanya terlalu lemah.</summary>
    public bool TryGiveBestMedicine()
    {
        if (growth == null || playerInv == null || !growth.CanReceiveMedicine) return false;
        int required = growth.IllnessStage == AnimalIllnessStage.Critical ? 3 :
            growth.IllnessStage == AnimalIllnessStage.Severe ? 2 : 1;
        AnimalCareCatalog catalog = AnimalCareCatalog.Load();
        if (catalog == null) return false;
        for (int i = required; i <= 3; i++)
        {
            ItemSO candidate = catalog.Medicine((AnimalMedicineLevel)i);
            if (candidate != null && playerInv.GetCount(candidate) > 0) return TryAdministerMedicine(candidate);
        }
        for (int i = required - 1; i >= 1; i--)
        {
            ItemSO candidate = catalog.Medicine((AnimalMedicineLevel)i);
            if (candidate != null && playerInv.GetCount(candidate) > 0) return TryAdministerMedicine(candidate);
        }
        return false;
    }

    bool TryAdministerMedicine(ItemSO item)
    {
        if (growth == null || playerInv == null) return false;
        if (!growth.CanReceiveMedicine)
        {
            SaveLoadFeedback.Instance?.ShowMessage($"{growth.AnimalName} tidak membutuhkan obat.");
            return false;
        }
        if (item == null || !item.IsAnimalMedicine || playerInv.GetCount(item) <= 0)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Pilih Animal Medicine dari hotbar.");
            return false;
        }
        if (!playerInv.Remove(item, 1)) return false;
        AnimalMedicineResult result = growth.TreatWithMedicine(item.animalMedicineLevel);
        if (result == AnimalMedicineResult.Rejected)
        {
            playerInv.Add(item, 1);
            return false;
        }
        PlayMedicineAnimation();
        return true;
    }

    void PlayMedicineAnimation()
    {
        Animator animator = playerInv != null ? playerInv.GetComponentInChildren<Animator>() : null;
        if (animator == null) return;
        string[] candidates = { "GiveMedicine", "UseItem", "UseTool" };
        for (int i = 0; i < candidates.Length; i++)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == candidates[i])
                {
                    animator.SetTrigger(candidates[i]);
                    return;
                }
        }
    }

    /// <summary>Reset timer legacy setelah load agar state runtime tidak bocor ke save yang dipulihkan.</summary>
    public void RestoreProduction(float savedFullness)
    {
        hunger = Mathf.Clamp(savedFullness, 0f, maxHunger);
        milkTimer = 0f;
        milkReady = false;
    }
}
