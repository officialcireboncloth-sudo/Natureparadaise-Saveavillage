"""Build the oil palm's persistent leaf bases, arching pinnate fronds and fruit bunches."""
import math
import os
import random
import sys
from bisect import bisect_right
from itertools import accumulate
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_apple_blender import (
    Geometry, UP, aim, curved_axis, material_atlas,
    ring_shape, setup_lighting, stage, tube, unity_mesh,
)
from bake_oilpalm_fronds import FrondBaker
from fruit_tree_atlas import OIL_PALM_ATLAS, OIL_PALM_BAKED_ATLAS, SOURCE, TEXTURES
from fruit_tree_dimensions import OIL_PALM_STARTER_SCALES, ORCHARD_HEIGHT


WORK = Path(os.environ['NP_OILPALM_STAGE'])
LEAF = 'OilPalm/Leaf'
BARK = 'OilPalm/Bark'
GREEN = 'OilPalm/FruitForming'
RIPE = 'OilPalm/FruitReady'

# Trace the mature reference in front elevation, then give the fronds separate depths.
# Each entry is the longest leaflet length and the points along its curved rachis.
REFERENCE_FRONDS = (
    (.31, ((-.045,.08,1.20),(-.20,.09,1.83),(-.40,.14,2.43),(-.64,.22,2.69),(-.90,.30,2.71))),
    (.31, ((.05,.10,1.22),(.21,.11,1.85),(.39,.16,2.43),(.65,.21,2.71),(.94,.26,2.75))),
    (.34, ((-.08,.12,1.18),(-.35,.15,1.82),(-.69,.21,2.32),(-.99,.25,2.42),(-1.20,.29,2.28))),
    (.33, ((.08,.10,1.18),(.34,.14,1.83),(.68,.21,2.29),(.98,.27,2.38),(1.24,.32,2.24))),
    (.36, ((-.09,.04,1.12),(-.37,-.01,1.68),(-.74,-.04,2.02),(-1.08,-.07,1.99),(-1.28,-.08,1.79))),
    (.35, ((.10,.01,1.13),(.41,.04,1.70),(.76,.04,2.01),(1.10,.03,1.97),(1.28,.04,1.77))),
    (.35, ((-.10,-.02,1.07),(-.40,-.16,1.41),(-.77,-.30,1.68),(-1.11,-.36,1.62),(-1.24,-.37,1.20))),
    (.35, ((.10,-.04,1.10),(.42,-.10,1.43),(.79,-.18,1.65),(1.08,-.24,1.59),(1.20,-.27,1.21))),
    (.28, ((-.10,-.02,1.03),(-.30,-.11,1.24),(-.56,-.23,1.33),(-.80,-.32,1.17),(-.86,-.36,.79))),
    (.28, ((.10,-.03,1.03),(.29,-.12,1.23),(.52,-.22,1.31),(.72,-.33,1.15),(.80,-.36,.76))),
    (.35, ((-.04,-.10,1.14),(-.23,-.34,1.61),(-.52,-.63,1.97),(-.67,-.84,1.93),(-.73,-1.03,1.63))),
    (.34, ((.035,-.08,1.17),(.16,-.34,1.76),(.41,-.67,2.16),(.58,-.94,2.07),(.66,-1.09,1.74))),
    (.34, ((-.02,.13,1.20),(-.14,.44,1.82),(-.29,.76,2.20),(-.40,1.08,2.17),(-.45,1.25,1.81))),
    (.34, ((.05,.11,1.19),(.28,.41,1.68),(.49,.72,2.04),(.67,.93,2.01),(.76,1.11,1.69))),
    (.31, ((.00,.16,1.22),(.02,.54,1.75),(.03,.89,1.99),(.06,1.19,1.88),(.11,1.29,1.59))),
    (.29, ((-.06,.09,1.14),(-.32,.45,1.51),(-.70,.74,1.67),(-.93,.89,1.54),(-1.00,.94,1.22))),
    (.28, ((-.025,.15,1.19),(-.03,.46,1.76),(-.15,.79,2.30),(-.32,1.07,2.45),(-.51,1.24,2.39))),
    (.29, ((.026,.17,1.21),(.07,.44,1.81),(.18,.72,2.28),(.38,.91,2.40),(.58,1.04,2.30))),
    (.26, ((-.065,.16,1.15),(-.35,.47,1.73),(-.60,.75,2.04),(-.79,1.00,2.05),(-.98,1.15,1.92))),
    (.26, ((.068,.15,1.16),(.33,.40,1.77),(.62,.61,2.06),(.81,.82,2.08),(1.00,1.02,1.96))),
)


