"""Render current exported shrub stages, silhouettes and the game-camera angle."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
source=Path(__file__).with_name('render_g01_leaf_r03.py');text=source.read_text().replace(" show(kind,'Flowering');frame(650,850,15,20);render(kind,'Flowering_Check',650,850)"," show(kind,'Ready');frame(650,850,15,52);render(kind,'Field',650,850)\n show(kind,'Flowering');frame(650,850,15,20);render(kind,'Flowering_Check',650,850)").replace('LEAF_R03_RUNTIME_RENDERS_PASS','BUSH_R04_RUNTIME_RENDERS_PASS');exec(compile(text,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
