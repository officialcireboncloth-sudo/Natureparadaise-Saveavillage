#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MarketDepartureAudit
{
 const string R="Assets/Nature  Paradaise",O="ArtSource/MarketDeparture",K="NP.MarketDepartureAudit";
 static StringBuilder report=new();static int checks;static IEnumerator steps;static double began;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;report.AppendLine("PASS "+label);}
 static MarketDepartureAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Fixture only");
  Directory.CreateDirectory(O);
  try
  {
   MarketCartDepartureSetup.Apply();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var clock=new GameObject("Clock").AddComponent<TimeManager>();clock.hour=19;clock.minute=50;clock.enabled=false;
   var weather=new GameObject("Weather").AddComponent<WeatherSystem>();weather.enabled=false;Set(weather,"currentWeather",WeatherType.Sunny);Set(weather,"currentWeatherDay",1);
   new GameObject("Gold").AddComponent<ScoreManager>();
   PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Prefabs/World/MarketStand.prefab"));
   var cam=new GameObject("Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=2;cam.aspect=1.5f;cam.transform.position=new Vector3(-3.8f,2.5f,-4.5f);cam.transform.LookAt(new Vector3(0,.8f,-.5f));cam.farClipPlane=100;cam.backgroundColor=new Color(.53f,.59f,.49f);cam.clearFlags=CameraClearFlags.SolidColor;
   var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.shadows=LightShadows.Soft;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.7f,.7f,.7f);
   var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.45f,.45f,.45f));RenderSettings.ambientProbe=sh;
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/MarketDepartureTest.unity");SessionState.SetBool(K,true);EditorApplication.isPlaying=true;
  }catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void Changed(PlayModeStateChange s)
 {
  if(!SessionState.GetBool(K,false))return;
  if(s==PlayModeStateChange.EnteredPlayMode){report=new StringBuilder("MARKET DEPARTURE NATIVE UNITY\n");checks=0;steps=Tests();began=EditorApplication.timeSinceStartup;}
  if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(K,false);EditorApplication.Exit(File.Exists(O+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(K,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-began>150)throw new Exception("Timeout");if(steps.MoveNext())return;steps=null;report.AppendLine("MARKET_DEPARTURE_PASS checks="+checks);File.WriteAllText(O+"/Audit.txt",report.ToString());if(File.Exists(O+"/Error.txt"))File.Delete(O+"/Error.txt");EditorApplication.isPlaying=false;}
  catch(Exception e){steps=null;Fail(e);EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){Directory.CreateDirectory(O);File.WriteAllText(O+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
 static Transform Named(GameObject g,string n)=>g.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 static IEnumerator Tests()
 {
  for(int i=0;i<10;i++)yield return null;
  Check(SaveManager.Instance==null,"No production save writer");
  var stand=Object.FindFirstObjectByType<MarketStand>();var schedule=stand.GetComponent<MarketCartSchedule>();var driver=stand.GetComponent<MarketCartDriver>();var npc=stand.GetComponentInChildren<NPCCharacterMotion>();var animator=npc.character;var library=npc.actions;
  Check(schedule.IsParked&&stand.CanTrade,"Open session starts parked at 19:50");schedule.enabled=false;npc.enabled=false;
  Check(stand.StandId=="village-market-stand-01","Stable market save identity");
  var lod=stand.GetComponentInChildren<LODGroup>();lod.ForceLOD(0);var b=lod.GetLODs()[0].renderers[0].bounds;foreach(var r in lod.GetLODs()[0].renderers.Skip(1))b.Encapsulate(r.bounds);
  Check(Mathf.Abs(b.size.x-1.95f)<.02f&&b.size.y<1.61f,"Cart is 1.95m wide and 1.60m tall: "+b.size);
  Check(Named(stand.gameObject,"KeeperAnchor").localPosition.z> -1.8f,"Keeper stands at actual shortened straight grips");
  var left=Named(stand.gameObject,"LeftGripAnchor");var right=Named(stand.gameObject,"RightGripAnchor");
  foreach(string id in new[]{"Cart_Grip","Cart_Pull","Cart_Push"})
  {
   var entry=library.Find(id);Check(entry?.clip!=null&&entry.clip.isHumanMotion&&Mathf.Abs(entry.clip.length-1)<.05f,id+" is valid 1-second Humanoid Action");Check(entry.loop==(id!="Cart_Grip"),id+" loop setting");
   using(var preview=new HumanoidActionPlayer(animator,library))
   {
    preview.Play(id);for(int i=0;i<30;i++)preview.Evaluate(1f/30);
    var lh=animator.GetBoneTransform(HumanBodyBones.LeftHand);var rh=animator.GetBoneTransform(HumanBodyBones.RightHand);
    float le=Vector3.Distance(lh.position,left.position),re=Vector3.Distance(rh.position,right.position);
    report.AppendLine("CONTACT "+id+" left="+le+" right="+re+" hand="+lh.position+" target="+left.position);
    Check(le<.09f&&re<.09f,id+" wrists remain close to actual handle anchors");

    if(entry.loop){for(int i=0;i<30;i++)preview.Evaluate(1f/30);var start=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;for(int i=0;i<30;i++)preview.Evaluate(1f/30);float drift=Vector3.Distance(start,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);report.AppendLine("LOOP "+id+" drift="+drift+" start="+start+" end="+animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);Check(drift<.015f,id+" loop returns without a foot pop");}
    yield return null;yield return null;Capture(Camera.main,id);
   }
  }
  npc.enabled=true;driver.ResetPresentation();
  var inventory=new GameObject("Audit bag").AddComponent<Inventory>();Set(stand,"playerInventory",inventory);
  var item=Resources.LoadAll<ItemSO>("Items").First(i=>i&&i.CanSellAtMarket&&i.GetMarketSellPrice(0,0)>0);inventory.Add(item,3);int slot=inventory.slots.FindIndex(s=>s?.item==item&&s.count>0);
  Check(stand.TryDepositFromSlot(slot,3)&&stand.TotalItemCount==3,"Place actual stock before departure");
  TimeManager.Instance.hour=20;TimeManager.Instance.minute=0;
  Check(!stand.CanTrade,"20:00 stops trading immediately");Call(schedule,"BeginDeparture");
  Check(driver.Action=="Cart_Grip","Closing reaches for handles before travelling");var initial=stand.transform.position;
  for(int i=0;i<19;i++){Call(schedule,"MoveAlongRoute",.05f);yield return null;}
  Check(Vector3.Distance(initial,stand.transform.position)<.001f,"Cart stays still during reach");
  for(int i=0;i<3;i++){Call(schedule,"MoveAlongRoute",.05f);yield return null;}
  Check(driver.Action=="Cart_Pull","Reach transitions to backwards pull");
  var wheel=Named(stand.gameObject,"WheelLeftPivot");var wheelBefore=wheel.localRotation;
  for(int i=0;i<20;i++){Call(schedule,"MoveAlongRoute",.05f);yield return null;}
  Check(Vector3.Dot(stand.transform.position-initial,stand.transform.forward)<-.35f,"Short backwards pull moves cart and keeper together");
  Check(Quaternion.Angle(wheelBefore,wheel.localRotation)>20,"Wheels rotate backwards with travelled distance");Check(driver.Action=="Cart_Push","Pull transitions to looping push");
  var stock=stand.Capture();
  for(int i=0;i<1000&&schedule.IsPresented;i++){Call(schedule,"MoveAlongRoute",.1f);if(i%10==0)yield return null;}
  Check(!schedule.IsPresented&&schedule.State==MarketCartSchedule.TravelState.Hidden,"Complete cart and keeper disappear only beyond camera");Check(stand.StandId==stock.id&&stand.TotalItemCount==stock.listings.Sum(x=>x.count),"Departure retains inventory and save identity");
  Check(stand.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"No hidden collisions after departure");
 }
 static void Capture(Camera cam,string name)
 {
  var rt=new RenderTexture(1200,800,24);rt.Create();ShaderUtil.allowAsyncCompilation=false;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(cam,request);RenderPipeline.SubmitRenderRequest(cam,request);var old=RenderTexture.active;RenderTexture.active=rt;var img=new Texture2D(1200,800,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1200,800),0,0);img.Apply();File.WriteAllBytes(O+"/"+name+".png",img.EncodeToPNG());RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(img);
 }
}
#endif
