"""Build the approved Coop A in a separate Blender process; no Unity asset changes."""
from pathlib import Path
import sys,math,json,hashlib,random,itertools
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
sys.path.insert(0,str(ART/'Scripts'))
import mesh_helpers as H
import bpy,bmesh
from mathutils import Vector
S=H.SPEC;M=S['master'];B=H.MeshBuilder
scene=H.scene;source=H.source;exports=H.exports
scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.cycles.samples=24
scene.render.use_file_extension=True
scene.render.image_settings.color_mode='RGBA'
scene['ApprovedSpecification']=S['revision'];scene['MasterLevel']=3
scene['Scope']='Approved Blender exterior; Unity integration not performed in this task'

def wall_panels(lod,col):
 objects=[]
 for view in ['front','rear','right','left']:
  b=B();front=view in ['front','rear'];half=3 if front else 3.5;sign=-1 if view in ['front','left'] else 1
  b.expected_normal=(0,0,sign) if front else (sign,0,0)
  def p(u,y,inset=0):return(u,y,sign*(3.5+inset)) if front else (sign*(3+inset),y,u)
  if lod==2:
   b.quad(p(-half,.45,-.002),p(half,.45,-.002),p(half,3.8,-.002),p(-half,3.8,-.002),'wall')
   if front:b.face([p(-3,3.8,-.002),p(3,3.8,-.002),p(0,5.6,-.002)],'wall')
  else:
   b.quad(p(-half,.45,-.008),p(half,.45,-.008),p(half,3.8,-.008),p(-half,3.8,-.008),'wall_side')
   if front:b.face([p(-3,3.8,-.008),p(3,3.8,-.008),p(0,5.6,-.008)],'wall_side')
   for k in range(round(half*2/.25)):
    u0=-half+k*.25+.004;u1=u0+.242
    h0=3.8+(1.8*(1-abs(u0)/3) if front else 0);h1=3.8+(1.8*(1-abs(u1)/3) if front else 0)
    b.quad(p(u0,.45,-.002),p(u1,.45,-.002),p(u1,h1,-.002),p(u0,h0,-.002),'wall',((k*17)%7-3)/3)
  obj=H.mesh_object('WallBoards_'+view,col,b);obj['BoardPitchMetres']=.25;objects.append(obj)
 return objects

def roof_planes(lod,col):
 objects=[]
 for side in [-1,1]:
  b=B();name='RoofRight' if side>0 else 'RoofLeft'
  def p(x,z,lip=0):return(side*x,5.6-.6*x+lip,z)
  delta=(side*.06*.6/math.sqrt(1.36),.06/math.sqrt(1.36),0)
  lip=.003 if lod==2 else 0
  top=[p(0,-3.9,lip),p(3.4,-3.9,lip),p(3.4,3.9,lip),p(0,3.9,lip)]
  bottom=[tuple(v[i]-delta[i] for i in range(3)) for v in top]
  b.face(bottom[::-1],'roof_dark',normal=(-side*.6,-1,0))
  for i,n in zip([0,1,2],[(0,0,-1),(side,0,0),(0,0,1)]):b.face([top[i],bottom[i],bottom[(i+1)%4],top[(i+1)%4]],'roof',normal=n)
  b.face(top,'roof_dark' if lod<2 else 'roof',normal=(side*.6,1,0))
  b.expected_normal=(side*.6,1,0)
  rows,cols=(8,12) if lod==0 else (5,6) if lod==1 else (0,0)
  for row in range(rows):
   x0=3.4*row/rows;x1=3.4*(row+1)/rows
   for c in range(cols):
    z0=-3.9+7.8*c/cols;z1=-3.9+7.8*(c+1)/cols
    if c:z0+=.009
    if c<cols-1:z1-=.009
    b.quad(p(x0+.003,z0,.003),p(x1-.004,z0,.02),p(x1-.004,z1,.02),p(x0+.003,z1,.003),'roof',((c+row*3)%5-2)/2)
   b.quad(p(x1-.004,-3.9,.02),p(x1-.004,-3.9,.002),p(x1-.004,3.9,.002),p(x1-.004,3.9,.02),'roof')
  obj=H.mesh_object(name,col,b);obj['RowsPerSlope']=rows;obj['ColumnsAlongDepth']=cols;objects.append(obj)
 return objects

