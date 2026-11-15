"""Build pink-fleshed guava for the orchard atlas and Unity growth stages."""
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
    ATLAS, GUAVA_PEEL_WIDTH, GUAVA_SECTION_CENTER, GUAVA_SECTION_RADIUS, SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays


WORK = Path(os.environ['NP_GUAVA_STAGE'])
shade_guava = partial(shade_sprays, species='Guava')
TRUNK = ((0, 0, -.023), (.010, -.012, .15), (.012, -.043, .42), (-.015, -.064, .71),
         (-.068, -.027, 1.02), (-.098, .033, 1.34), (-.116, .085, 1.64),
         (-.080, .123, 1.92), (.003, .124, 2.17))
RADII = (.139, .107, .085, .068, .053, .042, .030, .015, .005)
# Low forks fill the broad shoulders, with shorter leaders toward the crown's center.
BRANCHES = (
    (.57, 5.68, 1.07, 1.35, .069), (.73, 3.28, 1.02, 1.46, .062),
    (.85, 1.56, 1.03, 1.56, .053), (1.02, 4.12, .95, 1.68, .047),
    (1.15, .42, .91, 1.79, .042), (1.25, 2.49, .87, 1.87, .035),
    (1.43, 5.43, .78, 1.98, .029), (1.57, 1.15, .71, 2.06, .025),
    (1.67, 3.76, .62, 2.12, .021),
)
FRUIT_PROFILE = ((-1, .15), (-.96, .32), (-.84, .47), (-.65, .51),
                 (-.43, .48), (-.24, .39), (-.09, .26), (0, .076))


def direction(span, bearing, rise):
    return Vector((span * math.cos(bearing), span * math.sin(bearing), rise))


def fruit(mesh, socket, size, ripe, detail):
    start = len(mesh.surfaces)
    slot = 'Guava/FruitReady' if ripe else 'Guava/FruitForming'
    profile = FRUIT_PROFILE if detail else tuple(FRUIT_PROFILE[i] for i in (0, 1, 3, 5, 7))
    ring_shape(mesh, socket, size, profile, slot, 12 if detail else 8)
    if ripe:
        mesh.surfaces[start:] = [(surface, [(u * GUAVA_PEEL_WIDTH, v) for u, v in uv])
                                 for surface, uv in mesh.surfaces[start:]]
    # Five small sepals sit in the dimple opposite the stem.
    base = socket - UP * size * .991
    tips, faces = [], []
    for i in range(5):
        bearing = math.tau * i / 5 + .5
        a = 4 * i
        tips.extend((base + UP * size * .02,
                     base + direction(size * .114, bearing - .45, -.045 * size),
                     base + direction(size * .175, bearing, -.080 * size),
                     base + direction(size * .114, bearing + .45, -.036 * size)))
        faces.append(((a, a + 3, a + 2, a + 1), ((.46,.60),(.42,.52),(.51,.46),(.58,.52))))
    mesh.add(tips, faces, 'Twig')


def section(mesh, center):
    front = Vector((.33, -.78, .53)).normalized()
    across = UP.cross(front).normalized()
    rise = front.cross(across).normalized()
    sides = 24
    rings = ((-.064, .11), (-.058, .45), (-.043, .75), (-.019, .94), (0, 1))
    points = []
    for depth, width in rings:
        for i in range(sides):
            angle = math.tau * i / sides
            x, y = math.cos(angle), math.sin(angle)
            points.append(center + front * depth
                          + width * (across * x * (.072 - .009 * y) + rise * y * .084))
    faces = []
    for row in range(len(rings) - 1):
        for i in range(sides):
            a, b = row * sides + i, row * sides + (i + 1) % sides
            u, u1 = i * GUAVA_PEEL_WIDTH / sides, (i + 1) * GUAVA_PEEL_WIDTH / sides
            v, v1 = 1 - row / (len(rings) - 1), 1 - (row + 1) / (len(rings) - 1)
            faces.append(((a, b, b + sides, a + sides), ((u,v),(u1,v),(u1,v1),(u,v1))))
    faces.append((tuple(reversed(range(sides))), [(.27, .92)] * sides))
    mesh.add(points, faces, 'Guava/FruitReady')

    x, y = GUAVA_SECTION_CENTER
    uv = [((x + GUAVA_SECTION_RADIUS * math.cos(i * math.tau / sides)) / 256,
           (y - GUAVA_SECTION_RADIUS * math.sin(i * math.tau / sides)) / 256)
          for i in range(sides)]
    mesh.add(points[-sides:], [(tuple(range(sides)), uv)], 'Guava/FruitReady')


