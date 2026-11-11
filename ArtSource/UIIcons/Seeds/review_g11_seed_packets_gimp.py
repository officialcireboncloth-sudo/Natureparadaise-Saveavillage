"""Show the nine newly bound seed sprites at their actual exported size."""
import sys,ast,math,json
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'ArtSource/Crops/Scripts'))
from g11_integration_config import *
helper=ROOT/'ArtSource/UIIcons/HandPainted/draw_icons_gimp.py';tree=ast.parse(helper.read_text());ns=dict(Gimp=Gimp,Gio=Gio);exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='export_png'],type_ignores=[]),str(helper),'exec'),ns)
sheet=Gimp.Image.new(288,288,Gimp.ImageBaseType.RGB);bg=Gimp.Layer.new(sheet,'Sage review background',288,288,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);sheet.insert_layer(bg,None,0);Gimp.context_set_foreground(Gegl.Color.new('#344b3d'));bg.edit_fill(Gimp.FillType.FOREGROUND)
for i,(_,kind,key,label) in enumerate(NEW):
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(DEST/'Seeds'/('icon_seed_'+key+'.png'))));layer=Gimp.Layer.new_from_drawable(im.get_layers()[0],sheet);sheet.insert_layer(layer,None,0);layer.set_name(label);layer.set_offsets(i%2*288+16,i//2*288+16);im.delete()
ns['export_png'](sheet,ART/'G11_SeedPackets_Preview.png');sheet.delete();print('GIMP_SEED_EXTENSION_REVIEW_PASS',flush=True)
