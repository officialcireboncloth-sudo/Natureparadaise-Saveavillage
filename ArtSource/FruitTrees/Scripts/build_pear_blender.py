"""Build the orchard pear with rectangular cutout leaves and curved scaffold limbs."""
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
    GOLDEN_ANGLE, UP, Geometry, adjust_height, aim, curved_axis, fused_wood, height_adjustment,
    material_atlas, ring_shape, sample_axis, setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import (
    ATLAS, PEAR_BARK_UV, PEAR_PEEL_WIDTH, PEAR_PROFILE, PEAR_SECTION_CENTER, PEAR_SECTION_SCALE,
    SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, shade_sprays


WORK = Path(os.environ['NP_PEAR_STAGE'])
LEAF = 'Pear/Leaf'
ACCENT = 'Pear/LeafAccent'
GREEN = 'Pear/FruitForming'
RIPE = 'Pear/FruitReady'
SHADE = partial(shade_sprays, species='Pear', rect=(0, 0, 256, 256), strength=.54)
TRUNK = ((0, 0, -.024), (-.030, .010, .18), (-.038, .010, .43), (-.015, -.010, .77),
         (.033, .008, 1.06), (.070, .035, 1.38), (.081, .036, 1.68),
         (.048, .064, 1.96), (.028, .111, 2.24))
RADII = (.180, .147, .118, .093, .072, .053, .034, .018, .005)
# Fork height, azimuth, spread, tip height and width at the union.
LIMBS = (
    (.60, 4.97, .89, 1.46, .069), (.69, 2.70, 1.02, 1.48, .071),
    (.78, .64, .96, 1.61, .062), (.91, 5.84, 1.05, 1.75, .054),
    (1.02, 3.72, 1.00, 1.79, .052), (1.15, 1.73, .91, 1.87, .044),
    (1.25, 4.80, .82, 2.02, .039), (1.36, .25, .82, 2.03, .033),
    (1.49, 2.65, .65, 2.14, .029), (1.60, 5.81, .64, 2.19, .024),
    (1.72, 1.26, .52, 2.23, .020),
)
FORMING_LIMBS = (
    (.65, 4.87, .64, 1.52, .052), (.78, 2.67, .72, 1.66, .053),
    (.89, .59, .68, 1.76, .046), (1.08, 5.86, .70, 1.94, .039),
    (1.27, 3.69, .61, 2.02, .032), (1.44, 1.62, .47, 2.08, .026),
    (1.57, .32, .39, 2.19, .020),
)
YOUNG_TRUNK = ((0, 0, -.024), (-.027, .006, .24), (-.034, .007, .49),
               (-.010, .025, .73), (.075, .046, 1.01), (.13, .058, 1.27))
YOUNG_RADII = (.093, .066, .048, .032, .019, .011)
ROOTS = ((5.18, .66, .25, .086), (3.74, .53, -.23, .075), (6.12, .58, .27, .081),
         (2.53, .62, -.20, .075), (1.32, .53, .24, .072), (.37, .46, -.18, .066))


def direction(reach, azimuth, rise):
    return Vector((reach * math.cos(azimuth), reach * math.sin(azimuth), rise))


def roots(count, scale):
    axes = []
    for yaw, reach, turn, radius in ROOTS[:count]:
        path = [direction(reach * start, yaw + turn * bend, height) * scale
                for start, bend, height in ((.08, 0, .176), (.30, .11, .092), (.59, .57, .031),
                                            (.84, .91, -.003), (1, 1, -.035))]
        axes.append((path, [radius * scale * taper for taper in (1, .96, .68, .36, .09)]))
    return axes


def pear_bark(mesh):
    left, right = PEAR_BARK_UV
    mesh.surfaces = [(GREEN, [(left + (right - left) * u, v) for u, v in uv]) if slot in ('Bark', 'Twig')
                     else (slot, uv) for slot, uv in mesh.surfaces]
    return mesh


def card(mesh, base, heading, length, roll, accent, detail):
    """Use a rectangular card with a gentle longitudinal curve at LOD0."""
    forward = heading.normalized()
    right = forward.cross(UP).normalized()
    normal = right.cross(forward)
    right = math.cos(roll) * right + math.sin(roll) * normal
    normal = right.cross(forward)
    rows = (0, .34, .70, 1) if detail else (0, 1)
    points, uv = [], []
    for t in rows:
        center = base + forward * (length * (t - .035))
        center += normal * (length * (.084 * math.sin(math.pi * t) - .11 * t**2))
        points.extend((center - right * (length * .40), center + right * (length * .40)))
        uv.extend(((0, 1 - t), (1, 1 - t)))
    faces = [(i, i + 1, i + 3, i + 2) for i in range(0, len(points) - 2, 2)]
    assert len(faces) == (3 if detail else 1)
    mesh.add(points, [(face, [uv[i] for i in face]) for face in faces], ACCENT if accent else LEAF, anchor=base)


def seed():
    mesh = Geometry()
    profile = ((0, .15), (.08, .36), (.28, .53), (.52, .50), (.77, .31), (.92, .15), (1, .028))
    ring_shape(mesh, (0, 0, 0), .059, profile, 'AppleSeed', 16)
    for point in mesh.vertices:
        point.x += point.z * .38
        point.y *= .67
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0, 0, 0), (-.011, .002, .078), (-.001, -.006, .163), (.008, -.004, .222)]
    tube(stem, *curved_axis(points, [.010, .008, .006, .0036], 4), 'GreenStem', 8, closed=True)
    card(leaves, Vector(points[2]), direction(1, 3.49, .51), .150, -.25, False, True)
    card(leaves, Vector(points[3]), direction(1, 5.91, .53), .162, .20, True, True)
    return [('Sprout stem', stem), ('First serrated leaves', leaves)]


