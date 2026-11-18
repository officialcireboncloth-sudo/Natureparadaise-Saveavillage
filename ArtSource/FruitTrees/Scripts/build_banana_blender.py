"""Build the banana's leaf sheaths, arched blades and hanging hands."""
import math
import os
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_apple_blender import (
    Geometry, UP, adjust_height, aim, curved_axis, height_adjustment, material_atlas, ring_shape,
    setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import ATLAS, BANANA_BLOSSOM_UV, BANANA_GREEN_PEEL_WIDTH, SOURCE, TEXTURES
from fruit_tree_dimensions import BANANA_STARTER_SCALES, ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout


WORK = Path(os.environ['NP_BANANA_STAGE'])
SHEATH = 'BananaSheath'
GREEN = 'Banana/FruitForming'
RIPE = 'Banana/FruitReady'
# Azimuth, attachment height as a stem fraction, reach, rise, droop, width and bank.
YOUNG = (
    (2.96, .63, .68, .36, .38, .34, -.47), (5.78, .66, .69, .32, .44, .32, .53),
    (1.68, .78, .45, .43, .27, .31, .14), (.29, .83, .72, .45, .38, .38, .61),
    (3.57, .95, .54, .72, .06, .38, -.83), (.68, 1, .46, .76, .06, .36, .82),
)
FORMING = (
    (3.52, .75, 1.20, .59, .66, .70, -.55), (5.72, .76, 1.25, .58, .69, .74, .58),
    (2.14, .80, 1.04, .79, .59, .68, -.28), (6.12, .89, 1.40, .73, .57, .78, .61),
    (3.01, .95, 1.37, .86, .43, .76, -.62), (.73, .97, .88, 1.07, .20, .71, .51),
    (1.74, 1, .55, 1.18, .08, .64, .74), (3.94, .97, .64, 1.05, .31, .53, -.62),
)
MATURE = (
    (3.72, .72, 1.31, .54, .77, .78, -.60), (5.72, .75, 1.34, .57, .76, .80, .63),
    (1.72, .75, 1.14, .71, .63, .74, -.45), (3.07, .80, 1.43, .75, .59, .84, -.57),
    (6.16, .86, 1.53, .83, .56, .87, .57), (4.50, .88, .88, .76, .59, .64, .14),
    (2.19, .92, 1.12, .92, .54, .76, -.21), (.53, .93, 1.27, .94, .55, .82, .57),
    (3.49, .98, 1.04, 1.17, .14, .85, -.79), (.83, 1, .88, 1.19, .12, .81, .63),
)


def radial(distance, yaw, height=0):
    return Vector((distance * math.cos(yaw), distance * math.sin(yaw), height))


def pseudo_stem(mesh, height, radius, detail, *, cut=False):
    points = [Vector((radius * sway, radius * lean, height * t)) for sway, lean, t in
              ((0, 0, -.018), (-.07, .10, .17), (.07, .19, .48), (.18, .06, .76), (.24, -.12, 1))]
    widths = [radius * f for f in (1, .92, .76, .63, .50 if not cut else .79)]
    sides = 18 if detail else 11
    tube(mesh, *curved_axis(points, widths, 4 if detail else 2), SHEATH, sides,
         closed=not cut, uv_span=(.98, .12))
    if cut:
        uv = [(.50 + .46 * math.cos(i * math.tau / sides), .50 + .46 * math.sin(i * math.tau / sides))
              for i in range(sides)]
        mesh.add(mesh.vertices[-sides:], [(tuple(range(sides)), uv)], 'CutWood')

    # Overlapping leaf bases widen the stem near the soil and taper into individual sheaths.
    for index, (yaw, base, fraction) in enumerate(((.38, .01, .47), (2.52, .02, .53), (4.55, .01, .36),
                                                  (5.65, .12, .57), (1.42, .27, .59), (3.68, .30, .62))):
        if cut and index > 3:
            break
        vertices, uv, faces = [], [], []
        span = .89 + index % 3 * .11
        for row, t in enumerate((0, .18, .44, .73, .91, 1)):
            level = base + t * fraction
            core = Vector((radius * level * .24, radius * .115 * math.sin(level * math.pi), height * level))
            ring = radius * (1.038 - .45 * level + .24 * t**4)
            spread = max(.024, (1 - t**2.35) * span)
            for column, across in enumerate((-1, -.55, 0, .55, 1)):
                angle = yaw + .46 * t + across * spread
                point = core + radial(ring + radius * .095 * (1 - across**2), angle)
                point.z -= abs(across)**1.6 * fraction * height * (.09 + .08 * t) * math.sin(math.pi * t)
                vertices.append(point)
                uv.append((.12 + .74 * column / 4, .986 - (.51 + .05 * (index % 3)) * t))
            if row:
                for col in range(4):
                    a = (row - 1) * 5 + col
                    face = (a, a + 1, a + 6, a + 5)
                    faces.append((face, [uv[i] for i in face]))
        mesh.add(vertices, faces, SHEATH)
    return points[-1]


def frond(stem, blades, base, yaw, reach, rise, droop, width, bank, intact, detail, flip=False):
    forward, sideways = radial(1, yaw), radial(1, yaw + math.pi * .5)

    def center(t):
        return base + forward * (reach * (t - .03)) + sideways * (.17 * reach * bank * t**2) + UP * (
            rise * math.sin(math.pi * .60 * t) - droop * t**2.65)

    petiole = [center(t) for t in (0, .075, .158, .235)]
    thickness = width * .093
    first = len(stem.vertices)
    tube(stem, petiole, [thickness, thickness * .69, thickness * .39, thickness * .13],
         SHEATH, 7 if detail else 5, uv_span=(.67, .24))
    stem.anchor_from(first, base)
    rows = (0, .10, .18, .27, .37, .48, .60, .71, .81, .91, 1) if detail else (0, .16, .32, .49, .67, .84, 1)
    columns = (-1, -.55, 0, .55, 1) if detail else (-1, 0, 1)
    vertices, uv, faces = [], [], []
    for row, t in enumerate(rows):
        axis = (center(t + .003) - center(max(t - .003, 0))).normalized()
        up = axis.cross(sideways).normalized()
        roll = bank * (.34 + .66 * t)
        side = sideways * math.cos(roll) + up * math.sin(roll)
        up = axis.cross(side).normalized()
        for across in columns:
            sag = (width * .34 * abs(across)**1.45 + width * .034 * across * math.sin(21 * t))
            point = center(t) + side * (across * width * .55) - up * (sag * math.sin(math.pi * t)**.65)
            vertices.append(point)
            uv.append(((1 + (-across if flip else across)) * .5, 1 - t))
        if row:
            stride = len(columns)
            for col in range(stride - 1):
                a = (row - 1) * stride + col
                face = (a, a + stride, a + stride + 1, a + 1)
                faces.append((face, [uv[i] for i in face]))
    blades.add(vertices, faces, 'Banana/LeafAccent' if intact else 'Banana/Leaf', anchor=base)


def starter(sucker):
    stem, leaves = Geometry(), Geometry()
    height, radius = (.166, .055) if sucker else (.347, .081)
    apex = pseudo_stem(stem, height, radius, True)
    if sucker:
        top = apex + Vector((-.014, .012, .207))
        axis = [apex, apex.lerp(top, .56) + Vector((-.012, .004, 0)), top]
        tube(stem, *curved_axis(axis, [.034, .028, .0024], 5), SHEATH, 12, closed=True, uv_span=(.66, .15))
        frond(stem, leaves, apex - UP * .023, 5.68, .098, .106, .009, .068, .47, True, True)
    else:
        for yaw, level, reach, rise, drop, width, bank in ((3.45, .83, .325, .287, .008, .238, -1.08),
                                                           (5.98, .974, .334, .350, .020, .249, .95),
                                                           (.76, .67, .136, .156, .039, .087, -.31)):
            base = apex * level + radial(radius * .20, yaw)
            frond(stem, leaves, base, yaw, reach, rise, drop, width, bank, True, True)
    return [('Rolled spear and overlapping sheaths', stem), ('Intact starter blades', leaves)]


def plant(height, radius, rows, detail):
    stem, leaves = Geometry(), Geometry()
    apex = pseudo_stem(stem, height, radius, detail)
    for index, (yaw, level, reach, rise, drop, width, bank) in enumerate(rows):
        base = apex * level + radial(radius * .26, yaw)
        intact = rows is YOUNG and index in (2, 4, 5)
        frond(stem, leaves, base, yaw, reach, rise, drop, width, bank, intact, detail, index % 2 != 0)
    return stem, leaves, apex


def finger(mesh, socket, yaw, length, radius, fall, ripe, detail, *, spread=1):
    path = [socket + radial(length * reach * spread, yaw, -length * drop * fall)
            for reach, drop in ((0, 0), (.10, .24), (.39, .60), (.69, .87), (.91, .93), (1, .82))]
    widths = [radius * f for f in (.31, .75, 1, .96, .72, .30)]
    start = len(mesh.surfaces)
    slot = RIPE if ripe else GREEN
    tube(mesh, *curved_axis(path, widths, 3 if detail else 2), slot, 11 if detail else 7,
         closed=True, uv_span=(.19, .93))
    if not ripe:
        mesh.surfaces[start:] = [(slot, [(u * BANANA_GREEN_PEEL_WIDTH, v) for u, v in uv])
                                 for slot, uv in mesh.surfaces[start:]]
    end = path[-1]
    cap = end + radial(radius * .13, yaw, radius * .17)
    tube(mesh, [end, cap], [radius * .31, radius * .21], 'AppleSeed', 6 if detail else 4, closed=True)


def banana_heart(mesh, socket, detail):
    first = len(mesh.surfaces)
    profile = ((0, .07), (.11, .15), (.24, .27), (.46, .37), (.67, .40), (.86, .31), (1, .17))
    ring_shape(mesh, socket - UP * .27, .27, profile, GREEN, 14 if detail else 9)
    left, right = BANANA_BLOSSOM_UV
    mesh.surfaces[first:] = [(slot, [(left + (right - left) * u, v) for u, v in uv])
                             for slot, uv in mesh.surfaces[first:]]


def hanging_bunch(apex, ripe, detail):
    fruit = Geometry()
    yaw = 5.31
    hook = apex + radial(.26, yaw, .19)
    head = apex + radial(.55, yaw, -.075)
    tail = head + radial(.086, yaw, -.656 if ripe else -.52)
    tube(fruit, *curved_axis([apex - UP * .18, apex + radial(.093, yaw, .126), hook, head, tail],
                             [.053, .046, .036, .026, .016], 5 if detail else 3),
         SHEATH, 10 if detail else 7, closed=True, uv_span=(.79, .33))
    rng = random.Random(81224)
    counts = (7, 7, 6, 5) if ripe else (6, 6, 5)
    for row, count in enumerate(counts):
        level = head.lerp(tail, .11 + row * (.235 if ripe else .27))
        for i in range(count):
            bearing = yaw + math.tau * (i + .37 * row) / count
            start = level + radial(.072 if ripe else .051, bearing)
            length = rng.uniform(.172, .199) * (1.18 if ripe else 1) * (1 - .052 * row)
            radius = rng.uniform(.048, .057) * (1 if ripe else .80) * (1 - .045 * row)
            tube(fruit, [level + UP * .012, start], [radius * .43, radius * .31], SHEATH, 6 if detail else 4)
            finger(fruit, start, bearing, length, radius, 1.22, ripe, detail, spread=.75 if ripe else .65)
    if not ripe:
        banana_heart(fruit, tail, detail)
    fruit.anchor_from(0, apex)
    return fruit


def produce():
    mesh = Geometry()
    base = Vector((.085, .021, .168))
    tube(mesh, [base - UP * .028, base + Vector((.035, .029, .063))], [.055, .044], SHEATH, 11, closed=True)
    for i, yaw in enumerate((2.97, 3.47, 3.94, 4.45, 4.96)):
        start = base + radial(.044, yaw, -.008 * i)
        tube(mesh, [base + UP * .014, start], [.024, .017], SHEATH, 7)
        finger(mesh, start, yaw, .239 - .008 * i, .044 - .001 * i, .70, True, True)
    ground = min(point.z for point in mesh.vertices)
    for point in mesh.vertices:
        point.z -= ground
    return mesh


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    mesh_output = WORK / 'Mesh'
    mesh_output.mkdir(parents=True, exist_ok=True)
    atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(atlas)
    configure_cutout(material)
    unity_mesh.configure(material, mesh_output)
    for phase, sucker in (('Seed', True), ('Sprout', False)):
        stage(f'NP_Banana_{phase}', starter(sucker), BANANA_STARTER_SCALES[phase], material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        # Keep the mature height target by letting the two tallest blades rise less above the taller stem.
        for phase, height, radius, rows in (('Young', .870, .133, YOUNG),
                                             ('FruitForming', 1.61, .193, FORMING),
                                             ('HarvestReady', 1.72, .218, MATURE)):
            stem, leaves, apex = plant(height, radius, rows, detail)
            ripe = phase == 'HarvestReady'
            parts = [('Pseudostem, petioles and overlapping sheaths', stem), ('Broad banana blades', leaves)]
            if phase != 'Young':
                fruit = hanging_bunch(apex, ripe, detail)
                adjust_height((stem, leaves, fruit), height_adjustment((stem, leaves, fruit)))
                parts.append(('Golden banana hands' if ripe else 'Green hands and purple banana heart', fruit))
            stage(f'NP_Banana_{phase}{suffix}', parts, 1, material, ripe and detail)
    stump = Geometry()
    pseudo_stem(stump, .33, .218, True, cut=True)
    stage('NP_Banana_Stump', [('Cut banana pseudostem', stump)], 1, material, False)
    stage('NP_Banana_Produce', [('Ripe banana hand', produce())], 1, material, False)

    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT * .50)
    aim(scene.camera, (2.6, -7.7, 3.25), focus)
    scene.camera.data.ortho_scale = 3.84
    scene.render.resolution_x, scene.render.resolution_y = 1100, 950
    scene.render.filepath = '//'
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                view = area.spaces.active
                view.region_3d.view_location = focus
                view.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
                view.region_3d.view_distance = 5.1
                view.shading.type = 'MATERIAL'
    for node in material.node_tree.nodes:
        if node.type == 'TEX_IMAGE':
            image = TEXTURES / Path(node.image.filepath).name
            node.image.filepath = bpy.path.relpath(str(image), start=str(SOURCE / 'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Banana.blend'), relative_remap=False)
    print('BANANA_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
