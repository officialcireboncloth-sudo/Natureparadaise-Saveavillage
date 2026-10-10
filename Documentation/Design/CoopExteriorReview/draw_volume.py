"""Software z-buffer volume sketches; consumes canonical proposal, exports no meshes."""
from pathlib import Path
import json, math, itertools
import numpy as np
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parent
spec=json.loads((OUT/'coop-proposal-A.json').read_text())
colors={k:np.array(v['rgb'],float) for k,v in spec['materials']['swatches'].items()}
faces=[]
def face(v,mat):
 a=np.asarray(v,float);n=np.cross(a[1]-a[0],a[2]-a[0]);n/=max(1e-10,np.linalg.norm(n));faces.append((a,mat,n))
for p in spec['parts']:
 if p['type']=='surface':face(p['verticesXYZ'],p['material']);continue
 if p['type']=='box':
  c=np.array(p['centerXYZ']);e=np.array(p['sizeXYZ'])/2
  v=[c+np.array(q)*e for q in itertools.product([-1,1],repeat=3)]
 else:
  a=np.array(p['fromXYZ']);b=np.array(p['toXYZ']);delta=b-a;delta/=np.linalg.norm(delta)
  depth=np.array([0,0,1]) if abs(delta[2])<1e-8 else np.array([1,0,0])
  width=np.cross(delta,depth)
  v=[q+width*sw*p['width']/2+depth*sd*p['depth']/2 for q in [a,b] for sw,sd in itertools.product([-1,1],repeat=2)]
 for ids in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:face([v[i] for i in ids],p['material'])
def render(angle):
 w,h=1600,1180;scale=86
 right=np.array([1,0,1])/math.sqrt(2);look=np.array([1,.72,-1]);look/=np.linalg.norm(look);up=np.cross(right,look);up/=np.linalg.norm(up)
 pixels=np.full((h,w,3),[250,247,239],dtype=np.uint8);depth=np.full((h,w),-1e20)
 rot=np.array([[math.cos(angle),0,math.sin(angle)],[0,1,0],[-math.sin(angle),0,math.cos(angle)]])
 for vs,mat,n in faces:
  vs=vs@rot.T;n=rot@n
  # Flat planning illumination, preserve atlas hue. No claim of in-engine shader.
  col=np.clip(colors[mat]*(.84+.16*abs(n[1])),0,255).astype(np.uint8)
  coords=np.column_stack((800+vs@right*scale,820-vs@up*scale,vs@look))
  for k in range(1,len(coords)-1):
   a,b,c=coords[[0,k,k+1]]
   x0=max(0,int(math.floor(min(a[0],b[0],c[0]))));x1=min(w-1,int(math.ceil(max(a[0],b[0],c[0]))))
   y0=max(0,int(math.floor(min(a[1],b[1],c[1]))));y1=min(h-1,int(math.ceil(max(a[1],b[1],c[1]))))
   den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(den)<1e-9 or x1<x0 or y1<y0:continue
   xx,yy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
   aa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
   bb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;cc=1-aa-bb
   zz=aa*a[2]+bb*b[2]+cc*c[2];existing=depth[y0:y1+1,x0:x1+1]
   mask=(aa>=-1e-5)&(bb>=-1e-5)&(cc>=-1e-5)&(zz>existing+1e-7)
   existing[mask]=zz[mask];pixels[y0:y1+1,x0:x1+1][mask]=col
 im=Image.fromarray(pixels);d=ImageDraw.Draw(im)
 def text(x,y,s,n=24,b=False):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if b else 'arial.ttf'),n),fill='#302e2b')
 text(55,35,'COOP A / SKETSA VOLUME LOW POLY',32,True)
 text(55,86,'Depan + kanan' if angle==0 else 'Belakang + kiri (usulan sisi tidak terlihat)',25)
 text(55,1050,'Master Lv3: badan 6 x 7 m / envelope 7.35 x 8.70 x 5.74 m',26,True)
 text(55,1093,'Warna dari atlas pack. Gambar perencanaan; belum model Blender final / belum apply Unity.',23)
 return im
render(0).save(OUT/'coop-volume-front-right.png')
render(math.pi).save(OUT/'coop-volume-rear-left.png')
print('PASS: volume drawings use proposal coordinates and per-pixel depth, no mesh export.')
