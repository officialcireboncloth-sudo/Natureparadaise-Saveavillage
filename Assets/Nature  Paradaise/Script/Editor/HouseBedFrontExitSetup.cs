using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class HouseBedFrontExitSetup
{
    [MenuItem("Nature Paradise/House/Apply Bed Footboard Exits")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
        var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var house=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        if(house.transform.Find("BedFootboardExits_Authored_V2")!=null)return;
        bool alreadyRotated=house.transform.Find("BedFootboardExits_Authored_V1")!=null;
        for(int level=1;level<=4;level++)
        {
            house.PreviewLayout(level);
            var root=house.transform.Find($"InteriorLayout_Lv{level}_Editable/LivingBedroom_Editable/Bed_MeshSlot");
            if(level>=2&&!alreadyRotated)root.Rotate(0,180,0);
            Physics.SyncTransforms();
            var bounds=root.GetComponent<Collider>().bounds;
            var direction=level==1?Vector3.right:Vector3.left;
            var wake=root.Find("WakeStandPoint_Editable");
            wake.position=new Vector3(level==1?bounds.max.x+1.3f:bounds.min.x-1.3f,1.15f,bounds.center.z);
            wake.rotation=Quaternion.LookRotation(direction);
            var pose=root.Find("SleepPose_Editable");pose.position=new Vector3(bounds.center.x,bounds.min.y-.32f,bounds.center.z);
        }
        new GameObject("BedFootboardExits_Authored_V2").transform.SetParent(house.transform,false);
        house.PreviewLayout(1);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
