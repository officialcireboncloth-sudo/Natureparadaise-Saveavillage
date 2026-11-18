"""Build the durian's curved blades, bark and faceted fruit spikes for Unity."""
import math
import os
import random
import sys
from functools import cache
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_apple_blender import (
    GOLDEN_ANGLE, UP, Geometry, adjust_height, aim, curved_axis, fused_wood, height_adjustment,
    material_atlas, ring_shape, sample_axis, setup_lighting, stage, tube, unity_mesh,
)
from fruit_tree_atlas import (
    ATLAS, DURIAN_BARK_UV, DURIAN_PEEL_WIDTH, DURIAN_PULP_RECT, DURIAN_SECTION_CENTER,
    DURIAN_SECTION_RADIUS, DURIAN_SEED_RECT, SOURCE, TEXTURES,
)
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout


WORK = Path(os.environ['NP_DURIAN_STAGE'])
GREEN = 'Durian/Leaf'
BRONZE = 'Durian/LeafAccent'
WOOD = 'Durian/FruitForming'
RIPE = 'Durian/FruitReady'
TRUNK = ((0,0,-.026),(-.019,.009,.16),(-.024,.015,.39),(-.005,.009,.68),
         (.026,-.013,.97),(.056,-.029,1.28),(.065,-.020,1.58),(.049,.017,1.94),(.022,.053,2.33))
RADII = (.168,.142,.116,.093,.073,.055,.038,.022,.006)
# Attachment height, heading, spread, end height and diameter of the union.
LIMBS = (
    (.69,5.07,.99,1.56,.069), (.79,3.10,1.04,1.63,.065),
    (.88,.59,1.00,1.71,.061), (1.00,2.03,.98,1.81,.053),
    (1.12,4.18,.92,1.90,.047), (1.24,5.92,.89,1.97,.042),
    (1.37,2.53,.78,2.06,.035), (1.48,.76,.72,2.13,.031),
    (1.57,4.67,.61,2.18,.024), (1.70,2.88,.42,2.22,.017),
)


def offset(reach, yaw, rise):
    return Vector((reach * math.cos(yaw), reach * math.sin(yaw), rise))


def patch_uv(u, v, rect):
    left, top, right, bottom = rect
    return (left + (right - left)*u) / 256, (top + (bottom - top)*v) / 256


def paint_bark(mesh):
    left, right = DURIAN_BARK_UV
    mesh.surfaces = [(WOOD, [(left + (right-left)*u, v) for u,v in uv]) if slot in ('Bark','Twig')
                     else (slot, uv) for slot,uv in mesh.surfaces]
    return mesh


def blade(mesh, root, yaw, span, width, lift, droop, bronze, detail, roll=0):
    """Two curved sides give the green upper face and bronze underside their own UVs."""
    forward = offset(1,yaw,0)
    side = UP.cross(forward) * math.cos(roll) + UP * math.sin(roll)
    normal = forward.cross(side)
    sections = (.14,.32,.51,.70,.87) if detail else (.19,.46,.75)
    points = [root]
    uv = [(.50,.99)]
    for t in sections:
        half_width = width * .5 * math.sin(math.pi*t)**.70 * (1.13 - .30*t)
        center = root + forward * (span*t) + side * (roll*span*.11*t*t)
        center += UP * (lift*math.sin(math.pi*t*.83) - droop*t*t)
        points.extend((center - side*half_width, center + normal*(half_width*.17), center + side*half_width))
        uv.extend((u, 1 - t) for u in (.07,.50,.93))
    points.append(root + forward*span + side*(roll*span*.11) + UP*(lift*math.sin(math.pi*.83) - droop))
    uv.append((.50,.01))
    faces = [(0,1,2),(0,2,3)]
    for row in range(len(sections) - 1):
        a = 3*row + 1
        faces.extend(((a,a+3,a+4,a+1),(a+1,a+4,a+5,a+2)))
    a = 3*len(sections) - 2
    tip = len(points) - 1
    faces.extend(((a,tip,a+1),(a+1,tip,a+2)))
    mesh.add(points, [(face,[uv[i] for i in face]) for face in faces], BRONZE if bronze else GREEN, anchor=root)
    if detail:
        underside = [point - normal*(.0015 if i not in (0,tip) else .0009) for i,point in enumerate(points)]
        mesh.add(underside, [(face[::-1],[uv[i] for i in reversed(face)]) for face in faces], BRONZE, anchor=root)


