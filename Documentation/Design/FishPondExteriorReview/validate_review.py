"""Validate proposal coordinates/artifacts and audited source resources, not game runtime."""
from pathlib import Path
import json,hashlib,math
import numpy as np
from PIL import Image
import drawing

OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
s=json.loads((OUT/'pond-proposal-A.json').read_text(encoding='utf-8'))
checks=[]
def check(ok,label):
    assert ok,label
    checks.append(label)
def polygon_area(p):return abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2
mat=s['materials'];atlas=ROOT/mat['atlasPath']
check(hashlib.sha256(atlas.read_bytes()).hexdigest()==mat['atlasSha256'],'Original shared atlas hash unchanged')
check(hashlib.sha256((ROOT/mat['waterMaterial']).read_bytes()).hexdigest()==mat['waterMaterialSha256'],'Existing Toon Pack water material hash unchanged')
check(Image.open(atlas).size==(2048,2048),'Atlas dimensions read from original PSD')
check(s['status'].startswith('PROPOSED'),'Proposal status awaits user ACC')
check([m['capacity'] for m in s['models']]==[50,100,250],'Exactly requested capacities and three levels')
check([m['stations']['autoFeeder']['enabled'] for m in s['models']]==[False,True,True],'Auto feeder only Lv2/Lv3')
for name,q in mat['swatches'].items():
    a,b,c,d=q['safeUvBottomLeft'];check(0<a<c<1 and 0<b<d<1,name+' valid safe atlas UV')
    x,y,w,h=q['pixelRectTopLeft'];check(w==32 and h==64,name+' uses audited palette cell')
for m in s['models']:
    key=m['id'];parts=m['parts'];sw,sd=m['siteReservationWD']
    check(len({p['id'] for p in parts})==len(parts),key+' unique part IDs')
    vs=np.concatenate([v for v,_ in drawing.faces_for(m)])
    check(np.isfinite(vs).all(),key+' finite coordinates')
    check(np.all(vs[:,0]>=-sw/2) and np.all(vs[:,0]<=sw/2) and np.all(vs[:,2]>=-sd/2) and np.all(vs[:,2]<=sd/2),key+' complete model inside reserved site')
    lo=vs.min(axis=0);hi=vs.max(axis=0)
    check(np.allclose(lo,m['visualBounds']['minXYZ']) and np.allclose(hi,m['visualBounds']['maxXYZ']),key+' bounds computed from actual planning geometry')
    check(abs(polygon_area(m['waterPolygonXZ'])-m['waterAreaM2'])<.001,key+' water polygon area')
    check(m['basinFloorY']>0 and m['waterLevelY']>m['basinFloorY'],key+' basin floor hides ground below water')
    check(abs(m['waterLevelY']-m['basinFloorY']-m['waterDepth'])<1e-6,key+' exact water depth')
    check(m['waterLevelY']<.90,key+' water below deck and coping')
    band=m['fishSwimBandY'];half=m['maximumFishVisualThickness']/2
    check(band[0]-half>.26 and band[1]+half<m['waterLevelY'],key+' normalized fish fit above bottom rocks and below water')
    check(len(m['waterPolygonXZ'])==16,key+' low polygon water boundary')
    tri=sum(len(v)-2 for v,_ in drawing.faces_for(m))
    check(tri==m['sketchPrimitiveTriangles'] and tri<m['budgetTriangles']['LOD0Limit'],key+' triangle estimate within production budget')
    check(all(np.linalg.norm(np.cross(v[1]-v[0],v[2]-v[0]))>1e-9 for v,_ in drawing.faces_for(m)),key+' no zero-area planning faces')
    q=m['dock'];dx=q['centerXYZ'][0]
    check(q['stairRise']<=.35 and q['stairWidth']>=2,key+' steps and width fit proposed access')
    # Side fixtures must leave the central 1.4 m corridor, including the front/rear rails.
    barrel=m['stations']['storage']['centerXYZ'];check(barrel[0]-.43>=dx+.70,key+' storage clear of central lane')
    if m['stations']['autoFeeder']['enabled']:
        f=m['stations']['autoFeeder'];r=.535 if m['level']==3 else .530
        check(f['centerXYZ'][0]+r<=dx-.70,key+' feeder clear of central lane')
        check(f['emitterXYZ'][2]>q['backZ'] and f['directionXYZ']==[0,0,1],key+' feeder nozzle points into water beyond dock')
    for point in [m['stations']['maker']['interactionXYZ'],m['stations']['pondApproachXYZ']]:
        check(abs(point[0])<sw/2 and abs(point[2])<sd/2,key+' approach point inside reservation')
    check(not any(p['group']=='autoFeeder' for p in parts) if m['level']==1 else any(p['group']=='autoFeeder' for p in parts),key+' correct machine parts by level')
    for suffix in ['front-right','rear-left','elevations','plan']:
        p=OUT/f'pond-{key}-{suffix}.png';check(p.exists() and Image.open(p).width>=1500,key+' '+suffix+' image exists at review resolution')
for name in ['pond-levels-overview.png','pond-section-and-foundation.png','pond-atlas-and-materials.png','pond-machines-and-dock.png']:
    check((OUT/name).exists(),name+' exists')
check((OUT/'README.md').exists(),'Specification and implementation notes exist')
result={'status':'PASS','checksPassed':len(checks),'scope':'Coordinate sketches and source-resource audit only. No Blender/Unity/runtime validation claim.',
        'checks':checks,'newRuntimeTextures':0,'blenderExecuted':False,'unityAssetsEdited':False}
(OUT/'review-validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('POND_SKETCH_PASS',len(checks),'checks; proposal ready for ACC.')
