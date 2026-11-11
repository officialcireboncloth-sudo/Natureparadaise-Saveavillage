"""Build the editable Daisy/Moonflower master and matching Unity near/distance meshes."""
from pathlib import Path
import json
import sys

import bpy
from mathutils import Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
import g14_geometry as geometry
import plant_blender_common as art
from unified_plant_atlas import A, MAPS, NAME, REGISTRY, SOURCE, TEXTURES, load, write


GROUP = 'G14'
MASTER = SOURCE / GROUP / 'Blender/G14_DaisyMoonflower_Master.blend'
MODELS = A / 'mesh/Crops' / GROUP
PHASES = ('Seed', 'Sprout', 'Young', 'BudForming', 'Ready', 'Harvest')
KINDS = {'Daisy': geometry.daisy, 'Moonflower': geometry.moonflower}
PAGE = 'NP_Plants_DaisyMoonflower_Source_01'


def atlas_image(suffix):
    image = bpy.data.images.load(str(TEXTURES / f'{NAME}_{suffix}.png'), check_existing=False)
    image.colorspace_settings.name = 'sRGB' if suffix == 'BaseColor' else 'Non-Color'
    if suffix == 'SurfaceMask':
        image.alpha_mode = 'CHANNEL_PACKED'
    image.pack()
    return image


def material():
    mat = bpy.data.materials.new(NAME)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    bsdf.inputs['Specular IOR Level'].default_value = .25
    for suffix in MAPS:
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = atlas_image(suffix)
        tex.label = suffix
        if suffix == 'BaseColor':
            links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
        elif suffix == 'Normal':
            normal = nodes.new('ShaderNodeNormalMap')
            normal.inputs['Strength'].default_value = .32
            links.new(tex.outputs['Color'], normal.inputs['Color'])
            links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
        else:
            roughness = nodes.new('ShaderNodeMath')
            roughness.operation = 'SUBTRACT'
            roughness.inputs[0].default_value = 1.
            links.new(tex.outputs['Alpha'], roughness.inputs[1])
            links.new(roughness.outputs[0], bsdf.inputs['Roughness'])
    return mat


def setup_parts(collection, root, build, phase, low, floor):
    art.current = collection
    art.parent = root
    build(phase, low)
    parts = list(root.children)
    z_offset = floor - min(v.co.z for ob in parts for v in ob.data.vertices)
    for ob in parts:
        ob.data.transform(Matrix.Translation((0, 0, z_offset)))
    return parts


def export(parts, name, scene, mat):
    copies = []
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        obj = bpy.data.objects.new(part.name + ' export', part.data.copy())
        scene.collection.objects.link(obj)
        obj.select_set(True)
        copies.append(obj)
    bpy.context.view_layer.objects.active = copies[0]
    if len(copies) > 1:
        bpy.ops.object.join()
    obj = copies[0]
    obj.name = name
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.material_index = 0
    art.clean_export_triangles(obj)
    spec = art.write_mesh_spec(obj, name)
    spec.update(sourceAtlas=PAGE, unifiedAtlasVersion=1)
    (MODELS / f'{name}.json').write_text(json.dumps(spec, separators=(',', ':')))
    bpy.ops.export_scene.fbx(filepath=str(MODELS / f'{name}.fbx'), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, bake_anim=False, add_leaf_bones=False)
    bpy.data.objects.remove(obj, do_unlink=True)
    return spec


def main(only=None):
    manifest_path = MASTER.parent / 'G14_Manifest.json'
    plan_path = MASTER.parent / 'G14_VisualPlan.json'
    if only:
        bpy.ops.wm.open_mainfile(filepath=str(MASTER))
        mat = bpy.data.materials[NAME]
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.label in MAPS:
                previous = node.image
                node.image = atlas_image(node.label)
                if previous.users == 0:
                    bpy.data.images.remove(previous)
        for phase in PHASES:
            collection = bpy.data.collections[f'{only} / {phase}']
            for obj in list(collection.objects):
                bpy.data.objects.remove(obj, do_unlink=True)
            bpy.data.collections.remove(collection)
        plan = load(plan_path)['plants']
        exports = [entry for entry in load(manifest_path)['exports'] if entry['plant'] != only]
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        mat = material()
        plan, exports = {}, []
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    art.configure(mat, MODELS)
    art.slots = {slot['sourceSlot']: slot for slot in load(REGISTRY)['slots'] if slot['sourceAtlas'] == PAGE}
    assert len(art.slots) == 9
    for row, (kind, build) in enumerate(KINDS.items()):
        if only and kind != only:
            continue
        plan[kind] = []
        for col, phase in enumerate(PHASES):
            name = f'NP_{kind}_{phase}'
            collection = bpy.data.collections.new(f'{kind} / {phase}')
            scene.collection.children.link(collection)
            root = bpy.data.objects.new(name, None)
            collection.objects.link(root)
            root.location = ((col - 2.5) * 1.10, row * 1.65, 0)
            root['Plant'] = kind
            root['Phase'] = phase
            floor = .001 if phase != 'Harvest' else .003
            near = export(setup_parts(collection, root, build, phase, False, floor), name, scene, mat)
            lod_root = bpy.data.objects.new('Distance export fixture', None)
            collection.objects.link(lod_root)
            far_parts = setup_parts(collection, lod_root, build, phase, True, floor)
            far = export(far_parts, name + '_LOD1', scene, mat)
            for part in far_parts:
                bpy.data.objects.remove(part, do_unlink=True)
            bpy.data.objects.remove(lod_root, do_unlink=True)
            assert far['triangles'] < near['triangles'] <= 13000, (name, near['triangles'], far['triangles'])
            assert far['triangles'] <= 3000, (name, far['triangles'])
            plan[kind].append(dict(stage=phase, model=name, source_group=GROUP, scale=[1, 1, 1]))
            exports.append(dict(name=name, plant=kind, stage=phase, triangles=near['triangles'],
                lodTriangles=far['triangles'], bounds=[near['min'], near['max']]))
            print('G14_MESH', name, near['triangles'], far['triangles'], flush=True)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                area.spaces.active.region_3d.view_location = (0, .8, .36)
                area.spaces.active.region_3d.view_distance = 8
                area.spaces.active.shading.color_type = 'TEXTURE'
    exports.sort(key=lambda row: (tuple(KINDS).index(row['plant']), PHASES.index(row['stage'])))
    write(manifest_path, dict(group=GROUP, material=NAME, sourceAtlas=PAGE, exports=exports))
    write(plan_path, dict(group=GROUP, plants=plan))
    bpy.ops.wm.save_as_mainfile(filepath=str(MASTER))
    print('G14_MASTER_READY', flush=True)


if __name__ == '__main__':
    main(sys.argv[sys.argv.index('--only')+1] if '--only' in sys.argv else None)