def seed():
    mesh = Geometry()
    profile = ((0,.14),(.08,.35),(.27,.47),(.53,.51),(.75,.42),(.92,.24),(1,.06))
    ring_shape(mesh, (0,0,0), .071, profile, RIPE, 18)
    for point in mesh.vertices:
        point.x += point.z*.51
        point.y *= .79
        point.z *= .89
    mesh.surfaces = [(slot,[patch_uv(v,u,DURIAN_SEED_RECT) for u,v in uv]) for slot,uv in mesh.surfaces]
    return mesh


def sprout():
    stem, leaves = Geometry(), Geometry()
    points = [(0,0,0),(-.008,.002,.082),(.004,-.008,.173),(.014,-.006,.235)]
    tube(stem, *curved_axis(points,[.010,.008,.006,.0038],4), 'GreenStem', 7, closed=True)
    blade(leaves, Vector(points[-2]),3.35,.145,.071,.092,.031,False,True,roll=-.57)
    blade(leaves, Vector(points[-1]),5.85,.161,.074,.110,.029,False,True,roll=.51)
    return [('Stem',stem),('First tapered leaves',leaves)]


@cache
def ico_cells(detail):
    mesh = bmesh.new()
    bmesh.ops.create_icosphere(mesh, subdivisions=3 if detail else 2, radius=1)
    for vertex in mesh.verts:
        x,y,z = vertex.co
        jitter = Vector((math.sin(13*y + 17*z), math.sin(11*z - 19*x), math.sin(9*x + 15*y)))
        vertex.co = (vertex.co + jitter * (.027 if detail else .037)).normalized()
    cells = tuple(tuple(point.co.copy() for point in face.verts) for face in mesh.faces)
    mesh.free()
    return cells


def spikes(mesh, cells, center, radius, ripe, detail):
    """Every triangular shell cell becomes a pointed mesh with three shaded sides."""
    rng = random.Random(6307)
    slot = RIPE if ripe else WOOD
    count = 0
    for a,b,c in cells:
        face_center = (a+b+c) / 3
        direction = face_center.normalized()
        spike_height = (.14 if detail else .18) * rng.uniform(.75,1.23)
        point = face_center + direction * spike_height + (b - a) * rng.uniform(-.11,.11)
        profile = Vector((1,.92,1.19))
        base = [(center + corner * profile * radius) for corner in (a,b,c)]
        peak = center + point * profile * radius
        u = rng.uniform(.04,.42)
        v = rng.uniform(.47,.95)
        tip_uv = (u+.014, rng.uniform(.08,.34))
        for index in range(3):
            # Duplicate corner vertices so a spine keeps three readable flat faces.
            triangle = (base[index],base[(index+1) % 3],peak)
            tex = ((u-.018,v),(u+.018,v-.06),tip_uv)
            mesh.add(triangle,[((0,1,2),tex)],slot)
        count += 1
    return count


def fruit_cluster(mesh, anchor, yaw, radius, detail, ripe):
    start = len(mesh.vertices)
    socket = anchor + offset(.126,yaw,-.182)
    tip = socket - UP*(radius*1.12)
    tube(mesh,[anchor,anchor.lerp(socket,.54) + UP*.026,socket],
         [.020,.018,.014], 'Twig', 8 if detail else 5)
    count = spikes(mesh, ico_cells(detail), tip, radius, ripe, detail)
    mesh.anchor_from(start,anchor)
    return count


