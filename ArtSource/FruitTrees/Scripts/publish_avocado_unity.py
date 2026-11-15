"""Publish orchard species in the existing Unity mesh and fruit-tree format."""
import csv
import hashlib
import io
import json
import math
import os
import re
import shutil
import sys
import tempfile
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import ASSETS, ATLAS, PADDING, PROJECT, SIZE, SLOTS, SOURCE, TEXTURES, TILE
from fruit_tree_dimensions import ORCHARD_HEIGHT
from publish_apple_unity import CUTOUT_MATERIAL, TREE_DATA, blocks, meta, prefab, ref, save_cutout_material, serialize_mesh


SPECIES = os.environ.get('NP_FRUIT_TREE_SPECIES', 'Avocado')
SEASONS = {'Avocado': 15, 'Cherry': 2, 'Guava': 15, 'Mango': 2}
assert SPECIES in SEASONS
WORK = Path(os.environ[f'NP_{SPECIES.upper()}_STAGE']).resolve()
STAGED = WORK / 'Publish'
MODELS = ASSETS / f'mesh/Trees/{SPECIES}'
PREFABS = ASSETS / f'Prefabs/Trees/{SPECIES}'
ITEMS = ASSETS / 'Resources/Items'
# Mango is already in the catalog. Update its assets in place to keep their GUIDs.
SEED_ITEM = (TREE_DATA if SPECIES == 'Mango' else ITEMS / 'Farming/Seeds') / f'{SPECIES} Seedling.asset'
FRUIT_ITEM = (TREE_DATA if SPECIES == 'Mango' else ITEMS / 'Food') / f'{SPECIES} Fruit.asset'
ICON_ROOT = ASSETS / 'Textures/UI/Icons'
DEFINITION = TREE_DATA / f'{SPECIES}.asset'
PREFIX = f'NP_{SPECIES}_'
NAMES = ('Seed', 'Sprout', 'Young', 'FruitForming', 'HarvestReady', 'Stump', 'Produce')
LODS = ('Young', 'FruitForming', 'HarvestReady')
# Small growth stages export at editable size. Restore their landscape scale here.
SCALES = {'Seed': .10, 'Sprout': .26}


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def staged_path(target):
    out = STAGED / target.relative_to(PROJECT)
    out.parent.mkdir(parents=True, exist_ok=True)
    original_meta = Path(str(target) + '.meta')
    if original_meta.is_file():
        shutil.copy2(original_meta, Path(str(out) + '.meta'))
    return out


def asset_reference(guid, file_id, kind):
    return f'{{fileID: {file_id}, guid: {guid}, type: {kind}}}'


def field(source, name, value):
    line = f'  {name}: {value}'
    updated, count = re.subn(rf'(?m)^  {re.escape(name)}: .*$', lambda _: line, source)
    assert count <= 1, name
    return updated if count else source.rstrip() + '\n' + line + '\n'


