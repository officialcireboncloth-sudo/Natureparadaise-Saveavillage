"""Software-rasterized volume sketch from the proposed dimensions. No mesh asset export."""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parent
spec=json.loads((OUT/'barn-proposal-A.json').read_text())
colors={k:np.array(v['rgb'],float) for k,v in spec['materials']['swatches'].items()}
faces=[]
def face(v,material,normal=None):
 a=np.asarray(v,float)
 n=np.cross(a[1]-a[0],a[2]-a[0]) if normal is None else np.array(normal,float)
 n/=max(1e-10,np.linalg.norm(n));faces.append((a,material,n))
def cube(c,s,mat,bias=False):
 c=np.array(c);e=np.array(s)/2
 patterns=[[(1,-1,-1),(1,1,-1),(1,1,1),(1,-1,1)],
 [(-1,-1,1),(-1,1,1),(-1,1,-1),(-1,-1,-1)],
 [(-1,1,-1),(-1,1,1),(1,1,1),(1,1,-1)],
 [(-1,-1,-1),(-1,1,-1),(1,1,-1),(1,-1,-1)],
 [(1,-1,1),(1,1,1),(-1,1,1),(-1,-1,1)]]
 for p in patterns:
  points=[c+np.array(q)*e for q in p]
  if bias:
   normal=np.cross(points[1]-points[0],points[2]-points[0]);normal/=np.linalg.norm(normal)
   points=[v+normal*.0005 for v in points]
  face(points,mat)
def extruded_beam(p):
 a,b=np.array(p['fromXYZ']),np.array(p['toXYZ']);d=b-a;d=d/np.linalg.norm(d)
 depth=np.array((0,0,1)) if abs(d[2])<1e-8 else np.array((1,0,0))
 width=np.cross(d,depth);width/=np.linalg.norm(width)
 verts=[q+width*sw*p['width']/2+depth*sd*p['depth']/2 for q in (a,b) for sw in (-1,1) for sd in (-1,1)]
 for inds in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:face([verts[k] for k in inds],p['material'])
for p in spec['parts']:
 if p['type']=='box':cube(p['centerXYZ'],p['sizeXYZ'],p['material'],p['name'].startswith('CornerPost'))
 elif p['type']=='beam':extruded_beam(p)
 else:
  name=p['name'];norm={'GableFront':(0,0,-1),'GableRear':(0,0,1),'RoofRight':(.6,1,0),'RoofLeft':(-.6,1,0)}[name]
  if name.startswith('Roof'):
   side=1 if name=='RoofRight' else -1
   for row in range(10):
    for col in range(16):
     x0=side*4.45*row/10;x1=side*4.45*(row+1)/10;z0=-5.45+10.9*col/16;z1=-5.45+10.9*(col+1)/16
     y0=6.6-.6*abs(x0);y1=6.6-.6*abs(x1)
     mat='roof'
     face([(x0,y0,z0),(x1,y1,z0),(x1,y1,z1),(x0,y0,z1)],mat,norm)
     # Thin seam strip, not a solid shingle mesh.
     width=.012
     face([(x1-side*width,y1+.012,z0),(x1,y1+.012,z0),(x1,y1+.012,z1),(x1-side*width,y1+.012,z1)],'roof_dark',norm)
     face([(x0,y0+.003,z1-.010),(x1,y1+.003,z1-.010),(x1,y1+.003,z1),(x0,y0+.003,z1)],'roof_dark',norm)
  else:face(p['verticesXYZ'],p['material'],norm)
# Facade seams remain inside the same shared palette material.
for z,norm in [(-5.004,(0,0,-1)),(5.004,(0,0,1))]:
 for k in range(1,32):
  x=-4+k*.25;y=4.2+2.4*(1-abs(x)/4)
  face([(x-.006,.5,z),(x+.006,.5,z),(x+.006,y,z),(x-.006,y,z)],'red_dark',norm)
for x,norm in [(-4.004,(-1,0,0)),(4.004,(1,0,0))]:
 for k in range(1,40):
  z=-5+k*.25;face([(x,.5,z-.006),(x,.5,z+.006),(x,4.2,z+.006),(x,4.2,z-.006)],'red_dark',norm)
