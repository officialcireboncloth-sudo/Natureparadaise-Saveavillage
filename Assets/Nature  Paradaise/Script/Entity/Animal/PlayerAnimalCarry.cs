using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Mekanik mengangkat dan menurunkan ternak kecil tanpa mengubah ownership kandang.</summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimalCarry : MonoBehaviour
{
    [SerializeField] KeyCode interactionKey = KeyCode.E;
    [SerializeField] Transform carryAnchor;
    [SerializeField] Vector3 carryLocalPosition = new(0f, 1.8f, 0.42f);
    [SerializeField, Tooltip("Offset pusat visual unggas dari titik tengah kedua tangan.")]
    Vector3 handVisualOffset = new(0f, -0.18f, 0.08f);
    [SerializeField, Min(0.5f)] float dropDistance = 1.35f;
    [Header("Animation Timing")]
    [SerializeField, Min(0f)] float pickupImpactDelay = 0.62f;
    [SerializeField, Min(0f)] float pickupActionDuration = 1.2f;
    [SerializeField, Min(0f)] float placeImpactDelay = 0.62f;
    [SerializeField, Min(0f)] float placeActionDuration = 1.2f;

    readonly Dictionary<Collider, bool> colliderStates = new();
    AnimalGrowthSystem carriedAnimal;
    AnimalGrowthSystem pendingAnimal;
    AnimalRoutine carriedRoutine;
    Transform originalParent;
    Rigidbody carriedBody;
    bool routineWasEnabled;
    bool bodyWasKinematic;
    bool bodyUsedGravity;
    int pickedUpFrame = -1;
    PlayerController movement;
    bool actionBusy;
    Transform leftHand;
    Transform rightHand;

    public bool HasAnimal => carriedAnimal != null || pendingAnimal != null;
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
        StopAllCoroutines();
        TimeManager.OnBeforeDayChange -= DropBeforeDailyReset;
        if (carriedAnimal != null) DropImmediate();
        pendingAnimal=null;
        actionBusy=false;
        movement?.ReleaseMovementLock(this);
        movement?.SetCarryingAnimalAnimation(false);
        GetComponent<HeldItemPlacementSystem>()?.SetAnimalCarrySuppressed(false);
    }

    void Update()
    {
        if (actionBusy || carriedAnimal == null || Time.frameCount <= pickedUpFrame || WorldInteractionPrompt.IsSuppressed)
            return;

        WorldInteractionPrompt.RequestClean(
            this,
            transform,
            $"{interactionKey}: Turunkan {LocalizedAnimalName(carriedAnimal)}",
            0f,
            3.35f
        );
        if (PlayerInteractionTarget.Press(interactionKey)) Drop();
    }

    void LateUpdate()
    {
        if(carriedAnimal==null) return;
        EnsureAnchor();
        UpdateCarryAnchor();
    }

    public bool TryPickup(AnimalGrowthSystem animal)
    {
        if (animal == null || HasAnimal || actionBusy || !animal.HasBeenBorn ||
            !AnimalGrowthProfileSO.IsBird(animal.Type)) return false;
        PlayerGatheringTool gathering = GetComponent<PlayerGatheringTool>();
        if (gathering != null && gathering.IsCarrying) return false;
        pendingAnimal=animal;
        actionBusy=true;
        movement?.AcquireMovementLock(this);
        Vector3 towardAnimal=animal.transform.position-transform.position;
        towardAnimal.y=0f;
        if(towardAnimal.sqrMagnitude>0.001f)
            transform.rotation=Quaternion.LookRotation(towardAnimal.normalized,Vector3.up);
        movement?.SetCarryingAnimalAnimation(false);
        movement?.PlayPickUpChickenAnimation();
        GetComponent<HeldItemPlacementSystem>()?.SetAnimalCarrySuppressed(true);
        StartCoroutine(PickupRoutine());
        return true;
    }

    IEnumerator PickupRoutine()
    {
        if(pickupImpactDelay>0f) yield return new WaitForSeconds(pickupImpactDelay);
        AnimalGrowthSystem animal=pendingAnimal;
        pendingAnimal=null;
        if(animal==null)
        {
            FinishPickupAction(false);
            yield break;
        }

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
        UpdateCarryAnchor();
        SaveLoadFeedback.Instance?.ShowMessage($"Mengangkat {LocalizedAnimalName(animal)} — E untuk menurunkan");
        float remaining=Mathf.Max(0f,pickupActionDuration-pickupImpactDelay);
        if(remaining>0f) yield return new WaitForSeconds(remaining);
        movement?.SetCarryingAnimalAnimation(true);
        FinishPickupAction(true);
    }

    void FinishPickupAction(bool carrying)
    {
        movement?.ReleaseMovementLock(this);
        actionBusy=false;
        if(!carrying)
        {
            movement?.SetCarryingAnimalAnimation(false);
            GetComponent<HeldItemPlacementSystem>()?.SetAnimalCarrySuppressed(false);
        }
    }

    public bool Drop()
    {
        if (carriedAnimal == null || actionBusy) return false;
        actionBusy=true;
        movement?.AcquireMovementLock(this);
        movement?.SetCarryingAnimalAnimation(false);
        movement?.PlayPlaceChickenAnimation();
        StartCoroutine(DropRoutine());
        return true;
    }

    IEnumerator DropRoutine()
    {
        if(placeImpactDelay>0f) yield return new WaitForSeconds(placeImpactDelay);
        AnimalGrowthSystem animal=DetachCarriedAnimal();
        if(animal!=null)
            SaveLoadFeedback.Instance?.ShowMessage($"Menurunkan {LocalizedAnimalName(animal)}");
        float remaining=Mathf.Max(0f,placeActionDuration-placeImpactDelay);
        if(remaining>0f) yield return new WaitForSeconds(remaining);
        actionBusy=false;
        movement?.ReleaseMovementLock(this);
        GetComponent<HeldItemPlacementSystem>()?.SetAnimalCarrySuppressed(false);
    }

    AnimalGrowthSystem DetachCarriedAnimal()
    {
        if (carriedAnimal == null) return null;
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
        return animal;
    }

    void DropImmediate()
    {
        movement?.SetCarryingAnimalAnimation(false);
        DetachCarriedAnimal();
        actionBusy=false;
        movement?.ReleaseMovementLock(this);
        GetComponent<HeldItemPlacementSystem>()?.SetAnimalCarrySuppressed(false);
    }

    void EnsureAnchor()
    {
        if(leftHand==null) leftHand=FindRigBone(transform,"mixamorig:LeftHand");
        if(rightHand==null) rightHand=FindRigBone(transform,"mixamorig:RightHand");
        if (carryAnchor == null)
        {
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
    }

    void UpdateCarryAnchor()
    {
        if(carryAnchor==null || carriedAnimal==null) return;
        if(leftHand==null || rightHand==null)
        {
            carryAnchor.localPosition=carryLocalPosition;
            carryAnchor.localRotation=Quaternion.identity;
            return;
        }

        Vector3 desiredCenter=(leftHand.position+rightHand.position)*0.5f+
            transform.right*handVisualOffset.x+transform.up*handVisualOffset.y+
            transform.forward*handVisualOffset.z;
        Vector3 forward=transform.forward;
        forward.y=0f;
        if(forward.sqrMagnitude<0.001f) forward=Vector3.forward;
        carryAnchor.SetPositionAndRotation(desiredCenter,Quaternion.LookRotation(forward.normalized,Vector3.up));

        Renderer[] renderers=carriedAnimal.GetComponentsInChildren<Renderer>(true);
        bool hasBounds=false;
        Bounds visualBounds=default;
        foreach(Renderer renderer in renderers)
        {
            if(renderer==null || !renderer.gameObject.activeInHierarchy || !renderer.enabled) continue;
            if(!hasBounds) { visualBounds=renderer.bounds; hasBounds=true; }
            else visualBounds.Encapsulate(renderer.bounds);
        }
        if(hasBounds) carryAnchor.position+=desiredCenter-visualBounds.center;
    }

    static Transform FindRigBone(Transform root,string boneName)
    {
        if(root==null) return null;
        Transform[] children=root.GetComponentsInChildren<Transform>(true);
        foreach(Transform child in children)
            if(child.name==boneName) return child;
        return null;
    }

    static string LocalizedAnimalName(AnimalGrowthSystem animal) => animal==null?"hewan":animal.Type switch
    {
        AnimalType.Chicken=>"Ayam",
        AnimalType.Duck=>"Bebek",
        AnimalType.Goat=>"Kambing",
        AnimalType.Sheep=>"Domba",
        AnimalType.Cow=>"Sapi",
        _=>animal.AnimalName
    };

    void DropBeforeDailyReset()
    {
        if (carriedAnimal != null) DropImmediate();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstalled()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerAnimalCarry>() == null)
            player.gameObject.AddComponent<PlayerAnimalCarry>();
    }
}
