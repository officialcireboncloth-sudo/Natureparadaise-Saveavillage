"""Present the actual exported foliage from three perpendicular/oblique views."""
import runpy,sys,base64
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
runpy.run_path(str(SOURCE/'Scripts/review_g11_gimp.py'))
out=SOURCE/'Previews'
def label(x,y,text,size=24):return f'<text x="{x}" y="{y}" font-family="sans-serif" font-size="{size}" text-anchor="middle" fill="#315633">{text}</text>'
body=label(780,48,'Tulip | posisi dan lengkungan daun 3D',32)+label(780,83,'Mesh ekspor yang sama, dilihat dari tiga arah',21)
for col,title in enumerate(['Atas','Samping','Sudut 3/4']):body+=label(270+510*col,130,title,25)
for row,(stage,title) in enumerate([('Young','Tanaman muda'),('Ready','Siap panen')]):
 y=155+690*row
 for col,angle in enumerate(['Top','Side','Oblique']):
  p=out/f'G11_Tulip_{stage}_R03_{angle}.png';assert p.is_file(),p
  uri='data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode();body+=f'<image x="{25+510*col}" y="{y}" width="490" height="620" xlink:href="{uri}"/>'
 body+=label(780,y+655,title,26)
svg=out/'G11_R03_SpatialReview.svg';svg.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1560" height="1530"><rect width="1560" height="1530" fill="#f4f2e9"/>{body}</svg>',encoding='utf-8')
im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(svg)));proc=Gimp.get_pdb().lookup_procedure('file-png-export');cfg=proc.create_config()
for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(svg.with_suffix('.png'))),compression=9,include_color_profile=False).items():cfg.set_property(k.replace('_','-'),v)
assert proc.run(cfg).index(0)==Gimp.PDBStatusType.SUCCESS;im.delete();print('G11_R03_GIMP_REVIEW_PASS',flush=True)
