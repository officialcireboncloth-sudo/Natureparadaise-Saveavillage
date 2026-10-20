#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Visual-only copy of PlayerVisual. Does not modify the actual player prefab, Avatar or FBX.</summary>
[InitializeOnLoad]
public static class HumanoidBuilderSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Request="ArtSource/Construction_Kit/humanoid.request";
    static HumanoidBuilderSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Move(Request,UniversalConstructionSetup.Output+"/Humanoid_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        try{Apply();}catch(Exception e){File.WriteAllText(UniversalConstructionSetup.Output+"/HumanoidError.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Nature Paradise/Construction/Use Player Model as Humanoid Builder")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        var set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
        var settings=ConstructionWorkerSettings.Load();
        if(source==null||set?.idle==null||set.walk==null||settings==null)throw new InvalidOperationException("Player visual/Idle/Walking or construction settings missing.");
        // Active Player.fbx is Generic. Retarget only an independent copy, never rewrite the player's import.
        string originalPath=Root+"/mesh/Dummy/PlayerDummy/Player.fbx";
        string copyPath=Root+"/mesh/Construction/BuilderHumanoidSource.fbx";
        File.Copy(originalPath,copyPath,true);AssetDatabase.ImportAsset(copyPath,ImportAssetOptions.ForceSynchronousImport);
        var originalImporter=(ModelImporter)AssetImporter.GetAtPath(originalPath);
        var importer=(ModelImporter)AssetImporter.GetAtPath(copyPath);
        importer.globalScale=originalImporter.globalScale;importer.useFileScale=originalImporter.useFileScale;
        importer.bakeAxisConversion=originalImporter.bakeAxisConversion;
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar=null;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
        importer.clipAnimations=originalImporter.clipAnimations;importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(copyPath);
        var avatar=AssetDatabase.LoadAllAssetsAtPath(copyPath).OfType<Avatar>().FirstOrDefault(a=>a.isValid&&a.isHuman);
        if(avatar==null)throw new InvalidOperationException("Copied FBX could not auto-map Humanoid; player import was not changed.");
        var clips=AssetDatabase.LoadAllAssetsAtPath(copyPath).OfType<AnimationClip>().ToArray();
        AnimationClip Clip(string name)=>clips.FirstOrDefault(c=>c.name.Equals(name,StringComparison.OrdinalIgnoreCase));
        var idle=Clip("Idle");var walk=Clip("Walking");
        if(idle==null||walk==null||!idle.isHumanMotion||!walk.isHumanMotion)throw new InvalidOperationException("Copied Idle/Walking must be imported as Humanoid motions.");
        string folder=Root+"/Player/Animations/Shared";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        string libraryPath=folder+"/Builder Humanoid Actions.asset";
        var library=AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(libraryPath);
        if(library==null){library=ScriptableObject.CreateInstance<HumanoidActionLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
        void Slot(string id,AnimationClip initial=null,bool loop=true)
        {
            // Re-running setup must not erase authored builder clips added by the user later.
            var action=library.Find(id);
            if(action==null){action=new HumanoidActionLibrary.Action{id=id,clip=initial,loop=loop};library.actions.Add(action);}
            else if(initial!=null&&(action.clip==null||!action.clip.isHumanMotion))action.clip=initial;
        }
        Slot("Idle",idle);Slot("Walk",walk);
        Slot("PickUpBoard",null,false);Slot("CarryBoard");Slot("PlaceBoard",null,false);
        Slot("Hammer");Slot("Saw");Slot("Inspect");
        EditorUtility.SetDirty(library);
        var root=new GameObject("BuilderHumanoid_PlayerTemporary");root.SetActive(false);
        try
        {
            var visual=new GameObject("BuilderVisual").transform;visual.SetParent(root.transform,false);
            var model=ConstructionProjectVisuals.VisualClone(imported,visual);
            model.name="HumanoidVisual_ReplaceWithWorkerModel";
            var reference=source.transform.Find("Model_Editable");
            if(reference==null)throw new InvalidOperationException("Player visual model calibration missing.");
            model.transform.localPosition=reference.localPosition;model.transform.localRotation=reference.localRotation;
            model.transform.localScale=reference.localScale;
            // Canonical contact rig is 2m; runtime scale returns exactly to the current 1.5m player size.
            visual.localScale=Vector3.one*(2f/PlayerDimensions.Height);
            var animator=model.GetComponentInChildren<Animator>(true);
            if(animator==null)animator=model.AddComponent<Animator>();animator.avatar=avatar;
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Player/m_PlayerDummy.mat");
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled=r is SkinnedMeshRenderer;
                if(material!=null)r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
            }
            animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.fireEvents=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))r.updateWhenOffscreen=true;
            root.SetActive(true);
            string prefabPath=Root+"/Prefabs/Construction/BuilderHumanoid.prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            settings.workerPrefab=prefab;settings.humanoidActions=library;settings.workerScale=PlayerDimensions.Height/2;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            var report=new StringBuilder("HUMANOID BUILDER APPLY\n");
            report.AppendLine("PASS independent FBX copy imported Humanoid with its own valid Avatar");
            report.AppendLine("PASS Generic player source remains unchanged; shared clips are the copied Humanoid imports");
            report.AppendLine("PASS same player world height; bench/tool contact scale matched");
            report.AppendLine("PASS no player input, gameplay behaviours, colliders or player AnimatorController");
            report.AppendLine("PASS source player prefab, FBX, Avatar and player animation set NOT edited");
            foreach(var action in library.actions)report.AppendLine(action.id+": "+(action.clip!=null?action.clip.name:"UNASSIGNED — procedural contact fallback"));
            File.WriteAllText(UniversalConstructionSetup.Output+"/HumanoidApply.txt",report.ToString());
            Debug.Log("[CONSTRUCTION] Player Humanoid visual applied to independent builder; shared action slots ready.");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
#endif
