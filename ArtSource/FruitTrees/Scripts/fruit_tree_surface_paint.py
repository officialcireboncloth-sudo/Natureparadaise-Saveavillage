"""Brush patterns for the flat swatches used by the GIMP fruit-tree atlas."""

import math
import random

from fruit_tree_atlas import (
    BANANA_BLOSSOM_UV, BANANA_GREEN_PEEL_WIDTH,
    DURIAN_BARK_UV, DURIAN_PEEL_WIDTH, DURIAN_PULP_RECT, DURIAN_SECTION_CENTER, DURIAN_SECTION_RADIUS,
    DURIAN_SEED_RECT,
    CUTOUT_SPECIES, GUAVA_PEEL_WIDTH, GUAVA_SECTION_CENTER, GUAVA_SECTION_RADIUS,
    MANGO_PEEL_WIDTH, MANGO_SECTION_CENTER, MANGO_SECTION_RADIUS, MANGO_SEED_CENTER, MANGO_SEED_RADII,
    ORANGE_PEEL_WIDTH, ORANGE_SECTION_CENTER, ORANGE_SECTION_RADIUS, ORANGE_SEED_RECT,
    PEAR_BARK_UV, PEAR_PEEL_WIDTH, PEAR_PROFILE, PEAR_SECTION_CENTER, PEAR_SECTION_SCALE,
    RAMBUTAN_PEEL_WIDTH, RAMBUTAN_SECTION_CENTER, RAMBUTAN_SECTION_RADIUS, RAMBUTAN_SEED_RECT,
    PADDING, SINGLE_LEAF_RECT, SPRAY_ROOT, TILE,
)


# Pairing the colored paint with a separate relief palette keeps pigment out of the normal map.
RELIEF = ('#686868', '#808080', '#939393', '#acacac')


class Swatch:
    def __init__(self, name, palette, relief):
        self.rng = random.Random(name)
        self.palette = RELIEF if relief else palette
        self.relief = relief
        self.elements = []
        self.definitions = []
        self.washes = set()

    def stroke(self, points, tone, width, opacity, color=None):
        x, y, *curve = points
        command = {2: 'L', 4: 'Q', 6: 'C'}[len(curve)]
        coords = ' '.join(f'{value:.2f}' for value in curve)
        self.elements.append(f'<path d="M{x:.2f} {y:.2f}{command}{coords}" fill="none" '
                             f'stroke="{color or self.palette[tone]}" stroke-width="{width:.2f}" '
                             f'stroke-opacity="{opacity:.3f}" stroke-linecap="round"/>')

    def dab(self, x, y, rx, ry, tone, opacity, fill=None):
        self.elements.append(f'<ellipse cx="{x:.2f}" cy="{y:.2f}" rx="{rx:.2f}" ry="{ry:.2f}" '
                             f'fill="{fill or self.palette[tone]}" opacity="{opacity:.3f}"/>')

    def facet(self, points, tone, opacity):
        coords = ' '.join(f'{x:.2f},{y:.2f}' for x, y in points)
        self.elements.append(f'<polygon points="{coords}" fill="{self.palette[tone]}" opacity="{opacity:.3f}"/>')

    def ground(self, bands):
        if self.relief:
            fill = self.palette[1]
        else:
            stops = ''.join(f'<stop offset="{at}" stop-color="{self.palette[tone]}"/>' for at, tone in bands)
            self.definitions.append('<linearGradient id="ground" gradientUnits="userSpaceOnUse" '
                                    f'x1="0" y1="0" x2="0" y2="256">{stops}</linearGradient>')
            fill = 'url(#ground)'
        self.elements.append(f'<rect x="-24" y="-24" width="304" height="304" fill="{fill}"/>')

    def wash(self, x, y, rx, ry, tone, opacity, wrap=False):
        if self.relief:
            return
        name = f'wash{tone}'
        if tone not in self.washes:
            color = self.palette[tone]
            self.definitions.append(f'<radialGradient id="{name}"><stop stop-color="{color}"/>'
                                    f'<stop offset="1" stop-color="{color}" stop-opacity="0"/></radialGradient>')
            self.washes.add(tone)
        positions = wrapped(x, rx) if wrap else (x,)
        for position in positions:
            self.dab(position, y, rx, ry, tone, opacity, f'url(#{name})')

    def svg(self):
        margin = PADDING * 256 / (TILE - 2 * PADDING)
        box = f'{-margin} {-margin} {256 + 2 * margin} {256 + 2 * margin}'
        defs = ''.join(self.definitions)
        return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{TILE}" height="{TILE}" '
                f'viewBox="{box}"><defs>{defs}</defs>{"".join(self.elements)}</svg>')


def wrapped(x, radius):
    # Carry the same brushstroke through both sides of a cylindrical UV seam.
    return [position for position in (x, x - 256, x + 256) if -12 - radius < position < 268 + radius]


def leaf_cutouts(paint, species, accent):
    """Paint the paired UV patches for a branch spray and one close-up blade."""
    if paint.relief:
        paint.ground(((0, 1), (1, 1)))
        return
    mango = species == 'mango'
    blade = []
    for step in range(1, 24):
        t = step / 24
        arch = math.sin(math.pi * t)
        if species == 'apple':
            edge = 35 * arch**.67 * (1.18 - .37 * t)
            tooth = .95
        elif species == 'avocado':
            edge = 24.5 * arch**.94 * (1.12 - .22 * t)
            tooth = 1
        elif species == 'guava':
            edge = 33.5 * arch**.58
            tooth = 1
        elif species == 'orange':
            edge = 32.5 * arch**.74 * (1.10 - .25 * t)
            tooth = 1
        elif species == 'rambutan':
            edge = 28.5 * arch**.76 * (1.24 - .44 * t)
            tooth = 1
        elif mango:
            edge = 17.8 * arch**1.12 * (1.26 - .48 * t)
            tooth = 1
        else:
            edge = 30 * arch**.84 * (1.07 - .23 * t)
            tooth = .89
        blade.append((edge * (tooth if step % 2 == 0 else 1), -100 * t))
    points = [(0, 0), *blade, (1, -100), *((-x * .91, y) for x, y in reversed(blade))]
    def sweep(y):
        return 8.6 * (y / 100)**2 if mango else 0

    outline = ' '.join(f'{x + sweep(y):.2f},{y:.2f}' for x, y in points)
    for light, tones in enumerate(((0, 1, 2), (1, 2, 3), (0, 2, 3))):
        stops = ''.join(f'<stop offset="{at}" stop-color="{paint.palette[tone]}"/>'
                        for at, tone in zip((0, .54, 1), tones))
        paint.definitions.append(f'<linearGradient id="lamina{light}" gradientUnits="userSpaceOnUse" '
                                 f'x1="-22" y1="-2" x2="30" y2="-105">{stops}</linearGradient>')

    def stamp(x, y, angle, scale, light):
        paint.elements.append(f'<g transform="translate({x} {y}) rotate({angle}) scale({scale})">')
        paint.elements.append(f'<polygon points="{outline}" fill="url(#lamina{light})"/>')
        ridge = (0, -2, -1, -35, 3, -71, 8, -96) if mango else (0, -2, -2, -41, 3, -72, 1, -94)
        paint.stroke(ridge, 3, 1.35 if mango else 1.8, .56)
        pairs = 10 if mango else 9 if species == 'guava' else 6
        for k in range(pairs):
            y = -22 - 50 * k / (pairs - 1)
            width = 11 if mango else 17 if species == 'avocado' else 22
            edge = width * math.sin(math.pi * (k + 2) / (pairs + 3))
            for side in (-1, 1):
                paint.stroke((sweep(y), y, sweep(y - 5) + side * edge * .66, y - 5,
                              sweep(y - 13) + side * edge, y - 13),
                             3, .67 if mango else .92, .41 if species == 'guava' else .30)
        paint.elements.append('</g>')

    x, y = SPRAY_ROOT
    paint.stroke((x, y, 103, 177, 121, 128, 113, 61), 0, 5.1, 1)
    paint.stroke((x + 1, y, 105, 177, 123, 128, 114, 61), 3, 1.8, .72)
    leaves = (
        (109, 211, -71, .88, 0), (111, 198, 64, .93, 1),
        (111, 167, -61, 1.02, 2), (113, 154, 72, .86, 0),
        (116, 121, -54, .94, 1), (116, 107, 51, 1.02, 2),
        (114, 86, -11, .70, 1),
    )
    if species == 'guava':
        leaves = (
            (110, 202, -64, .91, 0), (110, 202, 66, .92, 1),
            (112, 156, -63, .96, 1), (112, 156, 65, .88, 2),
            (116, 111, -51, .92, 2), (116, 111, 53, .91, 1),
            (114, 85, -8, .69, 2),
        )
    elif species == 'orange':
        leaves = (
            (109, 204, -75, .86, 0), (109, 204, 71, .90, 1),
            (112, 159, -62, .97, 1), (112, 159, 58, .98, 2),
            (115, 118, -39, .96, 0), (115, 118, 43, 1.02, 1),
            (115, 105, -4, .92, 2),
        )
    elif species == 'rambutan':
        leaves = (
            (109, 210, -68, .94, 0), (109, 207, 68, .98, 1),
            (111, 166, -61, 1.02, 1), (111, 164, 61, 1.02, 2),
            (115, 121, -49, .94, 2), (115, 119, 49, .95, 1),
        )
    elif mango:
        leaves = (
            (110, 203, -76, .91, 0), (110, 203, 68, .87, 1),
            (113, 167, -58, 1.03, 1), (113, 167, 52, 1.06, 2),
            (115, 133, -38, 1.11, 2), (115, 133, 35, 1.14, 1),
            (113, 111, -8, .92, 2),
        )
    for leaf_x, leaf_y, angle, scale, light in leaves:
        turn = 6 if accent and not mango else 0
        if species == 'avocado':
            turn -= angle * .15
        elif species == 'apple':
            scale *= .96
        stamp(leaf_x, leaf_y, angle + turn, scale, light)

    left, _, right, bottom = SINGLE_LEAF_RECT
    # The separate blade stays large enough to read on the seedling and harvested pair.
    if mango:
        paint.elements.append(f'<g transform="translate({(left + right) / 2 - 2} {bottom - 3}) scale(.76 .71)">')
        stamp(0, 0, 0, 1, 1)
        paint.elements.append('</g>')
        return
    leaf_width = .48 if species == 'apple' else .53
    paint.elements.append(f'<g transform="translate({(left + right) / 2} {bottom - 3}) scale({leaf_width} .71)">')
    stamp(0, 0, 0, 1, 1)
    paint.elements.append('</g>')


