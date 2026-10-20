#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EggIncubatorValidation
{
    const string Output="ArtSource/EggIncubator";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] sealed class Bundle{public List<EggIncubatorSaveData> incubators;public AnimalSaveData animal;}
    static EggIncubatorValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        const string request=Output+"/verify.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        int pass=File.ReadAllText(request).Contains("PASS2")?2:1;
        File.Move(request,Output+"/Verify_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Run(pass);
    }
    [MenuItem("Nature Paradise/Animals/Validate Egg Incubator")]
    public static void RunMenu()=>Run(1);
    public static void Run(int pass)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder("EGG INCUBATOR — INDEPENDENT PASS "+pass+"\n");int checks=0;
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        var previousClock=TimeManager.Instance;var cache=EggIncubator.CaptureAll();var homes=new List<AnimalHome>();
        RenderTexture rt=null;var objects=new List<GameObject>();
        void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;report.AppendLine("PASS "+label);}
        GameObject Fixture(string name)
        {var o=new GameObject(name);SceneManager.MoveGameObjectToScene(o,scene);objects.Add(o);return o;}
        void Invoke(object target,string method)=>target.GetType().GetMethod(method,Flags).Invoke(target,null);
        Color32[] Capture(Camera camera,string file)
        {
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var before=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=before;
            File.WriteAllBytes(Output+"/"+file,image.EncodeToPNG());var pixels=image.GetPixels32();Object.DestroyImmediate(image);return pixels;
        }
        int Changed(Color32[] a,Color32[] b)
        {int count=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>20)count++;return count;}
        try
        {
            Directory.CreateDirectory(Output);EggIncubator.RestoreAll(null);
            var clock=Fixture("Fixture clock, not user clock").AddComponent<TimeManager>();TimeManager.Instance=clock;clock.day=10;
            var inv=Fixture("Fixture inventory").AddComponent<Inventory>();
            var egg=AnimalCareCatalog.Load().Product(AnimalType.Chicken);var duck=AnimalCareCatalog.Load().Product(AnimalType.Duck);
            var shop=Fixture("Fixture animal factory").AddComponent<ShopManager>();shop.playerInv=inv;
            var light=Fixture("Preview sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);
            var camera=Fixture("Actual prefab camera").AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=1.7f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.17f);
            rt=new RenderTexture(1000,800,24);rt.Create();
            for(int level=1;level<=4;level++)
            {
                clock.day=10;
                var layout=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Coop/Interior/CoopInteriorLayout_Lv"+level+".prefab"),scene);
                objects.Add(layout);var view=layout.GetComponentInChildren<EggIncubator>(true);
                Check(view!=null,"Lv"+level+" production prefab contains system, not a disconnected mock");
                var home=Fixture("Fixture coop "+level).AddComponent<AnimalHome>();home.homeId="incubator-test-"+pass+"-"+level;home.kind=AnimalHousingKind.Coop;home.capacity=1;
                home.door=home.transform;homes.Add(home);AnimalHome.Active.Add(home);view.Bind(home);
                Check(view.State==EggIncubatorState.Empty,"Lv"+level+" empty on new game");
                var single=view.transform.Find("Nest_EggCentre/SingleEgg_Visual");
                Check(single!=null&&!single.gameObject.activeSelf,"No decorative pre-filled eggs when empty");
                Check(layout.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Incubator_Geometry"||r.name=="GlassHood_Geometry").All(r=>!r.enabled),"Old baked egg/hood renderers disabled");
                var target=view.Nest.position;
                camera.transform.position=target+new Vector3(-1.4f,1.2f,-2.5f);camera.transform.LookAt(target+Vector3.up*.12f);
                var emptyImage=Capture(camera,$"Pass{pass}_Lv{level}_Empty.png");
                inv.slots.Clear();inv.Add(egg,3);inv.Add(duck,1);
                int eggSlot=inv.slots.FindIndex(s=>s?.item==egg),duckSlot=inv.slots.FindIndex(s=>s?.item==duck);
                Check(!view.TryInsert(inv,duckSlot,out _,shop)&&inv.GetCount(duck)==1,"Duck egg rejected without item loss");
                home.capacity=0;Check(!view.TryInsert(inv,eggSlot,out _,shop)&&inv.GetCount(egg)==3,"Full coop rejected without item loss");home.capacity=1;
                Check(view.TryInsert(inv,eggSlot,out var reason,shop),"One chicken egg accepted: "+reason);
                var record=view.Snapshot;var chick=EggIncubator.FindAnimal(record);
                Check(inv.GetCount(egg)==2&&view.State==EggIncubatorState.Incubating,"Exactly one egg consumed and timer started atomically");
                Check(chick!=null&&chick.Type==AnimalType.Chicken&&!chick.HasBeenBorn&&chick.IncubatorId==view.Id,"Correct chicken identity, unborn (not fallback cow)");
                Check(home.Residents.Count==1&&home.ReservedSlots==1&&!home.HasRoom(AnimalType.Chicken),"Real resident reserves last coop slot against other buyers");
                Check(!view.TryInsert(inv,eggSlot,out _,shop)&&inv.GetCount(egg)==2,"Repeated E cannot consume another egg");
                Check(record.readyDay==13&&view.RemainingDays==3,"Three calendar transitions, not three real-time days");
                view.RefreshVisual();Invoke(chick.GetComponent<AnimalRoutine>(),"LateUpdate");
                Check(single.gameObject.activeSelf&&chick.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Exactly one visible egg; prenatal animal visual hidden");
                var eggImage=Capture(camera,$"Pass{pass}_Lv{level}_Incubating.png");
                Check(Changed(emptyImage,eggImage)>30,"Actual egg visible in render, not occluded by wall");
                clock.day=12;EggIncubator.AdvanceAll(12);Check(view.RemainingDays==1&&!chick.HasBeenBorn,"One day left, no early hatch");
                var bundle=JsonUtility.FromJson<Bundle>(JsonUtility.ToJson(new Bundle{incubators=EggIncubator.CaptureAll(),animal=chick.Capture()}));
                view.Bind(null);clock.day=13;EggIncubator.AdvanceAll(13);
                Check(chick.HasBeenBorn,"Hatches while interior is unbound/unloaded");
                EggIncubator.RestoreAll(bundle.incubators);chick.Restore(bundle.animal);EggIncubator.AdvanceAll(13);view.Bind(home);
                Check(view.State==EggIncubatorState.ChickReady&&chick.HasBeenBorn&&chick.GrowthStage==AnimalGrowthStage.Hatchling,"Save/load catches up to correct hatchling stage");
                Check(view.Snapshot.animalId==chick.AnimalId&&home.Residents.Count==1,"Reload retains exactly the same animal ID and one resident");
                if(pass==2)
                {
                    // Fresh-session restore: destroy the instance, rebuild through the real
                    // animal factory, and bind another coop level to the exact same record.
                    var savedAnimal=chick.Capture();
                    Invoke(chick.GetComponent<AnimalRoutine>(),"OnDisable");Invoke(chick,"OnDisable");Object.DestroyImmediate(chick.gameObject);
                    chick=shop.RestoreAnimal(savedAnimal);SceneManager.MoveGameObjectToScene(chick.gameObject,scene);
                    Check(chick.AnimalId==record.animalId&&chick.GrowthStage==AnimalGrowthStage.Hatchling&&home.Residents.Count==1,"Fresh animal reconstruction uses real chicken profile, same ID and housing");
                    var alternate=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Coop/Interior/CoopInteriorLayout_Lv"+(level==4?1:4)+".prefab"),scene);
                    view.Bind(null);var other=alternate.GetComponentInChildren<EggIncubator>(true);other.Bind(home);
                    Check(other.State==EggIncubatorState.ChickReady&&other.Snapshot.animalId==chick.AnimalId,"Interior level change preserves one egg/chick record instead of resetting");
                    Object.DestroyImmediate(alternate);view.Bind(home);
                }
                view.RefreshVisual();Invoke(chick.GetComponent<AnimalRoutine>(),"LateUpdate");
                Check(!single.gameObject.activeSelf&&chick.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Ready shows chick instead of egg");
                var visibleBounds=chick.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).ToArray();
                float chickHeight=visibleBounds.Max(b=>b.max.y)-visibleBounds.Min(b=>b.min.y);
                Check(Mathf.Abs(chickHeight-.20f)<.02f,"Hatchling is legible at 20cm in nest, not a tiny dot");
                Check(chick.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Nest occupant cannot roam, collide or steal interaction input");
                var readyImage=Capture(camera,$"Pass{pass}_Lv{level}_Ready.png");
                Check(Changed(eggImage,readyImage)>100,"Actual hatchling/cover change visible in render");
                clock.day=30;Invoke(chick,"HandleDailyReset");EggIncubator.AdvanceAll(30);
                Check(chick.AgeDays==0&&chick.Health==AnimalHealthState.Healthy&&!home.Feed(chick),"Protected chick in nest does not starve, feed or age into adult");clock.day=13;
                Check(!view.TransferToPickup(_=>false,out _)&&view.State==EggIncubatorState.ChickReady,"Refused pickup leaves the exact chick in the nest");
                string id=chick.AnimalId;
                if(level%2==0)
                {AnimalGrowthSystem transferred=null;Check(view.TransferToPickup(a=>{transferred=a;return true;},out _)&&transferred==chick,"Pickup bridge transfers the existing chick, never spawns another");}
                else Check(view.TryRelease(out var released,out _)&&released==chick,"Release joins existing coop with same chick");
                Check(!chick.IsInIncubator&&view.State==EggIncubatorState.Empty&&chick.AnimalId==id&&home.AnimalCount==1,"Successful removal empties station but retains real owned chick");
                chick.GetComponent<AnimalRoutine>().enabled=false;
                Check(!home.HasRoom(AnimalType.Chicken),"Temporarily disabled/carried chick AI still reserves owned housing");
                chick.GetComponent<AnimalRoutine>().enabled=true;
                Check(!view.TryRelease(out _,out _)&&!view.TransferToPickup(_=>true,out _),"Repeated collect cannot duplicate a chick");
                Check(!view.TryInsert(inv,eggSlot,out _,shop)&&inv.GetCount(egg)==2,"Released chick still occupies capacity, no reservation bypass");
                Invoke(chick.GetComponent<AnimalRoutine>(),"OnDisable");Invoke(chick,"OnDisable");Object.DestroyImmediate(chick.gameObject);
                Object.DestroyImmediate(layout);objects.Remove(layout);AnimalHome.Active.Remove(home);
            }
            report.AppendLine("AUDIT_PASS checks="+checks+"; actual production prefabs, native transactions, JSON roundtrip and rendered states. Pickup transfer callback tested; full animated Play Mode pickup not simulated. No user save/scene writes.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);}
        finally
        {
            // Remove only fixture animals, never user animals or records.
            foreach(var animal in AnimalGrowthSystem.ActiveAnimals.ToArray())
                if(animal!=null&&animal.IncubatorId!=null&&animal.IncubatorId.Contains("incubator-test-"))
                {Invoke(animal.GetComponent<AnimalRoutine>(),"OnDisable");Invoke(animal,"OnDisable");Object.DestroyImmediate(animal.gameObject);}
            foreach(var home in homes)AnimalHome.Active.Remove(home);
            foreach(var o in objects)if(o!=null)Object.DestroyImmediate(o);
            TimeManager.Instance=previousClock;EggIncubator.RestoreAll(cache);
            if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);
            if(previous.IsValid())SceneManager.SetActiveScene(previous);
            File.WriteAllText(Output+"/Validation_Pass"+pass+".txt",report.ToString());
        }
    }
}
#endif
