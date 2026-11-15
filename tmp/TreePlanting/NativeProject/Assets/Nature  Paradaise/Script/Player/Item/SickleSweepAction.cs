using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One bounded player action drives the wide physical sweep, blade trail and ordered contacts.</summary>
[DefaultExecutionOrder(1200),DisallowMultipleComponent]
public sealed class SickleSweepAction : MonoBehaviour
{
    readonly List<SickleCutTarget> targets=new(32);
    readonly List<Transform> bones=new(16);
    readonly List<Vector3> savedPositions=new(16);
    readonly List<Quaternion> savedRotations=new(16);
    PlayerGatheringTool owner;
    PlayerController movement;
    PlayerHeldTools held;
    SickleSweepProfile profile;
    SickleSweepRegion region;
    Transform hips,spine,arm,elbow,hand,leftArm,leftElbow,leftHand,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot;
    Animator rig;
    SickleBladeTrail trail;
    Vector3 palmOffset,carryBlade;
    float elapsed,pausedSpeed;
    int next,cut;
    bool active,posed,clockPaused,swish;
    public bool IsActive=>active;
    public float Elapsed=>elapsed;
    public SickleSweepRegion Region=>region;
    public Vector3 BladePosition=>held!=null?held.WorkPoint(PlayerToolType.Sickle):transform.position;
    static bool Paused=>Time.timeScale<=0||(TimeManager.Instance!=null&&TimeManager.Instance.IsPaused);

