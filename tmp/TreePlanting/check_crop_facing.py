from pathlib import Path
import json
import math

import bpy
from mathutils import Quaternion, Vector


work = Path(__file__).resolve().parent
root = work.parents[1]
assets = root / 'Assets/Nature  Paradaise'
group = [('G09', 'Hibiscus', 0), ('G10', 'Sunflower', 0), ('G10', 'Dahlia', 0),
         ('G12', 'Orchid', 180), ('G14', 'Daisy', 180)]
yaw = math.radians(37.352)
field = Quaternion((0, 0, 1), -yaw)
forward = field @ Vector((0, -1, 0))
blender_cam = -forward * math.cos(math.radians(30)) + Vector((0, 0, .5))


def template_mesh(kind, spec, material):
    mesh = bpy.data.meshes.new(kind)
    vertices = [(x, z, y) for x, y, z in spec['vertices']]
    triangles = spec['submeshes'][0]
    mesh.from_pydata(vertices, [], [(a, c, b) for a, b, c in zip(
        triangles[::3], triangles[1::3], triangles[2::3])])
    mesh.materials.append(material)
    uv = mesh.uv_layers.new()
    for polygon in mesh.polygons:
        polygon.use_smooth = True
        for index in polygon.loop_indices:
            uv.data[index].uv = spec['uv'][mesh.loops[index].vertex_index]
    return mesh


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 9
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 1040, 472
    scene.world = bpy.data.worlds.new('Ambient')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes.get('Background').inputs[0].default_value = (.79, .85, 1, 1)
    scene.world.node_tree.nodes.get('Background').inputs[1].default_value = .31
    scene.view_settings.view_transform = 'AgX'
    petals = bpy.data.materials.new('Unity crop atlas')
    petals.use_nodes = True
    nodes = petals.node_tree.nodes
    node = nodes.new('ShaderNodeTexImage')
    node.image = bpy.data.images.load(str(assets / 'Textures/Shared/Plants/NP_Plants_Unified_01_BaseColor.png'))
    petals.node_tree.links.new(node.outputs['Color'], nodes.get('Principled BSDF').inputs['Base Color'])
    nodes.get('Principled BSDF').inputs['Roughness'].default_value = .82
    scene.view_settings.look = 'AgX - Medium High Contrast'

    ground = bpy.data.materials.new('Garden soil')
    ground.diffuse_color = (.21, .13, .086, 1)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.015))
    bpy.context.object.data.materials.append(ground)
    models = []
    for i, (folder, kind, old_yaw) in enumerate(group):
        path = assets / f'mesh/Crops/{folder}/NP_{kind}_Ready.json'
        if not path.is_file():
            path, = list((assets / f'mesh/Crops/{folder}').glob(f'NP_{kind}*Ready.json'))
        spec = json.loads(path.read_text())
        model = bpy.data.objects.new(kind, template_mesh(kind, spec, petals))
        scene.collection.objects.link(model)
        model.location = field @ Vector((i*1.18 - 2.36, 0, 0))
        model.rotation_mode = 'QUATERNION'
        before = field @ Quaternion((0, 0, 1), -math.radians(old_yaw))
        after = field @ Quaternion((0, 0, 1), -math.pi)
        assert ((after @ Vector((0, -1, 0))).normalized()).dot(-forward) > .9999
        assert ((after @ Vector((0, 0, 1))).normalized()).dot(Vector((0, 0, 1))) > .9999
        models.append((model, before, after))
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 6.45
    focus = Vector((0, 0, .55))
    camera.location = focus + blender_cam * 13
    camera.rotation_euler = (focus - camera.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.light_add(type='AREA', location=field @ Vector((-3, 4, 5)))
    light = bpy.context.object
    light.data.energy = 930
    light.data.shape = 'DISK'
    light.data.size = 5.0
    light.rotation_euler = (focus - light.location).to_track_quat('-Z', 'Y').to_euler()
    for i, label in enumerate(('crop-facing-before', 'crop-facing-after'), start=1):
        for model in models:
            model[0].rotation_quaternion = model[i]
        scene.render.filepath = str(work / (label + '.png'))
        bpy.ops.render.render(write_still=True)
    print('CROP_FACING_RENDER_PASS actual Unity mesh specs, camera and field yaw, upright plants', flush=True)


main()
