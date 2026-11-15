using UnityEngine;
using UnityEngine.UI;

/// <summary>Small mesh icons for the pond overview: no extra texture or emoji-font dependency.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FishPondHUDIcon : MaskableGraphic
{
    public enum Symbol { Fish,Machine,Feed,Heart }
    public Symbol symbol;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(symbol==Symbol.Fish)
        {
            Oval(vh,new(.08f,0),new(.34f,.23f),color);
            Poly(vh,color,new(-.17f,0),new(-.46f,.26f),new(-.46f,-.26f));
            Poly(vh,color,new(-.08f,.15f),new(.02f,.36f),new(.19f,.16f));
            Oval(vh,new(.25f,.075f),new(.032f,.032f),new(.13f,.19f,.15f));
        }
        else if(symbol==Symbol.Heart)
        {Oval(vh,new(-.17f,.15f),new(.23f,.23f),color);Oval(vh,new(.17f,.15f),new(.23f,.23f),color);Poly(vh,color,new(-.39f,.12f),new(.39f,.12f),new(0,-.40f));}
        else if(symbol==Symbol.Machine)
        {
            Poly(vh,color,new(-.32f,-.38f),new(.32f,-.38f),new(.32f,.05f),new(-.32f,.05f));
            Poly(vh,new(.91f,.66f,.25f),new(-.38f,.38f),new(.38f,.38f),new(.18f,.08f),new(-.18f,.08f));
            Oval(vh,new(0,-.14f),new(.12f,.12f),new(.21f,.28f,.23f));
        }
        else
        {
            Poly(vh,new(.76f,.53f,.23f),new(-.25f,.40f),new(.25f,.40f),new(.18f,.22f),new(.33f,-.34f),new(-.33f,-.34f),new(-.18f,.22f));
            Poly(vh,color,new(-.15f,-.04f),new(0,-.18f),new(.20f,.14f),new(.13f,.19f),new(-.01f,-.07f),new(-.10f,.02f));
        }
    }
    void Oval(VertexHelper vh,Vector2 center,Vector2 radius,Color shade)
    {var points=new Vector2[20];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=center+Vector2.Scale(new(Mathf.Cos(a),Mathf.Sin(a)),radius);}Poly(vh,shade,points);}
    void Poly(VertexHelper vh,Color shade,params Vector2[] points)
    {int start=vh.currentVertCount;Rect r=GetPixelAdjustedRect();foreach(var p in points)vh.AddVert(r.center+Vector2.Scale(p,r.size),shade,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
}
