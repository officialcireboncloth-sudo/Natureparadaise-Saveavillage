"""Check spatial leaf geometry and preservation against the delivered R02 master."""
import bpy,bmesh,sys,json,hashlib,re
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
base=json.loads((REVIEW/'R03_Baseline.json').read_text());checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
for p,s in base['protected'].items():check(sha(ROOT/p)==s,'Protected '+p)
def nonleaves(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));result={}
 for root in [o for o in bpy.data.objects if o.get('Phase')]:
  offset=float(root.get('GroundOffset',0))
  for ob in root.children_recursive:
   if ob.type!='MESH' or ob.get('ReferenceLeaf')=='Tulip':continue
   coordinates=np.array([tuple(v.co) for v in ob.data.vertices]);coordinates[:,2]-=offset
   result[(root['Phase'],ob.name)]=(coordinates,[tuple(p.vertices) for p in ob.data.polygons])
 return result
before=nonleaves(ROOT/base['snapshot']/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend');after=nonleaves(MASTER)
check(set(before)==set(after),'Same non-leaf components')
for key,(coords,polys) in before.items():
 now,faces=after[key];check(coords.shape==now.shape and np.allclose(coords,now,atol=2e-7,rtol=0) and polys==faces,'Flower/stalk/bulb shape retained '+str(key))
metrics={}
for stage in ['Sprout','Young','BudForming','Ready','Harvest']:
 root=bpy.data.objects['Tulip_'+stage];leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')=='Tulip'];areas=np.zeros(3);allcoords=[];details=[]
 for ob in leaves:
  check(ob.get('SpatialLeaf') and ob.get('LeafFrame')=='radial growth and world up',stage+' spatial leaf frame')
  bm=bmesh.new();bm.from_mesh(ob.data);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-9,stage+' closed leaf with real volume');bm.free()
  coords=np.array([tuple(v.co) for v in ob.data.vertices]);allcoords.extend(coords);ob.data.calc_loop_triangles();indices=np.array([tuple(t.vertices) for t in ob.data.loop_triangles]);tri=coords[indices]
  projected=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5;areas+=projected
  singular=np.linalg.svd(coords-coords.mean(axis=0),compute_uv=False);nonplanar=float(singular[-1]/singular[0]);check(nonplanar>.006,stage+' physically curved blade')
  details.append(dict(index=int(ob['LeafIndex']),projectedAreasYZ_XZ_XY=projected.tolist(),planarityRatio=nonplanar))
 span=np.ptp(np.array(allcoords),axis=0);ratios=(float(areas[2]/areas[1]),float(areas[0]/areas[1]));print('G11_R03_SPATIAL',stage,'top/front,side/front',ratios,'depth/width',float(span[1]/span[0]),flush=True)
 if stage in ['Young','BudForming','Ready']:
  check(ratios[0]>.25 and ratios[1]>.25,stage+' blade area visible in three perpendicular projections')
  check(span[1]/span[0]>.45,stage+' spatial front/back distribution')
 metrics[stage]=dict(projectedTopFrontRatio=ratios[0],projectedSideFrontRatio=ratios[1],depthWidthRatio=float(span[1]/span[0]),leaves=details)
body=(A/'Resources/Profiles/Farming/Tulip Crop.asset').read_text();strip=lambda t:re.sub(r'(?m)^    localOffset:[^\n]*\n?','',t).strip();check(strip(body)==strip(base['profileBefore']),'Game profile changes only visual centering')
scripts=['g11_r03_geometry.py','build_g11_r03.py','apply_g11_r03.py','verify_g11_r03.py','g11_geometry.py','g11_r02_geometry.py']
report=dict(status='PASS',revision='G11 R03',checks=len(checks),protectedFiles=len(base['protected']),metrics=metrics,nonLeafComponentsRetained=len(before),masterSha256=sha(MASTER),sourceHashes={str((SOURCE/'Scripts'/n).relative_to(ROOT)).replace('\\','/'):sha(SOURCE/'Scripts'/n) for n in scripts},unityPlayMode='NOT_RUN_BY_AGENT')
(REVIEW/'R03_FormAudit.json').write_text(json.dumps(report,indent=2));print('G11_R03_SPATIAL_PASS',len(checks),'checks;',len(before),'non-leaf shapes retained',flush=True)
