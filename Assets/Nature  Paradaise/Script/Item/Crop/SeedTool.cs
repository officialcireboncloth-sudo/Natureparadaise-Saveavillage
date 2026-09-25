using System.Collections;
using UnityEngine;

/// <summary>
/// Menanam definisi crop pada tile field di depan player setelah memvalidasi tool aktif,
/// inventory seed, status tile, dan biaya stamina.
/// </summary>
public class SeedTool : MonoBehaviour
{
    [Header("Animation Impact Timing")]
    [SerializeField, Min(0f)] float plantImpactDelay = 0.62f;
    [SerializeField, Min(0f)] float plantActionDuration = 0.95f;

    [Header("Raycast")]
    [Tooltip("Layer tanah/field yang bisa ditanami.")]
    public LayerMask fieldMask;

    [Header("Raycast Distance")]
    public float maxDistance = 100f;

    [Header("Inventory")]
    public Inventory playerInv;

    [Header("Player Stamina")]
    public PlayerStatusSystem playerStatus;
    [Min(0f)] public float plantingCost = 0.05f;

    [Header("Legacy Fallback")]
    [Tooltip("Dipertahankan untuk scene lama. Seed baru harus mengisi Seed Crop pada ItemSO.")]
    [SerializeField, HideInInspector] ItemSO seedItem;
    [SerializeField, HideInInspector] CropDataSO cropDefinition;

    PlayerToolHotbar hotbar;
    InventoryHotbarUI inventoryHotbar;
    FarmingTool farmingTool;
    PlayerController movement;
    bool actionBusy;

    CropDataSO ResolveCrop(ItemSO seed) => seed != null
        ? seed.seedCrop != null ? seed.seedCrop : seed == seedItem ? cropDefinition : null
        : null;

    /// <summary>Outline memakai bibit, stamina, dan aturan tile yang sama dengan aksi tanam.</summary>
    public bool CanPlantAt(FieldArea field, int x, int z)
    {
        if (inventoryHotbar == null) inventoryHotbar = GetComponent<InventoryHotbarUI>();
        ItemSO seed = inventoryHotbar != null ? inventoryHotbar.SelectedItem : null;
        return seed != null && seed.IsSeed && playerInv != null && playerInv.GetCount(seed) > 0 &&
            (playerStatus == null || playerStatus.CanSpendStamina(plantingCost)) &&
            field != null && field.CanPlant(x, z, ResolveCrop(seed));
    }

    void Awake()
    {
        // Kalau belum di-assign di Inspector,
        // coba cari Inventory otomatis.
        if (playerInv == null)
            playerInv = FindFirstObjectByType<Inventory>();
        if (playerStatus == null && playerInv != null)
            playerStatus = playerInv.GetComponent<PlayerStatusSystem>();
        hotbar = GetComponent<PlayerToolHotbar>();
        if (hotbar == null)
            hotbar = gameObject.AddComponent<PlayerToolHotbar>();
        inventoryHotbar = GetComponent<InventoryHotbarUI>();
        farmingTool = GetComponent<FarmingTool>();
        movement = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (actionBusy || (movement != null && movement.IsMovementLocked))
            return;

        if (inventoryHotbar == null)
            inventoryHotbar = GetComponent<InventoryHotbarUI>();
        ItemSO selectedSeed = inventoryHotbar != null ? inventoryHotbar.SelectedItem : null;
        if (selectedSeed == null || !selectedSeed.IsSeed ||
            !hotbar.IsUsePressed(PlayerToolType.Seed))
            return;

        // Pastikan Inventory tersedia
        if (playerInv == null)
        {
            ShowPlantFeedback("GAGAL MENANAM: Inventory player tidak ditemukan.");
            return;
        }

        CropDataSO selectedCrop = ResolveCrop(selectedSeed);
        if (selectedCrop == null)
        {
            ShowPlantFeedback($"GAGAL MENANAM: Seed Crop untuk {selectedSeed.itemName} belum dipasang.");
            return;
        }

        // Cek apakah player punya Seed
        int seedCount = playerInv.GetCount(selectedSeed);

        if (seedCount <= 0)
        {
            ShowPlantFeedback("GAGAL MENANAM: Bibit habis. Beli atau ambil Seed dahulu.");
            return;
        }

        if (playerStatus != null && !playerStatus.CanSpendStamina(plantingCost))
        {
            ShowPlantFeedback("GAGAL MENANAM: Stamina tidak cukup.");
            return;
        }

        if (!TryGetTarget(out FieldArea field, out int gx, out int gz))
        {
            ShowPlantFeedback("GAGAL MENANAM: Tidak ada tile di depan player.");
            return;
        }

        if (field.TryGetSnapshot(gx, gz, out FieldTileSnapshot snapshot) && snapshot.SoilDurability <= 0)
        {
            ShowPlantFeedback("GAGAL MENANAM: Tanah tandus. Istirahatkan atau gunakan Fertilizer.");
            return;
        }

        if (!field.CanPlant(gx, gz, selectedCrop))
        {
            string reason = snapshot.State switch
            {
                TileState.Empty => "Tanah belum dicangkul. Gunakan Hoe dahulu.",
                TileState.Planted => "Tile ini sudah memiliki tanaman.",
                TileState.Hoed => "Crop belum siap atau tanah tidak dapat ditanami.",
                _ => "Tile dipakai bangunan, jalan, atau object lain."
            };
            ShowPlantFeedback($"GAGAL MENANAM [{gx},{gz}]: {reason}");

            return;
        }

        // Kalau planting BERHASIL,
        // baru kurangi Seed dari inventory.
        bool removed = playerInv.Remove(selectedSeed, 1);

        if (!removed)
        {
            // Ini seharusnya tidak terjadi karena kita sudah
            // mengecek GetCount() sebelumnya.
            Debug.LogError(
                "[SEED] Plant berhasil tetapi Seed gagal dikurangi!"
            );
            ShowPlantFeedback("PERINGATAN: Bibit tertanam, tetapi inventory gagal diperbarui.");

            return;
        }

        movement?.PlayPlantingAnimation();
        StartCoroutine(PlantingRoutine(field, gx, gz, selectedSeed, selectedCrop));
    }

