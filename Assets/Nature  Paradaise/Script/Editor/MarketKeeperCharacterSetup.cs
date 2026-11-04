#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Reusable market keeper using the same shared Humanoid actions as other NPCs.</summary>
public static class MarketKeeperCharacterSetup
{
    const string Root = "Assets/Nature  Paradaise";
    public const string PrefabPath = Root + "/Resources/Characters/MarketKeeper.prefab";
    public const string ActionsPath = Root + "/Resources/Characters/MarketKeeper Actions.asset";
    const string CartVisual = Root + "/Prefabs/World/MarketStand/MarketStandVisual.prefab";
    [MenuItem("Nature Paradise/Narrative/Apply Animated Market Keeper")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before updating the keeper.");
        var shared = AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(Root + "/Player/Animations/Shared/Builder Humanoid Actions.asset");
        var idle = shared?.Find("Idle"); var walk = shared?.Find("Walk");
        if (idle?.clip == null || walk?.clip == null || !idle.clip.isHumanMotion || !walk.clip.isHumanMotion)
            throw new InvalidOperationException("Shared player Humanoid Idle and Walk clips are required.");
        Directory.CreateDirectory(Root + "/Resources/Characters"); AssetDatabase.Refresh();
        var library = AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(ActionsPath);
        if (!library) { library = ScriptableObject.CreateInstance<HumanoidActionLibrary>(); AssetDatabase.CreateAsset(library, ActionsPath); }
        // Update shared locomotion, preserving any merchant action assigned later.
        foreach (var entry in new[] { idle, walk })
        {
            var target = library.Find(entry.id);
            if (target == null) { target = new HumanoidActionLibrary.Action { id = entry.id }; library.actions.Add(target); }
            target.clip = entry.clip; target.loop = true; target.speed = entry.speed;
        }
        if (library.Find("MarketAction") == null) library.actions.Add(new HumanoidActionLibrary.Action { id = "MarketAction", loop = false, speed = 1 });
        library.blendSeconds = shared.blendSeconds; EditorUtility.SetDirty(library);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/mesh/Character/MarketKeeper/MarketKeeper.fbx");
        if (!source) throw new InvalidOperationException("Prepared MarketKeeper FBX is missing.");
        var root = new GameObject("MarketKeeper");
        root.SetActive(false);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "MarketKeeper_Humanoid";
            var animator = model.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Market keeper requires its own valid Humanoid Avatar.");
            // Preview on an active temporary object, then retain a clean bind pose in the prefab.
            root.SetActive(true);
            var bones = model.GetComponentsInChildren<Transform>(true);
            var positions = bones.Select(t => t.localPosition).ToArray(); var rotations = bones.Select(t => t.localRotation).ToArray(); var scales = bones.Select(t => t.localScale).ToArray();
            Bounds bounds;
            using (var preview = new HumanoidActionPlayer(animator, library))
            { for (int i = 0; i < 12; i++) preview.Evaluate(1f / 30); bounds = NPCCharacterSetup.VisibleMeshBounds(model); }
            for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = positions[i]; bones[i].localRotation = rotations[i]; bones[i].localScale = scales[i]; }
            float factor = PlayerDimensions.Height / bounds.size.y;
            model.transform.localScale *= factor;
            model.transform.localPosition -= Vector3.up * bounds.min.y * factor;
            animator.runtimeAnimatorController = null; animator.applyRootMotion = false; animator.fireEvents = false;
            var motion = root.AddComponent<NPCCharacterMotion>(); motion.character = animator; motion.actions = library;
            motion.sourceWalkSpeed = 1.6f;
            var collider = root.AddComponent<CapsuleCollider>(); collider.radius = PlayerDimensions.Radius; collider.height = PlayerDimensions.Height; collider.center = Vector3.up * PlayerDimensions.Height * .5f;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        AttachToCart(); AssetDatabase.SaveAssets();
        Debug.Log("[NPC] Market keeper shares player Humanoid Idle/Walk with the NPC system; MarketAction is reserved for the future custom clip.");
    }
    public static void InstantiateAtAnchor(Transform anchor)
    {
        if (!anchor) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) return;
        foreach (var t in anchor.Cast<Transform>().Where(t => t.name == "MarketKeeper").ToArray()) Object.DestroyImmediate(t.gameObject);
        var keeper = (GameObject)PrefabUtility.InstantiatePrefab(prefab, anchor); keeper.name = "MarketKeeper";
        keeper.transform.localPosition = Vector3.zero; keeper.transform.localRotation = Quaternion.identity; keeper.transform.localScale = Vector3.one;
    }
    static void AttachToCart()
    {
        if (!File.Exists(CartVisual)) return;
        var root = PrefabUtility.LoadPrefabContents(CartVisual);
        try
        {
            var anchor = root.transform.Find("KeeperAnchor");
            if (!anchor) throw new InvalidOperationException("Cart KeeperAnchor is missing.");
            InstantiateAtAnchor(anchor); PrefabUtility.SaveAsPrefabAsset(root, CartVisual);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
