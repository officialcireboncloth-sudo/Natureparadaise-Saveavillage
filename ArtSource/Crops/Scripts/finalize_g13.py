"""Close Lavender only after art, prior-asset preservation, bindings and crop logic pass."""
import json
from pathlib import Path
import g13_config as G
import g13_integration_config as I
import unified_plant_atlas as U
R=G.REVIEW;out=I.OUT
art=U.load(R/'ArtAudit.json');atlas=U.load(U.REVIEW/'Audit.json');game=U.load(out/'IntegrationAudit.json');compiled=U.load(out/'Compilation.json');logic=U.load(out/'CropStageDebugTests.json');icons=U.load(I.ART/'G13_IconAudit.json');csv=U.load(out/'G13_CsvEditAudit.json');paint=U.load(R/'GimpReopenAudit.json')
allocator=U.load(R/'AllocatorAudit.json')
assert all(r['status']=='PASS' for r in [art,atlas,game,compiled,logic,icons,csv,paint,allocator])
assert atlas['meshes']==344 and atlas['prefabs']==172 and atlas['runtimeMaterials']==1
assert game['crops']==game['seedStoreProducts']==28 and game['stageBindings']==141 and game['icons']==84
assert '28 authored crop schedules' in logic['result'] and art['masterSha256']==U.sha(G.MASTER)
for report in [art,atlas,compiled,logic,allocator]:
 for name,h in report['sourceHashes'].items():assert U.sha(G.ROOT/name)==h,name
base=U.load(R/'Baseline.json')
for name,h in base['protected'].items():assert U.sha(G.ROOT/name)==h,name
manifest=U.load(out/'Manifest.json')
for old in base['manifestBefore']['plants']:assert next(r for r in manifest['plants'] if r['key']==old['key'])==old
assert len(manifest['plants'])==28
previews=['G13_GrowthStages.png','G13_SpatialReview.png','G13_FlowerAndSeed.png','G13_AppliedPreview.png']
for name in previews:assert (G.SOURCE/'Previews'/name).stat().st_size>1000,name
log=(R/'GimpReview.log').read_text(encoding='utf-8',errors='replace');assert 'G13_GIMP_REVIEW_PASS' in log and 'batch command executed successfully' in log
registry=U.load(U.REGISTRY);manifest['runtimeAtlas'].update(registrySha256=U.sha(U.REGISTRY),size=[4096,4096],material=U.rel(U.MATERIAL),materialGuid=U.guid(U.MATERIAL))
manifest.update(runtimeValidation='PASSED_G13_ART_SHARED_ATLAS_BINDING_COMPILER_AND_CROP_LOGIC_CHECKS',unityEditorFixture='NOT_RUN_FOR_G13',unityPlayMode='NOT_RUN_BY_AGENT',extensionBaseline=U.rel(I.BASELINE))
U.write(out/'Manifest.json',manifest);U.migration_report()
summary=dict(status='APPLIED_AND_FILE_CHECKED',group='G13',species='Lavender',label='Lavender',crops=28,seedProducts=28,phaseBindings=141,uiSprites=84,authoredMeshesWithLods=344,authoredPrefabs=172,runtimeMaterials=1,runtimeAtlas=U.NAME,atlasSize=[4096,4096],atlasFreePercent=registry['freePercent'],artChecks=art['checks'],atlasChecks=atlas['checks'],assetChecks=game['checks'],cropLogic=logic['result'],compilation='PASS',oldCropsRetained=27,oldAtlasSurfacesAndPixelsRetained=130,closedSculptedVolumes=art['closedSculptedVolumes'],floralJoints=len(art['floralJoints']),master=U.rel(G.MASTER),masterSha256=U.sha(G.MASTER),bindingManifestSha256=U.sha(out/'Manifest.json'),registrySha256=U.sha(U.REGISTRY),preview='ArtSource/Crops/Previews/G13_AppliedPreview.png',spatialReview='ArtSource/Crops/Previews/G13_SpatialReview.png',growthReview='ArtSource/Crops/Previews/G13_GrowthStages.png',reviewHashes={name:U.sha(G.SOURCE/'Previews'/name) for name in previews},balance='Existing flower defaults: seed 5 G, first harvest 5 days, produce 12 G, nonfood; previous 27 crop values retained',unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN')
summary['sourceHashes']={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*g13*.py')}
U.write(R/'Completion.json',summary);U.write(out/'G13_Completion.json',summary)
current=U.load(out/'Completion.json');current.update(summary);current['runtimeAtlas']=manifest['runtimeAtlas'];current['g13Report']='ArtSource/Crops/Integration/G13_Completion.json';current['visualRevisionReport']='ArtSource/Crops/G13/Review/Completion.json';U.write(out/'Completion.json',current)
globalSummary=U.load(U.REVIEW/'Completion.json');globalSummary.update(status='APPLIED_AND_FILE_CHECKED',revision='Shared crop atlas extended with G13 Lavender',crops=28,meshes=344,prefabs=172,surfaces=len(registry['slots']),freePercent=registry['freePercent'],assetChecks=atlas['checks'],integrationChecks=game['checks'],registrySha256=U.sha(U.REGISTRY),bindingManifestSha256=U.sha(out/'Manifest.json'),maps={s:U.sha(G.RUNTIME_TEXTURES/(U.NAME+'_'+s+'.png')) for s in U.MAPS},gallerySha256=U.sha(U.WORK/'Blender/NP_Plants_Unified_Runtime.blend'),latestExtension='ArtSource/Crops/Integration/G13_Completion.json',futureCapacity='G01 berries simulated without moving existing UV allocations');U.write(U.REVIEW/'Completion.json',globalSummary)
U.write(U.REVIEW/'UnityImportRequest.json',dict(status='REQUESTED',group='G13',registrySha256=U.sha(U.REGISTRY),expectedPrefabs=172,expectedMaterials=1))
print('G13_LAVENDER_COMPLETE: 28 crops, 141 phase bindings, 84 sprites, 344 meshes, 172 prefabs, ONE shared material;',art['checks'],'art checks;',atlas['checks'],'atlas checks;',game['checks'],'binding checks; Play Mode/mobile profiling NOT_RUN',flush=True)
