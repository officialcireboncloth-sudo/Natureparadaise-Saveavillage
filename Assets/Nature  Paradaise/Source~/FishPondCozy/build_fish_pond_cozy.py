"""Reference Lv1 stone pond. Editable parts, shared Cozy atlas, independent Toonpack water.
Unity-local metres, front +Z; existing compact root scale remains 0.4.
"""
from pathlib import Path
library_path=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py'
exec(compile(library_path.read_text().split('\nfor name,builder in ')[0],str(library_path),'exec'),globals())
HELPERS=HELPERS.replace('ArtSource/PoultryCozy','ArtSource/FishPondCozy').replace('Source~/PoultryCozy','Source~/FishPondCozy').replace('mesh/Buildings/PoultryCozy','mesh/Buildings/FishPondCozy')
begin('Fish Pond Cozy Lv1')
def cv(p):return Vector((-p[0],-p[2],p[1]))
SOURCE=A/'Source~/FishPondCozy';MODELS=A/'mesh/Buildings/FishPondCozy';OUT=ROOT/'ArtSource/FishPondCozy'
for p in (SOURCE,MODELS,OUT/'Previews'):p.mkdir(parents=True,exist_ok=True)
E={s['name']:s for s in json.loads((A/'Textures/Shared/Cozy/NP_CozyAtlasSlots_03.json').read_text())['slots']};S.update(E)
ext=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_03_BaseColor.png'));ext.pack()
ext_normal=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_03_Normal.png'));ext_normal.colorspace_settings.name='Non-Color';ext_normal.pack()
extension=mat('NP_CozySurfaces_03',ext)
for node in extension.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image==normal:node.image=ext_normal
M.append(extension)
base_assign=assign
aliases={'WoodHoney':'PondWood','WoodDark':'PondDark','RiverStone':'PondRock','RiverStoneMoss':'PondRockMoss','Iron':'PondIron','PaintTeal':'PondTeal','BrassWarm':'PondBrass','Burlap':'PondBurlap','Rope':'PondRope','Terracotta':'PondBrass'}
wood_uv_audit=[]
def assign(obj,slot,mi=0,rect=None):
 if mi!=1:slot=aliases.get(slot,slot)
 if slot in E:mi=3
 base_assign(obj,slot,mi,rect)
 if slot in ('PondWood','PondDark') and rect is None:
  # Metre-based crops preserve horizontal grain and knot proportions on narrow timbers.
  co=[v.co for v in obj.data.vertices];lo=[min(p[i] for p in co) for i in range(3)];hi=[max(p[i] for p in co) for i in range(3)];spans=[hi[i]-lo[i] for i in range(3)];longest=max(range(3),key=lambda i:spans[i]);seed_u=random.random();seed_v=random.random()
  for p in obj.data.polygons:
   axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=sorted([i for i in range(3) if i!=axis],key=lambda i:spans[i],reverse=True);end=axis==longest
   r=region('PondEnd' if end else slot);du=min(.95,max(.025,spans[axes[0]]/(1.2 if end else 4.2)));dv=min(.95,max(.025,spans[axes[1]]/(1.2 if end else 1.45)));u0=r[0]+seed_u*(1-du)*(r[2]-r[0]);v0=r[1]+seed_v*(1-dv)*(r[3]-r[1])
   for li in p.loop_indices:
    v=co[obj.data.loops[li].vertex_index];u=(v[axes[0]]-lo[axes[0]])/max(1e-5,spans[axes[0]]);vv=(v[axes[1]]-lo[axes[1]])/max(1e-5,spans[axes[1]])
    obj.data.uv_layers.active.data[li].uv=(u0+u*du*(r[2]-r[0]),v0+vv*dv*(r[3]-r[1]))
  wood_uv_audit.append(dict(object=obj.name,lengthMetres=max(spans),grainDirection='longest face dimension maps to texture U',crossGrainMetres=1.45))
 return obj

