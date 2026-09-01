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
        if (distance > interactionRadius)
            return;
        string action = exitsInterior ? "Keluar Rumah" : "Masuk Rumah";
        WorldInteractionPrompt.Request(this, transform, $"{interactKey}: {action}", distance, promptHeight);
        if (!Input.GetKeyDown(interactKey))
            return;
        if (exitsInterior)
            SceneTransitionManager.Instance.ReturnToWorld(exteriorSpawnId);
        else
            SceneTransitionManager.Instance.EnterInterior(interiorSceneName, targetSpawnId);
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
