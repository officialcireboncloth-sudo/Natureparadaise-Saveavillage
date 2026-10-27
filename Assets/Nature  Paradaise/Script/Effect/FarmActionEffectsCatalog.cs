using System;
using UnityEngine;

public enum FarmEffect { Hoe, Sickle, Hammer, HammerBreak, Water, Brush, BrushComplete, Axe, TreeFall, Plant, Fertilize, Harvest, Milk, MilkComplete, Shear, Feed, FishCast, FishCatch, Swing, FallingLeaves }

[CreateAssetMenu(menuName="Nature Paradise/Effects/Farm Actions")]
public sealed class FarmActionEffectsCatalog : ScriptableObject
{
    public Material particleMaterial;
    [Range(.25f,1.5f)] public float density=1;
    [Range(96,768)] public int particleBudget=384;
    public Sound[] sounds=Array.Empty<Sound>();
    [Serializable] public sealed class Sound { public FarmEffect effect;public AudioClip clip;[Range(0,1)] public float volume=.28f; }
    public Sound Audio(FarmEffect effect){foreach(var sound in sounds)if(sound!=null&&sound.effect==effect)return sound;return null;}
}
