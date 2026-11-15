using UnityEngine;

/// <summary>Blends into an existing seated bed pose; active only during the short sleep transition.</summary>
[DefaultExecutionOrder(1400),DisallowMultipleComponent]
public sealed class PlayerBedSitPose : MonoBehaviour
{
    [SerializeField,Range(0,1),Tooltip("Seated moment in Wake Up Bed. The original animation asset is sampled once and remains unchanged.")]
    float seatedMoment=.8f;
    struct Pose
    {
        public Vector3 position,scale;public Quaternion rotation;
        public Pose(Transform bone){position=bone.localPosition;rotation=bone.localRotation;scale=bone.localScale;}
    }
    Animator rig;
    Transform visual,hips;
    Transform[] bones;
    Pose[] from,to;
    Vector3 visualPosition,startHips,targetHips;
    Quaternion visualRotation;
    bool wasEnabled,active;
    float elapsed,duration;
    public bool IsSeated=>active&&elapsed>=duration;

    /// <summary>Captures the current pose and blends the pelvis onto the mattress edge.</summary>
    public bool Begin(Animator animator,Vector3 seat,float seconds)
    {
        End();
        if(animator==null||animator.runtimeAnimatorController==null)return false;
        AnimationClip clip=null;
        foreach(var candidate in animator.runtimeAnimatorController.animationClips)
            if(candidate.name.Replace(" ","").Replace("_","").ToLowerInvariant().Contains("wakeupbed")){clip=candidate;break;}
        if(clip==null)return false;
        rig=animator;bones=rig.GetComponentsInChildren<Transform>();
        foreach(var bone in bones)if(bone.name is "mixamorig:Hips" or "Hips"){hips=bone;break;}
        if(hips==null){rig=null;return false;}
        visual=rig.transform;while(visual.parent!=null&&visual.parent!=transform)visual=visual.parent;
        visualPosition=visual.localPosition;visualRotation=visual.localRotation;
        wasEnabled=rig.enabled;startHips=hips.position;targetHips=seat;
        from=new Pose[bones.Length];to=new Pose[bones.Length];
        for(int i=0;i<bones.Length;i++)from[i]=new Pose(bones[i]);
        rig.enabled=false;
        clip.SampleAnimation(rig.gameObject,clip.length*seatedMoment);
        for(int i=0;i<bones.Length;i++)to[i]=new Pose(bones[i]);
        active=true;elapsed=0;duration=Mathf.Max(.01f,seconds);Apply(0);
        return true;
    }
    void LateUpdate()
    {
        if(!active)return;
        if(rig==null||hips==null){End();return;}
        elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);
        Apply(Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));
    }
    void Apply(float weight)
    {
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        for(int i=0;i<bones.Length;i++)if(bones[i]!=null)
        {
            bones[i].localPosition=Vector3.Lerp(from[i].position,to[i].position,weight);
            bones[i].localRotation=Quaternion.Slerp(from[i].rotation,to[i].rotation,weight);
            bones[i].localScale=Vector3.Lerp(from[i].scale,to[i].scale,weight);
        }
        visual.position+=Vector3.Lerp(startHips,targetHips,weight)-hips.position;
    }
    /// <summary>Restores normal animation and the original visual offset before standing on the floor.</summary>
    public void End()
    {
        if(!active)return;
        active=false;
        for(int i=0;i<bones.Length;i++)if(bones[i]!=null)
        {bones[i].localPosition=from[i].position;bones[i].localRotation=from[i].rotation;bones[i].localScale=from[i].scale;}
        if(visual!=null){visual.localPosition=visualPosition;visual.localRotation=visualRotation;}
        if(rig!=null){rig.enabled=wasEnabled;if(wasEnabled){rig.Rebind();rig.Update(0);}}
        rig=null;hips=null;visual=null;bones=null;from=to=null;
    }
    void OnDisable()=>End();
}
