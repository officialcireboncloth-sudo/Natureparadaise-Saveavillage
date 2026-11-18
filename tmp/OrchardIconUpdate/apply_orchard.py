import csv
import io
import os
import re
import shutil
import struct
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

work = Path(__file__).resolve().parent
project = work.parents[1]
sys.path.insert(0, str(project / 'ArtSource/FruitTrees/Scripts'))
os.environ['NP_FRUIT_TREE_SPECIES'] = 'Banana'
os.environ['NP_BANANA_STAGE'] = str(work)
from publish_avocado_unity import ASSETS, SOURCE, asset_reference, digest, field, meta, publish, staged_path


SPECIES = ('Apple', 'Avocado', 'Banana', 'Cherry', 'Coconut', 'Durian', 'Guava', 'Mango', 'Orange', 'Pear', 'Rambutan')
SEASONS = {'Spring': 1, 'Summer': 2, 'Fall': 4, 'Winter': 8}
TREES = ASSETS / 'Resources/Trees'
MUTATIONS = ASSETS / 'Resources/Items/Crops/Mutations'
ICONS = ASSETS / 'Textures/UI/Icons/Trees'
CSV_PATH = ASSETS / 'Data/Balance/Items.csv'
YIELD = struct.pack('<5i', 5, 7, 10, 15, 20).hex()
EMPTY = '{fileID: 0}'


def attribute(text, key):
    match = re.search(rf'(?m)^  {re.escape(key)}: (.*)$', text)
    return match[1] if match else None


def shared_rules():
    sheet = work.parent / 'FruitTreeRulesAudit/Nature_Paradise_13_Fruit_Trees.xlsx'
    ns = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
    with zipfile.ZipFile(sheet) as workbook:
        xml = ET.fromstring(workbook.read('xl/worksheets/sheet1.xml'))
        result = {}
        for row in xml.findall('.//s:row', ns):
            index = int(row.get('r'))
            if not 5 <= index <= 17:
                continue
            values = {}
            for cell in row.findall('s:c', ns):
                key = cell.get('r').removesuffix(str(index))
                values[key] = cell.findtext('s:v', namespaces=ns) or ''.join(
                    node.text or '' for node in cell.findall('.//s:t', ns))
            result[values['B']] = (SEASONS[values['C']], int(values['E']), int(values['F']),
                                    int(values['H']), tuple(int(values[col]) for col in 'JKLMN'))
        return result


def png_check(path):
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', data[16:24]) == (256, 256), path
    assert data[25] == 6, path


def stage_text(files, target, source):
    staged = staged_path(target)
    crlf = not target.exists() or b'\r\n' in target.read_bytes()
    staged.write_bytes((source.replace('\n', '\r\n') if crlf else source).encode('utf-8'))
    files[target] = staged
    return staged


def track_meta(files, target, staged, template=None, main_id=None):
    guid = meta(staged, template, main_id=main_id)
    path = Path(str(target) + '.meta')
    if not path.exists():
        files[path] = Path(str(staged) + '.meta')
    return guid


def icon(files, name):
    target = ICONS / name
    source = work / 'Icons' / name
    png_check(source)
    staged = staged_path(target)
    shutil.copyfile(source, staged)
    files[target] = staged
    return asset_reference(track_meta(files, target, staged, ICONS / 'icon_tree_fruit_apple.png'), 21300000, 3)


def sprite_fields(text, sprite):
    return field(field(text, 'icon', sprite), 'inventoryIllustration', sprite)


def stage_csv(files, replacements, new_rows):
    source = CSV_PATH.read_bytes()
    decoded = source.decode('utf-8-sig')
    newline = '\r\n' if '\r\n' in decoded else '\n'
    lines = decoded.splitlines(keepends=True)
    records = csv.reader(lines)
    header = next(records)
    fields = {name: i for i, name in enumerate(header)}
    output = list(lines[:records.line_num])
    start = records.line_num

    def serialize(row):
        stream = io.StringIO(newline='')
        csv.writer(stream, lineterminator=newline).writerow(row)
        return stream.getvalue()

    for row in records:
        replacement = replacements.get(row[0]) if row else None
        updated = row.copy()
        for key, value in (replacement or {}).items():
            updated[fields[key]] = value
        output.extend(lines[start:records.line_num] if updated == row else [serialize(updated)])
        start = records.line_num
    if new_rows and output and not output[-1].endswith('\n'):
        output.append(newline)
    for template, values in new_rows:
        row = template.copy()
        for key, value in values.items():
            row[fields[key]] = value
        output.append(serialize(row))
    staged = staged_path(CSV_PATH)
    prefix = b'\xef\xbb\xbf' if source.startswith(b'\xef\xbb\xbf') else b''
    staged.write_bytes(prefix + ''.join(output).encode('utf-8'))
    files[CSV_PATH] = staged


