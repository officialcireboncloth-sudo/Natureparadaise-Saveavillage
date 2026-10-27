using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored pond water bounds and dock interaction, independent of stored fish.</summary>
[DisallowMultipleComponent]
public sealed class FishPondExterior : MonoBehaviour
{
    /// <summary>Shared scale for authored pond geometry, colliders/stations and placement metres on the 0.8m farm grid.</summary>
    public const float WorldScale=.4f;
    [SerializeField] Transform interactionPoint;
    [SerializeField] Vector2[] waterPolygon;
    [SerializeField] Vector2 dockMinimum;
    [SerializeField] Vector2 dockMaximum;
    [SerializeField] float swimHeight = .44f;
    public Transform InteractionPoint => interactionPoint;
    /// <summary>Closest safe water landing from the bank, excluding the dock.</summary>
    public Vector3 FeedingWaterPoint(Vector3 actorPosition)
    {
        Vector3 start=transform.InverseTransformPoint(actorPosition),inside=SwimOrigin(0);
        for(int i=1;i<=32;i++)
        {Vector3 point=Vector3.Lerp(start,inside,i/32f);point.y=swimHeight;if(IsSwimPoint(point,.2f))return transform.TransformPoint(point);}
        return transform.TransformPoint(inside);
    }
    public static Vector2 PlacementSizeMeters(int level) => WorldScale*(level <= 1 ? new Vector2(14,11) : level == 2 ? new Vector2(16,13) : new Vector2(20,16));

    public bool IsSwimPoint(Vector3 point, float margin = .40f)
    {
        if (waterPolygon == null || waterPolygon.Length < 3) return false;
        Vector2 q = new(point.x, point.z);
        if (q.x > dockMinimum.x-margin && q.x < dockMaximum.x+margin &&
            q.y > dockMinimum.y-margin && q.y < dockMaximum.y+margin) return false;
        bool inside = false;
        for (int i = 0, j = waterPolygon.Length-1; i < waterPolygon.Length; j = i++)
        {
            Vector2 a = waterPolygon[j], b = waterPolygon[i];
            Vector2 edge = b-a;
            float t = Mathf.Clamp01(Vector2.Dot(q-a, edge)/Mathf.Max(.000001f, edge.sqrMagnitude));
            if ((q-(a+edge*t)).sqrMagnitude < margin*margin) return false;
            if ((a.y>q.y)!=(b.y>q.y) && q.x < (b.x-a.x)*(q.y-a.y)/(b.y-a.y)+a.x) inside = !inside;
        }
        return inside;
    }

    public Vector3 SwimOrigin(int index)
    {
        Vector2 lo = waterPolygon[0], hi = lo;
        foreach (Vector2 p in waterPolygon) { lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p); }
        for (int attempt=0;attempt<128;attempt++)
        {
            int n=index*131+attempt+1;
            Vector3 p=new(Mathf.Lerp(lo.x,hi.x,Halton(n,2)),swimHeight,Mathf.Lerp(lo.y,hi.y,Halton(n,3)));
            if (IsSwimPoint(p,.85f)) return p;
        }
        return new Vector3((lo.x+hi.x)/2,swimHeight,(lo.y+hi.y)/2);
    }

    static float Halton(int n,int radix)
    {
        float result=0,f=1;
        while(n>0){f/=radix;result+=f*(n%radix);n/=radix;}return result;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Transform point, Vector2[] polygon, Vector2 dockMin, Vector2 dockMax)
    { interactionPoint=point;waterPolygon=polygon;dockMinimum=dockMin;dockMaximum=dockMax; }
#endif
}
