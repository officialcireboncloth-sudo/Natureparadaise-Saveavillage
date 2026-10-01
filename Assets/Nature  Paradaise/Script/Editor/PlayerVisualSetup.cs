using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Authoring player dummy dan controller Humanoid. Hanya berjalan lewat menu Editor.</summary>
public static class PlayerVisualSetup
{
    const string SourceFolder="Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy";
    const string PreferredSourceModelPath=SourceFolder+"/Player.fbx";
    const string PreviousSourceModelPath=SourceFolder+"/Player_Farming.fbx";
    const string LegacySourceModelPath=SourceFolder+"/Player_Dummy.fbx";
    const string PreferredBaseTexturePath=SourceFolder+"/Player_Dummy_BaseColor.JPEG";
    const string IdleAnimationPath=SourceFolder+"/Breathing Idle.fbx";
    const string WalkingAnimationPath=SourceFolder+"/Walking.fbx";
    const string RunningAnimationPath=SourceFolder+"/Running.fbx";
    const string PlayerFolder="Assets/Nature  Paradaise/Player";
    const string AnimationFolder=PlayerFolder+"/Animations";
    const string MixamoFolder=AnimationFolder+"/Mixamo";
    const string PrefabFolder="Assets/Nature  Paradaise/Prefabs/Player";
    const string MaterialFolder="Assets/Nature  Paradaise/Material/Player";
    const string AnimationSetPath=AnimationFolder+"/Player Animation Set.asset";
    const string ControllerPath=AnimationFolder+"/Player Locomotion.controller";
    const string MaterialPath=MaterialFolder+"/m_PlayerDummy.mat";
    const string PrefabPath=PrefabFolder+"/PlayerVisual.prefab";
    const float PlayerVisualHeight=3.6f;
    const float PlayerVisualGroundOffset=0f;
    internal static bool IsRunning { get; private set; }

