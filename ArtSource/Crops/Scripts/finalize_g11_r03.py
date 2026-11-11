"""Record checked spatial foliage; preserve the historical R02 revision records."""
from g11_integration_config import *
review=ROOT/'ArtSource/Crops/G11/Review';base=load(review/'R03_Baseline.json');art=load(review/'ArtAudit.json');form=load(review/'R03_FormAudit.json');game=load(OUT/'IntegrationAudit.json');paint=load(review/'GimpReopenAudit.json');changes=load(review/'R03_AssetChanges.json');manifest=load(OUT/'Manifest.json')
assert all(r['status']=='PASS' for r in [art,form,game,paint]);assert changes['status']=='APPLIED_SCOPED_REVISION'
master=ROOT/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend';assert art['masterSha256']==form['masterSha256']==digest(master)
for p,sha in base['protected'].items():assert digest(ROOT/p)==sha,p
for p,c in changes['directChanges'].items():assert base['editableBefore'][p]==c['before'] and digest(ROOT/p)==c['after'],p
for p,c in changes['authorizedChanges'].items():assert digest(ROOT/p)==c['after'],p
for p,sha in form['sourceHashes'].items():assert digest(ROOT/p)==sha,p
comparison=ROOT/'ArtSource/Crops/Previews/G11_R03_SpatialReview.png';assert comparison.is_file()
manifest['runtimeValidation']='PASSED_R03_SPATIAL_ART_ASSET_AND_BINDING_CHECKS';(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
report=dict(status='APPLIED_AND_CHECKED',revision='G11 R03',scope='Radial leaf frames, front/back leaf placement, individually rolled and cupped blades; same accepted flowers and textures',artChecks=art['checks'],spatialChecks=form['checks'],integrationChecks=game['checks'],protectedFiles=len(base['protected']),changedFiles=len(changes['directChanges']),nonLeafComponentsRetained=form['nonLeafComponentsRetained'],preserved='Flower/stalk/bulb shapes, all textures/materials/icons and GUIDs, other 25 crops, gameplay balance and interaction hints',masterSha256=digest(master),bindingManifestSha256=digest(OUT/'Manifest.json'),preview=rel(comparison),previewSha256=digest(comparison),metrics=form['metrics'],unityPlayMode='NOT_RUN_BY_AGENT')
(review/'R03_Completion.json').write_text(json.dumps(report,indent=2))
for p in [OUT/'G11_Completion.json',OUT/'Completion.json']:
 data=load(p);data.update(status='APPLIED_AND_FILE_CHECKED',revision='G11 R03',masterSha256=digest(master),bindingManifestSha256=digest(OUT/'Manifest.json'),artChecks=art['checks'],assetChecks=game['checks'],preview=rel(comparison),previewSha256=digest(comparison),visualRevisionReport='ArtSource/Crops/G11/Review/R03_Completion.json');data['sourceHashes']={rel(s):digest(s) for s in (ROOT/'ArtSource/Crops/Scripts').glob('*g11*.py')};p.write_text(json.dumps(data,indent=2))
p=review/'Completion.json';data=load(p);data.update(revision='R03',masterSha256=digest(master),artChecks=art['checks'],revisionReport='ArtSource/Crops/G11/Review/R03_Completion.json');data['reviews']={n:digest(ROOT/'ArtSource/Crops/Previews'/n) for n in ['G11_GrowthStages.png','G11_VolumeReview.png','G11_FlowerAndHarvest.png','G11_R03_SpatialReview.png']};p.write_text(json.dumps(data,indent=2))
print('G11_R03_COMPLETE',form['checks'],'spatial checks;',game['checks'],'integration checks;',len(base['protected']),'protected files retained; visual Play Mode NOT_RUN_BY_AGENT',flush=True)