def radial(distance, yaw, rise=0):
    return Vector((distance * math.cos(yaw), distance * math.sin(yaw), rise))


def stalk(mesh, points, widths, sides, slot=LEAF, closed=False):
    start = len(mesh.surfaces)
    tube(mesh, points, widths, slot, sides, closed=closed)
    low, high = (.44, .56) if slot == LEAF else (.005, .61)
    v_start, v_end = (.18, .76) if slot == LEAF else (0, 1)
    mesh.surfaces[start:] = [(name, [(low + (high - low) * u, v_start + (v_end - v_start) * v) for u, v in uv])
                             for name, uv in mesh.surfaces[start:]]


def leaf_base(mesh, center, yaw, radius, half_width, height, projection, weathering):
    forward = radial(1, yaw)
    side = UP.cross(forward)
    base = center + forward * (radius * .89)
    tip = forward * (height * projection)
    # Keep the fibrous heel against the trunk and break the outline at the cut face.
    points = [base - UP * (height * .77),
              base + side * half_width + UP * (height * .08),
              base + side * (half_width * .60) + UP * (height * .51),
              base - side * (half_width * .60) + UP * (height * .51),
              base - side * half_width + UP * (height * .08),
              base + tip * .53 - UP * (height * .28),
              base + tip * .84 + side * (half_width * .71) + UP * (height * .28),
              base + tip * .81 - side * (half_width * .63) + UP * (height * .15),
              base + tip + side * (half_width * .11) + UP * (height * .22),
              base + tip * .23 + UP * (height * .59)]
    faces = [((0, 1, 6, 5), ((.33,.94),(.56,.46),(.54,.23),(.31,.62))),
                      ((0, 5, 7, 4), ((.12,.94),(.31,.62),(.15,.23),(.03,.46))),
                      ((5, 6, 8, 7), ((.32,.78),(.58,.27),(.32,.18),(.08,.27))),
                      ((1, 2, 6), ((.53,.81),(.56,.18),(.47,.53))),
                      ((4, 7, 3), ((.03,.81),(.13,.53),(.08,.18))),
                      ((2, 9, 8, 6), ((.94,.19),(.805,.15),(.805,.83),(.94,.75))),
             ((9, 3, 7, 8), ((.805,.15),(.67,.19),(.67,.75),(.805,.83)))]
    mesh.add(points, [(ids, [(u, .04 + v*.79 + weathering) for u, v in uv]) for ids, uv in faces], BARK)


def trunk(mesh, height, radius, detail, *, cut=False):
    points = [(0, 0, -.018), (-.022, .013, height*.31), (-.016, .010, height*.68), (.011, 0, height)]
    widths = [radius*1.22, radius*.97, radius*.88, radius*.85]
    trunk_axis, trunk_radii = curved_axis(points, widths, 3)
    stalk(mesh, trunk_axis, trunk_radii, 18 if detail else 12, BARK, closed=not cut)
    if cut:
        sides = 18 if detail else 12
        uv = [(.79 + .13 * math.cos(i * math.tau / sides), .50 + .31 * math.sin(i * math.tau / sides))
              for i in range(sides)]
        mesh.add(mesh.vertices[-sides:], [(tuple(range(sides)), uv)], BARK)
    count = 7 if detail else 6
    levels = max(3, round(height / .118))
    rng = random.Random(7156)
    for row in range(levels):
        for column in range(count):
            fraction = (row + .38 + .34 * column / count + rng.uniform(-.11, .11)) / levels
            z = height * fraction
            section = next(i for i in range(len(trunk_axis) - 1) if trunk_axis[i+1][2] >= z)
            a, b = trunk_axis[section:section+2]
            along = (z - a.z) / (b.z - a.z)
            center = a.lerp(b, along)
            radius_at_base = trunk_radii[section] * (1 - along) + trunk_radii[section+1] * along
            yaw = math.tau * (column + row*.41) / count + rng.uniform(-.16, .16)
            projection = rng.uniform(.26, .57) * (.69 + .31*fraction)
            leaf_base(mesh, center, yaw, radius_at_base, radius_at_base * rng.uniform(.43,.57),
                      height / levels * rng.uniform(1.19,1.63), projection, rng.uniform(0,.15))


