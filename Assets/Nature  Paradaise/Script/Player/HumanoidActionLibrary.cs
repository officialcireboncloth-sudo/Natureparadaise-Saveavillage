using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reusable action IDs and Humanoid clips. Every character keeps its own valid Avatar.</summary>
[CreateAssetMenu(menuName="Game/Animation/Humanoid Action Library")]
public sealed class HumanoidActionLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Action
    {
        public string id;
        public AnimationClip clip;
        public bool loop=true;
        [Min(.05f)] public float speed=1;
    }
    public List<Action> actions=new();
    [Min(.01f)] public float blendSeconds=.22f;
    public Action Find(string id)=>actions.Find(a=>a!=null&&a.id==id);
}
