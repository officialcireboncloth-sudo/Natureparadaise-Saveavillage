"""Approved minimarket exterior, mobile geometry budget; LIVE_BUILD preserves open work."""
import bpy,bmesh,math,json,hashlib,random,sys
from pathlib import Path
from mathutils import Vector,Matrix
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
LIVE=globals().get('LIVE_BUILD',False)
if LIVE:
 bpy.ops.wm.save_as_mainfile(filepath=str(ART/'References/BeforeMiniMarket_LiveSnapshot.blend'),copy=True)
else:bpy.ops.wm.read_factory_settings(use_empty=True)
random.seed(14102026)
audit=json.loads((ART/'References/atlas-audit.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256((ROOT/audit['sourcePSD']).read_bytes()).hexdigest()==audit['sourceSha256']
PAL={k:{'uvSafeRectBottomLeft':v['safeUvBottomLeft']} for k,v in audit['swatches'].items()}
BASE={'materials':{'atlasGuid':audit['sourceGUID']}}
mat=bpy.data.materials.get('TFP_Atlas_1A')
if mat is None:
 mat=bpy.data.materials.new('TFP_Atlas_1A');mat.use_nodes=True
 atlas=bpy.data.images.load(str(ART/'References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack();atlas['UnityGUID']=audit['sourceGUID']
 tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas
 bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88;bs.inputs['Specular IOR Level'].default_value=.05
 mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
exec(compile((ART/'Scripts/mesh_common.py').read_text(),str(ART/'Scripts/mesh_common.py'),'exec'),globals())
sc=bpy.data.scenes.new('MiniMarket A - EDITABLE');setup(sc);activate(sc)
sc['Approval']='Minimarket sketch approved; mobile geometry requested 2026-10-14'
source=coll('MM 01 EDITABLE components',sc);ec=coll('MM 02 EXPORT - hidden duplicate',sc);markers=coll('MM 03 SOCKETS',sc)
G={n:Build() for n in ['Structure','StoneBase','TimberAndTrim','GlassAndFrames','Awnings','Sign','Hardware','HVAC','DoorLeft','DoorRight']}
W,F,T,V,A,S,H,Q,DL,DR=G.values()
def clean(o):
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces))
 bad=[f for f in bm.faces if (f.verts[1].co-f.verts[0].co).cross(f.verts[2].co-f.verts[0].co).length<1e-10]
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
def wall(y,holes):
 cuts=sorted({-5,5}|{x-w/2 for x,w,z,h in holes}|{x+w/2 for x,w,z,h in holes})
 for a,b in zip(cuts,cuts[1:]):
  intervals=[(.25,3.95)]
  for x,w,z,h in holes:
   if x-w/2<(a+b)/2<x+w/2:intervals=[p for lo,hi in intervals for p in [(lo,min(hi,z)),(max(lo,z+h),hi)] if p[1]>p[0]+.005]
  for lo,hi in intervals:W.box(((a+b)/2,y,(lo+hi)/2),(b-a,.16,hi-lo),'plaster')
def frame(b,x,y,z,w,h,color='olive_coping'):
 # Butt joints avoid overlapping corner solids and welding artifacts.
 for xx in [x-w/2-.045,x+w/2+.045]:b.box((xx,y,z),(.09,.11,h),color)
 for zz in [z-h/2-.045,z+h/2+.045]:b.box((x,y,zz),(w+.18,.11,.09),color)
def face_x(b,x,y,z,w,h,color):
 b.face([(x,y-w/2,z-h/2),(x,y+w/2,z-h/2),(x,y+w/2,z+h/2),(x,y-w/2,z+h/2)],color,(1 if x>0 else -1,0,0))
def awning(cx,w,y,projection,brackets=True):
 # One slab; narrow single-face seams replace separate metal-panel solids.
 poly=[(cx-w/2,2.95),(cx+w/2,2.95),(cx+w/2,2.88),(cx-w/2,2.88)]
 top=[(cx-w/2,y,2.95),(cx+w/2,y,2.95),(cx+w/2,y-projection,2.65),(cx-w/2,y-projection,2.65)]
 bottom=[(x,yy,z-.07) for x,yy,z in top]
 A.solid(bottom+top,[(0,1,2,3),(4,5,6,7),(0,4,7,3),(1,5,6,2),(0,1,5,4),(3,7,6,2)],'olive_awning')
 for i in range(1,max(2,round(w/.55))):
  x=cx-w/2+i*w/max(2,round(w/.55))
  A.face([(x-.008,y,2.952),(x+.008,y,2.952),(x+.008,y-projection,2.652),(x-.008,y-projection,2.652)],'olive_coping',(0,0,1))
 if brackets:
  for x in [cx-w/2+.15,cx+w/2-.15]:T.beam((x,y,2.20),(x,y-projection*.75,2.61),.10,.10,'timber')
def rotate_builder_into_side(b,x,cy):
 off=len(A.v);A.v.extend(Vector((x-v.y,cy+v.x,v.z)) for v in b.v);A.f.extend(tuple(off+i for i in f) for f in b.f);A.col.extend(b.col)
def shade(x,y,z):
 # Six-sided shade with flat warm underside, no real-time light in exports.
 vs=[(x+math.cos(i*math.tau/6)*r,y+math.sin(i*math.tau/6)*r,zz) for zz,r in [(z,.18),(z+.15,.055)] for i in range(6)]
 H.solid(vs,[tuple(range(6)),tuple(range(6,12))]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)],'olive_coping')
 H.face([(x+math.cos(i*math.tau/6)*.14,y+math.sin(i*math.tau/6)*.14,z-.003) for i in reversed(range(6))],'lantern',(0,0,-1))
 H.beam((x,y+.28,z+.35),(x,y+.1,z+.45),.04,.04,'olive_coping');H.beam((x,y+.1,z+.45),(x,y,z+.15),.04,.04,'olive_coping')
