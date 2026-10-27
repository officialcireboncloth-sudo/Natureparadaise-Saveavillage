using UnityEngine;
using UnityEngine.UI;

/// <summary>Small vector icons; independent of sprite slots and font glyph availability.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MainMenuIcon : MaskableGraphic
{
    public enum Kind { Play, Sprout, Settings, People, Exit, Chevron, Globe, Monitor, Info, Heart, Energy, Sun, Coin, Drop,
        Horse, Dog, Cat, Footsteps, Hand, Home, Saddle, Close }
    public Kind kind;
    VertexHelper mesh;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); mesh = vh;
        switch (kind)
        {
            case Kind.Horse:
                Line(new(-.26f,.08f),new(.10f,.08f),.27f);
                Line(new(.10f,.08f),new(.21f,.32f),.17f);
                Line(new(.21f,.32f),new(.38f,.22f),.14f);
                Triangle(new(.16f,.35f),new(.20f,.48f),new(.25f,.34f));
                Line(new(-.28f,.04f),new(-.32f,-.37f),.065f);Line(new(-.15f,.02f),new(-.10f,-.37f),.06f);
                Line(new(.04f,.02f),new(.02f,-.37f),.065f);Line(new(.13f,.04f),new(.23f,-.33f),.06f);
                Line(new(-.37f,.15f),new(-.44f,-.20f),.05f);break;
            case Kind.Dog:
            case Kind.Cat:
                Line(new(-.23f,.02f),new(.12f,.02f),.24f);Disc(new(.20f,.21f),.14f);
                Triangle(new(.10f,.28f),new(.18f,.44f),new(.25f,.28f));
                if(kind==Kind.Dog)Line(new(.25f,.18f),new(.38f,.16f),.12f);
                else Triangle(new(.24f,.30f),new(.33f,.43f),new(.32f,.20f));
                Line(new(-.24f,-.02f),new(-.26f,-.34f),.07f);Line(new(.10f,-.02f),new(.13f,-.34f),.07f);
                Line(new(-.33f,.04f),new(-.43f,.28f),.06f);break;
            case Kind.Footsteps:
                Line(new(-.19f,-.30f),new(-.26f,-.04f),.15f);Disc(new(-.27f,.12f),.10f);
                Line(new(.17f,-.04f),new(.23f,.22f),.15f);Disc(new(.25f,.38f),.10f);break;
            case Kind.Hand:
                Disc(new(0,-.12f),.23f);Line(new(-.10f,-.26f),new(.10f,-.40f),.20f);
                for(int i=0;i<4;i++){float x=-.18f+i*.115f;float top=i==0?.23f:i==3?.28f:.38f;Line(new(x,-.06f),new(x,top),.085f);Disc(new(x,top),.0425f);}
                Line(new(-.15f,-.16f),new(-.36f,.05f),.10f);Disc(new(-.36f,.05f),.05f);break;
            case Kind.Home:
                Triangle(new(-.44f,.12f),new(0,.46f),new(.44f,.12f));
                Line(new(-.30f,.14f),new(-.30f,-.36f),.07f);Line(new(.30f,.14f),new(.30f,-.36f),.07f);
                Line(new(-.30f,-.36f),new(.30f,-.36f),.07f);Line(new(-.15f,-.34f),new(-.15f,.03f),.045f);Line(new(.15f,-.34f),new(.15f,.03f),.045f);
                Line(new(-.15f,.03f),new(.15f,.03f),.045f);Line(new(-.13f,-.31f),new(.13f,0),.035f);Line(new(.13f,-.31f),new(-.13f,0),.035f);break;
            case Kind.Saddle:
                Line(new(-.34f,.18f),new(.32f,.18f),.16f);Disc(new(-.32f,.24f),.12f);Disc(new(.32f,.24f),.12f);
                Triangle(new(-.24f,.14f),new(.24f,.14f),new(-.12f,-.28f));Triangle(new(.24f,.14f),new(.12f,-.28f),new(-.12f,-.28f));
                Line(new(.25f,.11f),new(.28f,-.30f),.045f);Ring(new(.28f,-.30f),.09f,.035f);break;
            case Kind.Close:
                Line(new(-.29f,-.29f),new(.29f,.29f),.065f);Line(new(-.29f,.29f),new(.29f,-.29f),.065f);break;
            case Kind.Heart:
                Disc(new(-.18f, .15f), .23f); Disc(new(.18f, .15f), .23f);
                Triangle(new(-.39f, .05f), new(.39f, .05f), new(0, -.43f)); break;
            case Kind.Energy:
                Disc(new(.10f, .32f), .10f);
                Line(new(.02f, .12f), new(-.10f, -.09f), .09f);
                Line(new(.02f, .12f), new(.26f, .0f), .065f);
                Line(new(.02f, .12f), new(-.24f, .04f), .065f);
                Line(new(-.10f, -.09f), new(.14f, -.22f), .07f);
                Line(new(.14f, -.22f), new(.03f, -.42f), .07f);
                Line(new(-.10f, -.09f), new(-.33f, -.38f), .07f); break;
            case Kind.Sun:
                Disc(Vector2.zero, .22f);
                for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Vector2 d=new(Mathf.Cos(a),Mathf.Sin(a)); Line(d*.30f,d*.44f,.035f); } break;
            case Kind.Coin: Disc(Vector2.zero,.40f); break;
            case Kind.Drop:
                Disc(new(0,-.15f),.25f); Triangle(new(-.24f,-.05f),new(.24f,-.05f),new(0,.43f)); break;
            case Kind.Play: Triangle(new(-.28f, -.42f), new(-.28f, .42f), new(.43f, 0)); break;
            case Kind.Chevron: Line(new(-.15f, -.28f), new(.14f, 0), .065f); Line(new(.14f, 0), new(-.15f, .28f), .065f); break;
            case Kind.Sprout:
                Line(new(0, -.43f), new(0, .15f), .06f);
                Leaf(new(0, -.03f), new(-.43f, .36f));
                Leaf(new(0, .04f), new(.43f, .4f)); break;
            case Kind.Settings:
                Ring(Vector2.zero, .29f, .13f);
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a)); Line(d * .25f, d * .43f, .12f); } break;
            case Kind.People:
                Disc(new(-.19f, .23f), .14f); Disc(new(.22f, .21f), .12f);
                Dome(new(-.19f, -.35f), .23f); Dome(new(.23f, -.35f), .20f); break;
            case Kind.Exit:
                Line(new(-.32f, -.4f), new(-.32f, .4f), .05f); Line(new(-.32f, .4f), new(.12f, .4f), .05f);
                Line(new(-.32f, -.4f), new(.12f, -.4f), .05f); Line(new(.12f, -.4f), new(.12f, .4f), .05f);
                Line(new(-.1f, 0), new(.43f, 0), .07f); Line(new(.25f, .17f), new(.43f, 0), .07f); Line(new(.25f, -.17f), new(.43f, 0), .07f); break;
            case Kind.Globe:
                Ring(Vector2.zero, .4f, .035f); Line(new(-.38f, 0), new(.38f, 0), .035f);
                Line(new(0, -.38f), new(0, .38f), .035f);
                for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12f, b = (i + 1) * Mathf.PI / 12f; Line(new(Mathf.Cos(a) * .19f, Mathf.Sin(a) * .4f), new(Mathf.Cos(b) * .19f, Mathf.Sin(b) * .4f), .025f); } break;
            case Kind.Monitor:
                Line(new(-.42f, -.2f), new(.42f, -.2f), .05f); Line(new(-.42f, .34f), new(.42f, .34f), .05f);
                Line(new(-.42f, -.2f), new(-.42f, .34f), .05f); Line(new(.42f, -.2f), new(.42f, .34f), .05f);
                Line(new(0, -.2f), new(0, -.4f), .05f); Line(new(-.2f, -.4f), new(.2f, -.4f), .05f); break;
            case Kind.Info: Ring(Vector2.zero, .4f, .04f); Disc(new(0, .2f), .04f); Line(new(0, -.22f), new(0, .06f), .06f); break;
        }
    }
    Vector2 Point(Vector2 p) { Rect r = rectTransform.rect; return r.center + p * Mathf.Min(r.width, r.height); }
    void Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(Point(a), color, Vector2.zero); mesh.AddVert(Point(b), color, Vector2.zero); mesh.AddVert(Point(c), color, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2);
    }
    void Line(Vector2 a, Vector2 b, float width)
    {
        Vector2 d = (b - a).normalized; Vector2 side = new Vector2(-d.y, d.x) * width * .5f;
        Triangle(a - side, a + side, b + side); Triangle(a - side, b + side, b - side);
    }
    void Disc(Vector2 c, float r)
    {
        for (int i = 0; i < 32; i++) { float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16; Triangle(c, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r); }
    }
    void Ring(Vector2 c, float r, float width)
    {
        for (int i = 0; i < 32; i++) { float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16; Line(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, width); }
    }
    void Dome(Vector2 c, float r)
    {
        for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16; Triangle(c, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r); }
    }
    void Leaf(Vector2 root, Vector2 tip)
    {
        Vector2 axis = tip - root, side = new Vector2(-axis.y, axis.x) * .38f;
        Vector2 center = (root + tip) * .5f;
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f, b = (i + 1) / 12f;
            Vector2 p = Vector2.Lerp(root, tip, a), q = Vector2.Lerp(root, tip, b);
            Triangle(center, p + side * Mathf.Sin(a * Mathf.PI), q + side * Mathf.Sin(b * Mathf.PI));
            Triangle(center, q - side * Mathf.Sin(b * Mathf.PI), p - side * Mathf.Sin(a * Mathf.PI));
        }
    }
}
