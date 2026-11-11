"""Close the atlas migration only when geometry, bindings and repeat publishing checks pass."""
from unified_plant_atlas import *
atlas=load(REVIEW/'Audit.json');pipeline=load(REVIEW/'PipelineAudit.json');game=load(SOURCE/'Integration/IntegrationAudit.json');compiled=load(SOURCE/'Integration/Compilation.json');packing=load(REVIEW/'Packing.json');registry=load(REGISTRY);base=load(BASELINE)
assert all(r['status']=='PASS' for r in [atlas,pipeline,game,compiled,packing])
assert atlas['meshes']==320 and atlas['prefabs']==160 and atlas['runtimeMaterials']==1 and game['crops']==26
assert sha(REGISTRY)==packing['registrySha256']
for name,s in atlas['sourceHashes'].items():assert sha(ROOT/name)==s,name
for name,s in compiled['sourceHashes'].items():assert sha(ROOT/name)==s,name
for name,s in base['protected'].items():assert sha(ROOT/name)==s,name
for suffix in MAPS:assert sha(TEXTURES/(NAME+'_'+suffix+'.png'))==packing['maps'][suffix]
preview=SOURCE/'Previews/Plants_Unified_Comparison.png';thumb=SOURCE/'Previews/Plants_Unified_Atlas.png';gallery=WORK/'Blender/NP_Plants_Unified_Runtime.blend'
assert all(p.is_file() and p.stat().st_size>1000 for p in [preview,thumb,gallery])
assert gallery.stat().st_size<100*1024*1024,'Keep gallery below repository file-size limit'
gimp=(REVIEW/'Gimp.log').read_text(encoding='utf-8',errors='replace');assert 'UNIFIED_GIMP_REVIEW_PASS' in gimp and 'batch command executed successfully' in gimp
changes=migration_report();manifest=load(SOURCE/'Integration/Manifest.json');assert manifest['plants']==base['integrationBefore']['plants']
manifest['runtimeAtlas']=dict(name=NAME,size=[SIZE,SIZE],material=rel(MATERIAL),materialGuid=guid(MATERIAL),registry=rel(REGISTRY),registrySha256=sha(REGISTRY),policy='One shared runtime atlas/material for all current and future crop groups; stable append-only allocations')
manifest['runtimeValidation']='PASSED_UNIFIED_ATLAS_GEOMETRY_BINDING_AND_COMPILER_CHECKS';write(SOURCE/'Integration/Manifest.json',manifest)
editor=load(REVIEW/'UnityImportAudit.json') if (REVIEW/'UnityImportAudit.json').exists() else dict(status='NOT_RUN_BY_AGENT')
if editor['status']=='FAIL':raise RuntimeError('Unity import validation failed: '+str(editor))
report=dict(status='APPLIED_AND_FILE_CHECKED',revision='Unified crop atlas; accepted G11 R03 geometry retained',crops=26,meshes=320,prefabs=160,runtimeMaterials=1,atlas=NAME,atlasSize=[SIZE,SIZE],maps=packing['maps'],surfaces=len(registry['slots']),freePercent=registry['freePercent'],primaryLeafAndPetalPixels='Exact source pixels and dimensions retained before the established mobile ASTC 6x6 import',utilitySurfaces='Area-resampled only for small stems/seeds/pollen; extra gutters; normal vectors renormalized',geometry='Positions/normals/tangents and triangle topology preserved in every native mesh; FBX geometry also preserved',assetChecks=atlas['checks'],integrationChecks=game['checks'],compilation=compiled['status'],protectedFiles=len(base['protected']),repeatPublishing=pipeline['status'],futureCapacity='G01/G12/G13 simulated with current UV allocations unchanged',shaderInstancing='Enabled; per-instance lifecycle tint and fade',unityEditorImport=editor['status'],unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN',registrySha256=sha(REGISTRY),bindingManifestSha256=sha(SOURCE/'Integration/Manifest.json'),preview=rel(preview),previewSha256=sha(preview),atlasPreview=rel(thumb),atlasPreviewSha256=sha(thumb),blenderGallery=rel(gallery),gallerySha256=sha(gallery),materialSha256=sha(MATERIAL),shaderSha256=sha(SHADER),preserved='Crop profiles/IDs/prices/growth schedules, planting depths, accepted geometry, seed/produce UI icons and interaction hints',changedFiles=len(changes['authorizedChanges']))
write(REVIEW/'Completion.json',report)
for p in [SOURCE/'Integration/Completion.json',SOURCE/'Integration/G11_Completion.json']:
 data=load(p);data.update(status='APPLIED_AND_FILE_CHECKED',revision=report['revision'],runtimeAtlas=manifest['runtimeAtlas'],atlasMigrationReport=rel(REVIEW/'Completion.json'),bindingManifestSha256=report['bindingManifestSha256'],assetChecks=game['checks'],unifiedAtlasChecks=atlas['checks'],preview=rel(preview),previewSha256=sha(preview));write(p,data)
print('UNIFIED_ATLAS_COMPLETE',atlas['checks'],'atlas checks;',game['checks'],'binding checks; 26 crops, 320 meshes, 160 prefabs, ONE runtime material; mobile profiling NOT_RUN',flush=True)
