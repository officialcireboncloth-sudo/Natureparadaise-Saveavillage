using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Small per-character clip player. No player input/controller, no animation events or root movement.</summary>
public sealed class HumanoidActionPlayer : IDisposable
{
    readonly PlayableGraph graph;
    readonly AnimationMixerPlayable mixer;
    readonly HumanoidActionLibrary library;
    readonly List<AnimationClipPlayable> clips=new();
    readonly List<HumanoidActionLibrary.Action> entries=new();
    readonly Dictionary<string,int> indices=new();
    readonly List<float> weights=new();
    int target;
    public string ActionId { get; private set; }="Idle";
    public bool HasClip(string id)=>indices.ContainsKey(id);
    public HumanoidActionPlayer(Animator animator,HumanoidActionLibrary source)
    {
        if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)
            throw new ArgumentException("Humanoid character requires its own valid Humanoid Avatar.");
        library=source;
        if(source==null||source.Find("Idle")?.clip==null)throw new ArgumentException("Humanoid library requires Idle.");
        var valid=source.actions.FindAll(a=>a!=null&&a.clip!=null);
        if(valid.Exists(a=>!a.clip.isHumanMotion))throw new ArgumentException("Shared action library clips must be imported as Humanoid, not Generic.");
        var ids=new HashSet<string>();
        foreach(var entry in valid)if(string.IsNullOrWhiteSpace(entry.id)||!ids.Add(entry.id))
            throw new ArgumentException("Humanoid action IDs must be nonempty and unique.");
        animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.fireEvents=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
        animator.Rebind();
        graph=PlayableGraph.Create("Humanoid Actions "+animator.GetInstanceID());graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer=AnimationMixerPlayable.Create(graph,valid.Count);
        foreach(var entry in valid)
        {
            int i=clips.Count;var clip=AnimationClipPlayable.Create(graph,entry.clip);
            clip.SetApplyFootIK(false);clip.SetApplyPlayableIK(false);clip.SetSpeed(entry.speed);
            graph.Connect(clip,0,mixer,i);indices[entry.id]=i;clips.Add(clip);entries.Add(entry);weights.Add(0);
        }
        target=indices["Idle"];weights[target]=1;mixer.SetInputWeight(target,1);
        var output=AnimationPlayableOutput.Create(graph,"Humanoid",animator);output.SetSourcePlayable(mixer);
        graph.Play();graph.Evaluate(0);
    }
    public void Play(string id)
    {
        int next=indices.TryGetValue(id,out var index)?index:indices["Idle"];
        ActionId=id;
        if(next==target)return;
        target=next;clips[target].SetTime(0);
    }
    public void Evaluate(float delta,float walkSpeed=1)
    {
        delta=Mathf.Clamp(delta,0,.1f);
        float blend=1-Mathf.Exp(-delta*4/Mathf.Max(.01f,library.blendSeconds));
        float total=0;
        for(int i=0;i<clips.Count;i++)
        {
            weights[i]=Mathf.Lerp(weights[i],i==target?1:0,blend);total+=weights[i];
            float speed=entries[i].speed*(entries[i].id=="Walk"?walkSpeed:1);
            clips[i].SetSpeed(speed);
            double length=Math.Max(.001,entries[i].clip.length);
            double t=clips[i].GetTime();
            if(entries[i].loop&&t>=length)clips[i].SetTime(t%length);
            else if(!entries[i].loop&&t>=length)clips[i].SetTime(length);
        }
        for(int i=0;i<clips.Count;i++)mixer.SetInputWeight(i,weights[i]/Mathf.Max(.0001f,total));
        graph.Evaluate(delta);
    }
    public void Dispose(){if(graph.IsValid())graph.Destroy();}
}
