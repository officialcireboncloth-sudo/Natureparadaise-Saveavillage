using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Explicit atlas authoring for reusable wood, existing house geometry and the pack pitchfork.</summary>
public static class SharedWoodAtlasSetup
{
    public const string Root = "Assets/Nature  Paradaise";
    public const string AtlasPath = Root + "/Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.png";
    public const string MaterialPath = Root + "/Material/Shared Wood Atlas.mat";
    const string HouseModels = Root + "/mesh/Buildings/HouseInterior_H";
    const string HousePrefabs = Root + "/Prefabs/PlayerHouse/Interior";
    const string ToolMeshPath = Root + "/mesh/Prop/Tools/Gameplay/Pitchfork.asset";
    const string ToolPrefabPath = Root + "/Prefabs/Player/Tools/Held_Pitchfork.prefab";
    static Material shared;
    static Dictionary<string, Vector2> woodPalette;
    public static Rect WoodSlot(string key) => new Rect(key is "wooddark" or "woodshadow" ? 272f/2048 : key=="cream" ? 784f/2048 : 16f/2048, 1552f/2048, 224f/2048, 480f/2048);

    [MenuItem("Nature Paradise/Art/Apply Shared Wood and Pitchfork")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring assets.");
        shared=EnsureMaterial();
        // Authored house palette samples from the original 2048px Toon Pack atlas.
        woodPalette=new Dictionary<string,Vector2>{["wood"]=new(.023681640625f,.328857421875f),["woodlight"]=new(.023681640625f,.336181640625f),["wooddark"]=new(.117431640625f,.477783203125f),["woodshadow"]=new(.304931640625f,.415283203125f)};
        for(int level=1;level<=5;level++)ApplyHouse(level);
        ApplyPitchfork();AssetDatabase.SaveAssets();Debug.Log("[SharedWood] Five interiors and Toon Pack pitchfork applied; existing atlas GUID retained.");
    }
    static Material EnsureMaterial()
    {
        AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,MaterialPath);}
        material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.08f);material.SetFloat("_Metallic",0);
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Normal.png"));material.SetFloat("_BumpScale",.32f);material.EnableKeyword("_NORMALMAP");material.enableInstancing=true;
        // Keep the existing compressed atlas; all users share its mip chain.
        var importer=(TextureImporter)AssetImporter.GetAtPath(AtlasPath);importer.mipmapEnabled=true;importer.isReadable=false;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ETC2_RGB4,textureCompression=TextureImporterCompression.Compressed});
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="iPhone",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,textureCompression=TextureImporterCompression.Compressed});
        importer.SaveAndReimport();EditorUtility.SetDirty(material);return material;
    }
    static void ApplyHouse(int level)
    {
        int faces=0;
        foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(HouseModels+"/HouseInterior_Lv"+level+"_Runtime.asset").OfType<Mesh>())
        {
            if(mesh.name.Contains("Collider")||mesh.name.Contains("Glass")||mesh.name.Contains("Display"))continue;
            faces+=Remap(mesh,uv=>woodPalette.FirstOrDefault(p=>uv.All(u=>(u-p.Value).sqrMagnitude<.00000001f)).Key);
        }
        string path=HousePrefabs+"/HouseInteriorLayout_Lv"+level+".prefab";
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try{
            foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true)){
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)if(materials[i]&&materials[i].name=="TFP_Atlas_1A"){materials[i]=shared;changed=true;}
                if(changed)renderer.sharedMaterials=materials;
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,path);
        }finally{PrefabUtility.UnloadPrefabContents(prefab);}
        Debug.Log("[SharedWood] House Lv"+level+": "+faces+" wood triangles receive grain UVs.");
    }
    sealed class Face { public int[] indices; public string wood; public int parent; public Vector3 normal; }
    static string Point(Vector3 p)=>Mathf.RoundToInt(p.x*10000)+","+Mathf.RoundToInt(p.y*10000)+","+Mathf.RoundToInt(p.z*10000);
    static int Find(List<Face> faces,int index){while(faces[index].parent!=index){faces[index].parent=faces[faces[index].parent].parent;index=faces[index].parent;}return index;}
    static int Remap(Mesh mesh,Func<Vector2[],string> classify)
    {
        var vertices=mesh.vertices;var originalUV=mesh.uv;if(originalUV.Length!=vertices.Length)return 0;
        var triangles=Enumerable.Range(0,mesh.subMeshCount).Select(s=>mesh.GetTriangles(s)).ToArray();var faces=new List<Face>();
        for(int sub=0;sub<triangles.Length;sub++)for(int i=0;i<triangles[sub].Length;i+=3){var ix=new[]{triangles[sub][i],triangles[sub][i+1],triangles[sub][i+2]};var key=classify(ix.Select(v=>originalUV[v]).ToArray());var normal=Vector3.Cross(vertices[ix[1]]-vertices[ix[0]],vertices[ix[2]]-vertices[ix[0]]).normalized;faces.Add(new Face{indices=ix,wood=key,parent=faces.Count,normal=normal});}
        var edges=new Dictionary<string,int>();
        for(int f=0;f<faces.Count;f++){
            var face=faces[f];if(face.wood==null)continue;
            string plane=face.wood+"/"+Point(face.normal)+"/"+Mathf.RoundToInt(Vector3.Dot(face.normal,vertices[face.indices[0]])*10000)+"/";
            for(int e=0;e<3;e++){
                string a=Point(vertices[face.indices[e]]),b=Point(vertices[face.indices[(e+1)%3]]);string edge=plane+(string.CompareOrdinal(a,b)<0?a+"/"+b:b+"/"+a);
                if(edges.TryGetValue(edge,out int other))faces[Find(faces,f)].parent=Find(faces,other);else edges[edge]=f;
            }
        }
        var targetUV=new Dictionary<(int face,int vertex),Vector2>();
        foreach(var group in Enumerable.Range(0,faces.Count).Where(i=>faces[i].wood!=null).GroupBy(i=>Find(faces,i))){
            var indices=group.SelectMany(i=>faces[i].indices).Distinct().ToArray();var bounds=new Bounds(vertices[indices[0]],Vector3.zero);foreach(int i in indices)bounds.Encapsulate(vertices[i]);
            var n=faces[group.First()].normal;int skip=Mathf.Abs(n.x)>Mathf.Abs(n.y)?Mathf.Abs(n.x)>Mathf.Abs(n.z)?0:2:Mathf.Abs(n.y)>Mathf.Abs(n.z)?1:2;
            int a=(skip+1)%3,b=(skip+2)%3;int along=bounds.size[a]>=bounds.size[b]?a:b,across=along==a?b:a;
            var slot=WoodSlot(faces[group.First()].wood);float spanU=Mathf.Max(.0001f,bounds.size[across]),spanV=Mathf.Max(.0001f,bounds.size[along]);
            float crop=.78f+(group.Key%13)*.01f,start=(group.Key%7)/7f*(1-crop);
            foreach(int f in group)foreach(int i in faces[f].indices)targetUV[(f,i)]=new Vector2(slot.x+slot.width*((vertices[i][across]-bounds.min[across])/spanU),slot.y+slot.height*(start+crop*(vertices[i][along]-bounds.min[along])/spanV));
        }
        if(targetUV.Count==0)return 0;
        var originalNormals=mesh.normals;var originalUV2=mesh.uv2;var originalColors=mesh.colors32;
        var vv=vertices.ToList();var nn=originalNormals.ToList();var uvOut=originalUV.ToList();var uv2=originalUV2.ToList();var colors=originalColors.ToList();var assigned=new Dictionary<int,Vector2>();var split=new Dictionary<(int,int,int),int>();int faceIndex=0;
        for(int sub=0;sub<triangles.Length;sub++)for(int i=0;i<triangles[sub].Length;i+=3,faceIndex++)for(int j=0;j<3;j++){
            int original=triangles[sub][i+j];Vector2 value=targetUV.TryGetValue((faceIndex,original),out var target)?target:originalUV[original];
            if(!assigned.TryGetValue(original,out var first)){assigned[original]=value;uvOut[original]=value;continue;}
            if((first-value).sqrMagnitude<.0000000001f)continue;
            var key=(original,Mathf.RoundToInt(value.x*1000000),Mathf.RoundToInt(value.y*1000000));
            if(!split.TryGetValue(key,out int mapped)){mapped=vv.Count;vv.Add(vertices[original]);uvOut.Add(value);if(nn.Count>0)nn.Add(originalNormals[original]);if(uv2.Count>0)uv2.Add(originalUV2[original]);if(colors.Count>0)colors.Add(originalColors[original]);split[key]=mapped;}
            triangles[sub][i+j]=mapped;
        }
        mesh.Clear();mesh.indexFormat=vv.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;mesh.SetVertices(vv);mesh.SetUVs(0,uvOut);if(nn.Count>0)mesh.SetNormals(nn);else mesh.RecalculateNormals();if(uv2.Count>0)mesh.SetUVs(1,uv2);if(colors.Count>0)mesh.SetColors(colors);
        mesh.subMeshCount=triangles.Length;for(int sub=0;sub<triangles.Length;sub++)mesh.SetTriangles(triangles[sub],sub);mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return faces.Count(f=>f.wood!=null);
    }
    static Texture2D ReadTexture(Texture source)
    {
        var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(source,rt);var old=RenderTexture.active;RenderTexture.active=rt;var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);return copy;
    }
    static void ApplyPitchfork()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Pack/Toon Series/Toon Farm Pack/Prefabs/Props/Handheld Tools/TFP_Spading_Fork_01A.prefab");
        var lod=source.GetComponent<LODGroup>().GetLODs()[0];var filter=lod.renderers[0].GetComponent<MeshFilter>();var original=filter.sharedMesh;
        var mesh=Object.Instantiate(original);mesh.name="Pitchfork_Atlas";
        var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var points=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
        int axis=bounds.size.x>bounds.size.y?0:1;if(bounds.size.z>bounds.size[axis])axis=2;
        // The wider end is the head; canonical +Z points toward the tines.
        float Width(bool high){var subset=points.Where(p=>high?p[axis]>bounds.max[axis]-bounds.size[axis]*.22f:p[axis]<bounds.min[axis]+bounds.size[axis]*.22f).ToArray();var b=new Bounds(subset[0],Vector3.zero);foreach(var p in subset)b.Encapsulate(p);return Mathf.Max(b.size[(axis+1)%3],b.size[(axis+2)%3]);}
        Vector3 direction=Vector3.zero;direction[axis]=Width(true)>Width(false)?1:-1;var rotation=Quaternion.FromToRotation(direction,Vector3.forward);float scale=1.25f/bounds.size[axis];
        var transformed=points.Select(p=>rotation*p*scale).ToArray();float tail=transformed.Min(p=>p.z);Vector3 grip=new Vector3((rotation*bounds.center*scale).x,(rotation*bounds.center*scale).y,tail+.25f);
        mesh.vertices=transformed.Select(p=>p-grip).ToArray();mesh.normals=original.normals.Select(n=>rotation*matrix.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();
        var sourceMaterial=lod.renderers[0].sharedMaterial;var texture=sourceMaterial.HasProperty("_TextureSample")?sourceMaterial.GetTexture("_TextureSample"):sourceMaterial.mainTexture;var readable=ReadTexture(texture);
        int wooden=Remap(mesh,uv=>{var c=readable.GetPixelBilinear(uv.Average(u=>u.x),uv.Average(u=>u.y));return c.r>c.g*1.12f&&c.g>c.b*1.15f&&c.r>.22f?"wood":null;});Object.DestroyImmediate(readable);
        if(wooden==0)throw new InvalidOperationException("Pitchfork shaft was not identified as wood.");
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(ToolMeshPath);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,ToolMeshPath);
        var root=new GameObject("Held_Pitchfork");var model=new GameObject("Model",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(root.transform,false);model.GetComponent<MeshFilter>().sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterial=shared;
        var tip=new GameObject("ToolImpact").transform;tip.SetParent(root.transform,false);tip.localPosition=new Vector3(mesh.bounds.center.x,mesh.bounds.center.y,mesh.bounds.max.z);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,ToolPrefabPath);Object.DestroyImmediate(root);
        var catalog=AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root+"/Resources/Player/Player Tool Visual Catalog.asset");var entry=catalog.entries.Find(e=>e.id=="Pitchfork");if(entry==null){entry=new PlayerToolVisualCatalog.Entry{id="Pitchfork",tool=PlayerToolType.Pitchfork};catalog.entries.Add(entry);}entry.prefab=prefab;entry.scale=1;entry.workGrip=true;entry.twoHandsDuringAction=true;entry.twoHandsWhileCarrying=false;
        var preview=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab"));
        try{
            var rig=preview.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController);rig.enabled=false;var clips=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");
            var hand=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");var left=preview.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:LeftHand");
            entry.carryDirection=new Vector3(.25f,-.3f,.92f);entry.handPosition=Vector3.zero;
            clips.idle.SampleAnimation(rig.gameObject,.25f);entry.carryHandEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(entry.carryDirection.normalized,Vector3.up)).eulerAngles;
            clips.scoopManure.SampleAnimation(rig.gameObject,Mathf.Min(clips.scoopManure.length*.45f,.9f));Vector3 shaft=(hand.position-left.position).normalized;entry.handEuler=(Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(shaft,Vector3.ProjectOnPlane(Vector3.up,shaft))).eulerAngles;entry.secondHandGrip=new Vector3(0,0,-Vector3.Distance(hand.position,left.position));
        }finally{Object.DestroyImmediate(preview);}EditorUtility.SetDirty(catalog);Debug.Log("[SharedWood] Existing fork: "+mesh.vertexCount+" vertices, "+wooden+" wooden triangles; hand-grip prefab calibrated.");
    }
}
