"""Approved modular barn interior. Blender metres, +Y rear, entrance origin.
Build in a separate Blender process; existing projects and Unity scenes untouched.
"""
import bpy, bmesh, math, json, random, sys, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ART=Path(__file__).resolve().parents[1]; ROOT=ART.parents[1]
for folder in ['Blender','Exports','Previews','References']:(ART/folder).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
random.seed(141026)
audit=json.loads((ROOT/'ArtSource/MiniMarket_A/References/atlas-audit.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256((ROOT/audit['sourcePSD']).read_bytes()).hexdigest()==audit['sourceSha256']
PAL={k:{'uvSafeRectBottomLeft':v['safeUvBottomLeft']} for k,v in audit['swatches'].items()}
PAL.update(json.loads((ROOT/'ArtSource/Barn_A/References/Barn_A_Approved.json').read_text())['materials']['swatches'])
BASE={'materials':{'atlasGuid':audit['sourceGUID']}}
atlas=bpy.data.images.load(str(ROOT/'ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack()
atlas['UnityGUID']=audit['sourceGUID']
# Select additional colours from the same regular palette grid, never a new texture.
pix=list(atlas.pixels);w,h=atlas.size
for key,rgb in {'teal':(48,115,124),'steel':(155,167,166),'water':(70,150,185),'earth':(165,116,57),'hay':(215,168,57),'green':(115,145,49),'cream':(225,220,184)}.items():
 best=None
 for x in range(16,1024,32):
  for top in range(32,2048,64):
   yy=h-1-top;rgb2=[pix[(yy*w+x)*4+i] for i in range(3)]
   dist=sum((rgb2[i]-rgb[i]/255)**2 for i in range(3))
   if best is None or dist<best[0]:best=(dist,x,top,rgb2)
 _,x,y,cc=best;PAL[key]={'uvSafeRectBottomLeft':[(x-10)/w,1-(y+24)/h,(x+10)/w,1-(y-24)/h],'sampleRGB':[round(t*255) for t in cc]}
mat=bpy.data.materials.new('TFP_Atlas_1A');mat.use_nodes=True;mat['UnityGUID']=audit['sourceGUID']
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.86;bs.inputs['Specular IOR Level'].default_value=.12
mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
exec(compile((ROOT/'ArtSource/MiniMarket_A/Scripts/mesh_common.py').read_text(),'<shared mesh helpers>','exec'),globals())
library=bpy.context.scene;library.name='00 MODULAR KIT';setup(library)
kit=coll('KIT - reusable meshes',library);reused=[];assets={}
def cylinder(b,a,c,r,color,n=8,r2=None,cap=True):
 a,c=Vector(a),Vector(c);axis=(c-a).normalized();u=axis.cross(Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))).normalized();v=axis.cross(u)
 vs=[p+(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))*rr for p,rr in [(a,r),(c,r if r2 is None else r2)] for i in range(n)]
 faces=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 if cap:faces+=[tuple(reversed(range(n))),tuple(range(n,2*n))]
 b.solid(vs,faces,color)
def asset(name,b):
 o=b.obj(name,kit);o['Role']='Reusable mobile module';assets[name]=o;return o
def boxasset(name,size,color,z=None):
 b=Build();b.box((0,0,size[2]/2 if z is None else z),size,color);return asset(name,b)
def uvcolor(o,color):
 a,b,c,d=PAL[color]['uvSafeRectBottomLeft'];uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for l in uv.data:l.uv=((a+c)/2,(b+d)/2)
def hopper(b,z0,z1,wx,wy,color):
 # Four open walls with a small rim, visible empty dark throat below fill.
 for i in [-1,1]:
  b.face([(i*.17,-.16,z0),(i*.17,.16,z0),(i*wx/2,wy/2,z1),(i*wx/2,-wy/2,z1)],color,(i,0,0))
  b.face([(-.17,i*.16,z0),(.17,i*.16,z0),(wx/2,i*wy/2,z1),(-wx/2,i*wy/2,z1)],color,(0,i,0))
 for x in [-wx/2,wx/2]:b.box((x,0,z1),(.045,wy+.05,.045),'steel')
 for y in [-wy/2,wy/2]:b.box((0,y,z1),(wx,.045,.045),'steel')
 b.face([(-wx*.38,-wy*.38,z1-.13),(wx*.38,-wy*.38,z1-.13),(wx*.38,wy*.38,z1-.13),(-wx*.38,wy*.38,z1-.13)],'green',(0,0,1))
def panel(b,x,y,z):
 b.box((x,y,z),(.35,.10,.43),'metal')
 for xx,zz,colr in [(-.08,.08,'green'),(.08,.08,'lantern'),(0,-.09,'red')]:
  cylinder(b,(x+xx,y-.06,z+zz),(x+xx,y-.085,z+zz),.037,colr,6)
# Modular walls: seams are flat ribbons rather than individual bevelled boards.
def wall(window=False):
 b=Build()
 if window:
  b.box((0,0,.55),(2,.12,1.1),'red');b.box((0,0,2.85),(2,.12,1.1),'red')
  for x in [-.75,.75]:b.box((x,0,1.7),(.5,.12,1.2),'red')
  b.box((0,0,1.7),(1,.04,1.2),'window')
  for x in [-.54,.54,0]:b.box((x,-.09,1.7),(.07,.10,1.34),'wood')
  for z in [1.06,1.7,2.34]:b.box((0,-.09,z),(1.14,.10,.07),'wood')
 else:b.box((0,0,1.7),(2,.12,3.4),'red')
 for x in [-.75,-.5,-.25,0,.25,.5,.75]:
  for lo,hi in ([(.12,1.05),(2.35,3.35)] if window and abs(x)<.51 else [(.12,3.35)]):
   b.face([(x-.006,-.061,lo),(x+.006,-.061,lo),(x+.006,-.061,hi),(x-.006,-.061,hi)],'red_dark',(0,-1,0))
 for z in [.14,2.65,3.34]:b.box((0,-.10,z),(2,.12,.12),'wood')
 return b
asset('N01_Wall_2m',wall());asset('N01_WindowWall_2m',wall(True));boxasset('N01_Post',(.20,.22,3.4),'wood')
b=Build();b.beam((0,0,2.65),(.75,0,3.32),.12,.14,'wood');asset('N01_Brace',b)
# Trough shell with open basin and two partitions, 3.6 x .9 x .65.
b=Build();b.box((0,0,.23),(.84,3.54,.07),'steel')
for x in [-.425,.425]:b.box((x,0,.44),(.05,3.60,.42),'steel')
for y in [-1.775,-.6,.6,1.775]:b.box((0,y,.44),(.80,.05,.42),'steel')
for x in [-.34,.34]:
 for y in [-1.45,1.45]:b.box((x,y,.12),(.09,.11,.24),'metal')
asset('N02_Trough_3Slots',b)
b=Build();b.solid([(-.37,-.54,.30),(.37,-.54,.30),(.37,.54,.30),(-.37,.54,.30),(-.29,-.44,.43),(.29,-.44,.43),(.29,.44,.43),(-.29,.44,.43)],[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'hay')
for i in range(14):
 x=random.uniform(-.32,.32);y=random.uniform(-.48,.48);a=random.uniform(0,math.tau);dx,dy=.12*math.cos(a),.12*math.sin(a)
 b.face([(x-dx,y-dy,.433),(x+dx,y+dy,.433),(x+dx+.025,y+dy-.025,.434)],'wood_shadow' if i%3 else 'hay',(0,0,1))
asset('N02_FeedFill',b)
def chopper(name,large=False,motor=False):
 b=Build();s=1.5 if large else 1
 for x in [-.38,.38]:
  for y in [-.3,.3]:b.beam((x*s,y*s,0),(x*.75*s,y*s,.7*s),.10*s,.10*s,'teal')
 b.box((0,0,.7*s),(.95*s,.7*s,.28*s),'teal')
 cylinder(b,(-.5*s,0,.88*s),(.5*s,0,.88*s),.28*s,'teal',10)
 for x in [-.48,.48]:cylinder(b,(x*s-.025,0,.88*s),(x*s+.025,0,.88*s),.29*s,'steel',10)
 # Top hopper grows upward, outlet faces player.
 old=Build();hopper(old,1.02,1.4,1,.75,'teal')
 b.v.extend(v*s for v in old.v);b.f.extend(tuple(i+len(b.v)-len(old.v) for i in f) for f in old.f);b.col.extend(old.col)
 b.box((0,-.43*s,.68*s),(.45*s,.35*s,.07*s),'steel');b.box((0,-.355*s,.82*s),(.42*s,.03*s,.20*s),'metal')
 if motor:
  cylinder(b,(.50*s,0,.87*s),(.95*s,0,.87*s),.23*s,'metal',10)
  for x in [.60,.72,.84]:cylinder(b,((x-.02)*s,0,.87*s),((x+.02)*s,0,.87*s),.25*s,'steel',10)
  panel(b,.32*s,-.36*s,.82*s)
 else:
  b.beam((.52,0,.88),(.72,0,.88),.05,.05,'steel');b.beam((.72,0,.88),(.72,0,.62),.05,.05,'steel');b.beam((.72,0,.62),(.94,0,.62),.09,.09,'timber')
 return asset(name,b)
chopper('N03_Manual');chopper('N03_Motor',motor=True);chopper('N03_LargeMotor',True,True)
b=Build()
for x in [-.42,.42]:
 for y in [-.35,.35]:b.box((x,y,.37),(.10,.10,.74),'teal')
hopper(b,.7,2,1.4,1.2,'teal');cylinder(b,(0,-.15,.8),(0,-.6,.8),.16,'steel');panel(b,.48,-.40,.70);asset('N04_Mixer',b)
b=Build();cylinder(b,(0,0,0),(2,0,0),.06,'steel');asset('N05_Pipe_2m',b)
b=Build()
for a,c in [((0,0,0),(.3,0,0)),((.3,0,0),(.3,.3,0))]:cylinder(b,a,c,.06,'steel')
asset('N05_Elbow',b)
b=Build();cylinder(b,(-.25,0,0),(.25,0,0),.07,'steel');cylinder(b,(0,0,0),(0,0,-.35),.06,'steel');asset('N05_Tee',b)
b=Build();cylinder(b,(-.05,0,0),(.05,0,0),.082,'metal');asset('N05_Coupler',b)
b=Build();cylinder(b,(0,0,0),(0,0,2.7),.065,'steel');b.box((0,0,.04),(.35,.35,.08),'metal');asset('N05_Support',b)
b=Build();b.box((0,0,.50),(.40,.35,.24),'teal');b.box((0,0,.635),(.44,.39,.03),'steel')
for x in [-.13,0,.13]:cylinder(b,(x,0,.38),(x,0,.1),.037,'steel',6)
cylinder(b,(0,0,.1),(0,0,0),.065,'steel',6,r2=.025);asset('N05_Dispenser',b)
b=Build();cylinder(b,(0,0,.25),(0,0,1.85),.5,'steel',12);cylinder(b,(0,0,1.85),(0,0,2.02),.5,'steel',12,r2=.12)
cylinder(b,(0,0,2.02),(0,0,2.1),.10,'metal')
for z in [.35,1.7]:cylinder(b,(0,0,z),(0,0,z+.04),.515,'metal',12)
for x,y in [(-.32,-.27),(.32,-.27),(0,.33)]:b.box((x,y,.125),(.09,.09,.25),'steel')
cylinder(b,(0,-.47,.45),(0,-.68,.45),.05,'steel');asset('N06_ProcessTank',b)
# Adapted fixtures are authored in the shared style, not labelled as original pack meshes.
b=Build();b.box((0,0,.05),(1.4,.65,.1),'timber');b.box((0,0,.85),(1.4,.65,.08),'timber');b.box((0,0,1.62),(1.4,.65,.08),'timber')
for x in [-.66,.66]:b.box((x,0,.85),(.08,.65,1.70),'timber')
asset('A_StorageRack',b)
b=Build();b.box((0,0,.52),(1.2,.65,1.04),'teal')
for x in [-.295,.295]:
 b.box((x,-.335,.55),(.57,.04,.94),'roof');cylinder(b,(x+(.16 if x<0 else -.16),-.36,.57),(x+(.16 if x<0 else -.16),-.40,.57),.025,'cream',6)
b.box((0,0,1.06),(1.28,.71,.07),'steel');asset('A_SupplyCabinet',b)
b=Build();b.box((0,0,.52),(2.8,.80,.16),'steel')
for x in [-1.35,1.35]:b.box((x,0,.66),(.10,.80,.26),'steel')
for y in [-.35,.35]:b.box((0,y,.66),(2.6,.10,.26),'steel')
for x in [-1.05,1.05]:b.box((x,0,.21),(.16,.60,.42),'foundation')
b.face([(-1.29,-.29,.69),(1.29,-.29,.69),(1.29,.29,.69),(-1.29,.29,.69)],'water',(0,0,1));asset('A_WaterTrough',b)
b=Build();b.box((0,0,.64),(1.25,.7,.12),'steel')
for x in [-.59,.59]:b.box((x,0,.76),(.07,.7,.2),'steel')
for y in [-.32,.32]:b.box((0,y,.76),(1.12,.06,.2),'steel')
for x in [-.45,.45]:b.box((x,0,.30),(.12,.5,.6),'foundation')
b.face([(-.52,-.26,.76),(.52,-.26,.76),(.52,.26,.76),(-.52,.26,.76)],'water',(0,0,1))
cylinder(b,(0,.24,.8),(0,.24,1.15),.025,'steel');cylinder(b,(0,.24,1.15),(0,.05,1.15),.025,'steel');asset('A_Sink',b)
b=Build();b.box((0,0,.7),(1.4,.6,.1),'timber')
for x in [-.58,.58]:b.box((x,0,.35),(.08,.5,.7),'teal')
asset('A_BottleCounter',b)
b=Build();b.box((0,0,.4),(.85,.6,.8),'hay')
for x in [-.23,.23]:
 for y in [-.307,.307]:b.box((x,y,.4),(.025,.012,.8),'wood_shadow')
 b.box((x,0,.808),(.025,.6,.015),'wood_shadow')
hay=asset('A_HayBale_Mobile',b)
b=Build();b.box((0,.12,.14),(.10,.20,.12),'metal')
for z,ww in [(-.17,.17),(.15,.20)]:b.box((0,0,z),(ww,.15,.055),'metal')
b.box((0,0,0),(.12,.09,.25),'lantern')
for x in [-.075,.075]:b.box((x,-.035,0),(.025,.10,.34),'metal')
b.extrude([(-.14,.18),(.14,.18),(.045,.32),(-.045,.32)],0,.18,'metal');asset('R_ChickenStore_Lantern',b)
b=Build();b.box((0,0,.5),(.7,.08,1),'timber');asset('A_ToolBoard',b)
# Import only chosen source meshes. Keep their original UVs and atlas colours.
models=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models'
for short,fbx,height in [('Barrel','TFP_Wood_Barrel_01A',.95),('Bucket','TFP_Bucket_Metal_01A',.45),('Crate','TFP_Wooden_Crate_01A',.65),('Sack','TFP_Cereal_Sack_01A',.85),('Bottle','TFP_Milk_Bottle_01A',.25),('Fork','TFP_Spading_Fork_01A',1.1)]:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(models/(fbx+'.fbx')),use_anim=False)
 imported=[o for o in set(bpy.data.objects)-before if o.type=='MESH']
 chosen=max(imported,key=lambda o:len(o.data.polygons));me=chosen.data.copy();matrix=chosen.matrix_world.copy();me.transform(matrix)
 lo=[min(v.co[i] for v in me.vertices) for i in range(3)];hi=[max(v.co[i] for v in me.vertices) for i in range(3)]
 scale=height/(hi[2]-lo[2]);shift=Vector(((hi[0]+lo[0])/2,(hi[1]+lo[1])/2,lo[2]))
 for v in me.vertices:v.co=(v.co-shift)*scale
 me.materials.clear();me.materials.append(mat)
 for p in me.polygons:p.material_index=0
 name='R_'+short;o=bpy.data.objects.new(name,me);kit.objects.link(o);assets[name]=o;o['SourceFBX']=str((models/(fbx+'.fbx')).relative_to(ROOT));reused.append(o['SourceFBX'])
 for o2 in list(set(bpy.data.objects)-before):
  if o2!=o:bpy.data.objects.remove(o2,do_unlink=True)
def instance(key,pos,col,name=None,rot=0,scale=None):
 src=assets[key];o=src.copy();o.data=src.data;col.objects.link(o);o.name=name or key;o.location=pos;o.rotation_euler[2]=rot
 if scale:o.scale=scale
 return o
def export_clean(sc,obs,filename):
 # FBX exporter warns on repeated shared mesh IDs. Temporary single-user export
 # copies avoid that exporter limitation; the .blend retains linked instances.
 activate(sc);bpy.context.view_layer.update();temp=coll('TEMP EXPORT',sc);copies=[]
 for src in obs:
  cp=src.copy()
  if src.type=='MESH':cp.data=src.data.copy()
  temp.objects.link(cp);cp.parent=None;cp.matrix_world=src.matrix_world.copy();copies.append(cp)
 mapping=dict(zip(obs,copies))
 for src,cp in mapping.items():
  mw=cp.matrix_world.copy();cp.parent=mapping.get(src.parent);cp.matrix_world=mw
 bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT')
 for cp in copies:cp.select_set(True)
 bpy.context.view_layer.objects.active=copies[0]
 bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/filename),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
 for cp in copies:
  me=cp.data if cp.type=='MESH' else None;bpy.data.objects.remove(cp,do_unlink=True)
  if me and me.users==0:bpy.data.meshes.remove(me)
 bpy.data.collections.remove(temp)
