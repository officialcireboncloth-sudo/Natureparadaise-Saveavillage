"""Contact sheets of current runtime meshes and the three matching seed packets."""
import sys,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_config import *
out=SOURCE/'Previews';kinds=['Blueberry','Raspberry','Blackberry'];stages=['Seed','Sprout','Young','Flowering','Ready','Harvest']
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def text(x,y,t,size=25):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="middle" fill="#314f55">{t}</text>'
def img(p,x,y,w,h):return f'<image x="{x}" y="{y}" width="{w}" height="{h}" xlink:href="{uri(p)}"/>'
def export(name,w,h,body):
 p=out/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f5f3ed"/>{body}</svg>',encoding='utf-8');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');c=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():c.set_property(k.replace('_','-'),v)
 assert proc.run(c).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('BERRY_REVIEW',name,flush=True)
body=text(1000,45,'G01 | Blueberry, Raspberry, Blackberry - mesh dan ikon terpasang',29)
for row,kind in enumerate(kinds):
 y=75+row*730
 for col,(p,label) in enumerate([(out/f'G01_{kind}_Ready.png',kind+' siap panen'),(out/f'G01_{kind}_FruitDetail.png','Hasil panen 3D'),(A/'Textures/UI/Icons/Seeds'/('icon_seed_'+kind.lower()+'.png'),'Benih - paper bag')]):
  x=20+col*660;body+=img(p,x,y,640,650)+text(x+320,y+690,label)
export('G01_AppliedPreview',2020,2290,body)
body=text(1065,47,'Raspberry | lima fase pertumbuhan dan model panen',31)
for i,stage in enumerate(stages):
 x=18+i*350;body+=img(out/f'G01_Raspberry_{stage}_Phase.png',x,85,335,570)+text(x+167,710,{'Young':'Young Bush','Ready':'Ready to Harvest'}.get(stage,stage),23)
export('G01_Raspberry_GrowthStages',2130,760,body)
body=text(780,47,'Raspberry | daun dalam ruang: atas, samping, sudut 3/4',29)
for row,(stage,label) in enumerate([('Young','Tanaman muda'),('Ready','Siap panen')]):
 y=105+row*690
 for col,view in enumerate(['Top','Side','Oblique']):body+=img(out/f'G01_Raspberry_{stage}_{view}.png',25+col*510,y,490,620)
 body+=text(780,y+655,label,26)
export('G01_Raspberry_SpatialReview',1560,1480,body)
body=text(1050,45,'G01 | detail tiga hasil panen 3D',29)
for i,kind in enumerate(kinds):body+=img(out/f'G01_{kind}_FruitDetail.png',20+i*700,80,670,650)+text(350+i*700,785,kind,26)
export('G01_FruitDetail',2120,830,body)
print('ALL_THREE_BERRY_CONTACT_SHEETS_PASS',flush=True)
