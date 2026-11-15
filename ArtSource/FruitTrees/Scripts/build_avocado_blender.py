"""Author the Avocado growth stages using the common fruit tree atlas."""
import math
import os
import random
import sys
from functools import partial
from pathlib import Path

import bpy
from mathutils import Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_apple_blender import (
    Geometry, UP, adjust_height, aim, curved_axis, fused_wood, height_adjustment, material_atlas,
    ring_shape, sample_axis, setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import ATLAS, SOURCE, TEXTURES
from fruit_tree_dimensions import AVOCADO_REFERENCE_HEIGHT, ORCHARD_HEIGHT, PLAYER_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays


WORK = Path(os.environ['NP_AVOCADO_STAGE'])
shade_avocado = partial(shade_sprays, species='Avocado')
SEED_SCALE = .10
SPROUT_SCALE = .26
# Keep the trunk and produce at their authored size when the canopy changes.
BASE_SCALE = AVOCADO_REFERENCE_HEIGHT * (1.01727273 / 2.25)

TRUNK = ((0, 0, -.022), (.003, -.018, .11), (-.038, -.028, .32),
         (-.073, -.013, .65), (-.038, .024, .96), (.035, .063, 1.28),
         (.087, .061, 1.56), (.060, .058, 1.86), (.006, .031, 2.055))
TRUNK_RADII = (.115, .084, .075, .064, .053, .044, .029, .018, .007)
# Steep branches form a narrow crown with the main trunk visible below the leaves.
SCAFFOLDS = (
    (.79, 3.52, .48, 1.45, .051), (.88, 5.72, .55, 1.57, .050),
    (.98, 1.72, .53, 1.68, .045), (1.12, 4.44, .42, 1.77, .041),
    (1.24, .52, .46, 1.88, .037), (1.37, 2.78, .40, 1.99, .033),
    (1.50, 5.60, .33, 2.07, .028), (1.60, 1.28, .32, 2.16, .023),
)


def fruit(mesh, socket, height, lean, ripe, detail):
    profile = ((-1, .085), (-.934, .36), (-.812, .477), (-.666, .50),
               (-.49, .447), (-.30, .326), (-.135, .215), (-.034, .132), (0, .066))
    if not detail:
        profile = tuple(profile[i] for i in (0, 1, 3, 5, 7, 8))
    first = len(mesh.vertices)
    slot = 'Avocado/FruitReady' if ripe else 'Avocado/FruitForming'
    ring_shape(mesh, socket, height, profile, slot, 12 if detail else 8)
    # The shoulder stays under the stem while the lower body leans outward.
    for point in mesh.vertices[first:]:
        point += lean * ((socket.z - point.z) / height)**1.6


SEED_PROFILE = ((0, .23), (.071, .62), (.21, .88), (.42, 1),
                (.64, .93), (.82, .69), (.96, .28), (1, .085))


def seed(split=False):
    mesh = Geometry()
    for sign in (-1, 1):
        # Two welded caps expose a thin dark line on the pit. On the sprout they rotate apart.
        sides = 11
        rotation = Quaternion((0, 1, 0), sign * (.32 if split else .025))
        coords = []
        vertices = []
        for t, breadth in SEED_PROFILE:
            for i in range(sides):
                angle = (i / (sides - 1) + (sign < 0)) * math.pi - math.pi * .5
                edge = .037 * breadth
                point = Vector((math.cos(angle) * edge, math.sin(angle) * edge * .88, .085 * t))
                vertices.append(rotation @ point + Vector((sign * .0012, 0, .003)))
                u = i / (2 * (sides - 1)) + (.5 if sign < 0 else 0)
                coords.append((u, 1 - t))
        faces = []
        cap = (len(SEED_PROFILE) - 1) * sides
        for a in range(cap):
            if a % sides != sides - 1:
                faces.append((a, a + 1, a + 1 + sides, a + sides))
        for ids in (tuple(reversed(range(sides))), tuple(range(cap, cap + sides))):
            faces.append(ids)
        mesh.add(vertices, [(ids, [coords[i] for i in ids]) for ids in faces], 'StoneSeed')
        seam = tuple(row * sides for row in range(len(SEED_PROFILE)))
        rim = seam + tuple(a + sides - 1 for a in reversed(seam))
        mesh.add([vertices[i] for i in rim], [(tuple(range(len(rim))),
                 [(.16 + .20 * (i >= len(seam)), coords[a][1]) for i, a in enumerate(rim)])], 'StoneSeed')
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0, 0, .032), (-.013, -.003, .112), (-.020, .006, .183),
              (.001, .012, .243), (.018, .009, .295)]
    tube(stem, *curved_axis(points, [.0095, .009, .006, .004, .0025], 3), 'GreenStem', 7, closed=True)
    for base, heading, length, width, twist in (
        ((-.019,.002,.162), (-1,-.28,.40), .138, .054, -.24),
        ((-.010,.010,.207), (.88,-.53,.42), .151, .064, .37),
        ((-.002,.012,.231), (.61,.72,.59), .128, .048, -.32),
        ((.015,.010,.280), (-.44,.24,.85), .092, .039, .21),
        ((.018,.009,.294), (.57,-.28,.77), .064, .027, .24),
    ):
        leaf_card('Avocado', leaves, Vector(base), Vector(heading), length, width, twist, True)
    return [('Split seed', seed(split=True)), ('New stem', stem), ('First leaves', leaves)]


