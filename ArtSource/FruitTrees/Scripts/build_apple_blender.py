"""Build the editable Apple models and export single-material Unity mesh buffers."""
import math
import os
import random
import sys
from functools import partial
from pathlib import Path

import bpy
from mathutils import noise
from mathutils.bvhtree import BVHTree
from mathutils.geometry import interpolate_bezier
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import ATLAS, PROJECT, SCRATCH, SOURCE, TEXTURES, atlas_uv
from fruit_tree_dimensions import ORCHARD_HEIGHT
from fruit_tree_foliage import configure_cutout, leaf_card, shade_sprays

sys.path.insert(0, str(PROJECT / 'ArtSource/Crops/Scripts'))
import plant_blender_common as unity_mesh


UP = Vector((0, 0, 1))
GOLDEN_ANGLE = math.pi * (3 - math.sqrt(5))
shade_apple = partial(shade_sprays, species='Apple')
STAGE_DIR = Path(os.environ['NP_APPLE_STAGE']) if 'NP_APPLE_STAGE' in os.environ else None
ATLAS_INPUT = STAGE_DIR / 'Atlas' if STAGE_DIR is not None and (STAGE_DIR / 'Atlas').is_dir() else TEXTURES
RENDER_PREVIEWS = os.environ.get('NP_APPLE_SKIP_PREVIEWS') != '1'


class Geometry:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.surfaces = []
        self.anchors = []

    def add(self, vertices, faces, slot, *, anchor=None):
        start = len(self.vertices)
        self.vertices.extend(Vector(point) for point in vertices)
        if anchor is not None:
            self.anchor_from(start, anchor)
        for ids, uv in faces:
            self.faces.append(tuple(start + index for index in ids))
            self.surfaces.append((slot, uv))

    def anchor_from(self, start, anchor):
        self.anchors.append((start, len(self.vertices), anchor.z))

    def height_anchors(self):
        heights = [point.z for point in self.vertices]
        for start, end, height in self.anchors:
            heights[start:end] = [height] * (end - start)
        return heights

    def object(self, name, collection, material):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        mesh.materials.append(material)
        uv_layer = mesh.uv_layers.new(name='Shared Atlas UV')
        for face, (slot, coords) in zip(mesh.polygons, self.surfaces):
            face.use_smooth = True
            for loop, (u, v) in zip(face.loop_indices, coords):
                uv_layer.data[loop].uv = atlas_uv(slot, u, v)
        return obj


def height_adjustment(parts):
    """Find the height scale delta that makes the topmost vertex reach the target."""
    return min((ORCHARD_HEIGHT - point.z) / anchor
               for part in parts
               for point, anchor in zip(part.vertices, part.height_anchors())
               if anchor > 0)


def adjust_height(parts, adjustment):
    """Move each blade and hanging fruit at its attachment while fitting the trunk and branches."""
    for part in parts:
        for point, anchor in zip(part.vertices, part.height_anchors()):
            point.z += max(0, anchor) * adjustment


def tube(mesh, points, radii, slot='Bark', sides=8, root_scale=0, closed=False, uv_span=(1, .05)):
    points = [Vector(point) for point in points]
    vertices = []
    faces = []
    distance = [0.0]
    for left, right in zip(points, points[1:]):
        distance.append(distance[-1] + (right - left).length)
    across = None
    for row, (point, radius) in enumerate(zip(points, radii)):
        axis = (points[min(row + 1, len(points) - 1)] - points[max(0, row - 1)]).normalized()
        if root_scale:
            axis = UP.lerp(axis, max(0, min(1, point.z / (.27 * root_scale)))).normalized()
        if across is None:
            reference = Vector((0, 1, 0)) if abs(axis.y) < .89 else Vector((1, 0, 0))
            across = axis.cross(reference).normalized()
        else:
            across = (across - axis * across.dot(axis)).normalized()
        along = axis.cross(across).normalized()
        for side in range(sides):
            angle = math.tau * side / sides
            direction = across * math.cos(angle) + along * math.sin(angle)
            width = radius * (1 + .055 * math.cos(angle * 3 + .73))
            if root_scale:
                width += root_scale * root_flare(point.z / root_scale, math.atan2(direction.y, direction.x))
            vertices.append(point + width * direction)
    start_v, end_v = uv_span
    for row in range(len(points) - 1):
        v = start_v + distance[row] / distance[-1] * (end_v-start_v)
        v_next = start_v + distance[row+1] / distance[-1] * (end_v-start_v)
        for side in range(sides):
            next_side = (side + 1) % sides
            u, u_next = side / sides, (side + 1) / sides
            a, b = row * sides + side, row * sides + next_side
            faces.append(((a, b, b+sides, a+sides), [(u, v), (u_next, v), (u_next, v_next), (u, v_next)]))
    if closed:
        end = (len(points)-1) * sides
        outer_uv = [(.5+.36*math.cos(i*math.tau/sides), .5+.36*math.sin(i*math.tau/sides))
                    for i in range(sides)]
        faces.extend(((tuple(reversed(range(sides))), outer_uv[::-1]),
                      (tuple(range(end, end+sides)), outer_uv)))
    mesh.add(vertices, faces, slot)


