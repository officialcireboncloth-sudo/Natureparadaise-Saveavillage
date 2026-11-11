"""Check real stem/head continuity and preserve the other accepted G08 models."""
import bpy,sys,json,math,hashlib
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());checks=0
def check(ok,label):
 global checks
 assert ok,label
 checks+=1
for name,sha in baseline['protected'].items():
 p=ROOT/name;check(p.is_file() and hashlib.sha256(p.read_bytes()).hexdigest()==sha,'Preserved '+name)
for name,sha in baseline['targetMeta'].items():check(hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==sha,'Retained target GUID '+name)
bpy.ops.wm.open_mainfile(filepath=str(MASTER));pairs=[]
def ring(o,start):return sum((v.co for v in o.data.vertices[start:start+8]),Vector())/8
for stage in ['Heading','Harvest']:
 root=bpy.data.objects['Wheat_'+stage];obs=list(root.children_recursive)
 heads={int(o['CulmIndex']):o for o in obs if o.get('Panicle')=='Wheat'}
 stems={int(o['CulmIndex']):o for o in obs if o.get('CerealCulm') or o.get('CutCulm')}
 check(set(heads)==set(stems)=={0,1,2},stage+' three matched culms and ears')
 for i,head in heads.items():
  stem=stems[i];end=len(stem.data.vertices)-9;tip=ring(stem,end);before=ring(stem,end-9);attachment=ring(head,0);top=ring(head,len(head.data.vertices)-9)
  stem_axis=(tip-before).normalized();head_axis=(top-attachment).normalized();gap=(tip-attachment).length;angle=math.degrees(stem_axis.angle(head_axis))
  check(gap<1e-6,stage+' '+str(i)+' actual surface-ring attachment')
  check(angle<.04,stage+' '+str(i)+' continuous stem and ear axis')
  check(head.get('CulmAligned') and head.get('TaperedEar'),stage+' '+str(i)+' refined neck and apex')
  check(head_axis.dot(tip-ring(stem,0))>0,stage+' '+str(i)+' ear grows beyond stem tip')
  pairs.append(dict(stage=stage,culm=i,jointGapMetres=gap,axisAngleDegrees=angle))
report=dict(status='PASS',revision='R02 wheat heading and harvest attachment correction',checks=checks,preservedFiles=len(baseline['protected']),retainedTargetGuids=len(baseline['targetMeta']),jointPairs=pairs,masterSha256=hashlib.sha256(MASTER.read_bytes()).hexdigest(),scope=['Wheat_Heading','Wheat_Harvest'])
(REVIEW/'R02_AlignmentAudit.json').write_text(json.dumps(report,indent=2));print('G08_R02_ALIGNMENT_PASS',checks,'checks',json.dumps(pairs),flush=True)
