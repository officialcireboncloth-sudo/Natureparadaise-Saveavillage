#if UNITY_EDITOR
using UnityEditor;using UnityEngine;
[InitializeOnLoad]public static class PlayerGroundMountDriver{static PlayerGroundMountDriver(){EditorApplication.update+=()=>{if(!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();Object.FindFirstObjectByType<PlayerGroundMountCheck>()?.SendMessage("Start");};}}
#endif
