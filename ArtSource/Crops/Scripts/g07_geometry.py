"""Low spinach rosettes and jointed water-spinach clumps from the supplied sheets."""
import math
import bpy
from mathutils import Vector
import plant_blender_common as C
from g06_geometry import bezier,swept

GOLD=math.radians(137.507764)

def stalk(name,points,r=.007,hollow=False,solid_cut=False,sides=7):
 pts=bezier(points,16 if hollow else 7)
 if hollow:pts=list(reversed(pts))
 taper=.55 if 'main stem' in name else .15
 o=swept(name,pts,[r*(1-taper*i/(len(pts)-1)) for i in range(len(pts))],'StemPale',sides,hollow)
 if hollow or solid_cut:
  outer=(len(pts)-1)*sides
  def map_face(face,slot,radial=False):
   a,b,c,d=C.slots[slot]['uvSafe']
   for li in face.loop_indices:
    vertex=o.data.loops[li].vertex_index;j=vertex%(sides+1);theta=j*math.tau/sides
    u,v=(.5+.46*math.cos(theta),.5+.46*math.sin(theta)) if radial else (j/sides,.5)
    o.data.uv_layers.active.data[li].uv=(a+(c-a)*u,b+(d-b)*v)
  if hollow:
   for i,face in enumerate(o.data.polygons):
    if outer+1<=i<outer+1+sides:map_face(face,'CutStem',True)
    elif i>=outer+1+sides:map_face(face,'HollowStem')
   o['HollowCutEnd']=True
  else:
   map_face(o.data.polygons[outer],'CutStem',True);o['SolidCutEnd']=True
 return o

def collar(center,direction,r=.018):
 p=Vector(center);d=Vector(direction).normalized()
 o=swept('Water spinach swollen stem node',[p-d*.004,p,p+d*.004],[r*.97,r*1.16,r*.97],'StemPale',8)
 o['StemNode']=True;return o

def blade(kind,attach,direction,length,width,slot=None,roll=0,flat=False):
 d=Vector(direction).normalized();hint=Vector((0,-.60,.80)) if kind=='Spinach' else Vector((0,-.35,.94))
 if flat:hint=Vector((0,0,1))
 n=hint-d*hint.dot(d)
 if n.length<.01:n=Vector((1,0,0))-d*d.x
 n.normalize();u=d.cross(n).normalized();u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
 rows,cols=(25,7) if kind=='Spinach' else (19,5)
 verts=[];uv=[];faces=[];origin=Vector(attach)
 for i in range(rows):
  t=i/(rows-1)
  if kind=='Spinach':shape=.014+math.sin(math.pi*t)**.52*(.75+.25*t)
  else:shape=.008+math.sin(math.pi*t)**.84*(1-.25*t)
  for j in range(cols):
   q=j/(cols-1)*2-1
   fold=(.10 if kind=='WaterSpinach' else .065)*math.sin(math.pi*t)-(.20 if kind=='WaterSpinach' else .07)*t*t-.09*q*q*math.sin(math.pi*t)
   fold+=.014*math.sin(t*math.pi*4+roll)*q*q*math.sin(math.pi*t)
   fold+=.018*(1-q*q)**3*math.sin(math.pi*t)
   verts.append(origin+d*(length*t)+u*(width*.5*shape*q)+n*(length*fold))
   uv.append(((q+1)*.5,t))
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 o=C.mesh(kind+' curved '+('oval' if kind=='Spinach' else 'lanceolate')+' blade',verts,faces,slot or ('LeafOrchid' if kind=='Spinach' else 'LeafWaterSpinach'),uv)
 smooth=o.modifiers.new('Rounded supple outline','SMOOTH');smooth.factor=.28;smooth.iterations=2
 solid=o.modifiers.new('Thin living leaf edge','SOLIDIFY');solid.thickness=.0015;solid.offset=0
 o['ReferenceLeaf']=kind;o['LeafNormal']=list(n);o['LeafAzimuthDeg']=math.degrees(math.atan2(d.y,d.x))%360
 return o

def direction(angle,elevation):
 a,e=math.radians(angle),math.radians(elevation)
 return Vector((math.cos(a)*math.cos(e),math.sin(a)*math.cos(e),math.sin(e)))

