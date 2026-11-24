"""Build the orchard coconut stages from the SoStylized coastal palm in Blender."""

import math
import os
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'ArtSource/FruitTrees'
PACK = ROOT / 'Assets/SoStylized/Environment/Trees'
OUT = Path(os.environ.get('NP_COCONUT_STAGE', ROOT / 'tmp/Coconut'))
ATLAS_SIZE = 2048
CHARTS = {
    'frond': (8, 1032, 1008, 1008),
    'bark': (1032, 1032, 1008, 1008),
    'blade': (8, 8, 496, 1008),
    'fruit': (520, 520, 496, 496),
    'forming': (1032, 520, 496, 496),
    'mutation': (1544, 520, 496, 496),
    'stem': (520, 8, 496, 496),
    'cut': (1032, 8, 496, 496),
}
STAGE_SCALE = {'Seedling': .26, 'Young': .52}
sys.path.insert(0, str(ROOT / 'ArtSource/Crops/Scripts'))
import plant_blender_common as mesh_io


def pixels(name, width, height):
    image = bpy.data.images.load(str(PACK / 'Textures' / (name + '.png')))
    image.colorspace_settings.name = 'Non-Color'
    image.scale(width, height)
    data = np.empty(width * height * 4, dtype='f')
    image.pixels.foreach_get(data)
    bpy.data.images.remove(image)
    return data.reshape(height, width, 4)


def normal_from_height(height, strength):
    dy, dx = np.gradient(height)
    nx, ny = -dx * strength, -dy * strength
    length = np.sqrt(1 + nx * nx + ny * ny)
    return np.stack((nx / length, ny / length, 1 / length), -1) * .5 + .5


def frond_texture(u, v):
    # The donor FBX places alternate sides of the crown on two feather charts.
    # Paint broad, curved leaflets into those charts without increasing mesh density.
    variant = (u >= .5).astype('f')
    across = (u * 2) % 1 - .5
    center = .009 * np.sin(math.pi * v * 1.45) * (1 - 2 * variant)
    x = across - center
    rachis = .011 * (1 - np.clip((v - .07) / .94, 0, 1)) + .0017
    alpha = np.clip((rachis - abs(x)) / .0025 + .5, 0, 1) * (v >= .04)
    progress = np.clip((v - .13) / .88, 0, 1)
    rgb = np.zeros((*u.shape, 3), dtype='f')
    rgb[:] = (.145, .34, .083)
    rgb += progress[..., None] * (.081, .137, .042)
    rib_color = np.array((.36, .48, .11))
    rgb = np.where((alpha > .2)[..., None], rib_color, rgb)
    relief = alpha * .11
    for side in (-1, 1):
        for leaf in range(19):
            s = leaf / 18
            base = .17 + .781 * s + .011 * side + .004 * np.sin(s * 22)
            span = (.414 * (1 - s ** 2.2) + .024) * (.91 + .09*np.sin(leaf * 1.24 + side))
            t = (side * x + .006) / span
            sample = np.clip(t, 0, 1)
            curve = base + (.085 - .039*s) * sample ** 1.36 + .016 * np.sin(math.pi * sample)
            thickness = (.024 + .010 * np.sin(s * math.pi)) * (.92 - .30*s)
            profile = thickness * (np.sin(math.pi * sample ** .72) ** .78)
            distance = (v - curve) / np.maximum(.0001, profile)
            cover = np.clip((profile - abs(v - curve)) / .0014 + .5, 0, 1)
            cover *= (t > -.012) & (t < 1)
            ridge = np.exp(-(distance / .142) ** 2)
            veins = (.5 + .5 * np.cos(math.tau * (68 * sample - .45 * abs(distance)))) ** 8
            tone = .12 * np.sin(1.77 * leaf + side) + .15 * variant + .27 * s
            leaf_color = np.array((.12, .31, .055)) + tone[..., None] * (.36, .54, .13)
            leaf_color += (ridge * .13 + veins * .026 + sample * .045)[..., None] * (1, .88, .32)
            leaf_color += np.clip(distance, -.8, .8)[..., None] * (.052, .071, .021)
            weight = cover[..., None] * (1 - .72 * (abs(x) < rachis))[..., None]
            rgb = rgb * (1 - weight) + leaf_color * weight
            relief = np.maximum(relief, cover * (ridge * .035 + veins * .013))
            alpha = np.maximum(alpha, cover)
    return rgb, alpha, relief


