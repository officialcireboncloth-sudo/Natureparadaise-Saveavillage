#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Fixture Play Mode terisolasi untuk kontak animasi, pembatalan, dan transaksi penetasan.</summary>
[InitializeOnLoad]
public static class EggPlacementValidation
{
    const string Output = "Documentation/EggPlacement-2026-10-20";
    const string Running = "NatureParadise.EggPlacementValidation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static IEnumerator tests;
    static readonly StringBuilder report = new();
    static int checks;
    static double deadline;

    static EggPlacementValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Running, false)) return;
            tests = RunTests();
            deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Tick;
        };
    }

    /// <summary>Jalankan dengan Unity batchmode pada project yang sedang tidak dibuka pengguna.</summary>
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Documentation/EggPlacement-2026-10-20/NativeProject/Assets"))
            throw new InvalidOperationException("Run prepare_fixture.py, then execute validation only in the isolated NativeProject.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/PlayModeValidation.txt", "RUNNING isolated Play Mode validation\n");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    /// <summary>Mengukur clip yang tersedia terhadap sarang setinggi meja tanpa mengubah asset sumber.</summary>
    public static void PoseAudit()
    {
        Directory.CreateDirectory(Output);
        var root = new GameObject("Pose audit");
        var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Player/PlayerVisual.prefab"), root.transform);
        var hands = root.AddComponent<PlayerHeldTools>();
        var rig = visual.GetComponentsInChildren<Animator>().First(a => a.runtimeAnimatorController != null);
        var clips = AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>("Assets/Nature  Paradaise/Player/Animations/Player Animation Set.asset");
        var result = new StringBuilder();
        foreach (var clip in new[] { clips.handOverOneHand, clips.pickUpWaist, clips.placeItem, clips.planting })
        for (int step = 2; step <= 8; step++)
        foreach (float extraBend in new[] { 0f, 20f, 40f })
        {
            rig.Rebind(); rig.Update(0f);
            clip.SampleAnimation(rig.gameObject, clip.length * step / 10f);
            hands.ReachRightPalm(Vector3.zero, 0f);
            var spine = (Transform)typeof(PlayerHeldTools).GetField("placementSpine", Flags).GetValue(hands);
            spine.rotation = Quaternion.AngleAxis(extraBend, Vector3.right) * spine.rotation;
            Vector3 target = new(0f, .8f, .55f);
            Vector3 palm = hands.ReachRightPalm(target, 1f);
            result.AppendLine(clip.name + " time=" + step / 10f + " extraBend=" + extraBend + " distance=" + Vector3.Distance(target, palm));
        }
        File.WriteAllText(Output + "/PoseAudit.txt", result.ToString());
        Object.DestroyImmediate(root);
        EditorApplication.Exit(0);
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Play Mode validation timeout");
            if (tests != null && tests.MoveNext()) return;
            report.AppendLine("PASS_TOTAL " + checks + "; real Play Mode frames, production prefabs and player Animator; no user saves/scenes written.");
            Finish(0);
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL " + exception);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        File.WriteAllText(Output + "/PlayModeValidation.txt", report.ToString());
        SessionState.SetBool(Running, false);
        EditorApplication.update -= Tick;
        tests = null;
        EditorApplication.Exit(code);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
        report.AppendLine("PASS " + message);
    }

    static IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }

    static void Capture(Camera camera, string name)
    {
        var rt = new RenderTexture(1200, 900, 24);
        rt.Create();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous;
        rt.Release();
        Object.Destroy(texture);
        Object.Destroy(rt);
    }

    static IEnumerator RunTests()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Time.captureDeltaTime = 1f / 60f;
        for (int i = 0; i < 5; i++) yield return null;
        var clock = new GameObject("Fixture Clock").AddComponent<TimeManager>();
        clock.day = 10;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Fixture Ground";
        ground.transform.position = new Vector3(0, -.1f, 8);
        ground.transform.localScale = new Vector3(60, .2f, 60);
        var playerObject = new GameObject("Fixture Player", typeof(CharacterController));
        var capsule = playerObject.GetComponent<CharacterController>();
        capsule.height = 1.5f; capsule.radius = .24f; capsule.center = Vector3.up * .75f;
        var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Player/PlayerVisual.prefab"), playerObject.transform);
        visual.transform.localScale = Vector3.one;
        var player = playerObject.AddComponent<PlayerController>();
        var inventory = playerObject.AddComponent<Inventory>();
        var hotbar = playerObject.AddComponent<InventoryHotbarUI>();
        var hands = playerObject.GetComponent<PlayerHeldTools>();
        var carry = playerObject.AddComponent<PlayerAnimalCarry>();
        var held = playerObject.AddComponent<HeldItemPlacementSystem>();
        var shop = new GameObject("Fixture Animal Factory").AddComponent<ShopManager>();
        shop.playerInv = inventory;
        var egg = AnimalCareCatalog.Load().Product(AnimalType.Chicken);
        var duckEgg = AnimalCareCatalog.Load().Product(AnimalType.Duck);
        var camera = new GameObject("Fixture Camera").AddComponent<Camera>();
        camera.gameObject.AddComponent<AudioListener>();
        camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 1.35f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.1f, .14f, .18f);
        var light = new GameObject("Fixture Sun").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        var promptType = typeof(EggIncubator).Assembly.GetType("EggPlacementPromptDriver");
        Check(promptType != null, "Isolated fixture forwards production interaction on every game frame");
        var promptDriver = new GameObject("Fixture Prompt Driver").AddComponent(promptType);
        promptType.GetField("Player").SetValue(promptDriver, player);
        yield return null;
        Check(player.CharacterAnimator != null && player.CharacterAnimator.runtimeAnimatorController != null, "Production player Animator and controller connected");
        Check(player.CharacterAnimator.parameters.Any(p => p.name == "HandOverOneHand"), "Existing waist-height placement trigger available");
        report.AppendLine("RIG human=" + player.CharacterAnimator.isHuman + " grip=" + hands.GripPoint);
        foreach (var bone in player.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("RightHand") || t.name.Contains("RightArm") || t.name.Contains("RightForeArm") || t.name.Contains("Spine")))
            report.AppendLine("RIG " + bone.name + " parent=" + bone.parent?.name + " position=" + bone.position);
        var eggItemVisual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Animals/Visuals/Chicken Egg.prefab"));
        var eggBounds = eggItemVisual.GetComponentsInChildren<Renderer>().First().bounds;
        foreach (var renderer in eggItemVisual.GetComponentsInChildren<Renderer>()) eggBounds.Encapsulate(renderer.bounds);
        report.AppendLine("METRIC Chicken Egg prefab height=" + eggBounds.size.y);
        Object.Destroy(eggItemVisual);

        for (int level = 1; level <= 4; level++)
        {
            var layout = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nature  Paradaise/Prefabs/Coop/Interior/CoopInteriorLayout_Lv" + level + ".prefab"));
            layout.transform.localScale = Vector3.one * .75f;
            var station = layout.GetComponentInChildren<EggIncubator>(true);
            var home = new GameObject("Fixture Home " + level).AddComponent<AnimalHome>();
            home.homeId = "placement-fixture-" + level; home.kind = AnimalHousingKind.Coop; home.capacity = 1; home.door = home.transform;
            station.Bind(home);
            promptType.GetField("Station").SetValue(promptDriver, station);
            var stand = station.transform.Find("Interaction_PlayerStand");
            capsule.enabled = false; player.transform.position = stand.position; capsule.enabled = true;
            inventory.slots.Clear(); inventory.Add(egg, 3); hotbar.SelectSlot(0);
            var cover = station.transform.Find("Cover_Separate");
            Quaternion closedRotation = cover.localRotation;
            Vector3 closedPosition = cover.localPosition;
            camera.transform.position = station.Nest.position + new Vector3(-2f, 1.1f, -2f);
            camera.transform.LookAt(station.Nest.position + Vector3.down * .15f);
            yield return null;
            bool started;
            EggIncubatorMobileUI mobileActions = null;
            if (level == 4)
            {
                mobileActions = new GameObject("Fixture mobile actions", typeof(RectTransform)).AddComponent<EggIncubatorMobileUI>();
                var mobileWait = Wait(.15f); while (mobileWait.MoveNext()) yield return null;
                Check(EggIncubator.GetMobileTarget(player) == station, "Mobile target belongs to visible prompt");
                var button = mobileActions.GetComponentsInChildren<Button>(true).First(b => b.name == "Taruh Telur");
                report.AppendLine("MOBILE player=" + typeof(EggIncubatorMobileUI).GetField("player", Flags).GetValue(mobileActions) + " target=" + typeof(EggIncubatorMobileUI).GetField("target", Flags).GetValue(mobileActions) + " active=" + button.gameObject.activeInHierarchy + " enabled=" + button.interactable);
                Check(button.gameObject.activeInHierarchy && button.interactable, "Actual mobile placement button visible and enabled");
                Vector3 before = player.transform.position;
                capsule.enabled = false; player.transform.position += Vector3.right * 8f; capsule.enabled = true;
                Check(!station.RequestInteraction(player) && inventory.GetCount(egg) == 3, "Stale touch target out of range cannot consume egg");
                capsule.enabled = false; player.transform.position = before; capsule.enabled = true;
                button.onClick.Invoke();
                started = station.IsPlacing;
            }
            else started = station.TryBeginPlacement(player, out _);
            Check(started, "Lv" + level + " placement starts" + (level == 4 ? " via shared mobile entry" : ""));
            Check(inventory.GetCount(egg) == 3 && station.State == EggIncubatorState.Empty, "No early consumption before hand contact");
            Check(player.IsMovementLocked && clock.IsPaused && WorldInteractionPrompt.IsSuppressed, "Placement owns movement/clock/prompt locks");
            Check(!station.TryBeginPlacement(player, out _) && !station.TryInsert(inventory, 0, out _), "Repeated placement cannot bypass pending animation");
            var pauseOwner = new object(); clock.AcquirePause(pauseOwner);
            while (station.IsPlacing && Quaternion.Angle(closedRotation, cover.localRotation) < 5f) yield return null;
            Check(Quaternion.Angle(closedRotation, cover.localRotation) > 5f, "Cover actually opens over multiple frames");
            if (level == 1) Capture(camera, "01_Open");
            while (station.IsPlacing && !(bool)typeof(EggIncubator).GetField("reachingNest", Flags).GetValue(station)) yield return null;
            var wait = Wait(.45f); while (wait.MoveNext()) yield return null;
            if (level == 1) Capture(camera, "02_Reaching");
            Check(inventory.GetCount(egg) == 3, "Egg remains in inventory during reach");
            while (station.IsPlacing && inventory.GetCount(egg) == 3) yield return null;
            float contactGap = (float)typeof(EggIncubator).GetField("contactDistance", Flags).GetValue(station);
            report.AppendLine("METRIC Lv" + level + " palm to nest at contact=" + contactGap);
            report.AppendLine("CONTACT player=" + player.transform.position + " nest=" + station.Nest.position);
            foreach (var field in new[] { "rightArm", "rightForearm", "rightHand", "placementSpine" })
            {
                var bone = (Transform)typeof(PlayerHeldTools).GetField(field, Flags).GetValue(hands);
                report.AppendLine("CONTACT " + field + "=" + bone?.position);
            }
            Check(inventory.GetCount(egg) == 2 && station.State == EggIncubatorState.Incubating, "Exactly one egg committed at contact");
            Check(contactGap <= .04f, "Hand actually reaches egg contact point before inventory commit");
            Check((float)typeof(PlayerHeldTools).GetField("placementFootError", Flags).GetValue(hands) < .02f, "Placement body reach preserves animated foot contacts");
            if (level == 1) Capture(camera, "03_Contact");
            Check(home.Residents.Count == 1 && !home.HasRoom(AnimalType.Chicken), "Contact reserves exactly one real coop resident");
            var record = station.Snapshot;
            Check(record.startedDay == 10 && record.readyDay == 13, "Three-day incubation begins at contact");
            while (station.IsPlacing) yield return null;
            Check(!station.IsPlacing && !player.IsMovementLocked && !WorldInteractionPrompt.IsSuppressed, "All action locks released after animation");
            Check(clock.IsPaused, "Foreign clock pause remains owned by caller"); clock.ReleasePause(pauseOwner);
            Check(Quaternion.Angle(closedRotation, cover.localRotation) < .01f && Vector3.Distance(closedPosition, cover.localPosition) < .001f, "Cover returns to exact authored pose");
            if (level == 1) Capture(camera, "04_Closed");
            var data = EggIncubator.CaptureAll();
            EggIncubator.RestoreAll(data);
            Check(station.Snapshot.animalId == record.animalId, "Save snapshot restore preserves same incubating animal");
            clock.day = 13; EggIncubator.AdvanceAll(13);
            Check(station.State == EggIncubatorState.ChickReady && EggIncubator.FindAnimal(station.Snapshot).HasBeenBorn, "Normal hatch still works after animated insertion");
            AnimalGrowthSystem chick;
            if (mobileActions != null)
            {
                chick = EggIncubator.FindAnimal(station.Snapshot);
                wait = Wait(.15f); while (wait.MoveNext()) yield return null;
                var primaryButton = mobileActions.GetComponentsInChildren<Button>(true).First(b => b.name == "Taruh Telur");
                Check(primaryButton.gameObject.activeInHierarchy && primaryButton.interactable, "Mobile take button available after hatch");
                primaryButton.onClick.Invoke();
                Check(carry.HasAnimal && station.State == EggIncubatorState.Empty, "Mobile take starts real chicken pickup animation");
                wait = Wait(1.4f); while (wait.MoveNext()) yield return null;
                Check(carry.CarriedAnimal == chick && !player.IsMovementLocked, "Actual pickup animation transfers the same hatchling to hands");
                primaryButton.onClick.Invoke();
                wait = Wait(1.4f); while (wait.MoveNext()) yield return null;
                Check(!carry.HasAnimal && chick != null && !player.IsMovementLocked, "Mobile drop completes the existing placement animation");
                Object.Destroy(mobileActions.gameObject);
            }
            else Check(station.TryRelease(out chick, out _) && station.State == EggIncubatorState.Empty, "Existing release transfers same chick and empties nest");
            Object.Destroy(chick.gameObject);
            yield return null;
            clock.day = 10;
            inventory.slots.Clear(); inventory.Add(egg, 3); hotbar.SelectSlot(0);
            Check(station.TryBeginPlacement(player, out _), "Second animation starts on emptied station");
            station.enabled = false;
            yield return null;
            Check(inventory.GetCount(egg) == 3 && !player.IsMovementLocked && !clock.IsPaused && !WorldInteractionPrompt.IsSuppressed, "Disable before contact cancels with no egg loss or leaked locks");
            station.enabled = true; station.Bind(home);
            Check(station.TryBeginPlacement(player, out _), "Action can restart after cancellation");
            wait = Wait(1.08f); while (wait.MoveNext()) yield return null;
            Check(inventory.GetCount(egg) == 2, "Late cancellation begins after contact");
            data = EggIncubator.CaptureAll();
            EggIncubator.RestoreAll(data);
            Check(!station.IsPlacing && station.State == EggIncubatorState.Incubating && inventory.GetCount(egg) == 2 && !player.IsMovementLocked, "Restore during action retains committed egg and releases locks");
            var pendingAnimal = EggIncubator.FindAnimal(station.Snapshot);
            EggIncubator.RestoreAll(null);
            Object.Destroy(pendingAnimal.gameObject);
            yield return null;
            inventory.slots.Clear(); inventory.Add(duckEgg, 1); hotbar.SelectSlot(0);
            Check(!station.TryBeginPlacement(player, out _) && inventory.GetCount(duckEgg) == 1, "Duck egg rejected before animation");
            inventory.slots.Clear(); inventory.Add(egg, 3); hotbar.SelectSlot(0);
            home.capacity = 0;
            Check(!station.TryBeginPlacement(player, out _) && inventory.GetCount(egg) == 3, "Full coop rejected before animation");
            home.capacity = 1;
            Check(station.TryBeginPlacement(player, out _), "Changed-inventory cancellation fixture starts");
            inventory.RemoveFromSlot(0, 3);
            while (station.IsPlacing) yield return null;
            Check(station.State == EggIncubatorState.Empty && home.Residents.Count == 0 && !player.IsMovementLocked, "Missing source egg at contact creates no animal and releases player");
            inventory.Add(egg, 3); hotbar.SelectSlot(0);
            capsule.enabled = false;
            player.transform.position = stand.position - player.transform.forward * 1f;
            capsule.enabled = true;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Fixture blocked approach";
            blocker.transform.position = stand.position - player.transform.forward * .5f + Vector3.up * .75f;
            blocker.transform.localScale = new Vector3(2f, 1.5f, .2f);
            Physics.SyncTransforms();
            Check(station.TryBeginPlacement(player, out _), "Approach from edge of interaction radius starts");
            while (station.IsPlacing) yield return null;
            Check(inventory.GetCount(egg) == 3 && station.State == EggIncubatorState.Empty && !player.IsMovementLocked, "Blocked approach uses real collision and cancels without egg loss");
            Object.Destroy(blocker);
            Object.Destroy(layout); Object.Destroy(home.gameObject);
            yield return null;
        }
    }
}
#endif
