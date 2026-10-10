using UnityEngine;

/// <summary>One equipped model, following the animated hand without inheriting the FBX rig's scale.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(1100)]
public sealed class PlayerHeldTools : MonoBehaviour
{
    [SerializeField] PlayerToolVisualCatalog catalog;
    [SerializeField] Transform rightHand;
    [SerializeField, Tooltip("Titik pegangan pada telapak kanan; dibuat otomatis di dalam bone tangan.")]
    Transform gripPoint;
    Transform leftHand,rightForearm,rightArm,leftForearm,leftArm;
    Vector3 rightPalmOffset,leftPalmOffset;
    Animator rig;
    PlayerToolType actionTool;
    Vector3 actionTarget;
    float actionUntil;
    PlayerController movement;
    PlayerToolHotbar hotbar;
    PlayerGatheringTool gathering;
    PlayerAnimalCarry animalCarry;
    HeldItemPlacementSystem placement;
    PlayerToolVisualCatalog.Entry shown;
    GameObject visual;
    public GameObject ActiveVisual=>visual!=null && visual.activeSelf?visual:null;
    public PlayerToolVisualCatalog Catalog=>catalog;
    public Transform GripPoint { get { if(rightHand==null)ResolveHand();EnsureGripPoint();return gripPoint; } }

