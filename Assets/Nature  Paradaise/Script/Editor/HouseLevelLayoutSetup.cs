using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authors persistent Toon Farm rooms; no runtime geometry generation.</summary>
public static class HouseLevelLayoutSetup
{
    const string ScenePath="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
    static Transform[] sources;
    static Transform Find(string prefix) => sources.First(t=>t.name.StartsWith(prefix));
    [MenuItem("Nature Paradise/House/Author Progressive Level Layouts",false,127)]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        Transform root=controller.transform;
        if(root.Find("ProgressiveLevels_Authored_V1")!=null)
        {
            foreach(var layout in root.Cast<Transform>().Where(t=>t.name.StartsWith("InteriorLayout_Lv")))
                foreach(var t in layout.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="Refrigerator_MeshSlot" && t.GetComponent<Refrigerator>()==null)t.gameObject.AddComponent<Refrigerator>();
                    if(t.name.StartsWith("StorageChest_"))
                    {
                        if(t.GetComponent<ToolStorageChest>()!=null)Object.DestroyImmediate(t.GetComponent<ToolStorageChest>());
                        if(t.GetComponent<HouseStorageChest>()==null)t.gameObject.AddComponent<HouseStorageChest>();
                    }
                }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return;
        }
        // Preserve the original scene as an editable reference and a disk backup.
        File.Copy(ScenePath,"Library/HouseInterior_BeforeProgressiveLevels.unity",true);
        sources=root.GetComponentsInChildren<Transform>(true);
        var archive=new GameObject("OriginalPreset_Reference_Inactive").transform;
        archive.SetParent(root,false);
        foreach(Transform t in root.Cast<Transform>().ToArray())
            if(t.name.StartsWith("InteriorLayout_Lv")) { t.SetParent(archive,true); t.name+="_Original"; }
        var layouts=new List<GameObject>(); var entries=new List<Transform>(); var exits=new List<Transform>();
        for(int level=1;level<=4;level++)
        {
            int width=level==1?12:level==2?18:24;
            int depth=level<=2?12:level==3?18:24;
            var layout=new GameObject($"InteriorLayout_Lv{level}_Editable");layout.transform.SetParent(root,false);
            var structure=Group(layout.transform,"Structure_Editable");
            var furniture=Group(layout.transform,"LivingBedroom_Editable");
            for(int x=0;x<width;x+=6) for(int z=0;z<depth;z+=6)
                Fit(Find("TFP_House_Floor_Small_03A"),structure,$"WoodFloor_{x}_{z}",new Vector3(x+3,-.075f,z+3),new Vector3(6,.15f,6));
            // One continuous foundation prevents seams and stair openings from becoming voids.
            Box(structure,"ContinuousFloorCollider",new Vector3(width*.5f,-.18f,depth*.5f),new Vector3(width,.3f,depth));
            for(int z=0;z<depth;z+=6)
            { Wall(structure,new Vector3(0,1.8f,z+3),new Vector3(.3f,3.6f,6));Wall(structure,new Vector3(width,1.8f,z+3),new Vector3(.3f,3.6f,6)); }
            for(int x=0;x<width;x+=6) Wall(structure,new Vector3(x+3,1.8f,depth),new Vector3(6,3.6f,.3f));
            float doorX=width*.5f;
            Wall(structure,new Vector3((doorX-1.5f)*.5f,1.8f,0),new Vector3(doorX-1.5f,3.6f,.3f));
            Wall(structure,new Vector3((width+doorX+1.5f)*.5f,1.8f,0),new Vector3(width-doorX-1.5f,3.6f,.3f));
            var entry=Group(layout.transform,"EntryPoint_Editable");entry.localPosition=new Vector3(doorX,1.15f,2);
            var exit=Group(layout.transform,"ExitPoint_Editable");exit.localPosition=new Vector3(doorX,1.2f,0);
            entries.Add(entry);exits.Add(exit);layouts.Add(layout);
            var bed=Furniture(Find("Bed_MeshSlot"),furniture,"Bed_MeshSlot",new Vector3(width-3,0,depth-2));
            var b=BoundsOf(bed);var sleep=bed.Find("SleepPose_Editable");var wake=bed.Find("WakeStandPoint_Editable");
            if(sleep!=null)sleep.position=new Vector3(b.center.x,b.min.y-.32f,b.center.z);
            if(wake!=null)wake.position=new Vector3(b.center.x,1.15f,b.center.z-2.2f);
            var stand=Furniture(Find("TFP_TV_Stand_01B"),furniture,"TVStand_Editable",new Vector3(width-1.5f,0,depth*.5f));
            var tv=Furniture(Find("TV_MeshSlot"),furniture,"TV_MeshSlot",new Vector3(width-1.5f,BoundsOf(stand).max.y,depth*.5f));
            Furniture(Find("TFP_Armchair_01E"),furniture,"Armchair_Editable",new Vector3(width-4.5f,0,depth*.5f));
            Furniture(Find("ToolStorageChest_Editable"),furniture,"ToolStorageChest_Editable",new Vector3(1.5f,0,2));
            var bath=Group(layout.transform,"Bathroom_Editable");
            Fit(Find("TFP_House_Floor_Small_01B"),bath,"BathroomFloor_Editable",new Vector3(width-1.5f,.005f,3),new Vector3(3,.15f,6));
            Wall(bath,new Vector3(width-3,1.8f,1.5f),new Vector3(.3f,3.6f,3));
            Wall(bath,new Vector3(width-3,1.8f,5.75f),new Vector3(.3f,3.6f,.5f));
            Wall(bath,new Vector3(width-1.5f,1.8f,6),new Vector3(3,3.6f,.3f));
            Furniture(Find("TFP_Bathroom_Toilet_01A"),bath,"Toilet_Editable",new Vector3(width-1.5f,.08f,1.3f));
            Furniture(Find("TFP_Bathroom_Sink_01A"),bath,"BathroomSink_Editable",new Vector3(width-1.5f,.08f,4.8f));
            if(level>=2)
            {
                var kitchen=Group(layout.transform,"Kitchen_Editable");
                Fit(Find("TFP_House_Floor_Regular_04B"),kitchen,"KitchenTileFloor_Editable",new Vector3(3,.005f,depth-3),new Vector3(6,.15f,6));
                Furniture(Find("Refrigerator_MeshSlot"),kitchen,"Refrigerator_MeshSlot",new Vector3(1.5f,.08f,depth-1.2f));
                Furniture(Find("Kitchen_MeshSlot"),kitchen,"Kitchen_MeshSlot",new Vector3(1,.08f,depth-4));
                Furniture(Find("TFP_Kitchen_Cabinet_Sink_01A"),kitchen,"KitchenSink_Editable",new Vector3(4,.08f,depth-1.2f));
                Furniture(Find("TFP_Kitchen_Cabinet_02A"),kitchen,"KitchenCounter_Editable",new Vector3(4,.08f,depth-3));
                var dining=Group(layout.transform,"Dining_Editable");
                Furniture(Find("TFP_Wooden_Table_02B"),dining,"DiningTable_Editable",new Vector3(8,0,depth-4));
                Furniture(Find("TFP_Wooden_Chair_02B"),dining,"DiningChair_Editable",new Vector3(8,0,depth-6));
            }
            if(level>=3)
            {
                var storage=Group(layout.transform,"AdditionalStorage_Editable");
                Wall(storage,new Vector3(3,1.8f,6),new Vector3(6,3.6f,.3f));
                for(int i=0;i<level-1;i++)
                {
                    var chest=Furniture(Find("ToolStorageChest_Editable"),storage,$"StorageChest_{i+1}_Editable",new Vector3(1.5f+i*2,0,4.7f));
                    if(chest.GetComponent<ToolStorageChest>()!=null)Object.DestroyImmediate(chest.GetComponent<ToolStorageChest>());
                    chest.gameObject.AddComponent<HouseStorageChest>();
                }
                Furniture(Find("TFP_Wardrobe_01B"),storage,"StorageWardrobe_Editable",new Vector3(1.5f,0,8));
            }
            if(level==4)
            {
                var lounge=Group(layout.transform,"ExpandedLounge_Editable");
                Furniture(Find("TFP_Armchair_01E"),lounge,"GuestArmchair_Editable",new Vector3(14,0,7));
                Furniture(Find("TFP_Wooden_Table_02B"),lounge,"CoffeeTable_Editable",new Vector3(14,0,10));
            }
        }
        archive.gameObject.SetActive(false);
        Group(root,"ProgressiveLevels_Authored_V1");
        controller.ConfigureAuthoredLevels(layouts,entries,exits);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=controller.gameObject;
        Debug.Log("[HOUSE] Four persistent level layouts authored. Use Preview Lv.1-4 in the house Inspector.");
    }
    static Transform Group(Transform parent,string name) {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Bounds BoundsOf(Transform t)
    {
        var renderers=t.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        if(renderers.Length==0)throw new System.Exception("Missing visual for "+t.name);
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds;
    }
    static Transform Clone(Transform source,Transform parent,string name)
    {
        var clone=Object.Instantiate(source.gameObject,parent).transform;
        clone.name=name;clone.localScale=source.lossyScale;clone.rotation=source.rotation;clone.gameObject.SetActive(true);
        foreach(var c in clone.GetComponentsInChildren<Collider>(true))c.enabled=false;
        return clone;
    }
    static Transform Furniture(Transform source,Transform parent,string name,Vector3 position)
    {
        var wrapper=Group(parent,name);var visual=Clone(source,wrapper,source.name+"_Visual");
        // Keep the interaction pivot at the actual visible model, including preset offset pivots.
        var bounds=BoundsOf(visual);visual.position+=new Vector3(position.x,position.y+bounds.extents.y,position.z)-bounds.center;
        bounds=BoundsOf(visual);wrapper.position=new Vector3(position.x,position.y,position.z);
        visual.position-=wrapper.position; // parenting moved it once; restore the authored world placement.
        bounds=BoundsOf(visual);
        var collider=wrapper.gameObject.AddComponent<BoxCollider>();collider.center=wrapper.InverseTransformPoint(bounds.center);collider.size=bounds.size;
        foreach(var component in visual.GetComponents<MonoBehaviour>())
        {
            if(component is Refrigerator) {Object.DestroyImmediate(component);wrapper.gameObject.AddComponent<Refrigerator>();}
            else if(component is KitchenSet) {Object.DestroyImmediate(component);wrapper.gameObject.AddComponent<KitchenSet>();}
            else if(component is ToolStorageChest) {Object.DestroyImmediate(component);wrapper.gameObject.AddComponent<ToolStorageChest>();}
        }
        if(name=="Refrigerator_MeshSlot" && wrapper.GetComponent<Refrigerator>()==null)wrapper.gameObject.AddComponent<Refrigerator>();
        if(name=="Kitchen_MeshSlot" && wrapper.GetComponent<KitchenSet>()==null)wrapper.gameObject.AddComponent<KitchenSet>();
        if(name=="Bed_MeshSlot")
        {
            // Existing PlayerBed keeps its pose references; place its collider at its normalized visual.
            var bed=visual.GetComponent<PlayerBed>();if(bed!=null)
            {
                foreach(var c in visual.GetComponents<Collider>())Object.DestroyImmediate(c);
                var bc=visual.gameObject.AddComponent<BoxCollider>();bc.center=visual.InverseTransformPoint(bounds.center);bc.size=new Vector3(bounds.size.x/visual.lossyScale.x,bounds.size.y/visual.lossyScale.y,bounds.size.z/visual.lossyScale.z);
                foreach(Transform c in visual)if(c.name.Contains("Pose")||c.name.Contains("StandPoint"))c.SetParent(wrapper,true);
            }
        }
        return wrapper;
    }
    static Transform Fit(Transform source,Transform parent,string name,Vector3 center,Vector3 size)
    {
        var t=Clone(source,parent,name);var b=BoundsOf(t);
        if((size.x>size.z)!=(b.size.x>b.size.z) && size.y>1)t.Rotate(0,90,0,Space.World);
        b=BoundsOf(t);Vector3 ratio=new(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
        float Axis(Vector3 axis)=>Vector3.Dot(new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)),ratio);
        t.localScale=Vector3.Scale(t.localScale,new Vector3(Axis(t.right),Axis(t.up),Axis(t.forward)));
        b=BoundsOf(t);t.position+=center-b.center;return t;
    }
    static void Wall(Transform parent,Vector3 center,Vector3 size)
    {
        Fit(Find("TFP_House_01A_Interior_Wall_Base_Regular_1"),parent,"Wall_Editable",center,size);
        Box(parent,"WallCollider_Editable",center,size);
    }
    static void Box(Transform parent,string name,Vector3 center,Vector3 size)
    {var t=Group(parent,name);t.position=center;t.gameObject.AddComponent<BoxCollider>().size=size;}
}
