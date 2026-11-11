from pathlib import Path
import hashlib
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


first = ('watermelon', 'melon', 'zucchini', 'cucumber', 'pea', 'kiwi',
         'green_bean', 'chili_pepper', 'eggplant')
second = ('cabbage', 'broccoli', 'cauliflower', 'blueberry', 'blackberry', 'raspberry')
selected = first + second
root = Path.cwd()
art = root / 'ArtSource/UIIcons/Seeds'
assets = root / 'Assets/Nature  Paradaise'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot():
    icons = assets / 'Textures/UI/Icons'
    paths = (list((icons / 'Seeds').glob('*')) + list((icons / 'Crops').glob('*'))
             + list((assets / 'Resources/Items/Farming/Seeds').glob('*'))
             + [assets / 'Resources/Shops/Crops Catalog.asset'])
    hashes = {p.relative_to(root).as_posix(): digest(p) for p in paths if p.is_file()}
    (root / 'tmp/kraft_before.json').write_text(json.dumps(hashes))


def extract(cell):
    # Use the main opaque silhouette to remove isolated transparent-background speckles.
    alpha = cell.getchannel('A')
    foreground = alpha.point(lambda a: 255 if a >= 96 else 0)
    ys, xs = np.nonzero(np.asarray(foreground))
    center_x, center_y = cell.width / 2, cell.height / 2
    nearest = np.argmin((xs - center_x) ** 2 + (ys - center_y) ** 2)
    ImageDraw.floodfill(foreground, (int(xs[nearest]), int(ys[nearest])), 127)
    silhouette = foreground.point(lambda a: 255 if a == 127 else 0)
    assert np.count_nonzero(np.asarray(silhouette)) > 0.88 * len(xs)
    support = np.asarray(silhouette.filter(ImageFilter.MaxFilter(5))) != 0
    normalized = np.clip((np.asarray(alpha, dtype=float) - 18) * (255 / 230), 0, 255)
    cell.putalpha(Image.fromarray(np.where(support, normalized, 0).astype('uint8')))
    result = cell.crop(cell.getchannel('A').getbbox())
    result.thumbnail((384, 384), Image.Resampling.LANCZOS)
    return result


def save(key, cell):
    print_image = extract(cell)
    print_image.save(art / f'Generated/KraftSeedPrint_{key}.png', optimize=True)
    print(key, print_image.size)


if sys.argv[1] == 'snapshot':
    snapshot()
elif sys.argv[1] == 'extract':
    for index, keys in enumerate((first, second), start=1):
        with Image.open(root / f'tmp/kraft_print_sheet_0{index}.png') as sheet:
            assert sheet.size == (1254, 1254) and sheet.mode == 'RGBA'
            for n, key in enumerate(keys):
                x, y = n % 3 * 418, n // 3 * 418
                save(key, sheet.crop((x, y, x + 418, y + 418)))
elif sys.argv[1] == 'melon':
    with Image.open(root / 'tmp/kraft_melon.png') as melon:
        save('melon', melon)
