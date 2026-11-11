"""Assemble labelled true model renders and reopen all editable G09 texture sources."""
import sys,json,base64,hashlib
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,text,size=24):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" fill="#29472b">{text}</text>'
def export(name,w,h,body):
 p=SOURCE/'Previews'/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f3f1e8"/>{body}</svg>',encoding='utf-8')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G09_REVIEW',name,flush=True)
files=[]
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));layers=im.get_layers();assert im.get_width()==im.get_height()==2048;assert len(layers)==(2 if suffix=='Normal' else 16)
 if suffix=='Normal':assert sum(l.get_visible() for l in layers)==1
 files.append(dict(file=str(p.relative_to(ROOT)),layers=len(layers),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));im.delete()
(REVIEW/'GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',files=files),indent=2))
out=SOURCE/'Previews';body=label(36,45,'G09 | Five growth phases and cut flowers',29)
for row,(kind,stages) in enumerate(PHASES.items()):
 y=80+row*670
 for col,stage in enumerate(stages):
  x=18+col*400;title={'Young':'Young Plant','BudForming':'Bud Forming','Ready':'Ready to Harvest'}.get(stage,stage);body+=f'<image x="{x}" y="{y}" width="385" height="580" xlink:href="{uri(out/("G09_"+kind+"_"+stage+"_Phase.png"))}"/>'+label(x+22,y+625,kind+' / '+title,21)
export('G09_GrowthStages',2430,2100,body)
body=label(40,46,'G09 | Front, side, rear and top - actual 3D volume',29)
for row,kind in enumerate(PHASES):
 y=80+row*650;body+=label(40,y+30,kind,27)
 for col,title in enumerate(['Front','Side','Rear','Top']):
  x=30+col*430;body+=f'<image x="{x}" y="{y+45}" width="410" height="530" xlink:href="{uri(out/("G09_"+kind+"_"+title+".png"))}"/>'+label(x+155,y+618,title,21)
export('G09_VolumeReview',1760,2050,body)
body=label(35,45,'G09 | Spiral rose, hibiscus funnel and double jasmine',29)
for i,kind in enumerate(PHASES):
 x=20+i*690;body+=f'<image x="{x}" y="85" width="670" height="670" xlink:href="{uri(out/("G09_"+kind+"_FlowerDetail.png"))}"/>'+label(x+240,805,kind,27)
export('G09_FlowerDetail',2110,850,body)
body=label(35,45,'G09 | Petal fibres and registered branching leaf veins',29);spec=manifest();atlas=uri(TEXTURES/(ATLAS+'_BaseColor.png'));normal=uri(TEXTURES/(ATLAS+'_Normal.png'))
body+=f'<defs><image id="colour" width="2048" height="2048" xlink:href="{atlas}"/><image id="normal" width="2048" height="2048" xlink:href="{normal}"/></defs>'
for i,name in enumerate(['RosePetal','HibiscusPetal','JasminePetal','RoseLeaf','HibiscusLeaf','JasmineLeaf']):
 x=30+i%3*500;y=85+i//3*370;s=next(s for s in spec['slots'] if s['name']==name);a,b,w,h=s['pixelRectTopLeft'];body+=label(x,y+25,name,23)
 for offset,key,title in [(0,'colour','Colour'),(238,'normal','Normal')]:body+=f'<svg x="{x+offset}" y="{y+50}" width="230" height="230" overflow="hidden" viewBox="{a} {b} {w} {h}"><use xlink:href="#{key}"/></svg>'+label(x+offset+60,y+310,title,19)
export('G09_SurfaceDetail',1540,840,body);print('G09_GIMP_REVIEW_PASS',flush=True)
