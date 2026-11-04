"""Native GIMP: additive Cozy atlas page, editable layers, alpha wire and independent normal/mask."""
from pathlib import Path
from gi.repository import Gimp, Gegl, Gio
import json, math, random
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'; SOURCE=A/'Source~/SharedAtlas/Extension02'; DEST=A/'Textures/Shared/Cozy'
SIZE=1024; slots=[]; random.seed(1103)
def run(name,**values):
 p=Gimp.get_pdb().lookup_procedure(name); c=p.create_config()
 for k,v in values.items():c.set_property(k.replace('_','-'),v)
 result=p.run(c);assert result.index(0)==Gimp.PDBStatusType.SUCCESS,(name,str(result.index(0)))
def save(im,name):
 run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/(name+'.xcf'))))
 run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(DEST/(name+'.png'))),compression=9,include_color_profile=False)
def slot(name,rect):
 x,y,w,h=rect;p=8
 slots.append(dict(name=name,pixelRectTopLeft=rect,uvSafe=[(x+p)/SIZE,1-(y+h-p)/SIZE,(x+w-p)/SIZE,1-(y+p)/SIZE]))
def fill(im,name,w,h,color,x=0,y=0,opacity=100):
 la=Gimp.Layer.new(im,name,w,h,Gimp.ImageType.RGBA_IMAGE,opacity,Gimp.LayerMode.NORMAL);im.insert_layer(la,None,0)
 if color is None:la.fill(Gimp.FillType.TRANSPARENT)
 else:Gimp.context_set_foreground(Gegl.Color.new(color));la.fill(Gimp.FillType.FOREGROUND)
 la.set_offsets(x,y);return la
Gimp.context_push();Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
canvas=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
for i,(name,rect) in enumerate([('RoofClayClean',(0,0,512,448)),('WoodCreamClean',(512,0,512,448)),('RiverStone',(0,448,512,384)),('RiverStoneMoss',(512,448,512,384))]):
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Generated_SurfaceMaster.png')))
 iw,ih=im.get_width(),im.get_height();col,row=i%2,i//2
 x0=int(iw*(col*.5+.006));y0=int(ih*(row*.5+.006));x1=int(iw*((col+1)*.5-.006));y1=int(ih*((row+1)*.5-.006))
 im.crop(x1-x0,y1-y0,x0,y0);im.scale(rect[2],rect[3]);la=Gimp.Layer.new_from_drawable(im.get_layers()[0],canvas)
 canvas.insert_layer(la,None,0);la.set_name(name);la.set_offsets(rect[0],rect[1]);im.delete();slot(name,rect)
# Warm terracotta glaze is a material colour adjustment, not directional lighting.
glaze=fill(canvas,'Terracotta warm glaze',512,448,'#dca166',0,0,60);glaze.set_mode(Gimp.LayerMode.MULTIPLY)
# Wire is authored as a real alpha texture, not dark filled hexagons.
wire=Gimp.Image.new(256,192,Gimp.ImageBaseType.RGB);wl=fill(wire,'Hexagonal chicken wire',256,192,None)
Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 100'));Gimp.context_enable_dynamics(False);Gimp.context_set_opacity(100)
segments=set();r=20;dy=math.sqrt(3)*r
for col in range(-1,11):
 for row in range(-1,8):
  cx=col*1.5*r;cy=row*dy+(col%2)*dy/2
  points=[(cx+r*math.cos(j*math.pi/3),cy+r*math.sin(j*math.pi/3)) for j in range(6)]
  for a,b in zip(points,points[1:]+points[:1]):
   key=tuple(sorted((tuple(round(n,3) for n in a),tuple(round(n,3) for n in b))))
   if key in segments:continue
   segments.add(key)
   Gimp.context_set_foreground(Gegl.Color.new('#423b2b'));Gimp.context_set_brush_size(2.4);Gimp.paintbrush_default(wl,[*a,*b])
   Gimp.context_set_foreground(Gegl.Color.new('#b6a27b'));Gimp.context_set_brush_size(1.0);Gimp.paintbrush_default(wl,[*a,*b])
