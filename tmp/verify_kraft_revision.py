import json
from pathlib import Path
import re

from PIL import Image, ImageChops

from stage_kraft_prints import assets, art, digest, root, selected


def guid(path):
    return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]


before = json.loads((root / 'tmp/kraft_before.json').read_text())
expected = {f'Assets/Nature  Paradaise/Textures/UI/Icons/Seeds/icon_seed_{key}.png' for key in selected}
actual = {name for name, checksum in before.items() if digest(root / name) != checksum}
assert actual == expected, actual ^ expected
stage = root / 'tmp/kraft_stage'
manifest = json.loads((art / 'Manifest.json').read_text(encoding='utf8'))
catalog_path = assets / 'Resources/Shops/Crops Catalog.asset'
catalog = catalog_path.read_text().split('  - title: Benih\n', 1)[1].split('  - title:', 1)[0]
shop_items = set(re.findall(r'guid: (\w+)', catalog))
item_paths = {guid(path): path for path in (assets / 'Resources/Items').rglob('*.asset')}
bindings = set()
for item in shop_items:
    data = item_paths[item].read_text(encoding='utf-8-sig')
    icon = re.search(r'^  icon: \{fileID: 21300000, guid: (\w+), type: 3\}', data, re.M)
    if icon:
        bindings.add(icon[1])

for record in manifest['icons']:
    sprite = root / record['seed']
    assert digest(sprite) == record['seedSha256'], sprite
    assert guid(sprite) in bindings, sprite
    with Image.open(sprite) as im:
        assert im.mode == 'RGBA' and im.size == (256, 256), sprite
        assert im.getchannel('A').getextrema() == (0, 255), sprite
    if record['plant'] in selected:
        assert digest(sprite) == digest(stage / sprite.name), sprite

preview_name = 'SeedShopKraft_Preview.png'
with Image.open(art / preview_name) as preview, Image.open(stage / preview_name) as reference:
    assert ImageChops.difference(preview, reference).getbbox(alpha_only=False) is None

print(f'PASS: {len(actual)} updated sprites, {len(bindings)} shop bindings, preview matches staged build')
