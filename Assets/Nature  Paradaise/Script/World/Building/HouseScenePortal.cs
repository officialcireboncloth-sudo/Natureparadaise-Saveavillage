using UnityEngine;

/// <summary>Pintu world/interior yang memanggil SceneTransitionManager tanpa menyimpan state gameplay.</summary>
[DisallowMultipleComponent]
public sealed class HouseScenePortal : MonoBehaviour
{
    [SerializeField] bool exitsInterior;
    [SerializeField] string interiorSceneName = "HouseInterior";
    [SerializeField] string targetSpawnId = "house-interior-entry";
    [SerializeField] string exteriorSpawnId = "player-house-exit";
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.5f)] float interactionRadius = 1.8f;
    [SerializeField, Min(0f)] float promptHeight = 1.4f;

    Transform player;

    void Update()
    {
        if (SceneTransitionManager.Instance == null || SceneTransitionManager.Instance.IsTransitioning)
            return;
        if (player == null)
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            player = controller != null ? controller.transform : null;
        }
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (!CanInteract(player))
            return;
        string action = exitsInterior ? "Keluar Rumah" : "Masuk Rumah";
        WorldInteractionPrompt.Request(this, transform, $"{interactKey}: {action}", distance, promptHeight);
        if (!Input.GetKeyDown(interactKey) || !PlayerInteractionTarget.Press(interactKey))
            return;
        if (exitsInterior && WeatherSystem.Instance != null &&
            WeatherSystem.BlocksLeavingHome(WeatherSystem.Instance.CurrentWeather))
        {
            SaveLoadFeedback.Instance?.ShowMessage("Angin topan terlalu berbahaya. Kamu harus tetap di rumah.");
            return;
        }
        if (exitsInterior)
            SceneTransitionManager.Instance.ReturnToWorld(exteriorSpawnId);
        else
            SceneTransitionManager.Instance.EnterInterior(interiorSceneName, targetSpawnId);
    }

    /// <summary>Use the door volume, including triggers, so scaled door pivots do not block interaction.</summary>
    public bool CanInteract(Transform candidate)
    {
        if (candidate == null || WorldInteractionPrompt.IsSuppressed) return false;
        PlayerController controller = candidate.GetComponent<PlayerController>();
        if (controller != null && controller.IsMovementLocked) return false;
        CharacterController capsule = candidate.GetComponent<CharacterController>();
        Vector3 center = capsule != null && capsule.enabled
            ? capsule.bounds.center : candidate.position + Vector3.up;
        Collider door = GetComponent<Collider>();
        Vector3 point = door != null && door.enabled ? door.ClosestPoint(center) : transform.position;
        Vector3 delta = point - center;
        return delta.x * delta.x + delta.z * delta.z <= interactionRadius * interactionRadius &&
               Mathf.Abs(delta.y) <= 1.5f;
    }

    /// <summary>Konfigurasi portal dari setup editor agar field tetap private di runtime.</summary>
    public void Configure(bool isExit, string sceneName, string targetId, string exteriorId)
    {
        exitsInterior = isExit;
        interiorSceneName = sceneName;
        targetSpawnId = targetId;
        exteriorSpawnId = exteriorId;
    }
}
