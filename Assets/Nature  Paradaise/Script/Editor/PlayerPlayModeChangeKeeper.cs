using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menyimpan properti yang diedit manual lewat Inspector pada hierarchy Player selama
/// Play Mode. Perubahan runtime dari script tidak melewati Undo, jadi tidak ikut tersimpan.
/// </summary>
[InitializeOnLoad]
public static class PlayerPlayModeChangeKeeper
{
    const string EnabledKey="NatureParadise.KeepPlayerPlayModeInspectorChanges";
    const string PendingKey="NatureParadise.PendingPlayerInspectorChanges";
    static readonly Dictionary<string,ChangeRecord> Changes=new();

    [Serializable]
    sealed class ChangeBatch { public List<ChangeRecord> values=new(); }

    [Serializable]
    sealed class ChangeRecord
    {
        public string targetGlobalId;
        public string playerGlobalId;
        public string scenePath;
        public string relativeTransformPath;
        public string componentType;
        public int componentIndex;
        public string propertyPath;
        public int propertyType;
        public long integerValue;
        public double floatValue;
        public bool boolValue;
        public string stringValue;
        public string objectGlobalId;
        public Color colorValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Vector2Int vector2IntValue;
        public Vector3Int vector3IntValue;
        public Quaternion quaternionValue;
        public Rect rectValue;
        public RectInt rectIntValue;
        public Bounds boundsValue;
        public BoundsInt boundsIntValue;
        public AnimationCurve curveValue;
    }

    static PlayerPlayModeChangeKeeper()
    {
        Undo.postprocessModifications+=CaptureInspectorChanges;
        EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        Menu.SetChecked("Nature Paradise/Player/Keep Inspector Changes From Play Mode",Enabled);
        if(!EditorApplication.isPlayingOrWillChangePlaymode &&
            !string.IsNullOrEmpty(SessionState.GetString(PendingKey,string.Empty)))
            EditorApplication.delayCall+=ApplyPending;
    }

    static bool Enabled => EditorPrefs.GetBool(EnabledKey,true);

    [MenuItem("Nature Paradise/Player/Keep Inspector Changes From Play Mode",false,130)]
    static void Toggle()
    {
        bool enabled=!Enabled;
        EditorPrefs.SetBool(EnabledKey,enabled);
        Menu.SetChecked("Nature Paradise/Player/Keep Inspector Changes From Play Mode",enabled);
        Debug.Log(enabled
            ? "[PLAYER EDIT] Perubahan Inspector Player saat Play Mode akan dipertahankan."
            : "[PLAYER EDIT] Penyimpanan perubahan Inspector Play Mode dimatikan.");
    }

    static UndoPropertyModification[] CaptureInspectorChanges(UndoPropertyModification[] modifications)
    {
        if(!Enabled || !EditorApplication.isPlaying) return modifications;
        foreach(UndoPropertyModification modification in modifications)
        {
            Component component=modification.currentValue.target as Component;
            if(component==null || component.GetComponentInParent<PlayerController>(true)==null) continue;
            Capture(component,modification.currentValue.propertyPath);
        }
        return modifications;
    }

    static void Capture(Component component,string propertyPath)
    {
        PlayerController player=component.GetComponentInParent<PlayerController>(true);
        if(player==null) return;
        SerializedObject serialized=new(component);
        serialized.Update();
        SerializedProperty property=serialized.FindProperty(propertyPath);
        if(property==null || !TryRead(property,out ChangeRecord record)) return;
        record.targetGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(component).ToString();
        record.playerGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(player).ToString();
        record.scenePath=player.gameObject.scene.path;
        record.relativeTransformPath=AnimationUtility.CalculateTransformPath(component.transform,player.transform);
        record.componentType=component.GetType().AssemblyQualifiedName;
        Component[] sameType=component.GetComponents(component.GetType());
        record.componentIndex=Array.IndexOf(sameType,component);
        record.propertyPath=propertyPath;
        Changes[record.targetGlobalId+"|"+propertyPath]=record;
    }

    static bool TryRead(SerializedProperty property,out ChangeRecord value)
    {
        value=new ChangeRecord { propertyType=(int)property.propertyType };
        switch(property.propertyType)
        {
            case SerializedPropertyType.Integer: value.integerValue=property.longValue; break;
            case SerializedPropertyType.Boolean: value.boolValue=property.boolValue; break;
            case SerializedPropertyType.Float: value.floatValue=property.doubleValue; break;
            case SerializedPropertyType.String: value.stringValue=property.stringValue; break;
            case SerializedPropertyType.Color: value.colorValue=property.colorValue; break;
            case SerializedPropertyType.ObjectReference:
                if(property.objectReferenceValue!=null)
                    value.objectGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(property.objectReferenceValue).ToString();
                break;
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character: value.integerValue=property.intValue; break;
            case SerializedPropertyType.Vector2: value.vector2Value=property.vector2Value; break;
            case SerializedPropertyType.Vector3: value.vector3Value=property.vector3Value; break;
            case SerializedPropertyType.Vector4: value.vector4Value=property.vector4Value; break;
            case SerializedPropertyType.Rect: value.rectValue=property.rectValue; break;
            case SerializedPropertyType.AnimationCurve: value.curveValue=property.animationCurveValue; break;
            case SerializedPropertyType.Bounds: value.boundsValue=property.boundsValue; break;
            case SerializedPropertyType.Quaternion: value.quaternionValue=property.quaternionValue; break;
            case SerializedPropertyType.Vector2Int: value.vector2IntValue=property.vector2IntValue; break;
            case SerializedPropertyType.Vector3Int: value.vector3IntValue=property.vector3IntValue; break;
            case SerializedPropertyType.RectInt: value.rectIntValue=property.rectIntValue; break;
            case SerializedPropertyType.BoundsInt: value.boundsIntValue=property.boundsIntValue; break;
            default: return false;
        }
        return true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode) Changes.Clear();
        if(state==PlayModeStateChange.ExitingPlayMode && Changes.Count>0)
        {
            ChangeBatch batch=new() { values=Changes.Values.ToList() };
            SessionState.SetString(PendingKey,JsonUtility.ToJson(batch));
        }
        if(state==PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall+=ApplyPending;
    }

