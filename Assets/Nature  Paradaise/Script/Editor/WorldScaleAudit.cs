#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Measures actual mesh geometry in meters, including authored transforms; ignores rig helper geometry.</summary>
public static class WorldScaleAudit
{
    const string Root="Assets/Nature  Paradaise";
    public const float PlayerHeight=PlayerDimensions.Height;
    [Serializable] public sealed class Record { public string category,path,name; public Vector3 before,after; public string note; }
    static readonly List<Record> records=new();
    public static bool TryBounds(GameObject root,bool skinOnly,out Bounds bounds)
    {
        Bounds result=default;bool found=false;
        var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer is ParticleSystemRenderer || renderer is LineRenderer || !renderer.enabled) continue;
            if(renderer.GetComponentInParent<Canvas>(true)!=null || renderer.GetComponent<TextMesh>()!=null || renderer.GetComponent<TMPro.TMP_Text>()!=null)continue;
            if(skinOnly && skins.Length>0 && renderer is not SkinnedMeshRenderer)continue;
            if(renderer is SkinnedMeshRenderer skin && skin.sharedMesh!=null)
            {
                // Unity accounts for the root-bone coordinate system here; localBounds are not always in the renderer Transform's axes.
                var world=skin.bounds;
                if(!found){result=world;found=true;}else result.Encapsulate(world);
                continue;
            }
            Bounds local;
            if(renderer is MeshRenderer)
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if(filter==null || filter.sharedMesh==null)continue;
                local=filter.sharedMesh.bounds;
                if(local.size.sqrMagnitude<.000001f)continue;
            }
            else continue;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var point=renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z)));
                if(!found){result=new Bounds(point,Vector3.zero);found=true;}else result.Encapsulate(point);
            }

        }
        bounds=result;return found;
    }
    static Vector3 Size(GameObject root,bool skin=false)=>TryBounds(root,skin,out var b)?b.size:Vector3.zero;
    static void RecordPrefab(string category,string path)
    {
        GameObject go=PrefabUtility.LoadPrefabContents(path);
        try { var size=Size(go,category=="Player"||category=="Animal"); records.Add(new Record{category=category,path=path,name=go.name,before=size,after=size,note=size==Vector3.zero?"No measurable mesh / artwork slot":"Prefab at authored scale; X width, Y height, Z depth"}); }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }
    [MenuItem("Nature Paradise/Scale/Normalize Player and Tools Together to 1.5 m")]
    public static void ApplyAndReport()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before scale authoring");
        records.Clear();
        float playerFactor=1f;
        string playerPath=Root+"/Prefabs/Player/PlayerVisual.prefab";
        var player=PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            Vector3 before=Size(player,true);
            Debug.Log("PLAYER MEASURED BEFORE: "+before);
            var model=player.transform.Find("Model_Editable");if(model==null||before.y<=.01f)throw new InvalidOperationException("Missing player skin/model");
            playerFactor=PlayerHeight/before.y;
            model.localScale*=playerFactor;
            if(TryBounds(player,true,out var measured))model.localPosition-=player.transform.InverseTransformPoint(new Vector3(measured.center.x,measured.min.y,measured.center.z));
            Vector3 after=Size(player,true);
            if(Mathf.Abs(after.y-PlayerHeight)>.005f)throw new InvalidOperationException("Player height calibration failed: "+after.y);
            records.Add(new Record{category="Player",path=playerPath,name=player.name,before=before,after=after,note="Skinned renderer envelope; feet aligned to root pivot"});
            PrefabUtility.SaveAsPrefabAsset(player,playerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");
        foreach(var entry in catalog.entries)
        {
            if(entry?.prefab==null)continue;
            var go=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));
            try
            {
                var size=Size(go);var before=size*entry.scale;
                entry.scale*=playerFactor;
                entry.handPosition*=playerFactor; // Socket offsets are expressed in world meters.
                records.Add(new Record{category="Held tool",path=AssetDatabase.GetAssetPath(entry.prefab),name=entry.id,before=before,after=size*entry.scale,note="Equipped dimensions after catalog scale; grip/rotation preserved"});
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        foreach(string folder in new[]{"Animals","PlayerHouse/Exterior","Barn/Exterior","Coop/Exterior","Buildings","ShopsMobile/Modules","Shops/Testing","FishPond","World"})
        {
            string path=Root+"/Prefabs/"+folder;if(!AssetDatabase.IsValidFolder(path))continue;
            foreach(string asset in AssetDatabase.FindAssets("t:Prefab",new[]{path}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x))
                if(!asset.Contains("TerrainDetail"))RecordPrefab(folder=="Animals"?"Animal":"Structure / object",asset);
        }
        foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{Root+"/Map/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);bool changed=false;
            try
            {
                foreach(var controller in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PlayerController>(true)))
                {
                    var character=controller.GetComponent<CharacterController>();if(character==null)continue;
                    Vector3 before=new(character.radius*2,character.height,character.radius*2);
                    character.height=PlayerHeight;character.radius=PlayerDimensions.Radius;character.center=new Vector3(0,PlayerHeight*.5f,0);character.stepOffset=PlayerDimensions.StepOffset;
                    var visual=controller.transform.Find("PlayerVisual_Editable");
                    if(visual!=null){visual.localScale=Vector3.one;visual.localPosition=Vector3.zero;}
                    records.Add(new Record{category="Scene player collider",path=path,name=controller.name,before=before,after=new Vector3(PlayerDimensions.Radius*2,PlayerHeight,PlayerDimensions.Radius*2),note="Capsule; root position and rotation retained"});
                    changed=true;
                }
                if(changed)EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        WriteReport();
        AssetDatabase.SaveAssets();Debug.Log("WORLD SCALE AUDIT PASSED: "+records.Count+" measured records. Player=1.5m; report Documentation/WorldScaleAudit.md");
    }
    static string V(Vector3 v)=>$"{v.x:F3} × {v.y:F3} × {v.z:F3}";
    static void WriteReport()
    {
        Directory.CreateDirectory(Root+"/Documentation");
        var text=new StringBuilder("# World scale audit — 15 October 2026\n\n1 Unity unit = 1 meter. Dimensions are X width × Y height × Z depth. Mesh AABBs include authored transforms; skinned meshes use Unity world-space renderer bounds (including the root-bone coordinate system), so animals depend on bind pose and growth stage. These measurements are bounding boxes, not anatomical shoulder height or usable interior floor area. Prefab values exclude extra scene instance scaling.\n\nPlayer and held tools use the same uniform scale factor, preserving their visual proportions. Buildings/animals are measured without resizing.\n\n| Category | Object | Before (m) | After (m) | Asset |\n|---|---|---:|---:|---|\n");
        foreach(var row in records)text.AppendLine($"| {row.category} | {row.name} | {V(row.before)} | {V(row.after)} | `{row.path}` |");
        string path=Root+"/Documentation/WorldScaleAudit.md";File.WriteAllText(path,text.ToString());
        File.WriteAllText(Root+"/Documentation/WorldScaleAudit.json","["+string.Join(",",records.Select(x=>JsonUtility.ToJson(x)))+"]");
        AssetDatabase.Refresh();
    }
}
#endif
