using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mode dorong untuk ternak mamalia. Player tetap memakai movement normal, sedangkan
/// perpindahan majunya diteruskan ke hewan selama tombol E ditahan.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController),typeof(CharacterController))]
public sealed class PlayerAnimalPush : MonoBehaviour
{
    [SerializeField] KeyCode pushKey=KeyCode.E;
    [SerializeField,Min(1f)] float breakDistance=3f;
    [SerializeField,Min(0.1f)] float maximumPushSpeed=2.2f;
    [SerializeField,Min(90f)] float directionTurnSpeed=540f;
    [SerializeField,Min(0.1f)] float playerFollowSpeed=7.5f;
    [SerializeField,Min(0f)] float obstacleSkin=0.04f;

    readonly List<Collider> animalColliders=new();
    readonly List<bool> previousCollisionIgnores=new();
    PlayerController movement;
    PlayerAnimalCarry animalCarry;
    CharacterController playerCollider;
    AnimalGrowthSystem target;
    AnimalRoutine targetRoutine;
    Vector3 pushDirection;
    Vector3 pushInputDirection;
    float contactDistance;
    bool pushActive;
    float inputGraceUntil;

    public bool HasAnimal=>pushActive && target!=null;
    public bool IsPushing(AnimalGrowthSystem animal)=>pushActive && target!=null && target==animal;

    void Awake()
    {
        movement=GetComponent<PlayerController>();
        animalCarry=GetComponent<PlayerAnimalCarry>();
        playerCollider=GetComponent<CharacterController>();
    }

    public bool TryBeginPush(AnimalGrowthSystem animal)
    {
        if(animal==null || pushActive || !animal.HasBeenBorn ||
           AnimalGrowthProfileSO.IsBird(animal.Type) || movement==null ||
           movement.IsMovementLocked || movement.IsCarrying ||
           (animalCarry!=null && animalCarry.HasAnimal)) return false;

        AnimalRoutine routine=animal.GetComponent<AnimalRoutine>();
        if(routine!=null && !routine.BeginPlayerPush(transform)) return false;

        Vector3 direction=animal.transform.position-transform.position;
        direction.y=0f;
        if(direction.sqrMagnitude<0.001f)
            direction=transform.forward;
        pushDirection=direction.normalized;
        contactDistance=Mathf.Clamp(direction.magnitude,0.55f,Mathf.Min(1.6f,breakDistance*0.65f));
        target=animal;
        targetRoutine=routine;
        pushActive=true;
        inputGraceUntil=Time.unscaledTime+0.35f;
        CacheAndIgnoreAnimalCollision();
        movement.AcquireMovementLock(this);
        movement.FaceTowardsInteraction(animal.transform.position);
        movement.SetPushingAnimation(true);
        return true;
    }

    void Update()
    {
        if(!pushActive) return;
        if(target==null)
        {
            StopPushing();
            return;
        }
        Vector3 delta=target.transform.position-transform.position;
        delta.y=0f;
        pushInputDirection=movement!=null?movement.ReadWorldMovementDirection():Vector3.zero;
        bool hasPushControl=Time.unscaledTime<=inputGraceUntil || GameplayInput.GetKey(pushKey) ||
                            pushInputDirection.sqrMagnitude>0.001f;
        if(!hasPushControl || delta.sqrMagnitude>breakDistance*breakDistance ||
           movement==null || movement.HasMovementLockOtherThan(this) || movement.IsCarrying ||
           (animalCarry!=null && animalCarry.HasAnimal) ||
           !target.gameObject.activeInHierarchy)
            StopPushing();
    }

