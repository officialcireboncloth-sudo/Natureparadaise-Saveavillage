using UnityEngine;

/// <summary>One timeline and reach definition for the sickle pose, trail and cutting sector.</summary>
[CreateAssetMenu(menuName="Nature Paradise/Tools/Sickle Sweep")]
public sealed class SickleSweepProfile : ScriptableObject
{
    [Header("Forward cutting fan")]
    [Range(30,160)] public float angle=120;
    [Tooltip("Actual world-space radius shared by blade motion and cutting checks.")]
    [Min(.1f)] public float baseReach=.75f;
    [Range(0,.2f)] public float extraReachPerLevel=.04f;
    [Min(.1f)] public float verticalTolerance=.8f;
    [Header("Animation phases (seconds)")]
    [Min(.05f)] public float windup=.45f;
    [Min(.1f)] public float sweep=.5f;
    [Min(.05f)] public float followThrough=.4f;
    [Header("Physical blade motion")]
    [Range(.1f,.6f)] public float cuttingHeight=.3f;
    [Range(0,35)] public float torsoTurn=24;
    [Range(0,30)] public float lean=18;
    [Range(0,.35f)] public float weightShift=.28f;
    [Tooltip("Lower the hips during the sweep so a hand-sized sickle reaches low grass without stretching the arm.")]
    [Range(0,.35f)] public float crouch=.25f;
    [Range(.01f,.08f)] public float trailWidth=.035f;
    [Range(.05f,.3f)] public float trailSeconds=.14f;
    public float Duration=>windup+sweep+followThrough;
}

/// <summary>A player-centred forward sector; signed angles run from right to left during a sweep.</summary>
public readonly struct SickleSweepRegion
{
    public readonly Vector3 origin,forward;
    public readonly float radius,halfAngle,verticalTolerance;
    public SickleSweepRegion(Vector3 origin,Vector3 forward,float radius,float angle,float verticalTolerance)
    {this.origin=origin;forward.y=0;this.forward=forward.sqrMagnitude>.001f?forward.normalized:Vector3.forward;this.radius=radius;halfAngle=angle*.5f;this.verticalTolerance=verticalTolerance;}
    public bool Contains(Vector3 point,out float angle)
    {
        Vector3 delta=point-origin;angle=0;if(Mathf.Abs(delta.y)>verticalTolerance)return false;delta.y=0;
        if(delta.sqrMagnitude>radius*radius+.00001f)return false;
        angle=Vector3.SignedAngle(forward,delta,Vector3.up);
        return Mathf.Abs(angle)<=halfAngle+.001f;
    }
    public Vector3 Point(float angle)=>origin+Quaternion.AngleAxis(angle,Vector3.up)*forward*radius;
}

/// <summary>Stable references collected once per action; normal gameplay transactions still perform each cut.</summary>
public readonly struct SickleCutTarget
{
    readonly WorldGatherable gatherable;
    readonly FieldArea field;
    readonly TerrainDetailGrassManager terrain;
    readonly int x,z;
    public readonly Vector3 point;
    public readonly float angle;
    /// <summary>Current work position of this captured target.</summary>
    public Vector3 Position=>gatherable!=null?gatherable.transform.position:field!=null?field.GridToWorld(x,z):terrain!=null?terrain.GetPatchPosition(x):point;
    /// <summary>Optional gatherable hierarchy excluded from its own work line obstruction test.</summary>
    public Transform Anchor=>gatherable!=null?gatherable.transform:null;
    /// <summary>Whether the original grass, dead crop or terrain patch can still be cut.</summary>
    public bool IsAvailable=>gatherable!=null?gatherable.isActiveAndEnabled&&gatherable.CanSickle:
        field!=null?field.isActiveAndEnabled&&field.TryGetSnapshot(x,z,out var tile)&&tile.State==TileState.Planted&&tile.CropState==CropLifecycleState.Dead:
        terrain!=null&&terrain.isActiveAndEnabled&&terrain.IsPatchAvailable(x);
    public SickleCutTarget(WorldGatherable target,Vector3 point,float angle){gatherable=target;field=null;terrain=null;x=z=0;this.point=point;this.angle=angle;}
    public SickleCutTarget(FieldArea target,int x,int z,Vector3 point,float angle){field=target;gatherable=null;terrain=null;this.x=x;this.z=z;this.point=point;this.angle=angle;}
    public SickleCutTarget(TerrainDetailGrassManager target,int index,Vector3 point,float angle){terrain=target;field=null;gatherable=null;x=index;z=0;this.point=point;this.angle=angle;}
    public bool Cut(PlayerGatheringTool player,SickleSweepRegion region)
    {
        if(gatherable!=null)return region.Contains(gatherable.transform.position,out _)&&gatherable.Cut(player);
        if(field!=null)return region.Contains(field.GridToWorld(x,z),out _)&&field.TryCutDeadCrop(x,z);
        return terrain!=null&&region.Contains(terrain.GetPatchPosition(x),out _)&&terrain.TrySicklePatch(x);
    }
}
