using TMPro;
using UnityEngine;
[ExecuteAlways] public sealed class ShopWorldLabel : MonoBehaviour
{
 void LateUpdate(){var camera=Camera.main;if(camera!=null)transform.rotation=camera.transform.rotation;}
}