    void LateUpdate()
    {
        if(!pushActive || target==null) return;
        float pushIntent=Mathf.Clamp01(pushInputDirection.magnitude);
        if(pushIntent>0.001f)
        {
            Vector3 desiredDirection=pushInputDirection/pushIntent;
            desiredDirection.y=0f;
            if(desiredDirection.sqrMagnitude>0.001f)
                pushDirection=Vector3.RotateTowards(pushDirection,desiredDirection.normalized,
                    directionTurnSpeed*Mathf.Deg2Rad*Time.deltaTime,1f).normalized;
            Vector3 pushDelta=pushDirection*(maximumPushSpeed*pushIntent*Time.deltaTime);
            if(!AnimalMoveBlocked(pushDelta))
            {
                if(targetRoutine!=null) targetRoutine.TryMoveByPlayer(pushDelta);
                else TryMoveWithoutRoutine(pushDelta);
            }
        }

        if(target!=null)
        {
            // Player mengikuti titik kontak di belakang arah dorong. Ini memungkinkan
            // sapi dipindah ke segala arah tanpa tetap terkunci pada garis awal.
            Vector3 desiredPlayerPosition=target.transform.position-pushDirection*contactDistance;
            Vector3 followDelta=desiredPlayerPosition-transform.position;
            followDelta.y=0f;
            followDelta=Vector3.ClampMagnitude(followDelta,playerFollowSpeed*Time.deltaTime);
            if(followDelta.sqrMagnitude>0.000001f && playerCollider!=null && playerCollider.enabled)
                playerCollider.Move(followDelta);
            movement.FaceTowardsInteraction(target.transform.position);
            Vector3 facing=pushDirection;
            if(facing.sqrMagnitude>0.001f)
                target.transform.rotation=Quaternion.Slerp(target.transform.rotation,
                    Quaternion.LookRotation(facing,Vector3.up),Time.deltaTime*7f);
        }
    }

    public void CancelIfTarget(AnimalGrowthSystem animal)
    {
        if(IsPushing(animal)) StopPushing();
    }

    public void StopPushing()
    {
        if(!pushActive) return;
        RestoreAnimalCollision();
        if(targetRoutine!=null) targetRoutine.EndPlayerPush();
        movement?.ReleaseMovementLock(this);
        movement?.SetPushingAnimation(false);
        pushActive=false;
        target=null;
        targetRoutine=null;
        pushInputDirection=Vector3.zero;
        contactDistance=0f;
        inputGraceUntil=0f;
        animalColliders.Clear();
        previousCollisionIgnores.Clear();
    }

    void CacheAndIgnoreAnimalCollision()
    {
        animalColliders.Clear();
        previousCollisionIgnores.Clear();
        if(target==null || playerCollider==null) return;
        foreach(Collider candidate in target.GetComponentsInChildren<Collider>(true))
        {
            if(candidate==null || !candidate.enabled || candidate.isTrigger) continue;
            animalColliders.Add(candidate);
            previousCollisionIgnores.Add(Physics.GetIgnoreCollision(playerCollider,candidate));
            Physics.IgnoreCollision(playerCollider,candidate,true);
        }
    }

    void RestoreAnimalCollision()
    {
        if(playerCollider==null) return;
        for(int i=0;i<animalColliders.Count;i++)
            if(animalColliders[i]!=null)
                Physics.IgnoreCollision(playerCollider,animalColliders[i],previousCollisionIgnores[i]);
    }

    bool AnimalMoveBlocked(Vector3 delta)
    {
        if(animalColliders.Count==0 || delta.sqrMagnitude<0.000001f) return false;
        Bounds bounds=animalColliders[0].bounds;
        for(int i=1;i<animalColliders.Count;i++) bounds.Encapsulate(animalColliders[i].bounds);
        float radius=Mathf.Max(0.12f,Mathf.Min(bounds.extents.x,bounds.extents.z)*0.8f);
        float verticalExtent=Mathf.Max(radius,bounds.extents.y);
        Vector3 bottom=bounds.center+Vector3.down*Mathf.Max(0f,verticalExtent-radius-obstacleSkin);
        Vector3 top=bounds.center+Vector3.up*Mathf.Max(0f,verticalExtent-radius-obstacleSkin);
        RaycastHit[] hits=Physics.CapsuleCastAll(bottom,top,radius,delta.normalized,
            delta.magnitude+obstacleSkin,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        foreach(RaycastHit hit in hits)
        {
            Collider collider=hit.collider;
            if(collider==null || collider.transform.IsChildOf(target.transform) ||
               collider.transform.IsChildOf(transform)) continue;
            // Permukaan tanah/lereng bukan obstacle horizontal. Ground snapping di
            // AnimalWalkingPath tetap memvalidasi kemiringan dan beda ketinggiannya.
            if(collider is TerrainCollider || hit.normal.y>=0.65f) continue;
            return true;
        }
        return false;
    }

    bool TryMoveWithoutRoutine(Vector3 delta)
    {
        Vector3 candidate=target.transform.position+delta;
        if(!AnimalWalkingPath.Ground(candidate,target.transform,transform,out Vector3 grounded,false)) return false;
        target.transform.position=grounded;
        return true;
    }

    void OnDisable()=>StopPushing();
}
