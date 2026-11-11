"""Create the Lavender-specific preservation and completion reports from the accepted workflow."""
from pathlib import Path
S=Path(__file__).resolve().parent
def adapt(name,changes):
 t=(S/name.replace('g13','g12')).read_text().replace('g12','g13').replace('G12','G13').replace('Orchid','Lavender').replace('orchid','lavender').replace('Anggrek','Lavender')
 for before,after in changes:t=t.replace(before,after)
 (S/name).write_text(t,encoding='utf-8')
adapt('record_g13_integration.py',[
 ('26','27'),('==27','==28'),('"Tulip" };','"Orchid" };'),('"Tulip", "Lavender" };','"Orchid", "Lavender" };')])
adapt('finalize_g13.py',[
 ('==332','==344'),('==166','==172'),('==27','==28'),('==136','==141'),('==81','==84'),
 ('27 authored crop schedules','28 authored crop schedules'),('crops=27','crops=28'),('seedProducts=27','seedProducts=28'),('phaseBindings=136','phaseBindings=141'),('uiSprites=81','uiSprites=84'),('WithLods=332','WithLods=344'),('Prefabs=166','Prefabs=172'),('Retained=26','Retained=27'),('Retained=122','Retained=130'),('previous 26','previous 27'),('meshes=332','meshes=344'),('prefabs=166','prefabs=172'),('expectedPrefabs=166','expectedPrefabs=172'),('G01 and G13 simulated','G01 berries simulated'),('27 crops, 136 phase bindings, 81 sprites, 332 meshes, 166 prefabs','28 crops, 141 phase bindings, 84 sprites, 344 meshes, 172 prefabs')])
adapt('review_g13_gimp.py',[
 ('daun berdaging dan arah tumbuh dalam ruang','daun sage bervolume dan arah tumbuh dalam ruang'),('cawan bibir bunga, sepal, dan ikon bibit','kuntum ungu bertingkat dan ikon bibit')])

p=S/'verify_unified_plant_atlas.py';t=p.read_text().replace("remainingPrimary=2 if 'orchid' in currentByKey else 5","remainingPrimary=0 if 'lavender' in currentByKey else 2 if 'orchid' in currentByKey else 5");p.write_text(t)
p=S/'plant_library.py';t=p.read_text().replace("'G10','G11','G12'] and prepared","'G10','G11','G12','G13'] and prepared")
line="  if group=='G12' and prepared:slots=['OrchidPetal','OrchidSepal','OrchidLip','OrchidLeaf','StemGreen','RootVelamen','SeedCream','ColumnAmber']"
assert line in t
if "if group=='G13' and prepared:slots=" not in t:t=t.replace(line,line+"\n   if group=='G13' and prepared:slots=['LavenderPetal','LavenderLeaf','StemSage','SeedBrown']")
p.write_text(t)
p=S/'audit_crop_extension.py';t=p.read_text().replace("if hasG12:expected=27","if hasG12:expected=27\nhasG13=any(r['group']=='G13' for r in m['plants'])\nif hasG13:expected=28").replace("['G10','G11','G12']","['G10','G11','G12','G13']")
line="compiled=load(OUT/'Compilation.json');"
assert line in t;t=t.replace(line,"if hasG13:\n extra=load(ART/'G13_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==1,'Two Lavender GIMP masters reopened')\n check(len([r for r in m['plants'] if r['group']=='G13'])==1 and load(OUT/'G13_CsvEditAudit.json')['status']=='PASS','One Lavender species; CSV append verified')\n old=load(OUT/'G13_Baseline.json')\n for previous in old['manifestBefore']['plants']:check(previous==next(r for r in m['plants'] if r['key']==previous['key']),'Lavender retains previous crop '+previous['species'])\n"+line);p.write_text(t)
print('G13_COMPLETION_WORKFLOWS_READY')
