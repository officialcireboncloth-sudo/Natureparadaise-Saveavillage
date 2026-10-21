using UnityEngine;

/// <summary>Deteksi interaksi satu tile di depan; collider fisik object tidak diubah.</summary>
public static class PlayerInteractionTarget
{
    static int inputFrame = -1;
    static readonly System.Collections.Generic.HashSet<KeyCode> UsedKeys = new();
    static PlayerController actionPlayer;

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
    [System.Obsolete("World interactions must use PressPrompt(owner, key) after validating their current range.", true)]
    public static bool Press(Transform player, Transform target, KeyCode key)
    {
        if (!GameplayInput.GetKeyDown(key) || !Contains(player, target)) return false;
        return ConsumeKey(key);
    }

    public static bool ContainsPickup(Transform player, Transform target, float radius)
    {
        if (player == null || target == null || WorldInteractionPrompt.IsSuppressed) return false;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null && controller.IsMovementLocked) return false;
        Vector3 delta = target.position - player.position;
        return Mathf.Abs(delta.y) <= 2f && delta.x * delta.x + delta.z * delta.z <= radius * radius;
    }

    /// <summary>Comparable horizontal distance for furniture and doors, including trigger interaction shapes.</summary>
    public static float SurfaceDistance(Transform player, Transform target)
    {
        if (player == null || target == null) return float.PositiveInfinity;
        Collider shape = target.GetComponent<Collider>();
        Vector3 point = shape != null && shape.enabled
            ? shape.ClosestPoint(player.position + Vector3.up * .75f) : target.position;
        Vector3 delta = point - player.position;
        return Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
    }

    [System.Obsolete("World interactions must use PressPrompt(owner, key) after validating their current range.", true)]
    public static bool PressPickup(Transform player, Transform target, KeyCode key, float radius)
    {
        return GameplayInput.GetKeyDown(key) && ContainsPickup(player, target, radius) && ConsumeKey(key);
    }

    /// <summary>Memakai satu tombol untuk interaksi tanpa target dunia, misalnya menurunkan hewan.</summary>
    static bool Press(KeyCode key)
    {
        return GameplayInput.GetKeyDown(key) && ConsumeKey(key);
    }

    /// <summary>Only the visible prompt may consume this press; range is checked by its owner.</summary>
    public static bool PressPrompt(Object owner, KeyCode key)
    {
        return WorldInteractionPrompt.IsDisplayedFor(owner) && CanReceiveInput(owner) && Press(key);
    }

    public static bool PressPrompt(Object owner, Transform anchor, KeyCode key)
        => WorldInteractionPrompt.IsDisplayedFor(owner, anchor) && CanReceiveInput(owner) && Press(key);

    public static bool PressAction(Object owner, KeyCode key)
        => WorldInteractionPrompt.AllowsAction(owner) && CanReceiveInput(owner) && Press(key);

    static bool CanReceiveInput(Object owner)
    {
        if (actionPlayer == null) actionPlayer = Object.FindFirstObjectByType<PlayerController>();
        // An active session may use its own keys (e.g. dismount), never another session's lock.
        return actionPlayer == null || !actionPlayer.HasMovementLockOtherThan(owner);
    }

    /// <summary>
    /// True jika tombol sudah dipakai interaksi dunia pada frame ini. Sistem tool
    /// memakainya agar satu tekanan F tidak ikut mencangkul/menebang setelah mengambil item.
    /// </summary>
    public static bool WasConsumed(KeyCode key) => inputFrame==Time.frameCount && UsedKeys.Contains(key);

    static bool ConsumeKey(KeyCode key)
    {
        if (inputFrame != Time.frameCount) { inputFrame = Time.frameCount; UsedKeys.Clear(); }
        return UsedKeys.Add(key);
    }
}
