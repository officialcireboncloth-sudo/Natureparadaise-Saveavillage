using UnityEngine;

/// <summary>One equipped model, following the animated hand without inheriting the FBX rig's scale.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(1100)]
public sealed class PlayerHeldTools : MonoBehaviour
{
    [SerializeField] PlayerToolVisualCatalog catalog;
    [SerializeField] Transform rightHand;
    [SerializeField, Tooltip("Titik pegangan pada telapak kanan; dibuat otomatis di dalam bone tangan.")]
    Transform gripPoint;
    [Header("Hammer strike pose")]
    [Tooltip("Shaft elevation at the 0.68s hammer contact. Keeps the flat +Y striking face down and slightly forward.")]
    [SerializeField, Range(-10, 25)] float hammerContactElevation = 5f;
    [SerializeField] Vector2 hammerContactBlendIn = new(.48f, .64f);
    [SerializeField] Vector2 hammerContactBlendOut = new(.74f, .92f);
    Transform leftHand,rightForearm,rightArm,leftForearm,leftArm;
    Transform placementSpine;
    Transform placementHips, leftThigh, leftShin, leftFoot, rightThigh, rightShin, rightFoot;
    float placementFootError;
    Vector3 rightPalmOffset,leftPalmOffset;
    Animator rig;
    PlayerToolType actionTool;
    Vector3 actionTarget;
    float actionUntil;
    float actionStarted;
    PlayerController movement;
    PlayerToolHotbar hotbar;
    PlayerGatheringTool gathering;
    PlayerAnimalCarry animalCarry;
    HeldItemPlacementSystem placement;
    PlayerToolVisualCatalog.Entry shown;
    GameObject visual;
    public GameObject ActiveVisual=>visual!=null && visual.activeSelf?visual:null;
    /// <summary>Particle origin follows the visible tool's authored spout or container rim.</summary>
    public Vector3 WorkPoint(PlayerToolType tool)
    {
        if(ActiveVisual!=null)
        {
            string anchorName=tool==PlayerToolType.WateringCan?"WaterSpout":tool is PlayerToolType.None or PlayerToolType.Milker?"MilkSurface":"ToolImpact";
            var anchor=visual.transform.Find(anchorName);
            if(anchor!=null)return anchor.position;
            var renderer=visual.GetComponentInChildren<Renderer>();
            if(renderer!=null)return renderer.bounds.center;
        }
        return transform.position+transform.forward*.45f+Vector3.up*(tool==PlayerToolType.WateringCan?.7f:.4f);
    }
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
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "mixamorig:Spine" || bone.name == "Spine") placementSpine = bone;
                else if (bone.name == "mixamorig:Hips" || bone.name == "Hips") placementHips = bone;
                else if (bone.name == "mixamorig:LeftFoot" || bone.name == "LeftFoot") leftFoot = bone;
                else if (bone.name == "mixamorig:RightFoot" || bone.name == "RightFoot") rightFoot = bone;
            }
            if (leftFoot != null) { leftShin = leftFoot.parent; leftThigh = leftShin?.parent; }
            if (rightFoot != null) { rightShin = rightFoot.parent; rightThigh = rightShin?.parent; }
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
        actionTool=tool;actionTarget=target;actionStarted=Time.time;actionUntil=Time.time+duration;
    }

    /// <summary>Discards a pending work pose when the equipped selection is cleared or changed.</summary>
    public void EndWorkAction(){actionTool=PlayerToolType.None;actionUntil=0f;}

    /// <summary>Menjangkau titik penempatan dengan sedikit membungkuk tanpa meregangkan tulang lengan.</summary>
    public Vector3 ReachRightPalm(Vector3 target, float weight)
    {
        if (rightHand == null) ResolveHand();
        if (rightHand == null) return transform.position + transform.forward * .35f + Vector3.up;
        weight = Mathf.Clamp01(weight);
        // Geser berat badan ke meja, lalu pertahankan kedua kaki pada pose animasi.
        // Dengan begitu lengan pendek karakter dapat menjangkau tanpa memanjangkan tulang.
        if (placementHips != null && leftFoot != null && rightFoot != null)
        {
            Vector3 leftPosition = leftFoot.position, rightPosition = rightFoot.position;
            Quaternion leftRotation = leftFoot.rotation, rightRotation = rightFoot.rotation;
            placementHips.position += (transform.forward * .30f - Vector3.up * .14f) * weight;
            SolveArm(leftThigh, leftShin, leftFoot, leftPosition, leftRotation);
            SolveArm(rightThigh, rightShin, rightFoot, rightPosition, rightRotation);
            placementFootError = Mathf.Max(Vector3.Distance(leftFoot.position, leftPosition), Vector3.Distance(rightFoot.position, rightPosition));
        }
        if (placementSpine != null)
            placementSpine.rotation = Quaternion.AngleAxis(18f * weight, transform.right) * placementSpine.rotation;
        Vector3 palm = rightHand.position + rightHand.rotation * rightPalmOffset;
        Vector3 desired = Vector3.Lerp(palm, target, Mathf.Clamp01(weight));
        SolveArm(rightArm, rightForearm, rightHand, desired - rightHand.rotation * rightPalmOffset, rightHand.rotation);
        if (leftHand != null)
        {
            Vector3 support = target - transform.forward * .2f - transform.right * .18f - Vector3.up * .02f;
            Vector3 leftPalm = leftHand.position + leftHand.rotation * leftPalmOffset;
            SolveArm(leftArm, leftForearm, leftHand,
                Vector3.Lerp(leftPalm, support, weight) - leftHand.rotation * leftPalmOffset, leftHand.rotation);
        }
        return rightHand.position + rightHand.rotation * rightPalmOffset;
    }
    bool UsingWorkTool(PlayerToolVisualCatalog.Entry entry)
    {
        if(!entry.workGrip)return false;
        if(actionTool==entry.tool && Time.time<actionUntil)return true;
        if(rig==null || !rig.isActiveAndEnabled || rig.runtimeAnimatorController==null)return false;
        string state=entry.tool switch
        {
            PlayerToolType.Hoe=>"Hoeing",PlayerToolType.Axe=>"Chopping Tree",
            PlayerToolType.Hammer=>"Hammering Rock",PlayerToolType.Sickle=>"Sickle",
            PlayerToolType.WateringCan=>"Watering Plant",PlayerToolType.Pitchfork=>"Scoop Manure",_=>null
        };
        if(state==null)return false;
        return rig.GetCurrentAnimatorStateInfo(0).IsName(state) ||
            (rig.IsInTransition(0) && rig.GetNextAnimatorStateInfo(0).IsName(state));
    }
    void PoseTool(Transform tool,PlayerToolVisualCatalog.Entry entry,bool usingTool,Vector3 target)
    {
        SnapToGrip(tool,entry,!usingTool);
        if(entry.tool==PlayerToolType.WateringCan)
        {
            if(usingTool)
            {
                var timing = WateringActionProfile.Current;
                bool inClip = WateringActionProfile.TryPhase(rig, out float phase);
                bool fallbackPose = rig == null || !rig.isActiveAndEnabled || rig.runtimeAnimatorController == null ||
                    !rig.HasState(0, Animator.StringToHash("Base Layer.Watering Plant"));
                if(!inClip && fallbackPose && actionTool == PlayerToolType.WateringCan && Time.time <= actionUntil)
                    phase = Mathf.Clamp01((Time.time-actionStarted)/timing.fallbackDuration);
                float weight = timing.PoseWeight(phase);
                tool.localRotation = Quaternion.Slerp(Quaternion.Euler(entry.carryHandEuler), Quaternion.Euler(entry.handEuler), weight);
                // This prefab's cerat points along +X, body upright along +Y.
                // Retain the animated pouring tilt while keeping the spout in the forward vertical plane.
                Vector3 spout=tool.rotation*Vector3.right;
                Vector3 forwardSpout=transform.forward*new Vector2(spout.x,spout.z).magnitude+Vector3.up*spout.y;
                if(forwardSpout.sqrMagnitude>.001f)
                    tool.rotation=Quaternion.LookRotation(forwardSpout,Vector3.up)*
                        Quaternion.Inverse(Quaternion.LookRotation(Vector3.right,Vector3.up));
            }
            return;
        }
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
        if(entry.tool==PlayerToolType.Pitchfork && actionTool==entry.tool && Time.time<actionUntil)
        {
            // Preserve the scoop's body pose, but bring the real tines to the pile at contact.
            float elapsed=Time.time-actionStarted;
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.4f,elapsed)) *
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1.05f,elapsed)));
            var anchor=tool.Find("ToolImpact");
            float length=anchor!=null?anchor.localPosition.z*entry.scale:1f;
            Vector3 toward=(target+Vector3.up*.03f-grip).normalized;
            var aimed=Quaternion.LookRotation(toward,Vector3.ProjectOnPlane(Vector3.up,toward));
            rotation=Quaternion.Slerp(rotation,aimed,weight);
            grip=Vector3.Lerp(grip,target+Vector3.up*.03f-rotation*Vector3.forward*length,weight);
        }
        Vector3 shaft=rotation*Vector3.forward;
        // Keep the blade in the forward swing plane; vertical target offsets must not roll it sideways.
        Vector3 towardTarget=entry.tool==PlayerToolType.Hoe?transform.forward:entry.tool==PlayerToolType.Hammer?Vector3.down:Vector3.ProjectOnPlane(target-grip,Vector3.up);
        Vector3 cuttingDirection=Vector3.ProjectOnPlane(towardTarget,shaft);
        if(entry.tool==PlayerToolType.Hammer)
        {
            // The clip's palm separation contains a lateral component. It must not tilt the
            // mallet across the actor or roll the striking face into a diagonal plane.
            Vector3 planarShaft = Vector3.ProjectOnPlane(shaft, transform.right);
            if(planarShaft.sqrMagnitude < .001f) planarShaft = transform.forward;
            float elapsed = HammerPoseTime();
            float contact = elapsed < 0 ? 0 : Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(hammerContactBlendIn.x, hammerContactBlendIn.y, elapsed)) *
                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(hammerContactBlendOut.x, hammerContactBlendOut.y, elapsed)));
            Vector3 contactShaft = Quaternion.AngleAxis(-hammerContactElevation, transform.right) * transform.forward;
            planarShaft = Vector3.Slerp(planarShaft.normalized, contactShaft, contact);
            rotation = Quaternion.LookRotation(planarShaft, Vector3.Cross(transform.right, planarShaft));
        }
        else if(entry.tool==PlayerToolType.Hoe)
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
        else if(entry.actionPlaneNormal.sqrMagnitude>.001f)
        {
            // A signed plane derived from the updated clip avoids the roll reversal when
            // the shaft passes through the target direction. The authored hand still drives the stroke.
            Vector3 face=Vector3.Cross(transform.TransformDirection(entry.actionPlaneNormal),shaft);
            if(face.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(shaft,face);
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
    float HammerPoseTime()
    {
        if(actionTool == PlayerToolType.Hammer && Time.time <= actionUntil) return Time.time - actionStarted;
        if(rig == null || rig.runtimeAnimatorController == null) return -1;
        var state = rig.GetCurrentAnimatorStateInfo(0);
        if(rig.IsInTransition(0))
        {
            var next = rig.GetNextAnimatorStateInfo(0);
            if(next.IsName("Hammering Rock")) state = next;
        }
        return state.IsName("Hammering Rock") ? Mathf.Clamp01(state.normalizedTime) * state.length : -1;
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
