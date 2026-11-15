using UnityEngine;
using UnityEngine.UI;

/// <summary>Subtle field tint with a rounded, independently opaque selection outline.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SoilScannerFieldGraphic : MaskableGraphic
{
    public Color borderColor = Color.white;
    public float borderWidth = 2;
    public float radius = 12;
    readonly Vector2[] footprint={new(0,0),new(0,1),new(1,1),new(1,0)};
    public void SetFootprint(Vector2[] corners)
    {
        bool changed=false;
        for(int i=0;i<4;i++){changed|=(footprint[i]-corners[i]).sqrMagnitude>.0000001f;footprint[i]=corners[i];}
        if(changed)SetVerticesDirty();
    }
    Vector2 Warp(Vector2 point,Rect rect)
    {
        float x=(point.x-rect.xMin)/rect.width,y=(point.y-rect.yMin)/rect.height;
        var normalized=Vector2.LerpUnclamped(Vector2.LerpUnclamped(footprint[0],footprint[3],x),
            Vector2.LerpUnclamped(footprint[1],footprint[2],x),y);
        return rect.min+Vector2.Scale(normalized,rect.size);
    }
    public override bool Raycast(Vector2 screenPoint,Camera eventCamera)
    {
        if(!base.Raycast(screenPoint,eventCamera)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out var local))return false;
        Rect rect=rectTransform.rect;var p=new Vector2((local.x-rect.xMin)/rect.width,(local.y-rect.yMin)/rect.height);
        float sign=0;
        for(int i=0;i<4;i++)
        {
            var a=footprint[i];var b=footprint[(i+1)%4];float cross=(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
            if(Mathf.Abs(cross)<.00001f)continue;
            if(sign!=0&&Mathf.Sign(cross)!=sign)return false;sign=Mathf.Sign(cross);
        }
        return true;
    }
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        float r = Mathf.Clamp(radius, 0, Mathf.Min(rect.width,rect.height)*.5f);
        const int steps=8, count=(steps+1)*4;
        helper.AddVert(Warp(rect.center,rect),color,Vector2.zero);
        for(int corner=0;corner<4;corner++)
        {
            Vector2 center=new(corner==0||corner==3?rect.xMax-r:rect.xMin+r,
                corner<2?rect.yMax-r:rect.yMin+r);
            for(int step=0;step<=steps;step++)
            {
                float angle=(corner*90f+step*90f/steps)*Mathf.Deg2Rad;
                helper.AddVert(Warp(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r,rect),color,Vector2.zero);
            }
        }
        for(int i=0;i<count;i++)helper.AddTriangle(0,1+i,1+(i+1)%count);
        if(borderWidth<=0)return;
        float inner=Mathf.Max(0,r-borderWidth);
        for(int corner=0;corner<4;corner++)
        {
            Vector2 center=new(corner==0||corner==3?rect.xMax-r:rect.xMin+r,
                corner<2?rect.yMax-r:rect.yMin+r);
            for(int step=0;step<=steps;step++)
            {
                float angle=(corner*90f+step*90f/steps)*Mathf.Deg2Rad;
                Vector2 direction=new(Mathf.Cos(angle),Mathf.Sin(angle));
                helper.AddVert(Warp(center+direction*r,rect),borderColor,Vector2.zero);
                helper.AddVert(Warp(center+direction*inner,rect),borderColor,Vector2.zero);
            }
        }
        for(int i=0;i<count;i++)
        {
            int a=1+count+i*2,b=1+count+(i+1)%count*2;
            helper.AddTriangle(a,b,a+1);helper.AddTriangle(a+1,b,b+1);
        }
    }
}
