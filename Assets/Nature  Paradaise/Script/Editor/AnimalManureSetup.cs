#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
static class AnimalManureSetup
{
    static AnimalManureSetup(){EditorApplication.delayCall+=EnsureAnimation;}
    [MenuItem("Nature Paradise/Animals/Apply Manure Animation")]
    static void EnsureAnimation()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Nature  Paradaise/Player/Animations/Player Locomotion.controller");
        var clips=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>("Assets/Nature  Paradaise/Player/Animations/Player Animation Set.asset");
        if(controller==null || clips==null || clips.scoopManure==null)return;
        var machine=controller.layers[0].stateMachine;
        // Keep authored keys editable; only fit playback speed to the one-second action.
        var existing=machine.states.FirstOrDefault(s=>s.state.name=="Scoop Manure").state;
        float speed=clips.scoopManure.length/AnimalManureSettings.CollectionSeconds;
        if(existing!=null)
        {
            if(Mathf.Approximately(existing.speed,speed))return;
            existing.speed=speed;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);return;
        }
        if(!controller.parameters.Any(p=>p.name=="ScoopManure"))controller.AddParameter("ScoopManure",AnimatorControllerParameterType.Trigger);
        var state=machine.AddState("Scoop Manure");state.motion=clips.scoopManure;state.speed=speed;
        var enter=machine.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=.06f;enter.canTransitionToSelf=false;
        enter.AddCondition(AnimatorConditionMode.If,0,"ScoopManure");
        var locomotion=machine.states.First(s=>s.state.name=="Locomotion").state;
        var exit=state.AddTransition(locomotion);exit.hasExitTime=true;exit.exitTime=.95f;exit.duration=.08f;
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
    }
}
#endif
