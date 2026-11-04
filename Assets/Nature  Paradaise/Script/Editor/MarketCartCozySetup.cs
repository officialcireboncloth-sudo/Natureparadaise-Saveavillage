#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Reference pushcart art. Keeps the existing stand identity, listings, UI and save bindings.</summary>
[InitializeOnLoad]
public static class MarketCartCozySetup
{
 const string Root="Assets/Nature  Paradaise",Folder=Root+"/mesh/World/MarketStand/",Prefab=Root+"/Prefabs/World/MarketStand.prefab",Visual=Root+"/Prefabs/World/MarketStand/MarketStandVisual.prefab",Output="ArtSource/MarketCart/UnityReview",PlayKey="NatureParadise.MarketCart.PlayAudit";
 static StringBuilder report;static int checks;static double started;static bool tested;
 static bool Fixture=>Application.dataPath.Replace('\\','/').Contains("/HandoffAudit-2026-10-23/CleanClone/");
 static Vector3 V(JToken t)=>new Vector3((float)t[0],(float)t[1],(float)t[2]);
 static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);checks++;report.AppendLine("PASS "+label);}
 static MarketCartCozySetup(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
 static Material Produce()
 {
  const string path=Root+"/Material/World/MarketStand/MarketCart_Produce.mat";
  Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Nature Paradise/Cozy Atlas")){name="MarketCart_ToonpackProduce"};AssetDatabase.CreateAsset(m,path);}
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd");
  Check(texture,"Original Toonpack atlas resolves");m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",Color.white);m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.35f);m.SetFloat("_Cull",0);m.SetFloat("_UseNormal",0);m.DisableKeyword("_NORMALMAP");m.SetFloat("_UseMask",0);m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Wrap",.14f);m.SetFloat("_AmbientStrength",.9f);m.SetFloat("_Saturation",1);m.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(m);return m;
 }
 static Material Glass()
 {
  const string path=Root+"/Material/World/MarketStand/MarketCart_Glass.mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="MarketCart_Glass"};AssetDatabase.CreateAsset(m,path);}
  m.shader=Shader.Find("Universal Render Pipeline/Unlit");m.shaderKeywords=Array.Empty<string>();
  m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.SetFloat("_AlphaClip",0);
  m.SetColor("_BaseColor",new Color(.79f,.91f,.92f,.075f));m.SetColor("_SpecColor",new Color(.1f,.1f,.1f,1));m.SetFloat("_Smoothness",.3f);m.SetOverrideTag("RenderType","Transparent");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_ALPHATEST_ON");m.renderQueue=(int)RenderQueue.Transparent;m.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(m);return m;
 }
 static Mesh Store(Mesh mesh)
 {
  string path=Folder+"MarketCart_Runtime.asset",name=mesh.name;var old=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==name || (name=="MarketCart_Cart_LOD0"&&m.name=="MarketCart_Runtime"));
  if(old){EditorUtility.CopySerialized(mesh,old);old.name=name;Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
  if(File.Exists(path))AssetDatabase.AddObjectToAsset(mesh,path);else AssetDatabase.CreateAsset(mesh,path);mesh.name=name;EditorUtility.SetDirty(mesh);return mesh;
 }
 [MenuItem("Nature Paradise/Art/Apply Cozy Market Cart")]
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before applying cart art.");
  report=new StringBuilder("COZY MARKET CART\n");checks=0;Directory.CreateDirectory(Output);
  var spec=JObject.Parse(File.ReadAllText(Root+"/Source~/MarketCart/MarketCart_Manifest.json"));
  var protectedPaths=new[]{Root+"/Prefabs/World/ShippingBin.prefab",Root+"/Map/Scenes/World/Map.unity",Root+"/Textures/Shared/Cozy/NP_CozySurfaces_01_BaseColor.png"};
  var original=protectedPaths.Select(File.ReadAllBytes).ToArray();
  var timber=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared/NP_CozySurfaces_01.mat");
  Check(timber && timber.GetTexture("_BaseMap"),"Existing shared cozy atlas remains assigned");
  var produce=Produce();var glass=Glass();var mats=new Dictionary<string,Material>{{"Cart",timber},{"WheelLeft",timber},{"WheelRight",timber},{"Produce",produce},{"Glass",glass}};
  string modelPath=Folder+"MarketStand_A.fbx";AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);
  var imp=(ModelImporter)AssetImporter.GetAtPath(modelPath);imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;imp.importLights=false;imp.importCameras=false;imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  foreach(var mat in mats.Values){imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"MarketStand_A-"+mat.name),mat);}imp.SaveAndReimport();
  var imported=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
  var frames=imported.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cozy")).ToDictionary(t=>t.name,t=>t.position);
  var orientations=Enumerable.Range(0,4).SelectMany(i=>new[]{1f,-1f}.Select(s=>Matrix4x4.Rotate(Quaternion.Euler(0,i*90,0))*Matrix4x4.Scale(new Vector3(s,1,1))));
  var correction=orientations.OrderBy(m=>Vector3.Distance(m.MultiplyPoint3x4(frames["CozyRight"]),Vector3.right)+Vector3.Distance(m.MultiplyPoint3x4(frames["CozyForward"]),Vector3.forward)+Vector3.Distance(m.MultiplyPoint3x4(frames["CozyUp"]),Vector3.up)).First();
  Check(Vector3.Distance(correction.MultiplyPoint3x4(frames["CozyRight"]),Vector3.right)<.001f && Vector3.Distance(correction.MultiplyPoint3x4(frames["CozyForward"]),Vector3.forward)<.001f,"Unmirrored front and right match Blender");
  var filters=imported.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name);var meshes=new Dictionary<string,Mesh>();
  foreach(var e in spec["meshes"])
  {
   string name=(string)e["name"];var f=filters[name];var transform=correction*f.transform.localToWorldMatrix;var mesh=Object.Instantiate(f.sharedMesh);mesh.name=name;
   mesh.vertices=mesh.vertices.Select(transform.MultiplyPoint3x4).ToArray();mesh.normals=mesh.normals.Select(n=>transform.inverse.transpose.MultiplyVector(n).normalized).ToArray();float flip=transform.determinant<0?-1:1;
   mesh.tangents=mesh.tangents.Select(t=>{var v=transform.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*flip);}).ToArray();
   if(flip<0){var tri=mesh.triangles;for(int i=0;i<tri.Length;i+=3){int old=tri[i];tri[i]=tri[i+2];tri[i+2]=old;}mesh.triangles=tri;}
   mesh.RecalculateBounds();Check(Vector3.Distance(mesh.bounds.min,V(e["min"]))<.002f&&Vector3.Distance(mesh.bounds.max,V(e["max"]))<.002f,name+": metre bounds match");
   Check(mesh.triangles.Length/3==(int)e["triangles"],name+": triangles match export");Check(mesh.subMeshCount==1&&mesh.uv.All(u=>!float.IsNaN(u.x)&&!float.IsNaN(u.y)),name+": one material and finite UVs");meshes[name]=Store(mesh);
  }
  var visual=new GameObject("MarketStandVisual");
  try
  {
   var renderers=new List<Renderer>();
   var wheels=new Dictionary<string,Transform>();
   for(int i=0;i<2;i++){var t=new GameObject(i==0?"WheelLeftPivot":"WheelRightPivot").transform;t.SetParent(visual.transform,false);t.localPosition=V(spec["wheelPivots"][i]);wheels[i==0?"WheelLeft":"WheelRight"]=t;}
   foreach(var e in spec["meshes"])
   {
    string name=(string)e["name"],kind=(string)e["group"];var child=new GameObject(name);child.transform.SetParent(visual.transform,false);child.AddComponent<MeshFilter>().sharedMesh=meshes[name];var r=child.AddComponent<MeshRenderer>();r.sharedMaterial=mats[kind];r.shadowCastingMode=kind=="Glass"?ShadowCastingMode.Off:ShadowCastingMode.On;r.receiveShadows=kind!="Glass";renderers.Add(r);
   }
   foreach(var r in renderers){string kind=(string)spec["meshes"].First(e=>(string)e["name"]==r.name)["group"];if(wheels.TryGetValue(kind,out var pivot))r.transform.SetParent(pivot,true);}
   foreach(var pair in new[]{("KeeperAnchor",V(spec["keeperAnchor"])),("FrontInteractionAnchor",V(spec["frontAnchor"])),("LeftGripAnchor",V(spec["gripAnchors"][0])),("RightGripAnchor",V(spec["gripAnchors"][1]))}){var t=new GameObject(pair.Item1).transform;t.SetParent(visual.transform,false);t.localPosition=pair.Item2;}
   MarketKeeperCharacterSetup.InstantiateAtAnchor(visual.transform.Find("KeeperAnchor"));
   var group=visual.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.22f,renderers.Where(r=>r.name.EndsWith("LOD0")).ToArray()),new LOD(.10f,renderers.Where(r=>r.name.EndsWith("LOD1")).ToArray()),new LOD(.012f,renderers.Where(r=>r.name.EndsWith("LOD2")).ToArray())});group.fadeMode=LODFadeMode.None;group.RecalculateBounds();PrefabUtility.SaveAsPrefabAsset(visual,Visual);
  }
  finally{Object.DestroyImmediate(visual);}
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try
  {
   var stand=root.GetComponent<MarketStand>();string state=EditorJsonUtility.ToJson(stand),guid=AssetDatabase.AssetPathToGUID(Prefab);var pos=root.transform.localPosition;var rot=root.transform.localRotation;var scale=root.transform.localScale;
   foreach(var t in root.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Visual_")||t.name=="CartCollision").ToArray())Object.DestroyImmediate(t.gameObject);
   var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Visual),root.transform);art.name="Visual_AtlasMarketStand";
   var collision=new GameObject("CartCollision").transform;collision.SetParent(root.transform,false);
   foreach(var c in spec["colliders"])
   {
    bool body=(string)c["name"]=="Body";BoxCollider box;
    if(body)box=root.GetComponent<BoxCollider>();else{var child=new GameObject((string)c["name"]).transform;child.SetParent(collision,false);child.localPosition=V(c["center"]);if(c["rotation"]!=null)child.localRotation=Quaternion.Euler(V(c["rotation"]));box=child.gameObject.AddComponent<BoxCollider>();}
    box.center=body?V(c["center"]):Vector3.zero;box.size=V(c["size"]);box.isTrigger=false;
   }
   Check(EditorJsonUtility.ToJson(stand)==state,"Stand identity, listings, prices, hours and gameplay settings unchanged");Check(root.transform.localPosition==pos&&root.transform.localRotation==rot&&root.transform.localScale==scale,"Gameplay root placement and scale unchanged");PrefabUtility.SaveAsPrefabAsset(root,Prefab);Check(AssetDatabase.AssetPathToGUID(Prefab)==guid,"Existing gameplay prefab GUID preserved");
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  imp.isReadable=false;imp.SaveAndReimport();AssetDatabase.SaveAssets();
  for(int i=0;i<protectedPaths.Length;i++)Check(File.ReadAllBytes(protectedPaths[i]).SequenceEqual(original[i]),"Protected unchanged: "+protectedPaths[i]);
  if(Fixture&&Application.isBatchMode)ValidateAndRender(spec,mats);
  report.AppendLine("MARKET_CART_APPLY_PASS checks="+checks);File.WriteAllText(Output+"/Audit.txt",report.ToString());if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");
 }
 static void ValidateAndRender(JObject spec,Dictionary<string,Material> materials)
 {
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
  var group=obj.GetComponentInChildren<LODGroup>();var lods=group.GetLODs();Check(lods.Length==3 && lods.All(l=>l.renderers.Length==5),"Three LODs with separate rolling wheels and shared atlas materials");
  Check(obj.GetComponentsInChildren<Collider>(true).Count(c=>!c.GetComponentInParent<NPCCharacterMotion>())==5 && obj.GetComponentsInChildren<MeshCollider>().Length==0,"Five cart colliders; no mesh collider");
  Check(obj.transform.Find("Visual_AtlasMarketStand/KeeperAnchor") && obj.transform.Find("Visual_AtlasMarketStand/FrontInteractionAnchor"),"Separate keeper and interaction anchors");
  Check(obj.GetComponentsInChildren<Transform>(true).All(t=>t.localScale.x>0&&t.localScale.y>0&&t.localScale.z>0),"No negative transform scales");
  Check(materials.Values.All(m=>!ShaderUtil.ShaderHasError(m.shader)),"All material shaders compile");Check(materials["Glass"].renderQueue==3000&&materials["Glass"].GetFloat("_ZWrite")==0,"Glass transparent and depth writes disabled");
  group.ForceLOD(0);var bounds=lods[0].renderers[0].bounds;foreach(var r in lods[0].renderers.Skip(1))bounds.Encapsulate(r.bounds);
  Check(bounds.size.x<2.8f && bounds.size.y<2.2f && bounds.size.z<3.2f,"Compact cart world dimensions: "+bounds.size);
  for(int i=1;i<3;i++)Check(lods[i].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length)<lods[i-1].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length),"LOD "+i+" reduces geometry");
  var rects=JObject.Parse(File.ReadAllText(Root+"/Textures/Shared/Cozy/NP_CozyAtlasSlots.json"))["slots"].Select(s=>s["uvSafe"].Select(v=>(float)v).ToArray()).ToArray();
  foreach(var e in spec["meshes"].Where(e=>(string)e["group"]=="Cart"))
  {
   var mesh=AssetDatabase.LoadAllAssetsAtPath(Folder+"MarketCart_Runtime.asset").OfType<Mesh>().First(m=>m.name==(string)e["name"]);var uv=mesh.uv;var tri=mesh.triangles;bool valid=true;
   for(int i=0;i<tri.Length;i+=3)if(!rects.Any(r=>Enumerable.Range(0,3).All(j=>{var u=uv[tri[i+j]];return u.x>=r[0]-.00001f&&u.y>=r[1]-.00001f&&u.x<=r[2]+.00001f&&u.y<=r[3]+.00001f;}))){valid=false;break;}
   Check(valid,mesh.name+": every triangle stays inside a padded atlas swatch");
  }
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.78f,.82f,.88f);RenderSettings.ambientEquatorColor=new Color(.56f,.55f,.52f);RenderSettings.ambientGroundColor=new Color(.38f,.34f,.28f);RenderSettings.fog=false;
  var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(RenderSettings.ambientEquatorColor.linear*.8f);sh.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor.linear,.45f);RenderSettings.ambientProbe=sh;
  var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.02f;sun.color=new Color(1,.96f,.90f);sun.transform.rotation=Quaternion.Euler(48,155,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.7f;RenderSettings.sun=sun;
  var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.position=Vector3.down*.015f;var floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.SetColor("_BaseColor",new Color(.69f,.72f,.59f));floorMat.SetFloat("_Smoothness",0);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
  var camera=new GameObject("Review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.75f;camera.farClipPlane=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.75f,.66f);
  Vector3 target=new Vector3(0,1.06f,-.36f);
  foreach(var pair in new[]{("ThreeQuarter",new Vector3(-3.6f,2.0f,5.3f)),("Front",new Vector3(0,1.4f,5.8f)),("Rear",new Vector3(3,2,-5.3f)),("Top",new Vector3(0,7,.01f))}){camera.transform.position=target+pair.Item2;camera.transform.LookAt(target);Capture(camera,"MarketCart_"+pair.Item1);}
  camera.transform.position=target+new Vector3(-3.6f,2,5.3f);camera.transform.LookAt(target);
  for(int i=1;i<3;i++){group.ForceLOD(i);Capture(camera,"MarketCart_LOD"+i);}group.ForceLOD(-1);
  Object.DestroyImmediate(obj);Object.DestroyImmediate(floorMat);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  string map=Root+"/Map/Scenes/World/Map.unity";string before=File.ReadAllText(map);EditorSceneManager.OpenScene(map,OpenSceneMode.Single);
  Check(Object.FindObjectsByType<MarketStand>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(s=>s.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&f.sharedMesh.name=="MarketCart_Cart_LOD0")),"Actual Map resolves the new cart through existing prefab binding");Check(File.ReadAllText(map)==before,"Map scene not rewritten");
 }
 static void Capture(Camera camera,string name)
 {
  const int width=1300,height=1100;camera.aspect=(float)width/height;var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();ShaderUtil.allowAsyncCompilation=false;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
 }
 public static void RunBatch()
 {
  if(!Fixture)throw new InvalidOperationException("Use isolated fixture for batch review.");
  try
  {
   Apply();string review=Root+"/Documentation/MarketCartReview";Directory.CreateDirectory(review);
   foreach(string path in Directory.GetFiles(Output,"*.png"))File.Copy(path,review+"/"+Path.GetFileName(path),true);
   File.Copy(Output+"/Audit.txt",review+"/Audit.txt",true);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);EditorApplication.Exit(0);
  }
  catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",report+"\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
 }
 public static void RunPlayBatch()
 {
  if(!Fixture)throw new InvalidOperationException("Use isolated fixture.");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/MarketCartPlayWorld.unity");SessionState.SetBool(PlayKey,true);EditorApplication.isPlaying=true;
 }
 static void State(PlayModeStateChange s)
 {
  if(!SessionState.GetBool(PlayKey,false))return;
  if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;tested=false;}
  if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(PlayKey,false);EditorApplication.Exit(File.Exists(Output+"/PlayError.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(PlayKey,false)||!EditorApplication.isPlaying||tested||EditorApplication.timeSinceStartup-started<2)return;tested=true;report=new StringBuilder("MARKET CART NATIVE PLAY\n");checks=0;
  try
  {
   Check(SaveManager.Instance==null,"No production save writer");var stand=Object.FindFirstObjectByType<MarketStand>();Check(stand,"Cart gameplay component instantiates");Check(stand.StandId=="village-market-stand-01","Existing save identity retained");Check((stand.transform.lossyScale-Vector3.one).sqrMagnitude<.0001f,"Runtime scale is 1 metre convention");
   var actor=new GameObject("Audit inventory");var inventory=actor.AddComponent<Inventory>();typeof(MarketStand).GetField("playerInventory",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(stand,inventory);
   var item=Resources.LoadAll<ItemSO>("Items").FirstOrDefault(i=>i&&i.CanSellAtMarket&&i.GetMarketSellPrice(0,0)>0);Check(item,"Existing sellable item resolves");inventory.Add(item,3);int slot=inventory.slots.FindIndex(s=>s?.item==item&&s.count>0);
   Check(stand.TryDepositFromSlot(slot,2)&&stand.TotalItemCount==2,"Deposit reaches existing market listings");Check(stand.TryWithdraw(0,1)&&stand.TotalItemCount==1,"Withdraw returns stock to inventory");
   var snapshot=stand.Capture();Check(snapshot.listings.Count==1&&snapshot.listings[0].count==1,"Existing save capture includes cart stock");stand.Restore(snapshot);Check(stand.TotalItemCount==1&&stand.StandId==snapshot.id,"Restore retains stock and identity");
   stand.OpenPanel();Check(MarketStand.IsAnyOpen&&stand.GetComponent<MarketStandUI>(),"Existing market UI opens");stand.ClosePanel();Check(!MarketStand.IsAnyOpen,"UI closes and releases input");
   var colliders=stand.GetComponentsInChildren<Collider>().Where(c=>!c.GetComponentInParent<NPCCharacterMotion>()).ToArray();Check(colliders.Length==5&&colliders.All(c=>c.enabled&&!c.isTrigger),"Cart, wheels and shafts block movement");Physics.SyncTransforms();
   Check(Physics.Raycast(new Vector3(0,.72f,2),Vector3.back,out var hit,3)&&hit.collider.transform.IsChildOf(stand.transform),"Approach from front reaches solid cart");report.AppendLine("MARKET_CART_PLAY_PASS checks="+checks);if(File.Exists(Output+"/PlayError.txt"))File.Delete(Output+"/PlayError.txt");
  }
  catch(Exception e){File.WriteAllText(Output+"/PlayError.txt",report+"\n"+e);Debug.LogException(e);}
  finally{File.WriteAllText(Output+"/PlayAudit.txt",report.ToString());EditorApplication.isPlaying=false;}
 }
}
#endif