    static void ApplyPending()
    {
        string json=SessionState.GetString(PendingKey,string.Empty);
        SessionState.EraseString(PendingKey);
        if(string.IsNullOrEmpty(json)) return;
        ChangeBatch batch=JsonUtility.FromJson<ChangeBatch>(json);
        if(batch?.values==null) return;

        int applied=0;
        foreach(ChangeRecord change in batch.values)
        {
            UnityEngine.Object target=ResolveTarget(change);
            if(target==null) continue;
            SerializedObject serialized=new(target);
            serialized.Update();
            SerializedProperty property=serialized.FindProperty(change.propertyPath);
            if(property==null || !Apply(property,change)) continue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            if(target is Component component && component.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            applied++;
        }
        Changes.Clear();
        Debug.Log($"[PLAYER EDIT] {applied} perubahan Inspector dari Play Mode dipertahankan. Simpan scene dengan Ctrl+S.");
    }

    static UnityEngine.Object ResolveTarget(ChangeRecord change)
    {
        if(GlobalObjectId.TryParse(change.targetGlobalId,out GlobalObjectId targetId))
        {
            UnityEngine.Object direct=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(targetId);
            if(direct!=null) return direct;
        }

        PlayerController player=null;
        if(GlobalObjectId.TryParse(change.playerGlobalId,out GlobalObjectId playerId))
            player=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(playerId) as PlayerController;

        if(player==null)
        {
            player=Resources.FindObjectsOfTypeAll<PlayerController>()
                .FirstOrDefault(candidate => candidate.gameObject.scene.IsValid() &&
                    candidate.gameObject.scene.path==change.scenePath);
        }
        if(player==null) return null;

        Transform owner=string.IsNullOrEmpty(change.relativeTransformPath)
            ? player.transform
            : player.transform.Find(change.relativeTransformPath);
        Type type=Type.GetType(change.componentType);
        if(owner==null || type==null) return null;
        Component[] components=owner.GetComponents(type);
        return change.componentIndex>=0 && change.componentIndex<components.Length
            ? components[change.componentIndex]
            : null;
    }

    static bool Apply(SerializedProperty property,ChangeRecord value)
    {
        SerializedPropertyType type=(SerializedPropertyType)value.propertyType;
        if(property.propertyType!=type) return false;
        switch(type)
        {
            case SerializedPropertyType.Integer: property.longValue=value.integerValue; break;
            case SerializedPropertyType.Boolean: property.boolValue=value.boolValue; break;
            case SerializedPropertyType.Float: property.doubleValue=value.floatValue; break;
            case SerializedPropertyType.String: property.stringValue=value.stringValue; break;
            case SerializedPropertyType.Color: property.colorValue=value.colorValue; break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue=null;
                if(!string.IsNullOrEmpty(value.objectGlobalId) && GlobalObjectId.TryParse(value.objectGlobalId,out GlobalObjectId objectId))
                    property.objectReferenceValue=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectId);
                break;
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character: property.intValue=(int)value.integerValue; break;
            case SerializedPropertyType.Vector2: property.vector2Value=value.vector2Value; break;
            case SerializedPropertyType.Vector3: property.vector3Value=value.vector3Value; break;
            case SerializedPropertyType.Vector4: property.vector4Value=value.vector4Value; break;
            case SerializedPropertyType.Rect: property.rectValue=value.rectValue; break;
            case SerializedPropertyType.AnimationCurve: property.animationCurveValue=value.curveValue; break;
            case SerializedPropertyType.Bounds: property.boundsValue=value.boundsValue; break;
            case SerializedPropertyType.Quaternion: property.quaternionValue=value.quaternionValue; break;
            case SerializedPropertyType.Vector2Int: property.vector2IntValue=value.vector2IntValue; break;
            case SerializedPropertyType.Vector3Int: property.vector3IntValue=value.vector3IntValue; break;
            case SerializedPropertyType.RectInt: property.rectIntValue=value.rectIntValue; break;
            case SerializedPropertyType.BoundsInt: property.boundsIntValue=value.boundsIntValue; break;
            default: return false;
        }
        return true;
    }
}
