import sys,copy,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'ArtSource/Crops/Scripts'))
from unified_plant_atlas import *
def rectangles(registry):
 free=[(0,0,4096,4096)]
 for s in registry['slots']:
  a,b,c,d=s['pixelRectTopLeft'];right,bottom=a+c,b+d;new=[]
  for x,y,w,h in free:
   if a>=x+w or right<=x or b>=y+h or bottom<=y:new.append((x,y,w,h));continue
   if a>x:new.append((x,y,a-x,h))
   if right<x+w:new.append((right,y,x+w-right,h))
   if b>y:new.append((x,y,w,b-y))
   if bottom<y+h:new.append((x,bottom,w,y+h-bottom))
  unique=list(set(new));free=[r for r in unique if not any(r!=o and o[0]<=r[0] and o[1]<=r[1] and o[0]+o[2]>=r[0]+r[2] and o[1]+o[3]>=r[1]+r[3] for o in unique)]
 return free
r=load(REGISTRY);free=rectangles(r);print('FREE',len(free),sorted(free,key=lambda t:-t[2]*t[3])[:15],flush=True)
for method in ['shortside','area','bottomright','bottomleft','topright']:
 f=copy.deepcopy(r);alloc=[]
 for i,size in enumerate([256]*8+[128]*12):
  rects=[p for p in rectangles(f) if min(p[2:])>=size]
  if not rects:print(method,'FAIL',i,alloc,flush=True);break
  if method=='shortside':score=lambda p:(min(p[2]-size,p[3]-size),max(p[2]-size,p[3]-size),p[1],p[0])
  elif method=='area':score=lambda p:(p[2]*p[3],min(p[2]-size,p[3]-size),p[1],p[0])
  elif method=='bottomright':score=lambda p:(-p[1]-p[3],-p[0]-p[2])
  elif method=='bottomleft':score=lambda p:(-p[1]-p[3],p[0])
  else:score=lambda p:(p[1],-p[0]-p[2])
  p=min(rects,key=score);x,y=p[:2]
  if method in ['bottomright','bottomleft']:y+=p[3]-size
  if method in ['bottomright','topright']:x+=p[2]-size
  rect=[x,y,size,size];alloc.append(rect);f['slots'].append(dict(key=str(i),pixelRectTopLeft=rect))
 else:print(method,'PASS',alloc,flush=True)
