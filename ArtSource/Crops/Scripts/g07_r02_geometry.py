"""Registered, inflated leaves and a compact clump with thin organic stems."""
import math,json
import bpy
from mathutils import Vector
import plant_blender_common as C
import g07_geometry as B
from g07_config import GENERATED
DATA=json.loads((GENERATED/'LeafContours.json').read_text())
original_stalk=B.stalk

def stalk(name,points,r=.007,hollow=False,solid_cut=False,sides=7):
 return original_stalk(name,points,r,hollow,solid_cut,sides)

def collar(center,direction,r=.009):
 p=Vector(center);d=Vector(direction).normalized()
 o=B.swept('Water spinach subtle living leaf scar',[p-d*.0025,p,p+d*.0025],[r,r*1.035,r],'StemPale',8);o['StemNode']=True;return o

def leaf_profile(kind,t):
 p=DATA[kind]['profile'];z=t*(len(p)-1);i=min(len(p)-2,int(z));f=z-i
 return [a+(b-a)*f for a,b in zip(p[i],p[i+1])]

def blade(kind,attach,direction,length,width,slot=None,roll=0,flat=False):
 phase=C.parent.get('Phase','')
 young=slot=='LeafYoung' or phase=='Sprout'
 sprite='Spinach' if phase=='Sprout' else kind
 if young:slot='LeafYoung'+sprite
 else:slot=slot or ('LeafOrchid' if kind=='Spinach' else 'LeafWaterSpinach')
 data=DATA[sprite];d=Vector(direction).normalized();hint=Vector((0,-.46,.89)) if kind=='Spinach' else Vector((0,-.25,.97))
 if flat:hint=Vector((0,0,1))
 n=hint-d*hint.dot(d)
 if n.length<.01:n=Vector((1,0,0))-d*d.x
 n.normalize();u=d.cross(n).normalized();u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
 rows,cols=(33,9) if kind=='Spinach' else (25,7)
 verts=[];coords=[];faces=[];origin=Vector(attach)
 for i in range(rows):
  t=i/(rows-1);_,left,right,v=leaf_profile(sprite,t)
  for j in range(cols):
   q=j/(cols-1)*2-1;s=left+(right-left)*(q+1)*.5;x=(s-data['baseCenter'])/data['width']
   if kind=='Spinach':
    bow=.18*math.sin(math.pi*t)-.08*t*t-.075*q*q*math.sin(math.pi*t)
    bow-=.026*math.exp(-q*q/.026)*math.sin(math.pi*t)
    bow+=.030*(math.sin(q*math.pi)**2)*(math.sin(t*math.pi*6)**2)*math.sin(math.pi*t)
   else:
    bow=.13*math.sin(math.pi*t)-.24*t*t+.065*abs(q)*math.sin(math.pi*t)+.025*q*math.sin(t*math.pi*2+roll)
   verts.append(origin+d*(length*t)+u*(x*width)+n*(bow*length));coords.append((s,v))
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 o=C.mesh(kind+' registered quilted curved blade',verts,faces,slot,coords)
 solid=o.modifiers.new('Fine living leaf edge','SOLIDIFY');solid.thickness=.0017;solid.offset=0
 o['ReferenceLeaf']=kind;o['RegisteredLeafSource']=sprite;o['LeafAzimuthDeg']=math.degrees(math.atan2(d.y,d.x))%360;o['LeafNormal']=list(n)
 return o

def petiole_leaf(kind,origin,angle,elevation,length,width,petiole=.05,slot=None,roll=0):
 a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));p=Vector(origin);d=B.direction(angle,elevation)
 attach=p+radial*petiole+Vector((0,0,petiole*.7))
 stalk(kind+' supple curved petiole',[p,p+radial*petiole*.28+Vector((0,0,petiole*.5)),attach-radial*.008,attach+d*.018],.0042 if kind=='WaterSpinach' else .007)
 return blade(kind,attach,d,length,width,slot,roll)

def spinach(stage):
 if stage=='Seed':B.seeds('Spinach');return
 if stage=='Sprout':B.sprout('Spinach');return
 if stage=='Harvest':B.spinach_bunch();return
 if stage=='Young':specs=[(210,34,.25,.17,.05),(85,72,.265,.17,.06),(340,48,.25,.16,.05),(280,10,.18,.13,.04)]
 elif stage=='LeafGrowth':specs=[(205,34,.30,.23,.06),(55,70,.34,.24,.07),(330,43,.32,.24,.06),(140,25,.30,.24,.05),(280,60,.255,.19,.05),(15,15,.28,.23,.05)]
 else:
  specs=[]
  for i,a in enumerate([200,255,315,25,80,140]):specs.append((a,[22,10,26,22,32,18][i],.32+(i%3)*.018,.27+(i%2)*.018,.047))
  for i,a in enumerate([210,280,340,40,100,155]):specs.append((a,[53,58,52,63,60,47][i],.38+(i%2)*.03,.275+(i%2)*.015,.054))
  specs.extend([(70,79,.47,.285,.055),(155,72,.45,.28,.052),(325,69,.25,.155,.039),(270,75,.23,.14,.038)])
 for i,(a,e,l,w,p) in enumerate(specs):
  origin=(.012*math.cos(i*B.GOLD),.012*math.sin(i*B.GOLD),.004+(i%3)*.004)
  petiole_leaf('Spinach',origin,a,e,l,w,p,roll=.22*math.sin(i*1.71))

