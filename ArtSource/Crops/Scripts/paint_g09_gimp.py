"""Prepare crisp six-source flower/leaf surfaces and editable registered GIMP masters."""
import sys,json,math,random
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
def run(name,**kw):
 proc=Gimp.get_pdb().lookup_procedure(name);cfg=proc.create_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(la,name,**kw):
 f=Gimp.DrawableFilter.new(la,name,name);cfg=f.get_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 la.merge_filter(f)
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
raw=GENERATED/'ShrubFlowers_Source_Sheet.png';im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));rw,rh=im.get_width(),im.get_height();im.delete()
for i,name in enumerate(['RosePetal','HibiscusPetal','JasminePetal','RoseLeaf','HibiscusLeaf','JasmineLeaf']):
 im=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(raw)));im.insert_layer(la,None,0);la.resize(rw//2,rh//3,-(i%2)*(rw//2),-(i//2)*(rh//3));la.set_offsets(0,0);la.scale(512,512,False)
 filt(la,'gegl:exposure',exposure=.18 if name in ['RoseLeaf','RosePetal'] else .10 if 'Leaf' in name else .04)
 if name=='RosePetal':filt(la,'gegl:saturation',scale=1.18)
 png(im,GENERATED/(name+'_Source.png'));im.delete()
color=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);height=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);mask=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB)
for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
 x,y=(i%4)*512,(i//4)*512
 source=name if i<6 else 'RosePetal' if name=='BudRed' else 'JasmineLeaf' if name=='SepalGreen' else None
 for dest,relief in [(color,False),(height,True)]:
  if source:
   la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(GENERATED/(source+'_Source.png'))));dest.insert_layer(la,None,0)
   if relief:filt(la,'gegl:saturation',scale=0.0)
  else:
   b,h,l=('#808080','#9f9f9f','#626262') if relief else (base,hi,lo)
   body=f'<defs><linearGradient id="g"><stop stop-color="{l}"/><stop offset=".40" stop-color="{h}"/><stop offset=".63" stop-color="{b}"/><stop offset="1" stop-color="{l}"/></linearGradient></defs><rect width="512" height="512" fill="url(#g)"/>'
   rng=random.Random(9090+i)
   for j in range(36):
    xx=(j+.5)*512/36;body+=f'<path d="M{xx} 0 Q{xx+4} 256 {xx} 512" stroke="{h}" stroke-width="{rng.uniform(.6,1.7)}" opacity=".20" fill="none"/>'
   if pattern=='rings':
    for r in [70,152,223]:body+=f'<circle cx="256" cy="256" r="{r}" fill="none" stroke="{h}" stroke-width="3" opacity=".55"/>'
   if pattern=='pollen':
    for j in range(300):body+=f'<circle cx="{rng.uniform(1,511)}" cy="{rng.uniform(1,511)}" r="{rng.uniform(.8,2)}" fill="{h if j%2 else l}" opacity=".3"/>'
   p=GENERATED/(name+('_Height' if relief else '_Color')+'.svg');p.write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">'+body+'</svg>');la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,dest,Gio.File.new_for_path(str(p)));dest.insert_layer(la,None,0)
  la.set_offsets(x,y);la.set_name(name)
 la=Gimp.Layer.new(mask,name+' smoothness '+str(smooth),512,512,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);mask.insert_layer(la,None,0);la.set_offsets(x,y);Gimp.context_set_foreground(Gegl.Color.new('#00ff00'));la.fill(Gimp.FillType.FOREGROUND);la.set_opacity(smooth*100)
relief=height.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);relief.set_name('Registered petal fibres and leaf veins - editable height');normal=relief.copy();height.insert_layer(normal,None,0);normal.set_name('Shrub flowers tangent +Y normal');filt(normal,'gegl:normal-map',scale=1.15,flip_x=False,flip_y=False,tileable=False);relief.set_visible(False)
for im,suffix in [(color,'BaseColor'),(height,'Normal'),(mask,'SurfaceMask')]:
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf'))));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(manifest(),indent=2));print('G09_GIMP_PASS 16 detailed 512px regions; three editable masters',flush=True)
