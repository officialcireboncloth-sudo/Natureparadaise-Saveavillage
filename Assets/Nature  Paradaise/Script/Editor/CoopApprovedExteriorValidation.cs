using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Exercises saved coop restoration and actual portal routing in an isolated preview scene.</summary>
public static class CoopApprovedExteriorValidation
{
    public static string Run()
    {
        var report = new StringBuilder("PropertySite restore + rotated coop entrance checks: PASS\n");
        var definition = AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>("Assets/Nature  Paradaise/Resources/Buildings/Coop Building.asset");
        Require(definition != null && definition.footprintWidth == 10 && definition.footprintDepth == 13, "Reserved site 10x13 metres");
        Require(definition.levels.Count == 4 && definition.GetLevel(5) == null, "Existing gameplay levels retained; Lv5 visual only");
        Scene preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Coop validation - never save to Map");
        SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            var site = root.AddComponent<PropertySite>();
            Invoke(site, "Awake");
            var home = root.AddComponent<AnimalHome>();
            home.site = site;
            var legacyBarrier = root.AddComponent<BoxCollider>();
            var portal = root.AddComponent<BarnInterior>();
            portal.ConfigureScene(home, root.transform, legacyBarrier);
            int[] capacities = {6, 12, 20, 30};
            for (int level = 1; level <= 4; level++)
            {
                foreach (float yaw in new[] {0f, 90f, 180f, 270f})
                {
                    root.transform.SetPositionAndRotation(new Vector3(35, -500, 27), Quaternion.Euler(0, yaw, 0));
                    Invoke(site, "Restore", new PropertySiteSaveData {
                        buildingId = "building.coop", state = BuildingConstructionState.Completed,
                        currentLevel = level, unlocked = true
                    });
                    Invoke(portal, "RefreshExteriorAccess");
                    Invoke(portal, "ResolveInteriorDestination");
                    var authored = root.GetComponentInChildren<BarnExteriorAuthoring>();
                    Require(authored != null, "Restored approved exterior Lv" + level);
                    Require(home.Kind == AnimalHousingKind.Coop && home.Capacity == capacities[level - 1], "Coop kind/capacity");
                    Require(home.door == authored.Entrance, "Animal route uses actual doorway");
                    Vector3 local = root.transform.InverseTransformPoint(home.Entry);
                    float scale = .625f + .125f * level;
                    Vector3 expected = new Vector3(.75f, 0, -1.625f) + new Vector3(1.25f, 0, 4.65f) * scale;
                    Require(Vector3.Distance(local, expected) < .002f, "Rotated entrance/site offset");
                    Require(Vector3.Dot(home.door.forward, root.transform.forward) > .999f, "Entrance matches placement direction");
                    Require(!authored.BuildingCollision.bounds.Contains(home.Entry + Vector3.up), "Entrance outside body collider");
                    Require(!legacyBarrier.enabled, "Legacy portal collision disabled");
                    Require(authored.GetComponentsInChildren<Collider>(true).Length == 2, "Two simple authored colliders");
                    Require(authored.ModelRoot.GetComponent<LODGroup>().GetLODs().Length == 3, "Restored LODGroup");
                    Require((string)Read(portal, "interiorSceneName") == "CoopInterior" &&
                        (string)Read(portal, "entrySpawnId") == "coop-interior-entry", "Coop destination routing");
                }
                report.AppendLine($"Lv{level}: saved state restores approved mesh; 0/90/180/270-degree entrance, collision, capacity and CoopInterior routing PASS.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
        return report.ToString();
    }

    static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    static object Read(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Coop runtime path validation: " + message);
    }
}