# Walls with real front/rear openings. Amber glass is opaque; no interior shell.
wall(-3.5,[(-3,2.3,.55,1.8),(0,1.8,.15,2.4),(3,2.3,.55,1.8)])
wall(3.5,[(0,.9,.15,2.05)])
W.box((-5,0,2.1),(.16,7,3.7),'plaster');W.box((5,-2.7,2.1),(.16,1.6,3.7),'plaster');W.box((5,2.7,2.1),(.16,1.6,3.7),'plaster')
W.box((5,0,.4),(.16,3.8,.3),'plaster');W.box((5,0,3.15),(.16,3.8,1.6),'plaster')
F.box((0,0,.125),(10.16,7.16,.25),'foundation');F.box((0,-3.65,.075),(2.05,.5,.15),'foundation')
# Low roof/parapet: continuous volumes, not individual brick or tile solids.
W.box((0,0,3.90),(10,7,.10),'foundation')
for y in [-3.46,3.46]:
 W.box((0,y,4.11),(10,.18,.32),'plaster');T.box((0,y,4.28),(10.18,.27,.04),'olive_coping')
for x in [-4.96,4.96]:
 W.box((x,0,4.11),(.18,6.74,.32),'plaster');T.box((x,0,4.28),(.27,6.74,.04),'olive_coping')
# Timber frieze beneath roof coping, flat seams for plank definition.
for y in [-3.595,3.595]:
 T.box((0,y,3.62),(9.8,.05,.98),'timber')
 for z in [3.30,3.53,3.76,3.99]:T.face([(-4.9,y+(-.027 if y<0 else .027),z), (4.9,y+(-.027 if y<0 else .027),z),(4.9,y+(-.027 if y<0 else .027),z+.014),(-4.9,y+(-.027 if y<0 else .027),z+.014)],'wood_shadow',(0,-1 if y<0 else 1,0))
