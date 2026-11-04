"""Reference produce pushcart: editable Blender modules, genuine Toonpack display,
shared Cozy atlas, separate glass and three static mobile LODs. Unity metres: front +Z.
"""
from pathlib import Path
import bpy,bmesh,math,json,random
from mathutils import Vector,Euler
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise';OUT=ROOT/'ArtSource/MarketCart';SOURCE=A/'Source~/MarketCart';MODELS=A/'mesh/World/MarketStand'
for p in (OUT/'Previews',SOURCE,MODELS):p.mkdir(parents=True,exist_ok=True)
helpers=(A/'Source~/HouseCozy/build_house_cozy.py').read_text().split('# Ground, body and readable exposed framing.')[0]
exec(compile(helpers,str(A/'Source~/HouseCozy/build_house_cozy.py'),'exec'),globals())
OUT=ROOT/'ArtSource/MarketCart';random.seed(1104)
SIZE=.75 # Real cart: 1.95m wide, 1.60m tall beside the 1.50m keeper.
scene.name='Market Cart - Reference Cozy';source.name='01 EDITABLE CART AND BASKETS';exports.name='02 RUNTIME LODS';studio.name='03 PREVIEW ONLY - DO NOT EXPORT'
original_assign=assign;wood_audit=[]
def assign(obj,slot,mi=0,rect=None):
 original_assign(obj,slot,mi,rect)
 if slot in ('WoodHoney','WoodDark') and rect is None:
  co=[v.co for v in obj.data.vertices];lo=[min(p[i] for p in co) for i in range(3)];hi=[max(p[i] for p in co) for i in range(3)];span=[hi[i]-lo[i] for i in range(3)];longest=max(range(3),key=lambda i:span[i]);seed=random.random()
  for p in obj.data.polygons:
   normal_axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=sorted([i for i in range(3) if i!=normal_axis],key=lambda i:span[i]);end=normal_axis==longest
   r=region('EndGrain' if end else slot);du=min(.9,max(.04,span[axes[0]]/.85));dv=min(.94,max(.04,span[axes[1]]/3.4));u0=r[0]+seed*(1-du)*(r[2]-r[0]);v0=r[1]+seed*(1-dv)*(r[3]-r[1])
   for li in p.loop_indices:
    v=co[obj.data.loops[li].vertex_index];u=(v[axes[0]]-lo[axes[0]])/max(1e-5,span[axes[0]]);t=(v[axes[1]]-lo[axes[1]])/max(1e-5,span[axes[1]])
    obj.data.uv_layers.active.data[li].uv=(u0+u*du*(r[2]-r[0]),v0+t*dv*(r[3]-r[1]))
  wood_audit.append(dict(object=obj.name,longAxis=longest,lengthMetres=max(span),grain='V along timber; physical crop; end-grain on caps'))
 return obj

def tube(name,points,r,slot='WoodHoney',sides=6,detail=0):
 pts=[Vector(p) for p in points];vv=[]
 for i,p in enumerate(pts):
  tangent=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized();other=Vector((0,1,0))
  if abs(tangent.dot(other))>.95:other=Vector((1,0,0))
  u=tangent.cross(other).normalized();v=tangent.cross(u).normalized()
  vv.extend([tuple(p+r*(u*math.cos(j*math.tau/sides)+v*math.sin(j*math.tau/sides))) for j in range(sides)])
 ff=[tuple(range(sides-1,-1,-1)),tuple(range((len(pts)-1)*sides,len(pts)*sides))]
 ff += [(i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j) for i in range(len(pts)-1) for j in range(sides)]
 o=mesh(name,vv,ff,slot,detail)
 for p in o.data.polygons:p.use_smooth=True
 return o

