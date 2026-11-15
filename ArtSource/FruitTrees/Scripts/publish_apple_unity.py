"""Publish Blender meshes and the GIMP atlas through the existing Unity asset format."""
import json
import os
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import ASSETS, ATLAS, PROJECT, SCRATCH, TEXTURES

sys.path.insert(0, str(PROJECT / 'ArtSource/Crops/Scripts'))
from unity_art_io import bounds, meta, tangents, v3


MODELS = ASSETS / 'mesh/Trees/Apple'
PREFABS = ASSETS / 'Prefabs/Trees/Apple'
MATERIALS = ASSETS / 'Material/Shared/Plants'
CUTOUT_MATERIAL = MATERIALS / 'NP_FruitTrees_Cutout.mat'
TREE_DATA = ASSETS / 'Resources/Trees'
YAML = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
# A mature apple occupies about .25 of the map view at size 6.5 before LOD bias.
# Keep the lighter mesh at that zoom even on PC (LOD bias 2).
APPLE_LOD0_THRESHOLD = .52


def save_material():
    plant = 'NP_Plants_Unified_01'
    template = MATERIALS / f'{plant}.mat'
    data = template.read_text().replace(plant, ATLAS)
    for suffix in ('BaseColor', 'Normal', 'SurfaceMask'):
        original = TEXTURES.parent / 'Plants' / f'{plant}_{suffix}.png'
        new = TEXTURES / f'{ATLAS}_{suffix}.png'
        assert new.is_file(), new
        data = data.replace(meta(original), meta(new, original))
    data = data.replace('_BumpScale: 1', '_BumpScale: 0.6')
    destination = MATERIALS / f'{ATLAS}.mat'
    if not destination.is_file():
        destination.write_text(data)
    return meta(destination, main_id=2100000)


def save_cutout_material(destination=CUTOUT_MATERIAL):
    if not destination.is_file():
        source = (MATERIALS / (ATLAS + '.mat')).read_text()
        source = source.replace(f'm_Name: {ATLAS}', f'm_Name: {destination.stem}')
        source = source.replace('m_ValidKeywords:\n', 'm_ValidKeywords:\n  - _ALPHATEST_ON\n')
        source = source.replace('m_CustomRenderQueue: 2000', 'm_CustomRenderQueue: 2450')
        source = source.replace('RenderType: Opaque', 'RenderType: TransparentCutout')
        source = source.replace('_AlphaClip: 0', '_AlphaClip: 1')
        assert '- _ALPHATEST_ON' in source and '_AlphaClip: 1' in source
        destination.write_text(source)
    return meta(destination, main_id=2100000)


def serialize_mesh(spec, template, destination):
    vertices = spec['vertices']
    indices, = spec['submeshes']
    assert len(indices) == 3 * spec['triangles']
    assert len(vertices) == len(spec['normals']) == len(spec['uv'])
    center, extent = bounds(vertices)
    aabb = f'm_Center: {v3(center)}\n      m_Extent: {v3(extent)}\n'
    submesh = (f'  m_SubMeshes:\n  - serializedVersion: 2\n    firstByte: 0\n'
               f'    indexCount: {len(indices)}\n    topology: 0\n    baseVertex: 0\n'
               f'    firstVertex: 0\n    vertexCount: {len(vertices)}\n    localAABB:\n      {aabb}')
    index_bytes = struct.pack(f'<{len(indices)}I', *indices)
    vertex_bytes = bytearray()
    for point, normal, tangent, uv in zip(vertices, spec['normals'], tangents(spec), spec['uv']):
        vertex_bytes.extend(struct.pack('<12f', *point, *normal, *tangent, *uv))
    data = template.replace('SoilScanner_Cozy_LOD2', spec['name'])
    replacements = {
        r'(?s)  m_SubMeshes:\n.*?(?=  m_Shapes:)': submesh,
        r'(?m)^  m_IndexBuffer:.*$': '  m_IndexBuffer: ' + index_bytes.hex(),
        r'm_VertexCount: \d+': 'm_VertexCount: ' + str(len(vertices)),
        r'm_DataSize: \d+': 'm_DataSize: ' + str(len(vertex_bytes)),
        r'(?m)^    _typelessdata:.*$': '    _typelessdata: ' + vertex_bytes.hex(),
        r'(?s)  m_LocalAABB:\n.*?(?=  m_MeshUsageFlags:)':
            f'  m_LocalAABB:\n    m_Center: {v3(center)}\n    m_Extent: {v3(extent)}\n',
    }
    for pattern, replacement in replacements.items():
        data, count = re.subn(pattern, lambda _: replacement, data)
        assert count == 1, pattern
    destination.write_text(data)
    return meta(destination, main_id=4300000)


def blocks(source):
    return {int(file_id): (int(class_id), body)
            for class_id, file_id, body in re.findall(r'(?ms)^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)', source)}


