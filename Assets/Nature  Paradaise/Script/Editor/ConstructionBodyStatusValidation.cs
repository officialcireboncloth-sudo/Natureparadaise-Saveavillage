#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Real player-capsule moves, worker navigation, and rendered status using the construction kit.</summary>
public static class ConstructionBodyStatusValidation
{
    const string Output = "ArtSource/ConstructionBodyStatus";
    static StringBuilder report;
    static int checks;
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); checks++; report.AppendLine("PASS " + message); }
    public static void RunBatch()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/HandoffAudit-2026-10-23/CleanClone/")) throw new InvalidOperationException("Use isolated review fixture.");
        Directory.CreateDirectory(Output); report = new StringBuilder("CONSTRUCTION BODY AND STATUS — NATIVE UNITY\n");
        ConstructionProjectVisuals visuals = null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Check(SaveManager.Instance == null, "No production save manager");
            var settings = ConstructionWorkerSettings.Load();
            Check(settings != null && settings.workBench != null && settings.workerPrefab != null, "Real Lumber worker and bench assets resolve");
            var origin = new Vector3(10000, 0, 10000);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Fixture ground";
            floor.transform.position = origin + Vector3.down * .25f; floor.transform.localScale = new Vector3(80, .5f, 80);
            var owner = new GameObject("Construction fixture"); owner.transform.position = origin;
            var work = origin + Vector3.back * 8;
            visuals = new ConstructionProjectVisuals(owner.transform, owner.transform,
                new Bounds(origin + Vector3.up * 2, new Vector3(8, 4, 8)), work,
                new BuildingLevelDefinition { level = 1 }, null, false, settings);
            visuals.Worker.SetPositionAndRotation(work, Quaternion.identity);
            visuals.SetStage(.5f); visuals.Animate(false, false, 0);
            var workerBody = visuals.Worker.GetComponent<ConstructionWorkerBody>(); workerBody.Refresh();
            var capsule = visuals.Worker.GetComponent<CapsuleCollider>();
            Check(capsule != null && capsule.enabled && !capsule.isTrigger, "Dispatched builder has a solid capsule");
            Check(capsule.bounds.size.y > 1.4f && capsule.bounds.size.y < 1.9f && capsule.bounds.size.x < .65f,
                "Builder collision matches metre-sized body instead of an oversized box");
            var actor = new GameObject("Player-sized collision fixture").AddComponent<CharacterController>();
            actor.height = PlayerDimensions.Height; actor.radius = PlayerDimensions.Radius;
            actor.center = Vector3.up * actor.height / 2; actor.skinWidth = PlayerDimensions.SkinWidth;
            actor.stepOffset = PlayerDimensions.StepOffset;
            void Place(Vector3 position) { actor.enabled = false; actor.transform.position = position; actor.enabled = true; Physics.SyncTransforms(); }
            void Walk(Vector3 direction) { for (int i = 0; i < 60; i++) actor.Move(direction * .05f + Vector3.down * .01f); }
            Place(work + Vector3.left * 2); Walk(Vector3.right);
            Check(actor.transform.position.x < work.x - .3f, "Actual player capsule cannot walk through builder");
            Place(work + Vector3.back * 3);
            Check(ConstructionWorkerPath.Ground(work, out var grounded) && Mathf.Abs(grounded.y) < .01f,
                "Worker ground query ignores its own capsule and stays on ground");
            Check(ConstructionWorkerPath.Find(work, work + Vector3.back * 6) != null,
                "Builder can leave the work station with its own collision enabled");
            var bench = owner.GetComponentsInChildren<Transform>().Single(t => t.name == "BuilderWorkBench");
            var benchBox = bench.GetComponent<BoxCollider>();
            Check(benchBox != null && benchBox.enabled && !benchBox.isTrigger && benchBox.bounds.min.y < .02f,
                "Bench is solid from the floor to tabletop, including gap between legs");
            var center = benchBox.bounds.center; center.y = 0;
            Place(center + Vector3.right * 2); Walk(Vector3.left);
            report.AppendLine($"Bench bounds {benchBox.bounds}; player after move {actor.transform.position - center}");
            Check(actor.transform.position.x > benchBox.bounds.max.x + .1f && actor.transform.position.y < .15f,
                "Player cannot walk through or step onto the bench");
            Check(!ConstructionWorkerPath.Segment(center + Vector3.right * 2, center + Vector3.left * 2),
                "Builder navigation still detects workbench as a real obstacle");
            Place(work); capsule.enabled = false; workerBody.Refresh();
            Check(!capsule.enabled, "Restored builder overlapping player defers collision without pushing actor");
            var before = actor.transform.position; workerBody.Refresh();
            Check(actor.transform.position == before, "Deferred collision never teleports player");
            Place(work + Vector3.back * 3); workerBody.Refresh();
            Check(capsule.enabled, "Builder becomes solid once player walks clear");
            visuals.UpdateBar("Coop", "Istirahat · dilanjutkan besok", .5f);
            var status = Object.FindFirstObjectByType<ConstructionSiteStatus>();
            Check(status != null, "Site status exists without a nearby PlayerController or bottom hint");
            var texts = status.GetComponentsInChildren<TextMeshProUGUI>();
            Check(texts.Any(t => t.text.Contains("Coop") && t.text.Contains("50")) && texts.Any(t => t.text.Contains("Istirahat")),
                "Status names the building, target level, current activity and percent");
            Check(status.GetComponentInChildren<GraphicRaycaster>() == null && texts.All(t => !t.raycastTarget),
                "Status cannot intercept PC clicks or mobile touch");
            var camera = new GameObject("Review camera", typeof(Camera)).GetComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 9; camera.transform.position = work + new Vector3(8, 9, -12);
            camera.transform.LookAt(work + Vector3.up); camera.backgroundColor = new Color(.14f, .19f, .16f); camera.clearFlags = CameraClearFlags.SolidColor;
            status.SendMessage("LateUpdate");
            Check(status.GetComponent<Canvas>().enabled && status.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay,
                "Projected site status remains visible without 3D walls obscuring text");
            var light = new GameObject("Review light", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -35, 0); RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            Render(camera, status, Output + "/Construction_Status.png", 1280, 720);
            status.SendMessage("LateUpdate");
            float normalScale = status.GetComponent<Canvas>().scaleFactor; camera.orthographicSize = 18; status.SendMessage("LateUpdate");
            Check(Mathf.Abs(status.GetComponent<Canvas>().scaleFactor - normalScale) < .01f, "Status retains screen readability when zooming out");
            Render(camera, status, Output + "/Construction_Status_ZoomOut.png", 1280, 720);
            owner.SetActive(false); status.SendMessage("LateUpdate");
            Check(!status.GetComponent<Canvas>().enabled, "Outdoor construction status hides when the site is disabled for an interior");
            owner.SetActive(true); status.SendMessage("LateUpdate");
            Check(status.GetComponent<Canvas>().enabled, "Status returns with its active outdoor site");
            visuals.HideSite();
            Check(!status.gameObject.activeSelf && !bench.gameObject.activeInHierarchy, "Completion hides status and bench collision with scenery");
            visuals.UpdateBar("Coop", "Selesai", 1);
            Check(!status.gameObject.activeSelf, "Returning or queued-returning builder cannot resurrect site status");
            visuals.Dispose(); visuals = null;
            Check(capsule == null || !capsule.gameObject.activeInHierarchy, "Cancel/dispose disables worker collision immediately");
            Check(status == null, "Cancel/dispose also removes the projected status canvas");
            ConstructionSiteSafetyValidation.Run();
            Check(File.ReadAllText("ArtSource/Construction_Kit/Previews/SiteSafetyValidation.txt").Contains("AUDIT_PASS"),
                "Existing native construction gate, deferred wall and dust regression passes");
            report.AppendLine("CHECKS " + checks + "; CONSTRUCTION_BODY_STATUS_PASS; no production save/scene writes; native physics and render tests, not full Play Mode.");
            File.WriteAllText(Output + "/Audit.txt", report.ToString());
            if (File.Exists(Output + "/Error.txt")) File.Delete(Output + "/Error.txt");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        { File.WriteAllText(Output + "/Error.txt", report + "\n" + e); Debug.LogException(e); visuals?.Dispose(); EditorApplication.Exit(1); }
    }
    static void Render(Camera camera, ConstructionSiteStatus status, string path, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24); var previous = RenderTexture.active;
        camera.targetTexture = rt;
        // Overlay canvases are not included by Camera.Render; capture the same UI using its camera compositor.
        var canvas = status.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .1f;
        status.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
        foreach (var text in status.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
        camera.Render(); RenderTexture.active = rt; var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
    }
}
#endif
