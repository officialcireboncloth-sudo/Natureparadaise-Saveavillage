"""Compile current game and Editor source with Unity's cached compiler references.

Outputs go to OS temp, never replace Library assemblies. This does not run Unity.
"""
from pathlib import Path
import os,subprocess,re,json,hashlib
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'ArtSource/Crops/Integration'
TMP=Path(os.environ['TEMP'])/'NP_CropIntegrationCompile'
BASE=Path('C:/Program Files/Unity/Hub/Editor/6000.0.81f1/Editor/Data')
records=[]
for editor in [False,True]:
    name='Assembly-CSharp-Editor' if editor else 'Assembly-CSharp'
    rsp=next((ROOT/'Library/Bee/artifacts').glob('*/'+name+'.rsp'))
    lines=rsp.read_text(encoding='utf-8-sig').splitlines();dest=TMP/('Editor' if editor else 'Runtime');dest.mkdir(parents=True,exist_ok=True)
    kept=[];sources=set()
    for line in lines:
        if line.startswith(('-out:','-refout:','-pdb:')):continue
        if line.startswith('-reference:') and 'Assembly-CSharp' in line and editor:
            if re.search(r'Assembly-CSharp(?:\.ref)?\.dll',line):line='-reference:"'+str(TMP/'Runtime/Assembly-CSharp.dll')+'"'
        if line.startswith('"') and line.endswith('.cs"'):sources.add(line[1:-1].replace('\\','/'))
        kept.append(line)
    for source in (ROOT/'Assets/Nature  Paradaise/Script').rglob('*.cs'):
        in_editor='Editor' in source.relative_to(ROOT/'Assets').parts
        path=str(source.relative_to(ROOT)).replace('\\','/')
        if in_editor==editor and path not in sources:kept.append('"'+path+'"');sources.add(path)
    kept+=['-out:"'+str(dest/(name+'.dll'))+'"','-refout:"'+str(dest/(name+'.ref.dll'))+'"']
    response=dest/(name+'.rsp');response.write_text('\n'.join(kept))
    result=subprocess.run([str(BASE/'NetCoreRuntime/dotnet.exe'),str(BASE/'DotNetSdkRoslyn/csc.dll'),'@'+str(response)],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace',timeout=120)
    (OUT/(name+'.Compile.log')).write_text(result.stdout,encoding='utf-8')
    errors=[l for l in result.stdout.splitlines() if 'error CS' in l]
    records.append(dict(assembly=name,status='PASS' if result.returncode==0 else 'FAIL',exitCode=result.returncode,sourceCount=len(sources),errors=errors))
    print(name,records[-1]['status'],'sources',len(sources),flush=True)
    if errors:print('\n'.join(errors[:15]),flush=True)
    if result.returncode!=0:break
code={str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in [ROOT/'Assets/Nature  Paradaise/Script/Camera/TopDownCameraFollow.cs',ROOT/'Assets/Nature  Paradaise/Script/World/Field/FieldArea.cs',ROOT/'Assets/Nature  Paradaise/Script/World/Field/FieldCropView.cs',ROOT/'Assets/Nature  Paradaise/Script/World/Field/FieldAreaCropDebug.cs',ROOT/'Assets/Nature  Paradaise/Script/UI/TestingDebugMenu.cs',ROOT/'Assets/Nature  Paradaise/Script/Editor/CropGameplayValidation.cs',ROOT/'Assets/Nature  Paradaise/Script/Editor/CropArtValidation.cs']}
for name in ['Core/WorldInteractionPrompt.cs','Core/WorldInteractionVisuals.cs','Core/CollectibleClueRules.cs','Player/PlayerInteractionTarget.cs','Player/Inventory/WorldItemPickup.cs','Player/Item/HoeTool.cs','World/Gathering/WorldTree.cs','Entity/Animal/AnimalManureSystem.cs','Item/Crop/CropDataSO.cs']:
    p=ROOT/'Assets/Nature  Paradaise/Script'/name;code[str(p.relative_to(ROOT)).replace('\\','/')]=hashlib.sha256(p.read_bytes()).hexdigest()
atlas_validator=ROOT/'Assets/Nature  Paradaise/Script/Editor/CropSharedAtlasValidation.cs'
if atlas_validator.is_file():code[str(atlas_validator.relative_to(ROOT)).replace('\\','/')]=hashlib.sha256(atlas_validator.read_bytes()).hexdigest()
report=dict(status='PASS' if len(records)==2 and all(r['status']=='PASS' for r in records) else 'FAIL',method='Unity bundled Roslyn and existing cached project references; not an Editor import or Play Mode result',assemblies=records,sourceHashes=code)
(OUT/'Compilation.json').write_text(json.dumps(report,indent=2))
raise SystemExit(0 if report['status']=='PASS' else 1)
