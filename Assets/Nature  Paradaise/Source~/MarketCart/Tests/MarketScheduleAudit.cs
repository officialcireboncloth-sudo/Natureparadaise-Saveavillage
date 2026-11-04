#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MarketScheduleAudit
{
 const string Root="Assets/Nature  Paradaise",Out="ArtSource/MarketSchedule",Key="NP.MarketScheduleAudit";
 static StringBuilder report=new(); static int checks; static IEnumerator steps; static double began;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
 static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;report.AppendLine("PASS "+name);}
 static MarketScheduleAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Isolated fixture only");
  Directory.CreateDirectory(Out);report=new StringBuilder("MARKET CART SCHEDULE NATIVE UNITY AUDIT\n");checks=0;
  try
  {
   ApplyPrefab();Layout(); File.WriteAllText(Out+"/Layout.txt",report.ToString());
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var clock=new GameObject("Clock").AddComponent<TimeManager>();clock.hour=5;clock.minute=50;clock.day=1;clock.enabled=false;
   var weather=new GameObject("Weather").AddComponent<WeatherSystem>();weather.enabled=false;Set(weather,"currentWeather",WeatherType.Sunny);Set(weather,"currentWeatherDay",1);
   new GameObject("Gold").AddComponent<ScoreManager>();
   var cart=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/World/MarketStand.prefab"));cart.name="ScheduleTestCart";
   var camera=new GameObject("Camera",typeof(Camera));camera.tag="MainCamera";var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=6;cam.aspect=16f/9;camera.transform.position=new Vector3(0,9,9);camera.transform.LookAt(Vector3.up);
   var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-25,0);light.intensity=1.1f;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
   Box("Bin",new Vector3(2.92f,.38f,-1.59f),new Vector3(1.64f,.7f,1.54f));
   Box("House",new Vector3(2.65f,1.13f,-7.23f),new Vector3(5.96f,2.44f,5.82f));
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/MarketScheduleTest.unity");
   SessionState.SetBool(Key,true);SessionState.SetString(Key+"Report",report.ToString());SessionState.SetInt(Key+"Checks",checks);
   EditorApplication.isPlaying=true;
  }
  catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void Box(string name,Vector3 p,Vector3 size){var obj=new GameObject(name);obj.transform.position=p;obj.AddComponent<BoxCollider>().size=size;}
 static void ApplyPrefab()
 {
  string path=Root+"/Prefabs/World/MarketStand.prefab",guid=AssetDatabase.AssetPathToGUID(path);
  var obj=PrefabUtility.LoadPrefabContents(path);
  try{if(!obj.GetComponent<MarketCartSchedule>())obj.AddComponent<MarketCartSchedule>();var state=new SerializedObject(obj.GetComponent<MarketStand>());state.FindProperty("marketOpenHour").intValue=6;state.FindProperty("marketCloseHour").intValue=20;state.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(obj,path);}
  finally{PrefabUtility.UnloadPrefabContents(obj);}
  Check(AssetDatabase.AssetPathToGUID(path)==guid,"Gameplay prefab GUID retained with serialized travel settings");
 }
 static void Layout()
 {
  EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity");
  var stand=Object.FindFirstObjectByType<MarketStand>();Check(stand,"Real Map market exists");
  var original=stand.transform.position;Call(stand,"Awake");var cart=stand.GetComponent<MarketCartSchedule>();Call(cart,"Awake");
  Check(Vector3.Distance(stand.transform.position,original)<.001f&&Vector3.Distance(stand.transform.lossyScale,Vector3.one)<.001f,"Map stall placement retained and scale normalized");
  foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.tag="Untagged";
  var cam=new GameObject("Route review camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.aspect=16f/9;cam.transform.position=original+new Vector3(0,12,12);cam.transform.LookAt(original+Vector3.up);
  foreach(float zoom in new[]{5f,8f,12f})
  {
   cam.orthographicSize=zoom;Physics.SyncTransforms();object[] args={original,Vector3.zero};
   Check((bool)Call(cart,"TryFindExit",args),"Real Map clear exit found at zoom "+zoom);
   var end=(Vector3)args[1];Check(Vector3.Dot((end-original).normalized,cart.UpperExitDirection)>.45f,"Arrival/exit stays in upper screen hemisphere at zoom "+zoom);Check(!(bool)Call(cart,"VisibleAt",end),"Full cart/keeper outside gameplay frustum at zoom "+zoom);
   Check((bool)Call(cart,"SegmentClear",original,end,false),"Real Map route avoids house, bin and fences at zoom "+zoom);
   Check((bool)Call(cart,"GroundAvailable",end)&&Mathf.Abs(end.y-original.y)<2,"Exit remains on usable terrain at zoom "+zoom);
   report.AppendLine("ROUTE zoom="+zoom+" home="+original+" exit="+end+" length="+Vector3.Distance(original,end));
  }
 }
 static void State(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){report=new StringBuilder(SessionState.GetString(Key+"Report",""));checks=SessionState.GetInt(Key+"Checks",0);steps=Tests();began=EditorApplication.timeSinceStartup;}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(File.Exists(Out+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Audit timeout");if(steps.MoveNext())return;steps=null;report.AppendLine("MARKET_SCHEDULE_PASS checks="+checks);File.WriteAllText(Out+"/Audit.txt",report.ToString());if(File.Exists(Out+"/Error.txt"))File.Delete(Out+"/Error.txt");EditorApplication.isPlaying=false;}
  catch(Exception e){steps=null;Fail(e);EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
 static IEnumerator Tests()
 {
  for(int i=0;i<8;i++)yield return null;
  Check(SaveManager.Instance==null,"Test never accesses a production save writer");
  var stand=Object.FindFirstObjectByType<MarketStand>();var cart=stand.GetComponent<MarketCartSchedule>();var clock=TimeManager.Instance;var weather=WeatherSystem.Instance;var cam=Camera.main;
  Check(Mathf.Abs(cart.TravelSpeed-1.8f)<.001f,"Faster 1.8m/s travel is applied on actual prefab");
  Check(cart.State==MarketCartSchedule.TravelState.Hidden&&!cart.IsPresented&&!stand.CanTrade,"05:50 cart/keeper hidden and trading closed");
  Check(stand.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"No invisible cart/keeper collisions while absent");
  foreach(WeatherType w in Enum.GetValues(typeof(WeatherType)))
  {Set(weather,"currentWeather",w);clock.hour=12;Check(stand.IsScheduledOpen==(w==WeatherType.Sunny||w==WeatherType.PartlyCloudy||w==WeatherType.Cloudy||w==WeatherType.Heatwave),"Weather policy: "+w);}
  Set(weather,"currentWeather",WeatherType.Sunny);clock.hour=6;clock.minute=0;
  for(int i=0;i<4;i++)yield return null;
  Check(cart.State==MarketCartSchedule.TravelState.Arriving&&cart.IsPresented&&!stand.CanTrade,"06:00 arrives visibly but cannot trade on the move");
  Check(!(bool)Call(cart,"VisibleAt",cart.transform.position),"Arrival appears fully beyond camera edge");
  Vector3 start=cart.transform.position;
  for(int i=0;i<45;i++){Call(cart,"MoveAlongRoute",.1f);yield return null;}
  Check(Vector3.Distance(start,cart.transform.position)>1,"Cart and parented keeper travel toward stall");
  Check(cart.GetComponentInChildren<NPCCharacterMotion>().CurrentAction=="Cart_Push","Keeper uses authored Cart_Push while cart travels");
  Image(cam,"Arrival");
  for(int i=0;i<500&&!cart.IsParked;i++)Call(cart,"MoveAlongRoute",.1f);
  Check(cart.IsParked&&stand.CanTrade&&Vector3.Distance(cart.transform.position,cart.HomePosition)<.001f,"Arrival parks exactly at original location");
  for(int i=0;i<20;i++)yield return null;
  Check(cart.GetComponentInChildren<NPCCharacterMotion>().CurrentAction=="Idle","Keeper returns to shared Idle at stall");
  Check(stand.OpeningHoursLabel.Contains("06:00–20:00")&&stand.OpeningHoursLabel.Contains("Pasar buka"),"UI hours reflect 06:00–20:00");
  Image(cam,"Open");
  var inventory=new GameObject("Inventory").AddComponent<Inventory>();Set(stand,"playerInventory",inventory);
  var item=Resources.LoadAll<ItemSO>("Items").First(i=>i.CanSellAtMarket&&i.GetMarketSellPrice(0,0)>0);inventory.Add(item,8);int slot=inventory.slots.FindIndex(s=>s?.item==item&&s.count>0);
  Check(stand.TryDepositFromSlot(slot,5)&&stand.TotalItemCount==5,"Open-market deposit preserves original listing flow");
  Check(stand.TryWithdraw(0,1)&&stand.TotalItemCount==4,"Open-market withdrawal works");
  stand.OpenPanel();Check(MarketStand.IsAnyOpen&&clock.IsPaused,"Trading UI holds clock pause");
  Set(weather,"currentWeather",WeatherType.Drizzle);for(int i=0;i<4;i++)yield return null;
  Check(!MarketStand.IsAnyOpen&&!clock.IsPaused&&!stand.CanTrade,"Drizzle closes UI, releases pause, blocks trading");
  int bag=inventory.slots.Where(s=>s?.item==item).Sum(s=>s.count);
  Check(!stand.TryDepositFromSlot(slot,1)&&!stand.TryWithdraw(0,1)&&stand.TotalItemCount==4&&inventory.slots.Where(s=>s?.item==item).Sum(s=>s.count)==bag,"Closed deposit/withdraw cannot mutate inventory/stock");
  Check(cart.State==MarketCartSchedule.TravelState.Departing&&cart.IsPresented,"Rain/drizzle starts departure rather than onscreen disappearance ("+cart.State+")");
  bool seen=false;for(int i=0;i<600&&cart.IsPresented;i++){bool inView=(bool)Call(cart,"VisibleAt",cart.transform.position);seen|=inView;var target=(Vector3)cart.GetType().GetField("destination",Flags).GetValue(cart);var delta=target-cart.transform.position;delta.y=0;var predicted=cart.transform.position+delta.normalized*Mathf.Min(.18f,delta.magnitude);Call(cart,"MoveAlongRoute",.1f);if(!cart.IsPresented)Check(!(bool)Call(cart,"VisibleAt",predicted),"Departure hide occurs beyond camera (full bounds)");}
  Check(seen&&!cart.IsPresented&&cart.State==MarketCartSchedule.TravelState.Hidden,"Cart completes offscreen departure");
  Check(stand.TotalItemCount==4&&MarketStand.CaptureAll().Single(s=>s.id==stand.StandId).listings[0].count==4,"Absent market remains in save capture with stock");
  clock.day=9;clock.hour=12;Set(weather,"currentWeatherDay",9);Call(stand,"ProcessElapsedSales");Check(stand.TotalItemCount==4&&stand.LifetimeRevenue==0,"Seven-day guarantee never sells during rain/drizzle");
  Set(weather,"currentWeather",WeatherType.Sunny);clock.hour=20;Call(stand,"ProcessElapsedSales");for(int i=0;i<3;i++)yield return null;
  Check(!stand.IsScheduledOpen&&!stand.CanTrade&&stand.TotalItemCount==4,"20:00 is closed even when sunny");
  clock.hour=5;clock.minute=50;Check(!stand.IsScheduledOpen,"05:50 stays closed");clock.hour=19;clock.minute=50;Check(stand.IsScheduledOpen,"19:50 remains scheduled open");
  var saved=stand.Capture();stand.Restore(saved);for(int i=0;i<3;i++)yield return null;
  Check(stand.CanTrade&&stand.TotalItemCount==4&&stand.LifetimeRevenue==0,"Load restores parked open cart without offline sale or stock loss");
  clock.hour=20;for(int i=0;i<3;i++)yield return null;Check(cart.State==MarketCartSchedule.TravelState.Departing,"20:00 starts timed departure");
  for(int i=0;i<32;i++)Call(cart,"MoveAlongRoute",.1f);
  clock.hour=6;Set(weather,"currentWeather",WeatherType.Heatwave);for(int i=0;i<3;i++)yield return null;
  Check(cart.State==MarketCartSchedule.TravelState.Arriving,"Reopening while leaving reverses journey safely");
  for(int i=0;i<600&&!cart.IsParked;i++)Call(cart,"MoveAlongRoute",.1f);
  Check(cart.IsParked&&stand.CanTrade,"Heatwave permits arrival and trading");
  clock.day=10;Set(weather,"currentWeatherDay",10);clock.hour=7;Call(stand,"ProcessElapsedSales");Check(stand.TotalItemCount==0&&stand.LifetimeRevenue>0,"Overdue stock sells only on an open parked market hour");
  var revenue=stand.LifetimeRevenue;Call(stand,"ProcessElapsedSales");Check(stand.LifetimeRevenue==revenue,"Repeated same-hour updates cannot duplicate payout");
  // A following camera must no longer keep the departing cart alive forever.
  clock.hour=20;for(int i=0;i<3;i++)yield return null;
  for(int i=0;i<220&&cart.IsPresented;i++){cam.transform.position=cart.transform.position+new Vector3(0,9,9);cam.transform.LookAt(cart.transform.position+Vector3.up);cam.orthographicSize=12;Set(cart,"nextRouteTry",0f);Call(cart,"MoveAlongRoute",.1f);}
  Check(!cart.IsPresented,"Following/zooming player still completes graceful fade after 18m");
  cam.transform.position=new Vector3(0,9,9);cam.transform.LookAt(Vector3.up);for(int i=0;i<4;i++)yield return null;
  for(int i=0;i<500&&cart.IsPresented;i++)Call(cart,"MoveAlongRoute",.1f);
  Check(!cart.IsPresented,"Cart hides after tracking camera finally loses sight");
  // Rain save is immediately absent; restoring never replays yesterday's business.
  Set(weather,"currentWeather",WeatherType.Rain);stand.Restore(saved);for(int i=0;i<3;i++)yield return null;
  Check(!cart.IsPresented&&!stand.CanTrade&&stand.TotalItemCount==4&&stand.LifetimeRevenue==0,"Rain save restores hidden cart and exact stored listings/revenue");
  Check(stand.StandId=="village-market-stand-01"&&Vector3.Distance(cart.HomePosition,Vector3.zero)<.001f,"Travel preserves save identity and home transform");
  Set(weather,"currentWeather",WeatherType.Cloudy);clock.hour=12;stand.Restore(saved);for(int i=0;i<3;i++)yield return null;
  Check(cart.IsParked&&stand.CanTrade,"Mendung restored during opening hours can trade");
  clock.AcquirePause(cart);clock.hour=20;var pausedPosition=cart.transform.position;for(int i=0;i<4;i++)yield return null;
  Check(Vector3.Distance(cart.transform.position,pausedPosition)<.001f,"Game pause freezes cart movement");clock.ReleasePause(cart);
  for(int i=0;i<4;i++)yield return null;
  Check(cart.State==MarketCartSchedule.TravelState.Departing,"Departure resumes after pause released");
  for(int i=0;i<23;i++)Call(cart,"MoveAlongRoute",.1f);
  var targetPoint=(Vector3)cart.GetType().GetField("destination",Flags).GetValue(cart);var travelDirection=(targetPoint-cart.transform.position).normalized;
  var blocker=new GameObject("Animal crossing route");blocker.transform.position=cart.transform.position+travelDirection*.8f;blocker.AddComponent<CapsuleCollider>().height=1.5f;blocker.AddComponent<NPCCharacterMotion>();Physics.SyncTransforms();
  var beforeBlock=cart.transform.position;for(int i=0;i<30;i++)Call(cart,"MoveAlongRoute",.1f);
  Check(Vector3.Distance(cart.transform.position,beforeBlock)<.001f,"Cart waits for an actor crossing its initial sweep volume");
  Object.DestroyImmediate(blocker);Physics.SyncTransforms();for(int i=0;i<60&&cart.IsPresented;i++)Call(cart,"MoveAlongRoute",.1f);
  Check(Vector3.Distance(cart.transform.position,beforeBlock)>1||!cart.IsPresented,"Cart resumes after actor clears route");
 }
 static void Image(Camera cam,string name)
 {
  var rt=new RenderTexture(960,600,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var img=new Texture2D(960,600,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,960,600),0,0);img.Apply();File.WriteAllBytes(Out+"/"+name+".png",img.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(img);
 }
}
#endif
