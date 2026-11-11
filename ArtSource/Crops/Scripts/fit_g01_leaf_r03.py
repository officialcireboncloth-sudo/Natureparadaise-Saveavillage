"""Fit revised shrub silhouettes to their existing 85-percent tile footprint."""
import re,copy
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Leaf_R03';base=U.load(R/'Baseline.json');manifest=U.load(G.SOURCE/'Integration/Manifest.json');records={}
for row in manifest['plants']:
 if row['group']!='G01':continue
 before=copy.deepcopy(next(r for r in base['manifestBefore']['plants'] if r['key']==row['key']));p=G.ROOT/row['profile'];text=p.read_text();assert U.sha(p)==base['protected'][U.rel(p)],'Profile already revised; do not compound it'
 specs=[U.load(G.ROOT/s['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json')) for s in row['stages']]
 scale=round(.85/max(s['max'][a]-s['min'][a] for s in specs for a in [0,2]),6)
 for stage,spec in zip(row['stages'],specs):
  stage['scale']=scale;stage['offset']=[round(-(spec['min'][0]+spec['max'][0])*.5,6),stage['offset'][1],round(-(spec['min'][2]+spec['max'][2])*.5,6)]
 stages=iter(row['stages']);stage=None
 def transform(m):
  global stage
  if m[1]=='scale':stage=next(stages);v=[stage['scale']]*3
  else:v=stage['offset']
  return '    '+m[1]+': {x: '+str(v[0])+', y: '+str(v[1])+', z: '+str(v[2])+'}'
 updated=re.sub(r'(?m)^    (scale|localOffset): \{[^\r\n]+\}',transform,text);p.write_text(updated)
 records[row['key']]=dict(before=before,after=copy.deepcopy(row),profile=U.rel(p),beforeText=text,afterText=updated,beforeSha256=base['protected'][U.rel(p)],afterSha256=U.sha(p),scaleRatio=scale/before['stages'][0]['scale'])
U.write(R/'BindingChanges.json',dict(status='APPLIED_ART_TRANSFORMS_ONLY',reason='Recenter and rescale the revised foliage to the established 85-percent tile footprint',plants=records,onlyStageFields=['scale','offset']))
U.write(G.SOURCE/'Integration/Manifest.json',manifest)
print('LEAF_R03_FIT_PASS', {k:round(v['scaleRatio'],3) for k,v in records.items()},flush=True)
