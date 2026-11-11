"""Keep independent source-export proof for the traced Rubus leaf revision."""
import shutil
import g01_config as G
import unified_plant_atlas as U
W=G.REVIEW/'Leaf_R05';out=W/'SourceExports';out.mkdir(parents=True,exist_ok=True);records={}
for p in G.MODELS.glob('*.json'):
 spec=U.load(p)
 if spec['materials']==[U.NAME]:continue
 assert p.stem.split('_')[1] in ['Blackberry','Raspberry'],p
 for q in [p,p.with_suffix('.fbx')]:shutil.copy2(q,out/q.name)
 records[p.stem]=dict(sourceSpec=U.rel(out/p.name),sourceFbx=U.rel(out/p.with_suffix('.fbx').name),geometrySha256=U.geometry_hash(spec),specSha256=U.sha(p),triangles=spec['triangles'])
assert len(records)==16,len(records)
U.write(W/'ExportBaseline.json',dict(status='SOURCE_EXPORTS_CAPTURED',revision='R05 measured reference silhouettes preserved in near geometry',models=records));print('LEAF_R05_SOURCE_CAPTURED',len(records),flush=True)
