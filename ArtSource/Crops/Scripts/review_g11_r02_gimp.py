"""Visible comparison of the delivered and revised actual runtime meshes."""
import sys,json,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
before=ROOT/json.loads((REVIEW/'R02_Baseline.json').read_text())['snapshot']/'ArtSource/Crops/Previews';out=SOURCE/'Previews'
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,t,size=26):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="middle" fill="#315633">{t}</text>'
def export(name,w,h,body):
 p=out/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f4f2e9"/>{body}</svg>')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G11_R02_REVIEW',name,flush=True)
body=label(700,46,'Tulip | petal shape, leaf placement and surface revision',29)
for i,(folder,title) in enumerate([(before,'Before'),(out,'Revised')]):
 x=20+i*690;body+=f'<image x="{x}" y="85" width="670" height="850" xlink:href="{uri(folder/"G11_Tulip_Ready.png")}"/>'+label(x+335,980,title)
export('G11_R02_Comparison',1400,1030,body)
body=label(700,45,'Tulip | front and side of the revised runtime mesh',29)
for i,(name,title) in enumerate([('Ready','Front'),('Side','Side')]):
 x=20+i*690;body+=f'<image x="{x}" y="85" width="670" height="850" xlink:href="{uri(out/("G11_Tulip_"+name+".png"))}"/>'+label(x+335,980,title)
export('G11_R02_ReadyReview',1400,1030,body)
print('G11_R02_GIMP_REVIEW_PASS',flush=True)
