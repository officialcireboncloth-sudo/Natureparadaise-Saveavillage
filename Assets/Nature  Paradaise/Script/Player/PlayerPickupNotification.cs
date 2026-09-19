using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Notifikasi item ringan di atas player. Object teks dipakai ulang agar pickup beruntun
/// tidak membuat alokasi dan Destroy berulang pada mobile.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerPickupNotification : MonoBehaviour
{
    sealed class Entry
    {
        public GameObject root;
        public TextMesh text;
        public float age;
        public float lane;
        public bool active;
    }

    const float Duration = 1.75f;
    const int PoolSize = 5;
    readonly List<Entry> entries = new();
    Camera targetCamera;
    float playerTop = 2.4f;

    public static void Show(Transform player, string message)
    {
        if(player==null || string.IsNullOrWhiteSpace(message)) return;
        PlayerPickupNotification notifications=player.GetComponent<PlayerPickupNotification>();
        if(notifications==null) notifications=player.gameObject.AddComponent<PlayerPickupNotification>();
        notifications.ShowInternal(message);
    }

    public static void ShowItem(Inventory inventory, ItemSO item, int amount)
    {
        if(inventory==null || item==null || amount<=0) return;
        Show(inventory.transform,$"+{amount} {item.itemName}");
    }

    void Awake() => RefreshPlayerTop();

    void ShowInternal(string message)
    {
        RefreshPlayerTop();
        Entry entry=null;
        foreach(Entry candidate in entries)
            if(!candidate.active) {entry=candidate;break;}
        if(entry==null && entries.Count<PoolSize)
        {
            entry=CreateEntry(entries.Count);
            entries.Add(entry);
        }
        if(entry==null)
        {
            entry=entries[0];
            foreach(Entry candidate in entries) if(candidate.age>entry.age) entry=candidate;
        }

        int activeCount=0;
        foreach(Entry candidate in entries) if(candidate.active && candidate!=entry) activeCount++;
        entry.age=0f;
        entry.lane=activeCount*0.28f;
        entry.active=true;
        entry.root.SetActive(true);
        entry.text.text=message;
        entry.text.color=new Color(1f,0.92f,0.32f,1f);
        Position(entry);
    }

    Entry CreateEntry(int index)
    {
        GameObject owner=new($"PickupNotification_{index+1:00}");
        owner.transform.SetParent(transform,false);
        TextMesh text=owner.AddComponent<TextMesh>();
        text.anchor=TextAnchor.LowerCenter;
        text.alignment=TextAlignment.Center;
        text.fontSize=58;
        text.characterSize=0.075f;
        text.fontStyle=FontStyle.Bold;
        text.color=new Color(1f,0.92f,0.32f,1f);
        MeshRenderer renderer=owner.GetComponent<MeshRenderer>();
        if(renderer!=null) renderer.sortingOrder=200;
        owner.SetActive(false);
        return new Entry {root=owner,text=text};
    }

    void Update()
    {
        float delta=Time.unscaledDeltaTime;
        foreach(Entry entry in entries)
        {
            if(!entry.active) continue;
            entry.age+=delta;
            if(entry.age>=Duration)
            {
                entry.active=false;
                entry.root.SetActive(false);
                continue;
            }
            float normalized=entry.age/Duration;
            float alpha=normalized<0.55f ? 1f : 1f-(normalized-0.55f)/0.45f;
            entry.text.color=new Color(1f,0.92f,0.32f,Mathf.Clamp01(alpha));
            Position(entry);
        }
    }

    void Position(Entry entry)
    {
        if(targetCamera==null) targetCamera=Camera.main;
        float rise=Mathf.SmoothStep(0f,0.85f,entry.age/Duration);
        entry.root.transform.position=transform.position+Vector3.up*(playerTop+entry.lane+rise);
        if(targetCamera!=null)
            entry.root.transform.rotation=Quaternion.LookRotation(
                entry.root.transform.position-targetCamera.transform.position,targetCamera.transform.up);
    }

    void RefreshPlayerTop()
    {
        float highest=transform.position.y+2.4f;
        foreach(Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if(renderer==null || renderer.GetComponent<TextMesh>()!=null) continue;
            highest=Mathf.Max(highest,renderer.bounds.max.y);
        }
        playerTop=Mathf.Clamp(highest-transform.position.y+0.25f,1.8f,4.8f);
    }
}
