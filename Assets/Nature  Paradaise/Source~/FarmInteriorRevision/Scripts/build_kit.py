"""Editable low-poly fixtures using the existing shared Toon Farm atlas only."""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets').is_dir() and (p/'ProjectSettings').is_dir());ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True);random.seed(281026)
atlas=bpy.data.images.load(str(ROOT/'Assets/Nature  Paradaise/Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.png'));atlas.pack()
pixels=list(atlas.pixels);w,h=atlas.size
def swatch(rgb,neutral=False):
 best=None
 for x in range(16,1024,32):
  for top in range(1028,2044,8):
   y=h-1-top;cc=[pixels[(y*w+x)*4+i]*255 for i in range(3)]
   if neutral and max(cc)-min(cc)>10:continue
   score=sum((cc[i]-rgb[i])**2 for i in range(3))
   if best is None or score<best[0]:best=(score,x,top,cc)
 _,x,y,cc=best
 return {'uvSafeRectBottomLeft':[(x-9)/w,1-(y+3)/h,(x+9)/w,1-(y-3)/h],'sampleRGB':cc}
PAL={k:swatch(rgb,k.startswith('aluminum')) for k,rgb in {'aluminum':(180,182,180),'aluminum_light':(211,214,211),'dark':(43,47,49),'canvas':(189,165,120),'floor':(188,151,102),'ivory':(236,230,206),'hay':(210,165,62),'hay_light':(236,192,92),'blue':(54,91,107),'green':(91,184,127),'red':(177,66,49),'glow':(255,216,112)}.items()}
PAL['wood']={'uvSafeRectBottomLeft':[.0078125,.7578125,.1171875,.9921875]}
PAL['wood_dark']={'uvSafeRectBottomLeft':[.1328125,.7578125,.2421875,.9921875]}
mat=bpy.data.materials.new('FarmSharedAtlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.62
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
BASE={'materials':{'atlasGuid':'522fcb2b5717c7640b460ee9ff73a78a'}}
helpers=ART/'Scripts/mesh_common.py'
if not helpers.exists():helpers=ROOT/'ArtSource/MiniMarket_A/Scripts/mesh_common.py'
exec(compile(helpers.read_text(),'<shared mesh helpers>','exec'),globals())
scene=bpy.context.scene;scene.name='Farm Interior Revision - Editable Kit';scene.unit_settings.system='METRIC'
kit=coll('Fixtures - Atlas Only',scene);modules=[]
def cylinder(b,a,c,r,color,n=10,r2=None):
 a,c=Vector(a),Vector(c);axis=(c-a).normalized();u=axis.cross(Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))).normalized();v=axis.cross(u)
 vs=[p+(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))*rr for p,rr in [(a,r),(c,r if r2 is None else r2)] for i in range(n)]
 b.solid(vs,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(reversed(range(n))),tuple(range(n,2*n))],color)
def finish(name,b):
 o=b.obj(name,kit)
 # UVs span padded wood/metal regions along each planar face rather than one flat pixel.
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  color=b.col[p.index][0] if len(b.col)==len(o.data.polygons) else None
  coords=[o.data.vertices[o.data.loops[i].vertex_index].co for i in p.loop_indices]
  normal=p.normal;axes=(0,1) if abs(normal.z)>.7 else (0,2) if abs(normal.y)>.7 else (1,2)
  old=uv.data[p.loop_start].uv
  color=next((k for k in ('wood','wood_dark') if abs(old.x-sum(PAL[k]['uvSafeRectBottomLeft'][::2])/2)<.001),None)
  if color:
   a,c,d,e=PAL[color]['uvSafeRectBottomLeft'];lo=[min(v[ax] for v in coords) for ax in axes];hi=[max(v[ax] for v in coords) for ax in axes]
   for idx,v in zip(p.loop_indices,coords):uv.data[idx].uv=(a+(d-a)*(v[axes[0]]-lo[0])/max(.001,hi[0]-lo[0]),c+(e-c)*(v[axes[1]]-lo[1])/max(.001,hi[1]-lo[1]))
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 modules.append(o);return o

