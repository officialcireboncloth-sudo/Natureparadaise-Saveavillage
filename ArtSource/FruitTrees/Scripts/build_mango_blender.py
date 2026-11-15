"""Build mango growth stages with curved, lanceolate cutout foliage."""
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
from fruit_tree_atlas import (
    ATLAS, MANGO_PEEL_WIDTH, MANGO_SECTION_CENTER, MANGO_SECTION_RADIUS,
    MANGO_SEED_CENTER, MANGO_SEED_RADII, SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays


WORK = Path(os.environ['NP_MANGO_STAGE'])
SHADE_MANGO = partial(shade_sprays, species='Mango')
TRUNK = ((0, 0, -.023), (-.034, -.017, .16), (-.051, -.048, .46), (-.046, -.069, .76),
         (-.018, -.054, 1.07), (.047, -.009, 1.40), (.074, .028, 1.68),
         (.032, .061, 2.00), (-.070, .095, 2.29))
RADII = (.145, .108, .088, .072, .062, .050, .030, .017, .005)
# Fork height, bearing, horizontal reach, terminal height, and base radius.
BRANCHES = (
    (.70, 4.48, .95, 1.72, .063), (.84, 2.78, .96, 1.81, .060),
    (.99, 5.95, .94, 1.85, .056), (1.12, 1.35, .99, 1.94, .050),
    (1.31, 3.53, .87, 2.17, .046), (1.42, .35, .89, 2.26, .039),
    (1.54, 4.82, .82, 2.27, .034), (1.70, 2.18, .82, 2.35, .027),
    (1.86, 5.99, .76, 2.37, .023),
)
# The broad shoulder sits toward the stalk. The narrow tip curves away from the hanging axis.
FRUIT_PROFILE = ((-1, .10), (-.96, .235), (-.85, .365), (-.72, .426), (-.54, .49),
                 (-.36, .506), (-.19, .467), (-.067, .318), (0, .110))


def direction(span, bearing, rise):
    return Vector((span * math.cos(bearing), span * math.sin(bearing), rise))


def blade(mesh, base, bearing, pitch, length, roll, accent, detail, *, spray=False, width=None):
    leaf_card('Mango', mesh, base, direction(1, bearing, pitch), length,
              width if width is not None else length * .43, roll, accent,
              spray=spray, detail=detail, bend=.52 if spray else .66, lift=.18 if spray else .23)


def fruit(mesh, socket, length, bearing, ripe, detail, *, sides=None):
    vertex_start, surface_start = len(mesh.vertices), len(mesh.surfaces)
    profile = FRUIT_PROFILE if detail else tuple(FRUIT_PROFILE[i] for i in (0, 2, 4, 6, 8))
    slot = 'Mango/FruitReady' if ripe else 'Mango/FruitForming'
    ring_shape(mesh, socket, length, profile, slot, sides or (14 if detail else 8))
    axis = direction(1, bearing, 0)
    side = UP.cross(axis)
    for point in mesh.vertices[vertex_start:]:
        local = point - socket
        t = -local.z / length
        hook = length * (t * .075 + .255 * t**3)
        point[:] = socket + UP * local.z + axis * (local.x + hook) + side * local.y * .76
    if ripe:
        mesh.surfaces[surface_start:] = [(surface, [(u * MANGO_PEEL_WIDTH, v) for u, v in uv])
                                         for surface, uv in mesh.surfaces[surface_start:]]


def seed():
    # A broad, compressed pit with the fibers running along its long axis.
    points, faces, uv = [], [], []
    sides = 20
    x, y = MANGO_SEED_CENTER
    rx, ry = MANGO_SEED_RADII
    bands = ((-.010, .24), (-.009, .69), (-.003, 1), (.007, .99), (.014, .72), (.017, .26))
    for z, radius in bands:
        for i in range(sides):
            a = math.tau * i / sides
            u, v = math.cos(a), math.sin(a)
            ripple = 1 + .033 * math.cos(a * 5 + 1.1)
            points.append((.044 * u * radius * ripple, .023 * v * radius * (1 - .19 * u),
                           z + u * .074 * radius * .27 + .010))
            uv.append(((x + u * radius * rx) / 256, (y - v * radius * ry) / 256))
    for row in range(len(bands) - 1):
        for i in range(sides):
            a, b = row * sides + i, row * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.extend((tuple(reversed(range(sides))), tuple(range(len(points) - sides, len(points)))))
    floor = min(p[2] for p in points)
    mesh = Geometry()
    mesh.add([(x, y, z - floor) for x, y, z in points], [(face, [uv[i] for i in face]) for face in faces],
             'Mango/FruitReady')
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0, 0, .021), (-.012, .005, .084), (.003, .013, .178), (.018, .011, .240)]
    tube(stem, *curved_axis(points, [.008, .007, .005, .0025], 4), 'GreenStem', 7, closed=True)
    for base, bearing, pitch, length, roll, accent in (
        (points[2], 3.53, .67, .156, .91, False),
        (points[3], 5.88, .95, .166, .52, False),
        (points[3], 1.75, 1.37, .080, -.55, True),
    ):
        leaf_card('Mango', leaves, Vector(base), direction(1, bearing, pitch), length,
                  length * .47, roll, accent, bend=.43, lift=.18)
    return [('Fibrous mango pit', seed()), ('Sprout stem', stem), ('First lanceolate leaves', leaves)]