def canopy(young, detail):
    rng = random.Random(51051 if young else 81429)
    scale = Vector((.57,.57,.74)) if young else Vector((1,1,1))
    trunk = [Vector(point)*scale for point in TRUNK]
    widths = [radius * (.63 if young else 1) for radius in RADII]
    spine, _ = curved_axis(trunk,widths,8)
    branches, sprays, crop = [(trunk,widths)], [], []
    for index,(fork,yaw,reach,end,radius) in enumerate(LIMBS[:6] if young else LIMBS):
        z = fork*scale.z
        a,b = next((a,b) for a,b in zip(spine,spine[1:]) if a.z <= z <= b.z)
        root = a.lerp(b,(z-a.z)/(b.z-a.z))
        turn = rng.uniform(-.22,.21)
        rise = end - fork
        axis = [root,root + offset(reach*.14,yaw-turn*.18,rise*.28)*scale,
                root + offset(reach*.43,yaw,rise*.59)*scale,
                root + offset(reach*.75,yaw+turn*.56,rise*.84)*scale,
                root + offset(reach,yaw+turn,rise)*scale]
        width = radius * (.62 if young else 1)
        taper = [width,width*.86,width*.66,width*.40,width*.19]
        branches.append((axis,taper))
        guide, _ = curved_axis(axis,taper,6)
        placements = (.54,.75,.95) if young else (.40,.56,.69,.82,.94,1)
        for j,t in enumerate(placements):
            base, _ = sample_axis(guide,t)
            fan_yaw = yaw + (-1)**(index+j)*rng.uniform(.55,1.24)
            tip = base + offset(rng.uniform(.18,.31),fan_yaw,rng.uniform(.02,.149))*scale
            sprays.append((base,tip,fan_yaw,t,False))
        if not young and index in (0,1,2,3,5,7):
            attach, _ = sample_axis(guide,.80 if index==7 else .87)
            crop.append((attach,yaw+turn,rng.uniform(.145,.186)))

        # Shorter inward fans fill the gaps beside forks while leaving the hanging fruit clear.
        for j,t in enumerate((.73,) if young else (.58,.81)):
            base, _ = sample_axis(guide,t)
            fan_yaw = yaw + (-1)**(index+j) * (1.46 + .13*j)
            tip = base + offset(.23,fan_yaw,.155)*scale
            sprays.append((base,tip,fan_yaw,t,True))

    for j,t in enumerate((.55,.66,.76,.84,.90,.96,.99)):
        base, _ = sample_axis(spine,t)
        yaw = .43 + j*GOLDEN_ANGLE
        reach = rng.uniform(.17,.26) if t >= .90 else rng.uniform(.25,.39)
        tip = base + offset(reach,yaw,.16 if t >= .90 else .12)*scale
        sprays.append((base,tip,yaw,.86,False))

    wood = fused_wood(branches,detail,young,root_scale=.53 if young else .82)
    leaves, green, ripe = Geometry(), Geometry(), Geometry()
    blade_count = 0
    for index,(root,tip,yaw,t,inside) in enumerate(sprays):
        if detail or t >= .79 or index % 2 == 0:
            tube(wood,[root,root.lerp(tip,.53)+UP*.018,tip],
                 [.011 if t>.78 else .008,.006,.0026],'Twig',5 if detail else 4)
        count = (4 if detail else 3) if inside else (6 if t >= .78 else 5)
        for j in range(count):
            base = root.lerp(tip,.31 + .11*j)
            angle = yaw + (j-(count-1)*.5)*rng.uniform(.52,.91)
            span = (rng.uniform(.28,.36) if inside else rng.uniform(.34,.47)) * (.74 if young else 1)
            width = span*rng.uniform(.41,.48)
            lift = span*rng.uniform(.18,.32)
            droop = span*rng.uniform(.29,.49)
            roll = rng.uniform(-.61,.61)
            warm = rng.random() < (.20 if young else .10)
            if inside or detail or j != 1 or index % 3 == 0:
                blade(leaves,base,angle,span,width,lift,droop,warm,detail,roll)
                blade_count += 1
    spike_count = 0
    for anchor,yaw,radius in crop:
        fruit_cluster(green,anchor,yaw,radius*.65,detail,False)
        spike_count += fruit_cluster(ripe,anchor,yaw,radius,detail,True)
    print(f'Durian {"young" if young else "mature"} LOD{0 if detail else 1}: '
          f'{blade_count} blades, {spike_count} geometric spines',flush=True)
    return paint_bark(wood), leaves, paint_bark(green), paint_bark(ripe)


def stump():
    mesh = Geometry()
    sides = 24
    tube(mesh,*curved_axis(TRUNK[:3],RADII[:3],4),sides=sides,root_scale=.82,uv_span=(1,.79))
    uv = [(.5 + .45*math.cos(i*math.tau/sides), .5 + .45*math.sin(i*math.tau/sides)) for i in range(sides)]
    mesh.add(mesh.vertices[-sides:],[(tuple(range(sides)),uv)],'CutWood')
    return paint_bark(mesh)