# Compact modern feed processor: enclosed cabinet, hopper guard, motor vents and tactile controls.
b=Build()
for x in [-.38,.38]:
 for y in [-.28,.28]:b.box((x,y,.11),(.10,.12,.22),'dark')
b.box((0,0,.62),(.98,.72,1.0),'aluminum');b.box((0,0,.18),(1.05,.78,.10),'dark')
b.box((0,-.373,.62),(.82,.025,.73),'aluminum_light')
for x in [-.40,.40]:b.box((x,-.392,.62),(.025,.025,.66),'dark')
b.box((.20,-.405,.85),(.36,.04,.25),'dark');b.box((.20,-.43,.88),(.27,.015,.12),'blue')
for x,c in [(.09,'green'),(.21,'glow'),(.33,'red')]:cylinder(b,(x,-.43,.77),(x,-.46,.77),.026,c,8)
for z in [.39,.45,.51,.57]:b.box((-.22,-.405,z),(.30,.025,.018),'dark')
b.box((.36,-.43,.46),(.04,.05,.17),'dark')
b.box((0,-.45,.25),(.48,.32,.04),'aluminum_light');b.box((0,-.386,.32),(.43,.03,.12),'dark')
for x in [-1,1]:b.face([(x*.19,-.20,1.10),(x*.19,.20,1.10),(x*.48,.36,1.48),(x*.48,-.36,1.48)],'aluminum',(x,0,0))
for y in [-1,1]:b.face([(-.19,y*.20,1.10),(.19,y*.20,1.10),(.48,y*.36,1.48),(-.48,y*.36,1.48)],'aluminum',(0,y,0))
for y in [-.37,.37]:b.box((0,y,1.49),(1.03,.05,.05),'aluminum_light')
for x in [-.49,.49]:b.box((x,0,1.49),(.05,.78,.05),'aluminum_light')
b.box((0,0,1.11),(.40,.40,.035),'dark')
for x in [-.30,-.10,.10,.30]:b.box((x,0,1.47),(.018,.70,.018),'dark')
finish('ModernFeedMachine',b)

# A warming box, one central straw nest, a front observation window and an opening framed lid.
b=Build();b.box((0,0,-.10),(1.04,.76,.11),'wood_dark')
for y in [-.34,.34]:
 for z in [-.015,.07]:b.box((0,y,z),(.98,.065,.075),'wood')
for x in [-.48,.48]:
 b.box((x,0,.05),(.065,.66,.27),'wood')
 for y in [-.33,.33]:b.box((x,y,.17),(.07,.075,.43),'wood_dark')
for y in [-.34,.34]:b.box((0,y,.35),(1.04,.07,.08),'wood')
for x in [-.48,.48]:b.box((x,0,.35),(.07,.67,.08),'wood')
for x in [-.36,-.24,-.12,0,.12,.24,.36]:b.box((x,-.344,.21),(.012,.018,.20),'dark')
for z in [.14,.23,.31]:b.box((0,-.348,z),(.88,.018,.012),'dark')
for y in [-.345,.345]:
 for x in [-.33,.33]:cylinder(b,(x,y,.40),(x+.12,y,.40),.025,'dark',8)
cylinder(b,(0,0,-.035),(0,0,-.005),.30,'hay',16)
for i in range(40):
 a=random.random()*math.tau;r=random.uniform(.15,.30);x,y=math.cos(a)*r,math.sin(a)*r;b.beam((x,y,-.005),(x+math.cos(a+.8)*.09,y+math.sin(a+.8)*.09,.015),.018,.012,'hay_light' if i%3==0 else 'hay')
