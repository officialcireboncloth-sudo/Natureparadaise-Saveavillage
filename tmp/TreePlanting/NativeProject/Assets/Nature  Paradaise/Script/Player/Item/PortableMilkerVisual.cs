using UnityEngine;

/// <summary>One reusable held vacuum milker: a sealed canister, a hand-guided cup and a short low-poly hose.</summary>
[DisallowMultipleComponent]
public sealed class PortableMilkerVisual : MonoBehaviour
{
    public Transform suctionCup;
    public MeshFilter hose;
    public Vector3 cupRestPosition = new(.29f, -.16f, 0);
    public Vector3 hoseOutlet = new(.02f, .01f, 0);
    public Vector3 cupHoseInlet = new(0, .065f, 0);
    public Vector2 hoseUV;
    const int Segments = 10, Sides = 6;
    Mesh tube;
    Vector3[] vertices;
    public bool CupAttached { get; private set; }

    void Awake()
    {
        vertices = new Vector3[(Segments + 1) * Sides];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[Segments * Sides * 6];
        for (int i=0; i<uv.Length; i++) uv[i] = hoseUV;
        for (int i=0, t=0; i<Segments; i++) for (int j=0; j<Sides; j++)
        {
            int a=i*Sides+j, b=i*Sides+(j+1)%Sides, c=a+Sides, d=b+Sides;
            triangles[t++]=a; triangles[t++]=c; triangles[t++]=b;
            triangles[t++]=b; triangles[t++]=c; triangles[t++]=d;
        }
        tube = new Mesh { name = "Portable Milker Hose (66 vertices)" };
        tube.MarkDynamic(); tube.vertices=vertices; tube.uv=uv; tube.triangles=triangles;
        if(hose!=null) hose.sharedMesh=tube;
    }

    /// <summary>Follows the authored left palm during attachment; never alternates manual squeezing strokes.</summary>
    public void Pose(Vector3 leftPalm, Quaternion cupWorkRotation, float phase, bool working)
    {
        if(suctionCup==null || hose==null) return;
        float reach = working ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.13f,.35f,phase)) *
            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.90f,phase))) : 0;
        Vector3 rest=transform.TransformPoint(cupRestPosition);
        suctionCup.position=Vector3.Lerp(rest,leftPalm+Vector3.up*.015f,reach);
        suctionCup.rotation=Quaternion.Slerp(transform.rotation,cupWorkRotation,reach);
        CupAttached=working && phase>=.35f && phase<=.72f;
        Vector3 start=transform.TransformPoint(hoseOutlet), end=suctionCup.TransformPoint(cupHoseInlet);
        Vector3 controlA=start+Vector3.up*.12f, controlB=end+Vector3.up*.11f;
        for(int i=0;i<=Segments;i++)
        {
            float t=i/(float)Segments, u=1-t;
            Vector3 p=u*u*u*start+3*u*u*t*controlA+3*u*t*t*controlB+t*t*t*end;
            Vector3 tangent=(3*u*u*(controlA-start)+6*u*t*(controlB-controlA)+3*t*t*(end-controlB)).normalized;
            Vector3 axis=Vector3.Cross(tangent,transform.forward).normalized;
            if(axis.sqrMagnitude<.1f) axis=transform.right;
            Vector3 second=Vector3.Cross(tangent,axis).normalized;
            for(int j=0;j<Sides;j++)
            {
                float angle=j*Mathf.PI*2/Sides;
                vertices[i*Sides+j]=hose.transform.InverseTransformPoint(p+(axis*Mathf.Cos(angle)+second*Mathf.Sin(angle))*.007f);
            }
        }
        tube.vertices=vertices; tube.RecalculateNormals(); tube.RecalculateBounds();
    }

    void OnDestroy() { if(tube!=null) Destroy(tube); }
}