# Pairs, lamina width, vein sweep and vein contrast vary with the leaf geometry.
VENATION = {
    'pear': (7, 99, 37, .33),
    'peach': (11, 75, 37, .35),
}


def durian_leaf(paint, accent):
    """Paint the center ridge at the blade's middle UV and fan the veins toward its margins."""
    paint.ground(((0, 2), (.24, 1), (.65, 1), (1, 0)))
    paint.wash(80, 99, 79, 158, 2, .72)
    paint.wash(201, 155, 103, 160, 0, .54)
    paint.wash(145, 69, 57, 89, 2, .39)
    shade = '#4b381b' if accent else '#152f1c'
    veins = '#ba945c' if accent else '#7b9850'
    midrib = '#e0bc7a' if accent else '#a5b96a'
    for side in (-1, 1):
        for index, root in enumerate((50, 79, 108, 138, 168, 197, 224)):
            y = root + (2.4 if side == 1 else -1.8)
            spread = (79 + 22 * math.sin(math.pi * index / 7)) * side
            # Broader tonal folds hold the pattern when the small veins are mip-filtered.
            curve = (128, y, 128 + spread*.32, y-6, 128 + spread*.76, y-28,
                     128 + spread, y-52)
            paint.stroke(curve, 0, 13.0, .34, None if paint.relief else shade)
            paint.stroke(tuple(value - 2.9 if coordinate % 2 else value
                               for coordinate, value in enumerate(curve)),
                         2, 7.3, .39)
            paint.stroke(curve, 3, 3.9, .76, None if paint.relief else veins)
            branch = (128 + spread*.48, y-15, 128 + spread*.68, y-18, 128 + spread*.84, y-29)
            paint.stroke(branch, 2, 2.0, .40)
    spine = (128, 257, 126, 183, 130, 83, 128, 0)
    paint.stroke(spine, 0, 17.0, .59, None if paint.relief else shade)
    paint.stroke(spine, 2, 10.0, .96)
    light = (126, 255, 124, 183, 128, 83, 126, 2)
    paint.stroke(light, 3, 5.8, .82, None if paint.relief else midrib)
    if not paint.relief:
        for _ in range(105):
            x, y = paint.rng.uniform(12, 244), paint.rng.uniform(8, 245)
            length = paint.rng.uniform(2.3, 7.8)
            paint.stroke((x, y, x + length * .63, y - length), paint.rng.choice((0, 2)),
                         paint.rng.uniform(.57, 1.5), .21)


def leaf(paint, species, accent):
    if species == 'banana':
        banana_leaf(paint, accent)
    elif species.capitalize() in CUTOUT_SPECIES:
        leaf_cutouts(paint, species, accent)
    elif species == 'durian':
        durian_leaf(paint, accent)
    else:
        layered_leaf(paint, species, accent)


def banana_leaf(paint, accent):
    # Irregular cuts follow the veins and open through the outer edge of the older blades.
    splits = (((.13, .18), (.26, .45), (.40, .31), (.54, .54), (.69, .31), (.80, .21)),
              ((.13, .24), (.24, .32), (.36, .49), (.50, .32), (.64, .44), (.77, .28)))

    def edge(t, side):
        width = 114 * math.sin(math.pi * t)**.53 * (.91 + .18 * t)
        return 128 + side * width + 1.9 * math.sin(t * 9) * math.sin(math.pi * t), 9 + 222 * t

    sides = []
    for side, cuts in zip((-1, 1), splits):
        cuts = () if accent else cuts
        samples = [i / 64 for i in range(65)]
        points = [(t, edge(t, side)) for t in samples
                  if not any(at - .007 <= t <= at + .021 for at, _ in cuts)]
        for at, depth in cuts:
            start, inner, end = at - .007, at + .003, at + .021
            x, y = edge(inner, side)
            points.extend(((start, edge(start, side)), (inner, (x + (128 - x) * depth, y)),
                           (end, edge(end, side))))
        sides.append([point for _, point in sorted(points)])
    outline = ' '.join(f'{x:.2f},{y:.2f}' for x, y in (*sides[0], *reversed(sides[1][1:-1])))
    paint.definitions.append(f'<clipPath id="banana-edge"><polygon points="{outline}"/></clipPath>')
    if paint.relief:
        paint.elements.append(f'<rect x="-24" y="-24" width="304" height="304" fill="{paint.palette[1]}"/>')
    paint.elements.append('<g clip-path="url(#banana-edge)">')
    paint.ground(((0, 2), (.30, 1), (.69, 1), (1, 0)))
    paint.wash(79, 109, 77, 113, 2, .90)
    paint.wash(172, 153, 64, 106, 0, .55)
    paint.wash(240, 100, 54, 133, 3, .70)
    paint.wash(28, 151, 29, 96, 3, .49)
    for side in (-1, 1):
        for index in range(43):
            t = .07 + .87 * index / 42
            x, y = edge(max(.01, t - .09), side)
            ridge_y = 9 + t * 222 + paint.rng.uniform(-1.6, 1.6)
            reach = x - 128
            curve = (128, ridge_y, 128 + reach * .27, ridge_y - 3,
                     x - reach * .13, y + 7, x, y)
            paint.stroke(curve, 0, 2.0, .20)
            highlight = tuple(value - .89 if j % 2 else value for j, value in enumerate(curve))
            paint.stroke(highlight, 2 if index % 4 else 3, .86, .32)
    rib = (128, 232, 126, 163, 131, 83, 128, 10)
    paint.stroke(rib, 0, 7.4, .37)
    paint.stroke(rib, 3, 3.6, .77)
    paint.stroke((127, 231, 126, 181, 128, 146), 3, 5.1, .55)
    paint.elements.append('</g>')
    paint.stroke((128, 250, 127, 241, 128, 230), 1, 6.3, 1)
    paint.stroke((127, 248, 127, 239, 128, 230), 3, 2.6, .75)