def cut_half(mesh, center, radius):
    sides, rows = 18, 6
    rings = []
    for row in range(rows+1):
        angle = .07 + row*(math.pi*.5-.07)/rows
        rings.append([Vector((math.sin(angle)*math.cos(i*math.tau/sides),
                              math.sin(angle)*math.sin(i*math.tau/sides), -math.cos(angle)))
                      for i in range(sides)])
    cells = []
    pole = Vector((0,0,-1))
    for i in range(sides):
        cells.append((pole,rings[0][(i+1)%sides],rings[0][i]))
    for low,high in zip(rings,rings[1:]):
        for i in range(sides):
            j = (i+1) % sides
            cells.extend(((low[i],low[j],high[j]),(low[i],high[j],high[i])))
    start = len(mesh.vertices)
    spikes(mesh, cells, Vector((0,0,0)), radius, True, True)
    x,y = DURIAN_SECTION_CENTER
    section = [(0,1),(.055,.93),(.075,.74),(.083,.09)]
    points, uv, faces = [], [], []
    for z,width in section:
        for i in range(sides):
            angle = i*math.tau/sides
            u,v = width*math.cos(angle), width*math.sin(angle)
            points.append((radius*u,radius*v*.92,z*radius))
            uv.append(((x+DURIAN_SECTION_RADIUS*u)/256,(y-DURIAN_SECTION_RADIUS*v)/256))
    for row in range(len(section)-1):
        for i in range(sides):
            a = row*sides+i
            b = row*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(range(len(points)-sides,len(points))))
    mesh.add(points,[(face,[uv[i] for i in face]) for face in faces],RIPE)

    for px, py, angle in ((-.41,.15,-.35),(.31,.25,.22),(.09,-.40,.50)):
        part = len(mesh.vertices)
        first_face = len(mesh.surfaces)
        core = Vector((radius*px,radius*py,radius*.12))
        size = radius*.39
        ring_shape(mesh,(0,0,0),size,((-.55,.25),(-.44,.60),(-.16,.91),(.20,.93),
                                     (.57,.71),(.74,.32),(.78,.06)),RIPE,14)
        for point in mesh.vertices[part:]:
            x,y,z = point
            point[:] = core + Vector((x*.78*math.cos(angle)-1.30*y*math.sin(angle),
                                      x*.78*math.sin(angle)+1.30*y*math.cos(angle),z))
        mesh.surfaces[first_face:] = [(slot,[patch_uv(u,v,DURIAN_PULP_RECT) for u,v in tex])
                                      for slot,tex in mesh.surfaces[first_face:]]
    orientation = UP.rotation_difference(Vector((.32,-.82,.53)).normalized())
    for point in mesh.vertices[start:]:
        point[:] = center + orientation @ point


def produce():
    mesh = Geometry()
    center = Vector((-.122,.034,.184))
    radius = .160
    spikes(mesh,ico_cells(True),center,radius,True,True)
    socket = center+UP*(radius*1.12)
    tube(mesh,[socket,socket+Vector((.021,.013,.032)),socket+Vector((.027,.009,.081))],
         [.020,.018,.014],'Twig',9,closed=True)
    cut_half(mesh,Vector((.161,-.063,.135)),.143)
    floor = min(point.z for point in mesh.vertices)
    for point in mesh.vertices:
        point.z -= floor
    return paint_bark(mesh)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    output = WORK / 'Mesh'
    output.mkdir(parents=True,exist_ok=True)
    atlas = WORK / 'Atlas' if (WORK / 'Atlas' / f'{ATLAS}_BaseColor.png').is_file() else TEXTURES
    material = material_atlas(atlas)
    configure_cutout(material)
    unity_mesh.configure(material,output)
    stage('NP_Durian_Seed',[('Brown durian seed',seed())],.10,material,False)
    stage('NP_Durian_Sprout',sprout(),.26,material,False)
    for detail in (True,False):
        suffix = '' if detail else '_LOD1'
        wood, leaves, green, ripe = canopy(False,detail)
        young_wood, young_leaves, _, _ = canopy(True,detail)
        crown = (wood,leaves,green,ripe)
        adjust_height((*crown,young_wood,young_leaves),height_adjustment(crown))
        stage('NP_Durian_Young'+suffix,[('Young trunk',young_wood),('Young lanceolate leaves',young_leaves)],
              1,material,False)
        foliage = [('Durian bark and roots',wood),('Curved green and bronze leaves',leaves)]
        stage('NP_Durian_FruitForming'+suffix,foliage+[('Green spiked fruit',green)],1,material,False)
        stage('NP_Durian_HarvestReady'+suffix,foliage+[('Olive gold spiked fruit',ripe)],1,material,detail)
    stage('NP_Durian_Stump',[('Durian stump',stump())],1,material,False)
    stage('NP_Durian_Produce',[('Whole fruit and open spiked half',produce())],1,material,False)
    scene = bpy.context.scene
    setup_lighting(scene)
    focus = (0,0,ORCHARD_HEIGHT*.50)
    aim(scene.camera,(3.20,-7.0,4.0),focus)
    scene.camera.data.ortho_scale = 3.95
    scene.render.resolution_x,scene.render.resolution_y = 1080,1060
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
            node.image.filepath = bpy.path.relpath(str(image),start=str(SOURCE / 'Blender'))
    bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'NP_Durian.blend'),relative_remap=False)
    print('DURIAN_BLENDER_PASS',flush=True)


if __name__ == '__main__':
    main()
