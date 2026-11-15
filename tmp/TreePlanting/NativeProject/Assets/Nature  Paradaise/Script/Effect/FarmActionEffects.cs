using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Shape=FarmParticleMeshes.Shape;

/// <summary>Shared bounded emitters. Bursts follow successful contacts; pouring reports its first landing.</summary>
[DefaultExecutionOrder(1300)]
public sealed class FarmActionEffects : MonoBehaviour
{
    static FarmActionEffects instance;
    FarmActionEffectsCatalog catalog;
    readonly Dictionary<Shape,ParticleSystem> emitters=new();
    readonly List<Mesh> meshes=new();
    readonly List<WaterFlow> flows=new(4);
    readonly AudioSource[] voices=new AudioSource[6];
    readonly float[] lastSound=new float[System.Enum.GetValues(typeof(FarmEffect)).Length];
    readonly System.Random random=new(74019);
    Material fallbackMaterial;
    int voice;
    bool paused;
    public sealed class PourHandle { public bool HasReachedGround { get; internal set; } public bool Cancelled { get; internal set; } }
    struct WaterContact { public float remaining; public bool active; }
    sealed class WaterFlow {public MonoBehaviour owner;public PlayerHeldTools tools;public Transform target;public Vector3 point;public float remaining,timer,flight;public readonly WaterContact[] contacts=new WaterContact[16];public int nextContact;public PourHandle handle;public System.Func<bool> firstContact;}
    public static FarmActionEffects Active=>instance;
    public int LiveParticles {get{int count=0;foreach(var system in emitters.Values)count+=system.particleCount;return count;}}
    public int ActiveWaterFlows { get { int n=0;foreach(var f in flows)if(f.remaining>0)n++;return n; } }
    public static event System.Action<FarmEffect,Vector3> Emitted;
    static FarmActionEffects Get()
    {
        if(instance==null)instance=new GameObject("FarmActionEffects_Runtime").AddComponent<FarmActionEffects>();
        return instance;
    }
    void Awake(){instance=this;catalog=Resources.Load<FarmActionEffectsCatalog>("Effects/FarmActionEffects");for(int i=0;i<lastSound.Length;i++)lastSound[i]=-10;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){instance=null;Emitted=null;}

    /// <summary>Spawns an impact at the actual work position; amount scales visual density within a fixed budget.</summary>
    public static void Burst(FarmEffect effect,Vector3 point,Vector3 direction=default,float radius=.25f,int amount=1,bool sound=true)
    {
        if(!Application.isPlaying||IsPaused||amount<=0)return;
        var fx=Get();fx.Emit(effect,point,direction,Mathf.Clamp(radius,.04f,2f),Mathf.Clamp(amount,1,12));
        if(sound)fx.Sound(effect,point);
        Emitted?.Invoke(effect,point);
    }

    /// <summary>Follows the animated spout until pouring ends; disabling the owner cancels the stream.</summary>
    public static PourHandle Pour(MonoBehaviour owner,PlayerHeldTools tools,Transform target,Vector3 point,float duration,float flightTime=.18f,System.Func<bool> firstContact=null)
    {
        if(owner==null||target==null||duration<=0)return null;
        var fx=Get();Cancel(owner);
        if(fx.flows.Count>=4)return null;
        var handle=new PourHandle();
        fx.flows.Add(new WaterFlow{owner=owner,tools=tools,target=target,point=target.InverseTransformPoint(point),remaining=duration,flight=Mathf.Clamp(flightTime,.05f,.3f),handle=handle,firstContact=firstContact});
        fx.Sound(FarmEffect.Water,point);
        return handle;
    }
    /// <summary>Stops emission at the spout; already falling drops can finish their short flight.</summary>
    public static void StopPour(MonoBehaviour owner){if(instance!=null)foreach(var f in instance.flows)if(f.owner==owner)f.remaining=0;}
    public static void Cancel(MonoBehaviour owner){if(instance!=null){foreach(var f in instance.flows)if(f.owner==owner)f.handle.Cancelled=true;instance.flows.RemoveAll(f=>f.owner==owner);}}
    /// <summary>Plays one shared action sound without spawning an unrelated particle silhouette.</summary>
    public static void PlaySound(FarmEffect effect,Vector3 point){if(Application.isPlaying&&!IsPaused)Get().Sound(effect,point);}

