"""Sage lanceolate foliage and stacked lavender whorls in actual 3D space."""
import math,random
from mathutils import Vector
import plant_blender_common as C
from g12_geometry import closed_grid,bezier,sample
from g09_geometry import Frame
from g08_geometry import path_tube
TAU=math.tau

def leaf(origin,angle,elevation,length,width,index,roll=0,slot='LavenderLeaf'):
 origin=Vector(origin);a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));across=Vector((radial.y,-radial.x,0));lift=math.sin(math.radians(elevation));spread=math.cos(math.radians(elevation))
 points=[origin,origin+radial*length*.30*spread+Vector((0,0,length*.36*lift)),origin+radial*length*.78*spread+Vector((0,0,length*.86*lift+.015)),origin+radial*length*spread+Vector((0,0,length*lift-.012))];rows,cols=13,3;verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;center=bezier(points,t);p0,p1,p2,p3=points;tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized();u=(across-tangent*across.dot(tangent)).normalized();n=u.cross(tangent).normalized();r=roll+.28*math.sin(index*1.37)*t;u,n=u*math.cos(r)+n*math.sin(r),-u*math.sin(r)+n*math.cos(r)
   profile=.001+math.sin(math.pi*t)**.48*(1.04-.10*t)
   for j in range(cols):
    q=j/(cols-1)*2-1;rib=.0015*math.exp(-q*q/.09)*math.sin(math.pi*t);cup=width*.09*q*q*math.sin(math.pi*t);half=.00022+.0019*math.sin(math.pi*t)**.7*max(0,1-q*q)**.6
    verts.append(center+u*width*.5*profile*q+n*(rib+cup+side*half));uv.append(((q+1)/2,.02+.96*t))
 ob=closed_grid('Lavender spatial sage blade',verts,uv,rows,cols,slot);ob['ReferenceLeaf']='Lavender';ob['LeafIndex']=index;ob['SpatialLeaf']=True;ob['LeafFrame']='radial growth and world up';return ob

def pod(base,axis,length,width,depth,index,tier,bud=False,slot='LavenderPetal',mark=True):
 f=Frame(base,axis);rows,sides=5,8;verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);profile=.012+math.sin(math.pi*t)**.58*(1.13-.20*t)
  for j in range(sides+1):
   a=TAU*j/sides;ridge=1+.035*math.cos(a*3)*math.sin(math.pi*t);verts.append(f.p(width*.5*profile*math.cos(a)*ridge,depth*.5*profile*math.sin(a)*ridge,length*t));uv.append((.05+.90*j/sides,.04+.92*t))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((rows-1)*(sides+1)+j for j in range(sides))]);ob=C.mesh('Lavender '+('tapered bud' if bud else 'plump whorled floret'),verts,faces,slot,uv)
 if mark:ob['LavenderFloret']=True;ob['SpikeIndex']=index;ob['WhorlTier']=tier;ob['BudFloret']=bud
 return ob

def flowerhead(base,axis,index,size=1,bud=False):
 f=Frame(base,axis);n=f.n;attach=Vector(base);collar=path_tube('Lavender connected calyx neck',[attach,attach+n*.018*size],.009*size,'StemSage',sides=9,taper=.05);collar['JointRole']='head';collar['JointId']='Spike_'+str(index);collar['JointPoint']=list(attach);collar['ClosedBud' if bud else 'OpenFlower']='Lavender';collar['SpikeIndex']=index
 for j in range(4):
  a=TAU*j/4;radial=f.x*math.cos(a)+f.y*math.sin(a);pod(attach+n*.004*size+radial*.007*size,radial*.50+n*.87,.042*size,.019*size,.013*size,index,-1,slot='LavenderLeaf',mark=False)
 if bud:
  for j in range(3):
   a=TAU*j/3;radial=f.x*math.cos(a)+f.y*math.sin(a);pod(attach+n*.023*size+radial*.009*size,radial*.20+n,.068*size,.029*size,.024*size,index,0,True)
  return attach
 tiers=6
 for tier in range(tiers):
  t=tier/(tiers-1);ring=.017*size*(1-.32*t);z=(.020+tier*.028)*size
  for j in range(5):
   a=TAU*j/5+tier*.58+index*.17;radial=f.x*math.cos(a)+f.y*math.sin(a);direction=(radial*(.58-.15*t)+n*.82).normalized();start=attach+n*z+radial*ring
   pod(start,direction,(.066-.014*t)*size,(.032-.006*t)*size,.025*size,index,tier)
 for j in range(3):
  a=TAU*j/3+.2;radial=f.x*math.cos(a)+f.y*math.sin(a);pod(attach+n*.190*size+radial*.006*size,n+radial*.17,.052*size,.024*size,.020*size,index,6)
 return attach

