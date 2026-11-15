using System.Collections.Generic;
using UnityEngine;

/// <summary>Contact-driven placeholder animation for the independent capsule rig. Not a player animation.</summary>
public sealed class ConstructionBuilderMotion
{
    readonly Transform model, worker, hammer, saw;
    readonly Dictionary<string,Transform> parts=new();
    Transform board;
    Vector3 boardRest;
    Quaternion boardRotation;
    float workTime, previousTime;
    public string ActionName { get; private set; }="Idle";
    public ConstructionBuilderMotion(Transform owner,Transform character,ConstructionWorkerSettings settings)
    {
        worker=owner;model=character;
        foreach(var t in character.GetComponentsInChildren<Transform>())parts[t.name]=t;
        hammer=Tool(settings?.hammerPrefab,"WorkerHammer");saw=Tool(settings?.sawPrefab,"WorkerSaw");
        Transform Tool(GameObject source,string name)
        {
            if(source==null)return null;
            var t=ConstructionProjectVisuals.VisualClone(source,owner).transform;t.name=name;
            t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;return t;
        }
    }
    public void BindBoard(Transform plank)
    {
        if(board==plank)return;board=plank;
        if(board!=null){boardRest=board.position;boardRotation=board.rotation;}
    }
    Transform Part(string name)=>parts.TryGetValue(name,out var t)?t:null;
    void Pose(string name,Vector3 p,Quaternion q)
    {
        var t=Part(name);if(t==null)return;t.SetPositionAndRotation(model.TransformPoint(p),model.rotation*q);
    }
    void Link(string name,Vector3 a,Vector3 b)
    {
        var t=Part(name);if(t==null)return;
        Pose(name,(a+b)/2,Quaternion.FromToRotation(Vector3.up,b-a));
        t.localScale=new Vector3(1,(b-a).magnitude,1);
    }
    void Arm(string side,Vector3 shoulder,Vector3 hand)
    {
        const float upper=.33f,fore=.31f;
        Vector3 delta=hand-shoulder;float distance=Mathf.Clamp(delta.magnitude,.05f,upper+fore-.005f);
        Vector3 direction=delta.normalized;
        hand=shoulder+direction*distance;
        Vector3 pole=new Vector3(side=="L"?-.8f:.8f,-.6f,-.45f);
        Vector3 bend=(pole-direction*Vector3.Dot(pole,direction)).normalized;
        float along=(upper*upper-fore*fore+distance*distance)/(2*distance);
        Vector3 elbow=shoulder+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        Link("UpperArm"+side,shoulder,elbow);Link("Forearm"+side,elbow,hand);
        Pose("Hand"+side,hand,Quaternion.identity);
    }
    public void Animate(bool walking,bool working,float time)
    {
        float dt=Mathf.Clamp(time-previousTime,0,.1f);previousTime=time;
        if(working)workTime+=dt;
        float t=workTime%18;
        float gait=walking?Mathf.Sin(time*8):0,bob=walking?Mathf.Abs(gait)*.025f:Mathf.Sin(time*2)*.005f;
        float lean=working?12:0;
        Pose("Body",new Vector3(0,1.04f+bob,working?.06f:0),Quaternion.Euler(lean,0,0));
        Pose("Belt",new Vector3(0,.83f+bob,0),Quaternion.identity);
        Pose("Head",new Vector3(0,1.66f+bob,working?.08f:0),Quaternion.Euler(working?12:0,0,0));
        Pose("HatBrim",new Vector3(0,1.84f+bob,working?.11f:0),Quaternion.Euler(working?12:0,0,0));
        Pose("HatCrown",new Vector3(0,1.92f+bob,working?.13f:0),Quaternion.Euler(working?12:0,0,0));
        for(int side=-1;side<=1;side+=2)
        {
            string suffix=side<0?"L":"R";
            Vector3 hip=new Vector3(side*.15f,.65f+bob,0);
            Vector3 ankle=new Vector3(side*.15f,.14f+Mathf.Max(0,gait*side)*.07f,gait*side*.22f);
            Link("Leg"+suffix,hip,ankle);Pose("Boot"+suffix,ankle+new Vector3(0,-.065f,.055f),Quaternion.identity);
        }
        Vector3 left=new Vector3(-.34f,.86f,-gait*.18f),right=new Vector3(.34f,.86f,gait*.18f);
        bool hammering=working&&t>=6&&t<11,sawing=working&&t>=11&&t<16;
        if(hammer!=null)hammer.gameObject.SetActive(hammering);
        if(saw!=null)saw.gameObject.SetActive(sawing);
        ActionName=walking?"Walk":working?t<3?"Pick up board":t<5?"Hold board":t<6?"Place board":hammering?"Hammer":sawing?"Saw":"Inspect / stow":"Idle";
        if(working)
        {
            // Board is physically shared with the bench, never a duplicate appearing in the hands.
            if(t<6)
            {
                float lift=t<3?Mathf.SmoothStep(0,1,t/3):t<5?1:1-Mathf.SmoothStep(0,1,t-5);
                Vector3 center=Vector3.Lerp(new Vector3(0,1.04f,.67f),new Vector3(0,1.2f,.38f),lift);
                left=center+new Vector3(-.31f,.01f,0);right=center+new Vector3(.31f,.01f,0);
                if(board!=null)board.SetPositionAndRotation(worker.TransformPoint(center),worker.rotation);
            }
            else
            {
                if(board!=null)board.SetPositionAndRotation(boardRest,boardRotation);
                left=new Vector3(-.2f,1.10f,.58f);
                if(hammering)
                {
                    float stroke=Mathf.Pow((Mathf.Sin((t-6)*Mathf.PI*3)+1)*.5f,2);
                    right=new Vector3(.25f,1.14f+stroke*.29f,.46f-stroke*.09f);
                    if(hammer!=null)
                    {
                        Quaternion q=Quaternion.Euler(Mathf.Lerp(105,-35,stroke),0,0);
                        hammer.localRotation=q;hammer.localPosition=right;
                    }
                }
                else if(sawing)
                {
                    right=new Vector3(.22f,1.15f,.50f+Mathf.Sin((t-11)*Mathf.PI*3)*.10f);
                    if(saw!=null){saw.localPosition=right;saw.localRotation=Quaternion.Euler(0,90,0);}
                }
                else
                {
                    float settle=Mathf.SmoothStep(0,1,(t-16)/2);
                    left=Vector3.Lerp(left,new Vector3(-.34f,.86f,0),settle);
                    right=Vector3.Lerp(new Vector3(.25f,1.1f,.48f),new Vector3(.34f,.86f,0),settle);
                }
            }
        }
        else if(board!=null)board.SetPositionAndRotation(boardRest,boardRotation);
        Arm("L",new Vector3(-.29f,1.39f+bob,working?.13f:0),left);
        Arm("R",new Vector3(.29f,1.39f+bob,working?.13f:0),right);
    }
}
