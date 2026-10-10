using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Exercises actual property restoration and portal routing in an isolated preview scene.</summary>
public static class BarnApprovedExteriorValidation
{
    public static string Run()
    {
        var report = new StringBuilder("PropertySite restore + rotated entrance checks: PASS\n");
        Scene preview = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Barn validation - never save to Map");
        SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            var site = root.AddComponent<PropertySite>();
            Invoke(site, "Awake");
            var home = root.AddComponent<AnimalHome>();
            home.site = site;
            var portal = root.AddComponent<BarnInterior>();
            portal.ConfigureScene(home, root.transform);
            for (int level = 1; level <= 4; level++)
            {
                foreach (float yaw in new[] {0f, 90f, 180f, 270f})
                {
                    root.transform.SetPositionAndRotation(new Vector3(0, -500, 0), Quaternion.Euler(0, yaw, 0));
                    Invoke(site, "Restore", new PropertySiteSaveData {
                        buildingId = "building.barn", state = BuildingConstructionState.Completed,
                        currentLevel = level, unlocked = true
                    });
                    Invoke(portal, "RefreshExteriorAccess");
                    var authoring = root.GetComponentInChildren<BarnExteriorAuthoring>();
                    Require(authoring != null, "Restored combined exterior Lv" + level);
                    Require(home.door == authoring.Entrance, "Animal entry follows active prefab Lv" + level);
                    Vector3 local = root.transform.InverseTransformPoint(home.Entry);
                    float scale = .625f + .125f * level;
                    Require(Vector3.Distance(local, new Vector3(0, 0, 5.9f) * scale) < .001f,
                        "Portal entry rotated to front Lv" + level);
                    Require(Vector3.Dot(home.door.forward, root.transform.forward) > .999f, "Entrance matches placement direction");
                    Require(authoring.ModelRoot.GetComponent<LODGroup>().GetLODs().Length == 3, "Restored LODGroup");
                    Require(!authoring.BuildingCollision.bounds.Contains(home.Entry + Vector3.up), "Entry outside collider");
                }
                report.AppendLine($"Lv{level}: saved state restores new prefab; 0/90/180/270-degree animal/portal entry matches placement +Z PASS.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
        return report.ToString();
    }

    static void Invoke(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }
    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Barn runtime path validation: " + message);
    }
}