def pear(mesh, center, size, ripe, detail):
    profile = PEAR_PROFILE if detail else tuple(PEAR_PROFILE[i] for i in (0, 2, 4, 5, 7, 9, 10))
    first = len(mesh.surfaces)
    ring_shape(mesh, center, size, profile, RIPE if ripe else GREEN, 16 if detail else 10)
    mesh.surfaces[first:] = [(slot, [(u * PEAR_PEEL_WIDTH, v) for u, v in uv])
                             for slot, uv in mesh.surfaces[first:]]


def hanging_fruit(mesh, anchor, azimuth, size, ripe, detail):
    start = len(mesh.vertices)
    socket = anchor + direction(.093, azimuth, -.134)
    pear(mesh, socket - UP * (size * PEAR_PROFILE[-1][0]), size, ripe, detail)
    tube(mesh, [anchor, anchor.lerp(socket, .47) + UP * .037, socket],
         [.008, .006, .0045], 'Twig', 6 if detail else 4, closed=True)
    mesh.anchor_from(start, anchor)


def young_tree(detail):
    spine, _ = curved_axis(YOUNG_TRUNK, YOUNG_RADII, 8)
    limbs = [(YOUNG_TRUNK, YOUNG_RADII), *roots(3, .39)]
    leaves = Geometry()
    rng = random.Random(65133)
    for height, yaw, reach, rise in ((.39, 3.62, .42, .34), (.49, 5.80, .39, .28),
                                    (.67, 2.09, .39, .32), (.80, .63, .25, .25),
                                    (.97, 4.81, .27, .19)):
        a, b = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= height <= b.z)
        base = a.lerp(b, (height - a.z) / (b.z - a.z))
        axis = [base, base + direction(reach * .33, yaw - .17, rise * .47),
                base + direction(reach * .69, yaw, rise * .78), base + direction(reach, yaw + .14, rise)]
        width = .022 if height < .55 else .014
        taper = [width, width * .73, width * .45, width * .16]
        limbs.append((axis, taper))
        for index, at in enumerate((.46, .68, .87, .99)):
            start, _ = sample_axis(axis, at)
            heading = direction(1, yaw + (-1)**index * rng.uniform(.50, 1.10), rng.uniform(.05, .38))
            bank = rng.uniform(-.66, .66)
            card(leaves, start, heading, rng.uniform(.23, .31), bank, index % 3 != 0, detail)
    for index, at in enumerate((.74, .80, .85, .89, .955, .999)):
        start, _ = sample_axis(spine, at)
        card(leaves, start, direction(1, index * GOLDEN_ANGLE, .48), .255, .25 * (-1)**index, True, detail)
    return pear_bark(fused_wood(limbs, detail, True, root_scale=.18)), leaves