    /// <summary>Schedules an already-validated handover at hand contact instead of button-down.</summary>
    public static void Handover(MonoBehaviour owner,Transform target,Vector3 point,float delay=.45f)
    {
        if(owner!=null&&target!=null)Get().StartCoroutine(Get().DelayedHandover(owner,target,target.InverseTransformPoint(point),delay));
    }
    /// <summary>One mobile-budget grain arc from the animated hand to pond water at feed release.</summary>
    public static void ScatterFeed(PlayerController owner,Vector3 waterPoint)
    {
        if(owner==null||IsPaused)return;
        var fx=Get();
        Animator animator=owner.CharacterAnimator;
        Transform hand=animator!=null&&animator.isHuman?animator.GetBoneTransform(HumanBodyBones.RightHand):null;
        if(hand==null)hand=owner.GetComponent<PlayerHeldTools>()?.GripPoint;
        if(hand==null&&animator!=null)
            foreach(var bone in animator.GetComponentsInChildren<Transform>(true))
                if(bone.name=="mixamorig:RightHand"||bone.name=="RightHand"){hand=bone;break;}
        Vector3 source=hand!=null?hand.position:owner.transform.position+Vector3.up*.9f+owner.transform.forward*.35f;
        const float flight=.38f;
        Vector3 velocity=(waterPoint-source)/flight-Physics.gravity*.35f*flight*.5f;
        fx.Particles(Shape.Grain,source,new Color(.77f,.58f,.28f),12,.045f,.045f,flight,velocity,.06f,exactLifetime:true);
        fx.Sound(FarmEffect.Feed,source);
        Emitted?.Invoke(FarmEffect.Feed,waterPoint);
        fx.StartCoroutine(fx.FeedWaterLanding(waterPoint,flight));
    }
    IEnumerator FeedWaterLanding(Vector3 point,float flight)
    {
        yield return new WaitForSeconds(flight);
        Particles(Shape.Ripple,point+Vector3.up*.015f,new Color(.74f,.87f,.90f,.55f),3,.13f,.25f,.5f,Vector3.zero,.02f);
    }
    IEnumerator DelayedHandover(MonoBehaviour owner,Transform target,Vector3 local,float delay)
    {
        float elapsed=0;
        while(elapsed<delay){if(!IsPaused)elapsed+=Time.deltaTime;yield return null;}
        if(owner!=null&&owner.isActiveAndEnabled&&target!=null&&target.gameObject.activeInHierarchy)Burst(FarmEffect.Feed,target.TransformPoint(local));
    }
    static bool IsPaused=>Time.timeScale<=0||(TimeManager.Instance!=null&&TimeManager.Instance.IsPaused);
    void LateUpdate()
    {
        bool freeze=IsPaused;
        if(paused!=freeze)
        {
            paused=freeze;
            foreach(var ps in emitters.Values){if(paused)ps.Pause();else ps.Play();}
            foreach(var audio in voices)if(audio!=null){if(paused)audio.Pause();else audio.UnPause();}
        }
        if(paused)return;
        for(int i=flows.Count-1;i>=0;i--)
        {
            if(i>=flows.Count)continue;
            var f=flows[i];
            if(f.owner==null||!f.owner.isActiveAndEnabled||f.target==null||!f.target.gameObject.activeInHierarchy){f.handle.Cancelled=true;flows.Remove(f);continue;}
            Vector3 target=f.target.TransformPoint(f.point);
            int pending=0;
            for(int c=0;c<f.contacts.Length;c++)
            {
                var contact=f.contacts[c];if(!contact.active)continue;
                if(f.handle.Cancelled)break;
                contact.remaining-=Time.deltaTime;
                if(contact.remaining>0){f.contacts[c]=contact;pending++;continue;}
                f.contacts[c]=default;
                bool firstLanding=!f.handle.HasReachedGround;
                if(firstLanding)
                {
                    // Commit and splash in this same frame: putting the can away after landing
                    // must not undo a visible successful watering or spend water twice.
                    var commit=f.firstContact;f.firstContact=null;
                    if(commit!=null&&!commit()){f.handle.Cancelled=true;flows.Remove(f);break;}
                    f.handle.HasReachedGround=true;
                }
                Particles(Shape.Drop,target+Vector3.up*.025f,new Color(.68f,.88f,.98f,.7f),1,.035f,.025f,.22f,Vector3.up*.42f,.15f);
                if(firstLanding)Emitted?.Invoke(FarmEffect.Water,target);
            }
            if(f.handle.Cancelled)continue;
            if(f.remaining<=0){if(pending==0)flows.Remove(f);continue;}
            f.remaining-=Time.deltaTime;f.timer-=Time.deltaTime;
            Vector3 source=f.tools!=null?f.tools.WorkPoint(PlayerToolType.WateringCan):f.owner.transform.position+Vector3.up*.8f+f.owner.transform.forward*.3f;
            for(int n=0;f.timer<=0&&n<4;n++,f.timer+=.025f)
            {
                float flight=f.flight;
                Vector3 velocity=(target-source)/flight-Physics.gravity*.65f*flight*.5f;
                Particles(Shape.Drop,source,new Color(.57f,.83f,.96f,.78f),2,.018f,.035f,flight,velocity,.04f,exactLifetime:true);
                f.contacts[f.nextContact]=new WaterContact{active=true,remaining=flight};f.nextContact=(f.nextContact+1)%f.contacts.Length;
            }
        }
    }
    float Range(float min,float max)=>min+(max-min)*(float)random.NextDouble();
    Vector3 Spread(float radius){float a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(Range(0,1))*radius;return new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);}
    ParticleSystem Emitter(Shape shape)
    {
        if(emitters.TryGetValue(shape,out var found))return found;
        var root=new GameObject("Farm FX "+shape);root.transform.SetParent(transform,false);
        var ps=root.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startSpeed=0;main.startRotation3D=true;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        main.gravityModifier=shape switch {Shape.Soil or Shape.Stone or Shape.Wood=>.8f,Shape.Drop=>.65f,Shape.Grain=>.35f,Shape.Leaf=>.12f,_=>0};
        var emission=ps.emission;emission.enabled=false;var cone=ps.shape;cone.enabled=false;
        var colors=ps.colorOverLifetime;colors.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var size=ps.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,shape is Shape.Dust or Shape.Ripple?.6f:1,1,shape is Shape.Dust or Shape.Ripple?1.7f:.35f));
        var renderer=root.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
        var mesh=FarmParticleMeshes.Build(shape);meshes.Add(mesh);renderer.mesh=mesh;
        renderer.alignment=shape is Shape.Ripple or Shape.Swing?ParticleSystemRenderSpace.World:ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;
        if(catalog!=null&&catalog.particleMaterial!=null)renderer.sharedMaterial=catalog.particleMaterial;
        else{if(fallbackMaterial==null)fallbackMaterial=new Material(Shader.Find("Nature Paradise/Farm Particles")??Shader.Find("Sprites/Default"));renderer.sharedMaterial=fallbackMaterial;}
        emitters.Add(shape,ps);ps.Play();return ps;
    }
    void Particles(Shape shape,Vector3 point,Color color,int count,float spread,float size,float life,Vector3 velocity,float scatter=1,float yaw=0,bool exactLifetime=false)
    {
        var ps=Emitter(shape);count=Mathf.Min(Mathf.RoundToInt(count*(catalog!=null?catalog.density:1)),Mathf.Min(96-ps.particleCount,(catalog!=null?catalog.particleBudget:384)-LiveParticles));
        for(int i=0;i<count;i++)
        {
            var randomVelocity=Spread(scatter)+Vector3.up*Range(0,scatter*.65f);
            var emit=new ParticleSystem.EmitParams{position=point+Spread(spread),velocity=velocity+randomVelocity,
                startColor=color,startLifetime=exactLifetime?life:life*Range(.85f,1.1f),startSize=size*Range(.8f,1.2f),
                rotation3D=shape is Shape.Ripple or Shape.Swing?new Vector3(0,yaw,0):new Vector3(0,0,Range(0,360))};
            ps.Emit(emit,1);
        }
    }
    void Emit(FarmEffect effect,Vector3 p,Vector3 direction,float radius,int amount)
    {
        Vector3 drift=direction.sqrMagnitude>.001f?direction.normalized*.5f:Vector3.zero;
        int n=Mathf.Min(3,Mathf.CeilToInt(Mathf.Sqrt(amount)));
        void Dust(Color color,int count=5)=>Particles(Shape.Dust,p+Vector3.up*.04f,color,count*n,radius,.26f,.55f,Vector3.up*.22f,.12f);
        void Spark(int count=5)=>Particles(Shape.Sparkle,p+Vector3.up*.12f,new Color(1,.91f,.6f,.85f),count,.2f,.11f,.4f,Vector3.up*.35f,.25f);
        void Leaves(int count=8)=>Particles(Shape.Leaf,p+Vector3.up*.1f,new Color(.43f,.65f,.22f,.95f),count*n,radius,.09f,.65f,Vector3.up*.5f+drift,.6f);
        switch(effect)
        {
            case FarmEffect.Hoe:Dust(new Color(.52f,.37f,.22f,.32f));Particles(Shape.Soil,p+Vector3.up*.04f,new Color(.43f,.27f,.13f),9*n,radius,.075f,.55f,Vector3.up*1.1f,.75f);break;
            case FarmEffect.Sickle:Leaves();break;
            case FarmEffect.FallingLeaves:
                Particles(Shape.Leaf,p,new Color(.43f,.65f,.22f,.95f),5*n,radius,.09f,.8f,Vector3.down*.35f,.35f);break;
            case FarmEffect.Hammer:case FarmEffect.HammerBreak:
                int chips=effect==FarmEffect.HammerBreak?19:8;
                Dust(new Color(.6f,.63f,.64f,.3f),effect==FarmEffect.HammerBreak?10:4);
                Particles(Shape.Stone,p,new Color(.64f,.67f,.68f),chips,radius,.065f,.5f,Vector3.up*.9f,1);Spark(2);break;
            case FarmEffect.Water:Particles(Shape.Drop,p+Vector3.up*.025f,new Color(.63f,.86f,.97f,.8f),6,radius,.04f,.3f,Vector3.up*.6f,.4f);break;
            case FarmEffect.Brush:Dust(new Color(.79f,.73f,.61f,.24f),3);Particles(Shape.Wool,p,new Color(.87f,.83f,.72f,.75f),2,.08f,.035f,.5f,Vector3.up*.15f,.1f);break;
            case FarmEffect.BrushComplete:Spark();Particles(Shape.Heart,p+Vector3.up*.25f,new Color(.96f,.54f,.51f,.9f),2,.13f,.16f,.85f,Vector3.up*.45f,.07f);break;
            case FarmEffect.Axe:Particles(Shape.Wood,p,new Color(.68f,.45f,.22f),10,radius,.09f,.5f,Vector3.up*.65f+drift,.8f);Dust(new Color(.69f,.53f,.32f,.25f),3);break;
            case FarmEffect.TreeFall:Leaves(17);Dust(new Color(.58f,.46f,.3f,.28f),12);break;
            case FarmEffect.Plant:case FarmEffect.Fertilize:case FarmEffect.Feed:
                Color grain=effect==FarmEffect.Fertilize?new Color(.37f,.29f,.18f):new Color(.81f,.66f,.36f);
                Particles(Shape.Grain,p+Vector3.up*.32f,grain,effect==FarmEffect.Plant?5:10,radius,.045f,.4f,Vector3.down*.45f,.18f);
                if(effect!=FarmEffect.Feed)Dust(new Color(.55f,.39f,.23f,.22f),3);break;
            case FarmEffect.Harvest:Leaves(5);Spark();break;
            case FarmEffect.Milk:Particles(Shape.Drop,p,new Color(1,.98f,.9f,.95f),4,.035f,.035f,.22f,Vector3.up*.25f,.1f);break;
            case FarmEffect.MilkComplete:Spark();break;
            case FarmEffect.Shear:Particles(Shape.Wool,p,new Color(.96f,.94f,.85f,.9f),7,.15f,.13f,.7f,Vector3.up*.18f+drift*.25f,.35f);break;
            case FarmEffect.FishCast:case FarmEffect.FishCatch:
                bool big=effect==FarmEffect.FishCatch;
                Particles(Shape.Drop,p+Vector3.up*.04f,new Color(.65f,.86f,.96f,.85f),big?25:12,radius*.4f,.065f,big?.7f:.45f,Vector3.up*(big?1.6f:1),big?.9f:.55f);
                Particles(Shape.Ripple,p+Vector3.up*.025f,new Color(.72f,.92f,1,.65f),2,.04f,big?1.1f:.6f,.7f,Vector3.zero,0);break;
            case FarmEffect.Swing:
                float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                Particles(Shape.Swing,p+Vector3.up*.35f,new Color(.84f,.94f,.7f,.32f),1,0,radius*1.5f,.18f,Vector3.zero,0,yaw);break;
        }
    }
    void Sound(FarmEffect effect,Vector3 point)
    {
        var sound=catalog!=null?catalog.Audio(effect):null;if(sound?.clip==null||Time.time-lastSound[(int)effect]<.075f)return;
        lastSound[(int)effect]=Time.time;
        AudioSource source=voices[voice];
        if(source==null){var root=new GameObject("Farm FX Audio "+voice);root.transform.SetParent(transform,false);source=root.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=.85f;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=20;voices[voice]=source;}
        source.Stop();source.transform.position=point;source.pitch=Range(.94f,1.06f);source.volume=sound.volume*GameAudio.Volume(GameAudioBus.Main);source.clip=sound.clip;source.Play();voice=(voice+1)%voices.Length;
    }
    void OnDestroy(){if(instance==this)instance=null;foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(fallbackMaterial!=null)Destroy(fallbackMaterial);}
}
