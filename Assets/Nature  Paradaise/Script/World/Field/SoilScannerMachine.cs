using UnityEngine;

/// <summary>Authored station; presentation can be replaced without changing field/save ownership.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class SoilScannerMachine : MonoBehaviour
{
    [SerializeField, Min(.25f)] float interactionRange = 1.25f;
    [SerializeField] Transform interactionAnchor;
    Inventory nearbyInventory;
    public Transform InteractionAnchor => interactionAnchor != null ? interactionAnchor : transform;
    public bool IsOpen => SoilScannerUI.Instance != null && SoilScannerUI.Instance.Machine == this;

    void Update()
    {
        if (IsOpen) return;
        if (nearbyInventory == null) nearbyInventory = FindFirstObjectByType<PlayerController>()?.GetComponent<Inventory>();
        if (!CanInteract(nearbyInventory)) return;
        float distance = PlayerInteractionTarget.SurfaceDistance(nearbyInventory.transform, transform);
        WorldInteractionPrompt.Request(this, InteractionAnchor, "E: Gunakan Pemindai Tanah", distance, .8f,
            () => Open(nearbyInventory));
        if (PlayerInteractionTarget.PressPrompt(this, KeyCode.E)) Open(nearbyInventory);
    }

    public bool CanInteract(Inventory source)
    {
        if (!isActiveAndEnabled || source == null || WorldInteractionPrompt.IsSuppressed || LoadingScreen.IsVisible) return false;
        if (SceneTransitionManager.Instance != null &&
            (SceneTransitionManager.Instance.IsInsideInterior || SceneTransitionManager.Instance.IsTransitioning)) return false;
        var actor = source.GetComponent<PlayerController>();
        return actor != null && !actor.IsMovementLocked && !actor.IsRiding &&
            Mathf.Abs(source.transform.position.y - transform.position.y) < 2f &&
            PlayerInteractionTarget.SurfaceDistance(source.transform, transform) <= interactionRange;
    }

    public bool Open(Inventory source)
    {
        if (SoilScannerUI.Instance != null || !CanInteract(source)) return false;
        source.GetComponent<PlayerController>().FaceTowardsInteraction(transform.position);
        return SoilScannerUI.Show(this, source);
    }

    void OnDisable()
    {
        if (IsOpen) SoilScannerUI.Instance.Close();
    }

    void OnValidate() => interactionRange = Mathf.Max(.25f, interactionRange);
}
