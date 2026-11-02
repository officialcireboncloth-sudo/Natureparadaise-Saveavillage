# Completed house construction screens after loading

## Cause and fix

A reported Slot 1 had a completed level 1 house, no pending upgrade, and a non-null construction job with zero required work. Unity JSON can deserialize a null job into this empty placeholder. The house restore path resumed it and created construction screens.

PlayerHouseController now uses ConstructionJobData.ShouldRestoreFor, matching PropertySite and BuildingSite. Empty and stale jobs cannot restart work on a completed house. Genuine active upgrades retain their existing progress and legacy migration path.

ConstructionProject also keeps screens hidden when a completed builder's return journey is queued. The return journey can continue without reconstructing the work site.

## Validation

Unity 6000.0.81f1 batch validation exited 0; 51 checks passed (HOUSE_RESTORE_PASS). Checks used real house assets and an isolated controller: new game, completed house, empty JSON job, stale phases, save capture roundtrip, active paid upgrade, legacy upgrade, repeated load, and returning/queued-returning builders. This was a native restore/object test, not a full Play Mode test.

Both production save and autosave hashes were unchanged. No production scenes or save slots were written. To clear objects from an already running session, stop Play, start again, and load the existing slot. No new game or save deletion is needed.

The regression entry point is HouseConstructionRestoreValidation.RunBatch. It is restricted to the existing HandoffAudit-2026-10-23/CleanClone review project. Prepare ArtSource/HouseConstructionRestore/Slot1House.json in that isolated project with only the PlayerHouseSaveData payload (completed level 1, pending level 0, empty construction job); the test never opens production save files. Native audit output is ArtSource/HouseConstructionRestore/Audit.txt in that fixture.
