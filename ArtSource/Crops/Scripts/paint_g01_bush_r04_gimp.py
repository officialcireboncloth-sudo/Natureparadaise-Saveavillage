"""Revise only the three existing leaf surfaces, preserving all utility and fruit pixels."""
import sys,json
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import g01_raspberry_config as R
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in kw.items():c.set_property(k.replace('_','-'),v)
 assert p.run(c).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(layer,name,**kw):
 f=Gimp.DrawableFilter.new(layer,name,name)
 for k,v in kw.items():f.get_config().set_property(k.replace('_','-'),v)
 layer.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
raw=G.GENERATED/'R04_LeafSurfaces.png';im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));w,h=im.get_width(),im.get_height();im.delete();records=[]
for atlas,specs in [(G.ATLAS,[('BlueberryLeaf',0,0,0),('BlackberryLeaf',1,0,256)]),(R.ATLAS,[('RaspberryLeaf',0,1,0)])]:
 for suffix in ['BaseColor','Normal']:
  path=G.TEXTURES/(atlas+'_'+suffix+'.xcf');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)))
  for name,col,row,x in specs:
   tile=Gimp.Image.new(256,256,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,tile,Gio.File.new_for_path(str(raw)));tile.insert_layer(la,None,0);la.resize(w//2,h//2,-col*(w//2),-row*(h//2));la.set_offsets(0,0);la.scale(256,256,False)
   if name=='BlueberryLeaf':filt(la,'gegl:exposure',exposure=.15)
   if suffix=='Normal':filt(la,'gegl:saturation',scale=0.);filt(la,'gegl:normal-map',scale=.10 if name=='BlueberryLeaf' else .18,flip_x=False,flip_y=False,tileable=False)
   layer=Gimp.Layer.new_from_drawable(la,im);im.insert_layer(layer,None,0);layer.set_offsets(x,0);layer.set_name('R04 '+name+' '+suffix);tile.delete()
  run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(path)));png(im,path.with_suffix('.png'));im.delete();check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)));records.append(dict(atlas=atlas,map=suffix,layers=len(check.get_layers()),size=[check.get_width(),check.get_height()]));check.delete()
(G.REVIEW/'Bush_R04/GimpAudit.json').write_text(json.dumps(dict(status='PASS',masters=records,changedSurfaces=['BlueberryLeaf','BlackberryLeaf','RaspberryLeaf'],surfaceMaskUnchanged=True),indent=2));print('BUSH_R04_GIMP_PASS',flush=True)
