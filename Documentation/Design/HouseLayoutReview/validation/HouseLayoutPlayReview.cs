#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[DefaultExecutionOrder(-32000)]
public sealed class HouseLayoutPlayReview : MonoBehaviour
{
    [Serializable] class Routes { public Route[] lines; }
    [Serializable] class Route { public int level;public string id;public float width;public Vector2[] points; }
    readonly List<string> log=new();PlayerController player;CharacterController capsule;HouseInteriorController house;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(File.Exists("Library/HouseLayoutPlayReview.request"))new GameObject("TEMP House Play Review").AddComponent<HouseLayoutPlayReview>();}
    void Awake(){foreach(var save in FindObjectsByType<SaveManager>(FindObjectsSortMode.None))save.enabled=false;SaveManager.Instance=null;}
    IEnumerator Start()
    {
        var run=Run();bool done=false;
        while(!done)
        {
            object value=null;try{done=!run.MoveNext();if(!done)value=run.Current;}
            catch(Exception e){log.Add("FAIL "+e);done=true;}
            if(!done)yield return value;
        }
        File.WriteAllLines("Documentation/Design/HouseLayoutReview/applied-play-checks.txt",log);
        File.Delete("Library/HouseLayoutPlayReview.request");TimeManager.Instance?.ReleasePause(this);
        EditorApplication.isPlaying=false;
    }
    void Require(bool value,string message){if(!value)throw new Exception(message);log.Add("PASS "+message);}
    void Teleport(Vector3 target)
    {
        if(PlayerLifeCycle.TryResolveStandingPoint(target,capsule,house.transform,out var grounded))target=grounded;
        capsule.enabled=false;player.transform.position=target;Physics.SyncTransforms();capsule.enabled=true;
    }
    Vector3 Ground(Transform layout,Vector2 p)=>layout.TransformPoint(new Vector3(p.x,1.15f,p.y));
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(1);
        TimeManager.Instance?.AcquirePause(this);
        player=FindFirstObjectByType<PlayerController>();capsule=player.GetComponent<CharacterController>();
        Require(SceneTransitionManager.Instance.EnterInterior("HouseInterior","house-interior-entry"),"enter authored house through SceneTransitionManager");
        float deadline=Time.realtimeSinceStartup+20;
        while(SceneTransitionManager.Instance.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;
        Require(SceneTransitionManager.Instance.IsInsideInterior&&!SceneTransitionManager.Instance.IsTransitioning,"interior transition finished");
        house=FindObjectsByType<HouseInteriorController>(FindObjectsSortMode.None).First(x=>x.IsPlayerInside);
        var routes=JsonUtility.FromJson<Routes>(File.ReadAllText("Documentation/Design/HouseLayoutReview/approved-route-lines.json"));
        player.enabled=false;
        foreach(int level in new[]{3,4,5})
        {
            Require(house.SetDebugLayout(level),"activate Lv"+level);yield return null;
            var layout=house.transform.Find($"InteriorLayout_Lv{level}_Editable");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot($"Documentation/Design/HouseLayoutReview/level-{level}-applied.png");yield return null;
            foreach(var route in routes.lines.Where(x=>x.level==level))
            {
                for(int k=1;k<route.points.Length;k++)
                {
                    var a=route.points[k-1];var b=route.points[k];if(route.id=="entry"&&k==1)a.y=1.4f;
                    Teleport(Ground(layout,a));float y=player.transform.position.y;
                    var target=Ground(layout,b);target.y=y;int count=Mathf.CeilToInt(Vector3.Distance(player.transform.position,target)/.08f)+20;
                    for(int n=0;n<count;n++)
                    {
                        var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.025f)break;
                        capsule.Move(Vector3.ClampMagnitude(delta,.08f)+Vector3.down*.025f);
                        if(n%10==0)yield return null;
                    }
                    var remaining=target-player.transform.position;remaining.y=0;
                    Require(remaining.magnitude<.06f,$"Lv{level} CharacterController walks {route.id} segment {k}");
                }
            }
            float w=level==3?18:level==4?22:25,d=level==3?13:level==4?16:18;
            Teleport(layout.TransformPoint(new Vector3(2.2f,1.15f,d-4.1f)));yield return null;
            var bed=layout.Find("LivingBedroom_Editable/Bed_MeshSlot").GetComponent<PlayerBed>();
            capsule.Move(layout.TransformDirection(Vector3.forward)*.3f);yield return null;
            Require(bed.CanInteract(player.transform),$"Lv{level} bed reachable after approaching from approved wake point");
            bed.Sleep();yield return null;Require(BedRestMenu.IsOpen,$"Lv{level} bed menu opens");BedRestMenu.Instance.Close();yield return null;
            var kitchen=layout.GetComponentInChildren<KitchenSet>();
            Teleport(layout.TransformPoint(new Vector3(w-8.3f,1.15f,d-2.9f)));yield return null;
            Require(PlayerInteractionTarget.ContainsPickup(player.transform,kitchen.transform,2.5f),$"Lv{level} stove in interaction range");
            kitchen.OpenPanel();yield return null;Require(kitchen.IsPanelOpen,$"Lv{level} kitchen menu opens");KitchenUI.Instance.Close();yield return null;
            var tv=layout.GetComponentInChildren<WeatherForecastTV>();float lounge=level==3?3.2f:level==4?5.1f:6.6f;
            Teleport(layout.TransformPoint(new Vector3(2,1.15f,lounge+.3f)));yield return null;
            Require(PlayerInteractionTarget.ContainsPickup(player.transform,tv.transform,2.5f),$"Lv{level} TV reachable");
            tv.OpenTV();yield return null;Require(WeatherForecastTVUI.Instance!=null&&WeatherForecastTVUI.Instance.Owner==tv,$"Lv{level} TV menu opens");tv.CloseTV();yield return null;
            var tank=layout.GetComponentInChildren<Aquarium>();float az=level==3?6.8f:level==4?7:8;
            Teleport(layout.TransformPoint(new Vector3(w-2,1.15f,az)));yield return null;
            Require(PlayerInteractionTarget.ContainsPickup(player.transform,tank.transform,2.5f),$"Lv{level} aquarium reachable");
            tank.OpenPanel();yield return null;Require(AquariumUI.Active!=null,$"Lv{level} aquarium menu opens");tank.ClosePanel();yield return null;
            var chest=layout.GetComponentInChildren<HouseStorageChest>();
            Teleport(layout.TransformPoint(new Vector3(w-1.5f,1.15f,2.8f)));yield return null;
            Require(PlayerInteractionTarget.ContainsPickup(player.transform,chest.transform,2.2f),$"Lv{level} storage reachable");
            StorageChestUI.Show(chest);yield return null;Require(StorageChestUI.Instance!=null&&StorageChestUI.Instance.HouseChest==chest,$"Lv{level} storage menu opens");StorageChestUI.Instance.Close();yield return null;
            var fridge=layout.GetComponentInChildren<Refrigerator>();
            Teleport(layout.TransformPoint(new Vector3(w-1.3f,1.15f,d-2.9f)));yield return null;
            Require(PlayerInteractionTarget.ContainsPickup(player.transform,fridge.transform,2.2f),$"Lv{level} refrigerator reachable");
            StorageChestUI.Show(fridge);yield return null;Require(StorageChestUI.Instance!=null,$"Lv{level} refrigerator menu opens");StorageChestUI.Instance.Close();yield return null;
        }
        player.enabled=true;
        Require(SceneTransitionManager.Instance.ReturnToWorld("player-house-exit"),"return to world starts");deadline=Time.realtimeSinceStartup+20;
        while(SceneTransitionManager.Instance.IsTransitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Require(!SceneTransitionManager.Instance.IsInsideInterior,"return to world finishes");
        log.Add("No save, inventory transaction, sleep or progression changes performed.");
    }
}
#endif
