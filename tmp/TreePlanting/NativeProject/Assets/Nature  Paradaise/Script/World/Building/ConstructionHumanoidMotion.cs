using System;
using UnityEngine;

/// <summary>Player-skinned temporary builder. Shared Humanoid clips plus contact fallback until builder clips exist.</summary>
public sealed class ConstructionHumanoidMotion : IDisposable
{
    readonly Transform worker, proxy;
    readonly Animator animator;
    readonly ConstructionBuilderMotion targets;
    readonly HumanoidActionPlayer player;
    readonly ConstructionWorkerSettings settings;
    float previousTime,contactWeight;
    public string ActionId=>player.ActionId;
    public bool HasClip(string id)=>player.HasClip(id);
    public ConstructionHumanoidMotion(Transform root,Animator character,ConstructionWorkerSettings profile)
    {
        worker=root;animator=character;settings=profile;
        player=new HumanoidActionPlayer(character,profile.humanoidActions);
        proxy=new GameObject("Builder_WorkTargets_NoMesh").transform;proxy.SetParent(worker,false);
        foreach(string name in new[]{"Body","Belt","Head","HatBrim","HatCrown","LegL","LegR","BootL","BootR",
            "UpperArmL","UpperArmR","ForearmL","ForearmR","HandL","HandR"})
        {var t=new GameObject(name).transform;t.SetParent(proxy,false);}
        // Reuse the exact bench/board/tool routine; only the skeleton implementation is replaced.
        targets=new ConstructionBuilderMotion(worker,proxy,profile);
    }
    public void BindBoard(Transform board)=>targets.BindBoard(board);
    public void Animate(bool walking,bool working,float time)
    {
        float delta=Mathf.Clamp(time-previousTime,0,.1f);previousTime=time;
        targets.Animate(walking,working,time);
        string action=!working?(walking?"Walk":"Idle"):targets.ActionName switch
        {
            "Pick up board"=>"PickUpBoard","Hold board"=>"CarryBoard","Place board"=>"PlaceBoard",
            "Hammer"=>"Hammer","Saw"=>"Saw",_=>"Inspect"
        };
        player.Play(action);
        player.Evaluate(delta,Mathf.Max(.1f,settings.walkingSpeed/1.6f));
        contactWeight=Mathf.MoveTowards(contactWeight,working?1:0,delta*6);
        if(contactWeight>.001f)
        {
            // Specific authored builder clips take over the pose. Unfilled slots use contact-driven arms,
            // never pretend Hammering Rock is an authored workbench/saw animation.
            if(!player.HasClip(action))
            {
                var spine=animator.GetBoneTransform(HumanBodyBones.Spine);
                if(spine!=null)spine.rotation=Quaternion.Slerp(spine.rotation,Quaternion.AngleAxis(10,worker.right)*spine.rotation,contactWeight);
                Arm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,proxy.Find("HandL").position,-1);
                Arm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,proxy.Find("HandR").position,1);
            }
        }
        // The held tools follow the actual humanoid hands after pose evaluation (no detached hammer).
        var right=animator.GetBoneTransform(HumanBodyBones.RightHand);
        foreach(string name in new[]{"WorkerHammer","WorkerSaw"})
        {
            var tool=worker.Find(name);if(tool!=null&&tool.gameObject.activeSelf&&right!=null)tool.position=right.position;
        }
    }
    void Arm(HumanBodyBones upperId,HumanBodyBones lowerId,HumanBodyBones handId,Vector3 target,int side)
    {
        var upper=animator.GetBoneTransform(upperId);var lower=animator.GetBoneTransform(lowerId);var hand=animator.GetBoneTransform(handId);
        if(upper==null||lower==null||hand==null)return;
        float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
        Vector3 destination=Vector3.Lerp(hand.position,target,contactWeight),delta=destination-upper.position;
        float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 direction=delta.normalized;
        Vector3 pole=worker.right*side-worker.up*.5f-worker.forward*.35f;
        Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
        float along=(a*a-b*b+d*d)/(2*d);
        Vector3 elbow=upper.position+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
        Vector3 end=upper.position+direction*d;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
    }
    public void Dispose(){player.Dispose();}
}
