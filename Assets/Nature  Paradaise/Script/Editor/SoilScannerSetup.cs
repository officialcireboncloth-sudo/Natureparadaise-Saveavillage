#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SoilScannerSetup
{
    public const string Root = "Assets/Nature  Paradaise";
    public const string PrefabPath = Root + "/Prefabs/Soil/SoilScanner_Dummy.prefab";

    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static Material Material(string name,Color color)
    {
        string path=Root+"/Material/SoilScanner/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material,path);
        }
        material.color=color;material.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(material);return material;
    }

    [MenuItem("Nature Paradise/Soil/Build Dummy Scanner Prefab")]
    public static GameObject BuildDummy()
    {
        if (SoilScannerArtSetup.IsAvailable) return SoilScannerArtSetup.Apply();
        Folder(Root+"/Prefabs/Soil");Folder(Root+"/Material/SoilScanner");
        var teal=Material("Scanner Teal",new(.16f,.35f,.37f));
        var cream=Material("Scanner Cream",new(.76f,.71f,.59f));
        var dark=Material("Scanner Screen",new(.045f,.11f,.13f));
        var green=Material("Scanner Signal",new(.48f,.72f,.37f));
        var root=new GameObject("SoilScanner_Dummy");
        try
        {
            var machine=root.AddComponent<SoilScannerMachine>();
            var collider=root.GetComponent<BoxCollider>();collider.center=new(0,.68f,0);collider.size=new(.76f,1.36f,.58f);
            var visual=new GameObject("Visual_Dummy_ReplaceLater").transform;visual.SetParent(root.transform,false);
            void Part(string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube,Quaternion? rotation=null)
            {
                var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(visual,false);
                part.transform.localPosition=position;part.transform.localScale=scale;part.transform.localRotation=rotation??Quaternion.identity;
                Object.DestroyImmediate(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;
            }
            Part("Base",new(0,.07f,0),new(.80f,.14f,.62f),teal);
            Part("Cabinet",new(0,.61f,0),new(.65f,1.05f,.49f),cream);
            Part("Monitor",new(0,1.12f,-.035f),new(.76f,.54f,.55f),teal);
            Part("Display",new(0,1.12f,-.32f),new(.60f,.38f,.025f),dark);
            for(int i=0;i<3;i++)Part("Reading "+i,new(-.06f,1.22f-i*.10f,-.341f),new(.27f+i*.055f,.043f,.012f),green);
            for(int i=0;i<2;i++)Part("Control "+i,new(-.16f+i*.32f,.65f,-.282f),new(.15f,.045f,.15f),dark,PrimitiveType.Cylinder,Quaternion.Euler(90,0,0));
            Part("Probe",new(.47f,.61f,0),new(.08f,.34f,.08f),cream,PrimitiveType.Cylinder);
            Part("Probe Grip",new(.47f,.99f,0),new(.16f,.13f,.14f),teal);
            var anchor=new GameObject("InteractionAnchor").transform;anchor.SetParent(root.transform,false);anchor.localPosition=new(0,0,-.48f);
            var settings=new SerializedObject(machine);settings.FindProperty("interactionAnchor").objectReferenceValue=anchor;settings.ApplyModifiedPropertiesWithoutUndo();
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();return prefab;
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static SoilScannerMachine Install(Scene scene,GameObject prefab)
    {
        foreach(var root in scene.GetRootGameObjects())
        {
            var existing=root.GetComponentInChildren<SoilScannerMachine>(true);
            if(existing!=null)return existing;
        }
        FieldArea main=null;
        foreach(var root in scene.GetRootGameObjects())
            foreach(var field in root.GetComponentsInChildren<FieldArea>(true))
                if(field.FieldNumber==1){main=field;break;}
        if(main==null)throw new System.InvalidOperationException("Field 1 belum tersedia: "+scene.path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        instance.name="Pemindai Tanah - Dummy";
        var point=main.transform.TransformPoint(new Vector3(-main.Columns*main.CellSize*.5f-1.25f,0,0));
        instance.transform.position=FieldGroundSurface.GroundPosition(point);
        instance.transform.rotation=Quaternion.Euler(0,main.transform.eulerAngles.y,0);
        EditorSceneManager.MarkSceneDirty(scene);return instance.GetComponent<SoilScannerMachine>();
    }

    [MenuItem("Nature Paradise/Soil/Install Scanner In Current Scene")]
    public static void InstallCurrent()
    {var machine=Install(SceneManager.GetActiveScene(),BuildDummy());Selection.activeGameObject=machine.gameObject;}

    // Batch installation keeps all normal scene dependencies in the real project.
    public static void InstallMapsBatch()
    {
        try
        {
            var prefab=BuildDummy();
            foreach(string name in new[]{"Map","Map_GameplayPreview"})
            {
                var scene=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/"+name+".unity",OpenSceneMode.Single);
                Install(scene,prefab);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }
        catch(System.Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
