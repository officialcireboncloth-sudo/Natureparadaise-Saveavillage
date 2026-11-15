using System;
using UnityEngine;

/// <summary>Moves through normal character collision to one captured work target, then executes once.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(1100)]
public sealed class PlayerToolApproach : MonoBehaviour
{
    /// <summary>Maximum visible working gap; individual actions may request a closer position.</summary>
    public const float MaximumWorkDistance=.75f;
    [SerializeField, Min(1f)] float maximumDistance=4f;
    [SerializeField, Min(1f)] float timeout=6f;
    [SerializeField, Min(.2f)] float blockedSeconds=.8f;
    static readonly RaycastHit[] lineHits=new RaycastHit[32];
    PlayerController movement;
    PlayerToolHotbar hotbar;
    InventoryHotbarUI inventoryHotbar;
    UnityEngine.Object owner;
    Func<Vector3> position;
    Func<bool> available;
    Action arrived;
    Action finished;
    Transform anchor;
    ItemSO selectedItem;
    PlayerToolType selectedTool;
    float reach,elapsed,stalled,bestDistance;
    bool faceOnArrival;
    /// <summary>True only while walking toward the captured work target.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Returns the single approach coordinator on this player, without scene setup.</summary>
    public static PlayerToolApproach For(Component player)
        => player.GetComponent<PlayerToolApproach>()??player.gameObject.AddComponent<PlayerToolApproach>();

    /// <summary>Checks actual horizontal work reach, floor height and unobstructed line to the target.</summary>
    public static bool InReach(Transform player,Vector3 point,float distance,Transform target=null)
    {
        Vector3 delta=point-player.position;
        if(Mathf.Abs(delta.y)>1.2f||new Vector2(delta.x,delta.z).sqrMagnitude>distance*distance)return false;
        Vector3 start=player.position+Vector3.up*.65f,end=point+Vector3.up*.25f;
        Vector3 ray=end-start;int count=Physics.RaycastNonAlloc(start,ray.normalized,lineHits,ray.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++){
            var shape=lineHits[i].collider;
            if(shape==null||shape.transform.IsChildOf(player)||shape.transform==player||
               target!=null&&(shape.transform==target||shape.transform.IsChildOf(target))||
               shape.bounds.max.y<=Mathf.Min(player.position.y,point.y)+.12f)continue;
            return false;
        }
        return true;
    }

    /// <summary>Captures one target; no item or stamina is spent until the arrival callback validates the action.</summary>
    public bool Begin(UnityEngine.Object actionOwner,Func<Vector3> targetPosition,float workReach,Action onArrival,Func<bool> isAvailable,Transform targetAnchor=null,bool faceTarget=true,Action onFinish=null)
    {
        if(IsActive)return owner==actionOwner;
        workReach=Mathf.Clamp(workReach,.05f,MaximumWorkDistance);
        movement=GetComponent<PlayerController>();hotbar=GetComponent<PlayerToolHotbar>();inventoryHotbar=GetComponent<InventoryHotbarUI>();
        if(movement==null||!movement.isActiveAndEnabled||movement.IsMovementLocked||movement.IsRiding||
           WorldInteractionPrompt.IsSuppressed||Time.timeScale<=0||TimeManager.Instance!=null&&TimeManager.Instance.IsPaused||
           actionOwner==null||targetPosition==null||onArrival==null||isAvailable!=null&&!isAvailable())return false;
        Vector3 point=targetPosition();var delta=point-transform.position;
        if(Mathf.Abs(delta.y)>1.2f||new Vector2(delta.x,delta.z).magnitude>maximumDistance){SaveLoadFeedback.Instance?.ShowMessage("Target terlalu jauh atau di lantai lain");return false;}
        if(InReach(transform,point,workReach,targetAnchor)){onFinish?.Invoke();if(faceTarget)movement.FaceTowardsInteraction(point);onArrival();return true;}
        owner=actionOwner;position=targetPosition;available=isAvailable;arrived=onArrival;finished=onFinish;anchor=targetAnchor;reach=Mathf.Max(.1f,workReach);
        selectedTool=hotbar!=null?hotbar.SelectedTool:PlayerToolType.None;selectedItem=inventoryHotbar!=null?inventoryHotbar.SelectedItem:null;
        elapsed=stalled=0;bestDistance=HorizontalDistance(point);faceOnArrival=faceTarget;IsActive=true;movement.AcquireMovementLock(this);
        return true;
    }
    float HorizontalDistance(Vector3 point){var delta=point-transform.position;delta.y=0;return delta.magnitude;}
    void Update()
    {
        if(!IsActive)return;
        if(owner==null||owner is Behaviour behaviour&&!behaviour.isActiveAndEnabled||movement==null||!movement.isActiveAndEnabled||
           movement.HasMovementLockOtherThan(this)||WorldInteractionPrompt.IsSuppressed||
           hotbar!=null&&hotbar.SelectedTool!=selectedTool||inventoryHotbar!=null&&inventoryHotbar.SelectedItem!=selectedItem||
           available!=null&&!available()||movement.ReadWorldMovementDirection().sqrMagnitude>.04f||
           GameplayInput.GetKeyDown(KeyCode.Escape)||GameplayInput.GetKeyDown(KeyCode.Space)){Cancel();return;}
        if(Time.timeScale<=0||TimeManager.Instance!=null&&TimeManager.Instance.IsPaused){movement.MoveForInteraction(this,Vector3.zero);return;}
        Vector3 point=position();float distance=HorizontalDistance(point);
        if(Mathf.Abs(point.y-transform.position.y)>1.2f||distance>maximumDistance){Cancel();return;}
        if(InReach(transform,point,reach,anchor)){
            var action=arrived;bool face=faceOnArrival;Cancel();if(face)movement.FaceTowardsInteraction(point);action();return;
        }
        float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;stalled+=dt;
        if(distance<bestDistance-.025f){bestDistance=distance;stalled=0;}
        if(elapsed>timeout||stalled>blockedSeconds){Cancel();SaveLoadFeedback.Instance?.ShowMessage("Jalan ke target terhalang");return;}
        Vector3 direction=point-transform.position;direction.y=0;direction.Normalize();movement.FaceTowardsInteraction(point);
        movement.MoveForInteraction(this,direction*Mathf.Min(movement.InteractionWalkSpeed*dt,Mathf.Max(.01f,distance-reach+.005f)));
    }
    /// <summary>Cancels only this pending approach, preserving locks held by other systems.</summary>
    public void Cancel(UnityEngine.Object actionOwner=null)
    {
        if(actionOwner!=null&&owner!=actionOwner)return;
        if(IsActive&&movement!=null){movement.MoveForInteraction(this,Vector3.zero);movement.ReleaseMovementLock(this);}
        var cleanup=finished;finished=null;IsActive=false;owner=null;position=null;available=null;arrived=null;anchor=null;selectedItem=null;cleanup?.Invoke();
    }
    void OnDisable()=>Cancel();
}
