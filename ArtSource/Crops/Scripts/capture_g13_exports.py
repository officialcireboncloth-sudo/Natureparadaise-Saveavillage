"""Capture current unpublished source exports before shared-atlas remapping."""
import shutil
import g13_config as g
from unified_plant_atlas import *
out=g.REVIEW/'SourceExports';out.mkdir(parents=True,exist_ok=True);records={}
for p in g.MODELS.glob('*.json'):
 s=load(p);assert s['materials']==[g.ATLAS]
 for q in [p,p.with_suffix('.fbx')]:shutil.copy2(q,out/q.name)
 records[p.stem]=dict(sourceSpec=rel(out/p.name),sourceFbx=rel(out/p.with_suffix('.fbx').name),geometrySha256=geometry_hash(s),specSha256=sha(p),triangles=s['triangles'])
write(g.REVIEW/'ExportBaseline.json',dict(status='SOURCE_EXPORTS_CAPTURED',models=records));print('G13_SOURCE_EXPORTS_CAPTURED',len(records),flush=True)
