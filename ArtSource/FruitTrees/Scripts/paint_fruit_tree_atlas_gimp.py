"""Compose the shared layered XCFs in GIMP and export their Unity maps."""

import os
import sys
from pathlib import Path

import gi

gi.require_version('Gimp', '3.0')
gi.require_version('Gegl', '0.4')
from gi.repository import Gegl, Gimp, Gio

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import (
    ATLAS, CUTOUT_SPECIES, OIL_PALM_ATLAS, OIL_PALM_SIZE, OIL_PALM_SLOTS,
    PADDING, RAMBUTAN_PEEL_WIDTH, REGISTRY, SCRATCH, SIZE, SLOTS, SOURCE, TILE,
    tile_origin,
)
from fruit_tree_surface_paint import draw_surface


OIL_PALM = os.environ.get('NP_TREE_ATLAS_VARIANT') == 'OilPalm'
if OIL_PALM:
    ATLAS, SIZE, SLOTS = OIL_PALM_ATLAS, OIL_PALM_SIZE, OIL_PALM_SLOTS

# Shade, base pigment, soft light and ridge pigment in top-to-bottom tile UVs.
SPECIES = {
    'Apple': ('apple',
        ('#43582c', '#667f35', '#859e43', '#a3b65d'),
        ('#5a662c', '#7e913c', '#9aa94b', '#b6c269'),
        ('#44572b', '#728c41', '#a4b250', '#ded68e', '#bdb34f'),
        ('#862934', '#b84840', '#d76a4e', '#efb48c', '#d39a62'), .36),
    'Orange': ('orange',
        ('#294a28', '#44712d', '#64963b', '#8ab34e'),
        ('#38572a', '#5c842f', '#78a43b', '#a1bf53'),
        ('#3f641e', '#6c922d', '#9eb83a', '#c8d167'),
        ('#b34c12', '#e77516', '#fa9b26', '#ffcc58'), .39),
    'Mango': ('mango',
        ('#293e26', '#43592c', '#62793a', '#8c9954'),
        ('#724830', '#9b6239', '#ba7a47', '#d59f63'),
        ('#446427', '#749738', '#a6bd52', '#c4ce70'),
        ('#bd631d', '#e99721', '#ffc739', '#dc5829'), .39),
    'Guava': ('guava',
        ('#3e5640', '#587255', '#728d6c', '#94a67d'),
        ('#4b6347', '#66815d', '#849b74', '#a2b28b'),
        ('#425c28', '#6e8c3d', '#8da648', '#b8c46a'),
        ('#748734', '#aab94c', '#c5cd62', '#e0df8d'), .32),
    'Pear': ('pear',
        ('#253d17', '#3c5b20', '#577b2c', '#799640'),
        ('#3a4918', '#586f23', '#78962e', '#9eae45'),
        ('#536529', '#839544', '#adba63', '#c4cb7a'),
        ('#996622', '#c89a28', '#e8ba32', '#f5d661'), .35),
    'Peach': ('peach',
        ('#365240', '#5a7849', '#7e9556', '#acb876'),
        ('#51643a', '#7d914c', '#a3ad64', '#c8ca86'),
        ('#656a35', '#a6a459', '#c0b66d', '#dcc388'),
        ('#b77b44', '#e7a761', '#f0bd7f', '#d36962'), .25),
    'Cherry': ('cherry',
        ('#213d29', '#345b3b', '#4a784a', '#70935f'),
        ('#304a30', '#436841', '#5b844f', '#829e63'),
        ('#536431', '#819849', '#adc35e', '#cbd67c'),
        ('#56192a', '#a62639', '#cc414a', '#e27771'), .55),
    'Avocado': ('avocado',
        ('#1d4035', '#31624e', '#4c7f60', '#709b74'),
        ('#2b5444', '#40755b', '#5e906b', '#84a77f'),
        ('#40552b', '#789445', '#a0ae58', '#bdc77b'),
        ('#243229', '#3a5033', '#5a6d46', '#84935b'), .28),
    'Papaya': ('papaya',
        ('#2f5041', '#4f7555', '#7b9563', '#b1b879'),
        ('#405d43', '#6d8957', '#95a768', '#c1c987'),
        ('#435935', '#6c894a', '#91a95a', '#bcc47f'),
        ('#986526', '#d49039', '#edb550', '#f3ce72'), .34),
    'Banana': ('banana',
        ('#114320', '#215e20', '#39851f', '#9cbc24'),
        ('#2b5718', '#477a19', '#6da528', '#b9d336'),
        ('#456600', '#72970b', '#a5bd25', '#c4d754'),
        ('#a67a00', '#dcad04', '#f5ca12', '#ffdf30'), .39),
    'Dragon Fruit': ('dragon',
        ('#305445', '#568363', '#7a9e73', '#aabd8c'),
        ('#446343', '#6e9059', '#97ac70', '#c0c58b'),
        ('#4c623d', '#798b4e', '#a0a65d', '#c4be79'),
        ('#68263e', '#ab3356', '#c94f69', '#e98087'), .39),
    'Rambutan': ('rambutan',
        ('#244727', '#3b6734', '#598449', '#83a65a'),
        ('#365427', '#567c35', '#749b42', '#9bb45b'),
        ('#4f6321', '#788e31', '#a4b341', '#c3c767'),
        ('#640e20', '#a51a29', '#c52a34', '#ea463c'), .31),
    'Durian': ('durian',
        ('#1e3624', '#315931', '#537944', '#849b53'),
        ('#514329', '#765b32', '#99804b', '#b89c64'),
        ('#354823', '#627536', '#87954a', '#aab861'),
        ('#514e24', '#817739', '#aa9950', '#d4b869'), .28),
}

