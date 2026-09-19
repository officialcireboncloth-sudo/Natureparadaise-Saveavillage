using System.Collections.Generic;
using UnityEngine;

/// <summary>Mekanik mengangkat dan menurunkan ternak kecil tanpa mengubah ownership kandang.</summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimalCarry : MonoBehaviour
{
    [SerializeField] KeyCode interactionKey = KeyCode.E;
    [SerializeField] Transform carryAnchor;
    [SerializeField] Vector3 carryLocalPosition = new(0f, 2.6f, 0.55f);
    [SerializeField, Min(0.5f)] float dropDistance = 1.35f;

    readonly Dictionary<Collider, bool> colliderStates = new();
    AnimalGrowthSystem carriedAnimal;
    AnimalRoutine carriedRoutine;
    Transform originalParent;
    Rigidbody carriedBody;
    bool routineWasEnabled;
    bool bodyWasKinematic;
    bool bodyUsedGravity;
    int pickedUpFrame = -1;
    PlayerController movement;

    public bool HasAnimal => carriedAnimal != null;
    public AnimalGrowthSystem CarriedAnimal => carriedAnimal;
    public bool IsCarrying(AnimalGrowthSystem animal) => carriedAnimal != null && carriedAnimal == animal;

    void Awake()
    {
        movement = GetComponent<PlayerController>();
        EnsureAnchor();
    }

    void OnEnable() => TimeManager.OnBeforeDayChange += DropBeforeDailyReset;
    void OnDisable()
    {
        TimeManager.OnBeforeDayChange -= DropBeforeDailyReset;
        if (carriedAnimal != null) Drop();
    }

    void Update()
    {
        if (carriedAnimal == null || Time.frameCount <= pickedUpFrame || WorldInteractionPrompt.IsSuppressed)
            return;

        WorldInteractionPrompt.Request(
            this,
            transform,
            $"{interactionKey}: Turunkan {carriedAnimal.AnimalName}",
            0f,
            2.15f
        );
        if (PlayerInteractionTarget.Press(interactionKey)) Drop();
    }

    public bool TryPickup(AnimalGrowthSystem animal)
    {
        if (animal == null || carriedAnimal != null || !animal.HasBeenBorn ||
            !AnimalGrowthProfileSO.IsBird(animal.Type)) return false;
        PlayerGatheringTool gathering = GetComponent<PlayerGatheringTool>();
        if (gathering != null && gathering.IsCarrying) return false;
        EnsureAnchor();
        carriedAnimal = animal;
        pickedUpFrame = Time.frameCount;
        originalParent = animal.transform.parent;
        carriedRoutine = animal.GetComponent<AnimalRoutine>();
        routineWasEnabled = carriedRoutine != null && carriedRoutine.enabled;
        if (carriedRoutine != null) carriedRoutine.enabled = false;

        colliderStates.Clear();
        foreach (Collider collider in animal.GetComponentsInChildren<Collider>(true))
        {
            colliderStates[collider] = collider.enabled;
            collider.enabled = false;
        }

        carriedBody = animal.GetComponent<Rigidbody>();
        if (carriedBody != null)
        {
            bodyWasKinematic = carriedBody.isKinematic;
            bodyUsedGravity = carriedBody.useGravity;
            carriedBody.isKinematic = true;
            carriedBody.useGravity = false;
            carriedBody.linearVelocity = Vector3.zero;
            carriedBody.angularVelocity = Vector3.zero;
        }

        animal.transform.SetParent(carryAnchor, false);
        animal.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        movement?.SetCarrying(true);
        SaveLoadFeedback.Instance?.ShowMessage($"Mengangkat {animal.AnimalName} — E untuk menurunkan");
        return true;
    }

    public bool Drop()
    {
        if (carriedAnimal == null) return false;
        AnimalGrowthSystem animal = carriedAnimal;
        Vector3 facing = movement != null ? movement.FacingDirection : transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = transform.forward;
        Vector3 target = transform.position + facing.normalized * dropDistance;
        if (AnimalWalkingPath.Ground(target, animal.transform, out Vector3 ground)) target = ground;

        animal.transform.SetParent(originalParent, true);
        animal.transform.position = target;
        // AnimalRoutine dapat mengatur ulang mode Rigidbody saat aktif. Pulihkan routine
        // terlebih dahulu, lalu kembalikan state physics persis seperti sebelum diangkat.
        if (carriedRoutine != null) carriedRoutine.enabled = routineWasEnabled;
        if (carriedBody != null)
        {
            carriedBody.position = target;
            carriedBody.isKinematic = bodyWasKinematic;
            carriedBody.useGravity = bodyUsedGravity;
        }
        foreach (KeyValuePair<Collider, bool> pair in colliderStates)
            if (pair.Key != null) pair.Key.enabled = pair.Value;

        colliderStates.Clear();
        carriedAnimal = null;
        carriedRoutine = null;
        carriedBody = null;
        originalParent = null;
        movement?.SetCarrying(false);
        SaveLoadFeedback.Instance?.ShowMessage($"Menurunkan {animal.AnimalName}");
        return true;
    }

    void EnsureAnchor()
    {
        if (carryAnchor != null) return;
        Transform existing = transform.Find("CarriedAnimalAnchor");
        if (existing != null) carryAnchor = existing;
        else
        {
            GameObject anchor = new("CarriedAnimalAnchor");
            carryAnchor = anchor.transform;
            carryAnchor.SetParent(transform, false);
        }
        carryAnchor.localPosition = carryLocalPosition;
        carryAnchor.localRotation = Quaternion.identity;
    }

    void DropBeforeDailyReset()
    {
        if (carriedAnimal != null) Drop();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstalled()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerAnimalCarry>() == null)
            player.gameObject.AddComponent<PlayerAnimalCarry>();
    }
}