def curved_axis(points, radii, steps):
    """Sample connected Bézier segments with a shared tangent at each joint."""
    anchors = [Vector(point) for point in points]
    tangents = [(anchors[min(i + 1, len(anchors) - 1)] - anchors[max(0, i - 1)]).normalized()
                for i in range(len(anchors))]
    centers, widths = [], []
    for i, (start, end) in enumerate(zip(anchors, anchors[1:])):
        handle = (end - start).length / 3
        segment = interpolate_bezier(start, start + tangents[i] * handle,
                                     end - tangents[i + 1] * handle, end, steps + 1)
        centers.extend(segment[:-1])
        widths.extend(radii[i] + (radii[i + 1] - radii[i]) * j / steps for j in range(steps))
    centers.append(anchors[-1])
    widths.append(radii[-1])
    return centers, widths


def ring_shape(mesh, center, radius, profile, slot, sides, apple=False):
    vertices = []
    faces = []
    center = Vector(center)
    for i, (level, width) in enumerate(profile):
        for j in range(sides):
            angle = j * math.tau / sides
            rib = (1 + .052 * math.sin(5 * angle + .2) * (1 if i in (0, 1, len(profile)-1) else .48)) if apple else 1
            r = radius * width * rib
            vertices.append(center + Vector((r * math.cos(angle), r * math.sin(angle), radius * level)))
    for i in range(len(profile) - 1):
        for j in range(sides):
            k = (j + 1) % sides
            ids = (i*sides+j, i*sides+k, (i+1)*sides+k, (i+1)*sides+j)
            up = 1 - i / (len(profile)-1)
            next_up = 1 - (i+1) / (len(profile)-1)
            faces.append((ids, [(j/sides, up), ((j+1)/sides, up), ((j+1)/sides, next_up), (j/sides, next_up)]))
    # Close the narrow calyx and stem sockets with their own UVs.
    for level, flip in [(0, True), (len(profile)-1, False)]:
        ids = [level*sides + j for j in range(sides)]
        if flip:
            ids.reverse()
        faces.append((ids, [(.5 + .04 * math.cos((a%sides)*math.tau/sides),
                             (.91 if flip else .08) + .045 * math.sin((a%sides)*math.tau/sides)) for a in ids]))
    mesh.add(vertices, faces, slot)


def apple(mesh, center, radius, ripe=True, detail=True, stem=True):
    profile = [(-.72, .15), (-.83, .39), (-.77, .66), (-.51, .91), (-.10, .99),
               (.33, 1), (.65, .84), (.74, .62), (.59, .23), (.53, .11)]
    profile = [profile[i] for i in ((0, 2, 3, 4, 6, 7, 9) if detail else (0, 2, 4, 6, 9))]
    slot = 'Apple/FruitReady' if ripe else 'Apple/FruitForming'
    ring_shape(mesh, center, radius, profile, slot, 12 if detail else 8, apple=True)
    if stem:
        x, y, z = center
        tube(mesh, [(x, y, z+.50*radius), (x+.08*radius, y, z+.88*radius),
                    (x+.24*radius, y+.16*radius, z+1.14*radius)],
             [radius*.10, radius*.085, radius*.062], 'Twig', 5)


TRUNK = [(0, 0, -.026), (-.020, .008, .10), (-.113, .041, .29), (-.202, .087, .61),
         (-.087, .054, .98), (-.052, .080, 1.24), (.025, .122, 1.58),
         (.089, .146, 1.93), (.142, .088, 2.22), (.104, .04, 2.50), (-.02, .015, 2.77)]
TRUNK_RADII = [.174, .150, .130, .115, .105, .094, .077, .060, .041, .024, .007]
TRUNK_SPINE, _ = curved_axis(TRUNK, TRUNK_RADII, steps=8)
# Bearing, reach beyond the trunk, angular half-width, height, and sweep up the trunk.
ROOTS = (
    (.42, .123, .68, .29, .30),
    (1.84, .100, .65, .31, -.27),
    (3.24, .156, .78, .39, .36),
    (4.74, .133, .71, .34, -.37),
    (5.65, .080, .58, .24, .28),
)
# Height of union, bearing, spread, end height, taper. Each scaffold has its own arc.
SCAFFOLDS = (
    (.97, 3.69, 1.04, 1.99, .079),
    (1.24, 5.81, 1.23, 2.18, .081),
    (1.33, 1.93, 1.08, 2.19, .068),
    (1.45, 4.82, 1.06, 2.27, .066),
    (1.60, .61, 1.10, 2.49, .061),
    (1.78, 2.82, 1.12, 2.60, .055),
    (1.95, 4.11, .83, 2.67, .045),
    (2.12, 1.43, .70, 2.76, .038),
    (2.32, 5.84, .51, 2.83, .031),
)
# Leaf cluster dimensions along the branch, across it, and vertically, followed by leaf count.
CROWN_GROUPS = (
    (.54, .40, .32, 72), (.55, .44, .33, 70), (.56, .45, .38, 72),
    (.47, .39, .32, 59), (.53, .45, .40, 78), (.55, .45, .38, 75),
    (.46, .40, .34, 61), (.42, .37, .38, 62), (.43, .38, .32, 58),
)


