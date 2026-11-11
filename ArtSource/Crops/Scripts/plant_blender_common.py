"""Common Blender mesh/UV/export helpers from the accepted G05 R02 pipeline.
This module has no build side effects. Each group configures its own output.
"""
import bpy,bmesh,math,json
from mathutils import Vector
from pathlib import Path
from plant_library import manifest
scene=None
mat=None
current=None
parent=None
MESH=None
slots={s['name']:s for s in manifest('Produce')['slots']}
slots_by_index=tuple(slots.values())

def configure(material,output):
 global mat,MESH,scene
 mat=material;MESH=Path(output);scene=bpy.context.scene
 MESH.mkdir(parents=True,exist_ok=True)

def move(o, col):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    col.objects.link(o)

def select(o):
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

def atlas(o, slot, coords=None):
    o.data.materials.clear()
    o.data.materials.append(mat)
    old = o.data.uv_layers.active
    raw = [tuple(x.uv) for x in old.data] if old else [(0.5, 0.5)] * len(o.data.loops)
    while len(o.data.uv_layers):
        o.data.uv_layers.remove(o.data.uv_layers[-1])
    uv = o.data.uv_layers.new(name='AtlasUV')
    uv.active_render = True
    u0, v0, u1, v1 = slots[slot]['uvSafe']
    for i, loop in enumerate(o.data.loops):
        u, v = coords[loop.vertex_index] if coords is not None else raw[i]
        uv.data[i].uv = (u0 + (u1 - u0) * max(0.001, min(0.999, u)), v0 + (v1 - v0) * max(0.001, min(0.999, v)))
    o['AtlasSlot'] = slot
    for f in o.data.polygons:
        f.use_smooth = True
    return o

def mesh(name, verts, faces, slot, coords):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    o = bpy.data.objects.new(name, me)
    current.objects.link(o)
    o.parent = parent
    atlas(o, slot, coords)
    return o

def sphere(name, loc, scale, slot, sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=1)
    o = bpy.context.object
    o.name = name
    move(o, current)
    o.parent = parent
    o.location = loc
    o.scale = scale
    coords = []
    for v in o.data.vertices:
        n = v.co.normalized()
        coords.append((0.5 + math.atan2(n.y, n.x) / math.tau, 0.5 + math.asin(n.z) / math.pi))
    atlas(o, slot, coords)
    return o

def tube(name, a, b, r, slot, r2=None):
    d = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=r, radius2=r if r2 is None else r2, depth=d.length)
    o = bpy.context.object
    o.name = name
    move(o, current)
    o.parent = parent
    o.location = (Vector(a) + Vector(b)) / 2
    o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    atlas(o, slot)
    return o

def leaf(name, angle, length, width, z, slot='LeafBrassica', lift=0.7, curl=-0.2, base=0.025, cup=0.12, lobed=False):
    verts = []
    coords = []
    faces = []
    rows = 15
    cols = 9
    for i in range(rows):
        t = i / (rows - 1)
        blade = max(0, min(1, (t - 0.08) / 0.92))
        w = width * 0.5 * (0.018 + 0.982 * math.sin(math.pi * blade) ** 0.43) * (0.68 + 0.32 * t)
        if lobed:
            w *= 1 + 0.13 * math.sin(t * math.pi * 6 - 0.6) * math.sin(math.pi * t)
        for j in range(cols):
            q = j / (cols - 1) * 2 - 1
            x = w * q
            y = base + length * (t - 0.11 * t * t)
            zz = z + length * (lift * t + curl * t * t) + cup * length * abs(q) ** 1.6 * math.sin(math.pi * t)
            zz += length * 0.024 * math.sin(t * math.pi * 6 + angle) * abs(q) ** 2 * math.sin(math.pi * t)
            verts.append((x * math.cos(angle) - y * math.sin(angle), x * math.sin(angle) + y * math.cos(angle), zz))
            coords.append(((q + 1) / 2, t))
    for i in range(rows - 1):
        for j in range(cols - 1):
            k = i * cols + j
            faces.append((k, k + 1, k + 1 + cols, k + cols))
    o = mesh(name, verts, faces, slot, coords)
    solid = o.modifiers.new('Leaf edge thickness', 'SOLIDIFY')
    solid.thickness = 0.004
    solid.offset = 0
    return o