def atlas():
    maps = {name: np.ones((ATLAS_SIZE, ATLAS_SIZE, 4), dtype='f')
            for name in ('BaseColor', 'Normal', 'SurfaceMask')}
    maps['Normal'][:, :, :3] = (.5, .5, 1)
    for chart, (x, y, width, height) in CHARTS.items():
        u, v = np.meshgrid(np.linspace(0, 1, width), np.linspace(0, 1, height))
        alpha = np.ones_like(u)
        surface = np.zeros_like(u)
        smoothness = .24
        if chart == 'frond':
            rgb, alpha, surface = frond_texture(u, v)
            smoothness = .34
        elif chart == 'bark':
            color = pixels('T_CoconutPalmBark_BC', width, height)[:, :, :3]
            grain = np.sin(math.tau * (37 * u + .14 * np.sin(math.tau * 3 * v)))
            seam_position = (18 * v + .08 * np.sin(math.tau * (2 * u + v))) % 1
            seam = np.exp(-((seam_position - .13) / .074) ** 2)
            lip = np.exp(-((seam_position - .29) / .079) ** 2)
            rgb = color * (.64, .63, .70)
            rgb += (grain * .018 + .071 * lip - .117 * seam)[..., None] * (1, .81, .69)
            normal = pixels('T_CoconutPalmBark_N', width, height)[:, :, :3]
            normal = normal * .58 + normal_from_height(lip * .15 - seam * .21, 5.5) * .42
            smoothness = .17
        elif chart == 'blade':
            # An unsegmented juvenile blade with a strong midrib and curved parallel veins.
            rib = np.exp(-((u - .5) / .019) ** 2)
            slope = v - .094 * np.abs(u - .5) ** 1.16
            veins = (.5 + .5 * np.cos(math.tau * 30 * slope)) ** 10
            face = .40 * (1 - abs(u - .5) * 1.7) + .32 * v + .15 * (u < .5)
            rgb = np.array((.115, .244, .064)) + face[..., None] * (.32, .40, .10)
            rgb += (rib * .29 + veins * .045)[..., None] * (1, .90, .30)
            surface = rib * .18 + veins * .025
            smoothness = .38
        elif chart in ('fruit', 'forming', 'mutation'):
            # Fine husk fibres, subtle three-sided ribbing and green-to-olive ends.
            hue = {'fruit': (.32, .48, .115), 'forming': (.205, .36, .098),
                   'mutation': (.55, .52, .15)}[chart]
            fibre = .65 * np.sin(math.tau * 61 * u + .8 * np.sin(v * 6))
            fibre += .35 * np.sin(math.tau * 173 * u + .9 * np.sin(v * 9))
            ribs = np.cos(math.tau * 3 * u)
            ends = np.maximum(0, 1 - 8 * v) + np.maximum(0, 1 - 8 * (1 - v))
            color = .88 + .21 * np.sin(math.pi * v) + .053 * ribs + .028 * fibre - .18 * ends
            rgb = np.asarray(hue) * color[..., None]
            rgb += np.sin(math.pi * v)[..., None] * (.063, .066, .014)
            surface = fibre * .02 + ribs * .06
            smoothness = .37
        elif chart == 'stem':
            fibre = np.sin(math.tau * 24 * u + .4 * np.sin(v * 11))
            rgb = np.stack((.28 + .065 * fibre, .24 + .13 * v + .04 * fibre,
                            .091 + .023 * fibre), -1)
            surface = fibre * .04
        else:
            # Coconut trunks are fibrous monocots. Use dotted vascular bundles on the cut.
            rng = np.random.default_rng(811)
            flecks = (rng.random(u.shape) > .965).astype('f')
            rim = np.clip((np.hypot(u - .5, v - .5) - .32) * 2.2, 0, .4)
            rgb = np.asarray((.66, .49, .31)) - (rim + flecks * .24)[..., None]
            surface = flecks * .06

        interior = np.s_[y:y + height, x:x + width, :]
        maps['BaseColor'][interior] = np.dstack((np.clip(rgb, 0, 1), alpha))
        if chart != 'bark':
            normal = normal_from_height(surface, 1.6)
        maps['Normal'][interior][:, :, :3] = normal
        maps['SurfaceMask'][interior][:] = (0, 1, 0, smoothness)
        for channel, sheet in maps.items():
            tile = sheet[interior]
            sheet[y-8:y+height+8, x-8:x+width+8] = np.pad(
                tile, ((8, 8), (8, 8), (0, 0)), mode='edge')

    results = {}
    for channel, buffer in maps.items():
        name = 'NP_Coconut_' + channel
        filepath = OUT / (name + '.png')
        image = bpy.data.images.new(name, width=ATLAS_SIZE, height=ATLAS_SIZE, alpha=True)
        image.colorspace_settings.name = 'Non-Color'
        image.pixels.foreach_set(buffer.ravel())
        image.filepath_raw = str(filepath)
        image.file_format = 'PNG'
        image.save()
        results[channel] = filepath
        bpy.data.images.remove(image)
    return results