def sample_axis(points, position):
    i = min(int(position * (len(points) - 1)), len(points) - 2)
    t = position * (len(points) - 1) - i
    return points[i].lerp(points[i+1], t), (points[i+1]-points[i]).normalized()


def trunk_at(height):
    for first, second in zip(TRUNK_SPINE, TRUNK_SPINE[1:]):
        if height <= second.z:
            return first.lerp(second, (height - first.z) / (second.z - first.z))
    return TRUNK_SPINE[-1].copy()


def root_flare(height, angle):
    """Blend each root into the trunk rings as it tapers away from the ground."""
    width = 0
    for bearing, reach, half_width, top, sweep in ROOTS:
        rise = max(0, min(1, height / top))
        offset = math.remainder(angle - bearing - sweep * rise, math.tau) / half_width
        lobe = max(0, 1 - offset * offset)
        width += reach * ((1 - rise) * lobe) ** 2
    return width


def bark_uv(source, index, point):
    coords = source.surfaces[index][1]
    a, b, *_, d = (source.vertices[i] for i in source.faces[index])
    axis_u, axis_v = b - a, d - a
    dot = axis_u.dot(axis_v)
    determinant = axis_u.length_squared * axis_v.length_squared - dot ** 2
    if determinant > 1e-14:
        offset = point - a
        s = (offset.dot(axis_u) * axis_v.length_squared - offset.dot(axis_v) * dot) / determinant
        t = (offset.dot(axis_v) * axis_u.length_squared - offset.dot(axis_u) * dot) / determinant
    else:
        s = t = .5
    u0, v0 = coords[0]
    u = u0 + s * (coords[1][0] - u0) + t * (coords[-1][0] - u0)
    v = v0 + s * (coords[1][1] - v0) + t * (coords[-1][1] - v0)
    return max(.002, min(.998, u)), max(.002, min(.998, v))


def fused_wood(branches, detail, young, *, root_scale=None):
    source = Geometry()
    steps = 5 if detail else 4
    trunk, radii = branches[0]
    if root_scale is None:
        root_scale = .66 if young else 1
    tube(source, *curved_axis(trunk, radii, steps), sides=36 if detail else 28,
         root_scale=root_scale, closed=True)
    trunk_faces = len(source.faces) - 2  # Exclude the trunk caps from side-wall UVs.
    trunk_guide = BVHTree.FromPolygons(source.vertices, source.faces[:trunk_faces])
    for path, radii in branches[1:]:
        tube(source, *curved_axis(path, radii, steps), sides=14 if detail else 10,
             closed=True, uv_span=(.82, .31))

    # Union the coarse woody axes before bark mapping so the crotches share a continuous surface.
    mesh = bpy.data.meshes.new('Temporary branch union')
    mesh.from_pydata(source.vertices, [], source.faces)
    obj = bpy.data.objects.new('Temporary branch union', mesh)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    remesh = obj.modifiers.new('Blend branch unions', 'REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = .0085 if detail else .011
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    relax = obj.modifiers.new('Relax unions', 'SMOOTH')
    relax.factor = .83
    relax.iterations = 4
    bpy.ops.object.modifier_apply(modifier=relax.name)
    simplify = obj.modifiers.new('Tree wood triangles', 'DECIMATE')
    simplify.ratio = min(1, (2510 if detail else 1110) / (2 * len(obj.data.polygons)))
    simplify.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=simplify.name)

    wood = Geometry()
    # Use source faces rather than their centers to keep the bark attached to the curved wood.
    guides = BVHTree.FromPolygons(source.vertices, source.faces)
    trunk_uvs = {}
    for vertex in obj.data.vertices:
        co = vertex.co.copy()
        signal = noise.noise_vector(co*13.5).x*.54 + noise.noise_vector(co*36.0).z*.26
        co += vertex.normal * signal * (.008 if detail else .005)
        co.z = max(-.014, co.z)
        wood.vertices.append(co)
    for poly in obj.data.polygons:
        _, _, index, _ = guides.find_nearest(poly.center)
        face_u = source.surfaces[index][1][0][0]
        uv = []
        for vertex in poly.vertices:
            point = wood.vertices[vertex]
            if index < trunk_faces:
                # Reuse the same UV across the trunk triangles, splitting at the atlas seam.
                if vertex not in trunk_uvs:
                    contact, _, side, _ = trunk_guide.find_nearest(point)
                    trunk_uvs[vertex] = bark_uv(source, side, contact)
                mapped = trunk_uvs[vertex]
                if abs(mapped[0] - face_u) < .5:
                    uv.append(mapped)
                    continue
            uv.append(bark_uv(source, index, point))
        wood.faces.append(tuple(poly.vertices))
        wood.surfaces.append(('Bark', uv))
    bpy.data.objects.remove(obj, do_unlink=True)
    return wood


