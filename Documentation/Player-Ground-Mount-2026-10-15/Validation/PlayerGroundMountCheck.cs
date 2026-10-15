#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public sealed class PlayerGroundMountCheck:MonoBehaviour {
 const string Root="Assets/Nature  Paradaise/";
 const string Work="C:/Users/huxel/.codex/visualizations/2026/10/02/01a0fc03-fab9-79a2-914d-621e14dea114/WorldProportionWork/";
 bool started;static bool verify;
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static object Call(object o,string n,params object[] v)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,v);
 static void Check(bool b,string text){if(!b)throw new Exception(text);Debug.Log("GROUND MOUNT PASS "+text);}
 public static void RunAudit()=>Run(false);
 public static void RunCheck()=>Run(true);
 static void Run(bool check){verify=check;SessionState.SetBool("GroundMountVerify",check);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Check").AddComponent<PlayerGroundMountCheck>();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/GroundMountCheck.unity");EditorApplication.EnterPlaymode();}
 void Start(){if(started)return;started=true;verify=SessionState.GetBool("GroundMountVerify",false);StartCoroutine(Guard());}
 IEnumerator Guard(){var test=Test();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return test.Current;}Debug.Log("GROUND MOUNT CHECK COMPLETED");EditorApplication.Exit(0);}
 static Transform Hip(Animator a)=>a.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:Hips");
 static float Bottom(GameObject go)=>go.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).Min(r=>r.bounds.min.y);
 IEnumerator Test(){
  var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(100,1,100);
  var go=new GameObject("Player");go.SetActive(false);go.AddComponent<CharacterController>();go.AddComponent<Inventory>();go.AddComponent<PlayerStatusSystem>();var visual=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Player/PlayerVisual.prefab"),go.transform);var player=go.AddComponent<PlayerController>();go.transform.position=new Vector3(0,.1f,0);go.SetActive(true);
  var a=player.CharacterAnimator;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
  yield return new WaitForSeconds(.65f);var hip=Hip(a);float bottom=Bottom(go);Debug.Log("GROUND AUDIT idle root="+go.transform.position+" min="+bottom+" hip="+hip.position+" model="+a.transform.localPosition+" Generic="+!a.isHuman);
  foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var m=new Mesh();r.BakeMesh(m);Debug.Log("GROUND MESH "+r.name+" enabled="+r.enabled+" vertices="+m.vertexCount+" bounds="+r.bounds+" baked="+m.bounds+" scale="+r.transform.lossyScale+" position="+r.transform.position);Destroy(m);}
  foreach(var b in a.GetComponentsInChildren<Transform>())if(b.name.Contains("Foot")||b.name.Contains("Toe"))Debug.Log("GROUND BONE "+b.name+" position="+b.position);
  if(verify)Check(Mathf.Abs(bottom)<.06f,"Standing sole close to floor: "+bottom);
  player.SetMobileMovement(Vector2.up);yield return new WaitForSeconds(.25f);bottom=Bottom(go);Debug.Log("GROUND AUDIT running sole="+bottom);player.SetMobileMovement(Vector2.zero);yield return new WaitForSeconds(.3f);
  var horseGO=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Animals/Companions/Horse Companion.prefab"));horseGO.transform.position=go.transform.position+Vector3.right*1.2f;var horse=horseGO.GetComponent<PersonalAnimal>();Set(horse,"persistBetweenScenes",false);foreach(var ha in horseGO.GetComponentsInChildren<Animator>())ha.cullingMode=AnimatorCullingMode.AlwaysAnimate;yield return null;
  Check(horse.TryMount(player),"Mount started");yield return new WaitForSeconds(1.6f);
  Vector3 seat=(Vector3)Call(horse,"GetSeatWorldPosition");Debug.Log("GROUND AUDIT mounted root="+go.transform.position+" hip="+hip.position+" seat="+seat+" gap="+Vector3.Distance(hip.position,seat)+" scale="+go.transform.lossyScale+" feet="+Bottom(go));
  if(verify){Check(Vector3.Distance(hip.position,seat)<.035f,"Generic pelvis follows saddle");Check(Vector3.Distance(go.transform.lossyScale,Vector3.one)<.01f,"Rider world scale preserved");}
  horse.Dismount();yield return new WaitForSeconds(1.9f);bottom=Bottom(go);Debug.Log("GROUND AUDIT dismount root="+go.transform.position+" sole="+bottom+" scale="+go.transform.lossyScale);
  if(verify){Check(Mathf.Abs(bottom)<.06f,"Dismounted feet return to ground: "+bottom);Check(go.GetComponent<CharacterController>().enabled,"Character controller restored");}
 }
}
#endif
