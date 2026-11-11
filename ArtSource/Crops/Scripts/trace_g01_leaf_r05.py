"""Measure the supplied sprout leaf silhouettes instead of using a generic oval."""
import bpy,sys,json,math
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import unified_plant_atlas as U
W=G.REVIEW/'Leaf_R05';W.mkdir(parents=True,exist_ok=True)
def pixels(p):
 im=bpy.data.images.load(str(p),check_existing=False);w,h=im.size;a=np.empty(w*h*4,np.float32);im.pixels.foreach_get(a);bpy.data.images.remove(im);return a.reshape(h,w,4)[::-1]
def largest(mask):
 seen=np.zeros_like(mask,bool);best=[]
 for yy,xx in np.argwhere(mask):
  if seen[yy,xx]:continue
  stack=[(int(yy),int(xx))];seen[yy,xx]=True;points=[]
  while stack:
   y,x=stack.pop();points.append((x,y))
   for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
    a,b=y+dy,x+dx
    if 0<=a<mask.shape[0] and 0<=b<mask.shape[1] and mask[a,b] and not seen[a,b]:seen[a,b]=True;stack.append((a,b))
  if len(points)>len(best):best=points
 return np.array(best,float)
def rdp(points,tol):
 if len(points)<3:return points
 a,b=points[0],points[-1];d=b-a;n=np.linalg.norm(d);dist=np.abs(np.cross(d,points-a))/max(n,1e-8);i=int(np.argmax(dist))
 if dist[i]>tol:return np.vstack([rdp(points[:i+1],tol)[:-1],rdp(points[i:],tol)])
 return np.array([a,b])
records={}
for kind,index,rect in [('Blackberry',0,[295,342,393,423]),('Raspberry',1,[301,326,410,421])]:
 path=G.GROUP/'References'/('User_Berry_'+str(index)+'.png');a=pixels(path);x0,y0,x1,y1=rect;tile=a[y0:y1,x0:x1,:3];mask=(tile[:,:,1]>tile[:,:,0]*.98)&(tile[:,:,1]>tile[:,:,2]*1.22)&((tile[:,:,1]-tile[:,:,2])>.08);p=largest(mask);check=len(p)>500
 assert check,(kind,len(p))
 mean=p.mean(axis=0);v=np.linalg.eigh(np.cov((p-mean).T))[1][:,-1]
 if v.dot(np.array([1.,-1.]))<0:v=-v
 across=np.array([-v[1],v[0]]);t=(p-mean)@v;x=(p-mean)@across;lo,hi=float(t.min()),float(t.max());length=hi-lo
 # The line joining the base and pointed tip registers the central vein.
 base=float(np.median(x[t<lo+2]));tip=float(np.median(x[t>hi-2]));tt=(t-lo)/length;x=x-(base+(tip-base)*tt);width=float(x.max()-x.min());left=[];right=[]
 for i in range(65):
  z=i/64;row=x[np.abs(tt-z)<1/64]
  if len(row):left.append([z,float(row.min()/width)]);right.append([z,float(row.max()/width)])
 left=np.array(left);right=np.array(right);left[0]=[0,0];right[0]=[0,0];left[-1]=[1,0];right[-1]=[1,0]
 left=rdp(left,.011);right=rdp(right,.011)
 rawLeft,rawRight=left.tolist(),right.tolist();fine=[]
 # Preserve the measured blade envelope, without reproducing low-resolution
 # raster stair steps as large accidental notches in the 3D model.
 grid=np.linspace(0,1,257);kernel=np.exp(-.5*(np.arange(-14,15)/6.)**2);kernel/=kernel.sum();teeth=10 if kind=='Blackberry' else 11;depth=.045 if kind=='Blackberry' else .037
 for side,profile in [(-1,left),(1,right)]:
  envelope=np.convolve(np.pad(np.maximum(np.interp(grid,profile[:,0],np.abs(profile[:,1])),0),(14,14),mode='edge'),kernel,mode='valid');edge=[[0.,0.]]
  for tooth in range(teeth):
   for f,valley in [(.18,True),(.78,False)]:
    tt=.025+.865*(tooth+f)/teeth;xx=float(np.interp(tt,grid,envelope));xx=max(.0002,xx-(depth*math.sin(math.pi*tt) if valley else 0));edge.append([tt,side*xx])
  edge.append([1.,0.]);fine.append(edge)
 record=dict(species=kind,reference=U.rel(path),referenceSha256=U.sha(path),crop=rect,principalAxis=list(v),measuredAspect=width/length,left=fine[0],right=fine[1],rawMeasuredLeft=rawLeft,rawMeasuredRight=rawRight,teethPerEdge=teeth,serrationDepthFraction=depth,pixels=len(p),method='Measured supplied sprout blade envelope and aspect; small directed serrations fitted to the image, with raw measured edges retained for comparison')
 records[kind]=record
 print('REFERENCE_CONTOUR',kind,'aspect',round(width/length,3),'edge points',len(left),len(right),flush=True)
U.write(W/'ReferenceContours.json',dict(status='MEASURED_FROM_USER_IMAGES',leaves=records))
# Exact silhouette overlays, with no invented texture or botanical template.
body=''
for j,(kind,r) in enumerate(records.items()):
 x=60+j*480;points=[(x+180+v*300,420-t*360) for t,v in r['left']]+[(x+180+v*300,420-t*360) for t,v in reversed(r['right'])];body+='<polygon points="'+' '.join(f'{a:.2f},{b:.2f}' for a,b in points)+'" fill="#4d8a38" stroke="#243f21" stroke-width="2"/><text x="'+str(x+180)+'" y="470" font-family="sans-serif" text-anchor="middle" font-size="24">'+kind+'</text>'
(W/'MeasuredSilhouettes.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="510"><rect width="1000" height="510" fill="#f4f1e9"/>'+body+'</svg>')
