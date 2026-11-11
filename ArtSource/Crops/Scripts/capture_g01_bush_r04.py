"""Separate source proof for the fuller shrub revision, retaining all earlier proofs."""
import shutil
import g01_config as G
import unified_plant_atlas as U
W=G.REVIEW/'Bush_R04';out=W/'SourceExports';out.mkdir(parents=True,exist_ok=True);records={}
for p in G.MODELS.glob('*.json'):
 spec=U.load(p)
 if spec['materials']==[U.NAME]:continue
 assert p.stem.split('_')[2] in ['Sprout','Young','Flowering','Ready'],p
 for q in [p,p.with_suffix('.fbx')]:shutil.copy2(q,out/q.name)
 records[p.stem]=dict(sourceSpec=U.rel(out/p.name),sourceFbx=U.rel(out/p.with_suffix('.fbx').name),geometrySha256=U.geometry_hash(spec),specSha256=U.sha(p),triangles=spec['triangles'])
assert len(records)==24
U.write(W/'ExportBaseline.json',dict(status='SOURCE_EXPORTS_CAPTURED',revision='R04 compact compound berry shrubs',models=records));print('BUSH_R04_SOURCE_CAPTURED',len(records),flush=True)