def foundation(lod,col):
 b=B();b.box((0,.225,0),(5.96 if lod<2 else 6,.45,6.96 if lod<2 else 7),'stone')
 if lod<2:
  for view in ['front','rear','right','left']:
   front=view in ['front','rear'];half=3 if front else 3.5;sign=-1 if view in ['front','left'] else 1
   b.expected_normal=(0,0,sign) if front else(sign,0,0)
   def p(u,y,inset=0):return(u,y,sign*(3.5-inset)) if front else(sign*(3-inset),y,u)
   # Faceted stone faces stay inside the approved body footprint.
   for row in range(2):
    for k in range(math.ceil(half*2/.8)):
     a=-half+k*.8+(row%2)*.4;c=min(half,a+.77)
     if a>=half:continue
     y0=row*.225+.008;y1=(row+1)*.225-.008;a+=.005;c-=.005
     color='stone_light' if k%3==0 else 'stone'
     if lod==1:b.quad(p(a,y0),p(c,y0),p(c,y1),p(a,y1),color);continue
     bevel=min(.025,(c-a)*.12)
     outer=[(a+bevel,y0),(c-bevel,y0),(c,y0+bevel),(c,y1-bevel),(c-bevel,y1),(a+bevel,y1),(a,y1-bevel),(a,y0+bevel)]
     inner=[(a+bevel,y0+bevel),(c-bevel,y0+bevel),(c-bevel,y1-bevel),(a+bevel,y1-bevel)]
     b.face([p(x,y) for x,y in inner],color)
     for i,j,k0,l in [(0,1,1,0),(2,3,2,1),(4,5,3,2),(6,7,0,3)]:b.face([p(*outer[i],.018),p(*outer[j],.018),p(*inner[k0]),p(*inner[l])],color)
     for i,k0 in [(1,1),(3,2),(5,3),(7,0)]:b.face([p(*outer[i],.018),p(*outer[(i+1)%8],.018),p(*inner[k0])],color)
 return [H.mesh_object('Foundation_TwoStoneCourses',col,b)]

def expected_normal(part):
 return {'front':(0,0,-1),'rear':(0,0,1),'right':(1,0,0),'left':(-1,0,0)}.get(part.get('facade'),(0,1,0))

def planar_box(part,col):
 b=B();cx,cy,cz=part['centerXYZ'];sx,sy,sz=part['sizeXYZ'];f=part.get('facade')
 if f in ['right','left']:
  sign=1 if f=='right' else -1;x=cx+sign*sx/2
  v=[(x,cy-sy/2,cz-sz/2),(x,cy+sy/2,cz-sz/2),(x,cy+sy/2,cz+sz/2),(x,cy-sy/2,cz+sz/2)]
 else:
  sign=1 if f=='rear' else -1;z=cz+sign*sz/2
  v=[(cx-sx/2,cy-sy/2,z),(cx+sx/2,cy-sy/2,z),(cx+sx/2,cy+sy/2,z),(cx-sx/2,cy+sy/2,z)]
 # Distant faces need centimetre separation for Unity's perspective depth buffer,
 # while retaining the approved frame silhouette and exterior envelope.
 if any(token in part['name'] for token in ['_Rail','DoorRail_','HatchRail_','VentRail_']):
  n=Vector(expected_normal(part));v=[tuple(Vector(p)+n*.01) for p in v]
 b.face(v,part['material'],material_index=1 if part['material']=='glow' else 0,normal=expected_normal(part))
 return H.mesh_object(part['name'],col,b)

def planar_beam(part,col):
 a=Vector(part['fromXYZ']);c=Vector(part['toXYZ']);delta=(c-a).normalized();depth=Vector((0,0,1)) if abs(delta.z)<1e-8 else Vector((1,0,0));width=delta.cross(depth)
 normal=Vector(expected_normal(part));sign=1 if depth.dot(normal)>=0 else -1
 v=[q+width*sw*part['width']/2+depth*sign*part['depth']/2 for q,sw in [(a,-1),(c,-1),(c,1),(a,1)]]
 # A 1.5 cm lift avoids depth conflicts at Unity's far LOD distances.
 # LOD0 uses an exact solid Boolean union instead.
 if part['name']=='DoorDiagonal':v=[q+normal*.015 for q in v]
 b=B();b.face([tuple(x) for x in v],part['material'],normal=tuple(normal));return H.mesh_object(part['name'],col,b)

