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
    [SerializeField] KeyCode treatKey = KeyCode.Y;
    [SerializeField] KeyCode medicineKey = KeyCode.O;
    [Header("Growth System")]
    [SerializeField] AnimalGrowthSystem growth;
    [SerializeField] KeyCode petKey = KeyCode.P;
    [SerializeField] KeyCode debugNextStageKey = KeyCode.J;

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

    public KeyCode feedKey = KeyCode.F;
    public KeyCode milkKey = KeyCode.G;

    [Header("Inventory")]
    public Inventory playerInv;

    [InspectorName("Feed Item (Grass / Fodder)")]
    [Tooltip("Pakan harian; nama field legacy dipertahankan agar referensi scene lama tetap terbaca.")]
    public ItemSO cabbageItem;

    [Tooltip("ItemSO Milk.")]
    public ItemSO milkItem;

    [Header("Debug UI")]
    public TMP_Text hungerText;

    bool milkReady = false;
    float milkTimer = 0f;

    void Awake()
    {
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
        if (cabbageItem == null || cabbageItem.itemName == "Cabbage") cabbageItem = catalog.fodder;
        if (favoriteTreatItem == null) favoriteTreatItem = catalog.treat;
        if (medicineItem == null) medicineItem = catalog.medicine;
        if (milkItem == null && growth != null) milkItem = catalog.Product(growth.Type);
    }

    void Update()
    {
        if (growth == null) growth = GetComponent<AnimalGrowthSystem>();
        UpdateMilkProduction();
        UpdateDebugUI();
        if (GetComponent<AnimalRoutine>()?.IsHoused ?? false) return;
        if (playerInv == null)
            return;
        if (playerInv.GetComponent<PlayerController>()?.IsMovementLocked ?? false) return;
        if (WorldInteractionPrompt.IsSuppressed) return;

        float distance = Vector3.Distance(
            transform.position,
            playerInv.transform.position
        );

        // Player terlalu jauh dari hewan
        if (!PlayerInteractionTarget.Contains(playerInv.transform, transform))
            return;
        // Hanya hewan terdekat menerima satu tombol interaksi, bukan seluruh kandang.
        foreach (AnimalController other in Active)
        {
            if (other == this || other == null) continue;
            float otherDistance = Vector3.Distance(other.transform.position, playerInv.transform.position);
            if (PlayerInteractionTarget.Contains(playerInv.transform, other.transform) &&
                (otherDistance < distance || (Mathf.Approximately(otherDistance, distance) && other.GetInstanceID() < GetInstanceID()))) return;
        }

        string interactionPrompt = IsProductReady
            ? $"{feedKey}: Feed   {petKey}: Pet   {milkKey}: Ambil produk"
            : $"{feedKey}: Feed   {petKey}: Pet";
        if (HUDManager.DebugCluesEnabled)
            interactionPrompt += $"   {debugNextStageKey}: Debug Next Stage";
        if (favoriteTreatItem != null) interactionPrompt += $"   {treatKey}: Treat";
        if (medicineItem != null) interactionPrompt += $"   {medicineKey}: Medicine";
        if (growth != null) interactionPrompt = growth.InfoSummary + "\n" + interactionPrompt;
        interactionPrompt += "   I: Animal Info";
        WorldInteractionPrompt.Request(this, transform, interactionPrompt, distance, 1.45f);

        HandleInput();
    }

    void HandleInput()
    {
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
        if (PlayerInteractionTarget.Press(playerInv.transform, transform, treatKey)) TryGiveTreat();
        if (PlayerInteractionTarget.Press(playerInv.transform, transform, medicineKey)) TryGiveMedicine();

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
        growth?.RegisterFeeding(cabbageHunger);

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
        if (growth == null || !growth.CanReceiveMedicine ||
            medicineItem == null || playerInv == null || !playerInv.Remove(medicineItem, 1)) return false;
        growth.TreatWithMedicine();
        return true;
    }

    /// <summary>Reset timer legacy setelah load agar state runtime tidak bocor ke save yang dipulihkan.</summary>
    public void RestoreProduction(float savedFullness)
    {
        hunger = Mathf.Clamp(savedFullness, 0f, maxHunger);
        milkTimer = 0f;
        milkReady = false;
    }
}