def ring(name,c,outer,inner,depth,axis='X',slot='WoodHoney',segments=40,detail=0):
 x,y,z=c;vv=[]
 for d,r in [(-depth/2,outer),(depth/2,outer),(depth/2,inner),(-depth/2,inner)]:
  for i in range(segments):
   t=i*math.tau/segments
   vv.append((x+d,y+r*math.sin(t),z+r*math.cos(t)) if axis=='X' else (x+r*math.cos(t),y+d,z+r*math.sin(t)))
 ff=[]
 for j in range(4):
  for i in range(segments):ff.append((j*segments+i,j*segments+(i+1)%segments,((j+1)%4)*segments+(i+1)%segments,((j+1)%4)*segments+i))
 return mesh(name,vv,ff,slot,detail)

def rivet(c,r=.012):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=r,location=cv(c));o=bpy.context.object;assign(o,'MetalHighlight');return link(o,'Rounded brass fastener',2)

def rect_points(w,d,r,n=5):
 return [(sx*(w/2-r)+r*math.cos(t),sz*(d/2-r)+r*math.sin(t)) for sx,sz,start in [(1,1,0),(-1,1,90),(-1,-1,180),(1,-1,270)] for t in [math.radians(start+i*90/n) for i in range(n+1)]]

def basket(name,c,w,d,h,round_shape=True,lid=False):
 x,y,z=c;n=32 if round_shape else 24
 top=[(w/2*math.cos(i*math.tau/n),d/2*math.sin(i*math.tau/n)) for i in range(n)] if round_shape else rect_points(w,d,.06,5)
 n=len(top);vv=[]
 for yy,scale in [(0,.75),(h,1),(h-.018,.91),(.035,.69)]:vv.extend([(x+u*scale,y+yy,z+v*scale) for u,v in top])
 ff=[]
 for j in range(3):
  for i in range(n):ff.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 ff += [tuple(range(3*n,4*n)),tuple(range(n-1,-1,-1))]
 o=mesh(name+' woven tapered shell',vv,ff,'Wicker')
 # Wicker strips repeat around the basket, not a stretched picture of a whole basket.
 uv=o.data.uv_layers.active;r=region('Wicker')
 for p in o.data.polygons:
  idx=p.index % n;u0=(idx%6)/6;u1=(idx%6+1)/6
  for li,(u,v) in zip(p.loop_indices,[(u0,0),(u1,0),(u1,1),(u0,1)]):uv.data[li].uv=(r[0]+u*(r[2]-r[0]),r[1]+v*(r[3]-r[1]))
 for yy,scale,radius in [(h,1,.024),(.025,.77,.015)]:
  tube(name+' bound wicker rim',[(x+u*scale,y+yy,z+v*scale) for u,v in top+[top[0]]],radius,'Rope',6)
 for row in range(1,4):
  f=row/4;scale=.75+.25*f
  tube(name+' raised weave band',[(x+u*scale,y+h*f,z+v*scale) for u,v in top+[top[0]]],.008,'Rope',4,1)
 for i in range(0,n,2):
  u,v=top[i];tube(name+' woven upright',[(x+u*.77,y+.02,z+v*.77),(x+u*.99,y+h-.01,z+v*.99)],.009,'Rope',4,1)
 if lid:
  # Shallow woven dome and a real central knob.
  vv=[(x,y+h+.09,z)]+[(x+u,y+h+.012,z+v) for u,v in top]
  mesh(name+' domed woven lid',vv,[(0,i+1,(i+1)%n+1) for i in range(n)]+[tuple(range(n,0,-1))],'Wicker')
  cyl(name+' lid knob',(x,y+h+.105,z),.032,.035,'WoodDark',10,1)
 return (x,y+h,z)

