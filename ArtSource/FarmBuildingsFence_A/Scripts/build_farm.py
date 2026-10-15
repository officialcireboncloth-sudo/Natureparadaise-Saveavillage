"""Approved farm exterior kit; Blender 4.5, metres, front -Y, atlas only."""
import bpy, bmesh, math, json, hashlib, random, sys
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]; ROOT=ART.parents[1]
random.seed(13)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
BASE=json.loads((ROOT/'ArtSource/Barn_A/References/Barn_A_Approved.json').read_text())
PAL=BASE['materials']['swatches']
HS=json.loads((ROOT/'ArtSource/House_A/References/House_A_Approved.json').read_text())['materials']['swatches']
PAL['honey']={'uvSafeRectBottomLeft':HS['wood']['safeUvBottomLeft']}
PAL['glass']={'uvSafeRectBottomLeft':HS['glass']['safeUvBottomLeft']}
PAL['slate']={'uvSafeRectBottomLeft':HS['roof_light']['safeUvBottomLeft']}
PAL['charcoal']={'uvSafeRectBottomLeft':HS['roof']['safeUvBottomLeft']}
ATLAS_PATH=ROOT/BASE['materials']['atlasPath']; ATLAS_HASH=hashlib.sha256(ATLAS_PATH.read_bytes()).hexdigest()
atlas=bpy.data.images.load(str(ART/'References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack()
atlas['OriginalPSD']=BASE['materials']['atlasPath'];atlas['UnityGUID']=BASE['materials']['atlasGuid']
mat=bpy.data.materials.new('TFP_Atlas_1A');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9;bs.inputs['Specular IOR Level'].default_value=.08
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;tx.interpolation='Linear';tx.extension='EXTEND'
mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);mat['UnityMaterial']='TFP_Atlas_1A.mat'

def coll(name,scene,parent=None):
 c=bpy.data.collections.new(name);(parent or scene.collection).children.link(c);return c
def setup(scene):
 scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
 scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True
 scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard'
 scene.world=bpy.data.worlds.new(scene.name+' World');scene.world.use_nodes=True
 scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.78,.83,.9,1)
 scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
 scene['Approval']='User approved sketch 2026-10-13';scene['Front']='-Y';scene['Units']='metres'
def activate(scene):bpy.context.window.scene=scene

class Build:
 def __init__(self):self.v=[];self.f=[];self.col=[]
 def face(self,vs,color,normal=None):
  vs=[Vector(v) for v in vs]
  if normal is not None and (vs[1]-vs[0]).cross(vs[2]-vs[0]).dot(Vector(normal))<0:vs.reverse()
  start=len(self.v);self.v.extend(vs);self.f.append(tuple(range(start,start+len(vs))));self.col.append((color,random.uniform(-.3,.3)))
 def solid(self,vs,faces,color):
  vs=[Vector(v) for v in vs];ctr=sum(vs,Vector())/len(vs)
  for inds in faces:
   points=[vs[i] for i in inds];self.face(points,color,sum(points,Vector())/len(points)-ctr)
 def box(self,c,size,color):
  c=Vector(c);e=Vector(size)/2
  vs=[c+Vector((x*e.x,y*e.y,z*e.z)) for x in (-1,1) for y in (-1,1) for z in (-1,1)]
  self.solid(vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],color)
 def beam(self,a,b,width,depth,color):
  a=Vector(a);b=Vector(b);axis=(b-a).normalized();ref=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))
  side=axis.cross(ref).normalized();up=axis.cross(side).normalized()
  vs=[p+side*s*width/2+up*t*depth/2 for p in (a,b) for s in (-1,1) for t in (-1,1)]
  self.solid(vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],color)
 def extrude(self,poly,y,depth,color):
  # polygon is X/Z cross-section, thickness along Y
  n=len(poly);vs=[(x,y+d,z) for d in (-depth/2,depth/2) for x,z in poly]
  faces=[tuple(range(n)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  self.solid(vs,faces,color)
 def obj(self,name,col,bevel=0):
  me=bpy.data.meshes.new(name+'Mesh');me.from_pydata(self.v,[],self.f);me.update();me.materials.append(mat)
  uv=me.uv_layers.new(name='ToonAtlasUV');attr=me.attributes.new('PaletteID','INT','FACE');keys=list(PAL)
  for i,p in enumerate(me.polygons):
   color,var=self.col[i];a,b,c,d=PAL[color]['uvSafeRectBottomLeft'];u=(a+c)/2;v=(b+d)/2+var*(d-b)*.25
   attr.data[i].value=keys.index(color)
   for idx in p.loop_indices:uv.data[idx].uv=(u,v)
  ob=bpy.data.objects.new(name,me);col.objects.link(ob)
  if bevel:
   bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
   mod=ob.modifiers.new('Soft chunky edges','BEVEL');mod.width=bevel;mod.segments=1
   bpy.ops.object.modifier_apply(modifier=mod.name);ob.select_set(False)
   # Reassign bevel UV from inherited palette IDs.
   uv=me.uv_layers.active;attr=me.attributes.get('PaletteID')
   for p in me.polygons:
    color=keys[attr.data[p.index].value];a,b,c,d=PAL[color]['uvSafeRectBottomLeft']
    for idx in p.loop_indices:uv.data[idx].uv=((a+c)/2,(b+d)/2)
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
  ob['AtlasGUID']=BASE['materials']['atlasGuid'];return ob

def empty(name,pos,col):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.location=pos;o.empty_display_type='PLAIN_AXES';o.empty_display_size=.15;bpy.context.view_layer.update();return o
def join_copy(obs,name,col):
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for o in obs:
  cp=o.copy();cp.data=o.data.copy();col.objects.link(cp);cp.select_set(True);copies.append(cp)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces))
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.verts[1].co-f.verts[0].co).cross(f.verts[2].co-f.verts[0].co).length<2e-10],context='FACES')
 bm.to_mesh(o.data);bm.free();o.data.update()
 o.select_set(False);return o
