using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class UpgradeShopsSetup
{
 const string Root="Assets/Nature  Paradaise";
 [MenuItem("Nature Paradise/Shop/Apply Seller Scale and Upgrade Shops")]
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  var scene=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Additive);
  var roots=scene.GetRootGameObjects();var seller=roots.SelectMany(x=>x.GetComponentsInChildren<NPCSeller>(true)).First();var reference=seller.GetComponent<Renderer>();
  var parent=roots.SelectMany(x=>x.GetComponentsInChildren<ShopFront>(true)).First(x=>x.catalog!=null&&x.catalog.kind==ShopKind.Crops).transform.parent;
  foreach(var f in parent.GetComponentsInChildren<ShopFront>(true)){var capsule=f.transform.Find("Seller_Capsule_Editable");if(capsule!=null)StyleCapsule(capsule,seller,reference);Label(f.transform,f.catalog.displayName);PrefabUtility.RecordPrefabInstancePropertyModifications(f);}
  Directory.CreateDirectory(Root+"/Resources/Shops");Directory.CreateDirectory(Root+"/Prefabs/Shops/Testing");
  var catalog=AssetDatabase.LoadAssetAtPath<BlacksmithCatalog>(Root+"/Resources/Shops/Blacksmith Upgrades.asset");
  if(catalog==null){catalog=ScriptableObject.CreateInstance<BlacksmithCatalog>();AssetDatabase.CreateAsset(catalog,Root+"/Resources/Shops/Blacksmith Upgrades.asset");var all=AssetDatabase.FindAssets("t:ItemSO",new[]{Root+"/Resources"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemSO>).ToArray();var wood=all.FirstOrDefault(x=>x.itemName=="Wood");var iron=all.FirstOrDefault(x=>x.itemName.Contains("Iron")&&x.category==ItemCategory.Material);var stone=all.FirstOrDefault(x=>x.itemName=="Stone");foreach(var type in new[]{PlayerToolType.WateringCan,PlayerToolType.Hoe,PlayerToolType.Axe,PlayerToolType.Hammer,PlayerToolType.Sickle,PlayerToolType.FishingRod}){var item=all.FirstOrDefault(x=>x.category==ItemCategory.Tool&&x.equippedTool==type);if(item==null)continue;var offer=new SmithToolOffer{item=item,displayName=type==PlayerToolType.WateringCan?"Penyiram":type==PlayerToolType.Hoe?"Cangkul":type==PlayerToolType.Axe?"Kapak":type==PlayerToolType.Hammer?"Palu":type==PlayerToolType.Sickle?"Sabit":"Pancing"};for(int level=2;level<=4;level++){var cost=new BuildingLevelDefinition{level=level,goldCost=1000*(level-1),constructionDays=0};if(iron!=null)cost.materialCosts.Add(new BuildingMaterialCost{item=iron,amount=3*(level-1)});else if(stone!=null)cost.materialCosts.Add(new BuildingMaterialCost{item=stone,amount=5*(level-1)});if(wood!=null)cost.materialCosts.Add(new BuildingMaterialCost{item=wood,amount=3*(level-1)});offer.upgrades.Add(cost);}catalog.tools.Add(offer);}EditorUtility.SetDirty(catalog);}
  var basePos=parent.GetComponentsInChildren<ShopFront>().First().transform.position;
  foreach(var kind in new[]{UpgradeShopKind.Blacksmith,UpgradeShopKind.Lumber})
  {
   if(parent.GetComponentsInChildren<UpgradeShopFront>(true).Any(x=>x.kind==kind))continue;
   string name=kind==UpgradeShopKind.Blacksmith?"04_Blacksmith_Shop":"05_Lumber_Store";var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);var front=go.AddComponent<UpgradeShopFront>();front.kind=kind;front.catalog=catalog;front.housePreviewDefinition=roots.SelectMany(x=>x.GetComponentsInChildren<PlayerHouseController>(true)).FirstOrDefault()?.Definition;
   var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);capsule.name="Seller_Capsule_Editable";capsule.transform.SetParent(go.transform,false);StyleCapsule(capsule.transform,seller,reference);Label(go.transform,kind==UpgradeShopKind.Blacksmith?"PANDAI BESI":"LUMBER STORE");BuildUI(front);
   if(kind==UpgradeShopKind.Blacksmith)
   {
    var materialCatalog=AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(Root+"/Resources/Shops/Blacksmith Materials.asset");if(materialCatalog==null){materialCatalog=ScriptableObject.CreateInstance<ShopCatalogSO>();materialCatalog.displayName="PANDAI BESI  /  BELI MATERIAL";materialCatalog.kind=ShopKind.Minimarket;materialCatalog.theme=Resources.Load<ShopTheme>("UI/Minimarket Shop Theme");var tab=new ShopCatalogTab{title="Material"};foreach(var guid in AssetDatabase.FindAssets("t:ItemSO",new[]{Root+"/Resources"})){var item=AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));if(item.category==ItemCategory.Material&&item.buyPrice>0)tab.items.Add(item);}materialCatalog.tabs.Add(tab);AssetDatabase.CreateAsset(materialCatalog,Root+"/Resources/Shops/Blacksmith Materials.asset");RefineMaterials();}
    var materialRoot=new GameObject("MaterialShop_Service");materialRoot.transform.SetParent(go.transform,false);var m=materialRoot.AddComponent<ShopManager>();m.catalog=materialCatalog;materialCatalog.Populate(m);var shop=materialRoot.AddComponent<ShopFront>();shop.interactionEnabled=false;shop.catalog=materialCatalog;shop.manager=m;SeparatedShopSetup.BuildUI(shop);front.materialShop=shop;
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Shops/Testing/"+name+".prefab");Object.DestroyImmediate(go);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.SetParent(parent,true);var p=basePos+Vector3.right*(kind==UpgradeShopKind.Blacksmith?15:20);foreach(var t in Terrain.activeTerrains)if(p.x>=t.transform.position.x&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+t.terrainData.size.z){p.y=t.SampleHeight(p)+t.transform.position.y;break;}instance.transform.position=p;
  }
  foreach(var old in roots.SelectMany(x=>x.GetComponentsInChildren<CarpenterNPC>(true))){old.enabled=false;EditorUtility.SetDirty(old);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=parent.gameObject;
 }
 [MenuItem("Nature Paradise/Shop/Refresh Upgrade UI Layouts")]
 public static void RefreshLayouts()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  foreach(var front in Object.FindObjectsByType<UpgradeShopFront>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(front.uiRoot!=null)Object.DestroyImmediate(front.uiRoot);front.cards.Clear();front.cardImages.Clear();front.cardLabels.Clear();BuildUI(front);
   EditorUtility.SetDirty(front);EditorSceneManager.MarkSceneDirty(front.gameObject.scene);EditorSceneManager.SaveScene(front.gameObject.scene);
  }
  foreach(var name in new[]{"04_Blacksmith_Shop","05_Lumber_Store"}){
   string path=Root+"/Prefabs/Shops/Testing/"+name+".prefab";var go=PrefabUtility.LoadPrefabContents(path);var front=go.GetComponent<UpgradeShopFront>();Object.DestroyImmediate(front.uiRoot);front.cards.Clear();front.cardImages.Clear();front.cardLabels.Clear();BuildUI(front);PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
  }
 }
 public static void RefineMaterials()
 {
  var catalog=AssetDatabase.LoadAssetAtPath<BlacksmithCatalog>(Root+"/Resources/Shops/Blacksmith Upgrades.asset");
  var stock=AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(Root+"/Resources/Shops/Blacksmith Materials.asset");if(catalog==null||stock==null)return;
  var required=catalog.tools.SelectMany(t=>t.upgrades).SelectMany(x=>x.materialCosts).Where(x=>x.item!=null).Select(x=>x.item).Distinct().ToList();
  stock.tabs[0].items=required;stock.priceOverrides.Clear();foreach(var item in required)if(item.buyPrice<=0)stock.priceOverrides.Add(new ShopPriceOverride{item=item,price=Mathf.Max(50,item.sellPrice*3)});
  EditorUtility.SetDirty(stock);AssetDatabase.SaveAssets();
 }
 static void StyleCapsule(Transform capsule,NPCSeller seller,Renderer reference){capsule.localScale=seller.transform.lossyScale;capsule.localPosition=Vector3.up*capsule.localScale.y;capsule.localRotation=Quaternion.identity;if(reference!=null)capsule.GetComponent<Renderer>().sharedMaterials=reference.sharedMaterials;PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);PrefabUtility.RecordPrefabInstancePropertyModifications(capsule.GetComponent<Renderer>());}
 static void Label(Transform parent,string title){var child=parent.Find("StoreLabel_Editable");var text=child!=null?child.GetComponent<TextMeshPro>():new GameObject("StoreLabel_Editable").AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=new Vector3(0,2.65f,0);text.text=title; text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.color=GameplayHUDStyle.TextColor;text.rectTransform.sizeDelta=new Vector2(7,1.1f);text.raycastTarget=false;if(text.GetComponent<ShopWorldLabel>()==null)text.gameObject.AddComponent<ShopWorldLabel>();PrefabUtility.RecordPrefabInstancePropertyModifications(text);PrefabUtility.RecordPrefabInstancePropertyModifications(text.transform);}
 static TMP_Text T(string n,Transform p,string s,int size,float x,float y,float w,float h,bool bold=false)=>CommerceUIStyle.Label(n,p,s,size,x,y,w,h,bold);
 static Image I(string n,Transform p,float x,float y,float w,float h)=>CommerceUIStyle.ImageSlot(n,p,x,y,w,h);
 public static void BuildUI(UpgradeShopFront f)
 {
  bool smith=f.kind==UpgradeShopKind.Blacksmith;
  var go=new GameObject("Upgrade_UI_Editable",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(f.transform,false);f.uiRoot=go;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=650;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  var shade=CommerceUIStyle.Rect("FullViewport_Backdrop",go.transform,0,0,1,1).gameObject.AddComponent<Image>();shade.color=new Color(.025f,.045f,.05f,.77f);shade.raycastTarget=true;f.background=I("Background_ImageSlot",go.transform,0,0,1,1);f.background.preserveAspect=false;
  var safe=CommerceUIStyle.Rect("SafeArea",go.transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
  f.merchantPortrait=I("MerchantPortrait_ImageSlot",safe,.05f,.20f,.30f,.60f);
  if(smith)
  {
   CommerceUIStyle.Panel("Header",safe,.025f,.89f,.95f,.085f,CommerceUIStyle.Ink);f.heading=T("Heading",safe,"PANDAI BESI",32,.06f,.908f,.30f,.05f,true);f.gold=T("Gold",safe,"0 G",27,.84f,.908f,.12f,.05f);f.upgradeTab=CommerceUIStyle.Button("UpgradeTab",safe,"Upgrade Tools",.39f,.908f,.18f,.05f,true);f.materialsTab=CommerceUIStyle.Button("MaterialsTab",safe,"Beli Material",.58f,.908f,.16f,.05f);
   CommerceUIStyle.Panel("UpgradeDetails",safe,.15f,.16f,.70f,.39f,CommerceUIStyle.Ink);f.hero=I("ToolIllustration_ImageSlot",safe,.18f,.27f,.24f,.25f);f.selectionTitle=T("SelectedTool",safe,"Penyiram",38,.46f,.46f,.36f,.06f,true);f.comparison=T("LevelComparison",safe,"",29,.46f,.399f,.36f,.05f,true);f.benefits=T("ToolDescription",safe,"",22,.46f,.30f,.35f,.08f);f.requirements=T("Requirements",safe,"",23,.46f,.17f,.25f,.12f);
   for(int n=0;n<6;n++){var card=CommerceUIStyle.Button("ToolCard_"+n,safe,"",.15f+n*.116f,.065f,.107f,.083f);Object.DestroyImmediate(card.GetComponentInChildren<TMP_Text>().gameObject);f.cards.Add(card);f.cardImages.Add(I("Tool_ImageSlot",card.transform,.05f,.35f,.28f,.6f));f.cardLabels.Add(T("Name",card.transform,"",20,.36f,.15f,.6f,.7f));}
   f.confirm=CommerceUIStyle.Button("Upgrade",safe,"Upgrade Tool",.71f,.215f,.125f,.05f,true);f.balance=T("Balance",safe,"",18,.71f,.17f,.125f,.035f);f.timing=T("Duration",safe,"",19,.16f,.56f,.35f,.04f);f.feedback=T("Availability",safe,"",20,.46f,.555f,.37f,.045f);
  }
  else
  {
   var panel=CommerceUIStyle.Panel("ConstructionPanel",safe,.50f,.11f,.475f,.85f,CommerceUIStyle.Ink);f.heading=T("Heading",safe,"LUMBER STORE",32,.525f,.887f,.29f,.05f,true);f.gold=T("Gold",safe,"0 G",26,.825f,.887f,.12f,.05f);CommerceUIStyle.Panel("SelectedCategory",safe,.525f,.805f,.425f,.06f,CommerceUIStyle.Sage);T("ConstructionSubtitle",safe,"JASA BANGUNAN",18,.525f,.868f,.29f,.025f);T("Category",safe,"Rumah",26,.54f,.816f,.38f,.04f,true).alignment=TextAlignmentOptions.Center;
   f.selectionTitle=T("UpgradeTitle",safe,"UPGRADE RUMAH",30,.53f,.738f,.42f,.05f,true);f.hero=I("HousePreview_ImageSlot",safe,.53f,.40f,.23f,.29f);f.benefits=T("UnlockedFacilities",safe,"",23,.775f,.40f,.17f,.285f);f.comparison=T("LevelComparison",safe,"",25,.54f,.345f,.39f,.044f);f.requirements=T("Requirements",safe,"",22,.535f,.19f,.23f,.13f);f.timing=T("Duration",safe,"",23,.78f,.258f,.17f,.075f);f.confirm=CommerceUIStyle.Button("Upgrade",safe,"Upgrade Rumah",.78f,.192f,.17f,.057f,true);f.balance=T("Balance",safe,"",18,.78f,.155f,.17f,.035f);f.feedback=T("Availability",safe,"",20,.535f,.115f,.41f,.035f);
  }
  f.close=CommerceUIStyle.Button("Close",safe,"Tutup ×",.865f,.015f,.11f,.045f);T("KeyboardGuide",safe,smith?"A / D  Pilih tool     Enter  Upgrade     Esc / E  Tutup":"Enter  Upgrade Rumah     Esc / E  Tutup",21,.04f,.02f,.78f,.034f);f.gold.color=CommerceUIStyle.Gold;f.balance.color=CommerceUIStyle.Muted;go.SetActive(false);
 }
}
[CustomEditor(typeof(UpgradeShopFront))]public sealed class UpgradeShopFrontEditor:Editor
{
 public override void OnInspectorGUI(){DrawDefaultInspector();var front=(UpgradeShopFront)target;if(GUILayout.Button(Application.isPlaying?"Open For Testing":"Preview UI In Hierarchy"))front.Open();if(GUILayout.Button("Close Preview"))front.Close();}
}