# SurfaceMask stores normal weight in G and smoothness in A. All tiles are dielectric.
COMMON = {
    'Bark': (('#443934', '#74564c', '#987463', '#b69881'), .13, .66),
    'Twig': (('#473b35', '#6f554a', '#927361', '#ad907b'), .15, .54),
    'AppleSeed': (('#4e2e26', '#794631', '#9f6546', '#bf845d'), .32, .44),
    'CutWood': (('#94643e', '#c9915a', '#e2b981', '#eac78d'), .23, .61),
    'BananaSheath': (('#3b551e', '#659423', '#a2bb31', '#be9f35'), .31, .51),
    'PapayaStem': (('#47523d', '#748263', '#a2a885', '#c2bb90'), .24, .77),
    'DragonFruitCane': (('#274c3b', '#4c8060', '#78a473', '#a6bf86'), .35, .62),
    'GreenStem': (('#384c2c', '#5b7742', '#8aa258', '#abb56b'), .30, .51),
    'RambutanBristle': (('#7d3a24', '#b76937', '#d0ad4e', '#e0c26b'), .27, .44),
    'DurianSpine': (('#61522f', '#9c8346', '#c2a65d', '#e1c981'), .26, .57),
    'DragonFruitScale': (('#3d4d2d', '#67884b', '#a1ae66', '#b64568'), .34, .54),
    'StoneSeed': (('#604331', '#9a6c47', '#ca9a65', '#d9b078'), .26, .72),
}

FRUIT_RELIEF = {
    'apple': .61, 'orange': .85, 'guava': .55, 'pear': .48, 'peach': .47, 'cherry': .26,
    'avocado': .98, 'banana': .37, 'dragon': .71, 'rambutan': .77, 'durian': 1.0,
}

# Preserve hand edits on repeat runs. Explicit refresh stages a generated revision instead.
MAPS = ('BaseColor', 'Normal', 'SurfaceMask')
RGBA_FORMAT = "R'G'B'A u8"
WORK = Path(os.environ.get('NP_TREE_ATLAS_STAGE', str(SCRATCH / 'AtlasStage')))
REFRESH = os.environ.get('NP_TREE_ATLAS_REFRESH') == '1'
REPAINT_SLOTS = set(filter(None, os.environ.get('NP_TREE_ATLAS_SLOTS', '').split(',')))
RAMBUTAN_BAKE = Path(os.environ.get('NP_RAMBUTAN_PEEL_BAKE', SOURCE / 'Textures/RambutanPeel'))
DURIAN_LEAF_BAKE = Path(os.environ.get('NP_DURIAN_LEAF_BAKE', SOURCE / 'Textures/DurianLeaf'))
DEEPER_RELIEF = {
    'Bark': 2.12, 'Twig': 1.45, 'Apple/Leaf': 1.37, 'Apple/LeafAccent': 1.28,
    'Apple/FruitForming': 1.16, 'Apple/FruitReady': 1.13,
    'Rambutan/FruitForming': 1.59, 'Rambutan/FruitReady': 1.59,
    'Durian/Leaf': 1.40, 'Durian/LeafAccent': 1.34, 'Durian/FruitForming': 1.52,
}