def blade(mesh, base, azimuth, length, width, lift, droop, detail, *, bank=0):
    forward = radial(1, azimuth)
    side = UP.cross(forward)
    width_axis = (side * math.cos(bank) + UP * math.sin(bank)).normalized()
    rows = (0, .21, .44, .68, .87, 1) if detail else (0, .32, .67, 1)
    across = (-1, 0, 1) if detail else (-1, 1)
    points, uv, faces = [], [], []
    for row, t in enumerate(rows):
        center = base + forward * (length*t) + UP * (lift * math.sin(math.pi*t*.86) - droop*t**1.9)
        span = width*.5 * max(.024, math.sin(math.pi*t)**.77 * (1.20 - .40*t))
        center += side * (length*.045*t*t)
        for u in across:
            points.append(center + width_axis*u*span - UP * (abs(u) * span * .22))
            uv.append(((u+1)*.5, 1-t))
        if row:
            stride = len(across)
            for col in range(stride-1):
                a = (row-1)*stride+col
                face = (a,a+stride,a+stride+1,a+1)
                faces.append((face,[uv[i] for i in face]))
    mesh.add(points, faces, LEAF, anchor=base)


def leaflet(mesh, root, tangent, lateral, side, length, width, sag, lift, sweep, detail):
    # The pinnae fan out of the plane of the rachis, then curl down toward their tips.
    normal = tangent.cross(lateral)
    heading = (lateral * (.94 * side) + tangent * sweep + normal * lift).normalized()
    across = (tangent - heading * tangent.dot(heading) + normal * (.22 * side)).normalized()
    rows = (0, .22, .49, .76, 1) if detail else (0, .34, .70, 1)
    rails = (-1, 0, 1) if detail else (-1, 1)
    vertices, texcoords, faces = [], [], []
    for t in rows:
        spine = (root + heading * (length * t) + tangent * (.18 * length * t * t)
                 + normal * (.08 * length * math.sin(math.pi * t)) - UP * (sag * length * t**1.72))
        span = width * .5 * max(.018, math.sin(math.pi * t)**.53 * (1.04 - .25 * t))
        for rail in rails:
            vertices.append(spine + across * (rail * span) - normal * (.49 * abs(rail) * span))
            texcoords.append(((rail + 1) * .5, 1 - t))
    stride = len(rails)
    for row in range(len(rows) - 1):
        for column in range(stride - 1):
            a = row * stride + column
            quad = (a, a + stride, a + stride + 1, a + 1)
            if side > 0:
                quad = quad[::-1]
            faces.append((quad, [texcoords[i] for i in quad]))
    mesh.add(vertices, faces, LEAF)


def frond(stems, leaves, points, width, scale, detail, baker, slot, *, pairs=None):
    path, _ = curved_axis(points, [1] * len(points), 7)
    distances = list(accumulate(((b - a).length for a, b in zip(path, path[1:])), initial=0.0))

    def center(t):
        distance = min(1.0, max(0.0, t)) * distances[-1]
        i = min(bisect_right(distances, distance) - 1, len(path) - 2)
        mix = (distance - distances[i]) / (distances[i+1] - distances[i])
        return path[i].lerp(path[i+1], mix)

    positions = (0,.12,.25,.40,.56,.70,.81,.91,1) if detail else (0,.22,.45,.64,.83,1)
    stalk(stems, [center(t) for t in positions],
          [(.041*(1-t)**1.68 + .018*max(0,1-t/.24)**1.35 + .0014)*scale for t in positions],
          7 if detail else 5)
    if slot not in baker.charts:
        source = Geometry()
        frond_leaflets(source, center, width, distances[-1], detail, pairs)
        baker.bake(slot, source, center)
    baker.add_card(leaves, slot, detail)


def frond_leaflets(leaves, center, width, length, detail, pairs):
    if pairs is None:
        pairs = round((22 + length * 4.0) if detail else (13 + length * 1.8))
    # Groups of two or three share closely spaced roots and point in distinct planes.
    planes = ((.03,.37),(.43,.32),(-.29,.63),(.26,.43),(-.10,.54),(.36,.38),(-.37,.66))
    spacing = (.14, -.18, -.32, .33, .04, .42, .22)
    for side in (-1,1):
        for index in range(pairs):
            progress = index / (pairs - 1)
            shift = spacing[(index + (2 if side > 0 else 0)) % len(spacing)]
            at = min(.994, .226 + .764*progress + (shift + .26*side)/pairs)
            arch = math.sin(math.pi * max(0, (at - .222) / .786))
            taper = (.10 + .99 * arch**.68) * (1.13 - .19*at)
            root = center(at)
            tangent = (center(at+.01) - center(at-.01)).normalized()
            lateral = UP.cross(tangent).normalized()
            length = width*taper*(1+.075*math.sin(index*2.1+side))
            lift, sweep = planes[(index + (3 if side > 0 else 0)) % len(planes)]
            leaflet(leaves, root, tangent, lateral, side, length, length*(.178 if detail else .257),
                    .24 + .44*at, lift, sweep, detail)


