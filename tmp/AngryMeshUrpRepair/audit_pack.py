import hashlib
import json
import re
import sys
import types
from collections import Counter, defaultdict
from pathlib import Path


scratch = Path(__file__).resolve().parent
project = scratch.parents[1]
pack = project / 'Assets/ANGRY MESH'
meadow = pack / 'Stylized Pack - Meadow Environment'
sources = meadow / 'Sources'
template = scratch / 'Template'
guid_pattern = r'[a-f0-9]{32}'
metadata_guid = re.compile(r'(?m)^guid: (' + guid_pattern + r')\s*$')
object_guid = re.compile(r'guid: (' + guid_pattern + r')')
texture_guid = re.compile(r'm_Texture: \{fileID: (-?\d+), guid: (' + guid_pattern + r')')
shader_ref = re.compile(r'(?m)^  m_Shader: (\{[^}]+\})')
objects = re.compile(r'(?ms)^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)')
mesh_guid = re.compile(r'(?m)^  m_Mesh: \{fileID: (-?\d+), guid: (' + guid_pattern + r')')
material_array = re.compile(r'(?m)^  m_Materials:\n((?:  - \{fileID: [^\n]+\}\n)+)')


def path_for_guid(meta):
    return Path(str(meta).removesuffix('.meta'))


def guid_for(path):
    return metadata_guid.search(Path(str(path) + '.meta').read_text(encoding='utf-8-sig'))[1]


def unity_field(doc, name, pattern):
    match = re.search(r'(?m)^  ' + name + r': ' + pattern + r'\s*$', doc)
    return match[1] if match else None


def material_slots(body):
    match = material_array.search(body)
    if not match:
        return ()
    return tuple(object_guid.search(line)[1] if object_guid.search(line) else None for line in match[1].splitlines())


def audit_template():
    records = json.loads((scratch / 'TemplateManifest.json').read_text(encoding='utf-8-sig'))
    by_path = {project / entry['Path']: template / entry['Guid'] for entry in records if entry['HasAsset']}
    pack_guids = {guid_for(path_for_guid(meta)): path_for_guid(meta) for meta in pack.rglob('*.meta')}
    live_materials = set(pack.rglob('*.mat'))
    changes = defaultdict(list)
    for file, folder in by_path.items():
        if file.suffix not in {'.mat', '.shader', '.terrainlayer'}:
            continue
        incoming = (folder / 'asset').read_bytes()
        if file.exists():
            assert metadata_guid.search((folder / 'asset.meta').read_text(encoding='utf-8-sig'))[1] == guid_for(file), file
            current = file.read_bytes()
            if current.replace(b'\r\n', b'\n') != incoming.replace(b'\r\n', b'\n'):
                changes[file.suffix].append(file.name)
            if file.suffix == '.mat':
                old_textures = set(texture_guid.findall(current.decode('utf-8-sig')))
                new_textures = set(texture_guid.findall(incoming.decode('utf-8-sig')))
                if old_textures != new_textures:
                    print('Template texture change', file.name,
                          'adds', [pack_guids.get(g, g) for _, g in new_textures - old_textures],
                          'removes', [pack_guids.get(g, g) for _, g in old_textures - new_textures])
        elif file.suffix == '.mat':
            missing = {g for _, g in texture_guid.findall(incoming.decode('utf-8-sig')) if g not in pack_guids}
            print('Optional template material', file.name, 'missing texture GUIDs:', sorted(missing))
    print('Original materials outside template:', [x.name for x in live_materials - by_path.keys()])
    print('Existing assets with vendor URP changes:', {k: len(v) for k, v in changes.items()})
    print('Changed terrain layers:', changes['.terrainlayer'])
    return pack_guids


def collect_prefab_materials(pack_guids):
    candidates = defaultdict(list)
    for prefab in sorted(pack.rglob('*.prefab')):
        text = prefab.read_text(encoding='utf-8-sig')
        blocks = objects.findall(text)
        gameobjects = {key: unity_field(body, 'm_Name', '(.*)') for type_id, key, body in blocks if type_id == '1'}
        mesh_filters = {}
        for type_id, _, body in blocks:
            if type_id == '33' and (mesh := mesh_guid.search(body)):
                owner = unity_field(body, 'm_GameObject', r'\{fileID: (-?\d+)\}')
                mesh_filters[owner] = mesh[2]
        for type_id, _, body in blocks:
            if type_id not in ('23', '137', '199'):
                continue
            slots = material_slots(body)
            if not slots:
                continue
            owner = unity_field(body, 'm_GameObject', r'\{fileID: (-?\d+)\}')
            name = gameobjects.get(owner)
            if type_id == '23':
                mesh = mesh_filters.get(owner)
            else:
                match = mesh_guid.search(body)
                mesh = match[2] if match else None
            if mesh in pack_guids and pack_guids[mesh].suffix.lower() == '.fbx':
                candidates[mesh].append((prefab.relative_to(pack).as_posix(), name or '', slots))
    return candidates


def collect_fbx_slots(path, parser):
    fbx, _ = parser.parse(str(path))
    objects = next(node.elems for node in fbx.elems if node.id == b'Objects')
    connections = next(node.elems for node in fbx.elems if node.id == b'Connections')
    materials = {node.props[0]: node.props[1].partition(b'\x00\x01')[0].decode('utf-8')
                 for node in objects if node.id == b'Material'}
    models = {node.props[0]: node.props[1].partition(b'\x00\x01')[0].decode('utf-8')
              for node in objects if node.id == b'Model' and node.props[-1] == b'Mesh'}
    models_to_slots = {name: [] for name in models.values()}
    for link in connections:
        kind, child, parent, *_ = link.props
        if kind == b'OO' and child in materials and parent in models:
            models_to_slots[models[parent]].append(materials[child])
    return materials, models_to_slots


def rank_prefab(path, model_name):
    path = Path(path)
    name = path.stem
    exact_summer = name.replace('_Summer', '').removeprefix('P_') == model_name.removeprefix('SM_')
    return ('Prefabs Terrain Details' in path.parts, 'DioramaExample' in path.parts,
            any(season in name for season in ('_Autumn', '_Winter', '_DrySummer')),
            not exact_summer, name, path.as_posix())


if __name__ == '__main__':
    blender = types.ModuleType('blender_fbx')
    blender.__path__ = [sys.argv[1]]
    sys.modules['blender_fbx'] = blender
    from blender_fbx import parse_fbx

    pack_guids = audit_template()
    candidates = collect_prefab_materials(pack_guids)
    records = {}
    for fbx in sorted(pack.rglob('*.fbx')):
        guid = guid_for(fbx)
        materials, models = collect_fbx_slots(fbx, parse_fbx)
        matches = set(candidates[guid])
        ranked = sorted(matches, key=lambda value: (rank_prefab(value[0], fbx.stem), value[1], value[2]))
        material_names = {key: value.stem for key, value in pack_guids.items() if value.suffix == '.mat'}
        records[fbx.relative_to(pack).as_posix()] = {
            'guid': guid, 'source_materials': list(dict.fromkeys(materials.values())),
            'models': models, 'prefab_candidates': ranked,
        }
        chosen = ranked[0][0] if ranked else None
        slot_sets = {tuple(slots) for _, slots in models.items()}
        print(fbx.stem, 'models:', sorted(slot_sets), 'chosen prefab:', chosen or 'NONE')
        for prefab, name, slots in ranked:
            if prefab == chosen:
                print(' ', name, models.get(name, '-'), '=>', [material_names.get(guid, guid) for guid in slots])
    (scratch / 'FbxAudit.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
