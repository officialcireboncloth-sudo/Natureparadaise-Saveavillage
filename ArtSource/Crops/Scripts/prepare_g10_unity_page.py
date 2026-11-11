"""Create the G10 material and texture metadata from the accepted G09 settings."""
import re
from pathlib import Path
from g10_config import *
from unity_art_io import meta
old='NP_Plants_ShrubFlowers_01'
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');assert p.is_file();meta(p,TEXTURES/(old+'_'+suffix+'.png'))
dest=A/'Material/Shared/Plants'/(ATLAS+'.mat');text=(dest.parent/(old+'.mat')).read_text().replace(old,ATLAS)
for suffix in ['BaseColor','Normal','SurfaceMask']:
 def guid(page):return re.search(r'^guid: (\w+)',Path(str(TEXTURES/(page+'_'+suffix+'.png'))+'.meta').read_text(),re.M)[1]
 text=text.replace(guid(old),guid(ATLAS))
text=re.sub(r'(_BumpScale:) [\d.]+',r'\g<1> 0.22',text);text=re.sub(r'(_Cull:) \d+',r'\g<1> 0',text);dest.write_text(text);meta(dest,main_id=2100000)
print('G10_UNITY_MATERIAL_PASS',flush=True)