def export(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 # A second, unparented empty keeps Unity's generated file root at identity.
 # Axis conversion stays on authored child nodes, including gate hinges.
 axis_reference=bpy.data.objects.new('FBX_AxisReference',None);bpy.context.scene.collection.objects.link(axis_reference)
 axis_reference.empty_display_size=.01;axis_reference.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/f'{name}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
  global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
  use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 bpy.data.objects.remove(axis_reference,do_unlink=True)
 bpy.ops.object.select_all(action='DESELECT')

def studio(scene):
 c=coll('90 PREVIEW ONLY',scene)
 fm=bpy.data.materials.get('PREVIEW Floor') or bpy.data.materials.new('PREVIEW Floor');fm.diffuse_color=(.65,.69,.65,1)
 bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.02));o=bpy.context.object
 for old in list(o.users_collection):old.objects.unlink(o)
 c.objects.link(o);o.data.materials.append(fm);o.name='Studio floor - excluded export'
 for name,loc,power,size in [('Key',(-7,-10,13),1700,8),('Fill',(9,-2,10),1100,7),('Rim',(0,8,11),1400,6)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size;o=bpy.data.objects.new(name,data);c.objects.link(o);o.location=loc
  o.rotation_euler=(Vector((0,0,2.5))-o.location).to_track_quat('-Z','Y').to_euler()
 cam=bpy.data.objects.new('Preview Camera',bpy.data.cameras.new('Preview Camera'));c.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';return cam
def render(scene,cam,name,pos,target,scale):
 activate(scene);cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 scene.render.filepath=str(ART/'Previews'/f'{name}.png')
 if '--skip-renders' not in sys.argv:bpy.ops.render.render(write_still=True)

SPEC=[dict(name='Farm_Lumbung_A',w=7,d=5,m=4,left=1.5,right=1.5,h=5.6,e=3,side=2.2,k=1.25,kz=4.6,wall='wood',roof='red',doorh=2.6),
 dict(name='Farm_BarnStore_B',w=9,d=6,m=5,left=1.5,right=2.5,h=5.4,e=3.1,side=2.3,k=1.55,kz=4.6,wall='red',roof='charcoal',doorh=2.5)]
assets=[];report=[]
def building(p,index):
 name=p['name'];sc=bpy.data.scenes.new(name+' Editable');setup(sc);activate(sc)
 source=coll('01 EDITABLE '+name,sc);ec=coll('02 EXPORT '+name,sc);markers=coll('03 SOCKETS '+name,sc)
 groups={key:Build() for key in ['Walls','Foundation','RoofTiles','RoofBase','Timber','Doors','Windows','Hardware']}
 W,F,R,RB,T,D,WIN,M=[groups[x] for x in groups]
 w,d,m=p['w'],p['d'],p['m'];xmin=-w/2;xmax=w/2;c=xmin+p['left']+m/2;xl=c-m/2;xr=c+m/2;yfront=-d/2
 def height(x):
  t=abs(x-c)
  if t<=p['k']:return p['h']-.12+(p['kz']-p['h'])*t/p['k']
  return p['kz']-.12+(p['e']-p['kz'])*(t-p['k'])/(m/2-p['k'])
 def boards_front(a,b,y,top,holes=()):
  count=math.ceil((b-a)/.26);pitch=(b-a)/count
  for i in range(count):
   lo=a+i*pitch+.006;hi=a+(i+1)*pitch-.006;zlo=.25;zt=min(top(lo),top(hi));intervals=[(zlo,zt)]
   for hx,hw,hz,hh in holes:
    if lo<hx+hw/2 and hi>hx-hw/2:
     intervals=[part for aa,bb in intervals for part in [(aa,min(bb,hz)),(max(aa,hz+hh),bb)] if part[1]>part[0]+.002]
   for aa,bb in intervals:W.extrude([(lo,aa),(hi,aa),(hi,bb),(lo,bb)],y,.09,p['wall'])
 def boards_side(x,top):
  count=math.ceil(d/.26);pitch=d/count
  for i in range(count):W.box((x,-d/2+(i+.5)*pitch,(.25+top)/2),(.09,pitch-.012,top-.25),p['wall'])
 loftw=1 if index==0 else 1.4;lofth=.9 if index==0 else 1;loftz=3.65 if index==0 else 3.85
 boards_front(xl,xr,yfront,height,[(c,2.4,.15,p['doorh']),(c,loftw,loftz,lofth)])
 boards_front(xl,xr,d/2,height)
 # Main side shell closes loft and makes side canopies read as attached wings.
 boards_side(xl,p['e']-.1);boards_side(xr,p['e']-.1)
 # Side wing backs and outer walls; A left nook is open only at the front.
 for a,b,open_front in [(xmin,xl,index==0),(xr,xmax,index==1)]:
  slope=lambda x,a=a,b=b: p['side']-.16+(p['e']-p['side'])*(x-a)/(b-a) if b==xl else p['e']-.16-(p['e']-p['side'])*(x-a)/(b-a)
  if not open_front:boards_front(a,b,yfront,slope)
  boards_front(a,b,d/2,slope)
  if not(index==1 and a==xr):boards_side(a if b==xl else b,p['side']-.08)
  else:
   for yy in [-d/2,.0,d/2]:
    T.box((xmax-.10,yy,(.25+p['side']-.17)/2),(.20,.20,p['side']-.42),'wood')
    T.beam((xmax-.10,yy,p['side']-.65),(xmax-.75,yy,p['side']-.02),.13,.13,'wood')
   T.box(((xr+xmax)/2,0,.12),(p['right'],d,.24),'stone_light')
  if index==0 and b==xl:
   T.box((xmin+.1,yfront,(.25+p['side'])/2),(.20,.20,p['side']-.25),'wood')
   T.beam((xmin+.10,yfront,p['side']-.6),(xmin+.75,yfront,p['side']),.13,.13,'wood')
 # Foundation stones along full rear and external sides; front opening unobstructed.
 for y in [yfront,d/2]:
  count=math.ceil(w/.65);pitch=w/count
  for i in range(count):
   xx=xmin+(i+.5)*pitch
   if y==yfront and abs(xx-c)<1.25:continue
   F.box((xx,y,.125),(pitch-.014,.20,.25),'stone' if i%3 else 'stone_light')
 for x in [xmin,xmax]:
  if index==1 and x==xmax:continue
  count=math.ceil(d/.7);pitch=d/count
  for i in range(count):F.box((x,-d/2+(i+.5)*pitch,.125),(.2,pitch-.012,.25),'stone_light')
 F.box((c,yfront-.15,.075),(2.65,.45,.15),'stone_light')
 # Main gambrel plus lean-to cross-section; no curved or gable substitution.
 profiles=[(xmin-.35,p['side']-.22),(xl,p['e']-.04),(c-p['k'],p['kz']-.04),(c,p['h']-.04),(c+p['k'],p['kz']-.04),(xr,p['e']-.04),(xmax+.35,p['side']-.22)]
 for seg,(a,b) in enumerate(zip(profiles,profiles[1:])):
  x0,z0=a;x1,z1=b;length=math.hypot(x1-x0,z1-z0);rows=math.ceil(length/.46);cols=math.ceil((d+.7)/.58)
  RB.extrude([(x0,z0-.085),(x1,z1-.085),(x1,z1-.03),(x0,z0-.03)],0,d+.7,'red_dark' if index==0 else 'roof_dark')
  for row in range(rows):
   t0=row/rows+.005/length;t1=(row+1)/rows-.005/length
   aa=(x0+(x1-x0)*t0,z0+(z1-z0)*t0);bb=(x0+(x1-x0)*t1,z0+(z1-z0)*t1)
   for j in range(cols):
    pitch=(d+.7)/cols;yy=-(d+.7)/2+(j+.5)*pitch
    R.extrude([(aa[0],aa[1]-.028),(bb[0],bb[1]-.028),(bb[0],bb[1]),(aa[0],aa[1])],yy,pitch-.012,p['roof'])
  for yy in [-(d+.7)/2,(d+.7)/2]:T.extrude([(x0,z0+.01),(x1,z1+.01),(x1,z1-.22),(x0,z0-.22)],yy,.15,'wood')
 T.box((c,0,p['h']-.055),(.14,d+.7,.11),p['roof'])
 for x in [xmin-.35,xmax+.35]:T.box((x,0,p['side']-.32),(.16,d+.7,.18),'wood')
 for x in [xl,xr]:
  for yy in [yfront,d/2]:T.box((x,yy,(.25+p['e']-.20)/2),(.20,.19,p['e']-.45),'wood')
 T.box((c,yfront-.065,p['e']-.18),(m,.18,.18),'wood')
 # Door leaf boards and hardware. Front door is a closed exterior asset.
 for side in [-1,1]:
  mid=c+side*.60
  for i in range(5):D.box((mid-.6+(i+.5)*.24,yfront-.06,.15+p['doorh']/2),(.23,.12,p['doorh']),'honey')
  if index==1:
   WIN.box((mid,yfront-.135,1.98),(.90,.022,.97),'glass')
   for xx in [mid-.46,mid,mid+.46]:T.box((xx,yfront-.16,1.98),(.055,.05,1.04),'honey')
   for zz in [1.48,1.98,2.48]:T.box((mid,yfront-.16,zz),(.96,.05,.055),'honey')
  bh=1.1 if index else 2.24
  T.beam((mid-side*.50,yfront-.155,.34),(mid+side*.50,yfront-.155,.34+bh),.12,.075,'honey')
  if index==1:T.beam((mid+.50,yfront-.16,.34),(mid-.50,yfront-.16,.34+bh),.10,.065,'honey')
  for z in [.48,2.36]:M.box((c+side*1.04,yfront-.15,z),(.32,.06,.10),'metal')
  M.box((c+side*.12,yfront-.20,1.38),(.065,.10,.30),'metal')
 for x in [c-1.30,c+1.30]:T.box((x,yfront-.09,.15+p['doorh']/2),(.18,.20,p['doorh']+.15),'wood')
 T.box((c,yfront-.10,.15+p['doorh']+.09),(2.78,.24,.20),'wood')
 # Loft A has a real gap in wall boards and inset dark reveal; B closed X shutter.
 WIN.box((c,yfront+(.25 if index==0 else -.03),loftz+lofth/2),(loftw,.025,lofth),'metal' if index==0 else 'honey')
 for x in [c-loftw/2-.06,c+loftw/2+.06]:T.box((x,yfront-.06,loftz+lofth/2),(.13,.20,lofth+.22),'wood')
 for zz in [loftz-.06,loftz+lofth+.06]:T.box((c,yfront-.09,zz),(loftw+.26,.24,.13),'wood')
 if index==1:
  for sign in [-1,1]:T.beam((c-loftw/2+.07,yfront-.16,loftz+(.07 if sign==1 else lofth-.07)),(c+loftw/2-.07,yfront-.16,loftz+(lofth-.07 if sign==1 else .07)),.12,.07,'wood')
  D.box((c,yfront-.20,3.27),(2.8,.14,.65),'honey')
  for xx in [c-1.28,c+1.28]:
   for zz in [3.05,3.48]:M.box((xx,yfront-.285,zz),(.05,.025,.05),'metal')
 # Attached wall lights: simple eight-sided bell shades, no new emission material.
 for xx in [c-1.64,c+1.64]:
  M.beam((xx,yfront-.02,2.84),(xx,yfront-.33,2.84),.065,.065,'metal')
  verts=[]
  for zz,rad in [(2.85,.055),(2.67,.17)]:
   verts.extend((xx+rad*math.cos(i*math.tau/8),yfront-.36+rad*math.sin(i*math.tau/8),zz) for i in range(8))
  M.solid(verts,[tuple(range(8)),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)],'metal')
  WIN.box((xx,yfront-.36,2.62),(.10,.10,.11),'glow')
 # Windows/service details authored as facade assemblies, no interior promised.
 def window_front(x,y,z,ww=1,hh=1,closed=False):
  WIN.box((x,y,z),(ww,.08,hh),'wood_dark' if closed else 'glass')
  for xx in [x-ww/2-.05,x+ww/2+.05]:T.box((xx,y-.03,z),(.12,.14,hh+.2),'wood')
  for zz in [z-hh/2-.05,z+hh/2+.05]:T.box((x,y-.05,zz),(ww+.2,.17,.12),'wood')
  if closed:
   for zz in [z-.25,z,z+.25]:T.box((x,y-.055,zz),(ww,.08,.09),'honey')
  else:
   T.box((x,y-.06,z),(.055,.08,hh),'honey');T.box((x,y-.06,z),(ww,.08,.055),'honey')
 if index==1:window_front((xmin+xl)/2,yfront-.07,1.30,.85,.95)
 window_front(c,d/2+.07,3.90,.80,.60,True)
 if index==1:window_front(c,d/2+.07,1.48,1.15,1.05)
 # Outer side window in plane Y/Z.
 WIN.box((xmin-.06,-.6,1.35),(.10,.90,.9),'wood_dark' if index==0 else 'glass')
 for yy in [-1.1,-.1]:T.box((xmin-.11,yy,1.35),(.17,.12,1.08),'wood')
 for zz in [.85,1.85]:T.box((xmin-.11,-.6,zz),(.17,1.1,.12),'wood')
 if index==0:
  D.box((xmax+.06,.65,1),(.12,.90,2),'honey')
  for yy in [.14,1.16]:T.box((xmax+.08,yy,1),(.18,.12,2),'wood')
  T.box((xmax+.08,.65,2),(.18,1.14,.08),'wood')
  M.box((xmax+.16,.97,1.28),(.06,.065,.25),'metal')
 obs=[g.obj(name+'_'+key,source,.012 if key in ['Doors','Foundation'] else 0) for key,g in groups.items() if g.f]
 if index==1:
  # Text is geometry, so the board still uses only the existing atlas material.
  curve=bpy.data.curves.new('Barn Store letters','FONT');curve.body='BARN STORE';curve.align_x='CENTER';curve.align_y='CENTER'
  curve.size=.32;curve.extrude=.002;curve.resolution_u=2
  text=bpy.data.objects.new('Farm_BarnStore_B_SignLetters',curve);source.objects.link(text)
  text.location=(c,yfront-.283,3.27);text.rotation_euler=(math.pi/2,0,0)
  bpy.ops.object.select_all(action='DESELECT');text.select_set(True);bpy.context.view_layer.objects.active=text
  bpy.ops.object.convert(target='MESH');text=bpy.context.object;text.data.materials.append(mat)
  bm=bmesh.new();bm.from_mesh(text.data)
  bmesh.ops.triangulate(bm,faces=list(bm.faces))
  bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.verts[1].co-f.verts[0].co).cross(f.verts[2].co-f.verts[0].co).length<2e-10],context='FACES')
  bm.to_mesh(text.data);bm.free();text.data.update()
  while text.data.uv_layers:text.data.uv_layers.remove(text.data.uv_layers[0])
  uv=text.data.uv_layers.new(name='ToonAtlasUV');aa,bb,cc,dd=PAL['metal']['uvSafeRectBottomLeft']
  for loop in uv.data:loop.uv=((aa+cc)/2,(bb+dd)/2)
  text['AtlasGUID']=BASE['materials']['atlasGuid'];obs.append(text);text.select_set(False)
 combined=join_copy(obs,name,ec);export([combined],name);ec.hide_render=True;ec.hide_viewport=True
 empty('GroundOrigin',(0,0,0),markers);empty('Entrance',(c,yfront-.65,0),markers)
 sc['FootprintWH']=str((w,d));sc['Height']=p['h'];sc['RoofShape']='Gambrel with lean-to wings'
 cam=studio(sc);assets.append((sc,combined,p));return sc,cam,obs