# Reference proportions: compact counter, open lower shelf, closed plank sides.
for i in range(8):box('Counter individual honey plank '+str(i),(-.945+i*.27,1.095,0),(.264,.08,1.53),'WoodHoney',.013)
for z in [-.80,.80]:box('Counter continuous end rail',(0,1.12,z),(2.24,.115,.105),'WoodHoney',.018)
for x in [-1.07,1.07]:box('Counter continuous side rail',(x,1.12,0),(.105,.115,1.61),'WoodHoney',.018)
for x in [-.94,.94]:
 for z in [-.65,.65]:
  box('Thick square corner leg',(x,.69,z),(.105,.89,.105),'WoodHoney',.015)
  box('Lower foot metal plate',(x,.31,z),(.13,.11,.13),'Iron',.012,1)
for i in range(7):box('Open lower shelf plank '+str(i),(-.81+i*.27,.28,.02),(.26,.065,1.30),'WoodHoney',.011)
for x in [-.94,.94]:
 box('Side bottom bearer',(x,.30,0),(.11,.13,1.4),'WoodDark',.014)
 box('Side top bearer',(x,1.015,0),(.11,.13,1.4),'WoodHoney',.012)
 for i in range(7):box('Closed side vertical cladding',(x,.66,-.57+i*.19),(.047,.64,.182),'WoodHoney',.007)
for i in range(10):box('Rear vertical body cladding',(-.86+i*.19,.66,-.65),(.18,.64,.05),'WoodHoney',.006)
box('Open front lower bearer',(0,.31,.70),(1.98,.12,.10),'WoodHoney',.015)
box('Front apron below counter',(0,.987,.72),(1.98,.15,.065),'WoodHoney',.01)
for x in [-1.02,1.02]:
 for z in [-.70,.70]:
  for y in [.40,.95,1.12]:rivet((x,y,z))

# Pair of 10-spoke wooden wheels: clear open spokes, iron tyres, layered hubs.
tube('Single transverse axle',[(-1.30,.565,-.05),(1.30,.565,-.05)],.057,'Iron',10)
for sign in [-1,1]:
 wheel_start=len(parts)
 x=sign*1.16;center=(x,.565,-.05)
 ring('Iron tyre',center,.558,.501,.100,'X','Iron',40)
 ring('Honey wooden wheel rim',center,.507,.439,.110,'X','WoodHoney',40)
 ring('Outer rim bevel highlight',(x+sign*.052,.565,-.05),.499,.476,.016,'X','WoodHoney',40,1)
 for i in range(10):
  a=i*math.tau/10
  beam('Wheel spoke '+str(i),(x,.565+.077*math.sin(a),-.05+.077*math.cos(a)),(x,.565+.465*math.sin(a),-.05+.465*math.cos(a)),.046,.052,'WoodHoney')
 tube('Turned wooden wheel hub',[(x-sign*.085,.565,-.05),(x+sign*.105,.565,-.05)],.101,'WoodHoney',16)
 tube('Brass axle cap',[(x+sign*.105,.565,-.05),(x+sign*.127,.565,-.05)],.069,'MetalHighlight',16)
 for part in parts[wheel_start:]:part['Group']='WheelLeft' if sign>0 else 'WheelRight'

# Rear shafts rise toward the handles; useful empty centre reserved for the keeper.
for sign in [-1,1]:
 start=Vector((sign*.467,.91,-.57));end=Vector((sign*.467,1.27,-2.18));axis=(end-start).normalized()
 tube('Long push shaft',[tuple(start),tuple(end)],.043,'WoodHoney',12)
 gripstart=end-axis*.30;gripend=end+axis*.055
 tube('Turned hand grip',[tuple(gripstart),tuple(gripend)],.057,'WoodHoney',12)
 for t in [.04,.13,.24,.31]:
  c=gripstart+axis*t;tube('Hand grip carved collar',[tuple(c-axis*.012),tuple(c+axis*.012)],.063,'WoodDark',12,1)
 beam('Shaft lower diagonal brace',(sign*.79,.43,-.57),(sign*.60,.94,-1.16),.065,.065,'WoodHoney')
 rivet(tuple(start))
box('Rear shaft cross brace',(0,.91,-.68),(1.70,.085,.085),'WoodHoney',.012)

