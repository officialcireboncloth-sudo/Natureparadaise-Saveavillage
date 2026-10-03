using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(HouseInteriorController))]
public class HouseInteriorPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var house=(HouseInteriorController)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if(EditorGUI.EndChangeCheck() && !Application.isPlaying) house.PreviewLayout(house.PreviewLevel);
        EditorGUILayout.HelpBox("Preview memilih layout editor. Saat main, layout mengikuti level rumah yang selesai; testHouseLevel dapat menaikkannya jika bypass aktif.",MessageType.Info);
        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            for(int row=0;row<2;row++)
            {
                EditorGUILayout.BeginHorizontal();
                for(int level=row*2+1;level<=row*2+2;level++)if(GUILayout.Button("Preview Lv."+level))
                {
                    Undo.RecordObject(house,"Preview house level");
                    house.PreviewLayout(level);
                    EditorUtility.SetDirty(house);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(house.gameObject.scene);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            }
            if(GUILayout.Button("Fokus Preview Interior"))
            {
                SceneVisibilityManager.instance.Isolate(house.gameObject, true);
                var renderers=house.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0 && SceneView.lastActiveSceneView!=null)
                {
                    var bounds=renderers[0].bounds;
                    foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var view=house.GetComponent<HouseInteriorView>()?.ActiveView;
                    if(view!=null)
                        SceneView.lastActiveSceneView.LookAt(bounds.center,view.transform.rotation,bounds.extents.magnitude*.65f,false,true);
                    else SceneView.lastActiveSceneView.Frame(bounds,true);
                }
            }
        }
    }
}
