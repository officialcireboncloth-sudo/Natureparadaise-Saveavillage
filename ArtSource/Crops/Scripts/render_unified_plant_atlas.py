"""Before/after renders of actual mesh exports, plus a one-material 26-crop Blender gallery."""
import bpy,sys,math,zipfile
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from unified_plant_atlas import *
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;out=WORK/'Review';integration=load(SOURCE/'Integration/Manifest.json');chosen=['zucchini','cucumber','spinach','wheat','rose','sunflower','tulip','onion'];original=zipfile.ZipFile(BACKUP)
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=5;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.world=bpy.data.worlds.new('Neutral preview');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5

def material(page,unified=False):
 m=bpy.data.materials.new(page);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;bs=n.get('Principled BSDF');bs.inputs['Specular IOR Level'].default_value=.28
 textures={}
 for suffix in MAPS:
  im=bpy.data.images.load(str((TEXTURES/(page+'_'+suffix+'.png')) if unified else source_texture(page,suffix)),check_existing=True)
  if suffix!='BaseColor':im.colorspace_settings.name='Non-Color'
  tex=n.new('ShaderNodeTexImage');tex.image=im;textures[suffix]=tex
 sep=n.new('ShaderNodeSeparateColor');l.new(textures['SurfaceMask'].outputs['Color'],sep.inputs[0]);l.new(sep.outputs['Red'],bs.inputs['Metallic'])
 inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(textures['SurfaceMask'].outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],bs.inputs['Roughness'])
 normal=n.new('ShaderNodeNormalMap');l.new(textures['Normal'].outputs['Color'],normal.inputs['Color']);l.new(normal.outputs['Normal'],bs.inputs['Normal'])
 gray=n.new('ShaderNodeRGBToBW');l.new(textures['BaseColor'].outputs['Color'],gray.inputs[0]);sub=n.new('ShaderNodeVectorMath');sub.operation='SUBTRACT';l.new(textures['BaseColor'].outputs['Color'],sub.inputs[0]);l.new(gray.outputs[0],sub.inputs[1]);scale=n.new('ShaderNodeVectorMath');scale.operation='SCALE';l.new(sub.outputs[0],scale.inputs[0]);add=n.new('ShaderNodeVectorMath');add.operation='ADD';l.new(gray.outputs[0],add.inputs[0]);l.new(scale.outputs[0],add.inputs[1]);l.new(add.outputs[0],bs.inputs['Base Color'])
 if unified:
  l.new(sep.outputs['Green'],normal.inputs['Strength']);twice=n.new('ShaderNodeMath');twice.operation='MULTIPLY';twice.inputs[1].default_value=2;l.new(sep.outputs['Blue'],twice.inputs[0]);l.new(twice.outputs[0],scale.inputs['Scale'])
 else:
  settings=source_settings(page);normal.inputs['Strength'].default_value=settings['_BumpScale'];scale.inputs['Scale'].default_value=settings['_Saturation']
 return m

shared=material(NAME,True);sourceMaterials={};objects=[];labels=[];pairs=[]
for row in integration['plants']:
 phase=row['stages'][-1];path=phase['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json');spec=load(ROOT/path)
 if path in original.namelist():old=json.loads(original.read(path))
 else:
  proof=SOURCE/row['group']/('Review/Raspberry/ExportBaseline.json' if row['key']=='raspberry' else 'Review/ExportBaseline.json')
  old=load(ROOT/load(proof)['models'][Path(path).stem]['sourceSpec'])
 pair=[]
 for before,s in [(False,spec),(True,old)]:
  mesh=bpy.data.meshes.new(('Before ' if before else '')+row['species']);mesh.from_pydata([(p[0],p[2],p[1]) for p in s['vertices']],[],[(a,c,b) for a,b,c in zip(*[iter(s['submeshes'][0])]*3)]);mesh.update();uv=mesh.uv_layers.new(name='AtlasUV')
  for polygon in mesh.polygons:
   polygon.use_smooth=True
   for li in polygon.loop_indices:uv.data[li].uv=s['uv'][mesh.loops[li].vertex_index]
  mesh.normals_split_custom_set_from_vertices([(n[0],n[2],n[1]) for n in s['normals']]);page=s['materials'][0]
  if before and page not in sourceMaterials:sourceMaterials[page]=material(page)
  mesh.materials.append(sourceMaterials[page] if before else shared);ob=bpy.data.objects.new(('Before ' if before else '')+row['species'],mesh);scene.collection.objects.link(ob);ob.scale=(phase['scale'],)*3;ob['CropId']=row['cropId'];ob['RuntimeMaterial']=NAME;ob['PreviewBefore']=before;objects.append(ob);pair.append(ob)
 pairs.append((row,pair))