def seed():
    mesh = Geometry()
    profile = ((0,.12),(.09,.42),(.28,.53),(.55,.50),(.78,.40),(.94,.22),(1,.06))
    ring_shape(mesh, (0, 0, 0), .039, profile, 'StoneSeed', 16)
    for point in mesh.vertices:
        point.x += point.z * .62
        point.y *= .60
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0, 0, 0), (-.012, .001, .072), (.002, -.005, .166), (.015, -.004, .226)]
    tube(stem, *curved_axis(points, [.010, .009, .006, .0035], 3), 'GreenStem', 7, closed=True)
    for root, bearing, length, pitch, roll in (
        (points[2], 3.42, .151, .58, -.27),
        (points[2], 5.96, .169, .69, .38),
    ):
        leaf_card('Guava', leaves, Vector(root), direction(1, bearing, pitch),
                  length, length * .62, roll, True)
    return [('Guava seedling stem', stem), ('First oval leaves', leaves)]


def tree(young, detail):
    rng = random.Random(52287)
    size = Vector((.60, .60, .69)) if young else Vector((1, 1, 1))
    trunk = [Vector(p) * size for p in TRUNK]
    radii = [r * (.61 if young else 1) for r in RADII]
    spine, _ = curved_axis(trunk, radii, 8)
    branches = [(trunk, radii)]
    shoots, crop = [], []
    for i, (fork, yaw, reach, tip, radius) in enumerate(BRANCHES[:6] if young else BRANCHES):
        if fork >= 1.21:
            reach *= 1.18 if young else 1.42
        elif not young and fork >= .94:
            reach *= 1.14
        base_height = fork * size.z
        a, b = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= base_height <= b.z)
        base = a.lerp(b, (base_height - a.z) / (b.z - a.z))
        turn = rng.uniform(-.20, .20)
        rise = tip - fork
        axis = (base, base + direction(.19 * reach, yaw, .43 * rise) * size,
                base + direction(.59 * reach, yaw + turn, .75 * rise) * size,
                base + direction(reach, yaw + turn * 1.5, rise) * size)
        widths = [radius * factor * (.66 if young else 1) for factor in (1, .77, .48, .17)]
        branches.append((axis, widths))
        guide, _ = curved_axis(axis, widths, 5)
        for j, at in enumerate((.58, .77, .94) if young else (.44, .62, .76, .89, .98)):
            anchor, _ = sample_axis(guide, at)
            side = (-1)**(i + j)
            bearing = yaw + side * rng.uniform(.69, 1.34)
            end = anchor + direction(rng.uniform(.22, .32), bearing, rng.uniform(.064, .15)) * size
            curve = [anchor, anchor.lerp(end, .52) + UP * (.032 * size.z), end]
            shoots.append((curve, bearing, .009 * size.x, at))
            for k, pivot in enumerate((.65,) if young else (.56, .80)):
                root, _ = sample_axis(curve, pivot)
                twig_yaw = bearing + (-1)**(j + k) * rng.uniform(.86, 1.44)
                tiplet = root + direction(rng.uniform(.13, .22), twig_yaw, rng.uniform(-.06,.10)) * size
                shoots.append(([root, root.lerp(tiplet,.48), tiplet], twig_yaw, .0049 * size.x, at))
            if not young and (j == 4 or (j == 3 and i < 6)):
                crop.append((curve[1].lerp(curve[2], .91), bearing, rng.uniform(.11, .147)))
        if fork >= 1.21:
            # Fan out and down near the outer tips so the upper tiers overhang the fork.
            for k, at in enumerate((.84,) if young else (.74, .94)):
                anchor, _ = sample_axis(guide, at)
                bearing = yaw + (-1)**(i + k) * (.47 + .10 * k)
                end = anchor + direction(.28 + .04 * k, bearing, -.09 - .036 * k) * size
                elbow = anchor.lerp(end, .48) + UP * .049 * size.z
                shoots.append(([anchor, elbow, end], bearing, .0087 * size.x, at))
                twig_yaw = bearing + (-1)**i * .91
                side = elbow + direction(.21, twig_yaw, -.088) * size
                shoots.append(([elbow, elbow.lerp(side, .52), side], twig_yaw, .0049 * size.x, at))
    for i, at in enumerate((.65, .75, .84, .90, .965, .995)):
        root, _ = sample_axis(spine, at)
        yaw = i * 2.40 + .4
        span = .31 if young else .37 + .032 * min(i, 3)
        tip = root + direction(span * size.x, yaw, (.10 + .018 * (i % 2)) * size.z)
        bend = root.lerp(tip, .52) + UP * .052 * size.z
        shoots.append(([root, bend, tip], yaw, .008 * size.x, .83))
        for side in (-1, 1):
            fork_yaw = yaw + side * 1.18
            end = bend + direction((.17 if young else .21) * size.x, fork_yaw, -.064 * size.z)
            shoots.append(([bend, bend.lerp(end,.55), end], fork_yaw, .0045 * size.x, .78))
    if not young:
        for i, at in enumerate((.51, .57, .63, .69)):
            root, _ = sample_axis(spine, at)
            yaw = 1.9 + i * 2.4
            tip = root + direction(.33, yaw, .16)
            middle = root.lerp(tip, .50) + UP * .039
            shoots.append(([root, middle, tip], yaw, .0078, .71))
            for side in (-1, 1):
                bearing = yaw + side * 1.14
                end = middle + direction(.19, bearing, -.02)
                shoots.append(([middle, middle.lerp(end, .54), end], bearing, .0045, .70))

    wood = fused_wood(branches, detail, young, root_scale=.46 if young else .65)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    for i, (curve, yaw, radius, spread) in enumerate(shoots):
        if radius >= .008 or (detail and (i % 3 == 0 or spread > .82)):
            tube(wood, curve, [radius, radius * .60, radius * .20], 'Twig', 4 if detail else 3)
        for j, part in enumerate((.41, .76) if radius >= .007 or i % 2 else (.65,)):
            root, _ = sample_axis(curve, part + rng.uniform(-.042, .042))
            bearing = yaw + (-1)**(i + j) * rng.uniform(.49, 1.28)
            pitch = rng.uniform(-.61, .36)
            length = rng.uniform(.37, .48) * (.77 if young else 1)
            if root.z > 1.68 * size.z and math.hypot(root.x, root.y) > .55 * size.x:
                length *= 1.12
                pitch -= .14
            width = length * rng.uniform(.90, 1.05)
            roll = rng.uniform(-.74, .74)
            lit = .14 + .30 * (root.z / size.z - 1.05) + .13 * spread
            accent = rng.random() < lit
            if not detail and j == 1 and i % 3 == 1:
                continue
            leaf_card('Guava', leaves, root, direction(1, bearing, pitch), length, width,
                      roll, accent, spray=True, detail=detail)

    for join, bearing, diameter in crop:
        # Fruit hangs clear of the leaf cards across the outer edge of each tier.
        socket = join + direction(.112, bearing, -.212)
        for mesh, mature, scale in ((green, False, .79), (ripe, True, 1.14)):
            start = len(mesh.vertices)
            fruit(mesh, socket, diameter * scale, mature, detail)
            points = [join, join.lerp(socket, .55) + direction(.022,bearing,.015), socket]
            tube(mesh, points, [.0055,.0044,.0034], 'GreenStem', 5 if detail else 4)
            mesh.anchor_from(start, join)
    print(f'Guava {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{len(leaves.anchors)} sprays, {len(crop)} fruit positions', flush=True)
    return wood, leaves, green, ripe


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=sides, root_scale=.65, uv_span=(1, .81))
    points = mesh.vertices[-sides:]
    uv = [(.5 + .46 * math.cos(i * math.tau / sides), .5 + .46 * math.sin(i * math.tau / sides))
          for i in range(sides)]
    mesh.add(points, [(tuple(range(sides)), uv)], 'CutWood')
    return mesh


