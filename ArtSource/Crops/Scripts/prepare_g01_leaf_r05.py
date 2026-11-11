"""Immutable checkpoint before reference-traced leaf silhouettes and Blueberry restoration."""
from pathlib import Path
source=Path(__file__).with_name('prepare_g01_bush_r04.py');text=source.read_text().replace('Bush_R04','Leaf_R05').replace('BUSH_R04','LEAF_R05');exec(compile(text,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
