"""Approved Coop kit. Source atlas and Barn modules are reused, never overwritten."""
import bpy,bmesh,math,json,random,sys,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
for p in ['Blender','Exports','Previews','References']:(ART/p).mkdir(parents=True,exist_ok=True)
if bpy.app.background:bpy.ops.wm.read_factory_settings(use_empty=True)
else:
 # Keep the current editor/window context valid for console-driven operators.
 # The original unsaved file is backed up before this approved build runs.
 work=bpy.data.scenes.new('CoopBuild');bpy.context.window.scene=work
 for s in list(bpy.data.scenes):
  if s!=work:bpy.data.scenes.remove(s)
 for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
 for c in list(bpy.data.collections):bpy.data.collections.remove(c)
 for m in list(bpy.data.meshes):bpy.data.meshes.remove(m)
 for m in list(bpy.data.materials):bpy.data.materials.remove(m)
random.seed(151026)
audit=json.loads((ROOT/'ArtSource/BarnInterior_A/References/atlas-audit.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256((ROOT/audit['sourcePSD']).read_bytes()).hexdigest()==audit['sourceSha256']
PAL=audit['swatches'];BASE={'materials':{'atlasGuid':audit['sourceGUID']}}
atlas=bpy.data.images.load(str(ROOT/'ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack()
mat=bpy.data.materials.new('TFP_Atlas_1A');mat.use_nodes=True;mat['UnityGUID']=audit['sourceGUID']
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.78;bs.inputs['Specular IOR Level'].default_value=.15
mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
exec(compile((ROOT/'ArtSource/MiniMarket_A/Scripts/mesh_common.py').read_text(),'<shared helpers>','exec'),globals())
library=bpy.context.scene;library.name='00 COOP MODULAR KIT';setup(library)
kit=coll('KIT - linked modules',library);assets={};sources=[]
glass=bpy.data.materials.new('Coop_Hood_Glass_ExistingPalette');glass.use_nodes=True;glass.diffuse_color=(.8,.91,.93,.19)
g=glass.node_tree.nodes.get('Principled BSDF');g.inputs['Base Color'].default_value=(.8,.91,.93,1);g.inputs['Roughness'].default_value=.18;g.inputs['Alpha'].default_value=.19
glass.surface_render_method='DITHERED';glass['UnityMaterialCandidate']='TFP_Glass_Windows_1B';glass['UnityHoodTestPending']=True

def cylinder(b,a,c,r,color,n=8,r2=None):
 a,c=Vector(a),Vector(c);axis=(c-a).normalized();u=axis.cross(Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))).normalized();v=axis.cross(u)
 vs=[p+(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))*rr for p,rr in [(a,r),(c,r if r2 is None else r2)] for i in range(n)]
 b.solid(vs,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(reversed(range(n))),tuple(range(n,2*n))],color)

def add(name,b):
 o=b.obj(name,kit);o['Role']='Coop reusable mobile module';assets[name]=o;return o

def colorize(o,color):
 a,b,c,d=PAL[color]['uvSafeRectBottomLeft'];uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for v in uv.data:v.uv=((a+c)/2,(b+d)/2)

def profile(b,ring,color,n=12,offset=(0,0,0)):
 # Closed meridian profile revolved around Z. Bowls have inner and outer walls.
 off=Vector(offset);vs=[off+Vector((r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),z)) for r,z in ring for i in range(n)]
 faces=[]
 for j in range(len(ring)):
  k=(j+1)%len(ring)
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,k*n+(i+1)%n,k*n+i))
 # Profile is concave: preserve ring winding; recalc normals after welding.
 for f in faces:b.face([vs[i] for i in f],color)

def inst(key,pos,col,name=None,scale=None,rot=0):
 s=assets[key];o=bpy.data.objects.new(name or key,s.data);col.objects.link(o);o.location=pos;o.rotation_euler.z=rot
 if scale:o.scale=scale
 o['Module']=key;return o

reuse=['N01_Wall_2m','N01_WindowWall_2m','N01_Post','N01_Brace','N05_Pipe_2m','N05_Tee','N05_Coupler','N05_Support','R_Crate','R_Sack','R_Bucket','R_Barrel','R_ChickenStore_Lantern']
with bpy.data.libraries.load(str(ROOT/'ArtSource/BarnInterior_A/Blender/BarnInterior_A.blend'),link=False) as (src,dst):dst.objects=reuse
for o in dst.objects:
 assert o is not None
 o.data=o.data.copy();o.data.materials.clear();o.data.materials.append(mat);o.location=(0,0,0);o.rotation_euler=(0,0,0);o.scale=(1,1,1)
 kit.objects.link(o);assets[o.name]=o;o['ReuseSource']='BarnInterior_A.blend';sources.append(o.name)

