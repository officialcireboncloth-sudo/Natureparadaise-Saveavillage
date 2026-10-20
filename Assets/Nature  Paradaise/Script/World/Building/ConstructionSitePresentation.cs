using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Low side screens with open exits, safe perimeter collision and cosmetic completion. No anchor movement.</summary>
public sealed class ConstructionSitePresentation
{
    readonly Transform root, anchor, site, screens;
    readonly ConstructionWorkerSettings settings;
    readonly GameObject source;
    readonly Bounds bounds;
    readonly float footprintArea;
    readonly List<Material> materials = new();
    readonly List<Mesh> groundedMeshes = new();
    readonly Dictionary<Vector2Int,float> groundHeights = new();
    readonly RaycastHit[] groundHits = new RaycastHit[64];
    readonly Dictionary<Renderer, bool> hiddenOriginals = new();
    readonly List<ParticleSystem> dust = new();
    readonly List<ParticleSystem> interiorDust = new();
    readonly List<BoxCollider> pendingBarriers = new();
    readonly Collider[] overlaps = new Collider[64];
    ParticleSystem workDust, siteDust, completionSmoke;
    Transform rising;
    Vector3 finalPosition;
    float riseDistance;
    bool revealing, hidden, wasWorking;
    public Transform Bench { get; private set; }
    public Transform Board { get; private set; }