for x in [-5.095,5.095]:
 T.box((x,0,3.62),(.05,6.9,.98),'timber')
 for z in [3.30,3.53,3.76,3.99]:face_x(T,x+math.copysign(.027,x),0,z,6.9,.014,'wood_shadow')
for x in [-4.85,-1.7,1.7,4.85]:
 W.box((x,-3.61,2.35),(.40,.38,3.90),'plaster');F.box((x,-3.63,.38),(.56,.50,.76),'foundation')
 T.box((x,-3.61,4.31),(.52,.48,.08),'olive_coping')
for x in [-4.85,4.85]:
 W.box((x,3.51,2.35),(.40,.30,3.90),'plaster');F.box((x,3.51,.38),(.56,.42,.76),'foundation');T.box((x,3.51,4.31),(.52,.42,.08),'olive_coping')
# One-face mortar lines on bases; no per-stone volumes.
for z in [.23,.48]:
 F.face([(-5.08,-3.586,z),(5.08,-3.586,z),(5.08,-3.586,z+.012),(-5.08,-3.586,z+.012)],'wood_shadow',(0,-1,0))
for x in [-4.85,-1.7,1.7,4.85]:
 for z in [.25,.5]:F.face([(x-.28,-3.883,z),(x+.28,-3.883,z),(x+.28,-3.883,z+.012),(x-.28,-3.883,z+.012)],'wood_shadow',(0,-1,0))
for x in [-3,3]:
 V.box((x,-3.53,1.45),(2.3,.08,1.8),'window');frame(V,x,-3.60,1.45,2.3,1.8)
 for xx in [x-.38,x+.38]:V.box((xx,-3.67,1.45),(.055,.06,1.8),'olive_coping')
 T.box((x,-3.57,.48),(2.55,.30,.10),'timber')
# Right bank of three panes, left closed shutter, rear service door and vent.
face_x(V,5.025,0,1.45,3.8,1.8,'window')
for y in [-1.94,-.65,.65,1.94]:V.box((5.09,y,1.45),(.13,.08,1.8),'olive_coping')
for z in [.505,2.395]:V.box((5.09,0,z),(.13,3.96,.09),'olive_coping')
V.box((-5.10,.3,1.60),(.08,1,1),'wood_shadow')
for y in [-.25,.85]:V.box((-5.16,y,1.6),(.12,.10,1.2),'timber')
for z in [1.05,2.15]:V.box((-5.16,.3,z),(.12,1.2,.10),'timber')
for z in [1.25,1.5,1.75,2]:face_x(V,-5.147,.3,z,.99,.028,'timber')
V.box((0,3.54,1.175),(.9,.08,2.05),'timber');frame(V,0,3.60,1.175,.9,2.05,'timber');H.box((-.28,3.69,1.2),(.04,.05,.18),'metal')
V.box((-1.8,3.59,2.8),(.8,.07,.4),'wood_shadow');frame(V,-1.8,3.63,2.8,.8,.4,'foundation')
for z in [2.67,2.76,2.85,2.94]:V.box((-1.8,3.68,z),(.73,.03,.025),'metal')
frame(V,0,-3.60,1.35,1.8,2.4)
for b,cx in [(DL,-.45),(DR,.45)]:
 b.box((cx,-3.56,1.35),(.88,.10,2.4),'window');frame(b,cx,-3.64,1.35,.74,2.24)
 b.box((cx,-3.71,.39),(.74,.04,.32),'olive_coping')
 b.box((-.10 if cx<0 else .10,-3.76,1.34),(.035,.07,.30),'timber')
