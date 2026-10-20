using UnityEngine;

/// <summary>Metre-sized bounds in a yaw-only building frame, never a re-rotated world AABB.</summary>
public readonly struct ConstructionSiteLayout
{
    public readonly Vector3 Origin;
    public readonly Quaternion Rotation;
    public readonly Bounds LocalBounds;
    public ConstructionSiteLayout(Vector3 origin,Quaternion rotation,Bounds localBounds)
    {Origin=origin;Rotation=rotation;LocalBounds=localBounds;}
    public Vector3 ToWorld(Vector3 local)=>Origin+Rotation*local;
    public Vector3 ToLocal(Vector3 world)=>Quaternion.Inverse(Rotation)*(world-Origin);
    public Bounds WorldBounds
    {
        get
        {
            var result=new Bounds(ToWorld(LocalBounds.center),Vector3.zero);
            foreach(var p in Corners(LocalBounds))result.Encapsulate(ToWorld(p));return result;
        }
    }
    static Quaternion Heading(Transform anchor)
    {
        var forward=Vector3.ProjectOnPlane(anchor.forward,Vector3.up);
        return forward.sqrMagnitude>.0001f?Quaternion.LookRotation(forward):Quaternion.identity;
    }
    static System.Collections.Generic.IEnumerable<Vector3> Corners(Bounds bounds)
    {
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            yield return bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
    }
    public static ConstructionSiteLayout FromWorldBounds(Transform anchor,Bounds worldBounds)
    {
        var frame=new ConstructionSiteLayout(anchor.position,Heading(anchor),default);
        var local=new Bounds(frame.ToLocal(worldBounds.center),Vector3.zero);
        foreach(var p in Corners(worldBounds))local.Encapsulate(frame.ToLocal(p));
        return new ConstructionSiteLayout(frame.Origin,frame.Rotation,local);
    }
    public static ConstructionSiteLayout Measure(GameObject owner,Transform anchor,BuildingDefinitionSO definition,GameObject finalVisual,bool renovation)
    {
        Vector3 origin=anchor.position;Quaternion rotation=Heading(anchor);
        var worldToFrame=Matrix4x4.TRS(origin,rotation,Vector3.one).inverse;
        Bounds local=default;bool found=false;
        void Include(Renderer renderer,Matrix4x4 modelToWorld)
        {
            if(!renderer.enabled||renderer is ParticleSystemRenderer||renderer.GetComponent<TMPro.TMP_Text>()!=null)return;
            // Measuring local mesh corners avoids the inflation of Renderer.bounds on rotated models.
            Bounds b=renderer.localBounds;if(b.size.sqrMagnitude<.0001f)return;
            Matrix4x4 matrix=worldToFrame*modelToWorld*renderer.transform.localToWorldMatrix;
            foreach(var p in Corners(b))
            {
                Vector3 point=matrix.MultiplyPoint3x4(p);
                if(!found){local=new Bounds(point,Vector3.zero);found=true;}else local.Encapsulate(point);
            }
        }
        if(renovation)
            foreach(var renderer in owner.GetComponentsInChildren<Renderer>())
            {
                bool generated=false;
                for(var t=renderer.transform;t!=null&&t!=owner.transform;t=t.parent)
                    if(t.name=="Construction_Visuals_Runtime"){generated=true;break;}
                if(!generated)Include(renderer,Matrix4x4.identity);
            }
        if(finalVisual!=null)
        {
            // Instantiate(source, anchor.position, anchor.rotation, anchor) retains root scale,
            // but replaces the source root's pose. Reproduce that exact placement without spawning it.
            Matrix4x4 placement=anchor.localToWorldMatrix*Matrix4x4.Scale(finalVisual.transform.localScale)*finalVisual.transform.worldToLocalMatrix;
            foreach(var renderer in finalVisual.GetComponentsInChildren<Renderer>(true))Include(renderer,placement);
        }
        if(!found)
        {
            Bounds fallback=new Bounds(Vector3.up*1.5f,new Vector3(definition!=null?definition.footprintWidth:3,3,definition!=null?definition.footprintDepth:3));
            foreach(var p in Corners(fallback))
            {
                Vector3 point=worldToFrame.MultiplyPoint3x4(anchor.TransformPoint(p));
                if(!found){local=new Bounds(point,Vector3.zero);found=true;}else local.Encapsulate(point);
            }
        }
        return new ConstructionSiteLayout(origin,rotation,local);
    }
}
