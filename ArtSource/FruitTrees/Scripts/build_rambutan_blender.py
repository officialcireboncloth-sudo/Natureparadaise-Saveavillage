"""Build orchard rambutan with compound leaves and textured fruit clusters."""
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
    ATLAS, RAMBUTAN_PEEL_WIDTH, RAMBUTAN_SECTION_CENTER,
    RAMBUTAN_SECTION_RADIUS, RAMBUTAN_SEED_RECT, SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays


WORK = Path(os.environ['NP_RAMBUTAN_STAGE'])
SHADE = partial(shade_sprays, species='Rambutan')
TRUNK = ((0, 0, -.021), (-.022, .012, .15), (-.045, .029, .39), (-.020, .034, .64),
         (.044, .035, .91), (.088, .009, 1.21), (.085, -.047, 1.52),
         (.028, -.056, 1.89), (-.079, -.061, 2.20))
RADII = (.173, .140, .109, .088, .070, .056, .041, .025, .006)
# Attachment height, bearing, reach, end height and branch radius.
LIMBS = (
    (.63, 5.03, .96, 1.43, .078), (.70, 2.87, 1.00, 1.55, .073),
    (.82, .69, .91, 1.63, .066), (.92, 4.01, .90, 1.72, .062),
    (1.04, 1.82, .96, 1.81, .057), (1.12, 5.91, 1.02, 1.88, .051),
    (1.23, 3.29, .82, 1.94, .043), (1.34, 1.13, .73, 2.05, .035),
    (1.42, 5.02, .71, 2.14, .032), (1.57, 2.50, .58, 2.21, .025),
    (1.68, 6.09, .51, 2.26, .020),
)
CLUSTER_COUNTS = {0: 4, 2: 3, 3: 4, 4: 3, 5: 3, 7: 3, 8: 3}
FRUIT_PROFILE = ((-1.14, .10), (-1.08, .37), (-.83, .70), (-.48, .90), (-.09, .99),
                 (.29, .96), (.68, .78), (.96, .49), (1.08, .23), (1.07, .10))


def offset(reach, yaw, rise):
    return Vector((reach * math.cos(yaw), reach * math.sin(yaw), rise))


def patch_uv(u, v, rect):
    left, top, right, bottom = rect
    return (left + (right - left) * u) / 256, (top + (bottom - top) * v) / 256


def seed():
    mesh = Geometry()
    profile = ((0, .11), (.075, .33), (.23, .48), (.45, .51), (.65, .45),
               (.82, .34), (.94, .17), (1, .06))
    ring_shape(mesh, (0, 0, 0), .072, profile, 'Rambutan/FruitReady', 16)
    for point in mesh.vertices:
        height = point.z
        point.x += height * .59
        point.y *= .66
        point.z *= .78
    mesh.surfaces = [(slot, [patch_uv(1 - v, u, RAMBUTAN_SEED_RECT) for u, v in uv])
                     for slot, uv in mesh.surfaces]
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0,0,0), (-.012,.005,.085), (.004,.005,.166), (.011,0,.224)]
    tube(stem, *curved_axis(points, [.011,.009,.006,.004], 3), 'GreenStem', 8, closed=True)
    for base, bearing, pitch, length, roll in (
        (points[2], 3.54, .46, .153, .51),
        (points[3], 6.00, .59, .175, -.57),
    ):
        leaf_card('Rambutan', leaves, Vector(base), offset(1, bearing, pitch),
                  length, length * .69, roll, True, bend=.25)
    return [('Rambutan seedling stem', stem), ('First oval leaflets', leaves)]


def rind(mesh, center, radius, slot, profile, sides):
    first_face = len(mesh.surfaces)
    ring_shape(mesh, center, radius, profile, slot, sides)
    mesh.surfaces[first_face:] = [(name, [(u * RAMBUTAN_PEEL_WIDTH, v) for u, v in uv])
                                  for name, uv in mesh.surfaces[first_face:]]


def whole_fruit(mesh, center, radius, ripe, detail):
    slot = 'Rambutan/FruitReady' if ripe else 'Rambutan/FruitForming'
    profile = FRUIT_PROFILE if detail else tuple(FRUIT_PROFILE[index] for index in (0, 2, 4, 6, 8))
    rind(mesh, center, radius, slot, profile, 13 if detail else 7)


def bunch(mesh, anchor, bearing, radius, count, ripe, detail):
    start = len(mesh.vertices)
    base = anchor + offset(.143, bearing, -.125)
    tip = base + offset(.064, bearing, -.245)
    tube(mesh, [anchor, anchor.lerp(base, .51) + UP * .009, base, tip],
         [.0084,.0065,.0055,.0031], 'Twig', 5 if detail else 4)
    for index in range(count):
        level = index / max(1, count - 1)
        node = base.lerp(tip, .16 + level * .70)
        yaw = bearing + index * GOLDEN_ANGLE + .31
        size = radius * (1 - .12 * level)
        socket = node + offset(.102 + .028 * (index % 2), yaw, -.046 - .025 * (index % 2))
        center = socket - UP * size * 1.04
        if detail:
            tube(mesh, [node, node.lerp(socket, .54) + UP * .021, socket], [.0055,.0040,.0033], 'Twig', 4)
        else:
            tube(mesh, [node, socket], [.0055,.0033], 'Twig', 3)
        whole_fruit(mesh, center, size, ripe, detail)
    mesh.anchor_from(start, anchor)


