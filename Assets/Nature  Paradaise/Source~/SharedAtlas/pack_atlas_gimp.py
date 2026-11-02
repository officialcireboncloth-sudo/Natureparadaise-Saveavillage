"""Native GIMP packing of generated source surfaces, reusable palette, editable XCF and restrained height/normal."""
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
import math,json,hashlib
ROOT=next(p for start in (Path.cwd(),Path(__file__).resolve().parent) for p in [start,*start.parents] if (p/'Assets/Nature  Paradaise').is_dir());A=ROOT/'Assets/Nature  Paradaise';ART=ROOT/'ArtSource/HouseCozy';DEST=A/'Textures/Shared/Cozy';SOURCE=A/'Source~/SharedAtlas';SOURCE.mkdir(parents=True,exist_ok=True);DEST.mkdir(parents=True,exist_ok=True)
REF=ART/'References' if (ART/'References/Generated_Surfaces.png').exists() else SOURCE/'References'
def run(name,**values):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in values.items():c.set_property(k.replace('_','-'),v)
 r=p.run(c);assert r.index(0)==Gimp.PDBStatusType.SUCCESS,(name,str(r.index(0)))
def png(image,path):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(str(path)),compression=9,include_color_profile=False)
def xcf(image,path):run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(str(path)))
slots=[]
def slot(name,rect,source):
 x,y,w,h=rect;p=8
 slots.append(dict(name=name,pixelRectTopLeft=rect,uvSafe=[(x+p)/2048,1-(y+h-p)/2048,(x+w-p)/2048,1-(y+p)/2048],source=source))
Gimp.context_push();Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
canvas=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB)
def insert(path,crop,rect,name):
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)));iw,ih=im.get_width(),im.get_height();x0,y0,x1,y1=crop
 x,y,w,h=rect;ix,iy=int(iw*x0),int(ih*y0);ex,ey=int(iw*x1),int(ih*y1);im.crop(ex-ix,ey-iy,ix,iy);im.scale(w,h)
 la=Gimp.Layer.new_from_drawable(im.get_layers()[0],canvas);canvas.insert_layer(la,None,0);la.set_offsets(x,y);la.set_name(name);im.delete()
names=['WoodHoney','WoodDark','WoodCream','WoodFloor','RoofOlive','RoofSlate','RoofRed','Plaster','StoneWarm','StoneGray','Brick','Terracotta','Iron','Aluminium','FabricQuadrants','FiberQuadrants']
for i,name in enumerate(names):
 c,r=i%4,i//4;rect=(c*512,r*480,512,480);insert(REF/'Generated_Surfaces.png',(c/4,r/4,(c+1)/4,(r+1)/4),rect,name)
 if i<14:slot(name,rect,'imagegen surface master')
