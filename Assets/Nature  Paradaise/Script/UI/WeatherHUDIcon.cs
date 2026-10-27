using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using UnityEngine.U2D;

/// <summary>Weather marks with an optional sprite and a texture-free vector fallback.</summary>
[AddComponentMenu("Nature Paradise/UI/Weather HUD Icon")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class WeatherHUDIcon : MaskableGraphic
{
    WeatherType? weather;
    bool night;
    Sprite artwork;
    VertexHelper mesh;
    Color ink;
    static readonly Color Cloud = new(.74f,.85f,.91f);
    static readonly Color DarkCloud = new(.42f,.59f,.72f);
    static readonly Color Water = new(.34f,.73f,1f);

    public override Texture mainTexture => artwork != null ? artwork.texture : s_WhiteTexture;

    protected override void OnEnable()
    {
        useLegacyMeshGeneration = false;
        base.OnEnable();
        SpriteAtlasManager.atlasRegistered += RefreshAtlas;
    }

    protected override void OnDisable()
    {
        SpriteAtlasManager.atlasRegistered -= RefreshAtlas;
        base.OnDisable();
    }

    void RefreshAtlas(SpriteAtlas atlas)
    {
        if (artwork != null && atlas.CanBindTo(artwork)) SetAllDirty();
    }

    /// <summary>Updates only when the current weather, day/night or assigned artwork changes.</summary>
    public void SetWeather(WeatherType? current, bool atNight = false, Sprite sprite = null)
    {
        if (weather == current && night == atNight && artwork == sprite)
            return;
        weather = current; night = atNight; artwork = sprite;
        SetVerticesDirty(); SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear(); mesh = helper;
        if (artwork != null)
        {
            var uv = DataUtility.GetOuterUV(artwork);
            var rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width, rect.height);
            float aspect = artwork.rect.width / Mathf.Max(1, artwork.rect.height);
            Vector2 half = aspect > 1 ? new(size/2,size/aspect/2) : new(size*aspect/2,size/2);
            Vector2 c = rect.center;
            mesh.AddVert(c+new Vector2(-half.x,-half.y),color,new(uv.x,uv.y));
            mesh.AddVert(c+new Vector2(-half.x,half.y),color,new(uv.x,uv.w));
            mesh.AddVert(c+new Vector2(half.x,half.y),color,new(uv.z,uv.w));
            mesh.AddVert(c+new Vector2(half.x,-half.y),color,new(uv.z,uv.y));
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
            return;
        }
        if (!weather.HasValue) { DrawCloud(false); return; }
        switch (weather.Value)
        {
            case WeatherType.Sunny: SunOrMoon(Vector2.zero,.26f); break;
            case WeatherType.PartlyCloudy: SunOrMoon(new(-.17f,.17f),.20f); DrawCloud(false); break;
            case WeatherType.Heatwave:
                ink=new(1f,.55f,.18f); Disc(new(0,.12f),.23f);
                Rays(new(0,.12f),.29f,.39f);
                for(int i=0;i<3;i++) Wave(new(-.28f+i*.28f,-.40f),.13f,.14f);
                break;
            case WeatherType.Drizzle: DrawCloud(false); Rain(3,.09f,.05f); break;
            case WeatherType.Rain: DrawCloud(false); Rain(3,.16f,.065f); break;
            case WeatherType.HeavyRain: DrawCloud(true); Rain(5,.20f,.065f); break;
            case WeatherType.WindRainStorm: DrawCloud(true); Rain(3,.19f,.055f); Wind(); break;
            case WeatherType.Cyclone:
                ink=Cloud;
                for(int row=0;row<4;row++)
                    Line(new(-.38f+row*.08f,.30f-row*.18f),new(.38f-row*.08f,.30f-row*.18f),.065f);
                Line(new(-.06f,-.28f),new(.13f,-.43f),.065f);
                break;
            case WeatherType.Thunderstorm:
                DrawCloud(true);ink=new(1f,.82f,.23f);
                Triangle(new(.03f,-.03f),new(-.17f,-.29f),new(.02f,-.24f));
                Triangle(new(.02f,-.24f),new(-.05f,-.46f),new(.23f,-.16f));
                break;
            case WeatherType.Snow: DrawCloud(false); Snow(new(-.19f,-.30f));Snow(new(.20f,-.30f));break;
            case WeatherType.Blizzard: DrawCloud(true);Wind();Snow(new(.16f,-.31f));break;
        }
    }
    void SunOrMoon(Vector2 c,float radius)
    {
        ink=night?new(.94f,.94f,.76f):new(1f,.79f,.20f);
        if(!night){Disc(c,radius);Rays(c,radius+.06f,radius+.15f);return;}
        // A crescent polygon: no opaque cutout that depends on the panel color.
        for(int i=0;i<24;i++)
        {
            float t=i/24f,u=(i+1)/24f;
            Vector2 outerA=Arc(c,radius,75.5f+t*209),outerB=Arc(c,radius,75.5f+u*209);
            Vector2 innerA=Arc(c+new Vector2(radius*.5f,0),radius,104.5f+t*151);
            Vector2 innerB=Arc(c+new Vector2(radius*.5f,0),radius,104.5f+u*151);
            Triangle(outerA,outerB,innerB);Triangle(outerA,innerB,innerA);
        }
    }
    static Vector2 Arc(Vector2 center,float radius,float degrees){float a=degrees*Mathf.Deg2Rad;return center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
    void DrawCloud(bool dark)
    {
        ink=dark?DarkCloud:Cloud;
        Disc(new(-.23f,.02f),.17f);Disc(new(-.05f,.15f),.22f);Disc(new(.18f,.06f),.19f);Disc(new(.30f,-.015f),.12f);
        Line(new(-.23f,-.06f),new(.29f,-.06f),.19f);
    }
    void Rain(int count,float length,float width)
    {
        ink=Water;
        for(int i=0;i<count;i++)
        {
            float x=count==1?0:Mathf.Lerp(-.29f,.29f,i/(float)(count-1));
            Vector2 top=new(x,-.20f),bottom=top+new Vector2(-length*.4f,-length);
            Line(top,bottom,width);Disc(bottom,width*.5f);
        }
    }
    void Wind(){ink=new(.85f,.94f,1f);Line(new(-.45f,-.12f),new(-.05f,-.12f),.035f);Line(new(-.48f,-.24f),new(-.20f,-.24f),.035f);}
    void Snow(Vector2 c)
    {
        ink=new(.87f,.97f,1f);
        for(int i=0;i<3;i++){float a=i*Mathf.PI/3;Vector2 d=new(Mathf.Cos(a)*.10f,Mathf.Sin(a)*.10f);Line(c-d,c+d,.028f);}
    }
    void Rays(Vector2 c,float inner,float outer)
    {
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Vector2 d=new(Mathf.Cos(a),Mathf.Sin(a));Line(c+d*inner,c+d*outer,.034f);}
    }
    void Wave(Vector2 c,float height,float width)
    {
        for(int i=0;i<8;i++){float a=i/8f,b=(i+1)/8f;Line(c+new Vector2(Mathf.Sin(a*Mathf.PI*2)*width*.3f,a*height),c+new Vector2(Mathf.Sin(b*Mathf.PI*2)*width*.3f,b*height),.027f);}
    }
    Vector2 Point(Vector2 p){var r=rectTransform.rect;return r.center+p*Mathf.Min(r.width,r.height);}
    void Triangle(Vector2 a,Vector2 b,Vector2 c)
    {
        int n=mesh.currentVertCount;mesh.AddVert(Point(a),ink*color,Vector2.zero);mesh.AddVert(Point(b),ink*color,Vector2.zero);mesh.AddVert(Point(c),ink*color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);
    }
    void Line(Vector2 a,Vector2 b,float width){Vector2 d=(b-a).normalized,side=new Vector2(-d.y,d.x)*width*.5f;Triangle(a-side,a+side,b+side);Triangle(a-side,b+side,b-side);}
    void Disc(Vector2 center,float radius){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);}}
}
