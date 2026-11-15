using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// Mendaftarkan titik spawn berdasarkan ID stabil agar lifecycle dapat memindahkan player
/// ke rumah, klinik, atau lokasi lain tanpa referensi scene langsung.
/// </summary>
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    static readonly Dictionary<string, PlayerSpawnPoint> Points = new Dictionary<string, PlayerSpawnPoint>();

    [SerializeField] string spawnId = "player-home";

    public string SpawnId => spawnId;

    void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(spawnId))
            Points[spawnId] = this;
    }

    void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(spawnId) && Points.TryGetValue(spawnId, out PlayerSpawnPoint point) && point == this)
            Points.Remove(spawnId);
    }

    /// <summary>Mencari spawn aktif berdasarkan ID stabil.</summary>
    public static bool TryGet(string id, out PlayerSpawnPoint point)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            point = null;
            return false;
        }

        return Points.TryGetValue(id, out point) && point != null;
    }

    /// <summary>Mengatur stable ID dari setup editor sebelum object disimpan ke scene.</summary>
    public void Configure(string id)
    {
        if (isActiveAndEnabled && !string.IsNullOrWhiteSpace(spawnId))
            Points.Remove(spawnId);
        spawnId = id;
        if (isActiveAndEnabled && !string.IsNullOrWhiteSpace(spawnId))
            Points[spawnId] = this;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = spawnId == "village-clinic" ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.45f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.25f);
    }
}
