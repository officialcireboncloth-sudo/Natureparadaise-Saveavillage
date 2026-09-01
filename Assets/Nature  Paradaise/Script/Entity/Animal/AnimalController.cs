using UnityEngine;
using TMPro;

/// <summary>
/// Controller prototipe ternak untuk hunger, pemberian makan, produksi milk,
/// interaction range, dan sinkronisasi hasil dengan inventory player.
/// </summary>
public class AnimalController : MonoBehaviour
{
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

    [Tooltip("ItemSO Cabbage.")]
    public ItemSO cabbageItem;

    [Tooltip("ItemSO Milk.")]
    public ItemSO milkItem;

    [Header("Debug UI")]
    public TMP_Text hungerText;

    bool milkReady = false;
    float milkTimer = 0f;

    void Awake()
    {
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
    }

    void Update()
    {
        if (playerInv == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            playerInv.transform.position
        );

        // Player terlalu jauh dari hewan
        if (distance > interactionRadius)
            return;

        string interactionPrompt = milkReady
            ? $"{feedKey}: beri makan   {milkKey}: ambil susu"
            : $"Tekan {feedKey} untuk memberi makan";
        WorldInteractionPrompt.Request(this, transform, interactionPrompt, distance, 1.45f);

        HandleInput();
        UpdateMilkProduction();
        UpdateDebugUI();
    }

    void HandleInput()
    {
        // =========================
        // FEED CABBAGE
        // =========================

        if (Input.GetKeyDown(feedKey))
        {
            FeedCabbage();
        }

        // =========================
        // TAKE MILK
        // =========================

        if (Input.GetKeyDown(milkKey))
        {
            TakeMilk();
        }
    }

    void FeedCabbage()
    {
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

        Debug.Log(
            $"[ANIMAL] Diberi makan Cabbage. " +
            $"Hunger: {hunger}/{maxHunger}"
        );
    }

    void UpdateMilkProduction()
    {
        // Kalau susu sudah siap, jangan produksi lagi
        // sampai player mengambilnya.
        if (milkReady)
            return;

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

            Debug.Log(
                $"[ANIMAL] Milk READY! " +
                $"Hunger: {hunger}/{maxHunger}"
            );
        }
    }

    void TakeMilk()
    {
        if (!milkReady)
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

        // Masukkan 1 Milk ke inventory
        playerInv.Add(milkItem, 1);

        milkReady = false;

        Debug.Log(
            "[ANIMAL] Milk berhasil diambil!"
        );

        // Kalau Hunger masih > 0,
        // produksi susu berikutnya dimulai lagi.
        milkTimer = 0f;
    }

    void UpdateDebugUI()
    {
        if (hungerText == null)
            return;

        string milkStatus = milkReady
            ? "READY TO MILK!"
            : "Milk: Not Ready";

        hungerText.text =
            $"Hunger: {Mathf.RoundToInt(hunger)}/{Mathf.RoundToInt(maxHunger)}\n" +
            milkStatus;
    }

    void Start()
    {
        UpdateDebugUI();
    }
}