def stem_path(angle,height,radius,index):
 a=math.radians(angle);r1=Vector((math.cos(a+.42),math.sin(a+.42),0));r2=Vector((math.cos(a-.23),math.sin(a-.23),0));r3=Vector((math.cos(a+.12),math.sin(a+.12),0))
 return [r1*.028+Vector((0,0,.002)),r1*radius*.35+Vector((0,0,height*.30)),r2*radius*.94+Vector((0,0,height*.65)),r3*radius+Vector((0,0,height))]

def water_spinach(stage):
 if stage=='Seed':B.seeds('WaterSpinach');return
 if stage=='Sprout':B.sprout('WaterSpinach');return
 if stage=='Harvest':water_bunch();return
 if stage=='Young':
  points=stem_path(10,.40,.034,0);stalk('Young water spinach main stem',points,.0075)
  for t in [.34,.72]:collar(B.point_at(points,t),(0,0,1),.0075*(1-.55*t))
  for a,e,l,w,t in [(205,-18,.19,.095,.34),(335,-22,.19,.095,.34),(145,45,.245,.11,.72),(35,55,.245,.11,.72)]:petiole_leaf('WaterSpinach',B.point_at(points,t),a,e,l,w,.027,roll=.12*math.sin(a))
  p=B.point_at(points,1);petiole_leaf('WaterSpinach',p,285,80,.16,.06,.016,'LeafYoung');return
 count=4 if stage=='LeafGrowth' else 9
 heights=[.52,.61,.55,.49] if count==4 else [.68,.77,.65,.73,.60,.67,.70,.57,.63]
 for i in range(count):
  a=math.degrees(i*B.GOLD)+21;radius=(.13 if count==4 else .19)*(1+.13*math.sin(i*1.8));points=stem_path(a,heights[i],radius,i);r=.0065 if count==4 else .0075
  stalk('Water spinach soft arching main stem',points,r)
  ts=[.31,.57,.80] if count==4 else [.24,.43,.60,.76,.89]
  for j,t in enumerate(ts):
   p=B.point_at(points,t);d=B.point_at(points,min(1,t+.02))-B.point_at(points,max(0,t-.02));collar(p,d,r*(1-.55*t))
   az=a+j*137.5+(.23*i)*180/math.pi;e=[5,28,15,42,56][j]+8*math.sin(i*.9+j)
   if j==len(ts)-2:
    shoot=B.direction(az,32)*.075;end=p+shoot;o=stalk('Water spinach thin curved axillary branch',[p,p+shoot*.25+Vector((0,0,.016)),end-shoot*.18,end],.0038);o['AxillaryBranch']=True;collar(end,shoot,.004);p=end
   petiole_leaf('WaterSpinach',p,az,e,.26+(j%2)*.025,.115+(j%2)*.012,.026,roll=.20*math.sin(i*1.9+j))
  p=B.point_at(points,1);petiole_leaf('WaterSpinach',p,a+42,64,.18 if i%3==0 else .23,.065 if i%3==0 else .105,.018,'LeafYoung' if i%3==0 else None,roll=.13)

def water_bunch(single=False):
 count=1 if single else 9
 for i in range(count):
  x=(i%3-1)*.032 if not single else 0;z=.019+(i//3)*.025;start=Vector((x,-.30-(i%2)*.025,z));end=Vector((x+.05*math.sin(i*1.4),.24+(i%3)*.024,z+.025));points=[start,start+Vector((.01,.14,.014)),end-Vector((.015,.12,.004)),end]
  stalk('Water spinach hollow harvested stem',points,.011,hollow=True,sides=10)
  for j,t in enumerate([.54,.85] if single else [.42,.68,.94]):
   p=B.point_at(points,t);collar(p,(0,1,.04),.011*(1-.15*t));side=-1 if (i+j)%2 else 1;attach=p+Vector((side*.02,.014,.012))
   stalk('Harvest water spinach curved petiole',[p,p+Vector((side*.01,.005,.014)),attach,attach+Vector((0,.017,0))],.0035)
   blade('WaterSpinach',attach,(side*(.65 if j!=2 else .20),1,.07),.26 if not single else .29,.11,roll=side*.14,flat=True)

# Reuse seed and cut-stalk construction; all leaves and living nodes use R02 geometry.
B.blade=blade;B.collar=collar;B.petiole_leaf=petiole_leaf
spinach_bunch=B.spinach_bunch