awning(0,5.4,-3.66,1.1)
for x in [-3.85,3.85]:awning(x,1.8,-3.66,1.1)
# Right awning: transform a temporary slab builder, preserving a single mesh group.
oldA=A;A=Build();awning(0,5.2,0,.9,False);side=A;A=oldA;rotate_builder_into_side(side,5.13,0)
for y in [-2.4,2.4]:T.beam((5.1,y,2.20),(5.78,y,2.61),.10,.10,'timber')
# Small left shutter awning, minimal quad seams.
side=Build();side.solid([(-.9,0,2.83),(.9,0,2.83),(.9,-.65,2.60),(-.9,-.65,2.60),(-.9,0,2.90),(.9,0,2.90),(.9,-.65,2.67),(-.9,-.65,2.67)],[(0,1,2,3),(4,5,6,7),(0,4,7,3),(1,5,6,2),(0,1,5,4),(3,7,6,2)],'olive_awning')
off=len(A.v);A.v.extend(Vector((-5.1+v.y,.3+v.x,v.z)) for v in side.v);A.f.extend(tuple(off+i for i in f) for f in side.f);A.col.extend(side.col)
# Low segment curved sign crest. Exact highest point remains 4.85 m.
crest=[(-2.7,3.08),(2.7,3.08),(2.7,4.30),(1.6,4.30)]+[(1.6*math.cos(a),4.30+.55*math.sin(a)) for a in [i*math.pi/8 for i in range(9)]]+[(-2.7,4.30)]
S.extrude(crest,-3.74,.15,'timber')
for z in [3.10,3.25,3.49,3.73,3.97]:S.box((0,-3.824,z),(5.4,.018,.016),'wood_shadow')
for x in [-2.74,2.74]:S.box((x,-3.80,3.70),(.12,.20,1.35),'timber')
for x0,x1 in [(-2.7,-1.6),(1.6,2.7)]:S.box(((x0+x1)/2,-3.76,4.29),(x1-x0,.25,.02),'olive_coping')
arc=[(1.6*math.cos(i*math.pi/8),4.30+.55*math.sin(i*math.pi/8)) for i in range(9)]
for (x,z),(xx,zz) in zip(arc,arc[1:]):S.extrude([(x,z),(xx,zz),(xx,zz-.08),(x,z-.08)],-3.76,.25,'olive_coping')
for cx,angle in [(-.25,-.6),(0,0),(.25,.6)]:
 points=[]
 for x,z in [(0,0),(-.09,.10),(-.12,.23),(0,.43),(.12,.23),(.09,.10)]:points.append((cx+x*math.cos(angle)+z*math.sin(angle),4.22-x*math.sin(angle)+z*math.cos(angle)))
 S.extrude(points,-3.84,.025,'leaf_emblem')
# Flat lettering: no side-wall tessellation, no bevel, low curve resolution.
curve=bpy.data.curves.new('MM Sign Typography','FONT');curve.body='SUPERMARKET';curve.align_x='CENTER';curve.align_y='CENTER';curve.size=.57;curve.resolution_u=1;curve.extrude=0;curve.bevel_depth=0
font=Path('C:/Windows/Fonts/georgiab.ttf')
if font.exists():curve.font=bpy.data.fonts.load(str(font))
letters=bpy.data.objects.new('MM_SignLetters',curve);source.objects.link(letters);letters.location=(0,-3.841,3.78);letters.rotation_euler=(math.pi/2,0,0)
bpy.ops.object.select_all(action='DESELECT');letters.select_set(True);bpy.context.view_layer.objects.active=letters;bpy.ops.object.convert(target='MESH');letters=bpy.context.object;clean(letters)
bpy.context.view_layer.update()
if letters.dimensions.x>4.95:letters.scale*=4.95/letters.dimensions.x
letters.data.materials.append(mat)
while letters.data.uv_layers:letters.data.uv_layers.remove(letters.data.uv_layers[0])
uv=letters.data.uv_layers.new(name='ToonAtlasUV');a,b,c,d=PAL['awning_ivory']['uvSafeRectBottomLeft']
for loop in uv.data:loop.uv=((a+c)/2,(b+d)/2)
letters.select_set(False)
for x in [-4.15,4.15]:shade(x,-3.88,3.53)
for x in [-3,0,3]:shade(x,-3.94,2.48)
for x in [-1.30,1.30]:
 H.box((x,-3.86,1.97),(.18,.16,.30),'lantern')
 for xx in [x-.10,x+.10]:H.box((xx,-3.96,1.97),(.025,.03,.36),'metal')
 for z in [1.78,2.16]:H.box((x,-3.89,z),(.25,.21,.05),'metal')
 H.beam((x,-3.61,2.34),(x,-3.89,2.18),.045,.045,'metal')
