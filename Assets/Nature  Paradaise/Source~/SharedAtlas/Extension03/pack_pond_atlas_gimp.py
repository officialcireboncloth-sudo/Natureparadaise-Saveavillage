"""Native GIMP: generated pond materials, physically mapped timber, shared prop palette."""
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
import json,math,random
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir());A=ROOT/'Assets/Nature  Paradaise'
SOURCE=A/'Source~/SharedAtlas/Extension03';DEST=A/'Textures/Shared/Cozy';SIZE=1024;slots=[];random.seed(1104)
def run(name,**values):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in values.items():c.set_property(k.replace('_','-'),v)
 result=p.run(c);assert result.index(0)==Gimp.PDBStatusType.SUCCESS,name
def save(im,name):
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/(name+'.xcf'))))
 run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(DEST/(name+'.png'))),compression=9,include_color_profile=False)
def slot(name,rect):
 x,y,w,h=rect;p=8;slots.append(dict(name=name,pixelRectTopLeft=rect,uvSafe=[(x+p)/SIZE,1-(y+h-p)/SIZE,(x+w-p)/SIZE,1-(y+p)/SIZE]))
def layer(im,name,w,h,color,x=0,y=0,opacity=100):
 la=Gimp.Layer.new(im,name,w,h,Gimp.ImageType.RGBA_IMAGE,opacity,Gimp.LayerMode.NORMAL);im.insert_layer(la,None,0)
 if color is None:la.fill(Gimp.FillType.TRANSPARENT)
 else:Gimp.context_set_foreground(Gegl.Color.new(color));la.fill(Gimp.FillType.FOREGROUND)
 la.set_offsets(x,y);return la
def crop_insert(canvas,path,crop,rect,name,rotate=False):
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)));iw,ih=im.get_width(),im.get_height()
 x0,y0,x1,y1=crop;x,y,w,h=rect;left,top=int(iw*x0),int(ih*y0);im.crop(int(iw*x1)-left,int(ih*y1)-top,left,top)
 if rotate:im.rotate(Gimp.RotationType.DEGREES90)
 im.scale(w,h)
 la=Gimp.Layer.new_from_drawable(im.get_layers()[0],canvas);canvas.insert_layer(la,None,0);la.set_name(name);la.set_offsets(x,y);im.delete();return la
Gimp.context_push();Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
canvas=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
for i,(name,rect) in enumerate([('PondRock',(0,0,512,512)),('PondRockMoss',(512,0,512,512)),('PondWood',(0,512,512,448)),('PondPebbles',(512,512,512,448))]):
 col,row=i%2,i//2;crop=(col*.5+.008,row*.5+.008,(col+1)*.5-.008,(row+1)*.5-.008)
 crop_insert(canvas,SOURCE/'Generated_PondSurfaceMaster.png',crop,rect,name);slot(name,rect)
old={s['name']:s for s in json.loads((DEST/'NP_CozyAtlasSlots.json').read_text())['slots']}
for i,(name,prior,color) in enumerate([('PondIron','Iron',None),('PondBrass',None,'#c99c4d'),('PondTeal',None,'#365c58'),('PondDark','WoodDark',None),('PondBurlap','Burlap',None),('PondEnd','WoodEnd',None),('PondRope','Rope',None),('PondLeaf',None,'#516d2c')]):
 rect=(i*128,960,128,64)
 if prior:
  # Burlap/Rope are subregions of the old atlas exposed by geometry helpers.
  if prior in old:r=old[prior]['uvSafe']
  elif prior=='WoodEnd':r=old['EndGrain']['uvSafe']
  elif prior=='Burlap':r=(.63,.12,.74,.29)
  else:r=(.76,.20,.87,.29)
  crop_insert(canvas,DEST/'NP_CozySurfaces_01_BaseColor.png',(r[0],1-r[3],r[2],1-r[1]),rect,name,prior=='WoodDark')
 else:
  la=layer(canvas,name,128,64,color,rect[0],rect[1])
  Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 050'));Gimp.context_enable_dynamics(False);Gimp.context_set_opacity(24)
  for j in range(14):
   Gimp.context_set_foreground(Gegl.Color.new('#899b8a' if name=='PondTeal' else '#f2d495'));Gimp.context_set_brush_size(random.uniform(1,3));x=random.uniform(7,120);y=random.uniform(6,58)
   Gimp.paintbrush_default(la,[float(rect[0]+x),float(rect[1]+y),float(rect[0]+min(126,x+random.uniform(2,16))),float(rect[1]+y+.3)])
  Gimp.context_set_opacity(100)
 slot(name,rect)
save(canvas,'NP_CozySurfaces_03_BaseColor');canvas.delete()
# Independent restrained relief: broad rock creases and fine horizontal wood fibres.
height=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB);base=layer(height,'Independent editable material relief',SIZE,SIZE,'#808080')
Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 050'));Gimp.context_enable_dynamics(False);Gimp.context_set_opacity(50)
for xoff in [0,512]:
 height.select_rectangle(Gimp.ChannelOps.REPLACE,xoff+8,8,496,496)
 for j in range(28):
  x=xoff+random.uniform(10,500);y=random.uniform(5,500);Gimp.context_set_foreground(Gegl.Color.new('#777777'));Gimp.context_set_brush_size(random.uniform(1.5,3.5))
  Gimp.paintbrush_default(base,[float(x),float(y),float(x+26),float(y+8),float(x+41),float(y+23)])
Gimp.Selection.none(height);height.select_rectangle(Gimp.ChannelOps.REPLACE,8,520,496,432)
for j in range(68):
 y=520+j*432/68;Gimp.context_set_foreground(Gegl.Color.new('#797979' if j%3 else '#878787'));Gimp.context_set_brush_size(1.8)
 Gimp.paintbrush_default(base,[float(v) for t in range(0,513,12) for v in (t,y+math.sin(t/90+j)*1.6)])
Gimp.Selection.none(height);height.scale(512,512);base=height.get_layers()[0];normal=base.copy();height.insert_layer(normal,None,0);normal.set_name('Tangent +Y gentle mineral and wood relief')
f=Gimp.DrawableFilter.new(normal,'gegl:normal-map','Pond soft relief');c=f.get_config()
for k,v in [('scale',7.),('flip-x',False),('flip-y',False),('tileable',False)]:c.set_property(k,v)
normal.merge_filter(f);base.set_visible(False);save(height,'NP_CozySurfaces_03_Normal');height.delete()
mask=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
for s in slots:
 x,y,w,h=s['pixelRectTopLeft'];name=s['name'];metal=.70 if name=='PondBrass' else .50 if name=='PondIron' else 0;smooth=.30 if name=='PondBrass' else .20 if name in ('PondIron','PondTeal') else .08
 color=Gegl.Color.new('black');color.set_rgba(metal,1,0,1);la=layer(mask,name,w,h,None,x,y,smooth*100);Gimp.context_set_foreground(color);la.fill(Gimp.FillType.FOREGROUND)
mask.scale(512,512);save(mask,'NP_CozySurfaces_03_SurfaceMask');mask.delete();Gimp.context_pop()
(DEST/'NP_CozyAtlasSlots_03.json').write_text(json.dumps(dict(version=3,date='2026-11-04',colorSize=[1024,1024],normalSize=[512,512],maskSize=[512,512],paddingPixels=8,slots=slots,woodGrainDirection='Horizontal U; UV sized to individual timber, not stretched across face',legacyAtlasModified=False,source='Built-in imagegen four material quadrants, native GIMP packing and independent relief'),indent=2))
print('POND_ATLAS_GIMP_PASS',len(slots),flush=True)
