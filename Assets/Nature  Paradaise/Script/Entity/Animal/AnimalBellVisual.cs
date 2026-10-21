using UnityEngine;

/// <summary>Authored bell mesh, rope and interaction markers; animates only while ringing.</summary>
[DisallowMultipleComponent]
public sealed class AnimalBellVisual : MonoBehaviour
{
    [SerializeField] Transform swingPivot;
    [SerializeField] Transform promptAnchor;
    [SerializeField] Transform approach;
    Quaternion restRotation;
    float elapsed;
    bool initialized;
    public Transform PromptAnchor => promptAnchor;
    public Transform Approach => approach;
    public Transform SwingPivot => swingPivot;
    public bool IsRinging => enabled && initialized;

    public void Configure(Transform pivot, Transform prompt, Transform interactionPoint)
    { swingPivot=pivot;promptAnchor=prompt;approach=interactionPoint; }
    void Awake()
    {
        restRotation=swingPivot!=null?swingPivot.localRotation:Quaternion.identity;
        initialized=true;enabled=false;
    }
    /// <summary>Plays the bell and attached rope swing without an idle Update loop.</summary>
    public void Ring()
    {
        if(swingPivot==null)return;
        if(!initialized){restRotation=swingPivot.localRotation;initialized=true;}
        elapsed=0;enabled=true;
    }
    void Update()
    {
        elapsed+=Time.deltaTime;
        if(swingPivot==null||elapsed>=1.7f){enabled=false;return;}
        float angle=Mathf.Sin(elapsed*18)*24*Mathf.Exp(-elapsed*2);
        swingPivot.localRotation=restRotation*Quaternion.AngleAxis(angle,Vector3.forward);
    }
    void OnDisable(){if(initialized&&swingPivot!=null)swingPivot.localRotation=restRotation;}
}
