"""Run actual crop/debug runtime rules with minimal engine boundary stand-ins.

Uses Unity's bundled managed compiler/runtime. Does not launch the Unity Editor.
"""
from pathlib import Path
import hashlib
import json
import os
import subprocess

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('C:/Program Files/Unity/Hub/Editor/6000.0.81f1/Editor/Data')
RUNTIME = UNITY / 'NetCoreRuntime'
FRAMEWORK = next((RUNTIME / 'shared/Microsoft.NETCore.App').iterdir())
REFERENCES = ['System.Private.CoreLib', 'System.Runtime', 'System.Console', 'System.Linq',
              'System.Collections', 'System.Text.RegularExpressions', 'System.Reflection',
              'System.IO.FileSystem', 'System.Runtime.Extensions']
OUT = Path(os.environ['TEMP']) / 'NP_CropStageDebugTests'
OUT.mkdir(parents=True, exist_ok=True)
SCRIPT = ROOT / 'Assets/Nature  Paradaise/Script'
sources = [
    SCRIPT / 'Item/Crop/CropDataSO.cs',
    SCRIPT / 'Item/Crop/CropQualityCare.cs',
    SCRIPT / 'World/Field/FieldData.cs',
    SCRIPT / 'World/Field/FieldAreaCropDebug.cs',
    ROOT / 'Tests/CropStageDebugTests.cs',
]
exe = OUT / 'CropStageDebugTests.dll'
rsp = OUT / 'Tests.rsp'
rsp.write_text('\n'.join([
    '-nologo', '-target:exe', '-langversion:latest', '-nostdlib+',
    f'-out:"{exe}"',
    *[f'-reference:"{FRAMEWORK / (name + ".dll")}"' for name in REFERENCES],
    *[f'"{source}"' for source in sources],
]), encoding='utf-8')
subprocess.run([str(RUNTIME / 'dotnet.exe'), str(UNITY / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)],
               cwd=ROOT, check=True, timeout=60)
exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({
    'runtimeOptions': {'tfm': 'net6.0', 'framework': {
        'name': 'Microsoft.NETCore.App', 'version': FRAMEWORK.name}}
}), encoding='utf-8')
result = subprocess.run([str(RUNTIME / 'dotnet.exe'), str(exe),
                         str(ROOT / 'Assets/Nature  Paradaise/Resources/Profiles/Farming')],
                        cwd=ROOT, capture_output=True, text=True, timeout=60)
print(result.stdout, end='')
if result.stderr:
    print(result.stderr, end='')
report = {
    'status': 'PASS' if result.returncode == 0 else 'FAIL',
    'method': 'Actual crop/debug source with engine boundary stand-ins; not Unity Play Mode',
    'result': result.stdout.strip(),
    'sourceHashes': {str(p.relative_to(ROOT)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
                     for p in sources},
}
(ROOT / 'ArtSource/Crops/Integration/CropStageDebugTests.json').write_text(
    json.dumps(report, indent=2), encoding='utf-8')
raise SystemExit(result.returncode)