# Raised cabinet frame, one mid shelf, sliding front doors and round pulls.
cx=.24;cz=-.33;cw=1.52;cd=.67;bottom=1.16;top=2.105
for x in [cx-cw/2,cx+cw/2]:
 for z in [cz-cd/2,cz+cd/2]:
  box('Cabinet upright',(x,(bottom+top)/2,z),(.055,top-bottom,.055),'WoodHoney',.008)
  for y in [bottom+.025,top-.025]:rivet((x,y,z+.03))
for y in [bottom,1.62,top]:
 for z in [cz-cd/2,cz+cd/2]:box('Cabinet horizontal frame',(cx,y,z),(cw+.055,.056,.056),'WoodHoney',.008)
 for x in [cx-cw/2,cx+cw/2]:box('Cabinet side frame',(x,y,cz),(.056,.056,cd),'WoodHoney',.008)
for y in [bottom+.02,1.62]:
 for i in range(5):box('Display shelf plank',(cx-.62+i*.31,y,cz),(.305,.036,cd-.045),'WoodHoney',.005)
box('Sliding door centre stile',(cx,(bottom+top)/2,cz+cd/2+.012),(.027,top-bottom-.07,.035),'WoodDark',.004)
for dx in [-.045,.045]:
 tube('Sliding door pull stalk',[(cx+dx,1.635,cz+cd/2+.02),(cx+dx,1.635,cz+cd/2+.065)],.012,'MetalHighlight',8)
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.025,location=cv((cx+dx,1.635,cz+cd/2+.075)))
 o=bpy.context.object;assign(o,'MetalHighlight');link(o,'Round sliding door pull',0)

glass=bpy.data.materials.new('MarketCart_Glass');glass.use_nodes=True
bs=glass.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.79,.91,.92,1);bs.inputs['Roughness'].default_value=.22;bs.inputs['Alpha'].default_value=.10;glass.surface_render_method='DITHERED';glass.use_transparent_shadow=True
panes=[('Front',[(cx-cw/2+.03,bottom+.03,cz+cd/2),(cx+cw/2-.03,bottom+.03,cz+cd/2),(cx+cw/2-.03,top-.03,cz+cd/2),(cx-cw/2+.03,top-.03,cz+cd/2)]),('Top',[(cx-cw/2+.03,top,cz-cd/2+.03),(cx+cw/2-.03,top,cz-cd/2+.03),(cx+cw/2-.03,top,cz+cd/2-.03),(cx-cw/2+.03,top,cz+cd/2-.03)])]
panes.append(('Rear',[(cx-cw/2+.03,bottom+.03,cz-cd/2),(cx+cw/2-.03,bottom+.03,cz-cd/2),(cx+cw/2-.03,top-.03,cz-cd/2),(cx-cw/2+.03,top-.03,cz-cd/2)]))
for sign in [-1,1]:
 x=cx+sign*cw/2;panes.append(('Side',[(x,bottom+.03,cz-cd/2+.03),(x,bottom+.03,cz+cd/2-.03),(x,top-.03,cz+cd/2-.03),(x,top-.03,cz-cd/2+.03)]))
for name,vs in panes:
 o=mesh('Clear cabinet '+name,vs,[(0,1,2,3)],'GlassBlue');o.data.materials.clear();o.data.materials.append(glass);o['Group']='Glass'
 # A single surface per pane and one transparent material: no nested double shells.
 o.visible_shadow=False

