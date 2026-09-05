using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
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
    public static class Mathf {public static float Abs(float value)=>Math.Abs(value);}
    public enum KeyCode {E,F}
    public static class Input {public static bool pressed;public static bool GetKeyDown(KeyCode key)=>pressed;}
    public static class Time {public static int frameCount;}
}
public class PlayerController {public bool IsMovementLocked;public Vector3 FacingDirection=Vector3.forward;}
public static class WorldInteractionPrompt {public static bool IsSuppressed;}
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
        Check(PlayerInteractionTarget.Press(player,target,KeyCode.E),"first E");
        Check(!PlayerInteractionTarget.Press(player,target,KeyCode.E),"second E same frame rejected");
        Time.frameCount=2;Check(PlayerInteractionTarget.Press(player,target,KeyCode.E),"next press accepted");
        Console.WriteLine(checks+" interaction target assertions passed (stub geometry, not Play Mode).");
    }
}