def petiole_leaf(kind,origin,angle,elevation,length,width,petiole=.07,slot=None,roll=0):
 a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));p=Vector(origin)
 attach=p+radial*petiole+Vector((0,0,petiole*.65))
 d=direction(angle,elevation)
 stalk(kind+' curved leaf stalk',[p,p+radial*petiole*.23+Vector((0,0,petiole*.5)),attach-radial*.016,attach+d*.016],.006 if kind=='WaterSpinach' else .008)
 return blade(kind,attach,d,length,width,slot,roll)

def sprout(kind):
 height=.12 if kind=='Spinach' else .15
 stalk(kind+' seedling stem',[(0,0,.004),(0,0,height*.35),(.003,0,height*.78),(0,0,height)],.008)
 for i,(angle,elev) in enumerate([(205,32),(25,39)]):
  petiole_leaf(kind,(0,0,height),angle,elev,.145,.071 if kind=='Spinach' else .065,.025,'LeafYoung',.08*(-1 if i else 1))

def seeds(kind):
 positions=[(-.055,-.015),(.05,-.016),(-.005,.05)] if kind=='Spinach' else [(-.07,-.025),(.003,-.035),(.065,-.02),(-.045,.045),(.035,.05)]
 for i,(x,y) in enumerate(positions):
  o=C.sphere(kind+' '+('rounded tan seed' if kind=='Spinach' else 'angular brown seed'),(x,y,.017),(.025,.021,.014),'SeedTan' if kind=='Spinach' else 'SeedBrown',2 if kind=='Spinach' else 1)
  o.rotation_euler=(i*.21,.13,i*.83)

def spinach(stage):
 if stage=='Seed':seeds('Spinach');return
 if stage=='Sprout':sprout('Spinach');return
 if stage=='Harvest':spinach_bunch();return
 if stage=='Young':
  specs=[(210,32,.25,.155,.073),(85,73,.265,.15,.082),(340,52,.25,.15,.077),(280,5,.175,.12,.066)]
 elif stage=='LeafGrowth':
  specs=[(205,30,.30,.21,.085),(55,70,.34,.215,.11),(330,40,.32,.22,.090),(140,20,.30,.225,.085),(280,60,.255,.16,.085),(15,12,.27,.215,.085)]
 else:
  specs=[]
  for i,a in enumerate([205,260,320,25,80,140]):specs.append((a,[18,12,24,27,33,20][i],.34+(i%3)*.015,.245+(i%2)*.012,.085))
  for i,a in enumerate([215,285,345,60,140]):specs.append((a,[57,66,58,76,51][i],.39+(i%2)*.035,.25+(i%2)*.015,.12))
  specs.extend([(335,79,.24,.14,.09),(155,72,.24,.15,.095)])
 for i,(a,e,l,w,p) in enumerate(specs):
  origin=(.018*math.cos(i*GOLD),.018*math.sin(i*GOLD),.014+(i%3)*.009)
  petiole_leaf('Spinach',origin,a,e,l,w,p,roll=.16*math.sin(i*1.71))

def stem_path(angle,height,radius):
 a=math.radians(angle);v=Vector((math.cos(a),math.sin(a),0))
 return [v*.032+Vector((0,0,.003)),v*(radius*.40)+Vector((0,0,height*.31)),v*(radius*.92)+Vector((0,0,height*.72)),v*radius+Vector((0,0,height))]

