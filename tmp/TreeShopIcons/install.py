import re
import shutil
import struct
import uuid
from pathlib import Path


WORK = Path(__file__).resolve().parent
PROJECT = WORK.parents[1]
ASSETS = PROJECT / 'Assets/Nature  Paradaise'
RESOURCE = ASSETS / 'Resources'
ART = PROJECT / 'ArtSource/FruitTrees'
ICONS = ASSETS / 'Textures/UI/Icons'
TARGET = ICONS / 'Trees'
ORDER = ('apple', 'avocado', 'cherry', 'guava', 'mango', 'orange', 'coconut')
OLD = {'avocado', 'cherry', 'guava', 'mango'}
MUTATIONS = ('apple', 'mango', 'orange', 'coconut')
KINDS = ('seed', 'fruit', 'mutation')
TEMPLATE = ICONS / 'Crops/icon_crop_blueberry.png.meta'


def sidecar(path):
    return Path(str(path) + '.meta')


def guid(meta):
    return re.search(r'(?m)^guid: (\w+)$', meta)[1]


def new_meta(path):
    return re.sub(r'(?m)^guid: \w+$', f'guid: {uuid.uuid5(uuid.NAMESPACE_URL, path.relative_to(PROJECT).as_posix()).hex}',
                  path.read_text())


def item_asset(kind, key):
    name = key.title()
    base = RESOURCE / 'Trees'
    if kind == 'mutation':
        return RESOURCE / f'Items/Crops/Mutations/{name} Fruit Mutation.asset'
    role = 'Seedling' if kind == 'seed' else 'Fruit'
    if key in OLD and key != 'mango':
        base = RESOURCE / ('Items/Farming/Seeds' if kind == 'seed' else 'Items/Food')
    return base / f'{name} {role}.asset'


def filename(kind, key):
    return f'icon_tree_fruit_{key}_mutation.png' if kind == 'mutation' else f'icon_tree_{kind}_{key}.png'


def previous(kind, key):
    folder, prefix = ('Seeds', 'seed') if kind == 'seed' else ('Crops', 'crop')
    return ICONS / folder / f'icon_{prefix}_{key}.png'


def seeded(template, name):
    return re.sub(r'(?m)^guid: \w+$',
                  f'guid: {uuid.uuid5(uuid.NAMESPACE_URL, name.relative_to(PROJECT).as_posix()).hex}',
                  template.read_text())


def check_png(path):
    header = path.read_bytes()[:26]
    assert header[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>2I', header[16:24]) == (256,256)
    assert header[25] == 6, path


def main():
    assets, updates, moves = {}, {}, []
    for key in ORDER:
        for kind in KINDS:
            if kind == 'mutation' and key not in MUTATIONS:
                continue
            path = TARGET / filename(kind, key)
            source = WORK / path.name
            check_png(source)
            old = previous(kind, key)
            item = item_asset(kind, key)
            content = item.read_text()
            if sidecar(path).is_file():
                meta = sidecar(path).read_text()
            elif key in OLD and kind != 'mutation':
                assert old.is_file() and sidecar(old).is_file()
                meta = sidecar(old).read_text()
                assert guid(meta) in content
                moves.extend((old, sidecar(old)))
            else:
                meta = seeded(TEMPLATE, path)
            ref = f'  icon: {{fileID: 21300000, guid: {guid(meta)}, type: 3}}'
            updated, count = re.subn(r'(?m)^  icon: .+$', ref, content)
            assert count <= 1
            updates[item] = updated if count else content.rstrip() + '\n' + ref + '\n'
            assets[path] = (source,meta)

    shop = (RESOURCE / 'Shops/Crops Catalog.asset').read_text()
    seeds = shop.split('  - title: Benih\n')[1].split('  - title: ')[0]
    trees = shop.split('  - title: Bibit Pohon\n')[1].split('  - title: ')[0]
    catalog = (RESOURCE / 'Catalogs/FarmEquipmentCatalog.asset').read_text().split('  treeSeeds:\n')[1]
    expected = re.findall(r'guid: (\w+)', trees)
    assert len(expected) == 7 and len(set(expected)) == 7
    for key in ORDER:
        seed_guid = guid(sidecar(item_asset('seed',key)).read_text())
        assert seed_guid in expected and seed_guid not in seeds and seed_guid in catalog

    for key in ORDER:
        assert (WORK / f'Tree_{key.title()}.xcf').read_bytes().startswith(b'gimp xcf ')
    assert len(assets) == 18 and len({guid(meta) for _,meta in assets.values()}) == 18
    TARGET.mkdir(exist_ok=True)
    if not sidecar(TARGET).is_file():
        sidecar(TARGET).write_text(seeded(sidecar(ICONS/'Crops'), TARGET))
    for path,(source,meta) in assets.items():
        shutil.copy2(source,path)
        sidecar(path).write_text(meta)
        assert guid(sidecar(path).read_text()) == guid(meta)
        check_png(path)
    for path,content in updates.items():
        if path.read_text() != content:
            path.write_text(content)
    backup = WORK/'Previous'
    backup.mkdir(exist_ok=True)
    for path in moves:
        shutil.copy2(path,backup/path.name)
        path.unlink()
    master_dir = ART/'Icons'
    master_dir.mkdir(exist_ok=True)
    for key in ORDER:
        name = f'Tree_{key.title()}.xcf'
        shutil.copy2(WORK/name,master_dir/name)
    shutil.copy2(WORK/'NP_Tree_Shop_Icons.png', ART/'Previews/NP_Tree_Shop_Icons.png')
    for key in ORDER:
        for kind in KINDS:
            path = TARGET / filename(kind, key)
            if path.is_file():
                assert guid(sidecar(path).read_text()) in item_asset(kind,key).read_text()
    print(f'TREE_ICONS_PASS {len(assets)} transparent sprites, seven editable GIMP masters and seven exclusive shop listings.')


main()
