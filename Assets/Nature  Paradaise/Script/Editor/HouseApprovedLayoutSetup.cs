using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Explicit editor authoring from the user's approved floor plans; no runtime furniture movement.</summary>
public static class HouseApprovedLayoutSetup
{
    const string ManifestPath="Documentation/Design/HouseLayoutReview/proposed-layouts.json";
    const string Output="Documentation/Design/HouseLayoutReview";
    const string ScenePath="Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity";
    const string Marker="ApprovedLayout_A_2026_10_10";
    [Serializable] public class Manifest { public string revision;public Level[] levels; }
    [Serializable] public class Level { public int level;public float width,depth;public Item[] items;public float[] entryPoint,exitPoint,wakePoint; }
    [Serializable] public class Item { public string id,label,sourcePath,kind,layer;public float[] center,boundsSize,facing; }
    // JsonUtility doesn't support jagged arrays. Route geometry is read from a small flat companion,
    // exported from the exact same approved manifest rather than reconstructed from screenshots.
    [Serializable] public class Routes {public RouteLine[] lines;}
    [Serializable] public class RouteLine {public int level;public string id;public float width;public Vector2[] points;}
    static Manifest Read()=>JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
    public static HouseInteriorController House()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        return scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<HouseInteriorController>(true)).First();
    }
    public static Bounds Bounds(Transform t)
    {
        var rr=t.GetComponentsInChildren<Renderer>(true).Where(x=>x.enabled).ToArray();
        if(rr.Length==0)throw new Exception("Missing renderer: "+t.name);
        var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    static bool HasFace(Item i)=>i.facing!=null && i.facing.Length==2;
    // Native four-side review: Toon Farm furniture fronts / bed footboard run along prefab +X.
    // Custom aquarium is symmetric. Its authored front is -Z.
    static Vector3 Front(Transform t,Item i)=>i.id=="tank"?t.TransformDirection(Vector3.back):t.GetChild(0).TransformDirection(Vector3.right);
    static void Fit(Transform layout,Transform t,Item item)
    {
        Undo.RecordObjects(t.GetComponentsInChildren<Transform>(true).Cast<Object>().Concat(t.GetComponents<BoxCollider>().Cast<Object>()).ToArray(),"Apply approved house placement");
        var b=Bounds(t);
        if(HasFace(item))
        {
            var actual=Vector3.ProjectOnPlane(Front(t,item),Vector3.up).normalized;
            var desired=layout.TransformDirection(new Vector3(item.facing[0],0,item.facing[1]));
            t.RotateAround(b.center,Vector3.up,Vector3.SignedAngle(actual,desired,Vector3.up));
        }
        else if(Mathf.Abs(b.size.x-item.boundsSize[2])<.02f && Mathf.Abs(b.size.z-item.boundsSize[0])<.02f && Mathf.Abs(b.size.x-b.size.z)>.05f)
            t.RotateAround(b.center,Vector3.up,90);
        b=Bounds(t);
        var desiredSize=V(item.boundsSize);var ratio=new Vector3(desiredSize.x/b.size.x,desiredSize.y/b.size.y,desiredSize.z/b.size.z);
        // All original furniture is already fitted by the old authoring tool. Rotate its existing
        // silhouette and correct small footprint differences (stools / aquarium frame overhang).
        // The old chest wrapper was fitted sideways (drawers on its short edge).
        // Refit that model to the approved wide drawer-front footprint after turning it.
        if(item.id!="chest" && Mathf.Max(ratio.x,ratio.z)/Mathf.Min(ratio.x,ratio.z)>1.15f)
            throw new Exception("Unexpected footprint / facing mismatch for "+item.sourcePath+": "+b.size+" -> "+desiredSize);
        foreach(Transform child in t)
        {
            if(child.name.Contains("Pose")||child.name.Contains("StandPoint"))continue;
            // The wrapper can already have yaw and nonuniform scale. Express the world-axis
            // ratio in each child axis, avoiding swapping an existing scale a second time.
            Vector3 worldDelta=child.position-b.center;
            child.position=b.center+Vector3.Scale(worldDelta,ratio);
            float Along(Vector3 axis)=>Vector3.Dot(new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)),ratio);
            child.localScale=Vector3.Scale(child.localScale,new Vector3(Along(child.right),Along(child.up),Along(child.forward)));
        }
        b=Bounds(t);t.position+=layout.TransformPoint(V(item.center))-b.center;
        foreach(var c in t.GetComponents<BoxCollider>())
        {
            b=Bounds(t);var local=new Bounds(t.InverseTransformPoint(b.center),Vector3.zero);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                local.Encapsulate(t.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
            c.center=local.center;c.size=local.size;
        }
        EditorUtility.SetDirty(t);
    }
    [MenuItem("Nature Paradise/House/Apply Approved Layout A (Lv3-5)")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode before authoring house layout");
        var house=House();var manifest=Read();var scene=house.gameObject.scene;
        string backup="Library/HouseInterior_BeforeApprovedLayoutA.unity";
        if(!File.Exists(backup))File.Copy(ScenePath,backup);
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Approved house layout A");
        try
        {
            foreach(var level in manifest.levels)
            {
                var layout=house.transform.Find($"InteriorLayout_Lv{level.level}_Editable");
                foreach(var item in level.items)
                {
                    var t=layout.Find(item.sourcePath);if(t==null)throw new Exception("Missing approved furniture "+item.sourcePath);
                    Fit(layout,t,item);
                }
                var bed=layout.Find("LivingBedroom_Editable/Bed_MeshSlot");var bb=Bounds(bed);
                var sleep=bed.Find("SleepPose_Editable");var wake=bed.Find("WakeStandPoint_Editable");
                Undo.RecordObjects(new Object[]{sleep,wake},"Bed anchors");
                sleep.position=new Vector3(bb.center.x,bb.min.y-.32f,bb.center.z);sleep.rotation=Quaternion.Euler(0,180,0);
                wake.position=layout.TransformPoint(V(level.wakePoint));wake.rotation=Quaternion.Euler(0,180,0);
                var entry=layout.Find("EntryPoint_Editable");var exit=layout.Find("ExitPoint_Editable");
                Undo.RecordObjects(new Object[]{entry,exit},"Entrance anchors");entry.localPosition=V(level.entryPoint);exit.localPosition=V(level.exitPoint);
            }
            if(house.transform.Find(Marker)==null){var marker=new GameObject(Marker);Undo.RegisterCreatedObjectUndo(marker,"Layout approval marker");marker.transform.SetParent(house.transform,false);}
            house.PreviewLayout(3);Physics.SyncTransforms();
            Verify(manifest,house,true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
            Selection.activeGameObject=house.gameObject;
            Debug.Log("[HOUSE APPROVED] Layout A saved for Lv3-5; exact approved centers, footprints and fronts.");
        }
        catch{Undo.RevertAllDownToGroup(group);throw;}
    }
    static void Verify(Manifest manifest,HouseInteriorController house,bool write)
    {
        var log=new StringBuilder("APPROVED LAYOUT A — native Unity bounds validation\n");int checks=0;
        foreach(var level in manifest.levels)
        {
            house.PreviewLayout(level.level);Physics.SyncTransforms();
            var layout=house.transform.Find($"InteriorLayout_Lv{level.level}_Editable");
            foreach(var item in level.items)
            {
                var t=layout.Find(item.sourcePath);var b=Bounds(t);var center=layout.InverseTransformPoint(b.center);
                float error=Vector3.Distance(center,V(item.center)),sizeError=Vector3.Distance(b.size,V(item.boundsSize));
                if(error>.005f||sizeError>.012f)throw new Exception($"Lv{level.level} {item.id} mismatches sketch: center {error:F5}, size {sizeError:F5}");
                if(HasFace(item))
                {
                    var desired=layout.TransformDirection(new Vector3(item.facing[0],0,item.facing[1]));
                    if(Vector3.Angle(Vector3.ProjectOnPlane(Front(t,item),Vector3.up),desired)>.2f)throw new Exception("Facing mismatch "+item.id);
                }
                foreach(var c in t.GetComponents<BoxCollider>())if(!c.bounds.Contains(b.center))throw new Exception("Collider misplaced "+item.id);
                log.AppendLine($"Lv{level.level} {item.id}: center={center} size={b.size} facing=OK");checks++;
            }
        }
        log.AppendLine("PASS "+checks+" approved furniture centers / sizes / fronts. Gameplay checks are recorded separately.");
        house.PreviewLayout(3);Physics.SyncTransforms();
        if(write)File.WriteAllText(Output+"/applied-native-checks.txt",log.ToString());
    }
    [MenuItem("Nature Paradise/House/Verify Approved Layout A")]
    public static void VerifyApproved()=>Verify(Read(),House(),true);
}
