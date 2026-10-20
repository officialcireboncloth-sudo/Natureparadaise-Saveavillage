#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Actual ConfirmPreview regression: free resource bypass is not instant completion.</summary>
[InitializeOnLoad]
public static class FreeConstructionDebugValidation
{
    const string Request="ArtSource/Builder_Worker/verify-free-debug.request";
    const string Output="ArtSource/Builder_Worker/Previews/FreeConstructionDebugValidation.txt";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static FreeConstructionDebugValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Move(Request,Path.GetDirectoryName(Output)+"/FreeDebugRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        Run();
    }
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Private).SetValue(owner,value);
    static T Get<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
    [MenuItem("Nature Paradise/Construction/Validate Free Debug Keeps Builder Process")]
    public static void Run()
    {
        var report=new StringBuilder("FREE CONSTRUCTION DEBUG — ISOLATED NATIVE EDITOR CONFIRM PATH\n");
        int checks=0;var previous=SceneManager.GetActiveScene();var scene=default(Scene);
        BuildingDefinitionSO definition=null;ItemSO material=null;
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;report.AppendLine("PASS "+message);}
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode&&SaveManager.Instance==null,"No live game/SaveManager; cannot write player saves");
            scene=EditorSceneManager.NewPreviewScene();
            definition=ScriptableObject.CreateInstance<BuildingDefinitionSO>();definition.buildingId="audit.free-builder";definition.displayName="Debug audit";
            definition.placementArea=BuildingPlacementArea.Anywhere;
            material=ScriptableObject.CreateInstance<ItemSO>();
            var first=new BuildingLevelDefinition{level=1,goldCost=int.MaxValue,constructionDays=2};
            first.materialCosts.Add(new BuildingMaterialCost{item=material,amount=99999});
            var second=new BuildingLevelDefinition{level=2,goldCost=int.MaxValue,constructionDays=0};
            second.materialCosts.Add(new BuildingMaterialCost{item=material,amount=99999});
            definition.levels.Add(first);definition.levels.Add(second);
            var settings=ConstructionWorkerSettings.Load();int hours=settings.workEnds-settings.workStarts;
            int goldBefore=ScoreManager.Instance!=null?ScoreManager.Instance.points:0;
            PropertySite Fixture(string name,int level,bool previewFree,bool upgrade=false)
            {
                var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3(25000,0,25000);
                var site=go.AddComponent<PropertySite>();Set(site,"buildingAnchor",go.transform);
                Set(site,"previewActive",true);Set(site,"previewLocationValid",true);Set(site,"previewDefinition",definition);
                Set(site,"previewLevel",level);Set(site,"previewUsesFreePlacement",true);Set(site,"previewPlacementPosition",go.transform.position);
                Set(site,"previewPlacementRotation",Quaternion.identity);Set(site,"previewDebugFree",previewFree);
                if(upgrade){Set(site,"state",BuildingConstructionState.Completed);Set(site,"activeDefinition",definition);Set(site,"currentLevel",1);}
                return site;
            }
            var normal=Fixture("Normal cannot afford",1,false);
            Check(!normal.ConfirmPreview()&&normal.IsEmpty,"Normal path still requires payment");
            Object.DestroyImmediate(normal.gameObject);
            foreach(bool previewFlag in new[]{true,false})
            {
                var free=Fixture(previewFlag?"Build menu F9 flag":"Upgrade F9 explicit free",previewFlag?1:2,previewFlag,!previewFlag);
                Check(free.ConfirmPreview(debugFree:!previewFlag),"Free confirm succeeds without inventory/gold: "+previewFlag);
                Check(free.State==BuildingConstructionState.UnderConstruction,"Free confirm is NOT instantly completed: "+previewFlag);
                Check(free.CurrentLevel==(previewFlag?0:1),"Final level not unlocked before builder completion: "+previewFlag);
                Check(free.Construction!=null&&free.Construction.Phase==ConstructionWorkerPhase.Travelling,"Actual builder dispatch starts: "+previewFlag);
                var job=free.Construction.Capture();
                Check(job.requiredWorkHours==(previewFlag?2:1)*hours,"Work duration preserved; zero-day debug upgrade gets a full test workday: "+previewFlag);
                Check(job.completedWorkHours==0&&job.Progress==0,"Animation/work progress starts at zero: "+previewFlag);
                Check(free.CompletionDay> (TimeManager.Instance!=null?TimeManager.Instance.day:1),"Debug does not set completion day to today: "+previewFlag);
                Check((ScoreManager.Instance!=null?ScoreManager.Instance.points:0)==goldBefore,"Free request does not spend gold: "+previewFlag);
                Check(!free.ConfirmPreview(debugFree:true),"Duplicate confirm after preview closes cannot restart/spend twice: "+previewFlag);
                free.Construction.Cancel();Object.DestroyImmediate(free.gameObject);
            }
            var invalid=Fixture("Blocked preview",1,true);Set(invalid,"previewLocationValid",false);
            Check(!invalid.ConfirmPreview(debugFree:true)&&invalid.Construction==null,"Free mode does not bypass invalid location");Object.DestroyImmediate(invalid.gameObject);
            var instant=Fixture("Explicit fixture-only instant",1,false);
            Check(instant.ConfirmPreview(debugInstant:true)&&instant.State==BuildingConstructionState.Completed,"Explicit instant API remains available for old placement regression fixtures");
            Object.DestroyImmediate(instant.gameObject);
            Check(first.constructionDays==2&&second.constructionDays==0,"Definition duration data unchanged by debug override");
            report.AppendLine("AUDIT_PASS checks="+checks+"; isolated native confirmation tests, NOT full Play Mode/input simulation.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);}
        finally
        {
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);if(previous.IsValid())SceneManager.SetActiveScene(previous);
            if(definition!=null)Object.DestroyImmediate(definition);if(material!=null)Object.DestroyImmediate(material);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,report.ToString());
        }
    }
}
#endif
