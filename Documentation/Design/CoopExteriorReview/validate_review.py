"""Audit actual proposal geometry bounds, calculations, palette and gameplay assumptions."""
from pathlib import Path
import json,math,hashlib,itertools
from PIL import Image
OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
s=json.loads((OUT/'coop-proposal-A.json').read_text())
lo=s['master']['visualBoundsMinXYZ'];hi=s['master']['visualBoundsMaxXYZ'];points=[]
for p in s['parts']:
 if p['type']=='surface':vv=p['verticesXYZ']
 elif p['type']=='box':
  c=p['centerXYZ'];sz=p['sizeXYZ'];vv=[[c[i]+sg[i]*sz[i]/2 for i in range(3)] for sg in itertools.product([-1,1],repeat=3)]
 else:
  a=p['fromXYZ'];b=p['toXYZ'];delta=[b[i]-a[i] for i in range(3)];length=math.sqrt(sum(x*x for x in delta))
  n=[-delta[1]/length,delta[0]/length,0] if abs(delta[2])<1e-8 else [0,-delta[2]/length,delta[1]/length]
  d=[0,0,1] if abs(delta[2])<1e-8 else [1,0,0]
  vv=[[q[i]+wn*n[i]*p['width']/2+dn*d[i]*p['depth']/2 for i in range(3)] for q in [a,b] for wn,dn in itertools.product([-1,1],repeat=2)]
 for v in vv:
  assert all(lo[i]-1e-6<=v[i]<=hi[i]+1e-6 for i in range(3)),(p['name'],v)
 points.extend(vv)
assert len({p['name'] for p in s['parts']})==len(s['parts'])
actualMin=[min(p[i] for p in points) for i in range(3)];actualMax=[max(p[i] for p in points) for i in range(3)]
assert all(abs(actualMin[i]-lo[i])<1e-6 and abs(actualMax[i]-hi[i])<1e-6 for i in range(3)),(actualMin,actualMax)
for l in s['levels']:
 scale=l['uniformScale'];expected=[(hi[0]-lo[0])*scale,(hi[2]-lo[2])*scale,(hi[1]-lo[1])*scale]
 assert all(abs(a-b)<1e-6 for a,b in zip(expected,l['visualWidthDepthHeight']))
 assert l['doorWidthHeight'][1]>=2.2
 m=l['entranceMarkerXYZ'];assert m[2]<-3.5*scale and abs(m[0]+1.25*scale)<1e-6
site=s['integration']['reservedLevel5WorldBoundsXZ'];scale=1.25
assert site[0]<=lo[0]*scale and site[1]>=hi[0]*scale and site[2]<lo[2]*scale and site[3]==hi[2]*scale
atlasPath=ROOT/s['materials']['atlasPath'];atlas=Image.open(atlasPath).convert('RGB')
assert hashlib.sha256(atlasPath.read_bytes()).hexdigest()==s['materials']['sha256']
for sw in s['materials']['swatches'].values():
 assert list(atlas.getpixel(tuple(sw['samplePixel'])))==sw['rgb']
 u0,v0,u1,v1=sw['uvSafeRectBottomLeft'];assert 0<=u0<u1<=1 and 0<=v0<v1<=1
assert s['budget']['estimatedLOD0Triangles']==sum(v['triangles'] for v in s['budget']['breakdown'])
assert abs(s['master']['roofSlopeDegrees']-math.degrees(math.atan2(5.6-3.8,3)))<1e-6
assert abs(s['master']['roofEaveOuterY']-(5.6-.6*3.4))<1e-6
existing=(ROOT/'Assets/Nature  Paradaise/Resources/Buildings/Coop Building.asset').read_text()
assert all(f'- level: {i}' in existing for i in [1,2,3,4]) and '- level: 5' not in existing
assert 'footprintWidth: 3' in existing and 'footprintDepth: 3' in existing
report=dict(status='PASS',revision=s['revision'],parts=len(s['parts']),actualMinXYZ=actualMin,actualMaxXYZ=actualMax,allFullPrimitiveBounds='PASS',levelDimensions='PASS',minimumDoorHeight=2.2125,roofSlopeDegrees=s['master']['roofSlopeDegrees'],roofSurfaceArea=s['master']['roofSurfaceArea'],rampLength=s['master']['rampLength'],atlasSampleUVAndHash='PASS',triangleBudgetArithmetic='PASS',trianglesMeasured=False,existingGameplayLevels=[1,2,3,4],blenderProductionModelCreated=False,unityAssetsChanged=False)
(OUT/'review-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