    /// <summary>Captures facing and candidates once; no target behind the player enters the action.</summary>
    public void Begin(PlayerGatheringTool tool,SickleSweepProfile settings,SickleSweepRegion sector)
    {
        Cancel();owner=tool;profile=settings;region=sector;movement=GetComponent<PlayerController>();held=GetComponent<PlayerHeldTools>();rig=movement!=null?movement.CharacterAnimator:GetComponentInChildren<Animator>();
        ResolveBones();targets.Clear();next=cut=0;elapsed=0;swish=false;
        foreach(var target in WorldGatherable.Active)if(target!=null&&target.CanSickle&&region.Contains(target.transform.position,out float angle))targets.Add(new SickleCutTarget(target,target.transform.position,angle));
        FieldArea.CollectSickleTargets(region,targets);TerrainDetailGrassManager.CollectSickleTargets(region,targets);
        targets.Sort((a,b)=>b.angle.CompareTo(a.angle));carryBlade=BladePosition;active=true;
        held?.BeginWorkAction(PlayerToolType.Sickle,region.origin+region.forward*region.radius,profile.Duration);
        if(trail==null){var child=new GameObject("SickleBladeTrail");child.transform.SetParent(transform,false);trail=child.AddComponent<SickleBladeTrail>();}
        trail.Configure(Resources.Load<FarmActionEffectsCatalog>("Effects/FarmActionEffects")?.particleMaterial,profile.trailWidth,profile.trailSeconds);
    }
    void ResolveBones()
    {
        Transform Find(string name)=>GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name||t.name=="mixamorig:"+name);
        hips=Find("Hips");spine=Find("Spine");hand=Find("RightHand");leftHand=Find("LeftHand");
        if(hand!=null){elbow=hand.parent;arm=elbow?.parent;}if(leftHand!=null){leftElbow=leftHand.parent;leftArm=leftElbow?.parent;}
        leftFoot=Find("LeftFoot");rightFoot=Find("RightFoot");if(leftFoot!=null){leftShin=leftFoot.parent;leftThigh=leftShin?.parent;}if(rightFoot!=null){rightShin=rightFoot.parent;rightThigh=rightShin?.parent;}
        bones.Clear();foreach(var bone in new[]{hips,spine,arm,elbow,hand,leftArm,leftElbow,leftHand,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot})if(bone!=null&&!bones.Contains(bone))bones.Add(bone);
        savedPositions.Clear();savedRotations.Clear();foreach(var bone in bones){savedPositions.Add(bone.localPosition);savedRotations.Add(bone.localRotation);}
        if(hand!=null){var fingers=hand.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("HandIndex1")||t.name.EndsWith("HandMiddle1")||t.name.EndsWith("HandRing1")).ToArray();palmOffset=fingers.Length>0?Quaternion.Inverse(hand.rotation)*(Vector3.Lerp(hand.position,fingers.Aggregate(Vector3.zero,(p,t)=>p+t.position)/fingers.Length,.75f)-hand.position):Vector3.zero;}
    }
    // Undo only our last correction before the Animator evaluates. This also prevents pose accumulation while paused.
    void Update()=>RestorePose();
    void RestorePose(){if(!posed)return;for(int i=0;i<bones.Count;i++)if(bones[i]!=null){bones[i].localPosition=savedPositions[i];bones[i].localRotation=savedRotations[i];}posed=false;}
    void LateUpdate()
    {
        if(!active)return;
        if(owner==null||!owner.isActiveAndEnabled){Cancel();return;}
        bool pause=Paused;if(rig!=null&&pause!=clockPaused){if(pause){pausedSpeed=rig.speed;rig.speed=0;}else rig.speed=pausedSpeed;}clockPaused=pause;
        if(!pause)elapsed+=Time.deltaTime;
        float sweep=Mathf.Clamp01((elapsed-profile.windup)/profile.sweep);
        float angle=elapsed<profile.windup?Mathf.Lerp(108,region.halfAngle,Mathf.SmoothStep(0,1,elapsed/profile.windup)):elapsed<profile.windup+profile.sweep?Mathf.Lerp(region.halfAngle,-region.halfAngle,Mathf.SmoothStep(0,1,sweep)):Mathf.Lerp(-region.halfAngle,-100,Mathf.Clamp01((elapsed-profile.windup-profile.sweep)/profile.followThrough));
        float weight=elapsed<profile.windup?Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.2f)):1-Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-profile.windup-profile.sweep)/profile.followThrough));
        ApplyPose(angle,weight);
        if(pause)return;
        bool sweeping=elapsed>=profile.windup&&elapsed<=profile.windup+profile.sweep;
        if(sweeping&&!swish){swish=true;owner.PlaySickleSwish(BladePosition);}
        trail.Sample(BladePosition,elapsed,sweeping);
        if(elapsed>=profile.windup){float passed=elapsed>=profile.windup+profile.sweep?-region.halfAngle:angle;while(next<targets.Count&&targets[next].angle>=passed-.001f){if(targets[next].Cut(owner,region))cut++;next++;}TerrainDetailGrassManager.FlushSickleChanges();}
        if(elapsed>=profile.Duration){var completed=owner;int count=cut;Cancel(false);completed.FinishSickleSweep(count,true);}
    }
    void ApplyPose(float angle,float weight)
    {
        if(hand==null||arm==null||held==null||held.ActiveVisual==null)return;
        for(int i=0;i<bones.Count;i++){savedPositions[i]=bones[i].localPosition;savedRotations[i]=bones[i].localRotation;}posed=true;
        Vector3 leftPosition=leftFoot!=null?leftFoot.position:Vector3.zero,rightPosition=rightFoot!=null?rightFoot.position:Vector3.zero;Quaternion leftRotation=leftFoot!=null?leftFoot.rotation:Quaternion.identity,rightRotation=rightFoot!=null?rightFoot.rotation:Quaternion.identity;
        Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*region.forward;
        float yaw=profile.torsoTurn*Mathf.Clamp(angle/region.halfAngle,-1,1)*weight;
        if(hips!=null){hips.position+=(direction*profile.weightShift-Vector3.up*profile.crouch)*weight;hips.rotation=Quaternion.AngleAxis(yaw*.22f,Vector3.up)*hips.rotation;}
        if(spine!=null){spine.rotation=Quaternion.AngleAxis(yaw*.78f,Vector3.up)*spine.rotation;spine.rotation=Quaternion.AngleAxis(profile.lean*weight,Vector3.Cross(Vector3.up,direction))*spine.rotation;}
        if(leftFoot!=null)SolveArm(leftThigh,leftShin,leftFoot,leftPosition,leftRotation);if(rightFoot!=null)SolveArm(rightThigh,rightShin,rightFoot,rightPosition,rightRotation);
        var visual=held.ActiveVisual.transform;var entry=held.Catalog.Find(PlayerToolType.Sickle);var anchor=visual.Find("ToolImpact");if(entry==null||anchor==null)return;
        Vector3 goal=region.Point(angle);var terrain=FieldGroundSurface.TerrainAt(region.origin);if(terrain!=null&&Mathf.Abs(FieldGroundSurface.GroundPosition(region.origin).y-region.origin.y)<.3f)goal=FieldGroundSurface.GroundPosition(goal);goal.y+=profile.cuttingHeight;
        if(elapsed<.2f)goal=Vector3.Lerp(carryBlade,goal,weight);
        // The pack blade bows toward local +Y, so its concave cutting edge faces -Y.
        // A right-to-left sweep must lead with that inner edge rather than the convex spine.
        // Keep the blade in the radial/vertical plane even at close range, where shoulder
        // offset otherwise twists the inner edge away from the sweep's tangent.
        Vector3 shoulderToGoal=goal-arm.position;
        Vector3 shaft=(direction*Vector3.Dot(shoulderToGoal,direction)+Vector3.up*shoulderToGoal.y).normalized;
        var rotation=Quaternion.LookRotation(shaft,Vector3.Cross(Vector3.up,direction));
        Vector3 grip=goal-rotation*(anchor.localPosition*entry.scale);var wrist=rotation*Quaternion.Inverse(Quaternion.Euler(entry.handEuler));
        Vector3 target=grip-wrist*(palmOffset+entry.handPosition);
        SolveArm(arm,elbow,hand,Vector3.Lerp(hand.position,target,weight),Quaternion.Slerp(hand.rotation,wrist,weight));
        held.SnapToGrip(visual,entry,false);visual.rotation=Quaternion.Slerp(visual.rotation,rotation,weight);
        visual.position=hand.position+hand.rotation*(palmOffset+entry.handPosition);
    }
    static void SolveArm(Transform shoulder,Transform elbow,Transform hand,Vector3 target,Quaternion wrist)
    {
        if(shoulder==null||elbow==null||hand==null)return;
        Vector3 a=shoulder.position,b=elbow.position,c=hand.position;float upper=Vector3.Distance(a,b),lower=Vector3.Distance(b,c);Vector3 delta=target-a;if(delta.sqrMagnitude<.000001f)return;
        float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);Vector3 axis=delta.normalized,pole=Vector3.ProjectOnPlane(b-a,axis);if(pole.sqrMagnitude<.000001f)pole=Vector3.ProjectOnPlane(shoulder.forward,axis);pole.Normalize();float along=(upper*upper-lower*lower+distance*distance)/(2*distance);Vector3 bend=a+axis*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));shoulder.rotation=Quaternion.FromToRotation(b-a,bend-a)*shoulder.rotation;elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;hand.rotation=wrist;
    }
    /// <summary>Cancels remaining contacts and returns bones, trail and owned animation pause to normal.</summary>
    public void Cancel(bool notifyOwner=true){bool wasActive=active;RestorePose();if(clockPaused&&rig!=null)rig.speed=pausedSpeed;clockPaused=false;active=false;if(trail!=null)trail.Clear();held?.EndWorkAction();targets.Clear();if(wasActive&&notifyOwner&&owner!=null)owner.FinishSickleSweep(cut,false);}
    void OnDisable()=>Cancel();
}
