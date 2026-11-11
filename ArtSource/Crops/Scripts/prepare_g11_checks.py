"""Scope the established export audit to the tulip's reference-specific structure."""
from g11_config import *
s=SOURCE/'Scripts';t=(s/'verify_g10_blender.py').read_text().replace('g10_config','g11_config').replace('G10','G11')
t=t.replace('(2048,2048)','(2048,1024)').replace('2048*2048*4','2048*1024*4').replace('reshape(2048,2048,4)','reshape(1024,2048,4)').replace('size=[2048,2048]','size=[2048,1024]').replace('slots[:8]','slots[:4]')
t=t.replace("len(roots)==24,'24 editable", "len(roots)==6,'6 editable")
t=t.replace("expected={'Sunflower':2,'Chrysanthemum':3,'Dahlia':3,'Marigold':4}","expected={'Tulip':1}")
t=t.replace("expected[kind] if stage=='BudForming' else 1 if stage=='Ready' else 0","1 if stage=='BudForming' else 0")
t=t.replace("len(stem.data.vertices)-10,9","len(stem.data.vertices)-11,10").replace("len(stem.data.vertices)-20,9","len(stem.data.vertices)-22,10")
t=t.replace("span[1]/span[0]>.40","span[1]/span[0]>.18").replace("len(specs)==48,'48 near","len(specs)==12,'12 near")
t=t.replace('phaseAppearances=24,meshesWithLods=48','phaseAppearances=6,meshesWithLods=12')
t=t.replace("baseline=read(REVIEW/'Baseline.json')","baseline=read(REVIEW/('R02_Baseline.json' if (REVIEW/'R02_Baseline.json').is_file() else 'Baseline.json'))")
t=t.replace("float(sat.mean())>.45","float(sat.mean())>(.28 if (REVIEW/'R02_Baseline.json').is_file() and s['name']=='TulipPetal' else .45)").replace('two cotyledons','two rolled bulb shoots')
extra="""
# Verify the defining reference features independently of the builder's counters.
for stage,nleaves,nbulbs in [('Bulb',0,1),('Sprout',2,1),('Young',3,0),('BudForming',2,0),('Ready',2,0),('Harvest',1,0)]:
 obs=bpy.data.objects['Tulip_'+stage].children_recursive
 check(sum(ob.get('ReferenceLeaf')=='Tulip' for ob in obs)==nleaves,stage+' reference leaf count')
 check(sum(bool(ob.get('TulipBulb')) for ob in obs)==nbulbs,stage+' reference bulb visibility')
for stage in ['BudForming','Ready','Harvest']:
 obs=bpy.data.objects['Tulip_'+stage].children_recursive;petals=[ob for ob in obs if ob.get('PetalKind')=='Tulip']
 check(len(petals)==6,stage+' exactly six sculpted tepals')
 check(sum(ob['TepalLayer']=='inner' for ob in petals)==3,stage+' three inner tepals')
from mathutils.bvhtree import BVHTree
ready=specs['NP_Tulip_Ready'];points=[Vector((p[0],p[2],p[1])) for p in ready['vertices']];faces=[(a,c,b) for a,b,c in zip(*[iter(ready['submeshes'][0])]*3)];tree=BVHTree.FromPolygons(points,faces,all_triangles=True);highest=max(p.z for p in points);hit=tree.ray_cast(Vector((.003,0,highest+.05)),Vector((0,0,-1)))
check(hit[0] is not None and hit[0].z<highest-.12,'Runtime flower cup has a real open upper cavity')
"""
t=t.replace("report=dict(status='PASS'",extra+"\nreport=dict(status='PASS'")
(s/'verify_g11_blender.py').write_text(t)
print('G11_REFERENCE_CHECKS_READY',flush=True)
