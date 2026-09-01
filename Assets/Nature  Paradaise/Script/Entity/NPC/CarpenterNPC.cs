using UnityEngine;

/// <summary>
/// Pintu masuk construction milik player. Prototype UI menggunakan prompt dunia ringan;
/// menu visual final dapat memanggil API PlayerHouseController yang sama.
/// </summary>
[DisallowMultipleComponent]
public sealed class CarpenterNPC : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] KeyCode confirmHouseUpgradeKey = KeyCode.C;
    [SerializeField] KeyCode propertyInformationKey = KeyCode.B;
    [SerializeField] KeyCode closeKey = KeyCode.Escape;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.5f;
    [SerializeField, Min(0f)] float promptHeight = 1.8f;

    Inventory playerInventory;
    PlayerController movement;
    TimeManager timeManager;
    bool menuOpen;

    void Awake()
    {
        playerInventory = FindFirstObjectByType<Inventory>();
        movement = playerInventory != null ? playerInventory.GetComponent<PlayerController>() : null;
        timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
    }

    void Update()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<Inventory>();
            movement = playerInventory != null ? playerInventory.GetComponent<PlayerController>() : null;
        }
        if (playerInventory == null)
            return;

        float distance = Vector3.Distance(transform.position, playerInventory.transform.position);
        if (distance > interactionRadius)
        {
            if (menuOpen)
                CloseMenu();
            return;
        }

        if (!menuOpen)
        {
            WorldInteractionPrompt.Request(this, transform, $"{interactKey}: Carpenter", distance, promptHeight);
            if (Input.GetKeyDown(interactKey))
                OpenMenu();
            return;
        }

        DrawMenuPrompt(distance);
        if (Input.GetKeyDown(closeKey) || Input.GetKeyDown(interactKey))
            CloseMenu();
        else if (Input.GetKeyDown(confirmHouseUpgradeKey))
            ConfirmHouseUpgrade();
        else if (Input.GetKeyDown(propertyInformationKey))
            SaveLoadFeedback.Instance?.ShowMessage("Pilih Property Site kosong untuk menentukan lokasi bangunan farm");
    }

    void DrawMenuPrompt(float distance)
    {
        PlayerHouseController house = PlayerHouseController.Instance;
        string houseLine;
        if (house == null)
            houseLine = "<color=#FF6673>Player House belum dikonfigurasi</color>";
        else if (house.IsUnderConstruction)
            houseLine = $"House Lv.{house.CurrentLevel} → Lv.{house.PendingLevel}\nSelesai hari ke-{house.CompletionDay}";
        else if (!house.HasNextLevel)
            houseLine = $"House Lv.{house.CurrentLevel} — MAX LEVEL";
        else
            houseLine = $"House Lv.{house.CurrentLevel} → Lv.{house.NextLevel.level}\n{house.BuildNextLevelRequirements()}";

        WorldInteractionPrompt.Request(
            this,
            transform,
            $"CARPENTER\n{houseLine}\n{confirmHouseUpgradeKey}: Upgrade House  {propertyInformationKey}: Farm Building  {closeKey}: Tutup",
            distance,
            promptHeight);
    }

    void OpenMenu()
    {
        menuOpen = true;
        movement?.AcquireMovementLock(this);
        if (timeManager == null)
            timeManager = TimeManager.Instance;
        timeManager?.AcquirePause(this);
    }

    void CloseMenu()
    {
        menuOpen = false;
        movement?.ReleaseMovementLock(this);
        timeManager?.ReleasePause(this);
    }

    void ConfirmHouseUpgrade()
    {
        PlayerHouseController house = PlayerHouseController.Instance;
        if (house == null)
        {
            SaveLoadFeedback.Instance?.ShowMessage("Player House belum dikonfigurasi");
            return;
        }
        if (house.TryStartNextUpgrade(out string reason))
        {
            CloseMenu();
            return;
        }
        SaveLoadFeedback.Instance?.ShowMessage(reason ?? "Upgrade rumah tidak tersedia");
    }

    void OnDisable()
    {
        if (menuOpen)
            CloseMenu();
    }
}