def banana_sheath(paint):
    rng = paint.rng
    paint.ground(((0, 2), (.35, 1), (.66, 1), (.85, 1), (1, 0)))
    # Long, shallow folds catch a thin rim of light next to each darker overlap.
    for x, tilt in ((12, -12), (51, 8), (96, 19), (139, 11), (193, -10), (236, 12)):
        paint.wash(x - 5, 109, 27, 118, 2, .69, wrap=True)
        paint.wash(x + 13, 169, 15, 110, 0, .59, wrap=True)
        paint.wash(x + 5, 243, 20, 74, 3, .79, wrap=True)
        for xx in wrapped(x, abs(tilt) + 14):
            fold = (xx, 268, xx + tilt * .25 + 3, 187, xx + tilt * .84 - 3, 81, xx + tilt, -12)
            paint.stroke(fold, 0, 7.0, .22)
            paint.stroke(fold, 0, 2.9, .49)
            lip = tuple(value - 2.7 if i % 2 == 0 else value for i, value in enumerate(fold))
            paint.stroke(lip, 2, 2.5, .56)

    # The fibers vary in length and fade across the soft patches of pigment.
    for _ in range(164):
        x, y = rng.uniform(0, 256), rng.uniform(-28, 259)
        length, bend = rng.uniform(8, 66), rng.uniform(-4.7, 4.7)
        width, opacity = rng.uniform(.55, 1.3), rng.uniform(.15, .43)
        tone = rng.choice((0, 2, 2))
        for xx in wrapped(x, abs(bend) + 2):
            curve = (xx, y, xx + bend * .27, y + length * .58, xx + bend, y + length)
            paint.stroke(curve, tone, width, opacity)

    for _ in range(105):
        x, y = rng.uniform(0, 256), rng.uniform(26, 260)
        width, height = rng.uniform(.36, 1.1), rng.uniform(.8, 3.5)
        opacity = rng.uniform(.13, .34) * (.47 + .66 * y / 256)
        for xx in wrapped(x, width + 1):
            paint.dab(xx, y, width, height, 0, opacity)
            paint.dab(xx - .51, y - .21, width * .51, height * .67, 2, opacity * .52)

    # Warm, short scuffs are concentrated where the lowest sheaths age.
    if not paint.relief:
        for _ in range(22):
            x, y = rng.uniform(0, 256), rng.uniform(159, 256)
            width, height = rng.uniform(1.4, 4.7), rng.uniform(3.3, 15)
            paint.wash(x, y, width * 2.7, height * 1.5, 3, .56, wrap=True)
            for xx in wrapped(x, width + 2):
                paint.dab(xx, y, width, height, 0, rng.uniform(.18, .35), '#82602b')


def layered_leaf(paint, species, accent):

    rng = paint.rng
    paint.ground(((0, 2), (.36, 1), (.70, 1), (1, 0)))
    if species == 'papaya':
        papaya_leaf(paint)
        return
    if species == 'dragon':
        cactus(paint, accent)
        return
    pairs, width, sweep, opacity = VENATION[species]
    paint.wash(65, 72, 100, 111, 2, .49)
    paint.wash(196, 142, 130, 131, 0, .51)
    paint.wash(146, 90, 85, 80, 2, .19)
    for _ in range(34):
        x = rng.uniform(6, 251)
        y = rng.uniform(-12, 270)
        paint.wash(x, y, rng.uniform(12, 38), rng.uniform(16, 51), rng.choice((0, 2)), rng.uniform(.12, .31))

    ridge = (127.5, 267, 122, 192, 135, 97, 128, -11)
    paint.stroke(ridge, 0, 7.5, .37)
    paint.stroke(ridge, 3, 3.7, .67 if accent else .52)
    for side in (-1, 1):
        for vein in range(pairs):
            t = vein / (pairs - 1)
            y = 58 + 185 * t + rng.uniform(-4.5, 4.5)
            center = 128 + 2 * math.sin(y / 47)
            spread = width * (.66 + .34 * math.sin(math.pi * (t * .76 + .12))) * rng.uniform(.90, 1.12)
            target_x = center + side * spread
            end_y = y - sweep * rng.uniform(.86, 1.11)
            curve = (center, y, center + side * spread * .39, y - sweep * .37,
                     target_x - side * 15, end_y + 14, target_x, end_y)
            paint.stroke(curve, 0, 3, opacity * .67)
            light_curve = tuple(value - 1 if i % 2 else value for i, value in enumerate(curve))
            paint.stroke(light_curve, 3, 1.28, opacity)
            for branch in (.43, .65, .82):
                bx = center + side * spread * branch
                by = y + (end_y - y) * branch
                paint.stroke((bx, by, bx + side * 8, by - 11, bx + side * 18, by - 17),
                             2, .72, opacity * .73)
    for _ in range(105):
        x, y = rng.uniform(0, 256), rng.uniform(0, 256)
        length = rng.uniform(2, 10)
        reach = length * (1 if x > 128 else -1)
        paint.stroke((x, y, x + reach, y - length * .4), rng.choice((0, 2)), rng.uniform(.55, 1.45), .21)


def papaya_leaf(paint):
    paint.wash(107, 103, 142, 143, 2, .56)
    paint.wash(153, 216, 120, 81, 0, .55)
    root = (129, 222)
    tips = ((4, 182), (5, 108), (44, 31), (128, 2), (207, 25), (247, 105), (254, 172))
    for x, y in tips:
        bend = (root[0] * .56 + x * .44, y * .46 + 116)
        rib = (*root, *bend, x, y)
        paint.stroke(rib, 0, 6.1, .40)
        paint.stroke(rib, 3, 2.5, .67)
        for step in (.30, .45, .60, .75):
            bx = (1-step)**2 * root[0] + 2 * (1-step) * step * bend[0] + step**2 * x
            by = (1-step)**2 * root[1] + 2 * (1-step) * step * bend[1] + step**2 * y
            for side in (-1, 1):
                paint.stroke((bx, by, bx + side * 11, by - 6, bx + side * 19, by - 18), 2, 1.3, .44)


def cactus(paint, accent):
    rng = paint.rng
    paint.wash(61, 87, 83, 164, 2, .47, wrap=True)
    paint.wash(190, 147, 95, 166, 0, .52, wrap=True)
    for center in (0, 128, 256):
        paint.stroke((center-2, -17, center+14, 82, center-12, 181, center+4, 268),
                     0, 16 if accent else 10, .23)
        paint.stroke((center, -17, center+15, 82, center-9, 181, center+6, 268),
                     3, 3.8 if accent else 1.7, .57)
        for y in range(10, 256, 27):
            x = center + math.sin(y * .032) * 3
            paint.dab(x, y, 4.2, 2.9, 0, .39)
            paint.dab(x - .4, y - .9, 1.7, 1.6, 3, .66)
    for _ in range(115):
        x, y = rng.uniform(0, 256), rng.uniform(0, 256)
        tone = rng.choice((0, 2))
        for xx in wrapped(x, 1.5):
            paint.dab(xx, y, 1.5, 2.1, tone, .20)


def orchard_wood(paint, surface):
    rng = paint.rng
    trunk = surface == 'Bark'
    paint.ground(((0, 1), (.26, 1), (.64, 1), (.89, 1), (1, 0 if trunk else 1)))
    for _ in range(42):
        x, y = rng.uniform(0, 256), rng.uniform(-7, 266)
        paint.wash(x, y, rng.uniform(9, 26), rng.uniform(22, 81), rng.choice((0, 2)),
                   rng.uniform(.29, .73), wrap=True)
    # Layer a broken grain over the broad highlights instead of one stripe down the trunk.
    for x in range(3, 256, 15 if trunk else 25):
        x += rng.uniform(-4, 5)
        y = rng.uniform(-20, 224)
        span, bend = rng.uniform(34, 122), rng.uniform(-16, 16)
        for xx in wrapped(x, abs(bend)+7):
            seam = (xx, y, xx+bend*.12, y+span*.21, xx+bend*1.22, y+span*.68, xx+bend, y+span)
            light = tuple(value+2.9 if i % 2 == 0 else value for i, value in enumerate(seam))
            paint.stroke(seam, 0, 7.2 if trunk else 3.5, .24)
            paint.stroke(seam, 0, 2.7 if trunk else 1.3, .62)
            paint.stroke(light, 2, 3.8 if trunk else 2.1, .63)
    if trunk:
        for _ in range(105):
            x, y = rng.uniform(0, 256), rng.uniform(-17, 268)
            w, h = rng.uniform(3.8, 15), rng.uniform(6, 26)
            bend = rng.uniform(-7, 7)
            for xx in wrapped(x, w+abs(bend)):
                plate = ((xx-w*.50, y-h), (xx+w*.13, y-h*.92), (xx+w*.75, y-h*.57),
                         (xx+w*.61+bend, y-h*.11), (xx+w+bend, y+h*.33), (xx+bend, y+h),
                         (xx-w*.70+bend, y+h*.78), (xx-w*.49, y+h*.30), (xx-w, y-h*.12))
                paint.facet(plate, 0, .26)
                paint.facet([(xx+(px-xx)*.81-.6, y+(py-y)*.87-1) for px, py in plate], 2, .33)
                paint.stroke((xx-w*.48, y+h*.62, xx-w*.72, y, xx-w*.39, y-h*.68), 3, 1.38, .47)
    strokes = 104 if trunk else 58
    for _ in range(strokes):
        x, y = rng.uniform(0, 256), rng.uniform(-29, 268)
        span = rng.uniform(7, 66 if trunk else 43)
        turn, width = rng.uniform(-8, 8), rng.uniform(.52, 2.9 if trunk else 1.4)
        for xx in wrapped(x, abs(turn)+4):
            groove = (xx, y, xx+1, y+span*.31, xx+turn*1.12, y+span*.61, xx+turn, y+span)
            lip = tuple(value+1.25 if i % 2 == 0 else value-.48 for i, value in enumerate(groove))
            paint.stroke(groove, 0, width, .67 if trunk else .38)
            paint.stroke(lip, 3, width*.56, .46 if trunk else .29)
    for _ in range(142 if trunk else 124):
        x, y = rng.uniform(0, 256), rng.uniform(0, 256)
        width = rng.uniform(1.05, 4.8)
        for xx in wrapped(x, width):
            paint.dab(xx, y, width, width*.22, 0, .65)
            paint.dab(xx-.1, y+.80, width*.72, width*.12, 3, .55)
    if trunk:
        for x, y, width in ((43, 173, 12.0), (119, 101, 8.0), (204, 59, 9.5)):
            paint.wash(x, y, width*2.5, width*3.9, 0, .67)
            paint.dab(x, y, width*.72, width*1.04, 0, .63)
            paint.dab(x-.8, y-1.15, width*.56, width*.91, 2, .53)
            paint.dab(x+1.1, y+1.7, width*.31, width*.58, 0, .91)
            for offset in (-1, 1):
                xx = x+offset*width
                paint.stroke((x-1, y-width*3.0, xx+offset*5, y-width, xx, y+width,
                              x-2, y+width*2.6), 0, 2.8, .72)
                paint.stroke((x-3, y-width*3.4, xx+offset*7, y-width, xx+offset*3, y+width,
                              x-3, y+width*2.9), 3, 1.3, .48)