def tree(ripe, detail):
    scale = Vector((1.13, 1.13, .98)) if ripe else Vector((1, 1, 1))
    trunk = [Vector(point) * scale for point in TRUNK]
    widths = [radius if ripe else radius * .74 for radius in RADII]
    spine, _ = curved_axis(trunk, widths, 8)
    limbs, shoots, sites = [(trunk, widths), *roots(6 if ripe else 4, 1 if ripe else .71)], [], []
    rng = random.Random(38211)
    for index, (join, yaw, reach, tip, radius) in enumerate(LIMBS if ripe else FORMING_LIMBS):
        height = join * scale.z
        low, high = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= height <= b.z)
        root = low.lerp(high, (height - low.z) / (high.z - low.z))
        roll = rng.uniform(-.26, .26)
        rise = tip - join
        path = [root, root + direction(reach * .12, yaw - roll * .36, rise * .23) * scale,
                root + direction(reach * .41, yaw, rise * .56) * scale,
                root + direction(reach * .73, yaw + roll * .56, rise * .87) * scale,
                root + direction(reach, yaw + roll, rise) * scale]
        width = radius
        taper = [width, width * .88, width * .68, width * .43, width * .16]
        limbs.append((path, taper))
        guide, _ = curved_axis(path, taper, 5)
        positions = (.46, .59, .73, .855, .982) if ripe else (.59, .80, .984)
        for order, at in enumerate(positions):
            start, _ = sample_axis(guide, at)
            turn = (-1)**(order + index) * rng.uniform(.55, 1.19)
            shoot_reach = rng.uniform(.19, .33) if ripe else rng.uniform(.14, .24)
            end = start + direction(shoot_reach, yaw + turn, rng.uniform(.02, .20)) * scale
            middle = start.lerp(end, .50) + UP * (.028 * scale.z)
            shoots.append(([start, middle, end], yaw + turn, False))
            if ripe or order == len(positions) - 1:
                inward = yaw - turn * 1.18
                tip = middle + direction(rng.uniform(.16, .25), inward, rng.uniform(-.043, .084)) * scale
                shoots.append(([middle, middle.lerp(tip, .53), tip], inward, True))
        if index not in ((8, 10) if ripe else (1, 6)):
            anchor, _ = sample_axis(guide, .91 if index < 7 else .96)
            sites.append((anchor, yaw + roll, rng.uniform(.125, .171)))

    leader_positions = (.58, .70, .79, .864, .92, .965, 1) if ripe else (.75, .87, .95, 1)
    for index, at in enumerate(leader_positions):
        root, _ = sample_axis(spine, at)
        yaw = .43 + index * GOLDEN_ANGLE
        reach = (.38 - .021 * index) if ripe else (.23 - .021 * index)
        end = root + direction(reach, yaw, .13 if index < 5 else .095) * scale
        middle = root.lerp(end, .55) + UP * (.034 * scale.z)
        shoots.append(([root, middle, end], yaw, False))
        for turn in (-1.02, 1.14):
            tip = middle + direction(.19, yaw + turn, .08) * scale
            shoots.append(([middle, middle.lerp(tip, .46), tip], yaw + turn, True))

    if ripe:
        # Short ascending forks bridge the rounded shoulder of the crown.
        for index in range(13):
            yaw = .77 + index * math.tau / 13
            root, _ = sample_axis(spine, .65 + .025 * (index % 3))
            end = root + direction(.61 + .04 * (index % 2), yaw, .30)
            middle = root.lerp(end, .56) + UP * .061
            shoots.append(([root, middle, end], yaw, False))
            turn = yaw + (-1)**index * .86
            tip = middle + direction(.30, turn, .117)
            shoots.append(([middle, middle.lerp(tip, .52), tip], turn, True))
    else:
        for index, at in enumerate((.58, .62, .66, .705, .75, .79, .84)):
            root, _ = sample_axis(spine, at)
            yaw = 1.08 + index * GOLDEN_ANGLE
            end = root + direction(.345, yaw, .22)
            middle = root.lerp(end, .53) + UP * .03
            shoots.append(([root, middle, end], yaw, False))
            tip = middle + direction(.18, yaw + 1.03, .095)
            shoots.append(([middle, middle.lerp(tip, .53), tip], yaw + 1.03, True))

    wood = fused_wood(limbs, detail, False, root_scale=.24)
    leaves, fruits = Geometry(), Geometry()
    for index, (axis, yaw, secondary) in enumerate(shoots):
        if not secondary or (detail and index % 2 == 0):
            width = (.007 if secondary else .010) * scale.x
            tube(wood, axis, [width, width * .59, width * .23], 'Twig', 5 if detail else 4)
        positions = (.22, .46, .64, .80, .99) if secondary else (.19, .37, .50, .66, .78, .91, .996)
        if not ripe:
            positions = (.30, .59, .86, .99)
        for order, at in enumerate(positions):
            base, _ = sample_axis(axis, at)
            turn = (-1)**(index + order) * rng.uniform(.52, 1.32) * (1 - .51 * at**8)
            length = rng.uniform(.275, .392) if ripe else rng.uniform(.260, .342)
            pitch = rng.uniform(-.46, .45) + .045 * at + max(0, base.z / scale.z - 1.89) * .54
            bank = rng.uniform(-.60, .60)
            chance = (.24 if secondary else .34) + max(0, base.z - 1.75) * .10 + (0 if ripe else .13)
            accent = rng.random() < chance
            if detail or (order + index) % 6 != 1 or at > .8:
                card(leaves, base, direction(1, yaw + turn, pitch), length, bank, accent, detail)

    for anchor, yaw, radius in sites:
        hanging_fruit(fruits, anchor, yaw, radius if ripe else radius * .67, ripe, detail)
    print(f'Pear {"ready" if ripe else "forming"} LOD{0 if detail else 1}: '
          f'{len(leaves.anchors)} rectangular leaf cards, {len(sites)} fruit sites', flush=True)
    return pear_bark(wood), leaves, pear_bark(fruits)