def nursery(sprout, baker):
    stems, leaves = Geometry(), Geometry()
    height = .18 if sprout else .076
    base = Vector((0, 0, height))
    stalk(stems, [(0,0,0),(.011,0,height*.56),base], [.043,.030,.019], 8, closed=True)
    for i in range(5):
        azimuth = i * math.tau / 5 + .24
        blade(stems, radial(.024, azimuth, .008), azimuth, height*.75, height*.44,
              height*.63, height*.05, True)
    if sprout:
        for chart, index in enumerate((0,1,2,3,4,5,8,9,11)):
            width, points = REFERENCE_FRONDS[index]
            path = [(x*.43, y*.42, max(.16, (z-points[0][2])*.55 + height)) for x, y, z in points]
            frond(stems, leaves, path, width*.49, .36, False, baker, f'OilPalm/Sprout{chart}', pairs=17)
    else:
        blade(leaves, base, 3.24, .265, .107, .29, -.114, True, bank=1.19)
        blade(leaves, base, .08, .283, .112, .30, -.120, True, bank=-1.22)
    return [('Palm shoot and petioles', stems), ('Nursery leaves', leaves)]


def crown(phase, detail, baker):
    wood, stems, leaves = Geometry(), Geometry(), Geometry()
    young = phase == 'Young'
    radius = .229 if young else .245
    trunk(wood, 1.19, radius, detail)
    rng = random.Random(19173)
    for index, (width, points) in enumerate(REFERENCE_FRONDS):
        skew = Matrix.Rotation(rng.uniform(-.19, .19), 3, 'Z')
        lift = rng.uniform(-.094,.072)
        spread = rng.uniform(.93,1.08)
        if young and index in (6, 7, 14, 18, 19):
            continue
        if not detail and index in (14, 15, 18, 19):
            continue
        # Keep several rising tips, with longer outer fronds cascading below them.
        path = [skew @ Vector((x*spread, y*spread, z + lift - .095*max(0,i-2)))
                for i, (x,y,z) in enumerate(points)]
        frond(stems, leaves, path, width*1.08, 1, detail, baker, f'OilPalm/Frond{index}')
    for mesh in (stems, leaves):
        for point in mesh.vertices:
            point.x *= .938
    if young:
        for mesh in (wood, stems, leaves):
            for point in mesh.vertices:
                point.z -= .30 * min(1.0, max(0.0, point.z / 1.15))
                point.x *= 1.17
    return wood, stems, leaves


def fruitlet(mesh, base, axis, radius, length, tint, detail):
    offset = len(mesh.vertices)
    first_face = len(mesh.surfaces)
    profile = ((0,.39),(.26,.92),(.58,1),(.93,.68),(1.18,.13))
    ring_shape(mesh, (0,0,0), radius, profile, RIPE, 6 if detail else 5)
    mesh.surfaces[first_face:] = [(slot, [(u, max(.012,min(.988,v+tint))) for u,v in uv])
                                  for slot,uv in mesh.surfaces[first_face:]]
    rotation = UP.rotation_difference(axis.normalized())
    for point in mesh.vertices[offset:]:
        point.z *= length / (1.18*radius)
        point[:] = base + rotation @ point


def bunch(mesh, center, axis, radius, detail, seed):
    rng = random.Random(seed)
    turn = UP.rotation_difference(axis.normalized())
    start = len(mesh.vertices)
    ring_shape(mesh, (0,0,0), radius*.87,
               ((-1,.14),(-.76,.66),(-.34,.81),(.26,.78),(.71,.53),(.91,.17)), GREEN, 12 if detail else 8)
    for point in mesh.vertices[start:]:
        point[:] = center + turn @ point
    counts = (6,9,11,12,12,11,9,6) if detail else (4,8,10,8,5)
    for row, count in enumerate(counts):
        t = (row + .45) / len(counts)
        height = -1.06 + t*1.97
        reach = .79 * math.sin(math.pi*t)**.57
        for column in range(count):
            yaw = (column + .43 * row) * math.tau / count + rng.uniform(-.09,.09)
            position = radial(reach * radius, yaw, radius*(height + rng.uniform(-.057,.057)))
            direction = radial(1, yaw + .19, 1.30*height - .20 + rng.uniform(-.15,.15))
            fruitlet(mesh, center + turn @ position, turn @ direction, radius*rng.uniform(.183,.225),
                     radius*rng.uniform(.38,.52), rng.uniform(-.08,.105), detail)
    return center + axis.normalized()*radius*.73


