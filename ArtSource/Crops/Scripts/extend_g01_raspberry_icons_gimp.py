"""Reuse exactly the established paper bag layers, typography and mutation mark."""
import sys,runpy
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_integration_config as config
config.NEW=[('G01','Raspberry','raspberry','Raspberry')]
source=config.ART/'extend_g01_seed_packets_gimp.py'
t=source.read_text().replace('Generated/G01_Illustrations.png','Generated/G01_Raspberry_Illustration.png').replace('Generated/G01_AtlasCells.json','Generated/G01_Raspberry_AtlasCells.json').replace("ART/'G01_IconAudit.json'","ART/'G01_Raspberry_IconAudit.json'").replace('30 packets, two new editable source pairs','31 packets, Raspberry appended using the established renderer')
exec(compile(t,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
