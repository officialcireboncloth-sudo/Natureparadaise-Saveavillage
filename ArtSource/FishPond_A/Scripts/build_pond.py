"""Approved pond geometry, Blender 4.5; raw sources/exports only, no Unity edits."""
from pathlib import Path
import bpy, bmesh, json, math, itertools, re, hashlib, sys
from mathutils import Vector

ART=Path(__file__).resolve().parents[1]; ROOT=ART.parents[1]
SPEC_PATH=ART/'References/FishPond_A_Approved.json'
S=json.loads(SPEC_PATH.read_text(encoding='utf-8')); PAL=S['materials']['swatches']
assert hashlib.sha256((ROOT/S['materials']['atlasPath']).read_bytes()).hexdigest()==S['materials']['atlasSha256']
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0

def convert(p): return Vector((p[0],p[2],p[1]))
def coll(name,scene):
 c=bpy.data.collections.new(name);scene.collection.children.link(c);return c
def family(p):
 return re.sub(r'_(?:bottom|top|cap|side\d+|face\d+|bevel\d+)$','',p['name'])
def part_faces(p):
 if p['type']=='surface':return [[Vector(v) for v in p['verticesXYZ']]]
 if p['type']=='box':
  c=Vector(p['centerXYZ']);e=Vector(p['sizeXYZ'])/2
  vs=[c+Vector(q)*e for q in itertools.product([-1,1],repeat=3)]
 else:
  a=Vector(p['fromXYZ']);b=Vector(p['toXYZ']);axis=(b-a).normalized()
  ref=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((0,0,1))
  side=axis.cross(ref).normalized();up=axis.cross(side)
  vs=[q+side*t*p['width']/2+up*u*p['depth']/2 for q in [a,b] for t,u in itertools.product([-1,1],repeat=2)]
 return [[vs[i] for i in ids] for ids in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]]