def harvest_clusters(detail):
    mesh = Geometry()
    clusters = ((-.34,-.17,1.24,.164), (.06,-.415,1.36,.208), (.34,-.025,1.23,.150), (-.02,.27,1.32,.161))
    for i, (x, y, height, radius) in enumerate(clusters):
        center = Vector((x, y, height))
        axis = Vector((-1.6*x, -1.75*y, .86))
        neck = bunch(mesh, center, axis, radius, detail, 8116+i)
        base = Vector((x*.22, y*.24, height+.20))
        stalk(mesh, [base, base.lerp(neck,.6) + UP*.017, neck], [.043,.039,.035], 7 if detail else 5,
              BARK, closed=True)
    return mesh


def produce():
    mesh = Geometry()
    for center, direction, radius, seed in (((-.158,.075,.162),(-.66,-.77,.20),.158,536),
                                            ((.148,-.048,.149),(-.7,-.64,.22),.139,739)):
        axis = Vector(direction).normalized()
        neck = bunch(mesh, Vector(center), axis, radius, True, seed)
        stalk(mesh, [neck, neck + axis*(radius*.49)], [radius*.24,radius*.19], 8, BARK, closed=True)
    ground = min(vertex.z for vertex in mesh.vertices)
    for vertex in mesh.vertices:
        vertex.z -= ground
    return mesh


def fit_height(parts, height):
    scale = height / max(point.z for part in parts for point in part.vertices)
    for part in parts:
        for point in part.vertices:
            point.z *= scale


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    output = WORK / 'Mesh'
    output.mkdir(parents=True, exist_ok=True)
    atlas = WORK/'Atlas' if (WORK/'Atlas'/f'{OIL_PALM_ATLAS}_BaseColor.png').is_file() else TEXTURES
    baked = WORK / 'BakedAtlas'
    baker = FrondBaker(atlas)
    starter = {phase: nursery(phase == 'Sprout', baker) for phase in OIL_PALM_STARTER_SCALES}
    crowns = {(phase,detail): crown(phase, detail, baker) for detail in (True,False)
              for phase in ('Young','FruitForming')}
    baker.save(baked)
    material = material_atlas(baked, OIL_PALM_BAKED_ATLAS)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    clip = nodes.new('ShaderNodeMath')
    clip.operation = 'GREATER_THAN'
    clip.inputs[1].default_value = .42
    links.new(nodes.active.outputs['Alpha'], clip.inputs[0])
    links.new(clip.outputs[0], nodes.get('Principled BSDF').inputs['Alpha'])
    unity_mesh.configure(material, output)
    for phase, height in OIL_PALM_STARTER_SCALES.items():
        parts = starter[phase]
        fit_height([mesh for _, mesh in parts], height)
        stage(f'NP_OilPalm_{phase}', parts, height, material, False)
    for detail in (True, False):
        suffix = '' if detail else '_LOD1'
        for phase in ('Young','FruitForming','HarvestReady'):
            ripe = phase == 'HarvestReady'
            wood, stems, leaves = crown(phase, detail, baker) if ripe else crowns[phase,detail]
            # Young is scaled by the game's growth curve and is reused at full size between harvests.
            parts = (wood, stems, leaves, harvest_clusters(detail)) if ripe else (wood, stems, leaves)
            fit_height(parts, ORCHARD_HEIGHT)
            labels = ('Trunk with overlapping leaf scars', 'Arching rachises', 'Baked frond cards', 'Ripe oil palm bunches')
            stage(f'NP_OilPalm_{phase}{suffix}', list(zip(labels,parts)), 1, material, ripe and detail)
    stump = Geometry()
    trunk(stump, .34, .245, True, cut=True)
    stage('NP_OilPalm_Stump', [('Cut trunk',stump)], 1, material, False)
    stage('NP_OilPalm_Produce', [('Two ripe bunches',produce())], 1, material, False)
    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0, 0, ORCHARD_HEIGHT*.50)
    aim(scene.camera, (2.04,-7.80,3.5), focus)
    scene.camera.data.ortho_scale = 3.92
    scene.render.resolution_x,scene.render.resolution_y = 960,960
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
            node.image.filepath = bpy.path.relpath(str(image), start=str(SOURCE/'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'NP_OilPalm.blend'), relative_remap=False)
    print('OIL_PALM_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
