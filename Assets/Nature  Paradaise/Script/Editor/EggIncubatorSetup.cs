#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Separate existing coop incubator/hood/egg geometry without touching Blender, player or scenes.</summary>
[InitializeOnLoad]
public static class EggIncubatorSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Output="ArtSource/EggIncubator";
    static EggIncubatorSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        const string request=Output+"/apply.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Move(request,Output+"/Apply_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        try{Apply();}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Nature Paradise/Animals/Apply Egg Incubator System To Coop Layouts")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Output);var report=new StringBuilder("INCUBATOR PREFAB CONNECTIONS\n");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            for(int level=1;level<=4;level++)
            {
                string prefabPath=Root+"/Prefabs/Coop/Interior/CoopInteriorLayout_Lv"+level+".prefab";
                string modelPath=Root+"/mesh/Buildings/CoopInterior_A/CoopInterior_Lv"+level+".fbx";
                var source=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),scene);
                var layout=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    float width=8+level*2,depth=6+level*2;
                    var expected=new Bounds(new Vector3(0,1.61f,depth/2),new Vector3(width+.27f,3.58f,depth+.23f));
                    Quaternion original=source.transform.rotation,chosen=original;float best=float.MaxValue;
                    foreach(int x in new[]{0,90,180,270})foreach(int y in new[]{0,90,180,270})foreach(int z in new[]{0,90,180,270})
                    {
                        source.transform.rotation=Quaternion.Euler(x,y,z)*original;
                        var renderers=source.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                        foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                        float error=Vector3.Distance(bounds.min,expected.min)+Vector3.Distance(bounds.max,expected.max);
                        if(error<best){best=error;chosen=source.transform.rotation;}
                    }
                    source.transform.rotation=chosen;
                    if(best>.3f)throw new InvalidOperationException("Source basis mismatch Lv"+level+": "+best);
                    var old=layout.transform.Find("EggIncubator_Editable");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    var root=new GameObject("EggIncubator_Editable").transform;root.SetParent(layout.transform,false);
                    var nodes=source.GetComponentsInChildren<Transform>(true);
                    string eggName=level==4?"IncubatorEgg_2":"IncubatorEgg_1";
                    var egg=nodes.First(t=>t.name.StartsWith(eggName,StringComparison.Ordinal)&&t.GetComponent<MeshFilter>()!=null);
                    var socket=nodes.First(t=>t.name.StartsWith(level==4?"IncubatorSocket_2":"IncubatorSocket_1",StringComparison.Ordinal));
                    root.localPosition=new Vector3(socket.position.x,0,socket.position.z);
                    Transform Marker(string name,Vector3 world)
                    {var t=new GameObject(name).transform;t.SetParent(root,false);t.position=world;return t;}
                    var nest=Marker("Nest_EggCentre",socket.position);
                    var use=Marker("Interaction_PlayerStand",new Vector3(socket.position.x,0,socket.position.z-.95f));
                    var exit=Marker("Chick_Release",new Vector3(socket.position.x,0,socket.position.z-1.25f));
                    var filters=source.GetComponentsInChildren<MeshFilter>(true);
                    GameObject Geometry(string name,MeshFilter[] parts,Transform frame)
                    {
                        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(frame,false);
                        var mesh=new Mesh{name="Incubator_Lv"+level+"_"+name};
                        mesh.CombineMeshes(parts.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                        mesh.RecalculateBounds();string path=Root+"/mesh/Buildings/CoopInterior_A/Incubator_Lv"+level+"_"+name+".asset";
                        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
                        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=parts[0].GetComponent<Renderer>().sharedMaterial;
                        return go;
                    }
                    var body=Geometry("Body",filters.Where(f=>f.name.StartsWith("C03_")&&!f.name.Contains("_Hood")).ToArray(),root);
                    var cover=Geometry("Cover_Separate",filters.Where(f=>f.name.StartsWith("C03_")&&f.name.Contains("_Hood")).ToArray(),root);
                    // The old Lv4 art had three bowls. This feature explicitly has one slot
                    // at EVERY level, so use the existing single-bowl body/hood at the centre.
                    if(level==4)
                    {
                        body.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/mesh/Buildings/CoopInterior_A/Incubator_Lv1_Body.asset");
                        cover.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/mesh/Buildings/CoopInterior_A/Incubator_Lv1_Cover_Separate.asset");
                    }
                    var eggVisual=Geometry("SingleEgg_Visual",new[]{egg.GetComponent<MeshFilter>()},nest);
                    var collider=body.AddComponent<BoxCollider>();collider.center=body.GetComponent<MeshFilter>().sharedMesh.bounds.center;collider.size=body.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                    foreach(var renderer in layout.GetComponentsInChildren<Renderer>(true))
                        if(renderer.name=="Incubator_Geometry"||renderer.name=="GlassHood_Geometry")renderer.enabled=false;
                    var incubator=root.gameObject.AddComponent<EggIncubator>();incubator.EditorConfigure(nest,use,exit,eggVisual,cover);incubator.RefreshVisual();
                    PrefabUtility.SaveAsPrefabAsset(layout,prefabPath);
                    report.AppendLine($"PASS Lv{level}: existing geometry split, 1 active egg capacity (including Lv4), nest {nest.position}, independent cover/egg, controller linked");
                }
                finally{PrefabUtility.UnloadPrefabContents(layout);Object.DestroyImmediate(source);}
            }
            AssetDatabase.SaveAssets();report.AppendLine("APPLY_PASS: four production coop prefabs; no scene/player/Blender/save changes.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);File.WriteAllText(Output+"/ApplyReport.txt",report.ToString());}
    }
}
#endif