def rock(name,c,radii,moss=False,detail=0):
 # Broad irregular faces with softened shoulders, not uniform faceted cubes.
 x,y,z=c;n=9;offset=random.uniform(0,.7)
 radial=[random.uniform(.91,1.08) for _ in range(n)]
 rings=[(-.44,.72),(-.20,1),(.25,.93),(.48,.62)]
 vv=[(x+math.cos(offset+i*math.tau/n)*radii[0]*scale*radial[i],y+yy*radii[1]*2,z+math.sin(offset+i*math.tau/n)*radii[2]*scale*radial[i]) for yy,scale in rings for i in range(n)]
 ff=[tuple(range(n-1,-1,-1)),tuple(range(3*n,4*n))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(3) for i in range(n)]
 slot='PondRockMoss' if moss else 'PondRock';r=region(slot);u=random.uniform(0,.40);v=random.uniform(0,.3)
 rect=(r[0]+u*(r[2]-r[0]),r[1]+v*(r[3]-r[1]),r[0]+(u+.55)*(r[2]-r[0]),r[1]+(v+.65)*(r[3]-r[1]))
 o=mesh(name,vv,ff,slot,detail,rect=rect);bevel(o,.035)
 for face in o.data.polygons:face.use_smooth=True
 bpy.context.view_layer.objects.active=o;wn=o.modifiers.new('Broad weighted rock facets','WEIGHTED_NORMAL');wn.keep_sharp=True;wn.weight=35;bpy.ops.object.modifier_apply(modifier=wn.name)
 return o

def closed_ring(name,outer,inner,top,bottom,slot):
 n=len(outer);vv=[(x,y,z) for y,poly in [(bottom,outer),(top,outer),(top,inner),(bottom,inner)] for x,z in poly]
 ff=[]
 for i in range(n):
  j=(i+1)%n;ff.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i),(2*n+i,2*n+j,3*n+j,3*n+i),(3*n+i,3*n+j,j,i)])
 o=mesh(name,vv,ff,slot);o['KeepLOD']=True;return o

def round_rect(cx,cz,w,d,r,segments=5):
 return [(cx+sx*(w/2-r)+r*math.cos(a),cz+sz*(d/2-r)+r*math.sin(a)) for sx,sz,start in [(1,1,0),(-1,1,90),(-1,-1,180),(1,-1,270)] for a in [math.radians(start+i*90/segments) for i in range(segments+1)]]

# Keep the established water volume, compact placement envelope and swim height.
inner=round_rect(-1.5,0,6.8,4.8,.35)
outer=round_rect(-1.5,0,8,6,.7)
closed_ring('Continuous stone basin under irregular boulders',outer,inner,.58,.025,'RiverStone')
for col in range(3):
 for row in range(2):
  x0=-4.9+col*6.8/3;x1=x0+6.8/3;z0=-2.4+row*2.4;z1=z0+2.4
  floor=mesh('Continuous tiled pebble bed',[(x0,.10,z0),(x0,.10,z1),(x1,.10,z1),(x1,.10,z0)],[(0,1,2,3)],'PondPebbles');floor['KeepLOD']=True;floor['SurfaceUp']=True
for i in range(10):
 x=-5.03+(i+.5)*7.06/10
 for z in [-2.67,2.67]:rock('Individual mossy bank boulder',(x,.43,z),(.46,random.uniform(.35,.39),.43),moss=i%3!=1)
for side in [-1,1]:
 for i in range(6):rock('Individual side river boulder',(-1.5+side*3.68,.43,-2.25+(i+.5)*4.5/6),(.42,random.uniform(.35,.39),.48),moss=i%3==0)
for x,z in [(-4.98,-2.43),(1.98,-2.43),(-4.98,2.43),(1.98,2.43)]:rock('Rounded corner coping',(x,.43,z),(.48,.38,.48),True)
# Fine underwater pebbles belong to the reusable bed texture, preserving mobile geometry.

# Front-left landing: all railings stay behind/beside the deck, clear stair approach.
dx=.55;zfront=3.05;zback=.25;deck_top=.95
for x in [dx-1.68,dx+1.68]:
 for z in [zback+.12,zfront-.14]:box('Deck support foot',(x,.48,z),(.19,.96,.19),'WoodDark',.025)
for x in [dx-1.42,dx+1.42]:box('Deck underside stringer',(x,.77,1.65),(.16,.25,3.0),'WoodDark',.02)
for i in range(9):
 z=zback+(i+.5)*(zfront-zback)/9
 box('Individual warm deck plank',(dx,.885,z),(3.60,.13,(zfront-zback)/9-.015),'WoodHoney',.022)
 for x in [dx-1.47,dx+1.47]:cyl('Deck iron nail',(x,.955,z),.025,.010,'Iron',8,1)
