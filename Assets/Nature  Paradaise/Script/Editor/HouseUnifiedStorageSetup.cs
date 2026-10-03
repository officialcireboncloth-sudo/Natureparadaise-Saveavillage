using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class HouseUnifiedStorageSetup
{
    [MenuItem("Nature Paradise/House/Apply Unified House Storage")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
        var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var house=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        for(int lv=1;lv<=4;lv++)
        {
            var shelf=house.transform.Find($"InteriorLayout_Lv{lv}_Editable/LivingBedroom_Editable/Bookshelf_Editable");
            var tool=shelf.GetComponent<ToolStorageChest>();if(tool!=null)Object.DestroyImmediate(tool);
            if(shelf.GetComponent<HouseStorageChest>()==null)shelf.gameObject.AddComponent<HouseStorageChest>();
        }
        house.PreviewLayout(1);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
