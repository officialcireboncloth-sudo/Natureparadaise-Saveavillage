using UnityEngine;

/// <summary>
/// Contoh very-simple crop:
///   • Saat ditanam, mulai dari skala kecil + warna hijau.
///   • Setelah 'growTime' detik otomatis matang (skala besar + warna kuning).
///   • Jika matang, dapat dipanen (item masuk ke Inventory).
/// </summary>
[RequireComponent(typeof(Collider))]            // wajib ada collider utk raycast
public class CropInstance : MonoBehaviour
{
    [Header("Hasil Panen")]
    public ItemSO produceItem;                  // apa yang akan dimasukkan ke inventory

    [Header("Waktu Tumbuh (detik)")]
    public float growTime = 5f;                 // berapa detik sampai matang

    [Header("Skala & Warna")]
    public Vector3 seedScale = new(0.4f, 0.4f, 0.4f);
    public Vector3 matureScale = new(1.0f, 1.2f, 1.0f);
    public Color seedColor = Color.green;
    public Color matureColor = Color.yellow;

    bool mature;                              // sudah siap panen?
    float timer;
    Renderer rend;

    public bool IsMature => mature;             // public getter

    /*================= LIFE CYCLE =================*/
    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        // skala & warna bibit
        ApplyVisual(seedScale, seedColor);

        // pastikan posY rata tanah
        Vector3 p = transform.position;
        p.y = FieldGrid.Instance.transform.position.y;
        transform.position = p;
    }

    void Update()
    {
        if (mature) return;

        timer += Time.deltaTime;
        if (timer >= growTime)          // waktunya matang
        {
            mature = true;
            ApplyVisual(matureScale, matureColor);
        }
    }

    void ApplyVisual(Vector3 scale, Color color)
    {
        transform.localScale = scale * FieldGrid.Instance.cellSize;

        if (rend != null)
        {
            if (rend.material.HasProperty("_Color"))
                rend.material.color = color;
        }
    }

    /*================= HARVEST =================*/
    /// <summary>
    /// Coba panen tanaman. True bila sukses (tanaman dihancurkan & item masuk).
    /// </summary>
    public bool Harvest(Inventory inv)
    {
        if (!mature)
        {
            Debug.Log("DEBUG Harvest: belum matang");
            return false;
        }

        if (inv == null)
        {
            Debug.LogWarning("DEBUG Harvest: Inventory NULL – lupa drag Inventory ke FarmingTool?");
            return false;
        }
        if (produceItem == null)
        {
            Debug.LogWarning("DEBUG Harvest: produceItem belum di-assign!");
            return false;
        }

        inv.Add(produceItem, 1);
        if (FieldGrid.Instance.WorldToGrid(transform.position, out int gx, out int gz))
            FieldGrid.Instance.ClearTile(gx, gz);

        Destroy(gameObject);
        return true;
    }
}