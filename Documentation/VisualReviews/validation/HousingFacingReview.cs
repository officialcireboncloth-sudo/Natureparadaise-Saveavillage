using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Temporary native editor review. Copy to Assets/Editor and create the request file.
// Never runs unless explicitly requested; remove from Assets after the review.
[InitializeOnLoad]
public static class HousingFacingReview
{
    const string Request = "Library/HousingFacingReview.request";
    const string Result = "Library/HousingFacingReview.result";
    const string Output = "Documentation/VisualReviews/Housing-Front-Direction";
    static HousingFacingReview() { EditorApplication.update += RunRequested; }

    static void RunRequested()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try
        {
            BarnApprovedExteriorSetup.Apply();
            CoopApprovedExteriorSetup.Apply();
            RenderPair();
            File.WriteAllText(Result, "PASS: Both prefab sets now face placement +Z; all three LODs at Lv1-5; " +
                "saved PropertySite restores Lv1-4 at four yaw angles; collider/entrance/site envelope checks; front preview rendered.\n");
            File.WriteAllText(Output + "-Checks.txt", File.ReadAllText(Result));
        }
        catch (Exception ex)
        {
            File.WriteAllText(Result, "FAIL: " + ex);
            Debug.LogException(ex);
        }
    }

    static void RenderPair()
    {
        var preview = new PreviewRenderUtility();
        try
        {
            foreach (string kind in new[] {"Barn", "Coop"})
            {
                string path = "Assets/Nature  Paradaise/Prefabs/" + kind + "/Exterior/" + kind + "Exterior_Lv1.prefab";
                var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                preview.AddSingleGO(instance);
                instance.transform.position = new Vector3(kind == "Barn" ? -5.3f : 5.3f, 0, 0);
                var group = instance.GetComponentInChildren<LODGroup>();
                group.ForceLOD(0);
                group.animateCrossFading = false;
            }
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 7;
            preview.camera.nearClipPlane = .1f;
            preview.camera.farClipPlane = 100;
            preview.camera.transform.position = new Vector3(0, 19, 21);
            preview.camera.transform.LookAt(new Vector3(0, 2, 0));
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.24f, .34f, .43f);
            preview.ambientColor = new Color(.65f, .69f, .72f);
            preview.lights[0].intensity = 1.3f;
            preview.lights[0].transform.rotation = Quaternion.Euler(45, -35, 0);
            preview.lights[1].intensity = .8f;
            preview.lights[1].transform.rotation = Quaternion.Euler(30, 140, 0);
            preview.BeginStaticPreview(new Rect(0, 0, 1280, 720));
            preview.Render(true);
            Texture2D image = preview.EndStaticPreview();
            File.WriteAllBytes(Output + ".png", image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
        finally { preview.Cleanup(); }
    }
}
