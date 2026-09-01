using UnityEngine;

[System.Serializable]
/// <summary>Data runtime sistem grid lama; dipertahankan untuk kompatibilitas scene prototipe.</summary>
public class TileData
{
    public TileState state = TileState.Empty;
    public CropInstance crop;
}

/* ---------- FIELD GRID ---------- */
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
/// <summary>
/// Implementasi grid farming prototipe. Scene baru sebaiknya memakai <see cref="FieldArea"/>,
/// dan komponen ini otomatis nonaktif jika keduanya berada pada object yang sama.
/// </summary>
public class FieldGrid : MonoBehaviour
{
    public static FieldGrid Instance;

    [Header("Grid")]
    public int gridSize = 10;               // grid selalu persegi

    [HideInInspector] public float cellSize;

    [Header("Prefabs")]
    public GameObject cropPrefab;
    public GameObject hoeMarkPrefab;

    TileData[,] tiles;

    Vector3 originWS, stepX, stepZ;

    /*================ INIT ================*/
    void Awake()
    {
        // FieldArea is the modular replacement. Keep this legacy component in
        // the scene for reference, but never let both systems run together.
        if (GetComponent<FieldArea>() != null)
        {
            enabled = false;
            return;
        }

        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        BuildGridData();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (GetComponent<FieldArea>() != null)
        {
            enabled = false;
            return;
        }

        if (!Application.isPlaying)
            BuildGridData();
    }
#endif

    /*================ BUILD ================*/
    void BuildGridData()
    {
        if (gridSize < 1) gridSize = 1;

        Bounds b = GetComponent<Collider>().bounds;
        float side = Mathf.Min(b.size.x, b.size.z);

        cellSize = side / gridSize;
        originWS = new Vector3(b.min.x, b.center.y, b.min.z);
        stepX = transform.right * cellSize;
        stepZ = transform.forward * cellSize;

        tiles = new TileData[gridSize, gridSize];
        for (int x = 0; x < gridSize; ++x)
            for (int z = 0; z < gridSize; ++z)
                tiles[x, z] = new TileData();
    }

    /*================ ACTIONS ================*/
    public bool Hoe(int gx, int gz)
    {
        if (!InBounds(gx, gz) || tiles[gx, gz].state != TileState.Empty) return false;

        tiles[gx, gz].state = TileState.Hoed;
        if (hoeMarkPrefab)
            Instantiate(hoeMarkPrefab, GridToWorld(gx, gz), Quaternion.identity, transform);
        return true;
    }

    public bool Plant(int gx, int gz)
    {
        if (!InBounds(gx, gz) || tiles[gx, gz].state != TileState.Hoed) return false;

        tiles[gx, gz].state = TileState.Planted;

        Vector3 pos = GridToWorld(gx, gz) + Vector3.up * 0.01f;
        GameObject g = Instantiate(cropPrefab, pos, Quaternion.identity, transform);
        tiles[gx, gz].crop = g.GetComponent<CropInstance>();
        return true;
    }

    public void ClearTile(int gx, int gz)
    {
        if (!InBounds(gx, gz)) return;
        tiles[gx, gz].state = TileState.Empty;
        tiles[gx, gz].crop = null;
    }

    /*================ CONVERSION ================*/
    public Vector3 GridToWorld(int gx, int gz)
    {
        return originWS + stepX * (gx + 0.5f) + stepZ * (gz + 0.5f);
    }

    public bool WorldToGrid(Vector3 worldPos, out int gx, out int gz)
    {
        Vector3 diff = worldPos - originWS;
        gx = Mathf.FloorToInt(Vector3.Dot(diff, transform.right) / cellSize);
        gz = Mathf.FloorToInt(Vector3.Dot(diff, transform.forward) / cellSize);
        return InBounds(gx, gz);
    }

    bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < gridSize && z < gridSize;

#if UNITY_EDITOR
    /*================ GIZMOS ================*/
    void OnDrawGizmos()
    {
        if (GetComponent<FieldArea>() != null) return;
        if (gridSize <= 0) return;
        if (stepX == Vector3.zero || stepZ == Vector3.zero) BuildGridData();

        Gizmos.color = Color.yellow;

        for (int x = 0; x <= gridSize; ++x)
        {
            Vector3 a = originWS + stepX * x;
            Vector3 b = a + stepZ * gridSize;
            Gizmos.DrawLine(a, b);
        }
        for (int z = 0; z <= gridSize; ++z)
        {
            Vector3 a = originWS + stepZ * z;
            Vector3 b = a + stepX * gridSize;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}
