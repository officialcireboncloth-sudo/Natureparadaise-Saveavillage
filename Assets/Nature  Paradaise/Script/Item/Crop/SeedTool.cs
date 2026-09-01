using UnityEngine;

/// <summary>
/// Menanam definisi crop pada tile field di depan player setelah memvalidasi tool aktif,
/// inventory seed, status tile, dan biaya stamina.
/// </summary>
public class SeedTool : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layer tanah/field yang bisa ditanami.")]
    public LayerMask fieldMask;

    [Header("Input")]
    public KeyCode plantKey = KeyCode.B;

    [Header("Raycast Distance")]
    public float maxDistance = 100f;

    [Header("Inventory")]
    public Inventory playerInv;

    [Header("Player Stamina")]
    public PlayerStatusSystem playerStatus;
    [Min(0f)] public float plantingCost = 1f;

    [Tooltip("ItemSO Seed yang digunakan untuk menanam.")]
    public ItemSO seedItem;

    [Tooltip("Definisi crop yang ditanam oleh seed ini.")]
    public CropDataSO cropDefinition;

    PlayerToolHotbar hotbar;
    FarmingTool farmingTool;
    PlayerController movement;

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
        farmingTool = GetComponent<FarmingTool>();
        movement = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (movement != null && movement.IsMovementLocked)
            return;

        if (!Input.GetKeyDown(plantKey) && !hotbar.IsUsePressed(PlayerToolType.Seed))
            return;

        // Pastikan Inventory tersedia
        if (playerInv == null)
        {
            Debug.LogWarning(
                "[SEED] Player Inventory belum ditemukan."
            );
            return;
        }

        // Pastikan Seed Item sudah di-assign
        if (seedItem == null)
        {
            Debug.LogWarning(
                "[SEED] Seed ItemSO belum di-assign di Inspector."
            );
            return;
        }

        // Cek apakah player punya Seed
        int seedCount = playerInv.GetCount(seedItem);

        if (seedCount <= 0)
        {
            Debug.Log("[SEED] Tidak punya Seed. Tidak bisa menanam.");
            return;
        }

        if (playerStatus != null && !playerStatus.CanSpendStamina(plantingCost))
        {
            Debug.Log("[PLAYER] Stamina tidak cukup untuk menanam.");
            return;
        }

        if (!TryGetTarget(out FieldArea field, out int gx, out int gz))
            return;

        // Coba tanam terlebih dahulu
        bool planted = field.TryPlant(gx, gz, cropDefinition);

        if (!planted)
        {
            Debug.Log(
                $"[SEED] Tidak bisa menanam di ({gx}, {gz}). " +
                "Tanah belum dicangkul atau sudah terisi."
            );

            return;
        }

        // Kalau planting BERHASIL,
        // baru kurangi Seed dari inventory.
        bool removed = playerInv.Remove(seedItem, 1);

        if (!removed)
        {
            // Ini seharusnya tidak terjadi karena kita sudah
            // mengecek GetCount() sebelumnya.
            Debug.LogError(
                "[SEED] Plant berhasil tetapi Seed gagal dikurangi!"
            );

            return;
        }

        playerStatus?.TrySpendStamina(plantingCost);

        Debug.Log(
            $"[SEED] Berhasil menanam di ({gx}, {gz}). " +
            $"Seed tersisa: {playerInv.GetCount(seedItem)}"
        );
    }

    bool TryGetTarget(out FieldArea field, out int x, out int z)
    {
        if (farmingTool != null && farmingTool.TryGetCurrentTile(out field, out x, out z))
            return true;

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
