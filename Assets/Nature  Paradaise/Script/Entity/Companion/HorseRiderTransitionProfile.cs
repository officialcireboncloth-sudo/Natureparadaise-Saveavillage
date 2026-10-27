using UnityEngine;

/// <summary>Body trajectories paired with the saved horse Actions in the complete Player_Farming Blender master.</summary>
[CreateAssetMenu(menuName="Nature Paradise/Animals/Horse Rider Transition Profile")]
public sealed class HorseRiderTransitionProfile : ScriptableObject
{
    public AnimationClip mount, mountRight, dismount, dismountRight, ridingIdle;
    public Vector3 standingHipModelLocal = new(-.001346609f,.52547705f,.002305094f);
    [Min(1f)] public float actionTimeout=8;
    [Header("Mount: stirrup, body rise, crossing, settle")]
    [Min(0)] public float mountLift=.255f;
    public AnimationCurve mountHorizontal=Curve(0,0,.12f,.10f,.24f,.24f,.42f,.65f,.58f,.72f,.72f,.91f,.88f,1,1,1);
    public AnimationCurve mountVertical=Curve(0,0,.23f,0,.48f,1,1,1);
    public AnimationCurve mountLiftShape=Curve(0,0,.23f,0,.48f,.90f,.60f,1,.78f,.5f,.92f,0,1,0);
    [Header("Dismount: feet free, hands support, crossing, slide")]
    [Min(0)] public float dismountLift=.17f;
    public AnimationCurve dismountHorizontal=Curve(0,0,.12f,.02f,.28f,.23f,.44f,.27f,.55f,.43f,.65f,.66f,.78f,.95f,.90f,1,1,1);
    public AnimationCurve dismountVertical=Curve(0,0,.65f,0,.90f,1,1,1);
    public AnimationCurve dismountLiftShape=Curve(0,0,.12f,0,.28f,1,.48f,.95f,.62f,.75f,.72f,.40f,.88f,0,1,0);
    public float Horizontal(float phase,bool mounting)=>(mounting?mountHorizontal:dismountHorizontal).Evaluate(phase);
    public float Vertical(float phase,bool mounting)=>(mounting?mountVertical:dismountVertical).Evaluate(phase);
    public float Lift(float phase,bool mounting)=>(mounting?mountLiftShape:dismountLiftShape).Evaluate(phase)*(mounting?mountLift:dismountLift);
    /// <summary>Zero-slope keys reproduce the smooth pose-to-pose world trajectory authored in Blender.</summary>
    public static AnimationCurve Curve(params float[] values)
    {
        var keys=new Keyframe[values.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(values[i*2],values[i*2+1],0,0);
        return new AnimationCurve(keys);
    }
    /// <summary>Uses actual Animator phase so entry blends, speed changes and pause stay aligned with the authored poses.</summary>
    public static bool TryPhase(Animator animator,bool mounting,out float phase)
    {
        phase=0;if(animator==null||!animator.isActiveAndEnabled||animator.runtimeAnimatorController==null)return false;
        string left=mounting?"Mount Horse":"Dismount Horse",right=left+" Right";
        var current=animator.GetCurrentAnimatorStateInfo(0);
        if(current.IsName(left)||current.IsName(right)){phase=Mathf.Clamp01(current.normalizedTime);return true;}
        if(animator.IsInTransition(0)){var next=animator.GetNextAnimatorStateInfo(0);if(next.IsName(left)||next.IsName(right)){phase=Mathf.Clamp01(next.normalizedTime);return true;}}
        return false;
    }
}


