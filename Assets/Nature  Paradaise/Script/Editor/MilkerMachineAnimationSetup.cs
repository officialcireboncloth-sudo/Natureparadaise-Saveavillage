#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Applies only the authored milking take and its reusable portable-tool presentation, without touching scenes.</summary>
public static class MilkerMachineAnimationSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string MotionPath=Root+"/mesh/Dummy/PlayerDummy/Player.fbx";
    const string PrefabPath=Root+"/Prefabs/Player/Tools/Held_Milker.prefab";

    [MenuItem("Nature Paradise/Tools/Apply Machine Milking Animation")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before applying authored assets.");
        AssetDatabase.ImportAsset(MotionPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(MotionPath);
        var source=importer.defaultClipAnimations.Single(c=>c.name.EndsWith("Milking Animal"));
        var clips=importer.clipAnimations;
        var imported=clips.Single(c=>c.name=="Milking Animal");
        if(imported.firstFrame!=source.firstFrame||imported.lastFrame!=source.lastFrame||imported.loopTime)
        {
            imported.firstFrame=source.firstFrame;imported.lastFrame=source.lastFrame;imported.loopTime=false;imported.loopPose=false;
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        var clip=AssetDatabase.LoadAllAssetsAtPath(MotionPath).OfType<AnimationClip>().Single(c=>c.name=="Milking Animal");
        if(Mathf.Abs(clip.length-3f)>.04f)throw new InvalidOperationException("Export the new 91-frame Milking Animal take first.");
        var set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
        set.milkingAnimal=clip;EditorUtility.SetDirty(set);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Player/Animations/Player Locomotion.controller");
        var machine=controller.layers[0].stateMachine;var state=machine.states.Single(s=>s.state.name=="Milking Animal").state;
        state.motion=clip;state.speed=1;state.mirror=false;
        foreach(var exit in state.transitions)if(exit.hasExitTime){exit.exitTime=1;exit.duration=.06f;EditorUtility.SetDirty(exit);}
        foreach(var enter in machine.anyStateTransitions.Where(t=>t.destinationState==state)){enter.canTransitionToSelf=false;enter.duration=.04f;EditorUtility.SetDirty(enter);}
        EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);
        BuildPortableVisual();
        var catalog=Resources.Load<PlayerToolVisualCatalog>(PlayerToolVisualCatalog.ResourcePath);var entry=catalog.Find("Milker");
        if(!entry.workGrip)entry.carryHandEuler=entry.handEuler;
        entry.workGrip=true;
        var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab"));
        try
        {
            var animator=visual.GetComponentInChildren<Animator>();clip.SampleAnimation(animator.gameObject,1.45f);
            var hand=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="mixamorig:RightHand");
            entry.handEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(Vector3.left,Vector3.up)).eulerAngles;
        }
        finally{Object.DestroyImmediate(visual);}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("[MACHINE MILKING] Native player take applied: crouch, attach cup, steady vacuum, detach, stand. 3 seconds at 1x; sealed canister and independent cup use the existing material.");
    }

    static void BuildPortableVisual()
    {
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/mesh/Prop/Tools/Gameplay/Milker_1.asset");
        var v=original.vertices;var triangles=original.triangles;var cupIndices=new List<int>();var bodyIndices=new List<int>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            var p=(v[triangles[i]]+v[triangles[i+1]]+v[triangles[i+2]])/3;
            var destination=p.x>.19f&&p.y<-.075f?cupIndices:p.x<.12f?bodyIndices:null;
            if(destination!=null){destination.Add(triangles[i]);destination.Add(triangles[i+1]);destination.Add(triangles[i+2]);}
        }
        if(cupIndices.Count<60||bodyIndices.Count<300)throw new InvalidOperationException("Milker source geometry changed; recalibrate cup separation.");
        var bounds=new Bounds(v[cupIndices[0]],Vector3.zero);foreach(int i in cupIndices)bounds.Encapsulate(v[i]);
        Vector3 pivot=bounds.center;
        Mesh body=SavePart(original,bodyIndices,Vector3.zero,"Milker_Canister"),cup=SavePart(original,cupIndices,pivot,"Milker_SuctionCup");
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var model=prefab.transform.Find("Model");model.GetComponent<MeshFilter>().sharedMesh=body;
            var material=model.GetComponent<MeshRenderer>().sharedMaterial;
            Transform Child(string name)
            {
                var child=prefab.transform.Find(name);if(child!=null)return child;
                var created=new GameObject(name).transform;created.SetParent(prefab.transform,false);return created;
            }
            var cupTransform=Child("SuctionCup");cupTransform.localPosition=pivot;cupTransform.localRotation=Quaternion.identity;
            var cupFilter=cupTransform.GetComponent<MeshFilter>();if(cupFilter==null)cupFilter=cupTransform.gameObject.AddComponent<MeshFilter>();cupFilter.sharedMesh=cup;
            var cupRenderer=cupTransform.GetComponent<MeshRenderer>();if(cupRenderer==null)cupRenderer=cupTransform.gameObject.AddComponent<MeshRenderer>();cupRenderer.sharedMaterial=material;
            var hoseTransform=Child("VacuumHose");var hoseFilter=hoseTransform.GetComponent<MeshFilter>();if(hoseFilter==null)hoseFilter=hoseTransform.gameObject.AddComponent<MeshFilter>();
            var hoseRenderer=hoseTransform.GetComponent<MeshRenderer>();if(hoseRenderer==null)hoseRenderer=hoseTransform.gameObject.AddComponent<MeshRenderer>();hoseRenderer.sharedMaterial=material;hoseRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var portable=prefab.GetComponent<PortableMilkerVisual>();if(portable==null)portable=prefab.AddComponent<PortableMilkerVisual>();portable.suctionCup=cupTransform;portable.hose=hoseFilter;portable.cupRestPosition=pivot;
            portable.cupHoseInlet=new Vector3(0,bounds.extents.y*.85f,0);
            int sample=Enumerable.Range(0,v.Length).OrderBy(i=>(v[i]-new Vector3(.17f,.035f,0)).sqrMagnitude).First();portable.hoseUV=original.uv[sample];
            PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
    }
    static Mesh SavePart(Mesh source,List<int> indices,Vector3 pivot,string name)
    {
        var used=indices.Distinct().ToArray();var remap=used.Select((value,index)=>(value,index)).ToDictionary(x=>x.value,x=>x.index);
        string path=Root+"/mesh/Prop/Tools/Gameplay/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool created=mesh==null;if(created)mesh=new Mesh{name=name};else mesh.Clear();
        var vertices=source.vertices;var normals=source.normals;var uv=source.uv;
        mesh.vertices=used.Select(i=>vertices[i]-pivot).ToArray();mesh.normals=used.Select(i=>normals[i]).ToArray();mesh.uv=used.Select(i=>uv[i]).ToArray();mesh.triangles=indices.Select(i=>remap[i]).ToArray();mesh.RecalculateBounds();
        if(created)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);return mesh;
    }
}
#endif
