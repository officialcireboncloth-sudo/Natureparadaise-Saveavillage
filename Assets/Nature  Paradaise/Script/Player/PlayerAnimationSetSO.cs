using UnityEngine;

/// <summary>Slot animasi player yang dapat diisi dari clip Humanoid, termasuk FBX Mixamo.</summary>
[CreateAssetMenu(menuName="Game/Player/Animation Set",fileName="Player Animation Set")]
public sealed class PlayerAnimationSetSO : ScriptableObject
{
    [Header("Locomotion")]
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;
    public AnimationClip sprint;

    [Header("Actions")]
    public AnimationClip jump;
    public AnimationClip jumpForward;
    public AnimationClip pickUpFromFloor;
    public AnimationClip knockOut;
    public AnimationClip wakeUpFromKnockOut;
    public AnimationClip milkingAnimal;
    public AnimationClip pushingObject;
    public AnimationClip wateringPlant;
    public AnimationClip useTool;
}
