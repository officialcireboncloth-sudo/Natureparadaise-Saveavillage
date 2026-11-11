"""Complete the reference tulip batch and bind evidence to the current 26-crop setup."""
from g11_integration_config import *
artRoot=ROOT/'ArtSource/Crops/G11';audit=load(OUT/'IntegrationAudit.json');compiled=load(OUT/'Compilation.json');debug=load(OUT/'CropStageDebugTests.json');art=load(artRoot/'Review/ArtAudit.json');gimp=load(artRoot/'Review/GimpReopenAudit.json');icons=load(ART/'G11_IconAudit.json');csvAudit=load(OUT/'G11_CsvEditAudit.json');manifest=load(OUT/'Manifest.json')
assert all(r['status']=='PASS' for r in [audit,compiled,debug,art,gimp,icons,csvAudit])
audit['unityEditorImport']='NOT_RUN_FOR_G11';(OUT/'IntegrationAudit.json').write_text(json.dumps(audit,indent=2))
assert len(manifest['plants'])==audit['crops']==audit['seedStoreProducts']==26 and audit['stageBindings']==131 and audit['icons']==78
assert '26 authored crop schedules' in debug['result']
assert art['masterSha256']==digest(artRoot/'Blender/G11_Tulip_Master.blend')
for data in [compiled,debug]:
 for p,sha in data['sourceHashes'].items():assert digest(ROOT/p)==sha,p
row=next(r for r in manifest['plants'] if r['group']=='G11');assert row['species']=='Tulip' and row['stages'][0]['name']=='Bulb'
manifest.update(runtimeValidation='PASSED_FILE_COMPILER_AND_CROP_LOGIC_CHECKS',unityEditorFixture='NOT_RUN_FOR_G11',unityPlayMode='NOT_RUN_BY_AGENT',extensionBaseline=rel(BASELINE))
(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
oldArt=load(artRoot/'Review/Baseline.json');allowed={rel(A/'Data/Balance/Items.csv'),rel(A/'Data/Balance/Crops.csv'),rel(A/'Resources/Shops/Crops Catalog.asset')};changes={}
for p,sha in oldArt['protected'].items():
 actual=digest(ROOT/p)
 if actual!=sha:
  assert p.replace('\\','/') in allowed,p
  changes[p]=dict(before=sha,after=actual)
reviews=['G11_GrowthStages.png','G11_VolumeReview.png','G11_FlowerAndHarvest.png']
for name in reviews:assert (ROOT/'ArtSource/Crops/Previews'/name).is_file()
summary=dict(status='APPLIED_AND_FILE_CHECKED',group='G11',addedCrops=1,crops=26,seedProducts=26,phaseBindings=131,uiSprites=78,assetAudit='PASS',assetChecks=audit['checks'],artChecks=art['checks'],protectedFiles=audit['protectedFiles'],compilation='PASS',cropLogicChecks=debug['result'],unityEditorFixture='NOT_RUN_FOR_G11',unityPlayMode='NOT_RUN_BY_AGENT',preview='ArtSource/Crops/Previews/G11_GrowthStages.png',previewSha256=digest(ROOT/'ArtSource/Crops/Previews/G11_GrowthStages.png'),seedPreview=rel(ART/'G11_SeedPackets_Preview.png'),bindingManifestSha256=digest(OUT/'Manifest.json'),masterSha256=art['masterSha256'],authorizedArtBaselineChanges=changes,addedProfiles=[row['profile']],reference='Current user Tulip growth reference attachment',features='Pear bulb, rolled shoots, broad arching leaves, pink tapered bud, six closed overlapping cup tepals, real open upper cavity, curved harvested stem with one leaf',initialValues='Existing flower integration defaults: seed 5 G, harvest 5 days, produce 12 G, nonfood; preceding 25 crop balances retained')
summary['sourceHashes']={rel(p):digest(p) for p in (ROOT/'ArtSource/Crops/Scripts').glob('*g11*.py')}
(OUT/'G11_Completion.json').write_text(json.dumps(summary,indent=2))
current=load(OUT/'Completion.json');current.update(summary);current['g11Report']='ArtSource/Crops/Integration/G11_Completion.json';(OUT/'Completion.json').write_text(json.dumps(current,indent=2))
(artRoot/'Review/Completion.json').write_text(json.dumps(dict(status='ART_COMPLETE_AND_APPLIED',phaseVisuals=6,nativeMeshes=12,prefabs=6,atlasPages=1,artChecks=art['checks'],closedPetals=art['closedPetals'],physicalJoints=len(art['floralJoints']),fbxRoundtrips=art['verifiedFbx'],masterSha256=art['masterSha256'],gameplayBindings='APPLIED',gameplayIntegration='ArtSource/Crops/Integration/G11_Completion.json',unityPlayMode='NOT_RUN_BY_AGENT',reviews={n:digest(ROOT/'ArtSource/Crops/Previews'/n) for n in reviews}),indent=2))
p=artRoot/'Blender/G11_Manifest.json';data=load(p);data['integration']='Applied: Tulip profile, seed/produce/mutation items, paper packet icon and existing seed store';p.write_text(json.dumps(data,indent=2))
print('G11_TULIP_COMPLETE: 26 crops, 26 seed products, 131 phases, 78 sprites;',audit['checks'],'asset checks;',art['checks'],'art checks; compile PASS; Play Mode NOT_RUN_BY_AGENT',flush=True)