def material(maps):
    mat = bpy.data.materials.new('Coconut Atlas')
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    shading = nodes.get('Principled BSDF')
    albedo = nodes.new('ShaderNodeTexImage')
    albedo.image = bpy.data.images.load(str(maps['BaseColor']))
    alpha_clip = nodes.new('ShaderNodeMath')
    alpha_clip.operation = 'GREATER_THAN'
    alpha_clip.inputs[1].default_value = .42
    links.new(albedo.outputs['Color'], shading.inputs['Base Color'])
    links.new(albedo.outputs['Alpha'], alpha_clip.inputs[0])
    links.new(alpha_clip.outputs[0], shading.inputs['Alpha'])
    normal = nodes.new('ShaderNodeTexImage')
    normal.image = bpy.data.images.load(str(maps['Normal']))
    normal.image.colorspace_settings.name = 'Non-Color'
    normal_map = nodes.new('ShaderNodeNormalMap')
    normal_map.inputs['Strength'].default_value = .6
    links.new(normal.outputs['Color'], normal_map.inputs['Color'])
    links.new(normal_map.outputs['Normal'], shading.inputs['Normal'])
    shading.inputs['Roughness'].default_value = .67
    shading.inputs['Specular IOR Level'].default_value = .28
    albedo.image.pack()
    normal.image.pack()
    return mat


def atlas_uv(chart, uv):
    x, y, width, height = CHARTS[chart]
    u, v = (max(0, min(1, a)) for a in uv)
    return ((x + .5 + u * (width - 1)) / ATLAS_SIZE,
            (y + .5 + v * (height - 1)) / ATLAS_SIZE)


class Geometry:
    def __init__(self):
        self.points = []
        self.faces = []
        self.uvs = []

    def add(self, positions, faces, uvs, chart):
        offset = len(self.points)
        self.points.extend(positions)
        self.faces.extend(tuple(offset + i for i in face) for face in faces)
        self.uvs.extend(atlas_uv(chart, uv) for uv in uvs)

    def build(self, stage, mat):
        name = 'NP_Coconut_' + stage
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.points, [], self.faces)
        mesh.update()
        uv = mesh.uv_layers.new(name='AtlasUV')
        for loop, coord in zip(uv.data, self.uvs):
            loop.uv = coord
        for face in mesh.polygons:
            face.use_smooth = True
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        obj.data.materials.append(mat)
        mesh_io.clean_export_triangles(obj)
        return obj


