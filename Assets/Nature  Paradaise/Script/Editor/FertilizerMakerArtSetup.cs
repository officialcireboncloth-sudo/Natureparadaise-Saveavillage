#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

/// <summary>Import the Blender station through the same shared-atlas workflow as the soil scanner.</summary>
[InitializeOnLoad]
public static class FertilizerMakerArtSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Props/FertilizerMaker/";
    const string Textures=Root+"/Textures/Shared/Cozy/";
    const string PrefabPath=Root+"/Resources/World/FertilizerMaker_Dummy.prefab";
    const string Output="ArtSource/FertilizerMaker/Review";
    const string Request="ArtSource/FertilizerMaker/ApplyArt.request";
    static double nextCheck;
    public static bool IsAvailable=>File.Exists(Models+"FertilizerMaker_Cozy_LOD0.json");
    static FertilizerMakerArtSetup(){EditorApplication.update+=ProcessRequest;}
    static void ProcessRequest()
    {
        if(EditorApplication.timeSinceStartup<nextCheck||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        nextCheck=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request))return;
        if(File.GetLastWriteTimeUtc(typeof(FertilizerMakerArtSetup).Assembly.Location)<File.GetLastWriteTimeUtc(Root+"/Script/Editor/FertilizerMakerArtSetup.cs"))return;
        File.Delete(Request);
        try{Apply();}
        catch(Exception error){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ArtImportError.txt",error.ToString());Debug.LogException(error);}
    }
    static Vector3 V(JToken value)=>new((float)value[0],(float)value[1],(float)value[2]);
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException("Fertilizer art: "+message);}
    static Material Extension()
    {
        foreach(string suffix in new[]{"BaseColor","Normal","SurfaceMask"})
        {
            string path=Textures+"NP_CozySurfaces_05_"+suffix+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=suffix=="BaseColor";importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=512;importer.isReadable=false;
            foreach(string platform in new[]{"Android","iPhone"})
            {
                var settings=importer.GetPlatformTextureSettings(platform);settings.overridden=true;
                settings.maxTextureSize=512;settings.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }
        string materialPath=Root+"/Material/Shared/NP_CozySurfaces_05.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var shared=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/NP_CozySurfaces_01.mat");
        Check(shared!=null,"Existing building/prop atlas material 01 available");
        if(material==null){material=new Material(shared){name="NP_CozySurfaces_05"};AssetDatabase.CreateAsset(material,materialPath);}
        else material.CopyPropertiesFromMaterial(shared);
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"NP_CozySurfaces_05_BaseColor.png"));
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"NP_CozySurfaces_05_Normal.png"));
        material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"NP_CozySurfaces_05_SurfaceMask.png"));
        material.SetColor("_BaseColor",Color.white);material.SetFloat("_PresentationFade",1);material.enableInstancing=true;
        EditorUtility.SetDirty(material);return material;
    }
    [MenuItem("Nature Paradise/Soil/Apply Blender Fertilizer Maker Art")]
    public static GameObject Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before importing station art.");
        Check(IsAvailable,"Blender export exists");Directory.CreateDirectory(Output);
        var extension=Extension();
        var materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/NP_CozySurfaces_01.mat"),extension};
        var meshes=new Mesh[3];var report=new StringBuilder("FERTILIZER MAKER NATIVE ART IMPORT\n");
        for(int lod=0;lod<3;lod++)
        {
            string name="FertilizerMaker_Cozy_LOD"+lod;var spec=JObject.Parse(File.ReadAllText(Models+name+".json"));
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};
            mesh.vertices=spec["vertices"].Select(V).ToArray();mesh.normals=spec["normals"].Select(V).ToArray();
            mesh.uv=spec["uv"].Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();mesh.subMeshCount=2;
            for(int sub=0;sub<2;sub++)mesh.SetTriangles(spec["submeshes"][sub].Select(i=>(int)i).ToArray(),sub);
            mesh.RecalculateBounds();mesh.RecalculateTangents();
            Check(mesh.triangles.Length/3==(int)spec["triangles"],name+" triangle count matches Blender");
            Check(mesh.vertices.Length==mesh.normals.Length&&mesh.vertices.Length==mesh.uv.Length,name+" complete vertex attributes");
            Check(mesh.uv.All(u=>float.IsFinite(u.x)&&float.IsFinite(u.y)&&u.x>=0&&u.x<=1&&u.y>=0&&u.y<=1),name+" valid atlas UVs");
            Check(Vector3.Distance(mesh.bounds.min,V(spec["min"]))<.001f&&Vector3.Distance(mesh.bounds.max,V(spec["max"]))<.001f,name+" metre bounds match Blender");
            string path=Models+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);meshes[lod]=mesh;}
            else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);meshes[lod]=existing;}
            report.AppendLine($"LOD{lod}: {meshes[lod].triangles.Length/3} triangles, two shared atlas materials");
        }
        Check(meshes[0].triangles.Length>meshes[1].triangles.Length&&meshes[1].triangles.Length>meshes[2].triangles.Length,"LOD triangle counts decrease");
        string guidBefore=AssetDatabase.AssetPathToGUID(PrefabPath);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var machine=root.GetComponent<FertilizerProcessor>();Check(machine!=null,"Existing gameplay component retained");
            string saveId=machine.ProcessorId;
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())
                if(child.name=="Visual_Dummy_ReplaceLater"||child.name=="ProcessorVisual_ReplaceMe"||child.name=="Visual_FertilizerMaker_Cozy")Object.DestroyImmediate(child.gameObject);
            var visual=new GameObject("Visual_FertilizerMaker_Cozy");visual.transform.SetParent(root.transform,false);
            var group=visual.AddComponent<LODGroup>();var levels=new LOD[3];
            for(int lod=0;lod<3;lod++)
            {
                var part=new GameObject("FertilizerMaker_Cozy_LOD"+lod,typeof(MeshFilter),typeof(MeshRenderer));
                part.transform.SetParent(visual.transform,false);part.GetComponent<MeshFilter>().sharedMesh=meshes[lod];
                var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;
                levels[lod]=new LOD(new[]{.34f,.14f,.025f}[lod],new Renderer[]{renderer});
            }
            group.SetLODs(levels);group.RecalculateBounds();group.fadeMode=LODFadeMode.None;
            var body=root.GetComponent<BoxCollider>();Check(body!=null,"Existing collision body retained");
            body.center=meshes[0].bounds.center;body.size=meshes[0].bounds.size;
            Check(root.GetComponentsInChildren<Collider>(true).Length==1,"Station has exactly one collision body");
            Check(root.GetComponentsInChildren<Renderer>(true).Length==3,"Three LOD renderers installed");
            Check(machine.ProcessorId==saveId,"Machine save identity retained");
            root.name="FertilizerMaker";PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var mesh in meshes)AssetDatabase.SaveAssetIfDirty(mesh);AssetDatabase.SaveAssetIfDirty(extension);
        Check(AssetDatabase.AssetPathToGUID(PrefabPath)==guidBefore,"Prefab GUID retained");
        Check(!ShaderUtil.GetShaderMessages(extension.shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Cozy shader has no compile errors");
        report.AppendLine("Prefab/save identity retained; one collider; atlas 01 reused, additive GIMP atlas 05.");
        File.WriteAllText(Output+"/NativeArtAudit.txt",report+"FERTILIZER_ART_NATIVE_PASS\n");
        Debug.Log("FERTILIZER_ART_NATIVE_PASS");return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }
}
#endif
