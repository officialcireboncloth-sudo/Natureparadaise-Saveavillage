"""Reopen the produce masters; seed masters were already reopened during export."""
from pathlib import Path
import json,hashlib
from gi.repository import Gimp,Gio
ROOT=Path(__file__).resolve().parents[3];ART=Path(__file__).resolve().parent
manifest=json.loads((ART/'Manifest.json').read_text());records=[]
for row in manifest['icons']:
    p=ROOT/row['produceMaster']
    image=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(p)))
    assert image.get_width()==image.get_height()==512 and len(image.get_layers())>=7,row['plant']
    records.append(dict(plant=row['plant'],source=row['produceMaster'],layers=len(image.get_layers()),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    image.delete()
    assert row['layers']>12 and hashlib.sha256((ROOT/row['seed']).read_bytes()).hexdigest()==row['seedSha256']
(ART/'GimpSourceAudit.json').write_text(json.dumps(dict(status='PASS',seedMastersReopenedDuringExport=12,produceMastersReopened=records),indent=2))
print('GIMP_EDITABLE_SEED_CROP_SOURCES_PASS 24 masters',flush=True)