la=Gimp.Layer.new_from_drawable(wl,canvas);canvas.insert_layer(la,None,0);la.set_offsets(0,832);la.set_name('ChickenWire - transparent apertures');wire.delete();slot('ChickenWire',(0,832,256,192))
roof=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Generated_SurfaceMaster.png')))
roof.crop(roof.get_width()//2-8,roof.get_height()//2-8,4,4);roof.scale(256,192);rl=roof.get_layers()[0]
for row in range(3):
 y=(row+1)*64-1
 Gimp.context_set_foreground(Gegl.Color.new('#aa422c'));Gimp.context_set_brush_size(3);Gimp.paintbrush_default(rl,[0.,float(y),256.,float(y)])
 Gimp.context_set_foreground(Gegl.Color.new('#ee916c'));Gimp.context_set_brush_size(1.2);Gimp.paintbrush_default(rl,[0.,float(y-2),256.,float(y-2)])
 for col in range(-1,5):
  x=col*64+(32 if row%2 else 0)
  Gimp.context_set_foreground(Gegl.Color.new('#ae472e'));Gimp.context_set_brush_size(2);Gimp.paintbrush_default(rl,[float(x),float(row*64),float(x),float(y)])
la=Gimp.Layer.new_from_drawable(rl,canvas);canvas.insert_layer(la,None,0);la.set_offsets(256,832);la.set_name('RoofClayTiled - distant LOD');roof.delete();slot('RoofClayTiled',(256,832,256,192))
glaze=fill(canvas,'Distant tile warm glaze',256,192,'#dca166',256,832,60);glaze.set_mode(Gimp.LayerMode.MULTIPLY)
for i,(name,color) in enumerate([('PaintTeal','#406964'),('BrassWarm','#bc954c'),('TimberCream','#dfc99a')]):
 rect=(512+i*128,832,128,192);fill(canvas,name,128,192,color,rect[0],rect[1]);slot(name,rect)
hay=Gimp.Image.new(128,192,Gimp.ImageBaseType.RGB);hl=fill(hay,'Golden straw fibres',128,192,'#c99724')
for i in range(480):
 x=random.uniform(-25,135);y=random.uniform(-25,195);length=random.uniform(12,43);angle=random.uniform(-.7,.7)
 Gimp.context_set_foreground(Gegl.Color.new(random.choice(['#edbd43','#f8cf61','#b17c1c','#dca82d'])));Gimp.context_set_brush_size(random.uniform(1.1,2.3));Gimp.context_set_opacity(80)
 Gimp.paintbrush_default(hl,[x,y,x+math.sin(angle)*length*.5,y+length*.5,x+math.sin(angle+.2)*length,y+length])
Gimp.context_set_opacity(100);la=Gimp.Layer.new_from_drawable(hl,canvas);canvas.insert_layer(la,None,0);la.set_offsets(896,832);la.set_name('HayGold - stylized golden fibres');hay.delete();slot('HayGold',(896,832,128,192))
save(canvas,'NP_CozySurfaces_02_BaseColor');canvas.delete()
# Sculpted geometry supplies roof laps/nok. This independent height only adds gentle material grain.
height=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB);base=fill(height,'Independent neutral microrelief',SIZE,SIZE,'#808080')
Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 050'));Gimp.context_set_brush_size(3);Gimp.context_set_opacity(55)
height.select_rectangle(Gimp.ChannelOps.REPLACE,520,8,496,432)
for j in range(32):
 x=520+(j+.5)*496/32;Gimp.context_set_foreground(Gegl.Color.new('#858585' if j%2 else '#7a7a7a'))
 Gimp.paintbrush_default(base,[float(v) for t in range(0,449,8) for v in (x+math.sin(t/90+j)*2,t)])
Gimp.Selection.none(height);height.scale(512,512);base=height.get_layers()[0]
normal=base.copy();height.insert_layer(normal,None,0);normal.set_name('Tangent +Y soft grain')
f=Gimp.DrawableFilter.new(normal,'gegl:normal-map','Restrained independent normal');c=f.get_config()
for k,v in [('scale',5.),('flip-x',False),('flip-y',False),('tileable',False)]:c.set_property(k,v)
normal.merge_filter(f);base.set_visible(False);save(height,'NP_CozySurfaces_02_Normal');height.delete()
mask=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
for s in slots:
 x,y,w,h=s['pixelRectTopLeft'];name=s['name'];metal=.8 if name=='BrassWarm' else .65 if name=='ChickenWire' else 0.;smooth=.35 if name=='BrassWarm' else .22 if name in ('PaintTeal','ChickenWire') else .12
 color=Gegl.Color.new('black');color.set_rgba(metal,1.,0.,1.);la=fill(mask,name+' surface mask',w,h,None,x,y,smooth*100)
 Gimp.context_set_foreground(color);la.fill(Gimp.FillType.FOREGROUND)
mask.scale(512,512);save(mask,'NP_CozySurfaces_02_SurfaceMask');mask.delete();Gimp.context_pop()
data=dict(version=2,date='2026-11-03',colorSize=[1024,1024],normalSize=[512,512],maskSize=[512,512],paddingPixels=8,slots=slots,normalConvention='Tangent +Y; independent microrelief; geometry owns roof overlap',legacyAtlasModified=False,source='Built-in imagegen source master; native GIMP packing, alpha wire, palette, height and mask')
(DEST/'NP_CozyAtlasSlots_02.json').write_text(json.dumps(data,indent=2))
print('COZY_EXTENSION_GIMP_PASS',len(slots),flush=True)
