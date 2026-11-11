"""Render phase turntable stills and isolated inventory artwork from the authored meshes."""
from pathlib import Path
import argparse
from math import cos, radians, sin
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_g14_blender import KINDS, MASTER


def orient(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def render(folder, master=MASTER, only=None, details=False, skip_stages=False):
    bpy.ops.wm.open_mainfile(filepath=str(master))
    scene = bpy.context.scene
    roots = [o for o in scene.objects if 'Plant' in o]
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 20
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 8
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.world = bpy.data.worlds.new('Soft studio fill')
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get('Background')
    background.inputs['Color'].default_value = (.82,.87,1.,1.)
    background.inputs['Strength'].default_value = .19
    bpy.ops.mesh.primitive_plane_add(size=200)
    floor = bpy.context.object
    floor.name = 'Preview floor'
    floor.location.z = -.008
    ground = bpy.data.materials.new('Ivory background')
    ground.use_nodes = True
    ground.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = (.56,.60,.53,1.)
    ground.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .92
    floor.data.materials.append(ground)
    for name, pos, power, size, color in [
        ('Key', (-3,-4,6), 1150, 5.5, (1.,.92,.81)),
        ('Fill', (4,-3,3.7), 490, 5., (.87,.94,1.)),
        ('Edge', (0,4,5), 930, 4., (1.,.96,.86)),
    ]:
        lamp = bpy.data.lights.new(name, 'AREA')
        lamp.energy, lamp.size, lamp.color = power, size, color
        obj = bpy.data.objects.new(name, lamp)
        scene.collection.objects.link(obj)
        obj.location = pos
        orient(obj, (0,.6,.3))
    data = bpy.data.cameras.new('Growth reference camera')
    data.type = 'ORTHO'
    camera = bpy.data.objects.new('Growth reference camera', data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.resolution_percentage = 100
    folder.mkdir(parents=True, exist_ok=True)
    for i, kind in enumerate(KINDS):
        if skip_stages or (only and kind != only):
            continue
        for root in roots:
            for part in root.children:
                part.hide_render = root['Plant'] != kind
        y = i * 1.65
        camera.location = (0, y-8, 3.4)
        orient(camera, (0,y,.44))
        data.ortho_scale = 7.04
        floor.hide_render = False
        scene.render.film_transparent = False
        scene.render.resolution_x, scene.render.resolution_y = 1536, 345
        scene.render.filepath = str(folder / f'{kind}_phases.png')
        bpy.ops.render.render(write_still=True)
        for root in roots:
            for part in root.children:
                part.hide_render = root['Plant'] != kind or root['Phase'] != 'Harvest'
        camera.location = (3.08,y-3.2,2.0)
        orient(camera, (2.94 if kind == 'Daisy' else 2.79,y,.19))
        data.ortho_scale = 1.26 if kind == 'Daisy' else .84
        floor.hide_render = True
        scene.render.film_transparent = True
        scene.render.resolution_x = scene.render.resolution_y = 448
        scene.render.filepath = str(folder / f'{kind}_harvest.png')
        bpy.ops.render.render(write_still=True)
    if details:
        ready = next(root for root in roots if root['Plant']=='Daisy' and root['Phase']=='Ready')
        for root in roots:
            for part in root.children:
                part.hide_render = root != ready
        floor.hide_render = False
        scene.render.film_transparent = False
        for name, focus, offset, width, resolution in [
            ('Flower', (ready.location.x,.018,.84), (.17,-3.2,2.3), .55, (640,480)),
            ('Leaves', (ready.location.x,-.015,.16), (.23,-2.8,3.4), .96, (640,480)),
            # Map's outdoor pitch/yaw and standard 6.5 orthographic half-height.
            ('Field', (ready.location.x,0,.44), (15*-sin(radians(-142.648))*cos(radians(30)),
              15*-cos(radians(-142.648))*cos(radians(30)),15*sin(radians(30))), 13*16/9, (1920,1080)),
        ]:
            camera.location = Vector(focus)+Vector(offset)
            orient(camera, focus)
            data.ortho_scale = width
            scene.render.resolution_x, scene.render.resolution_y = resolution
            scene.render.filepath = str(folder / f'Daisy_{name}.png')
            bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    parser.add_argument('--master', type=Path, default=MASTER)
    parser.add_argument('--only', choices=tuple(KINDS))
    parser.add_argument('--details', action='store_true')
    parser.add_argument('--skip-stages', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    render(args.folder, args.master, args.only, args.details, args.skip_stages)
