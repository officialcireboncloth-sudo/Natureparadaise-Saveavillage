"""Apply field-only bulb planting depth and retain clue shader/material in builds."""
from crop_extension_config import *
from unity_art_io import meta
import uuid
shader=A/'Material/WorldInteractionClue.shader';sm=Path(str(shader)+'.meta')
if not sm.exists():sm.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
material=A/'Resources/UI/Interaction/WorldInteractionClue.mat';material.parent.mkdir(parents=True,exist_ok=True)
material.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: WorldInteractionClue\n  m_Shader: {fileID: 4800000, guid: '+guid(shader)+', type: 3}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: 4000\n  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats: []\n    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}\n')
meta(material,main_id=2100000)
for p in [A/'Resources/UI',material.parent]:meta(p,folder=True)
meta(A/'Script/Core/WorldInteractionVisuals.cs')
meta(A/'Script/Core/CollectibleClueRules.cs')
m=load(OUT/'Manifest.json');records=[]
for kind in ['Radish','Onion']:
 row=next(r for r in m['plants'] if r['species']==kind);p=ROOT/row['profile'];body=p.read_text();parts=re.split(r'(?=  - stageName: )',body)
 for i,stage in enumerate(row['stages']):
  key=['Seed','Sprout','Young','RootForming' if kind=='Radish' else 'BulbForming','Ready'][i];depth=SOIL_BURIAL.get((kind,key),0)
  if not depth:continue
  block=parts[i+1];block=re.sub(r'(?m)^    soilBurialDepth:.*\n?','',block);block=block.replace('    localEulerAngles:',f'    soilBurialDepth: {depth}\n    localEulerAngles:');parts[i+1]=block
  stage['soilBurialDepth']=depth
  low,high=(.075,.425) if kind=='Radish' else (.07,.47);s=.625 if key=='RootForming' else .54 if key=='BulbForming' else 1
  fraction=(high*s-depth)/((high-low)*s);assert abs(fraction-.5)<1e-6
  records.append(dict(plant=kind,phase=key,burialInModelUnits=depth,aboveSoilFraction=fraction))
 p.write_text(''.join(parts),encoding='utf-8',newline='\n')
m['rootCropPlanting']=records;(OUT/'Manifest.json').write_text(json.dumps(m,indent=2,ensure_ascii=False))
(OUT/'InteractionVisualPlacement.json').write_text(json.dumps(dict(status='APPLIED',rootCrops=records,material=rel(material),shader=rel(shader),depthTest='Always',revealDistanceMetres=12,maximumGlints=12,glintsPerObject=1,glintHalfSizeAt720p=7,mobileSizeMultiplier=1.2,twinklePeriodSeconds=5,twinkleDurationSeconds=.85),indent=2))
print('CROP_INTERACTION_VISUAL_ASSETS_PASS',len(records),'half-buried phases',flush=True)
