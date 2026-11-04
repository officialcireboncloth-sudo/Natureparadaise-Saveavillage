"""Editable player Coop exterior; reference facade, additive atlas and three shared-mesh LODs.
Unity-local metres: front +Z, main door +X. Preserve the existing entrance (1.25,0,4.65).
"""
from pathlib import Path
library_path=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py'
exec(compile(library_path.read_text().split('\nfor name,builder in ')[0],str(library_path),'exec'),globals())
HELPERS=HELPERS.replace('ArtSource/PoultryCozy','ArtSource/CoopExteriorCozy').replace('Source~/PoultryCozy','Source~/CoopExteriorCozy').replace('mesh/Buildings/PoultryCozy','mesh/Buildings/CoopExteriorCozy')
begin('Player Coop Exterior Cozy')
# Explicit handedness conversion makes the editable Blender front and Unity front match the reference.
def cv(p):return Vector((-p[0],-p[2],p[1]))
SOURCE=A/'Source~/CoopExteriorCozy';MODELS=A/'mesh/Buildings/CoopExteriorCozy';OUT=ROOT/'ArtSource/CoopExteriorCozy'
for p in (SOURCE,MODELS,OUT/'Previews'):p.mkdir(parents=True,exist_ok=True)
extra=json.loads((A/'Textures/Shared/Cozy/NP_CozyAtlasSlots_02.json').read_text());E={s['name']:s for s in extra['slots']};S.update(E)
ext=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_02_BaseColor.png'));ext.pack()
ext_normal=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_02_Normal.png'));ext_normal.colorspace_settings.name='Non-Color';ext_normal.pack()
extension=mat('NP_CozySurfaces_02',ext)
for node in extension.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image==normal:node.image=ext_normal
M.append(extension);M.append(mat('NP_CozyWire_02',ext,True))
base_assign=assign
def assign(obj,slot,mi=0,rect=None):
 if slot in E:mi=4 if slot=='ChickenWire' else 3
 return base_assign(obj,slot,mi,rect)
def solid(name,c,size,slot='WoodHoney',w=.025,detail=0):return box(name,c,size,slot,w,detail)
def oval(name,c,radii,slot='WoodHoney',detail=0):return sphere(name,c,radii,slot,detail)
def plaque(name,c,w,h,slot='WoodCreamClean'):
 x,y,z=c
 return mesh(name,[(x-w/2,y-h/2,z),(x+w/2,y-h/2,z),(x+w/2,y+h/2,z),(x-w/2,y+h/2,z)],[(0,1,2,3)],slot)

# Existing six-by-seven-metre shell, extended only by the authored decorative apron.
solid('Warm stone foundation',(0,.14,0),(6.08,.28,7.08),'StoneWarm',.045)
solid('Closed cream building shell',(0,1.91,0),(5.98,3.28,7.0),'WoodCreamClean',.02)
for x in [-2.98,2.98]:
 for i in range(24):
  z=-3.5+(i+.5)*7/24
  solid('Clean side vertical board',(x,1.92,z),(.09,3.27,7/24-.009),'WoodCreamClean',.009)
 for z in [-3.51,3.51]:solid('Honey corner post',(x,1.94,z),(.19,3.42,.20),'WoodHoney',.025)
 solid('Side timber header',(x,3.57,0),(.22,.22,7.14))
 solid('Side timber sill',(x,.32,0),(.16,.15,7.10))
for z in [-3.52,3.52]:
 for i in range(23):
  x=-2.99+(i+.5)*5.98/23
  solid('Clean facade vertical board',(x,1.92,z),(5.98/23-.009,3.27,.075),'WoodCreamClean',.008)
 solid('Honey facade header',(0,3.57,z+.015),(6.15,.23,.22))
 solid('Facade sill',(0,.32,z+.015),(6.12,.15,.17))

roof_y=lambda x:5.74-abs(x)*(5.74-3.50)/3.675
for z in [-3.52,3.52]:
 outline=[(-3,3.56),(3,3.56),(3,roof_y(3)),(0,5.74),(-3,roof_y(3))]
 panel('Enclosed ivory gable',outline,z,'WoodCreamClean')
 for i in range(-10,11):
  x=i*.275;top=roof_y(x)-.13
  solid('Gable pale board seam',(x,(3.69+top)/2,z+(.035 if z>0 else -.035)),(.018,top-3.69,.025),'TimberCream',.004,1)

