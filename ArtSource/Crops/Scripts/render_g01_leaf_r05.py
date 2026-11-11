"""Refresh exported planted-stage, front, game and spatial views for the corrected leaves."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
G.PHASES['Raspberry']=['Seed','Sprout','Young','Flowering','Ready','Harvest']
source=Path(__file__).with_name('render_g01.py');text=source.read_text().split("if '--spatial-only' in sys.argv:",1)[0];exec(compile(text,str(source),'exec'),globals())
for kind in ['Blueberry','Raspberry','Blackberry']:
 if '--rubus-only' in sys.argv and kind=='Blueberry':continue
 for stage in ['Sprout','Young','Flowering','Ready']:
  show(kind,stage);aim((0,0,.56),0,18,1.75);render(kind,stage+'_Phase',480,660)
 show(kind,'Ready');frame(650,850);render(kind,'Ready',650,850)
 for view,angle,e in [('Top',0,90),('Side',90,10),('Oblique',40,38)]:frame(540,680,angle,e);render(kind,'Ready_'+view,540,680)
 show(kind,'Ready');frame(650,850,15,52);render(kind,'Field',650,850)
 show(kind,'Flowering');frame(650,850,15,20);render(kind,'Flowering_Check',650,850)
print('LEAF_R05_RUNTIME_RENDERS_PASS',flush=True)
