"""Native GIMP prepares the registered high-detail leafy page and editable masters."""
import sys,json,math,random
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g07_r02_layout import *
def run(name,**kw):
 proc=Gimp.get_pdb().lookup_procedure(name);cfg=proc.create_config()
 for k,v in kw.items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS,name
def filt(la,name,**kw):
 f=Gimp.DrawableFilter.new(la,name,name);c=f.get_config()
 for k,v in kw.items():c.set_property(k.replace('_','-'),v)
 la.merge_filter(f)
def png(im,path):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(path)),compression=9,include_color_profile=False)
def layer(im,path):
 la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(path)));im.insert_layer(la,None,0);return la
raw=GENERATED/'Leaf_Source_Sheet.png';src=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(raw)));width,height=src.get_width(),src.get_height();src.delete()
for i,kind in enumerate(['Spinach','WaterSpinach']):
 im=Gimp.Image.new(1024,1024,Gimp.ImageBaseType.RGB);la=layer(im,raw);la.resize(width//2,height,-i*(width//2),0);la.set_offsets(0,0);la.scale(1024,1024,False);png(im,GENERATED/(kind+'_Leaf.png'));im.delete()
spec=manifest();color=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);height_im=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);mask=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB)
oldpng=TEXTURES/'NP_Plants_Trellis_01_BaseColor.png';oldslots=json.loads((TEXTURES/'NP_Plants_Trellis_01_Slots.json').read_text());oldslots={s['name']:s for s in oldslots['slots']}
for name,x,y,size in REGIONS:
 for dest,isheight in [(color,False),(height_im,True)]:
  tile=Gimp.Image.new(size,size,Gimp.ImageBaseType.RGB)
  bg=Gimp.Layer.new(tile,'Background',size,size,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);tile.insert_layer(bg,None,0)
  Gimp.context_set_foreground(Gegl.Color.new('#808080' if isheight else PALETTE.get(name,('#70ad2c',))[0]));bg.fill(Gimp.FillType.FOREGROUND)
  if name in ['LeafOrchid','LeafWaterSpinach','LeafYoungSpinach','LeafYoungWaterSpinach','LeafUnderside']:
   kind='WaterSpinach' if 'WaterSpinach' in name else 'Spinach';la=layer(tile,GENERATED/(kind+'_Leaf.png'));pad=24 if size==1024 else 12;la.scale(size-pad*2,size-pad*2,False);la.set_offsets(pad,pad)
   if name.startswith('LeafYoung'):filt(la,'gegl:exposure',exposure=.13);filt(la,'gegl:saturation',scale=1.08)
   if isheight:filt(la,'gegl:saturation',scale=0)
  elif name in ['SeedTan','SeedBrown']:
   la=layer(tile,oldpng);a,b,w,h=oldslots[name]['pixelRectTopLeft'];la.resize(w,h,-a,-b);la.set_offsets(0,0);la.scale(size,size,False)
   if isheight:filt(la,'gegl:saturation',scale=0)
  else:
   c='#80bd2b' if name=='StemPale' else '#387b22' if name=='HollowStem' else '#bad879'
   if isheight:c='#808080'
   body=f'<rect width="512" height="512" fill="{c}"/>'
   for j in range(30):
    x0=j*18;body+=f'<path d="M{x0} 0 Q{x0+2} 256 {x0} 512" fill="none" stroke="{"#999999" if isheight else "#acd85a"}" stroke-width=".8" opacity=".20"/>'
   if name=='CutStem':
    for r in [75,182,227]:body+=f'<circle cx="256" cy="256" r="{r}" fill="none" stroke="{"#969696" if isheight else "#e2eda4"}" stroke-width="3" opacity=".45"/>'
   p=GENERATED/(name+('_Height' if isheight else '_Color')+'.svg');p.write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512">'+body+'</svg>');la=layer(tile,p);la.scale(size,size,False);la.set_offsets(0,0)
  merged=tile.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);copy=Gimp.Layer.new_from_drawable(merged,dest);dest.insert_layer(copy,None,0);copy.set_offsets(x,y);copy.set_name(name);tile.delete()
 la=Gimp.Layer.new(mask,name+' metallic 0 / smoothness '+str(SMOOTH[name]),size,size,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);mask.insert_layer(la,None,0);la.set_offsets(x,y);Gimp.context_set_foreground(Gegl.Color.new('#00ff00'));la.fill(Gimp.FillType.FOREGROUND);la.set_opacity(SMOOTH[name]*100)
relief=height_im.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);relief.set_name('Registered generated leaf relief');normal=relief.copy();height_im.insert_layer(normal,None,0);normal.set_name('Leafy tangent +Y normal');filt(normal,'gegl:normal-map',scale=1.1,flip_x=False,flip_y=False,tileable=False);relief.set_visible(False)
for im,suffix in [(color,'BaseColor'),(height_im,'Normal'),(mask,'SurfaceMask')]:
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf'))));png(im,TEXTURES/(ATLAS+'_'+suffix+'.png'));im.delete()
(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(spec,indent=2));(REVIEW/'R02_GimpDetail.json').write_text(json.dumps(dict(status='PASS',atlas=ATLAS,leafRegionSize=1024,editableSlotLayers=10,registeredSource=raw.name),indent=2))
print('G07_R02_GIMP_PAGE_PASS',flush=True)