# Baskets are authored here; their produce remains genuine original Toonpack geometry/UV.
basket('Left tall vegetable basket',(-.77,1.15,.22),.56,.56,.29)
basket('Front rectangular tomato basket',(-.06,1.15,.51),.63,.43,.17,False)
basket('Right pear basket',(.65,1.15,.47),.53,.49,.19)
basket('Lower round lidded basket',(.43,.32,.35),.51,.51,.34,True,True)
# Lower potato crate with an open top, spaced slats and corner battens.
crate=(-.39,.32,.28)
for row in range(3):
 y=crate[1]+.055+row*.087
 for z in [crate[2]-.25,crate[2]+.25]:box('Potato crate front back slat',(crate[0],y,z),(.67,.077,.028),'WoodHoney',.004)
 for x in [crate[0]-.32,crate[0]+.32]:box('Potato crate side slat',(x,y,crate[2]),(.028,.077,.50),'WoodHoney',.004)
for x in [crate[0]-.29,crate[0]+.29]:
 for z in [crate[2]-.23,crate[2]+.23]:box('Potato crate corner batten',(x,crate[1]+.15,z),(.045,.30,.045),'WoodHoney',.005)
box('Potato crate bottom',(crate[0],crate[1]+.02,crate[2]),(.65,.035,.48),'WoodDark',.004)

data=json.loads((SOURCE/'ToonpackProduce.json').read_text());produce_by_id={d['id']:d for d in data};produce_sources=[];instances=[]
cached_atlas=OUT/'ToonpackAtlasPreview.png'
if cached_atlas.exists():image=bpy.data.images.load(str(cached_atlas))
else:
 # A clean clone can rebuild from the packed preview in the editable master,
 # without committing a second copy of the vendor's 2K atlas as a loose texture.
 with bpy.data.libraries.load(str(A/'Source~/MarketStand/MarketStand_A.blend'),link=False) as (available,loaded):
  loaded.images=[name for name in available.images if name.startswith('ToonpackAtlasPreview')][:1]
 assert loaded.images,'Missing packed Toonpack preview; re-export produce from Unity.'
 image=loaded.images[0]
image.pack()
produce_material=bpy.data.materials.new('MarketCart_ToonpackProduce');produce_material.use_nodes=True
ns=produce_material.node_tree.nodes;bs=ns.get('Principled BSDF');bs.inputs['Roughness'].default_value=.78;tx=ns.new('ShaderNodeTexImage');tx.image=image;produce_material.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);produce_material.node_tree.links.new(tx.outputs['Alpha'],bs.inputs['Alpha']);produce_material.surface_render_method='DITHERED'
templates={}
for key,row in produce_by_id.items():
 vv=[];ff=[];uvs=[]
 for m in row['meshes']:
  off=len(vv);vv.extend([cv(v) for v in m['vertices']]);ff.extend([tuple(off+i for i in m['triangles'][j:j+3]) for j in range(0,len(m['triangles']),3)]);uvs.extend(m['uv'])
 me=bpy.data.meshes.new('Original TFP '+key);me.from_pydata(vv,[],ff);me.update();uv=me.uv_layers.new(name='ToonpackOriginalUV')
 for p in me.polygons:
  p.use_smooth=True
  for li in p.loop_indices:uv.data[li].uv=uvs[me.loops[li].vertex_index]
 me.materials.append(produce_material);templates[key]=me

def fruit(key,at,size,rotation=(0,0,0)):
 me=templates[key];o=bpy.data.objects.new('Toonpack '+key,me);source.objects.link(o);pts=[v.co for v in me.vertices];lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)]);factor=size/max(hi-lo)
 o.data=me.copy()
 rot=Euler(tuple(math.radians(v) for v in rotation)).to_matrix()
 for v in o.data.vertices:v.co=rot@((v.co-(lo+hi)*.5)*factor)
 # Rotate around the produce centre, then rest its lowest point on the shelf.
 # Tilting an uncentred carrot/banana must never push it through the wood.
 low=min(v.co.z for v in o.data.vertices)
 for v in o.data.vertices:v.co.z-=low
 o.location=cv(at);parts.append(o);o['Group']='Produce';o['OriginalPrefab']=produce_by_id[key]['prefab'];o['CozyDetailTier']=0
 instances.append(dict(id=key,position=list(at),size=size,rotation=list(rotation),triangles=len(o.data.polygons)))
 if key not in produce_sources:produce_sources.append(key)
 return o

