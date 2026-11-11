"""Compare actual previous/current runtime foliage and the revised spatial silhouettes."""
import sys,base64,ast
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_config import *
out=SOURCE/'Previews';R=REVIEW/'Leaf_R03';kinds=['Blueberry','Raspberry','Blackberry']
# Share the exact established SVG-to-GIMP contact sheet renderer.
source=Path(__file__).with_name('review_g01_raspberry_gimp.py');tree=ast.parse(source.read_text());nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef)];exec(compile(ast.Module(body=nodes,type_ignores=[]),str(source),'exec'))
body=text(1000,45,'G01 | koreksi bentuk, tekstur UV, proporsi dan arah daun',29)
for row,tag in enumerate(['Sebelum','Sesudah']):
 for col,kind in enumerate(kinds):
  p=R/'Before'/(kind+'_Ready.png') if row==0 else out/('G01_'+kind+'_Ready.png');x=20+col*660;y=70+row*760;body+=img(p,x,y,640,680)+text(x+320,y+725,kind+' - '+tag,25)
export('G01_Leaf_R03_BeforeAfter',2020,1620,body)
body=text(1000,45,'G01 R03 | tajuk menyebar dalam ruang',29)
for row,kind in enumerate(kinds):
 for col,view in enumerate(['Top','Side','Oblique']):
  x=20+col*660;y=70+row*720;body+=img(out/f'G01_{kind}_Ready_{view}.png',x,y,640,650)+text(x+320,y+685,kind+' - '+view,25)
export('G01_Leaf_R03_SpatialReview',2020,2240,body)
# Refresh the existing primary preview without changing any icon or harvest artwork.
body=text(1000,45,'G01 R03 | Blueberry, Raspberry, Blackberry',29)
for row,kind in enumerate(kinds):
 for col,(p,label) in enumerate([(out/f'G01_{kind}_Ready.png',kind+' siap panen'),(out/f'G01_{kind}_FruitDetail.png','Hasil panen 3D'),(A/'Textures/UI/Icons/Seeds'/('icon_seed_'+kind.lower()+'.png'),'Benih - paper bag')]):
  x=20+col*660;y=75+row*730;body+=img(p,x,y,640,650)+text(x+320,y+690,label)
export('G01_AppliedPreview',2020,2290,body)
for kind in kinds:
 body=text(1065,47,kind+' R03 | pertumbuhan dan panen',31)
 for i,stage in enumerate(['Seed','Sprout','Young','Flowering','Ready','Harvest']):
  x=18+i*350;body+=img(out/f'G01_{kind}_{stage}_Phase.png',x,85,335,570)+text(x+167,710,{'Young':'Young Bush','Ready':'Ready to Harvest'}.get(stage,stage),23)
 export('G01_'+kind+'_GrowthStages',2130,760,body)
print('LEAF_R03_CONTACT_SHEETS_PASS',flush=True)