def orchard_axes(young):
    rng = random.Random(3223 if young else 2406)
    if young:
        scale = Vector((.66, .66, .71))
        trunk = [Vector(point)*scale for point in TRUNK[:10]]
        radii = [r*.55 for r in TRUNK_RADII[:10]]
    else:
        scale = Vector((1, 1, 1))
        trunk = list(map(Vector, TRUNK))
        radii = TRUNK_RADII
    branches = [(trunk, radii)]
    # Shoots are grown from the actual secondary branches. Keep gaps around the scaffold crotches.
    scaffold_guides = []
    shoots = []
    fruit_sites = []
    for i, (height, bearing, reach, tip_z, radius) in enumerate(SCAFFOLDS[:7] if young else SCAFFOLDS):
        outward = Vector((math.cos(bearing), math.sin(bearing), 0))
        side = outward.cross(UP)
        start = trunk_at(height)
        crook = rng.uniform(-.20,.20)
        points = [start, start+outward*(reach*.21)+UP*((tip_z-height)*.27),
                  start+outward*(reach*.64)+side*crook+UP*((tip_z-height)*.74),
                  start+outward*reach+side*rng.uniform(-.10,.10)+UP*(tip_z-height)]
        points = [p*scale for p in points]
        width = radius*(.66 if young else 1)
        # Follow the trunk for a short stretch so the branch eases into its outward curve.
        collar = trunk_at(height + (tip_z - height) * .12) + outward * (reach * .045)
        # Extend a woody shell only as far as it remeshes cleanly, then finish with a twig.
        shoulder = points[-2].lerp(points[-1], .76)
        scaffold_guides.append(points[:-1] + [shoulder])
        branches.append(([points[0], collar * scale, *points[1:-1], shoulder],
                         [width, width * .92, width * .76, width * .40, .016]))
        shoots.append(([points[-2].lerp(shoulder,.77), shoulder, points[-1]], [.017,.013,.004], 4))
        # Short laterals overlap in groups near each branch's outer half.
        for j, pos in enumerate((.43, .56, .70, .82, .94)):
            attach, direction = sample_axis(points, pos+rng.uniform(-.022,.022))
            sign = 1 if (i+j) % 2 == 0 else -1
            span = rng.uniform(.29, .46) * (1 if j < 4 else .74)
            rise = rng.uniform(.12, .26) + (.035 if i > 4 else 0)
            stretch = .71 if young else 1
            lead = rng.uniform(-.05,.14) if j < 2 else rng.uniform(.05,.17)
            end = attach + (side*(sign*span) + direction*lead + UP*rise) * stretch
            elbow = attach.lerp(end, .47)-UP*rng.uniform(.035,.060)*stretch
            subbranch = [attach, elbow, end]
            width = (.021-.0021*j) * (.78 if young else 1)
            shoots.append((subbranch, [width, width*.61, .004], 5 if young else rng.choice((5,6,7))))
            for k in range(2 if young else rng.choice((2,3,3,4))):
                base, axis = sample_axis(subbranch, .44 + k*.147 + rng.uniform(-.040, .040))
                side_sign = (-1)**(k+j+i)
                local_side = axis.cross(UP).normalized()
                heading = (axis*rng.uniform(.24,.62) + local_side*side_sign*rng.uniform(.25,.73)
                           + UP*rng.uniform(-.18,.43)).normalized()
                length = rng.uniform(.145, .235) * stretch
                tip = base + heading*length
                elbow = base.lerp(tip, .51)-UP*length*.105
                shoots.append(([base, elbow, tip], [.0065,.004,.0018], rng.choice((4,5,5,6))))
            if (j in (1, 3) and i < 6) or (j == 2 and i in (6, 8)):
                spur, direction = sample_axis(subbranch, rng.uniform(.48,.88))
                radius = rng.uniform(.065, .083)
                if all((spur-other).length > .17 for other, _, _ in fruit_sites):
                    fruit_sites.append((spur, direction, radius))
    # Shoots growing off the leader give the crown a continuous shape when seen from above.
    for i, pos in enumerate((.56,.64,.73,.81,.89,.95,1.0)):
        if young and i == 5:
            continue
        base, _ = sample_axis(trunk, min(pos,.985))
        angle = i*GOLDEN_ANGLE + rng.uniform(-.30,.30)
        tip = base + Vector((math.cos(angle)*.37, math.sin(angle)*.37, .13)) * (.70 if young else 1)
        axis = [base, base.lerp(tip,.49)-UP*.06, tip]
        shoots.append((axis, [.017,.009,.0035], 7))
        for fraction in (.42,.76):
            twig, _ = sample_axis(axis, fraction)
            yaw = angle+(-1 if fraction < .5 else 1)*1.36
            end = twig + Vector((math.cos(yaw)*.21, math.sin(yaw)*.21, .14))
            shoots.append(([twig, twig.lerp(end,.49)-UP*.029, end], [.008,.005,.0025], 5))
    if not young:
        # Short, leafy spurs occupy the spaces between the long shoots in the upper crown.
        for i, path in enumerate(scaffold_guides):
            toward = (path[-1]-path[0]).normalized()
            side = toward.cross(UP).normalized()
            for j, pos in enumerate((.61, .75, .86)):
                base, _ = sample_axis(path, pos)
                sign = (-1)**(i+j)
                end = base+side*(sign*rng.uniform(.15,.26))+toward*rng.uniform(.025,.085)+UP*rng.uniform(.15,.24)
                mid = base.lerp(end, .53)+side*(sign*.025)-UP*.025
                shoots.append(([base, mid, end], [.0075,.0041,.0016], 5))
                for factor in (.39,.64):
                    point, _ = sample_axis([base, mid, end], factor)
                    tip = point-toward*rng.uniform(.05,.13)-side*(sign*.05)+UP*rng.uniform(.09,.17)
                    shoots.append(([point, point.lerp(tip,.48), tip], [.0037,.0022,.0010], 4))
        # Leafy offshoots between the main forks fill the middle tier under the crown.
        for index, path in enumerate(scaffold_guides):
            for step, position in enumerate((.45, .57)):
                base, axis = sample_axis(path, position)
                side = axis.cross(UP).normalized() * (-1)**(index+step)
                tip = base + side*.23 + UP*(.20+.045*step)
                shoots.append(([base, base.lerp(tip,.53)+axis*.034, tip], [.006,.004,.0018], 5))
    return branches, scaffold_guides, shoots, fruit_sites, rng


