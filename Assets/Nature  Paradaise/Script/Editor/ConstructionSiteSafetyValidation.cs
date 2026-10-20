#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Native collision/exit/particle fixtures without invoking construction/save completion.</summary>
[InitializeOnLoad]
public static class ConstructionSiteSafetyValidation
{
    const string Request="ArtSource/Construction_Kit/verify-site-safety.request";
    const string Output="ArtSource/Construction_Kit/Previews/SiteSafetyValidation.txt";
    static ConstructionSiteSafetyValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        File.Move(Request,Path.GetDirectoryName(Output)+"/SafetyRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Run();
    }
    [MenuItem("Nature Paradise/Construction/Validate Site Collision Exits And Dust")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder("SITE SAFETY — NATIVE ISOLATED EDITOR FIXTURES\n");int count=0;
        var scene=EditorSceneManager.NewPreviewScene();var previous=SceneManager.GetActiveScene();
        ConstructionSitePresentation first=null,overlapping=null;ConstructionWorkerSettings profile=null;
        void Check(bool yes,string label){if(!yes)throw new InvalidOperationException(label);count++;report.AppendLine("PASS "+label);}
        GameObject InScene(string name,Vector3 position)
        {
            var o=new GameObject(name);SceneManager.MoveGameObjectToScene(o,scene);o.transform.position=position;return o;
        }
        CharacterController Actor(Vector3 position)
        {
            var c=InScene("Mock player capsule (no gameplay scripts)",position).AddComponent<CharacterController>();
            c.height=PlayerDimensions.Height;c.radius=PlayerDimensions.Radius;c.center=Vector3.up*c.height/2;
            c.skinWidth=PlayerDimensions.SkinWidth;c.stepOffset=PlayerDimensions.StepOffset;return c;
        }
        try
        {
            profile=Object.Instantiate(ConstructionWorkerSettings.Load());profile.screenHeight=1.8f;profile.exitWidth=2.2f;profile.screenCollision=true;
            var origin=new Vector3(25000,0,25000);var owner=InScene("Tall Barn footprint fixture",origin);var actor=Actor(origin);
            var bounds=new Bounds(origin+Vector3.up*4,new Vector3(8,8,10));
            first=new ConstructionSitePresentation(owner.transform,owner.transform,bounds,origin+Vector3.back*8,null,profile);
            var panels=owner.GetComponentsInChildren<Transform>().Where(t=>t.name=="SideTarp").ToArray();
            Check(panels.Length>0,"Tarp panels spawned");
            Check(panels.All(p=>p.GetComponentInChildren<Renderer>().bounds.max.y<=1.81f),"8m-tall structure gets <=1.8m tarp, not roof-height walls");
            Check(owner.GetComponentsInChildren<Transform>().Count(t=>t.name=="ConstructionExit_AlwaysOpen")==2,"Two opposing exits always open");
            var walls=owner.GetComponentsInChildren<BoxCollider>();
            Check(walls.Length>=4&&walls.All(c=>c.enabled&&!c.isTrigger),"Solid perimeter spans enabled when no actor overlaps them");
            Check(walls.All(c=>c.size.z<=.3f),"Only thin wall colliders; no full-volume prison collider");
            Physics.SyncTransforms();actor.Move(Vector3.forward*8);
            Check(actor.transform.position.z>origin.z+6,"Player initially inside can physically walk out through open gate");
            actor.transform.position=origin;Physics.SyncTransforms();actor.Move(Vector3.right*8);
            Check(actor.transform.position.x<origin.x+4.5f,"Player capsule is blocked by the non-gate side wall");
            first.Tick(true,false);
            var work=owner.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="WorkDust");
            var ambient=owner.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="SiteDust");
            Check(work.transform.position.z<origin.z-6,"Work dust emitter is outside opaque tarp, near workbench");
            Check(work.emission.rateOverTime.constant>0&&ambient.emission.rateOverTime.constant>0,"Work and site smoke emit while working");
            Check(work.main.startColor.color.a>=.2f&&work.velocityOverLifetime.y.constantMax>0&&ambient.main.startColor.color.a>work.main.startColor.color.a,"Subtle work puffs, denser interior clouds and upward movement");
            Check(work.shape.rotation==Vector3.zero,"Spawn volume remains horizontal, not an accidentally rotated tall box");
            work.Emit(12);work.Simulate(.3f,true,false);Check(work.particleCount>0,"Native particle simulation produces live work dust");
            first.Tick(false,false);Check(work.emission.rateOverTime.constant==0,"Work emission stops when builder is not working");

            var secondOrigin=origin+Vector3.right*40;var second=InScene("Overlapping player fixture",secondOrigin);
            var clipped=Actor(secondOrigin+Vector3.right*(4+profile.siteClearance));Physics.SyncTransforms();
            overlapping=new ConstructionSitePresentation(second.transform,second.transform,new Bounds(secondOrigin+Vector3.up*4,new Vector3(8,8,10)),secondOrigin+Vector3.back*8,null,profile);
            var deferred=second.GetComponentsInChildren<BoxCollider>().Where(c=>!c.enabled).ToArray();
            Check(deferred.Length>0,"A wall spawned on the player stays non-solid instead of trapping/pushing them");
            clipped.transform.position=secondOrigin+Vector3.right*8;var position=clipped.transform.position;
            overlapping.Tick(false,false);
            Check(deferred.All(c=>c.enabled),"Deferred wall becomes solid once the player clears it");
            Check(clipped.transform.position==position,"Safety handling never teleports or ejects player");
            overlapping.Reveal(.2f);Check(second.GetComponentsInChildren<BoxCollider>().Length==0,"Tarp collision disables with screens during completion reveal");
            first.Dispose();Check(walls.All(c=>!c.enabled),"Cancel/dispose disables barriers immediately");first=null;
            report.AppendLine("AUDIT_PASS checks="+count+"; native capsule moves and particles, not full Play Mode. No save/completion APIs invoked.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);}
        finally
        {
            first?.Dispose();overlapping?.Dispose();EditorSceneManager.ClosePreviewScene(scene);if(previous.IsValid())SceneManager.SetActiveScene(previous);
            if(profile!=null)Object.DestroyImmediate(profile);Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,report.ToString());
        }
    }
}
#endif