floorMat=bpy.data.materials.new('Neutral floor');floorMat.diffuse_color=(.91,.90,.86,1);bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='STUDIO floor';floor.data.materials.append(floorMat)
for name,location,energy,size in [('Key',(-3,-4,8),1100,6),('Fill',(6,3,5),850,5)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size;ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=location;ob.rotation_euler=(Vector((2,1,0))-ob.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Atlas review camera');camera=bpy.data.objects.new('Atlas review camera',data);scene.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO'
def aim(target,scale):
 target=Vector(target);camera.location=target+Vector((0,-8,6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
def frame_visible():
 bpy.context.view_layer.update();points=[o.matrix_world@Vector(p) for o in objects+labels if not o.hide_render and o.type in ['MESH','FONT'] for p in o.bound_box];rot=camera.rotation_euler.to_matrix();vectors=[rot.col[0],rot.col[1],-rot.col[2]];bounds=[(min(p.dot(v) for p in points),max(p.dot(v) for p in points)) for v in vectors];center=sum((v*((a+b)*.5) for v,(a,b) in zip(vectors,bounds)),Vector());width=bounds[0][1]-bounds[0][0];height=bounds[1][1]-bounds[1][0];aim(center,max(width,height*1.5)*1.12)
labelMat=bpy.data.materials.new('Preview labels');labelMat.diffuse_color=(.055,.15,.055,1)
def label(text,location,size=.10):
 curve=bpy.data.curves.new(text,'FONT');curve.body=text;curve.size=size;curve.align_x='CENTER';ob=bpy.data.objects.new(text,curve);scene.collection.objects.link(ob);ob.location=location;ob.rotation_euler=camera.rotation_euler;curve.materials.append(labelMat);labels.append(ob)
aim((2.25,1.3,.42),6.4)
for i,key in enumerate(chosen):
 row,pair=next((row,pair) for row,pair in pairs if row['key']==key);x=(i%4)*1.5;y=(i//4)*2.7
 for ob in pair:ob.location=(x,y,-min(v.co.z for v in ob.data.vertices)*ob.scale.z+.002)
 label(row['species'],(x,y-.65,.035),.105)
for before in ([] if '--gallery-only' in sys.argv else [True,False]):
 for ob in objects:ob.hide_render=ob['CropId'].split('.',1)[1] not in chosen or bool(ob['PreviewBefore'])!=before
 frame_visible()
 scene.render.resolution_x=1800;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.render.filepath=str(out/('Before.png' if before else 'After.png'));bpy.ops.render.render(write_still=True);print('UNIFIED_RENDER','Before' if before else 'After',flush=True)

# Persist only runtime meshes in the gallery; original authoring masters remain untouched.
for ob in [o for o in objects if o['PreviewBefore']]:bpy.data.objects.remove(ob,do_unlink=True)
for ob in labels:bpy.data.objects.remove(ob,do_unlink=True)
aim((4.5,3.6,.45),11.3)
for i,(row,pair) in enumerate(pairs):
 ob=pair[0];ob.hide_render=False;ob.location=(i%7*1.5,i//7*2.5,-min(v.co.z for v in ob.data.vertices)*ob.scale.z+.002);label(row['species'],(ob.location.x,ob.location.y-.38,.035),.105)
for mat in list(bpy.data.materials):
 if mat.name not in [shared.name,floorMat.name,labelMat.name]:bpy.data.materials.remove(mat,do_unlink=True)
for im in list(bpy.data.images):
 if im.name.startswith(NAME):im.pack()
 else:bpy.data.images.remove(im,do_unlink=True)
dest=WORK/'Blender/NP_Plants_Unified_Runtime.blend';dest.parent.mkdir(parents=True,exist_ok=True);bpy.data.orphans_purge(do_recursive=True);bpy.ops.wm.save_as_mainfile(filepath=str(dest),compress=True);original.close();print('UNIFIED_GALLERY_PASS',len(pairs),'crops; one plant material',flush=True)
