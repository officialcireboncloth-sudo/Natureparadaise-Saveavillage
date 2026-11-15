import json
from pathlib import Path


SOURCE = Path(__file__).resolve().parents[1]
PROJECT = SOURCE.parents[1]
ASSETS = PROJECT / 'Assets/Nature  Paradaise'
SCRATCH = PROJECT / 'tmp/FruitTreeApple'
REGISTRY = json.loads((SOURCE / 'AtlasRegistry.json').read_text(encoding='utf-8'))
ATLAS = REGISTRY['name']
SIZE = REGISTRY['size']
TILE = REGISTRY['tileSize']
PADDING = REGISTRY['padding']
TEXTURES = ASSETS / 'Textures/Shared/FruitTrees'
# Coordinates in the artist's 256-unit tile, measured from its top left.
SPRAY_RECT = (4, 12, 216, 248)
SPRAY_ROOT = (110, 242)
SINGLE_LEAF_RECT = (219, 29, 255, 107)
CUTOUT_SPECIES = ('Apple', 'Avocado', 'Cherry', 'Guava', 'Mango')
GUAVA_PEEL_WIDTH = .53
GUAVA_SECTION_CENTER = (201, 119)
GUAVA_SECTION_RADIUS = 47
MANGO_PEEL_WIDTH = .55
MANGO_SECTION_CENTER = (200, 83)
MANGO_SECTION_RADIUS = 45
MANGO_SEED_CENTER = (200, 200)
MANGO_SEED_RADII = (44, 27)

SLOTS = dict(REGISTRY['commonSlots'])
for species, start in REGISTRY['speciesSlots'].items():
    for offset, surface in enumerate(REGISTRY['slotOffsets']):
        SLOTS[f'{species}/{surface}'] = start + offset
assert len(set(SLOTS.values())) == len(SLOTS)
assert max(SLOTS.values()) < (SIZE // TILE) ** 2


def tile_origin(slot):
    row, column = divmod(SLOTS[slot], SIZE // TILE)
    return column * TILE, row * TILE


def atlas_uv(slot, u, v):
    x, y = tile_origin(slot)
    inner = TILE - 2 * PADDING
    return (x + PADDING + inner * u) / SIZE, 1 - (y + PADDING + inner * v) / SIZE
