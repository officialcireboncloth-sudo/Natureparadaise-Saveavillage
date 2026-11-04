#if UNITY_EDITOR
using System;using System.IO;using System.Text;using System.Linq;using System.Reflection;using System.Collections;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using TMPro;using Object=UnityEngine.Object;
[InitializeOnLoad] public static class LumberOwnerInteractionAudit
{
 const string Root="Assets/Nature  Paradaise",Out="ArtSource/LumberOwner",Key="NP.LumberOwnerInteractionAudit";
 static StringBuilder report=new();static int checks;static IEnumerator steps;static double began;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,args);
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;report.AppendLine("PASS "+label);}
 static LumberOwnerInteractionAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Isolated fixture only");
  Directory.CreateDirectory(Out);report=new StringBuilder("LUMBER OWNER INTERACTION — NATIVE UNITY\n");checks=0;
  try
  {
   EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity");
   var front=Object.FindObjectsByType<UpgradeShopFront>(FindObjectsSortMode.None).Single(f=>f.kind==UpgradeShopKind.Lumber);
   var p=front.transform.position;var rotation=front.transform.rotation;var scale=front.transform.lossyScale;Physics.SyncTransforms();
   var authored=front.builderDeparturePoint!=null?front.builderDeparturePoint.position:p+front.transform.forward*2.2f;
   var actualHome=ConstructionProject.GroundDeparture(authored);
   var template=Object.Instantiate(front.gameObject);template.name="LumberExactMapInstance";template.transform.SetParent(null,true);template.transform.SetPositionAndRotation(p,rotation);template.transform.localScale=scale;
   // Use the actual Map's safe result rather than allowing a simplified collision snapshot to choose another home.
   template.GetComponent<UpgradeShopFront>().builderDeparturePoint.position=actualHome;
   // Capture the terrain and raised solid obstacles around the real Map marker. No scene managers are copied.
   var data=Terrain.activeTerrains.Select(t=>(t.terrainData,t.transform.position)).ToArray();
   var blocks=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c is BoxCollider&&!c.isTrigger&&!c.transform.IsChildOf(template.transform)&&!c.transform.IsChildOf(front.transform)&&Vector3.Distance(c.bounds.ClosestPoint(p),p)<12).Select(c=>(c.name,c.bounds)).ToArray();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(template,scene);
   foreach(var terrain in data){var obj=Terrain.CreateTerrainGameObject(terrain.terrainData);obj.transform.position=terrain.position;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);}
   foreach(var block in blocks){var obj=new GameObject(block.name+"_CollisionSnapshot");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);obj.transform.position=block.bounds.center;obj.AddComponent<BoxCollider>().size=block.bounds.size;}
   EditorSceneManager.CloseScene(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity"),true);
   var player=new GameObject("Player interaction test");player.AddComponent<Inventory>();player.AddComponent<PlayerController>().enabled=false;player.transform.position=p+Vector3.back*8;
   var clock=new GameObject("Clock").AddComponent<TimeManager>();clock.enabled=false;clock.hour=9;new GameObject("Gold").AddComponent<ScoreManager>().points=10000;
   var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=p+new Vector3(8,9,-10);camera.transform.LookAt(p+Vector3.up);
   EditorSceneManager.SaveScene(scene,"Assets/LumberOwnerInteractionTest.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
  }
  catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void State(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){steps=Tests();report=new StringBuilder("LUMBER OWNER INTERACTION — NATIVE UNITY\n");checks=0;began=EditorApplication.timeSinceStartup;}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(File.Exists(Out+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Timeout");if(steps.MoveNext())return;steps=null;report.AppendLine("LUMBER_OWNER_INTERACTION_PASS checks="+checks);File.WriteAllText(Out+"/Audit.txt",report.ToString());if(File.Exists(Out+"/Error.txt"))File.Delete(Out+"/Error.txt");EditorApplication.isPlaying=false;}
  catch(Exception e){steps=null;Fail(e);EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
 static IEnumerator Tests()
 {
  for(int i=0;i<12;i++)yield return null;
  Check(SaveManager.Instance==null,"No production save writer in test");
  var front=Object.FindFirstObjectByType<UpgradeShopFront>();var owner=front.GetComponent<LumberStoreOwner>();var player=Object.FindFirstObjectByType<PlayerController>();
  var body=player.GetComponent<CharacterController>();var label=front.transform.Find("StoreLabel_Editable").GetComponent<TMP_Text>();
  void Place(Vector3 p){body.enabled=false;player.transform.position=p;body.enabled=true;Physics.SyncTransforms();}
  Check(owner!=null&&owner.IdleVisual!=null,"Real Lumber owner and dedicated model created");
  Check(front.InteractionAnchor==owner.IdleVisual,"Shop target follows actual idle model");
  Check(Vector3.Distance(label.transform.position,owner.IdleVisual.position+Vector3.up*2.15f)<.01f,"Lumber label follows grounded character rather than old pivot");
  float separation=Vector3.Distance(front.transform.position,owner.IdleVisual.position);report.AppendLine("Map pivot="+front.transform.position+" grounded owner="+owner.IdleVisual.position+" separation="+separation);
  Check(separation>front.interactionRadius,"Fixture reproduces model farther than old interaction radius");
  Check(Vector3.Distance(owner.Departure,owner.IdleVisual.position)<.01f,"Construction departure shares grounded owner home");
  Check(Vector3.Distance(ConstructionProject.FindDeparture(front.transform.position),owner.Departure)<.05f,"Project departure lookup uses same owner anchor");
  var origin=owner.IdleVisual.position;
  foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
  {
   Place(origin+direction*1.1f);for(int i=0;i<3;i++)yield return null;
   Check(front.CanInteractWith(player),"Character can be approached from "+direction);
   Check(WorldInteractionPrompt.IsDisplayedFor(front,front.InteractionAnchor),"One Lumber prompt uses visible character anchor from "+direction);
  }
  // Old empty shop pivot is not an alternative invisible interaction target.
  Place(front.transform.position);for(int i=0;i<3;i++)yield return null;
  Check(!front.CanInteractWith(player)&&!WorldInteractionPrompt.IsDisplayedFor(front),"Empty old pivot no longer opens an invisible shop");
  Place(origin+Vector3.back*1.1f);for(int i=0;i<4;i++)yield return null;
  Check(WorldInteractionPrompt.InvokeDisplayedAction(),"Mouse/touch action reaches visible Lumber service");
  Check(UpgradeShopFront.Active==front&&front.uiRoot.activeInHierarchy,"Touch opens actual scene-overridden Lumber UI");
  Check(TimeManager.Instance.IsPaused&&player.IsMovementLocked,"Open UI owns time and movement locks");
  Check(front.structureBuild.gameObject.activeInHierarchy&&front.structureUpgrade.gameObject.activeInHierarchy,"Build and Upgrade structure services remain present");
  front.Close();for(int i=0;i<3;i++)yield return null;
  Check(!TimeManager.Instance.IsPaused&&!player.IsMovementLocked&&!WorldInteractionPrompt.IsSuppressed,"Close releases all UI locks");
  Call(front,"OpenFromPrompt");Check(UpgradeShopFront.Active==front,"PC prompt callback uses same character target and UI");front.Close();
  Place(origin+Vector3.right*10);for(int i=0;i<3;i++)yield return null;Call(front,"OpenFromPrompt");Check(UpgradeShopFront.Active==null,"Stale callback rejects player outside current range");
  Place(origin+Vector3.back*1.1f);player.AcquireMovementLock(front);Call(front,"OpenFromPrompt");Check(UpgradeShopFront.Active==null,"Locked player cannot start shop interaction");player.ReleaseMovementLock(front);
  // Owner dispatch still hides the idle body once and permits queued orders at the anchored service location.
  var orderObject=new GameObject("Dispatch fixture");var project=orderObject.AddComponent<ConstructionProject>();var job=new ConstructionJobData{phase=ConstructionWorkerPhase.Working,requiredWorkHours=10,queueOrder=1};typeof(ConstructionProject).GetField("job",Flags).SetValue(project,job);
  string storeId=owner.StoreId;owner.Register(project);Check(owner.TryDispatch(project)&&owner.IsAway&&!owner.IdleVisual.gameObject.activeSelf,"Construction dispatch hides idle owner without duplication");
  for(int i=0;i<3;i++)yield return null;
  Check(front.InteractionAnchor.gameObject.activeInHierarchy&&Vector3.Distance(front.InteractionAnchor.position,origin)<.01f,"Away owner retains active grounded order point");
  Check(label.text.Contains("LUMBER STORE")&&label.text.Contains("pesanan"),"Store remains identifiable and displays owner-away status");
  Check(front.CanInteractWith(player),"Queued orders can still be placed while builder is away");
  owner.Release(project);for(int i=0;i<3;i++)yield return null;
  Check(!owner.IsAway&&owner.IdleVisual.gameObject.activeSelf&&front.InteractionAnchor==owner.IdleVisual,"Builder return restores character interaction");
  Check(label.text=="LUMBER STORE"&&owner.StoreId==storeId,"Return restores label and preserves saved builder store identity");
  var capsule=owner.IdleVisual.GetComponent<CapsuleCollider>();owner.IdleVisual.GetComponent<ConstructionWorkerBody>().Refresh();Check(capsule.enabled&&!capsule.isTrigger,"Idle owner remains a solid body");
  Camera.main.transform.position=origin+new Vector3(5,6,-8);Camera.main.transform.LookAt(origin+Vector3.up);Camera.main.orthographicSize=4.5f;
  for(int i=0;i<3;i++)yield return null;Render(Camera.main,"LumberOwner_Aligned");
 }
 static void Render(Camera cam,string name)
 {
  var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
 }
}
#endif