for parent,children in [('FabricQuadrants',['FabricSage','FabricCream','FabricRed','Burlap']),('FiberQuadrants',['Rope','Wicker','Hay','Grain'])]:
 idx=names.index(parent);x,y=(idx%4)*512,(idx//4)*480
 for j,n in enumerate(children):slot(n,(x+(j%2)*256,y+(j//2)*240,256,240),'imagegen surface master quadrant')
for i,name in enumerate(['EndGrain','Earth','WoodRed','Paving']):
 rect=(i*256,1920,256,128);insert(REF/'Generated_Details.png',((i%2)/2,(i//2)/2,(i%2+1)/2,(i//2+1)/2),rect,name);slot(name,rect,'imagegen detail master')
palette=[('GlowWarm','#f6ce70'),('RoofTrim','#60684c'),('LeafGreen','#4e6e2b'),('FlowerWhite','#f4eacb'),('FlowerGold','#d9a934'),('Paper','#ddd3b5'),('Ceramic','#d3d7cf'),('WaterBlue','#547e8e'),('FoodOrange','#cf6d29'),('FoodRed','#b53527'),('FoodPurple','#543e61'),('PaintCream','#ddd0ac'),('PaintRed','#a94937'),('GlassBlue','#718f9c'),('MetalHighlight','#a6b2ad'),('SoilDark','#463526')]
for i,(name,color) in enumerate(palette):
 x=1024+i*64;y=1920;Gimp.context_set_foreground(Gegl.Color.new(color));la=Gimp.Layer.new(canvas,name,64,128,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);canvas.insert_layer(la,None,0);la.fill(Gimp.FillType.FOREGROUND);la.set_offsets(x,y);slot(name,(x,y,64,128),'shared colour palette')
xcf(canvas,SOURCE/'NP_CozySurfaces_01.xcf');png(canvas,DEST/'NP_CozySurfaces_01_BaseColor.png');canvas.delete()
fol=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(REF/'Generated_Foliage.png')));assert fol.get_layers()[0].has_alpha(),'Foliage must have alpha';fol.scale(1024,1024);xcf(fol,SOURCE/'NP_CozyFoliage_01.xcf');png(fol,DEST/'NP_CozyFoliage_01_BaseColor.png');fol.delete()

# Independent relief authoring: never treat the material's painted colour/highlights as a height map.
image=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);image.convert_precision(Gimp.Precision.FLOAT_NON_LINEAR)
base=Gimp.Layer.new(image,'Editable height - neutral relief',2048,2048,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);image.insert_layer(base,None,0);Gimp.context_set_foreground(Gegl.Color.new('#808080'));base.fill(Gimp.FillType.FOREGROUND)
Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 050'));Gimp.context_enable_dynamics(False);Gimp.context_set_opacity(70);Gimp.context_set_brush_hardness(.22)
def stroke(points,value,width):
 Gimp.context_set_foreground(Gegl.Color.new('#%02x%02x%02x'%(value,value,value)));Gimp.context_set_brush_size(width);Gimp.paintbrush_default(base,[float(v) for p in points for v in p])
for s in slots:
 x,y,w,h=s['pixelRectTopLeft'];name=s['name'];image.select_rectangle(Gimp.ChannelOps.REPLACE,x+12,y+12,w-24,h-24)
 if name.startswith('Wood'):
  for j in range(7):stroke([(x+(j+.5)*w/7+10*math.sin(t/110+j),y+t) for t in range(-8,h+12,8)],108 if j%2 else 145,5)
 elif name.startswith('Roof') and name!='RoofTrim':
  for row in range(1,5):stroke([(x+t,y+row*h/4) for t in range(0,w+1,8)],108,8)
 elif name in ['Brick','StoneWarm','StoneGray','Paving']:
  for row in range(1,4):stroke([(x+t,y+row*h/4) for t in range(0,w+1,8)],113,6)
Gimp.Selection.none(image);Gimp.context_set_opacity(100);image.scale(1024,1024)
height=image.get_layers()[0];normal=height.copy();image.insert_layer(normal,None,0);normal.set_name('Tangent normal +Y - restrained stylized relief');f=Gimp.DrawableFilter.new(normal,'gegl:normal-map','Cozy material relief');c=f.get_config()
for k,v in [('scale',12.0),('flip-x',False),('flip-y',False),('tileable',False)]:c.set_property(k,v)
normal.merge_filter(f);height.set_visible(False);xcf(image,SOURCE/'NP_CozySurfaces_01_Normal.xcf');png(image,DEST/'NP_CozySurfaces_01_Normal.png');image.delete();Gimp.context_pop()
manifest=dict(version=1,date='2026-10-31',colorSize=[2048,2048],normalSize=[1024,1024],foliageSize=[1024,1024],paddingPixels=8,normalConvention='Tangent +Y; native GIMP independent neutral height, GEGL scale 12',slots=slots,foliageGrid=[4,4],authoring='Built-in imagegen original surfaces and foliage; native GIMP modular packing, palette, height and normal',legacyAtlasModified=False)
(DEST/'NP_CozyAtlasSlots.json').write_text(json.dumps(manifest,indent=2))
# URP shared surface mask: R metallic, G occlusion (neutral), A smoothness.
image=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB)
for s in slots:
 x,y,w,h=s['pixelRectTopLeft'];metal=.9 if s['name']=='Iron' else 1.0 if s['name']=='Aluminium' else 0.0;smooth=.45 if s['name']=='Aluminium' else .22 if s['name']=='Iron' else .12
 la=Gimp.Layer.new(image,s['name']+' surface',w,h,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);image.insert_layer(la,None,0);color=Gegl.Color.new('black');color.set_rgba(metal,1.0,0.0,1.0);Gimp.context_set_foreground(color);la.fill(Gimp.FillType.FOREGROUND);la.set_offsets(x,y);la.set_opacity(smooth*100)
xcf(image,SOURCE/'NP_CozySurfaces_01_SurfaceMask.xcf');png(image,DEST/'NP_CozySurfaces_01_SurfaceMask.png');image.delete()
print('COZY_ATLAS_GIMP_PASS',len(slots),flush=True)
# Preserve the approved rectangular-shingle revision when rebuilding the atlas.
roof_revision=SOURCE/'revise_roof_gimp.py'
if roof_revision.exists():exec(compile(roof_revision.read_text(encoding='utf-8'),str(roof_revision),'exec'),globals())
slate_revision=SOURCE/'rectangular_slate_gimp.py'
if slate_revision.exists():exec(compile(slate_revision.read_text(encoding='utf-8'),str(slate_revision),'exec'),globals())