levels=[]
for level,W,D,xs,Y in [(1,12,10,[-2.3,2.3],4.5),(2,16,12,[-4,0,4],5.5),(3,20,14,[-6,-2,2,6],6),(4,24,16,[-8,-4,0,4,8],6.7)]:
 sc=bpy.data.scenes.new(f'0{level} BARN INTERIOR - '+['MANUAL','MOTOR','AUTO','PROCESS'][level-1]);setup(sc);activate(sc)
 sc['Approval']='User approved sketch and Blender modeling 2026-10-14';sc['FootprintMetres']=[W,D];sc['GameplayCapacityUnchanged']=True
 structure=coll('01 SHELL modular',sc);props=coll('02 REUSED and adapted props',sc);equipment=coll('03 EQUIPMENT',sc);feed=coll('04 TROUGHS and removable fill',sc);pipes=coll('05 DISTRIBUTION',sc);markers=coll('06 SOCKETS - no geometry',sc)
 b=Build();b.box((0,D/2,-.09),(W,D,.18),'earth');floor=b.obj(f'Lv{level}_Floor',structure)
 # Sparse flat hay clumps: geometric strokes only, no alpha texture or grass overdraw.
 b=Build()
 for i in range(140+level*25):
  x=random.uniform(-W/2+.2,W/2-.2);y=random.uniform(.3,D-.3)
  if abs(x)<1 and y<D-3:continue
  for j in range(3):
   a=random.uniform(0,math.tau);dx,dy=math.cos(a)*.13,math.sin(a)*.13
   b.face([(x-dx,y-dy,.004),(x+dx,y+dy,.004),(x+dx+.018,y+dy-.018,.004)],'hay',(0,0,1))
 b.obj(f'Lv{level}_SparseStraw',structure)
 for i in range(W//2):instance('N01_Wall_2m',(-W/2+1+2*i,D+.06,0),structure)
 for side in [-1,1]:
  for i in range(D//2):instance('N01_WindowWall_2m' if i%3==1 else 'N01_Wall_2m',(side*(W/2+.06),1+2*i,0),structure,rot=-side*math.pi/2)
 for x in range(-W//2,W//2+1,4):
  instance('N01_Post',(x,D-.07,0),structure);instance('N01_Brace',(x+.10,D-.20,0),structure,scale=(-1 if x>0 else 1,1,1))
 for side in [-1,1]:
  for y in range(0,D+1,4):instance('N01_Post',(side*(W/2-.02),y,0),structure)
  for y in [D*.28,D*.64]:instance('R_ChickenStore_Lantern',(side*(W/2-.24),y,2),props,rot=-side*math.pi/2)
 # Front cutaway stubs leave a two-metre entrance.
 b=Build()
 for side in [-1,1]:b.box((side*(W/4+.5),0,.28),(W/2-1,.15,.56),'red');b.box((side*(W/4+.5),0,.6),(W/2-1,.20,.08),'wood')
 b.obj(f'Lv{level}_FrontCutaway',structure)
 for j,x in enumerate(xs):
  instance('N02_Trough_3Slots',(x,Y,0),feed,f'FeedTrough_{j+1:02}')
  for k,dy in enumerate([-1.2,0,1.2]):
   instance('N02_FeedFill',(x,Y+dy,0),feed,f'FeedFill_{j+1:02}_{k+1}')
   empty(f'FeedVisualSocket_{j+1:02}_{k+1}',(x,Y+dy,.4),markers)
 # Rear service strip. Main machines stay within footprint.
 machine='N03_Manual' if level==1 else 'N03_Motor' if level==2 else 'N03_LargeMotor'
 mx=0 if level==1 else -2.2 if level==2 else -4.3 if level==3 else -5.4
 my=D-1.25;instance(machine,(mx,my,0),equipment);empty('FeedMaker_Position',(mx,my,0),markers)
 instance('R_Crate',(mx,my-1.1,0),props,scale=(.8,.8,.8))
 if level>=3:
  instance('N04_Mixer',(mx+3,my,0),equipment);empty('FeedSilo_Position',(mx+3,my,0),markers)
 else:empty('FeedSilo_Position',(mx+1.8,my,0),markers)
 for side in [-1,1]:
  sx=side*(W/2-1.6)
  if level>1:
   instance('A_StorageRack',(sx,D-.65,0),props)
   for xx,zz in [(-.32,.1),(.32,.1),(-.32,.9)]:instance('A_HayBale_Mobile',(sx+xx,D-.65,zz),props,scale=(.65,.75,.78))
   instance('R_Sack',(sx+.34,D-.65,.9),props,scale=(.62,.62,.62))
  else:
   for xx,zz in [(-.4,0),(.4,0),(0,.8)]:instance('A_HayBale_Mobile',(sx+xx,D-.65,zz),props)
   instance('R_Sack',(sx,D-1.6,0),props)
  instance('R_Barrel',(side*(W/2-.65),D-1.8,0),props,scale=(.8,.8,.8))
  instance('R_Bucket',(side*(W/2-1.3),D-2,.0),props)
 # Water on left and right; one on manual stage.
 instance('A_WaterTrough',(-W/2+3.7,D-1.25,0),props,scale=(.75,1,1))
 if level>=2:instance('A_WaterTrough',(W/2-3.7,D-1.25,0),props,scale=(.75,1,1))
 if level>=2:
  cx=2.1 if level==2 else 3.2 if level==3 else 6.5
  instance('A_SupplyCabinet',(cx,D-.6,0),props)
  instance('A_ToolBoard',(.4,D-.17,1.35),props)
  instance('R_Fork',(.4,D-.32,1.35),props)
  for xx in [-.3,0,.3]:instance('R_Bottle',(cx+xx,D-.6,1.11),props)
 if level>=3:
  instance('A_Sink',(W/2-3.6,D-2.3,0),props)
  py=Y+1.2
  # All dispenser nozzles align above the rear compartment, and feed branch actually connects.
  left,right=xs[0]-.8,xs[-1]+.8
  span=right-left;count=math.ceil(span/2)
  for i in range(count):instance('N05_Pipe_2m',(left+i*span/count,py,2.7),pipes,scale=(span/count/2,1,1))
  for x in [left,right]:instance('N05_Support',(x,py,0),pipes)
  for x in xs:
   instance('N05_Tee',(x,py,2.7),pipes);instance('N05_Dispenser',(x,py,1.4),pipes)
   b=Build();cylinder(b,(x,py,2.35),(x,py,2.05),.06,'steel');b.obj('DropPipe',pipes)
  b=Build();cylinder(b,(mx+3,my,1),(mx+3,my,2.7),.06,'steel');cylinder(b,(mx+3,my,2.7),(mx+3,py,2.7),.06,'steel');b.obj('MixerSupplyConnection',pipes)
 if level==4:
  for x in [1.4,2.8]:instance('N06_ProcessTank',(x,D-1.2,0),equipment)
  b=Build();cylinder(b,(1.4,D-1.2,2.09),(1.4,D-1.2,2.35),.045,'steel');cylinder(b,(1.4,D-1.2,2.35),(2.8,D-1.2,2.35),.045,'steel');cylinder(b,(2.8,D-1.2,2.35),(2.8,D-1.2,2.09),.045,'steel');b.obj('TankBridgePipe',equipment)
  instance('A_BottleCounter',(4.8,D-1,0),props)
  for x in [4.35,4.65,4.95,5.25]:instance('R_Bottle',(x,D-1,.75),props)
 empty('Entrance',(0,0,0),markers);empty('PlayerSpawn',(0,1.7,0),markers)
 # Root group makes future scene placement straightforward; camera/lights excluded.
 root=empty(f'BarnInterior_Lv{level}_Root',(0,0,0),sc.collection)
 meshes=[o for o in sc.objects if o.type=='MESH']
 for o in list(sc.objects):
  if o!=root:o.parent=root
 export_clean(sc,[o for o in sc.objects if o.type in {'MESH','EMPTY'}],f'BarnInterior_Lv{level}.fbx')
 # Preview illumination only: no per-lantern lights exported.
 pc=coll('90 PREVIEW ONLY camera and lights',sc)
 for name,loc,power,size in [('Key',(-W*.4,-2,12),2600+level*1600,10),('Fill',(W*.5,D*.5,10),2000+level*1200,10)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power*.40;data.size=size;o=bpy.data.objects.new(name,data);pc.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,D*.55,0))-o.location).to_track_quat('-Z','Y').to_euler()
 sc.world.node_tree.nodes['Background'].inputs[1].default_value=.42
 cam=bpy.data.objects.new('Preview Camera',bpy.data.cameras.new('Preview Camera'));pc.objects.link(cam);sc.camera=cam
 cam.location=(W*.52,-D*.72,W*.95);target=Vector((0,D*.52,.3));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=W*(1.60 if level<=2 else 1.42)
 sc.cycles.samples=12;sc.render.resolution_x=1440;sc.render.resolution_y=1080
 sc.render.filepath=str(ART/'Previews'/f'Lv{level}_Overview.png')
 tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
 stats={'level':level,'footprint':[W,D],'triangles':tris,'meshVerticesInstanced':sum(len(o.data.vertices) for o in meshes),'meshObjects':len(meshes),'uniqueMeshes':len({o.data.name for o in meshes}),'materials':sorted({m.name for o in meshes for m in o.data.materials}),'budget':[6000,8000,12000,15000][level-1]}
 levels.append(stats)
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
# Lay out modular assets in separate kit scene, without extra runtime duplicates.
activate(library)
for key,o in assets.items():
 if not key.startswith('R_'):export_clean(library,[o],key+'.fbx')
for i,o in enumerate(assets.values()):o.location=((i%6)*3,(i//6)*4,0)
manifest={'status':'BUILT','atlasGUID':audit['sourceGUID'],'newRuntimeTextures':0,'reuseSources':reused,'levels':levels,'modules':{k:{'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertices':len(o.data.vertices)} for k,o in assets.items()},'notes':['Counts exclude camera/lights and animals; include all repeated source props and fill.','Unity map and gameplay untouched.','Four approved compact footprints; no automatic replacement of existing placeholders.']}
(ART/'Previews/MeshManifest.json').write_text(json.dumps(manifest,indent=2))
(ART/'References/atlas-audit.json').write_text(json.dumps({'sourcePSD':audit['sourcePSD'],'sourceGUID':audit['sourceGUID'],'sourceSha256':audit['sourceSha256'],'swatches':PAL},indent=2))
activate(bpy.data.scenes['01 BARN INTERIOR - MANUAL'])
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/BarnInterior_A.blend'))
print('BARN_INTERIOR_BUILD_COMPLETE',json.dumps(levels))
if '--render' in sys.argv:
 for sc in [s for s in bpy.data.scenes if s.name.startswith(('01 BARN','02 BARN','03 BARN','04 BARN'))]:activate(sc);bpy.ops.render.render(write_still=True)
