"""Create cereal material and metadata without modifying earlier atlas pages."""
import re,json
from pathlib import Path
from g08_config import *
from unity_art_io import meta
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');assert p.is_file();meta(p,TEXTURES/('NP_Plants_Leafy_01_'+suffix+'.png'))
dest=A/'Material/Shared/Plants'/(ATLAS+'.mat');src=A/'Material/Shared/Plants/NP_Plants_Leafy_01.mat';text=src.read_text().replace('NP_Plants_Leafy_01',ATLAS)
for suffix in ['BaseColor','Normal','SurfaceMask']:
 def guid(page):return re.search(r'^guid: (\w+)',Path(str(TEXTURES/(page+'_'+suffix+'.png'))+'.meta').read_text(),re.M)[1]
 text=text.replace(guid('NP_Plants_Leafy_01'),guid(ATLAS))
text=re.sub(r'(_BumpScale:) [\d.]+',r'\g<1> 0.50',text);dest.write_text(text);meta(dest,main_id=2100000)
print('G08_UNITY_PAGE_PASS',flush=True)
