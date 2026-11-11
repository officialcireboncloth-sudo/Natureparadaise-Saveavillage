"""Retain the existing tile footprint for the revised spatial silhouettes."""
from pathlib import Path
source=Path(__file__).with_name('fit_g01_leaf_r03.py');text=source.read_text().replace('Leaf_R03','Bush_R04').replace('LEAF_R03','BUSH_R04');exec(compile(text,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
