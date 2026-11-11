"""A true six-tepal tulip cup, tapered bud and broad arching strap leaves."""
import math
from mathutils import Vector
import plant_blender_common as C
from g08_geometry import path_tube
from g09_geometry import Frame
TAU=math.tau

def leaf(base,angle,length,width,lift=1.,spread=.52,curl=-.12,roll=0,shoot=False,flat=False):
 base=Vector(base);a=math.radians(angle);d=Vector((math.cos(a),math.sin(a),0));side=Vector((-d.y,d.x,0));verts=[];uv=[];faces=[];rows,cols=33,9
 for i in range(rows):
  t=i/(rows-1);profile=max(.001,math.sin(math.pi*t)**(.69 if shoot else .58))*(1.12-.24*t)
  center=base+d*(length*(spread*t-.09*t*t))+Vector((0,0,length*(lift*t+curl*t*t)))
  if flat:center=base+d*(length*t)+Vector((0,0,.035*math.sin(math.pi*t)))
  tangent=(d*(spread-.18*t)+Vector((0,0,lift+2*curl*t))).normalized() if not flat else d
  normal=Vector((0,0,1)) if flat else Vector((0,-.92,.30));normal-=tangent*normal.dot(tangent);normal.normalize();across=tangent.cross(normal).normalized()
  for j in range(cols):
   q=j/(cols-1)*2-1;fold=width*(.24*q*q-.065*math.exp(-q*q/.06))*math.sin(math.pi*t)
   if shoot:fold+=width*.45*q*q*math.sin(math.pi*t)
   p=center+across*(q*width*.5*profile)+normal*(fold+roll*width*q*t)
   verts.append(p);uv.append(((q+1)/2,t))
 for i in range(rows-1):
  for j in range(cols-1):k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 ob=C.mesh('Tulip rolled shoot' if shoot else 'Tulip broad cupped strap leaf',verts,faces,'TulipLeaf',uv);m=ob.modifiers.new('Fleshy leaf edge','SOLIDIFY');m.thickness=.003 if not shoot else .002;m.offset=0;ob['ReferenceLeaf']='Tulip';ob['TulipShoot']=shoot;return ob

def bulb():
 verts=[];uv=[];faces=[];rows,sides=37,40
 for i in range(rows):
  t=i/(rows-1);r=.001+.135*math.sin(math.pi*t)**.67*(1-.79*t**1.30)
  for j in range(sides+1):
   a=TAU*j/sides;rr=r*(1+.025*math.cos(a*11+t*.9));verts.append((rr*math.cos(a),rr*math.sin(a),.022+.26*t));uv.append((j/sides,t))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((rows-1)*(sides+1)+j for j in range(sides))]);ob=C.mesh('Tulip papery pear bulb',verts,faces,'BulbSkin',uv);ob['TulipBulb']=True
 C.sphere('Tulip basal plate',(0,0,.023),(.034,.034,.009),'RootCream',2)
 for j in range(7):
  a=TAU*j/7;p=Vector((.025*math.cos(a),.025*math.sin(a),.018));path_tube('Tulip short basal root',[p,p+Vector((.014*math.cos(a),.014*math.sin(a),-.009)),p+Vector((.026*math.cos(a),.026*math.sin(a),-.012))],.003,'RootCream',sides=6,taper=.8)

