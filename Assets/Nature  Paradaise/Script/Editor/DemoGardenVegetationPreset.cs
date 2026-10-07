using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoGardenVegetationPreset
{
 const string Root="Assets/Nature  Paradaise";
 const string Pack=Root+"/Pack/Toon Series/Toon Farm Pack";
 [MenuItem("Nature Paradise/World/Apply Demo Garden Vegetation Preset")]
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply in Edit Mode.");
  GardenWorldSetup.Apply();
  var demo=AssetDatabase.LoadAssetAtPath<TerrainData>(Pack+"/Terrain/TFP_Terrain_Data_Gardens.asset");
  var sourceLayer=demo.terrainLayers.First(l=>l.name.Contains("Grass"));
  string layerPath=Root+"/Resources/World/Demo Garden Grass.terrainlayer";
  var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
  if(layer==null){layer=UnityEngine.Object.Instantiate(sourceLayer);layer.name="Demo Garden Grass";AssetDatabase.CreateAsset(layer,layerPath);}
  else{EditorUtility.CopySerialized(sourceLayer,layer);layer.name="Demo Garden Grass";}
  EditorUtility.SetDirty(layer);
  // Use actual counts from the authored demo, including the less common 06A grass.
  var counts=new Dictionary<string,int>();
  var scales=new Dictionary<string,List<float>>();
  string sceneText=File.ReadAllText(Pack+"/Scenes/Demo_Gardens.unity");
  foreach(string block in sceneText.Split(new[]{"--- !u!1001 "},StringSplitOptions.None))
  {
   string instance=block.Split(new[]{"--- !u!"},StringSplitOptions.None)[0];
   var match=Regex.Match(instance,@"value: (TFP_Grass_Patch_\d+A)");if(!match.Success)continue;
   string name=match.Groups[1].Value;
   counts[name]=counts.TryGetValue(name,out int count)?count+1:1;
   if(!scales.ContainsKey(name))scales[name]=new List<float>();
   var scale=Regex.Match(instance,@"propertyPath: m_LocalScale.x\s+value: ([\d.eE+-]+)");
   if(scale.Success&&float.TryParse(scale.Groups[1].Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float size)&&size>0)scales[name].Add(size);
  }
  int channel=Array.IndexOf(demo.terrainLayers,sourceLayer);var paint=demo.GetAlphamaps(0,0,demo.alphamapWidth,demo.alphamapHeight);
  double area=0;for(int z=0;z<demo.alphamapHeight;z++)for(int x=0;x<demo.alphamapWidth;x++)if(paint[z,x,channel]>=.65f)area+=(double)demo.size.x*demo.size.z/demo.alphamapWidth/demo.alphamapHeight;
  var profile=Resources.Load<TerrainVegetationProfile>("World/Terrain Vegetation");
  var rule=profile.rules.First(r=>r.terrainLayer!=null&&r.terrainLayer.name.IndexOf("grass",StringComparison.OrdinalIgnoreCase)>=0);
  rule.terrainLayer=layer;rule.diffuseTexture=null;rule.name="Demo Gardens Grass";rule.enabled=true;rule.useWorldDensity=true;rule.density=(float)(counts.Values.Sum()/area);rule.clusterVariation=.35f;rule.variants.Clear();
  var report=new StringBuilder($"Source layer={sourceLayer.name}; texture={sourceLayer.diffuseTexture.name}; tiling={sourceLayer.tileSize}; grassArea={area:F2}m2; authoredClumps={counts.Values.Sum()}; worldDensity={rule.density:F5}/m2\n");
  foreach(var pair in counts.OrderBy(p=>p.Key))
  {
   var prefab=GardenWorldSetup.MakeDetailPrefab(pair.Key);var values=scales[pair.Key].OrderBy(s=>s).ToArray();
   float low=values[(int)(values.Length*.05f)],high=values[Mathf.Min(values.Length-1,(int)(values.Length*.95f))];
   rule.variants.Add(new TerrainVegetationProfile.Variant{prefab=prefab,weight=pair.Value,widthScale=new Vector2(low,high),heightScale=new Vector2(low,high)});
   report.AppendLine($"{pair.Key}: count={pair.Value}, scale5-95%={low:F4}..{high:F4}");
  }
  var spring=Resources.Load<SeasonVisualProfile>("Profiles/Seasons/Spring Season");if(spring!=null){spring.terrainTint=Color.white;EditorUtility.SetDirty(spring);}
  var demoMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("594ea882c5a793440b60ff72d896021e"));
  var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/World/m_MainTerrain.mat");
  if(demoMaterial!=null&&material!=null){string name=material.name;EditorUtility.CopySerialized(demoMaterial,material);material.name=name;EditorUtility.SetDirty(material);report.AppendLine("Terrain material="+demoMaterial.name+"; shader="+material.shader.name);}
  string mapPath=Root+"/Map/Scenes/World/Map.unity";var scene=SceneManager.GetSceneByPath(mapPath);bool opened=!scene.isLoaded;var prior=SceneManager.GetActiveScene();if(opened)scene=EditorSceneManager.OpenScene(mapPath,OpenSceneMode.Additive);
  try{
   foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)))
   {
    var layers=t.terrainData.terrainLayers;for(int i=0;i<layers.Length;i++)if(layers[i]!=null&&layers[i].name.IndexOf("grass",StringComparison.OrdinalIgnoreCase)>=0)layers[i]=layer;t.terrainData.terrainLayers=layers;
    var vegetation=t.GetComponent<TerrainLayerVegetation>();vegetation.profile=profile;vegetation.Rebuild();
    report.AppendLine($"{t.name}: scatter={t.terrainData.detailScatterMode}, totalTargetDensity={rule.density:F5}/m2");
   }
   EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Library/DemoGardenPresetAudit.txt",report.ToString());
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(prior.isLoaded)SceneManager.SetActiveScene(prior);}
 }
}
