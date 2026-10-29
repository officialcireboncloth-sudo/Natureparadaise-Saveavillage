using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Builds Mina's reusable animated visual. Does not rebuild or save any world scene.</summary>
public static class NPCCharacterSetup
{
    const string Root = "Assets/Nature  Paradaise";
    public const string MinaPath = Root + "/Resources/Characters/Mina.prefab";
    [MenuItem("Nature Paradise/Narrative/Apply Animated Mina")]
    public static void Apply()
    {
        string folder = Root + "/Resources/Characters";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        // Player gameplay uses Generic clips. The existing shared copy retargets those motions as Humanoid;
        // changing the main player importer here would invalidate tool and horse animation bindings.
        var shared = AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(Root + "/Player/Animations/Shared/Builder Humanoid Actions.asset");
        var idle = shared?.Find("Idle")?.clip;
        var walk = shared?.Find("Walk")?.clip;
        if (idle == null || walk == null || !idle.isHumanMotion || !walk.isHumanMotion)
            throw new InvalidOperationException("Shared player Humanoid Idle/Walking clips are missing.");
        string libraryPath = folder + "/Mina Actions.asset";
        var library = AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(libraryPath);
        if (library == null) { library = ScriptableObject.CreateInstance<HumanoidActionLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
        library.actions = new() { new() { id = "Idle", clip = idle }, new() { id = "Walk", clip = walk } };
        EditorUtility.SetDirty(library);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Pack/Toon Series/Toon Farm Pack/Prefabs/Characters/TFP_Female_Gardener_01A.prefab");
        if (source == null) throw new InvalidOperationException("Mina gardener prefab is missing.");
        var root = new GameObject("Mina");
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "Mina_Gardener_Humanoid";
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Mina requires a valid skinned Humanoid Avatar.");
            // Measure the evaluated retargeted idle, not the vendor bind pose or cached renderer bounds.
            // Humanoid translation retargeting changes the old vendor skeleton's evaluated height.
            Bounds bounds;
            var bones = model.GetComponentsInChildren<Transform>(true);
            var positions = bones.Select(t => t.localPosition).ToArray();
            var rotations = bones.Select(t => t.localRotation).ToArray();
            var scales = bones.Select(t => t.localScale).ToArray();
            using (var preview = new HumanoidActionPlayer(animator, library))
            { preview.Evaluate(.01f); bounds = VisibleMeshBounds(model); }
            for (int i = 0; i < bones.Length; i++)
            { bones[i].localPosition = positions[i]; bones[i].localRotation = rotations[i]; bones[i].localScale = scales[i]; }
            float scale = PlayerDimensions.Height / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition -= Vector3.up * bounds.min.y * scale;
            animator.runtimeAnimatorController = null; animator.applyRootMotion = false; animator.fireEvents = false;
            var motion = root.AddComponent<NPCCharacterMotion>(); motion.character = animator; motion.actions = library;
            PrefabUtility.SaveAsPrefabAsset(root, MinaPath);
            Debug.Log($"[NPC] Mina uses her own Humanoid rig + player Idle/Walking; height {PlayerDimensions.Height:F2} m; scale {scale:F4}. No scene changed.");
        }
        finally { Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
    }
    public static Bounds VisibleMeshBounds(GameObject model)
    {
        Bounds bounds = default; bool found = false;
        foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sharedMesh == null) continue;
            var mesh = new Mesh();
            try
            {
                // Compensate renderer scale before TransformPoint, otherwise uniform NPC scale is counted twice.
                renderer.BakeMesh(mesh, true);
                foreach (var vertex in mesh.vertices)
                {
                    var point = model.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }
        if (!found || bounds.size.y < .1f) throw new InvalidOperationException("No visible skinned Mina mesh.");
        return bounds;
    }
}
