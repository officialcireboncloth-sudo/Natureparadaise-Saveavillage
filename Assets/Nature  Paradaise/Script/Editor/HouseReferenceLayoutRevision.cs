using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>One-time correction of the authored reference layouts; never runs during gameplay.</summary>
public static class HouseReferenceLayoutRevision
{
    const string Marker="ReferenceRoomsAndFacing_Authored_V1";
    static Material plaster,wood;
    public static void Apply(HouseInteriorController house)
    {
        if(house.transform.Find(Marker)!=null)return;
        plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/Nature  Paradaise/Prefabs/House/InteriorMaterials/Warm Plaster.mat");
        wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Nature  Paradaise/Prefabs/House/InteriorMaterials/Warm Timber.mat");
        for(int lv=1;lv<=5;lv++)
        {
            house.PreviewLayout(lv);
            var layout=house.transform.Find($"InteriorLayout_Lv{lv}_Editable");
            float w=lv==1?10:lv==2?13:lv==3?18:lv==4?22:25;
            float d=lv==1?9:lv==2?11:lv==3?13:lv==4?16:18;
            var living=layout.Find("LivingBedroom_Editable");
            if(lv>=2)
            {
                var tv=layout.GetComponentInChildren<WeatherForecastTV>().transform;
                var bounds=Bounds(tv);var bottom=house.transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                ReplaceVisual(tv,"TFP_TV_01A",bottom,lv==5?2.6f:1.8f,90);
                var fridge=layout.GetComponentInChildren<Refrigerator>().transform;
                ReplaceVisual(fridge,"TFP_Fridge_01A",new Vector3(w-1.3f,0,d-1.1f),lv==5?1.65f:1.35f,90);
            }
            if(lv>=3)
            {
                // The bedroom is a complete room with a wide, footboard-aligned doorway.
                var bedroom=layout.Find("Bedroom_Editable");
                foreach(Transform child in bedroom.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                float extension=1.4f;
                Wall(bedroom,"BedroomSideWall_Editable",new Vector3(6.2f,1.7f,d-1.9f),new Vector3(.2f,3.4f,6.6f));
                Wall(bedroom,"BedroomFrontLeft_Editable",new Vector3(.45f,.65f,d-5.2f),new Vector3(.9f,1.3f,.2f));
                Wall(bedroom,"BedroomFrontRight_Editable",new Vector3(4.85f,.65f,d-5.2f),new Vector3(2.7f,1.3f,.2f));
                var structure=layout.Find("Structure_Editable");
                // Step the rear outline around the bedroom wing, instead of one huge rectangle.
                var oldBack=structure.Cast<Transform>().Where(t=>Mathf.Abs(t.localPosition.z-d)<.01f && (t.name=="Wall_Editable"||t.name=="Skirting_Editable")).ToArray();
                foreach(var t in oldBack)Object.DestroyImmediate(t.gameObject);
                float childExtension=lv>=4?2.2f:0;
                float rearStart=lv>=4?12:6.2f;
                Wall(structure,"LivingRearWall_Editable",new Vector3((rearStart+w)*.5f,1.7f,d),new Vector3(w-rearStart,3.4f,.22f));
                Wall(structure,"BedroomRearWall_Editable",new Vector3(3.1f,1.7f,d+extension),new Vector3(6.2f,3.4f,.22f));
                Wall(structure,"BedroomOuterExtension_Editable",new Vector3(0,1.7f,d+extension*.5f),new Vector3(.22f,3.4f,extension));
                Box(structure,"BedroomExtensionFloor_Editable",new Vector3(3.1f,-.18f,d+extension*.5f),new Vector3(6.2f,.3f,extension),wood,true);
                var extensionFloor=structure.Find("BedroomExtensionFloor_Editable");extensionFloor.name="ContinuousFloorCollider_Editable";
                foreach(string item in new[]{"Bed_MeshSlot","SaveNightstand_Editable","SaveBook_Editable","ToolCabinet_Editable"})
                {
                    var t=living.Find(item);t.localPosition+=Vector3.forward*extension;
                }
                // Keep wake at the clear doorway, not inside the front partition.
                var wake=living.Find("Bed_MeshSlot/WakeStandPoint_Editable");wake.position=house.transform.TransformPoint(new Vector3(2.2f,1.15f,d-5.65f));
                // U/L kitchen: a return counter joins the back line and front island.
                var kitchen=layout.Find("Kitchen_Editable");
                NewFurniture(kitchen,"TFP_Kitchen_Cabinet_01A","KitchenReturnCounter_Editable",new Vector3(w-7.2f,0,d-2.65f),1.25f,0);
                var island=kitchen.Find("KitchenIsland_Editable");island.localPosition=new Vector3(w-5.1f,0,d-4.2f);
                // Lounge sits directly below the bedroom; dining remains front-right.
                layout.Find("Lounge_Editable").localPosition+=Vector3.forward*1.1f;
                var tank=layout.GetComponentInChildren<Aquarium>().transform;
                tank.localPosition=new Vector3(lv==5?6.6f:5.6f,1.65f,d-6.8f);
                var stand=living.Find("AquariumStand_Editable");stand.localPosition=new Vector3(tank.localPosition.x,0,tank.localPosition.z);
                if(lv>=4)
                {
                    var room=layout.Find("ChildRoom_Preparation_Editable");
                    foreach(Transform t in room.Cast<Transform>().Where(t=>t.name=="Wall_Editable"||t.name=="Skirting_Editable").ToArray())Object.DestroyImmediate(t.gameObject);
                    // Shared bedroom wall on the left; complete child room, with a broad entry.
                    Wall(room,"ChildRoomSideWall_Editable",new Vector3(12,1.7f,d-1.6f),new Vector3(.2f,3.4f,7.6f));
                    Wall(room,"ChildFrontLeft_Editable",new Vector3(7.2f,.65f,d-5.4f),new Vector3(2,1.3f,.2f));
                    Wall(room,"ChildFrontRight_Editable",new Vector3(11.5f,.65f,d-5.4f),new Vector3(1,1.3f,.2f));
                    Wall(structure,"ChildRearWall_Editable",new Vector3(9.1f,1.7f,d+childExtension),new Vector3(5.8f,3.4f,.22f));
                    Wall(structure,"RearWingStep_Editable",new Vector3(6.2f,1.7f,d+1.8f),new Vector3(.2f,3.4f,.8f));
                    Wall(structure,"KitchenWingStep_Editable",new Vector3(12,1.7f,d+1.1f),new Vector3(.2f,3.4f,2.2f));
                    Box(structure,"ContinuousFloorCollider_Editable",new Vector3(9.1f,-.18f,d+1.1f),new Vector3(5.8f,.3f,2.2f),wood,true);
                    foreach(Transform t in room.Cast<Transform>().Where(t=>t.name.StartsWith("ChildBed")||t.name.StartsWith("ChildDesk")||t.name.StartsWith("ChildChair")||t.name.StartsWith("ChildRug")).ToArray())t.localPosition+=Vector3.forward*1.6f;
                }
                else Wall(structure,"BedroomWingStep_Editable",new Vector3(6.2f,1.7f,d+.7f),new Vector3(.2f,3.4f,1.4f));
                // Remove the obsolete straight rear beam and relocate bedroom window into its wing.
                foreach(Transform t in structure.Cast<Transform>().Where(t=>t.name=="BackBeam_Editable").ToArray())Object.DestroyImmediate(t.gameObject);
                foreach(Transform t in structure.Cast<Transform>().Where(t=>t.name.StartsWith("Window")&&t.localPosition.x<w*.5f).ToArray())t.localPosition+=Vector3.forward*(lv>=4?childExtension:extension);
            }
            var camera=layout.GetComponentInChildren<Camera>();camera.transform.localRotation=Quaternion.Euler(48,0,0);
            camera.transform.localPosition=new Vector3(w*.5f,1,d*.5f+.5f)-camera.transform.forward*(Mathf.Max(w*1.05f,(d+2.2f)*1.4f)+3);
        }
        var marker=new GameObject(Marker);marker.transform.SetParent(house.transform,false);house.PreviewLayout(1);
    }
    static Bounds Bounds(Transform t){var rr=t.GetComponentsInChildren<Renderer>(true);var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
    static string Prefab(string name)=>AssetDatabase.FindAssets(name+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>System.IO.Path.GetFileNameWithoutExtension(p)==name);
    static void ReplaceVisual(Transform wrapper,string prefab,Vector3 bottom,float width,float yaw)
    {
        foreach(Transform child in wrapper.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        wrapper.localPosition=Vector3.zero;wrapper.localRotation=Quaternion.identity;wrapper.localScale=Vector3.one;
        var visual=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(prefab)),wrapper)).transform;
        foreach(var c in visual.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var c in visual.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
        visual.localRotation=Quaternion.Euler(0,yaw,0);var b=Bounds(wrapper);visual.localScale*=width/b.size.x;b=Bounds(wrapper);
        wrapper.position=wrapper.parent.TransformPoint(bottom);
        b=Bounds(wrapper);visual.position+=wrapper.position-new Vector3(b.center.x,b.min.y,b.center.z);
        var collider=wrapper.GetComponent<BoxCollider>();if(collider==null)collider=wrapper.gameObject.AddComponent<BoxCollider>();b=Bounds(wrapper);collider.center=wrapper.InverseTransformPoint(b.center);collider.size=b.size;
    }
    static void NewFurniture(Transform parent,string prefab,string name,Vector3 bottom,float width,float yaw){var t=new GameObject(name).transform;t.SetParent(parent,false);ReplaceVisual(t,prefab,bottom,width,yaw);}
    static void Wall(Transform parent,string name,Vector3 pos,Vector3 size)=>Box(parent,name,pos,size,plaster,true);
    static void Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool collider)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;
        var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="Visual";visual.transform.SetParent(t,false);visual.transform.localScale=size;visual.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(visual.GetComponent<Collider>());
        if(collider)t.gameObject.AddComponent<BoxCollider>().size=size;
    }
}