def procedure(name, **settings):
    proc = Gimp.get_pdb().lookup_procedure(name)
    config = proc.create_config()
    for key, value in settings.items():
        config.set_property(key.replace('_', '-'), value)
    result = proc.run(config)
    if result.index(0) != Gimp.PDBStatusType.SUCCESS:
        raise RuntimeError((name, result.index(0)))
    return result


def svg_layer(image, slot, parent, drawing):
    vector = WORK / 'Brushes' / (slot.replace('/', '_') + '.svg')
    vector.write_text(drawing, encoding='utf-8')
    path = Gio.File.new_for_path(str(vector))
    result = procedure('file-svg-load', run_mode=Gimp.RunMode.NONINTERACTIVE,
                       file=path, width=TILE, height=TILE, prefer_native_dimensions=True)
    rendered = result.index(1)
    try:
        layer = Gimp.Layer.new_from_drawable(rendered.get_layers()[0], image)
        image.insert_layer(layer, parent, 0)
    finally:
        rendered.delete()
    if (layer.get_width(), layer.get_height()) != (TILE, TILE):
        raise RuntimeError(f'GIMP could not import the brushwork for {slot}')
    layer.set_name(slot)
    layer.set_offsets(*tile_origin(slot))
    return layer


def group(image, name):
    for layer in image.get_layers():
        if layer.is_group() and layer.get_name() == name:
            return layer
    layer = Gimp.GroupLayer.new(image, name)
    image.insert_layer(layer, None, 0)
    return layer


def draw_normal(image, slot, parent, drawing, scale):
    height = svg_layer(image, slot, parent, drawing)
    height.set_name(f'{slot} / relief master')
    relief = height.copy()
    image.insert_layer(relief, parent, 0)
    relief.set_offsets(*tile_origin(slot))
    relief.set_name(f'{slot} / normal')
    height.set_visible(False)
    effect = Gimp.DrawableFilter.new(relief, 'gegl:normal-map', 'Tangent +Y relief')
    config = effect.get_config()
    for key, value in [('scale', scale), ('flip-x', False), ('flip-y', False), ('tileable', False)]:
        config.set_property(key, value)
    relief.merge_filter(effect)


def baked_surface(slot, suffix):
    if suffix not in ('BaseColor', 'Normal') or REPAINT_SLOTS and slot not in REPAINT_SLOTS:
        return None
    if slot in ('Rambutan/FruitForming', 'Rambutan/FruitReady'):
        folder, label = RAMBUTAN_BAKE, 'baked peel'
        width = round((TILE - 2 * PADDING) * RAMBUTAN_PEEL_WIDTH) + 2 * PADDING
    elif slot in ('Durian/Leaf', 'Durian/LeafAccent'):
        folder, label, width = DURIAN_LEAF_BAKE, 'baked leaf', TILE
    else:
        return None
    filename = 'Normal.png' if suffix == 'Normal' else f'{slot.split("/")[-1]}_BaseColor.png'
    path = folder / filename
    return (path, f'{slot} / {label}', width) if path.is_file() else None


def add_baked_surface(image, slot, parent, bake):
    path, name, width = bake
    layer = Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE, image, Gio.File.new_for_path(str(path)))
    if layer is None or (layer.get_width(), layer.get_height()) != (width, TILE):
        raise RuntimeError(f'Unexpected baked texture dimensions for {path.name}')
    image.insert_layer(layer, parent, 0)
    layer.set_offsets(*tile_origin(slot))
    layer.set_name(name)