def tepal(f,angle,index,bud=False):
 rows,cols=29,11;verts=[];uv=[];faces=[];inner=index>=3
 height=(.254 if bud else .286 if inner else .265)
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5
   if bud:r=.001+.075*math.sin(math.pi*t)**.76;half=.58*math.sin(math.pi*t)**.13+.005
   else:r=.008+(.137 if inner else .153)*math.sin(t*math.pi*.76)**.66;half=(.99 if inner else 1.04)*(.015+.985*math.sin(t*math.pi*.50)**.52)
   for j in range(cols):
    q=j/(cols-1)*2-1;a=angle+q*half
    zz=height*t-(0 if bud else .046)*q*q*t**4
    rr=r*(1-(.025 if bud else .065)*q*q*math.sin(math.pi*t))
    thickness=.0007+(.0023 if bud else .0033)*math.sin(math.pi*t)**.5*max(0,1-q*q)**.52
    rr+=side*thickness
    verts.append(f.p(rr*math.cos(a),rr*math.sin(a),zz));uv.append(((q+1)/2,t))
 n=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.extend([face,tuple(v+n for v in reversed(face))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,k in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((k,k+n,b+n,b))
 ob=C.mesh('Tulip '+('folded bud' if bud else 'rounded cup')+' tepal',verts,faces,'TulipBud' if bud else 'TulipPetal',uv);ob['PetalKind']='Tulip';ob['ClosedPetal']=True;ob['TepalLayer']='inner' if inner else 'outer';return ob

def flower(base,normal,bud=False):
 f=Frame(base,normal);before=set(C.current.objects);attachment=f.p(0,0,-.015)
 neck=path_tube('Tulip green flower neck',[attachment,f.p(0,0,.012)],.011,'SepalGreen',sides=10,taper=-.20);neck['JointRole']='head';neck['JointId']='TulipHead';neck['JointPoint']=list(attachment)
 for layer in range(2):
  for j in range(3):tepal(f,TAU*j/3+layer*math.pi/3-math.pi/2,j+layer*3,bud)
 if not bud:
  core=C.sphere('Tulip hidden cream receptacle',f.p(0,0,.018),(.021,.021,.013),'CutStem',2)
  for j in range(6):
   a=TAU*j/6;p=f.p(.016*math.cos(a),.016*math.sin(a),.060)
   path_tube('Tulip inner filament',[f.p(.010*math.cos(a),.010*math.sin(a),.020),p],.0018,'CutStem',sides=6,taper=.15)
   C.sphere('Tulip inner anther',p,(.004,.004,.010),'RootCream',2)
  path_tube('Tulip short central pistil',[f.p(0,0,.020),f.p(0,0,.074)],.004,'CutStem',sides=8,taper=.15)
 for ob in set(C.current.objects)-before:ob['FlowerKind']='Tulip';ob['FlowerIndex']=0
 neck['ClosedBud' if bud else 'OpenFlower']='Tulip';return attachment

def stem(start,end,normal,radius=.011):
 start,end,n=Vector(start),Vector(end),Vector(normal).normalized();a,b,c,d=start,start.lerp(end,.40)+Vector((0,0,.012)),end-n*.060,end
 points=[a*(1-t)**3+b*3*(1-t)**2*t+c*3*(1-t)*t*t+d*t**3 for t in [i/16 for i in range(17)]];points[-2]=end-n*.012
 ob=path_tube('Tulip continuous flower stalk',points,radius,'StemGreen',sides=10,taper=.12);ob['JointRole']='stem';ob['JointId']='TulipHead';ob['JointPoint']=list(end)

def build(kind,stage):
 if stage in ['Young','BudForming','Ready','Harvest']:
  from g11_r02_geometry import build as revised_build
  return revised_build(kind,stage)
 if stage in ['Bulb','Sprout']:
  bulb()
  if stage=='Sprout':
   from g11_r03_geometry import shoots
   shoots()
 elif stage=='Young':
  for a,l,w,spread,lift in [(198,.48,.155,.58,1.0),(25,.58,.162,.45,1.0),(315,.34,.160,.62,.98)]:leaf((0,0,.005),a,l,w,lift=lift,spread=spread,curl=-.13)
 elif stage in ['BudForming','Ready']:
  for a,l,w in [(195,.67,.175),(25,.60,.184)]:leaf((0,0,.006),a,l,w,lift=1.08,spread=.58,curl=-.16,roll=.02)
  normal=(.015,-.025,1);base=(.003,0,.743 if stage=='BudForming' else .747);end=flower(base,normal,stage=='BudForming');stem((0,0,.012),end,normal)
 else:
  normal=Vector((-.90,-.16,.42)).normalized();base=Vector((-.020,.0,.110));end=flower(base,normal);start=(.490,.045,.026);stem(start,end,normal,.010)
  leaf((.245,.020,.031),215,.355,.135,flat=True,roll=.015)
  ob=C.sphere('Tulip cut stalk end',start,(.010,.010,.004),'CutStem',2);ob['HarvestCut']=True