def bark(paint, surface):
    rng = paint.rng
    paint.ground(((0, 1), (.29, 1), (.51, 2), (.81, 1), (1, 1)))
    plate_count, crack_count, stretch = {
        'PapayaStem': (12, 38, .74),
        'GreenStem': (27, 48, 1.45),
        'StoneSeed': (76, 62, .87),
    }[surface]
    for _ in range(37):
        x, y = rng.uniform(0, 256), rng.uniform(-10, 266)
        paint.wash(x, y, rng.uniform(12, 33), rng.uniform(21, 62), rng.choice((0, 2)), .50, wrap=True)
    for _ in range(plate_count):
        x, y = rng.uniform(0, 256), rng.uniform(-22, 278)
        w, h = rng.uniform(3, 13), rng.uniform(6, 27) * stretch
        tilt = rng.uniform(-8, 8)
        tone = rng.choice((0, 1, 2))
        for xx in wrapped(x, w + abs(tilt)):
            paint.facet(((xx - w * .40, y - h), (xx + w * .70, y - h * .59),
                         (xx + w + tilt, y + h * .34), (xx + tilt, y + h),
                         (xx - w * .65, y + h * .53), (xx - w, y - h * .28)), tone, .31)
            paint.stroke((xx - w * .61, y + h * .45, xx - w * .81, y - h * .15,
                          xx - w * .32, y - h * .81), 2, .97, .37)
    for _ in range(crack_count):
        x, y = rng.uniform(0, 256), rng.uniform(-15, 268)
        span, bend = rng.uniform(5, 25) * stretch, rng.uniform(-4, 4)
        width = rng.uniform(.69, 1.90)
        for xx in wrapped(x, abs(bend) + 2):
            paint.stroke((xx, y, xx + bend, y + span * .46, xx + bend * .32, y + span),
                         0, width, .30)
    if surface == 'PapayaStem':
        for row in range(7):
            for x in range(17 - 16 * (row % 2), 263, 56):
                y = 15 + row * 39 + rng.uniform(-5, 5)
                for xx in wrapped(x, 17):
                    paint.stroke((xx-13, y, xx-4, y+12, xx+13, y-2), 0, 3.7, .74)
                    paint.stroke((xx-13, y+2, xx-3, y+15, xx+12, y+1), 3, 1.6, .46)


def apple_peel(paint, ripe):
    rng = paint.rng
    paint.ground(((0, 0), (.12, 1), (.34, 1), (.62, 1), (.90, 1), (1, 0)))
    paint.wash(65, 95, 90, 143, 2, .87, wrap=True)
    paint.wash(222, 104, 71, 118, 2, .71, wrap=True)
    paint.wash(146, 153, 72, 106, 0, .70 if ripe else .42, wrap=True)
    paint.wash(74, 211, 88, 85, 4, .77, wrap=True)
    paint.wash(226, 214, 69, 77, 4, .56, wrap=True)
    paint.wash(36, 127, 40, 124, 2, .48, wrap=True)
    paint.wash(190, 22, 100, 37, 0, .69, wrap=True)

    # Red blush and green-yellow cheek meet in uneven, lengthwise bands.
    for _ in range(93):
        x, y = rng.uniform(0, 256), rng.uniform(-18, 252)
        span = rng.uniform(27, 143)
        turn = rng.uniform(-9, 9)
        width = rng.uniform(.70, 3.9)
        tone = rng.choice((0, 0, 2, 2, 4))
        strength = rng.uniform(.13, .37) if ripe else rng.uniform(.08, .25)
        if not paint.relief:
            for xx in wrapped(x, abs(turn)+4):
                paint.stroke((xx, y, xx+turn*.19, y+span*.28, xx+turn*1.12, y+span*.72,
                              xx+turn, y+span), tone, width, strength)
    for _ in range(530):
        x, y = rng.uniform(0, 256), rng.uniform(9, 247)
        radius = rng.uniform(.38, 1.42)
        opacity = rng.uniform(.22, .63)
        for xx in wrapped(x, radius+1):
            paint.dab(xx+.40, y+.28, radius*1.25, radius*.88, 0, opacity*.55)
            paint.dab(xx, y, radius*.64, radius*.59, 3, opacity)


# Peel relief ranges from nearly flat wax to closely packed raised pits.
PEELS = {
    'orange': ('pore', 8.5, 2.05),
    'mango': ('fleck', 13, .94),
    'guava': ('pore', 14, 1.75),
    'pear': ('fleck', 10, 1.04),
    'peach': ('fuzz', 7, 1.22),
    'cherry': ('fleck', 24, .82),
    'avocado': ('pebble', 10, 3.1),
    'papaya': ('fleck', 16, 1.15),
    'banana': ('fleck', 18, .82),
    'dragon': ('scale', 27, 8.7),
}


def peel(paint, species, ripe):
    if species == 'apple':
        apple_peel(paint, ripe)
        return

    rng = paint.rng
    pattern, space, radius = PEELS[species]
    paint.ground(((0, 0), (.14, 1), (.39, 1), (.62, 1), (.86, 1), (1, 0)))
    paint.wash(54, 105, 159, 149, 2, .83, wrap=True)
    paint.wash(167, 124, 108, 125, 0, .34, wrap=True)
    if ripe and species in {'mango', 'peach', 'dragon'}:
        paint.wash(31, 68, 114, 142, 3, .77, wrap=True)
        paint.wash(88, 124, 87, 130, 3, .41, wrap=True)
    for _ in range(34):
        x, y = rng.uniform(0, 256), rng.uniform(0, 256)
        paint.wash(x, y, rng.uniform(9, 43), rng.uniform(11, 48), rng.choice((0, 2, 3)),
                   rng.uniform(.09, .28), wrap=True)
    if species in {'mango', 'papaya', 'banana'}:
        paint_fibers(paint, species)

    columns, rows = round(256 / space), round(272 / space)
    for row in range(rows + 1):
        for column in range(columns):
            x = ((column + (row % 2) * .5) * 256 / columns + rng.uniform(-.34, .34) * space) % 256
            y = row * 272 / rows - 8 + rng.uniform(-.40, .40) * space
            size = radius * rng.uniform(.63, 1.47)
            strength = rng.uniform(.57, 1)
            for xx in wrapped(x, size * 2):
                peel_detail(paint, pattern, xx, y, size, strength)


def paint_fibers(paint, species):
    rng = paint.rng
    count = {'mango': 18, 'papaya': 31, 'banana': 22}[species]
    for _ in range(count):
        x, y = rng.uniform(0, 256), rng.uniform(-16, 234)
        h, bend = rng.uniform(18, 97), rng.uniform(-7, 7)
        tone = rng.choice((0, 2))
        width = rng.uniform(.7, 2.8)
        for xx in wrapped(x, abs(bend) + 3):
            paint.stroke((xx, y, xx+bend, y+h*.42, xx+bend*.12, y+h), tone, width,
                         .24 if species == 'banana' else .17)
    if species == 'banana':
        for x in range(0, 257, 51):
            paint.wash(x, 142, 7, 150, 2, .48, wrap=True)
            paint.stroke((x+3, -10, x-1, 97, x+2, 211, x+5, 269), 2, 2, .25)


