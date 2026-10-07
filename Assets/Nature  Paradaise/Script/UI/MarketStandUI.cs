using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared typography and surfaces for commerce screens, independent of artwork.</summary>
public static class CommerceUIStyle
{
 public static readonly Color Ink=GameplayHUDStyle.Modal, Surface=new(.16f,.23f,.26f,.95f), Card=GameplayHUDStyle.Card;
 public static readonly Color TextColor=GameplayHUDStyle.TextColor, Muted=GameplayHUDStyle.Muted, Gold=new(.94f,.75f,.38f), Sage=new(.38f,.54f,.43f);
 public static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)=>GameplayHUDStyle.Rect(name,parent,new Vector2(x,y),new Vector2(x+w,y+h));
 public static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
 {var r=Rect(name,parent,x,y,w,h);var image=GameplayHUDStyle.Surface(r,color,10);image.borderColor=new Color(.66f,.74f,.73f,.16f);image.borderWidth=1;return image;}
 public static TMP_Text Label(string name,Transform parent,string value,float size,float x,float y,float w,float h,bool bold=false)
 {var t=GameplayHUDStyle.Text(name,parent,value,size,new Vector2(x,y),new Vector2(x+w,y+h));t.color=TextColor;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;return t;}
 public static Button Button(string name,Transform parent,string value,float x,float y,float w,float h,bool primary=false)
 {var image=Panel(name,parent,x,y,w,h,primary?Sage:Card);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};var colors=b.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.75f,.85f,.8f);colors.disabledColor=new Color(.55f,.6f,.6f,.55f);b.colors=colors;GameplayHUDStyle.ButtonStates(b);Label("Label",image.transform,value,21,.06f,.08f,.88f,.84f,true).alignment=TextAlignmentOptions.Center;return b;}
 public static Image ImageSlot(string name,Transform parent,float x,float y,float w,float h)
 {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;image.color=Color.clear;return image;}
 public static void Selection(Button button,bool selected)
 {var image=button.GetComponent<MainMenuRoundedImage>();image.color=selected?new Color(.26f,.37f,.36f,.98f):Card;image.borderColor=selected?new Color(.73f,.86f,.73f,.9f):new Color(.66f,.74f,.73f,.16f);image.borderWidth=selected?1.5f:1;image.SetVerticesDirty();}
 public static void Icon(Image image,ItemSO item)
 {var sprite=item!=null?item.inventoryIllustration??item.icon??ShopPresentation.Thumbnail(new ShopFrontProduct(item)):null;image.sprite=sprite;image.color=sprite!=null?Color.white:Color.clear;}
}

