"""Compose the growth-stage proof and transparent inputs for the crop icon pipeline."""
from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageFont
from crop_extension_config import ART
from unified_plant_atlas import SOURCE, write


KINDS = ('Daisy', 'Moonflower')
PHASES = ('Seed', 'Sprout', 'Young plant', 'Bud forming', 'Ready to harvest', 'Harvest')
folder = Path(sys.argv[1])
sheet = Image.new('RGB', (1536, 858), '#f4f1e9')
draw = ImageDraw.Draw(sheet)
title = ImageFont.truetype('C:/Windows/Fonts/georgiab.ttf', 34)
label = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 21)
icons = Image.new('RGBA', (896, 448))
cells = []
for i, kind in enumerate(KINDS):
    y = 429 * i
    draw.text((52, y+13), kind, font=title, fill='#2d4530')
    with Image.open(folder / f'{kind}_phases.png') as rendered:
        sheet.paste(rendered.convert('RGB'), (0,y+56))
    for j, phase in enumerate(PHASES):
        draw.text((round(768+(j-2.5)*1536*1.10/7.04), y+413), phase, font=label,
                  fill='#2d4530', anchor='mm')
    with Image.open(folder / f'{kind}_harvest.png') as art:
        rgba = art.convert('RGBA')
        bbox = rgba.getchannel('A').getbbox()
        assert bbox and all(v > 0 for v in bbox[:2]) and bbox[2] < 448 and bbox[3] < 448, (kind, bbox)
        icons.paste(rgba, (i*448, 0))
        cells.append(dict(key=kind.lower(), pixelRectTopLeft=[i*448,0,448,448], contentRectInCell=bbox))
sheet.save(SOURCE / 'Previews/G14_DaisyMoonflower_Preview.png')
icons.save(ART / 'Generated/G14_Illustrations.png')
write(ART / 'Generated/G14_AtlasCells.json', dict(cells=cells))
