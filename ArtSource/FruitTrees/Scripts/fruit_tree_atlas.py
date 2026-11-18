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
OIL_PALM_ATLAS = 'NP_OilPalm_01'
OIL_PALM_SIZE = 1024
OIL_PALM_SLOTS = {'OilPalm/Leaf': 0, 'OilPalm/Bark': 1, 'OilPalm/FruitForming': 2, 'OilPalm/FruitReady': 3}
OIL_PALM_BAKED_ATLAS = 'NP_OilPalm_Fronds_01'
OIL_PALM_BAKED_SIZE = 2048
OIL_PALM_CHART_PADDING = 8
OIL_PALM_FROND_CHARTS = {
    **{f'OilPalm/Frond{i}': (i % 8 * 256, 1024 + i // 8 * 384, 256, 384) for i in range(16)},
    **{f'OilPalm/Frond{i}': (1024 + (i - 16) * 256, 0, 256, 384) for i in range(16, 20)},
    **{f'OilPalm/Sprout{i}': (1024 + i % 8 * 128, 384 + i // 8 * 256, 128, 256) for i in range(9)},
}
# Coordinates in the artist's 256-unit tile, measured from its top left.
SPRAY_RECT = (4, 12, 216, 248)
SPRAY_ROOT = (110, 242)
SINGLE_LEAF_RECT = (219, 29, 255, 107)
CUTOUT_SPECIES = ('Apple', 'Avocado', 'Banana', 'Cherry', 'Guava', 'Mango', 'Orange', 'Pear', 'Rambutan')
BANANA_GREEN_PEEL_WIDTH = .59
BANANA_BLOSSOM_UV = (.66, .97)
PEAR_PEEL_WIDTH = .51
PEAR_BARK_UV = (.57, .98)
PEAR_SECTION_CENTER = (195, 102)
PEAR_SECTION_SCALE = (48, 52)
PEAR_PROFILE = ((-.99, .13), (-.94, .44), (-.78, .76), (-.54, .94), (-.26, 1),
                (.04, .91), (.34, .71), (.65, .51), (.97, .36), (1.14, .26), (1.19, .10))
DURIAN_PEEL_WIDTH = .50
DURIAN_BARK_UV = (.55, .99)
DURIAN_SECTION_CENTER = (194, 77)
DURIAN_SECTION_RADIUS = 46
DURIAN_PULP_RECT = (149, 134, 241, 186)
DURIAN_SEED_RECT = (147, 198, 243, 247)
RAMBUTAN_PEEL_WIDTH = .52
RAMBUTAN_SECTION_CENTER = (195, 83)
RAMBUTAN_SECTION_RADIUS = 47
RAMBUTAN_SEED_RECT = (163, 143, 234, 194)
ORANGE_PEEL_WIDTH = .52
ORANGE_SECTION_CENTER = (194, 86)
ORANGE_SECTION_RADIUS = 48
ORANGE_SEED_RECT = (169, 163, 235, 238)
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
    slots, size = (OIL_PALM_SLOTS, OIL_PALM_SIZE) if slot in OIL_PALM_SLOTS else (SLOTS, SIZE)
    row, column = divmod(slots[slot], size // TILE)
    return column * TILE, row * TILE


def atlas_uv(slot, u, v, atlas_name=None):
    if slot in OIL_PALM_FROND_CHARTS:
        x, y, width, height = OIL_PALM_FROND_CHARTS[slot]
        edge = OIL_PALM_CHART_PADDING
        size = OIL_PALM_BAKED_SIZE
        return ((x + edge + (width - 2*edge)*u)/size, 1 - (y + edge + (height - 2*edge)*v)/size)
    x, y = tile_origin(slot)
    inner = TILE - 2 * PADDING
    size = OIL_PALM_SIZE if slot in OIL_PALM_SLOTS else SIZE
    if atlas_name == OIL_PALM_BAKED_ATLAS:
        size = OIL_PALM_BAKED_SIZE
    return (x + PADDING + inner * u) / size, 1 - (y + PADDING + inner * v) / size
