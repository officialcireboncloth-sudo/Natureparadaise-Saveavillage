import json
import math
import os
import re
import struct
import sys
from pathlib import Path

work = Path(__file__).resolve().parent
sys.path.insert(0, str(work.parents[1] / 'ArtSource/FruitTrees/Scripts'))
os.environ['NP_AVOCADO_STAGE'] = str(work)
from fruit_tree_atlas import ASSETS, ATLAS, SOURCE, TEXTURES, SIZE, TILE, PADDING, SLOTS
from fruit_tree_dimensions import ORCHARD_HEIGHT
from publish_apple_unity import CUTOUT_MATERIAL, TREE_DATA, blocks, serialize_mesh
from publish_avocado_unity import digest, publish, staged_path
from unity_art_io import bounds, v3


def sidecar(path):
    return path.with_name(path.name + '.meta')


def guid(path):
    return re.search(r'(?m)^guid: ([a-f0-9]{32})$', sidecar(path).read_text())[1]


template = (ASSETS / 'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
material_guid = guid(CUTOUT_MATERIAL)
files, protected = {}, {CUTOUT_MATERIAL, sidecar(CUTOUT_MATERIAL), ASSETS / 'Data/Balance/Items.csv'}

for species in ('Apple', 'Avocado', 'Cherry', 'Guava', 'Mango'):
    models = ASSETS / 'mesh/Trees' / species
    prefabs = ASSETS / 'Prefabs/Trees' / species
    definition = TREE_DATA / f'{species}.asset'
    protected.add(definition)
    for phase in ('Young', 'FruitForming', 'HarvestReady'):
        label = f'NP_{species}_{phase}'
        prefab = prefabs / (label + '.prefab')
        protected.add(sidecar(prefab))
        original = prefab.read_bytes()
        data = blocks(original.decode().replace('\r\n', '\n'))
        assert guid(prefab) in definition.read_text(), prefab
        assert material_guid in data[230][1] and material_guid in data[231][1], prefab
        for lod, suffix in enumerate(('', '_LOD1')):
            name = label + suffix
            source = work / species / 'Mesh' / (name + '.json')
            spec = json.loads(source.read_text())
            target = models / (name + '.asset')
            protected.add(sidecar(target))
            mesh_guid = guid(target)
            assert mesh_guid in data[330 + lod][1]
            assert spec['name'] == name and spec['materials'] == [ATLAS], name
            vertices = spec['vertices']
            assert all(math.isfinite(v) for key in ('vertices','normals','uv') for row in spec[key] for v in row)
            indices, = spec['submeshes']
            assert 0 <= min(indices) <= max(indices) < len(vertices)
            assert len(indices) == 3 * spec['triangles']
            for u, v in spec['uv']:
                column, x = divmod(u * SIZE, TILE)
                row, y = divmod((1 - v) * SIZE, TILE)
                assert int(row) * (SIZE // TILE) + int(column) in SLOTS.values(), name
                assert PADDING - .001 <= x <= TILE - PADDING + .001, name
                assert PADDING - .001 <= y <= TILE - PADDING + .001, name
            old = target.read_text()
            old_triangles = int(re.search(r'(?m)^    indexCount: (\d+)$', old)[1]) // 3
            assert spec['triangles'] <= max(old_triangles * 1.12, old_triangles + 350), name
            if phase != 'Young':
                assert abs(max(point[1] for point in vertices) - ORCHARD_HEIGHT) < .001, name
            if lod == 0:
                center, extents = bounds(vertices)
                size = max(extents) * 2
            out = staged_path(target)
            assert serialize_mesh(spec, template, out) == mesh_guid
            packed = re.search(r'(?m)^    _typelessdata: ([a-f0-9]+)$', out.read_text())[1]
            assert [row[:3] for row in struct.iter_unpack('<12f', bytes.fromhex(packed))] == list(map(tuple, vertices))
            if digest(out) != digest(target):
                files[target] = out
            print(f'{name} {old_triangles} → {spec["triangles"]} triangles', flush=True)

        first_lod = float(re.search(rb'screenRelativeHeight: ([\d.]+)', original)[1])
        assert size * 2 / 13 < first_lod, (prefab, size, first_lod)
        updated = original
        for pattern, value in ((rb'(m_LocalReferencePoint: )\{[^}]+\}', v3(center)),
                               (rb'(m_Size: )[\d.]+', f'{size:.7f}')):
            updated, count = re.subn(pattern, lambda m: m[1] + value.encode(), updated)
            assert count == 1
        out = staged_path(prefab)
        out.write_bytes(updated)
        if updated != original:
            files[prefab] = out

    blend = f'NP_{species}.blend'
    files[SOURCE / 'Blender' / blend] = work / species / blend

for map_name in ('BaseColor', 'SurfaceMask', 'Normal'):
    for folder, extension in ((SOURCE / 'Textures', 'xcf'), (TEXTURES, 'png')):
        name = f'{ATLAS}_{map_name}.{extension}'
        source, target = work / 'Atlas' / name, folder / name
        if map_name == 'BaseColor':
            files[target] = source
        else:
            protected.add(target)
        if extension == 'png':
            protected.add(sidecar(target))

if '--publish' in sys.argv:
    before = {path: digest(path) for path in protected}
    publish(files)
    assert all(digest(path) == expected for path, expected in before.items())
    print(f'Installed {len(files)} files. All existing references and tree definitions verified.', flush=True)
