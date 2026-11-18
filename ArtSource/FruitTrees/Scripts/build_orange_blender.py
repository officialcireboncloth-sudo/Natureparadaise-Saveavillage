"""Build the orchard orange tree, including its five in-field growth stages."""
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
    Geometry, GOLDEN_ANGLE, UP, adjust_height, aim, curved_axis, fused_wood, height_adjustment,
    material_atlas, ring_shape, sample_axis, setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import (
    ATLAS, ORANGE_PEEL_WIDTH, ORANGE_SECTION_CENTER, ORANGE_SECTION_RADIUS, ORANGE_SEED_RECT,
    SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays


WORK = Path(os.environ['NP_ORANGE_STAGE'])
SEED_SCALE = .10
SPROUT_SCALE = .26
SHADE_ORANGE = partial(shade_sprays, species='Orange')
TRUNK = ((0, 0, -.022), (.006, -.010, .16), (.018, -.033, .43), (.001, -.044, .74),
         (-.024, -.021, 1.05), (-.043, .027, 1.38), (-.026, .064, 1.69),
         (.012, .060, 1.99), (.037, .035, 2.24))
RADII = (.151, .122, .091, .075, .063, .050, .034, .019, .005)

# Each limb has a clear fork and its own group of rounded citrus sprays.
# Fork height, bearing, reach, tip height, and base radius.
SCAFFOLDS = (
    (.67, 5.23, .95, 1.44, .070), (.76, 3.12, .95, 1.49, .065),
    (.86, 1.18, .95, 1.60, .057), (.99, 6.06, .98, 1.75, .051),
    (1.13, 2.24, .84, 1.89, .046), (1.20, 4.26, .85, 1.93, .043),
    (1.35, .46, .87, 2.05, .039), (1.46, 3.39, .70, 2.09, .034),
    (1.56, 5.63, .63, 2.22, .028), (1.70, 1.56, .58, 2.24, .025),
    (1.79, 3.95, .40, 2.30, .020),
)
ORANGE_PROFILE = ((-.92, .13), (-.89, .40), (-.74, .68), (-.48, .88), (-.12, .996),
                  (.23, .968), (.53, .83), (.76, .61), (.86, .35), (.84, .12))


def direction(reach, bearing, rise):
    return Vector((reach * math.cos(bearing), reach * math.sin(bearing), rise))


def seed():
    pit = Geometry()
    profile = ((0, .22), (.11, .43), (.33, .50), (.52, .44), (.74, .32), (.9, .17), (1, .034))
    ring_shape(pit, (0, 0, 0), .053, profile, 'Orange/FruitReady', 16)
    for point in pit.vertices:
        point.x += point.z * .21
        point.y *= .72
    left, top, right, bottom = ORANGE_SEED_RECT
    pit.surfaces = [
        (slot, [((left + (right - left) * u) / 256, (top + (bottom - top) * v) / 256)
                for u, v in uv])
        for slot, uv in pit.surfaces
    ]
    return pit


def sprout():
    wood, leaves = Geometry(), Geometry()
    stem = [(0, 0, 0), (-.012, .003, .08), (.004, -.006, .173), (.013, -.002, .229)]
    tube(wood, *curved_axis(stem, [.010, .0075, .006, .0038], 3), 'GreenStem', 8, closed=True)
    for base, bearing, rise, length, roll in (
        (stem[2], 3.45, .55, .151, .58),
        (stem[3], 5.95, .62, .165, -.60),
    ):
        leaf_card('Orange', leaves, Vector(base), direction(1, bearing, rise),
                  length, length * .68, roll, True, bend=.22)
    return [('Citrus seedling stem', wood), ('First oval leaves', leaves)]


def orange(mesh, center, radius, ripe, detail):
    profile = ORANGE_PROFILE if detail else tuple(ORANGE_PROFILE[i] for i in (0, 2, 4, 6, 8, 9))
    start = len(mesh.surfaces)
    slot = 'Orange/FruitReady' if ripe else 'Orange/FruitForming'
    ring_shape(mesh, center, radius, profile, slot, 14 if detail else 9)
    if ripe:
        mesh.surfaces[start:] = [(slot, [(u * ORANGE_PEEL_WIDTH, v) for u, v in uv])
                                 for slot, uv in mesh.surfaces[start:]]


def hanging_fruit(mesh, anchor, bearing, radius, ripe, detail):
    start = len(mesh.vertices)
    socket = anchor + direction(.148, bearing, -.144)
    center = socket - UP * (radius * .84)
    orange(mesh, center, radius, ripe, detail)
    tube(mesh, [anchor, anchor.lerp(socket, .50) + UP * .019, socket],
         [.006, .0048, .0033], 'GreenStem', 5 if detail else 4)
    # A small green calyx at the stem socket reads above each round fruit.
    for index in range(5):
        yaw = bearing + index * math.tau / 5
        points = [socket, socket + direction(radius * .15, yaw - .45, .001),
                  socket + direction(radius * .28, yaw, -.007), socket + direction(radius * .15, yaw + .45, .001)]
        mesh.add(points, [((0, 1, 2, 3), ((.49, .58), (.46, .50), (.50, .44), (.54, .50)))], 'GreenStem')
    mesh.anchor_from(start, anchor)


def tree(young, detail):
    rng = random.Random(58244 if young else 60732)
    scale = Vector((.62, .62, .73)) if young else Vector((1, 1, 1))
    trunk = [Vector(point) * scale for point in TRUNK]
    radii = [radius * (.56 if young else 1) for radius in RADII]
    spine, _ = curved_axis(trunk, radii, 8)
    branches = [(trunk, radii)]
    shoots, fruit_sites = [], []
    for index, (fork, bearing, reach, tip, radius) in enumerate(SCAFFOLDS[:7] if young else SCAFFOLDS):
        height = fork * scale.z
        lower, upper = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= height <= b.z)
        base = lower.lerp(upper, (height - lower.z) / (upper.z - lower.z))
        turn = rng.uniform(-.20, .20)
        rise = tip - fork
        axis = [base, base + direction(.18 * reach, bearing, .41 * rise) * scale,
                base + direction(.58 * reach, bearing + turn, .79 * rise) * scale,
                base + direction(reach, bearing + turn, rise) * scale]
        width = radius * (.66 if young else 1)
        widths = [width, width * .82, width * .51, width * .17]
        branches.append((axis, widths))
        guide, _ = curved_axis(axis, widths, 5)
        fractions = (.54, .75, .95) if young else (.46, .65, .79, .91, .995)
        for step, fraction in enumerate(fractions):
            root, _ = sample_axis(guide, fraction)
            yaw = bearing + (-1)**(index + step) * rng.uniform(.57, 1.19)
            end = root + direction(rng.uniform(.19, .31), yaw, rng.uniform(.038, .18)) * scale
            mid = root.lerp(end, .54) + UP * (.039 * scale.z)
            shoots.append(([root, mid, end], yaw, .0085 * scale.x, fraction))
            if not young or step > 0:
                fork_yaw = yaw + (-1)**step * rng.uniform(.76, 1.29)
                fork_end = mid + direction(rng.uniform(.13, .22), fork_yaw, rng.uniform(-.05, .11)) * scale
                shoots.append(([mid, mid.lerp(fork_end, .48), fork_end], fork_yaw, .0048 * scale.x, fraction))
        if not young:
            anchor, _ = sample_axis(guide, .96)
            fruit_sites.append((anchor, bearing + turn, rng.uniform(.110, .145)))
            if index in (2, 5, 7):
                lower_site = axis[-2].lerp(axis[-1], .32)
                fruit_sites.append((lower_site, bearing + .66, rng.uniform(.090, .112)))

    # Set upward sprays near the leader to keep the dome full above the outer branches.
    for index, fraction in enumerate((.69, .78, .86, .92, .97, .997)):
        root, _ = sample_axis(spine, fraction)
        yaw = .4 + index * GOLDEN_ANGLE
        end = root + direction((.32 - .026 * (index % 3)) * scale.x, yaw, .13 * scale.z)
        mid = root.lerp(end, .58) + UP * (.041 * scale.z)
        shoots.append(([root, mid, end], yaw, .0068 * scale.x, .88))
        for side in (-1, 1):
            fork_yaw = yaw + side * 1.14
            fork_end = mid + direction(.174 * scale.x, fork_yaw, .024 * scale.z)
            shoots.append(([mid, mid.lerp(fork_end, .50), fork_end], fork_yaw, .004 * scale.x, .84))

    wood = fused_wood(branches, detail, young, root_scale=.43 if young else .64)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    for index, (axis, bearing, width, fraction) in enumerate(shoots):
        if width >= .0065 or (detail and index % 2 == 0):
            tube(wood, axis, [width, width * .56, width * .17], 'Twig', 4 if detail else 3)
        for order, at in enumerate((.32, .74)):
            root, _ = sample_axis(axis, at)
            yaw = bearing + (-1)**(index + order) * rng.uniform(.49, 1.10)
            pitch = rng.uniform(-.24, .48)
            length = rng.uniform(.32, .46) * (.73 if young else 1)
            roll = rng.uniform(-.70, .70)
            light = .18 + (root.z / scale.z - 1.36) * .18 + fraction * .16
            accent = rng.random() < light
            if detail or order == 0 or width > .0065 or index % 3 == 1:
                leaf_card('Orange', leaves, root, direction(1, yaw, pitch), length, length * .95,
                          roll, accent, spray=True, detail=detail, bend=.24)
        if width > .0065 or fraction > .84:
            # Folded individual blades break up the outline in front of the smaller sprays.
            root = axis[-2].lerp(axis[-1], .81)
            for order, turn in enumerate((-.77, .86)):
                length = rng.uniform(.19, .28) * (.75 if young else 1)
                pitch = rng.uniform(-.26, .42)
                roll = rng.uniform(-.61, .61)
                if detail or order == 1 or index % 2 == 0:
                    leaf_card('Orange', leaves, root, direction(1, bearing + turn, pitch), length, length * .72,
                              roll, index % 3 == 0, detail=detail, bend=.30)

    # Connect the lower and upper crown with sprays sheltered between the limbs.
    for index, (axis, _) in enumerate(branches[1:]):
        positions = ((.74, (-1.27, .45, 2.63)),)
        if not young and 2 <= index <= 8:
            positions += ((.89, (-1.63, -.71, .68, 1.62)),)
        for fraction, bearings in positions:
            root, heading = sample_axis(axis, fraction)
            yaw = math.atan2(heading.y, heading.x)
            for turn in bearings:
                direction_to_tip = direction(1, yaw + turn, rng.uniform(-.59, .14))
                length = rng.uniform(.36, .48) * (.73 if young else 1)
                leaf_card('Orange', leaves, root, direction_to_tip, length, length * .98, rng.uniform(-.52, .52),
                          False, spray=True, detail=detail, bend=.29)

    for anchor, bearing, radius in fruit_sites:
        hanging_fruit(green, anchor, bearing, radius * .78, False, detail)
        hanging_fruit(ripe, anchor, bearing, radius, True, detail)
    print(f'Orange {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{len(leaves.anchors)} leaf cards, {len(fruit_sites)} fruit sites', flush=True)
    return wood, leaves, green, ripe


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=sides, root_scale=.64, uv_span=(1, .81))
    cap_uv = [(.5 + .46 * math.cos(i * math.tau / sides), .5 + .46 * math.sin(i * math.tau / sides))
              for i in range(sides)]
    mesh.add(mesh.vertices[-sides:], [(tuple(range(sides)), cap_uv)], 'CutWood')
    return mesh