def tree(young, detail):
    rng = random.Random(31940)
    scale = Vector((.58, .58, .686)) if young else Vector((1, 1, 1))
    trunk = [Vector(point) * scale for point in TRUNK]
    radii = [radius * (.60 if young else 1) for radius in RADII]
    spine, _ = curved_axis(trunk, radii, 8)
    branches, shoots, crop = [(trunk, radii)], [], []
    for i, (fork, bearing, reach, tip, radius) in enumerate(BRANCHES[:6] if young else BRANCHES):
        height = fork * scale.z
        a, b = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= height <= b.z)
        base = a.lerp(b, (height - a.z) / (b.z - a.z))
        turn = rng.uniform(-.21, .21)
        climb = tip - fork
        axis = (base, base + direction(reach * .22, bearing, climb * .42) * scale,
                base + direction(reach * .58, bearing + turn, climb * .73) * scale,
                base + direction(reach, bearing + turn * 1.4, climb) * scale)
        widths = [radius * factor * (.70 if young else 1) for factor in (1, .76, .47, .13)]
        branches.append((axis, widths))
        guide, _ = curved_axis(axis, widths, 6)
        for j, at in enumerate((.60, .82, .99) if young else (.42, .59, .75, .88, .985)):
            root, _ = sample_axis(guide, at)
            side = (-1)**(j + i)
            yaw = bearing + side * rng.uniform(.61, 1.35)
            end = root + direction(rng.uniform(.22, .31), yaw, rng.uniform(-.045, .14)) * scale
            middle = root.lerp(end, .49) + UP * .038 * scale.z
            shoots.append(([root, middle, end], yaw, .009, at, j >= 2))
            if not young or at > .7:
                fork_yaw = bearing - side * rng.uniform(.30, .87)
                side_tip = middle + direction(rng.uniform(.14, .235), fork_yaw, rng.uniform(-.10, .06)) * scale
                shoots.append(([middle, middle.lerp(side_tip, .51), side_tip], fork_yaw, .005, at, at > .7))
            if not young and (j == 4 or (j == 3 and i in (0, 2, 6, 8))):
                crop.append((middle.lerp(end, .77), yaw, rng.uniform(.165, .212)))

    # Crown leaders fan out horizontally, with lower outward fans overhanging the forks.
    for i, at in enumerate((.61, .70, .78, .84, .89, .93, .97, .998)):
        base, _ = sample_axis(spine, at)
        yaw = .8 + 2.4 * i
        reach = .29 if young else .40 + .025 * (i % 4)
        tip = base + direction(reach, yaw, .10 if i % 2 else .14) * scale
        middle = base.lerp(tip, .53) + UP * .039 * scale.z
        shoots.append(([base, middle, tip], yaw, .009, at, True))
        for side in ((1,) if young else (-1, 1)):
            side_yaw = yaw + side * 1.23
            side_tip = middle + direction(.235, side_yaw, -.065) * scale
            shoots.append(([middle, middle.lerp(side_tip, .54), side_tip], side_yaw, .005, at, True))

    bridge_start = len(shoots)
    if not young:
        # Oblique side shoots bridge the upper tiers into one broad dome.
        for i in range(6):
            at = .86 + .028 * (i % 3)
            base, _ = sample_axis(spine, at)
            yaw = .42 + math.tau * i / 6
            tip = base + direction(.72 + .046 * (i % 2), yaw, .16 + .051 * (i % 3))
            middle = base.lerp(tip, .47) + UP * .058
            shoots.append(([base, middle, tip], yaw, .009, at, True))

    wood = fused_wood(branches, detail, young, root_scale=.43 if young else .67)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    for i, (twig, bearing, radius, at, outer) in enumerate(shoots):
        if radius >= .008 or (detail and (i % 3 == 0 or at > .78)):
            root_radius = radius * scale.x
            tube(wood, twig, [root_radius, root_radius * .56, root_radius * .19], 'Twig', 4 if detail else 3)
        terminal = twig[-2].lerp(twig[-1], .74)
        copper = at > .77 and rng.random() < (.18 if young else .065)
        # The tops of these fans fill the shoulders while their tips arc back down.
        inclination = .44 if i >= bridge_start else 0
        for j, offset in enumerate((-.59, .63)):
            length = rng.uniform(.43, .56) * (.79 if young else 1)
            yaw = bearing + offset + rng.uniform(-.22, .22)
            pitch = rng.uniform(-.24, .27) - (.23 if outer else 0) + inclination
            roll = rng.uniform(-.83, .83)
            if detail or j == 0 or i % 3 == 0:
                blade(leaves, twig[-2].lerp(terminal, .32 + .54 * j), yaw, pitch, length, roll,
                      copper and j == 0, detail, spray=True, width=length * .92)
        # Long individual blades overhang the terminal sprays and keep the species visible in both LODs.
        for j, turn in enumerate((-.94, .12, 1.16) if outer else (-.52, .75)):
            length = rng.uniform(.35, .49) * (.77 if young else 1)
            pitch = rng.uniform(-.49, .15) - (.24 if outer else 0) + inclination
            roll = rng.uniform(-.80, .80)
            if detail or j == 1 or (outer and i % 3 == 1):
                blade(leaves, terminal, bearing + turn, pitch, length, roll, copper, detail)

    for i, (join, bearing, length) in enumerate(crop):
        socket = join + direction(.116, bearing, -.235 - .041 * (i % 3))
        for mesh, ready, size in ((green, False, .78), (ripe, True, 1.15)):
            start = len(mesh.vertices)
            fruit(mesh, socket, length * size, bearing + .9 * (-1)**i, ready, detail)
            tube(mesh, [join, join.lerp(socket, .55) + direction(.022, bearing, .014), socket],
                 [.0049, .0041, .003], 'GreenStem', 5 if detail else 4)
            mesh.anchor_from(start, join)
    print(f'Mango {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{len(leaves.anchors)} leaf cards, {len(crop)} fruit positions', flush=True)
    return wood, leaves, green, ripe


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=sides, root_scale=.67, uv_span=(1, .81))
    points = mesh.vertices[-sides:]
    uv = [(.5 + .46 * math.cos(i * math.tau / sides), .5 + .46 * math.sin(i * math.tau / sides))
          for i in range(sides)]
    mesh.add(points, [(tuple(range(sides)), uv)], 'CutWood')
    return mesh


