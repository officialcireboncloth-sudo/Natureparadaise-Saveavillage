"""Split, trim and normalize the generated artwork using GIMP 3.

Run with gimp-console --new-instance --no-data --no-fonts
--batch-interpreter=python-fu-eval --batch=<exec script> --quit.
"""
from pathlib import Path
import json
from gi.repository import Gimp, Gio

root = Path(__file__).resolve().parents[2]
source = root / 'ArtSource/UIIcons/Originals/FertilizerMaker_Sheet.png'
dest = root / 'Assets/Nature  Paradaise/Textures/UI/Icons/FertilizerMaker'
dest.mkdir(parents=True, exist_ok=True)
names = ['basic', 'plus', 'super', 'premium', 'fish', 'manure', 'shell', 'mineral']
Gimp.context_push()
try:
    Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
    sheet = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(str(source)))
    width, height = sheet.get_width(), sheet.get_height()
    master = Gimp.Image.new(width, height, Gimp.ImageBaseType.RGB)
    report = []
    for index, name in enumerate(names):
        col, row = index % 4, index // 4
        x0, x1 = round(col * width / 4), round((col + 1) * width / 4)
        y0, y1 = round(row * height / 2), round((row + 1) * height / 2)
        image = sheet.duplicate()
        image.crop(x1 - x0, y1 - y0, x0, y0)
        layer = image.get_layers()[0]
        image.select_item(Gimp.ChannelOps.REPLACE, layer)
        # Ignore nearly invisible generation residue when finding the silhouette.
        Gimp.Selection.sharpen(image)
        nonempty, left, top, right, bottom = Gimp.Selection.bounds(image)[-5:]
        assert nonempty, name
        Gimp.Selection.none(image)
        image.crop(right - left, bottom - top, left, top)
        # Editable master has one named layer per icon, at the source resolution.
        copy = Gimp.Layer.new_from_drawable(image.get_layers()[0], master)
        master.insert_layer(copy, None, 0)
        copy.set_name(name)
        copy.set_offsets(x0 + left, y0 + top)
        scale = 116 / max(image.get_width(), image.get_height())
        w, h = round(image.get_width() * scale), round(image.get_height() * scale)
        image.scale(w, h)
        image.resize(128, 128, (128 - w) // 2, (128 - h) // 2)
        config = Gimp.get_pdb().lookup_procedure('file-png-export').create_config()
        for key, value in dict(run_mode=Gimp.RunMode.NONINTERACTIVE, image=image,
                               file=Gio.File.new_for_path(str(dest / f'icon_fertilizer_{name}.png')),
                               compression=9, include_color_profile=False).items():
            config.set_property(key.replace('_', '-'), value)
        assert Gimp.get_pdb().lookup_procedure('file-png-export').run(config).index(0) == Gimp.PDBStatusType.SUCCESS
        report.append(dict(name=name, size=[128, 128], content=[w, h], source_bounds=[x0+left, y0+top, right-left, bottom-top]))
        image.delete()
        print('FERTILIZER_ICON_EXPORTED ' + name, flush=True)
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE, master,
                         Gio.File.new_for_path(str(root / 'ArtSource/UIIcons/FertilizerMaker_Icons.xcf')))
    (root / 'ArtSource/FertilizerMaker/Review/IconExport.json').write_text(json.dumps(report, indent=2))
    sheet.delete()
    master.delete()
finally:
    Gimp.context_pop()
