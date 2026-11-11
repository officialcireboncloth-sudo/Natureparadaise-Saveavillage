"""Reset only this unfinished revision's own visual transforms for a refined rebuild."""
import g01_config as G
import unified_plant_atlas as U
W=G.REVIEW/'Leaf_R05';path=W/'BindingChanges.json'
if path.exists():
 report=U.load(path);manifest=U.load(G.SOURCE/'Integration/Manifest.json')
 for key,r in report['plants'].items():
  p=G.ROOT/r['profile'];assert U.sha(p)==r['afterSha256'],'External profile change detected';p.write_text(r['beforeText'])
  i=next(i for i,row in enumerate(manifest['plants']) if row['key']==key);assert manifest['plants'][i]==r['after'];manifest['plants'][i]=r['before']
 U.write(G.SOURCE/'Integration/Manifest.json',manifest)
 print('R05_OWN_VISUAL_FIT_RESET',flush=True)
