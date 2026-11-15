"""Model the cutout foliage and light each canopy as a volume."""
import math

from mathutils import Vector

from fruit_tree_atlas import SINGLE_LEAF_RECT, SPRAY_RECT, SPRAY_ROOT, atlas_uv


UP = Vector((0, 0, 1))


def leaf_card(species, mesh, base, heading, length, width, roll, accent, *, spray=False, detail=True,
              bend=None, lift=.13):
    forward = heading.normalized()
    side = forward.cross(UP).normalized()
    up = side.cross(forward).normalized()
    side = side * math.cos(roll) + up * math.sin(roll)
    up = side.cross(forward).normalized()
    left, top, right, bottom = SPRAY_RECT if spray else SINGLE_LEAF_RECT
    anchor_x, anchor_y = SPRAY_ROOT if spray else ((left + right) * .5, bottom - 3)
    pivot_u = (anchor_x - left) / (right - left)
    pivot_v = (bottom - anchor_y) / (bottom - top)
    rows = (0, .28, .61, 1) if detail else (0, .51, 1)
    vertices, uv, faces = [], [], []
    if bend is None:
        bend = .21 if spray else .27
    for t in rows:
        travel = t - pivot_v
        center = base + forward * (travel * length) + up * (length * (lift * math.sin(math.pi * travel) - bend * travel**2))
        for u in (0, .5, 1):
            cross = u - pivot_u
            vertices.append(center + side * width * cross - up * width * .43 * cross**2)
            # The atlas helper converts the painter's top-down tile coordinates to UVs.
            uv.append(((left + (right - left) * u) / 256, (bottom - (bottom - top) * t) / 256))
    for row in range(len(rows) - 1):
        for column in (0, 1):
            a = row * 3 + column
            faces.append((a, a + 1, a + 4, a + 3))
    slot = f"{species}/{'LeafAccent' if accent else 'Leaf'}"
    mesh.add(vertices, [(face, [uv[i] for i in face]) for face in faces], slot, anchor=base)


def shade_sprays(obj, runtime_scale, *, species):
    """Blend the curved card normals toward the crown, leaving the bark and fruit untouched."""
    mesh = obj.data
    uvs = mesh.uv_layers.active.data
    left, top, right, bottom = SPRAY_RECT
    rectangles = [
        (atlas_uv(slot, left / 256, bottom / 256), atlas_uv(slot, right / 256, top / 256))
        for slot in (f'{species}/Leaf', f'{species}/LeafAccent')
    ]
    normals = [corner.vector.copy() for corner in mesh.corner_normals]
    has_sprays = False
    for loop in mesh.loops:
        u, v = uvs[loop.index].uv
        if not any(xmin - 1e-7 <= u <= xmax + 1e-7 and ymin - 1e-7 <= v <= ymax + 1e-7
                   for (xmin, ymin), (xmax, ymax) in rectangles):
            continue
        x, y, z = mesh.vertices[loop.vertex_index].co * runtime_scale
        radial = Vector(((x - .06) * .67, (y - .045) * .67, .83 + (z - 1.72) * .27)).normalized()
        normals[loop.index] = normals[loop.index].lerp(radial, .74).normalized()
        has_sprays = True
    if has_sprays:
        mesh.normals_split_custom_set(normals)


def configure_cutout(material):
    nodes = material.node_tree.nodes
    base_color = next(node for node in nodes if node.label == 'Shared fruit tree BaseColor')
    cutoff = nodes.new('ShaderNodeMath')
    cutoff.operation = 'GREATER_THAN'
    cutoff.inputs[1].default_value = .42
    material.node_tree.links.new(base_color.outputs['Alpha'], cutoff.inputs[0])
    material.node_tree.links.new(cutoff.outputs[0], nodes.get('Principled BSDF').inputs['Alpha'])
