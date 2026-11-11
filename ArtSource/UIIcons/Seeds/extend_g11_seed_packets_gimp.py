"""Prepare generated illustrations as UI sprites inside the established GIMP packets."""
import sys,ast,math,json,hashlib
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'ArtSource/Crops/Scripts'))
from g11_integration_config import *
SIZE=512;INK='#42513a';CREAM='#f6ecd5'
ns=dict(Path=Path,math=math,json=json,hashlib=hashlib,Gimp=Gimp,Gegl=Gegl,Gio=Gio,SIZE=SIZE,INK=INK,CREAM=CREAM,ROOT=ROOT,ART=ART,DEST=DEST)
helpers=ROOT/'ArtSource/UIIcons/HandPainted/draw_icons_gimp.py';tree=ast.parse(helpers.read_text());nodes=[n for n in tree.body if isinstance(n,(ast.FunctionDef,ast.ClassDef)) and n.name in ['path','oval','Drawing','export_png']];exec(compile(ast.Module(body=nodes,type_ignores=[]),str(helpers),'exec'),ns)
old=ART/'draw_seed_packets_gimp.py';tree=ast.parse(old.read_text());nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ['leaf','text','packet','save_master','runtime']];exec(compile(ast.Module(body=nodes,type_ignores=[]),str(old),'exec'),ns)
Drawing=ns['Drawing'];export_png=ns['export_png'];source=ART/'Generated/G11_Illustrations.png';atlas=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(source)));w,h=atlas.get_width(),atlas.get_height();assert w==h
manifest=json.loads((ART/'Manifest.json').read_text());existing={r['plant']:r for r in manifest['icons']};records=[]
cells=json.loads((ART/'Generated/G11_AtlasCells.json').read_text())['cells']
Gimp.context_push()
try:
 Gimp.context_set_antialias(True);Gimp.context_set_feather(False);Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
 for i,(_,kind,key,label) in enumerate(NEW):
  x0,y0,cw,ch=cells[i]['pixelRectTopLeft'];x1,y1=x0+cw,y0+ch;assert cells[i]['key']==key
  cell=atlas.duplicate();cell.crop(x1-x0,y1-y0,x0,y0);cell.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
  left,top,right,bottom=cells[i]['contentRectInCell'];cell.crop(right-left,bottom-top,left,top);ratio=224/max(right-left,bottom-top);cw,ch=max(1,round((right-left)*ratio)),max(1,round((bottom-top)*ratio));cell.scale(cw,ch);cell.resize(256,256,(256-cw)//2,(256-ch)//2)
  # A transparent full-canvas layer makes the merged drawable retain its safe margins.
  canvas=Gimp.Layer.new(cell,'Transparent 256px canvas',256,256,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);cell.insert_layer(canvas,None,len(cell.get_layers()));cell.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
  crop=Drawing('Crop_'+key);full=Gimp.Layer.new_from_drawable(cell.get_layers()[0],crop.image);crop.image.insert_layer(full,None,0);full.scale(512,512,False);full.set_offsets(0,0);full.set_name('Generated illustration - '+label)
  master=ns['save_master'](crop,'Crop_'+key);sprite=ns['runtime'](crop.image,DEST/'Crops'/('icon_crop_'+key+'.png'))
  star=Drawing('Mutation_'+key);layer=Gimp.Layer.new_from_drawable(crop.image.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE),star.image);star.image.insert_layer(layer,None,0)
  points=[]
  for j in range(10):a=-math.pi/2+j*math.pi/5;r=45 if j%2==0 else 22;points.append((397+math.cos(a)*r,391+math.sin(a)*r))
  star.poly('Mutation star',points,'#d7ac43');mutated=ns['runtime'](star.image,DEST/'Crops'/('icon_crop_'+key+'_mutation.png'));mutated.delete();star.image.delete()
  packet=ns['packet'](key,label.upper(),sprite);seedmaster=ns['save_master'](packet,'Seed_'+key);seed=ns['runtime'](packet.image,DEST/'Seeds'/('icon_seed_'+key+'.png'))
  check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(seedmaster)));assert check.get_width()==512 and len(check.get_layers())>12;layers=len(check.get_layers());check.delete()
  check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(master)));assert check.get_width()==512;check.delete()
  record=dict(plant=key,label=label.upper(),size=[256,256],seedMaster=rel(seedmaster),produceMaster=rel(master),layers=layers,seed=rel(DEST/'Seeds'/('icon_seed_'+key+'.png')),seedSha256=digest(DEST/'Seeds'/('icon_seed_'+key+'.png')),illustrationSource=rel(source),atlasCell=[x0,y0,x1-x0,y1-y0]);existing[key]=record;records.append(record)
  seed.delete();sprite.delete();packet.image.delete();crop.image.delete();cell.delete();print('GIMP_ADDED_SEED_ICON',key,flush=True)
 # Combined preview uses the exact current runtime sprites, including the twelve preserved packets.
 sheet=Gimp.Image.new(2016,1152,Gimp.ImageBaseType.RGB);bg=Gimp.Layer.new(sheet,'Sage review background',2016,1152,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);sheet.insert_layer(bg,None,0);Gimp.context_set_foreground(Gegl.Color.new('#344b3d'));bg.edit_fill(Gimp.FillType.FOREGROUND)
 for i,r in enumerate(existing.values()):
  im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(ROOT/r['seed'])));layer=Gimp.Layer.new_from_drawable(im.get_layers()[0],sheet);sheet.insert_layer(layer,None,0);layer.set_name(r['label']);layer.set_offsets((i%7)*288+16,(i//7)*288+16);im.delete()
 export_png(sheet,ART/'SeedPackets_Preview.png');assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,sheet,Gio.File.new_for_path(str(ART/'SeedPackets_Review.xcf')));sheet.delete()
 manifest.update(status='PASS',renderer='Existing GIMP packet layers and text; G06-G11 generated illustrations normalized in GIMP',icons=list(existing.values()));(ART/'Manifest.json').write_text(json.dumps(manifest,indent=2))
 (ART/'G11_IconAudit.json').write_text(json.dumps(dict(status='PASS',newPackets=1,newProduceSprites=1,newMutationSprites=1,seedMastersReopened=1,produceMastersReopened=1,source=rel(source),sourceSha256=digest(source),records=records),indent=2))
 print('GIMP_G11_SEED_PASS 26 packets, one new editable source pair',flush=True)
finally:
 atlas.delete();Gimp.context_pop()
