"""Publish revised visuals under existing GUIDs and recenter only their field offsets."""
import re
from g11_integration_config import *
review=ROOT/'ArtSource/Crops/G11/Review';baseline=load(review/'R02_Baseline.json');manifest=load(OUT/'Manifest.json');row=next(r for r in manifest['plants'] if r['key']=='tulip');profile=ROOT/row['profile'];body=profile.read_text()
pattern=r'(  - stageName: [^\n]+\n.*?)(?=  - stageName: |\Z)';blocks=list(re.finditer(pattern,body,re.S));assert len(blocks)==5
for index in [4,3,2]:
 block=blocks[index];stage=row['stages'][index];spec=load(ROOT/stage['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json'))
 offset=[round(-(spec['min'][0]+spec['max'][0])/2,6),round(max(0,-spec['min'][1]),6),round(-(spec['min'][2]+spec['max'][2])/2,6)]
 for axis in [0,2]:assert (spec['max'][axis]-spec['min'][axis])*stage['scale']<=.85001
 changed=re.sub(r'(?m)^    localOffset:.*$',f'    localOffset: {{x: {offset[0]}, y: {offset[1]}, z: {offset[2]}}}',block[0]);body=body[:block.start()]+changed+body[block.end():];stage['offset']=offset
strip=lambda t:re.sub(r'(?m)^    localOffset:[^\n]*\n?','',t).strip()
assert strip(body)==strip(baseline['profileBefore']),'Only visual positioning may change on the crop profile'
profile.write_text(body)
old=next(r for r in baseline['manifestBefore']['plants'] if r['key']=='tulip')
assert {k:v for k,v in row.items() if k!='stages'}=={k:v for k,v in old.items() if k!='stages'}
manifest['runtimeValidation']='PENDING_R02_ART_CHECKS';(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
changes={}
for p,sha in baseline['editableBefore'].items():
 after=digest(ROOT/p)
 if after!=sha:changes[p]=dict(before=sha,after=after)
for p,sha in baseline['protected'].items():assert digest(ROOT/p)==sha,'Out-of-scope change '+p
report=dict(status='APPLIED_SCOPED_REVISION',revision='G11 R02 petal and leaf model/texture',authorizedChanges=changes,protectedFiles=len(baseline['protected']),existingGuidsRetained=True,earlyMeshesRetained=['Bulb','Sprout'],profileChanges='Young/Bud/Ready centering offsets only; field scale and gameplay balance retained',bindingManifestSha256=digest(OUT/'Manifest.json'))
(review/'R02_AssetChanges.json').write_text(json.dumps(report,indent=2));print('G11_R02_APPLIED',len(changes),'scoped files;',len(baseline['protected']),'protected files retained',flush=True)
