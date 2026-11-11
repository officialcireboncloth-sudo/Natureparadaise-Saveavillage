"""R02-specific checks: closed petal volumes, branching and retained early-stage assets."""
import bpy,bmesh,sys,json,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for name,sha in baseline['protected'].items():check((ROOT/name).is_file() and digest(ROOT/name)==sha,'Preserved '+name)
for name,sha in baseline['metas'].items():check(digest(ROOT/name)==sha,'Retained GUID metadata '+name)
bpy.ops.wm.open_mainfile(filepath=str(MASTER));report={}
for kind in PHASES:
 root=bpy.data.objects[kind+'_Ready'];petals=[o for o in root.children_recursive if o.get('PetalKind')==kind];count=0;volumes=[]
 for ob in petals:
  check(ob.get('ClosedPetal') and ob.get('RoundedOutline'),ob.name+' closed rounded petal source')
  bm=bmesh.new();bm.from_mesh(ob.data);closed=all(len(e.link_faces)==2 for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free()
  check(closed,ob.name+' watertight petal shell');check(volume>1e-9,ob.name+' nonzero physical volume');count+=1;volumes.append(volume)
 branches=[o for o in root.children_recursive if o.get('ShrubBranch')==kind]
 check(len(branches)>=6,kind+' distributed woody and floral branches')
 report[kind]=dict(closedPetals=count,minPetalVolumeMetres3=min(volumes),maxPetalVolumeMetres3=max(volumes),branches=len(branches))
for kind in PHASES:
 for stage in ['BudForming','Harvest']:
  for ob in bpy.data.objects[kind+'_'+stage].children_recursive:
   if ob.get('PetalKind')==kind:check(ob.get('ClosedPetal'),kind+' '+stage+' updated petal geometry')
result=dict(status='PASS',revision='R02 closed fleshy petals and branching shrubs',checks=len(checks),preservedFiles=len(baseline['protected']),retainedMetadata=len(baseline['metas']),masterSha256=digest(MASTER),plants=report,technicalScope='Physical mesh checks and preservation; artistic fidelity reviewed separately')
(REVIEW/'R02_FormAudit.json').write_text(json.dumps(result,indent=2));print('G09_R02_FORM_AUDIT_PASS',len(checks),'checks',json.dumps(report),flush=True)
