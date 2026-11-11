"""Register generated surfaces and mild physical relief in editable GIMP authoring masters."""
import sys,json,math
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g13_config import *
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in kw.items():c.set_property(k.replace('_','-'),v)
 assert p.run(c).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(layer,name,**kw):
 f=Gimp.DrawableFilter.new(layer,name,name)
 for k,v in kw.items():f.get_config().set_property(k.replace('_','-'),v)
 layer.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
raw=GENERATED/'Lavender_Source_Sheet.png';im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));rw,rh=im.get_width(),im.get_height();im.delete()
color=Gimp.Image.new(1024,1024,Gimp.ImageBaseType.RGB);height=Gimp.Image.new(1024,1024,Gimp.ImageBaseType.RGB);mask=Gimp.Image.new(1024,1024,Gimp.ImageBaseType.RGB)
for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
 x,y=i%2*512,i//2*512
 if i<4:
  tile=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,tile,Gio.File.new_for_path(str(raw)));tile.insert_layer(la,None,0);la.resize(rw//2,rh//2,-(i%2)*(rw//2),-(i//2)*(rh//2));la.set_offsets(0,0);la.scale(512,512,False);png(tile,GENERATED/(name+'_Source.png'));tile.delete()
 for dest,relief in [(color,False),(height,True)]:
  if i<4:
   layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(GENERATED/(name+'_Source.png'))));dest.insert_layer(layer,None,0)
   if relief:filt(layer,'gegl:saturation',scale=0.)
  else:
   b,h,l=('#808080','#979797','#6e6e6e') if relief else (base,hi,lo)
   body=f'<defs><linearGradient id="g"><stop stop-color="{l}"/><stop offset=".35" stop-color="{h}"/><stop offset=".66" stop-color="{b}"/><stop offset="1" stop-color="{l}"/></linearGradient></defs><rect width="512" height="512" fill="url(#g)"/>'
   for j in range(32):
    xx=j*16+8;body+=f'<path d="M {xx} 0 Q {xx+7} 256 {xx} 512" stroke="{h}" opacity=".13" stroke-width="1.2" fill="none"/>'
   p=GENERATED/(name+('_Height' if relief else '_Color')+'.svg');p.write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">'+body+'</svg>');layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(p)));dest.insert_layer(layer,None,0)
  layer.set_offsets(x,y);layer.set_name(name)
 layer=Gimp.Layer.new(mask,name+' smoothness',512,512,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);mask.insert_layer(layer,None,0);layer.set_offsets(x,y);Gimp.context_set_foreground(Gegl.Color.new('#000000'));layer.fill(Gimp.FillType.FOREGROUND);layer.set_opacity(smooth*100)
relief=height.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);relief.set_name('Registered fine surface height');normal=relief.copy();height.insert_layer(normal,None,0);normal.set_name('Tangent +Y normal');filt(normal,'gegl:normal-map',scale=.35,flip_x=False,flip_y=False,tileable=False);relief.set_visible(False)
records=[]
for im,suffix in [(color,'BaseColor'),(height,'Normal'),(mask,'SurfaceMask')]:
 p=TEXTURES/(ATLAS+'_'+suffix+'.xcf');run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete();check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));assert (check.get_width(),check.get_height())==(1024,1024);records.append(dict(map=suffix,layers=len(check.get_layers())));check.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(manifest(),indent=2));(REVIEW/'GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',masters=records),indent=2));print('G13_GIMP_SURFACES_PASS',flush=True)