def validate_meshes(specs):
    expected = set(NAMES) | {name + '_LOD1' for name in LODS}
    assert set(specs) == expected
    for name, spec in specs.items():
        assert spec['name'] == PREFIX + name and spec['materials'] == [ATLAS], name
        indices, = spec['submeshes']
        assert len(indices) == 3 * spec['triangles'] and 0 <= min(indices) <= max(indices) < len(spec['vertices']), name
        assert all(math.isfinite(value) for key in ('vertices', 'normals', 'uv') for row in spec[key] for value in row), name
        for u, v in spec['uv']:
            column, x = divmod(u * SIZE, TILE)
            row, y = divmod((1 - v) * SIZE, TILE)
            assert int(row) * (SIZE // TILE) + int(column) in SLOTS.values(), name
            assert PADDING - .001 < x < TILE - PADDING + .001 and PADDING - .001 < y < TILE - PADDING + .001, name
        if name.startswith(('HarvestReady', 'FruitForming')):
            assert abs(max(point[1] for point in spec['vertices']) - ORCHARD_HEIGHT) < .025, name
    seed_height, sprout_height = (max(point[1] for point in specs[name]['vertices']) * SCALES[name]
                                  for name in ('Seed', 'Sprout'))
    assert .025 < seed_height < .10 and .20 < sprout_height < .42
    print(f'{SPECIES} height from soil {ORCHARD_HEIGHT:.4f} m. Triangles by mesh:',
          {name: spec['triangles'] for name, spec in specs.items()}, flush=True)


def orchard_material(files):
    if CUTOUT_MATERIAL.is_file():
        return save_cutout_material()
    out = staged_path(CUTOUT_MATERIAL)
    files[CUTOUT_MATERIAL] = out
    return save_cutout_material(out)


def build_prefabs(files):
    specs = {file.stem.removeprefix(PREFIX): json.loads(file.read_text())
             for file in (WORK / 'Mesh').glob(PREFIX + '*.json')}
    validate_meshes(specs)
    material = orchard_material(files)
    template = (ASSETS / 'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
    prefab_template = blocks((ASSETS / 'Prefabs/Crops/G01/NP_Blueberry_Seed.prefab').read_text())
    mesh_guids = {}
    for stage_name, spec in specs.items():
        target = MODELS / (spec['name'] + '.asset')
        out = staged_path(target)
        mesh_guids[stage_name] = serialize_mesh(spec, template, out)
        files[target] = out
    named_specs = {spec['name']: spec for spec in specs.values()}
    prefabs = {}
    for stage_name in (*NAMES, 'SeedDrop'):
        name = PREFIX + stage_name
        target = PREFABS / (name + '.prefab')
        out = staged_path(target)
        ids = [mesh_guids['Seed' if stage_name == 'SeedDrop' else stage_name]]
        if stage_name in LODS:
            ids.append(mesh_guids[stage_name + '_LOD1'])
        prefabs[stage_name] = prefab(name, ids, material, prefab_template, named_specs, directory=out.parent)
        if stage_name == 'SeedDrop':
            # WorldTree retains the item's root scale. A child scale keeps the dropped seed small.
            scale = SCALES['Seed']
            text, count = re.subn(r'(?ms)(^--- !u!4 &410\n.*?^  m_LocalScale: )\{[^}]+\}',
                                  lambda match: match[1] + f'{{x: {scale}, y: {scale}, z: {scale}}}', out.read_text())
            assert count == 1
            out.write_text(text)
        files[target] = out
    return prefabs


def item_icon(files, kind):
    painted = ICON_ROOT / 'Trees' / f'icon_tree_{kind.lower()}_{SPECIES.lower()}.png'
    if painted.is_file() and Path(str(painted) + '.meta').is_file():
        return meta(painted)
    folder, category = ('Seeds', 'seed') if kind == 'Seed' else ('Crops', 'crop')
    target = ICON_ROOT / folder / f'icon_{category}_{SPECIES.lower()}.png'
    source = WORK / 'Previews' / f'{PREFIX}{kind}_Icon.png'
    if not source.is_file():
        assert target.is_file() and Path(str(target) + '.meta').is_file(), target
        return meta(target)
    out = staged_path(target)
    shutil.copy2(source, out)
    files[target] = out
    return meta(out, target.with_name(f'icon_{category}_blueberry.png'))


def bind_growth(files, prefabs):
    definition_path = staged_path(DEFINITION)
    definition_guid = meta(definition_path, main_id=11400000)
    for kind, target, template, visual in (
        ('Seed', SEED_ITEM, 'Apple Seedling', 'SeedDrop'), ('Fruit', FRUIT_ITEM, 'Apple Fruit', 'Produce'),
    ):
        out = staged_path(target)
        current = target if target.is_file() else TREE_DATA / (template + '.asset')
        text = current.read_text()
        if current != target:
            text = text.replace('Apple', SPECIES).replace('apple', SPECIES.lower())
        if kind == 'Seed':
            text = field(text, 'treeDefinition', asset_reference(definition_guid, 11400000, 2))
        text = field(text, 'worldScale', '{x: 1, y: 1, z: 1}')
        text = field(text, 'worldPrefab', ref(prefabs[visual]))
        text = field(text, 'icon', asset_reference(item_icon(files, kind), 21300000, 3))
        out.write_text(text)
        guid = meta(out, main_id=11400000)
        if kind == 'Fruit':
            fruit_guid = guid
        files[target] = out

    if DEFINITION.is_file():
        data = DEFINITION.read_text()
    else:
        data = (TREE_DATA / 'Apple.asset').read_text().replace('Apple', SPECIES)
        data = field(data, 'fruitSeasons', str(SEASONS[SPECIES]))
        data = field(data, 'mutationFruitItem', '{fileID: 0}')
        data = field(data, 'fruitMutationChance', '0')
    data = field(data, 'fruitItem', asset_reference(fruit_guid, 11400000, 2))
    data = field(data, 'stumpModel', ref(prefabs['Stump']))
    phases = iter(NAMES[:5])

    def stage_visual(_):
        stage_name = next(phases)
        scale = SCALES.get(stage_name, 1)
        return f'    model: {ref(prefabs[stage_name])}\n    scale: {{x: {scale}, y: {scale}, z: {scale}}}'

    data, count = re.subn(r'(?m)^    model: \{[^}]+}\n    scale: \{[^}]+}', stage_visual, data)
    assert count == 5
    definition_path.write_text(data)
    files[DEFINITION] = definition_path


def stage_item_rows(files):
    target = ASSETS / 'Data/Balance/Items.csv'
    data = target.read_bytes()
    rows = list(csv.reader(io.StringIO(data.decode('utf-8-sig'))))
    columns = {name: i for i, name in enumerate(rows[0])}
    by_id = {row[0]: row for row in rows[1:] if row}
    newline = '\r\n' if b'\r\n' in data else '\n'
    buffer = io.StringIO(newline='')
    writer = csv.writer(buffer, lineterminator=newline)
    for suffix, asset, display_name in (('fruit', FRUIT_ITEM, SPECIES), ('seedling', SEED_ITEM, f'{SPECIES} Seedling')):
        identifier = f'item.{SPECIES.lower()}_{suffix}'
        if identifier in by_id:
            continue
        row = list(by_id['item.apple_' + suffix])
        row[columns['item_id']] = identifier
        row[columns['asset_path']] = asset.relative_to(PROJECT).as_posix()
        row[columns['display_name']] = display_name
        writer.writerow(row)
    if buffer.tell():
        out = STAGED / target.relative_to(PROJECT)
        out.parent.mkdir(parents=True, exist_ok=True)
        separator = b'' if data.endswith(b'\n') else newline.encode()
        out.write_bytes(data + separator + buffer.getvalue().encode('utf-8'))
        files[target] = out


def publish(files):
    snapshot_root = WORK / 'Before'
    snapshot_root.mkdir(exist_ok=True)
    before = Path(tempfile.mkdtemp(prefix='Publish-', dir=snapshot_root))
    snapshots = {}
    for target, out in files.items():
        assert target.resolve().is_relative_to(PROJECT) and out.is_file() and out.stat().st_size > 0, target
        original = before / target.relative_to(PROJECT)
        if target.is_file():
            original.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(target, original)
            assert digest(target) == digest(original), target
            snapshots[target] = original
    for path in (MODELS, PREFABS):
        path.mkdir(parents=True, exist_ok=True)
        meta(path, folder=True)
    applied = []
    try:
        for target, source in files.items():
            target.parent.mkdir(parents=True, exist_ok=True)
            pending = WORK / 'Install' / target.relative_to(PROJECT)
            pending.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, pending)
            os.replace(pending, target)
            applied.append(target)
            assert digest(target) == digest(source), target
    except BaseException:
        for target in reversed(applied):
            if target in snapshots:
                shutil.copy2(snapshots[target], target)
            else:
                target.unlink()
        raise


def main():
    assets = {}
    prefabs = build_prefabs(assets)
    bind_growth(assets, prefabs)
    stage_item_rows(assets)
    files = {}
    for target, source in assets.items():
        sidecar = Path(str(source) + '.meta')
        live_meta = Path(str(target) + '.meta')
        if sidecar.is_file() and not live_meta.is_file():
            files[live_meta] = sidecar
        files[target] = source
    blend = f'NP_{SPECIES}.blend'
    files[SOURCE / 'Blender' / blend] = WORK / blend
    for suffix in ('Growth', 'Close', 'Scale', 'Meadow', 'Map', 'Canopy_Comparison'):
        name = f'{PREFIX}{suffix}.png'
        preview = WORK / 'Previews' / name
        if preview.is_file():
            files[SOURCE / 'Previews' / name] = preview
    for suffix in ('BaseColor', 'Normal', 'SurfaceMask'):
        name = f'{ATLAS}_{suffix}'
        for extension, directory in (('png', TEXTURES), ('xcf', SOURCE / 'Textures')):
            path = WORK / 'Atlas' / f'{name}.{extension}'
            if path.is_file():
                files[directory / path.name] = path
    if '--publish' in sys.argv:
        publish(files)
        print(f'{SPECIES.upper()}_UNITY_PUBLISH_PASS {len(files)} files.', flush=True)


if __name__ == '__main__':
    main()
