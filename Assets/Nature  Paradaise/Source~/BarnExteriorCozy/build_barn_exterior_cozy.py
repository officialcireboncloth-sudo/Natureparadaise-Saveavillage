"""Editable player Barn exterior; reference facade, additive atlas and three shared-mesh LODs.
Unity-local metres: front +Z, main door centered. Existing root entrance and gameplay IDs stay intact.
"""
from pathlib import Path
library_path=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py'
exec(compile(library_path.read_text().split('\nfor name,builder in ')[0],str(library_path),'exec'),globals())
HELPERS=HELPERS.replace('ArtSource/PoultryCozy','ArtSource/BarnExteriorCozy').replace('Source~/PoultryCozy','Source~/BarnExteriorCozy').replace('mesh/Buildings/PoultryCozy','mesh/Buildings/BarnExteriorCozy')
begin('Player Barn Exterior Cozy')
# Explicit handedness conversion makes the editable Blender front and Unity front match the reference.
def cv(p):return Vector((-p[0],-p[2],p[1]))
SOURCE=A/'Source~/BarnExteriorCozy';MODELS=A/'mesh/Buildings/BarnExteriorCozy';OUT=ROOT/'ArtSource/BarnExteriorCozy'
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


roof_profile=[(0,5.55),(2.20,4.63),(4.04,3.03)]
def roof_y(x):
 x=abs(x)
 a,b=(roof_profile[0],roof_profile[1]) if x<=2.20 else (roof_profile[1],roof_profile[2])
 return a[1]+(x-a[0])/(b[0]-a[0])*(b[1]-a[1])

# Main shell is closed, with real recesses in front so loft and doorway never
# show an opaque wall through their openings. Front +Z; preserve a clear apron.
solid('Warm stone foundation',(0,.15,0),(7.20,.30,5.50),'StoneWarm',.05)['KeepLOD']=True
for i in range(16):
 x=-3.60+(i+.5)*.45
 for z in [-2.84,2.84]:solid('Individual foundation facing stone',(x,.15,z),(.442,.30,.22),'StoneWarm',0)['KeepLOD']=True
for side in [-1,1]:
 for i in range(12):solid('Side foundation facing stone',(side*3.58,.15,-2.75+(i+.5)*5.50/12),(.22,.30,.45),'StoneWarm',0)['KeepLOD']=True
solid('Closed rear barn wall',(0,1.72,-2.70),(7.08,2.94,.14),'WoodCreamClean',.02)
for x in [-3.52,3.52]:
 solid('Closed cream side wall',(x,1.72,0),(.16,2.94,5.50),'WoodCreamClean',.02)
 for i in range(19):
  z=-2.73+(i+.5)*5.46/19
  solid('Clean vertical side plank',(x*1.015,1.76,z),(.055,2.96,.278),'WoodCreamClean',.007)
 for z in [-2.73,2.77]:solid('Heavy honey corner post',(x,1.80,z),(.22,3.05,.25),'WoodHoney',.03)
 solid('Side warm timber sill',(x,.32,0),(.20,.15,5.53))
for i in range(26):
 x=-3.51+(i+.5)*7.02/26
 if abs(x)>1.67:solid('Front ivory plank beside doors',(x,1.78,2.78),(.261,2.95,.10),'WoodCreamClean',.008)
 solid('Rear vertical cream plank',(x,1.76,-2.79),(.261,2.97,.07),'WoodCreamClean',.008)
solid('Facade header over double door',(0,3.15,2.82),(7.22,.25,.23))
for x in [-1.77,1.77]:solid('Deep double-door frame upright',(x,1.71,2.88),(.24,2.92,.25),'WoodHoney',.028)
solid('Door threshold stone',(0,.22,3.10),(3.60,.18,.73),'Paving',.04)
solid('Front step broad stone',(0,.10,3.48),(3.76,.20,.65),'Paving',.04)
solid('Door opening deep recess',(0,1.63,2.79),(3.36,2.73,.13),'WoodDark',.015)
for side in [-1,1]:
 center=side*.825
 for j in range(5):solid('Door leaf honey planks',(center+(j-2)*.319,1.63,2.90),(.309,2.60,.13),'WoodHoney',.014)
 for y in [.42,2.82]:solid('Door leaf horizontal rail',(center,y,3.005),(1.62,.115,.12),'WoodHoney',.018)
 beam('Door diagonal V brace',(side*1.56,2.71,3.01),(side*.12,1.43,3.01),.12,.11,'WoodHoney')
 for y in [.62,2.60]:
  solid('Black iron strap hinge',(side*1.40,y,3.075),(.37,.105,.065),'Iron',.017)
  solid('Iron hinge pin',(side*1.56,y,3.10),(.075,.18,.065),'Iron',.01)
  oval('Iron hinge rivet',(side*1.30,y,3.115),(.027,.027,.014),'Iron',1)
 hx=side*.11
 for y in [1.43,1.76]:solid('Handle mounting plate',(hx,y,3.09),(.095,.12,.06),'Iron',.014)
 beam('Raised black door pull',(hx,1.46,3.16),(hx,1.75,3.16),.055,.06,'Iron')

