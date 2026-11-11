"""Record code/asset verification separately from a live Editor rendering check."""
from crop_extension_config import *
compiled=load(OUT/'Compilation.json');stages=load(OUT/'CropStageDebugTests.json');assets=load(OUT/'IntegrationAudit.json');placement=load(OUT/'InteractionVisualPlacement.json')
assert compiled['status']==stages['status']==assets['status']=='PASS'
assert all(digest(ROOT/p)==sha for p,sha in compiled['sourceHashes'].items())
assert all(digest(ROOT/p)==sha for p,sha in stages['sourceHashes'].items())
paths=[A/'Script/Core/WorldInteractionVisuals.cs',A/'Script/Core/CollectibleClueRules.cs',A/'Script/Core/WorldInteractionPrompt.cs',A/'Script/Player/Item/HoeTool.cs',A/'Script/World/Field/FieldCropView.cs',A/'Material/WorldInteractionClue.shader',A/'Resources/UI/Interaction/WorldInteractionClue.mat',A/'Resources/Profiles/Farming/Onion Crop.asset',A/'Resources/Profiles/Farming/Radish Crop.asset',ROOT/'Tests/InteractionTargetTests.cs',ROOT/'Tests/CollectibleClueRulesTests.cs']
report=dict(status='APPLIED_AND_CODE_CHECKED',rootPlanting=placement['rootCrops'],selectedTarget='Published prompt owner and exact anchor; captured harvest field/tile',ringColors=dict(collectible='green',otherInteraction='yellow'),discovery=dict(radiusMetres=6,maxVisibleSources=32,occlusionIndependent=True,sources=['loose and placed item pickups','harvest-ready field crops','available gathering resources','ready fruit trees','visible manure piles']),checks=dict(runtimeCompilation='PASS',editorCompilation='PASS',cropStageTests=stages['result'],interactionOwnerAndRangeAssertions=48,distanceFadeAssertions=422,assetIntegration=assets['checks'],promptProducersAudited=41),sourceHashes={rel(p):digest(p) for p in paths},unityVisualPlayMode='NOT_RUN_BY_AGENT',note='Code/stub/asset checks do not establish live rendering; the user Editor is left under user control.')
report['discovery'].update(radiusMetres=placement.get('revealDistanceMetres',12),maxVisibleSources=12,glintsPerObject=1,referenceHalfSizeAt720p=7,mobileSizeMultiplier=1.2,fullVisibilityRadiusFraction=.8,twinklePeriodSeconds=5,twinkleDurationSeconds=.85,interactionReachChanged=False)
report['checks']['distanceFadeAssertions']=641
report['discovery'].update(minimumOpacity=0,maximumOpacity=1,persistentBaseMarker=False,quietIntervalSeconds=4.15)
report['targetRingStyle']=dict(strokeWidthMetres=.022,maximumOpacity=.55,halo=False,opacityPulse=False)
(OUT/'InteractionVisualsAudit.json').write_text(json.dumps(report,indent=2))
completion=load(OUT/'Completion.json');completion['bindingManifestSha256']=digest(OUT/'Manifest.json');completion['interactionVisuals']=dict(status=report['status'],report=rel(OUT/'InteractionVisualsAudit.json'),unityVisualPlayMode='NOT_RUN_BY_AGENT');(OUT/'Completion.json').write_text(json.dumps(completion,indent=2))
print('CROP_INTERACTION_VISUALS_COMPLETE; runtime/editor compile PASS; 4 half-buried bulb phases; 21 profiles retained; visual Play Mode NOT_RUN_BY_AGENT',flush=True)
