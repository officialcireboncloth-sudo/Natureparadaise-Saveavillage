"""Validate measured proposal and embed its coordinates in the review fragment."""
from pathlib import Path
import json, math, hashlib, re

out=Path(__file__).resolve().parent
spec=json.loads((out/'barn-proposal-A.json').read_text(encoding='utf-8'))
lo=spec['master']['visualBoundsMinXYZ'];hi=spec['master']['visualBoundsMaxXYZ']
points=[]
for part in spec['parts']:
    if part['type']=='box':
        assert part['rotationXYZDegrees']==[0,0,0]
        for a in (-1,1):
            for b in (-1,1):
                for c in (-1,1):
                    points.append((part['name'],[part['centerXYZ'][i]+[a,b,c][i]*part['sizeXYZ'][i]/2 for i in range(3)]))
    elif part['type']=='surface':
        points.extend((part['name'],p) for p in part['verticesXYZ'])
    else:
        a,b=part['fromXYZ'],part['toXYZ'];d=[b[i]-a[i] for i in range(3)];length=math.sqrt(sum(v*v for v in d))
        assert length>0
        normal=[-d[1]/length,d[0]/length,0] if abs(d[2])<1e-8 else [0,-d[2]/length,d[1]/length]
        depth=[0,0,1] if abs(d[2])<1e-8 else [1,0,0]
        for p in (a,b):
            for s in (-1,1):
                for t in (-1,1):
                    points.append((part['name'],[p[i]+s*normal[i]*part['width']/2+t*depth[i]*part['depth']/2 for i in range(3)]))
for name,p in points:
    assert all(lo[i]-1e-6<=p[i]<=hi[i]+1e-6 for i in range(3)),(name,p,lo,hi)
for level in spec['levels']:
    scale=level['uniformScale']
    assert all(abs(level['visualEnvelopeXYZ'][i]-(hi[i]-lo[i])*scale)<1e-6 for i in range(3))
    assert level['mainDoorClearWidthHeight'][1]>=2.4
assert len({p['name'] for p in spec['parts']})==len(spec['parts'])
atlas=out.parents[2]/spec['materials']['atlasPath']
assert hashlib.sha256(atlas.read_bytes()).hexdigest()==spec['materials']['sha256']
for s in spec['materials']['swatches'].values():
    u0,v0,u1,v1=s['uvSafeRectBottomLeft']
    assert 0<=u0<u1<=1 and 0<=v0<v1<=1

fragment=Path('C:/Users/huxel/.codex/visualizations/2026/09/30/01a0f224-0b97-7b20-80a9-aa020b217db1/barn-exterior-review.html')
source=fragment.read_text(encoding='utf-8')
data={k:spec[k] for k in ('revision','levels','parts')}
source,n=re.subn(r'(<script type="application/json" id="barn-data">).*?(</script>)',lambda m:m[1]+json.dumps(data,separators=(',',':'))+m[2],source,flags=re.S)
assert n==1
fragment.write_text(source,encoding='utf-8')
report={'proposal':spec['revision'],'status':'PROPOSED_NOT_MODELLED_NOT_APPLIED','draftParts':len(spec['parts']),
        'geometryEnvelopeCheck':'PASS: boxes, surfaces and full beam thickness inside declared bounds',
        'levelScaleCheck':'PASS: five envelopes derived from one master by uniform XYZ scale',
        'atlasSha256Check':'PASS: original atlas matches audit snapshot',
        'uvCheck':'PASS: selected safe UV rectangles in range',
        'productionMeshCreated':False,'unityApplied':False,'triangleCountsMeasured':False}
(out/'review-validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
