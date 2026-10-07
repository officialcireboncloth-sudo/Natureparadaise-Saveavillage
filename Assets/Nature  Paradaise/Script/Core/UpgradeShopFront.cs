using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UpgradeShopKind { Blacksmith, Lumber }
[DisallowMultipleComponent] public sealed class UpgradeShopFront : MonoBehaviour
{
 public static UpgradeShopFront Active {get;private set;}
 public UpgradeShopKind kind;
 public BlacksmithCatalog catalog;
 public ShopFront materialShop;
 public BuildingDefinitionSO housePreviewDefinition;
 public List<Sprite> houseLevelIllustrations=new();
 public GameObject uiRoot;
 public Image background,merchantPortrait,hero;
 public TMP_Text heading,gold,selectionTitle,comparison,benefits,requirements,timing,feedback,balance;
 public Button confirm,close,materialsTab,upgradeTab;
 public List<Button> cards=new();
 public List<Image> cardImages=new();
 public List<TMP_Text> cardLabels=new();
 public float interactionRadius=2.5f;
 Inventory inventory;PlayerController player;PlayerStatusSystem status;TimeManager clock;
 string notice="";
 int index;bool bound,cursorVisible;CursorLockMode cursorLock;
 void Awake(){if(uiRoot!=null)uiRoot.SetActive(false);Bind();}
 void Resolve(){player=FindFirstObjectByType<PlayerController>();inventory=player?.GetComponent<Inventory>();status=player?.GetComponent<PlayerStatusSystem>();clock=TimeManager.Instance;}
 void Bind(){if(bound)return;bound=true;confirm?.onClick.AddListener(Confirm);close?.onClick.AddListener(Close);materialsTab?.onClick.AddListener(()=>{Close();materialShop?.Open();});upgradeTab?.onClick.AddListener(()=>Select(0));for(int n=0;n<cards.Count;n++){int i=n;cards[n].onClick.AddListener(()=>Select(i));}}
 void Update()
 {
  if(Active==this){if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)){Close();return;}if(Input.GetKeyDown(KeyCode.A))Select(index-1);if(Input.GetKeyDown(KeyCode.D))Select(index+1);if(Input.GetKeyDown(KeyCode.Return))Confirm();Refresh();return;}
  if(Active!=null||ShopFront.IsAnyOpen||MarketStand.IsAnyOpen||GameplayPauseMenu.BlocksGameplayInput)return;
  Resolve();if(player==null||!PlayerInteractionTarget.ContainsPickup(player.transform,transform,interactionRadius))return;
  WorldInteractionPrompt.Request(this,transform,"E: "+(kind==UpgradeShopKind.Blacksmith?"Pandai Besi":"Lumber Store"),Vector3.Distance(player.transform.position,transform.position),2.2f);
  if(PlayerInteractionTarget.PressPickup(player.transform,transform,KeyCode.E,interactionRadius))Open();
 }
 [ContextMenu("Open Upgrade Shop For Testing")] public void Open()
 {
  if(!Application.isPlaying){Preview();return;}
  if(uiRoot==null||GameplayPauseMenu.BlocksGameplayInput||ShopFront.IsAnyOpen||MarketStand.IsAnyOpen)return;
  if(Active!=null)Active.Close();Resolve();Bind();Active=this;index=0;
  player?.AcquireMovementLock(this);clock?.AcquirePause(this);WorldInteractionPrompt.AcquireSuppression(this);
  cursorVisible=Cursor.visible;cursorLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
  uiRoot.SetActive(true);notice="";feedback.text="";Refresh();GameplayInput.ConsumeCurrentFrame();
 }
 public void Preview(){Resolve();Bind();uiRoot.SetActive(true);Refresh();}
 public void Close(){if(uiRoot!=null)uiRoot.SetActive(false);if(Active!=this)return;Active=null;player?.ReleaseMovementLock(this);clock?.ReleasePause(this);WorldInteractionPrompt.ReleaseSuppression(this);Cursor.visible=cursorVisible;Cursor.lockState=cursorLock;GameplayInput.ConsumeCurrentFrame();}
 void OnDisable()=>Close();void OnDestroy()=>Close();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Active=null;
 void Select(int value){int count=catalog?.tools.Count??0;index=count>0?(value+count)%count:0;notice="";feedback.text="";Refresh();}
 void Paint(Image image,Sprite sprite){if(image==null)return;image.sprite=sprite;image.color=sprite!=null?Color.white:Color.clear;}
 public void Refresh()
 {
  bool smith=kind==UpgradeShopKind.Blacksmith;heading.text=smith?"PANDAI BESI":"LUMBER STORE";int money=ScoreManager.Instance!=null?ScoreManager.Instance.points:0;gold.text=$"{money:N0} G";
  Paint(background,smith?catalog?.background:background.sprite);Paint(merchantPortrait,smith?catalog?.merchantPortrait:merchantPortrait.sprite);
  BuildingLevelDefinition next=null;bool available=false;string reason="";
  if(smith)
  {
   var offer=catalog!=null&&catalog.tools.Count>0?catalog.tools[Mathf.Clamp(index,0,catalog.tools.Count-1)]:null;
   int level=offer!=null&&status!=null?status.GetToolLevel(offer.item.equippedTool):1;
   next=offer!=null&&level<offer.maximumLevel?offer.Next(level):null;
   selectionTitle.text=offer?.displayName??"Upgrade Tools";comparison.text=next!=null?$"Level {level}   →   <color=#A8D4AC>Level {next.level}</color>":$"Level {level}  ·  Maksimum";
   benefits.text="Bawa alat dan bahan yang dibutuhkan.\nAku akan meningkatkan alat pilihanmu.";
   Paint(hero,offer?.illustration??offer?.item?.inventoryIllustration??offer?.item?.icon);
   for(int n=0;n<cards.Count;n++){bool valid=n<(catalog?.tools.Count??0);cards[n].gameObject.SetActive(valid);if(!valid)continue;var t=catalog.tools[n];cardLabels[n].text=t.displayName;Paint(cardImages[n],t.illustration??t.item?.inventoryIllustration??t.item?.icon);CommerceUIStyle.Selection(cards[n],index==n);}
   available=status!=null&&next!=null&&BuildingCostUtility.CanAfford(next,inventory,out reason);if(status==null)reason="Player belum tersedia";
   timing.text="Upgrade langsung diterapkan";
  }
  else
  {
   var house=PlayerHouseController.Instance;int level=house!=null?house.CurrentLevel:1;next=house!=null?house.NextLevel:housePreviewDefinition?.GetLevel(level+1);
   selectionTitle.text=next!=null?$"UPGRADE RUMAH LEVEL {next.level}":"RUMAH LEVEL MAKSIMUM";
   comparison.text=$"Level {level}   →   <color=#A8D4AC>{(next!=null?"Level "+next.level:"MAX")}</color>";
   int target=next?.level??level;Paint(hero,target<=houseLevelIllustrations.Count?houseLevelIllustrations[target-1]:null);
   benefits.text=HouseBenefits(target);timing.text=house!=null&&house.IsUnderConstruction?$"Konstruksi berjalan · selesai hari {house.CompletionDay}":next!=null?$"Waktu pengerjaan   {next.constructionDays} hari":"Semua fasilitas rumah telah terbuka";
   available=house!=null&&!house.IsUnderConstruction&&next!=null&&BuildingCostUtility.CanAfford(next,inventory,out reason);
   if(house==null)reason="Rumah player belum tersedia";else if(house.IsUnderConstruction)reason="Upgrade sedang dikerjakan";
  }
  requirements.text=next!=null?RequirementLabel(next):"Tidak ada upgrade berikutnya";
  confirm.interactable=Application.isPlaying&&available;confirm.GetComponentInChildren<TMP_Text>().text=smith?"Upgrade Tool":"Upgrade Rumah";
  balance.text=$"Sisa uang setelah upgrade  {Mathf.Max(0,money-(next?.goldCost??0)):N0} G";
  feedback.text=!string.IsNullOrEmpty(notice)?notice:available?"Semua bahan tersedia":reason; feedback.color=available?new Color(.65f,.82f,.68f):new Color(.91f,.65f,.61f);
 }
 string RequirementLabel(BuildingLevelDefinition level)
 {
  var label=new StringBuilder("<b>BAHAN DIPERLUKAN</b>\n");
  foreach(var cost in BuildingCostUtility.Aggregate(level)){int owned=inventory!=null?inventory.GetCount(cost.Key):0;label.Append("<color=").Append(owned>=cost.Value?"#A8D4AC":"#E7A6A0").Append('>').Append(cost.Key.itemName).Append("   ").Append(owned).Append(" / ").Append(cost.Value).Append("</color>\n");}
  label.Append("<color=#EFC061>Biaya   ").Append(level.goldCost.ToString("N0")).Append(" G</color>");
  if(level.requiredVillageLevel>0)label.Append("\nDesa Level ").Append(level.requiredVillageLevel);
  return label.ToString();
 }
 public static string HouseBenefits(int level)=>level<=2?"YANG KAMU DAPATKAN\n\nTV & kulkas\nPenyimpanan makanan terpisah\nRuang makan dan kasur standar":level==3?"YANG KAMU DAPATKAN\n\nKitchen set\nKamar terpisah\nAquarium\nLemari perkakas":level==4?"YANG KAMU DAPATKAN\n\nKasur jumbo\nKamar anak\nDapur & aquarium\nLemari perkakas":"YANG KAMU DAPATKAN\n\nTV & kulkas maksimum\nAquarium jumbo\nKasur jumbo & kamar anak\nKitchen set & lemari perkakas";
 public void Confirm()
 {
  if(Active!=this)return;Resolve();
  if(kind==UpgradeShopKind.Lumber){var house=PlayerHouseController.Instance;if(house!=null&&house.TryStartNextUpgrade(out var reason))notice="Upgrade rumah dimulai.";else notice=house!=null?HouseFailure(house):"Rumah player belum tersedia";}
  else if(catalog!=null&&index<catalog.tools.Count&&status!=null){var offer=catalog.tools[index];if(BlacksmithUpgradeService.TryUpgrade(offer,status,inventory,out var reason)){SaveManager.Instance?.SaveGame();notice="Alat berhasil ditingkatkan.";}else notice=reason??"Transaksi gagal";}
  Refresh();GameplayInput.ConsumeCurrentFrame();
 }
 string HouseFailure(PlayerHouseController house)=>house.IsUnderConstruction?"Upgrade sedang dikerjakan":BuildingCostUtility.CanAfford(house.NextLevel,inventory,out var reason)?"Transaksi gagal":reason;
}