for z,norm in [(-5.006,(0,0,-1)),(5.006,(0,0,1))]:
 for row in range(2):
  for k in range(12):
   x=-4+k*.67+(row%2)*.335;lo=row*.25+.008;hi=(row+1)*.25-.008
   if x<4:face([(max(-4,x),lo,z),(min(4,x+.64),lo,z),(min(4,x+.64),hi,z),(max(-4,x),hi,z)],'stone_light' if k%3==0 else 'stone',norm)
for x,norm in [(-4.006,(-1,0,0)),(4.006,(1,0,0))]:
 for row in range(2):
  for k in range(15):
   z=-5+k*.67+(row%2)*.335;lo=row*.25+.008;hi=(row+1)*.25-.008
   if z<5:face([(x,lo,max(-5,z)),(x,lo,min(5,z+.64)),(x,hi,min(5,z+.64)),(x,hi,max(-5,z))],'stone_light' if k%3==0 else 'stone',norm)
def render(angle):
 w,h=1600,1180;scale=73
 right=np.array([1,0,1],float)/math.sqrt(2)
 look=np.array([1,.80,-1],float);look/=np.linalg.norm(look)
 up=np.cross(right,look);up/=np.linalg.norm(up)
 pixels=np.full((h,w,3),[250,247,239],dtype=np.uint8);depth=np.full((h,w),-1e20)
 rot=np.array([[math.cos(angle),0,math.sin(angle)],[0,1,0],[-math.sin(angle),0,math.cos(angle)]])
 sun=np.array([-.35,.9,-.45]);sun/=np.linalg.norm(sun)
 for verts,mat,n in faces:
  verts=verts@rot.T;n=rot@n
  # Both faces of schematic beams draw; z buffer resolves which faces are visible.
  shade=.78+.22*max(0,np.dot(n,sun));col=np.clip(colors[mat]*shade,0,255).astype(np.uint8)
  coords=np.column_stack((800+verts@right*scale,760-verts@up*scale,verts@look))
  for k in range(1,len(coords)-1):
   a,b,c=coords[[0,k,k+1]];x0=max(0,int(math.floor(min(a[0],b[0],c[0]))));x1=min(w-1,int(math.ceil(max(a[0],b[0],c[0]))));y0=max(0,int(math.floor(min(a[1],b[1],c[1]))));y1=min(h-1,int(math.ceil(max(a[1],b[1],c[1]))))
   if x1<x0 or y1<y0:continue
   den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(den)<1e-9:continue
   xx,yy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
   aa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
   bb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;cc=1-aa-bb
   zz=aa*a[2]+bb*b[2]+cc*c[2];existing=depth[y0:y1+1,x0:x1+1]
   # Stable ties for coplanar roof/trim surfaces in this planning raster.
   # Prefer the first visible surface rather than numerical speckling.
   mask=(aa>=-.00001)&(bb>=-.00001)&(cc>=-.00001)&(zz>existing+1e-7)
   existing[mask]=zz[mask];pixels[y0:y1+1,x0:x1+1][mask]=col
 im=Image.fromarray(pixels);d=ImageDraw.Draw(im)
 def txt(x,y,s,n=26,b=False):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf' if b else 'C:/Windows/Fonts/arial.ttf',n),fill='#302e2b')
 txt(65,42,'BARN A / SKETSA VOLUME LOW POLY',34,True)
 txt(65,96,'Depan + kanan' if angle==0 else 'Belakang + kiri',25)
 txt(65,1050,'Master Lv3: badan 8 x 10 m / batas atap 8.9 x 10.9 x 6.8 m',27,True)
 txt(65,1095,'Warna disampling dari atlas pack. Sketsa volume; belum menjadi FBX / model Blender final.',23)
 return im
render(0).save(OUT/'barn-volume-front-right.png')
render(math.pi).save(OUT/'barn-volume-rear-left.png')
print('Volume sketches rendered from the same proposal primitives; no production mesh created.')
