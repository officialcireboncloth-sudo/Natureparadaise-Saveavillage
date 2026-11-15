using UnityEngine;
using UnityEngine.UI;

/// <summary>Sprite-independent icon slots. Replace with theme artwork without moving the layout.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FertilizerMakerIcon : MaskableGraphic
{
    public enum Kind { Sack, Fish, Manure, Shell, Mineral }
    public Kind kind;
    public Color band = new(.22f,.43f,.62f);
    VertexHelper mesh;
    Vector2 Point(Vector2 p) => new(rectTransform.rect.xMin + p.x * rectTransform.rect.width,
        rectTransform.rect.yMin + p.y * rectTransform.rect.height);
    void Polygon(Color fill, params Vector2[] points)
    {
        int start = mesh.currentVertCount;
        Color tint = fill * color;
        foreach (var p in points) mesh.AddVert(Point(p), tint, Vector2.zero);
        for (int i=1;i<points.Length-1;i++) mesh.AddTriangle(start,start+i,start+i+1);
    }
    void Oval(float x,float y,float w,float h,Color fill)
    {
        var points = new Vector2[32];
        for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;points[i]=new(x+Mathf.Cos(a)*w,y+Mathf.Sin(a)*h);}
        Polygon(fill,points);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();mesh=vh;
        Color fabric=new(.69f,.59f,.40f),light=new(.85f,.76f,.55f),edge=new(.40f,.34f,.24f);
        switch(kind)
        {
            case Kind.Sack:
                Oval(.50f,.12f,.37f,.07f,new(0,0,0,.22f));
                Polygon(edge,new(.14f,.15f),new(.24f,.43f),new(.32f,.75f),new(.27f,.88f),new(.42f,.97f),new(.65f,.95f),new(.76f,.88f),new(.68f,.74f),new(.77f,.40f),new(.86f,.15f),new(.70f,.10f),new(.34f,.10f));
                Polygon(fabric,new(.18f,.17f),new(.29f,.46f),new(.35f,.75f),new(.67f,.75f),new(.73f,.46f),new(.82f,.17f),new(.66f,.13f),new(.35f,.13f));
                Polygon(light,new(.29f,.19f),new(.39f,.74f),new(.49f,.76f),new(.45f,.18f));
                Polygon(band,new(.24f,.33f),new(.30f,.56f),new(.72f,.56f),new(.79f,.33f),new(.65f,.30f),new(.40f,.30f));
                Polygon(new(1,1,1,.10f),new(.31f,.35f),new(.35f,.54f),new(.43f,.54f),new(.40f,.33f));
                Oval(.52f,.86f,.23f,.075f,light);Oval(.52f,.87f,.18f,.048f,edge);
                Oval(.52f,.89f,.15f,.038f,new(.78f,.78f,.71f));
                for(int i=0;i<10;i++)Oval(.40f+(i%5)*.057f,.88f+(i/5)*.027f,.013f,.011f,new(.91f,.90f,.80f));
                Polygon(edge,new(.34f,.73f),new(.67f,.73f),new(.69f,.77f),new(.33f,.77f));break;
            case Kind.Fish:
                Polygon(new(.40f,.48f,.43f),new(.74f,.48f),new(.96f,.74f),new(.92f,.26f));
                Polygon(new(.49f,.56f,.49f),new(.38f,.62f),new(.48f,.86f),new(.66f,.66f));
                Oval(.44f,.48f,.38f,.21f,new(.56f,.63f,.55f));
                Polygon(new(.43f,.51f,.44f),new(.47f,.47f),new(.61f,.48f),new(.50f,.30f));
                Oval(.17f,.53f,.025f,.027f,new(.13f,.20f,.17f));break;
            case Kind.Manure:
                Oval(.50f,.27f,.35f,.19f,edge);Polygon(fabric,new(.16f,.28f),new(.23f,.68f),new(.77f,.68f),new(.85f,.28f));
                for(int i=0;i<5;i++)Polygon(edge,new(.25f+i*.11f,.31f),new(.23f+i*.11f,.65f),new(.25f+i*.11f,.65f),new(.27f+i*.11f,.31f));
                Oval(.50f,.68f,.33f,.12f,light);Oval(.50f,.70f,.27f,.07f,edge);
                for(int i=0;i<5;i++)Oval(.33f+(i%3)*.16f,.70f+(i/3)*.14f,.105f,.09f,new(.38f+i*.015f,.25f,.13f));break;
            case Kind.Shell:
                Polygon(new(.72f,.72f,.61f),new(.48f,.17f),new(.18f,.55f),new(.12f,.73f),new(.29f,.90f),new(.53f,.96f),new(.78f,.87f),new(.89f,.67f),new(.64f,.20f));
                for(int i=0;i<7;i++){float x=.20f+i*.10f;Polygon(new(.56f,.59f,.50f),new(.52f,.20f),new(x,.80f),new(x+.022f,.81f));}break;
            default:
                Polygon(new(.61f,.68f,.60f),new(.18f,.22f),new(.14f,.64f),new(.40f,.90f),new(.73f,.82f),new(.89f,.41f),new(.65f,.16f));
                Polygon(new(.82f,.85f,.74f),new(.14f,.64f),new(.40f,.90f),new(.54f,.55f),new(.18f,.22f));break;
        }
    }
}