# Tall main doorway on the image-left; preserve game approach anchor.
dx=1.25
solid('Main door deep shadow',(dx,1.67,3.60),(1.98,2.85,.10),'WoodDark',.02)
for i in range(5):solid('Main door vertical plank',(dx+(i-2)*.343,1.66,3.69),(.334,2.64,.10),'WoodHoney',.018)
frame('Heavy main door surround',dx,1.68,3.76,1.96,2.90,.16)
for yy in [.53,2.82]:solid('Door inner cross timber',(dx,yy,3.80),(1.73,.11,.09),'WoodHoney',.012)
beam('Door diagonal brace',(dx+.73,.53,3.84),(dx-.73,2.81,3.84),.13,.095,'WoodHoney')
for yy in [.78,2.69]:
 solid('Black forged door hinge',(dx+.56,yy,3.91),(.43,.085,.045),'Iron',.012,1)
 for xx in [dx+.72,dx+.41]:oval('Hinge pin',(xx,yy,3.943),(.035,.035,.015),'Iron',1)
beam('Door slide bolt',(dx-.67,1.08,3.92),(dx+.70,1.08,3.92),.055,.055,'Iron',1)
for xx in [dx-.61,dx+.56]:solid('Door latch bracket',(xx,1.08,3.93),(.065,.15,.05),'Iron',.008,1)
beam('Door pull handle',(dx-.67,1.03,3.99),(dx-.67,1.28,3.99),.042,.042,'Iron',1)
for i in range(3):
 y=(3-i)*.095;z=3.78+i*.34
 solid('Three shallow entrance steps',(dx,y/2,z),(2.09,y,.38),'Paving',.022)

# Image-right window, actual alpha chicken wire and a left-side wooden shutter.
wx=-1.72;wy=2.30
solid('Dark window recess',(wx,wy,3.60),(1.68,1.15,.10),'WoodDark',.02)
frame('Deep honey window frame',wx,wy,3.77,1.76,1.26,.15)
wire=plaque('Hexagonal wire window',(wx-.38,wy,3.785),.80,1.05,'ChickenWire');wire['TextureAlpha']='True open hexagonal apertures'
u0,v0,u1,v1=region('ChickenWire');assign(wire,'ChickenWire',rect=(u0,v0,u0+(u1-u0)*(.80/1.05)/(256/192),v1))
for i in range(3):solid('Closed half-window shutter',(wx+.47+(i-1)*.235,wy,3.81),(.225,1.06,.07),'WoodHoney',.012)
frame('Shutter perimeter',wx+.47,wy,3.86,.69,1.10,.065)
beam('Shutter diagonal brace',(wx+.73,wy-.40,3.90),(wx+.21,wy+.40,3.90),.075,.045,'WoodHoney',1)
for yy in [wy-.39,wy+.39]:solid('Shutter iron hinge',(wx+.44,yy,3.94),(.40,.055,.04),'Iron',.008,1)
solid('Flower box front',(wx,1.50,3.98),(1.44,.29,.12),'WoodHoney',.02)
solid('Flower box bottom',(wx,1.36,3.83),(1.49,.09,.44),'WoodDark',.01)
for x in [wx-.70,wx+.70]:solid('Flower box end',(x,1.50,3.83),(.10,.29,.42),'WoodHoney',.012)
plants((wx,1.64,3.84),18,.61)
card('Distant planter flower cluster',(wx,1.79,4.0),.75,.44,14);lodtag[parts[-1].name]=0

# Dark chicken hatch and separate slatted ramp below the flower box.
hx=-1.72
solid('Chicken hatch shadow',(hx,.80,3.60),(.94,.90,.10),'WoodDark',.015)
frame('Chicken hatch jamb',hx,.80,3.76,1.02,.96,.11)
solid('Hay inside empty chicken hatch',(hx,.44,3.79),(.78,.12,.24),'HayGold',.015)
for i in range(12):
 x=hx+random.uniform(-.35,.35)
 beam('Loose hatch straw',(x,.48,3.94),(x+random.uniform(-.08,.08),random.uniform(.54,.64),3.91),.012,.012,'HayGold',1)
beam('Continuous ramp board',(hx,.42,3.79),(hx,.04,5.16),1.04,.05,'WoodHoney')
for x in [hx-.45,hx+.45]:beam('Ramp supporting runner',(x,.43,3.79),(x,.04,5.16),.085,.075,'WoodDark')
for i in range(8):
 t=i/7;solid('Chicken ramp slat',(hx,.44*(1-t)+.045,3.79+1.37*t),(1.04,.065,.157),'WoodHoney',.012)