def peel_detail(paint, pattern, x, y, size, strength):
    if pattern == 'pore':
        paint.dab(x, y+.64, size*1.35, size*1.12, 2, .49*strength)
        paint.dab(x, y, size*.81, size*.71, 0, .56*strength)
    elif pattern == 'pebble':
        paint.dab(x, y+.42, size*1.25, size*1.12, 0, .56*strength)
        paint.dab(x-.42, y-.87, size*.96, size*.81, 2, .64*strength)
        paint.dab(x-.78, y-1.2, size*.39, size*.40, 3, .51*strength)
    elif pattern == 'fuzz':
        paint.stroke((x, y, x+size*.55, y-size*1.6), 2, size*.80, .48*strength)
        paint.dab(x, y, size*.47, size*.52, 0, .28*strength)
    elif pattern == 'facet':
        top, right, base, left = (x, y-size), (x+size, y+.12*size), (x+.2*size, y+size), (x-size, y+.20*size)
        tip = (x-.12*size, y-.15*size)
        paint.facet((top, right, base, left), 0, .65*strength)
        paint.facet((left, top, tip), 3, .71*strength)
        paint.facet((top, right, tip), 2, .72*strength)
    elif pattern == 'scale':
        paint.stroke((x-size, y+size*.50, x, y-size*.80, x+size, y+size*.50), 0, 2.2, .44*strength)
        paint.stroke((x-size*.91, y+size*.68, x, y-size*.56, x+size*.91, y+size*.68), 2, 1.5, .51*strength)
    elif pattern == 'root':
        paint.dab(x, y, size*1.42, size*1.18, 2, .65*strength)
        paint.dab(x+.3, y-.48, size*.52, size*.66, 0, .69*strength)
    else:
        paint.dab(x, y, size, size*.77, 0, .51*strength)
        paint.dab(x-.54, y-.49, size*.59, size*.42, 3, .56*strength)


def detail(paint, slot):
    paint.ground(((0, 2), (.19, 2), (.53, 1), (.84, 1), (1, 0)))
    for x in (-45, 48, 145, 230, 303):
        paint.wash(x, 96, 32, 186, 2, .56)
        for y in range(12, 267, 14):
            dx = 6 * math.sin(y * .09 + x)
            paint.stroke((x+dx, y, x+dx-2, y+12, x+dx+3, y+21), 0, 2.3, .39)
    if slot == 'DragonFruitScale':
        paint.wash(128, 247, 148, 95, 3, .88)
        paint.stroke((126, -9, 122, 96, 135, 185, 130, 267), 3, 3.5, .42)


def cut_wood(paint):
    paint.ground(((0, 2), (.41, 1), (1, 2)))
    paint.wash(73, 72, 110, 88, 2, .61)
    paint.wash(199, 170, 97, 101, 0, .46)
    for radius in (11, 19, 33, 44, 55, 58, 69, 84, 95, 109, 114, 128, 145, 151, 166, 176):
        points = []
        for step in range(97):
            a = step * math.tau / 96
            warp = 1 + .063 * math.sin(a * 3 + radius*.02) + .027 * math.cos(a * 7)
            x = 128 + math.cos(a) * radius * warp + radius * .035
            y = 129 + math.sin(a) * radius * warp
            points.append(f'{x:.2f},{y:.2f}')
        paint.elements.append(f'<polyline points="{" ".join(points)}" fill="none" '
                              f'stroke="{paint.palette[0]}" stroke-width="{paint.rng.uniform(.72, 1.9):.2f}" '
                              f'stroke-opacity="{paint.rng.uniform(.33, .57):.2f}"/>')
    for angle, end in ((.59, 102), (2.48, 73), (3.85, 93), (5.18, 114)):
        cx, cy = math.cos(angle), math.sin(angle)
        paint.stroke((128+cx*160, 129+cy*160, 130+cx*125, 130+cy*125,
                      126+cx*end, 129+cy*end), 0, 2.6, .68)


def brown_seed(paint):
    rng = paint.rng
    paint.ground(((0, 2), (.26, 1), (.79, 1), (1, 0)))
    paint.wash(67, 105, 110, 133, 2, .63, wrap=True)
    for _ in range(88):
        x, y = rng.uniform(0, 256), rng.uniform(-24, 231)
        span = rng.uniform(13, 74)
        width = rng.uniform(.6, 1.7)
        tone = rng.choice((0, 2, 3))
        for xx in wrapped(x, 7):
            paint.stroke((xx, y, xx+6, y+span*.44, xx-2, y+span), tone, width, .24)


def guava_section(paint):
    x, y = GUAVA_SECTION_CENTER
    radius = GUAVA_SECTION_RADIUS
    if paint.relief:
        paint.dab(x, y, radius + 4, radius + 4, 1, 1)
        return
    paint.definitions.extend((
        '<radialGradient id="guavaRind"><stop stop-color="#efe6ac"/>'
        '<stop offset=".86" stop-color="#e6e797"/><stop offset=".97" stop-color="#a4b550"/>'
        '<stop offset="1" stop-color="#7e9139"/></radialGradient>',
        '<radialGradient id="guavaFlesh" cx=".43" cy=".40"><stop stop-color="#eb786b"/>'
        '<stop offset=".55" stop-color="#e98178"/><stop offset=".91" stop-color="#f2a095"/>'
        '<stop offset="1" stop-color="#f5afa0"/></radialGradient>',
    ))
    paint.dab(x, y, radius + 3, radius + 3, 1, 1, 'url(#guavaRind)')
    paint.dab(x, y, radius - 3.8, radius - 3.8, 2, 1, 'url(#guavaFlesh)')
    for i in range(5):
        bearing = math.tau * i / 5 + .18
        turn = math.degrees(bearing)
        paint.elements.append(f'<g transform="translate({x} {y}) rotate({turn})">')
        paint.elements.append('<path d="M 2 -1 Q -15 -13 -13 -28 Q -7 -38 4 -35 '
                              'Q 14 -32 12 -22 Q 10 -11 2 -1" fill="#ce564b" opacity=".29"/>')
        for row, count in ((10, 2), (16, 3), (22, 3), (28, 2)):
            for j in range(count):
                sx = (j - (count - 1) * .5) * (3.7 + row * .11) + paint.rng.uniform(-.7, .8)
                sy = -row + paint.rng.uniform(-1.1, 1.1)
                paint.dab(sx + .38, sy + .5, 2.02, 2.64, 1, .75, '#c86445')
                paint.dab(sx, sy, 1.65, 2.24, 3, .98, '#f8d38b')
                paint.dab(sx - .29, sy - .49, .62, .90, 3, .58, '#fff0c2')
        paint.elements.append('</g>')
    paint.dab(x - .2, y - .4, 1.9, 2.3, 3, .57, '#ffd3ae')


def mango_sections(paint):
    x, y = MANGO_SECTION_CENTER
    radius = MANGO_SECTION_RADIUS
    seed_x, seed_y = MANGO_SEED_CENTER
    a, b = MANGO_SEED_RADII
    if paint.relief:
        paint.dab(x, y, radius + 4, radius + 4, 1, 1)
        paint.dab(seed_x, seed_y, a + 4, b + 4, 1, 1)
    else:
        paint.definitions.append('<radialGradient id="mangoFlesh" cx=".37" cy=".40">'
                                 '<stop stop-color="#ffb623"/><stop offset=".78" stop-color="#ef8c0c"/>'
                                 '<stop offset=".91" stop-color="#ffbd2c"/><stop offset="1" stop-color="#d47419"/>'
                                 '</radialGradient>')
        paint.dab(x, y, radius + 4, radius + 4, 2, 1, 'url(#mangoFlesh)')
        paint.dab(seed_x, seed_y, a + 4, b + 4, 1, 1, '#cda16a')
    for i in range(18):
        angle = math.tau * i / 18
        dx, dy = math.cos(angle), math.sin(angle)
        paint.stroke((x + 18 * dx, y + 18 * dy, x + 29 * dx - 3 * dy, y + 29 * dy + 3 * dx,
                      x + 39 * dx, y + 39 * dy), 2, .73, .20)
    for i in range(15):
        o = (i - 7) / 8
        width = 37 * math.sqrt(1 - o * o)
        line = seed_y + o * 24
        color = None if paint.relief else ('#ad825a', '#edd0a1')[i % 2]
        paint.stroke((seed_x - width, line + 1, seed_x - width * .44, line - 4,
                      seed_x + width * .45, line + 2, seed_x + width, line - 1),
                     i % 2 + 2, .76 + i % 3 * .26, .66, color)


