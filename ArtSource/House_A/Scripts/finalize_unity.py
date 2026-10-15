"""Record validated Unity integration and organize House A source backups."""
from pathlib import Path
import hashlib, json

art = Path(__file__).resolve().parents[1]
root = art.parents[1]
assert 'APPLY_PASS' in (art/'Previews/UnityImportAudit.txt').read_text(encoding='utf-8')
assert 'PLAY_PASS' in (art/'Previews/UnityPlayAudit.txt').read_text(encoding='utf-8')
for export in (art/'Exports').glob('*.fbx'):
    runtime=root/'Assets/Nature  Paradaise/mesh/Buildings/House_A'/export.name
    assert runtime.read_bytes()==export.read_bytes(), 'Runtime FBX differs from final Blender export: '+export.name
path = art/'Previews/MeshManifest.json'
data = json.loads(path.read_text(encoding='utf-8'))
data.update(status='APPLIED_TO_UNITY_VALIDATED', unityAssetsModified=True,
            unityVersion='6000.0.81f1', runtimeFrontConvention='Unity +Z, sketch-right becomes local -X through standard FBX handedness conversion',
            runtimePrefabs='Assets/Nature  Paradaise/Prefabs/PlayerHouse/Exterior',
            unityImportAudit='Previews/UnityImportAudit.txt', unityPlayAudit='Previews/UnityPlayAudit.txt',
            productionSpecificationSha256=hashlib.sha256((art/'References/House_A_Approved.json').read_bytes()).hexdigest())
path.write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')

readme = art/'README.md'
text = readme.read_text(encoding='utf-8')
if 'Ã' in text or 'â€' in text:
    text = text.encode('cp1252').decode('utf-8')
text = text.replace('| (URP emission) |\nPanel', '| (URP emission) |\n\nPanel')
text = text.replace('(URP emission) |\nPanel', '(URP emission) |\n\nPanel')
if '`Previews/UnityPlayAudit.txt`: **PLAY_PASS**' not in text:
    text += '\n`Previews/UnityPlayAudit.txt`: **PLAY_PASS** pada fixture terisolasi tanpa SaveManager. Controller diuji Lv1 → 2 → 3 → 4 → 5 → 1, satu portal/return marker aktif pada frame pergantian, CharacterController naik tangga semua level, penolakan interaksi belakang, mesh bersama Lv3–4, dan transisi interior sebenarnya serta kembali ke approach tangga pada Lv1/Lv5. Preview `House_A_Lv…_Unity.png` dirender dengan material pack di Unity, bukan mockup.\n'
readme.write_text(text, encoding='utf-8')

spec = art/'References/ApprovedSpecification.md'
text = spec.read_text(encoding='utf-8')
if not text.startswith('> Arsip'):
    text = '> Arsip spesifikasi sketch sebelum produksi. Model Blender dan Unity sudah selesai; kaca bay kanan Lv5 kini biru solid opaque dari atlas. Status/audit terbaru ada di ../README.md dan ../Previews. Konversi FBX standar ke Unity membuat sketch-right menjadi local -X jika facade depan +Z.\n\n'+text
    spec.write_text(text, encoding='utf-8')

backup = art/'Blender/House_A_Masters.blend1'
target = art/'Blender/Revisions/House_A_Masters_BeforeBlue_UI.blend'
assert root in backup.resolve().parents and root in target.resolve().parents
if backup.exists():
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists(): target = target.with_name('House_A_Masters_BeforeBlue_UI_'+str(backup.stat().st_mtime_ns)+'.blend')
    backup.rename(target)
print('House A integration manifest and source folders finalized.')
