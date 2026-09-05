using UnityEngine;

/// <summary>Deteksi interaksi satu tile di depan; collider fisik object tidak diubah.</summary>
public static class PlayerInteractionTarget
{
    static int inputFrame = -1;
    static readonly System.Collections.Generic.HashSet<KeyCode> UsedKeys = new();

    public static bool Contains(Transform player, Transform target)
    {
        if (player == null || target == null) return false;
        PlayerController controller = player.GetComponent<PlayerController>();
        if ((controller != null && controller.IsMovementLocked) || WorldInteractionPrompt.IsSuppressed) return false;
        FarmingTool farming = player.GetComponent<FarmingTool>();
        Vector3 center;
        Vector3 right = Vector3.right;
        Vector3 forward = Vector3.forward;
        float size = 3f;
        if (farming != null && farming.TryGetCurrentTile(out FieldArea field, out int x, out int z))
        {
            center = field.GridToWorld(x, z);
            size = field.CellSize;
            right = field.transform.right;
            forward = field.transform.forward;
        }
        else
        {
            // Di luar farm/interior, gunakan ukuran field terdekat sebagai satuan tile.
            float closest = float.PositiveInfinity;
            foreach (FieldArea area in FieldArea.ActiveAreas)
            {
                float distance = (area.transform.position - player.position).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                size = area.CellSize;
            }
            Vector3 facing = controller != null ? controller.FacingDirection : player.forward;
            Vector3 direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.z)
                ? Vector3.right * (facing.x >= 0 ? 1 : -1) : Vector3.forward * (facing.z >= 0 ? 1 : -1);
            center = player.position + direction * size;
        }
        // Object besar dinilai dari permukaan collider, bukan pivot di tengah bangunan.
        Collider collider = target.GetComponent<Collider>();
        Vector3 point = collider != null && collider.enabled && !collider.isTrigger
            ? collider.ClosestPoint(center + Vector3.up * 0.5f) : target.position;
        Vector3 delta = point - center;
        return Mathf.Abs(Vector3.Dot(delta, right)) <= size * 0.5f &&
               Mathf.Abs(Vector3.Dot(delta, forward)) <= size * 0.5f && Mathf.Abs(delta.y) <= 1.5f;
    }

    /// <summary>Satu penekanan tombol tidak menjalankan beberapa interaksi sekaligus.</summary>
    public static bool Press(Transform player, Transform target, KeyCode key)
    {
        if (!Input.GetKeyDown(key) || !Contains(player, target)) return false;
        if (inputFrame != Time.frameCount) { inputFrame = Time.frameCount; UsedKeys.Clear(); }
        return UsedKeys.Add(key);
    }
}
