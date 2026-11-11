"""Apply spatial foliage with the existing asset identities and unchanged game values."""
import re
from g11_integration_config import *
review=ROOT/'ArtSource/Crops/G11/Review';baseline=load(review/'R03_Baseline.json');manifest=load(OUT/'Manifest.json');row=next(r for r in manifest['plants'] if r['key']=='tulip');profile=ROOT/row['profile'];body=profile.read_text()
blocks=list(re.finditer(r'(  - stageName: [^\n]+\n.*?)(?=  - stageName: |\Z)',body,re.S));assert len(blocks)==5
for index in [4,3,2,1]:
 block=blocks[index];stage=row['stages'][index];spec=load(ROOT/stage['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json'))
 offset=[round(-(spec['min'][0]+spec['max'][0])/2,6),round(max(0,-spec['min'][1]),6),round(-(spec['min'][2]+spec['max'][2])/2,6)]
 for axis in [0,2]:assert (spec['max'][axis]-spec['min'][axis])*stage['scale']<=.85001
 changed=re.sub(r'(?m)^    localOffset:.*$',f'    localOffset: {{x: {offset[0]}, y: {offset[1]}, z: {offset[2]}}}',block[0]);body=body[:block.start()]+changed+body[block.end():];stage['offset']=offset
strip=lambda t:re.sub(r'(?m)^    localOffset:[^\n]*\n?','',t).strip()
assert strip(body)==strip(baseline['profileBefore']),'Only visual centering may change on the crop profile'
old=next(r for r in baseline['manifestBefore']['plants'] if r['key']=='tulip')
assert {k:v for k,v in row.items() if k!='stages'}=={k:v for k,v in old.items() if k!='stages'}
for now,previous in zip(row['stages'],old['stages']):assert {k:v for k,v in now.items() if k!='offset'}=={k:v for k,v in previous.items() if k!='offset'}
for previous in baseline['manifestBefore']['plants']:
 if previous['key']!='tulip':assert previous==next(r for r in manifest['plants'] if r['key']==previous['key'])
profile.write_text(body);manifest['runtimeValidation']='PENDING_R03_SPATIAL_ART_CHECKS';(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
direct={}
for p,sha in baseline['editableBefore'].items():
 after=digest(ROOT/p)
 if after!=sha:direct[p]=dict(before=sha,after=after)
for p,sha in baseline['protected'].items():assert digest(ROOT/p)==sha,'Out-of-scope change '+p
cumulative=dict(baseline['previousRevision']['authorizedChanges'])
for p,change in direct.items():cumulative[p]=dict(before=cumulative[p]['before'] if p in cumulative else change['before'],after=change['after'])
for p,c in cumulative.items():assert digest(ROOT/p)==c['after'],p
report=dict(status='APPLIED_SCOPED_REVISION',revision='G11 R03 spatial foliage',directChanges=direct,authorizedChanges=cumulative,protectedFiles=len(baseline['protected']),existingGuidsRetained=True,profileChanges='Sprout/Young/Bud/Ready centering only; uniform scale and gameplay retained')
(review/'R03_AssetChanges.json').write_text(json.dumps(report,indent=2));print('G11_R03_APPLIED',len(direct),'scoped files;',len(baseline['protected']),'protected files retained',flush=True)
