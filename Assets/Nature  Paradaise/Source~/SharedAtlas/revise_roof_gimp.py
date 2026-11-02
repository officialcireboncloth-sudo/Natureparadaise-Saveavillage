"""Native GIMP edit of the existing roof slot; other slot pixels are preserved."""
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
import json
ROOT=next(p for start in (Path.cwd(),Path(__file__).resolve().parent) for p in [start,*start.parents] if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise';SOURCE=A/'Source~/SharedAtlas';DEST=A/'Textures/Shared/Cozy'
BACKUP=ROOT/'ArtSource/HouseCozy/Revisions/2026-10-31-RectangularRoofUV';PREVIEW=ROOT/'ArtSource/HouseCozy/Previews'
def run(name,**values):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in values.items():c.set_property(k.replace('_','-'),v)
 r=p.run(c);assert r.index(0)==Gimp.PDBStatusType.SUCCESS,(name,str(r.index(0)))
def png(im,path):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(path)),compression=9,include_color_profile=False)
def xcf(im,path):run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(path)))
def load(path):return Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)))
Gimp.context_push();Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
image=load(SOURCE/'NP_CozySurfaces_01.xcf')
for layer in list(image.get_layers()):
 if layer.get_name().startswith('Roof olive rectangular '):image.remove_layer(layer)
core_path=SOURCE/'References/RoofOlive_PaintedCore.png'
if (BACKUP/'NP_CozySurfaces_01_BaseColor.png').exists():
 core=load(BACKUP/'NP_CozySurfaces_01_BaseColor.png');core.crop(56,46,100,585);png(core,core_path);core.delete()
# Reuse the old painted material interior without the curved boundary. The
# staggered rectangles have straight edges with tiny chips, not fish scales.
for row in range(4):
 for col in range(-1,5):
  left=col*128+(64 if row%2 else 0);top=row*120
  if left>=512 or left+128<=0:continue
  core=load(core_path);core.scale(128,120)
  visible_left=max(0,left);visible_right=min(512,left+128)
  if visible_left!=left or visible_right!=left+128:core.crop(visible_right-visible_left,120,visible_left-left,0)
  la=Gimp.Layer.new_from_drawable(core.get_layers()[0],image);image.insert_layer(la,None,0);la.set_name('Roof olive rectangular %d %d'%(row,col));la.set_offsets(visible_left,480+top);core.delete()
  tone=Gimp.DrawableFilter.new(la,'gegl:exposure','Subtle shingle variation');tone.get_config().set_property('exposure',[-.08,.035,-.025,.07][(row*3+col)%4]);la.merge_filter(tone)
  image.select_rectangle(Gimp.ChannelOps.REPLACE,0,480,512,480)
  Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 100'));Gimp.context_enable_dynamics(False);Gimp.context_set_opacity(100)
  for color,points,width in [('#434a35',[(left,480+top),(left+127,480+top)],4),('#434a35',[(left,480+top),(left+1,480+top+52),(left,480+top+118)],3),('#434a35',[(left+127,480+top),(left+126,480+top+75),(left+127,480+top+119)],3),('#8a8e68',[(left+3,480+top+115),(left+40,480+top+116),(left+44,480+top+114),(left+48,480+top+116),(left+124,480+top+115)],3)]:
   Gimp.context_set_foreground(Gegl.Color.new(color));Gimp.context_set_brush_size(width);Gimp.paintbrush_default(la,[float(v) for point in points for v in point])
Gimp.Selection.none(image)
xcf(image,SOURCE/'NP_CozySurfaces_01.xcf');png(image,DEST/'NP_CozySurfaces_01_BaseColor.png')
image.crop(512,480,0,480);png(image,PREVIEW/'RoofRectangular_Gimp.png');image.delete()
normal=load(SOURCE/'NP_CozySurfaces_01_Normal.xcf')
for layer in list(normal.get_layers()):
 if layer.get_name().startswith('Roof rectangular '):normal.remove_layer(layer)
height=Gimp.Layer.new(normal,'Roof rectangular height',256,240,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);normal.insert_layer(height,None,0);height.set_offsets(0,240)
Gimp.context_set_foreground(Gegl.Color.new('#808080'));height.fill(Gimp.FillType.FOREGROUND)
Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 050'));Gimp.context_set_opacity(100);Gimp.context_set_brush_size(2.5)
for row in range(4):
 y=240+row*60
 Gimp.context_set_foreground(Gegl.Color.new('#686868'));Gimp.paintbrush_default(height,[0.,float(y),255.,float(y)])
 for col in range(-1,5):
  x=col*64+(32 if row%2 else 0)
  if 0<=x<256:Gimp.paintbrush_default(height,[float(x),float(y),float(x),float(y+59)])
patch=height.copy();normal.insert_layer(patch,None,0);patch.set_name('Roof rectangular tangent normal +Y');f=Gimp.DrawableFilter.new(patch,'gegl:normal-map','Rectangular roof relief');c=f.get_config()
for k,v in [('scale',12.),('flip-x',False),('flip-y',False),('tileable',False)]:c.set_property(k,v)
patch.merge_filter(f);height.set_visible(False);xcf(normal,SOURCE/'NP_CozySurfaces_01_Normal.xcf');png(normal,DEST/'NP_CozySurfaces_01_Normal.png');normal.delete()
manifest=json.loads((DEST/'NP_CozyAtlasSlots.json').read_text())
for s in manifest['slots']:
 if s['name']=='RoofOlive':
  s['pattern']='Four staggered rows of rectangular shingles, native GIMP edit'
  s['singleTileUv']=[130/2048,1-598/2048,254/2048,1-482/2048]
  s['singleTileUvs']=[[(col*128+2)/2048,1-(480+row*120+118)/2048,((col+1)*128-2)/2048,1-(480+row*120+2)/2048] for row in (0,2) for col in range(4)]
manifest['revision']='2026-10-31 rectangular olive roof; original slot positions preserved'
(DEST/'NP_CozyAtlasSlots.json').write_text(json.dumps(manifest,indent=2))
Gimp.context_pop();print('RECTANGULAR_ROOF_GIMP_PASS',flush=True)