def draw_mask(image, slot, parent, smoothness, normal_weight):
    layer = Gimp.Layer.new(image, slot, TILE, TILE, Gimp.ImageType.RGBA_IMAGE, 100, Gimp.LayerMode.NORMAL)
    image.insert_layer(layer, parent, 0)
    layer.set_offsets(*tile_origin(slot))
    rgba = bytes((0, round(255 * normal_weight), 128, round(255 * smoothness)))
    buf = layer.get_buffer()
    buf.set(Gegl.Rectangle.new(0, 0, TILE, TILE), RGBA_FORMAT, rgba * (TILE * TILE))
    buf.flush()
    layer.update(0, 0, TILE, TILE)


def open_atlas(suffix):
    source = SOURCE / 'Textures' / f'{ATLAS}_{suffix}.xcf'
    if source.is_file() and not REFRESH:
        image = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(str(source)))
        if image is None or (image.get_width(), image.get_height()) != (SIZE, SIZE):
            raise RuntimeError(f'Unexpected atlas dimensions for {source.name}')
        image.undo_disable()
        return image

    image = Gimp.Image.new(SIZE, SIZE, Gimp.ImageBaseType.RGB)
    image.undo_disable()
    if suffix != 'SurfaceMask':
        color = '#697447' if suffix == 'BaseColor' else '#8080ff'
        background = Gimp.Layer.new(image, 'Background', SIZE, SIZE, Gimp.ImageType.RGB_IMAGE, 100,
                                    Gimp.LayerMode.NORMAL)
        image.insert_layer(background, None, 0)
        Gimp.context_set_foreground(Gegl.Color.new(color))
        background.fill(Gimp.FillType.FOREGROUND)
    for position in range(TILE, SIZE, TILE):
        image.add_hguide(position)
        image.add_vguide(position)
    return image


def named_layers(layers):
    found = {}
    for layer in layers:
        if layer.is_group():
            found.update(named_layers(layer.get_children()))
        else:
            found[layer.get_name()] = layer
    return found


def defaults():
    if OIL_PALM:
        for surface, palette, smoothness, relief in (
            ('Leaf', ('#132b12', '#28571e', '#4b7826', '#869d39'), .38, .31),
            ('Bark', ('#302119', '#594031', '#756047', '#9e8058'), .23, .61),
            ('FruitForming', ('#261113', '#461818', '#6a2219', '#843324'), .30, .24),
            ('FruitReady', ('#361b25', '#7d2330', '#c6471e', '#f39224'), .60, .16),
        ):
            yield f'OilPalm/{surface}', 'Oil palm', 'oil_palm', palette, smoothness, relief
        return
    for slot, (palette, smoothness, normal_weight) in COMMON.items():
        collection = 'Shared fruit details' if SLOTS[slot] >= 60 else 'Shared wood, stems and seed'
        yield slot, collection, slot, palette, smoothness, normal_weight
    for species, (pattern, leaf, accent, forming, ready, smoothness) in SPECIES.items():
        weight = FRUIT_RELIEF.get(pattern, .48)
        if species in ('Banana', 'Pear'):
            weight_leaf = .56 if species == 'Pear' else .34
            smoothness_leaf = .26 if species == 'Pear' else .32
            yield f'{species}/Leaf', species, pattern, leaf, smoothness_leaf, weight_leaf
            yield f'{species}/LeafAccent', species, pattern, accent, smoothness_leaf - .01, weight_leaf
        elif species in CUTOUT_SPECIES:
            yield f'{species}/Leaf', species, pattern, leaf, .23, 0
            yield f'{species}/LeafAccent', species, pattern, accent, .21, 0
        elif species == 'Durian':
            yield f'{species}/Leaf', species, pattern, leaf, .29, .68
            yield f'{species}/LeafAccent', species, pattern, accent, .22, .63
        else:
            yield f'{species}/Leaf', species, pattern, leaf, .20, .57
            yield f'{species}/LeafAccent', species, pattern, accent, .18, .54
        yield f'{species}/FruitForming', species, pattern, forming, smoothness - .05, weight
        yield f'{species}/FruitReady', species, pattern, ready, smoothness, weight


