#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Text.RegularExpressions;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;
public sealed class FieldDummyValidation:MonoBehaviour {
 const string Primary="C:/GitHub/Unity 6.000LTS - Nature Paradise/Natureparadaise-Saveavillage/";
 const string Root="Assets/Nature  Paradaise/";
 const string Work="C:/Users/huxel/.codex/visualizations/2026/10/02/01a0fc03-fab9-79a2-914d-621e14dea114/WorldProportionWork/";
 bool started;
 static void Check(bool b,string s){if(!b)throw new Exception(s);Debug.Log("FIELD DUMMY PASS "+s);}
 static void Near(float a,float b,string s)=>Check(Mathf.Abs(a-b)<.002f,s+" actual="+a+" expected="+b);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 public static void Run(){try{
  var refs=new Dictionary<string,string>();
  var paths=Directory.GetFiles(Primary+Root+"Script","*.cs",SearchOption.AllDirectories).ToDictionary(p=>Path.GetFileNameWithoutExtension(p),p=>p);
  foreach(var script in AssetDatabase.LoadAllAssetsAtPath("Assets/AnimalUIValidation.dll").OfType<MonoScript>()){
   var type=script.GetClass();if(type==null||!paths.TryGetValue(type.Name,out string path))continue;
   string guid=Regex.Match(File.ReadAllText(path+".meta"),@"guid: (\w+)").Groups[1].Value;
   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script,out string dllGuid,out long id);refs[guid]="{fileID: "+id+", guid: "+dllGuid+", type: 3}";
  }
  foreach(Match m in Regex.Matches(File.ReadAllText(Work+"field-copied.json"),"\"([^\"]+)\"")){
   string path=m.Groups[1].Value; if(!(path.EndsWith(".asset")||path.EndsWith(".prefab")))continue;
   string s=File.ReadAllText(path);if(!s.StartsWith("%YAML"))continue;
   string n=Regex.Replace(s,@"\{fileID: 11500000, guid: ([a-f0-9]{32}), type: 3\}",q=>refs.TryGetValue(q.Groups[1].Value,out string r)?r:q.Value);
   if(n!=s)File.WriteAllText(path,n);
  }
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  foreach(var path in Directory.GetFiles(Root+"Prefabs/Shops/Testing","*.prefab")){
   var go=PrefabUtility.LoadPrefabContents(path);try{
    var body=go.transform.Find("Seller_Capsule_Editable");Check(body!=null,path+" capsule exists");
    var b=body.GetComponent<Renderer>().bounds;Near(b.size.y,1.2f,path+" height");Near(b.size.x,.4f,path+" width");
    Near(body.GetComponent<CapsuleCollider>().height*body.lossyScale.y,1.2f,path+" collider height");
    Check(go.transform.localScale==Vector3.one,path+" UI owner scale retained");Near(b.min.y,0,path+" feet on ground");
   }finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  var p=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=p;QualitySettings.renderPipeline=p;
  Check(p!=null,"Production URP loaded for soil shader");
  new GameObject("FieldDummyValidation").AddComponent<FieldDummyValidation>();
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/FieldDummyValidation.unity");EditorApplication.EnterPlaymode();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 void Start(){if(started)return;started=true;StartCoroutine(Guard());}
 IEnumerator Guard(){var test=Test();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return test.Current;}Debug.Log("FIELD DUMMY VALIDATION PASSED");EditorApplication.Exit(0);}
 IEnumerator Test(){
  string map=File.ReadAllText(Primary+Root+"Map/Scenes/World/Map.unity");
  string fieldBlock=Regex.Match(map,@"(?m)^  fieldId: player-home-field[\s\S]*?(?=^---)").Value;
  float cell=float.Parse(Regex.Match(fieldBlock,@"cellSize: ([\d.]+)").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
  int cols=int.Parse(Regex.Match(fieldBlock,@"columns: (\d+)").Groups[1].Value),rows=int.Parse(Regex.Match(fieldBlock,@"rows: (\d+)").Groups[1].Value);
  var go=new GameObject("Actual Map Field Dimensions");go.SetActive(false);go.transform.position=new Vector3(17,0,11);go.transform.rotation=Quaternion.Euler(0,28,0);
  var surface=GameObject.CreatePrimitive(PrimitiveType.Plane);surface.name="FieldSurface";surface.transform.SetParent(go.transform,false);Destroy(surface.GetComponent<Collider>());
  var field=go.AddComponent<FieldArea>();Set(field,"columns",cols);Set(field,"rows",rows);Set(field,"cellSize",cell);Set(field,"fieldSurface",surface.transform);go.SetActive(true);yield return null;
  Near(field.CellSize,.8f,"Actual Map tile size");Near(field.WorldSize.x,12,"15 columns cover 12 metres");Near(go.GetComponent<BoxCollider>().size.x,12,"Field collider follows grid");Near(surface.transform.localScale.x,1.2f,"Authored plane follows grid");
  for(int z=0;z<rows;z++)for(int x=0;x<cols;x++)if(!field.WorldToGrid(field.GridToWorld(x,z),out int cx,out int cz)||cx!=x||cz!=z)throw new Exception("Grid roundtrip "+x+","+z);
  Check(!field.WorldToGrid(go.transform.TransformPoint(new Vector3(6.01f,0,0)),out _,out _),"Clicks outside resized field rejected");
  Near(Vector3.Distance(field.GridToWorld(0,0),field.GridToWorld(1,0)),.8f,"Neighbour cell world spacing");
  var profile=Resources.Load<FieldSoilVisualProfile>("Profiles/Field Soil Visuals");Check(profile!=null,"Shared soil profile resolved");
  Near(profile.fieldTextureSize,2,"Field texture repeat");Near(profile.hoedTextureSize,.8f,"Hoed texture repeat");
  var ground=go.transform.Find("FieldDirt_Blend");Check(ground!=null,"Blended field exists");var renderer=ground.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
  Near(block.GetFloat("_EdgeWidth"),.12f,"Field edge fade in metres");Near(block.GetFloat("_CornerRadius"),.2f,"Field corner radius");Near(renderer.sharedMaterial.GetVector("_GroundUVTransform").x,.5f,"Field shader texture frequency");
  Check(field.TryHoe(0,0)&&field.TryHoe(1,0)&&field.TryHoe(14,14),"Hoe tiles at both resized field corners");
  var saved=field.CaptureSaveData();Check(saved.tiles.Count==3,"Save captures three modified tiles");field.RestoreSaveData(saved);Check(field.CaptureSaveData().tiles.Count==3,"Save restore retains modified tile indices");
  var tiles=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="HoedSoil").ToArray();Check(tiles.Length==3,"Restored hoe meshes rebuilt");
  var tile=tiles[0];tile.GetPropertyBlock(block);Near(block.GetFloat("_EdgeWidth"),.052f,"Tile feather scales with 0.8m grid");Near(tile.sharedMaterial.GetFloat("_WetEdgeWidth"),.05f,"Wet soil fade reduced");Near(tile.GetComponent<MeshFilter>().sharedMesh.bounds.size.x,.712f,"Tile mesh respects inset");
  var dirt=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/SoStylized/Environment/Landscape/Layer/TL_Dirt.terrainlayer");var sand=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/SoStylized/Environment/Landscape/Layer/TL_Sand.terrainlayer");Near(dirt.tileSize.x,4,"Actual terrain dirt layer repeat");Near(sand.tileSize.x,5,"Actual terrain sand layer repeat");
  yield return null;
 }
}
#endif