def lantern(part,col):
 b=B();cx,cy,cz=part['centerXYZ'];sx,sy,sz=part['sizeXYZ'];rx=sx/2;rz=sz/2
 def ring(y,scale):return[(cx+rx*scale*math.cos(i*math.tau/8),cy+y,cz+rz*scale*math.sin(i*math.tau/8)) for i in range(8)]
 low=ring(-sy/2,.50);base=ring(-sy*.35,1);shoulder=ring(sy*.32,1);top=ring(sy/2,.45)
 b.face(low[::-1],'metal',normal=(0,-1,0));b.face(base,'metal',normal=(0,1,0))
 b.face(shoulder[::-1],'metal',normal=(0,-1,0));b.face(top,'metal',normal=(0,1,0))
 for a,c in [(low,base),(shoulder,top)]:
  for i in range(8):b.face([a[i],a[(i+1)%8],c[(i+1)%8],c[i]],'metal')
 for x,z in [(-.075,-.060),(.075,-.060),(-.075,.060),(.075,.060)]:b.box((cx+x,cy-.005,cz+z),(.012,sy*.67,.012),'metal')
 obj=H.mesh_object(part['name'],col,b);H.reset_uv(obj,'metal');return obj

def components(lod,col):
 result=[]
 for part in S['parts']:
  name=part['name'];kind=part['type']
  if name in ['Foundation','WallBody'] or name.startswith(('Gable_','Roof_','StoneBlock_','BoardLine_','Shingle')):continue
  if lod==2 and (any(token in name for token in ['Hinge','Handle','Wire','RampCleat']) or name.startswith(('Brace_','NestSupport_','VentSlat_')) or name=='LanternBracket'):continue
  target=(H.metal if part['material'] in ['metal','glow','wire'] else H.joinery) if lod==0 else col
  if kind=='surface':
   if 'Wire' in name and lod==1 and name.endswith(('_1','_3')):continue
   b=B();b.face(part['verticesXYZ'],part['material'],normal=expected_normal(part));obj=H.mesh_object(name,target,b)
  elif kind=='beam':
   # Keep depth on the door brace so the toon shader can shade its edge.
   # A flat face with the same palette colour disappears into the door panel.
   obj=H.beam_object(part,target,lod) if lod==0 or name=='DoorDiagonal' else planar_beam(part,target)
  elif name=='LanternFrame' and lod==0:obj=lantern(part,target)
  elif lod==2 and not name.startswith(('Post_','Eave_','RidgeCap','NestBox','Step')):obj=planar_box(part,target)
  elif lod==1 and (any(token in name for token in ['Hinge','Handle','Inset']) or name in ['LanternBracket','LanternCore']):obj=planar_box(part,target)
  else:
   b=B();b.box(part['centerXYZ'],part['sizeXYZ'],part['material'])
   # One segment bevel on large readable frame parts, matching Barn A.
   trim=name.startswith(('Post_','DoorJamb_','DoorRail_','HatchJamb_','HatchRail_')) or any(t in name for t in ['_Jamb','_Rail'])
   obj=H.mesh_object(name,target,b,.02 if lod==0 and trim else 0);H.reset_uv(obj,part['material'])
   if part['material']=='glow':
    for poly in obj.data.polygons:poly.material_index=1
  obj['ApprovedPart']=name;obj['ApprovedSource']=S['revision'];result.append(obj)
 return result

def union_frames(objects,lod,col):
 if lod==2:return objects
 groups=[('HumanDoorFrame',lambda n:n.startswith(('DoorJamb_','DoorRail_'))),('DoorBraceJoint',lambda n:n in ['DoorDiagonal','DoorCentreRail']),('ChickenDoorFrame',lambda n:n.startswith(('HatchJamb_','HatchRail_'))),('RearVentFrame',lambda n:n.startswith(('VentJamb_','VentRail_')))]
 for facade in ['FrontWindow','RightFrontWindow','RightRearWindow','LeftWindow']:
  groups.append((facade+'_Frame',lambda n,f=facade:n.startswith(f+'_Jamb') or n.startswith(f+'_Rail')))
 result=list(objects)
 for name,match in groups:
  pieces=[o for o in objects if match(o.name)]
  if len(pieces)<2:continue
  operands=H.collection('Joint operands - '+name,col)
  for obj in pieces[1:]:operands.objects.link(obj)
  visible=bpy.data.objects.new(name+'_CleanJoint',pieces[0].data.copy());(H.joinery if lod==0 else col).objects.link(visible)
  visible['EditableOperands']=[o.name for o in pieces];visible['JointMethod']='Live exact Boolean union; editable sources retained'
  mod=visible.modifiers.new('Clean timber joint','BOOLEAN');mod.operation='UNION';mod.solver='EXACT';mod.operand_type='COLLECTION';mod.collection=operands
  dissolve=visible.modifiers.new('Dissolve coplanar edges','DECIMATE');dissolve.decimate_type='DISSOLVE';dissolve.angle_limit=.001;dissolve.delimit={'UV','MATERIAL'}
  for obj in pieces:obj.hide_render=True;obj.hide_set(True)
  result.append(visible)
 return result