atlas=bpy.data.images.load(str(ART/'References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack()
atlas['SourcePSD']=S['materials']['atlasPath'];atlas['UnityGUID']=S['materials']['atlasGuid']
opaque=bpy.data.materials.new('TFP_Atlas_1A');opaque.use_nodes=True
bs=opaque.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9;bs.inputs['Specular IOR Level'].default_value=.08
tx=opaque.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;tx.interpolation='Linear';tx.extension='EXTEND'
opaque.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
opaque['UnityMaterial']='Existing shared TFP_Atlas_1A; no new runtime texture'
water=bpy.data.materials.new('TFP_Water_1A');water.use_nodes=True
wb=water.node_tree.nodes.get('Principled BSDF');wb.inputs['Base Color'].default_value=(.055,.38,.43,1)
wb.inputs['Roughness'].default_value=.24;wb.inputs['Transmission Weight'].default_value=.48;wb.inputs['IOR'].default_value=1.333
water.diffuse_color=(.1,.5,.58,1);water['PreviewOnly']='Blender water approximation; Unity Toon/CustomWaterSimple required'
water['UnityMaterialGUID']=S['materials']['waterMaterialGuid']

class Builder:
 def __init__(self):self.v=[];self.f=[];self.colors=[];self.pids=[]
 def face(self,vs,color,pid,normal):
  vs=[convert(v) for v in vs];normal=convert(normal)
  if (vs[1]-vs[0]).cross(vs[2]-vs[0]).dot(normal)<0:vs.reverse()
  i=len(self.v);self.v.extend(vs);self.f.append(tuple(range(i,i+len(vs))));self.colors.append(color);self.pids.append(pid)
 def object(self,name,c,is_water=False,soft=False):
  me=bpy.data.meshes.new(name+'Mesh');me.from_pydata(self.v,[],self.f);me.update()
  me.materials.append(water if is_water else opaque)
  uv=me.uv_layers.new(name='WaterMetreUV' if is_water else 'ToonAtlasUV')
  pi=me.attributes.new('ApprovedPartIndex','INT','FACE');pa=me.attributes.new('PaletteIndex','INT','FACE')
  for i,p in enumerate(me.polygons):
   pi.data[i].value=self.pids[i];pa.data[i].value=-1 if is_water else list(PAL).index(self.colors[i])
   for k in p.loop_indices:
    if is_water:
     q=me.vertices[me.loops[k].vertex_index].co;uv.data[k].uv=(q.x,q.y)
    else:
     a,b,d,e=PAL[self.colors[i]]['safeUvBottomLeft'];uv.data[k].uv=((a+d)/2,(b+e)/2)
  o=bpy.data.objects.new(name,me);c.objects.link(o)
  if soft:
   bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bm.to_mesh(me);bm.free()
   bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
   mod=o.modifiers.new('Barn style soft corners','BEVEL');mod.width=.018;mod.segments=1;mod.limit_method='ANGLE';mod.angle_limit=.65
   bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
   # A beveled part uses one palette only; bevel faces inherit that safe center.
   a,b,d,e=PAL[self.colors[0]]['safeUvBottomLeft']
   for p in me.polygons:
    for k in p.loop_indices:me.uv_layers.active.data[k].uv=((a+d)/2,(b+e)/2)
  return o

def centers(parts):
 groups={}
 for p in parts:
  if p['type']=='surface':groups.setdefault(family(p),[]).extend(Vector(v) for v in p['verticesXYZ'])
 return {k:sum(v,Vector())/len(v) for k,v in groups.items()}

def normal_for(p,vs,cs,m):
 n=p['name'];center=sum(vs,Vector())/len(vs)
 if p['type']!='surface':
  base=Vector(p['centerXYZ']) if p['type']=='box' else (Vector(p['fromXYZ'])+Vector(p['toXYZ']))/2
  return center-base
 if n.startswith('basin_innerwall'):return Vector((1.5-center.x,0,-center.z))
 if n.startswith('basin_outerwall'):return Vector((center.x-1.5,0,center.z))
 if n=='water_surface' or n=='basin_floor' or n.startswith(('basin_coping','moss')) or n=='maker_grain_top':return Vector((0,1,0))
 if n.startswith('leaf_patch'):return Vector((0,.25,-1))
 if n.startswith('maker_hopper'):return center-Vector((m['stations']['maker']['centerXYZ'][0],2.36,m['stations']['maker']['centerXYZ'][2]))
 return center-cs[family(p)]

def reduced_parts(m,lod):
 parts=m['parts'];result=[]
 for p in parts:
  n=p['name'];g=p['group']
  if g in ('water','sketchWaterFx'):continue
  if lod>=1 and (n.startswith(('motor_ring','moss')) or (lod==2 and g in ('decor','underwaterDecor'))):continue
  if lod==2 and n.startswith(('rim_boulder','dock_board','maker_button','barrel_hoop','barrel_handle','feeder_button','feeder_knob','feed_sack')):continue
  if lod>=1 and (n.startswith(('rim_boulder','bottom_rock','inlet_base'))):
   if '_bevel' in n:continue
   if '_face' in n:
    q=dict(p);bn=n.replace('_face','_bevel');upper=next(v for v in parts if v['name']==bn)
    q['verticesXYZ']=[p['verticesXYZ'][0],p['verticesXYZ'][1],upper['verticesXYZ'][2],upper['verticesXYZ'][3]];result.append(q);continue
  result.append(p)
 if lod==2:
  q=m['dock'];x,y,z=q['centerXYZ']
  result.append(dict(name='dock_surface_low',type='box',centerXYZ=[x,y,z],sizeXYZ=[q['width'],.10,q['depth']],material='wood_light',group='dock',id='LOD2_Deck'))
 return result

def build_pieces(m,lod,c):
 parts=reduced_parts(m,lod);cs=centers(m['parts']);builders={};soft=[]
 for p in parts:
  pid=int(p['id'].split('_')[-1]) if p['id'].startswith('Pond') else -1
  use_bevel=lod==0 and p['type']=='box' and p['name'].startswith(('maker_body','supply_box','supply_roof','dock_post'))
  b=Builder() if use_bevel else builders.setdefault(p['group'],Builder())
  for vs in part_faces(p):
   norm=normal_for(p,vs,cs,m);b.face(vs,p['material'],pid,norm)
   if p['name'].startswith('leaf_patch'):b.face(vs,p['material'],pid,-norm)
  if use_bevel:soft.append(b.object(m['id']+'_'+p['name']+'_soft',c,soft=True))
 return [b.object(m['id']+'_'+g,c) for g,b in builders.items()]+soft

def combine(objects,name,c):
 bpy.ops.object.select_all(action='DESELECT')
 copies=[]
 for o in objects:
  q=bpy.data.objects.new(name+'_piece',o.data.copy());c.objects.link(q);q.select_set(True);copies.append(q)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
 bmesh.ops.dissolve_degenerate(bm,dist=1e-8,edges=list(bm.edges));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 o['ModelledFrom']='FishPond_A_Approved.json';o['BlenderFront']='-Y';o['Origin']='Reserved site center at ground'
 return o

def marker(c,name,p,**props):
 o=bpy.data.objects.new(name,None);c.objects.link(o);o.location=convert(p);o.empty_display_type='ARROWS';o.empty_display_size=.3;o['ApprovedXYZ']=p
 for k,v in props.items():o[k]=v
 return o

world=bpy.data.worlds.new('Pond neutral daylight');world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.78,.83,.9,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
floor_mat=bpy.data.materials.new('PREVIEW ONLY Ground');floor_mat.use_nodes=True
floor_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.63,.67,.45,1)
floor_mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=1

def setup(scene):
 scene.world=world;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
 scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
 scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
 scene['Scope']='Approved Blender model only; no Unity apply; water is Blender preview approximation'

def studio(scene):
 c=coll('03 PREVIEW STUDIO',scene)
 me=bpy.data.meshes.new('PreviewGround');me.from_pydata([(-100,-100,-.025),(100,-100,-.025),(100,100,-.025),(-100,100,-.025)],[],[(0,1,2,3)]);me.materials.append(floor_mat)
 o=bpy.data.objects.new('Preview ground - not exported',me);c.objects.link(o)
 for name,power,loc,size in [('Key',2800,(-10,-12,18),10),('Fill',1800,(12,-2,14),10),('Rim',2200,(0,12,15),8)]:
  d=bpy.data.lights.new('Preview '+name,'AREA');d.energy=power;d.shape='DISK';d.size=size
  o=bpy.data.objects.new(d.name,d);c.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
 d=bpy.data.cameras.new('Pond camera');d.type='ORTHO';o=bpy.data.objects.new(d.name,d);c.objects.link(o);scene.camera=o
 return o

def aim(cam,m,side):
 b=m['visualBounds'];lo=b['minXYZ'];hi=b['maxXYZ'];center=convert([(lo[0]+hi[0])/2,.60,(lo[2]+hi[2])/2])
 direction=Vector((1,-1,1.15)) if side=='FrontRight' else Vector((-1,1,1.15))
 cam.location=center+direction*23;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 cam.data.ortho_scale=max(b['sizeWDH'][:2])*1.12+2

def export_obj(o,path):
 bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,
  apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',mesh_smooth_type='FACE',
  add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)

