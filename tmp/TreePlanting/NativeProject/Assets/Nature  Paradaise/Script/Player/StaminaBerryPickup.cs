using UnityEngine;

/// <summary>Drop this prefab anywhere; assign the shared ItemSO icon/model when ready.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SphereCollider))]
public sealed class StaminaBerryPickup : MonoBehaviour
{
    [SerializeField] ItemSO item;
    [SerializeField, Min(1)] int amount=1;
    void Start()
    {
        item ??= Resources.Load<ItemSO>("Items/Food/Stamina Berry");
        if(item==null||!item.increasesStaminaTier) { Debug.LogError("Assign a stamina berry ItemSO",this);return; }
        if(item.worldPrefab!=null)
        {
            var model=Instantiate(item.worldPrefab,transform);model.transform.localPosition=Vector3.zero;
            model.transform.localScale=item.worldScale;
            foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
        }
        var pickup=GetComponent<WorldItemPickup>();if(pickup==null)pickup=gameObject.AddComponent<WorldItemPickup>();
        pickup.Initialize(item,amount);pickup.SetDestroyTarget(gameObject);
    }
    public static bool GrantReward(Inventory inventory,int amount=1)
    {
        var berry=Resources.Load<ItemSO>("Items/Food/Stamina Berry");
        if(inventory==null||berry==null||amount<=0||!inventory.Add(berry,amount))return false;
        PlayerPickupNotification.ShowItem(inventory,berry,amount);
        QuestEventHub.Publish(QuestObjectiveType.Collect,berry.name,amount,berry);return true;
    }
    void OnDrawGizmos() { Gizmos.color=new Color(.7f,.3f,.9f);Gizmos.DrawWireSphere(transform.position,.4f); }
}
