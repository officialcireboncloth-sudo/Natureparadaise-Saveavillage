#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Native Unity isolated edit-mode regression audit. Does not enter the user's game or write saves.</summary>
[InitializeOnLoad]
public static class UniversalConstructionValidation
{
    const string Request="ArtSource/Construction_Kit/verify.request";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static UniversalConstructionValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Move(Request,UniversalConstructionSetup.Output+"/Verify_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        try{Run();}catch(Exception e){File.WriteAllText(UniversalConstructionSetup.Output+"/ValidationError.txt",e.ToString());Debug.LogException(e);}
    }
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
    static void Step(ConstructionProject project)=>typeof(ConstructionProject).GetMethod("Update",Private).Invoke(project,null);
    [MenuItem("Nature Paradise/Construction/Validate Universal Kit (Isolated)")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop play mode first.");
        // The production completion path calls SaveGame; refuse to run if a live game singleton exists.
        if(SaveManager.Instance!=null)throw new InvalidOperationException("A live SaveManager exists; audit will not touch player saves.");
        var report=new StringBuilder("UNIVERSAL CONSTRUCTION — NATIVE UNITY ISOLATED EDIT-MODE AUDIT\n");
        int checks=0;
        void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);checks++;report.AppendLine("PASS "+label);}
        var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        var settings=ConstructionWorkerSettings.Load();
        Check(settings!=null&&settings.tarpPanel!=null&&settings.workerPrefab!=null,"Resource settings actually connected");
        Check((int)ConstructionWorkerPhase.Returning==2&&(int)ConstructionWorkerPhase.Finished==3,"Existing saved phase values unchanged");
        Check(Math.Abs(ConstructionJobData.WorkBetween(8,24+17,8,17)-18)<.001,"Work clock integrates working windows only");
        try
        {
            foreach(var shader in new[]{settings.revealShader,settings.dustShader})
                Check(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),shader?.name+" compiled and supported");
            var definitions=Resources.LoadAll<BuildingDefinitionSO>("Buildings").Where(d=>d.levels.Any(l=>l.completedPrefab!=null)).ToArray();
            foreach(var definition in definitions)
            {
                var level=definition.levels.First(l=>l.completedPrefab!=null);
                var owner=new GameObject("Audit "+definition.name);SceneManager.MoveGameObjectToScene(owner,scene);
                owner.transform.SetPositionAndRotation(new Vector3(10000,10,10000),Quaternion.Euler(0,37,0));owner.transform.localScale=Vector3.one*1.15f;
                var old=GameObject.CreatePrimitive(PrimitiveType.Cube);old.transform.SetParent(owner.transform,false);old.transform.localPosition=Vector3.up;
                var oldRenderer=old.GetComponent<Renderer>();var collider=old.GetComponent<Collider>();
                Vector3 position=owner.transform.position,scale=owner.transform.localScale;Quaternion rotation=owner.transform.rotation;
                int completions=0;
                var saved=new ConstructionJobData{phase=ConstructionWorkerPhase.Working,workerPosition=position+Vector3.back*20,workPosition=position+Vector3.back*20,
                    departurePosition=position+Vector3.back*25,lastClockHours=ConstructionJobData.ClockHours,requiredWorkHours=2};
                ConstructionProject project=ConstructionProject.Begin(owner,owner.transform,definition,level,level.completedPrefab,true,()=>completions++,saved);
                var visuals=Field<ConstructionProjectVisuals>(project,"visuals");
                var motion=Field<ConstructionHumanoidMotion>(visuals,"humanoidMotion");
                Check(motion!=null,definition.name+" uses independent Humanoid worker, not capsule/player input controller");
                var animator=visuals.Worker.GetComponentInChildren<Animator>();
                Check(animator!=null&&animator.isHuman&&animator.avatar.isValid,"Valid per-model Humanoid Avatar");
                Check(animator.runtimeAnimatorController==null&&!animator.applyRootMotion,"No player AnimatorController or root-motion drift");
                Check(motion.HasClip("Idle")&&motion.HasClip("Walk"),"Retargeted Humanoid Idle and Walking available");
                Check(settings.humanoidActions.actions.Where(a=>a.clip!=null).All(a=>a.clip.isHumanMotion),"Shared library contains only genuine Humanoid clips");
                var panels=owner.GetComponentsInChildren<Transform>().Where(t=>t.name=="SideTarp").ToArray();
                Check(panels.Length>3,definition.name+" modular perimeter panels generated");
                foreach(var panel in panels)
                {
                    var filters=panel.GetComponentsInChildren<MeshFilter>();
                    Check(filters.Length==1,"One renderer per repeated panel");
                    Bounds b=filters[0].GetComponent<Renderer>().bounds;
                    Check(b.size.y>.3f&&Mathf.Min(b.size.x,b.size.z)<.5f,"Panel vertical, no canopy");
                }
                var rigidOrigin=visuals.Worker.position;
                Quaternion foot=Quaternion.identity;float footMotion=0;
                for(int f=0;f<=30;f++)
                {
                    visuals.Animate(true,false,f/30f);
                    var current=animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
                    if(f==5)foot=current;if(f>5)footMotion=Mathf.Max(footMotion,Quaternion.Angle(foot,current));
                }
                Check(footMotion>3,"Walking clip actually animates Humanoid feet");
                Check(visuals.Worker.position==rigidOrigin,"Humanoid walking clip does not drive navigation root");
                visuals.Animate(false,false,1.2f);
                var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes);var rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes);
                Check(leftToe!=null&&rightToe!=null,"Foot/toe facing landmarks mapped");
                var facing=Vector3.ProjectOnPlane((leftToe.position-leftFoot.position)+(rightToe.position-rightFoot.position),Vector3.up).normalized;
                Check(Vector3.Dot(facing,visuals.Worker.forward)>.8f,"Worker body faces navigation/work targets, not backwards");
                for(int f=0;f<=540;f++)visuals.Animate(false,true,f/30f);
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                foreach(string toolName in new[]{"WorkerHammer","WorkerSaw"})
                {
                    var tool=visuals.Worker.Find(toolName);
                    if(tool!=null&&tool.gameObject.activeSelf)Check(Vector3.Distance(tool.position,hand.position)<.001f,"Held tool follows evaluated Humanoid hand");
                }
                Check((visuals.Worker.position-rigidOrigin).sqrMagnitude<.00001f,"Work animation root does not drift");
                Check(owner.GetComponentsInChildren<Transform>(true).All(t=>float.IsFinite(t.position.y)),"All worker/prop poses finite");
                var job=Field<ConstructionJobData>(project,"job");job.completedWorkHours=job.requiredWorkHours;
                Step(project);
                Check(project.Phase==ConstructionWorkerPhase.Revealing&&completions==0,"Completion delayed until reveal ends");
                Check(collider.enabled&&old.activeSelf&&!oldRenderer.enabled,"Upgrade hides renderer only; original collider/object remains active");
                // Find the explicit visual-only parent, not a specific model node name.
                var presentation=Field<ConstructionSitePresentation>(visuals,"presentation");var rising=Field<Transform>(presentation,"rising");
                Check(rising!=null&&rising.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Rising clone has no gameplay behaviours");
                Check(rising.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Rising clone colliders disabled");
                Check(rising.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y)<position.y+.1f,"Initial clone is wholly below ground plane");
                Check(owner.transform.position==position&&owner.transform.rotation==rotation&&owner.transform.localScale==scale,"Authored anchor never moved or stretched");
                job.revealElapsed=settings.revealSeconds*.5f;Step(project);
                var capture=JsonUtility.FromJson<ConstructionJobData>(JsonUtility.ToJson(project.Capture()));
                Check(capture.phase==ConstructionWorkerPhase.Revealing&&capture.revealElapsed>0,"Save snapshot retains mid-reveal phase and progress");
                project.Cancel();Check(oldRenderer.enabled,"Cancel restores original renderer");
                project=ConstructionProject.Begin(owner,owner.transform,definition,level,level.completedPrefab,true,()=>completions++,capture);
                job=Field<ConstructionJobData>(project,"job");job.revealElapsed=settings.revealSeconds;Step(project);
                Check(project.Phase==ConstructionWorkerPhase.Returning&&completions==1,"Reloaded reveal completes exactly once");
                Check(oldRenderer.enabled&&collider.enabled,"Reveal cleanup restores original object state for normal completion callback");
                var returning=project.Capture();project.Cancel();
                project=ConstructionProject.Begin(owner,owner.transform,definition,level,level.completedPrefab,true,()=>completions++,returning);
                Check(project.Phase==ConstructionWorkerPhase.Returning&&completions==1,"Reloading a completed returning job does not repeat completion");
                project.Cancel();
                var legacy=saved.Copy();legacy.version=1;legacy.phase=ConstructionWorkerPhase.Travelling;
                project=ConstructionProject.Begin(owner,owner.transform,definition,level,level.completedPrefab,true,()=>completions++,legacy);
                var migrated=project.Capture();
                Check(migrated.version==3&&migrated.workerPosition==legacy.workerPosition,"Legacy travelling save preserves builder position");
                Check(migrated.workPosition!=legacy.workPosition,"Legacy bench moved outside new screens");project.Cancel();
                legacy.phase=ConstructionWorkerPhase.Working;
                project=ConstructionProject.Begin(owner,owner.transform,definition,level,level.completedPrefab,true,()=>completions++,legacy);
                migrated=project.Capture();
                Check(migrated.workerPosition==migrated.workPosition,"Legacy working builder follows migrated bench");
                Check(migrated.completedWorkHours==legacy.completedWorkHours,"Legacy paid work progress preserved");
                project.Cancel();Object.DestroyImmediate(owner);
            }
            report.AppendLine("AUDIT_PASS checks="+checks+"; native editor fixtures, NOT a full Play Mode or standalone build test; no player-save writes.");
            File.WriteAllText(UniversalConstructionSetup.Output+"/UnityValidation.txt",report.ToString());
        }
        catch(Exception e){File.WriteAllText(UniversalConstructionSetup.Output+"/UnityValidation.txt",report+"\nFAIL "+e);throw;}
        finally{EditorSceneManager.ClosePreviewScene(scene);if(original.IsValid())SceneManager.SetActiveScene(original);}
    }
}
#endif
