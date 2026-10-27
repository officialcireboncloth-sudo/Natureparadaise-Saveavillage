using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A texture-free, bounded ribbon sampled from the real blade; action time also controls fading.</summary>
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public sealed class SickleBladeTrail : MonoBehaviour
{
    const int Capacity=32;
    readonly Vector3[] points=new Vector3[Capacity];
    readonly float[] times=new float[Capacity];
    readonly Vector3[] vertices=new Vector3[Capacity*3];
    readonly Color32[] colors=new Color32[Capacity*3];
    readonly int[] indices=new int[(Capacity-1)*12];
    Mesh mesh;
    MeshRenderer renderer;
    float lifetime,width;
    int count;
    public int PointCount=>count;
    public Mesh Geometry=>mesh;
    public MeshRenderer Renderer=>renderer;

    /// <summary>Uses the same mobile particle material, with no new textures or shaders.</summary>
    public void Configure(Material material,float width,float lifetime)
    {
        if(mesh==null){mesh=new Mesh{name="Sickle blade ribbon"};mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh;renderer=GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;}
        renderer.sharedMaterial=material;this.width=width;this.lifetime=lifetime;Clear();
    }
    /// <summary>Stores at most 32 recent physical blade positions, independently of renderer-native timing.</summary>
    public void Sample(Vector3 position,float time,bool emitting)
    {
        while(count>0&&time-times[0]>lifetime){for(int i=1;i<count;i++){points[i-1]=points[i];times[i-1]=times[i];}count--;}
        if(emitting&&(count==0||(position-points[count-1]).sqrMagnitude>.0001f)){
            if(count==Capacity){for(int i=1;i<count;i++){points[i-1]=points[i];times[i-1]=times[i];}count--;}
            points[count]=position;times[count++]=time;
        }
        if(count<2){renderer.enabled=false;return;}
        var camera=Camera.main;
        for(int i=0;i<count;i++){
            Vector3 tangent=points[Mathf.Min(i+1,count-1)]-points[Mathf.Max(0,i-1)];
            Vector3 view=camera!=null?camera.transform.position-points[i]:Vector3.up;
            Vector3 side=Vector3.Cross(tangent,view).normalized*width*.5f;
            float fade=Mathf.Clamp01(1-(time-times[i])/lifetime);
            vertices[i*3]=transform.InverseTransformPoint(points[i]-side);vertices[i*3+1]=transform.InverseTransformPoint(points[i]);vertices[i*3+2]=transform.InverseTransformPoint(points[i]+side);
            colors[i*3]=new Color(.86f,.94f,.71f,0);colors[i*3+1]=new Color(.86f,.94f,.71f,.45f*fade);colors[i*3+2]=colors[i*3];
            if(i==0)continue;int index=(i-1)*12;int a=(i-1)*3,b=i*3;
            indices[index]=a;indices[index+1]=b;indices[index+2]=a+1;indices[index+3]=a+1;indices[index+4]=b;indices[index+5]=b+1;
            indices[index+6]=a+1;indices[index+7]=b+1;indices[index+8]=a+2;indices[index+9]=a+2;indices[index+10]=b+1;indices[index+11]=b+2;
        }
        mesh.Clear();mesh.SetVertices(vertices,0,count*3);mesh.SetColors(colors,0,count*3);mesh.SetTriangles(indices,0,(count-1)*12,0);mesh.RecalculateBounds();renderer.enabled=true;
    }
    public void Clear(){count=0;if(mesh!=null)mesh.Clear();if(renderer!=null)renderer.enabled=false;}
    void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
