using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

/// <summary>Render-only, metre-sized item representations. World prefabs and their gameplay remain untouched.</summary>
public static class HeldItemVisuals
{
    /// <summary>Copies visible geometry rather than running world-prefab scripts, physics, lights or cameras in the hand.</summary>
    public static GameObject Create(ItemSO item,Transform parent,string name)
    {
        var root=new GameObject(name);root.transform.SetParent(parent,false);
        var source=item.worldPrefab;
        if(source!=null){
            var transforms=new Dictionary<Transform,Transform>{{source.transform,root.transform}};
            void Copy(Transform original,Transform target){foreach(Transform child in original){var copy=new GameObject(child.name);copy.transform.SetParent(target,false);copy.transform.localPosition=child.localPosition;copy.transform.localRotation=child.localRotation;copy.transform.localScale=child.localScale;copy.SetActive(child.gameObject.activeSelf);transforms[child]=copy.transform;Copy(child,copy.transform);}}
            Copy(source.transform,root.transform);
            var excluded=new HashSet<Renderer>();
            var selectedRenderers=new HashSet<Renderer>();
            foreach(var group in source.GetComponentsInChildren<LODGroup>(true)){
                var lods=group.GetLODs();Renderer[] selected=null;
                for(int i=lods.Length-1;i>=0;i--)if(lods[i].renderers.Any(r=>r is MeshRenderer||r is SkinnedMeshRenderer)){selected=lods[i].renderers;break;}
                if(selected==null)continue;foreach(var r in selected)if(r!=null)selectedRenderers.Add(r);foreach(var lod in lods)foreach(var r in lod.renderers)if(r!=null&&!selected.Contains(r))excluded.Add(r);
            }
            foreach(var renderer in source.GetComponentsInChildren<Renderer>(true)){
                if(excluded.Contains(renderer)||!transforms.TryGetValue(renderer.transform,out var target))continue;
                Renderer copy=null;
                if(renderer is MeshRenderer){var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;target.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;copy=target.gameObject.AddComponent<MeshRenderer>();}
                else if(renderer is SkinnedMeshRenderer skin&&skin.sharedMesh!=null){var model=target.gameObject.AddComponent<SkinnedMeshRenderer>();model.sharedMesh=skin.sharedMesh;model.bones=skin.bones.Select(b=>b!=null&&transforms.TryGetValue(b,out var bone)?bone:null).ToArray();model.rootBone=skin.rootBone!=null&&transforms.TryGetValue(skin.rootBone,out var rootBone)?rootBone:root.transform;model.localBounds=skin.localBounds;copy=model;}
                if(copy!=null){copy.enabled=renderer.enabled||selectedRenderers.Contains(renderer);copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=ShadowCastingMode.Off;copy.lightProbeUsage=LightProbeUsage.Off;}
            }
        }
        if(root.GetComponentsInChildren<Renderer>(true).Length==0){var fallback=GameObject.CreatePrimitive(item.category==ItemCategory.Fish?PrimitiveType.Sphere:PrimitiveType.Cube);fallback.name="Item mesh fallback";fallback.transform.SetParent(root.transform,false);fallback.GetComponent<Collider>().enabled=false;Object.Destroy(fallback.GetComponent<Collider>());fallback.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        root.transform.localScale=SafeScale(item.worldScale);
        return root;
    }
    static Vector3 SafeScale(Vector3 value)
    {
        if(value==Vector3.zero)return Vector3.one*.4f;
        for(int i=0;i<3;i++)if(float.IsNaN(value[i])||float.IsInfinity(value[i]))value[i]=.4f;
        return value;
    }
    /// <summary>Compensates skeleton/parent scale so offsets and item sizes are expressed in world metres.</summary>
    public static void NormalizeAnchor(Transform anchor)
    {
        if(anchor==null)return;Vector3 scale=anchor.parent!=null?anchor.parent.lossyScale:Vector3.one;
        anchor.localScale=new Vector3(1/Mathf.Max(.00001f,Mathf.Abs(scale.x)),1/Mathf.Max(.00001f,Mathf.Abs(scale.y)),1/Mathf.Max(.00001f,Mathf.Abs(scale.z)));
    }
    /// <summary>Bounds the held representation and centres its geometry on the palm without changing world placement scale.</summary>
    public static void Fit(GameObject visual,ItemSO item)
    {
        var renderers=visual.GetComponentsInChildren<Renderer>(true);bool found=false;var bounds=new Bounds();
        foreach(var renderer in renderers){
            renderer.shadowCastingMode=ShadowCastingMode.Off;if(!renderer.enabled)continue;
            bool visible=true;for(var node=renderer.transform;node!=visual.transform&&node!=null;node=node.parent)if(!node.gameObject.activeSelf){visible=false;break;}if(!visible)continue;
            var b=renderer.localBounds;var matrix=visual.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);}
        }
        if(!found)return;
        float limit=item.IsSeed?.22f:item.category==ItemCategory.Fish?.65f:.55f;
        float diameter=Vector3.Scale(bounds.size,visual.transform.localScale).magnitude;
        if(diameter>limit)visual.transform.localScale*=limit/diameter;
        visual.transform.localPosition=-(visual.transform.localRotation*Vector3.Scale(bounds.center,visual.transform.localScale));
    }
}