# Attached nest storage follows the existing side collision volume.
solid('Attached side nesting box',(-3.48,1.10,1.4),(.87,1.00,1.86),'WoodHoney',.025)
solid('Side nest lid',(-3.52,1.66,1.4),(1.03,.11,2.02),'WoodHoney',.018)
solid('Side nest lower dark panel',(-3.93,.86,1.4),(.035,.38,1.78),'WoodDark',.01)
solid('Nest inspection latch',(-3.97,1.39,1.42),(.045,.075,.15),'Iron',.008,1)

# Curved terracotta roof: real overlapping rectangular shingles and barrel ridge caps.
for side in [-1,1]:
 for row in range(8):
  outer=3.675-row*3.675/8;inner=max(0,3.675-(row+1)*3.675/8-.05)
  z=-4.35-(.29 if row%2 else 0)
  while z<4.35:
   z0=max(-4.35,z)+.006;z1=min(4.35,z+.58)-.006;z+=.58
   if z1-z0<.04:continue
   a,b=side*outer,side*inner
   top=[(a,roof_y(a)+.075,z0),(a,roof_y(a)+.075,z1),(b,roof_y(b)+.028,z1),(b,roof_y(b)+.028,z0)]
   vv=top+[(x,y-.045,z) for x,y,z in top]
   o=mesh('Overlapping terracotta rectangle',vv,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofClayClean',3)
   bevel(o,.012);assign(o,'RoofClayClean')
 for band in range(2):
  x0=side*band*3.675/2;x1=side*(band+1)*3.675/2
  top=[(x0,roof_y(x0)+.01,-4.35),(x1,roof_y(x1)+.01,-4.35),(x1,roof_y(x1)+.01,4.35),(x0,roof_y(x0)+.01,4.35)]
  # Closed slabs retain outward normals through cleanup/LOD simplification.
  o=mesh('Distant tiled roof slab',top+[(x,y-.06,z) for x,y,z in top],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofClayTiled',2)
  rect=region('RoofClayTiled')
  for j,uv in enumerate([(rect[0],rect[1]),(rect[0],rect[3]),(rect[2],rect[3]),(rect[2],rect[1])]):o.data.uv_layers.active.data[j].uv=uv
 for z in [-4.39,4.39]:beam('Thick warm gable rake',(0,5.71,z),(side*3.72,3.46,z),.17,.17,'WoodHoney')
 solid('Honey eave beam',(side*3.64,3.48,0),(.19,.19,8.87),'WoodHoney',.025)
def ridge(name,z0,z1,r,detail):
 n=8;vv=[(rad*math.cos(i*math.pi/n),5.77+rad*math.sin(i*math.pi/n),z) for z in [z0,z1] for rad in [r,r-.035] for i in range(n+1)]
 faces=[];k=n+1
 for i in range(n):faces.extend([(i,i+1,2*k+i+1,2*k+i),(k+i,3*k+i,3*k+i+1,k+i+1),(i,k+i,k+i+1,i+1),(2*k+i,2*k+i+1,3*k+i+1,3*k+i)])
 faces.extend([(0,2*k,3*k,k),(n,k+n,3*k+n,2*k+n)])
 o=mesh(name,vv,faces,'RoofClayClean',detail)
 for p in o.data.polygons:p.use_smooth=True
 return o
for i in range(16):ridge('Overlapping curved ridge cap',-4.43+i*.56,-4.43+i*.56+.60,.145,3)
ridge('Distant continuous ridge',-4.43,4.57,.145,2)

# One continuous hen profile: rounded breast, short neck, comb, pointed beak,
# separated toes and a swept tail. Avoid disconnected sphere/rod anatomy.
hen=[(-.36,.12)]
def hen_curve(a,b,c):
 p=hen[-1]
 for i in range(1,7):
  t=i/6;s=1-t
  hen.append(tuple(s*s*s*p[k]+3*s*s*t*a[k]+3*s*t*t*b[k]+t*t*t*c[k] for k in range(2)))
def hen_line(p):hen.append(p)
hen_curve((-.42,.19),(-.47,.30),(-.43,.36))
hen_curve((-.40,.38),(-.33,.29),(-.29,.26))
hen_curve((-.33,.34),(-.34,.43),(-.29,.43))
hen_curve((-.24,.43),(-.22,.25),(-.13,.23))
hen_curve((-.03,.20),(.08,.23),(.12,.31))
hen_curve((.15,.36),(.14,.41),(.17,.43))
hen_curve((.15,.50),(.20,.54),(.22,.49))
hen_curve((.23,.56),(.29,.55),(.29,.49))
hen_curve((.34,.54),(.38,.49),(.35,.46))
hen_curve((.39,.45),(.40,.41),(.39,.39))
hen_line((.49,.355));hen_line((.38,.33))
hen_curve((.40,.26),(.35,.25),(.33,.29))
hen_curve((.34,.20),(.29,.07),(.19,.015))
hen_curve((.15,-.01),(.12,-.025),(.10,-.035))
for p in [(.105,-.16),(.19,-.177),(.18,-.205),(.075,-.194),(.035,-.18),(.045,-.052),(-.055,-.055),(-.075,-.16),(-.015,-.185),(-.023,-.21),(-.10,-.193),(-.155,-.205),(-.167,-.18),(-.12,-.155),(-.115,-.045)]:hen_line(p)
hen_curve((-.25,-.04),(-.33,.04),(-.36,.12))
hen.pop() # final cubic closes on the first vertex
panel('Hen emblem dark wood bevel rim',[(x*1.055,4.30+y*1.055) for x,y in hen],3.715,'WoodDark')
hen_face=panel('Hen emblem continuous carved silhouette',[(x,4.30+y) for x,y in hen],3.755,'WoodHoney')
bevel(hen_face,.007)
oval('Hen emblem small recessed eye',(.335,4.715,3.794),(.013,.013,.005),'WoodDark',1)
# Front lantern and right-side ivy.
lx=1.25
solid('Lantern wall mount',(lx,3.30,3.74),(.12,.19,.10),'Iron',.012)
beam('Lantern curved support',(lx,3.34,3.76),(lx,3.34,4.02),.035,.035,'Iron')
oval('Lantern hood',(lx,3.16,4.02),(.19,.075,.18),'Iron')
o=solid('Warm lantern glass',(lx,2.98,4.02),(.21,.28,.19),'GlowWarm',.015);assign(o,'GlowWarm',2)
for xx in [-.12,.12]:
 for zz in [-.10,.10]:beam('Lantern cage',(lx+xx,2.81,4.02+zz),(lx+xx,3.13,4.02+zz),.02,.02,'Iron',1)
solid('Lantern base',(lx,2.79,4.02),(.27,.055,.25),'Iron',.012)
for i in range(6):card('Ivy climbing front-right post',(-2.89,3.2-i*.43,3.69),.40,.65,4+i%4,random.uniform(-.15,.15))

def bale(c):
 x,y,z=c;solid('Rounded hay bale',(x,y+.36,z),(.86,.72,.78),'HayGold',.10)
 for zz in [-.24,.24]:
  solid('Bale rope top',(x,y+.732,z+zz),(.82,.025,.035),'Rope',.006,1)
  for xx in [-.435,.435]:solid('Bale rope side',(x+xx,y+.36,z+zz),(.026,.69,.035),'Rope',.006,1)
for c in [(-3.53,0,3.38),(-3.53,0,2.52),(-3.53,.72,2.95)]:bale(c)
barrel((2.78,.015,4.12),.36,.62);solid('Planter dark soil',(2.78,.64,4.12),(.54,.035,.49),'Earth',.01)
plants((2.78,.68,4.12),20,.33);card('Distant barrel flowers',(2.78,.96,4.18),.61,.62,14);lodtag[parts[-1].name]=0
# Wicker basket with physical handle and decorative eggs; no live animals/pickups are spawned.
bx,bz=2.15,4.52
cyl('Basket wicker body',(bx,.25,bz),.31,.40,'Wicker',12,1)
cyl('Basket dark mouth',(bx,.46,bz),.275,.025,'WoodDark',12,1)
for j in range(10):
 a=j*math.pi/10;b=(j+1)*math.pi/10
 beam('Arched wicker basket handle',(bx+.34*math.cos(a),.45+.39*math.sin(a),bz),(bx+.34*math.cos(b),.45+.39*math.sin(b),bz),.037,.037,'Wicker',1)
for x,z in [(-.15,-.1),(.04,-.12),(.17,.02),(-.12,.09),(.05,.10),(0,0)]:oval('Decorative basket egg',(bx+x,.49,bz+z),(.087,.12,.08),'Ceramic',1)
for x,z,r in [(.95,5.13,.34),(1.49,5.53,.34),(1.11,5.94,.36)]:
 o=oval('Natural stepping stone',(x,.045,z),(r,.06,r*.68),'RiverStone',1)
 for v in o.data.vertices:v.co.x*=random.uniform(.93,1.07);v.co.y*=random.uniform(.93,1.07)
for x,z in [(-2.65,4.35),(2.78,3.49),(-2.75,4.94)]:plants((x,.03,z),5,.18)

# Clean, recalculate outward closed parts, then author all three positive-scale LODs.
bpy.context.view_layer.update()
for o in parts:
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if p.z<0:p.z=0;v.co=o.matrix_world.inverted()@p
for o in parts:
 if any(m in [M[1],M[4]] for m in o.data.materials):continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
markers=[]
for label,p in [('CozyOrigin',(0,0,0)),('CozyRight',(1,0,0)),('CozyForward',(0,0,1)),('CozyUp',(0,1,0))]:
 o=bpy.data.objects.new(label,None);exports.objects.link(o);o.location=cv(p);o.empty_display_size=.12;markers.append(o)
stats=[];runtime=[]
for lod in range(3):
 selected=[o for o in parts if (lodtag[o.name]!=2 if lod==0 else lodtag[o.name] not in [1,2] if lod==1 else lodtag[o.name] not in [1,3] and not any(m in [M[1],M[4]] for m in o.data.materials))]
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for original in selected:
  o=original.copy();o.data=original.data.copy();exports.objects.link(o);o.select_set(True);copies.append(o)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name='CoopExterior_Cozy_LOD'+str(lod)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 used=[m for m in M if any(o.data.materials[p.material_index]==m for p in o.data.polygons)]
 polygon_materials=[o.data.materials[p.material_index] for p in o.data.polygons];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,polygon_materials):p.material_index=used.index(m)
 if lod>0:
  mod=o.modifiers.new('Authored mobile LOD reduction','DECIMATE');mod.ratio=.65 if lod==1 else .38;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces))
 bad=[f for f in bm.faces if f.calc_area()<1e-10]
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bm.to_mesh(o.data);bm.free();o.data.update()
 points=[(-v.co.x,v.co.z,-v.co.y) for v in o.data.vertices]
 stats.append(dict(name=o.name,triangles=len(o.data.polygons),vertices=len(o.data.vertices),materials=[m.name for m in used],min=[min(p[i] for p in points) for i in range(3)],max=[max(p[i] for p in points) for i in range(3)]))
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 for marker in markers:marker.select_set(True)
 bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(o.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 o.hide_render=lod>0;o.hide_set(lod>0);runtime.append(o)
source.hide_render=True;source.hide_viewport=True
for marker in markers:marker.hide_render=True
manifest=dict(status='SOURCE_EXPORTED',reference='CoopExterior_Target.png right panel',scope='Player-built Coop exterior, shared visual across existing level scales',entranceUnity=[1.25,0,4.65],doorCenterUnity=[1.25,1.66,3.69],lanternUnity=[1.25,2.98,4.02],lods=stats,sourceModules=len(parts),decorativeEggs=6,liveAnimals=0,atlasPages=['NP_CozySurfaces_01','NP_CozySurfaces_02'],legacyAtlasModified=False)
(SOURCE/'CoopExterior_Cozy_Manifest.json').write_text(json.dumps(manifest,indent=2))
# Review-only studio; never exported to Unity.
def studio_obj(o):
 for collection in list(o.users_collection):collection.objects.unlink(o)
 studio.objects.link(o)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;studio_obj(floor);fm=bpy.data.materials.new('Studio neutral warm floor');fm.diffuse_color=(.62,.66,.49,1);floor.data.materials.append(fm);floor.location.z=-.035
target=Vector((0,-.45,2.65))
for pos,power,size in [((-7,-12,15),1800,9),((9,-3,11),1050,10),((0,8,12),1000,9)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;studio_obj(o);o.data.energy=power;o.data.size=size;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Cozy exterior studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.48
bpy.ops.object.camera_add(location=(0,-26,18.3));camera=bpy.context.object;studio_obj(camera);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=12.2;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1150;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT');runtime[0].hide_set(False);runtime[0].select_set(True);bpy.context.view_layer.objects.active=runtime[0]
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=15;area.spaces.active.region_3d.view_location=target
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'CoopExterior_Cozy_Master.blend'))
print('COOP_EXTERIOR_COZY_BUILD_PASS',json.dumps(stats),flush=True)
if '--no-preview' not in __import__('sys').argv:
 for name,pos in [('Front',(0,-26,18.3)),('ThreeQuarter',(-13,-23,16)),('Rear',(12,22,15))]:
  camera.location=pos;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'Previews'/('Coop_'+name+'.png'));bpy.ops.render.render(write_still=True)