def build(lod,col):
 objs=wall_panels(lod,H.walls if lod==0 else col)+roof_planes(lod,H.roof if lod==0 else col)+foundation(lod,H.foundation if lod==0 else col)+components(lod,col)
 return union_frames(objs,lod,col)

def join_copy(objects,name):
 copies=[];exports.hide_viewport=False;exports.hide_render=False
 bpy.context.view_layer.update();depsgraph=bpy.context.evaluated_depsgraph_get()
 for obj in objects:
  if obj.hide_render:continue
  evaluated=obj.evaluated_get(depsgraph)
  clone=bpy.data.objects.new(name+'_piece',bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=depsgraph));exports.objects.link(clone);copies.append(clone)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in copies:obj.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
 bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
 obj.location=(0,0,0);obj.rotation_euler=(0,0,0);obj.scale=(1,1,1);obj['GroundCentrePivot']=True;obj['ApprovedRevision']=S['revision']
 return obj

source_objects=build(0,source);lod_meshes=[join_copy(source_objects,'Coop_A_Master_LOD0')]
for lod in [1,2]:
 temp=H.collection('TEMP LOD '+str(lod));pieces=build(lod,temp);lod_meshes.append(join_copy(pieces,'Coop_A_Master_LOD'+str(lod)))
 for o in pieces:bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(temp)

def bounds(obj):
 points=[obj.matrix_world@v.co for v in obj.data.vertices]
 return [[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]]

audit=[];limits=[4200,1600,420]
rects=[p['uvSafeRectBottomLeft'] for p in H.PAL.values()]
for i,obj in enumerate(lod_meshes):
 count=len(obj.data.polygons);lo,hi=bounds(obj)
 assert count<=limits[i],(obj.name,count,limits[i])
 for j,k in enumerate([0,2,1]):assert abs(lo[j]-M['visualBoundsMinXYZ'][k])<1e-5 and abs(hi[j]-M['visualBoundsMaxXYZ'][k])<1e-5,(i,lo,hi)
 assert len(obj.data.materials)==2
 for v in obj.data.uv_layers.active.data:
  u,w=v.uv;assert any(a-1e-7<=u<=c+1e-7 and b-1e-7<=w<=d+1e-7 for a,b,c,d in rects)
 assert all(p.area>1e-10 for p in obj.data.polygons)
 audit.append(dict(name=obj.name,triangles=count,vertices=len(obj.data.vertices),boundsBlenderXYZ=[lo,hi],materials=[m.name for m in obj.data.materials],identityTransforms=True,groundCentreOrigin=[0,0,0]))
 obj.hide_render=True;obj.hide_set(True)
exports.hide_render=True;exports.hide_viewport=True

for name,location in [('GroundCentre',(0,0,0)),('EntranceMarker',(-1.25,0,-4.65))]:
 obj=bpy.data.objects.new(name,None);H.markers.objects.link(obj);obj.location=H.convert(location);obj.empty_display_type='PLAIN_AXES';obj.empty_display_size=.3;obj['UnityXYZ']=list(location)
H.markers.hide_viewport=True

world=bpy.data.worlds.new('Coop studio ambient');scene.world=world;world.use_nodes=True
world.node_tree.nodes.get('Background').inputs[0].default_value=(.78,.82,.88,1);world.node_tree.nodes.get('Background').inputs[1].default_value=.7
def move_to(obj,col):
 for c in list(obj.users_collection):c.objects.unlink(obj)
 col.objects.link(obj)
def light(name,pos,power,size):
 bpy.ops.object.light_add(type='AREA',location=pos);obj=bpy.context.object;obj.name=name;move_to(obj,H.studio);obj.data.energy=power;obj.data.shape='DISK';obj.data.size=size;obj.rotation_euler=(Vector((0,0,2.5))-obj.location).to_track_quat('-Z','Y').to_euler()
light('Preview key - soft daylight',(-8,-10,14),1800,8);light('Preview fill',(9,-1,10),950,7);light('Preview rim',(0,8,12),1200,6)
floor_mat=bpy.data.materials.new('PREVIEW ONLY - neutral floor');floor_mat.use_nodes=True;bsdf=floor_mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(.78,.75,.66,1);bsdf.inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object;floor.name='Preview floor - never export';move_to(floor,H.studio);floor.data.materials.append(floor_mat)
bpy.ops.object.camera_add(location=(12,-16,12));camera=bpy.context.object;camera.name='Coop Preview Camera';move_to(camera,H.studio);camera.data.type='ORTHO';scene.camera=camera
def aim(pos,target=(.20,-.20,2.5),scale=14):
 camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
aim((12,-16,12))

