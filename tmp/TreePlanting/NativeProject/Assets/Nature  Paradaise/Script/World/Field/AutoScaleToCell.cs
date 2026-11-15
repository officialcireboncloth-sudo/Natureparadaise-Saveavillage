using UnityEngine;

/// <summary>Menyesuaikan ukuran visual child terhadap ukuran cell field saat scene dimulai.</summary>
public class AutoScaleToCell : MonoBehaviour
{
    void Start()
    {
        FieldArea area = GetComponentInParent<FieldArea>();
        float sourceCellSize = area != null
            ? area.CellSize
            : FieldGrid.Instance != null ? FieldGrid.Instance.cellSize : 1f;
        float s = sourceCellSize * 0.7f;
        transform.localScale = new Vector3(s, 0.05f, s);
    }
}