def add_missing(suffix, image, surfaces):
    layers = named_layers(image.get_layers())
    parents = {}
    for slot, collection, pattern, palette, smoothness, normal_weight in surfaces:
        layer_names = (f'{slot} / normal', f'{slot} / relief master') if suffix == 'Normal' else (slot,)
        bake = baked_surface(slot, suffix)
        bake_names = (bake[1],) if bake else ()
        if slot in REPAINT_SLOTS:
            for name in (*layer_names, *bake_names):
                if name in layers:
                    image.remove_layer(layers.pop(name))
        if all(name in layers for name in layer_names):
            if bake and bake[1] not in layers:
                add_baked_surface(image, slot, group(image, collection), bake)
            continue
        if any(name in layers for name in layer_names):
            raise RuntimeError(f'Incomplete relief pair for {slot} in the existing XCF')
        if collection not in parents:
            parents[collection] = group(image, collection)
        parent = parents[collection]
        if suffix == 'SurfaceMask':
            draw_mask(image, slot, parent, smoothness, normal_weight)
        else:
            drawing = draw_surface(slot, pattern, palette, relief=suffix == 'Normal')
            if suffix == 'BaseColor':
                species, _, surface = slot.partition('/')
                if species in CUTOUT_SPECIES and surface in ('Leaf', 'LeafAccent'):
                    background = layers.get('Background')
                    if background is not None:
                        if not background.has_alpha():
                            background.add_alpha()
                        x, y = tile_origin(slot)
                        buffer = background.get_buffer()
                        buffer.set(Gegl.Rectangle.new(x, y, TILE, TILE), RGBA_FORMAT, bytes(4 * TILE * TILE))
                        buffer.flush()
                        background.update(x, y, TILE, TILE)
                svg_layer(image, slot, parent, drawing)
            else:
                draw_normal(image, slot, parent, drawing, DEEPER_RELIEF.get(slot, 1.24))
        if bake:
            add_baked_surface(image, slot, parent, bake)


def verify_layers(image, suffix, surfaces):
    layers = named_layers(image.get_layers())
    for slot, *_ in surfaces:
        names = (f'{slot} / normal', f'{slot} / relief master') if suffix == 'Normal' else (slot,)
        for name in names:
            layer = layers[name]
            succeeded, x, y = layer.get_offsets()
            if (not succeeded or (x, y) != tile_origin(slot)
                    or (layer.get_width(), layer.get_height()) != (TILE, TILE)):
                raise RuntimeError(f'{name} no longer matches its registered UV tile')
        bake = baked_surface(slot, suffix)
        if bake:
            _, name, width = bake
            layer = layers[name]
            if layer.get_offsets()[1:] != tile_origin(slot) or (layer.get_width(), layer.get_height()) != (width, TILE):
                raise RuntimeError(f'{name} does not match its registered UV tile')


def save(image, suffix):
    name = f'{ATLAS}_{suffix}'
    procedure('gimp-xcf-save', run_mode=Gimp.RunMode.NONINTERACTIVE, image=image,
              file=Gio.File.new_for_path(str(WORK / f'{name}.xcf')))
    procedure('file-png-export', run_mode=Gimp.RunMode.NONINTERACTIVE, image=image,
              file=Gio.File.new_for_path(str(WORK / f'{name}.png')), compression=7,
              include_color_profile=False)
    print(f'Staged {suffix}', flush=True)


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    (WORK / 'Brushes').mkdir(exist_ok=True)
    surfaces = tuple(defaults())
    if ((not OIL_PALM and set(SPECIES) != set(REGISTRY['speciesSlots']))
            or {item[0] for item in surfaces} != set(SLOTS)):
        raise RuntimeError('The atlas brushwork and UV registry disagree')
    if REPAINT_SLOTS - SLOTS.keys():
        raise ValueError(f'Unknown atlas slots: {REPAINT_SLOTS - SLOTS.keys()}')
    Gimp.context_push()
    try:
        for suffix in MAPS:
            image = open_atlas(suffix)
            try:
                add_missing(suffix, image, surfaces)
                verify_layers(image, suffix, surfaces)
                save(image, suffix)
            finally:
                image.delete()
    finally:
        Gimp.context_pop()
    print(f'FRUIT_TREE_GIMP_PASS {len(surfaces)} registered tiles', flush=True)


main()
