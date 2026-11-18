"""Bake the rambutan peel maps. The high-detail surface exists only in this bake."""
import math
import os
import random
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.noise import noise

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import PADDING, RAMBUTAN_PEEL_WIDTH, TILE

WORK = Path(os.environ['NP_RAMBUTAN_PEEL_BAKE'])
SPAN = 2.0
HEIGHT = TILE - PADDING * 2
WIDTH = round(HEIGHT * RAMBUTAN_PEEL_WIDTH)
PAD = PADDING


def linear_rgb(color):
    rgb = [int(color[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(value / 12.92 if value < .04045 else ((value + .055) / 1.055)**2.4 for value in rgb)


def mix(a, b, weight):
    return tuple(x + (y - x) * weight for x, y in zip(a, b))


def surface_object(name, vertices, polygons, colors, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], polygons)
    mesh.update()
    pigment = mesh.color_attributes.new(name='Pigment', type='FLOAT_COLOR', domain='POINT')
    pigment.data.foreach_set('color', [channel for color in colors for channel in (*color, 1)])
    mesh.polygons.foreach_set('use_smooth', [True] * len(polygons))
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(material)


def skin_height(x, y):
    u, v = math.tau * (x / SPAN), math.tau * y
    fine = math.sin(23 * u + 13 * v) * math.sin(29 * u - 11 * v)
    folds = math.cos(14 * u - 19 * v + math.sin(8 * v))
    return .0018 * fine + .0010 * folds


def skin(tile, material):
    vertices, faces, colors = [], [], []
    dim_x, dim_y = 256, 128
    margin = 20
    stride = dim_x + 2 * margin + 1
    dark, medium, bright = map(linear_rgb, tile)
    for iy in range(-margin, dim_y + margin + 1):
        for ix in range(-margin, dim_x + margin + 1):
            x, y = SPAN * ix / dim_x, iy / dim_y
            u, v = math.tau * x / SPAN, math.tau * y
            ring = 1 + .23 * math.cos(v)
            point = Vector((ring * math.cos(u), ring * math.sin(u), .23 * math.sin(v)))
            rough = noise(point * 17)
            fine = noise(point * 105 + Vector((3.1, -4.7, 1)))
            z = skin_height(x, y) + .0012 * fine
            tint = max(0, min(1, .43 + .51 * rough + .24 * fine))
            color = mix(dark, medium, tint * 2) if tint < .5 else mix(medium, bright, (tint - .5) * 2)
            vertices.append((x, y, z))
            colors.append(color)
            if ix < dim_x + margin and iy < dim_y + margin:
                a = len(vertices) - 1
                faces.append((a, a + 1, a + stride + 1, a + stride))
    surface_object('Bumpy leathery peel', vertices, faces, colors, material)


def append_hair(vertices, faces, colors, root, heading, curl, reach, height, radius, swatch, shade):
    samples = 15
    sides = 8
    positions = []
    for i in range(samples):
        t = i / (samples - 1)
        bend = heading * (reach * t**1.87) + curl * (reach * .43 * t**4)
        rise = height * (math.sin(t * 2.05) * .87 + t * .13) - height * .19 * t**7
        positions.append(root + bend + Vector((0, 0, rise)))

    dark, red, golden, tip = swatch
    bottom = len(vertices)
    for index, center in enumerate(positions):
        t = index / (samples - 1)
        tangent = (positions[min(index + 1, samples - 1)] - positions[max(index - 1, 0)]).normalized()
        across = tangent.cross(curl).normalized()
        upright = tangent.cross(across).normalized()
        width = radius * ((1 - t)**1.75 * (1 + .31 * math.exp(-32 * t*t)) + .055)
        if t < .20:
            pigment = mix(dark, red, t * 5)
        elif t < .57:
            pigment = red
        elif t < .91:
            pigment = mix(red, golden, (t - .57) / .34)
        else:
            pigment = mix(golden, tip, (t - .91) / .09)
        pigment = tuple(channel * shade for channel in pigment)
        for j in range(sides):
            turn = j * math.tau / sides
            vertices.append(center + width * (across * math.cos(turn) + upright * math.sin(turn)))
            colors.append(pigment)
            if index < samples - 1:
                a = bottom + index * sides + j
                b = bottom + index * sides + (j + 1) % sides
                faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(range(len(vertices) - sides, len(vertices))))


def hairs(tile, material):
    rng = random.Random(40293)
    vertices, faces, colors = [], [], []
    swatch = tuple(map(linear_rgb, tile))
    across, down = 41, 24
    for j in range(down):
        for i in range(across):
            x = ((i + .5 * (j % 2) + rng.uniform(-.40, .40)) * SPAN / across) % SPAN
            y = (j + .5 + rng.uniform(-.42, .42)) / down
            yaw = rng.uniform(0, math.tau)
            heading = Vector((math.cos(yaw), math.sin(yaw), 0))
            curl = Vector((-heading.y, heading.x, 0)) * rng.choice((-1, 1))
            length = rng.uniform(.034, .118)
            rise = rng.uniform(.032, .079)
            radius = rng.uniform(.0061, .0105)
            shade = rng.uniform(.68, 1.19)
            height = skin_height(x, y) - .0035
            offsets_x = [0, SPAN] if x < .14 else [0, -SPAN] if x > SPAN - .14 else [0]
            offsets_y = [0, 1] if y < .14 else [0, -1] if y > .86 else [0]
            for dx in offsets_x:
                for dy in offsets_y:
                    root = Vector((x + dx, y + dy, height))
                    append_hair(vertices, faces, colors, root, heading, curl, length, rise, radius, swatch, shade)
    surface_object('Temporary tapered rambutan spinterns', vertices, faces, colors, material)


def make_material():
    material = bpy.data.materials.new('Temporary peel pigments')
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    pigment = nodes.new('ShaderNodeVertexColor')
    pigment.layer_name = 'Pigment'
    diffuse = nodes.get('Principled BSDF')
    links.new(pigment.outputs['Color'], diffuse.inputs['Base Color'])
    diffuse.inputs['Roughness'].default_value = .69
    diffuse.inputs['Specular IOR Level'].default_value = .17

    normal = nodes.new('ShaderNodeNewGeometry')
    bias = nodes.new('ShaderNodeVectorMath')
    bias.operation = 'ADD'
    bias.inputs[1].default_value = (0, 0, .75)
    links.new(normal.outputs['Normal'], bias.inputs[0])
    unit = nodes.new('ShaderNodeVectorMath')
    unit.operation = 'NORMALIZE'
    links.new(bias.outputs['Vector'], unit.inputs[0])
    encode = nodes.new('ShaderNodeVectorMath')
    encode.operation = 'MULTIPLY_ADD'
    encode.inputs[1].default_value = (.5,) * 3
    encode.inputs[2].default_value = (.5,) * 3
    links.new(unit.outputs['Vector'], encode.inputs[0])
    data = nodes.new('ShaderNodeEmission')
    links.new(encode.outputs['Vector'], data.inputs['Color'])
    return material, data


def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    # Pixel aspect preserves the physical 2:1 peel area while packing the atlas patch.
    scene.render.resolution_x, scene.render.resolution_y = WIDTH, HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.pixel_aspect_x = SPAN * HEIGHT / WIDTH
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.exposure = -.04
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.render.image_settings.file_format = 'PNG'
    scene.world = bpy.data.worlds.new('Diffuse daylight')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .53
    bpy.ops.object.camera_add(location=(SPAN / 2, .5, 3))
    scene.camera = bpy.context.object
    scene.camera.rotation_euler = (0, 0, 0)
    scene.camera.data.type = 'ORTHO'
    scene.camera.data.sensor_fit = 'HORIZONTAL'
    scene.camera.data.ortho_scale = SPAN
    for direction, energy, angle in (((-1.3, .45, 1.7), 1.95, .41), ((.64, -.84, 1), .40, .52)):
        bpy.ops.object.light_add(type='SUN')
        lamp = bpy.context.object
        lamp.rotation_euler = (-Vector(direction)).to_track_quat('-Z','Y').to_euler()
        lamp.data.energy, lamp.data.angle = energy, angle
    return scene


def save_padded(scene, name):
    work_file = WORK / 'Render.png'
    scene.render.filepath = str(work_file)
    bpy.ops.render.render(write_still=True)
    source = bpy.data.images.load(str(work_file))
    source.colorspace_settings.name = 'Non-Color'
    pixels = np.empty(HEIGHT * WIDTH * 4, dtype=np.float32)
    source.pixels.foreach_get(pixels)
    padded = np.pad(pixels.reshape(HEIGHT, WIDTH, 4), ((PAD, PAD), (PAD, PAD), (0, 0)), mode='wrap')
    bpy.data.images.remove(source)
    out = bpy.data.images.new(name, width=WIDTH + PAD * 2, height=TILE, alpha=True)
    out.colorspace_settings.name = 'Non-Color'
    out.pixels.foreach_set(padded.ravel())
    out.filepath_raw = str(WORK / f'{name}.png')
    out.file_format = 'PNG'
    out.save()
    bpy.data.images.remove(out)


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    entries = (
        ('FruitReady', ('820d1c', 'b9182b', 'd93033'), ('7e1525', 'd22734', 'd4b66b', 'eadbae')),
        ('FruitForming', ('394421', '667438', '889d49'), ('445421', '8c9b42', 'bbad62', 'd3c685')),
    )
    for name, peel_palette, hair_palette in entries:
        scene = setup_scene()
        material, normal_map = make_material()
        skin(peel_palette, material)
        hairs(hair_palette, material)
        save_padded(scene, f'{name}_BaseColor')
    material.node_tree.links.new(normal_map.outputs['Emission'],
                                 material.node_tree.nodes['Material Output'].inputs['Surface'])
    scene.view_settings.view_transform = 'Raw'
    scene.view_settings.exposure = 0
    scene.cycles.samples = 12
    scene.cycles.use_denoising = False
    save_padded(scene, 'Normal')
    print('RAMBUTAN_COMPLETE_PEEL_BAKE_PASS', flush=True)


if __name__ == '__main__':
    main()
