"""Pack eight generated surface studies into editable registered GIMP atlases."""
import sys,json,math,random
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);cfg=p.create_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 assert p.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(la,name,**kw):
 f=Gimp.DrawableFilter.new(la,name,name);cfg=f.get_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 la.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
raw=GENERATED/'Tulip_Source_Sheet.png';im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));rw,rh=im.get_width(),im.get_height();im.delete()
for i,(name,*_) in enumerate(SURFACES[:4]):
 im=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(raw)));im.insert_layer(la,None,0)
 la.resize(rw//2,rh//2,-(i%2)*(rw//2),-(i//2)*(rh//2));la.set_offsets(0,0);la.scale(512,512,False)
 png(im,GENERATED/(name+'_Source.png'));im.delete()
color=Gimp.Image.new(2048,1024,Gimp.ImageBaseType.RGB);height=Gimp.Image.new(2048,1024,Gimp.ImageBaseType.RGB);mask=Gimp.Image.new(2048,1024,Gimp.ImageBaseType.RGB)
for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
 x,y=i%4*512,i//4*512
 for dest,relief in [(color,False),(height,True)]:
  if i<4:
   la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(GENERATED/(name+'_Source.png'))));dest.insert_layer(la,None,0)
   if relief:filt(la,'gegl:saturation',scale=0.)
  else:
   b,h,l=('#808080','#a5a5a5','#626262') if relief else (base,hi,lo)
   body=f'<defs><linearGradient id="g"><stop stop-color="{l}"/><stop offset=".4" stop-color="{h}"/><stop offset=".63" stop-color="{b}"/><stop offset="1" stop-color="{l}"/></linearGradient></defs><rect width="512" height="512" fill="url(#g)"/>'
   rng=random.Random(1010+i)
   for j in range(42):
    xx=(j+.5)*512/42;body+=f'<path d="M{xx} 0 Q{xx+4} 256 {xx} 512" stroke="{h}" stroke-width="1.2" opacity=".18" fill="none"/>'
   if pattern=='stripe':
    for j in range(7):
     xx=27+j*76;body+=f'<path d="M{xx} 0 Q{xx+10} 256 {xx} 512" stroke="{h}" stroke-width="14" opacity=".9" fill="none"/>'
   if pattern=='disk':
    body=f'<rect width="512" height="512" fill="{b}"/>'
    for j in range(750):
     r=math.sqrt((j+.5)/750)*355;a=j*2.39996;xx=256+r*math.cos(a);yy=256+r*math.sin(a)
     body+=f'<circle cx="{xx}" cy="{yy}" r="3.2" fill="{l}" opacity=".3"/><circle cx="{xx-1}" cy="{yy-1}" r="1.2" fill="{h}" opacity=".25"/>'
   if pattern=='rings':
    for r in [70,152,223]:body+=f'<circle cx="256" cy="256" r="{r}" stroke="{h}" fill="none" stroke-width="4"/>'
   p=GENERATED/(name+('_Height' if relief else '_Color')+'.svg');p.write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">'+body+'</svg>')
   la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(p)));dest.insert_layer(la,None,0)
  la.set_offsets(x,y);la.set_name(name)
 la=Gimp.Layer.new(mask,name+' smoothness '+str(smooth),512,512,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);mask.insert_layer(la,None,0);la.set_offsets(x,y)
 Gimp.context_set_foreground(Gegl.Color.new('#00ff00'));la.fill(Gimp.FillType.FOREGROUND);la.set_opacity(smooth*100)
relief=height.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);relief.set_name('Registered surface height');normal=relief.copy();height.insert_layer(normal,None,0);normal.set_name('Tangent +Y normal');filt(normal,'gegl:normal-map',scale=.85,flip_x=False,flip_y=False,tileable=False);relief.set_visible(False)
for im,suffix in [(color,'BaseColor'),(height,'Normal'),(mask,'SurfaceMask')]:
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf'))));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(manifest(),indent=2));print('G11_GIMP_PASS: 8 regions, three editable XCF masters',flush=True)

if (GENERATED/'Tulip_R02_Surfaces.png').is_file():
 p=SOURCE/'Scripts/paint_g11_r02_gimp.py';exec(compile(p.read_text(),str(p),'exec'),{'__file__':str(p)})
