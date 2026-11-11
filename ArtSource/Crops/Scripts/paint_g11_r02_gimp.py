"""Replace only petal/leaf paint and registered normals; retain the other six regions."""
import sys,json,shutil,hashlib
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);cfg=p.create_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 assert p.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(layer,name,**kw):
 f=Gimp.DrawableFilter.new(layer,name,name);cfg=f.get_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 layer.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());snapshot=ROOT/baseline['snapshot']
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');dest=snapshot/p.relative_to(ROOT)
 if not dest.exists():
  key=str(p.relative_to(ROOT)).replace('\\','/');assert hashlib.sha256(p.read_bytes()).hexdigest()==baseline['editableBefore'][key];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
raw=GENERATED/'Tulip_R02_Surfaces.png';im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));rw,rh=im.get_width(),im.get_height();im.delete()
patches=[]
for i,name in enumerate(['TulipPetal','TulipLeaf']):
 im=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(raw)));im.insert_layer(la,None,0);la.resize(rw//2,rh,-i*(rw//2),0);la.set_offsets(0,0);la.scale(512,512,False);png(im,GENERATED/(name+'_R02_Color.png'))
 filt(la,'gegl:saturation',scale=0.);png(im,GENERATED/(name+'_R02_Height.png'));filt(la,'gegl:normal-map',scale=.30,flip_x=False,flip_y=False,tileable=False);png(im,GENERATED/(name+'_R02_Normal.png'));im.delete();patches.append((name,0 if i==0 else 1024,0))
color=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_BaseColor.xcf'))))
for name,x,y in patches:
 color.remove_layer(next(l for l in color.get_layers() if l.get_name()==name));la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,color,Gio.File.new_for_path(str(GENERATED/(name+'_R02_Color.png'))));color.insert_layer(la,None,0);la.set_offsets(x,y);la.set_name(name)
normal=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_Normal.xcf'))));normalLayer=next(l for l in normal.get_layers() if l.get_visible());heightLayer=next(l for l in normal.get_layers() if not l.get_visible());normalLayer.set_visible(False);heightLayer.set_visible(True)
for name,x,y in patches:
 la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,normal,Gio.File.new_for_path(str(GENERATED/(name+'_R02_Height.png'))));normal.insert_layer(la,None,0);la.set_offsets(x,y)
height=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);height.set_name('Registered surface height - R02 petal and leaf');height.set_visible(False);normalLayer.set_visible(True)
for name,x,y in patches:
 la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,normal,Gio.File.new_for_path(str(GENERATED/(name+'_R02_Normal.png'))));normal.insert_layer(la,None,0);la.set_offsets(x,y)
layer=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);layer.set_name('Tangent +Y normal - R02 petal and leaf')
for im,suffix in [(color,'BaseColor'),(normal,'Normal')]:
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf'))));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete()
print('G11_R02_PAINT_PASS: petal and leaf colour/normal replaced; other six surfaces and mask retained',flush=True)