SPIKES=[(-.35,.02,.46,.88),(-.23,.18,.66,1.02),(-.10,.24,.63,.96),(.025,.10,.78,1.07),(.16,-.08,.59,.94),(.29,.15,.68,1.00),(.39,.0,.56,.94),(.44,-.13,.37,.83),(-.04,-.21,.38,.81)]
def flowering_stem(end,axis,index,start=(0,0,.02),radius=.006):
 end=Vector(end);start=Vector(start);axis=Vector(axis).normalized();pts=sample([start,start.lerp(end,.32)+Vector((0,0,.055)),end-axis*.10,end],27);pts[-2]=end-axis*.009
 ob=path_tube('Lavender continuous flowering stem',pts,radius,'StemSage',sides=9,taper=.22);ob['JointRole']='stem';ob['JointId']='Spike_'+str(index);ob['JointPoint']=list(end);return pts

def foliage(stage):
 if stage=='Sprout':
  path_tube('Lavender short sprout stem',[(0,0,.008),(0,0,.075),(0,0,.102)],.009,'StemSage',sides=9,taper=.2)
  leaf((0,0,.08),190,23,.13,.061,0,.18);leaf((0,0,.086),25,36,.13,.060,1,-.25);return
 count=3 if stage=='Young' else 9 if stage=='BudForming' else 12;scale=.73 if stage=='Young' else .98 if stage=='BudForming' else 1.12;idx=0
 for j in range(count):
  a=TAU*j/count+.36;radial=Vector((math.cos(a),math.sin(a),0));base=radial*(.035+.025*(j%3));base.z=.012;top=base+radial*(.045*scale)+Vector((0,0,(.20+.065*(.5+.5*math.sin(j*2.3)))*scale));curve=[base,base+Vector((0,0,.08)),top-radial*.018-Vector((0,0,.05)),top];path_tube('Lavender leafy basal branch',sample(curve,17),.007*scale,'StemSage',sides=8,taper=.42)
  for node,t in enumerate([.23,.52,.80]):
   position=bezier(curve,t)
   for sign in [-1,1]:
    angle=math.degrees(a)+sign*(65+node*8)+math.sin(j*1.5)*14;length=(.18+.05*math.sin(j*1.73+node))*(1-.15*node)*scale;leaf(position,angle,24+node*18+7*math.sin(j),length,(.041-.004*node)*scale,idx,.30*math.sin(j+node+sign));idx+=1
 rosette=6 if stage=='Young' else 14 if stage=='BudForming' else 18
 for j in range(rosette):
  a=360*j/rosette+12;leaf((.013*math.cos(math.radians(a)),.013*math.sin(math.radians(a)),.022),a,10+18*(j%3)/2,(.23+.035*math.sin(j*2.1))*scale,.043*scale,idx,.45*math.sin(j*1.21));idx+=1

def build(kind,stage):
 if stage=='Seed':
  for i,(x,y,a) in enumerate([(-.034,.012,.40),(.030,.022,-.45),(.004,-.029,.18)]):
   ob=C.sphere('Lavender brown oblong seed',(x,y,.008),(.021,.010,.006),'SeedBrown',2);ob.rotation_euler=(0,.12,a);ob['LavenderSeed']=True
 elif stage=='Harvest':
  heads=[((.11,.11,.070),(.89,.16,.42),.93,(-.39,.055,.018)),((.29,.018,.080),(.92,.08,.38),.99,(-.36,.018,.021)),((.38,-.10,.085),(.91,-.02,.42),.91,(-.35,-.018,.022)),((.68,-.33,.070),(.93,.1,.35),.90,(.18,-.37,.020))]
  for i,(end,axis,size,start) in enumerate(heads):
   flowerhead(end,axis,i,size);curve=flowering_stem(end,axis,i,start,.005)
   if i<3:
    for k,t in enumerate([.27,.46]):leaf(curve[int(t*(len(curve)-1))],165+55*k+i*12,8,.15,.028,i*2+k,.18*math.sin(i+k))
   C.sphere('Lavender pale cut stem end',start,(.005,.005,.0025),'StemSage',1)['HarvestCut']=True
 else:
  foliage(stage)
  if stage in ['BudForming','Ready']:
   specs=SPIKES[:5] if stage=='BudForming' else SPIKES
   for i,(x,y,z,size) in enumerate(specs):
    if stage=='BudForming':x*=.87;y*=.87;z*=.84
    end=Vector((x,y,z));axis=Vector((x*1.4,y*.6,1)).normalized();flowerhead(end,axis,i,size,stage=='BudForming');start=(x*.13,y*.18,.023);curve=flowering_stem(end,axis,i,start)
    if stage=='Ready':
     for side in [-1,1]:leaf(curve[10],math.degrees(math.atan2(y,x))+side*76,46,.14,.025,100+i*2+(side+1)//2,.22*side)
