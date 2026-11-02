#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Save-restore regression using an isolated house and construction assets; never reads/writes gameplay save slots.</summary>
public static class HouseConstructionRestoreValidation
{
    const string Output="ArtSource/HouseConstructionRestore";
    static StringBuilder report;
    static int checks;
    static void Check(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);checks++;report.AppendLine("PASS "+message);}
    static int Screens(GameObject root)=>root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="SideTarp"&&t.gameObject.activeInHierarchy);
    static ConstructionJobData Job(ConstructionWorkerPhase phase)=>new ConstructionJobData
    {
        version=3,phase=phase,resumePhase=ConstructionWorkerPhase.Working,requiredWorkHours=18,completedWorkHours=4,
        lastClockHours=ConstructionJobData.ClockHours,departurePosition=new Vector3(10000,0,9980),
        workerPosition=new Vector3(10000,0,9985),workPosition=new Vector3(10000,0,9985)
    };

    public static void RunBatch()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/"))throw new InvalidOperationException("Use isolated review fixture.");
        Directory.CreateDirectory(Output);report=new StringBuilder("HOUSE CONSTRUCTION RESTORE — NATIVE UNITY\n");checks=0;
        GameObject owner=null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Check(SaveManager.Instance==null,"No live SaveManager; production save writes unavailable");
            var definition=Resources.Load<BuildingDefinitionSO>("Buildings/Player House Building");
            Check(definition!=null&&definition.GetLevel(1).completedPrefab!=null&&definition.GetLevel(2)!=null,"Real house Lv1 and upgrade assets resolve");
            owner=new GameObject("House restore regression");owner.transform.position=new Vector3(10000,0,10000);
            var house=owner.AddComponent<PlayerHouseController>();
            var fallback=new GameObject("Legacy construction fallback");fallback.transform.SetParent(owner.transform,false);
            house.Configure(definition,new System.Collections.Generic.List<GameObject>(),fallback);
            Check(house.CurrentLevel==1&&house.State==BuildingConstructionState.Completed&&house.Construction==null,"New game house already built at level 1");
            Check(Screens(owner)==0&&!fallback.activeSelf,"New house has no construction screens or fallback");
            void Built(string label,PlayerHouseSaveData data)
            {
                string input=JsonUtility.ToJson(data);int level=data.currentLevel;
                house.Restore(data);
                Check(house.State==BuildingConstructionState.Completed&&house.CurrentLevel==level,label+": completed state and house level retained");
                Check(house.Construction==null&&Screens(owner)==0&&!fallback.activeSelf,label+": no worker project, screens or fallback");
                Check(owner.transform.Find("PlayerHouse_Lv"+level+"_Runtime")!=null,label+": completed exterior remains visible");
                Check(JsonUtility.ToJson(data)==input,label+": loaded save object is not modified");
            }
            var actual=JsonUtility.FromJson<PlayerHouseSaveData>(File.ReadAllText(Output+"/Slot1House.json"));
            Check(actual.currentLevel==1&&actual.pendingLevel==0&&actual.state==BuildingConstructionState.Completed,"Read-only Slot 1 house snapshot reproduces the reported state");
            Check(actual.constructionJob!=null&&actual.constructionJob.requiredWorkHours==0&&!actual.constructionJob.ShouldRestoreFor(actual.state),"Slot 1 contains an empty job, rejected as construction");
            Built("Actual Slot 1 payload",actual);
            Built("Legacy missing job",new PlayerHouseSaveData{currentLevel=1,state=BuildingConstructionState.Completed});
            Built("JSON empty object",JsonUtility.FromJson<PlayerHouseSaveData>("{\"currentLevel\":1,\"state\":2,\"constructionJob\":{}}"));
            Built("Stale working phase on completed house",new PlayerHouseSaveData{currentLevel=2,state=BuildingConstructionState.Completed,constructionJob=Job(ConstructionWorkerPhase.Working)});
            Built("Finished builder",new PlayerHouseSaveData{currentLevel=2,state=BuildingConstructionState.Completed,constructionJob=Job(ConstructionWorkerPhase.Finished)});
            Built("Saved capture roundtrip",JsonUtility.FromJson<PlayerHouseSaveData>(JsonUtility.ToJson(house.Capture())));
            var active=new PlayerHouseSaveData{currentLevel=1,pendingLevel=2,state=BuildingConstructionState.UnderConstruction,completionDay=100,constructionJob=Job(ConstructionWorkerPhase.Working)};
            house.Restore(active);
            Check(house.IsUnderConstruction&&house.PendingLevel==2&&house.Construction!=null,"Genuine paid upgrade resumes");
            Check(Screens(owner)>0,"Genuine upgrade still creates visible construction screens");
            Check(Math.Abs(house.Construction.Capture().completedWorkHours-4)<.001,"Paid work progress retained");
            Built("Load completed slot over a live upgrade",actual);
            Check(house.Construction==null&&Screens(owner)==0,"Repeated load removes the previous active construction scenery");
            active.constructionJob=null;house.Restore(active);
            Check(house.IsUnderConstruction&&house.Construction!=null&&Screens(owner)>0,"Legacy active upgrade without job uses migration fallback");
            Built("Return to actual completed Slot 1",actual);
            foreach(bool queued in new[]{false,true})
            {
                var job=Job(queued?ConstructionWorkerPhase.Queued:ConstructionWorkerPhase.Returning);job.completedWorkHours=18;job.resumePhase=ConstructionWorkerPhase.Returning;
                house.Restore(new PlayerHouseSaveData{currentLevel=2,state=BuildingConstructionState.Completed,constructionJob=job});
                Check(house.Construction!=null&&house.State==BuildingConstructionState.Completed,"Finished upgrade keeps builder return journey (queued="+queued+")");
                Check(Screens(owner)==0&&!fallback.activeSelf,"Returning builder does not recreate screens (queued="+queued+")");
            }
            Built("Final completed Slot 1",actual);
            report.AppendLine("CHECKS "+checks+"; HOUSE_RESTORE_PASS — no save/scene writes; native restore and visual objects tested, no full Play Mode test.");
            File.WriteAllText(Output+"/Audit.txt",report.ToString());if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");
            Object.DestroyImmediate(owner);EditorApplication.Exit(0);
        }
        catch(Exception error)
        {File.WriteAllText(Output+"/Error.txt",report+"\n"+error);Debug.LogException(error);if(owner)Object.DestroyImmediate(owner);EditorApplication.Exit(1);}
    }
}
#endif
