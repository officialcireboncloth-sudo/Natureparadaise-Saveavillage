using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class Object {public static object found;public static T FindFirstObjectByType<T>() where T:class=>found as T;}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 right=>new Vector3(1,0,0);
        public static Vector3 forward=>new Vector3(0,0,1);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public class Transform
    {
        public Vector3 position;
        public Vector3 right=Vector3.right,forward=Vector3.forward;
        public Dictionary<Type,object> components=new Dictionary<Type,object>();
        public T GetComponent<T>() where T:class=>components.ContainsKey(typeof(T))?components[typeof(T)] as T:null;
    }
    public class Collider {public bool enabled=true,isTrigger;public Vector3 closest;public Vector3 ClosestPoint(Vector3 point)=>closest;}
    public static class Mathf {public static float Abs(float value)=>Math.Abs(value);public static float Sqrt(float value)=>(float)Math.Sqrt(value);}
    public enum KeyCode {E,F}
    public static class Input {public static bool pressed;public static bool GetKeyDown(KeyCode key)=>pressed;}
    public static class Time {public static int frameCount;}
}
public class PlayerController {public bool IsMovementLocked;public Vector3 FacingDirection=Vector3.forward;public bool HasMovementLockOtherThan(object owner)=>IsMovementLocked;}
public static class WorldInteractionPrompt
{
    public static bool IsSuppressed;
    public static UnityEngine.Object displayed;
    public static Transform displayedAnchor;
    public static bool IsDisplayedFor(UnityEngine.Object owner)=>!IsSuppressed && displayed==owner;
    public static bool IsDisplayedFor(UnityEngine.Object owner,Transform anchor)=>IsDisplayedFor(owner)&&anchor!=null&&displayedAnchor==anchor;
    public static bool AllowsAction(UnityEngine.Object owner)=>owner!=null&&!IsSuppressed&&(displayed==null||IsDisplayedFor(owner));
}
public static class GameplayInput {public static bool GetKeyDown(KeyCode key)=>Input.GetKeyDown(key);}
public class FieldArea
{
    public static List<FieldArea> ActiveAreas=new List<FieldArea>();
    public Transform transform=new Transform();public float CellSize=3;
    public Vector3 GridToWorld(int x,int z)=>new Vector3(x*CellSize,0,z*CellSize);
}
public class FarmingTool
{
    public FieldArea field;public bool valid;
    public bool TryGetCurrentTile(out FieldArea f,out int x,out int z){f=field;x=0;z=1;return valid;}
}
class InteractionTargetTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Main()
    {
        var player=new Transform();var controller=new PlayerController();player.components[typeof(PlayerController)]=controller;
        var target=new Transform {position=new Vector3(0,0,3)};
        Check(PlayerInteractionTarget.Contains(player,target),"front tile");
        target.position=new Vector3(0,0,-3);Check(!PlayerInteractionTarget.Contains(player,target),"behind rejected");
        target.position=new Vector3(3,0,0);Check(!PlayerInteractionTarget.Contains(player,target),"side rejected");
        controller.FacingDirection=Vector3.right;Check(PlayerInteractionTarget.Contains(player,target),"turn right");
        controller.FacingDirection=Vector3.forward;
        var field=new FieldArea();var farming=new FarmingTool {field=field,valid=true};player.components[typeof(FarmingTool)]=farming;
        foreach(float size in new[]{1f,2f,3f,4f})
        {
            field.CellSize=size;target.position=field.GridToWorld(0,1);
            Check(PlayerInteractionTarget.Contains(player,target),"same center as hoe");
            target.position=new Vector3(size*.51f,0,size);Check(!PlayerInteractionTarget.Contains(player,target),"outside tile width");
            target.position=new Vector3(0,2,size);Check(!PlayerInteractionTarget.Contains(player,target),"other floor rejected");
        }
        target.position=field.GridToWorld(0,1);
        controller.IsMovementLocked=true;Check(!PlayerInteractionTarget.Contains(player,target),"movement lock");controller.IsMovementLocked=false;
        WorldInteractionPrompt.IsSuppressed=true;Check(!PlayerInteractionTarget.Contains(player,target),"modal suppression");WorldInteractionPrompt.IsSuppressed=false;
        Input.pressed=true;Time.frameCount=1;
        var firstOwner=new UnityEngine.Object();WorldInteractionPrompt.displayed=firstOwner;
        Check(PlayerInteractionTarget.PressPrompt(firstOwner,KeyCode.E),"first E");
        Check(!PlayerInteractionTarget.PressPrompt(firstOwner,KeyCode.E),"second E same frame rejected");
        Time.frameCount=2;Check(PlayerInteractionTarget.PressPrompt(firstOwner,KeyCode.E),"next press accepted");
        var bed=new UnityEngine.Object();var aquarium=new UnityEngine.Object();
        WorldInteractionPrompt.displayed=bed;Time.frameCount=3;
        Check(!PlayerInteractionTarget.PressPrompt(aquarium,KeyCode.E),"aquarium updating first cannot steal visible bed prompt");
        Check(!PlayerInteractionTarget.WasConsumed(KeyCode.E),"rejected aquarium leaves E for the bed");
        Check(PlayerInteractionTarget.PressPrompt(bed,KeyCode.E),"visible bed consumes E");
        Check(!PlayerInteractionTarget.PressPrompt(bed,KeyCode.E),"visible owner cannot consume E twice");
        Time.frameCount=4;WorldInteractionPrompt.displayed=aquarium;
        Check(!PlayerInteractionTarget.PressPrompt(bed,KeyCode.E),"bed updating first cannot steal visible aquarium prompt");
        Check(PlayerInteractionTarget.PressPrompt(aquarium,KeyCode.E),"visible aquarium consumes E");
        Time.frameCount=5;WorldInteractionPrompt.displayed=null;
        Check(!PlayerInteractionTarget.PressPrompt(aquarium,KeyCode.E),"no displayed prompt cannot open a panel");
        WorldInteractionPrompt.displayed=bed;WorldInteractionPrompt.IsSuppressed=true;
        Check(!PlayerInteractionTarget.PressPrompt(bed,KeyCode.E),"modal suppresses prompt input");
        WorldInteractionPrompt.IsSuppressed=false;
        var door=new UnityEngine.Object();var storage=new UnityEngine.Object();
        Time.frameCount=6;WorldInteractionPrompt.displayed=door;
        Check(!PlayerInteractionTarget.PressPrompt(storage,KeyCode.E),"storage updating first leaves visible exit E untouched");
        Check(PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"visible exit consumes E after storage update");
        Time.frameCount=7;WorldInteractionPrompt.displayed=storage;
        Check(!PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"exit updating first leaves visible storage E untouched");
        Check(PlayerInteractionTarget.PressPrompt(storage,KeyCode.E),"visible storage consumes E after exit update");
        var shape=new Collider {isTrigger=true,closest=new Vector3(.5f,1,0)};
        target.components[typeof(Collider)]=shape;
        Check(Math.Abs(PlayerInteractionTarget.SurfaceDistance(player,target)-.5f)<.001f,"trigger surface distance ignores height and imported pivot");
        shape.enabled=false;target.position=new Vector3(0,5,2);
        Check(Math.Abs(PlayerInteractionTarget.SurfaceDistance(player,target)-2f)<.001f,"disabled shape uses horizontal pivot distance");
        var calendar=new UnityEngine.Object();Time.frameCount=8;WorldInteractionPrompt.displayed=door;
        Check(!PlayerInteractionTarget.PressPrompt(calendar,KeyCode.E),"calendar cannot steal the displayed exit E");
        Check(PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"exit still receives E after calendar update");
        Time.frameCount=9;WorldInteractionPrompt.displayed=calendar;
        Check(!PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"exit cannot steal the displayed calendar E");
        Check(PlayerInteractionTarget.PressPrompt(calendar,KeyCode.E),"calendar receives its own E");
        Time.frameCount=10;var machine=new UnityEngine.Object();var trough=new Transform();var exit=new Transform();
        WorldInteractionPrompt.displayed=machine;WorldInteractionPrompt.displayedAnchor=exit;
        Check(!PlayerInteractionTarget.PressPrompt(machine,trough,KeyCode.E),"same controller cannot substitute another anchor for the visible exit");
        Check(PlayerInteractionTarget.PressPrompt(machine,exit,KeyCode.E),"exact owner and anchor can act");
        Time.frameCount=11;WorldInteractionPrompt.displayed=door;
        Check(!PlayerInteractionTarget.PressAction(machine,KeyCode.F),"tool F cannot run under another object's prompt");
        Check(!PlayerInteractionTarget.WasConsumed(KeyCode.F),"rejected tool leaves the key untouched");
        WorldInteractionPrompt.displayed=null;
        Check(PlayerInteractionTarget.PressAction(machine,KeyCode.F),"tool remains usable when there is no world prompt");
        Check(!PlayerInteractionTarget.PressAction(machine,KeyCode.F),"tool cannot consume F twice");
        Time.frameCount=12;WorldInteractionPrompt.displayed=door;UnityEngine.Object.found=controller;controller.IsMovementLocked=true;
        Check(!PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"visible prompt cannot interrupt another movement lock");
        Check(!PlayerInteractionTarget.WasConsumed(KeyCode.E),"blocked session leaves E unused");
        controller.IsMovementLocked=false;
        Check(PlayerInteractionTarget.PressPrompt(door,KeyCode.E),"prompt works after the unrelated movement lock releases");
        Console.WriteLine(checks+" interaction target assertions passed (stub geometry, not Play Mode).");
    }
}