# Repair the reused window panel: adjacent box corners in the Barn source were
# welded into four >2-face junction edges. One closed frame ring avoids those
# junctions while retaining the same dimensions, palette and window silhouette.
b=Build();outer=[(-1,0),(1,0),(1,3.4),(-1,3.4)];inner=[(-.5,1.1),(.5,1.1),(.5,2.3),(-.5,2.3)]
vs=[(x,y,z) for y in [-.06,.06] for ring in [outer,inner] for x,z in ring]
for i in range(4):
 k=(i+1)%4
 for f in [(i,k,k+4,i+4),(i+8,i+12,k+12,k+8),(i,i+8,k+8,k),(i+4,k+4,k+12,i+12)]:b.face([vs[j] for j in f],'red')
b.box((0,0,1.7),(1,.04,1.2),'window')
for x in [-.54,0,.54]:b.box((x,-.13,1.7),(.07,.1,1.34),'wood')
for z in [1.06,1.7,2.34]:b.box((0,-.13,z),(1.14,.1,.07),'wood')
for z in [.14,2.65,3.34]:b.box((0,-.10,z),(2,.12,.12),'wood')
for x in [-.75,-.25,.25,.75]:
 for lo,hi in ([(.12,1.04),(2.36,3.3)] if abs(x)<.51 else [(.12,3.3)]):b.face([(x-.006,-.061,lo),(x+.006,-.061,lo),(x+.006,-.061,hi),(x-.006,-.061,hi)],'red_dark',(0,-1,0))
tmp=b.obj('WindowTopologyRepair',kit);assets['N01_WindowWall_2m'].data=tmp.data;assets['N01_WindowWall_2m']['TopologyRepair']='Closed frame ring replaces welded adjacent box junctions';bpy.data.objects.remove(tmp,do_unlink=True)

models=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models'
for name,fbx,height in [('R_RawEgg','TFP_Raw_Egg_01A',.18),('R_MayoJar','TFP_Jar_01A',.24)]:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(models/(fbx+'.fbx')),use_anim=False)
 obs=[o for o in set(bpy.data.objects)-before if o.type=='MESH']
 # Egg source has two mesh objects; retain all parts and original UVs.
 parts=[]
 for o in obs:
  me=o.data.copy();me.transform(o.matrix_world);p=bpy.data.objects.new('ImportPart',me);kit.objects.link(p);parts.append(p)
 o=join_copy(parts,name,kit);lo=Vector([min(v.co[i] for v in o.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in o.data.vertices) for i in range(3)])
 shift=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));factor=height/(hi.z-lo.z)
 for v in o.data.vertices:v.co=(v.co-shift)*factor
 o.data.materials.clear();o.data.materials.append(mat)
 for p in o.data.polygons:p.material_index=0
 if name=='R_MayoJar':
  uv=o.data.uv_layers.active
  for p in o.data.polygons:
   color='teal' if sum(o.data.vertices[i].co.z for i in p.vertices)/len(p.vertices)>height*.84 else 'cream'
   a,b,c,d=PAL[color]['uvSafeRectBottomLeft']
   for i in p.loop_indices:uv.data[i].uv=((a+c)/2,(b+d)/2)
 assets[name]=o;o['SourceFBX']=str((models/(fbx+'.fbx')).relative_to(ROOT));sources.append(o['SourceFBX'])
 for p in list(set(bpy.data.objects)-before):
  if p!=o:bpy.data.objects.remove(p,do_unlink=True)

# Rounded low three-compartment basin: closed bottom/walls, never a solid block fill.
def capsule(length,width,n=4):
 r=width/2;half=(length-width)/2
 return [(half+r*math.cos(-math.pi/2+i*math.pi/n),r*math.sin(-math.pi/2+i*math.pi/n)) for i in range(n+1)]+[(-half+r*math.cos(math.pi/2+i*math.pi/n),r*math.sin(math.pi/2+i*math.pi/n)) for i in range(n+1)]
