"""Close the two supplied berry species only after integrity, compiler and crop checks pass."""
import g01_config as G
import g01_integration_config as I
import unified_plant_atlas as U
R=G.REVIEW;out=I.OUT
art=U.load(R/'ArtAudit.json');atlas=U.load(U.REVIEW/'Audit.json');game=U.load(out/'IntegrationAudit.json');compiled=U.load(out/'Compilation.json');logic=U.load(out/'CropStageDebugTests.json');icons=U.load(I.ART/'G01_IconAudit.json');csv=U.load(out/'G01_CsvEditAudit.json');paint=U.load(R/'GimpReopenAudit.json')
allocator=U.load(R/'AllocatorAudit.json')
assert all(r['status']=='PASS' for r in [art,atlas,game,compiled,logic,icons,csv,paint,allocator])
assert atlas['meshes']==368 and atlas['prefabs']==184 and atlas['runtimeMaterials']==1
assert game['crops']==game['seedStoreProducts']==30 and game['stageBindings']==151 and game['icons']==90
assert '30 authored crop schedules' in logic['result'] and art['masterSha256']==U.sha(G.MASTER)
for report in [art,atlas,compiled,logic,allocator]:
 for name,h in report['sourceHashes'].items():assert U.sha(G.ROOT/name)==h,name
base=U.load(R/'Baseline.json')
for name,h in base['protected'].items():assert U.sha(G.ROOT/name)==h,name
manifest=U.load(out/'Manifest.json')
for old in base['manifestBefore']['plants']:assert next(r for r in manifest['plants'] if r['key']==old['key'])==old
assert len(manifest['plants'])==30 and not any(r['key']=='raspberry' for r in manifest['plants'])
previews=['G01_AppliedPreview.png','G01_FruitDetail.png']+[f'G01_{kind}_{name}.png' for kind in G.PHASES for name in ['GrowthStages','SpatialReview']]
for name in previews:assert (G.SOURCE/'Previews'/name).stat().st_size>1000,name
log=(R/'GimpReview.log').read_text(encoding='utf-8',errors='replace');assert 'G01_GIMP_REVIEW_PASS' in log and 'batch command executed successfully' in log
registry=U.load(U.REGISTRY);manifest['runtimeAtlas'].update(registrySha256=U.sha(U.REGISTRY),size=[4096,4096],material=U.rel(U.MATERIAL),materialGuid=U.guid(U.MATERIAL));manifest.update(runtimeValidation='PASSED_G01_SUPPLIED_BERRY_ART_SHARED_ATLAS_BINDING_COMPILER_AND_CROP_LOGIC_CHECKS',unityEditorFixture='NOT_RUN_FOR_G01',unityPlayMode='NOT_RUN_BY_AGENT',extensionBaseline=U.rel(I.BASELINE),pendingReference=['Raspberry']);U.write(out/'Manifest.json',manifest);U.migration_report()
summary=dict(status='APPLIED_AND_FILE_CHECKED',group='G01',species=list(G.PHASES),pendingReference=['Raspberry'],referenceNote='The second Blueberry attachment is a duplicate; no Raspberry model inferred',crops=30,seedProducts=30,phaseBindings=151,uiSprites=90,authoredMeshesWithLods=368,authoredPrefabs=184,runtimeMaterials=1,runtimeAtlas=U.NAME,atlasSize=[4096,4096],atlasFreePercent=registry['freePercent'],artChecks=art['checks'],atlasChecks=atlas['checks'],assetChecks=game['checks'],cropLogic=logic['result'],compilation='PASS',oldCropsRetained=28,oldAtlasSurfacesAndPixelsRetained=134,closedSculptedVolumes=art['closedSculptedVolumes'],floralJoints=len(art['floralJoints']),master=U.rel(G.MASTER),masterSha256=U.sha(G.MASTER),bindingManifestSha256=U.sha(out/'Manifest.json'),registrySha256=U.sha(U.REGISTRY),preview='ArtSource/Crops/Previews/G01_AppliedPreview.png',reviewHashes={n:U.sha(G.SOURCE/'Previews'/n) for n in previews},balance='Existing edible-crop defaults: seed 5 G, first harvest 5 days, produce 12 G; previous 28 crop values retained',unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN',sourceHashes={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*g01*.py')})
U.write(R/'Completion.json',summary);U.write(out/'G01_Completion.json',summary);current=U.load(out/'Completion.json');current.update(summary);current['runtimeAtlas']=manifest['runtimeAtlas'];current['g01Report']='ArtSource/Crops/Integration/G01_Completion.json';current['visualRevisionReport']='ArtSource/Crops/G01/Review/Completion.json';U.write(out/'Completion.json',current)
globalSummary=U.load(U.REVIEW/'Completion.json');globalSummary.update(status='APPLIED_AND_FILE_CHECKED',revision='Shared atlas extended with the supplied Blueberry and Blackberry references',crops=30,meshes=368,prefabs=184,surfaces=len(registry['slots']),freePercent=registry['freePercent'],assetChecks=atlas['checks'],integrationChecks=game['checks'],registrySha256=U.sha(U.REGISTRY),bindingManifestSha256=U.sha(out/'Manifest.json'),maps={s:U.sha(G.RUNTIME_TEXTURES/(U.NAME+'_'+s+'.png')) for s in U.MAPS},gallerySha256=U.sha(U.WORK/'Blender/NP_Plants_Unified_Runtime.blend'),latestExtension='ArtSource/Crops/Integration/G01_Completion.json',futureCapacity='Raspberry simulated as four 256px and four 128px surfaces without moving existing UV allocations',pendingReference=['Raspberry']);U.write(U.REVIEW/'Completion.json',globalSummary)
U.write(U.REVIEW/'UnityImportRequest.json',dict(status='REQUESTED',group='G01',registrySha256=U.sha(U.REGISTRY),expectedPrefabs=184,expectedMaterials=1));print('G01_TWO_BERRIES_COMPLETE: 30 crops, 151 phases, 90 sprites, 368 meshes, 184 prefabs; ONE material; Raspberry pending reference; Play Mode/mobile profiling NOT_RUN',flush=True)