def orange_section(paint):
    x, y = ORANGE_SECTION_CENTER
    radius = ORANGE_SECTION_RADIUS
    left, top, right, bottom = ORANGE_SEED_RECT
    if paint.relief:
        paint.dab(x, y, radius + 3, radius + 3, 1, 1)
        paint.elements.append(f'<rect x="{left}" y="{top}" width="{right-left}" height="{bottom-top}" fill="#808080"/>')
        return

    paint.definitions.extend((
        '<radialGradient id="citrusPulp"><stop stop-color="#ffb43c"/>'
        '<stop offset=".72" stop-color="#f78d16"/><stop offset="1" stop-color="#ffba40"/></radialGradient>',
        '<linearGradient id="citrusSeed" x1="0" y1="0" x2=".22" y2="1" gradientUnits="objectBoundingBox">'
        '<stop stop-color="#c6a678"/><stop offset=".46" stop-color="#e9ce9a"/>'
        '<stop offset="1" stop-color="#f7e4bc"/></linearGradient>',
    ))
    paint.elements.append(f'<rect x="{left-2}" y="{top-2}" width="{right-left+4}" height="{bottom-top+4}" '
                          'fill="url(#citrusSeed)"/>')
    paint.dab(x, y, radius + 3, radius + 3, 2, 1, '#f08718')
    paint.dab(x, y, radius - .7, radius - .7, 3, 1, '#ffe0a0')
    for index in range(10):
        turn = 5 + index * 36
        paint.elements.append(f'<g transform="translate({x} {y}) rotate({turn})">')
        paint.elements.append('<path d="M 0 -4 Q -7 -12 -12 -36 Q -12 -41 -5 -43 '
                              'Q 5 -45 12 -39 Q 14 -36 10 -28 Q 4 -12 0 -4 Z" fill="url(#citrusPulp)"/>')
        for row, count in ((13, 2), (20, 3), (27, 4), (35, 5), (40, 4)):
            for tooth in range(count):
                px = (tooth - (count - 1) * .5) * (2.2 + .06 * row) + paint.rng.uniform(-.85, .85)
                py = -row + paint.rng.uniform(-2, 2)
                paint.dab(px, py, .71, 1.40, 3, .42, '#ffe186')
        paint.elements.append('</g>')
    paint.dab(x, y, 4.3, 3.75, 3, 1, '#ffe6ad')


RIPE_SECTIONS = {
    'guava': (GUAVA_PEEL_WIDTH, guava_section),
    'mango': (MANGO_PEEL_WIDTH, mango_sections),
    'orange': (ORANGE_PEEL_WIDTH, orange_section),
}


def rambutan_hair_paths(length, bend, radius, hook):
    """Build tapered contours for the shadowed edge, raised shaft and lit crest."""
    contours = [[], [], []]
    for index in range(15):
        t = index / 14
        a = 1 - t
        x = bend * (1.94 * a * t * t + t**3)
        y = -length * (1.46 * a*a*t + 3.13 * a*t*t + hook * t**3)
        dx = bend * (3.88 * a*t + 1.06 * t*t)
        dy = -length * (1.46 * a*a + 3.34 * a*t + (3 * hook - 3.13) * t*t)
        slope = math.hypot(dx, dy)
        width = radius * a**.79 * (.90 + .37 * t)
        ux, uy = -dy / slope * width, dx / slope * width
        for contour, (outer, inner) in zip(contours, ((1, -1), (.78, -.61), (.45, -.18))):
            contour.append(((x + ux * outer, y + uy * outer), (x + ux * inner, y + uy * inner)))

    outlines = []
    for contour in contours:
        left, right = zip(*contour)
        outlines.append(' '.join(f'{x:.2f},{y:.2f}' for x, y in (*left, *reversed(right))))
    return outlines


def rambutan_skin(paint):
    paint.ground(((0, 0), (.18, 1), (.39, 2), (.72, 1), (1, 0)))
    paint.wash(76, 100, 123, 152, 2, .71, wrap=True)
    paint.wash(184, 155, 98, 109, 0, .46, wrap=True)
    rng = random.Random('rambutan peel grain')
    for _ in range(73):
        x, y = rng.uniform(0, 256), rng.uniform(-9, 265)
        width, height = rng.uniform(4, 21), rng.uniform(4, 16)
        paint.wash(x, y, width, height, rng.choice((0, 2, 3)), rng.uniform(.13, .48), wrap=True)
    for _ in range(1060):
        x, y = rng.uniform(0, 256), rng.uniform(-2, 258)
        size = rng.uniform(.37, 1.28)
        tone = rng.choice((0, 2, 3))
        for xx in wrapped(x, size):
            paint.dab(xx, y, size, size * .76, tone, rng.uniform(.08, .24))


def rambutan_hairs(paint, ripe):
    rng = random.Random('rambutan peel bristles')
    if paint.relief:
        edge = ('#858585', '#939393', '#999999')
        shaft = ('#929292', '#a8a8a8', '#a1a1a1')
        crest = ('#a8a8a8', '#c1c1c1', '#acacac')
        root_dark, root_mid, root_light = '#747474', '#939393', '#b3b3b3'
    elif ripe:
        edge = ('#7e1125', '#a12c27', '#766832')
        shaft = ('#ad152a', '#c52d31', '#b9a064')
        crest = ('#d52839', '#dc413b', '#e4cb8b')
        root_dark, root_mid, root_light = '#510c20', '#aa1a2b', '#ed4c4e'
    else:
        edge = ('#4b5620', '#6b7828', '#777638')
        shaft = ('#60752a', '#8c9a3e', '#aeac5a')
        crest = ('#859434', '#a8b14a', '#d0c275')
        root_dark, root_mid, root_light = '#414c1d', '#728536', '#adb651'

    for name, colors in (('hairEdge', edge), ('hairShaft', shaft), ('hairCrest', crest)):
        stops = ''.join(f'<stop offset="{offset}" stop-color="{color}"/>'
                        for offset, color in zip((0, .82, 1), colors))
        paint.definitions.append(f'<linearGradient id="{name}" x1="0" y1="1" x2="0" y2="0">'
                                 + stops + '</linearGradient>')

    hairs = []
    for row in range(23):
        for column in range(26):
            x = ((column + .5 * (row % 2)) * 256 / 26 + rng.uniform(-4.4, 4.4)) % 256
            y = (row + .5) * 256 / 23 + rng.uniform(-5.1, 5.1)
            length = rng.uniform(9, 17) if rng.random() < .49 else rng.uniform(17, 29)
            radius = rng.uniform(.68, 1.20) + length * .017
            flow = 21 * math.sin(x * math.tau / 128 + y * .047)
            hairs.append((x, y, length, rng.uniform(2, 11) * rng.choice((-1, 1)),
                          radius, flow + rng.uniform(-118, 118), rng.uniform(.64, 1.03)))

    # Place all follicles first so the ends of neighbouring hairs can overlap them.
    for x, y, _, _, radius, _, _ in hairs:
        for xx in wrapped(x, radius * 3.5):
            paint.dab(xx + .63, y + .83, radius * 2.65, radius * 1.98, 0, .51, root_dark)
            paint.dab(xx - .13, y - .26, radius * 2.0, radius * 1.66, 1, .88, root_mid)
            paint.dab(xx - .84, y - .87, radius * 1.40, radius * 1.04, 2, .61, root_light)

    for x, y, length, bend, radius, angle, hook in sorted(hairs, key=lambda hair: hair[1]):
        body, shaft, highlight = rambutan_hair_paths(length, bend, radius, hook)
        for xx in wrapped(x, length + abs(bend)):
            if not paint.relief:
                paint.elements.append(f'<polygon points="{body}" fill="{root_dark}" opacity=".37" '
                                      f'transform="translate({xx+.94:.2f} {y+1.51:.2f}) '
                                      f'scale(.87 1) rotate({angle:.2f})"/>')
            paint.elements.append(f'<g transform="translate({xx:.2f} {y:.2f}) scale(.87 1) rotate({angle:.2f})">')
            paint.elements.append(f'<polygon points="{body}" fill="url(#hairEdge)"/>')
            paint.elements.append(f'<polygon points="{shaft}" fill="url(#hairShaft)"/>')
            paint.elements.append(f'<polygon points="{highlight}" fill="url(#hairCrest)" opacity=".80"/>')
            paint.elements.append('</g>')


def rambutan_swatch(paint, ripe):
    width = RAMBUTAN_PEEL_WIDTH
    paint.elements.append(f'<rect width="256" height="256" fill="{paint.palette[1]}"/>')
    paint.elements.append(f'<g transform="scale({width} 1)">')
    rambutan_skin(paint)
    rambutan_hairs(paint, ripe)
    paint.elements.append('</g>')
    if paint.relief or not ripe:
        return

    x, y = RAMBUTAN_SECTION_CENTER
    radius = RAMBUTAN_SECTION_RADIUS
    paint.definitions.extend((
        '<radialGradient id="aril" cx=".43" cy=".29" r=".70"><stop stop-color="#fff6dc"/>'
        '<stop offset=".48" stop-color="#f2e2bf"/><stop offset="1" stop-color="#d6bc93"/></radialGradient>',
        '<linearGradient id="rambutanSeed" x2=".18" y2="1"><stop stop-color="#b78d55"/>'
        '<stop offset=".45" stop-color="#d8b481"/><stop offset="1" stop-color="#e8c898"/></linearGradient>',
    ))
    paint.dab(x, y, radius + 3, radius + 3, 1, 1, '#bc3b28')
    paint.dab(x, y, radius - 3, radius - 3, 1, 1, '#df815c')
    paint.dab(x, y, radius - 8, radius - 8, 1, 1, 'url(#aril)')
    paint.wash(x - 13, y - 19, 19, 10, 3, .19)
    left, top, right, bottom = RAMBUTAN_SEED_RECT
    paint.elements.append(f'<rect x="{left-2}" y="{top-2}" width="{right-left+4}" height="{bottom-top+4}" '
                          'fill="url(#rambutanSeed)"/>')
    for row in range(6):
        sy = top + 5 + row * 7
        paint.stroke((left + 1, sy, left + 26, sy - 3, right - 10, sy + 3, right - 1, sy - 1),
                     1, .8, .45, '#a4784a')


