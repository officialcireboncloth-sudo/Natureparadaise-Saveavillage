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
    LumberStoreOwner builderOwner;
    int registeredFrame;
    public long QueueOrder => job?.queueOrder ?? 0;
    public Transform Worker => visuals?.Worker;
    public LumberStoreOwner BuilderOwner => builderOwner;
    public float Progress => job?.Progress ?? 0;
    public ConstructionWorkerPhase Phase => job?.phase ?? ConstructionWorkerPhase.Finished;
    public string Status => job == null ? "" : job.phase == ConstructionWorkerPhase.Queued ? "Menunggu tukang Lumber Store" : job.pathBlocked ? "Jalur builder terhalang" :
        job.phase == ConstructionWorkerPhase.Travelling ? "Builder menuju proyek" :
        job.phase == ConstructionWorkerPhase.Returning ? "Selesai · builder kembali" :
        job.phase == ConstructionWorkerPhase.Revealing ? "Bangunan selesai · merapikan proyek" :
        !WithinWorkingHours ? "Istirahat · dilanjutkan besok" : StageName;
    public string StageName => Progress < .25f ? "Persiapan & material" : Progress < .5f ? "Pengerjaan" : Progress < .75f ? "Perakitan" : "Finishing";
    bool WithinWorkingHours => TimeManager.Instance == null ||
        (TimeManager.Instance.CurrentTimeHours >= job.workStarts && TimeManager.Instance.CurrentTimeHours < job.workEnds);

    public static ConstructionProject Begin(GameObject owner, Transform anchor, BuildingDefinitionSO building,
        BuildingLevelDefinition target, GameObject finalVisual, bool renovation, Action onCompleted,
        ConstructionJobData saved = null, Vector3? departure = null, int minimumWorkDays = 0)
    {
        var project=owner.GetComponent<ConstructionProject>();
        if(project == null)project=owner.AddComponent<ConstructionProject>();
        project.Clear();project.definition=building;project.settings=ConstructionWorkerSettings.Load();project.completed=onCompleted;
        int start=project.settings != null ? project.settings.workStarts : 8;
        int end=project.settings != null ? project.settings.workEnds : 17;
        var layout=ConstructionSiteLayout.Measure(owner,anchor,building,finalVisual,renovation);
        Bounds bounds=layout.WorldBounds;
        project.siteCenter=bounds.center;
        Vector3 origin=departure ?? FindDeparture(bounds.center);
        Vector3 work=FindWorkPosition(layout,origin);
        project.job=saved?.Copy() ?? new ConstructionJobData
        {
            phase=ConstructionWorkerPhase.Travelling,departurePosition=origin,workerPosition=origin,workPosition=work,
            lastClockHours=ConstructionJobData.ClockHours,workStarts=start,workEnds=end,
            requiredWorkHours=Math.Max(Math.Max(0,minimumWorkDays),target.constructionDays)*(end-start)
        };
        if (saved != null && saved.version == 0)
        {
            project.job.version = 1; project.job.workPosition = work; project.job.workerPosition = work;
        }
        if(saved!=null && project.job.version<3 && project.settings!=null && project.settings.tarpPanel!=null)
        {
            // v1 had close scaffolding; v2 measured stations against world-aligned AABBs.
            // Preserve travelling/returning positions and paid progress; migrate only the work station.
            project.job.workPosition=work;
            if(project.job.phase==ConstructionWorkerPhase.Working || project.job.phase==ConstructionWorkerPhase.Revealing)
                project.job.workerPosition=work;
            project.job.version=3;
        }
        project.job.workEnds=Mathf.Clamp(project.job.workEnds,project.job.workStarts+1,24);
        project.visuals=new ConstructionProjectVisuals(owner.transform,anchor,bounds,project.job.workPosition,target,finalVisual,renovation,project.settings,layout);
        project.visuals.Worker.SetPositionAndRotation(project.job.workerPosition,project.job.workerRotation);
        project.job.CatchUp(ConstructionJobData.ClockHours);
        if(project.Phase == ConstructionWorkerPhase.Returning || project.Phase == ConstructionWorkerPhase.Finished)project.visuals.HideSite();
        else project.visuals.SetStage(project.Progress);
        if(project.Phase == ConstructionWorkerPhase.Revealing)project.visuals.Reveal(project.RevealProgress);
        if(project.Phase == ConstructionWorkerPhase.Finished)project.visuals.Worker.gameObject.SetActive(false);
        if(project.Phase != ConstructionWorkerPhase.Finished)
        {
            project.builderOwner=LumberStoreOwner.Find(project.job.departurePosition,project.job.builderStoreId);
            if(project.builderOwner!=null)
            {
                project.job.builderStoreId=project.builderOwner.StoreId;
                if(project.job.queueOrder==0)project.job.queueOrder=DateTime.UtcNow.Ticks;
                if(project.job.phase!=ConstructionWorkerPhase.Queued)
                { project.job.resumePhase=project.job.phase;project.job.phase=ConstructionWorkerPhase.Queued; }
                project.registeredFrame=Time.frameCount;
                project.visuals.Worker.gameObject.SetActive(false);
                project.builderOwner.Register(project);
            }
        }
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
        return GroundDeparture(origin);
    }
    /// <summary>Uses the same safe, ground-level home position for the idle owner and every departure/return.</summary>
    public static Vector3 GroundDeparture(Vector3 origin)
    {
        if(ConstructionWorkerPath.Ground(origin,out var ground))return ground;
        for(int radius=1;radius<=6;radius++)for(int direction=0;direction<8;direction++)
        {
            float angle=direction*Mathf.PI/4;Vector3 candidate=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
            if(ConstructionWorkerPath.Ground(candidate,out ground))return ground;
        }
        return origin; // Explicit blocked state, rather than spawning at the project.
    }
    static Vector3 FindWorkPosition(ConstructionSiteLayout layout,Vector3 origin)
    {
        var profile=ConstructionWorkerSettings.Load();
        float margin=(profile!=null?profile.siteClearance:.65f)+(profile!=null?profile.workerScale:1)*1.1f;
        Bounds bounds=layout.LocalBounds;
        Vector3[] candidates={layout.ToWorld(new(bounds.min.x-margin,0,bounds.center.z)),layout.ToWorld(new(bounds.max.x+margin,0,bounds.center.z)),
            layout.ToWorld(new(bounds.center.x,0,bounds.min.z-margin)),layout.ToWorld(new(bounds.center.x,0,bounds.max.z+margin))};
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
        job.completedWorkHours=job.requiredWorkHours;
        if(job.phase==ConstructionWorkerPhase.Queued)job.resumePhase=ConstructionWorkerPhase.Returning;
        else job.phase=ConstructionWorkerPhase.Returning;
        path=null;nextPathAttempt=0;visuals?.HideSite();completed=null;
    }
    void Update()
    {
        if(job == null || visuals == null)return;
        job.CatchUp(ConstructionJobData.ClockHours);
        if(job.phase==ConstructionWorkerPhase.Queued)
        {
            // Defer one frame so all restored orders register before the oldest order claims the owner.
            if(Time.frameCount<=registeredFrame)return;
            if(builderOwner!=null && !builderOwner.TryDispatch(this))
            { visuals.UpdateBar(definition!=null?definition.displayName:"Proyek",Status,Progress);return; }
            job.phase=job.resumePhase;job.lastClockHours=ConstructionJobData.ClockHours;
            visuals.Worker.gameObject.SetActive(true);
        }
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
                if((visuals.Worker.position-destination).sqrMagnitude < .0025f)
                {
                    job.phase=returning ? ConstructionWorkerPhase.Finished : ConstructionWorkerPhase.Working;
                    job.lastClockHours=ConstructionJobData.ClockHours;job.pathBlocked=false;path=null;
                    if(returning){visuals.Worker.gameObject.SetActive(false);builderOwner?.Release(this);}
                }
            }
            if(job.phase == ConstructionWorkerPhase.Working)
            {
                working=WithinWorkingHours;
                Vector3 direction=siteCenter-visuals.Worker.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)visuals.Worker.rotation=Quaternion.Slerp(visuals.Worker.rotation,Quaternion.LookRotation(direction),Time.deltaTime*7);
                if(job.Progress >= 1)
                {
                    job.phase=ConstructionWorkerPhase.Revealing;job.revealElapsed=0;
                    visuals.Reveal(0);
                    working=false;
                }
                else visuals.SetStage(job.Progress);
            }
            if(job.phase == ConstructionWorkerPhase.Revealing)
            {
                job.revealElapsed+=Time.deltaTime;visuals.Reveal(RevealProgress);
                if(RevealProgress>=1)
                {
                    // Phase changes before callbacks so completion-triggered saves cannot repeat rewards/unlocks.
                    job.phase=ConstructionWorkerPhase.Returning;path=null;nextPathAttempt=0;visuals.HideSite();
                    var callback=completed;completed=null;callback?.Invoke();
                    SaveManager.Instance?.SaveGame();
                }
            }
        }
        visuals.Animate(walking,working,animationTime);
        if(job.phase==ConstructionWorkerPhase.Travelling || job.phase==ConstructionWorkerPhase.Working || job.phase==ConstructionWorkerPhase.Revealing)
            visuals.UpdateBar(definition != null ? definition.displayName : "Proyek",Status,Progress);
        job.workerPosition=visuals.Worker.position;job.workerRotation=visuals.Worker.rotation;
    }
    float RevealProgress=>Mathf.Clamp01(job.revealElapsed/(settings!=null?Mathf.Max(.5f,settings.revealSeconds):2.6f));
    bool MoveWorker(Vector3 destination)
    {
        if(path == null && Time.unscaledTime>=nextPathAttempt)
        {
            path=ConstructionWorkerPath.Find(visuals.Worker.position,destination);waypoint=0;
            job.pathBlocked=path == null;nextPathAttempt=Time.unscaledTime+4;
        }
        if(path == null)return false;
        while(waypoint<path.Count && (visuals.Worker.position-path[waypoint]).sqrMagnitude<(waypoint==path.Count-1?.0016f:.08f))waypoint++;
        if(waypoint>=path.Count){path=null;nextPathAttempt=0;return false;}
        Vector3 next=Vector3.MoveTowards(visuals.Worker.position,path[waypoint],Time.deltaTime*(settings != null ? settings.walkingSpeed : 2.4f));
        if(!ConstructionWorkerPath.Ground(next,out next) || !ConstructionWorkerPath.Segment(visuals.Worker.position,next))
        {path=null;job.pathBlocked=true;nextPathAttempt=Time.unscaledTime+2;return false;}
        Vector3 direction=next-visuals.Worker.position;direction.y=0;
        if(direction.sqrMagnitude>.0001f)visuals.Worker.rotation=Quaternion.Slerp(visuals.Worker.rotation,Quaternion.LookRotation(direction),Time.deltaTime*9);
        visuals.Worker.position=next;return true;
    }
    public void Cancel(){Clear();}
    void Clear(){visuals?.Dispose();visuals=null;builderOwner?.Release(this);builderOwner=null;job=null;completed=null;path=null;nextPathAttempt=0;}
    void OnDestroy()=>Clear();
}
