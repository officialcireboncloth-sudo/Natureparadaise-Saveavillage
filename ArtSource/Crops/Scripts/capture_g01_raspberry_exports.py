"""Separate source-export proof; never replace the accepted two-species baseline."""
import shutil
import g01_raspberry_config as G
from unified_plant_atlas import *
out=G.REVIEW/'Raspberry/SourceExports';out.mkdir(parents=True,exist_ok=True);records={}
for p in G.MODELS.glob('NP_Raspberry*.json'):
 s=load(p);assert s['materials']==[G.ATLAS],p
 for q in [p,p.with_suffix('.fbx')]:shutil.copy2(q,out/q.name)
 records[p.stem]=dict(sourceSpec=rel(out/p.name),sourceFbx=rel(out/p.with_suffix('.fbx').name),geometrySha256=geometry_hash(s),specSha256=sha(p),triangles=s['triangles'])
assert len(records)==12
write(G.REVIEW/'Raspberry/ExportBaseline.json',dict(status='SOURCE_EXPORTS_CAPTURED',models=records));print('RASPBERRY_SOURCE_EXPORTS_CAPTURED',len(records),flush=True)
