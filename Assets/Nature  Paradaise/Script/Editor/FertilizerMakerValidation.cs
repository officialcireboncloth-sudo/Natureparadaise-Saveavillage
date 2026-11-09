#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

/// <summary>Native Unity checks using disposable preview-scene actors, never loading or writing a player save.</summary>
public static class FertilizerMakerValidation
{
    static int checks;
    static StringBuilder report;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;report.AppendLine("PASS "+label);}
    static string InventoryJson(Inventory bag)=>JsonUtility.ToJson(new BagSnapshot{slots=bag.slots});
    [Serializable] sealed class BagSnapshot {public List<ItemStack> slots;}
    static void Empty(Inventory bag)=>bag.RestoreContents(0,Enumerable.Repeat<ItemStack>(null,bag.CapacityAtBackpackLevel(0)).ToList());
    static void Give(Inventory bag,FertilizerRecipe recipe,int batches=1)
    {
        foreach(var ingredient in recipe.ingredients)
        {
            float size=0;
            if(ingredient.item.category==ItemCategory.Fish)
            {
                var def=FishMeasurement.FindDefinition(ingredient.item);
                size=!ingredient.requireFishSize?20:ingredient.fishSize==FishSizeTier.Small?def.minimumSizeCm:
                    ingredient.fishSize==FishSizeTier.Medium?def.PondMediumSizeCm:def.PondLargeSizeCm;
            }
            Check(bag.Add(ingredient.item,ingredient.amount*batches,0,size),"Fixture can hold ingredient "+ingredient.DisplayName);
        }
    }
    [MenuItem("Nature Paradise/Soil/Validate Fertilizer Maker")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Run validation outside Play mode");
        checks=0;report=new StringBuilder("FERTILIZER MAKER - NATIVE UNITY\n");Directory.CreateDirectory(FertilizerMakerSetup.Output);
        string errorPath=FertilizerMakerSetup.Output+"/Error.txt";if(File.Exists(errorPath))File.Delete(errorPath);
        var preview=EditorSceneManager.NewPreviewScene();var garbage=new List<Object>();
        try
        {
            var actor=new GameObject("Fertilizer validation actor");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,preview);
            var bag=actor.AddComponent<Inventory>();actor.AddComponent<CharacterController>();actor.AddComponent<PlayerController>();Empty(bag);
            var catalog=FertilizerCatalog.Load();Check(catalog.makerRecipes.Length==4,"Exactly four maker recipes");
            var theme=Resources.Load<FertilizerMakerTheme>("UI/Fertilizer Maker Theme");
            Check(theme!=null&&theme.fertilizerIcons.Length==4&&theme.fertilizerIcons.All(s=>s!=null),"All four illustrated fertilizer sprites are bound");
            Check(theme.fishIcon!=null&&theme.manureIcon!=null&&theme.shellIcon!=null&&theme.mineralIcon!=null,"Every ingredient fallback has authored artwork");
            foreach(var sprite in theme.fertilizerIcons.Concat(new[]{theme.fishIcon,theme.manureIcon,theme.shellIcon,theme.mineralIcon}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Check(sprite.rect.size==new Vector2(128,128)&&importer.alphaIsTransparency&&!importer.mipmapEnabled,"UI sprite is 128px, transparent and has no mipmaps: "+sprite.name);
            }
            int[] counts={3,4,5,6};
            for(int i=0;i<4;i++)
            {
                var recipe=catalog.makerRecipes[i];Check(recipe.ingredients.Length==counts[i],"Recipe "+i+" ingredient row count");
                Empty(bag);string before=InventoryJson(bag);
                Check(!FertilizerMakerService.TryMake(bag,recipe,1,out _),"Missing materials cannot craft");
                Check(InventoryJson(bag)==before,"Missing materials leave inventory untouched");
                Give(bag,recipe,2);int events=0;Action changed=()=>events++;bag.OnInventoryChanged+=changed;
                Check(FertilizerMakerService.TryMake(bag,recipe,2,out _),"Craft two batches of "+recipe.output.itemName);
                bag.OnInventoryChanged-=changed;Check(events==1,"Craft commits exactly one inventory notification");
                Check(bag.GetCount(recipe.output)==2,"Exact 1-per-batch result");
                Check(recipe.ingredients.All(x=>FertilizerMakerService.Available(bag,x)==0),"Exact ingredients consumed");
                Check(!FertilizerMakerService.TryMake(bag,recipe,1,out _),"Repeated click without ingredients cannot duplicate output");
            }
            var basic=catalog.makerRecipes[0];Empty(bag);Give(bag,basic);
            var fish=bag.slots.First(s=>s?.item==basic.ingredients[0].item);fish.fishSizeCm=FishMeasurement.FindDefinition(fish.item).PondMediumSizeCm;
            string wrongSize=InventoryJson(bag);Check(!FertilizerMakerService.TryMake(bag,basic,1,out _),"Medium Tilapia does not satisfy small Tilapia");
            Check(wrongSize==InventoryJson(bag),"Wrong fish size never consumes manure or shell");
            Empty(bag);Give(bag,basic);var manure=bag.slots.First(s=>s?.item==basic.ingredients[1].item);manure.item=basic.ingredients[1].alternatives[0];
            Check(FertilizerMakerService.TryMake(bag,basic,1,out _),"Poultry manure accepted as animal manure");
            Empty(bag);Give(bag,basic,2);
            var filler=ScriptableObject.CreateInstance<ItemSO>();filler.itemId="test.filler";filler.maxStack=1;garbage.Add(filler);
            for(int i=0;i<bag.Capacity;i++)if(bag.slots[i]==null)bag.slots[i]=new ItemStack{item=filler,count=1};
            string full=InventoryJson(bag);Check(!FertilizerMakerService.TryMake(bag,basic,1,out _),"Full bag rejects output when no ingredient slot is freed");
            Check(InventoryJson(bag)==full,"Full bag failure preserves all fish metadata and stacks");
            Check(FertilizerMakerService.TryMake(bag,basic,2,out _),"Consumed ingredient slots can hold output in otherwise full bag");
            Empty(bag);Give(bag,basic,2);bool nestedSucceeded=true;
            Action nested=()=>nestedSucceeded=FertilizerMakerService.TryMake(bag,basic,1,out _);
            bag.OnInventoryChanged+=nested;
            Check(FertilizerMakerService.TryMake(bag,basic,1,out _),"Outer transaction succeeds with reentrant listener");
            bag.OnInventoryChanged-=nested;
            Check(!nestedSucceeded&&bag.GetCount(basic.output)==1,"Inventory event cannot reenter production and duplicate output");

            var perfect=new CropQualityCare();perfect.ApplyFertilizer(4,7);
            for(int d=1;d<=7;d++){perfect.RecordDay(true,80,7,d);perfect.FinishDay();Check(Math.Abs(perfect.QualityPercent-d*100d/7)<1e-10,"Unrounded perfect quality on day "+d);}
            Check(perfect.Stars==5&&perfect.QualityPercent==100,"Seven perfect days give exactly 100 and five stars");
            var missed=new CropQualityCare();
            for(int d=1;d<=7;d++){if(d!=4)missed.ApplyFertilizer(1,1);missed.RecordDay(true,80,7,d);missed.FinishDay();}
            Check(Math.Abs(missed.QualityPercent-94.28571428571429)<1e-10&&missed.Stars==4,"User example: one missed fertilizer day is 94.29 and four stars");
            var duration=new CropQualityCare();duration.ApplyFertilizer(3,3);
            for(int d=1;d<=4;d++){duration.RecordDay(true,80,7,d);duration.FinishDay();}
            Check(duration.boostedDays==3&&!duration.FertilizerActive,"Three-day fertilizer covers three samples and then expires");
            var saved=JsonUtility.FromJson<CropQualityCare>(JsonUtility.ToJson(missed));
            Check(saved.QualityPercent==missed.QualityPercent&&saved.Stars==4,"Care history round trips through save JSON without rounding");
            foreach(var crop in Resources.LoadAll<CropDataSO>(""))
                if(crop.produceItem!=null)Check(crop.mutationProduceItem!=null&&crop.mutationProduceItem.isMutatedCrop&&ItemCatalog.Resolve(crop.mutationProduceItem.Id)==crop.mutationProduceItem,"Distinct mutation item available for "+crop.name);
            // Exercise the real harvesting path, not only the standalone score calculator.
            var fieldObject=new GameObject("Mutation harvest fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fieldObject,preview);
            var field=fieldObject.AddComponent<FieldArea>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(FieldArea).GetMethod("InitializeData",flags).Invoke(field,null);
            var cells=(FieldTileData[])typeof(FieldArea).GetField("tiles",flags).GetValue(field);
            var sampleCrop=Object.Instantiate(Resources.LoadAll<CropDataSO>("").First(c=>c.produceItem!=null));garbage.Add(sampleCrop);
            sampleCrop.regrowsAfterHarvest=true;sampleCrop.regrowDays=3;
            var harvestCare=perfect.Copy();harvestCare.ApplyFertilizer(4,3);
            harvestCare.mutationRoll=0;harvestCare.doubleYieldRoll=1;
            harvestCare.harvestRollsReady=true;
            cells[0]=new FieldTileData{state=TileState.Planted,crop=sampleCrop,cropState=CropLifecycleState.HarvestReady,qualityCare=harvestCare,soilDurability=80,cropHealth=100};
            Empty(bag);
            Check(field.TryHarvest(0,0,bag,out var grade)&&grade==CropGrade.S,"Actual five-star crop harvest succeeds");
            Check(bag.GetCount(sampleCrop.mutationProduceItem)==sampleCrop.baseYield&&bag.GetCount(sampleCrop.produceItem)==0,"Successful mutation roll gives distinct produce");
            Check(cells[0].qualityCare.days==0&&cells[0].qualityCare.targetGrowthDays==3&&cells[0].qualityCare.ActiveDaysRemaining==3,"Regrow starts fresh history and retains unexpired fertilizer");

            var station=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("World/FertilizerMaker_Dummy"),preview);
            Check(station.GetComponentsInChildren<Collider>().Length==1,"Station has one collision body");
            var machine=station.GetComponent<FertilizerProcessor>();Empty(bag);bag.Add(basic.ingredients[1].item,3);
            float scale=Time.timeScale;bool cursor=Cursor.visible;CursorLockMode cursorLock=Cursor.lockState;
            Check(FertilizerMakerUI.Show(machine,bag),"Reference UI opens");var ui=FertilizerMakerUI.Instance;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ui.gameObject,preview);
            Check(Time.timeScale==0&&playerLocked(actor),"Modal pauses game and locks player");
            Check(!ui.CanProduce,"Reference missing-ingredient state disables production");
            for(int i=0;i<4;i++){ui.SelectRecipe(i);Canvas.ForceUpdateCanvases();Check(ui.SelectedIndex==i,"Recipe selection "+i);Render(ui,preview,"Recipe_"+i+".png");}
            ui.SelectRecipe(0);ui.SetQuantity(4);Check(ui.Quantity==4,"Quantity stepper updates batch");
            ui.SelectRecipe(1);Check(ui.Quantity==1,"Changing recipe resets quantity to one");
            Click(ui,"Recipe 0");Check(ui.SelectedIndex==0,"Pointer click selects the left recipe card");
            Click(ui,"Plus");Check(ui.Quantity==2,"Pointer click increments quantity");
            Click(ui,"Minus");Check(ui.Quantity==1,"Pointer click decrements quantity");
            Empty(bag);Give(bag,basic);ui.SelectRecipe(0);
            Check(ui.CanProduce,"Complete ingredients enable the craft button");
            Render(ui,preview,"Recipe_0_Ready.png");
            Click(ui,"Buat Pupuk");Check(bag.GetCount(basic.output)==1&&!ui.CanProduce,"Pointer craft click consumes ingredients and refreshes availability");
            Click(ui,"Buat Pupuk");Check(bag.GetCount(basic.output)==1,"Disabled craft button rejects a repeated pointer click");
            ui.Close();Check(FertilizerMakerUI.Instance==null&&Time.timeScale==scale&&Cursor.visible==cursor&&Cursor.lockState==cursorLock,"Closing restores cursor and time state");
            Check(!playerLocked(actor),"Closing releases player movement lock");
            Object.DestroyImmediate(station);
            var fieldSettings=new SerializedObject(field);fieldSettings.FindProperty("fieldNumber").intValue=1;fieldSettings.ApplyModifiedPropertiesWithoutUndo();
            var legacyObject=new GameObject("FertilizerProcessor_Editable");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(legacyObject,preview);
            legacyObject.transform.position=new(-32,0,14);var legacyMachine=legacyObject.AddComponent<FertilizerProcessor>();string legacyId=legacyMachine.ProcessorId;
            var installed=FertilizerMakerInstaller.Install(preview);
            Check(installed==legacyMachine&&installed.ProcessorId==legacyId,"Installer upgrades the old placeholder in place and preserves save identity");
            int stationRenderers=Resources.Load<GameObject>("World/FertilizerMaker_Dummy").GetComponentsInChildren<Renderer>(true).Length;
            Check(installed.GetComponentsInChildren<Renderer>().Length==stationRenderers&&installed.GetComponentsInChildren<Collider>().Length==1,"Upgraded placeholder has the authored visual and one physical collider");
            var siteDelta=installed.transform.position-field.transform.position;siteDelta.y=0;
            Check(siteDelta.magnitude<10,"Legacy machine moves from testing coordinates to the actual field");
            Check(FertilizerMakerInstaller.Install(preview)==installed&&preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FertilizerProcessor>(true)).Count()==1,"Repeated installation never creates a duplicate machine");
            report.AppendLine("FERTILIZER_MAKER_NATIVE_PASS checks="+checks);File.WriteAllText(FertilizerMakerSetup.Output+"/NativeAudit.txt",report.ToString());Debug.Log("FERTILIZER_MAKER_NATIVE_PASS checks="+checks);
        }
        catch(Exception error){File.WriteAllText(errorPath,report+"\n"+error);throw;}
        finally
        {
            if(FertilizerMakerUI.Instance!=null)FertilizerMakerUI.Instance.Close();
            EditorSceneManager.ClosePreviewScene(preview);foreach(var value in garbage)if(value!=null)Object.DestroyImmediate(value);
        }
    }
    static bool playerLocked(GameObject actor)=>actor.GetComponent<PlayerController>().IsMovementLocked;
    static void Click(FertilizerMakerUI ui,string name)
    {
        var button=ui.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    static void Render(FertilizerMakerUI ui,UnityEngine.SceneManagement.Scene scene,string file)
    {
        var go=new GameObject("Fertilizer UI review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
        var camera=go.AddComponent<Camera>();camera.enabled=false;camera.cameraType=CameraType.Game;camera.scene=scene;
        camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.orthographic=true;camera.orthographicSize=470.5f;camera.farClipPlane=2000;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new(.28f,.34f,.22f);camera.transform.position=new(0,0,-1000);
        var canvas=ui.GetComponent<Canvas>();var scaler=ui.GetComponent<CanvasScaler>();scaler.enabled=false;
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        var rect=(RectTransform)ui.transform;rect.position=Vector3.zero;rect.rotation=Quaternion.identity;rect.localScale=Vector3.one;rect.sizeDelta=new(1672,941);
        var texture=new RenderTexture(1672,941,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);texture.Create();camera.targetTexture=texture;
        Canvas.ForceUpdateCanvases();
        foreach(var graphic in ui.GetComponentsInChildren<Graphic>())
        {graphic.canvasRenderer.cull=false;graphic.SetAllDirty();graphic.Rebuild(CanvasUpdate.PreRender);}
        Canvas.ForceUpdateCanvases();
        var iconSlots=ui.GetComponentsInChildren<RectTransform>().Where(r=>r.name.EndsWith("Icon Slot")).ToArray();
        Check(iconSlots.Length>=9&&iconSlots.All(slot=>slot.GetComponentsInChildren<Image>().Any(iconImage=>iconImage.sprite!=null)),"Every visible fertilizer and ingredient slot uses a sprite: "+file);
        Check(ui.GetComponentsInChildren<FertilizerMakerIcon>().All(icon=>icon.GetComponent<CanvasRenderer>()!=null&&icon.canvasRenderer.GetMesh()!=null&&icon.canvasRenderer.GetMesh().vertexCount>0),"Every visible icon slot has rendered geometry: "+file);
        var layout=new StringBuilder("CANVAS "+rect.rect+" mode="+canvas.renderMode+" mask="+EditorSceneManager.GetSceneCullingMask(scene)+"\n");
        foreach(var property in typeof(Canvas).GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))
            if(property.Name.ToLowerInvariant().Contains("culling"))layout.AppendLine(property.Name+"="+property.GetValue(canvas));
        foreach(var graphic in ui.GetComponentsInChildren<Graphic>().Take(12))
            layout.AppendLine(graphic.name+" rect="+graphic.rectTransform.rect+" pos="+graphic.transform.position+" culled="+graphic.canvasRenderer.cull+" verts="+graphic.canvasRenderer.GetMesh()?.vertexCount);
        File.WriteAllText(FertilizerMakerSetup.Output+"/LayoutDebug.txt",layout.ToString());
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});
        var previous=RenderTexture.active;RenderTexture.active=texture;var image=new Texture2D(1672,941,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1672,941),0,0);image.Apply();File.WriteAllBytes(FertilizerMakerSetup.Output+"/"+file,image.EncodeToPNG());
        RenderTexture.active=previous;canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;scaler.enabled=true;
        camera.targetTexture=null;texture.Release();Object.DestroyImmediate(texture);Object.DestroyImmediate(image);Object.DestroyImmediate(go);
    }
}
#endif