levels=bpy.data.scenes.new('Coop A - Levels 1 to 5 - shared mesh');levels.world=world;levels.unit_settings.system='METRIC';levels.unit_settings.scale_length=1
levels.render.engine='CYCLES';levels.cycles.samples=24;levels.cycles.use_denoising=True;levels.view_settings.view_transform='Standard';levels.view_settings.look='None'
levels.render.resolution_x=2200;levels.render.resolution_y=850;levels.render.resolution_percentage=100;levels.render.image_settings.file_format='PNG'
compare=bpy.data.collections.new('Level instances - uniform size only');levels.collection.children.link(compare)
label_mat=bpy.data.materials.new('PREVIEW ONLY - level labels');label_mat.use_nodes=True;label_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.08,.055,.035,1)
for lv in S['levels']:
 root=bpy.data.objects.new('Coop_A_Lv'+str(lv['level']),None);compare.objects.link(root);root.location=((lv['level']-3)*11.5,0,0);s=lv['uniformScale'];root.scale=(s,s,s)
 root['VisualWidthDepthHeightMetres']=lv['visualWidthDepthHeight'];root['BodyWidthDepthMetres']=lv['bodyWidthDepth']
 obj=bpy.data.objects.new('Lv'+str(lv['level'])+' - shared master mesh',lod_meshes[0].data);compare.objects.link(obj);obj.parent=root
 marker=bpy.data.objects.new('Lv'+str(lv['level'])+' Entrance',None);compare.objects.link(marker);marker.parent=root;marker.location=(-1.25,-4.65,0);marker.hide_render=True
 text=bpy.data.curves.new('Lv'+str(lv['level'])+' label','FONT');text.body=f"Lv{lv['level']}   {6*s:g} x {7*s:g} m";text.align_x='CENTER';text.size=.65;text.materials.append(label_mat)
 label=bpy.data.objects.new(text.name,text);compare.objects.link(label);label.location=(root.location.x,-6.8,.01)
for obj in H.studio.objects:
 if obj.type!='CAMERA':levels.collection.objects.link(obj)
cam_data=camera.data.copy();level_cam=bpy.data.objects.new('Level comparison camera',cam_data);levels.collection.objects.link(level_cam);level_cam.location=(0,-42,30);level_cam.rotation_euler=(Vector((0,0,2))-level_cam.location).to_track_quat('-Z','Y').to_euler();level_cam.data.ortho_scale=59;levels.camera=level_cam

bpy.context.window.scene=scene;bpy.ops.object.select_all(action='DESELECT')
for o in source_objects:
 if not o.hide_render:o.select_set(True)
bpy.context.view_layer.objects.active=source_objects[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False;area.spaces.active.show_region_ui=False
report=dict(approvedRevision=S['revision'],blenderVersion=bpy.app.version_string,status='MODEL_BUILT_NOT_APPLIED_TO_UNITY',masterBodyWidthDepthMetres=[6,7],masterVisualWidthDepthHeightMetres=[7.35,8.7,5.74],lodMeshes=audit,levels=S['levels'],levelMeshesShared=True,materials=dict(opaque='TFP_Atlas_1A.mat',emission='TFP_Atlas_Lights_1A.mat',atlasGUID=S['materials']['atlasGuid'],newRuntimeTextures=0,blenderPreviewImagePacked=True),editableSourceObjects=len(source_objects),unityAssetsModified=False)
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
scene.render.filepath=str(ART/'Previews/Coop_A_FrontRight.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Coop_A_Master.blend'))
if '--skip-renders' not in sys.argv:
 for name,pos,target,scale in [('FrontRight',(12,-16,12),(.2,-.2,2.5),14),('RearLeft',(-12,16,12),(.2,-.2,2.5),14),('Front',(0,-20,3),(0,0,2.9),10.4),('Right',(20,0,3),(0,-.4,2.9),11.5),('Rear',(0,20,3),(0,0,2.9),10.4),('Left',(-20,0,3),(0,-.4,2.9),11.5)]:
  aim(pos,target,scale);scene.render.filepath=str(ART/'Previews'/f'Coop_A_{name}.png');bpy.ops.render.render(write_still=True)
 levels.render.filepath=str(ART/'Previews/Coop_A_Levels_1_to_5.png');bpy.ops.render.render(write_still=True,scene=levels.name)
aim((12,-16,12));scene.render.filepath=str(ART/'Previews/Coop_A_FrontRight.png')
for mesh in list(bpy.data.meshes):
 if mesh.users==0:bpy.data.meshes.remove(mesh)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Coop_A_Master.blend'))
print('COOP_BUILD_COMPLETE '+json.dumps(audit))
