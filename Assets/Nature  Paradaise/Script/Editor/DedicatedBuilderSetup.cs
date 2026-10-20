#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Explicit installation of the user's dedicated worker; never saves an open scene.</summary>
[InitializeOnLoad]
public static class DedicatedBuilderSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Folder=Root+"/mesh/Character/Builder/Prepared";
    const string Request="ArtSource/Builder_Worker/apply-unity.request";
    const string Output="ArtSource/Builder_Worker/Previews";
    static bool busy;
    static DedicatedBuilderSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try
        {
            Directory.CreateDirectory(Output);
            File.Move(Request,Output+"/UnityRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
            Apply();
        }
        catch(Exception e){File.WriteAllText(Output+"/DedicatedUnityError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    [MenuItem("Nature Paradise/Construction/Apply Dedicated Meshy Builder")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before installing worker assets.");
        Directory.CreateDirectory(Folder);
        string modelPath=Folder+"/Builder_Humanoid.fbx";
        File.Copy("ArtSource/Builder_Worker/Exports/Builder_Humanoid.fbx",modelPath,true);
        foreach(string name in new[]{"BaseColor","Normal","Roughness","Metallic"})
            File.Copy("ArtSource/Builder_Worker/Textures/Builder_"+name+".png",Folder+"/Builder_"+name+".png",true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Texture2D Texture(string name,bool normal=false,bool color=false)
        {
            string path=Folder+"/Builder_"+name+".png";
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            ti.sRGBTexture=color;ti.maxTextureSize=2048;ti.mipmapEnabled=true;
            ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var albedo=Texture("BaseColor",color:true);var normalMap=Texture("Normal",normal:true);
        Texture("Roughness");Texture("Metallic");
        // URP expects metallic in red and smoothness (1-roughness) in alpha.
        var rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        var metal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        var packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        try
        {
            rough.LoadImage(File.ReadAllBytes(Folder+"/Builder_Roughness.png"));
            metal.LoadImage(File.ReadAllBytes(Folder+"/Builder_Metallic.png"));
            if(rough.width!=metal.width||rough.height!=metal.height)throw new InvalidOperationException("PBR map dimensions differ.");
            var r=rough.GetPixels32();var m=metal.GetPixels32();
            for(int i=0;i<m.Length;i++)m[i]=new Color32(m[i].r,0,0,(byte)(255-r[i].r));
            packed.Reinitialize(metal.width,metal.height);packed.SetPixels32(m);packed.Apply();
            File.WriteAllBytes(Folder+"/Builder_MetallicSmoothness.png",packed.EncodeToPNG());
        }
        finally{Object.DestroyImmediate(rough);Object.DestroyImmediate(metal);Object.DestroyImmediate(packed);}
        AssetDatabase.ImportAsset(Folder+"/Builder_MetallicSmoothness.png",ImportAssetOptions.ForceSynchronousImport);
        var packedMap=Texture("MetallicSmoothness");
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null)throw new InvalidOperationException("URP Lit shader unavailable.");
        string materialPath=Folder+"/Builder_PBR.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
        material.shader=shader;material.SetTexture("_BaseMap",albedo);material.SetColor("_BaseColor",Color.white);
        material.SetTexture("_BumpMap",normalMap);material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap",packedMap);material.SetFloat("_Smoothness",1);material.SetFloat("_Metallic",1);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(material);
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar=null;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        var definitions=importer.defaultClipAnimations;
        foreach(var c in definitions)
        {
            int separator=c.name.LastIndexOf(" | ",StringComparison.Ordinal);
            if(separator>=0)c.name=c.name.Substring(separator+3);
            c.loopTime=c.name=="Idle"||c.name=="Walking"||c.name=="Running"||c.name=="Hold Two Hands";
            c.keepOriginalOrientation=true;c.lockRootRotation=true;
            c.keepOriginalPositionXZ=true;c.lockRootPositionXZ=true;
            c.keepOriginalPositionY=true;c.lockRootHeightY=true;
        }
        importer.clipAnimations=definitions;importer.SaveAndReimport();
        var avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(a=>a.isValid&&a.isHuman);
        if(avatar==null)throw new InvalidOperationException("Dedicated builder Avatar is not valid Humanoid.");
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().ToArray();
        AnimationClip Clip(string name)=>clips.FirstOrDefault(c=>c.name==name&&c.isHumanMotion);
        if(Clip("Idle")==null||Clip("Walking")==null)throw new InvalidOperationException("Retargeted Idle/Walking are missing.");
        var settings=ConstructionWorkerSettings.Load();
        if(settings==null)throw new InvalidOperationException("Construction worker settings missing.");
        string libraryPath=Root+"/Player/Animations/Shared/Dedicated Builder Actions.asset";
        var library=AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(libraryPath);
        if(library==null)
        {
            library=ScriptableObject.CreateInstance<HumanoidActionLibrary>();
            if(settings.humanoidActions!=null)EditorUtility.CopySerialized(settings.humanoidActions,library);
            AssetDatabase.CreateAsset(library,libraryPath);
        }
        foreach(var pair in new[]{("Idle","Idle"),("Walk","Walking")})
        {
            var action=library.Find(pair.Item1);
            if(action==null){action=new HumanoidActionLibrary.Action{id=pair.Item1};library.actions.Add(action);}
            action.clip=Clip(pair.Item2);action.loop=true;action.speed=1;
        }
        EditorUtility.SetDirty(library);
        var root=new GameObject("BuilderHumanoid_Dedicated");
        try
        {
            var visual=new GameObject("BuilderVisual").transform;visual.SetParent(root.transform,false);
            var model=ConstructionProjectVisuals.VisualClone(imported,visual);model.name="DedicatedBuilderModel";
            var animator=model.GetComponentInChildren<Animator>(true)??model.AddComponent<Animator>();
            animator.avatar=avatar;animator.applyRootMotion=false;animator.runtimeAnimatorController=null;
            animator.fireEvents=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled=renderer is SkinnedMeshRenderer;
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
            }
            // FBX -Z forward can differ from the navigation +Z convention. Align the actual
            // evaluated feet, not the FBX root's conversion quaternion, before solving work IK.
            float facingYaw=0;
            using(var pose=new HumanoidActionPlayer(animator,library))
            {
                pose.Evaluate(.001f);
                var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes);
                var rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes);
                if(leftFoot==null||rightFoot==null||leftToe==null||rightToe==null)
                    throw new InvalidOperationException("Worker foot/toe landmarks required for forward alignment.");
                var forward=Vector3.ProjectOnPlane((leftToe.position-leftFoot.position)+(rightToe.position-rightFoot.position),Vector3.up);
                if(forward.sqrMagnitude<.00001f)throw new InvalidOperationException("Ambiguous worker facing direction.");
                facingYaw=Vector3.SignedAngle(forward,Vector3.forward,Vector3.up);
                visual.localRotation=Quaternion.AngleAxis(facingYaw,Vector3.up);
            }
            string prefabPath=Root+"/Prefabs/Construction/BuilderDedicated.prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            // Assign only once all asset/Avatar/clip checks succeeded. No open scene changes.
            settings.workerPrefab=prefab;settings.humanoidActions=library;settings.workerScale=PlayerDimensions.Height/2;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            var report=new StringBuilder("DEDICATED BUILDER APPLIED "+DateTime.UtcNow.ToString("O")+"\n");
            report.AppendLine("Prefab: "+prefabPath);report.AppendLine("Model: "+modelPath);
            report.AppendLine("PASS own valid Humanoid Avatar; corrected builder Idle/Walk assigned");
            report.AppendLine("PASS separate library; previous player builder assets preserved; open scenes NOT saved");
            report.AppendLine("PASS URP PBR material with normal and metallic/smoothness maps");
            report.AppendLine("PASS normalized 2m model at workerScale "+settings.workerScale);
            report.AppendLine("PASS toe-derived navigation-facing correction: "+facingYaw+" degrees");
            foreach(var action in library.actions)report.AppendLine(action.id+": "+(action.clip? action.clip.name:"procedural work contact"));
            File.WriteAllText(Output+"/DedicatedUnityApply.txt",report.ToString());
        }
        finally{Object.DestroyImmediate(root);}
    }
}
#endif