def pack_palm():
    bpy.ops.import_scene.fbx(filepath=str(PACK / 'Coastal/Meshes/SM_CoconutPalm02.fbx'))
    obj, = bpy.context.selected_objects
    mesh = obj.data
    positions = [obj.matrix_world @ vert.co for vert in mesh.vertices]
    minimum = min(p.z for p in positions)
    factor = 4.05 / (max(p.z for p in positions) - minimum)
    positions = [Vector((p.x * factor * .96, p.y * factor * .96, (p.z - minimum) * factor))
                 for p in positions]
    charts = ['frond' if 'Leaves' in mat.name else 'bark' for mat in mesh.materials]
    patches = {}
    for i, chart in enumerate(charts):
        faces = [p for p in mesh.polygons if p.material_index == i]
        uv = [tuple(mesh.uv_layers.active.data[loop].uv) for p in faces for loop in p.loop_indices]
        indices = sorted({vertex for p in faces for vertex in p.vertices})
        remap = {index: n for n, index in enumerate(indices)}
        patches[chart] = ([positions[index] for index in indices],
                          [tuple(remap[index] for index in p.vertices) for p in faces], uv)
    bpy.data.objects.remove(obj, do_unlink=True)
    return patches


def palm(geometry, patches, young=False):
    for chart, (vertices, faces, uvs) in patches.items():
        points = []
        for point in vertices:
            x, y, z = point
            swell = 1 + .36 * max(0, 1 - z / 2.3) if chart == 'bark' else 1.085
            points.append(Vector((x * swell, y * swell, z)))
        if young:
            # Compress the exposed trunk and retain a deep, wide crown on the young palm.
            breakpoint = 1.65
            points = [(p.x * .62, p.y * .62, min(p.z, breakpoint) * .40 + max(0, p.z-breakpoint) * .65)
                      for p in points]
            points = [Vector(p) / STAGE_SCALE['Young'] for p in points]
        geometry.add(points, faces, uvs, chart)


def radial_surface(geometry, rings, chart, sides=16, lobe=0, cap_chart='cut'):
    """Build a smooth tapered trunk, stalk or husk along the given chain of rings."""
    points = []
    for row, (center, radius) in enumerate(rings):
        tangent = (Vector(rings[min(row+1, len(rings)-1)][0]) - Vector(rings[max(row-1, 0)][0])).normalized()
        side = tangent.cross(Vector((0, 1, 0))).normalized()
        up = tangent.cross(side)
        for column in range(sides):
            theta = math.tau * column / sides
            size = radius * (1 + lobe * math.cos(3 * theta))
            points.append(Vector(center) + size * (math.cos(theta) * side + math.sin(theta) * up))
    faces = []
    uv = []
    for j in range(len(rings)-1):
        for i in range(sides):
            n = (i + 1) % sides
            faces.append((j*sides+i, j*sides+n, (j+1)*sides+n, (j+1)*sides+i))
            a, b = j / (len(rings)-1), (j+1) / (len(rings)-1)
            uv.extend(((i/sides, a), ((i+1)/sides, a), ((i+1)/sides, b), (i/sides, b)))
    geometry.add(points, faces, uv, chart)
    cap_uv = [(.5 + .47 * math.cos(math.tau*i/sides), .5 + .47 * math.sin(math.tau*i/sides)) for i in range(sides)]
    for end, indices in ((0, tuple(reversed(range(sides)))), (-1, tuple(range(sides)))):
        rim = points[:sides] if end == 0 else points[-sides:]
        geometry.add(rim, [indices], [cap_uv[i] for i in indices], cap_chart)