scenes={};masters={};waters={};audit=[]
for index,m in enumerate(S['models']):
 key=m['id'];scene=bpy.context.scene if index==0 else bpy.data.scenes.new('Fish Pond A '+key)
 scene.name='Fish Pond A '+key;setup(scene);bpy.context.window.scene=scene
 editable=coll('01 EDITABLE SOURCE '+key,scene);exports=coll('02 COMBINED MESHES '+key,scene);markers=coll('04 STATION AND FX MARKERS '+key,scene)
 pieces=build_pieces(m,0,editable);lods=[]
 for lod in range(3):
  if lod:
   temp=coll('TEMP '+key,scene);ps=build_pieces(m,lod,temp)
  else:ps=pieces
  name='FishPond_A_'+key+'_'+['High','Medium','Low'][lod];o=combine(ps,name,exports);lods.append(o)
  o['LOD']=lod;o['Capacity']=m['capacity'];o['AutoFeeder']=m['stations']['autoFeeder']['enabled']
  if lod:
   for p in ps:bpy.data.objects.remove(p,do_unlink=True)
   bpy.data.collections.remove(temp)
  count=len(o.data.polygons);limit=m['budgetTriangles']['LOD0Limit' if lod==0 else 'LOD'+str(lod)+'Target']
  assert count+14<=limit,(name,count,limit)
  assert all(p.area>1e-10 for p in o.data.polygons),(name,'degenerate')
  path=ART/'Exports'/(name+'.fbx');export_obj(o,path)
  lo=[min(v.co[i] for v in o.data.vertices) for i in range(3)];hi=[max(v.co[i] for v in o.data.vertices) for i in range(3)]
  audit.append(dict(model=key,lod=lod,kind='opaque',name=name,triangles=count,vertices=len(o.data.vertices),boundsApprovedXYZ=[[lo[0],lo[2],lo[1]],[hi[0],hi[2],hi[1]]],materials=[a.name for a in o.data.materials],export='Exports/'+path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
  o.hide_render=lod!=0;o.hide_set(lod!=0)
  print('POND_MESH',name,count,'limit',limit,flush=True)
 wp=next(p for p in m['parts'] if p['group']=='water');b=Builder();b.face(wp['verticesXYZ'],'water',int(wp['id'].split('_')[-1]),(0,1,0))
 wo=b.object('FishPond_A_'+key+'_Water',exports,is_water=True)
 bm=bmesh.new();bm.from_mesh(wo.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(wo.data);bm.free();wo.data.update()
 wo['WaterLevelApprovedY']=m['waterLevelY'];wo['ReuseAcrossLODs']=True
 path=ART/'Exports'/(wo.name+'.fbx');export_obj(wo,path)
 audit.append(dict(model=key,lod=None,kind='water',name=wo.name,triangles=len(wo.data.polygons),vertices=len(wo.data.vertices),materials=[a.name for a in wo.data.materials],export='Exports/'+path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 st=m['stations'];marker(markers,'Site origin',[0,0,0]);marker(markers,'Pond UI',st['pondInteractionXYZ']);marker(markers,'Ground approach',st['pondApproachXYZ'])
 marker(markers,'Maker station',st['maker']['centerXYZ']);marker(markers,'Maker approach',st['maker']['interactionXYZ'])
 marker(markers,'Feed barrel',st['storage']['centerXYZ']);marker(markers,'Storage approach',st['storage']['interactionXYZ'])
 marker(markers,'Fish swim center',[1.5,.44,0],SwimBandApprovedY=m['fishSwimBandY'],MaximumVisualFish=m['maximumVisibleFish'])
 if st['autoFeeder']['enabled']:
  marker(markers,'Auto feeder station',st['autoFeeder']['centerXYZ'])
  marker(markers,'Pellet emitter',st['autoFeeder']['emitterXYZ'],DirectionApprovedXYZ=[0,0,1])
 for p in m['parts']:
  if p['group']=='sketchWaterFx':marker(markers,'FX '+p['name'],p['verticesXYZ'][0],PreviewRibbonExcluded=True)
 editable.hide_render=True;editable.hide_viewport=True;markers.hide_render=True;markers.hide_viewport=True
 cam=studio(scene);aim(cam,m,'FrontRight');scenes[key]=scene;masters[key]=lods;waters[key]=wo
 if '--skip-renders' not in sys.argv:
  for side in ['FrontRight','RearLeft']:
   aim(cam,m,side);scene.render.filepath=str(ART/'Previews'/f'FishPond_A_{key}_{side}.png');bpy.ops.render.render(write_still=True)
  aim(cam,m,'FrontRight')

comparison=bpy.data.scenes.new('Fish Pond A Levels 1-3');setup(comparison);c=coll('Shared actual production meshes',comparison)
for lv,m in enumerate(S['models']):
 for kind,src in [('Opaque',masters[m['id']][0]),('Water',waters[m['id']])]:
  o=bpy.data.objects.new(m['id']+' '+kind,src.data);c.objects.link(o);o.location=((lv-1)*21,0,0);o['Capacity']=m['capacity']
cam=studio(comparison);cam.data.ortho_scale=65;center=Vector((0,0,.65));cam.location=(20,-50,53);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
comparison.render.resolution_x=2400;comparison.render.resolution_y=1000
if '--skip-renders' not in sys.argv:
 comparison.render.filepath=str(ART/'Previews/FishPond_A_Levels_1_to_3.png');bpy.ops.render.render(write_still=True,scene=comparison.name)
bpy.context.window.scene=scenes['Lv3'];bpy.ops.object.select_all(action='DESELECT');active=masters['Lv3'][0];active.select_set(True);bpy.context.view_layer.objects.active=active
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
for me in list(bpy.data.meshes):
 if me.users==0:bpy.data.meshes.remove(me)
report=dict(status='MODEL_BUILT_EXPORTED_NOT_APPLIED_TO_UNITY',blenderVersion=bpy.app.version_string,models=audit,
 approvedSpecificationSha256=hashlib.sha256(SPEC_PATH.read_bytes()).hexdigest(),newRuntimeTextures=0,
 atlasSha256=S['materials']['atlasSha256'],runtimeMaterials=['TFP_Atlas_1A','TFP_Water_1A'],
 water='Separate 14 triangle horizontal plane per level, shared by all LODs. Blender preview only; Unity pack shader not applied.',
 coordinateSystem='Blender (x,y,z)=(approved x,z,y); front -Y, correct winding after reflection. Verify Unity FBX import before placement.',
 staticFish=False,bakedWaterFX=False,unityAssetsModified=False)
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/FishPond_A_Masters.blend'))
print('POND_BUILD_COMPLETE',flush=True)