for x in [dx-1.68,dx+1.68]:
 box('Rear deck balustrade post',(x,1.35,.42),(.17,1.06,.17),'WoodHoney',.025)
 box('Balustrade post cap',(x,1.90,.42),(.23,.07,.23),'WoodHoney',.014)
 for y in [1.24,1.83]:beam('Rear deck horizontal rail',(dx-1.70,y,.42),(dx+1.70,y,.42),.13,.13,'WoodHoney') if x<dx else None
for i in range(3):
 height=.72-i*.24;z=3.20+i*.32
 box('Stair solid support',(dx,height/2,z),(2.15,height,.34),'WoodDark',.012)
 box('Wide rounded stair tread',(dx,height+.035,z),(2.33,.075,.39),'WoodHoney',.022)
 for x in [dx-.96,dx+.96]:cyl('Stair tread nail',(x,height+.076,z),.026,.01,'Iron',8,1)

# Back-right feed/inlet rock, square brass spout with hollow dark opening.
rock('Tall inlet source rock',(-3.26,.83,-2.57),(.65,.86,.56),True)
box('Pancuran warm brass outlet',(-3.26,.98,-1.98),(.72,.39,.59),'BrassWarm',.035)
box('Pancuran dark hollow mouth',(-3.26,.91,-1.673),(.54,.22,.025),'Iron',.012)
box('Pancuran lower lip',(-3.26,.795,-1.64),(.72,.06,.18),'BrassWarm',.015)

# Image-left feed maker: teal metal casing, open brass hopper, dial, belt housing, tray.
mx=4.23;mz=1.10
for x in [mx-.55,mx+.55]:
 for z in [mz-.43,mz+.43]:box('Feed maker leg',(x,.28,z),(.15,.56,.16),'PaintTeal',.025)
box('Teal feed maker body',(mx,1.02,mz),(1.31,1.47,1.10),'PaintTeal',.065)
box('Machine dark front panel',(mx,1.01,mz+.573),(1.10,1.15,.055),'PaintTeal',.035)
for x in [mx-.47,mx+.47]:
 for y in [.57,1.48]:sphere('Brass casing rivet',(x,y,mz+.623),(.039,.039,.017),'BrassWarm',1)
box('Machine amber status slit',(mx,1.43,mz+.629),(.23,.045,.028),'Terracotta',.008,1)
# Front cylinder oriented toward camera.
o=cyl('Front grinding adjustment dial',(mx,.84,mz+.72),.30,.25,'Iron',16);o.rotation_euler.x=math.pi/2
box('Dial grip pointer',(mx,.84,mz+.87),(.08,.36,.06),'Iron',.016,1)
o=cyl('Side belt housing',(mx+.81,1.10,mz),.47,.35,'Iron',16);o.rotation_euler.y=math.pi/2
for i in range(7):
 o=cyl('Ribbed pulley casing',(mx+.69+i*.05,1.10,mz),.49,.019,'Iron',16,1);o.rotation_euler.y=math.pi/2
# Hopper four sloped walls, real inner recess; no fake solid yellow cube.
levels=[(1.72,.52,.43),(2.70,.89,.72),(2.70,.80,.63),(1.81,.43,.34)]
vv=[(mx+sx*w,y,mz+sz*d) for y,w,d in levels for sx,sz in [(-1,-1),(1,-1),(1,1),(-1,1)]]
ff=[(r*4+i,r*4+(i+1)%4,(r+1)*4+(i+1)%4,(r+1)*4+i) for r in range(3) for i in range(4)]+[(12,13,14,15)]
mesh('Open brass feed hopper',vv,ff,'BrassWarm');box('Hopper dark inside throat',(mx,1.825,mz),(.79,.025,.60),'WoodDark',.01)
for z in [mz-.74,mz+.74]:box('Rounded hopper lip',(mx,2.71,z),(1.82,.075,.10),'BrassWarm',.024)
for x in [mx-.86,mx+.86]:box('Hopper end lip',(x,2.71,mz),(.10,.075,1.49),'BrassWarm',.024)
box('Pellet chute',(mx,.31,mz+.66),(.53,.20,.40),'Iron',.018)
crate((mx,.0,mz+1.17),1.0,.66,.39)
box('Empty output tray dark inner',(mx,.10,mz+1.17),(.81,.045,.48),'WoodDark',.01)
barrel((5.56,.015,2.06),.36,.72)

