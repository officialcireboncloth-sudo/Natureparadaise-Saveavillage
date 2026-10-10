using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/Player/Tool Visual Catalog")]
public sealed class PlayerToolVisualCatalog : ScriptableObject
{
    public const string ResourcePath="Player/Player Tool Visual Catalog";
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public PlayerToolType tool;
        [Tooltip("Prefab dalam meter, pivot tepat pada genggaman.")]
        public GameObject prefab;
        [Tooltip("Offset dari pusat telapak dalam meter, mengikuti rotasi RightHand. Pivot prefab harus berada pada gagang.")]
        public Vector3 handPosition=new(.025f,0f,0f);
        public Vector3 handEuler=new(0f,-90f,90f);
        [Tooltip("Rotasi lokal saat dibawa untuk workGrip, relatif titik genggaman tangan. Alat tetap mengikuti ayunan walk/run.")]
        public Vector3 carryHandEuler=new(0f,-90f,90f);
        [Min(.01f)] public float scale=1f;
        [Tooltip("Gagang lokal +Z menuju kepala; +Y mata pacul/kapak atau muka palu; khusus sabit +X normal bidang bilah. Pose dibedakan antara membawa dan memakai.")]
        public bool workGrip;
        [Tooltip("Gunakan tangan kiri pada secondHandGrip selama aksi; sabit tetap satu tangan.")]
        public bool twoHandsDuringAction=true;
        public bool twoHandsWhileCarrying;
        [Tooltip("Titik genggaman tangan kiri pada prefab, dalam meter sebelum scale.")]
        public Vector3 secondHandGrip=new(0f,0f,.25f);
        [Tooltip("Arah gagang saat membawa, relatif arah player.")]
        public Vector3 carryDirection=new(0f,-.75f,.66f);
    }
    public List<Entry> entries=new();
    public Entry Find(PlayerToolType tool)=>tool==PlayerToolType.None?null:entries.Find(x=>x!=null && x.tool==tool && x.prefab!=null);
    public Entry Find(string id)=>entries.Find(x=>x!=null && x.id==id && x.prefab!=null);
}
