"""Compose labelled actual model renders and reopen all editable GIMP masters."""
import sys,json,base64,hashlib
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def export(name,w,h,body):
 p=SOURCE/'Previews'/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f3f1e8"/>{body}</svg>',encoding='utf-8')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete()
def label(x,y,text,size=24):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" fill="#29472b">{text}</text>'
files=[]
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=SOURCE/'Textures'/(ATLAS+'_'+suffix+'.xcf');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));layers=im.get_layers();assert im.get_width()==im.get_height()==2048;assert len(layers)==(2 if suffix=='Normal' else 16)
 if suffix=='Normal':assert sum(l.get_visible() for l in layers)==1
 files.append(dict(file=str(p.relative_to(ROOT)),layers=len(layers),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));im.delete()
(REVIEW/'GimpReopenAudit.json').write_text(json.dumps(dict(status='PASS',files=files),indent=2))
out=SOURCE/'Previews'
body=label(35,45,'G08 | Rice and Wheat - actual 3D model renders',29)
for i,kind in enumerate(PHASES):
 x=25+i*900;body+=f'<image x="{x}" y="80" width="880" height="1033" xlink:href="{uri(out/("G08_"+kind+"_Ready.png"))}"/>'+label(x+380,1155,kind,28)
export('G08_ReadyReview',1830,1190,body)
body=label(40,46,'G08 | Front, side, rear and top',29)
for row,kind in enumerate(PHASES):
 y=80+row*720;body+=label(40,y+30,kind,27)
 for col,(key,title) in enumerate([('Front','Front'),('Side','Side'),('Rear','Rear'),('Top','Top')]):
  x=38+col*430;body+=f'<image x="{x}" y="{y+55}" width="410" height="600" xlink:href="{uri(out/("G08_"+kind+"_"+key+".png"))}"/>'+label(x+155,y+688,title,21)
export('G08_VolumeReview',1760,1540,body)
body=label(36,45,'G08 | Five growth phases and harvested crop',29)
for row,(kind,stages) in enumerate(PHASES.items()):
 y=80+row*670
 for col,stage in enumerate(stages):
  x=18+col*400;title={'Young':'Young Plant','Ready':'Ready to Harvest'}.get(stage,stage)
  body+=f'<image x="{x}" y="{y}" width="385" height="580" xlink:href="{uri(out/("G08_"+kind+"_"+stage+"_Phase.png"))}"/>'+label(x+22,y+625,kind+' / '+title,21)
export('G08_GrowthStages',2430,1430,body)
body=label(38,46,'G08 | Grain ribs, kernel seams and parallel leaf fibres',28)
atlas=uri(TEXTURES/(ATLAS+'_BaseColor.png'));normal=uri(TEXTURES/(ATLAS+'_Normal.png'));spec=manifest()
body+=f'<defs><image id="color-atlas" width="2048" height="2048" xlink:href="{atlas}"/><image id="normal-atlas" width="2048" height="2048" xlink:href="{normal}"/></defs>'
for i,name in enumerate(['RiceGold','WheatGold','LeafGreen','RiceGreen','WheatGreen','LeafGold']):
 x=35+(i%3)*500;y=85+(i//3)*370;slot=next(s for s in spec['slots'] if s['name']==name);a,b,w,h=slot['pixelRectTopLeft'];body+=label(x,y+25,name,23)
 for offset,key,title in [(0,'color-atlas','Colour'),(238,'normal-atlas','Normal')]:body+=f'<svg x="{x+offset}" y="{y+50}" width="230" height="230" overflow="hidden" viewBox="{a} {b} {w} {h}"><use xlink:href="#{key}"/></svg>'+label(x+offset+60,y+310,title,19)
export('G08_SurfaceDetail',1540,840,body)
body=label(36,45,'G08 | Actual sculpted husks and grooved kernels',29)
for i,kind in enumerate(PHASES):
 x=20+i*940;body+=f'<image x="{x}" y="75" width="920" height="700" xlink:href="{uri(out/("G08_"+kind+"_SeedDetail.png"))}"/>'+label(x+370,815,kind,26)
export('G08_GrainDetail',1900,850,body)
print('G08_GIMP_REVIEW_PASS three editable masters and labelled actual renders',flush=True)