def durian_peel(paint, ripe):
    paint.elements.append(f'<rect x="-24" y="-24" width="304" height="304" fill="{paint.palette[1]}"/>')
    paint.elements.append(f'<g transform="scale({DURIAN_PEEL_WIDTH} 1)">')
    paint.ground(((0, 2), (.32, 1), (.66, 1), (1, 0)))
    for _ in range(122):
        x, y = paint.rng.uniform(0, 256), paint.rng.uniform(-3, 259)
        size = paint.rng.uniform(.64, 5.2)
        tone = paint.rng.choice((0, 2, 3))
        for xx in wrapped(x, size):
            paint.dab(xx, y, size, size * .82, tone, paint.rng.uniform(.07, .23))
    paint.elements.append('</g>')
    if not ripe:
        wood = Swatch('Durian wood', ('#543e29', '#805c3d', '#a87d53', '#c39a69'), paint.relief)
        orchard_wood(wood, 'Bark')
        if not paint.relief:
            wood.wash(65, 224, 117, 67, 1, .38, wrap=True)
            for _ in range(50):
                x, y = wood.rng.uniform(0, 256), wood.rng.uniform(197, 254)
                width = wood.rng.uniform(1.0, 4.5)
                for xx in wrapped(x, width):
                    wood.dab(xx, y, width, width * .49, 1, .33, '#6a663e')
        paint.definitions.extend(definition.replace('id="', 'id="durianBark-') for definition in wood.definitions)
        left, right = DURIAN_BARK_UV
        paint.elements.append(f'<g transform="translate({left * 256} 0) scale({right-left} 1)">')
        paint.elements.extend(element.replace('url(#', 'url(#durianBark-') for element in wood.elements)
        paint.elements.append('</g>')
        return

    if paint.relief:
        return
    x, y = DURIAN_SECTION_CENTER
    radius = DURIAN_SECTION_RADIUS
    paint.dab(x, y, radius + 4, radius + 4, 0, 1, '#81753d')
    paint.dab(x, y, radius - 1, radius - 1, 1, 1, '#d2b97b')
    paint.dab(x, y, radius - 5, radius - 5, 3, 1, '#f0deb0')
    paint.dab(x + 2, y + 1, radius - 12, radius - 10, 3, 1, '#e0c78f')
    for offset in (-16, 14):
        paint.stroke((x + offset, y - 33, x + offset - 4, y + 2, x + offset + 5, y + 31),
                     3, 2.2, .82, '#fff0ce')
    for name, rect, tones in (
        ('durianPulp', DURIAN_PULP_RECT, ('#edc255', '#dfa137', '#bf771e')),
        ('durianSeed', DURIAN_SEED_RECT, ('#bc804b', '#9f5f31', '#713c22')),
    ):
        left, top, right, bottom = rect
        stops = ''.join(f'<stop offset="{at}" stop-color="{tone}"/>' for at, tone in zip((0,.50,1), tones))
        paint.definitions.append(f'<linearGradient id="{name}" x2=".32" y2="1">{stops}</linearGradient>')
        paint.elements.append(f'<rect x="{left-2}" y="{top-2}" width="{right-left+4}" height="{bottom-top+4}" '
                              f'fill="url(#{name})"/>')
        tone = '#f6d079' if name == 'durianPulp' else '#d19557'
        for _ in range(38):
            x, y = paint.rng.uniform(left + 2, right - 17), paint.rng.uniform(top + 3, bottom - 6)
            length = paint.rng.uniform(7, 14)
            paint.stroke((x, y, x + length * .61, y - 1.5, x + length, y + 1.7), 3, .85, .30, tone)


def pear_leaf(paint):
    def margin(t, side):
        width = 104 * math.sin(math.pi * t)**.91 * (.73 + .55 * t)
        return 126 + 12 * math.sin(math.pi * t) + side * width, 12 + 220 * t

    # The entire silhouette, including the teeth, comes from the texture's alpha.
    edges = []
    for side in (-1, 1):
        points = [margin(0, side)]
        for index in range(1, 14):
            t = (index - (.18 if side == 1 else 0)) / 14
            x, y = margin(t, side)
            tooth = 5.8 * math.sin(math.pi * t)**.68 * (1 + .18 * math.sin(2.65 * index + side))
            before = margin(t - .023, side)
            points.extend(((before[0] - side * tooth * .29, before[1]),
                           (x + tooth * side, y - 2.0), (x - tooth * side * .57, y + 4.8)))
        edges.append(points + [margin(1, side)])
    points = [*edges[0][:-1], *reversed(edges[1][1:])]
    outline = f'M{points[0][0]:.2f},{points[0][1]:.2f}'
    for index, (x, y) in enumerate(points[1:], 1):
        if index == len(edges[0]) - 1:
            outline += f'L{x:.2f},{y:.2f}'
            continue
        x0, y0 = points[index - 1]
        x1, y1 = points[(index + 1) % len(points)]
        outline += (f'L{x + (x0 - x) * .21:.2f},{y + (y0 - y) * .21:.2f}'
                    f'Q{x:.2f},{y:.2f} {x + (x1 - x) * .21:.2f},{y + (y1 - y) * .21:.2f}')
    paint.definitions.append(f'<clipPath id="pear-blade"><path d="{outline}Z"/></clipPath>')
    if paint.relief:
        paint.ground(((0, 1), (1, 1)))
    paint.elements.append('<g clip-path="url(#pear-blade)">')
    paint.ground(((0, 0), (.22, 1), (.54, 1), (.90, 1), (1, 0)))
    paint.wash(108, 132, 71, 103, 2, .88)
    paint.wash(198, 149, 68, 117, 0, .64)
    paint.wash(129, 76, 25, 62, 3, .39)
    paint.wash(107, 187, 43, 38, 3, .25)
    for _ in range(150):
        x, y = paint.rng.uniform(25, 230), paint.rng.uniform(21, 229)
        paint.wash(x, y, paint.rng.uniform(2.4, 10), paint.rng.uniform(4, 15),
                   paint.rng.choice((0, 2)), paint.rng.uniform(.05, .14))
    for side in (-1, 1):
        for index in range(9):
            t = .24 + index * .08 + paint.rng.uniform(-.021, .021)
            root_x, y = margin(t, 0)
            end_x, end_y = margin(t - .164, side)
            reach = (end_x - root_x) * (.88 + .035 * math.cos(index * 1.3))
            end_x = root_x + reach
            controls = (root_x, y, root_x + reach * .46, y - 11, end_x - reach * .10, end_y + 16, end_x, end_y)
            paint.stroke(controls, 0, 5, .16)
            paint.stroke(controls, 0, 2.25, .48)
            color = tuple(value - 1.05 if i % 2 else value for i, value in enumerate(controls))
            paint.stroke(color, 3, 1.38, .48)
            paint.stroke((root_x, y, root_x + reach * .25, y - 5, root_x + reach * .56, y - 15),
                         3, 1.55, .40)
            for offset in (.53, .73):
                x = root_x + reach * offset
                start_y = y + (end_y - y) * offset + 4.0 * math.sin(math.pi * offset)
                paint.stroke((x, start_y, x + reach * .13, start_y - 4, x + reach * .18, start_y - 12),
                             2, .82, .40)
    rib = (127, 233, 140, 178, 142, 85, 126, 12)
    paint.stroke(rib, 0, 5.6, .33)
    paint.stroke(rib, 3, 2.45, .72)
    paint.stroke((127, 233, 134, 207, 136, 159), 3, 3.60, .74)
    paint.stroke((136, 150, 139, 106, 133, 58, 126, 16), 2, 1.25, .57)
    paint.elements.append('</g>')
    paint.stroke((128, 248, 124, 241, 127, 231), 1, 5.2, 1)
    paint.stroke((127, 248, 123, 240, 127, 231), 2, 1.5, .59)


