"""Refresh only Daisy's phase row and icon artwork, then publish the texture comparison."""
from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageFont
from crop_extension_config import ART
from unified_plant_atlas import SOURCE, load, write


def refresh(folder):
    previews = SOURCE / 'Previews'
    phase_path = previews / 'G14_DaisyMoonflower_Preview.png'
    with Image.open(phase_path) as original, Image.open(folder/'after/Daisy_phases.png') as daisy:
        phases = original.copy()
        phases.paste(daisy.convert('RGB'), (0,56))
    phases.save(phase_path)

    icon_path = ART / 'Generated/G14_Illustrations.png'
    cells_path = ART / 'Generated/G14_AtlasCells.json'
    cells = load(cells_path)
    with Image.open(icon_path) as original, Image.open(folder/'after/Daisy_harvest.png') as daisy:
        icons = original.copy()
        x,y,w,h = cells['cells'][0]['pixelRectTopLeft']
        assert daisy.size == (w,h) and cells['cells'][0]['key']=='daisy'
        bounds = daisy.getchannel('A').getbbox()
        assert bounds and 0 < bounds[0] < bounds[2] < w and 0 < bounds[1] < bounds[3] < h
        icons.paste(daisy, (x,y))
    icons.save(icon_path)
    cells['cells'][0]['contentRectInCell'] = bounds
    write(cells_path, cells)

    sheet = Image.new('RGB', (1280, 1134), '#f4f1e9')
    draw = ImageDraw.Draw(sheet)
    title = ImageFont.truetype('C:/Windows/Fonts/georgiab.ttf', 32)
    label = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 23)
    draw.text((36,16), 'Daisy', font=title, fill='#2d4530')
    for x, column, caption in [(36,'before','Sebelum'),(652,'after','Sesudah')]:
        draw.text((x,59), caption, font=label, fill='#2d4530')
        for y, view in [(96,'Flower'),(552,'Leaves')]:
            with Image.open(folder/column/f'Daisy_{view}.png') as rendered:
                sheet.paste(rendered.convert('RGB').resize((592,444),Image.Resampling.LANCZOS),(x,y))
        with Image.open(folder/column/'Daisy_Field.png') as field:
            sample = field.crop((836,486,1084,606)).convert('RGB')
            sheet.paste(sample,(x+344,1002))
        draw.text((x,1046), 'Skala kamera Map', font=label, fill='#2d4530')
    sheet.save(previews / 'G14_Daisy_R02_Detail.png')


if __name__ == '__main__':
    refresh(Path(sys.argv[1]))