def point_at(points,t):
 a,b,c,d=map(Vector,points)
 return (1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d

def water_spinach(stage):
 if stage=='Seed':seeds('WaterSpinach');return
 if stage=='Sprout':sprout('WaterSpinach');return
 if stage=='Harvest':water_bunch();return
 if stage=='Young':
  points=stem_path(10,.45,.012);stalk('Young water spinach main stem',points,.011)
  for t in [.32,.70]:collar(point_at(points,t),(0,0,1),.011*(1-.55*t)*1.06)
  for a,e,l,w,t in [(205,-24,.19,.095,.34),(335,-26,.19,.095,.35),(145,48,.245,.11,.72),(35,55,.245,.11,.74)]:
   petiole_leaf('WaterSpinach',point_at(points,t),a,e,l,w,.034,roll=.10*math.sin(a))
  blade('WaterSpinach',point_at(points,1),direction(285,80),.16,.055,'LeafYoung')
  return
 count=4 if stage=='LeafGrowth' else 7
 heights=[.61,.69,.64,.55] if count==4 else [.77,.85,.77,.81,.69,.73,.66]
 for i in range(count):
  a=math.degrees(i*GOLD)+17;radius=(.13 if count==4 else .22)*(1+.12*math.sin(i*1.3));points=stem_path(a,heights[i],radius)
  stem_radius=.0105 if count==4 else .0128
  stalk('Water spinach jointed main stem',points,stem_radius)
  ts=[.28,.54,.79] if count==4 else [.20,.40,.61,.82]
  for j,t in enumerate(ts):
   p=point_at(points,t);d=point_at(points,min(1,t+.02))-point_at(points,max(0,t-.02));collar(p,d,stem_radius*(1-.55*t)*1.06)
   az=a+j*137.5+(31 if i%2 else -18);e=-12+j*16+7*math.sin(i*1.41+j)
   if j>=len(ts)-2:
    shoot=direction(az,42)*(.085 if count==4 else .11);end=p+shoot
    branch=stalk('Water spinach curved axillary branch',[p,p+shoot*.28+Vector((0,0,.025)),end-shoot*.15,end],.0048 if count==4 else .0062)
    branch['AxillaryBranch']=True;collar(end-shoot*.13,shoot,.0055 if count==4 else .0068);p=end
   petiole_leaf('WaterSpinach',p,az,e,.27+(j%2)*.03,.13 if count==4 else .15,.032,roll=.18*math.sin(i*2+j))
  tip=point_at(points,1);attach=tip+direction(a+35,68)*.025
  apical=direction(a+35,68)
  stalk('Water spinach slender apical petiole',[tip,tip+Vector((0,0,.012)),attach,attach+apical*.018],.0035)
  blade('WaterSpinach',attach,direction(a+35,68),.18 if i%2==0 else .24,.065 if i%2==0 else .12,'LeafYoung' if i%2==0 else 'LeafWaterSpinach',.1)
  if count==7:
   for j,t in enumerate([.70,.92]):
    p=point_at(points,t);collar(p,(0,0,1),stem_radius*(1-.55*t)*1.06)
    petiole_leaf('WaterSpinach',p,a+96+j*141,24+j*25,.23 if j==0 else .205,.115 if j==0 else .10,.044,roll=.12*math.sin(i+j))

def spinach_bunch(single=False):
 count=1 if single else 8
 for i in range(count):
  a=(i-3.5)*.018 if not single else 0
  start=Vector((a,-.255,.014+(i%3)*.006));attach=Vector((a*1.6+.045*math.sin(i*2),.005+(i%3)*.035,.040+(i//3)*.014))
  stalk('Spinach clean cut leaf stalk',[start,start+Vector((0,.08,.010)),attach-Vector((0,.05,0)),attach+Vector((0,.014,0))],.0085,solid_cut=True)
  blade('Spinach',attach,(.22*math.sin(i*1.8),1,.025),.34+(i%3)*.025,.22+(i%2)*.025,roll=.12*math.sin(i*1.1),flat=True)

def water_bunch(single=False):
 count=1 if single else 9
 for i in range(count):
  x=(i%3-1)*.036 if not single else 0;z=.020+(i//3)*.027
  start=Vector((x,-.30-(i%2)*.035,z));end=Vector((x+.045*math.sin(i*1.4),.24+(i%3)*.025,z+.025))
  points=[start,start+Vector((.008,.14,.016)),end-Vector((.012,.12,.006)),end]
  stem=stalk('Water spinach hollow harvested stem',points,.0128,hollow=True,sides=10)
  for t in [.32,.66]:collar(point_at(points,t),(0,1,.05),.014)
  for j,t in enumerate([.54,.85] if single else [.42,.68,.94]):
   p=point_at(points,t);side=-1 if (i+j)%2 else 1
   attach=p+Vector((side*.020,.018,.014));stalk('Harvest water spinach leaf petiole',[p,p+Vector((side*.01,.005,.015)),attach,attach+Vector((0,.014,0))],.0048)
   blade('WaterSpinach',attach,(side*(.65 if j!=2 else .20),1,.055),.26 if not single else .29,.12,roll=side*.10,flat=True)
