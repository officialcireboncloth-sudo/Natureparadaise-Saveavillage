"""Snapshot the approved design and reuse the Barn A mesh construction helpers."""
from pathlib import Path
import json, shutil, hashlib
from PIL import Image
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
for name in ['Blender','Exports','Previews','References','Scripts']:(ART/name).mkdir(parents=True,exist_ok=True)
review=ROOT/'Documentation/Design/CoopExteriorReview'
spec=json.loads((review/'coop-proposal-A.json').read_text(encoding='utf-8'))
spec['status']='APPROVED_FOR_BLENDER_MODEL_BY_USER_2026-10-11'
spec['approval']='User: oke eksekusi di blender kita bikin meshnya sama kayak yang barn style dan cara bikin dan hasilnya'
(ART/'References/Coop_A_Approved.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')
for filename in ['reference-coop.png','coop-four-views.png','coop-top.png','coop-level-comparison.png','coop-volume-front-right.png','coop-volume-rear-left.png','coop-atlas-and-materials.png']:
 shutil.copyfile(review/filename,ART/'References'/filename)
atlas=ROOT/spec['materials']['atlasPath']
assert hashlib.sha256(atlas.read_bytes()).hexdigest()==spec['materials']['sha256']
Image.open(atlas).convert('RGBA').save(ART/'References/TFP_Atlas_1A_BlenderPreview.png')
prefix=(ROOT/'ArtSource/Barn_A/Scripts/build_barn.py').read_text(encoding='utf-8').split('def timber_and_hardware')[0]
prefix=prefix.replace('Barn A','Coop A').replace('Barn_A','Coop_A').replace('ten rows','eight rows').replace('barn built','coop built')
(ART/'Scripts/mesh_helpers.py').write_text(prefix,encoding='utf-8')
print('PASS: approved dimensions snapshotted; original PSD unchanged; independent Barn-style helpers prepared.')
