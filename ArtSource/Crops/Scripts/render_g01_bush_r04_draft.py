"""Compare the fuller shrub silhouette before publication."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
source=Path(__file__).with_name('render_g01_leaf_draft.py');t=source.read_text().replace("Leaf_R03/Draft.blend","Bush_R04/"+('Draft_Fast.blend' if '--fast' in sys.argv else 'Draft.blend')).replace("'Leaf_R03'","'Bush_R04'")
if '--fast' in sys.argv:t=t.replace('_Draft.png','_DraftFast.png')
exec(compile(t,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