# Small egg-shaped badge identifies the warmer even before an egg is inserted.
b.face([(math.cos(i*math.tau/12)*.045,-.385,.04+math.sin(i*math.tau/12)*.065) for i in range(12)],'ivory',(0,-1,0))
finish('IncubatorBody',b)
b=Build()
for y in [-.36,.36]:b.box((0,y,.40),(1.10,.07,.065),'wood')
for x in [-.52,.52]:b.box((x,0,.40),(.065,.72,.065),'wood')
b.box((0,-.397,.435),(.24,.06,.045),'dark');finish('IncubatorLidFrame',b)
b=Build();b.face([(-.49,-.32,.401),(.49,-.32,.401),(.49,.32,.401),(-.49,.32,.401)],'aluminum_light',(0,0,1));finish('IncubatorGlass',b)
b=Build();b.box((0,.26,.29),(.12,.11,.14),'dark');cylinder(b,(0,.23,.14),(0,.23,.22),.082,'glow',10);b.box((0,.168,.20),(.11,.025,.075),'glow');finish('IncubatorHeater',b)

# Burlap feed sack with a tied mouth, seam and recognisable wheat marking.
b=Build();rings=[(.05,.18),(.18,.26),(.49,.27),(.64,.18),(.70,.12)];n=10
vs=[(math.cos(i*math.tau/n)*r,math.sin(i*math.tau/n)*r*.75,z) for z,r in rings for i in range(n)]
faces=[tuple(reversed(range(n))),tuple(range((len(rings)-1)*n,len(rings)*n))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rings)-1) for i in range(n)]
b.solid(vs,faces,'canvas');cylinder(b,(0,0,.67),(0,0,.69),.13,'wood_dark',10)
b.beam((0,-.207,.23),(0,-.212,.49),.016,.014,'wood_dark')
for j in range(3):
 for sign in [-1,1]:b.beam((0,-.216,.29+j*.065),(sign*.07,-.215,.34+j*.065),.025,.018,'glow')
finish('FeedSack',b)
b=Build();cylinder(b,(0,0,.02),(0,0,.14),.55,'hay',10,r2=.33)
for i in range(46):
 a=random.random()*math.tau;r=random.uniform(.10,.51);x,y=math.cos(a)*r,math.sin(a)*r;b.beam((x,y,.04),(x+math.cos(a)*.10,y+math.sin(a)*.10,random.uniform(.09,.18)),.024,.012,'hay_light' if i%3 else 'hay')
finish('CornerStraw',b)
# Wide shade, protected bulb and wall bracket; four visible fixtures, two unshadowed Unity point lights.
b=Build();b.box((0,.13,.23),(.13,.05,.42),'dark');b.beam((0,.10,.36),(0,-.16,.36),.045,.045,'dark')
cylinder(b,(0,-.16,.22),(0,-.16,.31),.23,'dark',12,r2=.09);cylinder(b,(0,-.16,.15),(0,-.16,.23),.065,'glow',10)
cylinder(b,(0,-.16,.311),(0,-.16,.327),.085,'glow',10)
for x in [-.10,.10]:b.beam((x,-.16,.13),(x,-.16,.25),.012,.012,'dark')
finish('WallLamp',b)

# Export at the origin; the editable .blend uses the same source geometry and units.
for o in modules:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/f'{o.name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,embed_textures=False,path_mode='STRIP')
for i,o in enumerate(modules):o.location=((i%4)*2.4,(i//4)*2.1,0)
scene.world=bpy.data.worlds.new('Warm farm review');scene.world.color=(.3,.3,.3)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/FarmInterior_Revision.blend'))
(ART/'Palette.json').write_text(json.dumps({'palette':PAL,'atlas':'Assets/Nature  Paradaise/Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.png','modules':{o.name:{'triangles':len(o.data.polygons),'vertices':len(o.data.vertices)} for o in modules}},indent=2))
print('FARM_INTERIOR_KIT_READY',flush=True)
