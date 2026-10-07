using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct ShopFrontProduct
{
 public readonly ItemSO item;public readonly AnimalShopOffer animal;public readonly bool selling;readonly int priceOverride; readonly ShopPriceOverride settings;
 public ShopFrontProduct(ItemSO value,bool sell=false,int price=-1,ShopPriceOverride configuration=null){item=value;animal=null;selling=sell;priceOverride=price;settings=configuration;}
 public ShopFrontProduct(AnimalShopOffer value){animal=value;item=null;selling=false;priceOverride=-1;settings=null;}
 public string Name=>animal!=null?animal.displayName:item!=null?item.itemName:"";
 public int Price=>priceOverride>=0?priceOverride:animal!=null?animal.price:item!=null?(selling?item.sellPrice:item.buyPrice):0;
 public int Quality=>selling?0:Mathf.Clamp(settings?.qualityStars??0,0,5);
 public int MaximumAmount=>selling?99:Mathf.Clamp(settings?.maximumAmount??99,1,99);
 public int DefaultAmount=>Mathf.Clamp(settings?.defaultAmount??1,1,MaximumAmount);
 public Sprite Icon=>settings?.image!=null?settings.image:animal!=null?animal.icon:item!=null?item.inventoryIllustration!=null?item.inventoryIllustration:item.icon:null;
 public int RequiredLevel=>animal!=null?animal.requiredVillageLevel:item!=null?item.requiredVillageLevel:1;
}
[DisallowMultipleComponent]
public sealed class ShopFront:MonoBehaviour
{
 public static ShopFront Active {get;private set;}
 public static bool IsAnyOpen=>Active!=null;
 [Header("Store")]
 public ShopCatalogSO catalog;
 public ShopManager manager;
 [Min(.5f)] public float interactionRadius=2.5f;
 public bool interactionEnabled=true;
 [Header("Authored UI — editable in Hierarchy")]
 public GameObject uiRoot;
 public TMP_Text title,gold,productName,description,unitPrice,totalPrice,balance,capacity,quantityText,feedback;
 public Image background,shopIcon,merchantPortrait,goldIcon,heroImage;
 public Image headerSurface,detailSurface,transactionSurface;
 public Button buyButton,minusButton,plusButton,previousButton,nextButton,closeButton;
 public List<Button> tabs=new(),cards=new();
 public List<Image> cardImages=new();
 public List<TMP_Text> cardLabels=new();
 readonly List<ShopFrontProduct> products=new();
 public IReadOnlyList<ShopFrontProduct> Products=>products;
 public int SelectedTab {get;private set;}
 public int SelectedIndex {get;private set;}
 public int Quantity {get;private set;}=1;
 public bool IsOpen=>Active==this;
 Inventory inventory;PlayerController player;TimeManager clock;
 float nextCatalogRefresh;
 bool bound;bool cursorVisible;CursorLockMode cursorLock;
 void Awake(){if(uiRoot!=null)uiRoot.SetActive(false);Bind();}
 void Bind()
 {
  if(bound)return;bound=true;ShopPresentation.Apply(this);
  for(int i=0;i<tabs.Count;i++){int n=i;tabs[i].onClick.AddListener(()=>SelectTab(n));}
  for(int i=0;i<cards.Count;i++){int n=i;cards[i].onClick.AddListener(()=>SelectProduct((SelectedIndex/cards.Count)*cards.Count+n));}
  buyButton?.onClick.AddListener(()=>Purchase());minusButton?.onClick.AddListener(()=>SetQuantity(Quantity-1));plusButton?.onClick.AddListener(()=>SetQuantity(Quantity+1));
  previousButton?.onClick.AddListener(()=>Move(-1));nextButton?.onClick.AddListener(()=>Move(1));closeButton?.onClick.AddListener(Close);
 }
 void Resolve(){if(player==null)player=FindFirstObjectByType<PlayerController>();inventory=player!=null?player.GetComponent<Inventory>():null;clock=TimeManager.Instance;if(manager!=null)manager.playerInv=inventory;}
 void Update()
 {
  if(catalog==null||catalog.tabs.Count==0)return;
  if(IsOpen)
  {
   if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)){Close();return;}
   if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))Move(-1);
   else if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))Move(1);
   else if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))SelectTab((SelectedTab+catalog.tabs.Count-1)%catalog.tabs.Count);
   else if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))SelectTab((SelectedTab+1)%catalog.tabs.Count);
   else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Purchase();
   else if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.KeypadPlus))SetQuantity(Quantity+1);
   else if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus))SetQuantity(Quantity-1);
   if(Time.unscaledTime>=nextCatalogRefresh){nextCatalogRefresh=Time.unscaledTime+.25f;RefreshCatalog();}else RefreshDetails();return;
  }
  if(IsAnyOpen||UpgradeShopFront.Active!=null||!interactionEnabled)return;Resolve();if(player==null||!PlayerInteractionTarget.ContainsPickup(player.transform,transform,interactionRadius))return;
  WorldInteractionPrompt.Request(this,transform,"E: "+catalog.displayName,Vector3.Distance(player.transform.position,transform.position),1.5f);
  if(PlayerInteractionTarget.PressPickup(player.transform,transform,KeyCode.E,interactionRadius))Open();
 }
 [ContextMenu("Open Shop For Testing")]
 public void Open()
 {
  if(IsOpen)return;
  if(Application.isPlaying&&MarketStand.IsAnyOpen)return;
  if(!Application.isPlaying){Preview();return;}
  if(catalog==null||uiRoot==null||GameplayPauseMenu.BlocksGameplayInput||(SceneTransitionManager.Instance?.IsTransitioning??false))return;
  if(Active!=null&&Active!=this)Active.Close();Resolve();Bind();Active=this;
  player?.AcquireMovementLock(this);clock?.AcquirePause(this);WorldInteractionPrompt.AcquireSuppression(this);
  cursorVisible=Cursor.visible;cursorLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
  uiRoot.SetActive(true);SelectedTab=0;Quantity=1;feedback.text="Silakan pilih barang.";RefreshCatalog();if(products.Count>0){Quantity=products[0].DefaultAmount;RefreshDetails();}GameplayInput.ConsumeCurrentFrame();
 }
 public void Close()
 {
  if(uiRoot!=null)uiRoot.SetActive(false);
  if(Active!=this)return;Active=null;player?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);
  Cursor.visible=cursorVisible;Cursor.lockState=cursorLock;GameplayInput.ConsumeCurrentFrame();
 }
 void OnDisable()=>Close();void OnDestroy()=>Close();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Active=null;
 public void Preview(){if(catalog==null||uiRoot==null)return;Bind();uiRoot.SetActive(true);RefreshCatalog();}
 public void SelectTab(int index){if(catalog==null||index<0||index>=catalog.tabs.Count)return;SelectedTab=index;SelectedIndex=0;Quantity=1;RefreshCatalog();if(products.Count>0){Quantity=products[0].DefaultAmount;RefreshDetails();}}
 public void SelectProduct(int index){if(index<0||index>=products.Count)return;SelectedIndex=index;Quantity=products[index].DefaultAmount;RefreshCatalog();}
 void Move(int step){if(products.Count==0)return;SelectedIndex=(SelectedIndex+step+products.Count)%products.Count;Quantity=products[SelectedIndex].DefaultAmount;RefreshCatalog();}
 public void SetQuantity(int value){Quantity=Mathf.Clamp(value,1,products.Count>0?products[SelectedIndex].MaximumAmount:99);RefreshDetails();}
 public void RefreshCatalog()
 {
  if(catalog==null||catalog.tabs.Count==0)return;
  Resolve();SelectedTab=Mathf.Clamp(SelectedTab,0,catalog.tabs.Count-1);var tab=catalog.tabs[SelectedTab];products.Clear();
  if(tab.sellInventory&&inventory!=null){foreach(var item in inventory.slots.Where(s=>s?.item!=null&&s.count>0&&s.item.CanSellAtMarket).Select(s=>s.item).Distinct())products.Add(new ShopFrontProduct(item,true));}
  else {foreach(var item in tab.items.Where(i=>i!=null).Distinct())products.Add(new ShopFrontProduct(item,false,catalog.BuyPrice(item),catalog.Settings(item)));foreach(var offer in tab.animals.Where(a=>a!=null))products.Add(new ShopFrontProduct(offer));}
  SelectedIndex=Mathf.Clamp(SelectedIndex,0,Mathf.Max(0,products.Count-1));if(products.Count>0)Quantity=Mathf.Clamp(Quantity,1,products[SelectedIndex].MaximumAmount);title.text=catalog.displayName;
  Paint(background,tab.background!=null?tab.background:catalog.theme!=null?catalog.theme.background:null,new Color(.09f,.14f,.12f,.98f),false);Paint(shopIcon,catalog.theme?.shopIcon,Color.clear);Paint(merchantPortrait,catalog.theme?.merchantPortrait,Color.clear);Paint(goldIcon,catalog.theme?.goldIcon,Color.clear);
  var panelColor=catalog.theme!=null?catalog.theme.panelColor:new Color(.20f,.25f,.21f,.72f);
  Paint(headerSurface,catalog.theme?.headerPanel,panelColor,false);Paint(detailSurface,catalog.theme?.detailPanel,panelColor,false);Paint(transactionSurface,catalog.theme?.transactionPanel,panelColor,false);Paint(buyButton.GetComponent<Image>(),catalog.theme?.buyButton,Accent,false);
  for(int i=0;i<tabs.Count;i++){tabs[i].gameObject.SetActive(i<catalog.tabs.Count);if(i>=catalog.tabs.Count)continue;tabs[i].GetComponentInChildren<TMP_Text>().text=catalog.tabs[i].title;Paint(tabs[i].GetComponent<Image>(),catalog.theme?.categoryTab,i==SelectedTab?Accent:new Color(.23f,.30f,.26f,.90f),false);}
  int page=cards.Count==0?0:SelectedIndex/cards.Count;
  for(int i=0;i<cards.Count;i++)
  {
   int index=page*cards.Count+i;bool valid=index<products.Count;cards[i].gameObject.SetActive(valid);cards[i].interactable=valid;
   var marker=cards[i].transform.Find("SelectionMarker");if(marker!=null)marker.gameObject.SetActive(valid&&index==SelectedIndex);
   Paint(cards[i].GetComponent<Image>(),index==SelectedIndex?catalog.theme?.selectedProductCard:catalog.theme?.productCard,index==SelectedIndex?Accent:new Color(.23f,.30f,.26f,.88f),false);
   cardLabels[i].text=valid?$"{products[index].Name}\n{products[index].Price:N0} G":"";
   Paint(cardImages[i],valid?products[index].Icon??catalog.theme?.productPlaceholder??ShopPresentation.Thumbnail(products[index]):null,Color.clear);
  }
  RefreshDetails();
 }
 Color Accent=>catalog?.theme!=null?catalog.theme.accentColor:new Color(.34f,.65f,.42f,1);
 void Paint(Image image,Sprite sprite,Color fallback,bool aspect=true){if(image==null)return;bool backdrop=image==background;image.sprite=sprite!=null?sprite:!aspect&&!backdrop?ShopPresentation.PanelSprite:null;image.type=sprite==null&&!aspect&&!backdrop?Image.Type.Sliced:Image.Type.Simple;image.color=sprite!=null?Color.white:fallback;image.preserveAspect=aspect;if(backdrop)GameplayHUDStyle.FullScreenBackground(image);}
 public bool CanPurchase
 {
  get
  {
   if(manager==null||products.Count==0||inventory==null||ScoreManager.Instance==null)return false;
   var p=products[SelectedIndex];bool locked=p.RequiredLevel>1&&(VillageProgressionService.Instance==null||!VillageProgressionService.Instance.MeetsRequirement(p.RequiredLevel));
   if(locked||p.Price<0)return false;
   if(p.selling)return p.item.CanSellAtMarket&&inventory.GetCount(p.item)>=Quantity;
   if((long)p.Price*Quantity>ScoreManager.Instance.points)return false;
   if(p.animal!=null)return AnimalHome.Active.Where(h=>h!=null&&h.HasRoom(p.animal.animalType)).Sum(h=>h.AvailableSlots)>=Quantity;
   if(p.item.IsFishingBait){var fishing=player.GetComponent<FishingSystem>();if(!ProgressionRequirementSettings.MeetsFishingLevel(fishing!=null?fishing.FishingLevel:1,p.item.requiredFishingLevel))return false;}
   return Quantity<=p.MaximumAmount&&inventory.CanAdd(p.item,Quantity,p.Quality);
  }
 }
 static int GrowthDays(AnimalShopOffer offer) => offer.offerKind==AnimalShopOfferKind.Egg ? (offer.growthProfile!=null?offer.growthProfile.incubationOrPregnancyDays+offer.growthProfile.bornToAdultDays:7+AnimalGrowthProfileSO.DefaultBornToAdultDays(offer.animalType)) : offer.growthProfile!=null?offer.growthProfile.purchasedYoungToAdultDays:AnimalGrowthProfileSO.DefaultPurchasedToAdultDays(offer.animalType);
 void RefreshDetails()
 {
  if(productName==null)return;int money=ScoreManager.Instance!=null?ScoreManager.Instance.points:0;gold.text=$"{money:N0} G";quantityText.text=Quantity.ToString();
  bool valid=products.Count>0;buyButton.interactable=valid&&CanPurchase;
  var availability=ShopPresentation.ContentRoot(this).Find("Availability")?.GetComponent<TMP_Text>();
  if(availability!=null)availability.text=!valid?"Belum ada barang di kategori ini.":CanPurchase?"Siap diproses. Pilih jumlah lalu konfirmasi.":"Belum dapat diproses. Periksa saldo, kapasitas, dan syarat level.";
  previousButton.interactable=nextButton.interactable=products.Count>1;
  minusButton.interactable=Quantity>1;plusButton.interactable=valid&&Quantity<(valid?products[SelectedIndex].MaximumAmount:99);
  if(!valid){productName.text="Belum ada produk";description.text="Kategori ini belum memiliki barang tersedia.";Paint(heroImage,null,Color.clear);unitPrice.text=totalPrice.text=balance.text=capacity.text="";return;}
  var p=products[SelectedIndex];Paint(heroImage,p.Icon??catalog.theme?.productPlaceholder??ShopPresentation.Thumbnail(p),Color.clear);productName.text=p.Name;
  description.text=p.animal!=null?$"Usia: {(p.animal.offerKind==AnimalShopOfferKind.Egg?"Telur / inkubasi":"Anakan")}\nDewasa dalam: {GrowthDays(p.animal)} hari pertumbuhan\nHasil dewasa: {p.animal.productItem?.itemName??AnimalCareCatalog.Load()?.Product(p.animal.animalType)?.itemName??"Produk ternak"}\nPertumbuhan membutuhkan pakan, kondisi sehat, dan kandang.":!string.IsNullOrWhiteSpace(p.item.inventoryDescription)?p.item.inventoryDescription:$"{p.item.itemName}\nDi inventory: {inventory?.GetCount(p.item)??0}";
  if(p.animal==null&&!p.selling&&p.Quality>0)description.text+=$"\nKualitas: {p.Quality} / 5";
  if(p.RequiredLevel>1)description.text+=$"\nSyarat Village Lv.{p.RequiredLevel}";
  long total=(long)p.Price*Quantity;unitPrice.text=$"{p.Price:N0} G × {Quantity}";totalPrice.text=$"Total  {total:N0} G";balance.text=!p.selling&&money<total?$"Saldo kurang: {total-money:N0} G":$"Sisa uang: {(p.selling?money+total:money-total):N0} G";
  if(p.animal!=null){var homes=AnimalHome.Active.Where(h=>h!=null&&h.Accepts(p.animal.animalType)).ToList();int free=homes.Sum(h=>h.AvailableSlots);int max=homes.Sum(h=>h.Capacity);capacity.text=max==0?"Belum ada kandang yang sesuai.":$"Kapasitas kandang: {max-free} / {max} → {max-free+Quantity} / {max}";}
  else capacity.text=p.selling?$"Dimiliki: {inventory?.GetCount(p.item)??0}":$"Di inventory: {inventory?.GetCount(p.item)??0}";
  buyButton.GetComponentInChildren<TMP_Text>().text=p.selling?"Jual":"Beli";
 }
 public bool Purchase()
 {
  Resolve();if(!IsOpen||!CanPurchase){if(feedback!=null)feedback.text="Cek saldo, slot, jumlah barang, atau level yang diperlukan.";return false;}
  var p=products[SelectedIndex];int count=0;
  if(p.animal!=null){for(int i=0;i<Quantity;i++){if(!manager.TryBuyAnimal(p.animal))break;count++;}}
  else if(p.selling?manager.TrySellItem(p.item,Quantity):p.item.IsFishingBait?manager.TryBuyBait(p.item,Quantity):manager.TryBuyItem(p.item,Quantity))count=Quantity;
  feedback.text=count>0?$"{(p.selling?"Terjual":"Dibeli")}: {p.Name} × {count}"+(count<Quantity?". Sisanya gagal; saldo hanya dipotong untuk yang berhasil.":""):"Transaksi gagal. Inventory atau kandang mungkin penuh.";
  RefreshCatalog();GameplayInput.ConsumeCurrentFrame();return count==Quantity;
 }
}
