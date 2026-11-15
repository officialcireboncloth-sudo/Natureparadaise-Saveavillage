"""Author an orchard cherry for the shared seasonal growth stages and fruit atlas."""
import math
import os
import random
import sys
from functools import partial
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_apple_blender import (
    Geometry, UP, adjust_height, aim, curved_axis, fused_wood, height_adjustment,
    material_atlas, ring_shape, sample_axis, setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import ATLAS, SOURCE, TEXTURES
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays
from fruit_tree_dimensions import ORCHARD_HEIGHT


WORK = Path(os.environ['NP_CHERRY_STAGE'])
shade_cherry = partial(shade_sprays, species='Cherry')
TRUNK = ((0, 0, -.026), (-.028, .006, .15), (-.058, .032, .44), (-.072, .063, .75),
         (-.044, .080, 1.04), (.048, .091, 1.32), (.138, .067, 1.60),
         (.161, .020, 1.94), (.129, -.015, 2.25))
RADII = (.143, .112, .093, .077, .061, .049, .034, .020, .005)
# Staggered tiers open the crown into a vase, with two longer shoulders.
BRANCHES = (
    (.81, 3.47, .98, 1.60, .061), (.93, 5.65, 1.15, 1.68, .057),
    (1.03, 1.53, .93, 1.92, .053), (1.14, 4.31, .60, 1.70, .045),
    (1.25, .44, 1.04, 1.98, .044), (1.40, 2.72, .88, 2.13, .039),
    (1.55, 5.84, .77, 2.04, .032), (1.67, 1.09, .55, 2.15, .025),
    (1.76, 3.72, .76, 2.09, .024),
)


def direction(reach, azimuth, rise):
    return Vector((reach * math.cos(azimuth), reach * math.sin(azimuth), rise))


def fruit_pair(mesh, join, yaw, diameter, ripe, detail, *, display=False):
    start = len(mesh.vertices)
    outward = direction(1, yaw, 0)
    lateral = direction(1, yaw + math.pi * .5, 0)
    slot = 'Cherry/FruitReady' if ripe else 'Cherry/FruitForming'
    shape = ((-.53, .16), (-.47, .33), (-.23, .48), (.07, .50),
             (.30, .44), (.43, .29), (.30, .10))
    if not detail:
        shape = tuple(shape[i] for i in (0, 2, 3, 5))
    outward_reach = (.50, .67) if display else (.72, .98)
    spacing = .57 if display else .65
    for side, reach in zip((-1, 1), outward_reach):
        size = diameter * (.94 if side < 0 else 1)
        center = join + outward * (size * reach) + lateral * (side * diameter * spacing)
        if display:
            center.z = size * .53
        else:
            center.z -= diameter * (2.21 if side < 0 else 2.42)
        ring_shape(mesh, center, size, shape, slot, 10 if detail else 5)
        socket = center + UP * (size * .305)
        mid = socket.lerp(join, .57) + lateral * (side * diameter * .13)
        tube(mesh, [socket, mid, join], [diameter * .036, diameter * .030, diameter * .026],
             'GreenStem', 4 if detail else 3)
    mesh.anchor_from(start, join)


def seed():
    mesh = Geometry()
    profile = ((0, .23), (.10, .65), (.28, .90), (.51, 1), (.70, .91), (.88, .63), (1, .18))
    ring_shape(mesh, (0, 0, 0), .046, tuple((row, width * .45) for row, width in profile), 'StoneSeed', 18)
    for point in mesh.vertices:
        angle = math.atan2(point.y, point.x)
        groove = .19 * abs(math.cos(angle))**26
        point.x = point.x * (1 - groove) + .20 * point.z
        point.y *= .76 * (1 - groove)
    return mesh


def sprout():
    wood, leaves = Geometry(), Geometry()
    points = [(0, 0, 0), (-.011, 0, .087), (.009, -.008, .178), (.018, 0, .224)]
    tube(wood, *curved_axis(points, [.011, .008, .007, .004], 3), 'GreenStem', 7, closed=True)
    for anchor, heading, length, roll in (
        (points[2], (-.95, -.21, .57), .148, -.26),
        (points[3], (.83, -.38, .40), .155, .28),
    ):
        leaf_card('Cherry', leaves, Vector(anchor), Vector(heading), length, length * .63, roll, True)
    return [('Seedling stem', wood), ('First serrated leaves', leaves)]


def tree(young, detail):
    rng = random.Random(44921)
    vigor = Vector((.62, .62, .69)) if young else Vector((1, 1, 1))
    trunk = [Vector(point) * vigor for point in TRUNK]
    radii = [radius * (.61 if young else 1) for radius in RADII]
    spine, _ = curved_axis(trunk, radii, 8)
    branches = [(trunk, radii)]
    sprays = []
    fruit_sites = []
    for i, (fork, yaw, reach, tip, radius) in enumerate(BRANCHES[:6] if young else BRANCHES):
        target = fork * vigor.z
        lower, upper = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= target <= b.z)
        root = lower.lerp(upper, (target - lower.z) / (upper.z - lower.z))
        sweep = rng.uniform(-.22, .22)
        rise = tip - fork
        points = [root, root + direction(.13 * reach, yaw, rise * .33) * vigor,
                  root + direction(.48 * reach, yaw + sweep, rise * .68) * vigor,
                  root + direction(reach, yaw + sweep * 1.4, rise) * vigor]
        radius *= .68 if young else 1
        widths = [radius, radius * .82, radius * .51, radius * .19]
        branches.append((points, widths))
        guide, _ = curved_axis(points, widths, 6)
        for j, t in enumerate((.55, .75, .94) if young else (.41, .55, .69, .83, .96)):
            anchor, _ = sample_axis(guide, t)
            side = (-1)**(i + j)
            bearing = yaw + side * rng.uniform(.66, 1.49)
            end = anchor + direction(rng.uniform(.23, .34), bearing, rng.uniform(.055, .15)) * vigor
            stem = [anchor, anchor.lerp(end, .51) + UP * .042 * vigor.z, end]
            sprays.append((stem, bearing, 5 if young else 6, .009 * vigor.x, t))
            for h, part in enumerate((.69,) if young else (.55, .81)):
                junction, _ = sample_axis(stem, part)
                branch_yaw = bearing + (-1)**(i + h) * rng.uniform(.85, 1.57)
                point = junction + direction(rng.uniform(.13, .22), branch_yaw, rng.uniform(-.055, .082)) * vigor
                sprays.append(([junction, junction.lerp(point, .48), point], branch_yaw,
                               4 if young else 5, .005 * vigor.x, t))
            if not young and j >= 2:
                join, _ = sample_axis(stem, .67 + .058 * j)
                fruit_sites.append((join, bearing, rng.uniform(.063, .085)))
            if not young and j in (1, 2, 3):
                inner = bearing - side * 1.36
                tiplet = anchor + direction(.18, inner, .130 - .035 * j)
                sprays.append(([anchor, anchor.lerp(tiplet, .56) + UP * .025, tiplet], inner, 5, .006, t))
    for i, proportion in enumerate((.74, .82, .90, .96)):
        root, _ = sample_axis(spine, proportion)
        yaw = i * 2.40 + .74
        end = root + direction(.26 * vigor.x, yaw, .136 * vigor.z)
        middle = root.lerp(end, .56)
        sprays.append(([root, middle, end], yaw, 6, .009 * vigor.x, .91))
        for side in (-1, 1):
            bearing = yaw + side * 1.18
            tip = middle + direction(.17 * vigor.x, bearing, .032 * vigor.z)
            sprays.append(([middle, middle.lerp(tip, .52), tip], bearing, 4, .005 * vigor.x, .87))
    if not young:
        for i, proportion in enumerate((.59, .63, .68, .73, .77)):
            root, _ = sample_axis(spine, proportion)
            yaw = .92 + i * 2.40
            tip = root + direction(.34 + .03 * (i % 2), yaw, .18)
            mid = root.lerp(tip, .51) + UP * .054
            sprays.append(([root, mid, tip], yaw, 6, .008, .68))
            for side in (-1, 1):
                fork = yaw + side * 1.12
                end = mid + direction(.17, fork, -.036)
                sprays.append(([mid, mid.lerp(end, .57), end], fork, 4, .004, .68))
    wood = fused_wood(branches, detail, young, root_scale=.48 if young else .69)
    foliage, green, ripe = Geometry(), Geometry(), Geometry()
    for i, (curve, yaw, count, radius, spread) in enumerate(sprays):
        if radius >= .008 or (detail and (spread >= .83 or i % 3 == 0)):
            tube(wood, curve, [radius, radius * .59, radius * .23], 'Twig', 4 if detail else 3)
        positions = (.39, .77) if count > 4 else (.59,)
        for j, portion in enumerate(positions):
            base, _ = sample_axis(curve, portion + rng.uniform(-.048, .048))
            bearing = yaw + (-1)**(i + j) * rng.uniform(.44, 1.17)
            pitch = rng.uniform(-.75, .51)
            length = rng.uniform(.32, .455) * (.76 if young else 1)
            width = length * rng.uniform(.86, 1.02)
            roll = rng.uniform(-.83, .83)
            accent = rng.random() < .11 + .26 * (base.z / vigor.z - 1.03) + .11 * spread
            if not detail and j == 1 and i % 3 == 0:
                continue
            leaf_card('Cherry', foliage, base, direction(1, bearing, pitch), length,
                      width, roll, accent, spray=True, detail=detail)
    for join, yaw, size in fruit_sites:
        fruit_pair(green, join, yaw, size * .81, False, detail)
        fruit_pair(ripe, join, yaw, size * 1.17, True, detail)
    print(f'Cherry {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{len(foliage.anchors)} leaf sprays, {len(fruit_sites)} pairs', flush=True)
    return wood, foliage, green, ripe


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=sides, root_scale=.69, uv_span=(1, .81))
    cap = mesh.vertices[-sides:]
    uv = [(.5 + .46 * math.cos(i * math.tau / sides), .5 + .46 * math.sin(i * math.tau / sides))
          for i in range(sides)]
    mesh.add(cap, [(tuple(range(sides)), uv)], 'CutWood')
    return mesh