# Hanging feed sack on separate timber post; no world text baked into the model.
px=6.08;pz=.1
box('Feeding post stone footing',(px,.17,pz),(.64,.34,.64),'RiverStone',.04)
box('Tall feed sack timber post',(px,1.91,pz),(.24,3.22,.24),'WoodHoney',.025)
box('Feed sack post cap',(px,3.56,pz),(.36,.12,.33),'WoodHoney',.023)
for y in [.72,3.11]:box('Post iron strap',(px,y,pz+.14),(.28,.12,.045),'Iron',.013,1)
box('Hanging sack hook block',(px,3.19,pz+.26),(.20,.40,.22),'WoodDark',.02)
beam('Sack hook forward',(px,3.0,pz+.26),(px,2.95,pz+.55),.04,.04,'Iron')
sack((px,1.78,pz+.58),.46,.96)

# A few bank leaves / flowers, plus flat lily shapes at the water edge only.
for x,z in [(-5.40,-1.8),(-4.8,2.90),(-1.7,-2.95),(1.90,-2.7)]:plants((x,.03,z),4,.22)
for x,z in [(-4.48,.75),(-4.15,.92),(-4.4,-1.6),(-1.98,-1.98)]:
 n=10;pts=[(x,.666,z)]+[(x+.15*math.cos(a),.666,z+.11*math.sin(a)) for a in [(.22+i*(math.tau-.44)/(n-1)) for i in range(n)]]
 mesh('Small edge lily pad',pts,[(0,i+1,i+2) for i in range(n-1)],'PondLeaf',1)

# Independent flat water; material placeholder in Blender, remapped to Toonpack in Unity.
for o in parts:
 if o.name.startswith(('Grain sack body','Sack tied neck','Coopered wood barrel','Crate base','Crate corner','Crate slat','Crate end slat')):lodtag[o.name]=0
watermat=bpy.data.materials.new('TFP_Water_1A');watermat.diffuse_color=(.12,.37,.40,1);watermat.use_nodes=True
bs=watermat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.08,.32,.34,1);bs.inputs['Roughness'].default_value=.32;bs.inputs['Alpha'].default_value=.72;watermat.surface_render_method='DITHERED'
water=mesh('FishPond_Cozy_Water',[(-1.5,.65,0)]+[(x,.65,z) for x,z in inner],[(0,i+1,(i+1)%len(inner)+1) for i in range(len(inner))],'WaterBlue')
parts.remove(water);water.data.materials.clear();water.data.materials.append(watermat)
for p in water.data.polygons:
 for li in p.loop_indices:
  co=water.data.vertices[water.data.loops[li].vertex_index].co;water.data.uv_layers.active.data[li].uv=(co.x*.18,co.y*.18)
water['UnityMaterial']='Toon/CustomWaterSimple; placeholder only in Blender';water['KeepSeparate']=True
for o in parts+[water]:
 if any(m==M[1] for m in o.data.materials):continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 if o.get('SurfaceUp',False):
  for face in bm.faces:
   if face.normal.z<0:face.normal_flip()
 bm.to_mesh(o.data);bm.free()
# Open plane must face up, regardless of reflection in the coordinate conversion.
bm=bmesh.new();bm.from_mesh(water.data)
for f in bm.faces:
 if f.normal.z<0:f.normal_flip()
bm.to_mesh(water.data);bm.free()
markers=[]
for label,p in [('CozyOrigin',(0,0,0)),('CozyRight',(1,0,0)),('CozyForward',(0,0,1)),('CozyUp',(0,1,0))]:
 o=bpy.data.objects.new(label,None);exports.objects.link(o);o.location=cv(p);o.empty_display_size=.12;markers.append(o)
