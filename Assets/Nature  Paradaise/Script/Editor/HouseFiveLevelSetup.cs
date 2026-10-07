using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authors five editable interiors once. Running Play never rebuilds furniture.</summary>
public static class HouseFiveLevelSetup
{
    const string ScenePath="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
    const string AssetRoot="Assets/Nature  Paradaise/Prefabs/House/InteriorMaterials";
    static Material wood,plaster,trim,metal,glass,green,pink;
    static Transform floorSource,chestSource,fridgeSource;
    static Dictionary<string,string> prefabPaths;
    [MenuItem("Nature Paradise/House/Apply Five Reference Layouts")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var house=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        if(house.transform.Find("FiveReferenceLayouts_Authored_V2")!=null)
        {
            Polish(house);OpenBedExit(house);HouseReferenceLayoutRevision.Apply(house);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return;
        }
        if(house.transform.Find("FiveReferenceLayouts_Authored_V1")!=null)
        {
            foreach(Transform t in house.transform.Cast<Transform>().Where(t=>t.name.StartsWith("InteriorLayout_Lv")).ToArray())Object.DestroyImmediate(t.gameObject);
            var previous=house.transform.Find("PreviousFourLayouts_Reference_Inactive");
            foreach(Transform t in previous.Cast<Transform>().ToArray()){t.SetParent(house.transform,true);t.name=t.name.Replace("_Previous","");}
            Object.DestroyImmediate(previous.gameObject);Object.DestroyImmediate(house.transform.Find("FiveReferenceLayouts_Authored_V1").gameObject);
        }
        // Recover an interrupted, unsaved authoring run before retrying.
        if(house.transform.Find("PreviousFourLayouts_Reference_Inactive")!=null)
        {
            EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            house=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<HouseInteriorController>(true)).First();
        }
        if(!File.Exists("Library/HouseInterior_BeforeFiveReferenceLayouts.unity"))File.Copy(ScenePath,"Library/HouseInterior_BeforeFiveReferenceLayouts.unity");
        var old=house.GetComponentsInChildren<Transform>(true);
        floorSource=old.First(t=>t.name.StartsWith("WoodFloor_"));
        chestSource=old.First(t=>t.name=="ToolStorageChest_Editable");
        fridgeSource=old.First(t=>t.name=="Refrigerator_MeshSlot");
        prefabPaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs"})
            .Select(AssetDatabase.GUIDToAssetPath).GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g=>g.Key,g=>g.First());
        Directory.CreateDirectory(AssetRoot);AssetDatabase.Refresh();
        wood=Mat("Warm Timber",new Color(.32f,.17f,.08f));plaster=Mat("Warm Plaster",new Color(.78f,.69f,.54f));
        trim=Mat("Dark Frame",new Color(.12f,.11f,.09f));metal=Mat("Radio Metal",new Color(.55f,.53f,.44f));
        green=Mat("Sage Fabric",new Color(.42f,.57f,.35f));pink=Mat("Child Fabric",new Color(.78f,.47f,.5f));
        glass=Mat("Aquarium Glass",new Color(.12f,.65f,.8f,.32f));
        glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
        // Existing edited layouts remain recoverable in the scene, with components inactive.
        var archive=Group(house.transform,"PreviousFourLayouts_Reference_Inactive");
        foreach(Transform t in house.transform.Cast<Transform>().ToArray())
            if(t.name.StartsWith("InteriorLayout_Lv")){t.SetParent(archive,true);t.name+="_Previous";}
        archive.gameObject.SetActive(false);
        var layouts=new List<GameObject>();var entries=new List<Transform>();var exits=new List<Transform>();var views=new List<Camera>();
        for(int level=1;level<=5;level++)
        {
            float w=level==1?10:level==2?13:level==3?18:level==4?22:25;
            float d=level==1?9:level==2?11:level==3?13:level==4?16:18;
            Transform layout=Group(house.transform,$"InteriorLayout_Lv{level}_Editable");layouts.Add(layout.gameObject);
            Transform structure=Group(layout,"Structure_Editable");
            for(float x=0;x<w;x+=4)for(float z=0;z<d;z+=4)
            {
                float tw=Mathf.Min(4,w-x),td=Mathf.Min(4,d-z);
                var floor=CloneVisual(floorSource,structure,$"WoodFloor_{x}_{z}_Editable");Fit(floor,new Vector3(x+tw*.5f,-.08f,z+td*.5f),new Vector3(tw,.16f,td));
            }
            Box(structure,"ContinuousFloorCollider_Editable",new Vector3(w*.5f,-.18f,d*.5f),new Vector3(w,.3f,d),null,true);
            Wall(structure,new Vector3(w*.5f,1.7f,d),new Vector3(w,3.4f,.22f));
            Wall(structure,new Vector3(0,1.7f,d*.5f),new Vector3(.22f,3.4f,d));
            Wall(structure,new Vector3(w,1.7f,d*.5f),new Vector3(.22f,3.4f,d));
            // Front cutaway stays low; the collision boundary still prevents walking into the void.
            float doorX=w*.5f;
            Wall(structure,new Vector3((doorX-1.3f)*.5f,.22f,0),new Vector3(doorX-1.3f,.44f,.22f));
            Wall(structure,new Vector3((w+doorX+1.3f)*.5f,.22f,0),new Vector3(w-doorX-1.3f,.44f,.22f));
            foreach(float x in new[]{0f,w})foreach(float z in new[]{0f,d})Box(structure,"TimberPost_Editable",new Vector3(x,1.7f,z),new Vector3(.3f,3.5f,.3f),wood,false);
            Box(structure,"BackBeam_Editable",new Vector3(w*.5f,3.35f,d-.1f),new Vector3(w,.22f,.3f),wood,false);
            Window(structure,new Vector3(level<=2?w*.52f:w*.33f,1.95f,d-.14f));
            if(level>=2)Window(structure,new Vector3(w-3,1.95f,d-.14f));
            Transform living=Group(layout,"LivingBedroom_Editable");
            var bed=Furniture(living,level>=4?"TFP_Bed_02A":"TFP_Bed_01A","Bed_MeshSlot",new Vector3(2.2f,0,d-2.4f),new Vector3(level>=4?2.8f:1.9f,1.15f,3.5f),90);
            // Pack beds run along X with the headboard on the left; rotate so the footboard faces -Z.
            var bedComponent=bed.gameObject.AddComponent<PlayerBed>();
            var sleep=Group(bed,"SleepPose_Editable");sleep.position=new Vector3(BoundsOf(bed).center.x,house.transform.position.y-.32f,BoundsOf(bed).center.z);sleep.rotation=Quaternion.Euler(0,180,0);
            var wake=Group(bed,"WakeStandPoint_Editable");wake.position=new Vector3(BoundsOf(bed).center.x,house.transform.position.y+1.15f,BoundsOf(bed).min.z-1.3f);wake.rotation=Quaternion.Euler(0,180,0);
            var bedData=new SerializedObject(bedComponent);bedData.FindProperty("sleepPose").objectReferenceValue=sleep;bedData.FindProperty("wakeStandPoint").objectReferenceValue=wake;bedData.FindProperty("interactionRadius").floatValue=1.3f;bedData.ApplyModifiedPropertiesWithoutUndo();
            var night=Furniture(living,"TFP_Nightstand_01A","SaveNightstand_Editable",new Vector3(level>=4?4.4f:4f,0,d-1.1f),new Vector3(1.05f,1.05f,.9f));
            float top=BoundsOf(night).max.y-house.transform.position.y;
            var book=Furniture(living,"TFP_Books_02A","SaveBook_Editable",new Vector3(night.localPosition.x,top,d-1.1f),new Vector3(.65f,.18f,.5f));book.gameObject.AddComponent<HouseSaveBook>();
            var chest=CloneVisual(chestSource,living,"StorageChest_Editable");Fit(chest,new Vector3(w-1.3f,.55f,2.1f),new Vector3(1.7f,1.1f,1.1f));AddCollider(chest);chest.gameObject.AddComponent<HouseStorageChest>();
            Transform dining=Group(layout,"Dining_Editable");float tx=level<=2?w-3.5f:w*.64f,tz=level<=2?d*.43f:4;
            Furniture(dining,"TFP_Wooden_Table_02A","DiningTable_Editable",new Vector3(tx,0,tz),new Vector3(2.3f,1.15f,1.8f));
            Furniture(dining,"TFP_Wooden_Chair_02A","DiningChair_1_Editable",new Vector3(tx-1.8f,0,tz),new Vector3(.8f,1.55f,.85f),0);
            Furniture(dining,"TFP_Wooden_Chair_02A","DiningChair_2_Editable",new Vector3(tx+1.8f,0,tz),new Vector3(.8f,1.55f,.85f),180);
            if(level>=2)
            {
                Furniture(dining,"TFP_Wooden_Chair_02A","DiningChair_3_Editable",new Vector3(tx,0,tz-1.6f),new Vector3(.8f,1.55f,.85f),-90);
                Furniture(dining,"TFP_Wooden_Chair_02A","DiningChair_4_Editable",new Vector3(tx,0,tz+1.6f),new Vector3(.8f,1.55f,.85f),90);
            }
            if(level==1)Radio(living,new Vector3(w-2,0,d-1.2f));
            else
            {
                float tvX=level==2?7.4f:2.6f,tvZ=level==2?d-1.2f:5.9f;
                var stand=Furniture(living,"TFP_TV_Stand_01B","TVStand_Editable",new Vector3(tvX,0,tvZ),new Vector3(level==5?3.2f:2.5f,.85f,.95f));
                var tv=Furniture(living,"TFP_TV_01A",level==5?"TV_Max_Editable":"TV_MeshSlot",new Vector3(tvX,BoundsOf(stand).max.y-house.transform.position.y,tvZ),new Vector3(level==5?2.6f:1.8f,level==5?1.5f:1.05f,.45f));tv.gameObject.AddComponent<WeatherForecastTV>();
                var fridge=CloneVisual(fridgeSource,living,level==5?"Refrigerator_Max_Editable":"Refrigerator_MeshSlot");fridge.GetChild(0).Rotate(0,90,0);Fit(fridge,new Vector3(w-1.3f,level==5?1.6f:1.3f,d-1.1f),new Vector3(level==5?1.8f:1.4f,level==5?3.2f:2.6f,1.25f));AddCollider(fridge);fridge.gameObject.AddComponent<Refrigerator>();
            }
            if(level>=3)
            {
                var bedroom=Group(layout,"Bedroom_Editable");
                float endX=6.2f;
                Partition(bedroom,new Vector3(endX,1.7f,d-2.6f),new Vector3(.2f,3.4f,5.2f));
                Partition(bedroom,new Vector3(2.1f,.65f,d-5.2f),new Vector3(4.2f,1.3f,.2f));
                Furniture(living,"TFP_Wardrobe_01B","ToolCabinet_Editable",new Vector3(5,0,d-1.1f),new Vector3(1.5f,2.6f,.9f),90).gameObject.AddComponent<HouseStorageChest>();
                Kitchen(layout,w,d,level);
                var lounge=Group(layout,"Lounge_Editable");
                Furniture(lounge,"TFP_Couch_01E","Sofa_Editable",new Vector3(4.6f,0,3.2f),new Vector3(3.7f,1.5f,1.25f),-90);
                Furniture(lounge,"TFP_Armchair_01E","Armchair_Editable",new Vector3(1.7f,0,2.6f),new Vector3(1.35f,1.35f,1.35f),90);
                Furniture(lounge,"TFP_Wooden_Table_02A","CoffeeTable_Editable",new Vector3(4.6f,0,1.35f),new Vector3(2f,.65f,1f));
                Furniture(lounge,"TFP_Rug_03A","LoungeRug_Editable",new Vector3(4.3f,.01f,2.9f),new Vector3(6,.025f,4.8f),0,false);
                AquariumFurniture(living,new Vector3(level==5?6.8f:5.7f,0,6.5f),level);
            }
            if(level>=4)ChildRoom(layout,d);
            var entry=Group(layout,"EntryPoint_Editable");entry.localPosition=new Vector3(doorX,1.15f,1.4f);entries.Add(entry);
            var exit=Group(layout,"ExitPoint_Editable");exit.localPosition=new Vector3(doorX,1.2f,0);exits.Add(exit);
            var camera=Group(layout,"InteriorPerspectiveCamera_Editable").gameObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.21f,.27f,.32f);camera.nearClipPlane=.1f;camera.farClipPlane=200;
            camera.transform.localRotation=Quaternion.Euler(48,-14,0);
            camera.transform.localPosition=new Vector3(w*.5f,1,d*.5f)-camera.transform.forward*(Mathf.Max(w*.94f,d*1.38f)+3);views.Add(camera);
        }
        house.ConfigureAuthoredLevels(layouts,entries,exits);house.GetComponent<HouseInteriorView>().Configure(views);
        ConfigureDefinition();Group(house.transform,"FiveReferenceLayouts_Authored_V1");Group(house.transform,"FiveReferenceLayouts_Authored_V2");house.PreviewLayout(1);Polish(house);OpenBedExit(house);HouseReferenceLayoutRevision.Apply(house);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=house.gameObject;
    }
    static void OpenBedExit(HouseInteriorController house)
    {
        if(house.transform.Find("FiveReferenceBedExit_Authored_V1")!=null)return;
        for(int level=3;level<=5;level++)
        {
            var bedroom=house.transform.Find($"InteriorLayout_Lv{level}_Editable/Bedroom_Editable");
            foreach(Transform wall in bedroom)
            {
                if(Mathf.Abs(wall.localPosition.x-2.1f)>.01f)continue;
                var position=wall.localPosition;position.x=4.8f;wall.localPosition=position;
                var collider=wall.GetComponent<BoxCollider>();
                if(collider!=null){var size=collider.size;size.x=2.8f;collider.size=size;}
                var visual=wall.Find("Visual");
                if(visual!=null){var size=visual.localScale;size.x=2.8f;visual.localScale=size;}
            }
        }
        Group(house.transform,"FiveReferenceBedExit_Authored_V1");
    }
    static void Polish(HouseInteriorController house)
    {
        if(house.transform.Find("FiveReferenceDecor_Authored_V1")!=null)return;
        prefabPaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs"})
            .Select(AssetDatabase.GUIDToAssetPath).GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g=>g.Key,g=>g.First());
        for(int level=1;level<=5;level++)
        {
            house.PreviewLayout(level);
            var layout=house.transform.Find($"InteriorLayout_Lv{level}_Editable");
            var dining=layout.Find("Dining_Editable/DiningTable_Editable");var tb=BoundsOf(dining);
            Furniture(layout,"TFP_Flower_Pot_04A","DiningFlowers_Editable",house.transform.InverseTransformPoint(new Vector3(tb.center.x,tb.max.y,tb.center.z)),new Vector3(.45f,.65f,.45f),0,false);
            if(level>=2)
            {
                var fridge=layout.GetComponentInChildren<Refrigerator>();
                fridge.transform.RotateAround(BoundsOf(fridge.transform).center,Vector3.up,180);
            }
            if(level>=3)
            {
                var sofa=layout.Find("Lounge_Editable/Sofa_Editable");sofa.RotateAround(BoundsOf(sofa).center,Vector3.up,180);
                Furniture(layout,"TFP_Flower_Pot_08A","CornerPlant_Editable",new Vector3(level==3?8:13.5f,0,level==3?11.8f:level==4?14.8f:16.8f),new Vector3(.8f,1.4f,.8f),0,false);
            }
            var entry=layout.Find("EntryPoint_Editable");
            var threshold=Group(layout,"EntranceThreshold_Editable");threshold.localPosition=new Vector3(entry.localPosition.x,-.16f,-.55f);
            threshold.gameObject.AddComponent<BoxCollider>().size=new Vector3(2.6f,.3f,1.4f);
        }
        Group(house.transform,"FiveReferenceDecor_Authored_V1");house.PreviewLayout(1);
    }
    static void ConfigureDefinition()
    {
        var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>("Assets/Nature  Paradaise/Resources/Buildings/Player House Building.asset");
        definition.GetLevel(1).unlockIds=new List<string>{"house.bed","house.save-book","house.radio","house.basic-storage","house.dining-area"};
        definition.GetLevel(2).unlockIds=new List<string>{"house.tv","house.refrigerator"};
        definition.GetLevel(3).unlockIds=new List<string>{"house.kitchen","house.aquarium","house.tool-cabinet","house.large-living-room","house.furniture-tier-3"};
        definition.GetLevel(4).unlockIds=new List<string>{"house.master-bedroom","house.jumbo-bed","house.child-room","house.marriage-ready","house.furniture-tier-4"};
        if(definition.GetLevel(5)==null)definition.levels.Add(new BuildingLevelDefinition{level=5,requiredVillageLevel=4,goldCost=4000,constructionDays=10,capacity=64,materialCosts=definition.GetLevel(4).materialCosts.Select(c=>new BuildingMaterialCost{item=c.item,amount=c.amount*2}).ToList(),unlockIds=new List<string>{"house.tv-max","house.refrigerator-max","house.aquarium-jumbo","house.furniture-tier-5"}});
        EditorUtility.SetDirty(definition);
    }
    static void Kitchen(Transform layout,float w,float d,int level)
    {
        var group=Group(layout,"Kitchen_Editable");float left=w-7.4f;
        var stove=Furniture(group,"TFP_Kitchen_Cabinet_Range_01A","Kitchen_MeshSlot",new Vector3(left+1,0,d-1.15f),new Vector3(2,1.3f,1.3f));stove.gameObject.AddComponent<KitchenSet>();
        Furniture(group,"TFP_Kitchen_Cabinet_Sink_01A","KitchenSink_Editable",new Vector3(left+3.2f,0,d-1.15f),new Vector3(2,1.3f,1.3f));
        Furniture(group,"TFP_Kitchen_Cabinet_01A","KitchenCounter_Editable",new Vector3(left+4.9f,0,d-1.15f),new Vector3(1.2f,1.3f,1.3f));
        Furniture(group,"TFP_Kitchen_Island_01A","KitchenIsland_Editable",new Vector3(left+2.5f,0,d-4.2f),new Vector3(level>=4?4.4f:3.5f,1.3f,1.3f));
    }
    static void ChildRoom(Transform layout,float d)
    {
        var room=Group(layout,"ChildRoom_Preparation_Editable");
        Partition(room,new Vector3(7.2f,1.7f,d-2.7f),new Vector3(.2f,3.4f,5.4f));
        Partition(room,new Vector3(12,1.7f,d-2.7f),new Vector3(.2f,3.4f,5.4f));
        Partition(room,new Vector3(8.1f,.65f,d-5.4f),new Vector3(1.8f,1.3f,.2f));
        Partition(room,new Vector3(11.5f,.65f,d-5.4f),new Vector3(1,1.3f,.2f));
        Furniture(room,"TFP_Bed_01B","ChildBed_Preparation_Editable",new Vector3(8.8f,0,d-2.4f),new Vector3(1.45f,.9f,2.9f),90);
        Furniture(room,"TFP_Wooden_Table_02A","ChildDesk_Editable",new Vector3(10.5f,0,d-1f),new Vector3(1.4f,1.05f,.85f));
        Furniture(room,"TFP_Wooden_Chair_02A","ChildChair_Editable",new Vector3(10.5f,0,d-2.5f),new Vector3(.65f,1.2f,.7f));
        Furniture(room,"TFP_Rug_03B","ChildRug_Editable",new Vector3(9.7f,.01f,d-3.4f),new Vector3(3.3f,.025f,2.5f),0,false);
    }
    static void Radio(Transform parent,Vector3 position)
    {
        var stand=Furniture(parent,"TFP_TV_Stand_01A","RadioStand_Editable",position,new Vector3(1.8f,.85f,.9f));
        var radio=Group(parent,"WeatherRadio_Editable");radio.localPosition=position+Vector3.up*(BoundsOf(stand).max.y-parent.position.y);
        Box(radio,"WoodCase",new Vector3(0,.35f,0),new Vector3(1.15f,.7f,.42f),wood,true);
        Box(radio,"SpeakerGrille",new Vector3(-.25f,.34f,-.23f),new Vector3(.5f,.45f,.04f),trim,false);
        for(int i=0;i<5;i++)Box(radio,"GrilleSlat",new Vector3(-.25f,.19f+i*.075f,-.26f),new Vector3(.45f,.025f,.02f),metal,false);
        Box(radio,"TunerDisplay",new Vector3(.3f,.45f,-.23f),new Vector3(.3f,.13f,.03f),metal,false);
        Box(radio,"TunerKnob",new Vector3(.3f,.23f,-.26f),new Vector3(.14f,.14f,.08f),trim,false);
        Box(radio,"Antenna",new Vector3(.35f,1,0),new Vector3(.025f,.65f,.025f),metal,false);
        var body=radio.gameObject.AddComponent<BoxCollider>();body.center=new Vector3(0,.35f,0);body.size=new Vector3(1.15f,.7f,.42f);
        var forecast=radio.gameObject.AddComponent<WeatherForecastTV>();var data=new SerializedObject(forecast);data.FindProperty("isRadio").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void AquariumFurniture(Transform parent,Vector3 position,int level)
    {
        bool jumbo=level==5;float width=jumbo?3.6f:2.1f;
        var stand=Furniture(parent,"TFP_TV_Stand_01B","AquariumStand_Editable",position,new Vector3(width,.95f,1.1f));
        var tank=Group(parent,jumbo?"Aquarium_Jumbo_Editable":"Aquarium_Editable");tank.localPosition=position+Vector3.up*1.65f;tank.localScale=new Vector3(width,1.3f,1f);
        Box(tank,"GlassTank_Editable",Vector3.zero,new Vector3(1,1,1),glass,false);
        foreach(float x in new[]{-.5f,.5f})Box(tank,"TopFrame_Editable",new Vector3(x,.48f,0),new Vector3(.04f,.045f,1.04f),trim,false);
        foreach(float z in new[]{-.5f,.5f})Box(tank,"TopFrame_Editable",new Vector3(0,.48f,z),new Vector3(1.04f,.045f,.04f),trim,false);
        Box(tank,"BottomFrame_Editable",new Vector3(0,-.48f,0),new Vector3(1.04f,.045f,1.04f),trim,false);
        tank.gameObject.AddComponent<BoxCollider>();tank.gameObject.AddComponent<Aquarium>().Configure("house.aquarium.test.main",jumbo?AquariumSize.Grand:AquariumSize.Medium);
    }
    static void Window(Transform parent,Vector3 center)
    {
        Box(parent,"WindowPane_Editable",center,new Vector3(1.7f,1.5f,.04f),green,false);
        foreach(float x in new[]{-.9f,0,.9f})Box(parent,"WindowFrame_Editable",center+Vector3.right*x,new Vector3(.1f,1.6f,.1f),wood,false);
        foreach(float y in new[]{-.8f,0,.8f})Box(parent,"WindowFrame_Editable",center+Vector3.up*y,new Vector3(1.9f,.1f,.1f),wood,false);
    }
    static void Wall(Transform parent,Vector3 center,Vector3 size)
    {
        Box(parent,"Wall_Editable",center,size,plaster,true);
        Box(parent,"Skirting_Editable",new Vector3(center.x,.12f,center.z),new Vector3(size.x,.22f,size.z),wood,false);
    }
    static void Partition(Transform parent,Vector3 center,Vector3 size)=>Wall(parent,center,size);
    static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Mat(string name,Color color)
    {
        string path=AssetRoot+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=ToonWorldStyle.CreateMaterial(color);AssetDatabase.CreateAsset(m,path);}m.color=color;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(m);return m;
    }
    static Transform Box(Transform parent,string name,Vector3 center,Vector3 size,Material material,bool collider)
    {
        var t=Group(parent,name);t.localPosition=center;
        if(material!=null){var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.name="Visual";mesh.transform.SetParent(t,false);mesh.transform.localScale=size;mesh.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(mesh.GetComponent<Collider>());}
        if(collider)t.gameObject.AddComponent<BoxCollider>().size=size;return t;
    }
    static Transform Furniture(Transform parent,string prefab,string name,Vector3 bottom,Vector3 size,float yaw=90,bool collider=true)
    {
        var wrapper=Group(parent,name);var visual=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[prefab]),wrapper)).transform;
        visual.localRotation=Quaternion.Euler(0,yaw,0);Strip(visual);Fit(wrapper,bottom+Vector3.up*size.y*.5f,size);if(collider)AddCollider(wrapper);return wrapper;
    }
    static Transform CloneVisual(Transform source,Transform parent,string name)
    {
        var wrapper=Group(parent,name);var visual=Object.Instantiate(source.gameObject,wrapper).transform;
        visual.localPosition=Vector3.zero;visual.localRotation=source.localRotation;visual.localScale=source.lossyScale;visual.gameObject.SetActive(true);Strip(visual);return wrapper;
    }
    static void Strip(Transform t){foreach(var c in t.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var c in t.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);}
    static Bounds BoundsOf(Transform t){var rr=t.GetComponentsInChildren<Renderer>(true);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
    static void Fit(Transform t,Vector3 center,Vector3 size)
    {
        var b=BoundsOf(t);t.localScale=Vector3.Scale(t.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));b=BoundsOf(t);t.position+=t.parent.TransformPoint(center)-b.center;
        // Normalize the wrapper pivot while preserving mesh positions.
        Vector3 target=t.parent.TransformPoint(new Vector3(center.x,center.y-size.y*.5f,center.z));Vector3 delta=t.position-target;
        t.position=target;foreach(Transform child in t)child.position+=delta;
    }
    static void AddCollider(Transform t){var b=BoundsOf(t);var c=t.gameObject.AddComponent<BoxCollider>();c.center=t.InverseTransformPoint(b.center);c.size=new Vector3(b.size.x/t.lossyScale.x,b.size.y/t.lossyScale.y,b.size.z/t.lossyScale.z);}
}
