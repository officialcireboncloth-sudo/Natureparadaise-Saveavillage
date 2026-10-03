using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HouseBedAccessSetup
{
    [MenuItem("Nature Paradise/House/Apply Bed Access and Tool Rack")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
        var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var house=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        if(house.transform.Find("BedAccess_Authored_V2")!=null)return;
        bool alreadyPlaced=house.transform.Find("BedAccess_Authored_V1")!=null;
        for(int level=1;level<=4;level++)
        {
            var living=house.transform.Find($"InteriorLayout_Lv{level}_Editable/LivingBedroom_Editable");
            var bedRoot=living.Find("Bed_MeshSlot");var bed=bedRoot.GetComponentInChildren<PlayerBed>(true);
            var shelf=living.Find("Bookshelf_Editable");
            if(level==1 && !alreadyPlaced)
            {
                bedRoot.localPosition=new Vector3(2.2f,0,7.5f);
                shelf.localPosition=new Vector3(6,0,8.8f);
                living.Find("BedroomWardrobe_Editable").localPosition=new Vector3(8.6f,0,8.8f);
                living.Find("Armchair_Editable").localPosition=new Vector3(3.5f,0,2.6f);
            }
            else if(!alreadyPlaced)
            {
                bedRoot.localPosition+=Vector3.back*.4f;
                shelf.localPosition+=Vector3.left*.4f;
                living.Find("BedroomWardrobe_Editable").localPosition+=Vector3.left*.4f;
            }
            var oldChest=living.Find("ToolStorageChest_Editable");if(oldChest!=null)oldChest.gameObject.SetActive(false);
            var rack=shelf.GetComponent<ToolStorageChest>();if(rack==null)rack=shelf.gameObject.AddComponent<ToolStorageChest>();
            var rackData=new SerializedObject(rack);rackData.FindProperty("interactionRadius").floatValue=1.6f;rackData.ApplyModifiedPropertiesWithoutUndo();
            // Imported preset pivots can be far from the visible mattress. Keep both
            // the interaction component and its collider on the normalized wrapper.
            if(bed.transform!=bedRoot)
            {
                var authored=bedRoot.gameObject.AddComponent<PlayerBed>();EditorUtility.CopySerialized(bed,authored);Object.DestroyImmediate(bed);bed=authored;
            }
            var renderers=bedRoot.GetComponentsInChildren<Renderer>(true);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            var bedCollider=bedRoot.GetComponent<BoxCollider>();
            foreach(var collider in bedRoot.GetComponentsInChildren<Collider>(true))collider.enabled=collider==bedCollider;
            bedCollider.center=bedRoot.InverseTransformPoint(bounds.center);bedCollider.size=bounds.size;bedCollider.isTrigger=false;
            var wake=bedRoot.Find("WakeStandPoint_Editable");
            if(wake==null){wake=new GameObject("WakeStandPoint_Editable").transform;wake.SetParent(bedRoot,false);}
            wake.position=level==1?new Vector3(bounds.center.x,1.15f,bounds.min.z-1.4f):new Vector3(bounds.min.x-1.4f,1.15f,bounds.center.z);
            wake.rotation=Quaternion.Euler(0,180,0);
            var pose=bedRoot.Find("SleepPose_Editable");
            if(pose==null){pose=new GameObject("SleepPose_Editable").transform;pose.SetParent(bedRoot,false);}
            var bedData=new SerializedObject(bed);
            var oldPose=bedData.FindProperty("sleepPose").objectReferenceValue as Transform;
            pose.rotation=oldPose!=null?oldPose.rotation:Quaternion.Euler(0,90,0);
            pose.position=new Vector3(bounds.center.x,bounds.min.y-.32f,bounds.center.z);
            bedData.FindProperty("interactionRadius").floatValue=1.3f;bedData.FindProperty("wakeStandPoint").objectReferenceValue=wake;bedData.FindProperty("sleepPose").objectReferenceValue=pose;bedData.ApplyModifiedPropertiesWithoutUndo();
        }
        new GameObject("BedAccess_Authored_V2").transform.SetParent(house.transform,false);
        house.PreviewLayout(1);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=house.gameObject;
    }
}
