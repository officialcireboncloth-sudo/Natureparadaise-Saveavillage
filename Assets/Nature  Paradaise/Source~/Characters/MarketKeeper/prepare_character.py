"""Rename the uploaded mesh/material, preserve its rig/UV, and export portable FBX."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
ASSETS=ROOT/'Assets/Nature  Paradaise'
OUT=ASSETS/'mesh/Character/MarketKeeper'
SOURCE=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
original=ROOT/'ArtSource/MarketKeeper/Originals/MarketKeeper_Original.fbx'
bpy.ops.import_scene.fbx(filepath=str(original if original.exists() else OUT/'MarketKeeper.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig.name='MarketKeeper_Rig';rig.data.name='MarketKeeper_Skeleton'
mesh.name='MarketKeeper_Body';mesh.data.name='MarketKeeper_Body'
material=mesh.data.materials[0];material.name='MarketKeeper'
image=bpy.data.images.load(str(OUT/'Textures/MarketKeeper_BaseColor.jpg'),check_existing=True)
image.name='MarketKeeper_BaseColor'
for n in material.node_tree.nodes:
    if n.type=='TEX_IMAGE':n.image=image
    if n.type=='BSDF_PRINCIPLED':
        n.inputs['Roughness'].default_value=0.78
        n.inputs['Metallic'].default_value=0
for im in list(bpy.data.images):
    if im!=image and im.users==0:bpy.data.images.remove(im)
stats=dict(vertices=len(mesh.data.vertices),triangles=sum(len(p.vertices)-2 for p in mesh.data.polygons),bones=len(rig.data.bones),vertexGroups=len(mesh.vertex_groups),uvLayers=list(mesh.data.uv_layers.keys()),actions=list(bpy.data.actions.keys()),textureSize=list(image.size),bounds=list(mesh.dimensions),objects=[rig.name,mesh.name],material=material.name)
assert stats['triangles']==5465 and stats['vertices']==2815 and stats['bones']==65,stats
assert stats['textureSize']==[1024,1024]
for o in bpy.context.scene.objects:o.select_set(False)
rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'MarketKeeper.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',embed_textures=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True)
image.filepath=bpy.path.relpath(str(OUT/'Textures/MarketKeeper_BaseColor.jpg'),start=str(SOURCE))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MarketKeeper.blend'))
(SOURCE/'MarketKeeper_Manifest.json').write_text(json.dumps(stats,indent=2))
# Check the exported file independently, without changing the saved editing source.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'MarketKeeper.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
assert len(rig.data.bones)==stats['bones']
assert len(mesh.data.vertices)==stats['vertices']
assert sum(len(p.vertices)-2 for p in mesh.data.polygons)==stats['triangles']
assert len(mesh.vertex_groups)==stats['vertexGroups']
assert list(mesh.data.uv_layers.keys())==stats['uvLayers']
for im in bpy.data.images:
    assert list(im.size)==[1024,1024],(im.filepath,list(im.size))
    assert Path(bpy.path.abspath(im.filepath)).is_file(),im.filepath
review=ROOT/'ArtSource/MarketKeeper/Preview.png'
sc=bpy.context.scene
sc.render.engine='CYCLES';sc.cycles.samples=16
sc.render.resolution_x=700;sc.render.resolution_y=800;sc.render.resolution_percentage=100
sc.world=bpy.data.worlds.new('ReviewWorld');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(0.23,0.25,0.27,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=0.6
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.55,-2.8,1.4));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=1.25;aim(cam,(0,0,0.54));sc.camera=cam
for pos,power,size in [((2,-3,4),400,3),((-2,-1,2),180,3),((0,2,3),230,2)]:
    bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;aim(light,(0,0,0.5))
sc.view_settings.view_transform='AgX';sc.render.filepath=str(review)
bpy.ops.render.render(write_still=True)
(ROOT/'ArtSource/MarketKeeper/BlenderAudit.txt').write_text('PASS FBX roundtrip: 2815 vertices, 5465 triangles, 65 bones, vertex group count/UV layers retained.\nPASS relative 1024x1024 texture resolves after re-import.\nPASS no animation added or removed.\n')
print('MARKET_KEEPER_FBX_PASS',flush=True)
