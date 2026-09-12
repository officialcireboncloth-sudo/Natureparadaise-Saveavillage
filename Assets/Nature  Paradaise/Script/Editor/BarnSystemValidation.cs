using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class BarnSystemValidation
{
    [MenuItem("Nature Paradise/Barn/Validation/Run Feed and Capacity Check",false,900)]
    public static void Validate()
    {
        GameObject owner=new("FeedMakerValidation_Temporary");
        try
        {
            FeedMaker machine=owner.AddComponent<FeedMaker>();
            var jobs=(List<FeedJob>)typeof(FeedMaker).GetField("jobs",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(machine);
            var settings=new SerializedObject(machine);
            settings.FindProperty("level").intValue=2;
            settings.FindProperty("outputCapacity").intValue=10;
            settings.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<3;i++) jobs.Add(new FeedJob {inputs=3,output=5,hours=4});
            machine.Advance(0);
            Require(jobs[0].finish==4 && jobs[1].finish==4 && jobs[2].finish<0,"parallel slots / output reservation");
            machine.Advance(24);
            Require(machine.Output==10 && jobs.Count==1 && jobs[0].finish<0,"sleep and full output preserve waiting job");
            typeof(FeedMaker).GetField("output",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(machine,0);
            machine.Advance(24);
            Require(jobs[0].finish==28,"blocked job does not get retroactive time");
            machine.Advance(28);
            Require(machine.Output==5 && jobs.Count==0,"resume after collect");
            typeof(FeedMaker).GetField("output",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(machine,0);
            settings.Update(); settings.FindProperty("level").intValue=1; settings.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<2;i++) jobs.Add(new FeedJob {inputs=5,output=5,hours=4});
            machine.Advance(30); machine.Advance(54);
            Require(machine.Output==10 && jobs.Count==0,"sleep completes consecutive batches");
            foreach(var pair in new[]{("Barn",new[]{4,8,14,20}),("Coop",new[]{6,12,20,30})})
            {
                var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>($"Assets/Nature  Paradaise/Resources/Buildings/{pair.Item1} Building.asset");
                for(int i=0;i<4;i++) Require(definition.GetLevel(i+1)?.capacity==pair.Item2[i],$"{pair.Item1} capacity level {i+1}");
            }
            Debug.Log("[BARN TEST] PASS: parallel processing, full-output blocking, sleep catch-up, resume, and all 8 building capacity levels.");
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }
    static void Require(bool value,string label) { if(!value) throw new InvalidOperationException("Barn validation failed: "+label); }
}