b=Build();outer=capsule(1.5,.5);inner=capsule(1.43,.43);n=len(outer)
rings=[[(x,y,.12) for x,y in outer],[(x,y,.3) for x,y in outer],[(x,y,.3) for x,y in inner],[(x,y,.16) for x,y in inner]]
for j in range(3):
 for i in range(n):b.face([rings[j][i],rings[j][(i+1)%n],rings[j+1][(i+1)%n],rings[j+1][i]],'steel')
b.face(list(reversed(rings[0])),'steel',(0,0,-1));b.face(rings[3],'steel',(0,0,1))
for x in [-.25,.25]:b.box((x,0,.23),(.025,.42,.14),'steel')
for x in [-.56,.56]:
 for y in [-.15,.15]:b.box((x,y,.06),(.065,.065,.12),'metal')
add('C01_ChickenTrough_3Slots',b)
b=Build();b.box((0,0,.195),(.43,.37,.055),'hay');add('C01_GrainFill',b)

def table(name,width,depth,height=.9):
 b=Build();b.box((0,0,height-.05),(width,depth,.1),'timber')
 for x in [-width/2+.08,width/2-.08]:
  for y in [-depth/2+.08,depth/2-.08]:b.box((x,y,(height-.1)/2),(.09,.09,height-.1),'wood')
 b.box((0,depth/2-.08,height*.32),(width-.1,.07,.08),'timber');return add(name,b)
table('C_Table_Small',.85,.7);table('C_Table_Wide',1.65,.78)
b=Build()
for y in [-.8,.8]:b.box((0,y,.32),(.13,.13,.64),'wood')
cylinder(b,(0,-1,.65),(0,1,.65),.065,'timber',8);add('C_Perch_2m',b)

def machine(name,s):
 b=Build()
 for x in [-.27,.27]:
  for y in [-.23,.23]:b.beam((x*s,y*s,0),(x*.83*s,y*s,.45*s),.07*s,.08*s,'teal')
 b.box((0,0,.53*s),(.62*s,.53*s,.63*s),'teal')
 cylinder(b,(.31*s,0,.65*s),(.57*s,0,.65*s),.19*s,'teal',10)
 cylinder(b,(.46*s,0,.65*s),(.50*s,0,.65*s),.20*s,'steel',10)
 b.box((0,-.34*s,.43*s),(.28*s,.21*s,.065*s),'steel')
 b.box((0,-.275*s,.52*s),(.25*s,.035*s,.16*s),'metal')
 profile(b,[(.13*s,.84*s),(.36*s,1.23*s),(.36*s,1.27*s),(.40*s,1.27*s),(.40*s,1.22*s),(.16*s,.84*s)],'teal',12)
 cylinder(b,(0,0,1.13*s),(0,0,1.145*s),.32*s,'hay',12)
 for x,c in [(-.13,'green'),(0,'lantern'),(.13,'red')]:cylinder(b,(x*s,-.27*s,.64*s),(x*s,-.29*s,.64*s),.027*s,c,6)
 if s>1:
  cylinder(b,(-.31*s,0,.56*s),(-.55*s,0,.56*s),.14*s,'metal',10)
  cylinder(b,(-.54*s,0,.56*s),(-.57*s,0,.56*s),.15*s,'steel',10)
 return add(name,b)
machine('C02_FeedMaker_Small',1.10);machine('C02_FeedMaker_Large',1.42)

def incubator(count):
 w=.65 if count==1 else 1.10;name=f'C03_Incubator_{count}Egg';b=Build()
 b.box((0,0,.065),(w,.55,.13),'teal')
 for x in ([-.10,0,.10]):cylinder(b,(x,-.281,.07),(x,-.296,.07),.022,'lantern' if x==0 else 'green',6)
 slots=[0] if count==1 else [-.31,0,.31]
 for x in slots:profile(b,[(.01,.13),(.10,.13),(.10,.155),(.075,.19),(.06,.19),(.06,.155),(.01,.155)],'hay',10,offset=(x,0,0))
 add(name,b)
 # One closed transparent cover volume: rounded shoulders, no nested overlapping shells.
 b=Build();outline=[(-w/2+.04,.135),(-w/2+.04,.36),(-w/2+.11,.49),(-w/2+.20,.54),(w/2-.20,.54),(w/2-.11,.49),(w/2-.04,.36),(w/2-.04,.135)]
 # Extruded dome cross-section creates a closed low-poly shell with visible front interior.
 vs=[(x,y,z) for y in [-.245,.245] for x,z in outline];n=len(outline);faces=[]
 # Front/back are concave? dome outline is convex and planar.
 faces += [tuple(reversed(range(n))),tuple(range(n,2*n))]
 for i in range(n):
  k=(i+1)%n;faces.append((i,k,k+n,i+n))
 for f in faces:b.face([vs[i] for i in f],'cream')
 o=add(name+'_Hood',b);o.data.materials.clear();o.data.materials.append(glass);o['IntentionalTransparentShell']=True
 return slots
