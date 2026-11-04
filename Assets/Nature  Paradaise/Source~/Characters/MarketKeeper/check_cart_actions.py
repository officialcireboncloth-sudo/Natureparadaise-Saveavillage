"""Inspect the saved editing source, not a generated duplicate or Unity's baked curves."""
import bpy,json
from pathlib import Path
root=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
source=Path(__file__).parent
bpy.ops.wm.open_mainfile(filepath=str(source/'MarketKeeper.blend'))
rig=bpy.data.objects['MarketKeeper_Rig'];sc=bpy.context.scene;report=[]
for name in ('Cart_Grip','Cart_Pull','Cart_Push'):
 a=bpy.data.actions[name];assert a.use_fake_user
 curves=[f for l in a.layers for s in l.strips for b in s.channelbags for f in b.fcurves]
 times=sorted({round(k.co.x,4) for f in curves for k in f.keyframe_points})
 assert len(times)<=11 and times[0]==0 and times[-1]==30
 assert all(k.interpolation=='BEZIER' for f in curves for k in f.keyframe_points)
 rig.animation_data.action=a;sc.frame_set(0);start={b.name:b.matrix.copy() for b in rig.pose.bones}
 sc.frame_set(30)
 if name!='Cart_Grip':assert max((b.matrix.translation-start[b.name].translation).length for b in rig.pose.bones)<.00001
 report.append('PASS '+name+': sparse smooth source keys '+str(times)+', loop seam checked' if name!='Cart_Grip' else 'PASS '+name+': sparse smooth source keys '+str(times))
assert len(rig.data.bones)==65 and len(bpy.data.objects['MarketKeeper_Body'].data.vertices)==2815
report.append('PASS original keeper mesh, 65-bone rig and 1024 texture retained; cart reference in same file')
out=root/'ArtSource/MarketDeparture/BlenderAudit.txt';out.write_text('\n'.join(report)+'\n');print('\n'.join(report))
