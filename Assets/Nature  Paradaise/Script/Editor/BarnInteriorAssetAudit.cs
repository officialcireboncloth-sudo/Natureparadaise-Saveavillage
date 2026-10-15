#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Read-only asset inventory and temporary preview rendering; no gameplay scene writes.</summary>
[InitializeOnLoad]
public static class BarnInteriorAssetAudit
{
    const string Folder="Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs";
    const string Output="Documentation/Design/BarnInteriorReview";
    const string Request=Output+"/audit.request";
    static bool busy;static double next;
    static readonly Regex Candidate=new("^TFP_(Trough_|Hay_|Round_Hay_|Wrapped_Hay_|Wood_Barrel_|Metal_Barrel_|Bucket_|Plastic_Bucket_|Cereal_Sack_|Sack_Pile_|Wooden_Crate_|Shelf_|Bookshelf_|Tool_Rack_|Tool_Storage_|Spading_Fork_|Shovel_|Lamp_|Bottle_|Plastic_Bottle_|Milk_Bottle_|Milk_Chug_|Water_Tank_|Water_Pump_|Hose_|Kitchen_Cabinet_|Bathroom_Cabinet_|Bathroom_Sink_|Barn_0|Hall_|Wooden_Struts_|Conveyor_Belt_|Dirt_Patch_|Pavement_Stone)");
    static readonly string[] Samples={
        "TFP_Trough_01A","TFP_Trough_01B","TFP_Trough_02A","TFP_Trough_03A","TFP_Trough_04A","TFP_Trough_05A","TFP_Trough_06A","TFP_Trough_07A","TFP_Trough_08A","TFP_Trough_09A",
        "TFP_Hay_Bale_01A","TFP_Hay_Bale_02A","TFP_Hay_Bale_03A","TFP_Hay_Bale_04A","TFP_Hay_Clump_01A","TFP_Hay_Patch_01A","TFP_Hay_Patch_02A","TFP_Hay_Silage_01A",
        "TFP_Wood_Barrel_01A","TFP_Wooden_Crate_01A","TFP_Wooden_Crate_03A","TFP_Bucket_Metal_01A","TFP_Cereal_Sack_01A","TFP_Cereal_Sack_02A","TFP_Shelf_01A","TFP_Shelf_02A",
        "TFP_Tool_Rack_01A","TFP_Tool_Rack_05A","TFP_Tool_Rack_10A","TFP_Tool_Rack_15A","TFP_Tool_Rack_19A","TFP_Spading_Fork_01A","TFP_Shovel_01A","TFP_Lamp_01A","TFP_Lamp_02A","TFP_Lamp_03A",
        "TFP_Kitchen_Cabinet_02A","TFP_Bathroom_Cabinet_01A","TFP_Kitchen_Cabinet_Sink_01A","TFP_Bathroom_Sink_01A","TFP_Milk_Bottle_01A","TFP_Milk_Chug_01A","TFP_Bottle_01A","TFP_Water_Tank_01A","TFP_Water_Tank_03A","TFP_Water_Tank_04A",
        "TFP_Barn_05A_Interior","TFP_Hall_Wall_01A","TFP_Hall_Arch_01A","TFP_Barn_05A_Module_1","TFP_Barn_05A_Module_2","TFP_Barn_05A_Module_3","TFP_Barn_05A_Module_4",
        "TFP_Bookshelf_01A","TFP_Bookshelf_02A","TFP_Bookshelf_03A","TFP_Tool_Storage_01A","TFP_Tool_Storage_02A","TFP_Dirt_Patch_01A","TFP_Pavement_Stones_01A"};
    static BarnInteriorAssetAudit(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try{File.Move(Request,Output+"/AuditRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Run();}
        catch(Exception e){File.WriteAllText(Output+"/AuditError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    [MenuItem("Nature Paradise/Audit/Barn Interior Toonpack Inventory")]
    public static void Run()
    {
        Directory.CreateDirectory(Output+"/AssetPreviews");
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
        File.WriteAllLines(Output+"/all-pack-prefabs.txt",paths);
        var inventory=new JArray();var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        var html=new StringBuilder("<!doctype html><meta charset='utf-8'><title>Toonpack — Barn interior candidates</title><style>body{font:16px system-ui;margin:25px;background:#eee9df;color:#333}.grid{display:grid;grid-template-columns:repeat(4,minmax(220px,1fr));gap:15px}figure{margin:0;padding:12px;background:white}img{width:100%;height:220px;object-fit:contain}small{display:block;overflow-wrap:anywhere}</style><h1>Toonpack — kandidat interior Barn</h1><p>Asset asli project, tanpa hewan. Preview dinormalisasi ukurannya agar mudah dibandingkan; lihat ukuran meter dan jumlah triangle per prefab. Ini inventory, bukan layout baru.</p><div class='grid'>");
        try
        {
            var sun=new GameObject("Inventory Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var camera=new GameObject("Inventory Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.orthographic=true;camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.74f,.70f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(420,340,24,RenderTextureFormat.ARGB32);rt.Create();
            foreach(var path in paths.Where(p=>Candidate.IsMatch(Path.GetFileNameWithoutExtension(p))))
            {
                string name=Path.GetFileNameWithoutExtension(path);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)continue;
                var meshes=prefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Where(m=>m).ToArray();
                long tris=meshes.Sum(m=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3));
                var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>AssetDatabase.GetAssetPath(m)).ToArray();
                var entry=new JObject{["name"]=name,["path"]=path,["guid"]=AssetDatabase.AssetPathToGUID(path),["trianglesAllMeshFilters"]=tris,["verticesAllMeshFilters"]=meshes.Sum(m=>m.vertexCount),["meshFilters"]=meshes.Length,["LODGroups"]=prefab.GetComponentsInChildren<LODGroup>(true).Length,["lights"]=prefab.GetComponentsInChildren<Light>(true).Length,["materials"]=new JArray(materials)};
                if(Samples.Contains(name))
                {
                    var go=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.SetActive(true);
                    foreach(var light in go.GetComponentsInChildren<Light>(true))light.enabled=false;
                    var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer && r.enabled).ToArray();
                    if(rs.Length>0)
                    {
                        Bounds b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                        entry["sizeMetres"]=new JArray(b.size.x,b.size.y,b.size.z);
                        float size=Mathf.Max(b.size.x,b.size.y,b.size.z,.1f);camera.orthographicSize=size*.77f;camera.transform.position=b.center+new Vector3(size*1.4f,size*.95f,size*1.7f);camera.transform.LookAt(b.center);
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
                        string imagePath="AssetPreviews/"+name+".png";File.WriteAllBytes(Output+"/"+imagePath,image.EncodeToPNG());Object.DestroyImmediate(image);entry["preview"]=imagePath;
                        html.Append("<figure><img src='").Append(imagePath).Append("'><figcaption><b>").Append(name).Append("</b><small>").Append(tris).Append(" tris · ").Append(b.size.ToString("F2")).Append(" m</small><small>").Append(path).Append("</small></figcaption></figure>");
                    }
                    Object.DestroyImmediate(go);
                }
                inventory.Add(entry);
                File.WriteAllText(Output+"/AuditProgress.txt",name+" · candidates "+inventory.Count);
            }
            html.Append("</div><p>Triangle counts sum all MeshFilters; if a prefab contains an LODGroup this includes all LODs, not one active LOD.</p>");
            File.WriteAllText(Output+"/asset-gallery.html",html.ToString());File.WriteAllText(Output+"/asset-inventory.json",new JObject{["status"]="PASS",["packRoot"]=Folder,["totalPrefabs"]=paths.Length,["candidates"]=inventory,["sceneAssetsChanged"]=false}.ToString());
            Debug.Log("BARN_INTERIOR_ASSET_AUDIT_PASS: "+inventory.Count+" candidates; gameplay scenes unchanged.");
        }
        finally{if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
