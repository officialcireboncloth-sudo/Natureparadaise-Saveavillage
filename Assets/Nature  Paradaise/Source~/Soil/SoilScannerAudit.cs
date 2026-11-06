#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class SoilScannerAudit
{
    const string Key="NatureParadise.ScannerAudit", Output="ArtSource/SoilScannerAudit";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static IEnumerator steps; static StringBuilder report; static int checks; static double began;
    static SoilScannerAudit(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;report.AppendLine("PASS "+message);}
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/CleanClone/"))throw new Exception("Isolated fixture only");
        Directory.CreateDirectory(Output);if(File.Exists(Output+"/Error.txt"))File.Delete(Output+"/Error.txt");
        if(File.Exists(Output+"/Audit.txt"))File.Delete(Output+"/Audit.txt");
        ShaderUtil.allowAsyncCompilation=false;
        var prefab=SoilScannerSetup.BuildDummy();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Object.Instantiate(prefab).name="Scanner fixture";
        for(int i=0;i<4;i++)
        {
            var field=new GameObject("Field "+(i+1),typeof(BoxCollider)).AddComponent<FieldArea>();
            Set(field,"fieldNumber",i+1);Set(field,"columns",i%2==0?8:6);Set(field,"rows",i<2?6:8);
            field.transform.position=Quaternion.Euler(0,37,0)*new Vector3(i%2==0?-7:7,0,i<2?10:-2);
            field.transform.rotation=Quaternion.Euler(0,37,0);
            var soil=GameObject.CreatePrimitive(PrimitiveType.Cube);soil.name="Actual soil surface";
            soil.transform.SetParent(field.transform,false);soil.transform.localPosition=new(0,-.08f,0);
            soil.transform.localScale=new(field.Columns*field.CellSize,.1f,field.Rows*field.CellSize);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new(.42f,.27f,.13f);
            soil.GetComponent<Renderer>().sharedMaterial=material;
        }
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Real scene ground";ground.transform.localScale=new(8,1,8);ground.transform.position=Vector3.down*.15f;
        var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=new(.30f,.46f,.19f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
        var liveMarker=GameObject.CreatePrimitive(PrimitiveType.Cube);liveMarker.name="Live view marker";liveMarker.transform.position=new(-7,1,10);liveMarker.transform.localScale=new(1,2,1);
        var markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));markerMaterial.color=Color.magenta;liveMarker.GetComponent<Renderer>().sharedMaterial=markerMaterial;
        var actor=new GameObject("Player fixture",typeof(CharacterController),typeof(Inventory),typeof(PlayerController));actor.transform.position=new(0,0,-1.2f);
        new GameObject("Clock fixture",typeof(TimeManager));
        var cam=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();cam.tag="MainCamera";
        cam.transform.position=new(0,2,-5);cam.transform.LookAt(Vector3.up*.75f);cam.backgroundColor=new(.18f,.24f,.22f);cam.clearFlags=CameraClearFlags.SolidColor;
        var light=new GameObject("Sun",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-25,0);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Editor/SoilScannerAudit.unity");
        SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
        report=new StringBuilder("SCANNER MACHINE + UI — NATIVE UNITY PLAYMODE\n");checks=0;began=EditorApplication.timeSinceStartup;steps=Test();
    }
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup-began>150)throw new Exception("Audit timeout");
            if(!steps.MoveNext())
            {steps=null;SessionState.SetBool(Key,false);File.WriteAllText(Output+"/Audit.txt",report+"SCANNER_AUDIT_PASS checks="+checks);EditorApplication.Exit(0);}
        }
        catch(Exception error)
        {steps=null;SessionState.SetBool(Key,false);File.WriteAllText(Output+"/Error.txt",report+"\n"+error);Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static IEnumerator Test()
    {
        yield return null;yield return null;
        var player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;
        var inv=player.GetComponent<Inventory>();var clock=TimeManager.Instance;var camera=Camera.main;
        var machine=Object.FindFirstObjectByType<SoilScannerMachine>();
        Check(Object.FindFirstObjectByType<SaveManager>()==null,"Fixture never creates a player save writer");
        Check(machine.GetComponentsInChildren<Collider>().Length==1&&!machine.GetComponent<BoxCollider>().isTrigger,"Dummy has one compact physical collider");
        Check(machine.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y)<1.6f,"Dummy stays below player-height scale");
        player.transform.position=new(0,0,-8);Check(!machine.Open(inv),"Opening from outside range rejected");
        player.transform.position=new(0,0,-1.1f);Physics.SyncTransforms();
        yield return null;yield return null;
        Check(machine.CanInteract(inv),"Nearby player can use authored station");
        Check(WorldInteractionPrompt.IsDisplayedFor(machine),"Machine owns one shared bottom prompt");
        var rules=Resources.Load<FieldSoilRulesSO>("Profiles/Farming/Field Soil Rules");
        FieldArea.TryGetByNumber(1,out var first);FieldArea.TryGetByNumber(2,out var second);FieldArea.TryGetByNumber(3,out var third);
        first.DebugSetFieldFertility(25);second.DebugSetFieldFertility(10);third.DebugSetFieldFertility(60);
        Set(third,"visualDistance",1f);Set(third,"lodTarget",player.transform);Set(third,"nextLodCheckTime",0f);third.SendMessage("Update");
        Check(!third.transform.Find("FieldVisuals_Runtime").gameObject.activeSelf,"Distant crops normally follow player-distance LOD");
        Time.timeScale=.75f;object otherPause=new object();clock.AcquirePause(otherPause);
        Check(machine.Open(inv)&&machine.IsOpen,"Opening station creates exactly one UI");
        var ui=SoilScannerUI.Instance;
        var drone=ui.GetComponent<SoilScannerDroneView>();
        Check(third.transform.Find("FieldVisuals_Runtime").gameObject.activeSelf,"Scanner reveals distant field crops for the live view");
        Check(drone!=null&&drone.Texture!=null&&drone.Texture.IsCreated(),"Scanner displays an allocated live scene texture");
        Check(Mathf.Abs(drone.ViewCamera.transform.eulerAngles.x-SoilScannerDroneView.ViewPitch)<.01f,"Aerial view uses the authored pitch without a sideways roll");
        Check(Vector3.Dot(Vector3.ProjectOnPlane(drone.ViewCamera.transform.up,Vector3.up).normalized,first.transform.forward)>.9999f,"Drone heading follows Field 1 so rotated farm is straight on screen");
        Check(!drone.ViewCamera.enabled&&drone.Texture.width<=SoilScannerDroneView.MaximumTextureSize&&drone.Texture.height<=SoilScannerDroneView.MaximumTextureSize,"Drone is demand-rendered with capped texture resolution");
        Check(Resources.Load<Shader>("UI/SoilScannerGlass")?.isSupported==true,"Scanner glass shader is included and supported");
        var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;
        CaptureLive(drone,liveMarker:GameObject.Find("Live view marker"));
        Check(camera.transform.position==cameraPosition&&camera.transform.rotation==cameraRotation,"Scanner leaves gameplay camera position and rotation untouched");
        Check(!machine.Open(inv)&&SoilScannerUI.Instance==ui,"Repeated open does not stack modals");
        Check(player.IsMovementLocked&&clock.IsPaused&&Time.timeScale==0&&WorldInteractionPrompt.IsSuppressed,"Modal pauses world, blocks movement and hides prompts");
        Check(GameplayPauseMenu.BlocksGameplayInput,"Inventory and other gameplay shortcuts respect the scanner modal");
        Check(ui.GetComponentsInChildren<Button>(true).Count(b=>b.name.StartsWith("Field "))==4,"Four real Field IDs appear on map");
        Check(ui.SelectedFieldNumber==1&&ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="25 / 80"),"First selected field reads live soil");
        Check(ui.SelectField(2)&&ui.SelectedFieldNumber==2,"Field card selection routes by numeric ID");
        Check(!ui.SelectField(99)&&ui.SelectedFieldNumber==2,"Missing ID cannot replace selection");
        Check(ui.SelectField(1),"Return to Field 1");
        foreach(var item in Resources.LoadAll<ItemSO>("Items/Farming/Terra")){int count=inv.GetCount(item);if(count>0)inv.Remove(item,count);}
        ui.SetBagOpen(true);
        Check(ui.IsBagOpen&&ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Tidak ada Terra")),"Empty inventory shows clear picker state");
        ui.SetBagOpen(false);Check(!ui.TryUseSelected(),"No chosen Terra cannot spend inventory or lock field");
        var miracle=Resources.Load<ItemSO>("Items/Farming/Terra/Terra Miracle");var ultra=Resources.Load<ItemSO>("Items/Farming/Terra/Terra Ultra");
        inv.Add(miracle,2);inv.Add(ultra,1);ui.SetBagOpen(true);
        Check(ui.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Terra "))==2,"Picker only lists Terra actually owned");
        Check(ui.SelectTerra(miracle)&&ui.SelectedTerra==miracle,"Inventory item selection retained");
        ui.SetBagOpen(false);
        Capture(camera,ui,"Scanner_Desktop",1600,900);
        Check(ui.TryUseSelected()&&first.IsSoilLocked&&inv.GetCount(miracle)==1,"Use button consumes exactly one and starts selected field");
        Check(!ui.TryUseSelected()&&inv.GetCount(miracle)==1&&!second.IsSoilLocked,"Repeated use cannot spend twice or affect another field");
        Check(ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("3 hari lagi")),"Locked state shows exact remaining days");
        Capture(camera,ui,"Scanner_Locked",1600,900);
        Capture(camera,ui,"Scanner_Mobile",900,1600);
        ui.Close();yield return null;
        Check(!third.transform.Find("FieldVisuals_Runtime").gameObject.activeSelf,"Closing scanner restores distant crop LOD");
        Check(drone==null&&Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).All(c=>c.name!="Soil Scanner Drone Camera"),"Closing releases the temporary drone camera");
        Check(SoilScannerUI.Instance==null&&!player.IsMovementLocked&&Mathf.Approximately(Time.timeScale,.75f)&&clock.IsPaused&&!WorldInteractionPrompt.IsSuppressed,
            "Closing restores previous time scale and movement without releasing another clock owner");
        clock.ReleasePause(otherPause);Time.timeScale=1;
        clock.AdvanceToNextDay();clock.AdvanceToNextDay();clock.AdvanceToNextDay();
        Check(!first.IsSoilLocked&&first.SoilSnapshot.Fertility==80,"Three normal calendar boundaries finish Miracle");
        player.transform.position=new(0,0,-1.1f);Check(machine.Open(inv),"Machine can reopen after closing");ui=SoilScannerUI.Instance;
        Check(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Use Terra").interactable==false,"Full field disables Use button");
        ui.SelectField(2);ui.SetBagOpen(true);ui.SelectTerra(ultra);inv.Remove(ultra,1);ui.SetBagOpen(false);
        Check(ui.SelectedTerra==null&&!ui.TryUseSelected()&&!second.IsSoilLocked,"Removed inventory item is revalidated before use");
        ui.SetBagOpen(true);Capture(camera,ui,"Scanner_Picker",1600,900);ui.SetBagOpen(false);
        var duplicate=new GameObject("Duplicate Field",typeof(BoxCollider)).AddComponent<FieldArea>();Set(duplicate,"fieldNumber",2);
        ui.Refresh();Check(!ui.SelectField(2)&&!ui.TryUseSelected(),"Duplicate IDs disable ambiguous UI operations");
        Object.Destroy(duplicate.gameObject);yield return null;
        ui.Refresh();Check(ui.SelectField(2),"Fixed identity restores selection");
        machine.gameObject.SetActive(false);yield return null;
        Check(SoilScannerUI.Instance==null&&!player.IsMovementLocked&&Mathf.Approximately(Time.timeScale,1),"Disabling station safely releases modal ownership");
        Check(Object.FindFirstObjectByType<SaveManager>()==null,"Audit finishes without touching real saves");
    }
    static void CaptureLive(SoilScannerDroneView drone,GameObject liveMarker)
    {
        drone.RenderNow();var first=Read(drone.Texture);
        liveMarker.transform.position+=Vector3.right*3;drone.RenderNow();var moved=Read(drone.Texture);
        int changed=0;var a=first.GetPixels32();var b=moved.GetPixels32();for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i]))changed++;
        Check(changed>20,"Live farm image updates when a real world object moves while the game is paused");
        File.WriteAllBytes(Output+"/Scanner_LiveWorld.png",moved.EncodeToPNG());Object.Destroy(first);Object.Destroy(moved);
    }
    static Texture2D Read(RenderTexture target)
    {
        var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=old;return image;
    }
    static void Capture(Camera cam,SoilScannerUI ui,string name,int width,int height)
    {
        foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas!=ui.GetComponent<Canvas>())canvas.enabled=false;
        var canvasUI=ui.GetComponent<Canvas>();canvasUI.renderMode=RenderMode.ScreenSpaceCamera;canvasUI.worldCamera=cam;canvasUI.planeDistance=.5f;
        var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;
        Canvas.ForceUpdateCanvases();
        typeof(CanvasScaler).GetMethod("Handle",F).Invoke(ui.GetComponent<CanvasScaler>(),null);Canvas.ForceUpdateCanvases();
        ui.GetType().GetMethod("Layout",F).Invoke(ui,null);ui.Refresh();Canvas.ForceUpdateCanvases();
        ui.GetComponent<SoilScannerDroneView>().RenderNow();
        var live=ui.GetComponent<SoilScannerDroneView>();
        var fullImage=ui.GetComponentsInChildren<RawImage>().Single(r=>r.name=="Drone Image").rectTransform;
        Check(Mathf.Abs(fullImage.rect.width*canvasUI.scaleFactor-width)<3&&
            Mathf.Abs(fullImage.rect.height*canvasUI.scaleFactor-height)<3,name+" live farm fills the whole screen");
        var clearArea=ui.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Unobstructed Farm Area");
        var clearCorners=new Vector3[4];clearArea.GetWorldCorners(clearCorners);
        var lower=cam.WorldToViewportPoint(clearCorners[0]);var upper=cam.WorldToViewportPoint(clearCorners[2]);
        foreach(var field in FieldArea.ActiveAreas.Where(f=>f.UsesFieldSoil))
            for(int i=0;i<4;i++)
            {
                var corner=field.transform.TransformPoint(new Vector3((i%2==0?-.5f:.5f)*field.Columns*field.CellSize,0,
                    (i<2?-.5f:.5f)*field.Rows*field.CellSize));
                var p=live.Project(FieldGroundSurface.GroundPosition(corner));
                Check(p.x>=lower.x-.005f&&p.x<=upper.x+.005f&&p.y>=lower.y-.005f&&p.y<=upper.y+.005f,
                    name+" farm corner stays clear of the detail panel");
            }
        Check(ui.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Field Status").All(t=>t.text.StartsWith("Petak ")),
            name+" live field badges use the reference Petak labels");
        var pixelsImage=Read(live.Texture);var colors=pixelsImage.GetPixels32();float markerX=0,markerY=0;int markerPixels=0;
        for(int pixel=0;pixel<colors.Length;pixel++)
            if(colors[pixel].r>200&&colors[pixel].g<40&&colors[pixel].b>200){markerX+=pixel%pixelsImage.width;markerY+=pixel/pixelsImage.width;markerPixels++;}
        var marker=GameObject.Find("Live view marker");var projected=live.Project(marker.transform.position);
        Check(markerPixels>0&&Mathf.Abs(markerX/markerPixels-projected.x*pixelsImage.width)<4&&
            Mathf.Abs(markerY/markerPixels-projected.y*pixelsImage.height)<4,name+" overlay projection matches a rendered world marker");
        Object.Destroy(pixelsImage);
        Check(ui.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Field ")).All(b=>Mathf.Abs(Mathf.DeltaAngle(0,b.transform.eulerAngles.z))<.02f),name+" field outlines stay straight for the 37 degree farm");
        cam.Render();
        var card=ui.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name.StartsWith("Field "));
        if(card!=null)
        {
            var corners=new Vector3[4];card.transform.Find("Field Label").GetComponent<RectTransform>().GetWorldCorners(corners);
            float pixels=Mathf.Abs(cam.WorldToViewportPoint(corners[2]).x-cam.WorldToViewportPoint(corners[0]).x)*width;
            Check(pixels>=64,name+" field label remains a readable touch target");
        }
        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
        // All interactive controls stay inside the viewport on desktop and portrait.
        foreach(var b in ui.GetComponentsInChildren<Button>())
        {
            var corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach(var corner in corners){var p=cam.WorldToViewportPoint(corner);Check(p.x>=-.01f&&p.x<=1.01f&&p.y>=-.01f&&p.y<=1.01f,name+" control in viewport: "+b.name);}
        }
        RenderTexture.active=old;cam.targetTexture=null;Object.Destroy(image);Object.Destroy(rt);
        canvasUI.renderMode=RenderMode.ScreenSpaceOverlay;canvasUI.worldCamera=null;
    }
}
#endif
