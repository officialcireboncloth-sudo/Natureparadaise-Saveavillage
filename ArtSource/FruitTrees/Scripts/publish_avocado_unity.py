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
from fruit_tree_atlas import (
    ASSETS, ATLAS as SHARED_ATLAS, OIL_PALM_ATLAS, OIL_PALM_BAKED_ATLAS, OIL_PALM_BAKED_SIZE,
    OIL_PALM_CHART_PADDING, OIL_PALM_FROND_CHARTS, OIL_PALM_SLOTS,
    PADDING, PROJECT, SIZE, SLOTS, SOURCE, TEXTURES, TILE, tile_origin,
)
from fruit_tree_dimensions import BANANA_STARTER_SCALES, OIL_PALM_STARTER_SCALES, ORCHARD_HEIGHT
from publish_apple_unity import CUTOUT_MATERIAL, TREE_DATA, blocks, meta, prefab, ref, save_cutout_material, serialize_mesh


SPECIES = os.environ.get('NP_FRUIT_TREE_SPECIES', 'Avocado')
SEASONS = {'Avocado': 1, 'Banana': 2, 'Cherry': 1, 'Guava': 1, 'Mango': 2, 'Orange': 1,
           'Rambutan': 2, 'Durian': 2, 'Pear': 4, 'OilPalm': 2}
assert SPECIES in SEASONS
ATLAS = SHARED_ATLAS
if SPECIES == 'OilPalm':
    ATLAS, SIZE = OIL_PALM_BAKED_ATLAS, OIL_PALM_BAKED_SIZE