def cheek(mesh, center):
    front = Vector((.23, -.49, .84)).normalized()
    across = UP.cross(front).normalized()
    rise = front.cross(across).normalized()
    sides = 24
    rings = ((-.051, .14), (-.046, .46), (-.032, .77), (-.014, .93), (0, 1))
    points, faces = [], []
    for depth, scale in rings:
        for i in range(sides):
            angle = math.tau * i / sides
            x, y = math.cos(angle), math.sin(angle)
            points.append(center + front * depth + scale * (across * (.058 * x * (1 + .24 * y) + .024 * y**2)
                                                            + rise * y * .089))
    for row in range(len(rings) - 1):
        for i in range(sides):
            a, b = row * sides + i, row * sides + (i + 1) % sides
            u, u2 = i * MANGO_PEEL_WIDTH / sides, (i + 1) * MANGO_PEEL_WIDTH / sides
            v, v2 = 1 - row * .25, 1 - (row + 1) * .25
            faces.append(((a, b, b + sides, a + sides), ((u, v), (u2, v), (u2, v2), (u, v2))))
    faces.append((tuple(reversed(range(sides))), [(.24, .97)] * sides))
    mesh.add(points, faces, 'Mango/FruitReady')
    x, y = MANGO_SECTION_CENTER
    radius = MANGO_SECTION_RADIUS
    uv = [((x + radius * math.cos(i * math.tau / sides)) / 256,
           (y - radius * math.sin(i * math.tau / sides)) / 256) for i in range(sides)]
    mesh.add(points[-sides:], [(tuple(range(sides)), uv)], 'Mango/FruitReady')


