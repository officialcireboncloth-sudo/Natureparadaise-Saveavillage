"""Unretouched source renders with neutral studio lighting and adaptive framing."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g10_config import *
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera;roots=[o for o in bpy.data.objects if o.get('Phase')];out=SOURCE/'Previews';out.mkdir(parents=True,exist_ok=True);scene.cycles.samples=12
selected=[sys.argv[sys.argv.index('--only')+1]] if '--only' in sys.argv else list(PHASES)
for root in roots:
 name='NP_'+root['Plant']+'_'+root['Phase'];s=json.loads((MODELS/(name+'.json')).read_text());me=bpy.data.meshes.new(name+' runtime preview')
 me.from_pydata([(p[0],p[2],p[1]) for p in s['vertices']],[],[(a,c,b) for a,b,c in zip(*[iter(s['submeshes'][0])]*3)]);me.update()
 uv=me.uv_layers.new(name='AtlasUV')
 for face in me.polygons:
  face.use_smooth=True
  for li in face.loop_indices:uv.data[li].uv=s['uv'][me.loops[li].vertex_index]
 me.normals_split_custom_set_from_vertices([(n[0],n[2],n[1]) for n in s['normals']]);me.materials.append(bpy.data.materials[ATLAS])
 ob=bpy.data.objects.new(name+' runtime preview',me);scene.collection.objects.link(ob);ob.parent=root;ob['RuntimeGeometry']=True
def visible(root,show):
 for ob in root.children_recursive:
  if ob.type=='MESH':ob.hide_render=not(show and ob.get('RuntimeGeometry'))
def reset():
 for r in roots:visible(r,False)
def aim(target,angle,elevation,scale):
 a,e=map(math.radians,[angle,elevation]);target=Vector(target);camera.location=target+Vector((4*math.cos(e)*math.sin(a),-4*math.cos(e)*math.cos(a),4*math.sin(e)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
def frame(w,h,angle=0,elevation=24):
 bpy.context.view_layer.update();points=[ob.matrix_world@Vector(p) for r in roots for ob in r.children_recursive if ob.type=='MESH' and not ob.hide_render for p in ob.bound_box]
 aim((0,0,.5),angle,elevation,1);rot=camera.rotation_euler.to_matrix();right,up,forward=rot.col[0],rot.col[1],-rot.col[2];xx,yy,zz=([p.dot(v) for p in points] for v in [right,up,forward]);center=right*((min(xx)+max(xx))/2)+up*((min(yy)+max(yy))/2)+forward*((min(zz)+max(zz))/2)
 width,height=max(xx)-min(xx),max(yy)-min(yy);aspect=w/h;scale=max(width,height*aspect) if w>=h else max(height,width/aspect);aim(center,angle,elevation,scale*1.16)
def render(name,w,h):
 scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/name);bpy.ops.render.render(write_still=True);print('G10_RENDER',name,flush=True)
reset()
for kind in selected:
 root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0);visible(root,True);frame(700,850);render('G10_'+kind+'_Ready.png',700,850);visible(root,False)
if '--quick' in sys.argv:sys.exit(0)
reset()
for i,kind in enumerate(PHASES):root=bpy.data.objects[kind+'_Ready'];root.location=((i-1.5)*.80,0,0);visible(root,True)
frame(2000,1050);render('G10_Closeup.png',2000,1050)
for kind,stages in PHASES.items():
 if kind not in selected:continue
 reset()
 for i,stage in enumerate(stages):root=bpy.data.objects[kind+'_'+stage];root.location=((i-2.5)*.86,0,0);visible(root,True)
 frame(2100,650,elevation=25);render('G10_'+kind+'_Growth.png',2100,650)
for kind in selected:
 reset();root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0)
 for ob in root.children_recursive:
  if ob.type=='MESH':ob.hide_render=not(ob.get('FlowerKind')==kind and ob.get('FlowerIndex')==0)
 frame(700,700,elevation=28);render('G10_'+kind+'_FlowerDetail.png',700,700)
reset()
for i,kind in enumerate(PHASES):root=bpy.data.objects[kind+'_Harvest'];root.location=((i-1.5)*.70,0,0);visible(root,True)
frame(1800,700,elevation=52);render('G10_HarvestStudy.png',1800,700)
for kind in selected:
 reset();root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0);visible(root,True)
 for title,angle,elev in [('Side',90,24),('Rear',180,24),('Top',0,83),('Field',0,52)]:frame(540,620,angle,elev);render('G10_'+kind+'_'+title+'.png',540,620)
print('G10_RENDERS_PASS',flush=True)