for i,p in enumerate(SPEC):
 sc,cam,obs=building(p,i)
 for label,pos in [('FrontRight',(12,-16,11)),('RearLeft',(-12,16,11)),('Front',(0,-20,3)),('Right',(20,0,3))]:
  render(sc,cam,p['name']+'_'+label,pos,(0,0,2.65),12 if i==0 else 14)
 # Restore source edit view to a useful 3/4 camera angle.
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_distance=15;area.spaces.active.region_3d.view_location=(0,0,2.5)

# Fence kit is local at origin per asset; source collections hide other modules.
fs=bpy.data.scenes.new('FenceKit Modular');setup(fs);activate(fs);fsource=coll('01 EDITABLE Fence',fs);fe=coll('02 EXPORT Fence',fs)
fence=[]
def module(name,build,pivot=(0,0,0),sockets=()):
 c=coll(name,fsource and fs,fsource);o=build.obj(name,c,.01);o['PitchMetres']=2 if '2m' in name else 1 if '1m' in name else 0
 root=empty(name+'_Root',pivot,c);o.parent=root;o.matrix_parent_inverse=root.matrix_world.inverted()
 for nm,pos in sockets:empty(name+'_'+nm,pos,c).parent=root
 export(list(c.objects),name);fence.append((c,o,root));return c,o,root
