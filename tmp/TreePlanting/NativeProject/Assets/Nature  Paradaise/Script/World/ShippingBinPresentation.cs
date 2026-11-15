using System.Collections;
using UnityEngine;

/// <summary>Atlas chest presentation only: stable world size and a rear-hinged lid that still animates while storage pauses time.</summary>
[DisallowMultipleComponent]
public sealed class ShippingBinPresentation : MonoBehaviour
{
    [SerializeField] Transform lidPivot;
    [SerializeField, Range(10,100)] float openAngle=78;
    [SerializeField, Min(.05f)] float duration=.25f;
    Quaternion closedRotation;
    Coroutine animation;
    bool wanted;
    public Transform LidPivot=>lidPivot;

    /// <summary>Connects the model's rear-edge hinge during prefab authoring.</summary>
    public void Configure(Transform pivot){lidPivot=pivot;closedRotation=pivot!=null?pivot.localRotation:Quaternion.identity;}
    void Awake()
    {
        closedRotation=lidPivot!=null?lidPivot.localRotation:Quaternion.identity;
        var owner=GetComponentInParent<ShippingBin>();
        if(owner==null)return;
        // Legacy Map instances scaled the old vendor crate by 2x. The replacement is authored in metres.
        var scale=owner.transform.parent!=null?owner.transform.parent.lossyScale:Vector3.one;
        if(Mathf.Abs(scale.x)>.0001f&&Mathf.Abs(scale.y)>.0001f&&Mathf.Abs(scale.z)>.0001f)
            owner.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
    }
    /// <summary>Opens or closes the decorative lid without changing stock, prices or collision.</summary>
    public void SetOpen(bool open)
    {
        if(lidPivot==null||wanted==open&&animation!=null)return;
        wanted=open;
        if(animation!=null)StopCoroutine(animation);
        animation=null;
        var target=closedRotation*Quaternion.Euler(open?-openAngle:0,0,0);
        if(!isActiveAndEnabled||Quaternion.Angle(lidPivot.localRotation,target)<.01f){lidPivot.localRotation=target;return;}
        animation=StartCoroutine(Animate(target));
    }
    IEnumerator Animate(Quaternion target)
    {
        var start=lidPivot.localRotation;float elapsed=0;
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            lidPivot.localRotation=Quaternion.Slerp(start,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));
            yield return null;
        }
        lidPivot.localRotation=target;animation=null;GetComponent<LODGroup>()?.RecalculateBounds();
    }
    void OnDisable(){if(animation!=null)StopCoroutine(animation);animation=null;wanted=false;if(lidPivot!=null)lidPivot.localRotation=closedRotation;}
}
