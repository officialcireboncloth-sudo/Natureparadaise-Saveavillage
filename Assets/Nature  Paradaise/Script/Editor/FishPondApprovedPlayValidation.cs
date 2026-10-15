#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class FishPondApprovedPlayValidation
{
    const string Key="PondA.PlayValidation";
    static double started;
    static bool executed;
    static FishPondApprovedPlayValidation()
    {EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}

    [MenuItem("Nature Paradise/Fish Pond/Validate Runtime Integration",false,122)]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(FishPondApprovedExteriorSetup.ReviewScene);
        SessionState.SetBool(Key,true);executed=false;started=EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;executed=false;}
        if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||executed||EditorApplication.timeSinceStartup-started<1.5)return;
        executed=true;var report=new StringBuilder("FISH POND A PLAY MODE\n");
        void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);report.AppendLine("PASS "+label);}
        try
        {
            var ponds=Object.FindObjectsByType<FishPond>(FindObjectsSortMode.None).OrderBy(p=>p.Level).ToArray();Check(ponds.Length==3,"Three level prefabs instantiate");
            var fish=Resources.Load<ItemSO>("Items/Fish/Tilapia");Check(fish!=null,"Existing fish item available");
            var records=FishPondService.Capture();Check(records.Count==3,"Unique persistent pond IDs");
            foreach(var r in records)
            {
                for(int i=0;i<32;i++)r.fish.Add(new FishPondFishData{itemId=fish.Id,assetName=fish.name,itemName=fish.itemName,sizeCm=20,weightKg=.3f,qualityStars=2});
                r.feedStock=1;
            }
            FishPondService.Restore(records);
            var player=new GameObject("Pond audit player");var inventory=player.AddComponent<Inventory>();
            foreach(var pond in ponds)
            {
                typeof(FishPond).GetField("inventory",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pond,inventory);
                var ext=pond.GetComponent<FishPondExterior>();var maker=pond.GetComponentInChildren<FeedMaker>();
                Check(maker!=null && maker.DedicatedFishFeed && maker.HasDedicatedOutput,"Lv"+pond.Level+" authored feed maker initialized");
                Check(!pond.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("ReplaceMe")),"Lv"+pond.Level+" no runtime prototype blocks");
                var visuals=pond.transform.Cast<Transform>().Last(t=>t.name=="FishVisuals_Runtime" && t.gameObject.activeSelf);int visible=new[]{8,12,18}[pond.Level-1];
                Check(visuals.childCount==visible,"Lv"+pond.Level+" representative fish count "+visible);
                foreach(Transform v in visuals)
                {
                    Check(ext.IsSwimPoint(v.localPosition),v.name+" within pond and outside dock");
                    Check(v.localPosition.y>=.38f && v.localPosition.y<=.50f,v.name+" underwater height");
                }
                player.transform.position=ext.InteractionPoint.position-pond.transform.forward;
                Check(PlayerInteractionTarget.ContainsPickup(player.transform,ext.InteractionPoint,3.5f),"Lv"+pond.Level+" E target reachable from dock approach");
                pond.OpenPanel();Check(pond.IsPanelOpen,"Lv"+pond.Level+" pond UI opens");pond.ClosePanel();
                inventory.slots.ForEach(s=>{if(s!=null)s.count=0;});inventory.Add(fish,1,2,20,.3f);
                int slot=inventory.slots.FindIndex(s=>s?.item==fish&&s.count>0);
                int before=pond.FishCount;pond.AddFish(slot);Check(pond.FishCount==before+1,"Lv"+pond.Level+" deposit into persistent pond");
                pond.TakeFish(pond.FishCount-1);Check(pond.FishCount==before,"Lv"+pond.Level+" withdrawal retains fish data");
                int level=pond.Level;var root=pond.gameObject;Vector3 position=root.transform.position;
                Object.DestroyImmediate(root);
                var replacement=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{FishPondApprovedExteriorSetup.Prefabs}/FishPond_Lv{level}.prefab"),position,Quaternion.identity);
                // Match the authored name, part of the stable standalone pond ID.
                replacement.name="FishPond_Lv"+level;
                replacement.GetComponent<FishPond>().Configure(records.First(r=>r.level==level).pondId,level,Resources.Load<ItemSO>("Items/Fish/Fish Feed"));
                replacement.SetActive(false);replacement.SetActive(true);
                Check(replacement.GetComponent<FishPond>().FishCount==before,"Lv"+level+" replacing visual keeps stored fish");
            }
            var over=FishPondService.Capture();var high=over.First(r=>r.level==3);
            for(int i=high.fish.Count;i<270;i++)high.fish.Add(new FishPondFishData{itemId=fish.Id,assetName=fish.name,itemName=fish.itemName,sizeCm=20});
            high.level=4;FishPondService.Restore(over);
            var final=Object.FindObjectsByType<FishPond>(FindObjectsSortMode.None).First(p=>p.Level==3);
            typeof(FishPond).GetField("inventory",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(final,inventory);
            inventory.Add(fish,1,2,20,.3f);int n=inventory.slots.FindIndex(s=>s?.item==fish&&s.count>0);int amount=inventory.GetCount(fish);
            final.AddFish(n);Check(final.FishCount==270 && inventory.GetCount(fish)==amount,"Over-capacity legacy pond blocks extra deposit without consuming inventory");
            final.TakeFish(269);Check(final.FishCount==269,"Over-capacity legacy pond permits withdrawal");
            report.AppendLine("PLAY PASS");
        }
        catch(Exception e){report.AppendLine("PLAY FAIL: "+e);Debug.LogException(e);}
        finally{File.WriteAllText(FishPondApprovedExteriorSetup.Output+"/UnityPlayAudit.txt",report.ToString());EditorApplication.isPlaying=false;}
    }
}
#endif
