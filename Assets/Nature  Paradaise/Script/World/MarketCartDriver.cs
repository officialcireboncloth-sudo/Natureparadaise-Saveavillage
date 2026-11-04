using UnityEngine;

/// <summary>Authored keeper actions and distance-driven wheels. Stock stays on MarketStand.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(200)]
public sealed class MarketCartDriver : MonoBehaviour
{
    public string Action { get; private set; }
    NPCCharacterMotion keeper;
    Transform leftWheel, rightWheel;
    Vector3 previousPosition;
    void Awake()
    {
        keeper=GetComponentInChildren<NPCCharacterMotion>(true);
        foreach(var t in GetComponentsInChildren<Transform>(true))
        { if(t.name=="WheelLeftPivot")leftWheel=t; if(t.name=="WheelRightPivot")rightWheel=t; }
        previousPosition=transform.position;
    }
    public void Play(string action)
    {
        if(Action==action)return;
        Action=action;
        if(keeper && keeper.isActiveAndEnabled)
        { if(string.IsNullOrEmpty(action))keeper.ResumeLocomotion();else keeper.PlayAction(action,true); }
    }
    public void ResetPresentation(){Action=null;previousPosition=transform.position;if(keeper)keeper.ResumeLocomotion();}
    void LateUpdate()
    {
        Vector3 delta=transform.position-previousPosition;previousPosition=transform.position;
        if(delta.sqrMagnitude>1)return; // Save restore/off-camera reset is not rolling.
        float angle=Vector3.Dot(delta,transform.forward)/.4185f*Mathf.Rad2Deg;
        if(leftWheel)leftWheel.Rotate(Vector3.right,angle,Space.Self);
        if(rightWheel)rightWheel.Rotate(Vector3.right,angle,Space.Self);
    }
}
