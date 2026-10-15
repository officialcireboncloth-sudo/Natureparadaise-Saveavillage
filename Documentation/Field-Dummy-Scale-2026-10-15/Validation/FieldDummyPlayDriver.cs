#if UNITY_EDITOR
using UnityEditor;using UnityEngine;
[InitializeOnLoad]public static class FieldDummyPlayDriver{static FieldDummyPlayDriver(){EditorApplication.update+=()=>{if(!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();Object.FindFirstObjectByType<FieldDummyValidation>()?.SendMessage("Start");};}}
#endif