def crown_cluster(foliage, center, heading, breadth, count, scale, detail, rng):
    across = heading.cross(UP).normalized()
    phase = rng.uniform(0, math.tau)
    for i in range(count):
        rise = 1 - 2 * (i + .5) / count
        ring = math.sqrt(1 - rise * rise)
        angle = phase + i * GOLDEN_ANGLE + rng.uniform(-.21, .21)
        x, y = math.cos(angle) * ring, math.sin(angle) * ring
        shell = rng.uniform(.58, .92) * scale
        point = center + shell * (heading * (x * breadth[0]) + across * (y * breadth[1]) + UP * (rise * breadth[2]))
        facing = heading * x + across * y
        direction = (facing + UP * rng.uniform(-.52, .48)).normalized()
        length = rng.uniform(.31, .44) * math.sqrt(scale)
        width = length * rng.uniform(.83, .99)
        roll = rng.uniform(-.66, .66)
        lit = .31 + rise * .33 + .13 * math.cos(angle) + (center.z / scale - 2.2) * .09
        accent = rng.random() < lit
        if not detail and i % 4 == 1:
            continue
        leaf_card('Apple', foliage, point - direction * (length * .28), direction, length, width,
                  roll, accent, spray=True, detail=detail)


def fill_crown(foliage, guides, young, detail):
    rng = random.Random(4143 if young else 5026)
    scale = .70 if young else 1
    density = .16 if young else .28
    for guide, (along, across, height, count) in zip(guides, CROWN_GROUPS):
        center, axis = sample_axis(guide, .90)
        direction = Vector((axis.x, axis.y, 0)).normalized()
        crown_cluster(foliage, center + UP * (.11 * scale), direction,
                      (along, across, height), round(count * density), scale, detail, rng)
    center = trunk_at(2.56)
    upper_groups = (((.03, -.07, .09), (.57, .47, .43), 82),
                    ((-.26, .19, -.26), (.46, .42, .36), 61))
    for offset, breadth, count in upper_groups:
        crown_cluster(foliage, (center + Vector(offset)) * scale, Vector((.8, .6, 0)),
                      breadth, round(count * density), scale, detail, rng)