def cut_half(mesh, center):
    front = Vector((.31, -.84, .51)).normalized()
    across = UP.cross(front).normalized()
    rise = front.cross(across).normalized()
    radius, sides = .106, 24
    points, faces = [], []
    rings = ((-.096, .14), (-.086, .42), (-.065, .71), (-.032, .938), (0, 1))
    for depth, width in rings:
        for index in range(sides):
            angle = index * math.tau / sides
            points.append(center + front * depth + width * radius * (across * math.cos(angle)
                          + rise * (.96 * math.sin(angle))))
    for row in range(len(rings) - 1):
        for index in range(sides):
            left = row * sides + index
            right = row * sides + (index + 1) % sides
            u, v = index * ORANGE_PEEL_WIDTH / sides, 1 - row * .25
            u_next = (index + 1) * ORANGE_PEEL_WIDTH / sides
            faces.append(((left, right, right + sides, left + sides),
                          ((u, v), (u_next, v), (u_next, v - .25), (u, v - .25))))
    faces.append((tuple(reversed(range(sides))), [(.24, .92)] * sides))
    mesh.add(points, faces, 'Orange/FruitReady')
    x, y = ORANGE_SECTION_CENTER
    uv = [((x + ORANGE_SECTION_RADIUS * math.cos(index * math.tau / sides)) / 256,
           (y - ORANGE_SECTION_RADIUS * math.sin(index * math.tau / sides)) / 256)
          for index in range(sides)]
    mesh.add(points[-sides:], [(tuple(range(sides)), uv)], 'Orange/FruitReady')


