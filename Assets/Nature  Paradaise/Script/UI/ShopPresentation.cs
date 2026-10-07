using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shop-only presentation; custom artwork continues to take priority.</summary>
public static class ShopPresentation
{
 public static Transform ContentRoot(ShopFront f)=>f.uiRoot.GetComponentInChildren<SafeAreaFitter>(true)?.transform??f.uiRoot.transform;
 static Sprite panel;
 static readonly Dictionary<string,Sprite> thumbnails=new();
 public static Sprite PanelSprite
 {
  get
  {
   if(panel!=null)return panel;
   const int size=64;const float radius=14;
   var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
   var pixels=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++)
   {float dx=Mathf.Max(radius-x,x-(size-1-radius));float dy=Mathf.Max(radius-y,y-(size-1-radius));float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-d));}
   texture.SetPixels(pixels);texture.Apply();panel=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));panel.hideFlags=HideFlags.HideAndDontSave;return panel;
  }
 }
 public static Sprite Thumbnail(ShopFrontProduct product)
 {
  string key=product.Name.Replace(" ","_");
  if(!thumbnails.TryGetValue(key,out var sprite)){sprite=Resources.Load<Sprite>("UI/ShopProducts/"+key);if(sprite!=null)thumbnails[key]=sprite;}
  return sprite;
 }
 static void Place(Component c,float x,float y,float w,float h){if(c==null)return;var r=c.transform as RectTransform;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;}
 static void Surface(Image image){if(image==null)return;image.sprite=PanelSprite;image.type=Image.Type.Sliced;var shadow=image.GetComponent<Shadow>()??image.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.20f);shadow.effectDistance=new Vector2(0,-4);}
 public static void Layout(ShopFront f)
 {
  Place(f.detailSurface,.05f,.425f,.64f,.405f);Place(f.heroImage,.075f,.475f,.22f,.30f);Place(f.productName,.32f,.70f,.34f,.06f);Place(f.description,.32f,.50f,.34f,.16f);
  Place(f.transactionSurface,.735f,.12f,.24f,.71f);
  Place(f.minusButton,.85f,.665f,.035f,.05f);Place(f.quantityText,.89f,.665f,.035f,.05f);Place(f.plusButton,.93f,.665f,.035f,.05f);
  Place(f.unitPrice,.755f,.605f,.20f,.035f);Place(f.totalPrice,.755f,.545f,.20f,.05f);Place(f.capacity,.755f,.46f,.20f,.05f);Place(f.buyButton,.755f,.35f,.20f,.063f);Place(f.balance,.755f,.295f,.20f,.04f);
  var safe=ContentRoot(f);Place(safe.Find("QuantityLabel"),.755f,.665f,.09f,.05f);Place(safe.Find("PurchaseHeading"),.755f,.745f,.20f,.055f);
 }
 public static void Apply(ShopFront f)
 {
  if(f.uiRoot==null)return;
  Surface(f.headerSurface);Surface(f.detailSurface);Surface(f.transactionSurface);
  foreach(var button in f.uiRoot.GetComponentsInChildren<Button>(true))
  {Surface(button.GetComponent<Image>());button.GetComponent<Image>().color=new Color(.27f,.34f,.28f,.96f);var colors=button.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f,1);colors.pressedColor=new Color(.82f,.82f,.82f,1);colors.disabledColor=new Color(.65f,.65f,.65f,.65f);button.colors=colors;}
  foreach(var label in f.uiRoot.GetComponentsInChildren<TMP_Text>(true))label.color=new Color(.96f,.94f,.86f,1);
  f.totalPrice.color=CommerceUIStyle.Gold;f.balance.color=CommerceUIStyle.Muted;f.capacity.color=CommerceUIStyle.Muted;
  f.description.fontSize=23;f.description.fontSizeMin=20;f.description.textWrappingMode=TextWrappingModes.Normal;
  f.title.fontStyle=FontStyles.Bold;f.title.fontSize=29;f.productName.fontStyle=FontStyles.Bold;f.productName.fontSize=40;
  var safe=ContentRoot(f);
  var footer=safe.Find("FooterPanel")?.GetComponent<Image>();if(footer!=null)footer.color=new Color(.11f,.18f,.155f,.96f);
  if(safe.Find("Availability") == null)CommerceUIStyle.Label("Availability",safe,"",19,.755f,.17f,.20f,.09f).color=CommerceUIStyle.Muted;
  var availability=safe.Find("Availability")?.GetComponent<TMP_Text>();
  if(availability!=null)availability.textWrappingMode=TextWrappingModes.Normal;
  foreach(var tab in f.tabs)
  {
   var label=tab.GetComponentInChildren<TMP_Text>();
   if(label==null)continue;
   label.textWrappingMode=TextWrappingModes.NoWrap;
   label.fontSize=label.fontSizeMax=22;
   if(label.text.Length>10)
   {var r=(RectTransform)tab.transform;r.anchorMax=new Vector2(Mathf.Max(r.anchorMax.x,r.anchorMin.x+.09f),r.anchorMax.y);}
  }
  foreach(var card in f.cards)if(card.transform.Find("SelectionMarker")==null)CommerceUIStyle.Panel("SelectionMarker",card.transform,.12f,.005f,.76f,.012f,CommerceUIStyle.Sage);
  if(safe.Find("Shop_Subtitle") == null)
  {
   GameplayHUDStyle.Text("Shop_Subtitle",safe,"NATURE PARADISE  /  KATALOG TOKO",17,new Vector2(.095f,.885f),new Vector2(.38f,.911f)).color=new Color(.72f,.76f,.65f);
   GameplayHUDStyle.Text("CatalogHeading",safe,"PILIH PRODUK",19,new Vector2(.06f,.365f),new Vector2(.30f,.393f)).fontStyle=FontStyles.Bold;
   GameplayHUDStyle.Text("PurchaseHeading",safe,"PESANANMU",21,new Vector2(.755f,.745f),new Vector2(.95f,.8f)).fontStyle=FontStyles.Bold;
   GameplayHUDStyle.Icon(safe,MainMenuIcon.Kind.Coin,new Color(.88f,.72f,.34f),new Vector2(.825f,.918f),new Vector2(.847f,.95f));
  }
  GameplayHUDStyle.ApplyHierarchy(f.uiRoot);
 }
}
