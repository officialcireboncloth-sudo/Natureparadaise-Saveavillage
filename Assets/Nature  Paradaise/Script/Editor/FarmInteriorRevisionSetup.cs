#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

/// <summary>Applies the authored farm fixture revision without replacing interior sockets, save IDs or scene framing.</summary>
public static class FarmInteriorRevisionSetup
{
    const string Root="Assets/Nature  Paradaise",Output=Root+"/mesh/Buildings/FarmInteriorRevision";
    const string Sources=Root+"/Source~/FarmInteriorRevision";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Dictionary<string,Mesh> modules=new();
    static Material atlas,metal,floor,glow,glass;
    static JObject palette;

    [MenuItem("Nature Paradise/Art/Apply Farm Interior Revision")]
    public static void Apply()
    {
        Directory.CreateDirectory(Output+"/Models");AssetDatabase.Refresh();
        palette=(JObject)JObject.Parse(File.ReadAllText(Sources+"/Palette.json"))["palette"];
        atlas=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared Wood Atlas.mat");
        if(atlas==null)throw new InvalidOperationException("Shared wood atlas is required.");
        metal=Material("Farm Aluminium",.60f,.43f);floor=Material("Barn Light Earth",0,.03f);glow=Material("Farm Warm Lamp",0,.1f);
        floor.SetColor("_BaseColor",Color.white);floor.DisableKeyword("_NORMALMAP");floor.SetTexture("_BumpMap",null);
        glow.EnableKeyword("_EMISSION");glow.SetTexture("_EmissionMap",atlas.GetTexture("_BaseMap"));glow.SetColor("_EmissionColor",new Color(1.05f,.72f,.29f));
        glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        glass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/mesh/Buildings/CoopInterior_A/CoopIncubatorGlass.mat");
        modules.Clear();
        foreach(string name in new[]{"ModernFeedMachine","IncubatorBody","IncubatorLidFrame","IncubatorGlass","IncubatorHeater","FeedSack","CornerStraw","WallLamp"})ImportModule(name);
        var report=new JArray();
        foreach(string kind in new[]{"Barn","Coop"})for(int level=1;level<=4;level++)report.Add(ApplyLayout(kind,level));
        foreach(var material in new[]{metal,floor,glow})EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        BarnFeedingCapacitySetup.Apply();
        Directory.CreateDirectory("ArtSource/FarmInteriorRevision");File.WriteAllText("ArtSource/FarmInteriorRevision/AppliedLayouts.json",report.ToString());
    }
    static Material Material(string name,float metallic,float smoothness)
    {
        string path=Output+"/"+name+".mat";var value=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(value==null){value=new Material(atlas);AssetDatabase.CreateAsset(value,path);}else value.CopyPropertiesFromMaterial(atlas);
        value.name=name;value.SetFloat("_Metallic",metallic);value.SetFloat("_Smoothness",smoothness);return value;
    }
    static Mesh Store(string name,Mesh mesh)
    {
        mesh.name=name;string path=Output+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void ImportModule(string name)
    {
        string path=Output+"/Models/"+name+".fbx";File.Copy(Sources+"/Exports/"+name+".fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
        importer.isReadable=true;importer.bakeAxisConversion=true;importer.globalScale=1;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try{var mesh=new Mesh();mesh.CombineMeshes(instance.GetComponentsInChildren<MeshFilter>(true).Select(filter=>new CombineInstance{mesh=filter.sharedMesh,transform=filter.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();modules[name]=Store(name,mesh);}
        finally{Object.DestroyImmediate(instance);}
    }
    static Vector4 Region(string key)
    {var p=palette[key]["uvSafeRectBottomLeft"];return new Vector4((float)p[0],(float)p[1],(float)p[2],(float)p[3]);}
    static Vector2 UV(Vector4 region,float u,float v)=>new(Mathf.Lerp(region.x,region.z,u),Mathf.Lerp(region.y,region.w,v));
    static Vector3 Normalize(Vector3 point,Bounds bounds)=>new(Mathf.InverseLerp(bounds.min.x,bounds.max.x,point.x),Mathf.InverseLerp(bounds.min.y,bounds.max.y,point.y),Mathf.InverseLerp(bounds.min.z,bounds.max.z,point.z));
    static Mesh Paint(Mesh source,string name,string mode)
    {
        var mesh=Object.Instantiate(source);var vertices=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;var bounds=mesh.bounds;
        var silver=Region("aluminum");var bright=Region("aluminum_light");var wood=Region("wood");
        for(int i=0;i<uv.Length;i++)
        {
            var p=Normalize(vertices[i],bounds);
            if(mode=="floor")uv[i]=UV(Region("floor"),.5f,.4f+p.x*.2f);
            else if(mode=="wood")uv[i]=UV(wood,Mathf.Abs(normals[i].y)>.6f?p.x:p.y,p.z);
            else if(mode=="silver")uv[i]=UV(normals[i].y>.6f?bright:silver,.5f,p.y);
            else if(mode=="props"&&(uv[i].x>=.330f&&uv[i].x<=.342f&&uv[i].y>=.128f&&uv[i].y<=.154f||
                uv[i].x>=.314f&&uv[i].x<=.326f&&uv[i].y>=.451f&&uv[i].y<=.468f))
                uv[i]=UV(silver,.5f,Mathf.Repeat(vertices[i].y,1));
        }
        mesh.uv=uv;return Store(name,mesh);
    }
    static GameObject Geometry(Transform parent,string name,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;return go;
    }
    static Mesh Transformed(Mesh source,Matrix4x4 matrix)
    {
        var mesh=new Mesh();mesh.CombineMeshes(new[]{new CombineInstance{mesh=source,transform=matrix}},true,true);mesh.RecalculateBounds();return mesh;
    }
    static Mesh Subset(Mesh source,List<int> triangles)
    {
        var map=new Dictionary<int,int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        var oldVertices=source.vertices;var oldNormals=source.normals;var oldUV=source.uv;
        foreach(int old in triangles)
        {
            if(!map.TryGetValue(old,out int index)){index=vertices.Count;map.Add(old,index);vertices.Add(oldVertices[old]);normals.Add(oldNormals[old]);uv.Add(oldUV[old]);}
            indices.Add(index);
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
    static void SeparateSteelProps(MeshFilter filter,string id)
    {
        var source=filter.sharedMesh;var uv=source.uv;var triangles=source.triangles;var silver=Region("aluminum");var steel=new List<int>();var other=new List<int>();
        bool Silver(int index)=>uv[index].x>=silver.x-.0001f&&uv[index].x<=silver.z+.0001f&&uv[index].y>=silver.y-.0001f&&uv[index].y<=silver.w+.0001f;
        for(int i=0;i<triangles.Length;i+=3){var output=Silver(triangles[i])&&Silver(triangles[i+1])&&Silver(triangles[i+2])?steel:other;output.Add(triangles[i]);output.Add(triangles[i+1]);output.Add(triangles[i+2]);}
        // Reapplication keeps the already separated authored mesh instead of duplicating it.
        if(steel.Count==0)return;
        var previous=filter.transform.Find("Aluminium Water and Bucket Surfaces");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var steelMesh=Store(id+"_WaterContainerSteel",Subset(source,steel));var remaining=Subset(source,other);
        filter.sharedMesh=Store(id+"_Props",remaining);Geometry(filter.transform,"Aluminium Water and Bucket Surfaces",steelMesh,metal);
    }
    static JObject ApplyLayout(string kind,int level)
    {
        string id=kind+"_Lv"+level,path=Root+"/Prefabs/"+kind+"/Interior/"+kind+"InteriorLayout_Lv"+level+".prefab";
        string guid=AssetDatabase.AssetPathToGUID(path);var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var binding=root.GetComponent<BarnInteriorLayout>();var sockets=binding.FeedingSlots.Select(t=>t.localToWorldMatrix).ToArray();
            var colliders=root.GetComponentsInChildren<Collider>(true);var floorCollider=colliders.OfType<BoxCollider>().First(c=>c.name=="Floor");
            Bounds room=new(root.transform.InverseTransformPoint(floorCollider.transform.TransformPoint(floorCollider.center)),Vector3.Scale(floorCollider.size,floorCollider.transform.lossyScale/root.transform.lossyScale.x));
            float minX=room.min.x,maxX=room.max.x,minZ=room.min.z,maxZ=room.max.z,ground=room.max.y;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                string name=filter.name;
                if(name.StartsWith("Trough_")&&name.EndsWith("_Geometry"))
                {filter.sharedMesh=Paint(filter.sharedMesh,id+"_"+name,kind=="Coop"?"silver":"wood");renderer.sharedMaterial=kind=="Coop"?metal:atlas;}
                else if(name=="Floor_Geometry"&&kind=="Barn")
                {filter.sharedMesh=Paint(filter.sharedMesh,id+"_Floor","floor");renderer.sharedMaterial=floor;}
                else if(name=="PropsAndStraw_Geometry")
                {filter.sharedMesh=Paint(filter.sharedMesh,id+"_Props","props");renderer.sharedMaterial=atlas;SeparateSteelProps(filter,id);}
                else if(kind=="Barn"&&name.StartsWith("Machine_N03_"))
                {
                    var old=filter.sharedMesh.bounds;var model=modules["ModernFeedMachine"];var b=model.bounds;
                    float scale=Mathf.Min(old.size.x/b.size.x,old.size.y/b.size.y,old.size.z/b.size.z);
                    Vector3 offset=new(old.center.x-b.center.x*scale,old.min.y-b.min.y*scale,old.center.z-b.center.z*scale);
                    filter.sharedMesh=Store(id+"_ModernMachine",Transformed(model,Matrix4x4.TRS(offset,Quaternion.identity,Vector3.one*scale)));renderer.sharedMaterial=metal;
                }
            }
            var previous=root.transform.Find("Farm Decor Revision");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var decor=new GameObject("Farm Decor Revision").transform;decor.SetParent(root.transform,false);
            var props=new List<CombineInstance>();var lamps=new List<CombineInstance>();
            foreach(int side in new[]{-1,1})foreach(int end in new[]{0,1})
            {
                float x=side<0?minX+.60f:maxX-.60f,z=end==0?minZ+.70f:maxZ-.70f;
                props.Add(new CombineInstance{mesh=modules["CornerStraw"],transform=Matrix4x4.TRS(new Vector3(x,ground+.015f,z),Quaternion.Euler(0,end*39+side*17,0),new Vector3(1.12f,1,1.0f))});
                float wallX=side<0?minX+.08f:maxX-.08f,wallZ=end==0?minZ+1.45f:maxZ-1.45f;
                var rotation=Quaternion.Euler(0,side<0?-90:90,0);var position=new Vector3(wallX,ground+2.35f,wallZ);
                lamps.Add(new CombineInstance{mesh=modules["WallLamp"],transform=Matrix4x4.TRS(position,rotation,Vector3.one*1.25f)});
                if(end==1){var anchor=new GameObject("Warm Light "+side).transform;anchor.SetParent(decor,false);anchor.localPosition=position+rotation*new Vector3(0,.18f,-.30f);var light=anchor.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.77f,.43f);light.range=4;light.intensity=.65f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;}
            }
            for(int i=0;i<2;i++)
            {
                Vector3 p=new(minX+1.15f+i*.55f,ground,minZ+1.18f+i*.12f);var rotation=Quaternion.Euler(0,i==0?-12:18,0);float scale=i==0?1.1f:.86f;
                props.Add(new CombineInstance{mesh=modules["FeedSack"],transform=Matrix4x4.TRS(p,rotation,Vector3.one*scale)});
                var collision=new GameObject("Feed Sack Collision "+i).transform;collision.SetParent(decor,false);collision.localPosition=p;collision.localRotation=rotation;var collider=collision.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.35f*scale,0);collider.size=new Vector3(.50f,.70f,.40f)*scale;
            }
            var combined=new Mesh();combined.CombineMeshes(props.ToArray(),true,true);Geometry(decor,"Burlap Sacks and Corner Straw",Store(id+"_CornerProps",combined),atlas);
            combined=new Mesh();combined.CombineMeshes(lamps.ToArray(),true,true);Geometry(decor,"Warm Wall Lamps",Store(id+"_WallLamps",combined),glow);
            if(kind=="Coop")ReviseIncubator(root,id);
            if(!sockets.SequenceEqual(binding.FeedingSlots.Select(t=>t.localToWorldMatrix)))throw new InvalidOperationException("Feed sockets changed: "+id);
            PrefabUtility.SaveAsPrefabAsset(root,path);
            if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Prefab GUID changed: "+id);
            return new JObject{["layout"]=id,["feedSlots"]=binding.FeedingSlots.Length,["lights"]=2,["shadowedPointLights"]=0,["cornerStrawPiles"]=4,["feedSacks"]=2,["renderers"]=root.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled),["triangles"]=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>()?.enabled==true).Sum(f=>f.sharedMesh.triangles.Length/3)};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void ReviseIncubator(GameObject root,string id)
    {
        var incubator=root.GetComponentInChildren<EggIncubator>(true);if(incubator==null)throw new InvalidOperationException("Existing incubator missing.");
        var nest=(Transform)typeof(EggIncubator).GetField("nest",Flags).GetValue(incubator);
        var cover=(GameObject)typeof(EggIncubator).GetField("coverVisual",Flags).GetValue(incubator);
        var body=incubator.transform.Find("Body");Vector3 offset=body.InverseTransformPoint(nest.position);
        body.GetComponent<MeshFilter>().sharedMesh=Store(id+"_IncubatorBody",Transformed(modules["IncubatorBody"],Matrix4x4.Translate(offset)));body.GetComponent<MeshRenderer>().sharedMaterial=atlas;
        var bodyCollider=body.GetComponent<BoxCollider>();bodyCollider.center=body.GetComponent<MeshFilter>().sharedMesh.bounds.center;bodyCollider.size=body.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        offset=cover.transform.InverseTransformPoint(nest.position);var mesh=new Mesh();mesh.CombineMeshes(new[]{new CombineInstance{mesh=modules["IncubatorLidFrame"],transform=Matrix4x4.Translate(offset)},new CombineInstance{mesh=modules["IncubatorGlass"],transform=Matrix4x4.Translate(offset)}},false,true);
        cover.GetComponent<MeshFilter>().sharedMesh=Store(id+"_OpeningLid",mesh);cover.GetComponent<MeshRenderer>().sharedMaterials=new[]{atlas,glass};
        var old=incubator.transform.Find("Warm Nest Heater");if(old!=null)Object.DestroyImmediate(old.gameObject);
        Geometry(incubator.transform,"Warm Nest Heater",Store(id+"_NestHeater",Transformed(modules["IncubatorHeater"],Matrix4x4.Translate(incubator.transform.InverseTransformPoint(nest.position)))),glow);
    }
}
#endif