def axis(length, yaw, rise):
    return Vector((math.cos(yaw) * length, math.sin(yaw) * length, rise))


def tree_framework(young):
    rng = random.Random(40716)
    size = Vector((.62, .62, .71)) if young else Vector((1, 1, 1))
    trunk = [Vector(point) * size for point in TRUNK]
    widths = [r * (.59 if young else 1) for r in TRUNK_RADII]
    spine, _ = curved_axis(trunk, widths, 8)
    branches = [(trunk, widths)]
    sprays = []
    crown_shoots = []
    fruit_sites = []

    def center_on_trunk(height):
        for lower, upper in zip(spine, spine[1:]):
            if upper.z >= height:
                return lower.lerp(upper, (height - lower.z) / (upper.z - lower.z))
        return spine[-1]

    for i, (height, yaw, reach, tip, width) in enumerate(SCAFFOLDS[:6] if young else SCAFFOLDS):
        base = center_on_trunk(height * size.z)
        sidelobe = rng.uniform(-.16, .16)
        points = [base, base + axis(reach * .075, yaw, (tip-height) * .16) * size,
                  base + axis(reach * .36, yaw + sidelobe, (tip-height) * .55) * size,
                  base + axis(reach * .77, yaw + sidelobe * 1.5, (tip-height) * .89) * size,
                  base + axis(reach, yaw + sidelobe, tip - height) * size]
        width *= .68 if young else 1
        branch_widths = [width, width * .91, width * .69, width * .41, .013 if young else .016]
        branches.append((points, branch_widths))
        guide, _ = curved_axis(points, branch_widths, 4)
        positions = (.56, .76, .94) if young else (.39, .54, .70, .83, .96)
        for j, position in enumerate(positions):
            attach, _ = sample_axis(guide, position)
            side = -1 if (i + j) % 2 else 1
            bearing = yaw + side * rng.uniform(.71, 1.46)
            length = rng.uniform(.23, .34) * size.x
            lift = rng.uniform(.030, .135) * size.z
            tip = attach + axis(length, bearing, lift)
            curve = [attach, attach.lerp(tip, .54) + UP * (.027 * size.z), tip]
            sprays.append((curve, bearing, 6, position, .010 * size.x))
            forks = (.70,) if young else (.57, .82)
            for branchlet, at in enumerate(forks):
                root, _ = sample_axis(curve, at)
                fork_yaw = bearing + (-1)**(branchlet + j) * rng.uniform(.84, 1.40)
                end = root + axis(rng.uniform(.126, .205) * size.x, fork_yaw, rng.uniform(-.022, .102) * size.z)
                sprays.append(([root, root.lerp(end, .52), end], fork_yaw, 5, position, .0053 * size.x))
            if not young and j >= 2 and (i + j) % 4 != 1:
                root, _ = sample_axis(curve, .65)
                bend = bearing - side * .87
                end = root + axis(.17, bend, -.064)
                sprays.append(([root, root.lerp(end, .43) + UP * .022, end], bend, 5, position, .0057))
            if not young and (j == 3 or (j == 4 and i in (0, 2, 4, 6))):
                join = curve[1].lerp(curve[2], .93)
                fruit_sites.append((join, bearing, rng.uniform(.14, .182)))
        # Short leafy shoots fill the spaces between the long scaffold branches.
        for j, position in enumerate((.72,) if young else (.51, .67, .80)):
            root, _ = sample_axis(guide, position)
            bearing = yaw + (-1)**(i + j) * (1.54 + .28 * j)
            tip = root + axis((.17 + .035 * j) * size.x, bearing, (.19 - .045 * j) * size.z)
            mid = root.lerp(tip, .46) + UP * (.038 * size.z)
            crown_shoots.append(([root, mid, tip], bearing, 6, position, .0073 * size.x))
            if not young and j != 2:
                fork_yaw = bearing + (-1)**i * 1.26
                end = mid + axis(.165, fork_yaw, -.042)
                crown_shoots.append(([mid, mid.lerp(end, .52), end], fork_yaw, 5, position, .0043))
    for j, fraction in enumerate((.83, .91, .96, .995)):
        start, _ = sample_axis(spine, fraction)
        bearing = 1.4 + j * 2.39
        end = start + axis(.26 * size.x, bearing, .13 * size.z)
        sprays.append(([start, start.lerp(end, .55), end], bearing, 6, .91, .009 * size.x))
        for side in (-1, 1):
            root = start.lerp(end, .68)
            direction = bearing + side * 1.08
            tip = root + axis(.16 * size.x, direction, .045 * size.z)
            sprays.append(([root, root.lerp(tip, .56), tip], direction, 4, .95, .005 * size.x))
    return branches, sprays + crown_shoots, fruit_sites


