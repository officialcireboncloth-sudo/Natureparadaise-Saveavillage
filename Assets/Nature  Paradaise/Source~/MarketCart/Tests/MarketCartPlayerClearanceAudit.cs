#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using System.Text;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
[InitializeOnLoad] public static class MarketCartPlayerClearanceAudit
{
 const string O="ArtSource/MarketCartClearance",K="NP.CartClearanceAudit";
 static IEnumerator steps;static StringBuilder report=new();static int checks;static double began;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;report.AppendLine("PASS "+text);}
 static MarketCartPlayerClearanceAudit(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
 public static void Run()
 {
  if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Fixture only");Directory.CreateDirectory(O);
  try
  {
   EditorSceneManager.OpenScene("Assets/MarketDepartureTest.unity");
   var floor=new GameObject("Real solid ground");floor.transform.position=new Vector3(0,-.15f,0);floor.AddComponent<BoxCollider>().size=new Vector3(100,.3f,100);
   var player=new GameObject("Blocking player");player.AddComponent<Inventory>();player.AddComponent<PlayerController>().enabled=false;player.transform.position=new Vector3(4,0,4);
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/MarketCartClearanceTest.unity");SessionState.SetBool(K,true);EditorApplication.isPlaying=true;
  }catch(Exception e){Fail(e);EditorApplication.Exit(1);}
 }
 static void Changed(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(K,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){steps=Tests();report=new StringBuilder("MARKET CART PLAYER CLEARANCE — NATIVE UNITY\n");checks=0;began=EditorApplication.timeSinceStartup;}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(K,false);EditorApplication.Exit(File.Exists(O+"/Error.txt")?1:0);}
 }
 static void Tick()
 {
  if(!SessionState.GetBool(K,false)||!EditorApplication.isPlaying||steps==null)return;
  try{if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Timeout");if(steps.MoveNext())return;steps=null;report.AppendLine("MARKET_CART_CLEARANCE_PASS checks="+checks);File.WriteAllText(O+"/Audit.txt",report.ToString());if(File.Exists(O+"/Error.txt"))File.Delete(O+"/Error.txt");EditorApplication.isPlaying=false;}
  catch(Exception e){steps=null;Fail(e);EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){Directory.CreateDirectory(O);File.WriteAllText(O+"/Error.txt",report+"\n"+e);Debug.LogException(e);}
 static void Place(PlayerController player,Vector3 at)
 {var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
 static GameObject Wall(float x)
 {var go=new GameObject("Side wall "+x);go.transform.position=new Vector3(x,1,1);go.AddComponent<BoxCollider>().size=new Vector3(.2f,2,8);Physics.SyncTransforms();return go;}
 static bool Advance(MarketCartSchedule cart)=> (bool)Call(cart,"ClearMovingSegment",cart.transform.position,cart.transform.position+Vector3.forward*.09f,.05f);
 static IEnumerator Tests()
 {
  for(int i=0;i<12;i++)yield return null;
  Check(SaveManager.Instance==null,"No production save access");
  var cart=Object.FindFirstObjectByType<MarketCartSchedule>();var player=Object.FindFirstObjectByType<PlayerController>();var cc=player.GetComponent<CharacterController>();cart.enabled=false;
  Check(cc&&cc.enabled,"Actual player CharacterController is active");
  foreach(float x in new[]{-.25f,0f,.25f})
  {
   cart.DebugShowAtHome();Place(player,new Vector3(x,0,1.2f));float beforeY=player.transform.position.y;bool clear=false;
   for(int i=0;i<20&&!clear;i++){clear=Advance(cart);yield return null;}
   report.AppendLine("SIDE startX="+x+" final="+player.transform.position);
   Check(clear,"Player cannot hold cart at lateral offset "+x);Check(x<0?player.transform.position.x< -1.2f:player.transform.position.x>1.2f,"Nearest available side selected at "+x);Check(Mathf.Abs(player.transform.position.y-beforeY)<.03f,"No lift/teleport onto cart at "+x);
   Check(!player.IsMovementLocked,"No persistent movement lock at "+x);
  }
  cart.DebugShowAtHome();Place(player,new Vector3(0,0,-2.24f));bool reverseClear=false;
  for(int i=0;i<25&&!reverseClear;i++){reverseClear=(bool)Call(cart,"ClearMovingSegment",cart.transform.position,cart.transform.position-Vector3.forward*.09f,.05f);yield return null;}
  Check(reverseClear&&Mathf.Abs(player.transform.position.x)>1.2f,"Backwards pull also clears the player beside the keeper");
  cart.DebugShowAtHome();var right=Wall(1.55f);Place(player,new Vector3(.1f,0,1.2f));bool free=false;
  for(int i=0;i<25&&!free;i++){free=Advance(cart);yield return null;}
  Check(free&&player.transform.position.x< -1.2f,"Right side blocked: player yields left instead");Check(player.transform.position.x<1.2f,"Player never crosses the side wall");Object.DestroyImmediate(right);
  cart.DebugShowAtHome();var left=Wall(-1.55f);Place(player,new Vector3(-.1f,0,1.2f));free=false;
  for(int i=0;i<25&&!free;i++){free=Advance(cart);yield return null;}
  Check(free&&player.transform.position.x>1.2f,"Left side blocked: player yields right instead");Object.DestroyImmediate(left);
  cart.DebugShowAtHome();left=Wall(-1.55f);right=Wall(1.55f);Place(player,new Vector3(0,0,1.2f));var original=player.transform.position;
  for(int i=0;i<34;i++){Advance(cart);yield return null;}
  Check((player.transform.position-original).sqrMagnitude<.001f,"Both sides blocked: player stays safely between walls");Check(cart.State==MarketCartSchedule.TravelState.Fading,"Boxed-in player triggers graceful cart fade instead of permanent blockage");
  for(int i=0;i<12;i++)Call(cart,"MoveAlongRoute",.1f);Check(!cart.IsPresented,"Blocked-side fallback completes disappearance");Object.DestroyImmediate(left);Object.DestroyImmediate(right);
  cart.DebugShowAtHome();Place(player,new Vector3(0,0,1.2f));original=player.transform.position;
  for(int i=0;i<4;i++)yield return null;
  Check((player.transform.position-original).sqrMagnitude<.001f,"Parked stand does not push nearby player");
  Place(player,new Vector3(4,0,4));var npc=new GameObject("Animal crossing");npc.transform.position=new Vector3(0,0,1.2f);var body=npc.AddComponent<CapsuleCollider>();body.center=Vector3.up*.75f;body.height=1.5f;body.radius=.24f;npc.AddComponent<NPCCharacterMotion>();Physics.SyncTransforms();
  Check(!Advance(cart),"Animal/NPC still blocks solid route");for(int i=0;i<35;i++)Advance(cart);Check(cart.State==MarketCartSchedule.TravelState.Open,"Animal is not forcibly shoved or faded by player-only logic");Object.DestroyImmediate(npc);
  player.enabled=true;player.SetMobileMovement(new Vector2(.5f,0));original=player.transform.position;float until=Time.time+.3f;while(Time.time<until)yield return null;
  Check(Vector3.Distance(original,player.transform.position)>.05f,"Movement input works again after sidestep grace period");player.SetMobileMovement(Vector2.zero);player.enabled=false;
  cart.DebugUseSchedule();Check(!cart.IsDebugTesting,"Testing override clears normally");
 }
}
#endif
