#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Bakes one visible wooden compartment and one fill renderer per Barn feed slot, using the building catalog as authority.</summary>
public static class BarnFeedingCapacitySetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string MeshFolder=Root+"/mesh/Buildings/BarnFeedingSlots";
    const float CellLength=1.2f,Width=.9f,Height=.65f;

    /// <summary>Updates only the four Barn layout prefabs and shared atlas meshes; scene framing, props and save capacity are preserved.</summary>
    [MenuItem("Nature Paradise/Art/Match Barn Troughs to Feed Capacity")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before editing Barn troughs.");
        var definition=Resources.Load<BuildingDefinitionSO>("Buildings/Barn Building");
        var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared Wood Atlas.mat");
        if(definition==null||wood==null)throw new InvalidOperationException("Barn building catalog and shared wood atlas are required.");
        Directory.CreateDirectory(MeshFolder);AssetDatabase.Refresh();
        var palette=JObject.Parse(File.ReadAllText(Root+"/Source~/FarmInteriorRevision/Palette.json"));
        var rect=palette["palette"]["wood"]["uvSafeRectBottomLeft"];
        var uv=new Vector4((float)rect[0],(float)rect[1],(float)rect[2],(float)rect[3]);
        for(int level=1;level<=4;level++)ApplyLayout(level,definition.GetLevel(level).capacity,wood,uv);
        AssetDatabase.SaveAssets();
    }

    static void ApplyLayout(int level,int capacity,Material wood,Vector4 woodUV)
    {
        string path=Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv"+level+".prefab";
        string guid=AssetDatabase.AssetPathToGUID(path);var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var binding=root.GetComponent<BarnInteriorLayout>();
            var slots=binding.FeedingSlots.ToArray();
            if(slots.Length!=capacity||slots.Any(t=>t==null))throw new InvalidOperationException("Logical Barn slots differ from catalog at Lv."+level);
            var troughs=Enumerable.Range(0,binding.FeedingRoot.childCount).Select(i=>binding.FeedingRoot.GetChild(i))
                .Where(t=>t.Find("TroughCollider")!=null).OrderBy(t=>t.localPosition.x).ToArray();
            if(troughs.Length==0||capacity<troughs.Length)throw new InvalidOperationException("Barn trough layout has no usable rows.");
            var template=binding.FeedFills.First(r=>r!=null);var fillMaterial=template.sharedMaterial;
            var fillMesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder+"/BarnFeedPortion.asset");
            if(fillMesh==null)fillMesh=Store("BarnFeedPortion",FitFill(template.GetComponent<MeshFilter>().sharedMesh));
            foreach(var trough in troughs)
                foreach(var renderer in trough.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Fill_")||r.name.StartsWith("FeedFill_")).ToArray())
                    Object.DestroyImmediate(renderer.gameObject);
            int perRow=capacity/troughs.Length,remainder=capacity%troughs.Length;
            var extras=new HashSet<int>();
            for(int k=0;k<remainder;k++)extras.Add(k%2==0?k/2:troughs.Length-1-k/2);
            var portions=new List<(Transform slot,Renderer fill)>();int marker=0;
            for(int row=0;row<troughs.Length;row++)
            {
                var trough=troughs[row];int count=perRow+(extras.Contains(row)?1:0);float length=count*CellLength;
                var body=trough.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.StartsWith("Trough_")&&f.name.EndsWith("_Geometry"));
                body.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);body.transform.localScale=Vector3.one;
                body.sharedMesh=Store("BarnTrough_"+count+"Compartments",BuildBody(count,woodUV));body.GetComponent<Renderer>().sharedMaterial=wood;
                var collision=trough.Find("TroughCollider").GetComponent<BoxCollider>();
                collision.center=new Vector3(0,Height*.5f,0);collision.size=new Vector3(Width,Height,length);
                for(int cell=0;cell<count;cell++)
                {
                    float z=-length*.5f+(cell+.5f)*CellLength;
                    var slot=slots[marker++];slot.SetParent(trough,false);slot.localPosition=new Vector3(0,.48f,z);
                    slot.localRotation=Quaternion.identity;slot.localScale=Vector3.one;slot.gameObject.SetActive(true);
                    var fill=new GameObject("FeedFill_Temporary",typeof(MeshFilter),typeof(MeshRenderer));fill.transform.SetParent(trough,false);
                    fill.transform.localPosition=new Vector3(0,.48f,z);fill.GetComponent<MeshFilter>().sharedMesh=fillMesh;
                    var renderer=fill.GetComponent<MeshRenderer>();renderer.sharedMaterial=fillMaterial;fill.SetActive(false);
                    portions.Add((slot,renderer));
                }
            }
            // World-local Z points toward the rear wall. The order is independent of the approaching player's side.
            portions=portions.OrderBy(p=>root.transform.InverseTransformPoint(p.slot.position).z)
                .ThenBy(p=>root.transform.InverseTransformPoint(p.slot.position).x).ToList();
            for(int i=0;i<portions.Count;i++){portions[i].slot.name="FeedPortion_"+(i+1).ToString("00");portions[i].fill.name="FeedFill_"+(i+1).ToString("00");}
            binding.EditorConfigure(binding.FeedingRoot,portions.Select(p=>p.slot).ToArray(),portions.Select(p=>p.fill).ToArray(),binding.AnimalSpots,binding.FeedMakerPoint);
            if(binding.FeedFills.Distinct().Count()!=capacity)throw new InvalidOperationException("A feed renderer is shared by multiple logical slots.");
            PrefabUtility.SaveAsPrefabAsset(root,path);
            if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Barn prefab GUID changed.");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static Mesh FitFill(Mesh source)
    {
        var mesh=Object.Instantiate(source);var b=source.bounds;var vertices=source.vertices;var normals=source.normals;
        var scale=new Vector3(.68f/Mathf.Max(.001f,b.size.x),.12f/Mathf.Max(.001f,b.size.y),1.0f/Mathf.Max(.001f,b.size.z));
        for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Scale(vertices[i]-b.center,scale);
        for(int i=0;i<normals.Length;i++)normals[i]=Vector3.Scale(normals[i],new Vector3(1/scale.x,1/scale.y,1/scale.z)).normalized;
        mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
    static Mesh Store(string name,Mesh mesh)
    {
        mesh.name=name;string path=MeshFolder+"/"+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(previous==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(previous);return previous;
    }
    static Mesh BuildBody(int cells,Vector4 woodUV)
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var template=cube.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(cube);
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();float length=cells*CellLength;
        void Plank(Vector3 centre,Vector3 size)
        {
            int offset=vertices.Count;var points=template.vertices;var ns=template.normals;
            for(int i=0;i<points.Length;i++)
            {
                var p=points[i]+Vector3.one*.5f;vertices.Add(centre+Vector3.Scale(points[i],size));normals.Add(ns[i]);
                float u=Mathf.Abs(ns[i].y)>.6f?p.x:p.y,v=Mathf.Abs(ns[i].z)>.6f?p.x:p.z;
                uvs.Add(new Vector2(Mathf.Lerp(woodUV.x,woodUV.z,u),Mathf.Lerp(woodUV.y,woodUV.w,v)));
            }
            triangles.AddRange(template.triangles.Select(i=>i+offset));
        }
        Plank(new Vector3(0,.07f,0),new Vector3(Width,.14f,length));
        foreach(int side in new[]{-1,1})
        {
            Plank(new Vector3(side*(Width*.5f-.05f),.385f,0),new Vector3(.10f,.53f,length));
            Plank(new Vector3(0,.385f,side*(length*.5f-.05f)),new Vector3(Width-.10f,.53f,.10f));
        }
        for(int i=1;i<cells;i++)Plank(new Vector3(0,.385f,-length*.5f+i*CellLength),new Vector3(Width-.10f,.53f,.08f));
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
}
#endif