def tree(young, detail):
    rng = random.Random(27663 if young else 70531)
    scale = Vector((.60, .60, .73)) if young else Vector((1, 1, 1))
    trunk = [Vector(point) * scale for point in TRUNK]
    radii = [radius * (.58 if young else 1) for radius in RADII]
    spine, _ = curved_axis(trunk, radii, 7)
    branches = [(trunk, radii)]
    sprays, crop = [], []
    for index, (fork, yaw, reach, end, radius) in enumerate(LIMBS[:7] if young else LIMBS):
        height = fork * scale.z
        low, high = next((a, b) for a, b in zip(spine, spine[1:]) if a.z <= height <= b.z)
        base = low.lerp(high, (height - low.z) / (high.z - low.z))
        turn = rng.uniform(-.19, .21)
        rise = end - fork
        axis = [base, base + offset(reach * .21, yaw, rise * .35) * scale,
                base + offset(reach * .59, yaw + turn, rise * .78) * scale,
                base + offset(reach, yaw + turn, rise) * scale]
        width = radius * (.64 if young else 1)
        widths = [width, width * .82, width * .52, width * .18]
        branches.append((axis, widths))
        guide, _ = curved_axis(axis, widths, 5)
        fractions = (.54, .76, .95) if young else (.42, .59, .75, .88, .985)
        for step, fraction in enumerate(fractions):
            root, _ = sample_axis(guide, fraction)
            angle = yaw + (-1)**(index + step) * rng.uniform(.59, 1.25)
            tip = root + offset(rng.uniform(.19, .31), angle, rng.uniform(.026, .16)) * scale
            mid = root.lerp(tip, .55) + UP * (.045 * scale.z)
            sprays.append(([root, mid, tip], angle, .0082 * scale.x, fraction))
            if not young or step > 0:
                fork_yaw = angle - (-1)**step * rng.uniform(.71, 1.19)
                fork_tip = mid + offset(rng.uniform(.15, .23), fork_yaw, rng.uniform(-.075, .081)) * scale
                sprays.append(([mid, mid.lerp(fork_tip, .52), fork_tip], fork_yaw, .0049 * scale.x, fraction))
        if not young and index in CLUSTER_COUNTS:
            anchor, _ = sample_axis(guide, .93)
            crop.append((anchor, yaw + turn, rng.uniform(.092, .108), CLUSTER_COUNTS[index]))

    # Broad sprays off the leader fill gaps between the lower shelves and the top.
    for index, fraction in enumerate((.62,.72,.81,.88,.94,.989)):
        root, _ = sample_axis(spine, fraction)
        yaw = .72 + index * GOLDEN_ANGLE
        tip = root + offset(.37 * scale.x, yaw, (.127 + .024 * (index % 2)) * scale.z)
        mid = root.lerp(tip, .52) + UP * (.031 * scale.z)
        sprays.append(([root, mid, tip], yaw, .0076 * scale.x, .88))
        for side in (-1, 1):
            bearing = yaw + side * 1.23
            end = mid + offset(.184 * scale.x, bearing, -.053 * scale.z)
            sprays.append(([mid, mid.lerp(end, .50), end], bearing, .0044 * scale.x, .84))

    wood = fused_wood(branches, detail, young, root_scale=.47 if young else .72)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    for index, (curve, yaw, width, fraction) in enumerate(sprays):
        if width >= .007 or (detail and index % 2 == 0):
            tube(wood, curve, [width, width * .59, width * .17], 'Twig', 4 if detail else 3)
        for order, at in enumerate((.35,.76)):
            root, _ = sample_axis(curve, at)
            bearing = yaw + (-1)**(index + order) * rng.uniform(.50, 1.19)
            pitch = rng.uniform(-.57,.36)
            length = rng.uniform(.36,.48) * (.75 if young else 1)
            roll = rng.uniform(-.71,.71)
            accent = rng.random() < .16 + (root.z / scale.z - 1.20) * .27 + fraction * .14
            if detail or order == 0 or width >= .007 or index % 3 == 1:
                leaf_card('Rambutan', leaves, root, offset(1,bearing,pitch), length, length * .91,
                          roll, accent, spray=True, detail=detail, bend=.31)
        if width >= .007 or fraction > .85:
            tip = curve[-2].lerp(curve[-1], .78)
            for side in (-1, 1):
                length = rng.uniform(.175,.250) * (.74 if young else 1)
                pitch = rng.uniform(-.34,.43)
                turn = rng.uniform(.64,.96) * side
                roll = rng.uniform(-.62,.62)
                if detail or (width >= .007 and side == 1) or (index % 3 == 0 and side == -1):
                    leaf_card('Rambutan', leaves, tip, offset(1,yaw+turn,pitch), length, length * .65,
                              roll, index % 4 == 0, detail=detail, bend=.28)

    for anchor, yaw, radius, count in crop:
        bunch(green, anchor, yaw, radius * .77, count, False, detail)
        bunch(ripe, anchor, yaw, radius, count, True, detail)
    print(f'Rambutan {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{len(leaves.anchors)} leaf cards, {len(crop)} clusters', flush=True)
    return wood, leaves, green, ripe


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh, *curved_axis(TRUNK[:3], RADII[:3], 4), sides=sides, root_scale=.72, uv_span=(1,.81))
    uv = [(.5 + .45 * math.cos(i * math.tau / sides), .5 + .45 * math.sin(i * math.tau / sides))
          for i in range(sides)]
    mesh.add(mesh.vertices[-sides:], [(tuple(range(sides)), uv)], 'CutWood')
    return mesh


