"""Review the new measured leaf blades and restored Blueberry surface before publishing."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
source=Path(__file__).with_name('render_g01_leaf_draft.py');text=source.read_text().replace('Leaf_R03','Leaf_R05');exec(compile(text,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