def prefab(name, meshes, material, reference, mesh_specs, *, directory=PREFABS):
    data = dict(reference)
    data[100] = (1, re.sub(r'(?m)^  m_Name: .+$', '  m_Name: ' + name, reference[100][1]))
    for index, mesh_id in enumerate(meshes):
        game, mesh, renderer = 110 + index, 330 + index, 230 + index
        data[game] = (1, re.sub(r'(?m)^  m_Name: .+$', '  m_Name: ' + ('Mesh LOD' + str(index)), reference[game][1]))
        data[mesh] = (33, re.sub(r'm_Mesh: \{[^}]+}', f'm_Mesh: {{fileID: 4300000, guid: {mesh_id}, type: 2}}',
                                 reference[mesh][1]))
        data[renderer] = (23, re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',
                                     lambda m: m[1] + material, reference[renderer][1]))
    if len(meshes) == 1:
        for file_id in (411, 111, 331, 231, 2050):
            del data[file_id]
        data[400] = (4, data[400][1].replace('  - {fileID: 411}\n', ''))
        data[100] = (1, data[100][1].replace('  - component: {fileID: 2050}\n', ''))
    else:
        center, extent = bounds(mesh_specs[name]['vertices'])
        lod = re.sub(r'm_LocalReferencePoint: \{[^}]+}', 'm_LocalReferencePoint: ' + v3(center), data[2050][1])
        lod = re.sub(r'm_Size: [\d.]+', f'm_Size: {max(extent)*2:.7f}', lod)
        lod = re.sub(r'screenRelativeHeight: [\d.]+', f'screenRelativeHeight: {APPLE_LOD0_THRESHOLD}', lod, count=1)
        data[2050] = (205, lod)
    destination = directory / (name + '.prefab')
    destination.write_text(YAML + ''.join(f'--- !u!{type_id} &{file_id}\n{body}'
                                          for file_id, (type_id, body) in data.items()))
    return meta(destination, ASSETS / 'Prefabs/Crops/G01/NP_Blueberry_Seed.prefab')


def ref(guid):
    return f'{{fileID: 100, guid: {guid}, type: 3}}'


def bind(prefabs):
    definition = TREE_DATA / 'Apple.asset'
    data = definition.read_text()
    slots = ['Seed', 'Sprout', 'Young', 'FruitForming', 'HarvestReady']
    scales = ['0.09', '0.3', '1', '1', '1']
    stage = iter(zip(slots, scales))

    def visual(match):
        name, scale = next(stage)
        return f'    model: {ref(prefabs[name])}\n    scale: {{x: {scale}, y: {scale}, z: {scale}}}'

    data, count = re.subn(r'(?m)^    model: \{[^}]+}\n    scale: \{[^}]+}', visual, data)
    assert count == len(slots)
    stump = '  stumpModel: ' + ref(prefabs['Stump']) + '\n'
    data = re.sub(r'(?m)^  stumpModel: .*\n', '', data) + stump
    if data != definition.read_text():
        definition.write_text(data)

    for item, model, scale in [('Apple Seedling', 'Seed', '0.09'), ('Apple Fruit', 'Produce', '1')]:
        path = TREE_DATA / (item + '.asset')
        item_data = re.sub(r'(?m)^  worldPrefab: .*\n', '', path.read_text())
        item_data, count = re.subn(r'(?m)^  worldScale: \{[^}]+}',
                                   f'  worldPrefab: {ref(prefabs[model])}\n'
                                   f'  worldScale: {{x: {scale}, y: {scale}, z: {scale}}}', item_data)
        assert count == 1
        if item_data != path.read_text():
            path.write_text(item_data)


def main():
    for path in (MODELS.parent, MODELS, PREFABS.parent, PREFABS, TEXTURES):
        path.mkdir(parents=True, exist_ok=True)
        meta(path, folder=True)
    save_material()
    material = save_cutout_material()
    example_mesh = (ASSETS / 'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
    example_prefab = blocks((ASSETS / 'Prefabs/Crops/G01/NP_Blueberry_Seed.prefab').read_text())
    mesh_source = Path(os.environ['NP_APPLE_STAGE']) / 'Mesh' if 'NP_APPLE_STAGE' in os.environ else SCRATCH
    specs = {p.stem: json.loads(p.read_text()) for p in mesh_source.glob('NP_Apple_*.json')}
    names = ['Seed', 'Sprout', 'Young', 'FruitForming', 'HarvestReady', 'Stump', 'Produce']
    lods = ['Young', 'FruitForming', 'HarvestReady']
    assert set(specs) == {'NP_Apple_' + s for s in names + [v + '_LOD1' for v in lods]}
    meshes = {key: serialize_mesh(spec, example_mesh, MODELS / (key + '.asset')) for key, spec in specs.items()}
    prefabs = {}
    for key in names:
        name = 'NP_Apple_' + key
        refs = [meshes[name]]
        if key in lods:
            refs.append(meshes[name + '_LOD1'])
        prefabs[key] = prefab(name, refs, material, example_prefab, specs)
    bind(prefabs)
    print('APPLE_UNITY_PUBLISH_PASS', {key: spec['triangles'] for key, spec in specs.items()})


if __name__ == '__main__':
    main()