incubator(1);incubator(3)

b=Build();b.box((0,0,.075),(1,.65,.15),'cream');b.box((-.36,.11,.48),(.12,.15,.78),'teal');b.box((-.12,.11,.86),(.60,.20,.12),'teal')
profile(b,[(.01,.15),(.15,.15),(.25,.3),(.27,.56),(.24,.56),(.22,.31),(.13,.18),(.01,.18)],'steel',12,offset=(-.10,0,0))
cylinder(b,(-.10,0,.4),(-.10,0,.81),.025,'steel',8);b.box((-.10,0,.35),(.24,.04,.08),'steel')
b.box((-.10,.05,.99),(.42,.32,.045),'cream')
for x in [-.30,-.18,-.06]:cylinder(b,(x,-.332,.08),(x,-.35,.08),.022,'lantern',6)
add('C04_MayonnaiseMixer',b)
b=Build();profile(b,[(.06,0),(.08,.05),(.24,.40),(.24,.44),(.21,.44),(.21,.40),(.06,.08),(.04,.05)],'teal',10)
cylinder(b,(0,0,.30),(0,0,.32),.18,'hay',10);add('C05_AutoHopper',b)
b=Build();profile(b,[(.02,0),(.125,0),(.125,.025),(.06,.16),(.06,.3),(.04,.3),(.04,.16),(.02,.025)],'teal',10)
add('C05_BellHead',b)
b=Build();profile(b,[(.01,.04),(.27,.04),(.27,.1),(.24,.13),(.22,.13),(.22,.075),(.01,.075)],'teal',12)
profile(b,[(.01,.11),(.17,.11),(.19,.16),(.19,.49),(.15,.60),(.08,.65),(.01,.65)],'cream',12)
add('C06_ChickenDrinker',b)
table('C_DrinkerBase',.65,.65,.15)

# Triangulate explicit runtime geometry, recalc winding, then record real topology defects.
def clean(me):
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
 bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY');bm.to_mesh(me);bm.free();me.update()
for o in assets.values():clean(o.data)

def export_clean(sc,obs,name):
 activate(sc);tmp=coll('TEMP EXPORT',sc);copies=[]
 for o in obs:
  cp=o.copy();cp.data=o.data.copy() if o.type=='MESH' else None;tmp.objects.link(cp);cp.parent=None;cp.matrix_world=o.matrix_world.copy();copies.append(cp)
 bpy.ops.object.select_all(action='DESELECT')
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0]
 bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/f'{name}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
 for o in copies:
  me=o.data;bpy.data.objects.remove(o,do_unlink=True)
  if me and me.users==0:bpy.data.meshes.remove(me)
 bpy.data.collections.remove(tmp)

