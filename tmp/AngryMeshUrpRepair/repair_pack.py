import hashlib
import json
import os
import re
import shutil
import sys
from collections import Counter
from pathlib import Path

from audit_pack import guid_for, material_array, metadata_guid, meadow, object_guid, pack, project, scratch, shader_ref, template, texture_guid


def content_hash(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').digest()


def normalized(data):
    return data.replace(b'\r\n', b'\n')


def texture_references(data):
    return set(texture_guid.findall(data.decode('utf-8-sig')))


def source_remaps(fbx, report, materials_by_name):
    candidates = report['prefab_candidates']
    names = report['source_materials']
    mapping = {}
    if candidates:
        prefab = candidates[0][0]
        for _, model, slots in (row for row in candidates if row[0] == prefab):
            source = report['models'].get(model, names)
            # Some far LODs strip one slot. The full LODs identify its correct material.
            if len(source) != len(slots):
                continue
            for name, guid in zip(source, slots):
                assert guid and mapping.setdefault(name, guid) == guid, (fbx, model, name)
    if re.fullmatch(r'SM_Fence_0[1-5]', fbx.stem):
        assert not mapping and names == ['Plank', 'Ivy'], fbx
        mapping = {name: materials_by_name[target] for name, target in (
            ('Plank', 'M_Plank_01_Summer'), ('Ivy', 'M_FenceIvy_01_Summer'))}
    elif re.fullmatch(r'SM_VFX_WindMesh_0[23]', fbx.stem):
        assert not mapping and names == ['VFXMaterial'], fbx
        mapping = {'VFXMaterial': materials_by_name['M_VFX_Wind_01']}
    assert set(mapping) == set(names), (fbx, mapping, names)
    return mapping


def model_import_settings(path, mapping):
    raw = path.read_bytes()
    newline = '\r\n' if b'\r\n' in raw else '\n'
    source = normalized(raw).decode('utf-8')
    lines = ['  externalObjects:']
    for name, guid in mapping.items():
        assert re.fullmatch(r'[\w ]+', name), name
        lines.extend(('  - first:', '      type: UnityEngine:Material',
                      '      assembly: UnityEngine.CoreModule', f'      name: {name}',
                      f'    second: {{fileID: 2100000, guid: {guid}, type: 2}}'))
    remaps = '\n'.join(lines)
    if '\n' + remaps + '\n' in source:
        assert '    materialImportMode: 1\n' in source, path
        return raw
    assert source.count('  externalObjects: {}\n') == source.count('    materialImportMode: 0\n') == 1, path
    revised = source.replace('  externalObjects: {}\n', remaps + '\n')
    revised = revised.replace('    materialImportMode: 0\n', '    materialImportMode: 1\n')
    assert metadata_guid.search(source)[1] == metadata_guid.search(revised)[1], path
    return revised.replace('\n', newline).encode('utf-8')


def plan_changes():
    changes = {}
    entries = json.loads((scratch / 'TemplateManifest.json').read_text(encoding='utf-8-sig'))
    shipped = {project / entry['Path']: template / entry['Guid'] for entry in entries if entry['HasAsset']}
    materials = sorted(pack.rglob('*.mat'))
    material_guids = {guid_for(path): path for path in materials}
    material_names = {path.stem: guid for guid, path in material_guids.items()}
    textures = {guid_for(path): path for path in pack.rglob('*') if path.suffix.lower() in {'.tif', '.hdr'}}
    shaders = {guid_for(path): path for path in pack.rglob('*.shader')}
    shader_sources = {}
    package = next((project / 'Library/PackageCache').glob('com.unity.render-pipelines.universal@*'))
    urp_shaders = {guid_for(path): path for path in (package / 'Shaders').rglob('*.shader')}

    assert len(materials) == len(material_guids) == len(material_names) and set(materials) <= shipped.keys()
    for path in shaders.values():
        folder = shipped[path]
        assert folder.name == guid_for(path)
        data = (folder / 'asset').read_bytes()
        assert b'"RenderPipeline"="UniversalPipeline"' in data, path
        shader_sources[folder.name] = data.decode('utf-8-sig')
        if normalized(data) != normalized(path.read_bytes()):
            changes[path] = data
    for path in materials:
        folder = shipped[path]
        assert folder.name == guid_for(path)
        old = path.read_bytes()
        data = (folder / 'asset').read_bytes()
        if path.name == 'M_TerrainLit_Standard.mat':
            # Use the vendor's URP terrain material with the original identity used by the demo terrains.
            replacement = shipped[path.with_name('M_TerrainLit_URP.mat')] / 'asset'
            data = replacement.read_bytes().replace(b'm_Name: M_TerrainLit_URP', b'm_Name: M_TerrainLit_Standard')
            data = data.replace(b'm_EnableInstancingVariants: 0', b'm_EnableInstancingVariants: 1')
        old_references = texture_references(old)
        template_references = texture_references(data)
        shader = shader_ref.search(data.decode('utf-8-sig'))[1]
        shader_id = object_guid.search(shader)[1]
        if shader_id not in shaders and shader_id not in urp_shaders:
            assert path.parent.name == 'Skybox' and 'fileID: 108, guid: 0000000000000000f000000000000000' in shader, path
        for _, guid in texture_references(data):
            if guid in textures:
                continue
            if path.name == 'M_Skybox_Diorama_01.mat' and guid == '58761b8bc8b08534f8d7966c4558b26f':
                summer = meadow / 'Sources/Textures/Skybox/T_Skybox_Summer_01.HDR'
                data = data.replace(guid.encode(), guid_for(summer).encode())
                continue
            references = re.findall(rb'(?m)^    - ([^:\n]+):\r?\n        m_Texture: \{fileID: 2800000, guid: '
                                    + guid.encode() + rb', type: 3\}', data)
            assert references and all(not re.search(r'\b' + re.escape(prop.decode()) + r'\b', shader_sources[shader_id])
                                      for prop in references), (path, guid, references)
        assert old_references in (template_references, texture_references(data)), path
        if normalized(data) != normalized(old):
            changes[path] = data

    audited = json.loads((scratch / 'FbxAudit.json').read_text(encoding='utf-8'))
    expected = {}
    for relative, report in audited.items():
        fbx = pack / relative
        assert fbx.is_file() and guid_for(fbx) == report['guid'], fbx
        mapping = source_remaps(fbx, report, material_names)
        assert set(mapping.values()) <= material_guids.keys()
        meta = Path(str(fbx) + '.meta')
        data = model_import_settings(meta, mapping)
        if data != meta.read_bytes():
            changes[meta] = data
        expected[fbx.relative_to(project).as_posix()] = {key: material_guids[value].relative_to(project).as_posix()
                                                       for key, value in mapping.items()}

    prefabs = sorted(pack.rglob('*.prefab'))
    models = sorted(pack.rglob('*.fbx'))
    assert len(audited) == len(models)
    for prefab in prefabs:
        for materials in material_array.findall(prefab.read_text(encoding='utf-8-sig')):
            for slot in materials.splitlines():
                guid = object_guid.search(slot)
                assert guid and guid[1] in material_guids, (prefab, slot)
    protected = set(prefabs + models + list(textures.values()) + list(pack.rglob('*.unity')))
    protected.update(Path(str(path) + '.meta') for path in materials_by_paths(material_guids, shaders, textures))
    protected.update((project / path) for path in ('Packages/manifest.json', 'Packages/packages-lock.json',
                                                  'ProjectSettings/GraphicsSettings.asset', 'ProjectSettings/QualitySettings.asset'))
    return changes, protected, expected, len(prefabs), len(material_guids)


def materials_by_paths(*groups):
    for group in groups:
        yield from group.values()


def publish(changes, protected):
    backups = scratch / 'Before'
    staged = scratch / 'Staged'
    protected_hashes = {path: content_hash(path) for path in protected}
    baseline = {}
    for target, data in changes.items():
        relative = target.resolve().relative_to(pack.resolve())
        before, after = backups / relative, staged / relative
        before.parent.mkdir(parents=True, exist_ok=True)
        after.parent.mkdir(parents=True, exist_ok=True)
        if not before.exists():
            shutil.copy2(target, before)
        after.write_bytes(data)
        baseline[target] = (before, after)

    updated = []
    try:
        for target, (before, after) in baseline.items():
            assert content_hash(target) == content_hash(before), f'Changed while staging: {target}'
            os.replace(after, target)
            updated.append(target)
            assert target.read_bytes() == changes[target], target
        assert all(content_hash(path) == expected for path, expected in protected_hashes.items())
    except BaseException:
        for target in reversed(updated):
            shutil.copy2(baseline[target][0], target)
        raise


if __name__ == '__main__':
    changes, protected, expected, prefabs, materials = plan_changes()
    (scratch / 'ExpectedModelMaterials.json').write_text(json.dumps(expected, indent=2), encoding='utf-8')
    print('Planned changes:', dict(Counter(path.suffix for path in changes)), flush=True)
    print(f'Resolved original textures and renderers across {materials} materials, {prefabs} prefabs, and {len(expected)} FBXs.', flush=True)
    if '--apply' in sys.argv:
        publish(changes, protected)
        print('URP shader, material, and FBX import changes applied.')
