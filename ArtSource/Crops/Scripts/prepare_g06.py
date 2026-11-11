"""Snapshot the exact pre-G06 boundary once; record the inspected attachments."""
import hashlib, json, shutil
from g06_config import *

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

for p in [REVIEW, MASTER.parent, GENERATED, MODELS]:
    p.mkdir(parents=True, exist_ok=True)
baseline = REVIEW / 'Baseline.json'
if not baseline.exists():
    protected = {}
    for folder in [A/'mesh/Crops', A/'Prefabs/Crops', A/'Resources/Profiles/Farming',
                   A/'Resources/Items', A/'Data/Balance', A/'Textures/Shared/Plants',
                   A/'Material/Shared/Plants']:
        for p in folder.rglob('*'):
            if p.is_file() and 'G06' not in p.parts:
                protected[str(p.relative_to(ROOT))] = digest(p)
    for group in ['G02', 'G03', 'G04', 'G05']:
        for p in (SOURCE/group/'Blender').glob('*.blend'):
            protected[str(p.relative_to(ROOT))] = digest(p)
    for p in (SOURCE/'Textures').glob('*.xcf'):
        protected[str(p.relative_to(ROOT))] = digest(p)
    backup = SOURCE/'Revisions/Before_G06'
    for p in [*(TEXTURES.glob(ATLAS+'_*')), *(SOURCE/'Textures').glob(ATLAS+'_*.xcf'), TEXTURES/'NP_Plants_Catalog.json']:
        if p.is_file():
            target = backup/p.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(p, target)
    baseline.write_text(json.dumps(dict(protected=protected, snapshot=str(backup.relative_to(ROOT))), indent=2))
spec = json.loads((TEXTURES/(ATLAS+'_Slots.json')).read_text())
used = set()
for group in ['G03', 'G04']:
    for p in (A/'mesh/Crops'/group).glob('*.json'):
        for u, v in json.loads(p.read_text())['uv']:
            used.add(spec['slots'][min(7, int((1-v)*8))*8+min(7, int(u*8))]['name'])
assert not used.intersection(CHANGED), used.intersection(CHANGED)
(REVIEW/'ReferenceStudy.json').write_text(json.dumps(dict(
    source='Two images attached directly by the user in the current conversation; visually inspected before modeling. Original downloads were unavailable.',
    radish=dict(seedCount=3, sproutLeaves=2, youngLeaves=4, formingLeaves=4, readyLeaves=6,
                features=['rounded deeply scalloped broad leaves', 'pale branching veins', 'red spherical shoulder fading irregularly to ivory lower root', 'curved pointed taproot', 'whole harvest retains foliage', 'ivory cut face with thin red rim']),
    onion=dict(seedCount=4, sprout='single loop with pale root', youngLeaves=3, formingLeaves=4, readyLeaves=5,
               features=['black angular seeds', 'hollow tubular leaves', 'long arched outer leaves with golden tips', 'golden pear-shaped bulb with narrow neck', 'fine longitudinal papery fibres', 'radial cream roots', 'harvest has dry neck and no green leaves', 'cut face has concentric rings']),
    reviewTarget='Match the supplied compositions and proportions; render actual 3D volume from front/side/rear/top. Visual similarity requires review and is not a pixel-identical guarantee.',
    previousUsedTiles=sorted(used), changedPreviouslyUnusedTiles=CHANGED), indent=2))
print('G06_PREPARATION_PASS', len(json.loads(baseline.read_text())['protected']), 'protected files', flush=True)
