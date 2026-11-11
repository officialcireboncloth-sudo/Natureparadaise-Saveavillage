"""Compact asymmetric berry crowns with connected lateral shoots and compound leaf groups."""
import math
from mathutils import Vector
import bpy
import numpy as np
import plant_blender_common as C
import g01_leaf_r03 as L
import g01_geometry as B
import g01_raspberry_geometry as R
from g08_geometry import path_tube
from g12_geometry import sample

def leaf(origin,a,e,l,w,i,kind,roll=0):
 if kind=='Blackberry':w*=1.10
 elif kind=='Raspberry':l*=1.02;w*=.97
 stage=C.parent['Phase'];bounds=[]
 if stage in ['Ready','Flowering']:
  fruit=(BLUE if stage=='Ready' else BLUE[4:7]) if kind=='Blueberry' else (RUBUS if stage=='Ready' else RUBUS[:5])
  for x,y,z,size in fruit:
   ext=np.array([size,size,size*.92]) if kind=='Blueberry' else np.array([.075,.075,.10])*size
   if stage=='Flowering':ext*=.72 if kind=='Blueberry' else .70
   bounds.append((np.array([x,y,z]),ext*1.03))
 best=None
 for da,de,dr in [(0,0,0),(-20,0,0),(20,0,0),(0,18,.15),(0,-18,-.15),(-32,12,.2),(32,12,-.2)]:
  ob=L.create(origin,a+da,e+de,l,w,i,kind,roll+dr);pts=np.array([tuple(v.co) for v in ob.data.vertices]);hits=sum(int(np.sum(np.sum(((pts-center)/ext)**2,axis=1)<1.)) for center,ext in bounds);score=hits*100+abs(da)*.03+abs(de)*.04
  if best is None or score<best[0]:
   if best is not None:bpy.data.objects.remove(best[1],do_unlink=True)
   best=(score,ob)
  else:bpy.data.objects.remove(ob,do_unlink=True)
  if hits==0:break
 ob=best[1];ob['LeafRevision']='R04';ob['LeafCluster']=i//3;ob['FruitEnvelopeSamples']=int(best[0]//100);return ob

def shrub(kind,stage):
 if stage=='Sprout':
  path_tube(kind+' R04 sprout',[(0,0,.008),(.006,0,.095),(.002,0,.14)],.010,'BarkRed',sides=8,taper=.5)
  for i,(a,e,l,w) in enumerate([(170,24,.17,.105),(15,37,.185,.11),(285,6,.12,.075)][:2 if kind=='Blueberry' else 3]):leaf((.003,0,.12),a,e,l,w,i,kind,.25*math.sin(i+1))
  return []
 young=stage=='Young';s=.63 if young else .94 if stage=='Flowering' else 1.;blue=kind=='Blueberry';idx=0;anchors=[];wood_nodes=[]
 # Several curved canes occupy depth as well as width, avoiding a three-pole silhouette.
 canes=[(150,.19,.54),(65,.12,.64),(5,.20,.53),(255,.14,.48)] if blue else [(158,.20,.62),(92,.12,.72),(18,.21,.64),(242,.14,.53),(312,.16,.57)]
 if kind=='Blackberry':canes=[(160,.22,.58),(82,.14,.67),(12,.23,.61),(240,.17,.50),(315,.20,.55)]
 for j,(heading,radius,height) in enumerate(canes):
  angle=math.radians(heading);base=Vector(((j-(len(canes)-1)*.5)*.019,.016*math.sin(j),.009));tip=Vector((math.cos(angle)*radius,math.sin(angle)*radius,height))*s;curve=[base,base+Vector((math.cos(angle)*.025,math.sin(angle)*.025,.19))*s,tip-Vector((math.cos(angle)*.06,math.sin(angle)*.06,.16))*s,tip]
  cane_points=sample(curve,21);path_tube(kind+' R04 curved main cane',cane_points,(.016 if blue else .018)*s,'BarkRed',sides=8,taper=.70);wood_nodes.extend(cane_points)
  for node,t in enumerate([.40,.72] if young else [.22,.43,.64,.84]):
   start=B.bezier(curve,t);a=heading+(-1 if node%2 else 1)*(48+10*(j%3));rad=Vector((math.cos(math.radians(a)),math.sin(math.radians(a)),0));shoot_len=(.10+.028*math.sin(j*1.6+node)) *s;end=start+rad*shoot_len+Vector((0,0,(.036+.020*math.sin(j+node))*s));control=[start,start+rad*shoot_len*.3+Vector((0,0,.018*s)),end-rad*shoot_len*.25,end]
   pts=sample(control,13);path_tube(kind+' R04 leafy lateral shoot',pts,.0065*s,'BarkRed',sides=7,taper=.73);anchors.extend([start,end]);wood_nodes.extend(pts)
   if blue:
    for k,(u,delta) in enumerate([(.50,-46),(1.,29)]):
     origin=B.bezier(control,u);length=(.185+.018*math.sin(idx*1.3))*s;elev=18+18*math.sin(idx*1.83+.2);leaf(origin,a+delta,elev,length,length*.68,idx,kind,.44*math.sin(idx*1.72));idx+=1
   else:
    # True terminal leaflet and two side leaflets share the same short rachis.
    for k,(u,delta,scale) in enumerate([(1.,0,1.),(.58,-58,.76),(.58,58,.76)]):
     origin=B.bezier(control,u);length=(.195+.015*math.sin(idx*1.61))*s*scale;elev=19+18*math.sin(idx*1.67+.4);ob=leaf(origin,a+delta,elev,length,length*.59,idx,kind,.48*math.sin(idx*1.83));ob['CompoundCluster']=str(j)+'_'+str(node);ob['LeafletRole']='Terminal' if k==0 else 'Lateral';idx+=1
    if node%2==0:
     p=start+rad*.009;C.tube(kind+' R04 fine prickle',p,p+rad*.014*s+Vector((0,0,.007*s)),.003*s,'BarkRed',r2=.0003)['BerryThorn']=True
  leaf(tip,heading,42+7*math.sin(j),.19*s,(.13 if blue else .112)*s,idx,kind,.24*math.sin(j));idx+=1;anchors.append(tip)
 # Short inner twigs knit together the canopy; basal leaves do not form a flat skirt.
 if not young:
  for j,(a,z) in enumerate([(215,.13),(325,.16),(85,.28),(170,.36),(15,.43),(260,.51)]):
   desired=Vector((.018*math.cos(j),.015*math.sin(j),z))*s;start=min(wood_nodes,key=lambda p:(p-desired).length);rad=Vector((math.cos(math.radians(a)),math.sin(math.radians(a)),0));end=start+rad*.075*s+Vector((0,0,.025*s));path_tube(kind+' R04 inner canopy twig',[start,start.lerp(end,.5),end],.0045*s,'BarkRed',sides=7,taper=.60)
   for k,da in enumerate([-24,38]):leaf(end,a+da,24+18*math.sin(idx),.18*s,(.12 if blue else .105)*s,idx,kind,.52*math.sin(idx*1.8));idx+=1
   anchors.extend([start,end])
  # Compact upper shoots fill the center of the rounded crown rather than ending
  # in a few isolated upright leaves on bare sticks.
  for j in range(3 if blue else 4):
   a=j*(120 if blue else 90)+30;desired=Vector((0,.025,.54 if blue else .61 if kind=='Blackberry' else .64))*s;start=min(wood_nodes,key=lambda p:(p-desired).length);rad=Vector((math.cos(math.radians(a)),math.sin(math.radians(a)),0));end=start+rad*.052*s+Vector((0,0,.022*s));path_tube(kind+' R04 upper crown shoot',[start,start.lerp(end,.5),end],.004*s,'BarkRed',sides=7,taper=.65)
   for k,da in enumerate([-35,38] if blue else [0,-57,57]):
    origin=end if k==0 else start.lerp(end,.60);ob=leaf(origin,a+da,34+12*math.sin(idx*1.5),(.17 if blue else .175)*s,(.118 if blue else .103)*s,idx,kind,.40*math.sin(idx*1.9))
    if not blue:ob['CompoundCluster']='crown_'+str(j);ob['LeafletRole']='Terminal' if k==0 else 'Lateral'
    idx+=1
   anchors.extend([start,end])
 return anchors

