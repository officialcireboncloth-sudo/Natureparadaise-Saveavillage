"""Exercise real interaction ownership/range rules without taking over the running Editor."""
from pathlib import Path
import json,os,subprocess
ROOT=Path(__file__).resolve().parents[1];UNITY=Path('C:/Program Files/Unity/Hub/Editor/6000.0.81f1/Editor/Data');RUNTIME=UNITY/'NetCoreRuntime';FRAMEWORK=next((RUNTIME/'shared/Microsoft.NETCore.App').iterdir());OUT=Path(os.environ['TEMP'])/'NP_InteractionTargetTests';OUT.mkdir(parents=True,exist_ok=True)
sources=[ROOT/'Assets/Nature  Paradaise/Script/Player/PlayerInteractionTarget.cs',ROOT/'Tests/InteractionTargetTests.cs'];exe=OUT/'InteractionTargetTests.dll';rsp=OUT/'Tests.rsp'
rsp.write_text('\n'.join(['-nologo','-target:exe','-langversion:latest','-nostdlib+',f'-out:"{exe}"',*[f'-reference:"{FRAMEWORK/(name+".dll")}"' for name in ['System.Private.CoreLib','System.Runtime','System.Console','System.Collections']],*[f'"{p}"' for p in sources]]))
subprocess.run([str(RUNTIME/'dotnet.exe'),str(UNITY/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],cwd=ROOT,check=True,timeout=60)
exe.with_suffix('.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm='net6.0',framework=dict(name='Microsoft.NETCore.App',version=FRAMEWORK.name)))))
result=subprocess.run([str(RUNTIME/'dotnet.exe'),str(exe)],cwd=ROOT,capture_output=True,text=True,timeout=60);print(result.stdout);print(result.stderr)
if result.returncode:raise SystemExit(result.returncode)
ruleSources=[ROOT/'Assets/Nature  Paradaise/Script/Core/CollectibleClueRules.cs',ROOT/'Tests/CollectibleClueRulesTests.cs']
rsp.write_text('\n'.join([line for line in rsp.read_text().splitlines() if not line.endswith('.cs"')]+[f'"{p}"' for p in ruleSources]))
subprocess.run([str(RUNTIME/'dotnet.exe'),str(UNITY/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],cwd=ROOT,check=True,timeout=60)
result=subprocess.run([str(RUNTIME/'dotnet.exe'),str(exe)],cwd=ROOT,capture_output=True,text=True,timeout=60);print(result.stdout);print(result.stderr);raise SystemExit(result.returncode)