def export(o):
 bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True)
 for m in markers:m.select_set(True)
 bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(o.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
def clean(o):
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
def stat(o):
 points=[(-v.co.x,v.co.z,-v.co.y) for v in o.data.vertices]
 return dict(name=o.name,triangles=len(o.data.polygons),vertices=len(o.data.vertices),materials=[m.name for m in o.data.materials],min=[min(p[i] for p in points) for i in range(3)],max=[max(p[i] for p in points) for i in range(3)])
stats=[];runtime=[]
for lod in range(3):
 selected=[o for o in parts if lod==0 or lodtag[o.name]!=1]
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for original in selected:
  o=original.copy();o.data=original.data.copy();exports.objects.link(o);o.select_set(True);copies.append(o)
 protected=[c for c in copies if lod>0 and c.get('KeepLOD',False)]
 for c in protected:c.select_set(False)
 bpy.context.view_layer.objects.active=next(c for c in copies if c not in protected);bpy.ops.object.join();o=bpy.context.object;o.name='FishPond_Cozy_LOD'+str(lod);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if lod>0:
  mod=o.modifiers.new('Mobile LOD reduction','DECIMATE');mod.ratio=.72 if lod==1 else .31;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
  for c in protected:c.select_set(True)
  bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
 pm=[o.data.materials[p.material_index] for p in o.data.polygons];used=[m for m in M if m in pm];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,pm):p.material_index=used.index(m)
 clean(o);stats.append(stat(o));export(o);o.hide_render=lod>0;o.hide_set(lod>0);runtime.append(o)
water.name='Editable water template';watercopy=water.copy();watercopy.data=water.data.copy();exports.objects.link(watercopy);watercopy.name='FishPond_Cozy_Water';bpy.context.view_layer.objects.active=watercopy
bpy.ops.object.select_all(action='DESELECT');watercopy.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);clean(watercopy);export(watercopy)
source.hide_render=True;source.hide_viewport=True
for m in markers:m.hide_render=True
stations={'pond':[.55,.96,2.17],'approach':[.55,0,4.23],'maker':[mx,1.0,mz+1.12],'feeding':[px,2.24,pz+.58],'inlet':[-3.26,.86,-1.63],'splash':[-3.26,.66,-1.63]}
manifest=dict(status='SOURCE_EXPORTED',level=1,worldScale=.4,reference='FishPond_Target.png',lods=stats,water=stat(watercopy),waterPolygon=inner,dockMinimum=[dx-1.8,zback],dockMaximum=[dx+1.8,zfront],stations=stations,waterHeight=.65,swimHeight=.44,sourceModules=len(parts),atlasPages=['NP_CozySurfaces_03','NP_CozyFoliage_01'],capacity=50,placeholderWater=True,woodUV=wood_uv_audit)
(SOURCE/'FishPond_Cozy_Manifest.json').write_text(json.dumps(manifest,indent=2))
# Editable station anchors, not part of FBX render meshes.
anchors=bpy.data.collections.new('04 GAMEPLAY AND WATER FX ANCHORS');scene.collection.children.link(anchors)
for name,p in stations.items():
 o=bpy.data.objects.new(name,None);anchors.objects.link(o);o.location=cv(p);o.empty_display_type='SPHERE';o.empty_display_size=.12
def studio_obj(o):
 for c in list(o.users_collection):c.objects.unlink(o)
 studio.objects.link(o)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;studio_obj(floor);fm=bpy.data.materials.new('Preview grass neutral');fm.diffuse_color=(.29,.39,.18,1);floor.data.materials.append(fm);floor.location.z=-.04
target=cv((0,1,0))
for pos,power,size in [((-7,-12,16),1900,8),((9,-3,11),900,10),((0,8,12),1100,8)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;studio_obj(o);o.data.energy=power;o.data.size=size;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Pond studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.ops.object.camera_add(location=(0,-20,17));camera=bpy.context.object;studio_obj(camera);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=14.2;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT');runtime[0].hide_set(False);runtime[0].select_set(True);bpy.context.view_layer.objects.active=runtime[0]
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=16;area.spaces.active.region_3d.view_location=target
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FishPond_Cozy_Master.blend'))
print('FISH_POND_COZY_BUILD_PASS',json.dumps(stats),flush=True)
if '--no-preview' not in __import__('sys').argv:
 scene.render.filepath=str(OUT/'Previews/Pond_Front.png');bpy.ops.render.render(write_still=True)