Q.box((-2.8,1.8,4.28),(1.1,.85,.66),'foundation');Q.box((-2.8,1.8,4.625),(1.18,.93,.05),'awning_ivory')
Q.box((-2.8,1.8,4.642),(.5,.45,.012),'metal')
for x in [-3.23,-2.37]:
 for z in [4.08,4.18,4.28,4.38,4.48]:Q.box((x,1.359,z),(.19,.012,.035),'metal')
for y in [1.52,1.66,1.8,1.94,2.08]:Q.box((-2.244,y,4.30),(.012,.045,.44),'metal')
# Convert components once, without global bevel/subdivision. Source stays editable by component.
obs=[letters]
for name,b in G.items():
 o=b.obj('MM_'+name,source);clean(o);obs.append(o)
static=[o for o in obs if o.name not in {'MM_DoorLeft','MM_DoorRight'}]
body=join_copy(static,'MiniMarket_A_Body',ec);ex=[body]
for name,pivot in [('DoorLeft',(-.9,-3.56,.15)),('DoorRight',(.9,-3.56,.15))]:
 o=bpy.data.objects['MM_'+name];hinge=empty('MM_'+name+'_Hinge',pivot,source);o.parent=hinge;o.matrix_parent_inverse=Matrix.Translation(-Vector(pivot))
 eh=empty('MiniMarket_A_'+name+'_Hinge',pivot,ec);cp=o.copy();cp.data=o.data.copy();ec.objects.link(cp);cp.parent=eh;cp.matrix_parent_inverse=o.matrix_parent_inverse.copy();cp.matrix_basis=o.matrix_basis.copy();ex.extend([eh,cp])
bpy.context.view_layer.update();export(ex,'MiniMarket_A');ec.hide_render=True;ec.hide_viewport=True
empty('MM_Ground',(0,0,0),markers);empty('MM_Entrance',(0,-4.3,0),markers)
cam=studio(sc);cam.location=(14,-19,12);cam.rotation_euler=(Vector((0,-.3,2.0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=15.7
for area in bpy.context.screen.areas:
 if area.type in {'CONSOLE','VIEW_3D'}:
  area.type='VIEW_3D';sp=area.spaces.active;sp.region_3d.view_location=(0,-.3,2.0);sp.region_3d.view_distance=19;sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.shading.type='MATERIAL';sp.overlay.show_overlays=True;sp.overlay.show_stats=True
def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
meshes=[o for o in ex if o.type=='MESH'];points=[world(o)@v.co for o in meshes for v in o.data.vertices]
triangles=sum(len(o.data.polygons) for o in meshes)
report={'status':'BUILT','scene':sc.name,'targetTrianglesMax':5000,'triangles':triangles,'meshVertices':sum(len(o.data.vertices) for o in meshes),'materialCount':1,'runtimeMeshes':len(meshes),'texturesNew':0,'atlasGUID':audit['sourceGUID'],'atlasSHA256':audit['sourceSha256'],'bounds':[[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]],'export':'MiniMarket_A.fbx','mobileChoices':['Single shared opaque atlas material','No subdivision or global bevel','Roof and canopy panel seams are thin quads','Flat low-resolution sign letters','Six-sided lamp shades; no runtime lights','Three runtime meshes: static building and two hinged doors']}
assert triangles<=5000,report
assert abs(report['bounds'][1][2]-4.85)<.01,report
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/MiniMarket_A.blend'))
print('MINIMARKET_BUILD_COMPLETE',json.dumps(report))
