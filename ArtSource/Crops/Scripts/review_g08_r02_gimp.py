"""Compare unretouched wheat corrections and update the growth-stage sheet."""
import sys,json,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,text,size=24):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" fill="#29472b">{text}</text>'
def export(name,w,h,body):
 p=SOURCE/'Previews'/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f3f1e8"/>{body}</svg>',encoding='utf-8')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G08_R02_REVIEW',name,flush=True)
out=SOURCE/'Previews';baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());before=ROOT/baseline['snapshot']/'ArtSource/Crops/Previews'
body=label(35,45,'G08 R02 | Wheat stem and ear attachment',29)+label(210,85,'Previous',25)+label(1010,85,'Revised',25)
for row,stage in enumerate(['Heading','Harvest']):
 y=115+row*690;body+=label(35,y+30,stage,25)
 for x,folder in [(35,before),(815,out)]:body+=f'<image x="{x}" y="{y+45}" width="750" height="620" xlink:href="{uri(folder/("G08_Wheat_"+stage+"_Phase.png"))}"/>'
export('G08_Wheat_R02_Comparison',1610,1510,body)
body=label(35,45,'G08 R02 | Wheat Heading and Harvest - three views',29)
for row,stage in enumerate(['Heading','Harvest']):
 y=85+row*650;body+=label(35,y+25,stage,24)
 for i,title in enumerate(['Front','Side','Rear']):
  x=30+i*500;body+=f'<image x="{x}" y="{y+45}" width="475" height="555" xlink:href="{uri(out/("G08_Wheat_"+stage+"_R02_"+title+".png"))}"/>'+label(x+190,y+630,title,21)
export('G08_Wheat_R02_Alignment',1540,1410,body)
body=label(36,45,'G08 | Five growth phases and harvested crop',29)
for row,(kind,stages) in enumerate(PHASES.items()):
 y=80+row*670
 for col,stage in enumerate(stages):
  x=18+col*400;title={'Young':'Young Plant','Ready':'Ready to Harvest'}.get(stage,stage)
  body+=f'<image x="{x}" y="{y}" width="385" height="580" xlink:href="{uri(out/("G08_"+kind+"_"+stage+"_Phase.png"))}"/>'+label(x+22,y+625,kind+' / '+title,21)
export('G08_GrowthStages',2430,1430,body)
print('G08_R02_GIMP_REVIEW_PASS',flush=True)