def produce():
    mesh = Geometry()
    center = Vector((-.095, .043, .105))
    radius = .112
    orange(mesh, center, radius, True, True)
    base = center + UP * (radius * .84)
    top = base + Vector((.017, -.003, .038))
    tube(mesh, [base, base.lerp(top, .54), top], [.008, .006, .004], 'GreenStem', 6)
    leaf_card('Orange', mesh, top, direction(1, .51, -.31), .124, .076, -.14, True)
    cut_half(mesh, Vector((.080, -.061, .100)))
    floor = min(point.z for point in mesh.vertices)
    for point in mesh.vertices:
        point.z -= floor
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
    stage('NP_Orange_Seed', [('Cream citrus seed', seed())], SEED_SCALE, material, False)
    stage('NP_Orange_Sprout', sprout(), SPROUT_SCALE, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        wood, leaves, green, ripe = tree(False, detail)
        young_wood, young_leaves, _, _ = tree(True, detail)
        crown = (wood, leaves, green, ripe)
        adjust_height((*crown, young_wood, young_leaves), height_adjustment(crown))
        stage('NP_Orange_Young' + suffix, [('Young wood', young_wood), ('Young oval leaves', young_leaves)],
              1, material, False, shade=SHADE_ORANGE)
        foliage = [('Orange trunk and limbs', wood), ('Citrus leaf sprays and folded blades', leaves)]
        stage('NP_Orange_FruitForming' + suffix, foliage + [('Green oranges', green)],
              1, material, False, shade=SHADE_ORANGE)
        stage('NP_Orange_HarvestReady' + suffix, foliage + [('Ripe oranges', ripe)],
              1, material, detail, shade=SHADE_ORANGE)
    stage('NP_Orange_Stump', [('Orange stump', stump())], 1, material, False)
    stage('NP_Orange_Produce', [('Whole orange and sliced half', produce())], 1, material, False)

    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT * .50)
    aim(scene.camera, (3.8, -7.0, 4.02), focus)
    scene.camera.data.ortho_scale = 3.90
    scene.render.resolution_x, scene.render.resolution_y = 1080, 1060
    scene.render.filepath = '//'
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                view = area.spaces.active
                view.region_3d.view_location = focus
                view.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
                view.region_3d.view_distance = 5.1
                view.shading.color_type = 'TEXTURE'
    for node in material.node_tree.nodes:
        if node.type == 'TEX_IMAGE':
            image = TEXTURES / Path(node.image.filepath).name
            node.image.filepath = bpy.path.relpath(str(image), start=str(SOURCE / 'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Orange.blend'), relative_remap=False)
    print('ORANGE_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
