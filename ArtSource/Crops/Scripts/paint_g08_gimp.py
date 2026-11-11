"""Paint editable cereal surfaces and registered relief in native GIMP 3."""
import sys,json,random,math
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *

def run(name,**kw):
 proc=Gimp.get_pdb().lookup_procedure(name);cfg=proc.create_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS,name

def pattern(i,entry,relief):
 name,base,hi,lo,kind,smooth=entry
 if relief:base,hi,lo='#808080','#b4b4b4','#515151'
 defs=f'<defs><linearGradient id="g"><stop stop-color="{lo}"/><stop offset=".23" stop-color="{base}"/><stop offset=".43" stop-color="{hi}"/><stop offset=".67" stop-color="{base}"/><stop offset="1" stop-color="{lo}"/></linearGradient></defs>'
 body='<rect width="512" height="512" fill="url(#g)"/>'
 rng=random.Random(8100+i)
 # Longitudinal husk ribs and leaf fibres share one registered colour/height master.
 for j in range(52 if kind in ['strap','stem'] else 34):
  x=(j+.5)*512/(52 if kind in ['strap','stem'] else 34);bend=rng.uniform(1,7)
  body+=f'<path d="M{x:.2f} -2 Q{x+bend:.2f} 256 {x:.2f} 514" fill="none" stroke="{hi if j%3 else lo}" stroke-width="{rng.uniform(.6,1.8):.2f}" opacity="{rng.uniform(.14,.36):.2f}"/>'
 if kind=='strap':
  body+=f'<path d="M256 -2 L256 514" stroke="{hi}" stroke-width="5" opacity=".72"/><path d="M261 -2 L261 514" stroke="{lo}" stroke-width="3" opacity=".36"/>'
  for j in range(35):
   x=rng.uniform(16,496);y=rng.uniform(8,504);body+=f'<path d="M{x} {y} l.7 {rng.uniform(9,33)}" stroke="{hi}" stroke-width=".6" opacity=".21"/>'
 elif kind in ['husk','wheat','rice']:
  for k in [-2,-1,0,1,2]:
   x=256+k*64
   body+=f'<path d="M{x} -4 Q{x-12} 256 {x} 516" stroke="{lo}" stroke-width="{4 if k==0 else 1.6}" opacity=".46" fill="none"/><path d="M{x-4} -4 Q{x-16} 256 {x-4} 516" stroke="{hi}" stroke-width="{3 if k==0 else 1.2}" opacity=".72" fill="none"/>'
  if kind=='wheat':body+=f'<path d="M256 36 C235 140 239 375 256 482" stroke="{lo}" stroke-width="6" fill="none" opacity=".58"/>'
  for j in range(80):
   x=rng.uniform(5,507);y=rng.uniform(4,508);body+=f'<path d="M{x} {y} l{rng.uniform(-1,1)} {rng.uniform(2,10)}" stroke="{hi if j%2 else lo}" stroke-width=".7" opacity=".16"/>'
 elif kind=='rings':
  for r in [65,150,218]:body+=f'<circle cx="256" cy="256" r="{r}" fill="none" stroke="{hi}" stroke-width="2" opacity=".6"/>'
 p=GENERATED/(name+('_Height' if relief else '_Color')+'.svg')
 p.write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">'+defs+body+'</svg>');return p

spec=manifest();color=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);height=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);mask=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB)
raw=GENERATED/'Cereal_Source_Sheet.png'
im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));rw,rh=im.get_width(),im.get_height();im.delete()
source_regions={'RiceGold':(0,0),'WheatGold':(1,0),'LeafGreen':(0,1),'LeafGold':(1,1)}
for key,(col,row) in source_regions.items():
 im=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB);la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(raw)));im.insert_layer(la,None,0);la.resize(rw//2,rh//2,-col*(rw//2),-row*(rh//2));la.set_offsets(0,0);la.scale(512,512,False)
 run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(GENERATED/(key+'_Source.png'))),compression=9,include_color_profile=False);im.delete()
source_for=dict(RiceGold='RiceGold',WheatGold='WheatGold',RiceSeed='RiceGold',WheatSeed='WheatGold',RiceGreen='LeafGreen',WheatGreen='LeafGreen',LeafGreen='LeafGreen',LeafGold='LeafGold',LeafWarm='LeafGreen',LeafUnderside='LeafGreen',StemGreen='LeafGreen',StemGold='LeafGold',AwnGold='LeafGold')
for i,entry in enumerate(SURFACES):
 name=entry[0];x,y=(i%4)*512,(i//4)*512
 for dest,isheight in [(color,False),(height,True)]:
  tile=Gimp.Image.new(512,512,Gimp.ImageBaseType.RGB)
  if name in source_for:
   la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,tile,Gio.File.new_for_path(str(GENERATED/(source_for[name]+'_Source.png'))));tile.insert_layer(la,None,0)
   if isheight:
    f=Gimp.DrawableFilter.new(la,'gegl:saturation','Registered detail height');f.get_config().set_property('scale',0.0);la.merge_filter(f)
  la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,tile,Gio.File.new_for_path(str(pattern(i,entry,isheight))));tile.insert_layer(la,None,0)
  if name in source_for:la.set_opacity(25 if not isheight else 35)
  merged=tile.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);la=Gimp.Layer.new_from_drawable(merged,dest);dest.insert_layer(la,None,0);la.set_offsets(x,y);la.set_name(name);tile.delete()
 la=Gimp.Layer.new(mask,name+' smoothness '+str(entry[-1]),512,512,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);mask.insert_layer(la,None,0);la.set_offsets(x,y);Gimp.context_set_foreground(Gegl.Color.new('#00ff00'));la.fill(Gimp.FillType.FOREGROUND);la.set_opacity(entry[-1]*100)
relief=height.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);relief.set_name('Registered longitudinal fibres and seams - editable height');normal=relief.copy();height.insert_layer(normal,None,0);normal.set_name('Cereal tangent +Y normal')
f=Gimp.DrawableFilter.new(normal,'gegl:normal-map','Cereal fibres');cfg=f.get_config()
for k,v in dict(scale=1.8,flip_x=False,flip_y=False,tileable=False).items():cfg.set_property(k.replace('_','-'),v)
normal.merge_filter(f);relief.set_visible(False)
for im,suffix in [(color,'BaseColor'),(height,'Normal'),(mask,'SurfaceMask')]:
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf'))))
 run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(TEXTURES/(ATLAS+'_'+suffix+'.png'))),compression=9,include_color_profile=False);im.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(spec,indent=2))
print('G08_GIMP_PASS 16 editable 512px surfaces and registered normal',flush=True)
