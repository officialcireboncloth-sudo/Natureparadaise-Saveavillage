using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TreeInputValidation
{
    const string Active = "TreeInputValidation.Active";
    static string Status => Path.Combine(Application.dataPath, "..", "TreeInputResults.txt");
    static IEnumerator tests;
    static double deadline;

    static TreeInputValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            tests = CheckInPlayMode();
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\','/').EndsWith("/tmp/TreePlanting/NativeProject/Assets"))
            throw new InvalidOperationException("Isolated fixture only");
        File.WriteAllText(Status, "START\n");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Active, true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException();
            if (tests.MoveNext()) return;
            Finish("PASS ALL", 0);
        }
        catch (Exception error) { Finish(error.ToString(), 1); }
    }

    static void Finish(string message, int exit)
    {
        File.AppendAllText(Status, message + "\n");
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(exit);
    }

    static void Check(bool passed, string label)
    {
        if (!passed) throw new InvalidOperationException(label);
        File.AppendAllText(Status, "PASS " + label + "\n");
    }

    static bool Busy(HeldItemPlacementSystem held) => (bool)typeof(HeldItemPlacementSystem)
        .GetField("actionBusy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(held);

    static IEnumerator CheckInPlayMode()
    {
        Time.captureDeltaTime = 1f / 60f;
        for (int i = 0; i < 5; i++) yield return null;
        Check(UnityEngine.Object.FindFirstObjectByType<PlayerController>() == null, "Initial scene has no player");
        new GameObject("Unrelated inventory").AddComponent<Inventory>();
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0, -.1f, 3);
        ground.transform.localScale = new Vector3(25, .2f, 25);
        var field = new GameObject("Test field").AddComponent<FieldArea>();
        field.GetComponent<BoxCollider>().enabled = false;
        var root = new GameObject("Late-loading player", typeof(CharacterController));
        root.GetComponent<CharacterController>().center = Vector3.up;
        root.AddComponent<PlayerController>();
        var inventory = root.AddComponent<Inventory>();
        var farming = root.AddComponent<FarmingTool>();
        var hotbar = root.AddComponent<InventoryHotbarUI>();
        var held = root.GetComponent<HeldItemPlacementSystem>();
        Check(held != null && held.isActiveAndEnabled, "Enabling late player hotbar attaches placement to the same player");
        for (int i = 0; i < 6; i++) yield return null;

        var asset = AssetDatabase.LoadAssetAtPath<ItemSO>(
            "Assets/Nature  Paradaise/Resources/Items/Farming/Seeds/Avocado Seedling.asset");
        Check(asset != null && asset.treeDefinition != null, "Production avocado seedling loads");
        var slots = Enumerable.Repeat<ItemStack>(null, inventory.CapacityAtBackpackLevel(0)).ToList();
        slots[0] = new ItemStack { item = asset, count = 3 };
        inventory.RestoreContents(0, slots);
        hotbar.SelectSlot(0);
        Check(held.HeldItem == asset, "Placement reads the selected hotbar item");

        foreach (var key in new[] { KeyCode.P, KeyCode.F, KeyCode.G })
        {
            Check(!WorldInteractionPrompt.IsDisplayedFor(held), key + " can be used without a displayed world prompt");
            Check(farming.TryGetCurrentTile(out var target, out int x, out int z) && target == field &&
                  (key == KeyCode.G || farming.CanPlantTreeAt(target, x, z)), key + " has a valid test context");
            var location = field.PlantingSurfacePosition(x, z);
            int count = inventory.GetCount(asset);
            int previouslyPlaced = PlacedWorldItem.Active.Count;
            TreeFixtureInput.Key = key;
            for (int i = 0; i < 10 && !Busy(held); i++) yield return null;
            TreeFixtureInput.Key = KeyCode.None;
            Check(Busy(held), key + " starts production placement input");
            for (int i = 0; i < 140 && Busy(held); i++) yield return null;
            Check(!Busy(held) && inventory.GetCount(asset) == count - 1 && PlacedWorldItem.Active.Count == previouslyPlaced + 1,
                key + " consumes exactly one seed and creates one world item");
            var placed = PlacedWorldItem.Active.Last();
            if (key == KeyCode.G)
                Check(placed.GetComponent<WorldTree>() == null && placed.GetComponent<Rigidbody>() != null,
                    "G drops a seed item with physics");
            else
            {
                var planted = placed.GetComponent<WorldTree>();
                Check(planted != null && planted.definition == asset.treeDefinition && planted.Progress.growth == 0 &&
                    Vector3.Distance(placed.transform.position, location) < .002f,
                    key + " plants a seed-stage avocado in the highlighted tile");
                root.GetComponent<CharacterController>().enabled = false;
                root.transform.position += Vector3.right * 1.5f;
                root.GetComponent<CharacterController>().enabled = true;
                Physics.SyncTransforms();
                for (int i = 0; i < 3; i++) yield return null;
            }
        }
        hotbar.enabled = false;
        hotbar.enabled = true;
        Check(root.GetComponents<HeldItemPlacementSystem>().Length == 1,
            "Re-enabling the hotbar keeps a single placement component");
    }
}
