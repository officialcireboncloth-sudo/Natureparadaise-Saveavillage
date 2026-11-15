using System.Collections.Generic;
using UnityEngine;

/// <summary>Small texture-free silhouettes; soft dust uses a transparent outer vertex ring.</summary>
public static class FarmParticleMeshes
{
    public enum Shape { Dust, Soil, Leaf, Stone, Wood, Drop, Sparkle, Heart, Wool, Grain, Ripple, Swing }
    public static Mesh Build(Shape shape)
    {
        // Shuriken mesh particles expect packed UNorm8 vertex colors. Float colors
        // are interpreted as bytes by the particle mesh path and lose red/green.
        var vertices=new List<Vector3>();var colors=new List<Color32>();var triangles=new List<int>();
        bool horizontal=shape is Shape.Ripple or Shape.Swing;
        if(horizontal)
        {
            int segments=shape==Shape.Swing?16:24;
            float arc=shape==Shape.Swing?Mathf.PI*.85f:Mathf.PI*2;
            float thickness=shape==Shape.Swing?.018f:.025f;
            for(int i=0;i<=segments;i++)
            {
                float angle=arc*i/segments-arc*.5f;
                foreach(float radius in new[]{.5f-thickness,.5f})
                {vertices.Add(new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius));colors.Add(Color.white);}
                if(i==0)continue;
                int n=i*2;triangles.AddRange(new[]{n-2,n-1,n,n,n-1,n+1});
            }
        }
        else
        {
            int count=shape switch {Shape.Sparkle=>8,Shape.Leaf=>4,Shape.Wood=>4,Shape.Soil=>5,Shape.Stone=>6,Shape.Heart=>24,Shape.Dust=>16,_=>10};
            vertices.Add(Vector3.zero);colors.Add(Color.white);
            for(int i=0;i<count;i++)
            {
                float a=2*Mathf.PI*i/count;Vector3 v;
                if(shape==Shape.Heart)
                {float s=Mathf.Sin(a);v=new Vector3(16*s*s*s,13*Mathf.Cos(a)-5*Mathf.Cos(2*a)-2*Mathf.Cos(3*a)-Mathf.Cos(4*a),0)/34f;}
                else
                {
                    float r=shape==Shape.Sparkle?(i%2==0?.5f:.12f):.5f;
                    v=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);
                    if(shape==Shape.Leaf){v.x*=.45f;v.y*=1.15f;}
                    if(shape==Shape.Wood){v.x*=.28f;v.y*=1.25f;}
                    if(shape==Shape.Grain)v.x*=.55f;
                    if(shape==Shape.Drop){v.x*=.55f;v.y*=1.2f;if(v.y<0)v.x*=.65f;}
                    if(shape is Shape.Soil or Shape.Stone)v*=i%2==0?1:.78f;
                    if(shape==Shape.Wool)v*=i%2==0?1:.83f;
                }
                vertices.Add(v);colors.Add(shape==Shape.Dust?new Color(1,1,1,0):Color.white);
                triangles.AddRange(new[]{0,i+1,(i+1)%count+1});
            }
        }
        var mesh=new Mesh{name="Farm FX "+shape};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
}
