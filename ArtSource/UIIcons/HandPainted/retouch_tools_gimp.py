"""Restore the approved original tool designs and apply a restrained GIMP retouch.

Original silhouettes, proportions, surface detail and sprite GUIDs are retained.
Each editable XCF contains the untouched source and the visible retouch layer.
"""
from pathlib import Path
import json
import re
from gi.repository import Gimp, Gegl, Gio

ROOT=Path(__file__).resolve().parents[3]
ART=Path(__file__).resolve().parent
ICONS=ROOT/'Assets/Nature  Paradaise/Textures/UI/Icons'
TOOLS=['Hoe','Axe','Hammer','Pickaxe','Pitchfork','Sickle','WateringCan','FishingRod','Shears','Brush','Milker']


def export_png(image,path):
    proc=Gimp.get_pdb().lookup_procedure('file-png-export')
    config=proc.create_config()
    for key,value in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,
                          file=Gio.File.new_for_path(str(path)),compression=9,
                          include_color_profile=False).items():
        config.set_property(key.replace('_','-'),value)
    assert proc.run(config).index(0)==Gimp.PDBStatusType.SUCCESS


Gimp.context_push()
try:
    Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
    preview=Gimp.Image.new(1024,768,Gimp.ImageBaseType.RGB)
    bg=Gimp.Layer.new(preview,'Dark sage background',1024,768,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL)
    preview.insert_layer(bg,None,0)
    Gimp.context_set_foreground(Gegl.Color.new('#29392f'));bg.edit_fill(Gimp.FillType.FOREGROUND)
    manifest=json.loads((ART/'Manifest.json').read_text())
    revised=[]
    for index,name in enumerate(TOOLS):
        snake=re.sub(r'([a-z0-9])([A-Z])',r'\1_\2',name).lower()
        source=ART.parent/'Originals'/(name+'.png')
        # The generic Milker source is an older bucket illustration. The approved
        # runtime icon is the hand milker with hose, recovered without changing its shape.
        if name=='Milker':source=ART.parent/'Originals/Milker_Approved.png'
        image=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(source)))
        original=image.get_layers()[0];original.set_name('Original approved artwork - untouched')
        layer=Gimp.Layer.new_from_drawable(original,image)
        image.insert_layer(layer,None,0);layer.set_name('Retouch - saturation minus 8 / softened highlights')
        original.set_visible(False)
        assert layer.hue_saturation(Gimp.HueRange.ALL,0,0,-8,0)
        assert layer.curves_spline(Gimp.HistogramChannel.VALUE,[0,0,.25,.255,.5,.5,.75,.73,1,.92])
        master=ART/('Retouched_'+name+'.xcf')
        assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,image,Gio.File.new_for_path(str(master)))
        image.scale(128,128)
        image.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
        destination=ICONS/'Tools'/('icon_tool_'+snake+'.png')
        export_png(image,destination)
        revised.append(dict(name='Tool_'+name,group='Tools',file=str(destination.relative_to(ROOT)),
                            master=str(master.relative_to(ROOT)),method='Original artwork with subtle native GIMP color retouch'))
        image.scale(220,220)
        copy=Gimp.Layer.new_from_drawable(image.get_layers()[0],preview)
        preview.insert_layer(copy,None,0);copy.set_offsets((index%4)*256+18,(index//4)*256+18)
        image.delete()
        print('GIMP_TOOL_RETOUCHED '+name,flush=True)
    reference=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(ICONS/'FertilizerMaker/icon_fertilizer_basic.png')))
    reference.scale(220,220)
    copy=Gimp.Layer.new_from_drawable(reference.get_layers()[0],preview)
    preview.insert_layer(copy,None,0);copy.set_name('Existing fertilizer - style reference');copy.set_offsets(786,530)
    reference.delete()
    export_png(preview,ART/'RetouchedTools_Preview.png');preview.delete()
    manifest['renderer']='Original tool artwork retouched in GIMP; native GIMP chemical artwork'
    manifest['icons']=revised+[entry for entry in manifest['icons'] if entry['group']!='Tools']
    (ART/'Manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print('GIMP_RETOUCH_PASS tools='+str(len(revised)),flush=True)
finally:
    Gimp.context_pop()
