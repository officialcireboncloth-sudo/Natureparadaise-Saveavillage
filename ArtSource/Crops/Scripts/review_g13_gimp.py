"""Contact sheets from the actual exported runtime meshes and existing paper-bag UI style."""
import sys,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g13_config import *
out=SOURCE/'Previews'
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def text(x,y,t,size=25):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="middle" fill="#624480">{t}</text>'
def export(name,w,h,body):
 path=out/(name+'.svg');path.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f5f3ed"/>{body}</svg>',encoding='utf-8');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(path)));p=Gimp.get_pdb().lookup_procedure('file-png-export');c=p.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(path.with_suffix('.png'))),compression=9,include_color_profile=False).items():c.set_property(k.replace('_','-'),v)
 assert p.run(c).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G13_REVIEW',name,flush=True)
body=text(1065,47,'G13 | Lavender / Lavender - tahap pertumbuhan dan panen',31)
for i,stage in enumerate(PHASES['Lavender']):
 x=18+i*350;body+=f'<image x="{x}" y="85" width="335" height="570" xlink:href="{uri(out/("G13_Lavender_"+stage+"_Phase.png"))}"/>'+text(x+167,710,{'Young':'Young Plant','BudForming':'Bud Forming','Ready':'Ready to Harvest'}.get(stage,stage),23)
export('G13_GrowthStages',2130,760,body)
body=text(780,47,'Lavender | daun sage bervolume dan arah tumbuh dalam ruang',31)
for col,name in enumerate(['Atas','Samping','Sudut 3/4']):body+=text(270+col*510,103,name,24)
for row,(stage,label) in enumerate([('Young','Tanaman muda'),('Ready','Siap panen')]):
 y=130+row*690
 for col,view in enumerate(['Top','Side','Oblique']):body+=f'<image x="{25+col*510}" y="{y}" width="490" height="620" xlink:href="{uri(out/("G13_Lavender_"+stage+"_R03_"+view+".png"))}"/>'
 body+=text(780,y+655,label,26)
export('G13_SpatialReview',1560,1505,body)
body=text(900,48,'G13 | kuntum ungu bertingkat dan ikon bibit',30)
for i,(p,label) in enumerate([(out/'G13_Lavender_FlowerDetail.png','Detail bunga 3D'),(A/'Textures/UI/Icons/Seeds/icon_seed_lavender.png','Ikon bibit - paper bag')]):
 x=20+i*900;body+=f'<image x="{x}" y="90" width="860" height="690" xlink:href="{uri(p)}"/>'+text(x+430,825,label,25)
export('G13_FlowerAndSeed',1820,875,body)
body=text(1000,47,'G13 Lavender | satu atlas dan material bersama dengan tanaman lain',28)
for i,(p,label) in enumerate([(out/'G13_Lavender_Ready.png','Tanaman siap panen'),(out/'G13_Lavender_Field.png','Sudut kamera game'),(A/'Textures/UI/Icons/Seeds/icon_seed_lavender.png','Benih Lavender')]):
 x=20+i*660;body+=f'<image x="{x}" y="100" width="640" height="720" xlink:href="{uri(p)}"/>'+text(x+320,860,label,25)
export('G13_AppliedPreview',2020,910,body);print('G13_GIMP_REVIEW_PASS',flush=True)