    internal static bool NeedsSourceRefresh()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PreferredSourceModelPath)==null) return false;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null) return true;
        PlayerAnimationSetSO set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(AnimationSetPath);
        if(set==null || set.idle==null || set.walk==null || set.run==null || set.jump==null ||
           set.jumpForward==null || set.pickUpFromFloor==null || set.pickUpWaist==null || set.knockOut==null ||
           set.wakeUpFromKnockOut==null || set.hoeing==null || set.choppingTree==null ||
           set.hammeringRock==null || set.planting==null || set.sickle==null ||
           set.weedPulling==null || set.refillWateringCan==null || set.fishingCast==null ||
           set.fishingIdle==null || set.fishingReel==null || set.holdItem==null ||
           set.placeItem==null || set.brushAnimal==null || set.mountHorse==null ||
           set.dismountHorse==null || set.ridingIdle==null || set.pickUpChicken==null ||
           set.holdTwoHands==null || set.placeChicken==null || set.scoopManure==null ||
           set.shearSheep==null || set.tiredPose==null || set.wakeUpBed==null || set.yawn==null ||
           set.staggerOverlay==null)
            return true;
        if(AssetDatabase.GetAssetPath(set.idle)!=PreferredSourceModelPath) return true;
        if(!AssetDatabase.GetDependencies(PrefabPath).Contains(PreferredSourceModelPath)) return true;
        AvatarMask holdMask=AssetDatabase.LoadAssetAtPath<AvatarMask>(AnimationFolder+"/Player Hold Upper Body.mask");
        if(holdMask==null || holdMask.transformCount==0) return true;
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null || controller.parameters.All(parameter=>parameter.name!="CarryingAnimal") ||
           controller.parameters.All(parameter=>parameter.name!="Riding") ||
           controller.parameters.All(parameter=>parameter.name!="HorseMountMirror") ||
           controller.parameters.All(parameter=>parameter.name!="Fatigue") ||
           controller.layers.All(layer=>layer.name!="Fatigue") ||
           controller.layers.All(layer=>layer.stateMachine.states.All(child=>child.state.name!="Carry Animal Two Hands")))
            return true;
        // Overwrite FBX mempertahankan path dan GUID sehingga dependency saja tidak cukup.
        // Rebuild prefab bila source fisik lebih baru daripada prefab hasil setup.
        return File.GetLastWriteTimeUtc(PreferredSourceModelPath) > File.GetLastWriteTimeUtc(PrefabPath);
    }

    [MenuItem("Nature Paradise/Player/Setup or Update Player Visual",false,100)]
    public static void Setup()
    {
        if(IsRunning) return;
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PLAYER VISUAL] Hentikan Play Mode sebelum setup.");
            return;
        }

        IsRunning=true;
        try
        {
            EnsureFolders();
            string sourceModelPath=FindSourceModelPath();
            ConfigurePlayerModelImporter(sourceModelPath);
            PlayerAnimationSetSO animationSet=GetOrCreateAnimationSet();
            if(sourceModelPath==PreferredSourceModelPath &&
               !PopulateEmbeddedAnimationSet(animationSet,sourceModelPath))
            {
                Debug.LogError("[PLAYER ANIMATION] Player.fbx belum menghasilkan AnimationClip. " +
                    "Pilih Assets > Refresh, tunggu kompilasi/reimport selesai, lalu jalankan setup lagi.");
                return;
            }
            AnimatorController controller=GetOrCreateController(animationSet);
            Material material=GetOrCreateMaterial();
            GameObject prefab=GetOrCreateVisualPrefab(sourceModelPath,controller,material);
            int installed=InstallOnLoadedPlayers(prefab,controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject=prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[PLAYER VISUAL] PlayerVisual siap dari '{sourceModelPath}'. Terpasang pada {installed} Player di scene yang terbuka. Isi clip di '{AnimationSetPath}', lalu jalankan menu ini lagi.",prefab);
        }
        finally { IsRunning=false; }
    }

    [MenuItem("Assets/Nature Paradise/Configure Selected Mixamo FBX",false,2100)]
    static void ConfigureSelectedMixamo()
    {
        int configured=0;
        foreach(Object selected in Selection.objects)
        {
            string path=AssetDatabase.GetAssetPath(selected);
            if(AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;
            importer.animationType=ModelImporterAnimationType.Human;
            importer.importAnimation=true;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar=null;
            importer.SaveAndReimport();
            configured++;
        }
        Debug.Log($"[PLAYER ANIMATION] {configured} FBX diatur Humanoid memakai Avatar dari skeleton FBX sendiri.");
    }

    [MenuItem("Assets/Nature Paradise/Configure Selected Mixamo FBX",true)]
    static bool CanConfigureSelectedMixamo() => Selection.objects.Any(item=>
        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item)) is ModelImporter);

    [MenuItem("Nature Paradise/Player/Apply Walk and Run Animations",false,110)]
    public static void ApplyWalkAndRunAnimations()
    {
        if(IsRunning || EditorApplication.isPlayingOrWillChangePlaymode) return;
        IsRunning=true;
        try
        {
            Avatar avatar=FindPlayerAvatar();
            if(avatar==null)
            {
                Debug.LogWarning("[PLAYER ANIMATION] Avatar player belum tersedia.");
                return;
            }
            ConfigureLocomotionImporter(IdleAnimationPath);
            ConfigureLocomotionImporter(WalkingAnimationPath);
            ConfigureLocomotionImporter(RunningAnimationPath);
            AnimationClip idle=FindAnimationClip(IdleAnimationPath);
            AnimationClip walk=FindAnimationClip(WalkingAnimationPath);
            AnimationClip run=FindAnimationClip(RunningAnimationPath);
            if(idle==null || walk==null || run==null)
            {
                Debug.LogWarning("[PLAYER ANIMATION] Clip Idle, Walking, atau Running belum selesai di-import.");
                return;
            }
            SetClipLooping(idle);
            SetClipLooping(walk);
            SetClipLooping(run);

            PlayerAnimationSetSO set=GetOrCreateAnimationSet();
            set.idle=idle;
            set.walk=walk;
            set.run=run;
            if(set.sprint==null) set.sprint=run;
            EditorUtility.SetDirty(set);
            GetOrCreateController(set);
            AssetDatabase.SaveAssets();
            Debug.Log("[PLAYER ANIMATION] Breathing Idle, Walking, dan Running sudah dipasang. Running dipakai sebagai Sprint sementara.",set);
        }
        finally { IsRunning=false; }
    }

    static void SetClipLooping(AnimationClip clip)
    {
        AnimationClipSettings settings=AnimationUtility.GetAnimationClipSettings(clip);
        if(settings.loopTime && settings.loopBlend) return;
        settings.loopTime=true;
        settings.loopBlend=true;
        settings.loopBlendOrientation=true;
        settings.loopBlendPositionY=true;
        settings.loopBlendPositionXZ=true;
        settings.keepOriginalOrientation=true;
        settings.keepOriginalPositionY=true;
        settings.keepOriginalPositionXZ=true;
        settings.heightFromFeet=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);
    }

    static void ConfigureLocomotionImporter(string path)
    {
        if(AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
        bool importerChanged=importer.animationType!=ModelImporterAnimationType.Human ||
                              importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel ||
                              !importer.importAnimation;
        if(importerChanged)
        {
            importer.animationType=ModelImporterAnimationType.Human;
            // Mixamo "Without Skin" tetap membawa skeleton. Membuat Avatar dari
            // skeleton animasinya sendiri menghindari Invalid copied Avatar Rig Configuration.
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar=null;
            importer.importAnimation=true;
            importer.SaveAndReimport();
            importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null) return;
        }

        // Artifact lama dapat tetap kosong setelah konfigurasi Copy Avatar yang gagal.
        // Paksa satu reimport dengan konfigurasi baru sebelum controller membaca clip.
        if(FindAnimationClip(path)==null)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null) return;
        }

        ModelImporterClipAnimation[] clips=importer.clipAnimations;
        if(clips.Length==0) clips=importer.defaultClipAnimations;
        if(clips.Length>0)
        {
            bool loopChanged=false;
            foreach(ModelImporterClipAnimation clip in clips)
            {
                loopChanged|=!clip.loopTime || !clip.loopPose;
                clip.loopTime=true;
                clip.loopPose=true;
            }
            if(loopChanged || importer.clipAnimations.Length==0)
            {
                importer.clipAnimations=clips;
                importer.SaveAndReimport();
            }
        }
    }

    static AnimationClip FindAnimationClip(string path)
    {
        string preferred=Path.GetFileNameWithoutExtension(path);
        AnimationClip[] clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(clip=>!clip.name.StartsWith("__preview__"))
            .ToArray();
        return clips.FirstOrDefault(clip=>clip.name.Equals(preferred,System.StringComparison.OrdinalIgnoreCase))
               ??clips.FirstOrDefault();
    }

    static PlayerAnimationSetSO GetOrCreateAnimationSet()
    {
        PlayerAnimationSetSO result=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(AnimationSetPath);
        if(result!=null) return result;
        result=ScriptableObject.CreateInstance<PlayerAnimationSetSO>();
        AssetDatabase.CreateAsset(result,AnimationSetPath);
        return result;
    }

    static bool PopulateEmbeddedAnimationSet(PlayerAnimationSetSO set,string path)
    {
        AnimationClip[] clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(clip=>!clip.name.StartsWith("__preview__"))
            .ToArray();
        if(clips.Length==0) return false;
        AnimationClip Clip(params string[] names)=>clips.FirstOrDefault(clip=>names.Any(name=>
            clip.name.Equals(name,System.StringComparison.OrdinalIgnoreCase)));

        set.idle=Clip("Idle");
        set.walk=Clip("Walking");
        set.run=Clip("Running");
        set.sprint=set.run;
        set.jump=Clip("Jumping");
        set.jumpForward=Clip("Jumping Forward","JumpingForward");
        set.pickUpFromFloor=Clip("Pick Up From Floor","PickUpFromFloor");
        set.pickUpWaist=Clip("Pick Up Waist","PickUpWaist");
        set.knockOut=Clip("Knock Out","KnockOut");
        set.wakeUpFromKnockOut=Clip("Wake Up From Knock Out","WakeUpFromKnockOut");
        set.milkingAnimal=Clip("Milking Animal","MilkingAnimal");
        set.pushingObject=Clip("Pushing Object","PushingObject");
        set.wateringPlant=Clip("Watering Plant","WateringPlant");
        set.hoeing=Clip("Hoeing");
        set.choppingTree=Clip("Chopping Tree");
        set.hammeringRock=Clip("Hammering Rock");
        set.planting=Clip("Planting");
        set.sickle=Clip("Sickle");
        set.weedPulling=Clip("Weed Pulling");
        set.refillWateringCan=Clip("Refill Watering Can");
        set.handOverOneHand=Clip("Hand Over One Hand");
        set.handOverTwoHands=Clip("Hand Over Two Hands");
        set.fishingCast=Clip("Fishing Cast");
        set.fishingIdle=Clip("Fishing Idle");
        set.fishingReel=Clip("Fishing Reel");
        set.holdItem=Clip("Hold Item");
        set.placeItem=Clip("Place Item");
        set.brushAnimal=Clip("Brush Animal");
        set.mountHorse=Clip("Mount Horse");
        set.dismountHorse=Clip("Dismount Horse");
        set.ridingIdle=Clip("Riding Idle");
        set.pickUpChicken=Clip("Pick Up Chicken");
        set.holdTwoHands=Clip("Hold Two Hands");
        set.placeChicken=Clip("Place Chicken");
        set.scoopManure=Clip("Scoop Manure");
        set.shearSheep=Clip("Shear Sheep");
        set.tiredPose=Clip("Tired Pose");
        set.wakeUpBed=Clip("Wake Up Bed");
        set.yawn=Clip("Yawn");
        set.staggerOverlay=Clip("Stagger Overlay");
        EditorUtility.SetDirty(set);
        return set.idle!=null && set.walk!=null && set.run!=null && set.jump!=null &&
               set.jumpForward!=null;
    }

    static AnimatorController GetOrCreateController(PlayerAnimationSetSO clips)
    {
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameter(controller,"Speed",AnimatorControllerParameterType.Float);
            AddParameter(controller,"Grounded",AnimatorControllerParameterType.Bool);
            AddParameter(controller,"Sprint",AnimatorControllerParameterType.Bool);
            AddParameter(controller,"Carry",AnimatorControllerParameterType.Bool);
            AddParameter(controller,"Jump",AnimatorControllerParameterType.Trigger);
            AddParameter(controller,"UseTool",AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine=controller.layers[0].stateMachine;
            AnimatorState locomotion=machine.AddState("Locomotion");
            machine.defaultState=locomotion;
            BlendTree tree=new() {name="Player Locomotion Blend",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller);
            locomotion.motion=tree;

            AnimatorState jump=machine.AddState("Jump");
            AnimatorStateTransition toJump=machine.AddAnyStateTransition(jump);
            toJump.hasExitTime=false;
            toJump.duration=0.08f;
            toJump.AddCondition(AnimatorConditionMode.If,0f,"Jump");
            AnimatorStateTransition fromJump=jump.AddTransition(locomotion);
            fromJump.hasExitTime=true;
            fromJump.exitTime=0.9f;
            fromJump.duration=0.1f;

            AnimatorState tool=machine.AddState("Use Tool");
            AnimatorStateTransition toTool=machine.AddAnyStateTransition(tool);
            toTool.hasExitTime=false;
            toTool.duration=0.05f;
            toTool.AddCondition(AnimatorConditionMode.If,0f,"UseTool");
            AnimatorStateTransition fromTool=tool.AddTransition(locomotion);
            fromTool.hasExitTime=true;
            fromTool.exitTime=0.95f;
            fromTool.duration=0.08f;
        }

        AddParameter(controller,"Speed",AnimatorControllerParameterType.Float);
        AddParameter(controller,"Grounded",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"Sprint",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"Carry",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"Jump",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"JumpForward",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"PickUp",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"PickUpWaist",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"KnockOut",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"WakeUp",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Milking",AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller,"Pushing",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"Watering",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Hoeing",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Axe",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Hammer",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Planting",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Sickle",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Pull",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"RefillWateringCan",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"HandOverOneHand",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"HandOverTwoHands",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Cast",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Hook",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Catch",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"FishingActive",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"HoldingItem",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"CarryingAnimal",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"PlaceItem",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"BrushAnimal",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"MountHorse",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"DismountHorse",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Riding",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"HorseMountMirror",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"PickUpChicken",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"PlaceChicken",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"WakeUpBed",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Yawn",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"ShearSheep",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Tired",AnimatorControllerParameterType.Bool);
        AddFloatParameter(controller,"Fatigue",0f);
        AddFloatParameter(controller,"FatigueLocomotionSpeed",1f);
        AddParameter(controller,"UseTool",AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine=controller.layers[0].stateMachine;
        AnimatorState locomotionState=FindState(stateMachine,"Locomotion");
        if(locomotionState?.motion is BlendTree locomotionTree)
        {
            locomotionTree.children=new[]
            {
                Child(clips.idle,0f), Child(clips.walk,0.35f),
                Child(clips.run,0.65f), Child(clips.sprint,1f)
            };
            EditorUtility.SetDirty(locomotionTree);
        }
        if(locomotionState!=null)
        {
            locomotionState.speedParameterActive=true;
            locomotionState.speedParameter="FatigueLocomotionSpeed";
        }
        AnimatorState jumpState=FindState(stateMachine,"Jump");
        AnimatorState toolState=FindState(stateMachine,"Use Tool");
        if(jumpState!=null)
        {
            jumpState.motion=clips.jump;
            // Standing Jump 57 frame memuat anticipation dan landing. Pada 2x,
            // takeoff/landingnya selaras dengan arc CharacterController (~0,6 detik).
            jumpState.speed=2f;
        }
        if(toolState!=null) toolState.motion=clips.useTool;
        // 30 frame / 30 fps pada 1.25x memberi durasi sekitar 0,8 detik,
        // dekat dengan arc fisik 0,61 detik ditambah waktu blend keluar.
        EnsureActionState(stateMachine,"Jump Forward","JumpForward",clips.jumpForward,1.25f,true);
        EnsureActionState(stateMachine,"Pick Up From Floor","PickUp",clips.pickUpFromFloor,6.5f,true);
        // 30 frame pada 1.25x menghasilkan action sekitar 0,8 detik.
        EnsureActionState(stateMachine,"Pick Up Waist","PickUpWaist",clips.pickUpWaist,1.25f,true);
        EnsureActionState(stateMachine,"Knock Out","KnockOut",clips.knockOut,1f,false);
        EnsureActionState(stateMachine,"Wake Up","WakeUp",clips.wakeUpFromKnockOut,4f,true);
        EnsureActionState(stateMachine,"Milking Animal","Milking",clips.milkingAnimal,2f,true);
        EnsurePushingState(stateMachine,clips.pushingObject);
        EnsureActionState(stateMachine,"Watering Plant","Watering",clips.wateringPlant,2f,true);
        EnsureActionState(stateMachine,"Hoeing","Hoeing",clips.hoeing,1f,true);
        EnsureActionState(stateMachine,"Chopping Tree","Axe",clips.choppingTree,1f,true);
        EnsureActionState(stateMachine,"Hammering Rock","Hammer",clips.hammeringRock,1f,true);
        EnsureActionState(stateMachine,"Planting","Planting",clips.planting,1f,true);
        EnsureActionState(stateMachine,"Sickle","Sickle",clips.sickle,1f,true);
        EnsureActionState(stateMachine,"Weed Pulling","Pull",clips.weedPulling,1f,true);
        EnsureActionState(stateMachine,"Refill Watering Can","RefillWateringCan",clips.refillWateringCan,1.5f,true);
        EnsureActionState(stateMachine,"Hand Over One Hand","HandOverOneHand",clips.handOverOneHand,1f,true);
        EnsureActionState(stateMachine,"Hand Over Two Hands","HandOverTwoHands",clips.handOverTwoHands,1f,true);
        EnsureActionState(stateMachine,"Place Item","PlaceItem",clips.placeItem,1f,true);
        AddParameter(controller,"BrushingActive",AnimatorControllerParameterType.Bool);
        AnimatorState brush=EnsureActionState(stateMachine,"Brush Animal","BrushAnimal",clips.brushAnimal,1f,true);
        foreach(var exit in brush.transitions)
        {
            exit.hasExitTime=false;
            exit.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="BrushingActive"}};
        }
        AnimatorState mountHorse=EnsureActionState(stateMachine,"Mount Horse","MountHorse",clips.mountHorse,2f,true);
        AnimatorState dismountHorse=EnsureActionState(stateMachine,"Dismount Horse","DismountHorse",clips.dismountHorse,2f,true);
        ConfigureHorseSideMirror(mountHorse);
        ConfigureHorseSideMirror(dismountHorse);
        EnsureActionState(stateMachine,"Pick Up Chicken","PickUpChicken",clips.pickUpChicken,1.25f,true);
        EnsureActionState(stateMachine,"Place Chicken","PlaceChicken",clips.placeChicken,1.25f,true);
        EnsureActionState(stateMachine,"Wake Up Bed","WakeUpBed",clips.wakeUpBed,2f,true);
        EnsureActionState(stateMachine,"Yawn","Yawn",clips.yawn,1.5f,true);
        AddParameter(controller,"ShearingActive",AnimatorControllerParameterType.Bool);
        AddParameter(controller,"ScoopManure",AnimatorControllerParameterType.Trigger);
        EnsureActionState(stateMachine,"Scoop Manure","ScoopManure",clips.scoopManure,2f,true);
        AnimatorState shear=EnsureActionState(stateMachine,"Shear Sheep","ShearSheep",clips.shearSheep,1f,true);
        foreach(var exit in shear.transitions)
        {
            exit.hasExitTime=false;
            exit.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="ShearingActive"}};
        }
        EnsureRidingState(stateMachine,clips.ridingIdle);
        EnsureTiredState(stateMachine,clips.tiredPose);
        EnsureHoldItemLayer(controller,clips.holdItem,clips.holdTwoHands);
        EnsureFatigueLayer(controller,clips.staggerOverlay);
        EnsureFishingStates(stateMachine,clips);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void ConfigureHorseSideMirror(AnimatorState state)
    {
        if(state==null) return;
        state.mirrorParameterActive=true;
        state.mirrorParameter="HorseMountMirror";
        EditorUtility.SetDirty(state);
    }

    static void EnsureRidingState(AnimatorStateMachine machine,AnimationClip clip)
    {
        AnimatorState riding=FindState(machine,"Riding Idle")??machine.AddState("Riding Idle");
        riding.motion=clip;
        riding.speed=1f;
        AnimatorStateTransition enter=machine.anyStateTransitions.FirstOrDefault(transition=>
            transition.destinationState==riding && transition.conditions.Any(condition=>condition.parameter=="Riding"));
        if(enter==null)
        {
            enter=machine.AddAnyStateTransition(riding);
            enter.hasExitTime=false;
            enter.duration=0.08f;
            enter.AddCondition(AnimatorConditionMode.If,0f,"Riding");
        }
        enter.canTransitionToSelf=false;
        AnimatorState locomotion=FindState(machine,"Locomotion");
        if(locomotion!=null && !riding.transitions.Any(transition=>transition.destinationState==locomotion &&
           transition.conditions.Any(condition=>condition.parameter=="Riding")))
        {
            AnimatorStateTransition exit=riding.AddTransition(locomotion);
            exit.hasExitTime=false;
            exit.duration=0.08f;
            exit.AddCondition(AnimatorConditionMode.IfNot,0f,"Riding");
        }
    }

    static void EnsureTiredState(AnimatorStateMachine machine,AnimationClip clip)
    {
        AnimatorState tired=FindState(machine,"Tired Pose")??machine.AddState("Tired Pose");
        tired.motion=clip;
        tired.speed=1f;
        AnimatorStateTransition enter=machine.anyStateTransitions.FirstOrDefault(transition=>
            transition.destinationState==tired && transition.conditions.Any(condition=>condition.parameter=="Tired"));
        if(enter==null)
        {
            enter=machine.AddAnyStateTransition(tired);
            enter.hasExitTime=false;
            enter.duration=0.12f;
            enter.AddCondition(AnimatorConditionMode.If,0f,"Tired");
        }
        enter.canTransitionToSelf=false;
        AnimatorState locomotion=FindState(machine,"Locomotion");
        if(locomotion!=null && !tired.transitions.Any(transition=>transition.destinationState==locomotion &&
           transition.conditions.Any(condition=>condition.parameter=="Tired")))
        {
            AnimatorStateTransition exit=tired.AddTransition(locomotion);
            exit.hasExitTime=false;
            exit.duration=0.1f;
            exit.AddCondition(AnimatorConditionMode.IfNot,0f,"Tired");
        }
    }

    static AnimatorState EnsureActionState(AnimatorStateMachine machine,string stateName,string trigger,
        AnimationClip clip,float speed,bool returnsToLocomotion)
    {
        AnimatorState state=FindState(machine,stateName)??machine.AddState(stateName);
        state.motion=clip;
        state.speed=speed;
        if(!machine.anyStateTransitions.Any(transition=>transition.destinationState==state &&
            transition.conditions.Any(condition=>condition.parameter==trigger)))
        {
            AnimatorStateTransition enter=machine.AddAnyStateTransition(state);
            enter.hasExitTime=false;
            enter.duration=0.06f;
            enter.AddCondition(AnimatorConditionMode.If,0f,trigger);
        }
        AnimatorState locomotion=FindState(machine,"Locomotion");
        if(returnsToLocomotion && locomotion!=null &&
           !state.transitions.Any(transition=>transition.destinationState==locomotion))
        {
            AnimatorStateTransition exit=state.AddTransition(locomotion);
            exit.hasExitTime=true;
            exit.exitTime=0.95f;
            exit.duration=0.08f;
        }
        return state;
    }

    static void EnsureFishingStates(AnimatorStateMachine machine,PlayerAnimationSetSO clips)
    {
        AnimatorState locomotion=FindState(machine,"Locomotion");
        AnimatorState cast=FindState(machine,"Fishing Cast")??machine.AddState("Fishing Cast");
        AnimatorState idle=FindState(machine,"Fishing Idle")??machine.AddState("Fishing Idle");
        AnimatorState reel=FindState(machine,"Fishing Reel")??machine.AddState("Fishing Reel");
        cast.motion=clips.fishingCast;
        idle.motion=clips.fishingIdle;
        reel.motion=clips.fishingReel;

        EnsureAnyTrigger(machine,cast,"Cast");
        EnsureAnyTrigger(machine,reel,"Hook");
        EnsureAnyTrigger(machine,reel,"Catch");
        EnsureExitTransition(cast,idle,true,null);
        EnsureExitTransition(reel,idle,true,null);
        EnsureExitTransition(idle,locomotion,false,"FishingActive");
    }

    static void EnsureHoldItemLayer(AnimatorController controller,AnimationClip clip,AnimationClip animalClip)
    {
        const string layerName="Held Item Upper Body";
        const string maskPath=AnimationFolder+"/Player Hold Upper Body.mask";
        AvatarMask mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if(mask==null)
        {
            mask=new AvatarMask();
            AssetDatabase.CreateAsset(mask,maskPath);
        }
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers,true);
        ConfigureGenericUpperBodyMask(mask);
        EditorUtility.SetDirty(mask);

        AnimatorControllerLayer[] layers=controller.layers;
        int index=System.Array.FindIndex(layers,layer=>layer.name==layerName);
        if(index<0)
        {
            AnimatorControllerLayer created=new()
            {
                name=layerName,
                defaultWeight=1f,
                blendingMode=AnimatorLayerBlendingMode.Override,
                avatarMask=mask,
                stateMachine=new AnimatorStateMachine {name=layerName}
            };
            AssetDatabase.AddObjectToAsset(created.stateMachine,controller);
            controller.AddLayer(created);
            layers=controller.layers;
            index=System.Array.FindIndex(layers,layer=>layer.name==layerName);
        }
        AnimatorControllerLayer heldLayer=layers[index];
        heldLayer.defaultWeight=1f;
        heldLayer.avatarMask=mask;
        AnimatorStateMachine machine=heldLayer.stateMachine;
        AnimatorState empty=FindState(machine,"Empty")??machine.AddState("Empty");
        AnimatorState hold=FindState(machine,"Hold Item")??machine.AddState("Hold Item");
        AnimatorState animalHold=FindState(machine,"Carry Animal Two Hands")??machine.AddState("Carry Animal Two Hands");
        machine.defaultState=empty;
        hold.motion=clip;
        hold.speed=1f;
        animalHold.motion=animalClip;
        animalHold.speed=1f;
        if(!empty.transitions.Any(t=>t.destinationState==hold))
        {
            AnimatorStateTransition enter=empty.AddTransition(hold);
            enter.hasExitTime=false;
            enter.duration=0.12f;
            enter.AddCondition(AnimatorConditionMode.If,0f,"HoldingItem");
        }
        if(!hold.transitions.Any(t=>t.destinationState==empty))
        {
            AnimatorStateTransition exit=hold.AddTransition(empty);
            exit.hasExitTime=false;
            exit.duration=0.08f;
            exit.AddCondition(AnimatorConditionMode.IfNot,0f,"HoldingItem");
        }
        if(!hold.transitions.Any(t=>t.destinationState==animalHold))
        {
            AnimatorStateTransition holdToAnimal=hold.AddTransition(animalHold);
            holdToAnimal.hasExitTime=false;
            holdToAnimal.duration=0.08f;
            holdToAnimal.AddCondition(AnimatorConditionMode.If,0f,"CarryingAnimal");
        }
        if(!empty.transitions.Any(t=>t.destinationState==animalHold))
        {
            AnimatorStateTransition enterAnimal=empty.AddTransition(animalHold);
            enterAnimal.hasExitTime=false;
            enterAnimal.duration=0.08f;
            enterAnimal.AddCondition(AnimatorConditionMode.If,0f,"CarryingAnimal");
        }
        if(!animalHold.transitions.Any(t=>t.destinationState==empty))
        {
            AnimatorStateTransition exitAnimal=animalHold.AddTransition(empty);
            exitAnimal.hasExitTime=false;
            exitAnimal.duration=0.1f;
            exitAnimal.AddCondition(AnimatorConditionMode.IfNot,0f,"CarryingAnimal");
        }
        layers[index]=heldLayer;
        controller.layers=layers;
        EditorUtility.SetDirty(machine);
    }

    static void ConfigureGenericUpperBodyMask(AvatarMask mask)
    {
        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(PreferredSourceModelPath);
        if(source==null) return;

        // Player.fbx memakai Generic rig agar semua multi-take Blender tetap terbaca.
        // Karena itu mask humanoid di atas dilengkapi daftar transform eksplisit.
        mask.transformCount=0;
        mask.AddTransformPath(source.transform,true);
        for(int i=0;i<mask.transformCount;i++)
        {
            string path=mask.GetTransformPath(i);
            int separator=path.LastIndexOf('/');
            string bone=separator>=0?path.Substring(separator+1):path;
            bool upper=bone.StartsWith("mixamorig:Spine",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:Neck",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:Head",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:LeftShoulder",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:RightShoulder",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:LeftArm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:RightArm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:LeftForeArm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:RightForeArm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:LeftHand",System.StringComparison.Ordinal) ||
                       bone.StartsWith("mixamorig:RightHand",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Spine",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Neck",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Head",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Shoulder",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Arm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_ForeArm",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Ctrl_Hand",System.StringComparison.Ordinal) ||
                       bone.StartsWith("Arm_IK",System.StringComparison.Ordinal) ||
                       bone.StartsWith("ForeArm_IK",System.StringComparison.Ordinal);
            mask.SetTransformActive(i,upper);
        }
    }

    static void EnsureFatigueLayer(AnimatorController controller,AnimationClip clip)
    {
        if(clip==null) return;
        const string layerName="Fatigue";
        const string maskPath=AnimationFolder+"/Player Fatigue Upper Body.mask";
        AvatarMask mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if(mask==null)
        {
            mask=new AvatarMask();
            AssetDatabase.CreateAsset(mask,maskPath);
        }
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm,true);
        ConfigureGenericFatigueMask(mask);
        EditorUtility.SetDirty(mask);

        AnimatorControllerLayer[] layers=controller.layers;
        int index=System.Array.FindIndex(layers,layer=>layer.name==layerName);
        if(index<0)
        {
            AnimatorControllerLayer created=new()
            {
                name=layerName,
                defaultWeight=0f,
                blendingMode=AnimatorLayerBlendingMode.Additive,
                avatarMask=mask,
                stateMachine=new AnimatorStateMachine {name=layerName}
            };
            AssetDatabase.AddObjectToAsset(created.stateMachine,controller);
            controller.AddLayer(created);
            layers=controller.layers;
            index=System.Array.FindIndex(layers,layer=>layer.name==layerName);
        }
        AnimatorControllerLayer fatigueLayer=layers[index];
        fatigueLayer.defaultWeight=0f;
        fatigueLayer.blendingMode=AnimatorLayerBlendingMode.Additive;
        fatigueLayer.avatarMask=mask;
        AnimatorStateMachine machine=fatigueLayer.stateMachine;
        AnimatorState stagger=FindState(machine,"Stagger Overlay")??machine.AddState("Stagger Overlay");
        stagger.motion=clip;
        stagger.speed=1f;
        machine.defaultState=stagger;
        layers[index]=fatigueLayer;
        controller.layers=layers;
        EditorUtility.SetDirty(machine);
    }

    static void EnsurePushingState(AnimatorStateMachine machine,AnimationClip clip)
    {
        AnimatorState locomotion=FindState(machine,"Locomotion");
        AnimatorState pushing=FindState(machine,"Pushing Object")??machine.AddState("Pushing Object");
        pushing.motion=clip;
        pushing.speed=1f;
        foreach(AnimatorStateTransition transition in machine.anyStateTransitions
                    .Where(item=>item.destinationState==pushing).ToArray())
            machine.RemoveAnyStateTransition(transition);
        foreach(AnimatorStateTransition transition in pushing.transitions.ToArray())
            pushing.RemoveTransition(transition);
        AnimatorStateTransition enter=machine.AddAnyStateTransition(pushing);
        enter.hasExitTime=false;
        enter.duration=0.08f;
        enter.canTransitionToSelf=false;
        enter.AddCondition(AnimatorConditionMode.If,0f,"Pushing");
        if(locomotion!=null)
        {
            AnimatorStateTransition exit=pushing.AddTransition(locomotion);
            exit.hasExitTime=false;
            exit.duration=0.1f;
            exit.AddCondition(AnimatorConditionMode.IfNot,0f,"Pushing");
        }
    }

    static void ConfigureGenericFatigueMask(AvatarMask mask)
    {
        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(PreferredSourceModelPath);
        if(source==null) return;
        mask.transformCount=0;
        mask.AddTransformPath(source.transform,true);
        for(int i=0;i<mask.transformCount;i++)
        {
            string path=mask.GetTransformPath(i);
            int separator=path.LastIndexOf('/');
            string bone=separator>=0?path.Substring(separator+1):path;
            bool torsoOrArm=bone.StartsWith("mixamorig:Spine",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:Neck",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:Head",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:LeftShoulder",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:RightShoulder",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:LeftArm",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:RightArm",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:LeftForeArm",System.StringComparison.Ordinal) ||
                            bone.StartsWith("mixamorig:RightForeArm",System.StringComparison.Ordinal) ||
                            bone.Equals("mixamorig:LeftHand",System.StringComparison.Ordinal) ||
                            bone.Equals("mixamorig:RightHand",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_Spine",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_Neck",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_Head",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_Shoulder",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_Arm_FK",System.StringComparison.Ordinal) ||
                            bone.StartsWith("Ctrl_ForeArm_FK",System.StringComparison.Ordinal);
            mask.SetTransformActive(i,torsoOrArm);
        }
    }

    static void EnsureAnyTrigger(AnimatorStateMachine machine,AnimatorState state,string trigger)
    {
        if(machine.anyStateTransitions.Any(transition=>transition.destinationState==state &&
           transition.conditions.Any(condition=>condition.parameter==trigger))) return;
        AnimatorStateTransition transition=machine.AddAnyStateTransition(state);
        transition.hasExitTime=false;
        transition.duration=0.06f;
        transition.AddCondition(AnimatorConditionMode.If,0f,trigger);
    }

    static void EnsureExitTransition(AnimatorState source,AnimatorState destination,bool exitTime,string falseBool)
    {
        if(source==null || destination==null || source.transitions.Any(transition=>
           transition.destinationState==destination && (string.IsNullOrEmpty(falseBool) ||
           transition.conditions.Any(condition=>condition.parameter==falseBool)))) return;
        AnimatorStateTransition transition=source.AddTransition(destination);
        transition.hasExitTime=exitTime;
        transition.exitTime=0.95f;
        transition.duration=0.08f;
        if(!string.IsNullOrEmpty(falseBool))
            transition.AddCondition(AnimatorConditionMode.IfNot,0f,falseBool);
    }

    static ChildMotion Child(AnimationClip clip,float threshold) => new() {motion=clip,threshold=threshold,timeScale=1f};

    static AnimatorState FindState(AnimatorStateMachine machine,string name) =>
        machine.states.Select(item=>item.state).FirstOrDefault(state=>state.name==name);

    static void AddParameter(AnimatorController controller,string name,AnimatorControllerParameterType type)
    {
        if(controller.parameters.All(parameter=>parameter.name!=name)) controller.AddParameter(name,type);
    }

    static void EnsureParameter(AnimatorController controller,string name,AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter[] parameters=controller.parameters;
        int index=System.Array.FindIndex(parameters,item=>item.name==name);
        if(index>=0 && parameters[index].type==type) return;
        if(index>=0) controller.RemoveParameter(index);
        controller.AddParameter(name,type);
    }

    static void AddFloatParameter(AnimatorController controller,string name,float defaultValue)
    {
        AnimatorControllerParameter[] parameters=controller.parameters;
        int index=System.Array.FindIndex(parameters,item=>item.name==name);
        if(index>=0 && parameters[index].type==AnimatorControllerParameterType.Float &&
           Mathf.Approximately(parameters[index].defaultFloat,defaultValue)) return;
        if(index>=0) controller.RemoveParameter(index);
        controller.AddParameter(new AnimatorControllerParameter
        {
            name=name,
            type=AnimatorControllerParameterType.Float,
            defaultFloat=defaultValue
        });
    }

    static Material GetOrCreateMaterial()
    {
        Material material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null)
        {
            Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            material=new Material(shader) {name="m_PlayerDummy"};
            AssetDatabase.CreateAsset(material,MaterialPath);
        }
        Texture2D baseMap=AssetDatabase.LoadAssetAtPath<Texture2D>(PreferredBaseTexturePath);
        if(material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",baseMap);
        if(material.HasProperty("_MainTex")) material.SetTexture("_MainTex",baseMap);
        if(material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap",null);
        if(material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap",null);
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness",0.28f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static string FindSourceModelPath()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PreferredSourceModelPath)!=null)
            return PreferredSourceModelPath;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PreviousSourceModelPath)!=null)
            return PreviousSourceModelPath;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(LegacySourceModelPath)!=null)
            return LegacySourceModelPath;
        string guid=AssetDatabase.FindAssets("t:Model",new[]{SourceFolder})
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(path=>path.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase) ||
                                  path.EndsWith(".obj",System.StringComparison.OrdinalIgnoreCase));
        if(string.IsNullOrWhiteSpace(guid))
            throw new FileNotFoundException("Model Player Dummy tidak ditemukan di folder.",SourceFolder);
        return guid;
    }

    static void ConfigurePlayerModelImporter(string path)
    {
        if(AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
        // Player.fbx berisi mesh, skeleton, control rig, dan semua take dalam satu file.
        // Generic mempertahankan kurva pada skeleton aslinya; Humanoid dapat menerima
        // nama take tetapi menghasilkan nol AnimationClip pada export Blender ini.
        ModelImporterAnimationType targetType=path==PreferredSourceModelPath
            ? ModelImporterAnimationType.Generic
            : ModelImporterAnimationType.Human;
        bool dirty=importer.animationType!=targetType ||
                    importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel || !importer.importAnimation;
        importer.animationType=targetType;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        if(dirty) importer.SaveAndReimport();
        if(path!=PreferredSourceModelPath) return;

        importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer==null) return;
        ModelImporterClipAnimation[] sourceClips=importer.defaultClipAnimations;
        // Beberapa export multi-take FBX kehilangan defaultClipAnimations ketika file
        // ditimpa pada GUID yang sama. Rekonstruksi take yang sudah diverifikasi agar
        // controller tidak tersimpan dengan semua Motion = None.
        if(sourceClips.Length==0)
        {
            sourceClips=new[]
            {
                EmbeddedClip("Idle","Armature|Idle",298f),
                EmbeddedClip("Jumping","Armature|Jumping ",57f),
                EmbeddedClip("JumpingForward","Armature|JumpingForward",30f),
                EmbeddedClip("KnockOut","Armature|KnockOut",78f),
                EmbeddedClip("MilkingAnimal","Armature|MilkingAnimal",136f),
                EmbeddedClip("PickUpFromFloor","Armature|PickUpFromFloor",287f),
                EmbeddedClip("PushingObject","Armature|PushingObject",51f),
                EmbeddedClip("Running","Armature|Running",19f),
                EmbeddedClip("WakeUpFromKnockOut","Armature|WakeUpFromKnockOut",342f),
                EmbeddedClip("Walking","Armature|Walking",30f),
                EmbeddedClip("WateringPlant","Armature|WateringPlant",84f),
                EmbeddedClip("Hoeing","Armature|Hoeing",38f),
                EmbeddedClip("Chopping Tree","Armature|Chopping Tree",30f),
                EmbeddedClip("Hammering Rock","Armature|Hammering Rock",30f),
                EmbeddedClip("Planting","Armature|Planting",30f),
                EmbeddedClip("Sickle","Armature|Sickle",45f),
                EmbeddedClip("Weed Pulling","Armature|Weed Pulling",45f),
                EmbeddedClip("Refill Watering Can","Armature|Refill Watering Can",96f),
                EmbeddedClip("Hand Over One Hand","Armature|Hand Over One Hand",33f),
                EmbeddedClip("Hand Over Two Hands","Armature|Hand Over Two Hands",36f),
                EmbeddedClip("Fishing Cast","Armature|Fishing Cast",48f),
                EmbeddedClip("Fishing Idle","Armature|Fishing Idle",90f),
                EmbeddedClip("Fishing Reel","Armature|Fishing Reel",75f),
                EmbeddedClip("Hold Item","Armature|Hold Item",60f),
                EmbeddedClip("Place Item","Armature|Place Item",36f),
                EmbeddedClip("Brush Animal","Armature|Brush Animal",45f),
                EmbeddedClip("Dismount Horse","Armature|Dismount Horse",90f),
                EmbeddedClip("Hold Two Hands","Armature|Hold Two Hands",60f),
                EmbeddedClip("Mount Horse","Armature|Mount Horse",75f),
                EmbeddedClip("Pick Up Chicken","Armature|Pick Up Chicken",45f),
                EmbeddedClip("Pick Up Waist","Armature|Player | Pick Up Waist",30f),
                EmbeddedClip("Place Chicken","Armature|Place Chicken",45f),
                EmbeddedClip("Riding Idle","Armature|Riding Idle",60f),
                EmbeddedClip("Scoop Manure","Armature|Scoop Manure",60f),
                EmbeddedClip("Shear Sheep","Armature|Shear Sheep",60f),
                EmbeddedClip("Tired Pose","Armature|Tired Pose",90f),
                EmbeddedClip("Wake Up Bed","Armature|Wake Up Bed",120f),
                EmbeddedClip("Yawn","Armature|Yawn",75f),
                EmbeddedClip("Stagger Overlay","Armature|Player | Stagger Overlay",91f)
            };
        }
        ModelImporterClipAnimation[] configured=sourceClips.Select(source=>
        {
            ModelImporterClipAnimation clip=source;
            string cleanName=source.name;
            int separator=cleanName.LastIndexOf('|');
            if(separator>=0) cleanName=cleanName.Substring(separator+1);
            cleanName=cleanName.Trim();
            clip.name=cleanName;
            bool loop=cleanName=="Idle" || cleanName=="Walking" || cleanName=="Running" ||
                      cleanName=="Fishing Idle" || cleanName=="Hold Item" ||
                      cleanName=="Hold Two Hands" || cleanName=="Riding Idle" || cleanName=="Tired Pose" ||
                      cleanName=="Stagger Overlay" || cleanName=="Pushing Object" || cleanName=="Shear Sheep" || cleanName=="Brush Animal";
            if(cleanName=="Stagger Overlay")
            {
                clip.firstFrame=1f;
                clip.lastFrame=91f;
                clip.hasAdditiveReferencePose=true;
                clip.additiveReferencePoseFrame=0f;
            }
            clip.loopTime=loop;
            clip.loopPose=loop;
            clip.keepOriginalOrientation=true;
            clip.keepOriginalPositionY=true;
            clip.keepOriginalPositionXZ=true;
            clip.lockRootRotation=true;
            clip.lockRootHeightY=true;
            clip.lockRootPositionXZ=true;
            return clip;
        }).ToArray();
        bool hasImportedClips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Any(clip=>!clip.name.StartsWith("__preview__"));
        bool needsClipUpdate=!hasImportedClips || importer.clipAnimations.Length!=configured.Length ||
            importer.clipAnimations.Where((clip,index)=>index<configured.Length &&
                (clip.name!=configured[index].name || clip.loopTime!=configured[index].loopTime ||
                 clip.firstFrame!=configured[index].firstFrame || clip.lastFrame!=configured[index].lastFrame ||
                 clip.hasAdditiveReferencePose!=configured[index].hasAdditiveReferencePose ||
                 clip.additiveReferencePoseFrame!=configured[index].additiveReferencePoseFrame)).Any();
        if(needsClipUpdate)
        {
            importer.clipAnimations=configured;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }
    }

    static ModelImporterClipAnimation EmbeddedClip(string name,string takeName,float lastFrame) => new()
    {
        name=name,
        takeName=takeName,
        firstFrame=0f,
        lastFrame=lastFrame
    };

    static GameObject GetOrCreateVisualPrefab(string sourceModelPath,AnimatorController controller,Material material)
    {
        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
        if(source==null) throw new FileNotFoundException("Model Player Dummy tidak dapat dimuat.",sourceModelPath);
        GameObject existing=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(existing!=null)
        {
            GameObject contents=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Animator wrapperAnimator=contents.GetComponent<Animator>();
                if(wrapperAnimator!=null) Object.DestroyImmediate(wrapperAnimator);
                ReplaceModel(contents.transform,source,material,controller);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        GameObject root=new("PlayerVisual_Editable");
        root.transform.localPosition=Vector3.up*PlayerVisualGroundOffset;
        try
        {
            ReplaceModel(root.transform,source,material,controller);
            return PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void ReplaceModel(Transform root,GameObject source,Material material,AnimatorController controller)
    {
        // Bersihkan semua hasil setup lama. Beberapa versi prefab pernah menyimpan
        // dua nested model dengan nama yang sama sehingga Transform.Find hanya
        // menghapus salah satunya.
        Transform[] previousModels=root.Cast<Transform>()
            .Where(child=>child.name=="Model_Editable")
            .ToArray();
        foreach(Transform previous in previousModels)
        {
            if(PrefabUtility.IsAnyPrefabInstanceRoot(previous.gameObject))
                PrefabUtility.UnpackPrefabInstance(previous.gameObject,PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            Object.DestroyImmediate(previous.gameObject);
        }
        GameObject model=(GameObject)PrefabUtility.InstantiatePrefab(source,root);
        model.name="Model_Editable";
        model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
        model.transform.localScale=Vector3.one;
        // Dummy baru sengaja dibuat dua kali lebih tinggi dari ukuran visual lama.
        // Kaki disejajarkan ke pivot player agar tubuh tidak tenggelam ke terrain.
        FitModelToPlayer(model,root,PlayerVisualHeight);
        Animator importedAnimator=model.GetComponent<Animator>();
        if(importedAnimator==null)
        {
            importedAnimator=model.AddComponent<Animator>();
            importedAnimator.avatar=FindPlayerAvatar();
        }
        importedAnimator.enabled=true;
        importedAnimator.runtimeAnimatorController=controller;
        importedAnimator.applyRootMotion=false;
        foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            int count=Mathf.Max(1,renderer.sharedMaterials.Length);
            renderer.sharedMaterials=Enumerable.Repeat(material,count).ToArray();
        }
    }

    static void FitModelToPlayer(GameObject model,Transform root,float targetHeight)
    {
        // Export Blender terbaru juga membawa geometry control-rig. Jika semua Renderer
        // dihitung, bounds menjadi sekitar 10x tinggi badan dan player diperkecil ke 0.19.
        // Ukuran visual harus mengikuti mesh yang benar-benar di-skin ke skeleton.
        Renderer[] renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Cast<Renderer>()
            .ToArray();
        if(renderers.Length==0)
            renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0) return;
        Bounds bounds=renderers[0].bounds;
        for(int index=1;index<renderers.Length;index++) bounds.Encapsulate(renderers[index].bounds);
        float scale=targetHeight/Mathf.Max(0.001f,bounds.size.y);
        model.transform.localScale=Vector3.one*scale;
        bounds=renderers[0].bounds;
        for(int index=1;index<renderers.Length;index++) bounds.Encapsulate(renderers[index].bounds);
        Vector3 worldBottomCenter=new(bounds.center.x,bounds.min.y,bounds.center.z);
        Vector3 localBottomCenter=root.InverseTransformPoint(worldBottomCenter);
        model.transform.localPosition-=localBottomCenter;
    }

    static int InstallOnLoadedPlayers(GameObject prefab,AnimatorController controller)
    {
        int installed=0;
        foreach(PlayerController player in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Transform visual=player.transform.Find("PlayerVisual_Editable");
            if(visual==null)
            {
                GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,player.transform);
                instance.name="PlayerVisual_Editable";
                instance.transform.SetLocalPositionAndRotation(Vector3.up*PlayerVisualGroundOffset,Quaternion.identity);
                instance.transform.localScale=Vector3.one;
                visual=instance.transform;
                installed++;
            }
            Animator wrapperAnimator=visual.GetComponent<Animator>();
            if(wrapperAnimator!=null) Object.DestroyImmediate(wrapperAnimator);
            Animator animator=visual.GetComponentInChildren<Animator>(true);
            if(animator==null)
            {
                Transform model=visual.Find("Model_Editable");
                animator=(model!=null?model.gameObject:visual.gameObject).AddComponent<Animator>();
                animator.avatar=FindPlayerAvatar();
            }
            animator.enabled=true;
            animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;
            SerializedObject playerData=new(player);
            playerData.FindProperty("animator").objectReferenceValue=animator;
            playerData.ApplyModifiedProperties();
            MeshRenderer rootDummy=player.GetComponent<MeshRenderer>();
            if(rootDummy!=null) rootDummy.enabled=false;
            EditorUtility.SetDirty(player.gameObject);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        }
        return installed;
    }

    static Avatar FindPlayerAvatar()
    {
        return AssetDatabase.LoadAllAssetsAtPath(PreferredSourceModelPath).OfType<Avatar>()
            .FirstOrDefault(avatar=>avatar.isValid && avatar.isHuman);
    }

    static void EnsureFolders()
    {
        EnsureFolder(PlayerFolder);
        EnsureFolder(AnimationFolder);
        EnsureFolder(MixamoFolder);
        EnsureFolder("Assets/Nature  Paradaise/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Nature  Paradaise/Material");
        EnsureFolder(MaterialFolder);
    }

    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        int split=path.LastIndexOf('/');
        string parent=path.Substring(0,split);
        if(!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,path.Substring(split+1));
    }
}

