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
        // Existing authored state is left editable; PlayerVisualSetup also creates it on rebuild.
        if(machine.states.Any(s=>s.state.name=="Scoop Manure"))return;
        if(!controller.parameters.Any(p=>p.name=="ScoopManure"))controller.AddParameter("ScoopManure",AnimatorControllerParameterType.Trigger);
        var state=machine.AddState("Scoop Manure");state.motion=clips.scoopManure;state.speed=2f;
        var enter=machine.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=.06f;enter.canTransitionToSelf=false;
        enter.AddCondition(AnimatorConditionMode.If,0,"ScoopManure");
        var locomotion=machine.states.First(s=>s.state.name=="Locomotion").state;
        var exit=state.AddTransition(locomotion);exit.hasExitTime=true;exit.exitTime=.95f;exit.duration=.08f;
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
    }
}
#endif
