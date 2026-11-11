"""Versioned authoring-UV proof for the explicitly requested leaf revision."""
import shutil
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Leaf_R03';out=R/'SourceExports';out.mkdir(parents=True,exist_ok=True);records={}
for p in G.MODELS.glob('*.json'):
 s=U.load(p)
 if s['materials']==[U.NAME]:continue
 assert p.stem.split('_')[1] in ['Blueberry','Blackberry','Raspberry'] and p.stem.split('_')[2] in ['Sprout','Young','Flowering','Ready'],p
 for q in [p,p.with_suffix('.fbx')]:shutil.copy2(q,out/q.name)
 records[p.stem]=dict(sourceSpec=U.rel(out/p.name),sourceFbx=U.rel(out/p.with_suffix('.fbx').name),geometrySha256=U.geometry_hash(s),specSha256=U.sha(p),triangles=s['triangles'])
assert len(records)==24,len(records)
U.write(R/'ExportBaseline.json',dict(status='SOURCE_EXPORTS_CAPTURED',revision='R03 user-requested leaf form, ratio, UV and orientation correction',models=records));print('LEAF_R03_SOURCE_CAPTURED',len(records),flush=True)
