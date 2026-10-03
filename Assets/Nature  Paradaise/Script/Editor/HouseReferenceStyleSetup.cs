using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-time editor migration: compact rooms, front cutaway, warm central living area.</summary>
public static class HouseReferenceStyleSetup
{
    const string ScenePath = "Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
    [MenuItem("Nature Paradise/House/Apply Frontal Interior Style")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var house = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        if(house.transform.Find("FrontalStyle_Authored_V1") != null)
        {
            Polish(house);
            FrameAndDecorate(house);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return;
        }
        var views = new List<Camera>();
        for(int level=1; level<=4; level++)
        {
            var layout = house.transform.Find($"InteriorLayout_Lv{level}_Editable");
            float oldW=level==1?12:level==2?18:24, oldD=level<=2?12:level==3?18:24;
            float width=level==1?12:level==2?16:level==3?20:22;
            float depth=level==1?10:level==2?11:level==3?13:15;
            var structure = layout.Find("Structure_Editable");
            structure.localScale = new Vector3(width/oldW,1,depth/oldD);
            foreach(Transform t in structure)
            {
                if(t.name!="Wall_Editable") continue;
                if(t.localPosition.z<.2f) LowerWall(t,.18f);
                else if(t.localPosition.z<3.1f) LowerWall(t,1.1f);
            }
            var bath=layout.Find("Bathroom_Editable");
            foreach(Transform t in bath)
            {
                t.localPosition += Vector3.right*(width-oldW);
                if(t.name=="Wall_Editable") LowerWall(t,1.1f);
            }
            var living=layout.Find("LivingBedroom_Editable");
            Move(living,"Bed_MeshSlot",new Vector3(level==1?2:width-2.2f,0,depth-2));
            Move(living,"ToolStorageChest_Editable",new Vector3(level==1?width-1.7f:1.5f,0,level==1?depth-1.3f:1.5f));
            float tvX=level==1?1.5f:width-1.25f;
            var stand=living.Find("TVStand_Editable");
            var tv=living.Find("TV_MeshSlot");
            var shift=new Vector3(tvX,0,4)-stand.localPosition;
            stand.localPosition+=shift;tv.localPosition+=shift;
            Move(living,"Armchair_Editable",new Vector3(level==1?3.8f:width*.57f-2.8f,0,3.2f));
            AddFurniture(living,"TFP_Bookshelf_01A","Bookshelf_Editable",new Vector3(level==1?4.8f:width-6.4f,0,depth-.9f));
            AddFurniture(living,"TFP_Wardrobe_01B","BedroomWardrobe_Editable",new Vector3(level==1?7.1f:width-4.4f,0,depth-.9f));
            var rug=AddFurniture(living,"TFP_Entrance_Rug_01A","LivingRug_Editable",new Vector3(width*.55f,.025f,depth*.46f),false);
            Fit(rug,new Vector3(width*.55f,.025f,depth*.46f),new Vector3(level==1?5:6,.035f,level==1?3.5f:4));
            if(level>=2)
            {
                layout.Find("Kitchen_Editable").localPosition+=Vector3.forward*(depth-oldD);
                var dining=layout.Find("Dining_Editable");
                Move(dining,"DiningTable_Editable",new Vector3(3.3f,0,3.8f));
                Move(dining,"DiningChair_Editable",new Vector3(3.3f,0,2));
            }
            if(level>=3)
            {
                var storage=layout.Find("AdditionalStorage_Editable");
                foreach(Transform t in storage) if(t.name=="Wall_Editable") LowerWall(t,1.1f);
                Move(storage,"StorageWardrobe_Editable",new Vector3(6.9f,0,4.7f));
            }
            if(level==4)
            {
                var lounge=layout.Find("ExpandedLounge_Editable");
                Move(lounge,"GuestArmchair_Editable",new Vector3(width*.57f+2.8f,0,3.2f));
                Move(lounge,"CoffeeTable_Editable",new Vector3(width*.57f,0,4.4f));
            }
            layout.Find("EntryPoint_Editable").localPosition=new Vector3(width*.5f,1.15f,1.5f);
            layout.Find("ExitPoint_Editable").localPosition=new Vector3(width*.5f,1.2f,0);
            var cameraGo=new GameObject("InteriorPerspectiveCamera_Editable");cameraGo.transform.SetParent(layout,false);
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=false;camera.fieldOfView=40;
            camera.nearClipPlane=.1f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.25f,.35f);
            camera.transform.localRotation=Quaternion.Euler(48,0,0);
            float distance=Mathf.Max(width*.95f,depth*1.55f)+2;
            camera.transform.localPosition=new Vector3(width*.5f,1,depth*.5f)-camera.transform.forward*distance;
            views.Add(camera);
        }
        var profile=house.GetComponent<HouseInteriorView>();if(profile==null)profile=house.gameObject.AddComponent<HouseInteriorView>();
        profile.Configure(views);
        new GameObject("FrontalStyle_Authored_V1").transform.SetParent(house.transform,false);
        Polish(house);
        FrameAndDecorate(house);
        house.PreviewLayout(1);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=house.gameObject;
    }
    static void Move(Transform parent,string name,Vector3 position){var t=parent.Find(name);if(t!=null)t.localPosition=position;}
    static void FrameAndDecorate(HouseInteriorController house)
    {
        if(house.transform.Find("FrontalStyle_Framed_V1")!=null)return;
        for(int level=1;level<=4;level++)
        {
            float width=level==1?12:level==2?16:level==3?20:22,depth=level==1?10:level==2?11:level==3?13:15;
            var layout=house.transform.Find($"InteriorLayout_Lv{level}_Editable");
            var camera=layout.Find("InteriorPerspectiveCamera_Editable").GetComponent<Camera>();
            camera.transform.localRotation=Quaternion.Euler(38,0,0);
            camera.transform.localPosition=new Vector3(width*.5f,1,depth*.5f)-camera.transform.forward*(Mathf.Max(width*.95f,depth*1.3f)+2);
            var shelf=layout.Find("LivingBedroom_Editable/Bookshelf_Editable");
            for(int row=0;row<3;row++)
            {
                var books=AddFurniture(shelf,"TFP_Books_01A",$"ShelfBooks_{row+1}_Editable",new Vector3(0,.4f+row*.65f,0),false);
                books.localScale=Vector3.one*.65f;
            }
        }
        new GameObject("FrontalStyle_Framed_V1").transform.SetParent(house.transform,false);
    }
    static void Polish(HouseInteriorController house)
    {
        if(house.transform.Find("FrontalStyle_Polished_V1")!=null)return;
        for(int level=1;level<=4;level++)
        {
            float width=level==1?12:level==2?16:level==3?20:22;
            var living=house.transform.Find($"InteriorLayout_Lv{level}_Editable/LivingBedroom_Editable");
            foreach(string name in new[]{"Bookshelf_Editable","BedroomWardrobe_Editable"})
                living.Find(name).localRotation=Quaternion.Euler(0,90,0);
            var oldRug=living.Find("LivingRug_Editable");var center=oldRug.localPosition;Object.DestroyImmediate(oldRug.gameObject);
            var rug=AddFurniture(living,"TFP_Rug_03A","LivingRug_Editable",center,false);
            Fit(rug,center,new Vector3(level==1?5:6,.035f,level==1?3.5f:4));
            if(level>=2)
            {
                var stand=living.Find("TVStand_Editable");var tv=living.Find("TV_MeshSlot");
                var shift=new Vector3(width-4.8f,0,6)-stand.localPosition;
                stand.localPosition+=shift;tv.localPosition+=shift;
            }
        }
        new GameObject("FrontalStyle_Polished_V1").transform.SetParent(house.transform,false);
    }
    static Bounds BoundsOf(Transform t)
    {
        var rr=t.GetComponentsInChildren<Renderer>(true);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;
    }
    static void LowerWall(Transform t,float height)
    {
        var b=BoundsOf(t);float ground=b.min.y;t.localScale=new Vector3(t.localScale.x,t.localScale.y*height/b.size.y,t.localScale.z);
        b=BoundsOf(t);t.position+=Vector3.up*(ground-b.min.y);
    }
    static Transform AddFurniture(Transform parent,string asset,string name,Vector3 position,bool collider=true)
    {
        string path=AssetDatabase.FindAssets(asset+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>System.IO.Path.GetFileNameWithoutExtension(p)==asset && p.Contains("Toon Farm Pack"));
        var wrapper=new GameObject(name).transform;wrapper.SetParent(parent,false);wrapper.localPosition=position;
        var visual=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),wrapper)).transform;
        visual.localScale*=1.5f;
        foreach(var c in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var b=BoundsOf(visual);visual.position+=wrapper.position+Vector3.up*b.extents.y-b.center;
        b=BoundsOf(visual);
        if(collider){var box=wrapper.gameObject.AddComponent<BoxCollider>();box.center=wrapper.InverseTransformPoint(b.center);box.size=b.size;}
        return wrapper;
    }
    static void Fit(Transform t,Vector3 localCenter,Vector3 size)
    {
        var b=BoundsOf(t);t.localScale=Vector3.Scale(t.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
        b=BoundsOf(t);t.position+=t.parent.TransformPoint(localCenter)-b.center;
    }
}