def produce():
    loose = Geometry()
    socket = Vector((-.075, .030, .171))
    fruit(loose, socket, .157, True, True)
    tip = socket + Vector((.015, .008, .031))
    tube(loose, [socket, socket.lerp(tip,.55), tip], [.007,.006,.004], 'Twig', 6)
    leaf_card('Guava', loose, tip, direction(1,.21,-.33), .129, .069, -.25, False)
    section(loose, Vector((.068,-.038,.074)))
    floor = min(point.z for point in loose.vertices)
    for point in loose.vertices:
        point.z -= floor
    return loose


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    mesh_out = WORK / 'Mesh'
    mesh_out.mkdir(parents=True, exist_ok=True)
    atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(atlas)
    configure_cutout(material)
    unity_mesh.configure(material, mesh_out)
    stage('NP_Guava_Seed', [('Guava seed', seed())], .10, material, False)
    stage('NP_Guava_Sprout', sprout(), .26, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        wood, leaves, green, ripe = tree(False, detail)
        young_wood, young_leaves, _, _ = tree(True, detail)
        crown = (wood, leaves, green, ripe)
        adjust_height((*crown, young_wood, young_leaves), height_adjustment(crown))
        stage('NP_Guava_Young' + suffix, [('Young wood', young_wood), ('Young oval leaf sprays', young_leaves)],
              1, material, False, shade=shade_guava)
        foliage = [('Guava trunk and shoots', wood), ('Guava leaf sprays', leaves)]
        stage('NP_Guava_FruitForming' + suffix, foliage + [('Green fruit', green)],
              1, material, False, shade=shade_guava)
        stage('NP_Guava_HarvestReady' + suffix, foliage + [('Ripe fruit with calyx', ripe)],
              1, material, detail, shade=shade_guava)
    stage('NP_Guava_Stump', [('Guava stump', stump())], 1, material, False)
    stage('NP_Guava_Produce', [('Whole guava and pink section', produce())], 1, material, False)
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
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Guava.blend'), relative_remap=False)
    print('GUAVA_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
