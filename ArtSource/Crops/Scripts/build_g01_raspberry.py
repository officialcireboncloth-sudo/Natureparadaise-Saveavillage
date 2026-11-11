"""Append Raspberry without rebuilding either accepted berry species."""
import sys,runpy
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as config
import g01_raspberry_config as raspberry
import g01_geometry as geometry
import g01_raspberry_geometry as rgeo
original=geometry.build
def dispatch(kind,stage):
 if kind=='Raspberry':
  if stage=='Seed':
   # Seed geometry is shared, but remains an independently editable phase.
   return original(kind,stage)
  return rgeo.build(kind,stage)
 return original(kind,stage)
geometry.build=dispatch
config.PHASES['Raspberry']=['Seed','Sprout','Young','Flowering','Ready','Harvest']
config.ATLAS=raspberry.ATLAS;config.configure=raspberry.configure
sys.argv += ['--only','Raspberry']
runpy.run_path(str(Path(__file__).with_name('build_g01_blender.py')),run_name='__main__')