def full_tree(detail, young=False):
    branches, guides, shoots, fruit_sites, rng = orchard_axes(young)
    wood, foliage, green, ripe = fused_wood(branches, detail, young), Geometry(), Geometry(), Geometry()
    for i, (points, widths, _) in enumerate(shoots):
        if detail or widths[0] > .012:
            sides = 4 if detail and widths[0] > .012 else 3
            tube(wood, points, widths, 'Twig', sides)
        if widths[0] < .009 and i % 2:
            continue
        positions = (.36, .81) if widths[0] > .013 else (.68,)
        for j, fraction in enumerate(positions):
            base, axis = sample_axis(points, fraction)
            lateral = axis.cross(UP).normalized() * (-1)**(i + j)
            direction = (axis + lateral * rng.uniform(.45, .94) + UP * rng.uniform(-.65, .20)).normalized()
            length = rng.uniform(.30, .44) * (.74 if young else 1)
            width = length * rng.uniform(.77, .95)
            roll = rng.uniform(-.69, .69)
            exposed = .21 + .39 * (base.z / (.71 if young else 1) - 1.76)
            exposed += .13 * math.sin(base.x * 3.2 + base.y * 2.4)
            accent = rng.random() < exposed
            if not detail and j == 1 and i % 3 == 1:
                continue
            leaf_card('Apple', foliage, base, direction, length, width, roll, accent,
                      spray=True, detail=detail)
    fill_crown(foliage, guides, young, detail)
    if not young:
        for attach, direction, radius in fruit_sites:
            end = attach + direction*.082 - UP*.154
            for fruit, ripe_fruit, size in ((green, False, radius*.77), (ripe, True, radius*1.19)):
                start = len(fruit.vertices)
                center = end-UP*(size*.52)
                apple(fruit, center, size, ripe_fruit, detail, stem=False)
                tube(fruit, [end, end.lerp(attach,.47)-direction*.015, attach],
                     [.0037,.0042,.0031], 'Twig', 5 if detail else 4)
                fruit.anchor_from(start, attach)
    print(f'Apple {"young" if young else "mature"} LOD{0 if detail else 1}: {len(foliage.anchors)} leaf sprays',
          flush=True)
    return wood, foliage, green, ripe


def seed():
    mesh = Geometry()
    ring_shape(mesh, (0,0,.073), .105, [(-.69,.12),(-.58,.54),(-.30,.77),(.18,.75),(.62,.48),(.86,.07)],
               'AppleSeed', 9)
    for p in mesh.vertices:
        p.x *= .82
        p.y *= .56
        p.x += (.074-p.z)*.40
    return mesh


def sprout():
    stem, foliage = Geometry(), Geometry()
    tube(stem, [(0,0,0),(-.025,0,.12),(-.038,0,.245),(.015,0,.365),(.035,0,.49)],
         [.023,.019,.014,.011,.006], 'GreenStem', 7, closed=True)
    for start, heading, length, w, roll, slot in [
        ((-.028,0,.23),(-1,-.35,.26),.31,.162,-.19,'Leaf'),
        ((0,0,.33),(1,-.32,.38),.34,.168,.31,'LeafAccent'),
        ((.025,0,.42),(-.34,.65,.61),.24,.119,-.30,'Leaf'),
        ((.033,0,.46),(.17,-.27,.90),.19,.083,.24,'LeafAccent'),
    ]:
        leaf_card('Apple', foliage, Vector(start), Vector(heading), length, w, roll, slot == 'LeafAccent')
    return stem, foliage


def stump():
    mesh = Geometry()
    sides = 24
    anchors = [*map(Vector, TRUNK[:3]), trunk_at(.38)]
    radii = [*TRUNK_RADII[:3], .124]
    points, widths = curved_axis(anchors, radii, steps=2)
    tube(mesh, points, widths, sides=sides, root_scale=1, uv_span=(1, .86))
    center = points[-1]
    vertices = [center]
    for i in range(sides):
        a = i * math.tau / sides
        vertices.append(mesh.vertices[(len(points)-1)*sides+i])
    coords = [(.5,.5)] + [(.5+math.cos(i*math.tau/sides)*.49,.5+math.sin(i*math.tau/sides)*.49) for i in range(sides)]
    faces = [(0, i+1, (i+1)%sides+1) for i in range(sides)]
    mesh.add(vertices, [(f,[coords[i] for i in f]) for f in faces], 'CutWood')
    return mesh


def collection(name):
    group = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(group)
    return group