# Gambrel gables: front loft is an actual opening with warm dark interior.
outline=[(-3.52,3.19),(-3.52,roof_y(3.52)),(-2.20,4.63),(0,5.55),(2.20,4.63),(3.52,roof_y(3.52)),(3.52,3.19)]
panel('Closed cream rear gambrel gable',outline,-2.80,'WoodCreamClean')
for side in [-1,1]:
 p=[(side*1.15,3.24),(side*3.52,3.24),(side*3.52,roof_y(3.52)),(side*2.20,4.63),(side*1.15,roof_y(1.15))]
 panel('Gable beside hayloft opening',p,2.79,'WoodCreamClean')
panel('Gable above loft',[(-1.15,4.64),(1.15,4.64),(1.15,roof_y(1.15)),(0,5.55),(-1.15,roof_y(1.15))],2.79,'WoodCreamClean')
solid('Loft sill cream timber',(0,3.47,2.94),(2.39,.15,.31),'TimberCream',.022)
for x in [-1.15,1.15]:solid('Loft upright cream timber',(x,4.055,2.89),(.16,1.31,.23),'TimberCream',.02)
solid('Loft lintel cream timber',(0,4.67,2.89),(2.42,.15,.23),'TimberCream',.025)
solid('Loft dark back wall',(0,4.02,2.12),(2.12,1.18,.10),'WoodDark',.01)
solid('Loft shelf with depth',(0,3.48,2.45),(2.11,.08,.77),'WoodDark',.01)
for x in [-1.04,1.04]:solid('Loft inner side',(x,4.02,2.42),(.08,1.12,.77),'WoodDark',.01)
for x in [-.49,.49]:
 oval('Loft round golden hay roll',(x,3.94,2.51),(.43,.43,.39),'HayGold')
 # Fine crossed rope gives a readable tied roll without alpha straw clouds.
 for off in [-.12,.12]:beam('Loft roll tie',(x+off,3.60,2.82),(x+off,4.26,2.82),.015,.014,'Rope',1)
for z in [-2.88,2.90]:
 for side in [-1,1]:
  beam('Thick cream lower gambrel fascia',(side*4.09,3.015,z),(side*2.22,4.65,z),.19,.18,'TimberCream')
  beam('Thick cream upper gambrel fascia',(side*2.22,4.65,z),(0,5.58,z),.19,.18,'TimberCream')
for i in range(-12,13):
 x=i*.27
 if abs(x)<1.25:continue
 top=roof_y(x)-.12
 solid('Front gable board seam',(x,(3.28+top)/2,2.84),(.012,top-3.28,.014),'TimberCream',.002,1)

