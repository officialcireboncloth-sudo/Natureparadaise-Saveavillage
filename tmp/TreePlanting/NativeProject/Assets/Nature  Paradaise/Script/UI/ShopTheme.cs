using UnityEngine;
[CreateAssetMenu(menuName="Nature Paradise/UI/Shop Theme")]
public sealed class ShopTheme:ScriptableObject
{
 [Header("Image slots — optional, shown with a neutral fallback until assigned")]
 public Sprite background,shopIcon,merchantPortrait,goldIcon,productPlaceholder;
 public Sprite headerPanel,detailPanel,transactionPanel,productCard,selectedProductCard,categoryTab,buyButton;
 [Header("Colors")]
 public Color panelColor=new(.16f,.23f,.20f,.96f);
 public Color accentColor=new(.34f,.65f,.42f,1);
}
