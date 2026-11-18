"""Bake the durian blade's pigments and tangent-space relief in the existing leaf UVs."""
import os
import sys
from pathlib import Path

import bpy
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fruit_tree_atlas import PADDING, TILE


WORK = Path(os.environ['NP_DURIAN_LEAF_BAKE'])
U, V = np.meshgrid(*([(np.arange(TILE) + .5 - PADDING) / (TILE - 2 * PADDING)] * 2))
RNG = np.random.default_rng(78203)


def noise(columns, rows):
    samples = RNG.random((rows + 1, columns + 1))
    x = np.clip(U, 0, .99999) * columns
    y = np.clip(V, 0, .99999) * rows
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    rx, ry = x - ix, y - iy
    rx, ry = rx * rx * (3 - 2 * rx), ry * ry * (3 - 2 * ry)
    bottom = samples[iy,ix] * (1 - rx) + samples[iy,ix + 1] * rx
    top = samples[iy + 1,ix] * (1 - rx) + samples[iy + 1,ix + 1] * rx
    return bottom * (1 - ry) + top * ry


def gaussian(distance, width):
    return np.exp(-.5 * (distance / width)**2)


def vascular_fields():
    axis = .5 + .0045 * np.sin(V * 5.1 + .22)
    distance = U - axis
    rib_width = .0045 + .0092 * np.clip(V, 0, 1)**.83
    rib = gaussian(distance, rib_width)
    shoulder = gaussian(distance + .0019, rib_width * 2.8)
    lateral, tissue, finer = [np.zeros_like(U) for _ in range(3)]
    for side in (-1, 1):
        for index, root in enumerate((.205,.283,.367,.449,.548,.647,.752,.856,.928)):
            root += RNG.uniform(-.012,.012)
            reach = .385 + .048 * np.sin(index * .39) + RNG.uniform(-.025,.018)
            raw = distance * side / reach
            t = np.clip(raw,0,1)
            # The veins curve to the apex and lose relief as they reach the margin.
            sweep = .082 + .069 * root + RNG.uniform(-.013,.018)
            bend = root - sweep * (.31*t + .69*t**2.1)
            bend += .0036 * np.sin(t * 8.8 + root*11) * t
            width = .00315 * (1 - t)**.72 + .00073
            taper = np.clip((1-raw)*4.0,0,1) * (raw >= -.007)
            d = (V - bend) / np.sqrt(1 + (sweep / reach)**2)
            lateral += gaussian(d, width) * taper
            tissue += (gaussian(d + .013, .020) - .57 * gaussian(d - .013, .016)) * taper
            for start in (.36,.57,.74):
                t2 = np.clip((raw - start) * 3.8,0,1)
                anchor = root - sweep * (.31*start + .69*start**2.1)
                tip = anchor - .033*t2 - .022*t2**2
                mask = ((raw > start) & (raw < start + .25)) * (1-t2)
                finer += gaussian(V-tip,.00145) * mask
    return rib, shoulder, lateral, tissue, finer


def paint_leaf():
    rib, shoulder, lateral, tissue, finer = vascular_fields()
    broad = noise(6,9)
    grain = noise(40,57)
    cuticle = noise(112,118)
    sides = np.abs(U-.5)*2
    blade_tone = .69 + .18*broad - .22*sides**2 - .10*V + .046*(grain-.5)
    reflection = gaussian(U-.34 + .13*(V-.62)**2,.138) * gaussian(V-.34,.34)
    relief = .00165*rib + .00048*shoulder + .00069*lateral + .00013*finer
    relief += .00018*(grain-.5) + .000053*(cuticle-.5) + .00076*tissue

    # The top cuticle scatters a soft band of light, while the bronze underside has fine scales.
    top_light = blade_tone + .083*reflection + .034*tissue + .010*(cuticle-.5)
    underside = blade_tone + .042*reflection + .023*tissue + .042*(cuticle-.5)
    return (pigment(top_light,rib,lateral,finer,False),
            pigment(underside,rib,lateral,finer,True), relief)


def pigment(light, rib, lateral, finer, bronze):
    base, vein = ((.49,.346,.202),(.59,.450,.258)) if bronze else ((.245,.384,.177),(.390,.473,.220))
    base, vein = np.array(base), np.array(vein)
    pigment = light[:,:,None] * base * 1.37
    lift = np.clip(.79*rib + .46*lateral + .12*finer,0,.86)
    tone = (light*.61 + .44)[:,:,None] * vein
    return pigment + (tone - pigment) * lift[:,:,None]


def tangent_normal(relief):
    # V increases downward in this raster, opposite to tangent +Y.
    dy, dx = np.gradient(relief, 1/(TILE-2*PADDING))
    normal = np.stack((-dx/.43,dy,np.ones_like(relief)),axis=2)
    unit = normal / np.linalg.norm(normal,axis=2,keepdims=True)
    return unit*.5 + .5


def save(name, rgb):
    pixels = np.ones((TILE,TILE,4), dtype=np.float32)
    pixels[:,:,:3] = np.clip(rgb,0,1)
    image = bpy.data.images.new(name, width=TILE, height=TILE, alpha=True)
    image.colorspace_settings.name = 'Non-Color'
    image.pixels.foreach_set(pixels[::-1].copy().ravel())
    image.file_format = 'PNG'
    image.filepath_raw = str(WORK / f'{name}.png')
    image.save()
    bpy.data.images.remove(image)


def main():
    WORK.mkdir(parents=True,exist_ok=True)
    green, bronze, relief = paint_leaf()
    save('Leaf_BaseColor', green)
    save('LeafAccent_BaseColor', bronze)
    save('Normal', tangent_normal(relief))
    print('DURIAN_LEAF_BAKE_PASS', flush=True)


if __name__ == '__main__':
    main()
