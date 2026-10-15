#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Text.RegularExpressions;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class SoleSetupCheck {
 const float StandingSoleBoundsPadding=.031461403f;
 public static void Run(){try{
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject("Authoring root");var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx"),root.transform).transform;
  model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;model.localScale=Vector3.one;
  FitModelToPlayer(model.gameObject,root.transform,1.5f);
  Debug.Log("SOLE SETUP model position="+model.localPosition.ToString("F6")+" scale="+model.localScale);
  if(Mathf.Abs(model.localPosition.y-.15f)>.003f)throw new Exception("Setup offset differs from verified runtime placement");
  Debug.Log("SOLE SETUP VALIDATION PASSED");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void FitModelToPlayer(GameObject model,Transform root,float targetHeight)
    {
        // Export Blender terbaru juga membawa geometry control-rig. Jika semua Renderer
        // dihitung, bounds menjadi sekitar 10x tinggi badan dan player diperkecil ke 0.19.
        // Ukuran visual harus mengikuti mesh yang benar-benar di-skin ke skeleton.
        if(!WorldScaleAudit.TryBounds(model,true,out Bounds bounds)) return;
        float scale=targetHeight/Mathf.Max(.001f,bounds.size.y);
        model.transform.localScale=Vector3.one*scale;
        if(!WorldScaleAudit.TryBounds(model,true,out bounds)) return;
        Vector3 worldBottomCenter=new(bounds.center.x,
            bounds.min.y+StandingSoleBoundsPadding*(targetHeight/PlayerDimensions.Height),bounds.center.z);
        model.transform.localPosition-=root.InverseTransformPoint(worldBottomCenter);
    }

}
#endif
