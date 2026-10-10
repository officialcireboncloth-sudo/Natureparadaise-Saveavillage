using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One paid construction order: dispatch, arrival, clock-based work, completion, return.</summary>
[DisallowMultipleComponent]
public sealed class ConstructionProject : MonoBehaviour
{
    ConstructionJobData job;
    ConstructionProjectVisuals visuals;
    ConstructionWorkerSettings settings;
    Action completed;
    BuildingDefinitionSO definition;
    Vector3 siteCenter;
    List<Vector3> path;
    int waypoint;
    float nextPathAttempt, animationTime;
    public float Progress => job?.Progress ?? 0;
    public ConstructionWorkerPhase Phase => job?.phase ?? ConstructionWorkerPhase.Finished;
    public string Status => job == null ? "" : job.pathBlocked ? "Jalur builder terhalang" :
        job.phase == ConstructionWorkerPhase.Travelling ? "Builder menuju proyek" :
        job.phase == ConstructionWorkerPhase.Returning ? "Selesai · builder kembali" :
        !WithinWorkingHours ? "Istirahat · dilanjutkan besok" : StageName;
    public string StageName => Progress < .25f ? "Fondasi" : Progress < .5f ? "Rangka" : Progress < .75f ? "Dinding & atap" : "Finishing";
    bool WithinWorkingHours => TimeManager.Instance == null ||
        (TimeManager.Instance.CurrentTimeHours >= job.workStarts && TimeManager.Instance.CurrentTimeHours < job.workEnds);

