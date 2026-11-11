"""Record Editor limitations separately and bind the completion report to current assets."""
from crop_extension_config import *
log=OUT/'UnitySeedStore_Current.log';body=log.read_text(encoding='utf-8',errors='replace') if log.is_file() else ''
fixture=load(OUT/'UnityGameplayAudit.json') if (OUT/'UnityGameplayAudit.json').is_file() else {}
expected=['Store-to-seed-to-crop reverse links '+kind for _,kind,_,_ in NEW]
passed=fixture.get('status')=='PASS' and all(v in fixture.get('checks',[]) for v in expected)
blocked='Licensing initialization failed' in body or 'connection with the Unity Licensing Client has been lost' in body
status='PASS_EDITOR_FIXTURE' if passed else 'BLOCKED_BY_UNITY_LICENSING' if blocked else 'NOT_COMPLETED'
evidence=[line for line in body.splitlines() if any(v in line for v in ['Licensing initialization failed','Error: Timed-out','connection with the Unity Licensing Client has been lost'])]
attempt=dict(status=status,method='Hidden Unity batch process running CropGameplayValidation.Run',evidence=evidence,log=rel(log),playMode='NOT_RUN',note='The batch attempt was stopped after licensing initialization/reconnection failed; no Editor fixture result was produced.' if blocked and not passed else 'Editor fixture and Play Mode are recorded independently.')
(OUT/'UnityValidationAttempt.json').write_text(json.dumps(attempt,indent=2))
manifest=load(OUT/'Manifest.json');manifest['runtimeValidation']=status;manifest['unityPlayMode']='NOT_RUN'
for row in manifest['plants']:
 if row['key']=='wheat':row['initialBalance']='Existing Wheat ingredient ID/GUID, sale price 4 G and stack 99 retained; seed/growth follow integration defaults'
manifest['extensionSummary']=dict(addedCrops=9,totalCrops=21,seedShopProducts=21,totalUiSprites=63,acceptedModelsPreserved=True,legacyWheatIngredientReused=True)
(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
audit=load(OUT/'IntegrationAudit.json');audit.update(unityEditorImport=status,unityPlayMode='NOT_RUN');(OUT/'IntegrationAudit.json').write_text(json.dumps(audit,indent=2))
compiled=load(OUT/'Compilation.json');assert compiled['status']=='PASS' and all(digest(ROOT/p)==sha for p,sha in compiled['sourceHashes'].items())
summary=dict(status='APPLIED_AND_FILE_CHECKED',crops=21,seedProducts=21,phaseBindings=audit['stageBindings'],uiSprites=audit['icons'],assetAudit=audit['status'],compilation=compiled['status'],unityEditorFixture=status,unityPlayMode='NOT_RUN',preview=rel(ART/'G06_G09_SeedPackets_Preview.png'),previewSha256=digest(ART/'G06_G09_SeedPackets_Preview.png'),bindingManifestSha256=digest(OUT/'Manifest.json'))
(OUT/'Completion.json').write_text(json.dumps(summary,indent=2));print('CROP_EXTENSION_COMPLETION',json.dumps(summary),flush=True)
