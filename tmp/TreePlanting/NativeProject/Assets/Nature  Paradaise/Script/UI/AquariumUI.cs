using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UGUI aquarium screen backed by the tank, bag and saved fish collection.</summary>
[RequireComponent(typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster))]
public sealed class AquariumUI : MonoBehaviour
{
 public static AquariumUI Active {get;private set;}
 public Image backgroundImageSlot,heroImageSlot,tankImageSlot;
 public TMP_Text capacity,listHeading,fishName,stars,details,notice,pageLabel;
 public Button deposit,withdraw,collection,close,previous,next,feed;
 public List<Button> rows=new();
 public List<Image> rowImages=new();
 public List<TMP_Text> rowLabels=new();
 public List<Button> decorations=new();
 Aquarium tank;int selected,page;int mode; // 0 tank, 1 bag, 2 collection
 readonly List<int> bagSlots=new();
 readonly List<FishCollectionEntrySaveData> entries=new();
 bool bound,cursorVisible;CursorLockMode cursorLock;float nextRefresh;
 public static void Show(Aquarium value)
 {
  if(Active!=null)Active.tank?.ClosePanel();
  var prefab=Resources.Load<GameObject>("UI/Aquarium UI");
  var go=prefab!=null?Instantiate(prefab):new GameObject("Aquarium_UI_Editable");
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,value.gameObject.scene);
  var ui=go.GetComponent<AquariumUI>();if(ui==null)ui=go.AddComponent<AquariumUI>();if(ui.rows.Count==0)ui.Build();
  ui.tank=value;ui.mode=0;ui.page=ui.selected=0;ui.Bind();Active=ui;ui.cursorVisible=Cursor.visible;ui.cursorLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;go.SetActive(true);ui.Refresh();GameplayInput.ConsumeCurrentFrame();
 }
 public static void Hide(Aquarium value){if(Active==null||Active.tank!=value)return;var ui=Active;Active=null;Cursor.visible=ui.cursorVisible;Cursor.lockState=ui.cursorLock;Destroy(ui.gameObject);GameplayInput.ConsumeCurrentFrame();}
 void OnDestroy(){if(Active!=this)return;Active=null;Cursor.visible=cursorVisible;Cursor.lockState=cursorLock;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Active=null;
 void Bind()
 {
  if(bound)return;bound=true;
  close.onClick.AddListener(()=>tank?.ClosePanel());deposit.onClick.AddListener(Deposit);withdraw.onClick.AddListener(Withdraw);collection.onClick.AddListener(()=>Switch(mode==2?0:2));feed.onClick.AddListener(()=>{tank.FeedFish();Refresh();});previous.onClick.AddListener(()=>MovePage(-1));next.onClick.AddListener(()=>MovePage(1));
  for(int n=0;n<rows.Count;n++){int i=n;rows[n].onClick.AddListener(()=>{selected=page*rows.Count+i;Refresh();});}
  string[] fields={"Background","Base","Plant","Rock","Decor"};for(int n=0;n<decorations.Count;n++){string field=fields[n];decorations[n].onClick.AddListener(()=>{tank.CycleDecoration(field);Refresh();});}
 }
 void Update()
 {
  if(tank==null||Active!=this)return;
  if(Input.GetKeyDown(KeyCode.Escape)){tank.ClosePanel();return;}
  if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.A))Move(-1);
  if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.D))Move(1);
  if(Input.GetKeyDown(KeyCode.Return))Deposit();
  if(Input.GetKeyDown(KeyCode.R))Withdraw();
  if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;Refresh();}
 }
 void Switch(int value){mode=value;selected=page=0;Refresh();}
 int Count=>mode==1?bagSlots.Count:mode==2?entries.Count:tank?.FishCount??0;
 void Move(int delta){if(Count==0)return;selected=(selected+delta+Count)%Count;page=selected/rows.Count;Refresh();}
 void MovePage(int delta){int pages=Mathf.Max(1,Mathf.CeilToInt((mode==0?tank.Capacity:Count)/(float)rows.Count));page=Mathf.Clamp(page+delta,0,pages-1);selected=page*rows.Count;Refresh();}
 public void Deposit(){if(tank==null)return;if(mode!=1){Switch(1);return;}if(selected<bagSlots.Count){tank.AddFish(bagSlots[selected]);Refresh();}}
 public void Withdraw(){if(tank==null)return;if(mode!=0){Switch(0);return;}if(selected<tank.FishCount){tank.TakeFish(selected);Refresh();}}
 static ItemSO Item(AquariumFishData fish)=>ItemCatalog.Resolve(fish.itemId,fish.assetName,fish.itemName);
 static Sprite Illustration(ItemSO item)=>item!=null?item.inventoryIllustration??item.icon:null;
 static void Paint(Image image,Sprite sprite){image.sprite=sprite;image.color=sprite!=null?Color.white:Color.clear;}
 public void Refresh()
 {
  if(tank==null)return;Paint(backgroundImageSlot,backgroundImageSlot.sprite);Paint(tankImageSlot,tankImageSlot.sprite);bagSlots.Clear();var inv=tank.PlayerInventory;for(int n=0;inv!=null&&n<inv.slots.Count;n++){var stack=inv.GetSlot(n);if(stack?.item!=null&&stack.count>0&&stack.item.category==ItemCategory.Fish)bagSlots.Add(n);}
  entries.Clear();entries.AddRange(FishCollectionService.Collection.OrderBy(x=>x.itemId));
  capacity.text=$"Kapasitas   {tank.FishCount} / {tank.Capacity}";listHeading.text=mode==1?"IKAN DI TAS":mode==2?"KOLEKSI IKAN":"IKAN DI AQUARIUM";
  int slots=mode==0?tank.Capacity:Count;int pages=Mathf.Max(1,Mathf.CeilToInt(slots/(float)rows.Count));page=Mathf.Clamp(page,0,pages-1);selected=Mathf.Clamp(selected,0,Mathf.Max(0,Count-1));
  ItemSO chosen=null;float chosenSize=0,weight=0;int quality=0;
  for(int n=0;n<rows.Count;n++)
  {
   int index=page*rows.Count+n;bool occupied=index<Count;bool visible=index<slots||n==0&&slots==0;rows[n].gameObject.SetActive(visible);rows[n].interactable=occupied;
   ItemSO item=null;string name="Kosong",extra="";int q=0;float size=0,w=0;
   if(occupied&&mode==0){var f=tank.Fish[index];item=Item(f);name=item?.itemName??f.itemName;q=f.qualityStars;size=f.sizeCm;w=f.weightKg;}
   else if(occupied&&mode==1){var f=inv.GetSlot(bagSlots[index]);item=f.item;name=item.itemName;q=f.qualityStars;size=f.fishSizeCm;w=f.fishWeightKg;extra=$"  ×{f.count}";}
   else if(occupied){var f=entries[index];item=ItemCatalog.Resolve(f.itemId,null,null);name=item?.itemName??f.fishId;size=f.largestSizeCm;extra=$"  · {f.caughtCount} tangkapan";}
   rowLabels[n].text=occupied?$"{name}{extra}\n<color=#EFC061>{(q>0?$"Kualitas {q} / 5":mode==2?"Rekor koleksi":"Normal")}</color>   {size:0.0} cm":slots==0?"Belum ada ikan":"Kosong";Paint(rowImages[n],Illustration(item));CommerceUIStyle.Selection(rows[n],occupied&&index==selected);
   if(occupied&&index==selected){chosen=item;chosenSize=size;weight=w;quality=q;}
  }
  Paint(heroImageSlot,Illustration(chosen));fishName.text=chosen?.itemName??"Pilih ikan";stars.text=chosen!=null?(quality>0?$"Kualitas {quality} / 5":mode==2?"Rekor koleksi":"Normal"):"";
  var record=chosen!=null?FishCollectionService.Collection.FirstOrDefault(x=>x.itemId==chosen.Id):null;
  details.text=chosen==null?"Masukkan ikan dari tas untuk mengisi aquarium.":$"Ukuran             {chosenSize:0.0} cm\nBerat                {(mode==2?"Belum direkam":weight.ToString("0.00")+" kg")}\nRekor terbesar    {(record!=null?record.largestSizeCm.ToString("0.0")+" cm":"Belum tercatat")}\nLokasi tangkap   Belum direkam\nJam tangkap       Belum direkam";
  notice.text=!string.IsNullOrEmpty(tank.Feedback)?tank.Feedback:mode==2?"Koleksi mencatat hasil tangkapanmu.":"Ikan tetap tersimpan dan dapat diambil kembali.";
  deposit.GetComponentInChildren<TMP_Text>().text=mode==1?"Masukkan Ikan Terpilih":"Masukkan Ikan";deposit.interactable=mode!=1||Count>0&&tank.FishCount<tank.Capacity;
  withdraw.interactable=tank.FishCount>0;collection.GetComponentInChildren<TMP_Text>().text=mode==2?"Kembali ke Aquarium":"Lihat Koleksi";
  pageLabel.text=$"{page+1} / {pages}";previous.interactable=page>0;next.interactable=page+1<pages;
 }
 public void Build()
 {
  gameObject.name="Aquarium_UI_Editable";var canvas=GetComponent<Canvas>();if(canvas==null)canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=660;var scaler=GetComponent<CanvasScaler>();if(scaler==null)scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;if(GetComponent<GraphicRaycaster>()==null)gameObject.AddComponent<GraphicRaycaster>();
  var shade=CommerceUIStyle.Rect("FullViewport_Backdrop",transform,0,0,1,1).gameObject.AddComponent<Image>();shade.color=new Color(.025f,.045f,.05f,.77f);shade.raycastTarget=true;backgroundImageSlot=CommerceUIStyle.ImageSlot("Background_ImageSlot",transform,0,0,1,1);backgroundImageSlot.preserveAspect=false;
  var safe=CommerceUIStyle.Rect("SafeArea",transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
  CommerceUIStyle.Panel("AquariumPanel",safe,.445f,.025f,.54f,.95f,CommerceUIStyle.Ink);
  CommerceUIStyle.Label("Title",safe,"AQUARIUM RUMAH",36,.475f,.885f,.46f,.06f,true);CommerceUIStyle.Label("Subtitle",safe,"Koleksi Ikan",22,.475f,.852f,.26f,.032f);capacity=CommerceUIStyle.Label("Capacity",safe,"",23,.79f,.85f,.17f,.04f);
  listHeading=CommerceUIStyle.Label("ListHeading",safe,"IKAN DI AQUARIUM",22,.475f,.802f,.24f,.035f,true);
  for(int n=0;n<8;n++){var row=CommerceUIStyle.Button("FishSlot_"+(n+1),safe,"",.475f,.736f-n*.063f,.235f,.058f);var label=row.GetComponentInChildren<TMP_Text>();label.alignment=TextAlignmentOptions.Left;label.fontSize=19;label.rectTransform.anchorMin=new Vector2(.34f,.06f);label.rectTransform.anchorMax=new Vector2(.96f,.94f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;rows.Add(row);rowLabels.Add(label);rowImages.Add(CommerceUIStyle.ImageSlot("Fish_ImageSlot",row.transform,.025f,.05f,.29f,.90f));}
  previous=CommerceUIStyle.Button("PreviousPage",safe,"‹",.475f,.218f,.035f,.035f);next=CommerceUIStyle.Button("NextPage",safe,"›",.675f,.218f,.035f,.035f);pageLabel=CommerceUIStyle.Label("Page",safe,"1 / 1",18,.53f,.218f,.11f,.035f);pageLabel.alignment=TextAlignmentOptions.Center;
  CommerceUIStyle.Panel("FishDetails",safe,.73f,.436f,.23f,.37f,CommerceUIStyle.Card);fishName=CommerceUIStyle.Label("FishName",safe,"Pilih ikan",28,.747f,.737f,.19f,.05f,true);stars=CommerceUIStyle.Label("Quality",safe,"",23,.747f,.70f,.19f,.04f);stars.color=CommerceUIStyle.Gold;heroImageSlot=CommerceUIStyle.ImageSlot("FishIllustration_ImageSlot",safe,.747f,.565f,.19f,.135f);details=CommerceUIStyle.Label("CatchDetails",safe,"",18,.747f,.45f,.19f,.115f);
  CommerceUIStyle.Label("TankHeading",safe,"Tata Letak di Aquarium",20,.73f,.397f,.23f,.032f);CommerceUIStyle.Panel("TankPreview",safe,.73f,.278f,.23f,.11f,CommerceUIStyle.Card);tankImageSlot=CommerceUIStyle.ImageSlot("TankLayout_ImageSlot",safe,.735f,.281f,.22f,.104f);
  string[] names={"Air","Dasar","Tanaman","Batu","Dekor"};for(int n=0;n<5;n++){var button=CommerceUIStyle.Button("Decoration_"+n,safe,names[n],.73f+n*.047f,.231f,.043f,.032f);var text=button.GetComponentInChildren<TMP_Text>();text.enableAutoSizing=false;text.fontSize=16;decorations.Add(button);}
  feed=CommerceUIStyle.Button("FeedFish",safe,"Beri Makan",.73f,.188f,.23f,.034f);
  deposit=CommerceUIStyle.Button("DepositFish",safe,"Masukkan Ikan",.475f,.115f,.19f,.048f,true);withdraw=CommerceUIStyle.Button("WithdrawFish",safe,"Ambil Ikan",.675f,.115f,.145f,.048f);collection=CommerceUIStyle.Button("ViewCollection",safe,"Lihat Koleksi",.83f,.115f,.13f,.048f);
  notice=CommerceUIStyle.Label("Notice",safe,"",18,.475f,.075f,.485f,.03f);notice.color=CommerceUIStyle.Muted;CommerceUIStyle.Label("KeyboardGuide",safe,"W / S  Pilih ikan     Enter  Masukkan     R  Ambil     Esc  Tutup",18,.475f,.031f,.40f,.032f);close=CommerceUIStyle.Button("Close",safe,"Tutup ×",.887f,.031f,.073f,.032f);
 }
}
