"""Build the editable twelve-phase G06 master and export its LOD pairs."""
import sys, json, math
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g06_config import *
import plant_blender_common as C
import g06_geometry as G

bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new(ATLAS);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
for suffix in ['BaseColor','Normal','SurfaceMask']:
    im=bpy.data.images.load(str(TEXTURES/(ATLAS+'_'+suffix+'.png')));im.pack();tex=nodes.new('ShaderNodeTexImage');tex.image=im;tex.label=suffix
    if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    elif suffix=='Normal':
        im.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.60;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
    else:
        im.colorspace_settings.name='Non-Color';im.alpha_mode='CHANNEL_PACKED';inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
C.configure(mat,MODELS)
exports=[];plan={};counts={}
for row,(kind,stages) in enumerate(PHASES.items()):
    plan[kind]=[];counts[kind]={}
    for col,stage in enumerate(stages):
        coll=bpy.data.collections.new(kind+' / '+stage);scene.collection.children.link(coll)
        C.current=coll;root=bpy.data.objects.new(kind+'_'+stage,None);coll.objects.link(root);root.location=((col-2.5)*1.30,(.5-row)*1.9,0)
        root['Plant']=kind;root['Phase']=stage;root['Reference']='User attachment: '+kind+' five growth phases and harvested crop'
        C.parent=root
        {'Radish':G.radish,'Onion':G.onion}[kind](stage)
        if kind=='Radish' and stage=='Harvest':
            # Reorient the complete root and foliage together to rest on its bulb.
            children=list(root.children)
            pivot=bpy.data.objects.new('Harvest radish laid on side',None);coll.objects.link(pivot);pivot.parent=root;pivot.rotation_euler.y=math.radians(66)
            for o in children:o.parent=pivot
            bpy.context.view_layer.update()
            minimum=min((root.matrix_world.inverted()@o.matrix_world@Vector(c)).z for o in root.children_recursive if o.type=='MESH' for c in o.bound_box)
            pivot.location.z=-minimum+.006
        name='NP_'+kind+'_'+stage
        objects=[o for o in root.children_recursive if o.type=='MESH']
        counts[kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),hollowTips=sum(bool(o.get('HollowTip')) for o in objects))
        exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=objects),name))
        plan[kind].append(dict(stage=stage,model=name,source_group='G06',scale=[1,1,1]))
for i,kind in enumerate(PHASES):
    coll=bpy.data.collections.new('EXTRAS / '+kind+' cut half');scene.collection.children.link(coll);C.current=coll
    root=bpy.data.objects.new('NP_'+kind+'_CutHalf',None);coll.objects.link(root);root.location=((i-.5)*1.5,-2.4,0);C.parent=root
    G.cut_half(kind);exports.append(C.export(dict(plant=kind,stage='Extra',root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),root.name))

# Preserve actual vendor candidates separately for comparison, without exporting them.
source=bpy.data.collections.new('SOURCE - vendor comparison, excluded from exports');scene.collection.children.link(source)
folder=A/'Pack/Toon Series/Toon Farm Pack/Models';audit=[]
for filename in ['TFP_Radish_01A.fbx','TFP_Radish_Plant_01A_Leaves.fbx','TFP_Onion_01A.fbx','TFP_Onion_Spring_01A.fbx']:
    candidates=list(folder.rglob(filename))
    if not candidates:continue
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(candidates[0]));objects=set(bpy.data.objects)-before
    audit.append(dict(file=str(candidates[0].relative_to(ROOT)),triangles=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in objects if o.type=='MESH'),decision='Retained for comparison; new reference proportions, growth stages, leaf curvature and UV registration authored for G06'))
    for o in objects:C.move(o,source)
source.hide_render=True;source.hide_viewport=True
(REVIEW/'SourceAudit.json').write_text(json.dumps(audit,indent=2))

studio=bpy.data.collections.new('STUDIO - excluded from exports');scene.collection.children.link(studio)
floor=bpy.data.materials.new('G06 warm white studio');floor.diffuse_color=(.91,.90,.87,1);floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.91,.90,.87,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.78
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.006));o=bpy.context.object;o.name='Studio floor';C.move(o,studio);o.data.materials.append(floor)
for loc,power,size in [((-3,-4,6),260,5),((4,1,4),140,4),((0,4,5),180,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.35))-o.location).to_track_quat('-Z','Y').to_euler();C.move(o,studio)
bpy.ops.object.camera_add(location=(0,-8,6));camera=bpy.context.object;C.move(camera,studio);camera.data.type='ORTHO';camera.data.ortho_scale=8.9;scene.camera=camera
camera.rotation_euler=(Vector((0,0,.30))-camera.location).to_track_quat('-Z','Y').to_euler()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_location=(0,0,.30)
            area.spaces.active.region_3d.view_distance=9.5
            area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
            area.spaces.active.region_3d.view_perspective='ORTHO'
            area.spaces.active.shading.type='MATERIAL'
scene.world=bpy.data.worlds.new('G06 neutral white world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.15
ink=bpy.data.materials.new('G06 label ink');ink.diffuse_color=(.045,.10,.035,1)
for row,(kind,stages) in enumerate(PHASES.items()):
    for col,stage in enumerate(stages):
        cu=bpy.data.curves.new(kind+' '+stage,'FONT');cu.body=kind+' | '+stage;cu.size=.105;cu.align_x='CENTER';ob=bpy.data.objects.new(cu.name,cu);studio.objects.link(ob);ob.location=((col-2.5)*1.30,(.5-row)*1.9-.58,.006);cu.materials.append(ink)
scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-.05
scene.render.resolution_percentage=100
(MASTER.parent/'G06_Manifest.json').write_text(json.dumps(dict(group='G06',revision='R01 reference root vegetables',plants=list(PHASES),material=ATLAS,atlas=ATLAS,newAtlasPages=0,phaseAppearances=12,uniqueNewExports=len(exports),counts=counts,exports=exports,integration='Prepared visual assets; no changes to gameplay profiles or scene bindings'),indent=2))
(MASTER.parent/'G06_VisualPlan.json').write_text(json.dumps(dict(group='G06',plants=plan),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER))
print('G06_MASTER_EXPORT_PASS',len(exports),'base exports',counts,flush=True)