def write_mesh_spec(o, name):
    me = o.data
    assert all(len(f.vertices) == 3 for f in me.polygons), (name, 'export must be triangulated')
    uv = me.uv_layers.active.data
    norm = me.corner_normals
    verts = []
    norms = []
    uvs = []
    idx = []
    lookup = {}
    for f in me.polygons:
        ids = []
        for li in f.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co
            n = norm[li].vector
            u = uv[li].uv
            key = tuple((round(float(v), 7) for v in (*p, *n, *u)))
            if key not in lookup:
                lookup[key] = len(verts)
                verts.append([float(p.x), float(p.z), float(p.y)])
                norms.append([float(n.x), float(n.z), float(n.y)])
                uvs.append(list(u))
            ids.append(lookup[key])
        idx.extend([ids[0], ids[2], ids[1]])
    spec = dict(name=name, vertices=verts, normals=norms, uv=uvs, submeshes=[idx], materials=[mat.name], triangles=len(idx) // 3, min=[min((p[i] for p in verts)) for i in range(3)], max=[max((p[i] for p in verts)) for i in range(3)])
    assert all((0 < u < 1 and 0 < v < 1 for u, v in uvs))
    (MESH / (name + '.json')).write_text(json.dumps(spec, separators=(',', ':')))
    return spec

def clean_export_triangles(o):
    """FBX omits degenerate faces; keep the native buffers identical to it."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-8)
    dead = [f for f in bm.faces if f.calc_area() < 1e-14]
    if dead: bmesh.ops.delete(bm, geom=dead, context='FACES_ONLY')
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    # Decimation can leave isolated vertices at thin awn tips. FBX includes
    # those in its bounds while native serialization writes face vertices only.
    loose = [v for v in bm.verts if not v.link_faces]
    if loose: bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(o.data)
    bm.free()
    o.data.update()
    # Collapse can leave the two sides of a thin blade with duplicate triangles.
    # The FBX importer validates these; validate before writing either format.
    o.data.validate(verbose=False, clean_customdata=False)
    o.data.update()


def export(item, name):
    copies = []
    root = item['root']
    bpy.context.view_layer.update()
    for src in item['objects']:
        o = src.copy()
        o.data = src.data.copy()
        o.parent = None
        scene.collection.objects.link(o)
        o.matrix_world = root.matrix_world.inverted() @ src.matrix_world
        select(o)
        for mod in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        copies.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:
        o.select_set(True)
    bpy.context.view_layer.objects.active = copies[0]
    bpy.ops.object.join()
    o = copies[0]
    o.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    assert len(o.data.uv_layers) == 1, (name, list(o.data.uv_layers.keys()))
    tri = o.modifiers.new('Runtime triangulation', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=tri.name)
    clean_export_triangles(o)
    spec = write_mesh_spec(o, name)
    bpy.ops.export_scene.fbx(filepath=str(MESH / (name + '.fbx')), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
    dec = o.modifiers.new('Distance mesh', 'DECIMATE')
    dec.ratio = min(0.4, 2800 / max(spec['triangles'], 1))
    dec.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=dec.name)
    clean_export_triangles(o)
    for loop in o.data.uv_layers.active.data:
        u, v = loop.uv
        column = max(0, min(7, int(u * 8)))
        row = max(0, min(7, int((1 - v) * 8)))
        a, b, c, d = slots_by_index[row * 8 + column]['uvSafe']
        loop.uv = (max(a, min(c, u)), max(b, min(d, v)))
    write_mesh_spec(o, name + '_LOD1')
    bpy.ops.export_scene.fbx(filepath=str(MESH / (name + '_LOD1.fbx')), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
    bpy.data.objects.remove(o, do_unlink=True)
    return dict(name=name, plant=item['plant'], stage=item['stage'], triangles=spec['triangles'], bounds=[spec['min'], spec['max']])
