"""Assemble labelled unretouched render reviews and reopen the three XCF masters."""
import sys,json,base64,hashlib
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g10_config import *
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,t,size=24,anchor='start'):return f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-family="sans-serif" font-size="{size}" fill="#29472b">{t}</text>'
def export(name,w,h,body):
 p=SOURCE/'Previews'/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f3f1e8"/>{body}</svg>',encoding='utf-8')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G10_REVIEW',name,flush=True)
files=[]
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));layers=im.get_layers();assert im.get_width()==im.get_height()==2048;assert len(layers)==(2 if suffix=='Normal' else 16)
 files.append(dict(file=str(p.relative_to(ROOT)),layers=len(layers),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));im.delete()
(REVIEW/'GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',files=files),indent=2))
out=SOURCE/'Previews';body=label(30,45,'G10 | Sunflower, Chrysanthemum, Dahlia, Marigold',30)
for row,(kind,stages) in enumerate(PHASES.items()):
 y=80+row*760;body+=label(35,y+30,kind,28)
 body+=f'<image x="20" y="{y+55}" width="2060" height="638" xlink:href="{uri(out/("G10_"+kind+"_Growth.png"))}"/>'
 for col,stage in enumerate(stages):body+=label(180+col*340,y+733,{'Young':'Young Plant','BudForming':'Bud Forming','Ready':'Ready to Harvest'}.get(stage,stage),22,'middle')
export('G10_GrowthStages',2100,3160,body)
body=label(30,45,'G10 | Species-specific sculpted flower heads',30)
for i,kind in enumerate(PHASES):
 x=20+i*540;body+=f'<image x="{x}" y="90" width="520" height="520" xlink:href="{uri(out/("G10_"+kind+"_FlowerDetail.png"))}"/>'+label(x+260,665,kind,26,'middle')
export('G10_FlowerDetail',2180,715,body)
body=label(35,45,'G10 | Actual model volume: front, side, rear and top',29)
for row,kind in enumerate(PHASES):
 y=80+row*630;body+=label(35,y+28,kind,26)
 for col,title in enumerate(['Ready','Side','Rear','Top']):
  x=20+col*420;body+=f'<image x="{x}" y="{y+45}" width="400" height="510" xlink:href="{uri(out/("G10_"+kind+"_"+title+".png"))}"/>'+label(x+140,y+598,'Front' if title=='Ready' else title,21)
export('G10_VolumeReview',1700,2620,body)
body=label(35,45,'G10 | Ready plants - neutral studio render',29)
for i,kind in enumerate(PHASES):
 x=20+i*490;body+=f'<image x="{x}" y="85" width="470" height="590" xlink:href="{uri(out/("G10_"+kind+"_Ready.png"))}"/>'+label(x+235,725,kind,25,'middle')
export('G10_ReadyReview',1980,775,body)
print('G10_GIMP_REVIEW_PASS',flush=True)
