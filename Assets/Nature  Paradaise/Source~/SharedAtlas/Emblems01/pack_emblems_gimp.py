"""Resize and pack the generated alpha asset; preserve original silhouette and alpha."""
from pathlib import Path
from gi.repository import Gimp,Gio
import json
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise';SOURCE=A/'Source~/SharedAtlas/Emblems01';DEST=A/'Textures/Shared/Cozy'
def run(name,**args):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in args.items():c.set_property(k.replace('_','-'),v)
 result=p.run(c);assert result.index(0)==Gimp.PDBStatusType.SUCCESS,(name,str(result.index(0)))
source=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Cow_Generated_Master.png')))
assert source.get_layers()[0].has_alpha()
source.scale(384,256)
canvas=Gimp.Image.new(512,256,Gimp.ImageBaseType.RGB)
layer=Gimp.Layer.new_from_drawable(source.get_layers()[0],canvas);canvas.insert_layer(layer,None,0)
layer.set_name('Generated Cow - preserved alpha');layer.set_offsets(64,0)
run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=canvas,file=Gio.File.new_for_path(str(SOURCE/'NP_CozyEmblems_01_BaseColor.xcf')))
run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=canvas,file=Gio.File.new_for_path(str(DEST/'NP_CozyEmblems_01_BaseColor.png')),compression=9,include_color_profile=False)
(DEST/'NP_CozyEmblemSlots_01.json').write_text(json.dumps(dict(version=1,colorSize=[512,256],slots=[dict(name='Cow',uvSafe=[0,0,1,1],sourceSize=[1536,1024],packedRect=[64,0,384,256])],source='Built-in imagegen; native GIMP resize/packing; original alpha preserved'),indent=2))
source.delete();canvas.delete();print('COZY_COW_EMBLEM_PACK_PASS',flush=True)
