"""Reference Raspberry with compound spatial foliage and a genuinely hollow harvest fruit."""
import math
from mathutils import Vector
import plant_blender_common as C
import g01_geometry as B
from g09_geometry import Frame
from g12_geometry import sample
from g08_geometry import path_tube
TAU=math.tau
def leaf(origin,angle,elevation,length,width,index,roll=0):
 # Keep the accepted curved serration topology and the Raspberry's own detailed leaf surface.
 prior=C.slots.get('BlackberryLeaf');C.slots['BlackberryLeaf']=C.slots['RaspberryLeaf']
 ob=B.leaf(origin,angle,elevation,length,width,index,'Blackberry',roll)
 if prior:C.slots['BlackberryLeaf']=prior
 else:del C.slots['BlackberryLeaf']
 ob.name='Raspberry curved serrated leaflet';ob['ReferenceLeaf']='Raspberry';return ob
def raspberry(center,axis,size,index,green=False,hollow=False):
 f=Frame(center,axis);slot='BerryGreen' if green else 'RaspberryDrupe';beads=0
 # Wider open receptacle at the stem end, tapering toward the closed bottom.
 for row,(count,z,r) in enumerate([(5,-.071,.020),(8,-.048,.041),(10,-.019,.053),(11,.012,.057),(10,.041,.052),(9,.064,.042)]):
  for j in range(count):
   a=TAU*j/count+(row%2)*.29;pos=f.p(r*math.cos(a)*size,r*math.sin(a)*size,z*size)
   ob=C.sphere('Raspberry individual plump drupelet',pos,(.0195*size,.0195*size,.0205*size),slot,2);ob['RaspberryDrupelet']=True;ob['FruitIndex']=index;ob['DrupeRow']=row;beads+=1
 # Closed thin cup wall, visible below the rim, with a depressed inner floor.
 # It supports the individual beads without placing a solid core across the opening.
 rings=[(.012,-.084),(.041,-.058),(.053,-.018),(.057,.017),(.046,.064),(.029,.064),(.033,.014),(.022,-.035),(.009,-.048)]
 verts=[];uv=[];faces=[];n=40
 for r,z in rings:
  for j in range(n):
   a=TAU*j/n;verts.append(f.p(r*math.cos(a)*size,r*math.sin(a)*size,z*size));uv.append((j/n,(z+.09)/.17))
 for i in range(len(rings)-1):
  for j in range(n):a=i*n+j;b=i*n+(j+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(range(n-1,-1,-1)),tuple((len(rings)-1)*n+j for j in range(n))])
 ob=C.mesh('Raspberry recessed hollow cup wall',verts,faces,slot,uv);ob['BerryKind']='Raspberry';ob['FruitIndex']=index;ob['Drupelets']=beads;ob['HollowDepth']=.112*size;ob['DetachedHollow']=hollow;ob['Unripe']=green
 if hollow:return None,None
 B.sepals(Frame(f.p(0,0,.084*size),f.n),.034*size,'Raspberry',index)
 attach=f.p(0,0,.109*size);neck=path_tube('Raspberry attached calyx neck',[attach,f.p(0,0,.080*size)],.006*size,'StemGreen',sides=9,taper=.1);neck['JointRole']='head';neck['JointId']='Fruit_'+str(index);neck['JointPoint']=list(attach);return attach,-f.n
def shrub(stage):
 if stage=='Sprout':
  path_tube('Raspberry sprout trunk',[(0,0,.008),(0,0,.12),(0,0,.18)],.014,'BarkRed',sides=10,taper=.35)
  for i,(a,e,l,w) in enumerate([(180,30,.24,.12),(25,48,.25,.12),(285,15,.17,.085)]):leaf((0,0,.15),a,e,l,w,i,.25*math.sin(i+1))
  return []
 factor=.63 if stage=='Young' else .91 if stage=='Flowering' else 1.;anchors=[];idx=0
 for j,(x,y,h) in enumerate([(-.23,.04,.67),(0,.11,.82),(.24,.0,.72)]):
  base=Vector(((j-1)*.035,0,.009));end=Vector((x,y,h))*factor;curve=[base,base+Vector((0,0,.24))*factor,end-Vector((x*.4,y*.4,.20))*factor,end];path_tube('Raspberry branching woody cane',sample(curve,25),.024*factor,'BarkRed',sides=10,taper=.68)
  for node,t in enumerate([.38,.72] if stage=='Young' else [.23,.43,.64,.84]):
   pos=B.bezier(curve,t);a=math.degrees(math.atan2(y,x))+(-1 if node%2 else 1)*(42+15*j);d=Vector((math.cos(math.radians(a)),math.sin(math.radians(a)),.27)).normalized();tip=pos+d*.15*factor;path_tube('Raspberry compound leaf petiole',[pos,pos.lerp(tip,.5)+Vector((0,0,.015)),tip],.009*factor,'BarkRed',sides=8,taper=.6)
   for k,angle in enumerate([a,a-61,a+61]):
    origin=tip if k==0 else pos.lerp(tip,.67);leaf(origin,angle,(30+12*math.sin(idx*1.7)) if k else 48,.28*factor if k==0 else .21*factor,.147*factor if k==0 else .115*factor,idx,.34*math.sin(idx*1.9));idx+=1
   anchors.append(tip)
   start=pos+d*.012;C.tube('Raspberry fine cane prickle',start,start+d*.022*factor+Vector((0,0,.010)),.0045*factor,'BarkRed',r2=.0004)['BerryThorn']=True
  leaf(end,[150,65,30][j],68,.25*factor,.13*factor,idx,.18*math.sin(j));idx+=1
 if stage!='Young':
  for j in range(5):leaf((.06*math.cos(j*TAU/5),.06*math.sin(j*TAU/5),.085),j*72+15,23,.26,.14,idx,.35*math.sin(j));idx+=1
 return anchors
FRUIT=[(-.34,-.31,.59,1.0),(-.10,-.39,.51,1.03),(.27,-.30,.67,.98),(.43,-.30,.49,.94),(-.29,-.38,.28,1.02),(.19,-.40,.25,1.06),(.37,-.32,.23,.92)]
def build(kind,stage):
 assert kind=='Raspberry'
 if stage=='Seed':return B.build(kind,stage)
 if stage=='Harvest':
  raspberry((-.13,.06,.12),(0,-.10,1),1.30,0);raspberry((.15,.09,.11),(.1,-.15,1),1.16,1);raspberry((.015,-.16,.10),(0,-.72,.69),1.30,2,hollow=True);return
 anchors=shrub(stage)
 if stage not in ['Flowering','Ready']:return
 def connect(end,n,key):B.pedicel(min(anchors,key=lambda p:(Vector(p)-Vector(end)).length),end,n,key,kind)
 for i,(x,y,z,size) in enumerate(FRUIT if stage=='Ready' else FRUIT[:5]):
  attach,n=raspberry((x,y,z*(1 if stage=='Ready' else .91)),(x*.1,.1,1),size*(1 if stage=='Ready' else .72),i,green=stage=='Flowering');connect(attach,n,'Fruit_'+str(i))
 if stage=='Flowering':
  for i,(x,y,z) in enumerate([(-.24,-.21,.73),(.11,-.20,.83),(.31,-.23,.51),(-.13,-.25,.43)]):
   attach,n=B.flower((x,y,z),(x*.2,-.8,.6),i,kind,.86);connect(attach,n,'Flower_'+str(i))
