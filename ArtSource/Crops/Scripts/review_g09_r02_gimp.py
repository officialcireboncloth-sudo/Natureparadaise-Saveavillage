"""Compare the saved R01 renders with the current R02 models without image retouching."""
import sys,json,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
baseline=json.loads((REVIEW/'R02_Baseline.json').read_text())
before=ROOT/baseline['snapshot']/SOURCE.relative_to(ROOT)/'Previews'
out=SOURCE/'Previews'

def uri(p):return 'data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
def label(x,y,t,size=25):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" fill="#29472b">{t}</text>'
def export(name,w,h,body):
 p=out/(name+'.svg');p.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{w}" height="{h}"><rect width="{w}" height="{h}" fill="#f3f1e8"/>{body}</svg>',encoding='utf-8')
 im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
 for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
 assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G09_R02_COMPARISON',name,flush=True)

for suffix,name,cellw,cellh in [('Ready','Comparison',650,740),('FlowerDetail','FlowerComparison',650,650)]:
 body=label(30,40,'G09 | R01 / R02 - actual Blender renders',29)
 body+=label(250,85,'Before - R01')+label(900,85,'Revised - R02')
 for row,kind in enumerate(PHASES):
  y=110+row*(cellh+55)
  for col,folder in enumerate([before,out]):
   body+=f'<image x="{20+col*cellw}" y="{y}" width="{cellw-20}" height="{cellh}" xlink:href="{uri(folder/('G09_'+kind+'_'+suffix+'.png'))}"/>'
  body+=label(35,y+cellh+34,kind)
 export('G09_R02_'+name,cellw*2+20,130+3*(cellh+55),body)
print('G09_R02_COMPARISONS_PASS',flush=True)