BLUE=[(-.26,-.245,.43,.079),(-.06,-.26,.57,.084),(.19,-.245,.54,.083),(.30,-.20,.38,.076),(.075,-.31,.29,.082),(-.22,-.29,.19,.075),(.29,-.12,.20,.071),(-.22,.07,.50,.065),(.14,.10,.40,.068)]
RUBUS=[(-.285,-.22,.50,.88),(-.09,-.295,.43,.91),(.21,-.22,.55,.91),(.33,-.16,.36,.85),(-.25,-.265,.25,.90),(.12,-.29,.21,.93),(.30,-.19,.19,.84),(-.22,.075,.43,.79)]
def build(kind,stage):
 if stage=='Seed':return B.build(kind,stage)
 if stage=='Harvest':
  return R.build(kind,stage) if kind=='Raspberry' else B.build(kind,stage)
 anchors=shrub(kind,stage)
 if stage not in ['Flowering','Ready']:return
 def connect(attach,n,key):
  # Short connections start on the nearest physical shoot, never at the trunk center.
  start=min(anchors,key=lambda p:(Vector(p)-Vector(attach)).length);B.pedicel(start,attach,n,key,kind,.0045 if kind=='Blueberry' else .0055)
 if kind=='Blueberry':
  fruit=BLUE if stage=='Ready' else BLUE[4:7]
  for i,(x,y,z,r) in enumerate(fruit):
   attach,n=B.blueberry((x,y,z),(.15*x,-.42,.91),r*(1 if stage=='Ready' else .72),i,green=stage=='Flowering');connect(attach,n,'Fruit_'+str(i))
 else:
  fruit=RUBUS if stage=='Ready' else RUBUS[:5]
  for i,(x,y,z,size) in enumerate(fruit):
   axis=(.12*x,.13*math.sin(i),1)
   if kind=='Raspberry':attach,n=R.raspberry((x,y,z),axis,size*(1 if stage=='Ready' else .70),i,green=stage=='Flowering')
   else:attach,n=B.blackberry((x,y,z),axis,size*(1 if stage=='Ready' else .70),i,'BlackberryDrupe' if stage=='Ready' and i!=7 else 'BerryRed' if stage=='Ready' else 'BerryGreen')
   connect(attach,n,'Fruit_'+str(i))
 if stage=='Flowering':
  points=[(-.23,-.18,.54),(.075,-.20,.64),(.27,-.16,.40),(-.11,-.245,.33)]
  for i,p in enumerate(points):
   attach,n=B.flower(p,(-.10,-.40,-.92) if kind=='Blueberry' else (.15,-.78,.61),i,kind,.94 if kind=='Blueberry' else .80);connect(attach,n,'Flower_'+str(i))
