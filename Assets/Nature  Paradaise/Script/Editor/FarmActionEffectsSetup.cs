using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Connects the shared mobile particle material, short SFX and physical tool emit points.</summary>
public static class FarmActionEffectsSetup
{
    const string Root="Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/Effects/Apply Farming Particles and Audio")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying FX assets.");
        AssetDatabase.Refresh();
        string folder=Root+"/Resources/Effects";
        string materialPath=folder+"/farm_particles.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Nature Paradise/Farm Particles"));AssetDatabase.CreateAsset(material,materialPath);}
        var catalog=AssetDatabase.LoadAssetAtPath<FarmActionEffectsCatalog>(folder+"/FarmActionEffects.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<FarmActionEffectsCatalog>();AssetDatabase.CreateAsset(catalog,folder+"/FarmActionEffects.asset");}
        catalog.particleMaterial=material;catalog.density=1;catalog.particleBudget=384;
        var values=(FarmEffect[])Enum.GetValues(typeof(FarmEffect));catalog.sounds=new FarmActionEffectsCatalog.Sound[values.Length];
        for(int i=0;i<values.Length;i++)
        {
            string path=Root+"/Audio/SFX/Farming/sfx_farm_"+values[i].ToString().ToLowerInvariant()+".wav";
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer==null)throw new FileNotFoundException(path);
            importer.forceToMono=true;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData=true;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.45f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            catalog.sounds[i]=new FarmActionEffectsCatalog.Sound{effect=values[i],clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path),volume=values[i] is FarmEffect.BrushComplete or FarmEffect.MilkComplete?.19f:.27f};
        }
        EditorUtility.SetDirty(catalog);
        foreach(string id in new[]{"WateringCan","Milker","Hoe","Sickle","Hammer","Axe"})AddToolAnchor(id);
        AssetDatabase.SaveAssets();
        Debug.Log("[Farm FX] Shared material, bounded particles, 20 short sound clips and 6 physical tool anchors applied.");
    }
    internal static void RefreshHammerAnchor() => AddToolAnchor("Hammer");
    static void AddToolAnchor(string id)
    {
        string path=Root+"/Prefabs/Player/Tools/Held_"+id+".prefab";
        if(!File.Exists(path))throw new FileNotFoundException(path);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            string name=id=="WateringCan"?"WaterSpout":id=="Milker"?"MilkSurface":"ToolImpact";
            var existing=root.transform.Find(name);
            if(existing!=null && id!="Hammer")return;
            var points=new System.Collections.Generic.List<Vector3>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                foreach(var v in filter.sharedMesh.vertices)points.Add(root.transform.InverseTransformPoint(filter.transform.TransformPoint(v)));
            if(points.Count==0)throw new Exception("Tool mesh has no vertices "+id);
            float max=float.MinValue,min=float.MaxValue;
            foreach(var p in points){float a=id=="WateringCan"?p.x:id is "Milker" or "Hammer"?p.y:p.z;max=Mathf.Max(max,a);min=Mathf.Min(min,a);}
            Vector3 center=Vector3.zero;int count=0;float band=Mathf.Max(.001f,(max-min)*.018f);
            foreach(var p in points){float a=id=="WateringCan"?p.x:id is "Milker" or "Hammer"?p.y:p.z;if(a>=max-band){center+=p;count++;}}
            Vector3 position=center/Mathf.Max(1,count);
            if(id=="Milker")
            {
                // The hose is the highest part. Locate the can from its bottom ring,
                // then place droplets above its lid rather than on top of the hose.
                var bottom=new Bounds();bool first=true;
                foreach(var p in points)if(p.y<=min+(max-min)*.2f)
                {if(first){bottom=new Bounds(p,Vector3.zero);first=false;}else bottom.Encapsulate(p);}
                position=new Vector3(bottom.center.x,min+(max-min)*.72f,bottom.center.z);
            }
            // Hammer's +Y end is the flat striking face; +Z is only the shaft/head end.
            if(id=="Hammer")
            {
                var face=new Bounds(position,Vector3.zero);
                foreach(var p in points)if(p.y>=max-band)face.Encapsulate(p);
                position=new Vector3(face.center.x,max,face.center.z);
            }
            var anchor=existing!=null?existing:new GameObject(name).transform;anchor.SetParent(root.transform,false);anchor.localPosition=position;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