def full_tree(young, detail):
    branches, sprays, fruit_sites = tree_framework(young)
    wood = fused_wood(branches, detail, young, root_scale=.36 if young else .53)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    rng = random.Random(2966)
    for i, (curve, bearing, count, outward, radius) in enumerate(sprays):
        if radius >= .007 or (detail and (outward >= .80 or i % 3 == 0)):
            tube(wood, curve, [radius, radius * .65, radius * .2], 'Twig', 4 if detail else 3)
        positions = (.38, .76) if count > 4 else (.61,)
        for j, portion in enumerate(positions):
            base, _ = sample_axis(curve, portion + rng.uniform(-.04, .04))
            turn = bearing + (-1)**(i + j) * rng.uniform(.47, 1.23)
            heading = axis(1, turn, rng.uniform(-.76, .28))
            length = rng.uniform(.38, .50) * (.76 if young else 1)
            width = length * rng.uniform(.74, .87)
            twist = rng.uniform(-.74, .74)
            exposed = .09 + .36 * (base.z / (.71 if young else 1) - 1.18) + .16 * outward
            exposed += .13 * math.cos(bearing * 1.71)
            accent = rng.random() < exposed
            if not detail and j == 1 and i % 3 == 0:
                continue
            leaf_card('Avocado', leaves, base, heading, length, width, twist, accent,
                      spray=True, detail=detail, bend=.32)

    print(f'Avocado {"young" if young else "mature"} LOD{0 if detail else 1}: {len(leaves.anchors)} leaf sprays',
          flush=True)
    for join, bearing, height in fruit_sites:
        # Keep the pear-shaped fruit visible below the layered cutout sprays.
        reach = axis(.101, bearing, -.215)
        socket = join + reach
        for mesh, age, factor in ((green, False, .82), (ripe, True, 1.14)):
            start = len(mesh.vertices)
            fruit(mesh, socket, height * factor, axis(.018 * factor, bearing, 0), age, detail)
            stem = 'Twig' if age else 'GreenStem'
            tube(mesh, [join, join.lerp(socket, .53) + UP * .006, socket], [.0043, .0034, .0027],
                 stem, 5 if detail else 4)
            mesh.anchor_from(start, join)
    return wood, leaves, green, ripe


def scale_geometry(geometries, factor):
    for mesh in geometries:
        for point in mesh.vertices:
            point *= factor
        mesh.anchors = [(start, end, height * factor) for start, end, height in mesh.anchors]