    void Awake()
    {
        movement=GetComponent<PlayerController>();hotbar=GetComponent<PlayerToolHotbar>();
        if(hotbar==null)hotbar=gameObject.AddComponent<PlayerToolHotbar>();
        if(catalog==null)catalog=Resources.Load<PlayerToolVisualCatalog>(PlayerToolVisualCatalog.ResourcePath);
    }
    void Start()=>ResolveHand();
    void ResolveHand()
    {
        rig=movement!=null?movement.CharacterAnimator:GetComponentInChildren<Animator>();
        if(rightHand==null && rig!=null && rig.isHuman)rightHand=rig.GetBoneTransform(HumanBodyBones.RightHand);
        if(rightHand==null)foreach(var t in GetComponentsInChildren<Transform>(true))
            if(t.name=="mixamorig:RightHand" || t.name=="RightHand"){rightHand=t;break;}
        if(rightHand!=null)
        {
            rightForearm=rightHand.parent;rightArm=rightForearm!=null?rightForearm.parent:null;
            foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name=="mixamorig:LeftHand" || t.name=="LeftHand"){leftHand=t;break;}
            if(leftHand!=null){leftForearm=leftHand.parent;leftArm=leftForearm!=null?leftForearm.parent:null;}
            rightPalmOffset=PalmOffset(rightHand);leftPalmOffset=PalmOffset(leftHand);
            EnsureGripPoint();
        }
        gathering=GetComponent<PlayerGatheringTool>();animalCarry=GetComponent<PlayerAnimalCarry>();placement=GetComponent<HeldItemPlacementSystem>();
    }
    void LateUpdate()
    {
        if(catalog==null || hotbar==null)return;
        if(rightHand==null)ResolveHand();
        var desired=catalog.Find(hotbar.SelectedTool);
        // Fishing owns its rod pose, line origin and visibility during the minigame.
        if(hotbar.SelectedTool==PlayerToolType.FishingRod)desired=null;
        var care=AnimalController.CurrentCareAction;
        if(care!=null && care.CarePlayer==movement)
            desired=care.IsBrushing?catalog.Find("Brush"):care.IsShearing?catalog.Find("Shears"):
                care.Growth!=null && (care.Growth.Type==AnimalType.Cow || care.Growth.Type==AnimalType.Goat)?catalog.Find("Milker"):null;
        bool occupied=(gathering!=null && gathering.IsCarrying) || (animalCarry!=null && animalCarry.HasAnimal) ||
            (placement!=null && placement.HeldItem!=null) || (movement!=null && (movement.IsCarrying || movement.IsRiding));
        if(occupied || rightHand==null)desired=null;
        Show(desired);
        if(visual==null || shown==null)return;
        // World-unit offsets and compensated scale prevent oversized tools on a scaled humanoid skeleton.
        bool usingTool=UsingWorkTool(shown);
        Vector3 target=Time.time<=actionUntil && actionTool==shown.tool?actionTarget:transform.position+transform.forward*2f;
        if(shown.tool==PlayerToolType.Hoe && Time.time>actionUntil)target.y=transform.position.y;
        PoseTool(visual.transform,shown,usingTool,target);
    }
    void EnsureGripPoint()
    {
        if(rightHand==null || gripPoint!=null)return;
        gripPoint=new GameObject("ToolGrip_Right").transform;
        gripPoint.SetParent(rightHand,false);
        gripPoint.localPosition=rightHand.InverseTransformVector(rightHand.rotation*rightPalmOffset);
        Vector3 handScale=rightHand.lossyScale;
        gripPoint.localScale=new Vector3(1f/Mathf.Max(.0001f,Mathf.Abs(handScale.x)),
            1f/Mathf.Max(.0001f,Mathf.Abs(handScale.y)),1f/Mathf.Max(.0001f,Mathf.Abs(handScale.z)));
    }
    /// <summary>Attach the prefab's handle pivot directly to the animated palm, without lag or world-facing rotation.</summary>
    public void SnapToGrip(Transform tool,PlayerToolVisualCatalog.Entry entry,bool carrying=true)
    {
        var socket=GripPoint;if(socket==null || tool==null || entry==null)return;
        // Offsets stay in meters even though the imported hand hierarchy is scaled.
        socket.localPosition=rightHand.InverseTransformVector(rightHand.rotation*(rightPalmOffset+entry.handPosition));
        socket.localRotation=Quaternion.identity;
        Vector3 handScale=rightHand.lossyScale;
        socket.localScale=new Vector3(1f/Mathf.Max(.0001f,Mathf.Abs(handScale.x)),
            1f/Mathf.Max(.0001f,Mathf.Abs(handScale.y)),1f/Mathf.Max(.0001f,Mathf.Abs(handScale.z)));
        if(tool.parent!=socket)tool.SetParent(socket,false);
        tool.localPosition=Vector3.zero;
        tool.localRotation=Quaternion.Euler(carrying && entry.workGrip?entry.carryHandEuler:entry.handEuler);
        tool.localScale=Vector3.one*entry.scale;
    }
    public void BeginWorkAction(PlayerToolType tool,Vector3 target,float duration)
    {
        actionTool=tool;actionTarget=target;actionUntil=Time.time+duration;
    }
    bool UsingWorkTool(PlayerToolVisualCatalog.Entry entry)
    {
        if(!entry.workGrip)return false;
        if(actionTool==entry.tool && Time.time<actionUntil)return true;
        if(rig==null || !rig.isActiveAndEnabled || rig.runtimeAnimatorController==null)return false;
        string state=entry.tool switch
        {
            PlayerToolType.Hoe=>"Hoeing",PlayerToolType.Axe=>"Axe",
            PlayerToolType.Hammer=>"Hammering Rock",PlayerToolType.Sickle=>"Sickle",_=>null
        };
        if(state==null)return false;
        return rig.GetCurrentAnimatorStateInfo(0).IsName(state) ||
            (rig.IsInTransition(0) && rig.GetNextAnimatorStateInfo(0).IsName(state));
    }
    void PoseTool(Transform tool,PlayerToolVisualCatalog.Entry entry,bool usingTool,Vector3 target)
    {
        SnapToGrip(tool,entry,!usingTool);
        if(!entry.workGrip || !usingTool)
        {
            // Walking/running leaves the animated primary hand untouched. An idle support hand may reach the shaft.
            if(entry.twoHandsWhileCarrying && movement!=null && movement.CurrentMode==PlayerController.MovementMode.Idle && leftHand!=null && leftArm!=null && leftForearm!=null)
            {
                Vector3 wristTarget=tool.TransformPoint(entry.secondHandGrip)-leftHand.rotation*leftPalmOffset;
                float reach=Vector3.Distance(leftArm.position,leftForearm.position)+Vector3.Distance(leftForearm.position,leftHand.position);
                if(Vector3.Distance(leftArm.position,wristTarget)<reach-.002f)
                    SolveArm(leftArm,leftForearm,leftHand,wristTarget,leftHand.rotation);
            }
            return;
        }
        // Grip the shaft with the palm, rather than placing its pivot at the wrist.
        Vector3 grip=gripPoint.position;
        Quaternion rotation=rightHand.rotation*Quaternion.Euler(entry.handEuler);
        Vector3 shaft=rotation*Vector3.forward;
        // Keep the blade in the forward swing plane; vertical target offsets must not roll it sideways.
        Vector3 towardTarget=entry.tool==PlayerToolType.Hoe?transform.forward:entry.tool==PlayerToolType.Hammer?Vector3.down:Vector3.ProjectOnPlane(target-grip,Vector3.up);
        Vector3 cuttingDirection=Vector3.ProjectOnPlane(towardTarget,shaft);
        if(entry.tool==PlayerToolType.Hoe)
        {
            // Keep one signed swing plane through the whole stroke. Projecting forward onto the
            // shaft flips the blade 180 degrees when the handle passes the forward direction.
            Vector3 bladeDirection=Vector3.Cross(transform.right,shaft);
            if(bladeDirection.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(shaft,bladeDirection);
        }
        else if(entry.tool==PlayerToolType.Sickle)
        {
            // The sickle's local +X is the blade-plane normal. Keep its edge in the low sweeping plane.
            Vector3 bladeNormal=Vector3.ProjectOnPlane(Vector3.up,shaft);
            if(bladeNormal.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(shaft,Vector3.Cross(shaft,bladeNormal));
        }
        else if(cuttingDirection.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(shaft,cuttingDirection);
        // Solve only arms after animation, preserving the clip's body, feet and swing.
        Quaternion wristRotation=rotation*Quaternion.Inverse(Quaternion.Euler(entry.handEuler));
        Quaternion leftRotation=leftHand!=null?leftHand.rotation:Quaternion.identity;
        Vector3 secondOffset=rotation*(entry.secondHandGrip*entry.scale);
        // Both wrists must stay reachable. Move the grip a few centimetres instead of stretching an arm.
        for(int i=0;i<16;i++)
        {
            grip=ReachableGrip(rightArm,rightForearm,rightHand,grip,wristRotation*(rightPalmOffset+entry.handPosition));
            if(entry.twoHandsDuringAction && leftHand!=null)grip=ReachableGrip(leftArm,leftForearm,leftHand,grip,leftRotation*leftPalmOffset-secondOffset);
        }
        SolveArm(rightArm,rightForearm,rightHand,grip-wristRotation*(rightPalmOffset+entry.handPosition),wristRotation);
        // The primary palm is the final anchor, including when a blended pose reaches the arm limit.
        // Keep the handle on the actual hand rather than the solver's intended target.
        grip=rightHand.position+rightHand.rotation*(rightPalmOffset+entry.handPosition);
        if(entry.twoHandsDuringAction && leftHand!=null)
        {
            Vector3 secondGrip=grip+secondOffset;
            SolveArm(leftArm,leftForearm,leftHand,secondGrip-leftRotation*leftPalmOffset,leftRotation);
        }
        // Apply the action pose after IK: the tool is a child of the hand and inherits its final motion.
        tool.SetPositionAndRotation(grip,rotation);
    }
    static Vector3 ReachableGrip(Transform shoulder,Transform elbow,Transform hand,Vector3 grip,Vector3 offset)
    {
        if(shoulder==null || elbow==null || hand==null)return grip;
        float reach=Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,hand.position)-.002f;
        Vector3 delta=grip-offset-shoulder.position;
        return delta.magnitude>reach?shoulder.position+delta.normalized*reach+offset:grip;
    }
    static Vector3 PalmOffset(Transform hand)
    {
        if(hand==null)return Vector3.zero;
        Vector3 knuckles=Vector3.zero;int count=0;
        foreach(var t in hand.GetComponentsInChildren<Transform>())
            if(t.name.EndsWith("HandIndex1") || t.name.EndsWith("HandMiddle1") || t.name.EndsWith("HandRing1")){knuckles+=t.position;count++;}
        return count==0?Vector3.zero:Quaternion.Inverse(hand.rotation)*(Vector3.Lerp(hand.position,knuckles/count,.75f)-hand.position);
    }
    static void SolveArm(Transform shoulder,Transform elbow,Transform hand,Vector3 target,Quaternion wristRotation)
    {
        if(shoulder==null || elbow==null || hand==null)return;
        Vector3 a=shoulder.position,b=elbow.position,c=hand.position;
        float upper=Vector3.Distance(a,b),lower=Vector3.Distance(b,c);
        Vector3 delta=target-a;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
        if(delta.sqrMagnitude<.0001f)return;
        Vector3 axis=delta.normalized,pole=Vector3.ProjectOnPlane(b-a,axis);
        if(pole.sqrMagnitude<.0001f)pole=Vector3.ProjectOnPlane(shoulder.forward,axis);
        pole.Normalize();float along=(upper*upper-lower*lower+distance*distance)/(2f*distance);
        Vector3 desiredElbow=a+axis*along+pole*Mathf.Sqrt(Mathf.Max(0f,upper*upper-along*along));
        shoulder.rotation=Quaternion.FromToRotation(b-a,desiredElbow-a)*shoulder.rotation;
        elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
        hand.rotation=wristRotation;
    }
    void Show(PlayerToolVisualCatalog.Entry desired)
    {
        if(desired==shown){if(visual!=null)visual.SetActive(isActiveAndEnabled);return;}
        if(visual!=null){visual.SetActive(false);Destroy(visual);}
        visual=null;shown=desired;
        if(desired==null)return;
        visual=Instantiate(desired.prefab,GripPoint,false);visual.name="HeldTool_"+desired.id;
        foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
    }
    void OnDisable(){if(visual!=null)visual.SetActive(false);}
    void OnDestroy(){if(visual!=null)Destroy(visual);}
}