def pear_swatch(paint, ripe):
    paint.ground(((0, 1), (1, 1)))
    paint.elements.append(f'<g transform="scale({PEAR_PEEL_WIDTH} 1)">')
    peel(paint, 'pear', ripe)
    paint.elements.append('</g>')
    if not ripe:
        left, right = PEAR_BARK_UV
        x, width = (left - .012) * 256, (right - left + .036) * 256
        bark = Swatch('Pear bark', ('#4b2d1e', '#714726', '#996338', '#af784a'), paint.relief)
        orchard_wood(bark, 'Bark')
        # The peel and the bark each keep their own horizontal UV seam and gutters.
        paint.definitions.extend(definition.replace('id="ground"', 'id="bark-ground"')
                                 .replace('id="wash', 'id="bark-wash') for definition in bark.definitions)
        body = ''.join(bark.elements).replace('url(#ground)', 'url(#bark-ground)').replace('url(#wash', 'url(#bark-wash')
        paint.definitions.append(f'<clipPath id="pear-bark"><rect x="{x}" y="-15" width="{width}" height="286"/></clipPath>')
        paint.elements.append(f'<g clip-path="url(#pear-bark)"><g transform="translate({left * 256}) scale({right - left} 1)">'
                              f'{body}</g></g>')
        return

    if paint.relief:
        return
    x, y = PEAR_SECTION_CENTER
    across, rise = PEAR_SECTION_SCALE
    points = [(x + side * radius * across, y - z * rise) for side in (-1, 1)
              for z, radius in (PEAR_PROFILE if side == -1 else reversed(PEAR_PROFILE))]
    outline = ' '.join(f'{px:.2f},{py:.2f}' for px, py in points)
    paint.elements.append(f'<polygon points="{outline}" fill="#e7c958" stroke="#cfa742" stroke-width="4"/>'
                          f'<polygon points="{outline}" transform="translate({x * .05} {y * .05}) scale(.95)" fill="#f1dfb5"/>')
    paint.wash(x - 8, y, 37, 46, 3, .32)
    paint.dab(x, y + 10, 13, 19, 0, .19, '#b7955c')
    paint.stroke((x, y - 54, x - 2, y - 4, x + 5, y + 34), 0, 1, .30, '#b6976f')
    for side in (-1, 1):
        seed_x = x + 5 * side
        paint.dab(seed_x, y + 12, 2.50, 6.2, 0, .95, '#785136')
        paint.dab(seed_x - .55, y + 10, .82, 2.8, 2, .67, '#bd8c57')


def banana_green_fruit(paint):
    paint.elements.append(f'<g transform="scale({BANANA_GREEN_PEEL_WIDTH} 1)">')
    peel(paint, 'banana', False)
    paint.elements.append('</g>')
    left, right = BANANA_BLOSSOM_UV
    x = (left - .029) * 256
    width = (right - left + .10) * 256
    if paint.relief:
        fill = paint.palette[1]
    else:
        paint.definitions.append('<linearGradient id="banana-bud" x2=".45" y2="1">'
                                 '<stop stop-color="#6d3a59"/><stop offset=".46" stop-color="#883e67"/>'
                                 '<stop offset="1" stop-color="#4f293f"/></linearGradient>')
        fill = 'url(#banana-bud)'
    paint.elements.append(f'<rect x="{x}" y="-17" width="{width}" height="290" fill="{fill}"/>')
    for i in range(8):
        u = left * 256 + 9.8 * i
        curve = (u, 259, u + 12, 186, u - 12, 73, u + 4, -3)
        paint.stroke(curve, 0, 2.8, .28, None if paint.relief else '#4d253e')
        paint.stroke(tuple(value + 1.2 if j % 2 == 0 else value for j, value in enumerate(curve)),
                     2, 1.25, .36, None if paint.relief else '#b66d89')


def oil_palm_surface(paint, surface):
    rng = paint.rng
    if surface == 'Leaf':
        paint.ground(((0, 2), (.42, 2), (.78, 1), (1, 0)))
        paint.wash(65, 92, 57, 127, 3, .48)
        paint.wash(188, 130, 63, 130, 1, .65)
        paint.wash(24, 176, 23, 123, 0, .51)
        for y in range(34, 249, 11):
            for side in (-1, 1):
                sweep = rng.uniform(26, 44)
                end = 128 + side * 115
                stroke = (128, y, 128 + side * 45, y - 11, end, y - sweep)
                paint.stroke(stroke, 0, 1.6, .15)
                paint.stroke(tuple(p - .8 if i % 2 else p for i, p in enumerate(stroke)), 3, .9, .21)
        paint.stroke((128, 256, 125, 158, 130, 81, 128, 0), 0, 5.4, .44)
        paint.stroke((127, 256, 124, 158, 129, 81, 127, 0), 3, 2.6, .74)
        return

    if surface == 'Bark':
        paint.ground(((0, 1), (.22, 2), (.56, 1), (1, 0)))
        paint.wash(82, 93, 46, 86, 2, .65)
        paint.wash(27, 177, 22, 66, 0, .71)
        for _ in range(95):
            x, y = rng.uniform(-3, 151), rng.uniform(-14, 254)
            width, length = rng.uniform(.6, 3.7), rng.uniform(10, 62)
            curve = (x, y, x + rng.uniform(-10, 10), y + length * .52, x + rng.uniform(-7, 7), y + length)
            paint.stroke(curve, 0, width, .44)
            lip = tuple(coord - 2 if i % 2 == 0 else coord for i, coord in enumerate(curve))
            paint.stroke(lip, 2, width * .56, .46)
        # A separate patch supplies the lighter exposed cut on each old leaf base.
        cut = paint.palette[3]
        paint.elements.append(f'<rect x="163" y="-16" width="110" height="288" fill="{cut}"/>')
        paint.wash(210, 109, 43, 55, 2, .65)
        paint.wash(241, 205, 32, 58, 1, .76)
        for _ in range(29):
            x, y = rng.uniform(164, 260), rng.uniform(-5, 260)
            run = rng.uniform(22, 54)
            paint.stroke((x, y, x-6, y+run*.54, x-19, y+run), 0,
                         rng.uniform(.5, 1.35), rng.uniform(.18, .46))
        for i in range(9):
            x = 186 + i * 6
            paint.stroke((x-6, 214, x+5, 166, x-1, 121, x-8, 57), 1, rng.uniform(1.1, 2.9), .25)
        for _ in range(59):
            x, y = rng.uniform(163, 261), rng.uniform(-14, 254)
            run = rng.uniform(5, 30)
            paint.stroke((x, y, x + 4, y + run * .52, x + rng.uniform(-8, 7), y + run),
                         rng.choice((0, 1, 2)), rng.uniform(.53, 1.77), rng.uniform(.16, .44))
        return

    # Oil-palm fruits carry a dark tip, a red shoulder, and warmer pigment toward the attachment.
    paint.ground(((0, 0), (.13, 0), (.37, 1), (.68, 2), (.94, 3), (1, 3)))
    paint.wash(70, 153, 97, 82, 2, .42, wrap=True)
    paint.wash(174, 75, 79, 84, 0, .37, wrap=True)
    for _ in range(32):
        x, y = rng.uniform(0, 256), rng.uniform(41, 245)
        for xx in wrapped(x, 2):
            paint.dab(xx, y, rng.uniform(.47, 1.5), rng.uniform(.6, 2.3), 0, .24)


def draw_surface(slot, pattern, palette, relief=False):
    stage = slot.partition('/')[2]
    accent = stage == 'LeafAccent'
    # Color variants of a surface share the same underlying brushwork and relief.
    paint = Swatch(slot.split('/')[0] if stage else slot, palette, relief)
    if pattern == 'oil_palm':
        oil_palm_surface(paint, stage)
    elif stage in {'Leaf', 'LeafAccent'}:
        if pattern == 'pear':
            pear_leaf(paint)
        else:
            leaf(paint, pattern, accent)
    elif stage in {'FruitForming', 'FruitReady'}:
        ripe = stage == 'FruitReady'
        if pattern == 'rambutan':
            rambutan_swatch(paint, ripe)
        elif pattern == 'durian':
            durian_peel(paint, ripe)
        elif pattern == 'pear':
            pear_swatch(paint, ripe)
        elif pattern == 'banana' and not ripe:
            banana_green_fruit(paint)
        elif ripe and pattern in RIPE_SECTIONS:
            width, section = RIPE_SECTIONS[pattern]
            paint.elements.append(f'<rect x="-24" y="-24" width="304" height="304" fill="{paint.palette[1]}"/>')
            paint.elements.append(f'<g transform="scale({width} 1)">')
            peel(paint, pattern, True)
            paint.elements.append('</g>')
            section(paint)
        else:
            peel(paint, pattern, ripe)
    elif slot == 'DragonFruitCane':
        paint.ground(((0, 1), (.35, 2), (.78, 1), (1, 0)))
        cactus(paint, False)
    elif slot in {'RambutanBristle', 'DurianSpine', 'DragonFruitScale'}:
        detail(paint, slot)
    elif slot == 'CutWood':
        cut_wood(paint)
    elif slot == 'AppleSeed':
        brown_seed(paint)
    elif slot in {'Bark', 'Twig'}:
        orchard_wood(paint, slot)
    elif slot == 'BananaSheath':
        banana_sheath(paint)
    else:
        bark(paint, slot)
    return paint.svg()
