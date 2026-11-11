"""Use the accepted runtime geometry renderer for the new Raspberry phase roots."""
import sys,runpy
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as config
config.PHASES['Raspberry']=['Seed','Sprout','Young','Flowering','Ready','Harvest']
if '--all-berries' not in sys.argv:sys.argv+=['--only','Raspberry']
runpy.run_path(str(Path(__file__).with_name('render_g01.py')),run_name='__main__')
