#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Text;
using UnityEngine;using UnityEngine.UI;using TMPro;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad] public static class BellsPondAudit
{
 const string Root="Assets/Nature  Paradaise",Out="ArtSource/BellsPond",Key="NP.BellsPondAudit";
 static StringBuilder report=new();static int checks;static IEnumerator steps;static double began;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;report.AppendLine("PASS "+message);}
 static BellsPondAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Isolated fixture only");
  Directory.CreateDirectory(Out);if(File.Exists(Out+"/Error.txt"))File.Delete(Out+"/Error.txt");
  try
  {
   report=new StringBuilder("BARN/COOP BELLS AND POND OVERVIEW — NATIVE UNITY\n");checks=0;
   ShaderUtil.allowAsyncCompilation=false;FishPondIconSetup.Apply();HousingBellSetup.Apply();
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var cam=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=4;cam.farClipPlane=100;cam.backgroundColor=new(.32f,.40f,.23f);cam.clearFlags=CameraClearFlags.SolidColor;
   var sun=new GameObject("Review sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new(.62f,.65f,.64f);
   foreach(string kind in new[]{"Barn","Coop"})for(int level=1;level<=5;level++)
   {
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/{kind}/Exterior/{kind}Exterior_Lv{level}.prefab");
    var item=Object.Instantiate(prefab);var authored=item.GetComponent<BarnExteriorAuthoring>();var bell=authored.Bell;Physics.SyncTransforms();
    Check(bell!=null&&bell.SwingPivot!=null&&bell.Approach!=null,kind+level+" has authored shared bell and approach");
    var pivot=authored.ModelRoot.InverseTransformPoint(bell.SwingPivot.position);var entrance=authored.ModelRoot.InverseTransformPoint(authored.Entrance.position);
    Check(pivot.x<entrance.x&&pivot.z>2.9f,kind+level+" bell visible in front on viewer-right of door");
    var post=bell.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.Contains("StandalonePost"));
    Check(post.gameObject.activeInHierarchy,kind+level+" standalone timber post visible");
    float targetHeight=kind=="Coop"?1.2432f:1.5096f;
    Check(Mathf.Abs(post.bounds.min.y-item.transform.position.y)<.005f&&Mathf.Abs(post.bounds.size.y-targetHeight)<.015f,kind+level+" post grounded and height stays modest across upgrades");
    Check(post.bounds.size.x<(kind=="Coop"?.24f:.29f),kind+level+" narrow post and small base");
    var mesh=bell.SwingPivot.GetComponentInChildren<MeshRenderer>();Check(mesh.bounds.size.x<.65f&&mesh.bounds.size.y<1.55f,kind+level+" bell and rope remain small");
    var approach=bell.Approach.position+Vector3.up*.6f;Check(!authored.BuildingCollision.bounds.Contains(approach),kind+level+" bell approach outside wall");
    Check(Vector3.Distance(bell.Approach.position,authored.Entrance.position)>.65f,kind+level+" door and bell approaches separated");
    if(level==1){cam.transform.position=new Vector3(-.6f,4.1f,10);cam.transform.LookAt(new Vector3(0,1.8f,0));Capture(cam,kind+"Bell",1200,900);}
    Object.DestroyImmediate(item);
   }
   var pond=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/FishPond/FishPond_Lv1.prefab"));pond.name="Pond presentation test";
   var old=new GameObject("FishPond_DebugLabel",typeof(TextMesh));old.transform.SetParent(pond.transform,false);old.GetComponent<TextMesh>().text="OLD DEBUG MUST HIDE";
   var player=new GameObject("Overview player",typeof(Inventory));player.transform.position=pond.GetComponent<FishPond>().OverviewPoint+Vector3.back;
   cam.transform.position=new Vector3(0,7.8f,9.8f);cam.transform.LookAt(new Vector3(-.2f,.3f,0));cam.orthographicSize=4.2f;
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/BellsPondTest.unity");
   File.WriteAllText(Out+"/EditAudit.txt",report.ToString());SessionState.SetInt(Key+"Count",checks);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
  }catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void State(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){report=new StringBuilder(File.ReadAllText(Out+"/EditAudit.txt"));checks=SessionState.GetInt(Key+"Count",0);steps=Tests();began=EditorApplication.timeSinceStartup;}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(File.Exists(Out+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-began>90)throw new Exception("Timed out");if(steps.MoveNext())return;report.AppendLine("BELLS_POND_PASS checks="+checks);File.WriteAllText(Out+"/Audit.txt",report.ToString());steps=null;EditorApplication.isPlaying=false;}
  catch(Exception e){Fail(e);steps=null;EditorApplication.isPlaying=false;}
 }
 static IEnumerator Tests()
 {
  for(int i=0;i<8;i++)yield return null;
  var pond=Object.FindFirstObjectByType<FishPond>();var player=Object.FindFirstObjectByType<Inventory>();var hud=Object.FindFirstObjectByType<FishPondHUD>();var cam=Camera.main;
  Check(Object.FindFirstObjectByType<SaveManager>()==null,"No production save writer in test scene");
  Check(hud!=null&&Object.FindObjectsByType<FishPondHUD>(FindObjectsSortMode.None).Length==1,"One pond overview auto-installed");
  var theme=Resources.Load<FishPondTheme>("UI/FishPondTheme");
  Check(new[]{theme.overviewFish,theme.overviewMachine,theme.overviewFeed,theme.overviewHealth}.All(s=>s!=null&&s.texture.width==128&&s.texture.height==128),"Four genuine PNG sprite icons imported at 128 square");
  Check(hud.GetComponentsInChildren<FishPondHUDIcon>(true).Length==0,"Overview uses PNG Images instead of procedural vector placeholders");
  Check(hud.GetComponentsInChildren<Image>(true).Count(i=>i.sprite!=null&&i.material==theme.overviewIconMaterial)==5,"Header and all four rows have the linked silhouette sprites");
  Check(!ShaderUtil.GetShaderMessages(theme.overviewIconMaterial.shader).Any(m=>m.severity.ToString()=="Error"),"Mobile silhouette UI shader compiles cleanly");
  Check(!pond.transform.Find("FishPond_DebugLabel").gameObject.activeSelf,"Legacy giant debug label disabled at startup");
  Check(!pond.GetComponentsInChildren<TextMesh>().Any(t=>t.text.Contains("EMPTY")),"No world-space pond/machine debug text visible");
  player.transform.position=pond.OverviewPoint+Vector3.back*.7f;for(int i=0;i<3;i++)yield return null;
  var content=hud.transform.Find("Safe Area/Nearby Pond");Check(content.gameObject.activeSelf,"Overview appears only near pond");
  hud.RefreshData(pond);Check(hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="Belum ada ikan"),"Empty pond footer uses real fish count");
  Check(hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text==$"0 / {pond.Capacity}"),"Displayed capacity follows system, not reference mockup");
  CaptureHUD(cam,hud,"PondOverview_Empty",1600,900);
  var fish=Resources.Load<ItemSO>("Items/Fish/Tilapia");player.Add(fish,1,0,20,.3f);int slot=player.slots.FindIndex(s=>s?.item==fish&&s.count>0);pond.AddFish(slot);hud.RefreshData(pond);
  Check(pond.FishCount==1&&hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text==$"1 / {pond.Capacity}"),"Depositing fish updates overview from existing service");
  var records=FishPondService.Capture();records[0].fish[0].sick=true;records[0].feedStock=Mathf.Min(2,pond.FeedCapacity);FishPondService.Restore(records);hud.RefreshData(pond);
  Check(hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="1 sakit")&&hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text==$"{records[0].feedStock} / {pond.FeedCapacity}"),"Health/feed show stored sick fish and real stock");
  var before=JsonUtility.ToJson(records[0]);CaptureHUD(cam,hud,"PondOverview_Mobile",900,1600);
  Check(JsonUtility.ToJson(FishPondService.Capture()[0])==before,"Rendering overview leaves pond save data untouched");
  pond.OpenPanel();yield return null;Check(pond.IsPanelOpen&&FishPondUI.Instance!=null&&!content.gameObject.activeSelf,"Manage pond modal still works and hides overview");pond.ClosePanel();for(int i=0;i<2;i++)yield return null;
  Check(content.gameObject.activeSelf&&Mathf.Approximately(Time.timeScale,1),"Closing management restores overview and time scale");
  player.transform.position=Vector3.one*100;yield return null;Check(!content.gameObject.activeSelf,"Overview hides when player leaves pond");
  Object.Destroy(pond.gameObject);yield return null;Check(!content.gameObject.activeSelf,"Removing pond leaves no orphan panel");
  foreach(string kind in new[]{"Barn","Coop"})
  {
   var building=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/{kind}/Exterior/{kind}Exterior_Lv1.prefab"));
   cam.transform.position=new Vector3(-.6f,4.1f,10);cam.transform.LookAt(new Vector3(0,1.8f,0));cam.orthographicSize=4;
   for(int i=0;i<3;i++)yield return null;Capture(cam,kind+"Bell",1200,900);Object.Destroy(building);
  }
 }
 static void CaptureHUD(Camera cam,FishPondHUD hud,string name,int w,int h)
 {
  var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
  var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();
  typeof(FishPondHUD).GetField("expanded",F).SetValue(hud,true);typeof(FishPondHUD).GetMethod("Align",F).Invoke(hud,null);Canvas.ForceUpdateCanvases();
  cam.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=prior;cam.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
  canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
 }
 static void Capture(Camera cam,string name,int w,int h)
 {
  var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
  var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=prior;cam.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
 }
 static void Fail(Exception e){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
}
#endif