def post_build():
 b=Build();b.box((0,0,.60),(.24,.24,1.20),'honey')
 for zz in [.4,.85]:b.box((0,-.127,zz),(.045,.025,.045),'metal')
 return b
module('Fence_Post',post_build())
for pitch in [1,2]:
 b=Build()
 for zz in [.4,.85]:b.box((pitch/2,0,zz),(pitch-.24,.10,.16),'honey')
 module(f'Fence_Rails_{pitch}m',b,sockets=[('Start',(0,0,0)),('End',(pitch,0,0))])
for name,dirs in [('Fence_Corner_90',[(1,0),(0,1)]),('Fence_Junction_T',[(1,0),(-1,0),(0,1)])]:
 # Only one post geometry; sockets describe rail directions without extra stubs.
 cc,_,_=module(name,post_build(),sockets=[('Socket'+str(i),(x*.12,y*.12,0)) for i,(x,y) in enumerate(dirs)])
 for i,(x,y) in enumerate(dirs):
  socket=bpy.data.objects[name+'_Socket'+str(i)];socket.rotation_euler.z=math.atan2(y,x);socket['DirectionXY']=[x,y]
 export(list(cc.objects),name)
for pitch in [2,3]:
 c=coll(f'Fence_Gate_{pitch}m',fs,fsource);root=empty(f'Fence_Gate_{pitch}m_Root',(0,0,0),c)
 for xx in [0,pitch]:
  b=post_build();o=b.obj(f'Gate{pitch}_Post_{xx}',c,.01);o.location.x=xx;o.parent=root
 empty('GateSocketStart',(0,0,0),c).parent=root;empty('GateSocketEnd',(pitch,0,0),c).parent=root
 leaves=[(.18,1.64,False)] if pitch==2 else [(.18,1.29,False),(pitch-.18,1.29,True)]
 for n,(hingex,width,flip) in enumerate(leaves):
  b=Build();sg=-1 if flip else 1;mid=hingex+sg*width/2
  for z in [.23,.97]:b.box((mid,0,z),(width,.10,.16),'honey')
  for xx in [hingex+sg*.06,hingex+sg*(width-.06)]:b.box((xx,0,.60),(.12,.10,.90),'honey')
  b.beam((hingex+sg*.08,-.015,.31),(hingex+sg*(width-.08),-.015,.89),.09,.065,'honey')
  b.beam((hingex+sg*.08,-.025,.89),(hingex+sg*(width-.08),-.025,.31),.09,.065,'honey')
  for zz in [.32,.88]:b.box((hingex+sg*.11,-.075,zz),(.22,.04,.07),'metal')
  b.box((hingex+sg*(width-.08),-.075,.62),(.05,.05,.18),'metal')
  o=b.obj(f'Gate{pitch}_Leaf_{n}',c,.008)
  hinge=empty(f'Gate{pitch}_Hinge_{n}',(hingex,0,.15),c);hinge.parent=root
  o.parent=hinge;o.matrix_parent_inverse=hinge.matrix_world.inverted();hinge['RotationAxis']='local Z';hinge['LeafWidth']=width
 export(list(c.objects),f'Fence_Gate_{pitch}m');fence.append((c,None,root))

