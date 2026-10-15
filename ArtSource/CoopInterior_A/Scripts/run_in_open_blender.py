from pathlib import Path
import traceback
p=Path(__file__).with_name('build_interior.py')
try:
 exec(compile(p.read_text(encoding='utf-8-sig'),str(p),'exec'),{'__file__':str(p),'__name__':'__main__'})
 p.parents[1].joinpath('gui-build-status.txt').write_text('PASS: Coop build completed in the open Blender session.')
except Exception:
 p.parents[1].joinpath('gui-build-status.txt').write_text(traceback.format_exc())
 raise
