#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Temporary isolated native Play Mode review, archived outside Assets after verification.
public sealed class HouseExteriorPlayReview : MonoBehaviour
{
    internal const string Fixture="Assets/HouseExteriorRegression_Test.unity";
    internal const string Active="Library/HouseExteriorPlayReview.active";
    internal const string Result="ArtSource/House_A/Previews/UnityPlayAudit.txt";
    const string Root="Assets/Nature  Paradaise";
    readonly List<string> report=new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if(File.Exists(Active) && SceneManager.GetActiveScene().name=="HouseExteriorRegression_Test")
            new GameObject("House exterior review").AddComponent<HouseExteriorPlayReview>();
    }
    void Check(bool pass,string label){if(!pass)throw new Exception(label);report.Add("PASS "+label);}
    IEnumerator Start()
    {
        var run=Test();Exception error=null;
        while(true)
        {
            object wait=null;bool more=false;
            try{more=run.MoveNext();if(more)wait=run.Current;}catch(Exception e){error=e;}
            if(error!=null||!more)break;
            yield return wait;
        }
        report.Add(error==null?"PLAY_PASS":"FAIL "+error);
        File.WriteAllLines(Result,report);
        if(error!=null)Debug.LogException(error);
        EditorApplication.isPlaying=false;
    }
    static void Teleport(CharacterController cc,Vector3 p)
    {
        cc.enabled=false;cc.transform.position=p;cc.enabled=true;Physics.SyncTransforms();
    }
    IEnumerator Test()
    {
        Check(Object.FindFirstObjectByType<SaveManager>()==null,"fixture has no SaveManager; no player save altered");
        var ground=new GameObject("Ground");var floor=ground.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.5f,0);floor.size=new Vector3(100,1,100);
        var player=new GameObject("Player review");var cc=player.AddComponent<CharacterController>();
        cc.height=3.6f;cc.center=new Vector3(0,1.8f,0);cc.radius=.4f;cc.stepOffset=.35f;cc.skinWidth=.04f;
        var controller=player.AddComponent<PlayerController>();
        foreach(var component in player.GetComponents<MonoBehaviour>())component.enabled=false;
        var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Root+"/Resources/Buildings/Player House Building.asset");
        var houseObject=new GameObject("Player house");houseObject.SetActive(false);houseObject.transform.rotation=Quaternion.Euler(0,180,0);
        var house=houseObject.AddComponent<PlayerHouseController>();
        var data=new SerializedObject(house);data.FindProperty("houseDefinition").objectReferenceValue=definition;data.ApplyModifiedPropertiesWithoutUndo();
        houseObject.SetActive(true);
        var transition=SceneTransitionManager.Instance;
        if(transition==null)transition=new GameObject("Transition review").AddComponent<SceneTransitionManager>();
        yield return null;
        Mesh level3=null;
        foreach(int level in new[]{1,2,3,4,5,1})
        {
            house.Restore(new PlayerHouseSaveData{currentLevel=level});
            var portals=house.GetComponentsInChildren<HouseScenePortal>();
            Check(portals.Length==1,$"Lv{level} swap has exactly one active stair portal immediately");
            Check(house.GetComponentsInChildren<PlayerSpawnPoint>().Length==1,$"Lv{level} one return marker immediately");
            yield return null;
            var portal=portals[0];var exterior=portal.transform.parent;
            Check(exterior.name==$"PlayerHouse_Lv{level}_Runtime",$"Lv{level} actual controller selects correct exterior");
            Check(Vector3.Dot(exterior.forward,Vector3.back)>.999f,$"Lv{level} doorway faces fixed camera");
            var mesh=exterior.Find("House_A_Model/House_A_LOD0").GetComponent<MeshFilter>().sharedMesh;
            if(level==3)level3=mesh;
            if(level==4)Check(mesh==level3,"Lv3 and Lv4 share the same mesh and scale");
            float pd=level==1?2.2f:level==2?2.8f:level<=4?2.4f:3;
            // Walk from approach over all three real stair colliders onto porch.
            Teleport(cc,exterior.TransformPoint(new Vector3(0,.03f,pd+2.4f)));
            Vector3 target=exterior.TransformPoint(new Vector3(0,0,pd-.55f));
            for(int frame=0;frame<85;frame++)
            {
                Vector3 delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.04f)break;
                cc.Move(Vector3.ClampMagnitude(delta,.06f)+Vector3.down*.015f);
                yield return null;
            }
            Vector3 local=exterior.InverseTransformPoint(player.transform.position);
            Check(Mathf.Abs(local.z-(pd-.55f))<.12f && local.y>.5f,$"Lv{level} CharacterController walks stairs onto porch ({local})");
            Teleport(cc,exterior.TransformPoint(new Vector3(0,.4f,pd+.75f)));
            Check(portal.CanInteract(player.transform),$"Lv{level} entrance available on stairs");
            Teleport(cc,exterior.TransformPoint(new Vector3(0,0,-12)));
            Check(!portal.CanInteract(player.transform),$"Lv{level} rear facade cannot enter");
            if(level==1 || level==5)
            {
                Teleport(cc,exterior.TransformPoint(new Vector3(0,.4f,pd+.75f)));
                Check(transition.EnterInterior("HouseInterior","house-interior-entry"),$"Lv{level} enter authored interior starts");
                float deadline=Time.realtimeSinceStartup+30;
                while(transition.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;
                Check(transition.IsInsideInterior&&!transition.IsTransitioning,$"Lv{level} enter finishes");
                Check(PlayerSpawnPoint.TryGet("house-interior-entry",out var entry)&&Vector3.Distance(player.transform.position,entry.transform.position)<.1f,$"Lv{level} reaches actual interior spawn");
                Check(transition.ReturnToWorld("player-house-exit"),$"Lv{level} return starts");
                deadline=Time.realtimeSinceStartup+30;
                while(transition.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;
                Check(!transition.IsInsideInterior&&!transition.IsTransitioning,$"Lv{level} return finishes");
                Check(PlayerSpawnPoint.TryGet("player-house-exit",out var exit)&&Vector3.Distance(player.transform.position,exit.transform.position)<.1f,$"Lv{level} returns to current exterior stairs approach");
                Check(!controller.IsMovementLocked,$"Lv{level} transition releases movement lock");
            }
        }
    }
}

