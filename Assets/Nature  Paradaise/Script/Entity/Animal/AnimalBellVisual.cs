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
    void NormalizeWrapper()
    {
        // The legacy standalone resource has an extra scaled/rotated wrapper.
        for(var wrapper=swingPivot.parent;wrapper!=null&&wrapper!=transform;wrapper=wrapper.parent)
        {wrapper.localPosition=Vector3.zero;wrapper.localRotation=Quaternion.identity;wrapper.localScale=Vector3.one;}
    }
    /// <summary>Reuse the authored timber post at ground level without enlarging it with the building level.</summary>
    public void PlaceOnPost(Transform parent,Vector3 postGround,Vector3 approachPosition,float size)
    {
        if(parent==null||swingPivot==null)return;
        NormalizeWrapper();
        MeshFilter post=null;
        foreach(var mesh in GetComponentsInChildren<MeshFilter>(true))
            if(mesh.name.Contains("StandalonePost")){post=mesh;break;}
        if(post==null||post.sharedMesh==null)return;
        var matrix=transform.worldToLocalMatrix*post.transform.localToWorldMatrix;
        Bounds local=post.sharedMesh.bounds,rootBounds=new(matrix.MultiplyPoint3x4(local.min),Vector3.zero);
        for(int i=0;i<8;i++)rootBounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(
            (i&1)==0?local.min.x:local.max.x,(i&2)==0?local.min.y:local.max.y,(i&4)==0?local.min.z:local.max.z)));
        Vector3 postBase=new(rootBounds.center.x,rootBounds.min.y,rootBounds.center.z);
        Vector3 pivotPosition=postGround+Quaternion.Euler(0,180,0)*((swingPivot.localPosition-postBase)*size);
        MountOnFacade(parent,pivotPosition,approachPosition,size);
        post.gameObject.SetActive(true);
    }
    /// <summary>Mount the shared small bell on the front wall, with independent door/bell approaches.</summary>
    public void MountOnFacade(Transform facade,Vector3 pivotPosition,Vector3 approachPosition,float size=1f)
    {
        if(facade==null||swingPivot==null)return;
        enabled=false;
        NormalizeWrapper();
        transform.SetParent(facade,false);
        transform.localRotation=Quaternion.Euler(0,180,0);
        transform.localScale=Vector3.one*size;
        transform.localPosition=pivotPosition-transform.localRotation*(swingPivot.localPosition*size);
        foreach(var part in GetComponentsInChildren<MeshRenderer>(true))
            part.gameObject.SetActive(!part.name.Contains("StandalonePost"));
        if(approach!=null){approach.position=facade.TransformPoint(approachPosition);approach.rotation=facade.rotation;}
        if(promptAnchor!=null)promptAnchor.position=facade.TransformPoint(pivotPosition+Vector3.down*.45f*size);
    }
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
