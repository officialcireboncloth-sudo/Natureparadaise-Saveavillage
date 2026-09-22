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
    const float PlayerVisualGroundOffset=0.5f;
    internal static bool IsRunning { get; private set; }

    internal static bool NeedsSourceRefresh()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PreferredSourceModelPath)==null) return false;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null) return true;
        PlayerAnimationSetSO set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(AnimationSetPath);
        if(set==null || set.idle==null || set.walk==null || set.run==null || set.jump==null ||
           set.jumpForward==null || set.pickUpFromFloor==null || set.knockOut==null ||
           set.wakeUpFromKnockOut==null)
            return true;
        if(!AssetDatabase.GetDependencies(PrefabPath).Contains(PreferredSourceModelPath)) return true;
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
        AnimationClip Clip(string name)=>clips.FirstOrDefault(clip=>
            clip.name.Equals(name,System.StringComparison.OrdinalIgnoreCase));

        set.idle=Clip("Idle");
        set.walk=Clip("Walking");
        set.run=Clip("Running");
        set.sprint=set.run;
        set.jump=Clip("Jumping");
        set.jumpForward=Clip("JumpingForward");
        set.pickUpFromFloor=Clip("PickUpFromFloor");
        set.knockOut=Clip("KnockOut");
        set.wakeUpFromKnockOut=Clip("WakeUpFromKnockOut");
        set.milkingAnimal=Clip("MilkingAnimal");
        set.pushingObject=Clip("PushingObject");
        set.wateringPlant=Clip("WateringPlant");
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
        AddParameter(controller,"KnockOut",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"WakeUp",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Milking",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Pushing",AnimatorControllerParameterType.Trigger);
        AddParameter(controller,"Watering",AnimatorControllerParameterType.Trigger);
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
        // 27 frame / 30 fps pada 1.25x memberi durasi sekitar 0,72 detik,
        // dekat dengan arc fisik 0,61 detik ditambah waktu blend keluar.
        EnsureActionState(stateMachine,"Jump Forward","JumpForward",clips.jumpForward,1.25f,true);
        EnsureActionState(stateMachine,"Pick Up From Floor","PickUp",clips.pickUpFromFloor,4f,true);
        EnsureActionState(stateMachine,"Knock Out","KnockOut",clips.knockOut,1f,false);
        EnsureActionState(stateMachine,"Wake Up","WakeUp",clips.wakeUpFromKnockOut,4f,true);
        EnsureActionState(stateMachine,"Milking Animal","Milking",clips.milkingAnimal,2f,true);
        EnsureActionState(stateMachine,"Pushing Object","Pushing",clips.pushingObject,1f,true);
        EnsureActionState(stateMachine,"Watering Plant","Watering",clips.wateringPlant,2f,true);
        EditorUtility.SetDirty(controller);
        return controller;
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

    static ChildMotion Child(AnimationClip clip,float threshold) => new() {motion=clip,threshold=threshold,timeScale=1f};

    static AnimatorState FindState(AnimatorStateMachine machine,string name) =>
        machine.states.Select(item=>item.state).FirstOrDefault(state=>state.name==name);

    static void AddParameter(AnimatorController controller,string name,AnimatorControllerParameterType type)
    {
        if(controller.parameters.All(parameter=>parameter.name!=name)) controller.AddParameter(name,type);
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
                EmbeddedClip("JumpingForward","Armature|JumpingForward",27f),
                EmbeddedClip("KnockOut","Armature|KnockOut",78f),
                EmbeddedClip("MilkingAnimal","Armature|MilkingAnimal",136f),
                EmbeddedClip("PickUpFromFloor","Armature|PickUpFromFloor",287f),
                EmbeddedClip("PushingObject","Armature|PushingObject",51f),
                EmbeddedClip("Running","Armature|Running",19f),
                EmbeddedClip("WakeUpFromKnockOut","Armature|WakeUpFromKnockOut",342f),
                EmbeddedClip("Walking","Armature|Walking",29f),
                EmbeddedClip("WateringPlant","Armature|WateringPlant",168f)
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
            bool loop=cleanName=="Idle" || cleanName=="Walking" || cleanName=="Running";
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
                (clip.name!=configured[index].name || clip.loopTime!=configured[index].loopTime)).Any();
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
        Renderer[] renderers=model.GetComponentsInChildren<Renderer>(true);
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