public sealed class MarketStandUI:MonoBehaviour
{
 MarketStand stand;GameObject root;TMP_Text status,revenue,stock,detail,price,amount,interest,notice,bagPage,standPage;Image hero;
 readonly List<int> inventorySlots=new();readonly List<Button> bagCards=new(),listingCards=new();readonly List<TMP_Text> bagLabels=new(),listingLabels=new();readonly List<Image> bagImages=new(),listingImages=new();
 Button deposit,withdraw,increase,decrease;bool listingFocus;int bagIndex,listingIndex,quantity=1;float nextRefresh;
 public Image backgroundImageSlot;
 public void Show(MarketStand value){stand=value;if(root==null)Build();root.SetActive(true);bagIndex=listingIndex=0;listingFocus=false;quantity=1;Refresh();}
 public void Hide(){if(root!=null)root.SetActive(false);}
 void OnDestroy(){if(root!=null)Destroy(root);}
 void Update()
 {
  if(root==null||!root.activeSelf)return;
  if(Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))MoveBag(-1);
  if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow))MoveBag(1);
  if(Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))MoveListing(-1);
  if(Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))MoveListing(1);
  if(Input.GetKeyDown(KeyCode.Return)&&deposit.interactable)Deposit();
  if(Input.GetKeyDown(KeyCode.R)&&withdraw.interactable)Withdraw();
  if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.KeypadPlus)){quantity++;Refresh();}
  if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus)){quantity=Mathf.Max(1,quantity-1);Refresh();}
  if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;Refresh();}
 }
 void Build()
 {
  root=new GameObject("MarketStand_UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=660;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  var safe=CommerceUIStyle.Rect("SafeArea",root.transform,0,0,1,1);safe.gameObject.AddComponent<SafeAreaFitter>();
  backgroundImageSlot=CommerceUIStyle.ImageSlot("MarketBackground_ImageSlot",root.transform,0,0,1,1);
  GameplayHUDStyle.FullScreenBackground(backgroundImageSlot);
  var shade=CommerceUIStyle.Rect("ModalScrim",root.transform,0,0,1,1).gameObject.AddComponent<Image>();shade.color=new Color(.04f,.08f,.10f,.42f);shade.raycastTarget=true;
  shade.transform.SetSiblingIndex(1);
  var p=CommerceUIStyle.Panel("StandPanel",safe,.405f,.035f,.575f,.93f,CommerceUIStyle.Ink).transform;
  CommerceUIStyle.Label("Title",p,"STAND PASAR",34,.04f,.91f,.53f,.06f,true);
  status=CommerceUIStyle.Label("OpeningHours",p,"",18,.04f,.875f,.51f,.04f);status.color=CommerceUIStyle.Muted;
  CommerceUIStyle.Panel("RevenuePanel",p,.61f,.883f,.35f,.08f,CommerceUIStyle.Surface);
  CommerceUIStyle.Label("RevenueLabel",p,"TOTAL PENDAPATAN",14,.635f,.927f,.30f,.025f).color=CommerceUIStyle.Muted;
  revenue=CommerceUIStyle.Label("Revenue",p,"",27,.635f,.89f,.30f,.04f,true);revenue.color=CommerceUIStyle.Gold;
  CommerceUIStyle.Panel("BagSection",p,.025f,.68f,.95f,.185f,CommerceUIStyle.Surface);
  CommerceUIStyle.Label("BagHeading",p,"BARANG DI TAS",19,.045f,.821f,.56f,.035f,true);
  bagPage=CommerceUIStyle.Label("BagPage",p,"",15,.77f,.821f,.18f,.035f);bagPage.alignment=TextAlignmentOptions.Right;
  for(int i=0;i<6;i++){int n=i;var b=CommerceUIStyle.Button("BagSlot_"+i,p,"",.045f+i*.153f,.695f,.143f,.12f);b.onClick.AddListener(()=>{bagIndex=(bagIndex/6)*6+n;listingFocus=false;quantity=1;Refresh();});bagCards.Add(b);b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);bagImages.Add(CommerceUIStyle.ImageSlot("Product_ImageSlot",b.transform,.1f,.34f,.8f,.6f));var label=CommerceUIStyle.Label("NameCount",b.transform,"",16,.04f,.025f,.92f,.32f);label.alignment=TextAlignmentOptions.Center;bagLabels.Add(label);}
  CommerceUIStyle.Button("BagPrevious",p,"‹",.005f,.735f,.032f,.05f).onClick.AddListener(()=>MoveBag(-6));CommerceUIStyle.Button("BagNext",p,"›",.963f,.735f,.032f,.05f).onClick.AddListener(()=>MoveBag(6));
  CommerceUIStyle.Panel("ListingsSection",p,.025f,.36f,.95f,.30f,CommerceUIStyle.Surface);
  stock=CommerceUIStyle.Label("ListingsHeading",p,"",19,.045f,.616f,.63f,.035f,true);
  standPage=CommerceUIStyle.Label("ListingPage",p,"",15,.77f,.616f,.18f,.035f);standPage.alignment=TextAlignmentOptions.Right;
  for(int i=0;i<6;i++){int n=i;var b=CommerceUIStyle.Button("DisplaySlot_"+i,p,"",.045f+(i%3)*.307f,.386f+(1-i/3)*.105f,.295f,.096f);b.onClick.AddListener(()=>{listingIndex=(listingIndex/6)*6+n;listingFocus=true;quantity=1;Refresh();});listingCards.Add(b);b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);listingImages.Add(CommerceUIStyle.ImageSlot("Product_ImageSlot",b.transform,.025f,.13f,.23f,.74f));listingLabels.Add(CommerceUIStyle.Label("ListingInfo",b.transform,"",18,.28f,.09f,.69f,.82f));}
  CommerceUIStyle.Button("ListingPrevious",p,"‹",.69f,.616f,.03f,.033f).onClick.AddListener(()=>MoveListing(-6));CommerceUIStyle.Button("ListingNext",p,"›",.725f,.616f,.03f,.033f).onClick.AddListener(()=>MoveListing(6));
  CommerceUIStyle.Panel("DetailSection",p,.025f,.16f,.95f,.18f,CommerceUIStyle.Surface);
  hero=CommerceUIStyle.ImageSlot("SelectedProduct_ImageSlot",p,.045f,.18f,.12f,.135f);
  detail=CommerceUIStyle.Label("SelectedProduct",p,"",23,.18f,.284f,.46f,.04f,true);
  price=CommerceUIStyle.Label("SalePrice",p,"",19,.18f,.242f,.43f,.035f);price.color=CommerceUIStyle.Gold;
  CommerceUIStyle.Label("QuantityLabel",p,"Jumlah",18,.18f,.197f,.25f,.033f);
  (decrease=CommerceUIStyle.Button("Decrease",p,"−",.425f,.194f,.045f,.039f)).onClick.AddListener(()=>{quantity=Mathf.Max(1,quantity-1);Refresh();});
  amount=CommerceUIStyle.Label("Quantity",p,"",21,.477f,.194f,.07f,.039f,true);amount.alignment=TextAlignmentOptions.Center;
  (increase=CommerceUIStyle.Button("Increase",p,"+",.55f,.194f,.045f,.039f)).onClick.AddListener(()=>{quantity++;Refresh();});
  deposit=CommerceUIStyle.Button("Deposit",p,"Pajang Barang",.68f,.249f,.27f,.056f,true);deposit.onClick.AddListener(Deposit);
  withdraw=CommerceUIStyle.Button("Withdraw",p,"Ambil Kembali",.68f,.183f,.27f,.056f);withdraw.onClick.AddListener(Withdraw);
  interest=CommerceUIStyle.Label("BuyerInterest",p,"",17,.045f,.105f,.88f,.039f);interest.color=CommerceUIStyle.Muted;
  notice=CommerceUIStyle.Label("Feedback",p,"",17,.045f,.06f,.91f,.044f);
  CommerceUIStyle.Label("KeyboardGuide",p,"A/D  Barang    W/S  Slot    Enter  Pajang    R  Ambil",16,.045f,.011f,.73f,.036f).color=CommerceUIStyle.Muted;
  CommerceUIStyle.Button("Close",p,"Tutup  Esc",.80f,.012f,.15f,.038f).onClick.AddListener(()=>stand.ClosePanel());
 }
 void MoveBag(int step){listingFocus=false;if(inventorySlots.Count>0)bagIndex=(bagIndex+step%inventorySlots.Count+inventorySlots.Count)%inventorySlots.Count;quantity=1;Refresh();}
 void MoveListing(int step){listingFocus=true;quantity=1;int count=Mathf.Max(1,stand.Listings.Count);listingIndex=(listingIndex+step%count+count)%count;Refresh();}
 void Deposit(){if(bagIndex<inventorySlots.Count)stand.TryDepositFromSlot(inventorySlots[bagIndex],quantity);Refresh();GameplayInput.ConsumeCurrentFrame();}
 void Withdraw(){if(listingIndex<stand.Listings.Count)stand.TryWithdraw(listingIndex,Mathf.Min(quantity,stand.Listings[listingIndex].count));Refresh();GameplayInput.ConsumeCurrentFrame();}
 void Refresh()
 {
  inventorySlots.Clear();var inv=stand.PlayerInventory;
  if(inv!=null)for(int i=0;i<inv.slots.Count;i++){var s=inv.GetSlot(i);if(s?.item!=null&&s.count>0&&s.item.CanSellAtMarket)inventorySlots.Add(i);}
  bagIndex=Mathf.Clamp(bagIndex,0,Mathf.Max(0,inventorySlots.Count-1));listingIndex=Mathf.Clamp(listingIndex,0,Mathf.Max(0,stand.Listings.Count-1));
  var selected=inventorySlots.Count>0?inv.GetSlot(inventorySlots[bagIndex]):null;var selectedListing=stand.Listings.Count>0?stand.Listings[listingIndex]:null;var shownItem=listingFocus?selectedListing?.item:selected?.item;quantity=Mathf.Clamp(quantity,1,Mathf.Max(1,listingFocus?selectedListing?.count??1:selected?.count??1));
  decrease.interactable=quantity>1;increase.interactable=quantity<(listingFocus?selectedListing?.count??0:selected?.count??0);
  status.text=stand.OpeningHoursLabel;revenue.text=$"{stand.LifetimeRevenue:N0} G";stock.text=$"BARANG DIPAJANG   {stand.Listings.Count}/{stand.ListingCapacity}";
  bagPage.text=$"{(inventorySlots.Count==0?0:bagIndex/6+1)} / {Mathf.CeilToInt(inventorySlots.Count/6f)}";standPage.text=$"{listingIndex/6+1} / {Mathf.Max(1,Mathf.CeilToInt(stand.ListingCapacity/6f))}";
  for(int i=0;i<6;i++)
  {int n=(bagIndex/6)*6+i;bool exists=n<inventorySlots.Count;bagCards[i].interactable=exists;CommerceUIStyle.Selection(bagCards[i],exists&&n==bagIndex&&!listingFocus);var s=exists?inv.GetSlot(inventorySlots[n]):null;CommerceUIStyle.Icon(bagImages[i],s?.item);bagLabels[i].text=s!=null?$"{s.item.itemName}\n×{s.count}":i==0&&inventorySlots.Count==0?"Tas kosong":"—";
   n=(listingIndex/6)*6+i;exists=n<stand.Listings.Count;listingCards[i].interactable=exists;CommerceUIStyle.Selection(listingCards[i],exists&&n==listingIndex&&listingFocus);var listing=exists?stand.Listings[n]:null;CommerceUIStyle.Icon(listingImages[i],listing?.item);listingLabels[i].text=listing!=null?$"{listing.item.itemName} <color=#EFC061>{new string('★',listing.qualityStars)}</color>\nTersisa {listing.count}   <color=#EFC061>{listing.UnitPrice:N0} G</color>":"+   Slot kosong";}
  CommerceUIStyle.Icon(hero,shownItem);detail.text=shownItem?.itemName??"Pilih barang dari tas";price.text=shownItem!=null?$"Harga jual   {(listingFocus?selectedListing.UnitPrice:selected.item.GetMarketSellPrice(selected.qualityStars,selected.fishSizeCm)):N0} G / item":"Barang yang belum laku tetap tersimpan.";amount.text=quantity.ToString();
  deposit.interactable=!listingFocus&&selected!=null&&stand.CanDepositFromSlot(inventorySlots[bagIndex]);withdraw.interactable=listingFocus&&stand.Listings.Count>0&&inv!=null&&inv.CanAdd(stand.Listings[listingIndex].item,Mathf.Min(quantity,stand.Listings[listingIndex].count),stand.Listings[listingIndex].qualityStars,stand.Listings[listingIndex].fishSizeCm);
  interest.text=$"Peluang pembeli per jam {stand.CurrentBuyerChance:P0}  •  Pendapatan masuk otomatis ke uangmu";
  notice.text=listingFocus?(selectedListing==null?"Stand masih kosong. Pilih barang dari tas untuk mulai memajang.":!withdraw.interactable?"Tas penuh. Kosongkan ruang sebelum mengambil barang.":"Pilih jumlah lalu Ambil Kembali untuk mengembalikan barang ke tas."):!string.IsNullOrEmpty(stand.Feedback)?stand.Feedback:selected==null?"Belum ada barang yang dapat dijual di tas.":!deposit.interactable?"Stand penuh. Ambil barang untuk membuka slot baru.":"Pilih jumlah, lalu pajang barang. Harga mengikuti kualitas produk.";
 }
}
