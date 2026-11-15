using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable monochrome livestock, poultry and feeding-trough silhouettes, without extra mobile textures.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BarnHUDIcon : MaskableGraphic
{
    public enum Symbol { Cow, Poultry, Trough }
    public Symbol symbol;
    VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();mesh=vh;
        if(symbol==Symbol.Trough)
        {
            // Open rim and a deep basin read as a feeding station at small HUD sizes.
            Line(new(-.43f,.14f),new(-.24f,.31f),.055f);
            Line(new(-.24f,.31f),new(.43f,.31f),.055f);
            Line(new(.43f,.31f),new(.24f,.14f),.055f);
            Line(new(.24f,.14f),new(-.43f,.14f),.055f);
            Triangle(new(-.43f,.10f),new(.22f,.10f),new(-.34f,-.19f));
            Triangle(new(.22f,.10f),new(.17f,-.19f),new(-.34f,-.19f));
            Triangle(new(.27f,.09f),new(.43f,.24f),new(.34f,-.04f));
            Triangle(new(.27f,.09f),new(.34f,-.04f),new(.22f,-.17f));
            Line(new(-.29f,-.18f),new(-.33f,-.38f),.065f);
            Line(new(.13f,-.18f),new(.17f,-.38f),.065f);
            Line(new(.33f,-.03f),new(.37f,-.23f),.055f);
            return;
        }
        if(symbol==Symbol.Poultry)
        {
            Ellipse(new(-.06f,-.04f),new(.30f,.23f));
            Line(new(.12f,-.01f),new(.24f,.22f),.18f);Ellipse(new(.25f,.24f),new(.12f,.13f));
            Ellipse(new(.18f,.37f),new(.05f,.07f));Ellipse(new(.26f,.39f),new(.05f,.08f));
            Triangle(new(.34f,.27f),new(.48f,.20f),new(.34f,.17f));
            Triangle(new(-.27f,.02f),new(-.46f,.27f),new(-.40f,-.12f));
            Line(new(-.12f,-.20f),new(-.16f,-.39f),.045f);Line(new(.08f,-.20f),new(.11f,-.39f),.045f);
            Line(new(-.22f,-.40f),new(-.06f,-.40f),.04f);Line(new(.04f,-.40f),new(.19f,-.40f),.04f);return;
        }
        // Front-facing bovine head: broad ears/muzzle and curved horns remain clear at small sizes.
        Ellipse(new(0,.05f),new(.205f,.275f));
        Ellipse(new(0,-.245f),new(.265f,.155f));
        Ear(-1);Ear(1);Horn(-1);Horn(1);
    }
    void Ear(float side)
    {
        Vector2 root=new(side*.16f,.20f),tip=new(side*.49f,.23f),bottom=new(side*.34f,.075f);
        Triangle(root,tip,bottom);
        Ellipse(new(side*.30f,.17f),new(.13f,.075f));
    }
    void Horn(float side)
    {
        Vector2 a=new(side*.145f,.27f),b=new(side*.30f,.26f),c=new(side*.40f,.36f),d=new(side*.34f,.49f);
        const int segments=12;
        for(int i=0;i<segments;i++)
        {
            float t=i/(float)segments,u=(i+1)/(float)segments;
            Vector2 p=Curve(a,b,c,d,t),q=Curve(a,b,c,d,u),direction=(q-p).normalized;
            Vector2 normal=new(-direction.y,direction.x);
            Vector2 start=normal*Mathf.Lerp(.042f,.002f,t),end=normal*Mathf.Lerp(.042f,.002f,u);
            Triangle(p-start,p+start,q+end);Triangle(p-start,q+end,q-end);
        }
    }
    static Vector2 Curve(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
    {float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;}
    Vector3 Point(Vector2 p)=>rectTransform.rect.center+Vector2.Scale(p,rectTransform.rect.size);
    void Triangle(Vector2 a,Vector2 b,Vector2 c)
    {
        int start=mesh.currentVertCount;Color shade=color;
        mesh.AddVert(Point(a),shade,Vector2.zero);mesh.AddVert(Point(b),shade,Vector2.zero);mesh.AddVert(Point(c),shade,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);
    }
    void Quad(Vector2 min,Vector2 max)
    {Triangle(min,new(min.x,max.y),max);Triangle(min,max,new(max.x,min.y));}
    void Line(Vector2 a,Vector2 b,float width)
    {
        Vector2 delta=(b-a).normalized,side=new Vector2(-delta.y,delta.x)*width*.5f;
        Triangle(a-side,a+side,b+side);Triangle(a-side,b+side,b-side);
    }
    void Ellipse(Vector2 center,Vector2 radii)
    {
        const int segments=24;
        for(int i=0;i<segments;i++)
        {float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;Triangle(center,center+Vector2.Scale(new(Mathf.Cos(a),Mathf.Sin(a)),radii),center+Vector2.Scale(new(Mathf.Cos(b),Mathf.Sin(b)),radii));}
    }
}