def material_atlas(atlas_folder=ATLAS_INPUT):
    material = bpy.data.materials.new(ATLAS)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .69
    shader.inputs['Specular IOR Level'].default_value = .25
    for suffix, location in [('BaseColor', (-770,300)), ('Normal',(-770,15)), ('SurfaceMask',(-770,-285))]:
        tex = nodes.new('ShaderNodeTexImage')
        tex.label = 'Shared fruit tree ' + suffix
        tex.location = location
        path = atlas_folder / f'{ATLAS}_{suffix}.png'
        tex.image = bpy.data.images.load(str(path))
        if suffix == 'BaseColor':
            color_texture = tex
            links.new(tex.outputs['Color'], shader.inputs['Base Color'])
        else:
            tex.image.colorspace_settings.name = 'Non-Color'
            if suffix == 'Normal':
                normal = nodes.new('ShaderNodeNormalMap')
                normal.location = (-220,25)
                links.new(tex.outputs['Color'], normal.inputs['Color'])
                links.new(normal.outputs['Normal'], shader.inputs['Normal'])
            else:
                tex.image.alpha_mode = 'CHANNEL_PACKED'
                roughness = nodes.new('ShaderNodeMath')
                roughness.operation = 'SUBTRACT'
                roughness.location = (-200,-230)
                roughness.inputs[0].default_value = 1
                links.new(tex.outputs['Alpha'], roughness.inputs[1])
                links.new(roughness.outputs[0], shader.inputs['Roughness'])
                channels = nodes.new('ShaderNodeSeparateColor')
                channels.location = (-520, -95)
                strength = nodes.new('ShaderNodeMath')
                strength.operation = 'MULTIPLY'
                strength.location = (-290, 35)
                strength.inputs[1].default_value = .60
                links.new(tex.outputs['Color'], channels.inputs['Color'])
                links.new(channels.outputs['Green'], strength.inputs[0])
                links.new(strength.outputs[0], normal.inputs['Strength'])
    nodes.active = color_texture
    return material


def stage(name, parts, runtime_scale, material, source_visible=True, *, shade=None):
    group = collection(name)
    objects = [mesh.object(part, group, material) for part, mesh in parts]
    clones = []
    for obj in objects:
        clone = obj.copy()
        clone.data = obj.data.copy()
        group.objects.link(clone)
        clones.append(clone)
    bpy.ops.object.select_all(action='DESELECT')
    for clone in clones:
        clone.select_set(True)
    bpy.context.view_layer.objects.active = clones[0]
    if len(clones) > 1:
        bpy.ops.object.join()
    combined = clones[0]
    combined_mesh = combined.data
    for vertex in combined.data.vertices:
        vertex.co /= runtime_scale
    unity_mesh.clean_export_triangles(combined)
    if shade:
        shade(combined, runtime_scale)
    unity_mesh.write_mesh_spec(combined, name)
    bpy.data.objects.remove(combined, do_unlink=True)
    if combined_mesh.users == 0:
        bpy.data.meshes.remove(combined_mesh)
    if shade:
        for obj in objects:
            shade(obj, 1)
    group.hide_render = not source_visible
    group.hide_viewport = not source_visible
    return group


def aim(obj, position, target):
    obj.location = position
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()


def setup_lighting(scene):
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.world = bpy.data.worlds.new('Soft studio fill')
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get('Background')
    background.inputs['Color'].default_value = (.82,.87,1,1)
    background.inputs['Strength'].default_value = .19
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGB'
    bpy.ops.object.camera_add()
    scene.camera = bpy.context.object
    scene.camera.data.type = 'ORTHO'
    # Same light colors and angular sizes as the crop reference, scaled to frame a taller asset.
    focus = Vector((0, 0, 1.20))
    distance_scale = 1.35
    for name, position, energy, size, color in (
        ('Warm sun',(-3,-4,6),1150,5.5,(1,.92,.81)),
        ('Sky fill',(4,-3,3.7),490,5,(.87,.94,1)),
        ('Canopy rim',(0,4,5),930,4,(1,.96,.86)),
    ):
        bpy.ops.object.light_add(type='AREA')
        light = bpy.context.object
        light.name = name
        light.data.shape = 'DISK'
        light.data.energy = energy * distance_scale**2
        light.data.size = size * distance_scale
        light.data.color = color
        aim(light, focus + (Vector(position)-Vector((0, .6, .3)))*distance_scale, focus)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.013))
    ground = bpy.data.materials.new('Ivory background')
    ground.diffuse_color = (.56,.60,.53,1)
    ground.use_nodes = True
    shader = ground.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = ground.diffuse_color
    shader.inputs['Roughness'].default_value = .92
    bpy.context.object.data.materials.append(ground)