/// <summary>Memperbarui PlayerVisual otomatis ketika model dummy sumber diganti.</summary>
[InitializeOnLoad]
public sealed class PlayerDummyAssetPostprocessor : AssetPostprocessor
{
    static bool queued;
    static bool pendingVisual;
    static bool pendingAnimations;

    static PlayerDummyAssetPostprocessor()
    {
        EditorApplication.delayCall+=EnsureCurrentVisual;
        EditorApplication.delayCall+=EnsureCurrentAnimations;
    }

    static void EnsureCurrentVisual()
    {
        if(!PlayerVisualSetup.IsRunning && !EditorApplication.isPlayingOrWillChangePlaymode &&
           PlayerVisualSetup.NeedsSourceRefresh())
            PlayerVisualSetup.Setup();
    }

    static void EnsureCurrentAnimations()
    {
        if(!PlayerVisualSetup.IsRunning && !EditorApplication.isPlayingOrWillChangePlaymode &&
           AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx")==null &&
           AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Breathing Idle.fbx")!=null &&
           AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Walking.fbx")!=null &&
           AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Running.fbx")!=null)
            PlayerVisualSetup.ApplyWalkAndRunAnimations();
    }

    static void OnPostprocessAllAssets(string[] importedAssets,string[] deletedAssets,
        string[] movedAssets,string[] movedFromAssetPaths)
    {
        if(PlayerVisualSetup.IsRunning || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        pendingVisual|=importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player_Dummy.fbx");
        pendingVisual|=importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx");
        pendingVisual|=importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player_Farming.fbx");
        pendingAnimations|=importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Breathing Idle.fbx") ||
                           importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Walking.fbx") ||
                           importedAssets.Contains("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Running.fbx");
        if(queued || (!pendingVisual && !pendingAnimations)) return;
        queued=true;
        EditorApplication.delayCall+=RunSetup;
    }

    static void RunSetup()
    {
        queued=false;
        if(PlayerVisualSetup.IsRunning || EditorApplication.isPlayingOrWillChangePlaymode) return;
        bool updateVisual=pendingVisual;
        bool updateAnimations=pendingAnimations;
        pendingVisual=pendingAnimations=false;
        if(updateVisual) PlayerVisualSetup.Setup();
        if(updateAnimations &&
           AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx")==null)
            PlayerVisualSetup.ApplyWalkAndRunAnimations();
    }
}