def produce():
    mesh = Geometry()
    join = Vector((0, 0, .169))
    fruit_pair(mesh, join, -.93, .076, True, True, display=True)
    leaf_card('Cherry', mesh, join, direction(1, .42, -.26), .098, .060, .33, False)
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
    stage('NP_Cherry_Seed', [('Cherry pit', seed())], .10, material, False)
    stage('NP_Cherry_Sprout', sprout(), .26, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        trunk, leaves, green, ripe = tree(False, detail)
        young_trunk, young_leaves, _, _ = tree(True, detail)
        crown = (trunk, leaves, green, ripe)
        adjustment = height_adjustment(crown)
        adjust_height((*crown, young_trunk, young_leaves), adjustment)
        stage('NP_Cherry_Young' + suffix, [('Young trunk', young_trunk), ('Young crown', young_leaves)],
              1, material, False, shade=shade_cherry)
        foliage = [('Cherry trunk and twigs', trunk), ('Cherry leaf sprays', leaves)]
        stage('NP_Cherry_FruitForming' + suffix, foliage + [('Green cherry pairs', green)],
              1, material, False, shade=shade_cherry)
        stage('NP_Cherry_HarvestReady' + suffix, foliage + [('Ripe cherry pairs', ripe)],
              1, material, detail, shade=shade_cherry)
    stage('NP_Cherry_Stump', [('Cherry stump', stump())], 1, material, False)
    stage('NP_Cherry_Produce', [('Cherry pair with leaf', produce())], 1, material, False)
    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT * .51)
    aim(scene.camera, (3.6, -7.0, 3.56), focus)
    scene.camera.data.ortho_scale = 3.90
    scene.render.resolution_x, scene.render.resolution_y = 1120, 1060
    scene.render.filepath = '//'
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                viewport = area.spaces.active
                viewport.region_3d.view_location = focus
                viewport.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
                viewport.region_3d.view_distance = 5.1
                viewport.shading.color_type = 'TEXTURE'
    for node in material.node_tree.nodes:
        if node.type == 'TEX_IMAGE':
            image = TEXTURES / Path(node.image.filepath).name
            node.image.filepath = bpy.path.relpath(str(image), start=str(SOURCE / 'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Cherry.blend'), relative_remap=False)
    print('CHERRY_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
