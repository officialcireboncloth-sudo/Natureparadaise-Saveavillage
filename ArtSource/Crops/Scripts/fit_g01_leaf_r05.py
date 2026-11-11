"""Fit only the two revised Rubus silhouettes; Blueberry geometry/transforms stay exact."""
from pathlib import Path
source=Path(__file__).with_name('fit_g01_leaf_r03.py');text=source.read_text().replace('Leaf_R03','Leaf_R05').replace('LEAF_R03','LEAF_R05').replace("if row['group']!='G01':continue","if row['key'] not in ['raspberry','blackberry']:continue");exec(compile(text,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