def prepare():
    balance = shared_rules()
    with CSV_PATH.open(newline='', encoding='utf-8-sig') as stream:
        reader = csv.reader(stream)
        header = next(reader)
        records = {row[0]: row for row in reader if row}
    col_path = header.index('asset_path')
    shop = (ASSETS / 'Resources/Shops/Crops Catalog.asset').read_text(encoding='utf-8')
    tree_shop = shop.split('  - title: Bibit Pohon\n')[1].split('  - title: ')[0]
    equipment = (ASSETS / 'Resources/Catalogs/FarmEquipmentCatalog.asset').read_text().split('  treeSeeds:\n')[1]
    files, replacements, new_rows = {}, {}, []
    for species in SPECIES:
        key = species.lower()
        seed_id = f'item.{key}_seedling'
        fruit_id = f'item.{key}_fruit'
        mutation_id = f'{fruit_id}_mutation'
        seed_path = project / records[seed_id][col_path]
        fruit_path = project / records[fruit_id][col_path]
        mutation_path = MUTATIONS / f'{species} Fruit Mutation.asset'
        definition = TREES / f'{species}.asset'
        seed, fruit = (path.read_text(encoding='utf-8') for path in (seed_path, fruit_path))
        seed_guid = meta(seed_path)
        assert tree_shop.count(seed_guid) == 1 and equipment.count(seed_guid) == 1, species
        assert meta(definition) in attribute(seed, 'treeDefinition'), species
        seed = sprite_fields(seed, icon(files, f'icon_tree_seed_{key}.png'))
        fruit = sprite_fields(fruit, icon(files, f'icon_tree_fruit_{key}.png'))
        variant_sprite = icon(files, f'icon_tree_fruit_{key}_mutation.png')

        if mutation_path.exists():
            mutation = mutation_path.read_text(encoding='utf-8')
        else:
            mutation = (MUTATIONS / 'Apple Fruit Mutation.asset').read_text(encoding='utf-8')
            mutation = mutation.replace('Apple', species).replace('apple', key)
        data = definition.read_text(encoding='utf-8')
        fruit_ref = asset_reference(meta(fruit_path), 11400000, 2)
        mutation = sprite_fields(mutation, variant_sprite)
        for name, value in (('mutationSourceItem', fruit_ref), ('isMutatedCrop', '1')):
            mutation = field(mutation, name, value)
        if (prefab := attribute(fruit, 'worldPrefab')) and prefab != EMPTY:
            mutation = field(mutation, 'worldPrefab', prefab)
            mutation = field(mutation, 'worldScale', attribute(fruit, 'worldScale'))

        if species in balance:
            season, buy, maturity, regrow, prices = balance[species]
            flowering = int(attribute(data, 'floweringDays'))
            assert regrow > flowering
            data = field(data, 'fruitSeasons', str(season))
            data = field(data, 'matureDays', str(maturity))
            data = field(data, 'fruitGrowthDays', str(regrow - flowering))
            seed = field(seed, 'buyPrice', str(buy))
            fruit = field(fruit, 'sellPrice', str(prices[0]))
            fruit = field(fruit, 'sellPricesByStars', struct.pack('<5i', *prices).hex())
            mutation = field(mutation, 'sellPrice', str(prices[0]))
            replacements[seed_id] = {'buy_price': str(buy)}
            replacements[fruit_id] = {'sell_price': str(prices[0]), 'sell_prices_by_stars': '|'.join(map(str, prices))}

        staged_mutation = stage_text(files, mutation_path, mutation)
        mutation_guid = track_meta(files, mutation_path, staged_mutation, main_id=11400000)
        for name, value in (('fruitItem', fruit_ref), ('useFruitCare', '1'), ('fruitAmount', '5'),
                            ('fruitAmountsByStars', YIELD), ('fruitMutationChance', '0.01'),
                            ('mutationFruitItem', asset_reference(mutation_guid, 11400000, 2))):
            data = field(data, name, value)
        assert len(re.findall(r'(?m)^    model: ', data)) == 5, species
        if species != 'Coconut':
            phases = ('Seed', 'Sprout', 'Young', 'FruitForming', 'HarvestReady')
            visual_folder = ASSETS / f'Prefabs/Trees/{species}'
            actual = re.findall(r'(?m)^    model: .*?guid: (\w+)', data)
            assert actual == [meta(visual_folder / f'NP_{species}_{phase}.prefab') for phase in phases], species

        mutation_row = {'sell_price': attribute(mutation, 'sellPrice'), 'is_mutated_crop': 'TRUE',
                        'mutation_source_item_id': fruit_id}
        if mutation_id not in records:
            mutation_row.update(item_id=mutation_id, asset_path=mutation_path.relative_to(project).as_posix(),
                                display_name=f'{species} Mutasi')
            new_rows.append((records['item.apple_fruit_mutation'], mutation_row))
        else:
            replacements[mutation_id] = mutation_row
        for path, yaml in ((seed_path, seed), (fruit_path, fruit), (definition, data)):
            stage_text(files, path, yaml)
        files[SOURCE / 'Icons' / f'Tree_{species}.xcf'] = work / 'Icons' / f'Tree_{species}.xcf'
        print(f'{species}: season {attribute(data, "fruitSeasons")}, seed {attribute(seed, "buyPrice")}, '
              f'cycle {int(attribute(data, "floweringDays")) + int(attribute(data, "fruitGrowthDays"))} days', flush=True)

    stage_csv(files, replacements, new_rows)
    files[SOURCE / 'Previews/NP_Tree_Shop_Icons.png'] = work / 'NP_Tree_Shop_Icons.png'
    return {target: staged for target, staged in files.items()
            if not target.exists() or digest(target) != digest(staged)}


if __name__ == '__main__':
    updates = prepare()
    if '--publish' in sys.argv:
        publish(updates)
    print(f'ORCHARD_ITEMS_PASS {len(SPECIES)} registered fruit trees, {len(updates)} files', flush=True)