def stump(factor):
    mesh = Geometry()
    anchors = (*TRUNK[:3], (-.047, -.028, .38))
    points, widths = curved_axis(anchors, (*TRUNK_RADII[:3], .067), 3)
    sides = 24
    tube(mesh, points, widths, root_scale=.53, sides=sides, uv_span=(1, .81))
    cap = mesh.vertices[-sides:]
    coords = [(.5 + .46 * math.cos(i * math.tau / sides), .5 + .46 * math.sin(i * math.tau / sides)) for i in range(sides)]
    mesh.add(cap, [(tuple(range(sides)), coords)], 'CutWood')
    scale_geometry([mesh], factor)
    return mesh


def produce():
    mesh = Geometry()
    socket = Vector((.012, .003, .168))
    fruit(mesh, socket, .168, Vector((-.013, 0, 0)), True, True)
    tube(mesh, [socket, socket + Vector((.002, 0, .012)), socket + Vector((.007, 0, .023))],
         [.0055, .0043, .0035], 'Twig', 6)
    return mesh


def configure_view(scene):
    aim(scene.camera, (3.1, -6.0, 3.4), (0, 0, ORCHARD_HEIGHT * .51))
    scene.camera.data.ortho_scale = ORCHARD_HEIGHT * 1.38
    scene.render.resolution_x = 1120
    scene.render.resolution_y = 1060
    scene.render.filepath = '//'
    for workspace in bpy.data.workspaces:
        for screen in workspace.screens:
            for area in screen.areas:
                if area.type == 'VIEW_3D':
                    viewport = area.spaces.active
                    viewport.region_3d.view_distance = ORCHARD_HEIGHT * 1.85
                    viewport.region_3d.view_location = (0, 0, ORCHARD_HEIGHT * .51)
                    viewport.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
                    viewport.shading.color_type = 'TEXTURE'


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    mesh_output = WORK / 'Mesh'
    mesh_output.mkdir(parents=True, exist_ok=True)
    source_atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(source_atlas)
    configure_cutout(material)
    unity_mesh.configure(material, mesh_output)
    print(f'Player height {PLAYER_HEIGHT:.3f} m, target avocado {ORCHARD_HEIGHT:.4f} m.', flush=True)
    stage('NP_Avocado_Seed', [('Whole seed', seed())], SEED_SCALE, material, False)
    stage('NP_Avocado_Sprout', sprout(), SPROUT_SCALE, material, False)

    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        young_wood, young_leaves, _, _ = full_tree(young=True, detail=detail)
        wood, leaves, green, ripe = full_tree(young=False, detail=detail)
        parts = (young_wood, young_leaves, wood, leaves, green, ripe)
        scale_geometry(parts, BASE_SCALE)
        adjustment = height_adjustment((wood, leaves, green, ripe))
        adjust_height(parts, adjustment)
        stage('NP_Avocado_Young' + suffix, [('Sapling trunk and shoots', young_wood), ('Sapling leaves', young_leaves)],
              1, material, False, shade=shade_avocado)
        crown = [('Curved trunk and shoots', wood), ('Avocado leaf sprays', leaves)]
        stage('NP_Avocado_FruitForming' + suffix, crown + [('Green avocados', green)],
              1, material, False, shade=shade_avocado)
        stage('NP_Avocado_HarvestReady' + suffix, crown + [('Ripe avocados', ripe)],
              1, material, detail, shade=shade_avocado)
    stage('NP_Avocado_Stump', [('Cut avocado trunk', stump(BASE_SCALE))], 1, material, False)
    stage('NP_Avocado_Produce', [('Loose avocado', produce())], 1, material, False)

    scene = bpy.context.scene
    setup_lighting(scene)
    configure_view(scene)
    # Keep the editable model linked to the project maps once it is moved out of staging.
    for node in material.node_tree.nodes:
        if node.type == 'TEX_IMAGE':
            source = TEXTURES / Path(node.image.filepath).name
            node.image.filepath = bpy.path.relpath(str(source), start=str(SOURCE / 'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Avocado.blend'), relative_remap=False)
    print('AVOCADO_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