# Overview scene displays reusable mesh copies; no scenery is exported.
ov=bpy.data.scenes.new('00 Farm + Fence Overview');setup(ov);activate(ov);display=coll('01 DISPLAY Instances',ov)
for sc,combined,p in assets:
 o=combined.copy();o.data=combined.data;display.objects.link(o);o.hide_render=False;o.hide_viewport=False;o.location.x=-5.5 if 'Lumbung' in p['name'] else 5.5
for i,(c,o,root) in enumerate(fence):
 # Preserve hierarchy and hinge pivots in overview copies.
 copies={}
 for original in c.objects:
  cp=original.copy();display.objects.link(cp);copies[original]=cp
 for original,cp in copies.items():
  if original.parent in copies:cp.parent=copies[original.parent]
  else:cp.parent=None;cp.location+=Vector((-8+i*2.6,-7,0))
  if original.parent in copies:cp.matrix_parent_inverse=original.matrix_parent_inverse.copy();cp.matrix_basis=original.matrix_basis.copy()
cam=studio(ov);render(ov,cam,'FarmKit_Overview',(24,-33,24),(0,-2,2),27)
cam.location=(24,-33,24);cam.rotation_euler=(Vector((0,-2,2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=27

def bounds(obs):
 pts=[o.matrix_world@v.co for o in obs if o.type=='MESH' for v in o.data.vertices]
 return [[min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]]
for sc,o,p in assets:
 activate(sc);bpy.context.view_layer.update();lo,hi=bounds([o]);assert abs((hi[0]-lo[0])-(p['w']+.7))<.25
 assert abs(hi[2]-p['h'])<.01,(p['name'],hi)
 report.append(dict(asset=p['name'],triangles=sum(len(f.vertices)-2 for f in o.data.polygons),bounds=[lo,hi],footprint=[p['w'],p['d']],origin=list(o.location)))
for c,o,root in fence:
 meshes=[a for a in c.objects if a.type=='MESH'];report.append(dict(asset=c.name,triangles=sum(len(f.vertices)-2 for a in meshes for f in a.data.polygons),bounds=bounds(meshes)))
assert hashlib.sha256(ATLAS_PATH.read_bytes()).hexdigest()==ATLAS_HASH
activate(ov)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_location=(0,-2,2);area.spaces.active.region_3d.view_distance=29
  area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.shading.type='MATERIAL'
  area.spaces.active.clip_end=400
(ART/'Previews/MeshManifest.json').write_text(json.dumps(dict(blender=bpy.app.version_string,atlasGUID=BASE['materials']['atlasGuid'],atlasSHA256=ATLAS_HASH,newRuntimeTextures=0,assets=report),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/FarmBuildingsFence_A.blend'))
print('FARM_BUILD_COMPLETE '+json.dumps(report))
