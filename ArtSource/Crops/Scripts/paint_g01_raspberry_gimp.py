"""Preserve editable source maps; add detailed Raspberry leaf and drupelet surfaces."""
import sys,json
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_raspberry_config import *
from g01_config import ATLAS as OLD
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in kw.items():c.set_property(k.replace('_','-'),v)
 assert p.run(c).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(layer,name,**kw):
 f=Gimp.DrawableFilter.new(layer,name,name)
 for k,v in kw.items():f.get_config().set_property(k.replace('_','-'),v)
 layer.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
raw=GENERATED/'Raspberry_Source_Sheet.png';source=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));w,h=source.get_width(),source.get_height();source.delete();records=[]
for suffix in ['BaseColor','Normal','SurfaceMask']:
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(TEXTURES/(OLD+'_'+suffix+'.xcf'))))
 # Old utility layers and coordinates stay aligned; only the top source surfaces are replaced.
 for layer in list(im.get_layers()):
  if suffix=='BaseColor' and layer.get_name() in ['BlueberryLeaf','BlackberryLeaf','BlueberrySkin','BlackberryDrupe']:im.remove_layer(layer)
 for i,name in enumerate(['RaspberryLeaf','RaspberryDrupe']):
  if suffix=='SurfaceMask':
   layer=Gimp.Layer.new(im,name+' smoothness',256,256,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);im.insert_layer(layer,None,0);Gimp.context_set_foreground(Gegl.Color.new('#000000'));layer.fill(Gimp.FillType.FOREGROUND);layer.set_opacity((.25 if i==0 else .38)*100)
  else:
   tile=Gimp.Image.new(256,256,Gimp.ImageBaseType.RGB);layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,tile,Gio.File.new_for_path(str(raw)));tile.insert_layer(layer,None,0);layer.resize(w//2,h,-i*(w//2),0);layer.set_offsets(0,0);layer.scale(256,256,False)
   if suffix=='Normal':
    filt(layer,'gegl:saturation',scale=0.);filt(layer,'gegl:normal-map',scale=.18 if i==0 else .07,flip_x=False,flip_y=False,tileable=False)
   new=Gimp.Layer.new_from_drawable(layer,im);im.insert_layer(new,None,0);tile.delete();layer=new
  layer.set_offsets(i*256,0);layer.set_name(name+(' '+suffix));
 p=TEXTURES/(ATLAS+'_'+suffix+'.xcf');run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete();check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));records.append(dict(map=suffix,layers=len(check.get_layers()),size=[check.get_width(),check.get_height()]));check.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(manifest(),indent=2));(REVIEW/'Raspberry/GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',masters=records),indent=2));print('RASPBERRY_GIMP_SURFACES_PASS',flush=True)