# Real overlapping rectangular red clay tiles on both slopes of each gambrel.
for side in [-1,1]:
 for span,(outer,inner,rows) in enumerate([(4.04,2.20,5),(2.20,0,5)]):
  for row in range(rows):
   a=outer-(outer-inner)*row/rows;b=max(inner,outer-(outer-inner)*(row+1)/rows-.035)
   z=-2.98-(.27 if row%2 else 0)
   while z<2.98:
    z0=max(-2.98,z)+.006;z1=min(2.98,z+.54)-.006;z+=.54
    if z1-z0<.035:continue
    top=[(side*a,roof_y(a)+.072,z0),(side*a,roof_y(a)+.072,z1),(side*b,roof_y(b)+.026,z1),(side*b,roof_y(b)+.026,z0)]
    o=mesh('Beveled overlapping red clay tile',top+[(x,y-.05,z) for x,y,z in top],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofClayClean',3)
    bevel(o,.014)
  top=[(side*inner,roof_y(inner)+.01,-2.98),(side*outer,roof_y(outer)+.01,-2.98),(side*outer,roof_y(outer)+.01,2.98),(side*inner,roof_y(inner)+.01,2.98)]
  o=mesh('Closed distant gambrel roof',top+[(x,y-.07,z) for x,y,z in top],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofClayTiled',2)
  under=[(x,y-.07,z) for x,y,z in top]
  mesh('Continuous terracotta roof underlay',under+[(x,y-.045,z) for x,y,z in under],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofClayClean',0)['KeepLOD']=True
 for z in [-3.01,3.01]:
  beam('Outer honey rake',(side*4.02,3.0,z),(side*2.20,4.61,z),.105,.12,'WoodHoney')
  beam('Upper honey rake',(side*2.20,4.61,z),(0,5.53,z),.105,.12,'WoodHoney')
 solid('Side timber eave',(side*3.99,3.025,0),(.17,.18,6.12),'WoodHoney',.025)
def ridge(z0,z1,detail):
 n=8;k=n+1;r=.145
 vv=[(rad*math.cos(i*math.pi/n),5.57+rad*math.sin(i*math.pi/n),z) for z in [z0,z1] for rad in [r,r-.035] for i in range(n+1)]
 faces=[]
 for i in range(n):faces.extend([(i,i+1,2*k+i+1,2*k+i),(k+i,3*k+i,3*k+i+1,k+i+1),(i,k+i,k+i+1,i+1),(2*k+i,2*k+i+1,3*k+i+1,3*k+i)])
 faces.extend([(0,2*k,3*k,k),(n,k+n,3*k+n,2*k+n)])
 o=mesh('Rounded terracotta ridge cap',vv,faces,'RoofClayClean',detail)
 for p in o.data.polygons:p.use_smooth=True
for i in range(12):ridge(-3.02+i*.51,-3.02+i*.51+.55,3)
ridge(-3.02,3.14,2)
panel('Closed rounded front ridge end',[(.145*math.cos(i*math.pi/12),5.57+.145*math.sin(i*math.pi/12)) for i in range(13)],3.14,'RoofClayClean')

# Sculpted timber sign and legible side-view cow silhouette, not a head icon.
solid('Cow sign honey timber border',(0,4.94,3.02),(1.91,.51,.11),'WoodHoney',.04)
solid('Cow sign clean cream inset',(0,4.94,3.095),(1.78,.40,.065),'TimberCream',.03)
# Generated image decal replaces the manually authored cow geometry.
# True alpha is packed by GIMP into the small shared emblem atlas.
emblem_image=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozyEmblems_01_BaseColor.png'));emblem_image.pack()
emblem_material=mat('NP_CozyEmblems_01',emblem_image,True);M.append(emblem_material)
decal=mesh('Generated Cow PNG sign decal',[(-.36,4.77,3.145),(.36,4.77,3.145),(.36,5.13,3.145),(-.36,5.13,3.145)],[(3,2,1,0)],'WoodDark',0)
decal.data.materials.clear();decal.data.materials.append(emblem_material)
decal_uv={0:(1,0),1:(0,0),2:(0,1),3:(1,1)}
for face in decal.data.polygons:
 for li in face.loop_indices:decal.data.uv_layers.active.data[li].uv=decal_uv[decal.data.loops[li].vertex_index]
decal['KeepLOD']=True
for side in [-1,1]:
 beam('Wheat stalk beside cow',(side*.60,4.80,3.14),(side*.70,5.06,3.14),.015,.015,'WoodDark',1)
 for j in range(4):
  y=4.84+j*.055;x=side*(.615+j*.021)
  for flip in [-1,1]:
   o=oval('Wheat carved grain',(x+flip*.025,y,3.145),(.025,.044,.008),'WoodDark',1);o.rotation_euler.y=flip*.48
for x in [-.81,.81]:oval('Sign dark pin',(x,4.94,3.145),(.024,.024,.012),'Iron',1)

def lamp(x):
 solid('Wall lamp backplate',(x,2.86,2.94),(.10,.21,.09),'Iron',.01)
 beam('Curved lamp neck upright',(x,2.80,3.02),(x,3.04,3.02),.045,.045,'Iron')
 beam('Curved lamp neck forward',(x,3.04,3.02),(x,2.96,3.25),.045,.045,'Iron')
 bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.22,radius2=.065,depth=.17,location=cv((x,2.76,3.25)))
 assign(bpy.context.object,'Iron');link(bpy.context.object,'Soft black bell-shaped lamp shade',0)
 o=oval('Lamp warm glass',(x,2.53,3.25),(.135,.18,.13),'GlowWarm');assign(o,'GlowWarm',2)
 for dx in [-.13,.13]:beam('Thin lantern cage',(x+dx,2.36,3.25),(x+dx,2.70,3.25),.018,.018,'Iron',1)
 solid('Lantern base',(x,2.35,3.25),(.24,.045,.23),'Iron',.018)
for x in [-2.03,2.03]:
 lamp(x)
 cyl('Hanging planter dark iron rim',(x,1.77,3.06),.28,.065,'Iron',12,1)
 oval('Hanging planter wooden bowl',(x,1.65,3.05),(.27,.16,.22),'WoodHoney',1)
 plants((x,1.82,3.10),13,.26)

# Image-left hay rack, image-right milk cans, three-tine fork and flower barrel.
beam('Side bell iron bracket',(-3.55,2.34,1.45),(-3.88,2.34,1.45),.065,.065,'Iron')
beam('Side bell bracket diagonal',(-3.55,2.12,1.45),(-3.85,2.34,1.45),.04,.04,'Iron')
for c in [(2.89,.30,2.87),(2.89,.96,2.87)]:
 x,y,z=c;solid('Tied rectangular hay bale',(x,y+.30,z),(1.05,.60,.74),'HayGold',.09)
 for dx in [-.27,.27]:
  solid('Hay rope across front',(x+dx,y+.30,z+.385),(.025,.57,.025),'Rope',.004,1)
  solid('Hay rope over top',(x+dx,y+.612,z),(.025,.023,.73),'Rope',.004,1)
for x in [2.29,3.49]:solid('Hay rack corner post',(x,.76,3.31),(.11,1.11,.11),'WoodHoney',.02)
for y in [.50,.78,1.06]:solid('Hay rack front rail',(2.89,y,3.32),(1.28,.115,.10),'WoodHoney',.018)

def milkcan(c,h=.84,r=.24):
 x,y,z=c;n=16
 profile=[(r*.90,0),(r,.055*h),(r,.65*h),(r*.64,.82*h),(r*.63,.96*h),(r*.68,h),(r*.56,h),(r*.53,.91*h)]
 vv=[(x+rad*math.cos(i*math.tau/n),y+yy,z+rad*math.sin(i*math.tau/n)) for rad,yy in profile for i in range(n)]
 faces=[tuple(range(n-1,-1,-1))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]
 mesh('Open aluminium milk can',vv,faces,'Aluminium',1)
 cyl('Milk can dark interior',(x,y+h*.90,z),r*.52,.012,'Iron',16,1)
 for side in [-1,1]:
  pts=[(x+side*r*.78,y+h*.82,z),(x+side*r*1.30,y+h*.75,z),(x+side*r*1.28,y+h*.55,z),(x+side*r*.98,y+h*.50,z)]
  for a,b in zip(pts,pts[1:]):beam('Milk can metal handle',a,b,.023,.024,'Aluminium',1)
milkcan((-2.20,.02,3.40),.89,.25);milkcan((-2.76,.02,3.55),.71,.22)
barrel((-3.32,.02,3.19),.36,.55);plants((-3.32,.59,3.19),18,.32)
fx=-2.90
beam('Display fork honey handle',(fx,.06,2.98),(fx,1.54,2.98),.058,.058,'WoodHoney',1)
beam('Fork dark shoulder',(fx-.17,1.62,3.0),(fx+.17,1.62,3.0),.047,.047,'Iron',1)
for dx in [-.17,0,.17]:
 beam('Fork tine lower',(fx+dx,1.62,3.0),(fx+dx,1.91,3.0),.044,.044,'Iron',1)
 beam('Fork tine tip',(fx+dx,1.91,3.0),(fx+dx,2.07,3.035),.034,.034,'Iron',1)
# Clean, recalculate outward closed parts, then author all three positive-scale LODs.
bpy.context.view_layer.update()
for o in parts:
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if p.z<0:p.z=0;v.co=o.matrix_world.inverted()@p
for o in parts:
 if any(m in [M[1],M[4],emblem_material] for m in o.data.materials):continue
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
 protected=[c for c in copies if lod>0 and c.get('KeepLOD',False)]
 for c in protected:c.select_set(False)
 bpy.context.view_layer.objects.active=next(c for c in copies if c not in protected);bpy.ops.object.join();o=bpy.context.object;o.name='BarnExterior_Cozy_LOD'+str(lod)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if lod>0:
  mod=o.modifiers.new('Authored mobile LOD reduction','DECIMATE');mod.ratio=.65 if lod==1 else .38;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
  for c in protected:c.select_set(True)
  bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
 used=[m for m in M if any(o.data.materials[p.material_index]==m for p in o.data.polygons)]
 polygon_materials=[o.data.materials[p.material_index] for p in o.data.polygons];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,polygon_materials):p.material_index=used.index(m)
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
manifest=dict(status='SOURCE_EXPORTED',reference='BarnExterior_Target.png right panel',scope='Player-built Barn exterior, shared visual across existing level scales',entranceUnity=[0,0,3.15],doorCenterUnity=[0,1.63,2.90],lanternUnity=[-2.02,2.50,3.13],lods=stats,sourceModules=len(parts),decorativeEggs=0,liveAnimals=0,atlasPages=['NP_CozySurfaces_01','NP_CozySurfaces_02'],legacyAtlasModified=False)
(SOURCE/'BarnExterior_Cozy_Manifest.json').write_text(json.dumps(manifest,indent=2))
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
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BarnExterior_Cozy_Master.blend'))
print('BARN_EXTERIOR_COZY_BUILD_PASS',json.dumps(stats),flush=True)
if '--no-preview' not in __import__('sys').argv:
 for name,pos in [('Front',(0,-26,18.3)),('ThreeQuarter',(-13,-23,16)),('Rear',(12,22,15))]:
  camera.location=pos;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'Previews'/('Barn_'+name+'.png'));bpy.ops.render.render(write_still=True)