def blade(geometry, base, angle, reach, rise, arch, width):
    forward = Vector((math.cos(angle), math.sin(angle), 0))
    across = Vector((-forward.y, forward.x, 0))
    offsets = (-1, -.50, 0, .50, 1)
    steps = 12
    points = []
    faces = []
    uv = []
    for i in range(steps + 1):
        t = i / steps
        spread = width * (.018 + .982 * math.sin(math.pi * t) ** .82) * (.88 + .20 * t)
        center = Vector(base) + forward * reach * t ** .95
        center.z += rise * t + arch * math.sin(math.pi * t)
        for a in offsets:
            offset = across * a * spread
            offset.z = -spread * (.26 + .48 * t) * abs(a) ** 1.5
            points.append(center + offset)
    stride = len(offsets)
    for i in range(steps):
        for j in range(stride-1):
            faces.append((i*stride+j, (i+1)*stride+j, (i+1)*stride+j+1, i*stride+j+1))
            uv.extend(((j/4, i/steps), (j/4, (i+1)/steps),
                       ((j+1)/4, (i+1)/steps), ((j+1)/4, i/steps)))
    geometry.add(points, faces, uv, 'blade')


def seedling(geometry):
    radial_surface(geometry, [((0, 0, 0), .063), ((0, 0, .08), .076), ((0, 0, .23), .040),
                              ((.01, -.01, .37), .023), ((.013, -.02, .49), .009)], 'stem', 12)
    rows = [
        (-.10, .53, .05, .17, .081, .16),
        (3.2, .48, .18, .19, .095, .21),
        (5.0, .43, .31, .17, .091, .27),
        (2.0, .44, .50, .14, .105, .35),
        (.71, .27, .55, .12, .083, .40),
        (4.1, .15, .64, .07, .055, .41),
    ]
    for angle, reach, rise, arch, width, height in rows:
        root = Vector((0, 0, height))
        petiole = root + Vector((math.cos(angle) * .08, math.sin(angle) * .08, .08))
        radial_surface(geometry, [(root, .016), (petiole, .009)], 'stem', 8)
        blade(geometry, petiole, angle, reach, rise, arch, width)
    geometry.points = [Vector(p) / STAGE_SCALE['Seedling'] for p in geometry.points]


def coconut(geometry, top, direction, length, chart):
    axis = Vector(direction).normalized()
    top = Vector(top)
    bottom = top + axis * length
    profile = [(0, .025), (.10, .207), (.27, .35), (.46, .39),
               (.67, .34), (.84, .23), (.96, .093), (1, .027)]
    radial_surface(geometry, [(bottom.lerp(top, t), length*r) for t, r in profile],
                   chart, sides=14, lobe=.082, cap_chart='stem')


def bunch(geometry, forming):
    # Each fruit hangs from a member of the cluster just under its crown.
    for i in range(5):
        a = .84 + i * math.tau / 5
        axis = Vector((math.cos(a), math.sin(a), 0))
        anchor = Vector((-.013, -.026, 2.63)) + axis * .074
        neck = anchor + axis * .13 - Vector((0, 0, .08))
        top = neck + axis * (.049 + .014 * (i % 2)) - Vector((0, 0, .073 + .041 * (i % 3)))
        radial_surface(geometry, [(top, .014), (neck, .020), (anchor, .025)], 'stem', 8, cap_chart='stem')
        size = (.21 if forming else .43) * (1 - .053 * i)
        coconut(geometry, top, axis * (.38 if forming else .29) - Vector((0, 0, 1)), size,
                'forming' if forming else 'fruit')