# Hand-staggered piles: shelf/floor-supported base layers, small nested upper layers.
# Banana geometry already lies along the shelf in Toonpack; do not rotate it upright.
for at,size,rot in [((-.39,1.65,-.46),.32,(-4,5,-22)),((-.27,1.65,-.43),.34,(3,-5,2)),((-.15,1.65,-.38),.31,(-3,7,21)),((-.36,1.688,-.29),.33,(4,-4,-9)),((-.23,1.685,-.27),.32,(-5,2,14))]:fruit('Banana_01A',at,size,rot)
for at,size,rot in [((.10,1.65,-.46),.18,(7,-5,17)),((.28,1.65,-.49),.185,(-6,8,61)),((.42,1.65,-.37),.175,(4,9,113)),((.20,1.65,-.27),.19,(-8,-4,202)),((.23,1.775,-.40),.18,(11,7,48)),((.35,1.765,-.29),.17,(-9,4,157))]:fruit('Orange_01A',at,size,rot)
for at,size,rot in [((.62,1.65,-.46),.17,(8,-6,24)),((.81,1.65,-.44),.18,(-5,9,81)),((.65,1.65,-.26),.175,(6,11,152)),((.83,1.65,-.25),.16,(-9,-7,223)),((.72,1.765,-.37),.17,(12,-4,63)),((.82,1.755,-.32),.16,(-7,13,192))]:fruit('Pomegranate_01A',at,size,rot)
# Lower cabinet: a loose tomato mound, fanned lying carrots and differently turned eggplants.
for at,size,rot in [((-.38,1.20,-.46),.18,(5,-8,13)),((-.18,1.20,-.44),.19,(-6,7,64)),((-.33,1.20,-.26),.175,(9,4,142)),((-.12,1.20,-.27),.18,(-8,-3,238)),((-.25,1.325,-.36),.18,(7,11,304))]:fruit('Tomato_01A',at,size,rot)
for at,size,rot in [((.08,1.20,-.35),.30,(84,5,-17)),((.17,1.205,-.35),.32,(88,-4,-4)),((.27,1.20,-.33),.29,(81,7,12)),((.34,1.227,-.34),.31,(86,-3,23))]:fruit('Carrot_01A',at,size,rot)
for at,size,rot in [((.60,1.20,-.37),.29,(3,-6,-12)),((.75,1.20,-.39),.31,(-4,8,7)),((.86,1.225,-.34),.28,(5,-5,21))]:fruit('Eggplant_03A',at,size,rot)
# Basket base is y+0.035; bodies stay inside the rim, only leaves spill over it.
fruit('Salad_01A',(-.72,1.185,.22),.31,(-9,4,-18))
fruit('Cauliflower_01A',(-.68,1.29,.26),.35,(4,-7,-16))
fruit('Salad_01A',(-.88,1.19,.17),.43,(-6,8,37))
fruit('Leek_01A',(-.82,1.19,.31),.48,(9,-12,-19))
for at,size,rot in [((-.24,1.185,.41),.17,(7,-5,18)),((-.065,1.185,.39),.16,(-6,9,74)),((.105,1.185,.43),.17,(5,7,147)),((-.17,1.185,.58),.165,(-9,-3,211)),((.03,1.185,.59),.175,(6,-8,272)),((-.22,1.285,.43),.17,(-7,12,323)),((-.045,1.29,.415),.165,(11,5,48)),((.115,1.285,.47),.155,(-8,9,177)),((-.08,1.30,.575),.16,(7,-11,253))]:fruit('Tomato_01A',at,size,rot)
for at,size,rot in [((.52,1.185,.40),.24,(10,-8,16)),((.69,1.185,.38),.23,(-12,5,83)),((.80,1.185,.49),.25,(9,11,154)),((.63,1.185,.55),.225,(-6,-9,227)),((.63,1.33,.44),.23,(14,-8,308)),((.72,1.32,.53),.22,(-11,7,41))]:fruit('Pear_01B',at,size,rot)
for i,(x,z) in enumerate([(-.60,.12),(-.41,.105),(-.22,.14),(-.58,.32),(-.39,.33),(-.20,.31)]):fruit('Potato_02A',(x,.36,z),.155+(i%3)*.008,(11-i*4,-9+i*5,i*53+17))
for i,(x,z) in enumerate([(-.53,.19),(-.34,.17),(-.16,.23),(-.49,.36),(-.29,.35)]):fruit('Potato_01A',(x,.425+(i%2)*.008,z),.16+(i%3)*.007,(-8+i*4,12-i*3,i*67+26))
for i,(x,z) in enumerate([(-.59,.22),(-.40,.20),(-.22,.28),(-.45,.36)]):fruit('Potato_01A',(x,.50+(i%2)*.012,z),.165+(i%3)*.006,(12-i*3,-8+i*5,i*51+38))