def produce():
    mesh = Geometry()
    socket = Vector((-.071, .028, .181))
    fruit(mesh, socket, .180, 3.47, True, True, sides=22)
    tip = socket + Vector((.015, .01, .027))
    tube(mesh, [socket, socket.lerp(tip, .50), tip], [.006, .005, .0038], 'Twig', 6)
    blade(mesh, tip, .45, -.18, .146, -.18, False, True)
    cheek(mesh, Vector((.073, -.043, .063)))
    floor = min(point.z for point in mesh.vertices)
    for point in mesh.vertices:
        point.z -= floor
    return mesh


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    mesh_dir = WORK / 'Mesh'
    mesh_dir.mkdir(parents=True, exist_ok=True)
    atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(atlas)
    configure_cutout(material)
    unity_mesh.configure(material, mesh_dir)
    stage('NP_Mango_Seed', [('Fibrous mango pit', seed())], .10, material, False)
    stage('NP_Mango_Sprout', sprout(), .26, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        wood, leaves, green, ripe = tree(False, detail)
        young_wood, young_leaves, _, _ = tree(True, detail)
        crown = (wood, leaves, green, ripe)
        adjust_height((*crown, young_wood, young_leaves), height_adjustment(crown))
        stage('NP_Mango_Young' + suffix,
              [('Young wood', young_wood), ('Young mango leaf fans', young_leaves)],
              1, material, False, shade=SHADE_MANGO)
        foliage = [('Mango trunk and shoots', wood), ('Mango leaf fans and hanging blades', leaves)]
        stage('NP_Mango_FruitForming' + suffix, foliage + [('Green mangoes', green)],
              1, material, False, shade=SHADE_MANGO)
        stage('NP_Mango_HarvestReady' + suffix, foliage + [('Golden mangoes', ripe)],
              1, material, detail, shade=SHADE_MANGO)
    stage('NP_Mango_Stump', [('Mango stump', stump())], 1, material, False)
    stage('NP_Mango_Produce', [('Whole mango and orange cheek', produce())], 1, material, False)
    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT * .50)
    aim(scene.camera, (3.6, -7.0, 3.56), focus)
    scene.camera.data.ortho_scale = 3.91
    scene.render.resolution_x, scene.render.resolution_y = 1120, 1060
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
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Mango.blend'), relative_remap=False)
    print('MANGO_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
