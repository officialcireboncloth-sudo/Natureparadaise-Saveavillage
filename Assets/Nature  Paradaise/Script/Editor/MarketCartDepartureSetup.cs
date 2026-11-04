#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MarketCartDepartureSetup
{
    const string Root="Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/Narrative/Apply Market Cart Departure")]
    public static void Apply()
    {
        var avatar=AssetDatabase.LoadAllAssetsAtPath(Root+"/mesh/Character/MarketKeeper/MarketKeeper.fbx").OfType<Avatar>().First(a=>a.isHuman&&a.isValid);
        var library=AssetDatabase.LoadAssetAtPath<HumanoidActionLibrary>(MarketKeeperCharacterSetup.ActionsPath);
        foreach(string id in new[]{"Cart_Grip","Cart_Pull","Cart_Push"})
        {
            string path=Root+"/mesh/Character/MarketKeeper/"+id+".fbx";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=avatar;
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.importBlendShapes=false;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clip=new ModelImporterClipAnimation{name=id,takeName=importer.defaultClipAnimations.First().takeName,firstFrame=0,lastFrame=30,loopTime=id!="Cart_Grip",loopPose=false,lockRootRotation=true,lockRootHeightY=true,lockRootPositionXZ=true,keepOriginalOrientation=true,keepOriginalPositionY=true,keepOriginalPositionXZ=true};
            importer.clipAnimations=new[]{clip};importer.SaveAndReimport();
            var animation=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name==id);
            if(!animation.isHumanMotion)throw new Exception(id+" is not Humanoid");
            var entry=library.Find(id);if(entry==null){entry=new HumanoidActionLibrary.Action{id=id};library.actions.Add(entry);}
            entry.clip=animation;entry.loop=id!="Cart_Grip";entry.speed=1;
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        MarketCartCozySetup.Apply();
        string prefab=Root+"/Prefabs/World/MarketStand.prefab";var obj=PrefabUtility.LoadPrefabContents(prefab);
        try
        {
            if(!obj.GetComponent<MarketCartDriver>())obj.AddComponent<MarketCartDriver>();
            var settings=new SerializedObject(obj.GetComponent<MarketCartSchedule>());
            settings.FindProperty("walkingSpeed").floatValue=1.8f;settings.FindProperty("departureFadeDistance").floatValue=18f;settings.FindProperty("travelHalfExtents").vector3Value=new Vector3(1.05f,.8f,1.65f);settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(obj,prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(obj);}
        AssetDatabase.SaveAssets();
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Isolated fixture only");
        try{Apply();File.WriteAllText("ArtSource/MarketCart/UnityReview/DepartureSetup.txt","PASS Three Humanoid clips, separate wheel pivots, grip anchors and departure driver assigned.\n");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);File.WriteAllText("ArtSource/MarketCart/UnityReview/DepartureError.txt",e.ToString());EditorApplication.Exit(1);}
    }
}
#endif