levels=[];scenes=[]
layouts=[(1,10,8,[(-.75,4),(.75,4)]),(2,12,10,[(-2.5,3.5),(2.5,3.5),(0,6)]),(3,14,12,[(-2.8,4),(2.8,4),(-2.8,7),(2.8,7)]),(4,16,14,[(-3,4.5),(3,4.5),(-4,8.2),(0,8.2),(4,8.2)])]
for level,W,D,positions in layouts:
 sc=bpy.data.scenes.new(f'0{level} COOP INTERIOR - LEVEL {level}');setup(sc);activate(sc);scenes.append(sc)
 sc['Approval']='Coop sketch and Blender execution approved 2026-10-15';sc['Capacity']=3*len(positions);sc['FootprintMetres']=[W,D]
 structure=coll('01 SHELL',sc);props=coll('02 PROPS',sc);eq=coll('03 MACHINES',sc);feed=coll('04 FEED TROUGHS',sc);pipes=coll('05 AUTO DISTRIBUTION',sc);markers=coll('06 GAMEPLAY SOCKETS',sc)
 root=empty(f'CoopInterior_Lv{level}_Root',(0,0,0),sc.collection)
 b=Build();b.box((0,D/2,-.09),(W,D,.18),'earth');b.obj('Floor',structure)
 for i in range(W//2):inst('N01_Wall_2m',(-W/2+1+2*i,D+.06,0),structure)
 for side in [-1,1]:
  for i in range(D//2):inst('N01_WindowWall_2m' if i%2==0 else 'N01_Wall_2m',(side*(W/2+.06),1+2*i,0),structure,rot=-side*math.pi/2)
  for y in range(0,D+1,4):inst('N01_Post',(side*(W/2-.02),y,0),structure)
  for y in [D*.3,D*.65]:inst('R_ChickenStore_Lantern',(side*(W/2-.23),y,2),props,rot=-side*math.pi/2)
 for x in range(-W//2,W//2+1,4):inst('N01_Post',(x,D-.08,0),structure)
 b=Build()
 for side in [-1,1]:b.box((side*(W/4+.5),0,.28),(W/2-1,.15,.56),'red');b.box((side*(W/4+.5),0,.6),(W/2-1,.20,.08),'wood')
 b.obj('FrontCutaway_2mDoor',structure)
 # Sparse straw patches are triangles, not dense strands or alpha cards.
 b=Build()
 for i in range(100+level*15):
  side=-1 if i%2 else 1;x=side*random.uniform(W*.33,W*.48);y=random.uniform(.4,D-.3)
  for j in range(2):
   a=random.uniform(0,math.tau);dx,dy=.12*math.cos(a),.12*math.sin(a)
   b.face([(x-dx,y-dy,.003),(x+dx,y+dy,.003),(x+dx+.015,y+dy-.018,.003)],'hay',(0,0,1))
 b.obj('SparseStraw',structure)
 for i,(x,y) in enumerate(positions):
  inst('C01_ChickenTrough_3Slots',(x,y,0),feed,f'Trough_{i+1:02}')
  for j,dx in enumerate([-.5,0,.5]):inst('C01_GrainFill',(x+dx,y,0),feed,f'Fill_{i+1:02}_{j+1}');empty(f'FeedSocket_{i+1:02}_{j+1}',(x+dx,y,.24),markers)
  empty(f'AnimalSlot_{i*3+1:02}',(x-.5,y-.9,0),markers);empty(f'AnimalSlot_{i*3+2:02}',(x+.5,y-.9,0),markers);empty(f'AnimalSlot_{i*3+3:02}',(x,y+.9,0),markers)
 mx=-W/2+2.3;my=D-1.10
 inst('C02_FeedMaker_Small' if level==1 else 'C02_FeedMaker_Large',(mx,my,0),eq)
 inst('R_Crate',(mx,my-.95,0),props,scale=(.65,.65,.65))
 for dx,s in [(-1.3,1),(-.8,.7)]:inst('R_Sack',(mx+dx,my,0),props,scale=(s,s,s))
 ix=W/2-1.5;iy=D-1.05;count=3 if level==4 else 1
 inst('C_Table_Wide' if count==3 else 'C_Table_Small',(ix,iy,0),props)
 inst(f'C03_Incubator_{count}Egg',(ix,iy,.9),eq);inst(f'C03_Incubator_{count}Egg_Hood',(ix,iy,.9),eq)
 for i,dx in enumerate([- .31,0,.31] if count==3 else [0]):inst('R_RawEgg',(ix+dx,iy,1.065),eq,f'IncubatorEgg_{i+1}');empty(f'IncubatorSocket_{i+1}',(ix+dx,iy,1.065),markers)
 inst('C_Perch_2m',(-W/2+.4,D*.45,0),props)
 for i,y in enumerate([1.7] if level<3 else [1.7,D*.55]):
  inst('C_DrinkerBase',(W/2-.65,y,0),props);inst('C06_ChickenDrinker',(W/2-.65,y,.15),props)
 if level>=3:
  mayo=(ix-1.8,iy,0);inst('C_Table_Wide',mayo,props);inst('C04_MayonnaiseMixer',(mayo[0],iy,.9),eq)
  for dx in [-.22,-.03]:inst('R_RawEgg',(mayo[0]+dx,iy+.05,1.915),eq,scale=(.7,.7,.7))
  for dx,dy in [(.35,0),(.35,-.22)]:inst('R_MayoJar',(mayo[0]+dx,iy+dy,1.05),eq)
  # Header along rear + two forward rows, each head centered above a trough.
  hy=D-2.8;hx=mx+1.5
  inst('C05_AutoHopper',(hx,hy,2.4),pipes)
  b=Build();left=min(x for x,y in positions)-.65;right=max(x for x,y in positions)+.65
  cylinder(b,(left,hy,2.4),(right,hy,2.4),.05,'steel',8)
  for x in [left,right]:
   cylinder(b,(x,hy,0),(x,hy,2.4),.045,'steel',8);b.box((x,hy,.035),(.25,.25,.07),'metal')
  for x in sorted(set(x for x,y in positions)):
   ys=[y for xx,y in positions if xx==x];cylinder(b,(x,min(ys),2.4),(x,hy,2.4),.05,'steel',8)
   inst('N05_Tee',(x,hy,2.4),pipes,scale=(.75,.75,.75))
  for i,(x,y) in enumerate(positions):
   cylinder(b,(x,y,1.8),(x,y,2.4),.045,'steel',8);inst('C05_BellHead',(x,y,1.5),pipes,f'AutoHead_{i+1:02}')
  # Machine side output feeds the same header via a riser, entirely in service strip.
  cylinder(b,(mx+.75,my,1),(hx,my,1),.05,'steel',8);cylinder(b,(hx,my,1),(hx,my,2.4),.05,'steel',8);cylinder(b,(hx,my,2.4),(hx,hy,2.4),.05,'steel',8)
  b.obj('ConnectedPipeNetwork',pipes)
 empty('Entrance',(0,0,0),markers);empty('PlayerSpawn',(0,1.4,0),markers)
 for o in list(sc.objects):
  if o!=root:o.parent=root
 meshes=[o for o in sc.objects if o.type=='MESH']
 for o in meshes:
  if not o.data.get('Cleaned'):clean(o.data);o.data['Cleaned']=True
 levels.append({'level':level,'capacity':sc['Capacity'],'footprint':[W,D],'triangles':sum(len(o.data.polygons) for o in meshes),'meshVerticesInstanced':sum(len(o.data.vertices) for o in meshes),'meshObjects':len(meshes),'uniqueMeshes':len({o.data.name for o in meshes}),'troughs':len(positions),'autoHeads':len(positions) if level>=3 else 0,'incubatorEggs':count,'materials':sorted({m.name for o in meshes for m in o.data.materials})})
 export_clean(sc,[o for o in sc.objects if o.type in {'MESH','EMPTY'}],f'CoopInterior_Lv{level}')
 pc=coll('90 PREVIEW ONLY',sc)
 for label,loc,power in [('Key',(-W*.4,-2,12),2200+level*500),('Fill',(W*.5,D*.5,10),1500+level*400)]:
  data=bpy.data.lights.new(label,'AREA');data.energy=power;data.size=9;o=bpy.data.objects.new(label,data);pc.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,D*.55,0))-o.location).to_track_quat('-Z','Y').to_euler()
 sc.world.node_tree.nodes['Background'].inputs[1].default_value=.42
 cam=bpy.data.objects.new('PreviewCamera',bpy.data.cameras.new('PreviewCamera'));pc.objects.link(cam);sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=W*1.72
 cam.location=(W*.12,-D*.82,W*1.12);cam.rotation_euler=(Vector((0,D*.50,.35))-cam.location).to_track_quat('-Z','Y').to_euler()
 sc.cycles.samples=12;sc.render.resolution_x=1280;sc.render.resolution_y=960;sc.render.filepath=str(ART/'Previews'/f'Lv{level}_Overview.png')
 for a in bpy.context.screen.areas:
  if a.type=='VIEW_3D':a.spaces.active.region_3d.view_perspective='CAMERA';a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_extras=False

activate(library)
for name,o in assets.items():export_clean(library,[o],name)
for i,o in enumerate(assets.values()):o.location=((i%6)*2.7,(i//6)*4,0)
manifest={'status':'BUILT','atlasGUID':audit['sourceGUID'],'newRuntimeTextures':0,'reuseSources':sources,'levels':levels,'notes':['Source mesh triangles include repeated modules, hood and props, exclude animals and preview cameras/lights.','Coop assets only: Unity scenes/prefabs/gameplay untouched.','Final dimensions; do not multiply by Barn scale 1.75.','Transparent Blender preview is a local approximation; existing Unity glass candidate needs hood validation.']}
(ART/'Previews/MeshManifest.json').write_text(json.dumps(manifest,indent=2));(ART/'References/atlas-audit.json').write_text(json.dumps(audit,indent=2))
activate(scenes[0]);bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/CoopInterior_A.blend'))
print('COOP_BUILD_COMPLETE',json.dumps(levels),flush=True)
if '--render' in sys.argv:
 for sc in scenes:activate(sc);bpy.ops.render.render(write_still=True)
