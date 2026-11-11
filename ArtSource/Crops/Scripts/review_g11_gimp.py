"""Label the actual exported tulip renders and reopen its editable texture masters."""
import json,sys,base64,hashlib
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,t,size=25,anchor='start'):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="{anchor}" fill="#315633">{t}</text>'
def export(name,w,h,body):
 p=SOURCE/'Previews'/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f4f2e9"/>{body}</svg>')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G11_REVIEW',name,flush=True)
files=[]
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));assert (im.get_width(),im.get_height())==(2048,1024);layers=im.get_layers();assert len(layers)==(2 if suffix=='Normal' else 8);files.append(dict(file=str(p.relative_to(ROOT)),layers=len(layers),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));im.delete()
(REVIEW/'GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',files=files),indent=2))
out=SOURCE/'Previews';body=label(35,48,'G11 | Tulip - five growth phases and harvested flower',30)
for i,stage in enumerate(PHASES['Tulip']):
 x=18+i*350;body+=f'<image x="{x}" y="90" width="335" height="570" xlink:href="{uri(out/("G11_Tulip_"+stage+"_Phase.png"))}"/>'+label(x+167,710,{'Young':'Young Plant','BudForming':'Bud Forming','Ready':'Ready to Harvest'}.get(stage,stage),23,'middle')
export('G11_GrowthStages',2130,760,body)
body=label(35,45,'G11 | Tulip cup and three-dimensional form',30)
for i,title in enumerate(['Ready','Side','Rear','Top']):
 x=20+i*450;body+=f'<image x="{x}" y="90" width="430" height="570" xlink:href="{uri(out/("G11_Tulip_"+title+".png"))}"/>'+label(x+215,705,'Front' if title=='Ready' else title,24,'middle')
export('G11_VolumeReview',1830,755,body)
body=label(30,45,'G11 | Rounded pink cup, layered tepals and curved cut stem',29)
for i,(p,title) in enumerate([('FlowerDetail','Sculpted flower cup'),('Harvest_Phase','Harvest model')]):
 x=20+i*730;body+=f'<image x="{x}" y="85" width="710" height="710" xlink:href="{uri(out/("G11_Tulip_"+p+".png"))}"/>'+label(x+355,840,title,26,'middle')
export('G11_FlowerAndHarvest',1490,890,body);print('G11_GIMP_REVIEW_PASS',flush=True)
