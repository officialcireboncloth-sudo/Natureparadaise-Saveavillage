"""Historical one-time importer derivation. The maintained C# setup is authoritative.

Do not rerun this generator: subsequent facing/collider/validation corrections
live in CoopApprovedExteriorSetup.cs and would be overwritten by this template.
"""
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[3]
EDITORS = ROOT / 'Assets/Nature  Paradaise/Script/Editor'
source = (EDITORS / 'BarnApprovedExteriorSetup.cs').read_text(encoding='utf-8')
source = source.replace('BarnApprovedExterior', 'CoopApprovedExterior').replace('Barn_A', 'Coop_A').replace('Barn A', 'Coop A').replace('barn assets', 'coop assets').replace('barn LODs', 'coop LODs').replace('barn audit', 'coop audit')
source = source.replace('/Prefabs/Barn/Exterior', '/Prefabs/Coop/Exterior').replace('/Resources/Buildings/Barn Building.asset', '/Resources/Buildings/Coop Building.asset').replace('BarnExterior_Lv', 'CoopExterior_Lv')
source = source.replace('Nature Paradise/Barn/Apply Approved Coop A Exterior', 'Nature Paradise/Coop/Apply Approved Coop A Exterior').replace('Barn definition missing', 'Coop definition missing')
source = source.replace('BARN A UNITY IMPORT', 'COOP A UNITY IMPORT')
source = source.replace('{ 3938, 1788, 368 }', '{ 3316, 1278, 286 }')
source = source.replace('new Vector3(8.9f, 6.8f, 10.9f)', 'new Vector3(7.35f, 5.74f, 8.7f)')
start = source.index('            string name = $"Coop_A_Master_LOD')
end = source.index('            AssetDatabase.ImportAsset(path', start)
source = source[:start] + '''            string path = Models + "/" + ImportNames[lod];
            File.Copy("ArtSource/Coop_A/Exports/" + ImportNames[lod], path, true);
''' + source[end:]
source = source.replace('Mathf.Abs(glow.z) > 4.9f && Mathf.Abs(glow.x) < .1f', 'Mathf.Abs(glow.z) > 3.7f && Mathf.Abs(Mathf.Abs(glow.x) - 1.25f) < .1f')
source = source.replace('        definition.footprintWidth = 12;\n        definition.footprintDepth = 14;', '''        // Outside-field free placement measures these values in metres (cell size 1).
        // Offset the building within the centred site; reserve the full Lv5 entrance apron.
        definition.footprintWidth = 10;
        definition.footprintDepth = 13;''')
source = source.replace('        try\n        {\n            // Retain root/file GUIDs', '''        string previousGuid = null;
        long previousId = 0;
        if (existing) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadAssetAtPath<GameObject>(path), out previousGuid, out previousId);
        try
        {
            // Retain root/file GUIDs''')
source = source.replace('            model.localScale =', '''            model.localPosition = new Vector3(-.75f, 0, 1.625f);
            model.localScale =''')
source = source.replace('new Vector3(0, 2.1f, 0) * scale', 'new Vector3(-.75f, 0, 1.625f) + new Vector3(0, 1.9f, 0) * scale')
source = source.replace('new Vector3(8, 4.2f, 10) * scale', 'new Vector3(6, 3.8f, 7) * scale')
source = source.replace('            var entrance = Child(root.transform, "Entrance_Editable");', '''            var nest = Child(root.transform, "NestCollision_Editable").gameObject.AddComponent<BoxCollider>();
            nest.center = new Vector3(-.75f, 0, 1.625f) + new Vector3(3.505f, 1.115f, -1.4f) * scale;
            nest.size = new Vector3(.89f, 1.03f, 1.9f) * scale;
            var entrance = Child(root.transform, "Entrance_Editable");''')
source = source.replace('new Vector3(0, 0, -5.9f) * scale', 'new Vector3(-.75f, 0, 1.625f) + new Vector3(-1.25f, 0, -4.65f) * scale')
source = source.replace('            return PrefabUtility.SaveAsPrefabAsset(root, path);', '''            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Require(saved != null, "Prefab save failed");
            if (existing)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved, out string savedGuid, out long savedId);
                Require(previousGuid == savedGuid && previousId == savedId, "Existing prefab root/GUID retained");
            }
            return saved;''')
source = source.replace('Length == 1, "Single body collision"', 'Length == 2, "Body and nest collision only"')
source = source.replace('        Require(glow.z < -4.9f * Scales[level - 1], "Front faces entrance -Z");', '''        Vector3 correctedGlow = glow - authoring.ModelRoot.localPosition;
        Require(correctedGlow.z < -3.7f * Scales[level - 1] && correctedGlow.x < 0, "Front/left lantern faces entrance -Z");
        Require(authoring.ModelRoot.localPosition == new Vector3(-.75f, 0, 1.625f), "Site centre compensation");
        var reservation = new Bounds(Vector3.up * 5, new Vector3(10.002f, 20.002f, 13.002f));
        var renderer0 = group.GetLODs()[0].renderers[0];
        var b = renderer0.localBounds;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            Require(reservation.Contains(renderer0.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x,y,z)))), "Model fits reserved site at Lv" + level);
        Require(reservation.Contains(authoring.Entrance.position + Vector3.up), "Entrance fits reserved site");''')
source = source.replace('"[Barn A]', '"[Coop A]').replace('Single body collision', 'Body and nest collision')
source = source.replace('barn assets', 'coop assets').replace('for applying barn', 'for applying coop')
source = source.replace('        File.WriteAllText(Output + "/UnityImportAudit.txt", report.ToString());', '''        File.WriteAllText(Output + "/UnityImportAudit.txt", report.ToString());
        string manifestPath = Output + "/MeshManifest.json";
        if (File.Exists(manifestPath))
            File.WriteAllText(manifestPath, File.ReadAllText(manifestPath)
                .Replace("MODEL_BUILT_EXPORTED_NOT_APPLIED_TO_UNITY", "APPLIED_TO_UNITY_VALIDATED")
                .Replace("MODEL_BUILT_NOT_APPLIED_TO_UNITY", "APPLIED_TO_UNITY_VALIDATED")
                .Replace("\\"unityAssetsModified\\": false", "\\"unityAssetsModified\\": true"));''')
source = source.rstrip()[:-1] + '''
    [MenuItem("Nature Paradise/Coop/Open Approved Coop A Lv3", false, 106)]
    public static void OpenPreview()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(Prefabs + "/CoopExterior_Lv3.prefab");
        Selection.activeGameObject = stage.prefabContentsRoot;
        SceneView.FrameLastActiveSceneView();
    }
}
'''
(EDITORS / 'CoopApprovedExteriorSetup.cs').write_text(source, encoding='utf-8')
for name in ['CoopApprovedExteriorSetup.cs', 'CoopApprovedExteriorValidation.cs']:
    meta = EDITORS / (name + '.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n', encoding='utf-8')
print('COOP importer prepared; no barn files changed.')
