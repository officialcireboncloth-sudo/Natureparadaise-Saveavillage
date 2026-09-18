using System.Collections.Generic;
using UnityEngine;

/// <summary>Controller scene BarnInterior: layout per level, exit, management, dan slot hewan.</summary>
[DisallowMultipleComponent]
public sealed class BarnInteriorSceneController : MonoBehaviour
{
    public static BarnInteriorSceneController Instance { get; private set; }
    [SerializeField] List<GameObject> levelLayouts = new();
    [SerializeField] Transform exitDoor;
    [SerializeField] Transform[] animalSpots;
    [SerializeField, Min(1f)] float interactionRadius = 3.5f;
    PlayerController player;

    void Awake() => Instance=this;
    void OnDestroy() { if(Instance==this) Instance=null; }
    void Start() => RefreshLayout();

    public void Configure(List<GameObject> layouts,Transform exit,Transform[] spots)
    {
        levelLayouts=layouts??new List<GameObject>();
        exitDoor=exit;
        animalSpots=spots;
    }

#if UNITY_EDITOR
    /// <summary>Dipakai setup Editor setelah layout scene diubah menjadi prefab instance.</summary>
    public void EditorConfigureLayouts(List<GameObject> layouts)
    {
        levelLayouts=layouts??new List<GameObject>();
    }
#endif

    public void RefreshLayout()
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        int level=home?.site!=null ? Mathf.Clamp(home.site.CurrentLevel,1,4) : 1;
        for(int i=0;i<levelLayouts.Count;i++) if(levelLayouts[i]!=null)
            levelLayouts[i].SetActive(i==level-1);
        LayoutAnimalSpots(home,level);
    }

    void LayoutAnimalSpots(AnimalHome home,int level)
    {
        int capacity=home!=null ? Mathf.Max(1,home.Capacity) : new[]{4,8,14,20}[level-1];
        int columns=level==1 ? 2 : level==2 ? 4 : 5;
        float spacing=level==1 ? 3.5f : level==2 ? 4f : level==3 ? 4.5f : 5f;
        float startX=-(columns-1)*spacing*0.5f;
        if(animalSpots==null) return;
        // Scene lama menyimpan container slot di Y=-200 untuk menyembunyikan dummy.
        // Routine memakai posisi slot ini secara nyata, jadi pulihkan container ke lantai.
        if(animalSpots.Length>0 && animalSpots[0]!=null && animalSpots[0].parent!=null)
            animalSpots[0].parent.localPosition=Vector3.zero;
        for(int i=0;i<animalSpots.Length;i++)
        {
            if(animalSpots[i]==null) continue;
            animalSpots[i].gameObject.SetActive(i<capacity);
            if(i<capacity) animalSpots[i].localPosition=new Vector3(startX+(i%columns)*spacing,0.1f,2+(i/columns)*spacing);
        }
    }

    public Vector3 AnimalPosition(AnimalRoutine animal)
    {
        AnimalHome home=BarnInterior.Current!=null ? BarnInterior.Current.home : null;
        int index=home!=null ? home.Residents.IndexOf(animal) : -1;
        if(animalSpots!=null && index>=0 && index<animalSpots.Length && animalSpots[index]!=null)
            return animalSpots[index].position;
        return transform.position+new Vector3(0f,0.2f,2f);
    }

    void Update()
    {
        if(player==null) player=FindFirstObjectByType<PlayerController>();
        if(player==null || exitDoor==null || BarnInterior.Current==null) return;
        if(!PlayerInteractionTarget.ContainsPickup(player.transform,exitDoor,interactionRadius)) return;
        float distance=Vector3.Distance(player.transform.position,exitDoor.position);
        WorldInteractionPrompt.Request(this,exitDoor,"E: Keluar Kandang | I: Kelola Hewan",distance,1.5f);
        if(PlayerInteractionTarget.PressPickup(player.transform,exitDoor,KeyCode.E,interactionRadius)) BarnInterior.Current.Leave();
        else if(PlayerInteractionTarget.PressPickup(player.transform,exitDoor,KeyCode.I,interactionRadius))
            AnimalCarePanel.Show(null,BarnInterior.Current.home,player.GetComponent<Inventory>());
    }
}
