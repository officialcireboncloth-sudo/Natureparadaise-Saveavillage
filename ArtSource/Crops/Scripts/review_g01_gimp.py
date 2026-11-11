"""Contact sheets of the actual berry runtime meshes and matching paper-bag icons."""
import sys,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_config import *
out=SOURCE/'Previews'
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def text(x,y,t,size=25):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="middle" fill="#314f55">{t}</text>'
def export(name,w,h,body):
 p=out/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f5f3ed"/>{body}</svg>',encoding='utf-8');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');c=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():c.set_property(k.replace('_','-'),v)
 assert proc.run(c).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G01_REVIEW',name,flush=True)
for kind in PHASES:
 body=text(1065,47,'G01 | '+kind+' - pertumbuhan dan panen',31)
 for i,stage in enumerate(PHASES[kind]):
  x=18+i*350;body+=f'<image x="{x}" y="85" width="335" height="570" xlink:href="{uri(out/("G01_"+kind+"_"+stage+"_Phase.png"))}"/>'+text(x+167,710,{'Young':'Young Bush','Ready':'Ready to Harvest'}.get(stage,stage),23)
 export('G01_'+kind+'_GrowthStages',2130,760,body)
 body=text(780,47,kind+' | daun dalam ruang: atas, samping, sudut 3/4',29)
 for row,(stage,label) in enumerate([('Young','Tanaman muda'),('Ready','Siap panen')]):
  y=105+row*690
  for col,view in enumerate(['Top','Side','Oblique']):body+=f'<image x="{25+col*510}" y="{y}" width="490" height="620" xlink:href="{uri(out/("G01_"+kind+"_"+stage+"_"+view+".png"))}"/>'
  body+=text(780,y+655,label,26)
 export('G01_'+kind+'_SpatialReview',1560,1480,body)
body=text(1000,45,'G01 | Blueberry dan Blackberry - mesh dan ikon yang dipasang',29)
for row,kind in enumerate(PHASES):
 y=75+row*780
 for col,(p,label) in enumerate([(out/f'G01_{kind}_Ready.png',kind+' siap panen'),(out/f'G01_{kind}_Field.png','Sudut kamera game'),(A/'Textures/UI/Icons/Seeds'/('icon_seed_'+kind.lower()+'.png'),'Paper bag - '+kind)]):
  x=20+col*660;body+=f'<image x="{x}" y="{y}" width="640" height="700" xlink:href="{uri(p)}"/>'+text(x+320,y+742,label,25)
export('G01_AppliedPreview',2020,1645,body)
body=text(900,45,'G01 | detail buah dan hasil panen 3D',29)
for i,kind in enumerate(PHASES):
 body+=f'<image x="{20+i*900}" y="80" width="860" height="760" xlink:href="{uri(out/f"G01_{kind}_FruitDetail.png")}"/>'+text(450+i*900,885,kind,26)
export('G01_FruitDetail',1820,930,body);print('G01_GIMP_REVIEW_PASS',flush=True)