def save_models(mat):
    patches = pack_palm()
    main = {}
    for stage in ('Seedling', 'Young', 'Mature', 'FruitForming', 'HarvestReady', 'Stump', 'Produce', 'Mutation'):
        mesh = Geometry()
        if stage == 'Seedling':
            seedling(mesh)
        elif stage in ('Produce', 'Mutation'):
            coconut(mesh, (-.15, 0, .13), (1, 0, -.19), .39, 'fruit' if stage == 'Produce' else 'mutation')
            radial_surface(mesh, [((-.197, 0, .149), .015), ((-.149, 0, .13), .024)],
                           'stem', 8, cap_chart='stem')
            base = min(p.z for p in mesh.points)
            mesh.points = [Vector((p.x, p.y, p.z - base)) for p in mesh.points]
        elif stage == 'Stump':
            radial_surface(mesh, [((0, 0, 0), .215), ((0, 0, .072), .223), ((0, 0, .19), .183),
                                  ((.014, -.016, .32), .156), ((.015, -.016, .34), .150)],
                           'bark', sides=20, lobe=.045)
        else:
            palm(mesh, patches, young=stage == 'Young')
            if stage in ('FruitForming', 'HarvestReady'):
                bunch(mesh, stage == 'FruitForming')

        obj = mesh.build(stage, mat)
        spec = mesh_io.write_mesh_spec(obj, obj.name)
        print(stage, spec['triangles'], spec['min'], spec['max'], flush=True)
        obj.scale = (STAGE_SCALE.get(stage, 1.0),) * 3
        main[stage] = obj
    return main


def stage_preview(objects):
    sc = bpy.context.scene
    for name, x in zip(('Seedling', 'Young', 'Mature', 'HarvestReady'), (-6.67, -4.84, -2.65, 2.98)):
        objects[name].location.x = x
        objects[name].rotation_euler.z = .19
    objects['FruitForming'].location = (.17, 0, 0)
    objects['FruitForming'].rotation_euler.z = .19
    pruned = objects['Mature'].copy()
    pruned.name = 'Preview_AfterHarvest'
    sc.collection.objects.link(pruned)
    pruned.location.x = 5.94
    objects['Stump'].location = (-7.1, 1.7, 0)
    objects['Stump'].hide_render = True
    objects['Mutation'].location = (-6.8, 1.7, 0)
    objects['Mutation'].hide_render = True
    objects['Produce'].location = (5.64, -.55, 0)
    for location, angle in [((6.12, -.62, 0), .54), ((5.97, -.91, 0), -.35)]:
        fruit = objects['Produce'].copy()
        fruit.name = 'Preview_HarvestedCoconut'
        sc.collection.objects.link(fruit)
        fruit.location = location
        fruit.rotation_euler.z = angle
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 1.6, -.013))
    bpy.context.object.name = 'Preview_Ground'
    ground = bpy.data.materials.new('Warm Neutral Ground')
    ground.diffuse_color = (.72, .71, .65, 1)
    bpy.context.object.data.materials.append(ground)
    bpy.ops.object.camera_add(location=(0, -13, 5.1))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, 2.03)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 15.65
    sc.camera = camera
    world = bpy.data.worlds.new('Review background')
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes.get('Background').inputs[0].default_value = (.8, .85, .79, 1)
    world.node_tree.nodes.get('Background').inputs[1].default_value = .65
    sc.view_settings.view_transform = 'AgX'
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 20
    sc.cycles.use_denoising = True
    sc.render.resolution_x = 1800
    sc.render.resolution_y = 650
    sc.render.resolution_percentage = 100
    for name, location, power, size in [
        ('Sunlit key', (-4, -4, 8), 1450, 7), ('Crown rim', (3, 5, 8), 1800, 6),
    ]:
        data = bpy.data.lights.new(name, 'AREA')
        data.energy, data.shape, data.size = power, 'DISK', size
        lamp = bpy.data.objects.new(name, data)
        sc.collection.objects.link(lamp)
        lamp.location = location
        lamp.rotation_euler = (Vector((0, 0, 2)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = str(OUT / 'NP_Coconut_Cycle.png')
    sc.render.film_transparent = False
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Blender/NP_Coconut_Cycle.blend'))
    bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = material(atlas())
    mesh_io.configure(mat, OUT / 'Mesh')
    stage_preview(save_models(mat))
