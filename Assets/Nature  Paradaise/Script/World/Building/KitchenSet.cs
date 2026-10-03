using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Interactable dummy Kitchen Set yang dapat diganti mesh tanpa mengganti komponen.</summary>
[DisallowMultipleComponent]
public sealed class KitchenSet : MonoBehaviour
{
    [SerializeField] Inventory playerInventory;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.5f)] float interactionRadius = 2.5f;
    [SerializeField, Min(0f)] float promptHeight = 1.35f;
    Rect windowRect = new(0f, 0f, 940f, 650f);
    Vector2 scroll; PlayerController player; bool panelOpen; string feedback = string.Empty;
    void Awake() { ResolvePlayer(); CenterWindow(); }
    void OnDisable() => ClosePanel();
    void Update()
    {
        ResolvePlayer(); if (playerInventory == null) return;
        if (panelOpen) { if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)) ClosePanel(); return; }
        if (!PlayerInteractionTarget.ContainsPickup(playerInventory.transform, transform, interactionRadius)) return;
        float distance = Vector3.Distance(playerInventory.transform.position, transform.position);
        WorldInteractionPrompt.Request(this, transform, KitchenService.IsUnlocked
            ? $"{interactKey}: Cooking — Kitchen Lv.{KitchenService.Level}" : "Kitchen terkunci — Upgrade House ke Lv.2",
            distance, promptHeight);
        if (!PlayerInteractionTarget.PressPickup(playerInventory.transform, transform, interactKey, interactionRadius)) return;
        if (!KitchenService.IsUnlocked) { SaveLoadFeedback.Instance?.ShowMessage("Kitchen Set terbuka pada House Lv.2."); return; }
        OpenPanel();
    }
    void OnGUI()
    {
        if (!panelOpen) return;
        windowRect.width = Mathf.Clamp(Screen.width - 24f, 620f, 940f);
        windowRect.height = Mathf.Clamp(Screen.height - 24f, 430f, 650f);
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow,
            $"KITCHEN SET LV.{KitchenService.Level} — {KitchenService.FormatEquipment(KitchenService.AvailableEquipment)}");
    }
    void DrawWindow(int id)
    {
        GUILayout.Label("Bahan dibaca otomatis dari PLAYER INVENTORY + REFRIGERATOR.");
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (KitchenRecipeSO recipe in KitchenService.Recipes)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            bool learned = KitchenService.IsLearned(recipe); int max = learned ? KitchenService.GetMaxBatch(playerInventory, recipe) : 0;
            GUILayout.Label(learned ? $"{recipe.DisplayName}  |  Pernah dibuat: {KitchenService.GetCookedCount(recipe)}" : "??? — Recipe belum dipelajari");
            if (learned)
            {
                GUILayout.Label($"{string.Join(" + ", recipe.ingredients.Where(value => value?.item != null).Select(value => $"{value.item.itemName} x{value.amount}"))}  ->  {recipe.resultItem.itemName} x{recipe.resultAmount}");
                GUILayout.Label($"Alat: {KitchenService.FormatEquipment(recipe.requiredEquipment)} | Maks: {max}");
                if (!KitchenService.CanCook(playerInventory, recipe, 1, out string reason)) GUILayout.Label($"Status: {reason}");
                GUILayout.BeginHorizontal(); DrawCookButton(recipe, 1, "Cook 1"); DrawCookButton(recipe, 5, "Cook 5");
                DrawCookButton(recipe, max, $"Cook Max ({max})"); GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        if (!string.IsNullOrWhiteSpace(feedback)) GUILayout.Label(feedback);
        if (GUILayout.Button("Tutup [E / Esc]", GUILayout.Height(34f))) ClosePanel();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }
    void DrawCookButton(KitchenRecipeSO recipe, int batches, string label)
    {
        bool enabled = KitchenService.CanCook(playerInventory, recipe, batches, out _); bool previous = GUI.enabled; GUI.enabled = enabled;
        if (GUILayout.Button(label, GUILayout.Height(30f))) KitchenService.TryCook(playerInventory, recipe, batches, out feedback);
        GUI.enabled = previous;
    }
    void OpenPanel() { panelOpen = true; feedback = string.Empty; CenterWindow(); player?.AcquireMovementLock(this); TimeManager.Instance?.AcquirePause(this); WorldInteractionPrompt.AcquireSuppression(this); }
    void ClosePanel() { if (!panelOpen) return; panelOpen = false; player?.ReleaseMovementLock(this); TimeManager.Instance?.ReleasePause(this); WorldInteractionPrompt.ReleaseSuppression(this); }
    void ResolvePlayer() { if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory>(); if (player == null && playerInventory != null) player = playerInventory.GetComponent<PlayerController>(); }
    void CenterWindow() { windowRect.x = Mathf.Max(12f, (Screen.width - windowRect.width) * 0.5f); windowRect.y = Mathf.Max(12f, (Screen.height - windowRect.height) * 0.5f); }
}