    public ConstructionSitePresentation(Transform parent, Transform buildingAnchor, Bounds worldBounds,
        Vector3 work, GameObject finalVisual, ConstructionWorkerSettings profile,ConstructionSiteLayout? siteLayout=null)
    {
        root=parent; anchor=buildingAnchor; bounds=worldBounds; source=finalVisual; settings=profile;
        site=new GameObject("Universal_OpenTop_Site").transform;site.SetParent(root,false);
        screens=new GameObject("SideScreens_NoRoof").transform;screens.SetParent(site,false);
        var layout=siteLayout??ConstructionSiteLayout.FromWorldBounds(anchor,worldBounds);
        var local=layout.LocalBounds;
        site.SetPositionAndRotation(layout.ToWorld(new Vector3(local.center.x,0,local.center.z)),layout.Rotation);
        float clearance=profile != null ? profile.siteClearance : .65f;
        float w=Mathf.Max(2,local.size.x)+clearance*2,d=Mathf.Max(2,local.size.z)+clearance*2;
        footprintArea=w*d;
        float h=Mathf.Clamp(bounds.max.y-anchor.position.y+.3f,1.2f,profile!=null?Mathf.Max(1.2f,profile.screenHeight):1.8f);
        Physics.SyncTransforms();
        if(profile != null && profile.tarpPanel != null && profile.supportPost != null)
        {
            // Front/back openings follow the building's authored approach axis, not the camera.
            // Always open in both directions: players already inside can leave without a teleport.
            BuildSide(new Vector3(0,0,-d/2),Quaternion.identity,w,h,true);
            BuildSide(new Vector3(0,0,d/2),Quaternion.Euler(0,180,0),w,h,true);
            BuildSide(new Vector3(-w/2,0,0),Quaternion.Euler(0,90,0),d,h,false);
            BuildSide(new Vector3(w/2,0,0),Quaternion.Euler(0,-90,0),d,h,false);
        }
        Vector3 facing=site.position-work;facing.y=0;
        Quaternion rotation=facing.sqrMagnitude>.01f?Quaternion.LookRotation(facing):Quaternion.identity;
        float scale=profile != null ? profile.workerScale : 1f;
        Bench=Spawn(profile?.workBench,"BuilderWorkBench",work+rotation*Vector3.forward*(.67f*scale),rotation,Vector3.one*scale,site);
        if(Bench != null)
        {
            Spawn(profile?.lumberStack,"LumberStack",work+rotation*new Vector3(-1.15f*scale,0,.5f*scale),rotation,Vector3.one*scale,site);
            Spawn(profile?.toolCrate,"ToolCrate",work+rotation*new Vector3(1.05f*scale,0,.4f*scale),rotation,Vector3.one*scale,site);
            Board=Spawn(profile?.workingBoard,"WorkingBoard",work+rotation*new Vector3(0,1.04f*scale,.67f*scale),rotation,Vector3.one*scale,site);
        }
        BuildDust(w,d,work,rotation);
        RefreshBarriers();
    }
    static Transform Spawn(GameObject prefab,string name,Vector3 position,Quaternion rotation,Vector3 scale,Transform parent)
    {
        if(prefab==null)return null;
        var t=ConstructionProjectVisuals.VisualClone(prefab,parent).transform;t.name=name;
        t.SetPositionAndRotation(position,rotation);t.localScale=scale;return t;
    }
    void BuildSide(Vector3 center,Quaternion rotation,float length,float height,bool exit)
    {
        if(!exit){BuildSpan(center,rotation,length,height);return;}
        float gap=Mathf.Min(Mathf.Max(1.5f,settings.exitWidth),length);
        float span=(length-gap)/2;
        if(span>.05f)
        {
            BuildSpan(center+rotation*Vector3.left*(gap/2+span/2),rotation,span,height);
            BuildSpan(center+rotation*Vector3.right*(gap/2+span/2),rotation,span,height);
        }
        // Visible gap has NO panel, gate collider or invisible full-area blocker.
        var marker=new GameObject("ConstructionExit_AlwaysOpen").transform;marker.SetParent(screens,false);
        marker.SetPositionAndRotation(OnGround(site.TransformPoint(center)),site.rotation*rotation);
    }
    float GroundHeight(Vector3 point)
    {
        var key=new Vector2Int(Mathf.RoundToInt(point.x*20),Mathf.RoundToInt(point.z*20));
        if(groundHeights.TryGetValue(key,out float cached))return cached;
        // Terrain sampling works even when unfinished-building colliders cover the ray.
        foreach(var terrain in Terrain.activeTerrains)
        {
            if(terrain.gameObject.scene!=site.gameObject.scene||terrain.terrainData==null)continue;
            var local=point-terrain.transform.position;var size=terrain.terrainData.size;
            if(local.x<0||local.z<0||local.x>size.x||local.z>size.z)continue;
            return groundHeights[key]=terrain.SampleHeight(point)+terrain.transform.position.y;
        }
        var physics=site.gameObject.scene.GetPhysicsScene();
        int count=physics.Raycast(point+Vector3.up*6,Vector3.down,groundHits,12,~0,QueryTriggerInteraction.Ignore);
        float height=site.position.y,nearest=float.MaxValue;
        for(int i=0;i<count;i++)
        {
            var hit=groundHits[i];var collider=hit.collider;
            if(hit.normal.y<.65f||collider.transform.IsChildOf(anchor)||collider.transform.IsChildOf(root)||
                collider.GetComponentInParent<CharacterController>()!=null||collider.GetComponentInParent<AnimalController>()!=null||collider.attachedRigidbody!=null)continue;
            float distance=Mathf.Abs(hit.point.y-point.y);
            if(distance<nearest){nearest=distance;height=hit.point.y;}
        }
        return groundHeights[key]=height; // No ground in an isolated preview: retain the authored elevation.
    }
    Vector3 OnGround(Vector3 point,float inset=0){point.y=GroundHeight(point)-inset;return point;}
    void GroundPanel(Transform panel,float height,float baseHeight)
    {
        // Deform only our visual clone. Keep posts upright; the fabric hem follows the terrain
        // between them, including the source mesh's intentional 8cm bottom margin.
        var renderers=panel.GetComponentsInChildren<MeshRenderer>();
        float bottom=float.MaxValue,top=float.MinValue;
        foreach(var renderer in renderers)
        {
            var b=renderer.localBounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                float elevation=renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))).y;
                bottom=Mathf.Min(bottom,elevation);top=Mathf.Max(top,elevation);
            }
        }
        foreach(var filter in panel.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
            var mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Terrain fitted tarp (runtime only)";groundedMeshes.Add(mesh);
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var world=filter.transform.TransformPoint(vertices[i]);
                float t=Mathf.InverseLerp(bottom,top,world.y);
                world.y=GroundHeight(world)-.06f+baseHeight+t*height;
                vertices[i]=filter.transform.InverseTransformPoint(world);
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();filter.sharedMesh=mesh;
        }
    }
    void BuildSpan(Vector3 center,Quaternion rotation,float length,float height)
    {
        int columns=Mathf.Max(1,Mathf.CeilToInt(length/settings.panelWidth));
        int rows=Mathf.Max(1,Mathf.CeilToInt(height/settings.panelHeight));
        float bay=length/columns,row=height/rows;
        for(int x=0;x<=columns;x++)
        {
            Vector3 local=center+rotation*new Vector3(-length/2+x*bay,0,0);
            Spawn(settings.supportPost,"SidePost",OnGround(site.TransformPoint(local),.04f),site.rotation*rotation,new Vector3(1,height+.1f,1),screens);
        }
        for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)
        {
            Vector3 local=center+rotation*new Vector3(-length/2+(x+.5f)*bay,y*row,0);
            var panel=Spawn(settings.tarpPanel,"SideTarp",site.TransformPoint(local),site.rotation*rotation,
                new Vector3(bay/settings.panelWidth,row/settings.panelHeight,1),screens);
            GroundPanel(panel,row,y*row);
        }
        if(settings.screenCollision)for(int x=0;x<columns;x++)
        {
            Vector3 middle=center+rotation*new Vector3(-length/2+(x+.5f)*bay,0,0);
            Vector3 left=site.TransformPoint(middle+rotation*Vector3.left*bay/2),right=site.TransformPoint(middle+rotation*Vector3.right*bay/2);
            float low=Mathf.Min(GroundHeight(left),GroundHeight(right),GroundHeight(site.TransformPoint(middle)))-.06f;
            float high=Mathf.Max(GroundHeight(left),GroundHeight(right),GroundHeight(site.TransformPoint(middle)));
            var wall=new GameObject("ConstructionWallBarrier").transform;wall.SetParent(screens,false);
            var position=site.TransformPoint(middle);position.y=low;
            wall.SetPositionAndRotation(position,site.rotation*rotation);
            var box=wall.gameObject.AddComponent<BoxCollider>();box.enabled=false;
            float wallHeight=height+high-low;box.center=new Vector3(0,wallHeight/2,0);
            box.size=new Vector3(bay,wallHeight,Mathf.Clamp(settings.screenCollisionThickness,.08f,.3f));
            pendingBarriers.Add(box);
        }
    }
    void RefreshBarriers()
    {
        if(pendingBarriers.Count==0||hidden)return;
        Physics.SyncTransforms();
        for(int i=pendingBarriers.Count-1;i>=0;i--)
        {
            var box=pendingBarriers[i];if(box==null){pendingBarriers.RemoveAt(i);continue;}
            Vector3 scale=box.transform.lossyScale;
            Vector3 half=Vector3.Scale(box.size/2,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))+Vector3.one*.08f;
            // Query the site's own physics scene (also works in additive/local-physics scenes).
            var physics=site.gameObject.scene.GetPhysicsScene();
            int count=physics.OverlapBox(box.transform.TransformPoint(box.center),half,overlaps,box.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            bool occupied=count==overlaps.Length;
            for(int n=0;n<count&&!occupied;n++)
            {
                var other=overlaps[n];
                occupied=other!=null&&(other.GetComponentInParent<CharacterController>()!=null || other.GetComponentInParent<PlayerController>()!=null || other.attachedRigidbody!=null);
            }
            // Do not turn a newly spawned wall solid while it overlaps an actor/capsule.
            // Other walls and the two open exits remain available while this one is deferred.
            if(!occupied){box.enabled=true;pendingBarriers.RemoveAt(i);}
        }
    }
    void BuildDust(float w,float d,Vector3 work,Quaternion rotation)
    {
        if(settings==null || settings.dustShader==null)return;
        // Four overlapping interior quadrants cover the empty middle, not the outside fence.
        // Keep emitter volumes inset; particle drift is deliberately small and upward.
        float innerW=Mathf.Max(.8f,w-2),innerD=Mathf.Max(.8f,d-2);
        float cloudSize=Mathf.Clamp(Mathf.Min(innerW,innerD)*.25f,1.6f,4.5f);
        for(int i=0;i<6;i++)
        {
            bool workPuff=i==0,completion=i==2;
            var go=new GameObject(workPuff?"WorkDust":completion?"CompletionSmoke":i==1?"SiteDust":"SiteDust_Interior"+i);go.transform.SetParent(site,false);
            int quadrant=i==1?0:i-2;
            Vector3 position=workPuff?work+rotation*new Vector3(0,0,.42f):completion?site.position:
                site.TransformPoint(new Vector3((quadrant%2==0?-1:1)*innerW*.25f,0,(quadrant<2?-1:1)*innerD*.25f));
            float ground=GroundHeight(position);position.y=ground+(workPuff?.65f:completion?.85f:1.05f);go.transform.position=position;
            var material=new Material(settings.dustShader){name="Construction dust runtime"};materials.Add(material);
            material.SetFloat("_GroundY",ground-.12f);
            var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.loop=!completion;main.playOnAwake=false;main.duration=4;main.maxParticles=220;
            main.startLifetime=completion?new ParticleSystem.MinMaxCurve(settings.revealSeconds):workPuff?new ParticleSystem.MinMaxCurve(.7f,1.1f):new ParticleSystem.MinMaxCurve(3,4.5f);
            main.startSpeed=0;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            main.startSize=workPuff?new ParticleSystem.MinMaxCurve(.22f,.42f):completion?new ParticleSystem.MinMaxCurve(1.1f,2.5f):new ParticleSystem.MinMaxCurve(cloudSize*.8f,cloudSize*1.25f);
            main.startColor=new Color(.75f,.68f,.54f,workPuff?.28f:completion?.58f:.62f);main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Box;
            shape.scale=workPuff?new Vector3(.5f,.12f,.3f):completion?new Vector3(w,.3f,d):new Vector3(innerW*.5f,.45f,innerD*.5f);
            // Keep the box horizontal. Rotate neither the wide spawn volume nor the site;
            // give the particles world-up velocity instead of the shape's default +Z launch.
            var velocity=p.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=new ParticleSystem.MinMaxCurve(-.03f,.03f);velocity.z=new ParticleSystem.MinMaxCurve(-.03f,.03f);
            velocity.y=new ParticleSystem.MinMaxCurve(workPuff?.15f:completion?.5f:.12f,workPuff?.3f:completion?.85f:.28f);
            var emission=p.emission;emission.rateOverTime=0;
            var color=p.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.6f,.65f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=p.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.35f));
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            p.useAutoRandomSeed=false;p.randomSeed=(uint)(137+i);dust.Add(p);
            if(workPuff)workDust=p;else if(completion)completionSmoke=p;else{siteDust??=p;interiorDust.Add(p);}
        }
    }
    public void Tick(bool working,bool paused)
    {
        RefreshBarriers();
        foreach(var p in dust)
        {
            if(paused){if(p.isPlaying)p.Pause();continue;}
            if(p.isPaused)p.Play();
        }
        if(workDust!=null)
        {
            bool active=working&&!revealing&&!hidden;
            var emission=workDust.emission;emission.rateOverTime=active?4*settings.dustIntensity:0;
            foreach(var cloud in interiorDust){var ambient=cloud.emission;ambient.rateOverTime=active?Mathf.Clamp(Mathf.Sqrt(footprintArea)*.7f,6,22)*settings.dustIntensity:0;}
            if(active&&!paused)
            {
                if(!workDust.isPlaying)workDust.Play();
                foreach(var cloud in interiorDust)if(!cloud.isPlaying)cloud.Play();
                if(!wasWorking&&settings.dustIntensity>0)
                {
                    workDust.Emit(Mathf.CeilToInt(2*settings.dustIntensity));
                    foreach(var cloud in interiorDust)
                    {
                        // Populate visible aged puffs immediately, rather than leaving a hollow
                        // middle until several seconds of gameplay have accumulated.
                        cloud.Simulate(1.5f,true,false);cloud.Play();
                    }
                }
            }
            if(!paused)wasWorking=active;
        }
    }
    public void Reveal(float progress)
    {
        if(!revealing)
        {
            revealing=true;
            // Keep colliders, doors, stairs and all authoritative transforms unchanged during renovations.
            if(source != null)
            {
                var holder=new GameObject("Building_Rise_VisualOnly").transform;
                holder.SetParent(anchor,false);
                rising=ConstructionProjectVisuals.VisualClone(source,holder).transform;
                rising.localPosition=Vector3.zero;rising.localRotation=Quaternion.identity;
                foreach(var a in rising.GetComponentsInChildren<Animator>(true))a.enabled=false;
                foreach(var p in rising.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                // Preserve exactly the building anchor's authored scale, including scaled legacy sites.
                rising.SetParent(root,true);ConstructionProjectVisuals.DestroyRuntime(holder.gameObject);
                finalPosition=rising.position;
                float highest=anchor.position.y;
                foreach(var r in rising.GetComponentsInChildren<Renderer>())highest=Mathf.Max(highest,r.bounds.max.y);
                riseDistance=Mathf.Max(.4f,highest-anchor.position.y+.2f);
                foreach(var r in rising.GetComponentsInChildren<Renderer>())
                {
                    r.shadowCastingMode=ShadowCastingMode.Off;
                    var slots=r.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        if(slots[i]==null || settings?.revealShader==null)continue;
                        var old=slots[i];var m=new Material(settings.revealShader);
                        Texture texture=old.HasProperty("_TextureSample")?old.GetTexture("_TextureSample"):null;
                        texture ??= old.HasProperty("_BaseMap")?old.GetTexture("_BaseMap"):old.mainTexture;
                        m.SetTexture("_BaseMap",texture);m.SetFloat("_ClipY",anchor.position.y-.04f);
                        if(old.HasProperty("_BaseColor"))m.SetColor("_BaseColor",old.GetColor("_BaseColor"));
                        else if(old.HasProperty("_Color"))m.SetColor("_BaseColor",old.GetColor("_Color"));
                        materials.Add(m);slots[i]=m;
                    }
                    r.sharedMaterials=slots;
                }
            }
            // Clone first: scene-based legacy sources may also be the currently visible exterior.
            foreach(var renderer in anchor.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.transform.IsChildOf(root)||renderer is ParticleSystemRenderer||renderer.GetComponent<TMPro.TMP_Text>()!=null)continue;
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                hiddenOriginals[renderer]=renderer.enabled;renderer.enabled=false;
            }
            if(completionSmoke!=null){completionSmoke.Play();completionSmoke.Emit(Mathf.Clamp(Mathf.CeilToInt(footprintArea*.7f),25,140));}
        }
        if(rising != null)
        {
            // First 15% masks the exterior change, middle 70% rises, final 15% settles under smoke.
            float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.85f,progress));
            rising.position=finalPosition-Vector3.up*(riseDistance*(1-t));
        }
        // Remove screens under the first completion puff; the rising building can then be seen.
        screens.gameObject.SetActive(progress<.15f);
    }
    public void Hide()
    {
        hidden=true;site.gameObject.SetActive(false);
        if(rising != null)rising.gameObject.SetActive(false);
        RestoreOriginals();
    }
    void RestoreOriginals(){foreach(var pair in hiddenOriginals)if(pair.Key!=null)pair.Key.enabled=pair.Value;hiddenOriginals.Clear();}
    public void Dispose()
    {
        RestoreOriginals();
        if(screens!=null)foreach(var collider in screens.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        pendingBarriers.Clear();
        foreach(var m in materials)if(m!=null)ConstructionProjectVisuals.DestroyRuntime(m);
        foreach(var mesh in groundedMeshes)if(mesh!=null)ConstructionProjectVisuals.DestroyRuntime(mesh);
    }
}
