using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class SeparatedShopSetup
{
 const string Root="Assets/Nature  Paradaise";
 [MenuItem("Nature Paradise/Shop/Create Three Testing Shops")]
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  var scene=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Additive);
  var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopFront>(true)).ToArray();if(existing.Length>=3){Selection.activeGameObject=existing[0].transform.parent.gameObject;return;}
  var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopManager>(true)).FirstOrDefault(s=>s.catalog==null);
  var seller=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCSeller>(true)).FirstOrDefault();
  Vector3 position=seller!=null?seller.transform.position:scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerController>(true)).First().transform.position+Vector3.forward*4;
  Directory.CreateDirectory(Root+"/Resources/Shops");Directory.CreateDirectory(Root+"/Resources/UI");Directory.CreateDirectory(Root+"/Prefabs/Shops/Testing");AssetDatabase.Refresh();
  var items=AssetDatabase.FindAssets("t:ItemSO",new[]{Root+"/Resources"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemSO>).Where(i=>i!=null).OrderBy(i=>i.requiredVillageLevel).ThenBy(i=>i.itemName).ToList();
  var parent=new GameObject("SHOPS_TESTING — 01 Crops • 02 Animal • 03 Minimarket");SceneManager.MoveGameObjectToScene(parent,scene);
  for(int index=0;index<3;index++)
  {
   var kind=(ShopKind)index;string name=$"{index+1:00}_{kind}_Shop";var catalog=AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(Root+$"/Resources/Shops/{kind} Catalog.asset");
   if(catalog==null){catalog=ScriptableObject.CreateInstance<ShopCatalogSO>();catalog.kind=kind;catalog.displayName=kind==ShopKind.Crops?"TOKO BIBIT & KEBUN":kind==ShopKind.Animal?"TOKO HEWAN & PETERNAKAN":"MINIMARKET";AssetDatabase.CreateAsset(catalog,Root+$"/Resources/Shops/{kind} Catalog.asset");Stock(catalog,items,source);}
   var theme=AssetDatabase.LoadAssetAtPath<ShopTheme>(Root+$"/Resources/UI/{kind} Shop Theme.asset");if(theme==null){theme=ScriptableObject.CreateInstance<ShopTheme>();theme.accentColor=kind==ShopKind.Crops?new Color(.34f,.65f,.42f):kind==ShopKind.Animal?new Color(.3f,.57f,.6f):new Color(.58f,.43f,.26f);AssetDatabase.CreateAsset(theme,Root+$"/Resources/UI/{kind} Shop Theme.asset");}catalog.theme=theme;EditorUtility.SetDirty(catalog);
   var go=new GameObject(name);var manager=go.AddComponent<ShopManager>();if(source!=null)EditorUtility.CopySerialized(source,manager);manager.playerInv=null;manager.animalDeliveryPoint=null;manager.catalog=catalog;catalog.Populate(manager);var front=go.AddComponent<ShopFront>();front.catalog=catalog;front.manager=manager;BuildUI(front);
   var counter=GameObject.CreatePrimitive(PrimitiveType.Capsule);counter.name="Seller_Capsule_Editable";counter.transform.SetParent(go.transform,false);counter.transform.localPosition=new Vector3(0,1f,0);counter.transform.localScale=new Vector3(.7f,1f,.7f);
   var text=new GameObject("StoreLabel_Editable").AddComponent<TextMeshPro>();text.transform.SetParent(go.transform,false);text.transform.localPosition=new Vector3(0,2.4f,0);text.text=$"{index+1}. {kind}\n[E]";text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(5,1.4f);
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+$"/Prefabs/Shops/Testing/{name}.prefab");Object.DestroyImmediate(go);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.SetParent(parent.transform,false);instance.transform.position=position+new Vector3(index*5,0,3);
   var terrain=Terrain.activeTerrains.FirstOrDefault(t=>position.x>=t.transform.position.x&&position.x<=t.transform.position.x+t.terrainData.size.x&&position.z>=t.transform.position.z&&position.z<=t.transform.position.z+t.terrainData.size.z);if(terrain!=null){var p=instance.transform.position;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;instance.transform.position=p;}
  }
  // Keep the original transaction service for existing animal save/restore callers; retire its mixed storefront.
  foreach(var old in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCSeller>(true))){old.enabled=false;EditorUtility.SetDirty(old);}
  foreach(var tester in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopTester>(true))){tester.enabled=false;EditorUtility.SetDirty(tester);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=parent;
 }
 static ShopCatalogTab Tab(ShopCatalogSO catalog,string title,IEnumerable<ItemSO> items=null){var tab=new ShopCatalogTab{title=title};if(items!=null)tab.items.AddRange(items.Where(i=>i!=null));catalog.tabs.Add(tab);return tab;}
 static void Stock(ShopCatalogSO c,List<ItemSO> items,ShopManager source)
 {
  if(c.kind==ShopKind.Crops){Tab(c,"Benih",items.Where(i=>i.category==ItemCategory.Seed));Tab(c,"Pupuk",items.Where(i=>i.IsFertilizer));Tab(c,"Alat",items.Where(i=>i.category==ItemCategory.Tool&&!AnimalTool(i)));Tab(c,"Lainnya",items.Where(i=>i.IsCropBooster||i.IsSprinkler));}
  else if(c.kind==ShopKind.Animal)
  {
   var ayam=Tab(c,"Ayam");var bebek=Tab(c,"Bebek");var ternak=Tab(c,"Ternak");var care=AnimalCareCatalog.Load();Tab(c,"Perlengkapan",items.Where(i=>i.IsAnimalMedicine||AnimalTool(i)||i==care?.fodder||i==care?.treat||i.itemName=="Fish Feed"));
   var offers=source?.animalOffers?.Where(a=>a!=null).ToList()??new List<AnimalShopOffer>();
   if(offers.Count==0){foreach(var type in new[]{AnimalType.Chicken,AnimalType.Duck,AnimalType.Cow,AnimalType.Goat,AnimalType.Sheep}){offers.Add(new AnimalShopOffer{animalType=type,displayName="Young "+type,offerKind=AnimalShopOfferKind.Young,price=type==AnimalType.Cow?5000:type==AnimalType.Goat?3000:type==AnimalType.Sheep?3500:type==AnimalType.Duck?1400:1000,productItem=care?.Product(type)});if(AnimalGrowthProfileSO.IsBird(type))offers.Add(new AnimalShopOffer{animalType=type,displayName=type+" Egg",offerKind=AnimalShopOfferKind.Egg,price=type==AnimalType.Chicken?500:700,productItem=care?.Product(type)});}}
   foreach(var offer in offers)(offer.animalType==AnimalType.Chicken?ayam:offer.animalType==AnimalType.Duck?bebek:ternak).animals.Add(offer);
  }
  else
  {
   Tab(c,"Makanan",items.Where(i=>i.category==ItemCategory.Food&&i.refrigeratorCategory!=RefrigeratorCategory.Drink));Tab(c,"Minuman",items.Where(i=>i.refrigeratorCategory==RefrigeratorCategory.Drink));Tab(c,"Bahan",items.Where(i=>i.category==ItemCategory.Material&&i.buyPrice>0&&i!=AnimalCareCatalog.Load()?.fodder&&i!=AnimalCareCatalog.Load()?.treat));Tab(c,"Umpan",items.Where(i=>i.IsFishingBait));Tab(c,"Jual").sellInventory=true;

  }
  foreach(var item in c.tabs.SelectMany(t=>t.items).Where(i=>i.buyPrice<=0).Distinct())c.priceOverrides.Add(new ShopPriceOverride{item=item,price=Mathf.Max(100,item.sellPrice*3)});
 }
 static bool AnimalTool(ItemSO item)=>item.equippedTool==PlayerToolType.Shears||item.equippedTool==PlayerToolType.Pitchfork;
 static RectTransform R(string name,Transform parent,float x,float y,float w,float h)=>GameplayHUDStyle.Rect(name,parent,new Vector2(x,y),new Vector2(x+w,y+h));
 static Image I(string name,Transform parent,float x,float y,float w,float h,Color color){var image=R(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.preserveAspect=true;image.raycastTarget=false;return image;}
 static TMP_Text T(string name,Transform parent,string value,int size,float x,float y,float w,float h)=>GameplayHUDStyle.Text(name,parent,value,size,new Vector2(x,y),new Vector2(x+w,y+h));
 static Button B(string name,Transform parent,string text,float x,float y,float w,float h){var image=I(name,parent,x,y,w,h,new Color(.17f,.25f,.29f,.92f));image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};T("Label",image.transform,text,25,.03f,.03f,.94f,.94f).alignment=TextAlignmentOptions.Center;return b;}
 public static void BuildUI(ShopFront f)
 {
  var go=new GameObject("Shop_UI_Editable",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(f.transform,false);f.uiRoot=go;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=650;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  var safe=R("SafeArea_Editable",go.transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();f.background=I("Background_ImageSlot",safe,0,0,1,1,new Color(.08f,.14f,.16f));
  f.headerSurface=I("Header_PanelImageSlot",safe,.025f,.89f,.95f,.09f,new Color(.08f,.15f,.19f,.88f));f.shopIcon=I("ShopIcon_ImageSlot",safe,.04f,.91f,.045f,.05f,Color.clear);f.title=T("ShopTitle",safe,f.catalog.displayName,32,.095f,.91f,.32f,.05f);f.goldIcon=I("GoldIcon_ImageSlot",safe,.82f,.91f,.03f,.05f,Color.clear);f.gold=T("Gold",safe,"0 G",28,.86f,.91f,.1f,.05f);
  for(int i=0;i<5;i++)f.tabs.Add(B($"Category_{i+1}_Editable",safe,"Tab",.43f+i*.072f,.91f,.067f,.05f));
  f.merchantPortrait=I("MerchantPortrait_ImageSlot",safe,.47f,.42f,.17f,.38f,Color.clear);
  f.detailSurface=I("ProductDetail_PanelImageSlot",safe,.04f,.39f,.64f,.30f,new Color(.08f,.15f,.19f,.76f));f.heroImage=I("ProductIllustration_ImageSlot",safe,.055f,.405f,.20f,.26f,Color.clear);f.productName=T("ProductName",safe,"Pilih barang",35,.28f,.585f,.375f,.07f);f.description=T("Description",safe,"",23,.28f,.415f,.375f,.16f);f.description.textWrappingMode=TextWrappingModes.Normal;
  var carousel=R("ProductCarousel_Editable",safe,.06f,.12f,.63f,.24f);
  for(int i=0;i<6;i++){var card=B($"ProductCard_{i+1:00}_Editable",carousel,"",i/6f,.02f,.155f,.96f);Object.DestroyImmediate(card.GetComponentInChildren<TMP_Text>().gameObject);f.cards.Add(card);f.cardImages.Add(I("Product_ImageSlot",card.transform,.07f,.35f,.86f,.62f,Color.clear));var label=T("ProductNameAndPrice",card.transform,"",21,.04f,.03f,.92f,.28f);label.alignment=TextAlignmentOptions.Center;f.cardLabels.Add(label);}
  f.previousButton=B("PreviousProduct",safe,"‹",.025f,.2f,.03f,.065f);f.nextButton=B("NextProduct",safe,"›",.69f,.2f,.03f,.065f);
  f.transactionSurface=I("Transaction_PanelImageSlot",safe,.74f,.12f,.235f,.36f,new Color(.08f,.15f,.19f,.88f));T("QuantityLabel",safe,"Jumlah",25,.755f,.405f,.09f,.045f);f.minusButton=B("DecreaseQuantity",safe,"−",.845f,.40f,.035f,.055f);f.quantityText=T("Quantity",safe,"1",30,.885f,.40f,.035f,.055f);f.quantityText.alignment=TextAlignmentOptions.Center;f.plusButton=B("IncreaseQuantity",safe,"+",.925f,.40f,.035f,.055f);
  f.unitPrice=T("UnitPrice",safe,"",24,.755f,.335f,.20f,.045f);f.totalPrice=T("TotalPrice",safe,"",32,.755f,.28f,.20f,.05f);f.capacity=T("InventoryOrBarnCapacity",safe,"",21,.755f,.235f,.20f,.045f);f.buyButton=B("BuySell_ButtonImageSlot",safe,"Beli",.755f,.17f,.20f,.06f);f.balance=T("RemainingGold",safe,"",21,.755f,.125f,.20f,.035f);
  I("FooterPanel",safe,0,0,1,.085f,new Color(.08f,.15f,.19f,.9f));T("KeyboardGuide",safe,"A / D  Pilih barang     W / S  Kategori     + / −  Jumlah     Enter  Beli/Jual     Esc / E  Tutup",23,.035f,.02f,.70f,.05f);f.feedback=T("TransactionFeedback",safe,"Silakan pilih barang.",22,.04f,.075f,.66f,.035f);f.closeButton=B("CloseShop",safe,"Tutup ×",.86f,.02f,.115f,.05f);
  ShopPresentation.Layout(f);go.SetActive(false);
 }
}
[CustomEditor(typeof(ShopFront))]
public sealed class ShopFrontEditor:Editor
{
 public override void OnInspectorGUI(){DrawDefaultInspector();var shop=(ShopFront)target;if(GUILayout.Button(Application.isPlaying?"Open Shop For Testing":"Preview UI In Hierarchy")){if(Application.isPlaying)shop.Open();else shop.Preview();}if(GUILayout.Button("Close / Hide Preview"))shop.Close();if(shop.catalog?.theme!=null&&GUILayout.Button("Select Theme / Image Slots"))Selection.activeObject=shop.catalog.theme;}
}

