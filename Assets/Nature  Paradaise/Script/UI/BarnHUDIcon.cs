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
        Ellipse(new(-.07f,.02f),new(.30f,.18f));
        Line(new(.13f,.01f),new(.24f,.22f),.18f);Ellipse(new(.28f,.21f),new(.14f,.11f));
        Quad(new(.30f,.10f),new(.46f,.22f));
        Triangle(new(.20f,.28f),new(.17f,.43f),new(.27f,.30f));
        Triangle(new(.30f,.29f),new(.38f,.40f),new(.35f,.27f));
        Triangle(new(.17f,.23f),new(.07f,.28f),new(.14f,.16f));
        Line(new(-.26f,-.06f),new(-.29f,-.35f),.065f);Line(new(-.12f,-.08f),new(-.08f,-.35f),.06f);
        Line(new(.09f,-.08f),new(.06f,-.35f),.065f);Line(new(.18f,-.06f),new(.26f,-.34f),.06f);
        Line(new(-.35f,.10f),new(-.44f,-.04f),.035f);Line(new(-.44f,-.04f),new(-.43f,-.21f),.035f);
        Ellipse(new(-.43f,-.23f),new(.028f,.055f));
    }
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
