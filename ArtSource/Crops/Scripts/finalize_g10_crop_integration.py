"""Bind the application report to the current 25-crop assets and actual check results."""
from g10_integration_config import *
audit=load(OUT/'IntegrationAudit.json');compiled=load(OUT/'Compilation.json');debug=load(OUT/'CropStageDebugTests.json');icons=load(ART/'G10_IconAudit.json');csvAudit=load(OUT/'G10_CsvEditAudit.json');manifest=load(OUT/'Manifest.json')
assert all(r['status']=='PASS' for r in [audit,compiled,debug,icons,csvAudit])
assert len(manifest['plants'])==audit['crops']==audit['seedStoreProducts']==25
assert audit['stageBindings']==126 and audit['icons']==75
assert '25 authored crop schedules' in debug['result']
for data in [compiled,debug]:
 for p,sha in data['sourceHashes'].items():assert digest(ROOT/p)==sha,p
new=[r for r in manifest['plants'] if r['group']=='G10'];assert len(new)==4
manifest.update(runtimeValidation='PASSED_FILE_COMPILER_AND_CROP_LOGIC_CHECKS',unityEditorFixture='NOT_RUN_FOR_G10',unityPlayMode='NOT_RUN_BY_AGENT',extensionBaseline=rel(BASELINE))
(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
oldArt=load(ROOT/'ArtSource/Crops/G10/Review/Baseline.json');changes={}
allowed={rel(A/'Data/Balance/Items.csv'),rel(A/'Data/Balance/Crops.csv'),rel(A/'Resources/Shops/Crops Catalog.asset')}
for p,sha in oldArt['protected'].items():
 actual=digest(ROOT/p)
 if actual!=sha:
  assert p.replace('\\','/') in allowed,p
  changes[p]=dict(before=sha,after=actual)
summary=dict(status='APPLIED_AND_FILE_CHECKED',group='G10',addedCrops=4,crops=25,seedProducts=25,phaseBindings=126,uiSprites=75,assetAudit=audit['status'],assetChecks=audit['checks'],protectedFiles=audit['protectedFiles'],compilation=compiled['status'],cropLogicChecks=debug['result'],unityEditorFixture='NOT_RUN_FOR_G10',unityPlayMode='NOT_RUN_BY_AGENT',preview=rel(ART/'G10_SeedPackets_Preview.png'),previewSha256=digest(ART/'G10_SeedPackets_Preview.png'),bindingManifestSha256=digest(OUT/'Manifest.json'),authorizedArtBaselineChanges=changes,addedProfiles=[r['profile'] for r in new],initialValues='5 G seeds, 5-day growth, 12 G produce, flower produce nonfood; preceding 21 balances retained')
summary['sourceHashes']={rel(p):digest(p) for p in (ROOT/'ArtSource/Crops/Scripts').glob('*g10*crop*.py')}
(OUT/'G10_Completion.json').write_text(json.dumps(summary,indent=2))
current=load(OUT/'Completion.json');current.update(summary);current['g10Report']='ArtSource/Crops/Integration/G10_Completion.json';(OUT/'Completion.json').write_text(json.dumps(current,indent=2))
p=ROOT/'ArtSource/Crops/G10/Review/Completion.json';art=load(p);art.update(gameplayBindings='APPLIED',gameplayIntegration='ArtSource/Crops/Integration/G10_Completion.json');p.write_text(json.dumps(art,indent=2))
p=ROOT/'ArtSource/Crops/G10/Blender/G10_Manifest.json';art=load(p);art['integration']='Applied: G10 profiles, seed/produce/mutation items, paper packet icons and existing seed store';p.write_text(json.dumps(art,indent=2))
print('G10_GAMEPLAY_COMPLETE: 25 crops, 25 seed products, 126 phases, 75 sprites;',audit['checks'],'asset checks; compile PASS; Play Mode NOT_RUN_BY_AGENT',flush=True)
