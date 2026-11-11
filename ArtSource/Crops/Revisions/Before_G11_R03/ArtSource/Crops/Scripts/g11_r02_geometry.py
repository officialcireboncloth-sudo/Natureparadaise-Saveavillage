"""R02 tulip: rounded closed elliptical tepals and individually curved sheathing leaves."""
import math
from mathutils import Vector
import plant_blender_common as C
import g11_geometry as B
from g08_geometry import path_tube
from g09_geometry import Frame
TAU=math.tau

def bezier(points,t):
 a,b,c,d=map(Vector,points);return a*(1-t)**3+3*b*(1-t)**2*t+3*c*(1-t)*t*t+d*t**3

def leaf(points,width,index,roll0=0,roll1=0,flat=False):
 rows,cols=37,11;verts=[];uv=[];faces=[];p0,p1,p2,p3=map(Vector,points)
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;center=bezier(points,t)
   tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized()
   n=Vector((0,0,1)) if flat else Vector((-.10 if index==0 else .12,-.98,.18));n-=tangent*n.dot(tangent);n.normalize();u=tangent.cross(n).normalized()
   roll=roll0+(roll1-roll0)*t**1.3;u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
   profile=.001+math.sin(math.pi*t)**.57*(1.24-.56*t)
   # Broad lower blade, narrow sheathing base and a gently twisting pointed tip.
   profile+=.115*(1-t)**12
   for j in range(cols):
    q=j/(cols-1)*2-1;fold=width*(.075*q*q-.020*(1-q*q))*math.sin(math.pi*t)
    half=.00010+.00145*math.sin(math.pi*t)**.6*max(0,1-q*q)**.6
    verts.append(center+u*(width*.5*profile*q)+n*(fold+side*half));uv.append(((q+1)/2,t))
 nverts=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.extend([face,tuple(v+nverts for v in reversed(face))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,k in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((k,k+nverts,b+nverts,b))
 ob=C.mesh('Tulip R02 curved sheathing leaf',verts,faces,'TulipLeaf',uv);ob['ReferenceLeaf']='Tulip';ob['LeafIndex']=index;ob['CurveControlPoints']=[list(p) for p in map(Vector,points)];ob['LeafRollRadians']=[roll0,roll1];ob['ClosedLeaf']=True;return ob

def tepal(f,angle,index):
 rows,cols=37,13;verts=[];uv=[];faces=[];inner=index>=3
 height=.350 if inner else .334;radius=.154 if inner else .176
 def meridian(t):return .012+radius*math.sin(math.pi*.5*t)**.64-.008*t**4,height*t
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;r,z=meridian(t)
   profile=.001+math.sin(math.pi*t)**.49*(.50+.50*t**.35)
   halfangle=(1.20 if inner else 1.24)*profile
   ta=max(0,t-.0001);tb=min(1,t+.0001);ra,za=meridian(ta);rb,zb=meridian(tb);dr,dz=rb-ra,zb-za;ln=math.hypot(dr,dz);nr,nz=dz/ln,-dr/ln
   for j in range(cols):
    q=j/(cols-1)*2-1;a=angle+q*halfangle+.020*math.sin(math.pi*t)*math.sin(index*1.9)
    rr=r*(1-.018*q*q*math.sin(math.pi*t));zz=z+.0035*math.sin(q*math.pi)*math.sin(math.pi*t)
    half=.00006+.0041*math.sin(math.pi*t)**.65*max(0,1-q*q)**.6
    rr+=side*half*nr;zz+=side*half*nz
    verts.append(f.p(rr*math.cos(a),rr*math.sin(a),zz));uv.append(((q+1)/2,t))
 n=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.extend([face,tuple(v+n for v in reversed(face))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,k in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((k,k+n,b+n,b))
 ob=C.mesh('Tulip R02 closed oval tepal',verts,faces,'TulipPetal',uv);ob['PetalKind']='Tulip';ob['ClosedPetal']=True;ob['RoundedTip']=True;ob['EllipticalOutline']=True;ob['TepalLayer']='inner' if inner else 'outer';return ob

def flower(base,normal):
 f=Frame(base,normal);before=set(C.current.objects);attach=f.p(0,0,-.015)
 neck=path_tube('Tulip R02 flower neck',[attach,f.p(0,0,.012)],.011,'SepalGreen',sides=10,taper=-.20);neck['JointRole']='head';neck['JointId']='TulipHead';neck['JointPoint']=list(attach)
 for layer in range(2):
  for j in range(3):tepal(f,TAU*j/3+layer*math.pi/3-math.pi/2+[0,.018,-.014][j],j+layer*3)
 C.sphere('Tulip closed pink basal receptacle',f.p(0,0,.020),(.035,.035,.030),'TulipPetal',3)
 for j in range(6):
  a=TAU*j/6;p=f.p(.016*math.cos(a),.016*math.sin(a),.059)
  path_tube('Tulip R02 filament',[f.p(.010*math.cos(a),.010*math.sin(a),.020),p],.0017,'CutStem',sides=6,taper=.1);C.sphere('Tulip R02 anther',p,(.004,.004,.009),'RootCream',2)
 path_tube('Tulip R02 pistil',[f.p(0,0,.020),f.p(0,0,.073)],.004,'CutStem',sides=8,taper=.1)
 for ob in set(C.current.objects)-before:ob['FlowerKind']='Tulip';ob['FlowerIndex']=0
 neck['OpenFlower']='Tulip';return attach

def leaves(stage):
 if stage=='Young':
  leaf([(0,.006,.012),(-.02,.025,.15),(-.105,.040,.32),(-.225,.045,.47)],.155,0,-.18,.30)
  leaf([(.008,.012,.012),(.025,.075,.18),(.080,.110,.40),(.150,.100,.58)],.156,1,.20,-.22)
  leaf([(.005,-.012,.010),(.035,-.055,.09),(.110,-.085,.20),(.225,-.070,.30)],.155,2,.16,-.16)
 else:
  leaf([(-.008,.006,.012),(-.027,.025,.19),(-.130,.055,.445),(-.252,.048,.615)],.171,0,-.13,.40)
  leaf([(.011,-.013,.010),(.032,-.052,.16),(.145,-.065,.365),(.282,-.018,.540)],.186,1,.22,-.27)

def build(kind,stage):
 if stage=='Young':leaves(stage)
 elif stage in ['BudForming','Ready']:
  leaves(stage);normal=(.015,-.025,1);base=(.003,0,.600)
  if stage=='BudForming':
   end=B.flower(base,normal,True)
   for ob in C.parent.children_recursive:
    if ob.get('PetalKind')=='Tulip':
     for v in ob.data.vertices:v.co=Vector(base)+(v.co-Vector(base))*1.15
  else:end=flower(base,normal)
  B.stem((0,0,.012),end,normal)
 elif stage=='Harvest':
  normal=Vector((-.90,-.16,.42)).normalized();base=Vector((-.020,0,.020));end=flower(base,normal);start=(.490,.045,-.045);B.stem(start,end,normal,.010)
  leaf([(.245,.020,-.018),(.170,-.020,-.008),(.022,-.140,.008),(-.090,-.190,-.015)],.133,0,.08,-.22,True)
  C.sphere('Tulip R02 cut stalk end',start,(.010,.010,.004),'CutStem',2)['HarvestCut']=True
