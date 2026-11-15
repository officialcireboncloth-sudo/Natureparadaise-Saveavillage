import sys
from pathlib import Path

import bpy
from mathutils import Vector

work = Path(__file__).resolve().parent
sys.path.insert(0, str(work.parents[1] / 'ArtSource/FruitTrees/Scripts'))
from build_apple_blender import aim, material_atlas, setup_lighting
from fruit_tree_atlas import SOURCE, TEXTURES
from fruit_tree_foliage import configure_cutout


scene = bpy.context.scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
setup_lighting(scene)
scene.cycles.samples = 10
scene.view_settings.look = 'AgX - Medium High Contrast'
ground = bpy.data.materials['Ivory background'].node_tree.nodes.get('Principled BSDF')
ground.inputs['Base Color'].default_value = (.43, .34, .185, 1)
focus = Vector((0, 0, 1.13))
tilt = Vector((6.8, -10.5, 7.77))
aim(scene.camera, focus + tilt, focus)
right = scene.camera.rotation_euler.to_matrix() @ Vector((1, 0, 0))
horizontal_up = Vector((-right.y, right.x, 0)).normalized()
scene.camera.data.ortho_scale = 21.4
scene.render.resolution_x = 1260
scene.render.resolution_y = 570
species = ('Apple', 'Avocado', 'Cherry', 'Guava', 'Mango')

for revision in ('before', 'after'):
    if revision == 'before':
        model_folder, atlas_folder = SOURCE / 'Blender', TEXTURES
    else:
        model_folder, atlas_folder = work, work / 'Atlas'
    atlas = material_atlas(atlas_folder)
    configure_cutout(atlas)
    copies = []
    for index, tree in enumerate(species):
        path = model_folder / f'NP_{tree}.blend' if revision == 'before' else model_folder / tree / f'NP_{tree}.blend'
        names = [f'NP_{tree}_{phase}_LOD1' for phase in ('Young', 'HarvestReady')]
        with bpy.data.libraries.load(str(path)) as (_, loaded):
            loaded.collections = names
        assert all(loaded.collections), path
        for row, collection in enumerate(loaded.collections):
            for obj in collection.objects:
                copy = obj.copy()
                copy.data = obj.data.copy()
                copy.data.materials[0] = atlas
                copy.location += right * ((index - 2) * 3.75) + horizontal_up * ((row - .5) * 7.1)
                copy.rotation_euler.z += .28
                scene.collection.objects.link(copy)
                copies.append(copy)

    path = work / f'orchard-{revision}.png'
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    for copy in copies:
        bpy.data.objects.remove(copy, do_unlink=True)
    print(f'Review saved to {path}', flush=True)