def opened_half(mesh, center):
    slot = 'Rambutan/FruitReady'
    origin = Vector((0,0,0))
    radius, sides = .102, 21
    start = len(mesh.vertices)
    shells = ((-1.10,.17), (-1.02,.42), (-.85,.66), (-.59,.83), (-.29,.96), (0,1))
    rind(mesh, origin, radius, slot, shells, sides)
    mesh.faces.pop()
    mesh.surfaces.pop()
    x, y = RAMBUTAN_SECTION_CENTER
    radius_uv = RAMBUTAN_SECTION_RADIUS
    points, uv, faces = [], [], []
    cap = ((0,1), (.007,.85), (.032,.76), (.057,.59), (.074,.33), (.080,.08))
    for level, width in cap:
        for index in range(sides):
            angle = index * math.tau / sides
            a, b = width * math.cos(angle), width * math.sin(angle)
            points.append(Vector((radius*a, radius*b, level)))
            uv.append(((x + radius_uv*a) / 256, (y - radius_uv*b) / 256))
    for row in range(len(cap) - 1):
        for index in range(sides):
            a, b = row*sides + index, row*sides + (index + 1) % sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(range(len(points) - sides, len(points))))
    mesh.add(points, [(face, [uv[index] for index in face]) for face in faces], slot)
    normal = Vector((.32,-.81,.55)).normalized()
    rotation = UP.rotation_difference(normal)
    for point in mesh.vertices[start:]:
        point[:] = center + rotation @ point


def produce():
    mesh = Geometry()
    crotch = Vector((-.096, .018, .289))
    for index, center in enumerate((Vector((-.155,.021,.127)),Vector((-.027,.101,.117)))):
        radius = .103 if index == 0 else .092
        whole_fruit(mesh, center, radius, True, True)
        stem = center + UP * radius * 1.05
        tube(mesh, [stem, stem.lerp(crotch,.57) + UP * .026, crotch], [.004,.006,.007], 'Twig', 6)
    leaf_card('Rambutan', mesh, crotch, offset(1,.54,-.26), .144, .094, -.33, True)
    opened_half(mesh, Vector((.135,-.037,.118)))
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
    stage('NP_Rambutan_Seed', [('Fibrous rambutan seed', seed())], .10, material, False)
    stage('NP_Rambutan_Sprout', sprout(), .26, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        wood, leaves, green, ripe = tree(False, detail)
        young_wood, young_leaves, _, _ = tree(True, detail)
        crown = (wood, leaves, green, ripe)
        adjust_height((*crown, young_wood, young_leaves), height_adjustment(crown))
        stage('NP_Rambutan_Young' + suffix, [('Young trunk', young_wood), ('Young compound leaves', young_leaves)],
              1, material, False, shade=SHADE)
        foliage = [('Rambutan branches and roots', wood), ('Compound leaf sprays', leaves)]
        stage('NP_Rambutan_FruitForming' + suffix, foliage + [('Green rambutan clusters', green)],
              1, material, False, shade=SHADE)
        stage('NP_Rambutan_HarvestReady' + suffix, foliage + [('Red rambutan clusters', ripe)],
              1, material, detail, shade=SHADE)
    stage('NP_Rambutan_Stump', [('Cut rambutan trunk', stump())], 1, material, False)
    stage('NP_Rambutan_Produce', [('Rambutan cluster and exposed aril', produce())], 1, material, False)

    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0,0,ORCHARD_HEIGHT * .50)
    aim(scene.camera, (3.55,-7.0,3.91), focus)
    scene.camera.data.ortho_scale = 3.95
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
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Rambutan.blend'), relative_remap=False)
    print('RAMBUTAN_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