# Fix outward winding on authored solids; vendor UV/geometry is untouched.
for o in parts:
 o.location*=SIZE;o.scale*=SIZE
for o in parts:
 if o.get('Group') in ('Produce','Glass'):continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()

def combine(items,name,material,ratio=1):
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for p in items:
  o=p.copy();o.data=p.data.copy();exports.objects.link(o);o.select_set(True);copies.append(o)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # Source art is authored like the front-view reference. Bake the width axis for
 # Unity's front camera convention; runtime transforms remain positive and unit.
 for v in o.data.vertices:v.co.x*=-1
 o.data.materials.clear();o.data.materials.append(material)
 for p in o.data.polygons:p.material_index=0
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bm.to_mesh(o.data);bm.free()
 if ratio<1:
  mod=o.modifiers.new('Mobile distance reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o

entries=[];runtime=[]
for lod in range(3):
 for group,material in [('Cart',M[0]),('WheelLeft',M[0]),('WheelRight',M[0]),('Produce',produce_material),('Glass',glass)]:
  items=[p for p in parts if p.get('Group','Cart')==group and (lod==0 or p.get('CozyDetailTier',0)<(2 if lod==1 else 1))]
  o=combine(items,f'MarketCart_{group}_LOD{lod}',material,1 if lod==0 or group=='Glass' else (.62 if lod==1 else .32));runtime.append(o)
  pts=[o.matrix_world@v.co for v in o.data.vertices];u=[(p.x,p.z,-p.y) for p in pts]
  entries.append(dict(name=o.name,group=group,lod=lod,material=material.name,triangles=len(o.data.polygons),min=[min(p[i] for p in u) for i in range(3)],max=[max(p[i] for p in u) for i in range(3)]))
  if group in ('Cart','WheelLeft','WheelRight'):
   rects=[s['uvSafe'] for s in S.values()]
   for p in o.data.polygons:
    uv=[o.data.uv_layers.active.data[li].uv for li in p.loop_indices]
    assert any(all(r[0]-1e-6<=v.x<=r[2]+1e-6 and r[1]-1e-6<=v.y<=r[3]+1e-6 for v in uv) for r in rects),o.name+' atlas bleed'

bpy.ops.object.select_all(action='DESELECT')
for o in runtime:o.select_set(True)
for name,p in [('CozyOrigin',(0,0,0)),('CozyRight',(1,0,0)),('CozyForward',(0,0,1)),('CozyUp',(0,1,0))]:
 o=bpy.data.objects.new(name,None);exports.objects.link(o);o.location=cv(p);o.select_set(True)
bpy.context.view_layer.objects.active=runtime[0]
bpy.ops.export_scene.fbx(filepath=str(MODELS/'MarketStand_A.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
exports.hide_render=True;exports.hide_viewport=True
for name,p in [('KeeperAnchor',(0,0,-2.32)),('FrontInteractionAnchor',(0,0,1.05)),('LeftGripAnchor',(-.467,1.27,-2.12)),('RightGripAnchor',(.467,1.27,-2.12))]:
 o=bpy.data.objects.new(name,None);source.objects.link(o);o.location=cv(p)*SIZE;o.empty_display_type='ARROWS';o.empty_display_size=.15
scene['Reference']='MarketCart_Target.png';scene['Produce']='Original Toon Farm Pack geometry and UV; dragonfruit replaced with pomegranate';scene['Glass']='5 single surfaces; no transparent nested shells'
# Soft reference studio is excluded from runtime export.
scene.world=bpy.data.worlds.new('Cozy neutral daylight');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.54,.57,.60,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,pos,power,size in [('Key',(-3,-4,6),380,4),('Fill',(4,-1,4),150,4),('Rim',(0,4,5),210,3),('Low front fill',(0,-4,1.2),80,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1.0))-o.location).to_track_quat('-Z','Y').to_euler()
cam=bpy.data.objects.new('Reference camera',bpy.data.cameras.new('Reference camera'));studio.objects.link(cam);cam.location=(3.5,-5,3.0);target=Vector((0,.35,1.1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.85;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.view_settings.view_transform='AgX';scene.render.resolution_x=1300;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
bpy.ops.object.select_all(action='DESELECT')
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=4.2;area.spaces.active.region_3d.view_location=target
master=A/'Source~/MarketStand/MarketStand_A.blend';bpy.ops.wm.save_as_mainfile(filepath=str(master))
manifest=dict(kind='MarketCart',meshes=entries,sourceProduce=[produce_by_id[k]['prefab'] for k in produce_sources],produceInstances=instances,woodUV=wood_audit,glassPanels=len(panes),wheelSpokes=10,wheels=2,displayBaskets=4,cabinetShelves=2,keeperAnchor=[0,0,-2.42],frontAnchor=[0,0,1.05],gripAnchors=[[-.28,.98,-2.12],[.28,.98,-2.12]],wheelPivots=[[-1.16,.565,-.05],[1.16,.565,-.05]],colliders=[dict(name='Body',center=[0,.65,0],size=[2.04,.96,1.40]),dict(name='WheelLeft',center=[-1.16,.565,-.05],size=[.18,1.116,1.116]),dict(name='WheelRight',center=[1.16,.565,-.05],size=[.18,1.116,1.116]),dict(name='HandleLeft',center=[-.535,.945,-1.375],size=[.13,.13,1.75],rotation=[2.4,-17.6,0]),dict(name='HandleRight',center=[.535,.945,-1.375],size=[.13,.13,1.75],rotation=[2.4,17.6,0])],scale=1)
manifest.update(keeperAnchor=[0,0,-2.32],gripAnchors=[[-.467,1.27,-2.12],[.467,1.27,-2.12]],designScale=SIZE)
for key in ('keeperAnchor','frontAnchor'):manifest[key]=[v*SIZE for v in manifest[key]]
for key in ('gripAnchors','wheelPivots'):manifest[key]=[[v*SIZE for v in row] for row in manifest[key]]
for c in manifest['colliders']:
 if c['name'].startswith('Handle'):
  c['center']=[(-1 if c['name'].endswith('Left') else 1)*.467,1.09,-1.375];c['rotation']=[12.6,0,0];c['size']=[.13,.13,1.75]
 for key in ('center','size'):c[key]=[v*SIZE for v in c[key]]
(SOURCE/'MarketCart_Manifest.json').write_text(json.dumps(manifest,indent=2))
scene.render.filepath=str(OUT/'Previews/MarketCart_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
print('MARKET_CART_BUILD_PASS',json.dumps({k:sum(m['triangles'] for m in entries if m['lod']==k) for k in range(3)}),flush=True)
