using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Builds a compact pack sickle and fits its grip, preserving the authored UI illustration.</summary>
public static class SickleSweepSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const float SickleLength=.65f;
    [MenuItem("Nature Paradise/Player/Apply Wide Sickle Fit")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before fitting the sickle.");
        var catalog=Resources.Load<PlayerToolVisualCatalog>(PlayerToolVisualCatalog.ResourcePath);var entry=catalog!=null?catalog.Find(PlayerToolType.Sickle):null;
        if(catalog==null)throw new InvalidOperationException("Held tool catalog is missing.");
        if(entry==null){entry=new PlayerToolVisualCatalog.Entry{id="Sickle",tool=PlayerToolType.Sickle};catalog.entries.Add(entry);}
        BuildCompactSickle(entry);
        var preview=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab"));
        try{
            var rig=preview.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController!=null);rig.enabled=false;
            var clips=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");clips.idle.SampleAnimation(rig.gameObject,.25f);
            var hand=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand"||t.name=="RightHand");
            entry.scale=1;entry.handPosition=Vector3.zero;entry.carryDirection=new Vector3(.1f,.2f,1);
            entry.carryHandEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(entry.carryDirection.normalized,Vector3.up)).eulerAngles;
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }finally{Object.DestroyImmediate(preview);}
    }
    static void BuildCompactSickle(PlayerToolVisualCatalog.Entry entry)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Pack/Toon Series/Toon Farm Pack/Prefabs/Props/Handheld Tools/TFP_Sickle_01A.prefab");
        if(!source)throw new InvalidOperationException("Toon Pack sickle is missing.");
        var lod=source.GetComponent<LODGroup>();var renderer=lod?lod.GetLODs()[0].renderers[0]:source.GetComponent<MeshRenderer>();
        var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;
        var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
        var points=original.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        float min=points.Min(p=>p.y),max=points.Max(p=>p.y),scale=SickleLength/(max-min);
        // The wooden handle spans the lower end. Keep its centre in the palm; +Z aims at the hooked tip.
        var grip=new Vector3(0,min+(max-min)*.16f,0);
        var tip=points.Where(p=>p.y>max-(max-min)*.2f).OrderBy(p=>p.z).First();
        var rotation=Quaternion.Inverse(Quaternion.LookRotation(Vector3.up,Vector3.forward));
        rotation=Quaternion.FromToRotation(rotation*(tip-grip),Vector3.forward)*rotation;
        var mesh=Object.Instantiate(original);mesh.name="Sickle_1";
        mesh.vertices=points.Select(p=>rotation*(p-grip)*scale).ToArray();
        mesh.normals=original.normals.Select(n=>rotation*matrix.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
        string meshPath=Root+"/mesh/Prop/Tools/Gameplay/Sickle_1.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(saved){
            saved.Clear(false);saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.uv=mesh.uv;saved.tangents=mesh.tangents;saved.subMeshCount=mesh.subMeshCount;
            for(int sub=0;sub<mesh.subMeshCount;sub++)saved.SetTriangles(mesh.GetTriangles(sub),sub);
            saved.bounds=mesh.bounds;saved.UploadMeshData(false);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);
        }else AssetDatabase.CreateAsset(mesh,meshPath);
        AssetDatabase.SaveAssetIfDirty(mesh);
        string path=Root+"/Prefabs/Player/Tools/Held_Sickle.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var model=root.GetComponentInChildren<MeshFilter>();model.sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            var anchor=root.transform.Find("ToolImpact");if(!anchor){anchor=new GameObject("ToolImpact").transform;anchor.SetParent(root.transform,false);}
            anchor.localPosition=rotation*(tip-grip)*scale;
            entry.prefab=PrefabUtility.SaveAsPrefabAsset(root,path);entry.workGrip=true;entry.twoHandsDuringAction=false;entry.twoHandsWhileCarrying=false;
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