def render(scene, name, camera, target, width, height, ortho_width):
    aim(scene.camera, camera, target)
    scene.camera.data.ortho_scale = ortho_width
    scene.render.resolution_x, scene.render.resolution_y = width, height
    out = STAGE_DIR if STAGE_DIR is not None else SOURCE / 'Previews'
    scene.render.filepath = str(out / f'{name}.png')
    bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    # Keep saved sources from writing a PNG over the reference render on a manual render.
    scene.render.filepath = '//'
    bpy.context.view_layer.update()
    material = material_atlas()
    configure_cutout(material)
    unity_mesh.configure(material, STAGE_DIR / 'Mesh' if STAGE_DIR is not None else SCRATCH)
    stage('NP_Apple_Seed', [('Apple seed', seed())], .09, material, False)
    stage('NP_Apple_Sprout', list(zip(('Young stem', 'First leaves'), sprout())), .30, material, False)
    trunk, foliage, green, ripe = full_tree(True)
    adjustment = height_adjustment((trunk, foliage, green, ripe))
    adjust_height((trunk, foliage, green, ripe), adjustment)
    young_wood, young_leaves, _, _ = full_tree(True, young=True)
    adjust_height((young_wood, young_leaves), adjustment)
    young = stage('NP_Apple_Young', [('Young branches', young_wood), ('Young foliage', young_leaves)],
                  1, material, False, shade=shade_apple)
    young_wood, young_leaves, _, _ = full_tree(False, young=True)
    adjust_height((young_wood, young_leaves), adjustment)
    stage('NP_Apple_Young_LOD1', [('Young branches LOD1', young_wood), ('Young foliage LOD1', young_leaves)],
          1, material, False, shade=shade_apple)
    fruitless = [('Trunk and branches', trunk), ('Apple foliage', foliage)]
    stage('NP_Apple_FruitForming', fruitless+[('Green apples', green)], 1, material, False, shade=shade_apple)
    ready = stage('NP_Apple_HarvestReady', fruitless+[('Ripe apples', ripe)], 1, material, shade=shade_apple)
    trunk, foliage, green, ripe = full_tree(False)
    adjust_height((trunk, foliage, green, ripe), height_adjustment((trunk, foliage, green, ripe)))
    fruitless = [('Branches LOD1',trunk), ('Foliage LOD1',foliage)]
    stage('NP_Apple_FruitForming_LOD1', fruitless+[('Green apples LOD1',green)], 1, material, False, shade=shade_apple)
    ready_lod = stage('NP_Apple_HarvestReady_LOD1', fruitless+[('Ripe apples LOD1',ripe)], 1, material, False, shade=shade_apple)
    stage('NP_Apple_Stump', [('Cut trunk',stump())], 1, material, False)
    produce = Geometry()
    apple(produce,(0,0,0),.12)
    stage('NP_Apple_Produce',[('Loose apple',produce)],1,material,False)

    setup_lighting(scene)
    close_focus = (0, 0, ORCHARD_HEIGHT * .515)
    aim(scene.camera, (4, -8, 4.7), close_focus)
    scene.camera.data.ortho_scale = 4.15
    scene.render.resolution_x, scene.render.resolution_y = 1050, 1030
    for workspace in bpy.data.workspaces:
        for screen in workspace.screens:
            for area in screen.areas:
                if area.type == 'VIEW_3D':
                    area.spaces.active.region_3d.view_distance = 6.5
                    area.spaces.active.region_3d.view_location = (0,0,1.5)
                    area.spaces.active.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
                    area.spaces.active.shading.color_type = 'TEXTURE'
    if STAGE_DIR is None:
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE':
                node.image.filepath = bpy.path.relpath(node.image.filepath, start=str(SOURCE / 'Blender'))
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Blender/NP_Apple.blend'), relative_remap=False)
    if RENDER_PREVIEWS:
        render(scene,'NP_Apple_Close_R13',(4,-8,4.7),close_focus,1050,1030,4.15)
        ready.hide_render = True
        young.hide_render = False
        render(scene, 'NP_Apple_Young_R13', (4,-8,4.7), (0,0,.96), 840, 830, 3.10)
        ready.hide_render = False
        young.hide_render = True

        # Use the Map camera's outdoor pitch and standard orthographic size.
        mobile_parts = list(ready.objects)
        for x, angle, parts in [(-4.1,-.63,mobile_parts),(4.0,1.92,list(ready_lod.objects))]:
            for original in parts:
                duplicate = original.copy()
                ready.objects.link(duplicate)
                duplicate.location.x = x
                duplicate.rotation_euler.z = angle
        try:
            render(scene,'NP_Apple_Mobile_R13',(0,-13,8.50),(0,0,1.0),1100,619,13*1100/619)
        finally:
            for obj in list(ready.objects):
                if obj not in mobile_parts:
                    bpy.data.objects.remove(obj, do_unlink=True)
    if STAGE_DIR is not None:
        # Keep the baked preview independent from the source-linked Blender file.
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE':
                name = Path(node.image.filepath).name
                node.image.filepath = bpy.path.relpath(str(TEXTURES/name), start=str(SOURCE / 'Blender'))
        aim(scene.camera, (4, -8, 4.7), close_focus)
        scene.camera.data.ortho_scale = 4.15
        scene.render.resolution_x, scene.render.resolution_y = 1050, 1030
        scene.render.filepath = '//'
        bpy.ops.wm.save_as_mainfile(filepath=str(STAGE_DIR / 'NP_Apple.blend'), relative_remap=False)
    print('APPLE_BLENDER_PASS', flush=True)


if __name__ == '__main__':
    main()
