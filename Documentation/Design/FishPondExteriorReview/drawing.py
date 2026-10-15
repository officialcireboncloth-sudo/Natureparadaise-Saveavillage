from pathlib import Path
import math,itertools,numpy as np
from PIL import Image,ImageDraw,ImageFont
BG=(248,245,235)
INK='#263b41'
COLORS={}
def font(n=24,bold=False):
 return ImageFont.truetype(str(Path('C:/Windows/Fonts')/('arialbd.ttf' if bold else 'arial.ttf')),n)
def label(d,xy,s,n=24,bold=False,fill=INK):
 d.text(xy,s,font=font(n,bold),fill=fill)
def faces_for(model):
    faces=[]
    for p in model['parts']:
        mat=p['material']
        if p['type']=='surface': faces.append((np.array(p['verticesXYZ'],float),mat));continue
        if p['type']=='box':
            c=np.array(p['centerXYZ']);e=np.array(p['sizeXYZ'])/2
            v=[c+np.array(q)*e for q in itertools.product([-1,1],repeat=3)]
        else:
            a=np.array(p['fromXYZ']);b=np.array(p['toXYZ']);axis=(b-a)/np.linalg.norm(b-a)
            ref=np.array([0,1,0]) if abs(axis[1])<.9 else np.array([0,0,1])
            side=np.cross(axis,ref);side/=np.linalg.norm(side);up=np.cross(axis,side)
            v=[q+side*s*p['width']/2+up*t*p['depth']/2 for q in [a,b] for s,t in itertools.product([-1,1],repeat=2)]
        for idx in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:
            faces.append((np.array([v[i] for i in idx]),mat))
    return faces

def render(m,cam,size=(1200,1000),same_scale=None,top=False):
    w,h=size;look=np.array(cam,float);look/=np.linalg.norm(look)
    if top: right=np.array([1.,0,0]);up=np.array([0.,0,-1.])
    else:
        right=np.cross(look,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,look)
    faces=faces_for(m);allvs=np.concatenate([v for v,_ in faces]);xs=allvs@right;ys=allvs@up
    vertical_margin=280 if h>=1000 else 180
    scale=same_scale or min((w-120)/(xs.max()-xs.min()),(h-vertical_margin)/(ys.max()-ys.min()))
    center=(xs.min()+xs.max())/2;centery=(ys.min()+ys.max())/2
    pixels=np.full((h,w,3),BG,dtype=np.uint8);depth=np.full((h,w),-1e20)
    for vs,mat in faces:
        n=np.cross(vs[1]-vs[0],vs[2]-vs[0]);n/=max(1e-10,np.linalg.norm(n))
        # Flat schematic illumination, not an in-game shader preview.
        col=np.clip(COLORS[mat]*(.76+.24*abs(n[1])),0,255).astype(np.uint8)
        coords=np.column_stack((w/2+(vs@right-center)*scale,h/2-(vs@up-centery)*scale,vs@look))
        for k in range(1,len(coords)-1):
            a,b,c=coords[[0,k,k+1]]
            x0=max(0,int(math.floor(min(a[0],b[0],c[0]))));x1=min(w-1,int(math.ceil(max(a[0],b[0],c[0]))))
            y0=max(0,int(math.floor(min(a[1],b[1],c[1]))));y1=min(h-1,int(math.ceil(max(a[1],b[1],c[1]))))
            den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
            if abs(den)<1e-9 or x1<x0 or y1<y0: continue
            xx,yy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
            aa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
            bb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;cc=1-aa-bb
            zz=aa*a[2]+bb*b[2]+cc*c[2];existing=depth[y0:y1+1,x0:x1+1]
            mask=(aa>=-1e-5)&(bb>=-1e-5)&(cc>=-1e-5)&(zz>existing+1e-7)
            existing[mask]=zz[mask];pixels[y0:y1+1,x0:x1+1][mask]=col
    im=Image.fromarray(pixels)
    # Thin shape edges from depth differences, improves legibility of palette surfaces.
    occupied=depth>-1e19;edges=np.zeros((h,w),bool)
    edges[:,1:] |= occupied[:,1:] & (~occupied[:,:-1] | ((abs(depth[:,1:]-depth[:,:-1])>.10)&occupied[:,:-1]))
    edges[1:,:] |= occupied[1:,:] & (~occupied[:-1,:] | ((abs(depth[1:,:]-depth[:-1,:])>.10)&occupied[:-1,:]))
    arr=np.array(im);arr[edges]=(arr[edges]*.68).astype(np.uint8)
    return Image.fromarray(arr)

def dims(d,xy1,xy2,s):
    d.line([xy1,xy2],fill='#71858a',width=2)
    for x,y in [xy1,xy2]: d.line([(x-6,y-6),(x+6,y+6)],fill='#71858a',width=2)
    x=(xy1[0]+xy2[0])/2;y=(xy1[1]+xy2[1])/2
    bb=d.textbbox((0,0),s,font=font(20));ww=bb[2]
    d.rectangle((x-ww/2-7,y-14,x+ww/2+7,y+13),fill=BG)
    label(d,(x-ww/2,y-12),s,20)