WORK = Path(os.environ[f'NP_{SPECIES.upper()}_STAGE']).resolve()
STAGED = WORK / 'Publish'
MODELS = ASSETS / f'mesh/Trees/{SPECIES}'
PREFABS = ASSETS / f'Prefabs/Trees/{SPECIES}'
ITEMS = ASSETS / 'Resources/Items'
# Existing catalog items keep their paths and GUIDs.
SEED_ITEM = (TREE_DATA if SPECIES in ('Mango', 'Orange') else ITEMS / 'Farming/Seeds') / f'{SPECIES} Seedling.asset'
FRUIT_ITEM = (TREE_DATA if SPECIES in ('Mango', 'Orange') else ITEMS / 'Food') / f'{SPECIES} Fruit.asset'
MUTATION_ITEM = ITEMS / 'Crops/Mutations' / f'{SPECIES} Fruit Mutation.asset'
ICON_ROOT = ASSETS / 'Textures/UI/Icons'
DEFINITION = TREE_DATA / f'{SPECIES}.asset'
PREFIX = f'NP_{SPECIES}_'
NAMES = ('Seed', 'Sprout', 'Young', 'FruitForming', 'HarvestReady', 'Stump', 'Produce')
LODS = ('Young', 'FruitForming', 'HarvestReady')
# Small growth stages export at editable size. Restore their landscape scale here.
SCALES = {'Banana': BANANA_STARTER_SCALES, 'OilPalm': OIL_PALM_STARTER_SCALES}.get(
    SPECIES, {'Seed': .10, 'Sprout': .26},
)
SHOP_BALANCE = {
    'Avocado': (500, (65, 78, 98, 117, 143), 3),
    'Banana': (100, (35, 42, 53, 63, 77), 3),
    'Cherry': (450, (65, 78, 98, 117, 143), 1),
    'Durian': (1000, (180, 216, 270, 324, 396), 4),
    'Guava': (250, (30, 36, 45, 54, 66), 1),
    'Mango': (450, (60, 72, 90, 108, 132), 2),
    'Orange': (300, (40, 48, 60, 72, 88), 2),
    'OilPalm': (350, (35, 42, 53, 63, 77), 3),
    'Pear': (500, (65, 78, 98, 117, 143), 2),
    'Rambutan': (350, (35, 42, 53, 63, 77), 2),
}.get(SPECIES)


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
    oil_palm_rects = [(x+edge-.001,y+edge-.001,x+w-edge+.001,y+h-edge+.001)
                      for x,y,w,h,edge in (
                          [(*tile_origin(slot),TILE,TILE,PADDING) for slot in OIL_PALM_SLOTS]
                          + [(*rect,OIL_PALM_CHART_PADDING) for rect in OIL_PALM_FROND_CHARTS.values()])]
    for name, spec in specs.items():
        assert spec['name'] == PREFIX + name and spec['materials'] == [ATLAS], name
        indices, = spec['submeshes']
        assert len(indices) == 3 * spec['triangles'] and 0 <= min(indices) <= max(indices) < len(spec['vertices']), name
        assert all(math.isfinite(value) for key in ('vertices', 'normals', 'uv') for row in spec[key] for value in row), name
        for u, v in spec['uv']:
            if SPECIES == 'OilPalm':
                x, y = u*SIZE, (1-v)*SIZE
                assert any(left <= x <= right and top <= y <= bottom
                           for left,top,right,bottom in oil_palm_rects), name
                continue
            column, x = divmod(u * SIZE, TILE)
            row, y = divmod((1 - v) * SIZE, TILE)
            assert int(row) * (SIZE // TILE) + int(column) in SLOTS.values(), name
            assert PADDING - .001 < x < TILE - PADDING + .001 and PADDING - .001 < y < TILE - PADDING + .001, name
        if name.startswith(('HarvestReady', 'FruitForming')):
            assert abs(max(point[1] for point in spec['vertices']) - ORCHARD_HEIGHT) < .025, name
    seed_height, sprout_height = (max(point[1] for point in specs[name]['vertices']) * SCALES[name]
                                  for name in ('Seed', 'Sprout'))
    if SPECIES == 'Banana':
        assert .24 < seed_height < .48 and .50 < sprout_height < .87
    elif SPECIES == 'OilPalm':
        assert math.isclose(seed_height, SCALES['Seed'], abs_tol=.001)
        assert math.isclose(sprout_height, SCALES['Sprout'], abs_tol=.001)
    else:
        assert .025 < seed_height < .10 and .20 < sprout_height < .42
    print(f'{SPECIES} height from soil {ORCHARD_HEIGHT:.4f} m. Triangles by mesh:',
          {name: spec['triangles'] for name, spec in specs.items()}, flush=True)


def orchard_material(files):
    if SPECIES == 'OilPalm':
        target = CUTOUT_MATERIAL.with_name(f'{OIL_PALM_ATLAS}.mat')
        template = CUTOUT_MATERIAL.read_text().replace(CUTOUT_MATERIAL.stem, target.stem)
        for suffix in ('BaseColor', 'Normal', 'SurfaceMask'):
            original = TEXTURES / f'{SHARED_ATLAS}_{suffix}.png'
            texture = TEXTURES / f'{ATLAS}_{suffix}.png'
            staged = staged_path(texture)
            shutil.copyfile(WORK / 'BakedAtlas' / texture.name, staged)
            template = template.replace(meta(original), meta(staged, original))
            if suffix == 'BaseColor':
                importer = Path(str(staged) + '.meta')
                text = importer.read_text().replace('mipMapsPreserveCoverage: 0', 'mipMapsPreserveCoverage: 1')
                text = text.replace('alphaTestReferenceValue: 0.5', 'alphaTestReferenceValue: 0.42')
                importer.write_text(text)
            files[texture] = staged
        output = staged_path(target)
        output.write_text(template)
        files[target] = output
        return meta(output, main_id=2100000)
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
            scale = .10 if SPECIES == 'OilPalm' else SCALES['Seed']
            text, count = re.subn(r'(?ms)(^--- !u!4 &410\n.*?^  m_LocalScale: )\{[^}]+\}',
                                  lambda match: match[1] + f'{{x: {scale}, y: {scale}, z: {scale}}}', out.read_text())
            assert count == 1
            out.write_text(text)
        files[target] = out
    return prefabs


def item_icon(files, kind):
    painted = ICON_ROOT / 'Trees' / f'icon_tree_{kind.lower()}_{SPECIES.lower()}.png'
    if kind == 'Mutation':
        painted = painted.with_name(f'icon_tree_fruit_{SPECIES.lower()}_mutation.png')
    generated = WORK / 'Icons' / painted.name
    if generated.is_file():
        out = staged_path(painted)
        shutil.copy2(generated, out)
        files[painted] = out
        template = painted.with_name(painted.name.replace(SPECIES.lower(), 'orange'))
        return meta(out, template)
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


def bind_mutation_item(files, fruit_guid, produce_guid, sell_price):
    target = MUTATION_ITEM
    template = target.with_name('Apple Fruit Mutation.asset')
    data = target.read_text() if target.is_file() else template.read_text().replace('Apple', SPECIES).replace('apple', SPECIES.lower())
    if not target.is_file():
        data = field(data, 'sellPrice', str(sell_price))
    if SPECIES == 'OilPalm':
        data = field(data, 'itemName', 'Kelapa Sawit Mutasi')
    data = field(data, 'mutationSourceItem', asset_reference(fruit_guid, 11400000, 2))
    data = field(data, 'worldPrefab', ref(produce_guid))
    data = field(data, 'worldScale', '{x: 1, y: 1, z: 1}')
    sprite = asset_reference(item_icon(files, 'Mutation'), 21300000, 3)
    data = field(field(data, 'icon', sprite), 'inventoryIllustration', sprite)
    out = staged_path(target)
    out.write_text(data)
    files[target] = out
    return meta(out, main_id=11400000)


def register_tree_seed(files, seed_guid):
    reference = asset_reference(seed_guid, 11400000, 2)
    for relative, section, indent in (
        ('Catalogs/FarmEquipmentCatalog.asset', r'^  treeSeeds:\r?\n', '  '),
        ('Shops/Crops Catalog.asset', r'^  - title: Bibit Pohon\r?\n(?:^    (?!items:)[^\r\n]*\r?\n)*^    items:\r?\n', '    '),
    ):
        target = ASSETS / 'Resources' / relative
        text = target.read_bytes().decode('utf-8')
        if reference in text:
            continue
        newline = '\r\n' if '\r\n' in text else '\n'
        pattern = section + rf'(?:^{indent}- [^\r\n]+(?:\r?\n|\Z))+'
        def append_item(match):
            items = match[0]
            return items + ('' if items.endswith('\n') else newline) + indent + '- ' + reference + newline
        updated, count = re.subn(pattern, append_item, text, flags=re.M)
        assert count == 1, target
        out = staged_path(target)
        out.write_bytes(updated.encode('utf-8'))
        files[target] = out


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
            if SHOP_BALANCE:
                seed_price, star_prices, _ = SHOP_BALANCE
                if kind == 'Seed':
                    text = field(text, 'buyPrice', str(seed_price))
                else:
                    text = field(text, 'sellPrice', str(star_prices[0]))
                    prices = ''.join(price.to_bytes(4, 'little').hex() for price in star_prices)
                    text = field(text, 'sellPricesByStars', prices)
        if kind == 'Seed':
            text = field(text, 'treeDefinition', asset_reference(definition_guid, 11400000, 2))
        if SPECIES == 'OilPalm':
            text = field(text, 'itemName', 'Bibit Kelapa Sawit' if kind == 'Seed' else 'Kelapa Sawit')
        text = field(text, 'worldScale', '{x: 1, y: 1, z: 1}')
        text = field(text, 'worldPrefab', ref(prefabs[visual]))
        sprite = asset_reference(item_icon(files, kind), 21300000, 3)
        text = field(field(text, 'icon', sprite), 'inventoryIllustration', sprite)
        out.write_text(text)
        guid = meta(out, main_id=11400000)
        if kind == 'Fruit':
            fruit_guid = guid
        elif SHOP_BALANCE:
            register_tree_seed(files, guid)
        files[target] = out

    if DEFINITION.is_file():
        data = DEFINITION.read_text()
    else:
        data = (TREE_DATA / 'Apple.asset').read_text().replace('Apple', SPECIES)
        data = field(data, 'fruitSeasons', str(SEASONS[SPECIES]))
        data = field(data, 'mutationFruitItem', '{fileID: 0}')
        data = field(data, 'fruitMutationChance', '0')
        if SHOP_BALANCE:
            data = field(data, 'fruitGrowthDays', str(SHOP_BALANCE[2]))
            data = field(data, 'fruitMutationChance', '0.01')
    if SPECIES == 'OilPalm':
        data = field(data, 'treeType', 'Kelapa Sawit')
    if SHOP_BALANCE:
        mutation_guid = bind_mutation_item(files, fruit_guid, prefabs['Produce'], SHOP_BALANCE[1][0])
        data = field(data, 'mutationFruitItem', asset_reference(mutation_guid, 11400000, 2))
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
    species_items = [('fruit', FRUIT_ITEM, SPECIES), ('seedling', SEED_ITEM, f'{SPECIES} Seedling')]
    if SHOP_BALANCE:
        species_items.append(('fruit_mutation', MUTATION_ITEM, f'{SPECIES} Mutasi'))
    if SPECIES == 'OilPalm':
        display = {'fruit': 'Kelapa Sawit', 'seedling': 'Bibit Kelapa Sawit', 'fruit_mutation': 'Kelapa Sawit Mutasi'}
        species_items = [(key, asset, display[key]) for key, asset, _ in species_items]
    for suffix, asset, display_name in species_items:
        identifier = f'item.{SPECIES.lower()}_{suffix}'
        if identifier in by_id:
            continue
        row = list(by_id['item.apple_' + suffix])
        row[columns['item_id']] = identifier
        row[columns['asset_path']] = asset.relative_to(PROJECT).as_posix()
        row[columns['display_name']] = display_name
        if SHOP_BALANCE:
            seed_price, star_prices, _ = SHOP_BALANCE
            if suffix == 'seedling':
                row[columns['buy_price']] = str(seed_price)
            else:
                row[columns['sell_price']] = str(star_prices[0])
            if suffix == 'fruit':
                row[columns['sell_prices_by_stars']] = '|'.join(map(str, star_prices))
            elif suffix == 'fruit_mutation':
                row[columns['mutation_source_item_id']] = f'item.{SPECIES.lower()}_fruit'
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
    icon_master = WORK / 'Icons' / f'Tree_{SPECIES}.xcf'
    if icon_master.is_file():
        files[SOURCE / 'Icons' / icon_master.name] = icon_master
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
