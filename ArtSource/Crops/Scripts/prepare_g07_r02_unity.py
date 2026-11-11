"""Create the independent leafy material and retain all earlier Trellis surfaces."""
import json,re,shutil
from pathlib import Path
from g07_config import *
from unity_art_io import meta
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');assert p.is_file(),p
 meta(p,TEXTURES/('NP_Plants_Trellis_01_'+suffix+'.png'))
dest=A/'Material/Shared/Plants'/(ATLAS+'.mat');src=A/'Material/Shared/Plants/NP_Plants_Trellis_01.mat'
text=src.read_text().replace('NP_Plants_Trellis_01',ATLAS)
for suffix in ['BaseColor','Normal','SurfaceMask']:
 a=re.search(r'^guid: (\w+)',Path(str(TEXTURES/('NP_Plants_Trellis_01_'+suffix+'.png'))+'.meta').read_text(),re.M)[1]
 b=re.search(r'^guid: (\w+)',Path(str(TEXTURES/(ATLAS+'_'+suffix+'.png'))+'.meta').read_text(),re.M)[1]
 text=text.replace(a,b)
text=re.sub(r'(_BumpScale:) [\d.]+',r'\g<1> 0.55',text);dest.write_text(text);meta(dest,main_id=2100000)
# Restore the complete, byte-identical page from before G07. Older groups never used the five R01 slots.
original=json.loads((REVIEW/'Baseline.json').read_text());old=ROOT/original['snapshot']
for p in [*TEXTURES.glob('NP_Plants_Trellis_01_*'),*(SOURCE/'Textures').glob('NP_Plants_Trellis_01_*.xcf')]:
 saved=old/p.relative_to(ROOT)
 if saved.is_file():shutil.copy2(saved,p)
print('G07_R02_UNITY_PAGE_PASS independent leafy material; original Trellis bytes restored',flush=True)
