#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using System.Text;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Object=UnityEngine.Object;
[InitializeOnLoad] public static class MarketCartDebugAudit
{
 const string R="Assets/Nature  Paradaise",O="ArtSource/MarketCartDebug",K="NP.CartDebugAudit";
 static StringBuilder report=new();static IEnumerator steps;static int checks;static double start;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;report.AppendLine("PASS "+label);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static MarketCartDebugAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Fixture only");Directory.CreateDirectory(O);
  try
  {
   EditorSceneManager.OpenScene("Assets/MarketDepartureTest.unity");
   var player=new GameObject("Debug test player");player.AddComponent<Inventory>();player.AddComponent<PlayerController>().enabled=false;player.transform.position=new Vector3(0,0,-5);
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/MarketCartDebugTest.unity");SessionState.SetBool(K,true);EditorApplication.isPlaying=true;
  }catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void Changed(PlayModeStateChange s)
 {
  if(!SessionState.GetBool(K,false))return;
  if(s==PlayModeStateChange.EnteredPlayMode){report=new StringBuilder("MARKET CART DEBUG NATIVE UNITY\n");checks=0;steps=Tests();start=EditorApplication.timeSinceStartup;}
  if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(K,false);EditorApplication.Exit(File.Exists(O+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(K,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-start>120)throw new Exception("Timeout");if(steps.MoveNext())return;steps=null;report.AppendLine("MARKET_CART_DEBUG_PASS checks="+checks);File.WriteAllText(O+"/Audit.txt",report.ToString());if(File.Exists(O+"/Error.txt"))File.Delete(O+"/Error.txt");EditorApplication.isPlaying=false;}
  catch(Exception e){steps=null;Fail(e);EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){Directory.CreateDirectory(O);File.WriteAllText(O+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
 static void Click(TestingDebugMenu menu,string name)
 {var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==name);Check(button.interactable,"Debug button enabled: "+name);button.onClick.Invoke();}
 static IEnumerator Tests()
 {
  for(int i=0;i<12;i++)yield return null;
  Check(SaveManager.Instance==null,"No production save writer");var menu=TestingDebugMenu.Instance;Check(menu!=null,"Existing F12 debug menu bootstraps");
  int page=Array.IndexOf(TestingDebugMenu.Categories,"Gerobak Pasar");Check(page==8,"Dedicated category preserves existing eight category indices");
  var cart=Object.FindFirstObjectByType<MarketCartSchedule>();var stand=cart.GetComponent<MarketStand>();var clock=TimeManager.Instance;var weather=WeatherSystem.Instance;
  cart.enabled=false;cart.DebugShowAtHome();int hour=clock.hour,minute=clock.minute;var w=weather.CurrentWeather;
  var bag=new GameObject("Test stock inventory").AddComponent<Inventory>();Set(stand,"playerInventory",bag);var item=Resources.LoadAll<ItemSO>("Items").First(i=>i&&i.CanSellAtMarket&&i.GetMarketSellPrice(0,0)>0);bag.Add(item,3);int slot=bag.slots.FindIndex(s=>s?.item==item&&s.count>0);Check(stand.TryDepositFromSlot(slot,3),"Nonempty stock prepared · state="+cart.State+" trading="+stand.CanTrade+" slot="+slot);
  var renderers=cart.GetComponentsInChildren<Renderer>(true);var materials=renderers.Select(r=>r.sharedMaterials).ToArray();var colors=materials.SelectMany(m=>m).Where(m=>m&&m.HasProperty("_BaseColor")).Distinct().ToDictionary(m=>m,m=>m.GetColor("_BaseColor"));
  menu.Open(page);Check(TestingDebugMenu.IsOpen&&clock.IsPaused&&Time.timeScale==0,"Opening category pauses gameplay");
  var buttons=Object.FindObjectsByType<Button>(FindObjectsSortMode.None);Check(buttons.Any(b=>b.name=="Kembali ke jadwal normal"),"Mouse/touch controls built");
  Click(menu,"Preview fade-out di tempat · 1 detik");Check(!TestingDebugMenu.IsOpen&&!clock.IsPaused&&Time.timeScale==1,"Preview closes menu and releases only its locks");
  Check(cart.IsDebugTesting&&cart.State==MarketCartSchedule.TravelState.Fading,"Fade test starts at home despite open business hours");Capture("Before");
  Call(cart,"MoveAlongRoute",.25f);Call(cart,"MoveAlongRoute",.25f);Check(Mathf.Abs(cart.FadeVisibility-.5f)<.01f,"Fade reaches half visibility after half a second");
  yield return null;Capture("Half");
  var block=new MaterialPropertyBlock();var body=renderers.First(r=>r.sharedMaterial&&r.sharedMaterial.HasProperty("_PresentationFade"));body.GetPropertyBlock(block,0);Check(Mathf.Abs(block.GetFloat("_PresentationFade")-.5f)<.01f,"Actual renderer receives per-cart fade");
  Check(colors.All(pair=>pair.Key.GetColor("_BaseColor")==pair.Value),"Shared materials/colors remain unchanged");Check(cart.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"No collision during disappearing preview");
  Call(cart,"MoveAlongRoute",.5f);Check(!cart.IsPresented&&cart.State==MarketCartSchedule.TravelState.Hidden,"Preview ends fully hidden");yield return null;Capture("Hidden");
  for(int j=0;j<renderers.Length;j++)Check(renderers[j].sharedMaterials.SequenceEqual(materials[j]),"Original materials restored: "+renderers[j].name);
  menu.Open(page);Click(menu,"Tampilkan ulang di posisi stand");Check(cart.IsPresented&&cart.IsParked&&cart.FadeVisibility==1,"Show again restores full visibility");
  menu.Open(page);Click(menu,"Tes pergi / tutup · raih, tarik, dorong keluar kamera");Check(cart.State==MarketCartSchedule.TravelState.Departing&&!stand.CanTrade,"Departure test overrides open hours without allowing transactions");
  for(int i=0;i<1000&&cart.IsPresented;i++){Call(cart,"MoveAlongRoute",.1f);if(i%20==0)yield return null;}
  Check(!cart.IsPresented,"Departure test reaches offscreen fade and hidden state");Check(clock.hour==hour&&clock.minute==minute&&weather.CurrentWeather==w,"Test leaves clock and weather unchanged");Check(stand.TotalItemCount==3,"Fade/departure leave actual stock untouched");
  for(int i=0;i<3;i++){cart.DebugPreviewFade();Call(cart,"MoveAlongRoute",.35f);cart.DebugShowAtHome();yield return null;}
  Check(Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.name.EndsWith("(temporary cart fade)"))==0,"Repeated preview/cancel releases temporary keeper materials");
  menu.Open(page);Click(menu,"Sembunyikan langsung");Check(!cart.IsPresented,"Immediate hide test");
  menu.Open(page);Click(menu,"Kembali ke jadwal normal");Check(!cart.IsDebugTesting&&cart.IsParked&&stand.CanTrade,"Normal schedule resumes with no lingering override");
  menu.Open(page);Click(menu,"Tes datang dari atas layar");
  Check(cart.State==MarketCartSchedule.TravelState.Arriving&&Vector3.Dot((cart.transform.position-cart.HomePosition).normalized,cart.UpperExitDirection)>.45f,"Debug arrival begins above the stand, never below");
  Check(Vector3.Dot(cart.transform.forward,(cart.HomePosition-cart.transform.position).normalized)>.99f,"Arriving cart faces toward the stall");
  for(int i=0;i<600&&!cart.IsParked;i++){Call(cart,"MoveAlongRoute",.1f);if(i%20==0)yield return null;}
  Check(cart.IsParked,"Upper arrival parks successfully");
  cart.DebugPreviewFade();Call(cart,"MoveAlongRoute",.2f);cart.RestorePresentation();Check(!cart.IsDebugTesting&&cart.FadeVisibility==1,"Save load cancels debug/fade override");
  Check(!UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("Nature Paradise/Cozy Atlas")),"Fade shader compiles natively");
 }
 static void Capture(string name)
 {
  var rt=new RenderTexture(1200,800,24);rt.Create();ShaderUtil.allowAsyncCompilation=false;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(Camera.main,request);RenderPipeline.SubmitRenderRequest(Camera.main,request);var old=RenderTexture.active;RenderTexture.active=rt;var img=new Texture2D(1200,800,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1200,800),0,0);img.Apply();File.WriteAllBytes(O+"/"+name+".png",img.EncodeToPNG());RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(img);
 }
}
#endif
