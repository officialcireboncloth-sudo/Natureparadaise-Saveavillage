"""Paint Daisy and Moonflower surfaces into unoccupied shared atlas rectangles."""
from pathlib import Path
import hashlib

import numpy as np
from PIL import Image

from g14_daisy_surfaces import SURFACE_PAINTERS, paint_daisy
from unified_plant_atlas import MAPS, NAME, REGISTRY, REVIEW, SIZE, SOURCE, TEXTURES
from unified_plant_atlas import allocate, load, rel, sha, write


PAGE = 'NP_Plants_DaisyMoonflower_Source_01'
SOURCE_DIR = SOURCE / 'G14/Textures'
SURFACES = [
    ('DaisyLeaf', 0, 0, 256, (95, 133, 48), .26),
    ('MoonflowerLeaf', 256, 0, 256, (40, 105, 45), .29),
    ('DaisyPetal', 512, 0, 192, (241, 234, 219), .33),
    ('MoonflowerPetal', 704, 0, 192, (239, 233, 233), .35),
    ('DaisyDisk', 0, 256, 128, (215, 138, 5), .27),
    ('DaisySeed', 128, 256, 128, (177, 136, 78), .23),
    ('MoonflowerSeed', 256, 256, 128, (221, 198, 151), .28),
    ('FlowerStem', 384, 256, 128, (96, 132, 43), .31),
    ('DaisyStem', 512, 256, 96, (79, 122, 38), .28),
]


def paint(name, size, base, smoothness):
    y, x = np.mgrid[0:size, 0:size] / (size - 1)
    y = 1 - y
    lateral = abs(x - .5) * 2
    noise = np.random.default_rng(197).normal(0, 1, x.shape)
    fiber = np.sin(x * 127 + np.sin(y * 9) * .8)
    if name == 'MoonflowerLeaf':
        midrib = np.exp(-(lateral / .025) ** 2) * (1 - .55 * y)
        branch = np.exp(-(np.sin(np.pi * (y * 7.7 - lateral * 1.7 + lateral**2 * .7)) / .22) ** 2)
        branch *= lateral**.45 * (1 - lateral**2) * np.sin(np.pi * y)**.5
        color = 18 * (y - .35) - 14 * lateral**2 + 32 * midrib + 22 * branch
        color += 3 * fiber + 2 * noise + 4 * np.sin(15 * x + 24 * y)
        height = .6 * midrib + .24 * branch + .04 * fiber - .24 * lateral**2
    elif name == 'MoonflowerPetal':
        vein = np.exp(-(lateral / .09) ** 2)
        throat = np.exp(-y * 5.7)
        fold = (1 - lateral**2) * np.sin(np.pi * y)
        color = 11 * fold - 11 * throat - 7 * lateral**2 + 1.8 * fiber + noise
        color += 15 * vein * np.sin(np.pi * y) - 20 * lateral**3
        base = np.asarray(base) + np.exp(-y*3.8)[..., None] * np.array([-19, -10, -64])
        height = .37 * fold + .23 * vein + .04 * fiber
    else:
        rib = np.cos(x * 6 * np.pi)
        strength = 20 if name.endswith('Seed') else 6
        color = strength * rib + 12 * (y - .5) + 2 * noise + 3 * fiber
        height = .3 * rib + .025 * fiber
    rgb = np.clip(np.asarray(base) + color[..., None], 0, 255)
    dy, dx = np.gradient(height)
    n = np.stack((-dx * 2.4, dy * 2.4, np.ones_like(dx)), axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    packed = np.zeros((size, size, 4), dtype=np.uint8)
    packed[..., 3] = round(smoothness * 255)
    alpha = np.full((size, size, 1), 255)
    return [np.concatenate((rgb, alpha), axis=-1).astype('uint8'),
            np.rint(np.concatenate(((n * .5 + .5) * 255, alpha), axis=-1)).astype('uint8'), packed]


def main():
    registry = load(REGISTRY)
    packing_path = REVIEW / 'Packing.json'
    packing = load(packing_path)
    prior = {slot['key']: slot for slot in registry['slots']}
    canvas = {suffix: Image.open(TEXTURES / f'{NAME}_{suffix}.png').convert('RGBA') for suffix in MAPS}
    source = {suffix: Image.new('RGBA', (1024, 512)) for suffix in MAPS}
    slots = []
    for name, sx, sy, size, color, smoothness in SURFACES:
        key = f'{PAGE}:{name}'
        x, y, _, _ = prior[key]['pixelRectTopLeft'] if key in prior else allocate(registry, key, size)
        pad = 12 if size >= 192 else 8
        uv_source = [(sx+pad)/1024, 1-(sy+size-pad)/512, (sx+size-pad)/1024, 1-(sy+pad)/512]
        uv_runtime = [(x+pad)/SIZE, 1-(y+size-pad)/SIZE, (x+size-pad)/SIZE, 1-(y+pad)/SIZE]
        slots.append(dict(name=name, index=len(slots), pixelRectTopLeft=[sx, sy, size, size],
                          uvSafe=uv_source, smoothness=smoothness, primaryDetail=True))
        if key not in prior:
            rect = [x, y, size, size]
            registry['slots'].append(dict(key=key, name=key, index=len(registry['slots']),
                sourceAtlas=PAGE, sourceSlot=name, sourceRect=[sx, sy, size, size], sourceSize=[1024, 512],
                sourceUvSafe=uv_source, pixelRectTopLeft=rect, contentRectTopLeft=rect, uvSafe=uv_runtime,
                normalStrength=.32, saturation=1., smoothnessScale=1., primaryDetailPreserved=True))
        painted = paint_daisy(name, size, pad) if name in SURFACE_PAINTERS else paint(name, size, color, smoothness)
        for suffix, pixels in zip(MAPS, painted):
            source[suffix].paste(Image.fromarray(pixels), (sx, sy))
            if suffix == 'SurfaceMask':
                pixels[..., 1] = round(.32 * 255)
                pixels[..., 2] = 128
            canvas[suffix].paste(Image.fromarray(pixels), (x, y))
            packing['tilePixelHashes'][key + ':' + suffix] = hashlib.sha256(pixels.tobytes()).hexdigest()
    assert all(next(s for s in registry['slots'] if s['key'] == k) == old for k, old in prior.items())
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    write(SOURCE_DIR / f'{PAGE}_Slots.json', dict(name=PAGE, colorSize=[1024, 512], normalSize=[1024, 512],
          maskSize=[1024, 512], materialSettings={'_BumpScale': .32, '_Saturation': 1., '_Smoothness': 1.}, slots=slots))
    for suffix in MAPS:
        art = SOURCE_DIR / f'{PAGE}_{suffix}.png'
        source[suffix].save(art)
        packing['sourceHashes'][rel(art)] = sha(art)
        dest = TEXTURES / f'{NAME}_{suffix}.png'
        canvas[suffix].save(dest)
        packing['maps'][suffix] = sha(dest)
    used = sum(slot['pixelRectTopLeft'][2] * slot['pixelRectTopLeft'][3] for slot in registry['slots'])
    registry.update(usedPixels=used, freePixels=SIZE*SIZE-used, freePercent=round(100*(1-used/SIZE**2), 2))
    write(REGISTRY, registry)
    packing['registrySha256'] = sha(REGISTRY)
    write(packing_path, packing)
    print('G14 atlas:', len(slots), 'surfaces,', registry['freePercent'], '% free')


if __name__ == '__main__':
    main()
