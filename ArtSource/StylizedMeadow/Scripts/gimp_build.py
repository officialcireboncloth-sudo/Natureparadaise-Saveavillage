"""Run with GIMP 3 python-fu-eval; edits stay in this asset's source folder."""
import os, json, traceback
from gi.repository import Gimp, Gio, Gegl
ROOT = os.path.abspath('ArtSource/StylizedMeadow')
OUT = os.path.abspath('Assets/Nature  Paradaise/Art/StylizedMeadow/Textures')

def run(name, **values):
    proc = Gimp.get_pdb().lookup_procedure(name)
    cfg = proc.create_config()
    for key, value in values.items(): cfg.set_property(key.replace('_','-'),value)
    result = proc.run(cfg)
    if result.index(0) != Gimp.PDBStatusType.SUCCESS:
        raise RuntimeError(name + ': ' + str(result.index(0)))
    return result

def save(image, path):
    run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(path))

def export(image, path):
    run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(path))

try:
    raw = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(ROOT+'/Textures/GrassGround_Generated.png'))
    raw.scale(1024,1024)
    base = raw.get_layers()[0];base.set_name('01 Original generated albedo — preserved');base.set_visible(False)
    final = base.copy();raw.insert_layer(final,None,0);final.set_visible(True);final.set_name('02 Seamless grass albedo — editable')
    filt=Gimp.DrawableFilter.new(final,'gegl:tile-seamless','Seamless wrap')
    final.append_filter(filt)
    save(raw,ROOT+'/GIMP/GrassGround.xcf')
    export(raw,OUT+'/GrassGround_Albedo.png')
    export(raw,ROOT+'/Textures/GrassGround_Albedo.png')

    # Eight padded UV lanes: 4 greens, ivory petals, gold buds, stems, dark recess.
    colors=[('#3d7725','#a1c944'),('#316b24','#8fbf38'),('#538728','#b2d452'),('#427a2a','#89b73c'),('#b5c597','#fff7d8'),('#c69e2e','#ffe897'),('#416d26','#9db547'),('#3b6028','#81a849')]
    defs='';rects=''
    for i,(a,b) in enumerate(colors):
        defs+=f'<linearGradient id="g{i}" x1="0" y1="1" x2="0" y2="0"><stop stop-color="{a}"/><stop offset="1" stop-color="{b}"/></linearGradient>'
        rects+=f'<rect x="{i*128}" y="0" width="128" height="1024" fill="url(#g{i})"/>'
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024"><defs>{defs}</defs>{rects}</svg>'
    os.makedirs(ROOT+'/Textures/PaletteSources',exist_ok=True)
    path=ROOT+'/Textures/PaletteSources/LeafPalette.svg';open(path,'w',encoding='utf8').write(svg)
    atlas=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(path))
    atlas.get_layers()[0].set_name('01 Editable leaf / petal / stem gradient lanes')
    # Store every colour lane independently for native GIMP painting and revisions.
    for i in range(8):
        strip_svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024"><defs>{defs}</defs><rect x="{i*128}" y="0" width="128" height="1024" fill="url(#g{i})"/></svg>'
        strip_path=ROOT+f'/Textures/PaletteSources/PaletteLane_{i}.svg';open(strip_path,'w',encoding='utf8').write(strip_svg)
        layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,atlas,Gio.File.new_for_path(strip_path))
        layer.set_name(['Grass lime','Grass fresh','Grass sunlit','Bush green','Ivory petals','Gold buds','Stems','Recess'][i]);atlas.insert_layer(layer,None,0)
    save(atlas,ROOT+'/GIMP/LeafPalette.xcf');export(atlas,OUT+'/LeafPalette_Albedo.png');export(atlas,ROOT+'/Textures/LeafPalette_Albedo.png')
    open(ROOT+'/Previews/GimpBuild.json','w').write(json.dumps({'status':'complete','grass':1024,'paletteLanes':8,'xcf':['GrassGround.xcf','LeafPalette.xcf']}))
except Exception:
    open(ROOT+'/Previews/GimpBuildError.txt','w').write(traceback.format_exc())
    raise