def stump():
    mesh = Geometry()
    count = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=count, root_scale=.24, uv_span=(1, .79))
    uv = [(.50 + .45 * math.cos(i * math.tau / count), .50 + .45 * math.sin(i * math.tau / count))
          for i in range(count)]
    mesh.add(mesh.vertices[-count:], [(tuple(range(count)), uv)], 'CutWood')
    for path, widths in roots(6, 1):
        tube(mesh, *curved_axis(path, widths, 4), sides=12, closed=True, uv_span=(.94, .75))
    return pear_bark(mesh)


def cut_half(mesh, center, size):
    front = Vector((.27, -.82, .50)).normalized()
    side = UP.cross(front).normalized()
    rise = front.cross(side)
    count = 12
    points, skin, face = [], [], []
    for level, width in PEAR_PROFILE:
        for index in range(count + 1):
            angle = math.pi * index / count
            radial = (side * math.cos(angle) - front * math.sin(angle)) * width
            points.append(center + size * (rise * level + radial))
    stride = count + 1
    for row in range(len(PEAR_PROFILE) - 1):
        v, v_next = (1 - index / (len(PEAR_PROFILE) - 1) for index in (row, row + 1))
        for index in range(count):
            a = row * stride + index
            u, u_next = (column * PEAR_PEEL_WIDTH / count for column in (index, index + 1))
            skin.append(((a, a + 1, a + 1 + stride, a + stride), ((u, v), (u_next, v), (u_next, v_next), (u, v_next))))
    skin.extend(((tuple(reversed(range(stride))), [(.20, .97)] * stride),
                 (tuple(range(len(points) - stride, len(points))), [(.20, .03)] * stride)))
    mesh.add(points, skin, RIPE)
    x, y = PEAR_SECTION_CENTER
    sx, sy = PEAR_SECTION_SCALE
    for row in range(len(PEAR_PROFILE) - 1):
        z1, w1 = PEAR_PROFILE[row]
        z2, w2 = PEAR_PROFILE[row + 1]
        contour = ((-w1, z1), (w1, z1), (w2, z2), (-w2, z2))
        panel = [center + size * (side * u + rise * v) for u, v in contour]
        uv = [((x + sx * u) / 256, (y - sy * v) / 256) for u, v in contour]
        face.append((panel, uv))
    for panel, uv in face:
        mesh.add(panel, [((0, 1, 2, 3), uv)], RIPE)


def produce():
    mesh = Geometry()
    center = Vector((-.114, .026, .135))
    pear(mesh, center, .132, True, True)
    base = center + UP * (.132 * PEAR_PROFILE[-1][0])
    tip = base + Vector((.021, .003, .051))
    tube(mesh, [base, base.lerp(tip, .51), tip], [.008, .007, .005], 'Twig', 8, closed=True)
    card(mesh, tip, direction(1, .66, -.29), .147, -.31, True, True)
    cut_half(mesh, Vector((.117, -.076, .127)), .119)
    floor = min(point.z for point in mesh.vertices)
    for point in mesh.vertices:
        point.z -= floor
    return pear_bark(mesh)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    mesh_output = WORK / 'Mesh'
    mesh_output.mkdir(parents=True, exist_ok=True)
    atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(atlas)
    configure_cutout(material)
    unity_mesh.configure(material, mesh_output)
    stage('NP_Pear_Seed', [('Pear seed', seed())], .10, material, False)
    stage('NP_Pear_Sprout', sprout(), .26, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        young_wood, young_leaves = young_tree(detail)
        stage('NP_Pear_Young' + suffix, [('Young pear wood', young_wood), ('Young leaf cards', young_leaves)],
              1, material, False, shade=SHADE)
        for phase, ripe in (('FruitForming', False), ('HarvestReady', True)):
            parts = tree(ripe, detail)
            adjust_height(parts, height_adjustment(parts))
            labels = ('Pear trunk, limbs and roots', 'Serrated pear leaf cards', 'Golden pears' if ripe else 'Green pears')
            stage('NP_Pear_' + phase + suffix, list(zip(labels, parts)), 1, material, ripe and detail, shade=SHADE)
    stage('NP_Pear_Stump', [('Pear stump', stump())], 1, material, False)
    stage('NP_Pear_Produce', [('Whole pear and sliced half', produce())], 1, material, False)
    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT * .49)
    aim(scene.camera, (2.8, -7.2, 3.7), focus)
    scene.camera.data.ortho_scale = 3.85
    scene.render.resolution_x, scene.render.resolution_y = 1080, 1060
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
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Pear.blend'), relative_remap=False)
    print('PEAR_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
