#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Exercise the actual build-preview/confirm/upgrade path, separate from the player's map/save.</summary>
[InitializeOnLoad]
public static class FishPondPlacementValidation
{
    const string Key="PondA.PlacementRegression";
    static double started;static bool done;
    static FishPondPlacementValidation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
    [MenuItem("Nature Paradise/Fish Pond/Validate Placement Regression",false,123)]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        SessionState.SetString(Key+".Scene",SceneManager.GetActiveScene().path);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var player=new GameObject("PlacementAuditPlayer");player.AddComponent<Inventory>();
        var go=new GameObject("PlacementAuditField");var field=go.AddComponent<FieldArea>();
        var so=new SerializedObject(field);so.FindProperty("columns").intValue=15;so.FindProperty("rows").intValue=15;so.FindProperty("cellSize").floatValue=2;
        so.FindProperty("blendGroundSurface").boolValue=false;so.FindProperty("areaCollider").objectReferenceValue=go.GetComponent<BoxCollider>();so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene,FishPondApprovedExteriorSetup.Root+"/Map/Scenes/Testing/FishPondPlacementRegression.unity");
        SessionState.SetBool(Key,true);done=false;EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s)
    {
        if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;done=false;}
        if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
        {
            SessionState.SetBool(Key,false);string prior=SessionState.GetString(Key+".Scene","");
            if(!string.IsNullOrEmpty(prior))EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(prior);
        }
    }
    static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||done||EditorApplication.timeSinceStartup-started<1.5)return;
        done=true;var report=new StringBuilder("FISH POND PLACEMENT REGRESSION\n");
        void Check(bool okay,string label){if(!okay)throw new InvalidOperationException(label);report.AppendLine("PASS "+label);}
        try
        {
            var field=Object.FindFirstObjectByType<FieldArea>();var player=Object.FindFirstObjectByType<Inventory>();
            var definition=Resources.Load<BuildingDefinitionSO>("Buildings/Fish Pond Building");var catalog=Resources.Load<BuildingCatalogSO>("Buildings/Field Building Catalog");
            Check(field.Columns==15 && field.Rows==15 && field.CellSize==2,"Actual map field size: 15x15 cells, 2m");
            Check(SaveManager.Instance==null,"Isolated test cannot write a player save");
            Vector3 centre=field.GridToWorld(7,7);player.transform.SetPositionAndRotation(centre-Vector3.forward*4,Quaternion.identity);
            var site=PropertySite.GetOrCreateGlobalBuildSite(catalog,player.transform.position);
            int records=FishPondService.Capture().Count,makers=FeedMaker.CaptureAll().Count;
            Check(site.BeginBuildSelection(catalog.IndexOf(definition),1,true),"Player build menu starts actual Lv1 preview");
            var preview=(GameObject)Get(site,"previewObject");
            Check(preview.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Preview contains no gameplay scripts");
            Check(Object.FindObjectsByType<FishPond>(FindObjectsSortMode.None).Length==0,"No interactable pond exists during preview");
            Check(FishPondService.Capture().Count==records && FeedMaker.CaptureAll().Count==makers,"Preview Awake/OnEnable creates no pond/maker persistent records");
            Check(preview.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Preview cannot block its own placement");
            Check(!(bool)Get(site,"previewLocationValid") && ((string)Get(site,"previewLocationReason")).Contains("resource"),"Wild grass blocks installation with a readable reason");
            foreach(var grass in field.GetComponentsInChildren<WorldGatherable>())grass.gameObject.SetActive(false);
            Physics.SyncTransforms();
            Set(site,"previewLocationValid",(bool)Call(site,"ValidatePreviewLocation",definition));
            Check((bool)Get(site,"previewLocationValid"),"Lv1 installs on a clear centre of the actual field grid");
            object[] args={definition,field,0,0};Call(site,"FieldFootprint",args);
            Check((int)args[2]<13 && (int)args[3]<13,"Lv1 footprint smaller than old maximum-Lv3 reservation");
            Quaternion rotation=(Quaternion)Get(site,"previewPlacementRotation");Vector2 size=FishPondExterior.PlacementSizeMeters(1);
            Check((int)args[2]*2+.001f>=Mathf.Abs((rotation*Vector3.right).x)*size.x+Mathf.Abs((rotation*Vector3.forward).x)*size.y,"Rotated footprint covers full physical width");
            site.CancelPreview();player.transform.position=field.GridToWorld(7,0)-Vector3.forward*4;
            site.BeginBuildSelection(catalog.IndexOf(definition),1,true);
            Check(!(bool)Get(site,"previewLocationValid") && ((string)Get(site,"previewLocationReason")).Contains("tapak"),"Edge location rejected with explicit whole-footprint explanation");
            Check(!site.ConfirmPreview(true) && site.IsEmpty,"Invalid C confirm cannot create a pond");site.CancelPreview();
            var tiles=(FieldTileData[])Get(field,"tiles");for(int i=0;i<tiles.Length;i++)tiles[i].state=TileState.Hoed;
            tiles[0].surfaceWetness=.6f;
            Check(field.TryPlaceBuilding(0,0,1,1,"audit.rollback",false,out Action undoReservation),"Reservation can be rolled back when payment fails");
            undoReservation();
            Check(tiles[0].state==TileState.Hoed && tiles[0].surfaceWetness==.6f && tiles[0].buildingId==null,"Rollback preserves cultivated and wet soil");
            player.transform.position=centre-Vector3.forward*4;site.BeginBuildSelection(catalog.IndexOf(definition),1,true);
            Check((bool)Get(site,"previewLocationValid"),"Bare hoed soil can host a building");
            Check(site.ConfirmPreview(true),"Actual C/Confirm path builds Lv1");
            Check(site.CurrentLevel==1 && site.GetComponentsInChildren<FishPond>().Length==1,"Exactly one usable pond appears after confirmed completion");
            int cells=tiles.Count(t=>t.state==TileState.Building && t.buildingId==site.SiteId);
            Check(cells>0,"Placed pond reserves field cells");
            Check(site.BeginUpgradePreview(),"Actual upgrade preview starts");
            Check((bool)Get(site,"previewLocationValid"),"Upgrade accepts its own reserved cells");
            var targetArgs=new object[]{definition,field,0,0};Call(site,"FieldFootprint",targetArgs);
            int sx=7-(int)targetArgs[2]/2,sz=7-(int)targetArgs[3]/2;
            int cropIndex=-1;
            for(int z=sz;z<sz+(int)targetArgs[3];z++)for(int x=sx;x<sx+(int)targetArgs[2];x++)
            {int i=z*field.Columns+x;if(tiles[i].state!=TileState.Building)cropIndex=i;}
            Check(cropIndex>=0,"Lv2 requests additional cells");tiles[cropIndex].state=TileState.Planted;
            bool valid=(bool)Call(site,"ValidatePreviewLocation",definition);Set(site,"previewLocationValid",valid);
            Check(!valid && !site.ConfirmPreview(true),"Upgrade blocked by crop in expansion ring");
            Check(tiles[cropIndex].state==TileState.Planted && site.CurrentLevel==1,"Blocked upgrade preserves crop and existing pond");
            tiles[cropIndex].state=TileState.Hoed;valid=(bool)Call(site,"ValidatePreviewLocation",definition);Set(site,"previewLocationValid",valid);
            Check(valid && site.ConfirmPreview(true),"Clear expansion ring permits Lv2 upgrade");
            Check(site.CurrentLevel==2 && tiles.Count(t=>t.state==TileState.Building && t.buildingId==site.SiteId)>cells,"Lv2 footprint actually expands");
            Check(site.BeginUpgradePreview() && (bool)Get(site,"previewLocationValid") && site.ConfirmPreview(true),"Lv3 upgrade fits and reserves its expanded area");
            Check(site.CurrentLevel==3 && site.GetComponentsInChildren<FishPond>().Length==1,"Only final completed Lv3 pond is interactable");
            report.AppendLine("PLACEMENT PASS");
        }
        catch(Exception e){report.AppendLine("PLACEMENT FAIL: "+e);Debug.LogException(e);}
        finally{Directory.CreateDirectory(FishPondApprovedExteriorSetup.Output);File.WriteAllText(FishPondApprovedExteriorSetup.Output+"/UnityPlacementAudit.txt",report.ToString());EditorApplication.isPlaying=false;}
    }
}
#endif