[InitializeOnLoad]
public static class HouseExteriorPlayReviewEditor
{
    const string Request="Library/HouseExteriorPlayReview.request";
    static HouseExteriorPlayReviewEditor(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Timeout;EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string task=File.ReadAllText(Request).Trim();File.Delete(Request);
        if(task=="apply")
        {
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HouseApprovedExteriorSetup")).First(t=>t!=null);
            setup.GetMethod("Apply").Invoke(null,null);Render();
        }
        else if(task=="render")Render();else Run();
    }
    public static void Run()
    {
        Render();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene,HouseExteriorPlayReview.Fixture);
        File.WriteAllText("Library/HouseExteriorPlayReview.previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(HouseExteriorPlayReview.Fixture);
        File.WriteAllText(HouseExteriorPlayReview.Active,DateTime.UtcNow.AddMinutes(3).Ticks.ToString());
        EditorApplication.EnterPlaymode();
    }
    static void Timeout()
    {
        if(!File.Exists(HouseExteriorPlayReview.Active))return;
        if(DateTime.UtcNow.Ticks<long.Parse(File.ReadAllText(HouseExteriorPlayReview.Active)))return;
        File.WriteAllText(HouseExteriorPlayReview.Result,"FAIL Play Mode timed out");
        if(Application.isBatchMode)EditorApplication.Exit(1);
        else EditorApplication.isPlaying=false;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!File.Exists(HouseExteriorPlayReview.Active))return;
        string previous=File.ReadAllText("Library/HouseExteriorPlayReview.previous");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        File.Delete(HouseExteriorPlayReview.Active);
        bool passed=File.Exists(HouseExteriorPlayReview.Result)&&File.ReadAllText(HouseExteriorPlayReview.Result).Contains("PLAY_PASS");
        if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);
        else EditorSceneManager.OpenScene("Assets/Nature  Paradaise/Map/Scenes/World/Map.unity",OpenSceneMode.Single);
    }
    public static void Render()
    {
        for(int level=1;level<=5;level++)
        {
            var preview=new PreviewRenderUtility();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Nature  Paradaise/Prefabs/PlayerHouse/Exterior/HouseExterior_Lv{level}.prefab");
            var instance=Object.Instantiate(prefab);preview.AddSingleGO(instance);
            var group=instance.GetComponentInChildren<LODGroup>();group.ForceLOD(0);
            // Static preview may not tick LOD culling before the render; isolate LOD0 explicitly.
            var lods=group.GetLODs();
            for(int lod=1;lod<lods.Length;lod++)foreach(var r in lods[lod].renderers)r.gameObject.SetActive(false);
            if(level==5)
            {
                var f=lods[0].renderers[0].GetComponent<MeshFilter>();var mesh=f.sharedMesh;
                var audit=new List<string>{"rotation="+f.transform.eulerAngles};
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var indices=mesh.GetTriangles(sub);var vertices=mesh.vertices;var uv=mesh.uv;
                    for(int k=0;k<indices.Length;k+=3)
                    {
                        var a=f.transform.TransformPoint(vertices[indices[k]]);var b=f.transform.TransformPoint(vertices[indices[k+1]]);var c=f.transform.TransformPoint(vertices[indices[k+2]]);
                        var center=(a+b+c)/3;
                        if(Mathf.Abs(center.x)<2 && center.y>2 && center.y<4.8f && Mathf.Abs(center.z)<.8f)
                            audit.Add(sub+" center="+center.ToString("F3")+" normal="+Vector3.Cross(b-a,c-a).normalized.ToString("F3")+" uv="+uv[indices[k]].ToString("F5"));
                    }
                }
                File.WriteAllLines("ArtSource/House_A/Previews/UnityDoorGeometry.txt",audit);
            }
            var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=10.8f;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            camera.transform.position=new Vector3(-23.5f,22.43f,19.755f);camera.transform.LookAt(new Vector3(-1.5f,3.73f,-2.245f));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.25f,.3f);
            preview.ambientColor=new Color(.7f,.73f,.76f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(45,-35,0);
            preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(30,140,0);
            try
            {
                preview.BeginStaticPreview(new Rect(0,0,1440,1000));preview.Render(true);
                Texture2D image=preview.EndStaticPreview();
                File.WriteAllBytes($"ArtSource/House_A/Previews/House_A_Lv{level}_Unity.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally{preview.Cleanup();}
        }
    }
}
#endif