    public static ConstructionProject Begin(GameObject owner, Transform anchor, BuildingDefinitionSO building,
        BuildingLevelDefinition target, GameObject finalVisual, bool renovation, Action onCompleted,
        ConstructionJobData saved = null, Vector3? departure = null)
    {
        var project=owner.GetComponent<ConstructionProject>();
        if(project == null)project=owner.AddComponent<ConstructionProject>();
        project.Clear();project.definition=building;project.settings=ConstructionWorkerSettings.Load();project.completed=onCompleted;
        int start=project.settings != null ? project.settings.workStarts : 8;
        int end=project.settings != null ? project.settings.workEnds : 17;
        Bounds bounds=MeasureSite(owner,anchor,building,finalVisual,renovation);
        project.siteCenter=bounds.center;
        Vector3 origin=departure ?? FindDeparture(bounds.center);
        Vector3 work=FindWorkPosition(bounds,origin,anchor.position.y);
        project.job=saved?.Copy() ?? new ConstructionJobData
        {
            phase=ConstructionWorkerPhase.Travelling,departurePosition=origin,workerPosition=origin,workPosition=work,
            lastClockHours=ConstructionJobData.ClockHours,workStarts=start,workEnds=end,
            requiredWorkHours=Math.Max(0,target.constructionDays)*(end-start)
        };
        if (saved != null && saved.version == 0)
        {
            project.job.version = 1; project.job.workPosition = work; project.job.workerPosition = work;
        }
        project.job.workEnds=Mathf.Clamp(project.job.workEnds,project.job.workStarts+1,24);
        project.visuals=new ConstructionProjectVisuals(owner.transform,anchor,bounds,project.job.workPosition,target,finalVisual,renovation,project.settings);
        project.visuals.Worker.SetPositionAndRotation(project.job.workerPosition,project.job.workerRotation);
        project.job.CatchUp(ConstructionJobData.ClockHours);
        if(project.Phase == ConstructionWorkerPhase.Returning || project.Phase == ConstructionWorkerPhase.Finished)project.visuals.HideSite();
        else project.visuals.SetStage(project.Progress);
        if(project.Phase == ConstructionWorkerPhase.Finished)project.visuals.Worker.gameObject.SetActive(false);
        return project;
    }
    public static ConstructionJobData MigrateLegacy(BuildingLevelDefinition target, int completionDay, Vector3 site)
    {
        var settings=ConstructionWorkerSettings.Load();int start=settings != null ? settings.workStarts : 8,end=settings != null ? settings.workEnds : 17;
        double required=Math.Max(1,target?.constructionDays ?? 1)*(end-start);
        double remaining=ConstructionJobData.WorkBetween(ConstructionJobData.ClockHours,(completionDay-1)*24d+end,start,end);
        return new ConstructionJobData {version=0,phase=ConstructionWorkerPhase.Working,workerPosition=site,workPosition=site,
            departurePosition=FindDeparture(site),lastClockHours=ConstructionJobData.ClockHours,requiredWorkHours=required,
            completedWorkHours=Math.Max(0,required-remaining),workStarts=start,workEnds=end};
    }
    static Bounds MeasureSite(GameObject owner,Transform anchor,BuildingDefinitionSO building,GameObject finalVisual,bool renovation)
    {
        var bounds=new Bounds(anchor.position+Vector3.up*1.5f,new Vector3(building != null ? building.footprintWidth : 3,3,building != null ? building.footprintDepth : 3));
        bool found=false;
        foreach(var renderer in renovation ? owner.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>())
        {
            if(renderer.GetComponentInParent<ConstructionProject>() != null && renderer.transform.name.Contains("Runtime"))continue;
            if(!renderer.enabled || renderer.GetComponent<TMPro.TMP_Text>() != null || renderer.bounds.size.sqrMagnitude<.01f)continue;
            if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
        }
        if(!found && finalVisual != null)
        {
            // Asset render bounds are in prefab coordinates. The placement anchor supplies the world pose.
            foreach(var renderer in finalVisual.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b=renderer.bounds;if(b.size.sqrMagnitude<.01f)continue;
                Vector3 center=anchor.TransformPoint(finalVisual.transform.InverseTransformPoint(b.center));
                if(!found){bounds=new Bounds(center,b.size);found=true;}
                else bounds.Encapsulate(new Bounds(center,b.size));
            }
        }
        return bounds;
    }
    public static Vector3 FindDeparture(Vector3 site)
    {
        UpgradeShopFront nearest=null;float distance=float.MaxValue;
        foreach(var front in FindObjectsByType<UpgradeShopFront>(FindObjectsSortMode.None))
        {
            if(front.kind != UpgradeShopKind.Lumber)continue;
            float candidate=(front.transform.position-site).sqrMagnitude;
            if(candidate < distance){nearest=front;distance=candidate;}
        }
        Vector3 origin=nearest != null ? (nearest.builderDeparturePoint != null ? nearest.builderDeparturePoint.position : nearest.transform.position+nearest.transform.forward*2.2f) : site+Vector3.back*8;
        if(ConstructionWorkerPath.Ground(origin,out var ground))return ground;
        for(int radius=1;radius<=6;radius++)for(int direction=0;direction<8;direction++)
        {
            float angle=direction*Mathf.PI/4;Vector3 candidate=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
            if(ConstructionWorkerPath.Ground(candidate,out ground))return ground;
        }
        return origin; // Explicit blocked state, rather than spawning at the project.
    }
    static Vector3 FindWorkPosition(Bounds bounds,Vector3 origin,float groundY)
    {
        Vector3[] candidates={new(bounds.min.x-.95f,groundY,bounds.center.z),new(bounds.max.x+.95f,groundY,bounds.center.z),
            new(bounds.center.x,groundY,bounds.min.z-.95f),new(bounds.center.x,groundY,bounds.max.z+.95f)};
        Array.Sort(candidates,(a,b)=>(a-origin).sqrMagnitude.CompareTo((b-origin).sqrMagnitude));
        foreach(var point in candidates)if(ConstructionWorkerPath.Ground(point,out var ground))return ground;
        return candidates[0];
    }
    public ConstructionJobData Capture()
    {
        if(job == null || job.phase == ConstructionWorkerPhase.Finished)return null;
        job.CatchUp(ConstructionJobData.ClockHours);
        if(visuals?.Worker != null){job.workerPosition=visuals.Worker.position;job.workerRotation=visuals.Worker.rotation;}
        return job.Copy();
    }
    public void RefreshClock()
    {
        if(job != null)job.CatchUp(ConstructionJobData.ClockHours);
    }
    public void FinishVisuals()
    {
        if(job == null || job.phase == ConstructionWorkerPhase.Returning || job.phase == ConstructionWorkerPhase.Finished)return;
        job.completedWorkHours=job.requiredWorkHours;job.phase=ConstructionWorkerPhase.Returning;
        path=null;nextPathAttempt=0;visuals?.HideSite();completed=null;
    }
    void Update()
    {
        if(job == null || visuals == null)return;
        job.CatchUp(ConstructionJobData.ClockHours);
        bool paused=TimeManager.Instance != null && TimeManager.Instance.IsPaused;
        bool walking=false,working=false;
        if(!paused)
        {
            animationTime+=Time.deltaTime;
            if(job.phase == ConstructionWorkerPhase.Travelling || job.phase == ConstructionWorkerPhase.Returning)
            {
                bool returning=job.phase == ConstructionWorkerPhase.Returning;
                Vector3 destination=returning ? job.departurePosition : job.workPosition;
                walking=MoveWorker(destination);
                if((visuals.Worker.position-destination).sqrMagnitude < .12f)
                {
                    job.phase=returning ? ConstructionWorkerPhase.Finished : ConstructionWorkerPhase.Working;
                    job.lastClockHours=ConstructionJobData.ClockHours;job.pathBlocked=false;path=null;
                    if(returning)visuals.Worker.gameObject.SetActive(false);
                }
            }
            if(job.phase == ConstructionWorkerPhase.Working)
            {
                working=WithinWorkingHours;
                Vector3 direction=siteCenter-visuals.Worker.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)visuals.Worker.rotation=Quaternion.Slerp(visuals.Worker.rotation,Quaternion.LookRotation(direction),Time.deltaTime*7);
                if(job.Progress >= 1)
                {
                    // Change phase BEFORE callbacks: a completion-triggered SaveGame sees a completed job.
                    job.phase=ConstructionWorkerPhase.Returning;path=null;nextPathAttempt=0;visuals.HideSite();
                    completed?.Invoke();completed=null;
                    SaveManager.Instance?.SaveGame();
                    working=false;
                }
                else visuals.SetStage(job.Progress);
            }
        }
        visuals.Animate(walking,working,animationTime);
        if(job.phase==ConstructionWorkerPhase.Travelling || job.phase==ConstructionWorkerPhase.Working)
            visuals.UpdateBar(definition != null ? definition.displayName : "Proyek",Status,Progress);
        job.workerPosition=visuals.Worker.position;job.workerRotation=visuals.Worker.rotation;
    }
    bool MoveWorker(Vector3 destination)
    {
        if(path == null && Time.unscaledTime>=nextPathAttempt)
        {
            path=ConstructionWorkerPath.Find(visuals.Worker.position,destination);waypoint=0;
            job.pathBlocked=path == null;nextPathAttempt=Time.unscaledTime+4;
        }
        if(path == null)return false;
        while(waypoint<path.Count && (visuals.Worker.position-path[waypoint]).sqrMagnitude<.08f)waypoint++;
        if(waypoint>=path.Count){path=null;nextPathAttempt=0;return false;}
        Vector3 next=Vector3.MoveTowards(visuals.Worker.position,path[waypoint],Time.deltaTime*(settings != null ? settings.walkingSpeed : 2.4f));
        if(!ConstructionWorkerPath.Ground(next,out next) || !ConstructionWorkerPath.Segment(visuals.Worker.position,next))
        {path=null;job.pathBlocked=true;nextPathAttempt=Time.unscaledTime+2;return false;}
        Vector3 direction=next-visuals.Worker.position;direction.y=0;
        if(direction.sqrMagnitude>.0001f)visuals.Worker.rotation=Quaternion.Slerp(visuals.Worker.rotation,Quaternion.LookRotation(direction),Time.deltaTime*9);
        visuals.Worker.position=next;return true;
    }
    public void Cancel(){Clear();}
    void Clear(){visuals?.Dispose();visuals=null;job=null;completed=null;path=null;nextPathAttempt=0;}
    void OnDestroy()=>Clear();
}