    IEnumerator PlantingRoutine(FieldArea field, int gx, int gz, ItemSO selectedSeed, CropDataSO selectedCrop)
    {
        actionBusy = true;
        movement?.AcquireMovementLock(this);
        playerStatus?.AcquireActivity(this, PlayerMovementState.ToolAction);
        if (plantImpactDelay > 0f) yield return new WaitForSeconds(plantImpactDelay);

        // Perubahan tile dan pengurangan seed terjadi ketika tangan mencapai tanah.
        bool planted = playerInv != null && playerInv.GetCount(selectedSeed) > 0 &&
            field != null && field.TryPlant(gx, gz, selectedCrop);
        if (!planted)
        {
            ShowPlantFeedback("GAGAL MENANAM: Tile atau bibit berubah sebelum tangan mencapai tanah.");
            FinishPlantingAction();
            yield break;
        }

        playerStatus?.TrySpendStamina(plantingCost);
        playerStatus?.PulseActivity(PlayerMovementState.ToolAction);
        string cropName = selectedCrop.produceItem != null
            ? selectedCrop.produceItem.itemName
            : "Bibit";
        ShowPlantFeedback(
            $"BERHASIL MENANAM [{gx},{gz}]: Bibit {cropName} sudah tertanam. Tekan V untuk menyiram."
        );

        Debug.Log(
            $"[SEED] Berhasil menanam di ({gx}, {gz}). " +
            $"{selectedSeed.itemName} tersisa: {playerInv.GetCount(selectedSeed)}"
        );

        float remaining = Mathf.Max(0f, plantActionDuration - plantImpactDelay);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        FinishPlantingAction();
    }

    void FinishPlantingAction()
    {
        movement?.ReleaseMovementLock(this);
        playerStatus?.ReleaseActivity(this);
        actionBusy = false;
    }

    void OnDisable()
    {
        FinishPlantingAction();
    }

    void ShowPlantFeedback(string message)
    {
        SaveLoadFeedback.Instance?.ShowMessage(message);
        Debug.Log($"[SEED] {message}");
    }

    bool TryGetTarget(out FieldArea field, out int x, out int z)
    {
        // Saat target directional tersedia, jangan diam-diam beralih ke tile di bawah mouse.
        if (farmingTool != null)
            return farmingTool.TryGetCurrentTile(out field, out x, out z);

        field = null;
        x = -1;
        z = -1;
        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, fieldMask))
            return false;
        field = hit.collider.GetComponentInParent<FieldArea>();
        if (field == null && !FieldArea.TryGetAt(hit.point, out field, out _, out _))
            return false;
        return field.WorldToGrid(hit.point, out x, out z);
    }
}
